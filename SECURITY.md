# Security Policy

This policy defines how security vulnerabilities should be reported for GPT Actions Orchestrator, what release channels receive security maintenance, and how coordinated disclosure is handled.

## 📑 Table of Contents

- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Latest version | Source code from the `master` branch | ✅ |
| Latest version | Unofficial forked or repackaged binaries | ❌ |
| Latest version | Unofficial container images | ❌ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/gpt-actions-orchestrator/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The subsequent report categories are in scope for this repository:
- Authentication, authorisation, and API key handling weaknesses
- Vulnerabilities that impact confidentiality, integrity, or availability of orchestrated actions and integrations

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Denial-of-service findings that rely exclusively on unrealistic local-only conditions
- Vulnerabilities in unsupported, unofficial forks or third-party redistributions

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.