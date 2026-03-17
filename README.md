[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Latest GitHub release](https://img.shields.io/github/v/release/hmlendea/gptactionsorchestrator)](https://github.com/hmlendea/gptactionsorchestrator/releases/latest) [![Build Status](https://github.com/hmlendea/gptactionsorchestrator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/gptactionsorchestrator/actions/workflows/dotnet.yml)

# Overview

This service exposes a single HTTP endpoint that routes requests to specific integrations.
It is designed to be used as an Actions backend for GPT-style assistants.

Current integrations:

- [GitHub API](https://docs.github.com/en/rest)
- [Personal Log Manager](https://github.com/hmlendea/personal-log-manager)
- [Steam Web API](https://steamcommunity.com/dev)

# Requirements

- .NET SDK 10.0

# Getting Started

1. Clone the repository.
2. Update `appsettings.json` with your real values.
3. Run the service:

```bash
dotnet restore
dotnet run
```

By default, ASP.NET Core also reads `appsettings.Development.json` and environment variables if present.

# Configuration

Configuration is loaded from `appsettings.json`.

```json
{
	"securitySettings": {
		"clientId": "GptActionsOrchestrator",
		"apiKey": "[[GPT_ACTIONS_ORCHESTRATOR_API_KEY]]"
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

## securitySettings

- `clientId`: Client identifier used when calling upstream APIs, where applicable.
- `apiKey`: API key required to access this orchestrator endpoint.

## gitHubSettings

- `username`: Default GitHub username used when no `username` query parameter is provided.
- `apiKey`: GitHub personal access token used for authenticated API calls.

## personalLogManagerSettings

- `baseUrl`: Base URL for the Personal Log Manager API.
- `apiKey`: Bearer token for Personal Log Manager.
- `hmacSigningKey`: Shared key used for HMAC request signing and response validation.

## nuciLoggerSettings

- `logFilePath`: Path to the log file.
- `isFileOutputEnabled`: Enables/disables file logging.

# API

## Endpoint

- Method: `GET`
- Route: `/Actions`

The endpoint always expects a mandatory `action` query parameter, as well as other action-specific parameters.

## Authorization

The API uses API-key authorization.
Configure your caller to send the expected authorization header based on your `securitySettings` values.

## Response Shape

Successful responses follow this shape:
```json
{
	"action": "GetSteamAppData",
	"data": { },
    "success": true,
    "message": "Operation completed successfully."
}
```

Notes:
- `action` is returned as the action ID (`personallogmanager.logs.get`, `steam.store.app.get`).
- `data` depends on the selected action.

## Error Handling

- Invalid action values return `400 Bad Request`.
- Upstream integration failures are propagated as API errors.

# Supported Actions

Both action names and action IDs are accepted in the `action` query parameter.

## github.repository.file.get

- Name: `GetGitHubRepositoryFile`
- ID: `github.repository.file.get`

Action-specific query parameters:
- `username` *(Mandatory)*
- `repository` *(Mandatory)*
- `path` *(Mandatory)*

Example:

**Request:**
```http
GET /Actions?action=github.repository.file.get&username=hmlendea&repository=gptactionsorchestrator&path=README.md
```

**Response `data` field:**
```json
"# Overview\n..."
```

## github.repository.readme.get

- Name: `GetGitHubRepositoryReadme`
- ID: `github.repository.readme.get`

Action-specific query parameters:
- `username` *(Mandatory)*
- `repository` *(Mandatory)*

Example:

**Request:**
```http
GET /Actions?action=github.repository.readme.get&username=hmlendea&repository=gpt-actions-orchestrator
```

**Response `data` field:**
```json
"# Overview\n..."
```

## github.user.repositories.get

- Name: `GetGitHubUserRepositories`
- ID: `github.user.repositories.get`

Action-specific query parameters:
- `username` *(Optional)* - If omitted, `gitHubSettings.username` is used.

Example:

**Request:**
```http
GET /Actions?action=github.user.repositories.get&username=hmlendea
```

**Response `data` field:**
```json
[
	{
		"name": "gptactionsorchestrator",
		"description": "GPT actions orchestration API",
		"language": "C#",
		"stargazers_count": 0,
		"topics": ["dotnet", "gpt"],
		"archived": false,
		"private": false,
		"fork": false,
		"created_at": "2026-01-01T12:00:00+00:00",
		"pushed_at": "2026-03-15T08:30:00+00:00"
	}
]
```

## personallogmanager.logs.get

- Name: `GetPersonalLogs`
- ID: `personallogmanager.logs.get`

Action-specific query parameters:
- `date_beginning` *(Mandatory)*
- `date_end` *(Mandatory)*
- `template`
- `localisation` (defaults to `ro` if omitted)
- `count` (defaults to `1000` if omitted)
- `data.<key>` for dynamic key-value pairs

Example:

**Request:**
```http
GET /Actions?action=personallogmanager.logs.get&date_beginning=2026-03-12&date_end=2026-03-14
```

**Response `data` field:**
```json
{
	"logs": [
		"L202465947 2026-03-12: 23:11 RO: This is a log entry",
		"L065524256 2026-03-14: 22:15 RO: This is another log entry"
	],
	"count": 2
}
```

## steam.store.app.get

- Name: `GetSteamAppData`
- ID: `steam.store.app.get`

Action-specific query parameters:
- `appId` *(Mandatory)*

Example:

**Request:**
```http
GET /Actions?action=steam.store.app.get&appId=730
```

**Response `data` field:**
```json
{
    "id": "730",
    "name": "Counter-Strike 2"
}
```

# Release

Use the helper script:

```bash
./release.sh v1.0.0
```

It delegates to the shared release script maintained in `hmlendea/deployment-scripts`.
