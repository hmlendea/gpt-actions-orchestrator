# Orchestration Engine

Deep dive into the `ActionsOrchestrator` — the central dispatch and parameter processing component.

## Overview

The `ActionsOrchestrator` is the single point of action routing in the system. It:
1. Transforms raw query parameters into structured dictionaries
2. Resolves action identifiers (including alias indirection)
3. Dispatches to the appropriate integration adapter
4. Constructs the standard response envelope

**File:** `GptActionsOrchestrator/Service/ActionsOrchestrator.cs`
**Interface:** `IActionsOrchestrator` (`GptActionsOrchestrator/Service/IActionsOrchestrator.cs`)

## Constructor and Dependencies

```csharp
public sealed class ActionsOrchestrator(
    IGitHubService gitHubService,
    IPersonalLogManagerService personalLogManagerService,
    ISteamStoreService steamStoreService,
    IFileRepository<GptActionAliasDataObject> aliasesRepository) : IActionsOrchestrator
```

| Dependency | Purpose |
|------------|---------|
| `IGitHubService` | GitHub API operations |
| `IPersonalLogManagerService` | Personal Log Manager operations |
| `ISteamStoreService` | Steam Storefront operations |
| `IFileRepository<GptActionAliasDataObject>` | Alias lookup |

All dependencies are singletons injected via DI.

## Main Entry Point: Get(Dictionary<string, string> rawParameters)

```csharp
public GetActionResponse Get(Dictionary<string, string> rawParameters)
{
    // 1. Transform parameters
    Dictionary<string, object> parameters = BuildParameters(rawParameters);

    // 2. Resolve action (with alias support)
    GptAction action = GetGptActionFromParameters(rawParameters);

    // 3. Dispatch to adapter
    object data = DispatchAction(action, parameters);

    // 4. Build response
    return new GetActionResponse
    {
        GptActionName = action.ToString(),
        Data = data
    };
}
```

## Phase 1: Parameter Building — BuildParameters()

**Purpose:** Convert flat query string dictionary into nested structure supporting dotted keys.

**Input:** `Dictionary<string, string>` — raw query parameters from HTTP request
**Output:** `Dictionary<string, object>` — structured parameters with nested dictionaries

### Algorithm

```csharp
private Dictionary<string, object> BuildParameters(Dictionary<string, string> rawParameters)
{
    Dictionary<string, object> parameters = [];

    foreach (var pair in rawParameters)
    {
        if (pair.Key.Contains('.'))
        {
            // Dotted key: "parent.child" → parameters["parent"]["child"] = value
            string[] parts = pair.Key.Split('.', 2);
            string parentKey = parts[0];
            string childKey = parts[1];

            if (!parameters.ContainsKey(parentKey))
            {
                parameters[parentKey] = new Dictionary<string, string>();
            }

            ((Dictionary<string, string>)parameters[parentKey])[childKey] = pair.Value;
        }
        else
        {
            // Scalar key: "action" → parameters["action"] = value
            parameters[pair.Key] = pair.Value;
        }
    }

    return parameters;
}
```

### Transformation Examples

| Input Query | Output Structure |
|-------------|------------------|
| `action=GetPersonalLogs&data.mood=happy&data.energy=high` | `{ "action": "GetPersonalLogs", "data": { "mood": "happy", "energy": "high" } }` |
| `action=GetGitHubRepository&username=test&repository=repo` | `{ "action": "GetGitHubRepository", "username": "test", "repository": "repo" }` |
| `action=GetPersonalLogs&template=daily&data.key1=val1&data.key2=val2` | `{ "action": "GetPersonalLogs", "template": "daily", "data": { "key1": "val1", "key2": "val2" } }` |

### Key Behaviors

1. **Single-level nesting only:** Only the first `.` is treated as a separator. `data.mood.value` → `data["mood.value"]`
2. **Overwrites on conflict:** If `data` exists as scalar and `data.mood` appears, scalar is replaced by dictionary
3. **Type preservation:** Scalar values remain strings; nested values become `Dictionary<string, string>`
4. **Order independence:** Processing order doesn't affect final structure (last write wins for duplicates)

## Phase 2: Action Resolution — GetGptActionFromParameters()

**Purpose:** Extract and resolve the action identifier, applying alias mapping.

**Input:** `Dictionary<string, string>` — raw query parameters (before BuildParameters)
**Output:** `GptAction` — resolved canonical action

### Algorithm

```csharp
private GptAction GetGptActionFromParameters(Dictionary<string, string> parameters)
{
    // 1. Check if action parameter exists
    bool theGptActionIsSpecified = parameters.TryGetValue("action", out string gptActionId);

    if (!theGptActionIsSpecified)
    {
        return GptAction.Unknown;
    }

    // 2. Check for alias
    if (aliasesRepository.ContainsId(gptActionId))
    {
        gptActionId = aliasesRepository.Get(gptActionId).TargetActionId;
    }

    // 3. Convert to canonical GptAction
    return GptAction.FromString(gptActionId);
}
```

