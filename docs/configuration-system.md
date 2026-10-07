# Configuration System

Detailed documentation of the configuration system: settings binding, precedence, secret management, and validation.

## Overview

The configuration system uses ASP.NET Core's standard configuration providers with typed settings classes bound to configuration sections.

## Settings Classes

### SecuritySettings

**File:** `GptActionsOrchestrator/Configuration/SecuritySettings.cs`
**Section:** `securitySettings`

```csharp
public sealed class SecuritySettings
{
    public string ClientId { get; set; }   // Outbound authorisation metadata
    public string ApiKey { get; set; }     // Inbound API key for /Actions
}
```

**Usage:**
- `ClientId`: Sent to Personal Log Manager in HMAC authorisation
- `ApiKey`: Validated by `NuciApiAuthorisation.ApiKey` in controller

### DataStoreSettings

**File:** `GptActionsOrchestrator/Configuration/DataStoreSettings.cs`
**Section:** `dataStoreSettings`

```csharp
public sealed class DataStoreSettings
{
    public string GptActionAliasesStorePath { get; set; }
}
```

**Usage:** Path passed to `JsonRepository<GptActionAliasDataObject>` constructor

### GitHubSettings

**File:** `GptActionsOrchestrator/Integrations/GitHub/Configuration/GitHubSettings.cs`
**Section:** `gitHubSettings`

```csharp
public sealed class GitHubSettings
{
    public string Username { get; set; }   // Default username for queries
    public string ApiKey { get; set; }     // Optional Bearer token
}
```

**Usage:**
- `Username`: Used when authenticated user requested (empty or matching username)
- `ApiKey`: Added as Bearer token to GitHub HTTP client if configured

### PersonalLogManagerSettings

**File:** `GptActionsOrchestrator/Integrations/PersonalLogManager/Configuartion/PersonalLogManagerSettings.cs`
**Section:** `personalLogManagerSettings`

**Note:** Directory name typo: `Configuartion` (should be `Configuration`)

```csharp
public sealed class PersonalLogManagerSettings
{
    public string BaseUrl { get; set; }        // API base URL
    public string ApiKey { get; set; }         // Bearer token
    public string HmacSigningKey { get; set; } // HMAC shared secret
}
```

**Usage:**
- `BaseUrl`: Passed to `NuciApiClient` constructor
- `ApiKey`: Used in `NuciApiRequestAuthorisationInfo.BearerToken`
- `HmacSigningKey`: Used in `NuciApiRequestAuthorisationInfo.HmacSharedSecretKey`

### NuciLoggerSettings

**Source:** NuciLog library (not in repository)
**Section:** `nuciLoggerSettings`

```csharp
// Properties as used in appsettings.json
public string LogFilePath { get; set; }        // Default: "logfile.log"
public bool IsFileOutputEnabled { get; set; }  // Default: true
```

**Usage:** Configured by NuciLog internally

## Configuration Binding

**File:** `GptActionsOrchestrator/ServiceCollectionExtensions.cs` → `AddConfigurations()`

### Binding Process

```csharp
public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
{
    // 1. Instantiate settings objects
    securitySettings = new SecuritySettings();
    dataStoreSettings = new DataStoreSettings();
    loggingSettings = new NuciLoggerSettings();
    gitHubSettings = new GitHubSettings();
    personalLogManagerSettings = new PersonalLogManagerSettings();

    // 2. Bind configuration sections to objects
    configuration.Bind(nameof(SecuritySettings), securitySettings);
    configuration.Bind(nameof(DataStoreSettings), dataStoreSettings);
    configuration.Bind(nameof(NuciLoggerSettings), loggingSettings);
    configuration.Bind(nameof(GitHubSettings), gitHubSettings);
    configuration.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);

    // 3. Register as singletons in DI
    services.AddSingleton(securitySettings);
    services.AddSingleton(dataStoreSettings);
    services.AddSingleton(loggingSettings);
    services.AddSingleton(gitHubSettings);
    services.AddSingleton(personalLogManagerSettings);

    return services;
}
```

**Key Points:**
- Settings objects instantiated once (static fields)
- `configuration.Bind()` maps section to object properties
- Section name matches class name (e.g., `SecuritySettings` → `securitySettings` section)
- Registered as singletons for injection into services

## Configuration Precedence

ASP.NET Core default host configuration order (later overrides earlier):

1. **`appsettings.json`** — Base configuration (committed to repo with placeholders)
2. **`appsettings.{Environment}.json`** — Environment-specific overrides (e.g., `appsettings.Development.json`)
3. **Environment variables** — System environment variables
4. **Command-line arguments** — Passed at startup

### Environment Variable Mapping

| Setting | Environment Variable |
|---------|---------------------|
| `securitySettings.apiKey` | `SecuritySettings__ApiKey` |
| `gitHubSettings.apiKey` | `GitHubSettings__ApiKey` |
| `personalLogManagerSettings.apiKey` | `PersonalLogManagerSettings__ApiKey` |
| `personalLogManagerSettings.hmacSigningKey` | `PersonalLogManagerSettings__HmacSigningKey` |
| `dataStoreSettings.gptActionAliasesStorePath` | `DataStoreSettings__GptActionAliasesStorePath` |

**Format:** `{SectionName}__{PropertyName}` (double underscore)

### Example: Production Deployment

