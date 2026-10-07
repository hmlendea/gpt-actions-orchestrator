# Component Reference

Detailed reference for each component in the GPT Actions Orchestrator system, including responsibilities, dependencies, lifetime, and source locations.

## Hosting and API Boundary

### Program.cs

**File:** `GptActionsOrchestrator/Program.cs`

**Responsibility:**
- Host bootstrap and web host configuration
- Process entry point

**Dependencies:**
- ASP.NET Core host builder (`Microsoft.Extensions.Hosting`)
- `Startup` class

**Lifetime:** Process-owned static entry point

**Implementation:**
```csharp
public sealed class Program
{
    public static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
```

### Startup.cs

**File:** `GptActionsOrchestrator/Startup.cs`

**Responsibility:**
- Middleware and endpoint pipeline composition
- Configuration binding to services

**Dependencies:**
- `IConfiguration` (injected)
- NuciAPI middleware components
- MVC controllers

**Lifetime:** Process-owned configuration root

**Implementation:**
```csharp
public sealed class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration => configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services
            .AddNuciApiScannerProtection()
            .AddConfigurations(Configuration)
            .AddCustomServices();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseNuciApiExceptionHandling();
        app.UseNuciApiScannerProtection();
        app.UseNuciApiRequestLogging();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### ServiceCollectionExtensions.cs

**File:** `GptActionsOrchestrator/ServiceCollectionExtensions.cs`

**Responsibility:**
- Dependency injection container configuration
- Typed settings binding from configuration
- Service registration

**Dependencies:**
- Configuration models (`SecuritySettings`, `DataStoreSettings`, etc.)
- Integration service interfaces and implementations
- NuciDAL repository
- NuciLog logger

**Lifetime:** Configuration-time only (called during startup)

**Implementation:**
```csharp
public static class ServiceCollectionExtensions
{
    // Settings storage (static for binding)
    private static SecuritySettings securitySettings;
    private static DataStoreSettings dataStoreSettings;
    private static NuciLoggerSettings loggingSettings;
    private static GitHubSettings gitHubSettings;
    private static PersonalLogManagerSettings personalLogManagerSettings;

    public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        // Instantiate settings objects
        securitySettings = new SecuritySettings();
        dataStoreSettings = new DataStoreSettings();
        loggingSettings = new NuciLoggerSettings();
        gitHubSettings = new GitHubSettings();
        personalLogManagerSettings = new PersonalLogManagerSettings();

