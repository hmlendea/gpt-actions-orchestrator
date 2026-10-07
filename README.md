[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![License](https://img.shields.io/github/license/hmlendea/gpt-actions-orchestrator)](https://github.com/hmlendea/gpt-actions-orchestrator/blob/main/LICENSE)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/gpt-actions-orchestrator)](https://github.com/hmlendea/gpt-actions-orchestrator/releases/latest)
[![Build Status](https://github.com/hmlendea/gpt-actions-orchestrator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/gpt-actions-orchestrator/actions/workflows/dotnet.yml)

# GPT Actions Orchestrator

An ASP.NET Core web service that provides a single HTTP endpoint for executing GPT Actions. The service accepts an action identifier plus query parameters, resolves aliases where applicable, dispatches to a dedicated integration service (GitHub, Personal Log Manager, or Steam Storefront), and returns a normalised response.

> **Project status:** Active development

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Use Cases](#-use-cases)
- [Usage](#-usage)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Development](#-development)
- [Architecture](#-architecture)
- [Documentation](#-documentation)
- [Contributing](#-contributing)
- [Security](#-security)
- [License](#-license)

## ✨ Capabilities

- Single `/Actions` endpoint accepting action name/ID and parameters
- Action alias resolution from local JSON datastore
- Three integration adapters: GitHub REST API, Personal Log Manager (NuciAPI with HMAC), Steam Storefront API
- API key authentication with NuciAPI middleware
- Structured operation logging with NuciLog
- Global exception handling with standardised error responses

## 🎯 Use Cases

- **GPT Action backend:** Serve as the HTTP backend for OpenAI GPT Actions
- **GitHub data retrieval:** Fetch repository metadata, file contents, releases, user repositories
- **Personal log queries:** Retrieve structured personal log entries with date ranges and localisation
- **Steam app metadata:** Look up Steam application details by app ID

## 🚀 Usage

The service exposes one endpoint:

```http
GET /Actions?action=github-repository&username=owner&repository=repo
Authorization: Bearer <api-key>
```

**Example response:**
```json
{
  "success": true,
  "action": "github-repository",
  "data": {
    "name": "repo",
    "fullName": "owner/repo",
    "description": "Repository description",
    "stars": 42
  }
}
```

Supported actions:
- `github-repository` — Get repository metadata
- `github-repository-file` — Get file contents from repository
- `github-repository-releases` — Get repository releases
- `github-user-repositories` — Get user's repositories
- `personal-log-manager-get-personal-logs` — Query personal logs
- `steam-app-data` — Get Steam app metadata

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| .NET Runtime | 10.0 | 10.0.x latest patch |
| .NET SDK (for local builds) | 10.0 | 10.0.x latest patch |

## 📦 Installation

```bash
git clone https://github.com/hmlendea/gpt-actions-orchestrator
cd gpt-actions-orchestrator
dotnet restore GptActionsOrchestrator.slnx
```

## ⚙️ Configuration

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `GptActionsOrchestrator/appsettings.json` | Application-wide | Base configuration for all settings |
| `GptActionsOrchestrator/appsettings.Development.json` | Development | Local overrides (gitignored) |

### Settings

| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `SecuritySettings` | `ClientId` | `string` | `GptActionsOrchestrator` | Yes | Client identifier used for outbound authorisation metadata |
| `SecuritySettings` | `ApiKey` | `string` | — | Yes | API key expected for inbound `/Actions` requests |
| `DataStoreSettings` | `GptActionAliasesStorePath` | `string` | `Data/gpt-action-aliases.json` | Yes | File path used for action alias mapping |
| `GitHubSettings` | `Username` | `string` | — | Yes | Default GitHub username for repository queries |
| `GitHubSettings` | `ApiKey` | `string` | — | No | Bearer token for authenticated GitHub API calls |
| `PersonalLogManagerSettings` | `BaseUrl` | `string` | — | Yes | Base URL of the Personal Log Manager API |
| `PersonalLogManagerSettings` | `ApiKey` | `string` | — | Yes | Bearer token for Personal Log Manager requests |
| `PersonalLogManagerSettings` | `HmacSigningKey` | `string` | — | Yes | Shared key for HMAC signing and response validation |
| `NuciLoggerSettings` | `LogFilePath` | `string` | `logfile.log` | No | File path for logger output when file logging is enabled |
| `NuciLoggerSettings` | `IsFileOutputEnabled` | `bool` | `true` | No | Enables or disables file-based logging |

### Secret Management

Store `SecuritySettings.ApiKey`, `GitHubSettings.ApiKey`, `PersonalLogManagerSettings.ApiKey`, and `PersonalLogManagerSettings.HmacSigningKey` in a secure secret source for non-local environments:
- Environment variables (e.g., `SecuritySettings__ApiKey`)
- Azure Key Vault / AWS Secrets Manager / HashiCorp Vault
- GitHub Actions secrets for CI/CD

### Precedence

Configuration precedence follows the ASP.NET Core default host order, where later providers override earlier providers:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command-line arguments

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

```bash
git clone https://github.com/hmlendea/gpt-actions-orchestrator
cd gpt-actions-orchestrator
dotnet restore GptActionsOrchestrator.slnx
```

### Build

```bash
dotnet build GptActionsOrchestrator.slnx
```

### Run

```bash
dotnet run --project GptActionsOrchestrator/GptActionsOrchestrator.csproj
```

### Test

Run the unit and hosted integration test suites:

```bash
dotnet test GptActionsOrchestrator.slnx
```

### Continuous Integration

The primary CI workflow is `.github/workflows/dotnet.yml` and runs restore, build, unit tests, and hosted integration tests against the `master` branch and pull requests targeting `master`.

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.6.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| `NuciAPI` | `3.5.1` | Runtime | API request and response contracts |
| `NuciAPI.Middleware` | `2.0.2` | Runtime | Exception handling, request logging, and scanner protection middleware |
| `NuciDAL` | `3.2.1` | Runtime | File-backed alias repository |
| `NuciWeb.HTTP` | `1.7.2` | Runtime | HTTP client creation for external integrations |
| `NuciLog` | `1.2.1` | Runtime | Application logging |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.0` | Development | In-memory ASP.NET Core host for integration tests |
| `Moq` | `4.20.72` | Development | Deterministic integration boundary and unit-test substitutes |
| `NUnit` | `4.6.1` | Development | Unit and integration testing framework |

## 🏗️ Architecture

See the [ARCHITECTURE.md](./ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 📚 Documentation

Comprehensive implementation-grounded documentation lives in [`docs/`](docs/):

| Resource | Description |
|----------|-------------|
| [`architecture-overview.md`](docs/architecture-overview.md) | High-level system decomposition, component diagram, dependency rules |
| [`runtime-flows.md`](docs/runtime-flows.md) | Causal execution traces with sequence diagrams for each action type |
| [`component-reference.md`](docs/component-reference.md) | Detailed responsibilities, dependencies, and interfaces for every component |
| [`integration-adapters.md`](docs/integration-adapters.md) | GitHub, Personal Log Manager, and Steam adapter contracts and failure semantics |
| [`orchestration-engine.md`](docs/orchestration-engine.md) | Deep dive into `ActionsOrchestrator` dispatch, parameter building, alias resolution |
| [`api-boundary.md`](docs/api-boundary.md) | HTTP API layer: controller, middleware pipeline, request/response models |
| [`configuration-system.md`](docs/configuration-system.md) | Typed settings binding, precedence, secret management |
| [`data-architecture.md`](docs/data-architecture.md) | Data structures, alias repository, transformation rules |
| [`logging-observability.md`](docs/logging-observability.md) | NuciLog infrastructure, custom log keys, operations, diagnostic flow |
| [`error-handling.md`](docs/error-handling.md) | Layered exception handling, failure semantics, error response contracts |
| [`security.md`](docs/security.md) | Authentication, HMAC, headers, threat model, compliance |
| [`testing-strategy.md`](docs/testing-strategy.md) | Unit/integration test patterns, mocking conventions, CI setup |
| [`source-map.md`](docs/source-map.md) | Cross-reference between documentation topics and source code locations |

> These documents complement the root [`ARCHITECTURE.md`](ARCHITECTURE.md) with implementation-level detail so a future agent can understand the system without rediscovering from source code.

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Add tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/gpt-actions-orchestrator/issues) for problems or suggestions

## 🔒 Security

For information on reporting security vulnerabilities, see [SECURITY.md](./SECURITY.md).

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for further information.
