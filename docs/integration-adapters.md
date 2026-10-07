# Integration Adapters

Detailed documentation for each external integration adapter in the GPT Actions Orchestrator system.

## Overview

The system integrates with three external services through dedicated adapter services, each implementing a specific interface:

| Adapter | Interface | External Service | Protocol |
|---------|-----------|------------------|----------|
| `GitHubService` | `IGitHubService` | GitHub REST API | HTTPS + JSON |
| `PersonalLogManagerService` | `IPersonalLogManagerService` | Personal Log Manager API | HTTPS + NuciAPI (HMAC) |
| `SteamStoreService` | `ISteamStoreService` | Steam Storefront API | HTTP + JSON |

All adapters are registered as singletons and accessed through their interfaces by the `ActionsOrchestrator`.

---

## GitHubService

**File:** `GptActionsOrchestrator/Integrations/GitHub/Service/GitHubService.cs`

**Interface:** `IGitHubService` (`GptActionsOrchestrator/Integrations/GitHub/Service/IGitHubService.cs`)

### Configuration

**Settings:** `GitHubSettings` (`GptActionsOrchestrator/Integrations/GitHub/Configuration/GitHubSettings.cs`)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Username` | string | Yes | Default GitHub username for queries |
| `ApiKey` | string | No | Bearer token for authenticated requests |

### HTTP Client Configuration

```csharp
private static HttpClient CreateHttpClient(GitHubSettings settings)
{
    HttpClient client = HttpClientCreator.Create();

    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    if (!string.IsNullOrWhiteSpace(settings.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    return client;
}
```

**Constants:**
- `ApiBaseUrl = "https://api.github.com"`
- `ApiVersion = "2022-11-28"`

### Methods

#### GetUserRepositories(string username)

**Purpose:** Retrieve repositories for a GitHub user

**Logic:**
1. Determine if authenticated user is requested:
   - `username` is null/empty OR matches `settings.Username` (case-insensitive)
2. If authenticated user:
   - Use endpoint: `/user/repos?visibility=all&affiliation=owner,collaborator,organization_member&sort=updated&per_page=100&page={page}`
   - Effective username = `settings.Username`
3. If specific user:
   - Use endpoint: `/users/{username}/repos?sort=updated&per_page=100&page={page}`
   - Effective username = provided `username`
4. Paginate through all pages (100 per page) until empty result
5. Return `IEnumerable<GitHubRepository>`

**Logging:**
- Started: `MyOperation.GitHubUserRepositoriesRetrieval` with `Username`
- Success: `OperationStatus.Success` with `Count`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:** Logs error and rethrows

#### GetRepository(string username, string repositoryName)

**Purpose:** Retrieve a single GitHub repository

**Logic:**
1. Determine effective username (same logic as `GetUserRepositories`)
2. Build endpoint: `/repos/{encodedOwner}/{encodedRepository}`
3. GET request with standard headers
4. Deserialize to `GitHubRepository`
5. Return model

**Logging:**
- Started: `MyOperation.GitHubRepositoryRetrieval` with `Username`, `Repository`
- Success: `OperationStatus.Success`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:** Logs error and rethrows

#### GetRepositoryFile(string username, string repositoryName, string path)

**Purpose:** Retrieve raw content of a file in a GitHub repository

**Logic:**
1. Encode owner, repository, and path components
2. Split path by `/`, encode each segment, rejoin
3. Build request: `GET /repos/{owner}/{repo}/contents/{path}?ref=HEAD`
4. Set Accept header: `application/vnd.github.raw+json` (critical for raw content)
5. Send request, read response as string
6. Return raw content string

**Logging:**
- Started: `MyOperation.GitHubRepositoryFileContentRetrieval` with `Username`, `Repository`, `Path`
- Success: `OperationStatus.Success`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:** Logs error and rethrows

#### GetRepositoryReleases(string username, string repositoryName)

**Purpose:** Retrieve all releases for a GitHub repository

**Logic:**
1. Encode owner and repository
2. Paginate through `/repos/{owner}/{repo}/releases?per_page=100&page={page}`
3. Accumulate all pages until empty
4. Return `IEnumerable<GitHubRelease>`

**Logging:**
- Started: `MyOperation.GitHubRepositoryReleasesRetrieval` with `Username`, `Repository`
- Success: `OperationStatus.Success` with `Count`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:** Logs error and rethrows

### Response Models

**GitHubRepository** (`GptActionsOrchestrator/Integrations/GitHub/Service/Models/GitHubRepository.cs`):
```csharp
public sealed class GitHubRepository
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Language { get; set; }
    public int StargazersCount { get; set; }
    public IReadOnlyCollection<string> Topics { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsFork { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset PushedAt { get; set; }
}
```

**GitHubRelease** (`GptActionsOrchestrator/Integrations/GitHub/Service/Models/GitHubRelease.cs`):
```csharp
public sealed class GitHubRelease
{
    public string TagName { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPrerelease { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
```

### Error Semantics

All GitHub methods follow the same pattern:
1. Log operation start with context
2. Execute HTTP call synchronously (`.Result`)
3. On success: log success with metrics, return data
4. On exception: log failure with context and exception, rethrow

Exceptions propagate to `ActionsOrchestrator` → `ActionsController` → NuciAPI exception middleware → HTTP error response.

---

## PersonalLogManagerService

**File:** `GptActionsOrchestrator/Integrations/PersonalLogManager/Service/PersonalLogManagerService.cs`

**Interface:** `IPersonalLogManagerService` (`GptActionsOrchestrator/Integrations/PersonalLogManager/Service/IPersonalLogManagerService.cs`)

### Configuration

**Settings:** `PersonalLogManagerSettings` (`GptActionsOrchestrator/Integrations/PersonalLogManager/Configuartion/PersonalLogManagerSettings.cs`)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `BaseUrl` | string | Yes | Base URL of the Personal Log Manager API |
| `ApiKey` | string | Yes | Bearer token for API requests |
| `HmacSigningKey` | string | Yes | Shared key for HMAC signing and response validation |

**Note:** Directory name has typo: `Configuartion` (should be `Configuration`)

### Constants

```csharp
private static string DefaultLocalisation => "ro";
private static int DefaultCount => 1000;
```

### HTTP Client

Uses `NuciApiClient` (from `NuciAPI.Client` package) initialized with `BaseUrl`.

### Methods

#### GetPersonalLogs(string dateBeginning, string dateEnd, string template, string localisation, Dictionary<string, string> data, string count)

**Purpose:** Retrieve personal logs with filtering and templating

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `dateBeginning` | string | Start date (yyyy-MM-dd) |
| `dateEnd` | string | End date (yyyy-MM-dd) |
| `template` | string | Template identifier |
| `localisation` | string | Localisation code (defaults to "ro") |
| `data` | Dictionary<string,string> | Additional data payload |
| `count` | string | Maximum number of logs (defaults to 1000) |

**Logic:**
1. Build authorisation info:
   ```csharp
   var authorisation = new NuciApiRequestAuthorisationInfo
   {
       ClientId = securitySettings.ClientId,
       BearerToken = personalLogManagerSettings.ApiKey,
       HmacSharedSecretKey = personalLogManagerSettings.HmacSigningKey
   };
   ```
2. Build request via `BuildRequest()`
3. Call `NuciApiClient.SendRequestAsync<GetPersonalLogsRequest, GetPersonalLogsResponse>()`
4. Check `response.IsSuccessful`
5. If unsuccessful: throw `Exception(response.Message)`
6. If successful: return `PersonalLogs { Logs = response.Logs }`

**Logging:**
- Started: `MyOperation.GetPersonalLogs` with `Template`, `DateBeginning`, `DateEnd`, `Localisation`, `Count`
- Success: `OperationStatus.Success`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:** Logs error and rethrows

#### BuildRequest(...) — Private

**Purpose:** Construct `GetPersonalLogsRequest` with proper defaults and validation

**Logic:**
1. `Date` = `BuildDateRangeRegex(dateBeginning, dateEnd)` (can be null)
2. `Template` = provided template
3. `Localisation` = provided or `DefaultLocalisation` ("ro")
4. `Data` = provided data dictionary
5. `Count` = parsed count or `DefaultCount` (1000)

#### BuildDateRangeRegex(string dateBeginning, string dateEnd) — Public Static

**Purpose:** Convert date range to regex pattern for API filtering

**Logic:**
```csharp
public static string BuildDateRangeRegex(string dateBeginning, string dateEnd)
{
    // Both empty → no filter
    if (string.IsNullOrWhiteSpace(dateBeginning) && string.IsNullOrWhiteSpace(dateEnd))
        return null;

    // One empty → error
    if (string.IsNullOrWhiteSpace(dateBeginning) || string.IsNullOrWhiteSpace(dateEnd))
        throw new ArgumentException("Both the beginning and end dates must be provided when filtering by date.");

    // Parse dates
    DateOnly start = DateOnly.ParseExact(dateBeginning, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    DateOnly end = DateOnly.ParseExact(dateEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Validate order
    if (end < start)
        throw new ArgumentException($"The end date must be greater than or equal to the beginning date.");

    // Build date list
    List<string> dates = [];
    DateOnly current = start;
    while (current <= end)
    {
        dates.Add(current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        current = current.AddDays(1);
    }

    // Return regex pattern: (date1|date2|date3|...)
    return "(" + string.Join("|", dates) + ")";
}
```

**Unit Tests:** `GptActionsOrchestrator.UnitTests/Integrations/PersonalLogManager/Service/PersonalLogManagerServiceTests.cs`

### Request/Response Models

**GetPersonalLogsRequest** (`GptActionsOrchestrator/Integrations/PersonalLogManager/Client/GetPersonalLogsRequest.cs`):
```csharp
public sealed class GetPersonalLogsRequest : NuciApiRequest
{
    [HmacOrder(1)] public string Date { get; set; }
    [HmacOrder(2)] public string Time { get; set; }
    [HmacOrder(3)] public string Template { get; set; }
    [HmacOrder(4)] public string Localisation { get; set; }
    [HmacOrder(5)] public Dictionary<string, string> Data { get; set; }
    [HmacOrder(6)] public int Count { get; set; }
}
```

**GetPersonalLogsResponse** (`GptActionsOrchestrator/Integrations/PersonalLogManager/Client/GetPersonalLogsResponse.cs`):
```csharp
public sealed class GetPersonalLogsResponse : NuciApiSuccessResponse
{
    [JsonPropertyName("logs")]
    public List<string> Logs { get; set; } = [];

    [JsonPropertyName("count")]
    public int Count => Logs.Count;
}
```

**PersonalLogs** (`GptActionsOrchestrator/Integrations/PersonalLogManager/Service/Models/PersonalLogs.cs`):
```csharp
public sealed class PersonalLogs
{
    public List<string> Logs { get; set; } = [];
    public int Count => Logs.Count;
}
```

### HMAC Signing

The `NuciApiClient` automatically computes HMAC signature using the ordered properties (via `HmacOrder` attributes). The signature includes:
1. Date
2. Time (not set by orchestrator)
3. Template
4. Localisation
5. Data
6. Count

### Error Semantics

1. Log operation start with context
2. Build request and authorisation
3. Execute NuciAPI call
4. On unsuccessful response: throw `Exception(response.Message)`
5. On exception: log failure with context and exception, rethrow

Exceptions propagate to `ActionsOrchestrator` → `ActionsController` → NuciAPI exception middleware → HTTP error response.

---

## SteamStoreService

**File:** `GptActionsOrchestrator/Integrations/SteamStorefront/Service/SteamStoreService.cs`

**Interface:** `ISteamStoreService` (`GptActionsOrchestrator/Integrations/SteamStorefront/Service/ISteamStoreService.cs`)

### Configuration

No dedicated settings class. Uses hardcoded constants.

### Constants

```csharp
private static string StorefrontApiUrl => "http://store.steampowered.com/api";
private static string StorefrontApiCountry => "RO";
private static string StorefrontApiFilters => "basic";
private static string AppNamePattern => "\"name\": *\"([^\"]*)\"";
```

### HTTP Client

Uses `HttpClientCreator.Create()` from `NuciWeb.HTTP`.

### Methods

#### GetAppData(string appId)

**Purpose:** Retrieve Steam application name and basic metadata

**Logic:**
1. Build endpoint: `$"{StorefrontApiUrl}/appdetails?appids={appId}&cc={StorefrontApiCountry}&filters={StorefrontApiFilters}"`
2. GET request
3. Extract app name using regex: `"\"name\": *\"([^\"]*)\""`
4. Create `SteamAppEntity` with `Id = appId` and extracted `Name`
5. Return entity

**Logging:**
- Started: `MyOperation.SteamStoreAppDataRetrieval` with `AppId`
- Success: `OperationStatus.Success`
- Failure: `OperationStatus.Failure` with exception

**Exception Handling:**
- Catches all exceptions
- Logs error
- Returns `null` (does NOT rethrow)

**This is a key difference from other adapters!**

### Response Model

**SteamAppEntity** (`GptActionsOrchestrator/Integrations/SteamStorefront/Service/Models/SteamAppEntity.cs`):
```csharp
public sealed class SteamAppEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```

### Error Semantics

**Unique among adapters:** Returns `null` on failure instead of throwing.

Flow:
1. Log operation start
2. Try HTTP call and parsing
3. On exception: log error, `steamAppEntity` remains `null`
4. Log success (even if entity is null)
5. Return `steamAppEntity` (may be null)

This means `ActionsOrchestrator` receives `null` data for Steam failures, which gets wrapped in a successful `GetActionResponse` with `Data = null`.

---

## Adapter Comparison

| Aspect | GitHubService | PersonalLogManagerService | SteamStoreService |
|--------|---------------|---------------------------|-------------------|
| **Auth** | Optional Bearer | Bearer + HMAC | None |
| **Protocol** | HTTPS + JSON | HTTPS + NuciAPI | HTTP + JSON |
| **Client** | `HttpClient` | `NuciApiClient` | `HttpClient` |
| **Failure** | Log + rethrow | Log + rethrow | Log + return null |
| **Pagination** | Yes (100/page) | No (server-side) | No |
| **Logging** | Started/Success/Failure | Started/Success/Failure | Started/Success/Failure |
| **Config** | `GitHubSettings` | `PersonalLogManagerSettings` | Hardcoded constants |

---

## Adding a New Integration Adapter

See [Extension Guide](extension-guide.md) for the complete process.

### Required Steps

1. **Create interface** in `Integrations/{Name}/Service/I{Name}Service.cs`
2. **Create implementation** in `Integrations/{Name}/Service/{Name}Service.cs`
3. **Add configuration** (if needed) in `Integrations/{Name}/Configuration/{Name}Settings.cs`
4. **Register in DI** in `ServiceCollectionExtensions.cs`
5. **Add dispatch branch** in `ActionsOrchestrator.Get()`
6. **Add action to `GptAction`** registry
7. **Add unit tests** in `GptActionsOrchestrator.UnitTests/`
8. **Add integration tests** in `GptActionsOrchestrator.IntegrationTests/`

---

## Related Documentation

- [Architecture Overview](architecture-overview.md) - High-level context
- [Runtime Execution Flows](runtime-flows.md) - Detailed execution traces
- [Orchestration Engine](orchestration-engine.md) - How orchestrator calls adapters
- [Component Reference](component-reference.md) - Component details
- [Error Handling](error-handling.md) - Exception semantics
- [Extension Guide](extension-guide.md) - Adding new adapters
- [Source Map](source-map.md) - File locations