        // Bind configuration sections
        configuration.Bind(nameof(SecuritySettings), securitySettings);
        configuration.Bind(nameof(DataStoreSettings), dataStoreSettings);
        configuration.Bind(nameof(NuciLoggerSettings), loggingSettings);
        configuration.Bind(nameof(GitHubSettings), gitHubSettings);
        configuration.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);

        // Register as singletons
        services.AddSingleton(securitySettings);
        services.AddSingleton(dataStoreSettings);
        services.AddSingleton(loggingSettings);
        services.AddSingleton(gitHubSettings);
        services.AddSingleton(personalLogManagerSettings);

        return services;
    }

    public static IServiceCollection AddCustomServices(this IServiceCollection services) => services
        // Alias repository
        .AddSingleton<IFileRepository<GptActionAliasDataObject>>(x =>
            new JsonRepository<GptActionAliasDataObject>(dataStoreSettings.GptActionAliasesStorePath))
        // Orchestrator
        .AddSingleton<IActionsOrchestrator, ActionsOrchestrator>()
        // Integration services
        .AddSingleton<IGitHubService, GitHubService>()
        .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
        .AddSingleton<ISteamStoreService, SteamStoreService>()
        // Logger
        .AddSingleton<ILogger, NuciLogger>();
}
```

## Orchestration and Action Model

### ActionsController.cs

**File:** `GptActionsOrchestrator/Api/Controllers/ActionsController.cs`

**Responsibility:**
- Inbound API route (`[Route("[controller]")]` → `/Actions`)
- Request processing wrapper
- API key authorisation delegation to NuciAPI

**Dependencies:**
- `IActionsOrchestrator` (orchestration service)
- `SecuritySettings` (for API key)

**Lifetime:** Singleton (framework-activated)

**Implementation:**
```csharp
[Route("[controller]")]
[ApiController]
public class ActionsController(
    IActionsOrchestrator actionsOrchestrator,
    SecuritySettings securitySettings) : NuciApiController
{
    private readonly NuciApiAuthorisation authorisation =
        NuciApiAuthorisation.ApiKey(securitySettings.ApiKey);

    [HttpGet]
    public ActionResult Get([FromQuery] GetActionRequest request)
        => ProcessRequest(
            request,
            () => actionsOrchestrator.Get(Request.Query.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToString()
            )),
            authorisation);
}
```

### ActionsOrchestrator.cs

**File:** `GptActionsOrchestrator/Service/ActionsOrchestrator.cs`

**Responsibility:**
- Action parsing and alias resolution
- Request parameter shaping (dotted key conversion)
- Adapter dispatch based on resolved action
- Response construction

**Dependencies:**
- `IGitHubService`
- `IPersonalLogManagerService`
- `ISteamStoreService`
- `IFileRepository<GptActionAliasDataObject>`

**Lifetime:** Singleton

**Implementation Details:**
```csharp
public sealed class ActionsOrchestrator(
    IGitHubService gitHubService,
    IPersonalLogManagerService personalLogManagerService,
    ISteamStoreService steamStoreService,
    IFileRepository<GptActionAliasDataObject> aliasesRepository) : IActionsOrchestrator
{
    public GetActionResponse Get(Dictionary<string, string> rawParameters)
    {
        // 1. Build parameters (convert dotted keys to nested dictionaries)
        Dictionary<string, object> parameters = BuildParameters(rawParameters);

        // 2. Resolve action (with alias indirection)
        GptAction action = GetGptActionFromParameters(rawParameters);

        // 3. Dispatch to appropriate adapter
        object data = DispatchAction(action, parameters);

        // 4. Build response
        return new GetActionResponse
        {
            GptActionName = action.ToString(),
            Data = data
        };
    }

    private Dictionary<string, object> BuildParameters(Dictionary<string, string> rawParameters)
    {
        Dictionary<string, object> parameters = [];

        foreach (var pair in rawParameters)
        {
            if (pair.Key.Contains('.'))
            {
                // Handle dotted keys: "data.mood" → parameters["data"]["mood"]
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
                // Handle scalar keys
                parameters[pair.Key] = pair.Value;
            }
        }

        return parameters;
    }

    private GptAction GetGptActionFromParameters(Dictionary<string, string> parameters)
    {
        // Check if action parameter exists
        bool theGptActionIsSpecified = parameters.TryGetValue("action", out string gptActionId);

        if (!theGptActionIsSpecified)
        {
            return GptAction.Unknown;
        }

        // Resolve alias if present
        if (aliasesRepository.ContainsId(gptActionId))
        {
            gptActionId = aliasesRepository.Get(gptActionId).TargetActionId;
        }

        return GptAction.FromString(gptActionId);
    }

    private object DispatchAction(GptAction action, Dictionary<string, object> parameters)
    {
        // Extract parameters with type checking
        T GetParameter<T>(string key) =>
            parameters.TryGetValue(key, out object value) && value is T typedValue
                ? typedValue
                : default;

        // Dispatch based on action
        if (action == GptAction.GetGitHubRepository)
        {
            return gitHubService.GetRepository(
                GetParameter<string>("username"),
                GetParameter<string>("repository"));
        }
        else if (action == GptAction.GetGitHubRepositoryFile)
        {
            return gitHubService.GetRepositoryFile(
                GetParameter<string>("username"),
                GetParameter<string>("repository"),
                GetParameter<string>("path"));
        }
        // ... (other actions follow same pattern)
        else
        {
            throw new NotImplementedException($"The '{action.Id}' action is not supported.");
        }
    }
}
```

### IActionsOrchestrator.cs

**File:** `GptActionsOrchestrator/Service/IActionsOrchestrator.cs`

**Responsibility:**
- Orchestration service interface contract

**Dependencies:** None (interface only)

**Implementation:**
```csharp
public interface IActionsOrchestrator
{
    public GetActionResponse Get(Dictionary<string, string> parameters);
}
```

### GptAction.cs

**File:** `GptActionsOrchestrator/Service/Models/GptAction.cs`

**Responsibility:**
- Canonical action identifier model
- Static registry of all supported actions
- String-to-action conversion with alias awareness

**Dependencies:** None (self-contained)

**Lifetime:** Static registry (process lifetime)

**Implementation:**
```csharp
public class GptAction : IEquatable<GptAction>
{
    // Static registry - single source of truth for canonical actions
    private static readonly Dictionary<string, GptAction> values = new()
    {
        { nameof(Unknown), new GptAction("unknown", nameof(Unknown)) },
        { nameof(GetGitHubRepository), new GptAction("github.repository.get", nameof(GetGitHubRepository)) },
        { nameof(GetGitHubRepositoryFile), new GptAction("github.repository.file.get", nameof(GetGitHubRepositoryFile)) },
        { nameof(GetGitHubRepositoryReadme), new GptAction("github.repository.readme.get", nameof(GetGitHubRepositoryReadme)) },
        { nameof(GetGitHubRepositoryReleases), new GptAction("github.repository.releases.get", nameof(GetGitHubRepositoryReleases)) },
        { nameof(GetGitHubUserRepositories), new GptAction("github.user.repositories.get", nameof(GetGitHubUserRepositories)) },
        { nameof(GetPersonalLogs), new GptAction("personallogmanager.logs.get", nameof(GetPersonalLogs)) },
        { nameof(GetSteamAppData), new GptAction("steam.store.app.get", nameof(GetSteamAppData)) },
    };

