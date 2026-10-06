# Privacy and Personal Data

This document describes how GPT Actions Orchestrator handles personal data when self-hosted. It covers the application behaviour and verified integrations. Instance operators control their deployment's configuration, local storage, logs, backups, access controls, retention, and request handling.

**Information reviewed:** 2026-10-06

## 📑 Table of Contents

- What This Document Covers
- Self-Hosted Deployments
- Data We Handle
- Processing and Use
- Storage, Retention, and Deletion
- External Processing and Integrations
- Data Protection and Security
- Document Changes
- Contact

## 🔎 What This Document Covers

This document describes how GPT Actions Orchestrator at https://github.com/hmlendea/gpt-actions-orchestrator handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

GPT Actions Orchestrator is designed for self-hosted deployment. This document covers the application behaviour as implemented in the repository. Instance operators control their instance's configuration, local storage, logs, backups, access controls, retention, and request handling.

The application sends data to the following external services when configured and invoked:
- **GitHub API** (api.github.com) — repository metadata, file contents, and releases. Data sent includes the requested username, repository name, and file path. Authentication uses a GitHub API key configured by the operator.
- **Personal Log Manager API** (operator-configured base URL) — personal log retrieval. Data sent includes date range, template, localisation, optional data payload, and count. Authentication uses API key and HMAC signing key configured by the operator.
- **Steam Storefront API** (store.steampowered.com) — application metadata. Data sent includes the requested Steam app ID. No authentication is used.

Operators can disable each integration by not configuring the corresponding credentials or base URL in `appsettings.json`. No telemetry, update checks, crash reports, or other data are sent to project maintainers.

## 📥 Data We Handle

### Data Provided to the Application

- **Action query parameters** — the `action` parameter and action-specific parameters (e.g., `username`, `repository`, `path`, `date_beginning`, `date_end`, `template`, `localisation`, `data`, `count`, `appId`) provided in the inbound HTTP request to `/Actions`.
- **API key** — provided in the `Authorization` header for inbound request authentication.

### Data Generated or Collected by the Application

- **Log metadata** — operational diagnostics written to configured logger outputs (console and optional file `logfile.log`). Includes action names, usernames, repository names, file paths, app IDs, timestamps, and operation status. No request bodies or response payloads are logged.
- **Action alias catalogue** — static JSON records mapping alias action IDs to canonical action IDs, loaded from `Data/gpt-action-aliases.json` at startup.

### Data Received from Integrations

- **GitHub API responses** — repository metadata, file contents, README content, and release information returned by api.github.com.
- **Personal Log Manager API responses** — personal log entries returned by the configured Personal Log Manager endpoint.
- **Steam Storefront API responses** — application name and basic metadata returned by store.steampowered.com.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- **Action dispatch and integration request shaping** — Action query parameters
- **Alias-to-canonical-action resolution** — Action alias catalogue
- **GitHub repository, file, README, and releases retrieval** — GitHub username, repository name, file path
- **GitHub user repositories retrieval** — GitHub username
- **Personal log retrieval by date range and options** — Date range, template, localisation, data payload, count
- **Steam application metadata retrieval** — Steam app ID
- **Operational diagnostics** — Log metadata

## 🗄️ Storage, Retention, and Deletion

| Data category | Storage location | Retention | Deletion control |
|---------------|------------------|-----------|------------------|
| Action query parameters | In-memory request scope | Request lifetime | Automatic (end of request) |
| Action alias catalogue | `Data/gpt-action-aliases.json` (file) | Until modified by maintainers | Operator edits or replaces file |
| Log metadata | Configured logger outputs (console, `logfile.log` when enabled) | According to deployment log policy | Operator controls log rotation, retention, and deletion |
| GitHub API responses | In-memory request scope | Request lifetime | Automatic (end of request) |
| Personal Log Manager API responses | In-memory request scope | Request lifetime | Automatic (end of request) |
| Steam Storefront API responses | In-memory request scope | Request lifetime | Automatic (end of request) |

For self-hosted deployments, the instance operator controls local storage, log files, backups, and deletion. The project does not operate a central service and does not store or access operator data.

## 🔗 External Processing and Integrations

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| GitHub API (api.github.com) | Retrieve repository metadata, file contents, README, releases | Username, repository name, file path; GitHub API key for authentication | `appsettings.json` → `gitHubSettings`; https://docs.github.com/en/rest |
| Personal Log Manager API (operator-configured) | Retrieve personal logs by date range and template | Date range, template, localisation, data payload, count; API key and HMAC key for authentication | `appsettings.json` → `personalLogManagerSettings`; NuciAPI specification |
| Steam Storefront API (store.steampowered.com) | Retrieve Steam application name and basic metadata | Steam app ID | Hardcoded endpoint; https://partner.steamgames.com/doc/store/api |

The application has no built-in external data transfer beyond these three integrations. All outbound calls are triggered only by inbound action requests.

## 🛡️ Data Protection and Security

- **Inbound authentication** — API-key authorisation at the `/Actions` endpoint (configured via `securitySettings.apiKey`).
- **Outbound authentication** — Per-provider credentials from configuration (GitHub Bearer token, Personal Log Manager API key + HMAC).
- **Transport security** — HTTPS enforced for GitHub and Personal Log Manager APIs; Steam Storefront uses HTTP (public endpoint).
- **Secrets management** — All credentials provided via configuration placeholders (`[[PLACEHOLDER]]`); operators must supply values through their preferred secret management (environment variables, user secrets, vaults).
- **Operator responsibilities** — For self-hosted deployments, the operator is responsible for: keeping the .NET runtime and dependencies updated; protecting configuration secrets; configuring access controls and network exposure; managing log file permissions and rotation; securing backups; and applying OS-level hardening.

No absolute security is promised. The application implements standard ASP.NET Core security practices and delegates infrastructure security to the operator.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/gpt-actions-orchestrator/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers via GitHub issues at https://github.com/hmlendea/gpt-actions-orchestrator/issues. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the action ID and approximate timestamp to identify the deployment or data flow; do not send passwords, access tokens, or other secrets.