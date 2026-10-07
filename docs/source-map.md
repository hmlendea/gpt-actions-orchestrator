# Source Map

Mapping between documentation topics and their corresponding source code locations.

## Documentation to Source Code Mapping

| Documentation File | Primary Source Files | Secondary Sources |
|--------------------|----------------------|-------------------|
| [architecture-overview.md](architecture-overview.md) | `Program.cs`, `Startup.cs`, `ServiceCollectionExtensions.cs` | All component files |
| [runtime-flows.md](runtime-flows.md) | `ActionsOrchestrator.cs`, adapter service files, `ActionsController.cs` | Middleware files |
| [component-reference.md](component-reference.md) | All `.cs` files in `GptActionsOrchestrator/` | Test files, configuration |
| [integration-adapters.md](integration-adapters.md) | `Integrations/GitHub/`, `Integrations/PersonalLogManager/`, `Integrations/SteamStorefront/` | Service interfaces, models |
| [orchestration-engine.md](orchestration-engine.md) | `Service/ActionsOrchestrator.cs`, `Service/IActionsOrchestrator.cs` | `Models/GptAction.cs`, `DataAccess/` |
| [api-boundary.md](api-boundary.md) | `Api/Controllers/ActionsController.cs`, `Api/Requests/`, `Api/Responses/` | `Startup.cs` middleware |
| [configuration-system.md](configuration-system.md) | `Configuration/DataStoreSettings.cs`, `Configuration/SecuritySettings.cs` | `appsettings.json`, `Program.cs` |
| [data-architecture.md](data-architecture.md) | `Data/gpt-action-aliases.json`, `DataAccess/GptActionAliasDataObject.cs` | `Models/GptAction.cs`, `Service/` |
| [logging-observability.md](logging-observability.md) | `Logging/MyLogInfoKey.cs`, `Logging/MyOperation.cs` | All service files (logging calls) |
| [error-handling.md](error-handling.md) | `Startup.cs` (middleware), `Service/ActionsOrchestrator.cs`, adapter service files | `Models/GptActionResponse.cs` |
| [security.md](security.md) | `Startup.cs` (security middleware), `Configuration/SecuritySettings.cs` | Adapter HMAC implementations |
| [testing-strategy.md](testing-strategy.md) | `GptActionsOrchestrator.UnitTests/`, `GptActionsOrchestrator.IntegrationTests/` | Test helpers, factory classes |

## Detailed File Mapping

### Core Application Files

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Program.cs` | Application entry point, host configuration | architecture-overview.md |
| `Startup.cs` | Middleware configuration, service registration | architecture-overview.md, api-boundary.md, security.md |
| `ServiceCollectionExtensions.cs` | Custom service registration | architecture-overview.md |
| `appsettings.json` | Application configuration | configuration-system.md |

### Service Layer

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Service/ActionsOrchestrator.cs` | Main orchestration logic | orchestration-engine.md, error-handling.md, testing-strategy.md |
| `Service/IActionsOrchestrator.cs` | Orchestrator interface | orchestration-engine.md |
| `ServiceCollectionExtensions.cs` | Service registration | architecture-overview.md |

### Models

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Models/GptAction.cs` | Action model with parameters | data-architecture.md, orchestration-engine.md, testing-strategy.md |
| `Models/GptActionResponse.cs` | Response model | api-boundary.md, error-handling.md |
| `DataAccess/GptActionAliasDataObject.cs` | Alias data object | data-architecture.md |

### Data

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Data/gpt-action-aliases.json` | Action alias definitions | data-architecture.md, configuration-system.md |

### API Layer

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Api/Controllers/ActionsController.cs` | HTTP API endpoint | api-boundary.md, testing-strategy.md |
| `Api/Requests/GetActionRequest.cs` | Request model | api-boundary.md |
| `Api/Responses/GetActionResponse.cs` | Response model | api-boundary.md |

### Integrations

#### GitHub Adapter

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Integrations/GitHub/Service/GitHubService.cs` | GitHub API client | integration-adapters.md, error-handling.md, logging-observability.md |
| `Integrations/GitHub/Configuration/` | GitHub-specific configuration | integration-adapters.md |
| `Models/GitHubRepository.cs` | GitHub repository model | integration-adapters.md |