    public string Id { get; }      // Canonical action ID (e.g., "github.repository.get")
    public string Name { get; }    // Action name (e.g., "GetGitHubRepository")

    private GptAction(string id, string name)
    {
        Id = id;
        Name = name;
    }

    // Static accessors for each action
    public static GptAction Unknown => values[nameof(Unknown)];
    public static GptAction GetGitHubRepository => values[nameof(GetGitHubRepository)];
    // ... (other static accessors)

    public static Array GetValues() => values.Values.ToArray();

    // Equality and hashing
    public bool Equals(GptAction other) => !ReferenceEquals(other, null) && Id == other.Id;
    public override int GetHashCode() => $"{nameof(GptAction)}:{Id}".GetHashCode();
    public override string ToString() => Name;

    // Conversion from string (handles both canonical IDs and aliases via registry lookup)
    public static GptAction FromString(string value)
    {
        if (values.ContainsKey(value))                    // Direct registry hit
            return values[value];

        if (values.Values.Any(v => v.Id == value))        // ID lookup in values
            return values.Values.First(v => v.Id == value);

        return Unknown;                                   // Fallback
    }

    // Operator overloads
    public static bool operator ==(GptAction current, GptAction other) => current.Equals(other);
    public static bool operator !=(GptAction current, GptAction other) => !current.Equals(other);
    public static implicit operator string(GptAction obj) => obj.Name;
}
```

## Integration Adapters

### GitHubService.cs

**File:** `GptActionsOrchestrator/Integrations/GitHub/Service/GitHubService.cs`

**Responsibility:**
- GitHub REST API integration
- Repository, file, README, releases, and user repositories retrieval

**Dependencies:**
- `GitHubSettings` (configuration)
- `ILogger` (diagnostic logging)
- `HttpClient` (HTTP communication)
- `NuciWeb.HTTP` (HTTP client creation helper)
- `NuciExtensions` (JSON extension methods)
- `NuciLog.Core` (logging infrastructure)

**Lifetime:** Singleton

**Key Implementation Details:**
- Base URL: `https://api.github.com`
- API Version: `2022-11-28` (sent as `X-GitHub-Api-Version` header)
- Default Accept: `application/vnd.github+json`
- Optional Bearer token authentication from `GitHubSettings.ApiKey`
- Pagination for list endpoints (100 items per page)
- Special Accept header for file content: `application/vnd.github.raw+json`
- Exception handling: Log and rethrow (except where noted)

**Methods:**
1. `GetUserRepositories(string username)` - Handles authenticated vs anonymous user logic
2. `GetRepository(string username, string repositoryName)` - Single repository retrieval
3. `GetRepositoryFile(string username, string repositoryName, string path)` - Raw file content
4. `GetRepositoryReleases(string username, string repositoryName)` - Paginated releases

### PersonalLogManagerService.cs

