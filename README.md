[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/gpt-actions-orchestrator)](https://github.com/hmlendea/gpt-actions-orchestrator/releases/latest)
[![Build Status](https://github.com/hmlendea/gpt-actions-orchestrator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/gpt-actions-orchestrator/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/gpt-actions-orchestrator)](https://github.com/hmlendea/gpt-actions-orchestrator/blob/master/LICENSE)

# GPT Actions Orchestrator

GPT Actions Orchestrator is a .NET HTTP service that exposes a single action-driven endpoint and dispatches requests to supported upstream integrations.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Use Cases](#-use-cases)
- [Usage](#-usage)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
  - [Manual Installation](#manual-installation)
- [Configuration](#-configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Precedence](#precedence)
  - [Secret Management](#secret-management)
  - [Validation](#validation)
- [Compatibility](#-compatibility)
- [Integrations](#-integrations)
- [Authentication and Authorisation](#-authentication-and-authorisation)
- [Privacy and Data](#-privacy-and-data)
  - [Data Locations](#data-locations)
- [Development](#-development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Continuous Integration](#continuous-integration)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [Project Structure](#-project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Architecture](#-architecture)
- [Contributing](#-contributing)
- [Security](#-security)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- Routes a single inbound action request to the correct integration service.
- Supports both canonical action IDs and alias action IDs.
- Retrieves GitHub repositories, files, README content, and releases.
- Retrieves Personal Log Manager logs with date range and optional data payload.
- Retrieves Steam Storefront application metadata.

## 🎯 Use Cases

- **GPT actions backend:** Route assistant tool calls through one stable HTTP endpoint.
- **Repository retrieval automation:** Query GitHub repository metadata and file content for assistant workflows.
- **Personal activity reporting:** Retrieve personal logs with date range and templating parameters.

## 🚀 Usage

Send a `GET` request to the `/Actions` endpoint with an `action` parameter and the action-specific parameters.

```http
GET /Actions?action=github.repository.get&username=hmlendea&repository=gpt-actions-orchestrator
```

Successful responses return a common envelope with the selected action and its provider-specific data payload.

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| .NET Runtime | 10.0 | 10.0.x latest patch |
| .NET SDK (for local builds) | 10.0 | 10.0.x latest patch |

## 📦 Installation

[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/gpt-actions-orchestrator/releases)

### Manual Installation

1. Clone this repository.
2. Restore dependencies.
3. Configure `GptActionsOrchestrator/appsettings.json`.
4. Run the service.

```bash
git clone git@github.com:hmlendea/gpt-actions-orchestrator.git
cd gpt-actions-orchestrator
dotnet restore GptActionsOrchestrator.slnx
dotnet run --project GptActionsOrchestrator/GptActionsOrchestrator.csproj
```

## ⚙️ Configuration

The service uses typed settings bound from ASP.NET Core configuration providers.

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `GptActionsOrchestrator/appsettings.json` | Application-wide | Defines API keys, integration settings, datastore path, and logger settings |

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| SecuritySettings | `clientId` | `string` | `GptActionsOrchestrator` | Yes | Client identifier used for outbound authorisation metadata |
| SecuritySettings | `apiKey` | `string` | `—` | Yes | API key expected for inbound `/Actions` requests |
| DataStoreSettings | `gptActionAliasesStorePath` | `string` | `Data/gpt-action-aliases.json` | Yes | File path used for action alias mapping |
| GitHubSettings | `username` | `string` | `—` | Yes | Default GitHub username for repository queries |
| GitHubSettings | `apiKey` | `string` | `—` | No | Bearer token for authenticated GitHub API calls |
| PersonalLogManagerSettings | `baseUrl` | `string` | `—` | Yes | Base URL of the Personal Log Manager API |
| PersonalLogManagerSettings | `apiKey` | `string` | `—` | Yes | Bearer token for Personal Log Manager requests |
| PersonalLogManagerSettings | `hmacSigningKey` | `string` | `—` | Yes | Shared key for HMAC signing and response validation |
| NuciLoggerSettings | `logFilePath` | `string` | `logfile.log` | No | File path for logger output when file logging is enabled |
| NuciLoggerSettings | `isFileOutputEnabled` | `bool` | `true` | No | Enables or disables file-based logging |

### Precedence

Configuration precedence follows the ASP.NET Core default host order, where later providers override earlier providers:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command-line arguments

### Secret Management

Store `securitySettings.apiKey`, `gitHubSettings.apiKey`, `personalLogManagerSettings.apiKey`, and `personalLogManagerSettings.hmacSigningKey` in a secure secret source for non-local environments.

### Validation

After configuration, validate the service with a request that includes a valid `action` and API key.

## 🧩 Compatibility

| Component | Supported Versions | Notes |
|-----------|--------------------|-------|
| .NET | `net10.0` | Project target framework in both main and test projects |
| GitHub REST API | Current REST API with `X-GitHub-Api-Version: 2022-11-28` | Version header is set by the GitHub adapter |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| GitHub API | REST API over HTTPS | Repository metadata, file content, and release retrieval | No |
| Personal Log Manager API | NuciAPI-compatible endpoint | Personal log retrieval by date range and options | No |
| Steam Storefront API | `store.steampowered.com/api/appdetails` | Steam application name retrieval | No |

## 🔐 Authentication and Authorisation

Inbound requests are protected with API-key authorisation at the `/Actions` endpoint. Outbound integration calls use per-provider credentials from configuration when configured.

## 🛡️ Privacy and Data

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Action query parameters | Action dispatch and integration request shaping | In-memory request scope | Request lifetime | No |
| Action alias catalogue | Alias-to-canonical-action resolution | JSON file datastore | Until modified by maintainers | No |
| Log metadata | Operational diagnostics | Configured logger outputs | According to deployment log policy | Yes |

### Data Locations

| Platform or Scope | Location | Contents |
|-------------------|----------|----------|
| Repository data | `GptActionsOrchestrator/Data/gpt-action-aliases.json` | Action alias records |
| Runtime log output | `GptActionsOrchestrator/logfile.log` (default) | Application log entries when file logging is enabled |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

```bash
git clone git@github.com:hmlendea/gpt-actions-orchestrator.git
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

```bash
dotnet test GptActionsOrchestrator.slnx
```

### Continuous Integration

The primary CI workflow is `.github/workflows/dotnet.yml` and runs restore, build, and test against the `master` branch and pull requests targeting `master`.

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
| `NuciDAL` | `3.1.1` | Runtime | File-backed alias repository |
| `NuciWeb.HTTP` | `1.7.1` | Runtime | HTTP client creation for external integrations |
| `NuciLog` | `1.2.1` | Runtime | Application logging |
| `NUnit` | `4.6.1` | Development | Unit-testing framework |

## 🗂️ Project Structure

The repository is organised as one ASP.NET Core service project plus one unit-test project.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `GptActionsOrchestrator/GptActionsOrchestrator.csproj` | ASP.NET Core web service | Hosts the `/Actions` endpoint and integration orchestration |
| `GptActionsOrchestrator.UnitTests/GptActionsOrchestrator.UnitTests.csproj` | .NET test project | Verifies action model and orchestration dispatch behaviour |

### Directories

| Directory | Purpose |
|-----------|---------|
| `GptActionsOrchestrator/Api/` | HTTP request and response contracts plus controller boundary |
| `GptActionsOrchestrator/Service/` | Action orchestration logic and action model |
| `GptActionsOrchestrator/Integrations/` | External provider adapters for GitHub, Personal Log Manager, and Steam |
| `GptActionsOrchestrator/Configuration/` | Typed configuration classes |
| `GptActionsOrchestrator/Data/` | Action alias datastore |
| `GptActionsOrchestrator.UnitTests/` | Unit tests and test project configuration |

## 🏗️ Architecture

See the [architecture documentation](./ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

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

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/gpt-actions-orchestrator/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for further information.