#### Personal Log Manager Adapter

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Integrations/PersonalLogManager/Service/PersonalLogManagerService.cs` | PLM API client | integration-adapters.md, error-handling.md, logging-observability.md, security.md |
| `Integrations/PersonalLogManager/Client/PersonalLogManagerClient.cs` | HMAC HTTP client | security.md |
| `Integrations/PersonalLogManager/Configuartion/` | PLM-specific configuration | integration-adapters.md |
| `Models/PersonalLogManagerResponse.cs` | PLM response model | integration-adapters.md |

#### Steam Storefront Adapter

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Integrations/SteamStorefront/Service/SteamStoreService.cs` | Steam API client | integration-adapters.md, error-handling.md, logging-observability.md |
| `Integrations/SteamStorefront/Configuration/` | Steam-specific configuration | integration-adapters.md |
| `Models/SteamAppEntity.cs` | Steam app model | integration-adapters.md |

### Logging

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Logging/MyLogInfoKey.cs` | Custom log info keys | logging-observability.md |
| `Logging/MyOperation.cs` | Custom operations | logging-observability.md |

### Configuration

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `Configuration/DataStoreSettings.cs` | Data store configuration | configuration-system.md |
| `Configuration/SecuritySettings.cs` | Security configuration | configuration-system.md, security.md |

### Tests

#### Unit Tests

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `GptActionsOrchestrator.UnitTests/Service/ActionsOrchestratorTests.cs` | Orchestrator unit tests | testing-strategy.md |
| `GptActionsOrchestrator.UnitTests/Integrations/PersonalLogManager/PersonalLogManagerInvocationTests.cs` | PLM invocation tests | testing-strategy.md |
| `GptActionsOrchestrator.UnitTests/Models/GptActionTests.cs` | Action model tests | testing-strategy.md |

#### Integration Tests

| File | Purpose | Related Documentation |
|------|---------|----------------------|
| `GptActionsOrchestrator.IntegrationTests/Api/Controllers/ActionsControllerTests.cs` | Controller integration tests | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionsWebApplicationFactory.cs` | Test infrastructure | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionsHttpClientFactory.cs` | HTTP client factory | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionRequestPathBuilder.cs` | Request builder | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionResponseReader.cs` | Response reader | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionErrorResponseReader.cs` | Error response reader | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/Infrastructure/IntegrationTestSettings.cs` | Test settings | testing-strategy.md |
| `GptActionsOrchestrator.IntegrationTests/PersonalLogManagerInvocation.cs` | PLM test data | testing-strategy.md |

## Cross-Reference by Concern

### Authentication & Authorization

- **Files:** `Startup.cs` (security middleware), `Configuration/SecuritySettings.cs`
- **Docs:** security.md, api-boundary.md

### Configuration Management

- **Files:** `Configuration/*.cs`, `appsettings.json`, `Program.cs`
- **Docs:** configuration-system.md

### Error Handling

- **Files:** `Startup.cs` (exception middleware), `Service/ActionsOrchestrator.cs`, adapter service files
- **Docs:** error-handling.md

### Logging & Observability

- **Files:** `Logging/*.cs`, all service files (logging calls)
- **Docs:** logging-observability.md

### Data Flow

- **Files:** `Data/gpt-action-aliases.json`, `DataAccess/GptActionAliasDataObject.cs`, `Models/GptAction.cs`, `Service/ActionsOrchestrator.cs`
- **Docs:** data-architecture.md, orchestration-engine.md

### External Integrations

- **Files:** `Integrations/*/Service/*.cs`, `Integrations/*/Client/*.cs` (PLM only)
- **Docs:** integration-adapters.md

### HTTP API

- **Files:** `Api/Controllers/ActionsController.cs`, `Api/Requests/`, `Api/Responses/`, `Startup.cs`
- **Docs:** api-boundary.md

### Testing Infrastructure

- **Files:** `GptActionsOrchestrator.UnitTests/`, `GptActionsOrchestrator.IntegrationTests/Infrastructure/`
- **Docs:** testing-strategy.md

## Navigation Guidelines

### For New Contributors

1. **Start with** `architecture-overview.md` for high-level understanding
2. **Read** `runtime-flows.md` to understand execution patterns
3. **Consult** `component-reference.md` for detailed component information
4. **Refer to** specific adapter documentation for integration details
5. **Use** `orchestration-engine.md` for orchestration logic
6. **Check** `api-boundary.md` for HTTP interface details
7. **Review** `configuration-system.md` for setup instructions
8. **Examine** `data-architecture.md` for data flow understanding
9. **Consult** `logging-observability.md` and `error-handling.md` for operational concerns
10. **Review** `security.md` for security considerations
11. **Refer to** `testing-strategy.md` for testing approach

### For Debugging

1. **Check logs** - See `logging-observability.md` for log format and keys
2. **Trace execution** - Follow `runtime-flows.md` for action execution paths
3. **Verify configuration** - Consult `configuration-system.md` for settings
4. **Check error handling** - Review `error-handling.md` for failure patterns
5. **Verify integrations** - See `integration-adapters.md` for adapter-specific issues
6. **Test fixes** - Use `testing-strategy.md` for testing guidance

### For Feature Development

1. **Understand architecture** - Read `architecture-overview.md` and `component-reference.md`
2. **Follow patterns** - Consult `component-reference.md` for existing component patterns
3. **Add logging** - See `logging-observability.md` for logging conventions
4. **Handle errors** - Review `error-handling.md` for error handling patterns
5. **Secure implementation** - Consult `security.md` for security guidelines
6. **Write tests** - Follow `testing-strategy.md` for testing approaches
7. **Update documentation** - Ensure corresponding documentation file is updated

## Change Impact Analysis

### High-Impact Changes

| Change | Affected Documentation | Affected Source Files |
|--------|------------------------|-----------------------|
| Adding new action type | All documentation files | `Models/GptAction.cs`, `Service/ActionsOrchestrator.cs`, `Data/gpt-action-aliases.json` |
| Adding new integration adapter | `integration-adapters.md`, `orchestration-engine.md`, `api-boundary.md` | New `Integrations/*/` folder, `Service/ActionsOrchestrator.cs` |
| Changing authentication mechanism | `security.md`, `api-boundary.md` | `Startup.cs`, `Configuration/SecuritySettings.cs` |
| Changing logging framework | `logging-observability.md` | `Logging/*.cs`, all service files |
| Changing configuration system | `configuration-system.md` | `Configuration/*.cs`, `Program.cs`, `appsettings.json` |

### Low-Impact Changes

| Change | Affected Documentation | Affected Source Files |
|--------|------------------------|-----------------------|
| Adding new logging key | `logging-observability.md` | `Logging/MyLogInfoKey.cs`, specific service files |
| Adding new operation | `logging-observability.md` | `Logging/MyOperation.cs`, specific service files |
| Changing HTTP status codes | `api-boundary.md`, `error-handling.md` | `ActionsController.cs`, adapter service files |
| Adding new configuration setting | `configuration-system.md` | `Configuration/*.cs`, `appsettings.json` |
| Adding new unit test | `testing-strategy.md` | New test file in `UnitTests/` |
| Adding new integration test | `testing-strategy.md` | New test file in `IntegrationTests/` |

## Generated Information

**Generated:** $(date +%Y-%m-%d)
**Repository:** gpt-actions-orchestrator
**Commit:** $(git rev-parse HEAD 2>/dev/null || echo "unknown")
**Documentation Version:** 1.0.0

This source map is intended to be updated as the codebase evolves. When making changes to the codebase, please update the corresponding documentation files and this source map if structural changes occur.