**File:** `GptActionsOrchestrator/Integrations/PersonalLogManager/Service/PersonalLogManagerService.cs`

**Responsibility:**
- Personal Log Manager API integration via NuciAPI
- Personal log retrieval with date range, template, localisation, and data payload

**Dependencies:**
- `PersonalLogManagerSettings` (configuration)
- `SecuritySettings` (for ClientId in HMAC)
- `ILogger` (diagnostic logging)
- `NuciApiClient` (NuciAPI communication)
- `NuciAPI.Requests` and `NuciAPI.Responses` (request/response models)
- `NuciLog.Core` (logging infrastructure)
- `NuciSecurity.HMAC` (HMAC signing)
- `System.Globalization` (date parsing)
- `System.Collections.Generic` (data handling)

**Lifetime:** Singleton

**Key Implementation Details:**
- Builds `GetPersonalLogsRequest` with HMAC-ordered properties
- Uses `NuciApiClient.SendRequestAsync<GetPersonalLogsRequest, GetPersonalLogsResponse>()`
- HMAC property order: Date(1), Time(2), Template(3), Localisation(4), Data(5), Count(6)
- Date range converted to regex pattern: `(yyyy-mm-dd|yyyy-mm-dd|...)`
- Throws exception on unsuccessful API response
- Returns `PersonalLogs` model with `Logs` list and `Count` property

### SteamStoreService.cs

**File:** `GptActionsOrchestrator/Integrations/SteamStorefront/Service/SteamStoreService.cs`

**Responsibility:**
- Steam Storefront API integration
- Steam application metadata retrieval

**Dependencies:**
- `ILogger` (diagnostic logging)
- `HttpClient` (HTTP communication)
- `NuciLog.Core` (logging infrastructure)
- `NuciWeb.HTTP` (HTTP client creation helper)
- `System.Text.RegularExpressions` (name extraction)
- `System.Collections.Generic` (collections)

**Lifetime:** Singleton

**Key Implementation Details:**
- Base URL: `http://store.steampowered.com/api`
- Country: `RO` (Romania)
- Filters: `basic`
- App name extraction via regex: `"\"name\": *\"([^\"]*)\""`
- Exception handling: Log error, return `null` (does not rethrow)
- Returns `SteamAppEntity` with `Id` and `Name` properties

### Integration Service Interfaces

**Files:**
- `GptActionsOrchestrator/Integrations/GitHub/Service/IGitHubService.cs`
- `GptActionsOrchestrator/Integrations/PersonalLogManager/Service/IPersonalLogManagerService.cs`
- `GptActionsOrchestrator/Integrations/SteamStorefront/Service/ISteamStoreService.cs`

**Responsibility:**
- Define contracts for integration adapters
- Enable dependency injection and mocking for testing

**Implementation:** Standard C# interfaces with method signatures matching implementations

## Configuration System

### DataStoreSettings.cs

**File:** `GptActionsOrchestrator/Configuration/DataStoreSettings.cs`

**Responsibility:**
- Configuration for alias datastore path

**Properties:**
- `GptActionAliasesStorePath` (string): Path to JSON alias file

**Binding:** Bound from `dataStoreSettings` section in `appsettings.json`

### SecuritySettings.cs

**File:** `GptActionsOrchestrator/Configuration/SecuritySettings.cs`

**Responsibility:**
- Configuration for security-related settings

**Properties:**
- `ClientId` (string): Identifier for outbound authorisation metadata
- `ApiKey` (string): Expected API key for inbound `/Actions` requests

**Binding:** Bound from `securitySettings` section in `appsettings.json`

### GitHubSettings.cs

**File:** `GptActionsOrchestrator/Integrations/GitHub/Configuration/GitHubSettings.cs`

**Responsibility:**
- Configuration for GitHub integration

**Properties:**
- `Username` (string): Default GitHub username for queries
- `ApiKey` (string): Optional bearer token for authenticated requests

**Binding:** Bound from `gitHubSettings` section in `appsettings.json`

### PersonalLogManagerSettings.cs

**File:** `GptActionsOrchestrator/Integrations/PersonalLogManager/Configuartion/PersonalLogManagerSettings.cs`

**Note:** Directory name contains typo (`Configuartion` instead of `Configuration`)

**Responsibility:**
- Configuration for Personal Log Manager integration