### Resolution Flow

```
Raw action parameter
       │
       ▼
┌──────────────────┐
│ ContainsId()?    │──No──▶ Use as-is
└──────────────────┘
       │Yes
       ▼
┌──────────────────┐
│ Get() → TargetId │
└──────────────────┘
       │
       ▼
┌──────────────────┐
│ FromString()     │──Unknown ID──▶ GptAction.Unknown
└──────────────────┘
       │
       ▼
  Canonical GptAction
```

### Alias Resolution Details

**Alias Repository:** `IFileRepository<GptActionAliasDataObject>` backed by `JsonRepository` reading `Data/gpt-action-aliases.json`

**Alias Data Object:**
```csharp
public sealed class GptActionAliasDataObject : EntityBase
{
    public string TargetActionId { get; set; }
}
```

**Alias File Format:**
```json
[
  { "id": "github.file.get", "targetActionId": "github.repository.file.get" },
  { "id": "github.repo.get", "targetActionId": "github.repository.get" },
  { "id": "personal.logs.get", "targetActionId": "personallogmanager.logs.get" }
]
```

**Resolution Rules:**
1. Alias lookup happens BEFORE `GptAction.FromString()`
2. Only one level of alias indirection (no chaining)
3. If alias target is also unknown, `FromString()` returns `GptAction.Unknown`
4. Alias `id` field matches the raw action parameter value

### GptAction.FromString() Logic

```csharp
public static GptAction FromString(string value)
{
    // 1. Direct registry key match (by Name, e.g., "GetGitHubRepository")
    if (values.ContainsKey(value))
        return values[value];

    // 2. Canonical ID match (by Id, e.g., "github.repository.get")
    if (values.Values.Any(v => v.Id == value))
        return values.Values.First(v => v.Id == value);

    // 3. Unknown
    return Unknown;
}
```

**This means both canonical names AND canonical IDs are accepted:**
- `action=GetGitHubRepository` ✓ (matches Name)
- `action=github.repository.get` ✓ (matches Id)
- `action=github.repo.get` ✓ (alias → canonical Id)

## Phase 3: Action Dispatch — DispatchAction()

**Purpose:** Route to the correct integration service based on resolved action.

**Implementation:** Explicit if-else chain in `Get()` method (not a separate method, but logically distinct)

### Dispatch Table

| GptAction | Adapter Method | Parameters Extracted |
|-----------|----------------|---------------------|
| `GetGitHubRepository` | `gitHubService.GetRepository()` | `username`, `repository` |
| `GetGitHubRepositoryFile` | `gitHubService.GetRepositoryFile()` | `username`, `repository`, `path` |
| `GetGitHubRepositoryReadme` | `gitHubService.GetRepositoryFile()` | `username`, `repository`, `path="README.md"` |
| `GetGitHubRepositoryReleases` | `gitHubService.GetRepositoryReleases()` | `username`, `repository` |
| `GetGitHubUserRepositories` | `gitHubService.GetUserRepositories()` | `username` |
| `GetPersonalLogs` | `personalLogManagerService.GetPersonalLogs()` | `date_beginning`, `date_end`, `template`, `localisation`, `data`, `count` |
| `GetSteamAppData` | `steamStoreService.GetAppData()` | `appId` |
| `Unknown` / other | `throw NotImplementedException` | — |

### Parameter Extraction Helper

```csharp
private TObject GetParameter<TObject>(Dictionary<string, object> parameters, string key)
{
    if (parameters.TryGetValue(key, out object value) && value is TObject typedValue)
    {
        return typedValue;
    }
    return default;
}
```

**Usage:**
```csharp
GetParameter<string>(parameters, "username")
GetParameter<Dictionary<string, string>>(parameters, "data")
```

**Behavior:**
- Returns `default(T)` if key missing or type mismatch
- For reference types: returns `null`
- For value types: returns `default` (e.g., `0` for int, `false` for bool)

### Special Cases

#### GetGitHubRepositoryReadme
```csharp
else if (action == GptAction.GetGitHubRepositoryReadme)
{
    data = gitHubService.GetRepositoryFile(
        GetParameter<string>(parameters, "username"),
        GetParameter<string>(parameters, "repository"),
        "README.md");  // Hardcoded path
}
```
Reuses `GetRepositoryFile` with fixed path.