```bash
# Environment variables
export SecuritySettings__ApiKey="prod-api-key-123"
export GitHubSettings__ApiKey="ghp_github-token"
export PersonalLogManagerSettings__ApiKey="plm-api-key"
export PersonalLogManagerSettings__HmacSigningKey="hmac-secret-key"
export DataStoreSettings__GptActionAliasesStorePath="/data/aliases.json"

# Run
dotnet GptActionsOrchestrator.dll
```

## appsettings.json Template

**File:** `GptActionsOrchestrator/appsettings.json`

```json
{
  "securitySettings": {
    "clientId": "GptActionsOrchestrator",
    "apiKey": "[[GPT_ACTIONS_ORCHESTRATOR_API_KEY]]"
  },
  "dataStoreSettings": {
    "gptActionAliasesStorePath": "Data/gpt-action-aliases.json"
  },
  "gitHubSettings": {
    "username": "[[GITHUB_USERNAME]]",
    "apiKey": "[[GITHUB_API_KEY]]"
  },
  "personalLogManagerSettings": {
    "baseUrl": "[[PERSONAL_LOG_MANAGER_BASE_URL]]",
    "apiKey": "[[PERSONAL_LOG_MANAGER_API_KEY]]",
    "hmacSigningKey": "[[PERSONAL_LOG_MANAGER_HMAC_SIGNING_KEY]]"
  },
  "nuciLoggerSettings": {
    "logFilePath": "logfile.log",
    "isFileOutputEnabled": true
  }
}
```

**Placeholder Convention:** `[[PLACEHOLDER_NAME]]` indicates required secret/value

## Secret Management

### Required Secrets

| Secret | Setting | Purpose |
|--------|---------|---------|
| Inbound API Key | `securitySettings.apiKey` | Authenticates callers to `/Actions` |
| GitHub Token | `gitHubSettings.apiKey` | Authenticated GitHub API calls (optional) |
| PLM API Key | `personalLogManagerSettings.apiKey` | Bearer token for Personal Log Manager |
| PLM HMAC Key | `personalLogManagerSettings.hmacSigningKey` | HMAC signing for PLM requests |

### Recommended Practices

1. **Never commit secrets** — Use placeholders in `appsettings.json`
2. **Environment variables** — For container deployments
3. **User secrets** — For local development (`dotnet user-secrets`)
4. **Key vaults** — For production (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
5. **Docker secrets** — For containerized deployments

### Local Development Setup

```bash
# Set user secrets
dotnet user-secrets set "SecuritySettings:ApiKey" "local-dev-key"
dotnet user-secrets set "GitHubSettings:ApiKey" "ghp_local-token"
dotnet user-secrets set "PersonalLogManagerSettings:ApiKey" "plm-local-key"
dotnet user-secrets set "PersonalLogManagerSettings:HmacSigningKey" "hmac-local-secret"
```

## Validation

### Startup Validation

No automatic validation at startup. Settings are bound but not validated until used.

### Runtime Validation Points

| Setting | Validated When | Failure Behavior |
|---------|----------------|------------------|
| `securitySettings.apiKey` | First request to `/Actions` | 401 Unauthorized |
| `gitHubSettings.apiKey` | First GitHub API call | GitHub returns 401 |
| `personalLogManagerSettings.baseUrl` | First PLM call | HttpRequestException |
| `personalLogManagerSettings.apiKey` | First PLM call | PLM returns auth error |
| `personalLogManagerSettings.hmacSigningKey` | First PLM call | PLM returns HMAC error |
| `dataStoreSettings.gptActionAliasesStorePath` | First alias lookup | FileNotFoundException |

### Recommended: Explicit Validation

Add validation in `Program.cs` or `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... existing code ...

    // Validate required settings
    var securitySettings = configuration.GetSection(nameof(SecuritySettings)).Get<SecuritySettings>();
    if (string.IsNullOrWhiteSpace(securitySettings?.ApiKey))
    {
        throw new InvalidOperationException("SecuritySettings:ApiKey is required");
    }

    // ... etc for other required settings ...
}
```

## Configuration in Tests

### Unit Tests

Settings mocked via Moq:
```csharp
var securitySettingsMock = new Mock<SecuritySettings>();
securitySettingsMock.Setup(x => x.ApiKey).Returns("test-key");
```

### Integration Tests

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionsWebApplicationFactory.cs`

```csharp
builder.ConfigureAppConfiguration((context, configBuilder) =>
{
    var configurationValues = new Dictionary<string, string?>
    {
        ["SecuritySettings:ApiKey"] = IntegrationTestSettings.ApiKey,  // "TestPassword!"
        ["NuciLoggerSettings:IsFileOutputEnabled"] = bool.FalseString
    };
    configBuilder.AddInMemoryCollection(configurationValues);
});
```

**Test Settings:** `IntegrationTestSettings.ApiKey = "TestPassword!"`

## Configuration Anti-Patterns to Avoid

1. **Hardcoding values** — Always use configuration binding
2. **Reading configuration in constructors** — Inject settings objects instead
3. **Using `IConfiguration` directly in services** — Use typed settings
4. **Committing real secrets** — Use placeholders and secret management
5. **Assuming settings are validated** — Validate explicitly where required

## Related Documentation

- [Architecture Overview](architecture-overview.md) - High-level context
- [Component Reference](component-reference.md) - Settings class details
- [API Boundary](api-boundary.md) - API key authorisation
- [Integration Adapters](integration-adapters.md) - Provider-specific config
- [Security](security.md) - Secret management best practices
- [Source Map](source-map.md) - File locations