**Properties:**
- `BaseUrl` (string): Base URL of the Personal Log Manager API
- `ApiKey` (string): Bearer token for API requests
- `HmacSigningKey` (string): Shared key for HMAC signing and response validation

**Binding:** Bound from `personalLogManagerSettings` section in `appsettings.json`

### NuciLoggerSettings.cs

**File:** Part of NuciLog library (not in repository)

**Responsibility:**
- Configuration for logging settings

**Properties (as used):**
- `logFilePath` (string): File path for logger output
- `isFileOutputEnabled` (bool): Enables/disables file-based logging

**Binding:** Bound from `nuciLoggerSettings` section in `appsettings.json`

## Data Access and Objects

### GptActionAliasDataObject.cs

**File:** `GptActionsOrchestrator/DataAccess/DataObjects/GptActionAliasDataObject.cs`

**Responsibility:**
- Data object for action alias mapping
- Inherits from `NuciDAL.DataObjects.EntityBase`

**Properties:**
- `TargetActionId` (string): The canonical action ID this alias maps to

**Inheritance:**
```csharp
public sealed class GptActionAliasDataObject : EntityBase
{
    public string TargetActionId { get; set; }
}
```

**Usage:** Used by `JsonRepository<GptActionAliasDataObject>` for alias resolution

### Alias JSON File

**File:** `GptActionsOrchestrator/Data/gpt-action-aliases.json`

**Responsibility:**
- Persistent storage for action alias mappings
- Loaded at runtime by `JsonRepository`

**Format:** JSON array of objects with `id` and `targetActionId` fields

**Example:**
```json
[
  { "id": "github.file.get", "targetActionId": "github.repository.file.get" },
  { "id": "github.repo.get", "targetActionId": "github.repository.get" },
  { "id": "personal.logs.get", "targetActionId": "personallogmanager.logs.get" }
]
```

## Logging Infrastructure

### MyLogInfoKey.cs

**File:** `GptActionsOrchestrator/Logging/MyLogInfoKey.cs`

**Responsibility:**
- Custom log info keys for structured logging
- Extends `NuciLog.Core.LogInfoKey`

**Keys:**
- `GptAction` - Action name being processed
- `AppId` - Steam application ID
- `Count` - Result count (logs, repositories, etc.)
- `DateBeginning` - Personal log date range start
- `DateEnd` - Personal log date range end
- `Localisation` - Personal log localisation
- `Path` - File path (GitHub)
- `Reference` - Generic reference
- `Repository` - GitHub repository name
- `Template` - Personal log template
- `Username` - GitHub username

**Implementation:**
```csharp
public sealed class MyLogInfoKey : LogInfoKey
{
    private MyLogInfoKey(string name) : base(name) { }

    public static LogInfoKey GptAction => new MyLogInfoKey(nameof(GptAction));
    // ... (other static keys)
}
```

### MyOperation.cs

**File:** `GptActionsOrchestrator/Logging/MyOperation.cs`

**Responsibility:**
- Custom operation identifiers for structured logging
- Extends `NuciLog.Core.Operation`

**Operations:**
- `GetPersonalLogs` - Personal log retrieval
- `GitHubRepositoryRetrieval` - GitHub repository fetch
- `GitHubRepositoryFileContentRetrieval` - GitHub file content fetch
- `GitHubRepositoryReleasesRetrieval` - GitHub releases fetch
- `GitHubUserRepositoriesRetrieval` - GitHub user repositories fetch
- `SteamStoreAppDataRetrieval` - Steam app data fetch

**Implementation:**
```csharp
public sealed class MyOperation : Operation
{
    private MyOperation(string name) : base(name) { }

    public static Operation GetPersonalLogs => new MyOperation(nameof(GetPersonalLogs));
    public static Operation GitHubRepositoryRetrieval => new MyOperation(nameof(GitHubRepositoryRetrieval));
    // ... (other static operations)
}
```

## Response Models

### GetActionRequest.cs

**File:** `GptActionsOrchestrator/Api/Requests/GetActionRequest.cs`

**Responsibility:**
- Inbound request model for `/Actions` endpoint
- Binds `action` query parameter

**Properties:**
- `GptActionName` (string): The action identifier from query string