#### GetPersonalLogs with Nested Data
```csharp
else if (action == GptAction.GetPersonalLogs)
{
    data = personalLogManagerService.GetPersonalLogs(
        GetParameter<string>(parameters, "date_beginning"),
        GetParameter<string>(parameters, "date_end"),
        GetParameter<string>(parameters, "template"),
        GetParameter<string>(parameters, "localisation"),
        GetParameter<Dictionary<string, string>>(parameters, "data"),
        GetParameter<string>(parameters, "count"));
}
```
Extracts nested `data` dictionary built by `BuildParameters()`.

## Phase 4: Response Construction

```csharp
return new GetActionResponse
{
    GptActionName = action.ToString(),  // Uses GptAction.Name (e.g., "GetGitHubRepository")
    Data = data
};
```

**Response Model:** `GetActionResponse` inherits from `NuciApiSuccessResponse`:
```json
{
  "success": true,
  "action": "GetGitHubRepository",
  "data": { ... }
}
```

## Error Handling in Orchestrator

### Unknown Action
```csharp
else
{
    throw new NotImplementedException($"The '{action.Id}' action is not supported.");
}
```
- Throws for `GptAction.Unknown` or any unhandled action
- Propagates to controller → NuciAPI exception middleware → HTTP error

### Missing Action Parameter
```csharp
if (!theGptActionIsSpecified)
{
    return GptAction.Unknown;
}
```
- Returns `GptAction.Unknown` which then triggers `NotImplementedException`

### Parameter Type Mismatch
- `GetParameter<T>()` returns `default(T)` on mismatch
- Adapter receives `null` or default value
- Adapter behavior varies (may throw, may return null, may error)

## Testing the Orchestrator

**Unit Tests:** `GptActionsOrchestrator.UnitTests/Service/ActionsOrchestratorTests.cs`

### Test Coverage Areas

1. **Action name resolution** — canonical names, canonical IDs, aliases
2. **Parameter passing** — correct parameters forwarded to adapters
3. **Response data** — adapter return value wrapped correctly
4. **Nested parameters** — `data.mood` → `data["mood"]` dictionary
5. **Error cases** — unknown action, missing action
6. **All 7 canonical actions** — each has dedicated test methods

### Test Pattern Example

```csharp
[Test]
public void GivenGetPersonalLogsWithNestedDataParameters_WhenGetIsCalled_ThenDataDictionaryIsPassedToService()
{
    Dictionary<string, string> capturedData = null;
    personalLogManagerServiceMock
        .Setup(service => service.GetPersonalLogs(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()))
        .Callback((string dateBeginning, string dateEnd, string template,
                   string localisation, Dictionary<string, string> data, string count)
            => capturedData = data)
        .Returns(new PersonalLogs());

    orchestrator.Get(new Dictionary<string, string>
    {
        { "action", "GetPersonalLogs" },
        { "date_beginning", "2012-09-05" },
        { "date_end", "2012-09-05" },
        { "template", "daily" },
        { "localisation", "ro" },
        { "count", "613" },
        { "data.mood", "happy" },
        { "data.energy", "high" }
    });

    Assert.That(capturedData, Is.Not.Null);
    Assert.That(capturedData["mood"], Is.EqualTo("happy"));
    Assert.That(capturedData["energy"], Is.EqualTo("high"));
}
```

## Design Constraints and Trade-offs

### Centralised Dispatch Branching
**Current:** Explicit if-else chain in `Get()`
**Pros:** Simple, traceable, easy to debug
**Cons:** Modification pressure as actions grow; violates Open/Closed Principle

### Synchronous Parameter Building
**Current:** `BuildParameters()` processes synchronously
**Impact:** Negligible (microseconds)

### No Action Metadata Registry
**Current:** Dispatch logic and action metadata co-located in `GptAction` and `ActionsOrchestrator`
**Alternative:** Could extract to attribute-based or configuration-driven registry

### Single-Level Nesting
**Current:** Only one `.` separator supported
**Limitation:** Cannot express `data.user.profile.name` → would need `data.user.profile.name` as key

## Extension Points

### Adding a New Action

1. Add to `GptAction.values` registry
2. Add static accessor property
3. Add dispatch branch in `ActionsOrchestrator.Get()`
4. Add parameter extraction
5. Add unit tests
6. Add integration tests

### Adding Alias Support for New Action

1. Add entry to `Data/gpt-action-aliases.json`
2. No code changes needed (alias resolution is generic)

## Related Documentation

- [Architecture Overview](architecture-overview.md) - High-level context
- [Runtime Execution Flows](runtime-flows.md) - Complete execution traces
- [Integration Adapters](integration-adapters.md) - Adapter implementations
- [Component Reference](component-reference.md) - Component details
- [GptAction Model](component-reference.md#gptactioncs) - Action registry details
- [Extension Guide](extension-guide.md) - Adding new actions
- [Testing Strategy](testing-strategy.md) - Test coverage