**Attributes:**
- `[FromQuery(Name = "action")]` - Binds from query parameter
- `[JsonPropertyName("action")]` - JSON serialization name

**Inheritance:** `NuciApiRequest` (base class from NuciAPI)

### GetActionResponse.cs

**File:** `GptActionsOrchestrator/Api/Responses/GetActionResponse.cs`

**Responsibility:**
- Outbound response model for successful `/Actions` requests
- Wraps provider-specific data in standard envelope

**Properties:**
- `GptActionName` (string): Name of the action that was processed
- `Data` (object): Provider-specific response data

**Attributes:**
- `[FromQuery(Name = "action")]` - Binds from query parameter (inherited)
- `[JsonPropertyName("action")]` - JSON serialization name (inherited)

**Inheritance:** `NuciApiSuccessResponse` (provides `success` property)

### Integration Response Models

**GitHub Models:**
- `GitHubRepository` - Repository metadata (name, description, language, stars, etc.)
- `GitHubRelease` - Release information (tag name, name, body, draft/prerelease flags, dates)

**Personal Log Manager Models:**
- `GetPersonalLogsRequest` - HMAC-ordered request for personal logs
- `GetPersonalLogsResponse` - Response containing `Logs` list and `Count`
- `PersonalLogs` - Service-layer model mirroring response

**Steam Model:**
- `SteamAppEntity` - Steam application data (Id, Name)

## Testing Infrastructure

### Unit Tests

**Location:** `GptActionsOrchestrator.UnitTests/`

**Structure:**
- `Service/ActionsOrchestratorTests.cs` - Orchestrator logic tests
- `Integrations/PersonalLogManager/Service/PersonalLogManagerServiceTests.cs` - PLM service tests

**Frameworks:**
- `NUnit` - Testing framework
- `Moq` - Mocking framework (replaced NSubstitute)
- `NuciDAL.Repositories` - Repository abstractions

**Test Patterns:**
- Parameter validation
- Dependency call verification
- Response data correctness
- Exception handling
- Alias resolution
- Nested parameter handling

### Integration Tests

**Location:** `GptActionsOrchestrator.IntegrationTests/`

**Structure:**
- `Infrastructure/` - Test support classes
  - `ActionsWebApplicationFactory.cs` - Custom test host
  - `IntegrationTestSettings.cs` - Test configuration
  - `ActionRequestPathBuilder.cs` - URL building helpers
  - `ActionResponseReader.cs` - Response parsing helpers
  - `ActionErrorResponseReader.cs` - Error response parsing helpers
  - `PersonalLogManagerInvocation.cs` - PLM request tracking

**Frameworks:**
- `Microsoft.AspNetCore.Mvc.Testing` - Web application factory
- `Moq` - Mocking framework
- `NUnit` - Testing framework

**Approach:**
- Hosts real service with `WebApplicationFactory`
- Preserves real middleware, authorisation, routing, controller
- Substitutes only outbound provider interfaces and operational logging
- Tests complete pipeline from HTTP request to JSON response

## Release Infrastructure

### release.sh

**File:** `release.sh`

**Responsibility:**
- Automated release script execution
- Downloads and runs version-specific release script

**Implementation:**
```bash
#!/bin/bash
DOTNET_VERSION='10.0'
RELEASE_SCRIPT_URL="https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/${DOTNET_VERSION}.sh"
wget --quiet -O - "${RELEASE_SCRIPT_URL}" | bash /dev/stdin ${@}
```

## Related Documentation

- [Architecture Overview](architecture-overview.md) - High-level system context
- [Runtime Execution Flows](runtime-flows.md) - Detailed execution traces
- [Orchestration Engine](orchestration-engine.md) - Dispatch logic deep dive
- [Integration Adapters](integration-adapters.md) - Adapter implementations
- [API Boundary](api-boundary.md) - Controller and middleware details
- [Configuration System](configuration-system.md) - Settings binding and precedence
- [Data Architecture](data-architecture.md) - Data models and transformations
- [Logging and Observability](logging-observability.md) - Diagnostic signal flow
- [Error Handling](error-handling.md) - Exception propagation and handling
- [Testing Strategy](testing-strategy.md) - Test coverage and architecture
- [Extension Guide](extension-guide.md) - Adding new capabilities
- [Source Map](source-map.md) - Complete file-to-responsibility mapping