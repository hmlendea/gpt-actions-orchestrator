# Security

Comprehensive documentation of the security architecture, authentication, authorization, secret management, and threat mitigation.

## Overview

The system implements defense-in-depth security through:
1. **Transport Security** — HTTPS enforcement, security headers
2. **Authentication** — API key validation via middleware
3. **Authorization** — Action-level access control
4. **Secret Management** — Configuration-based secret injection
5. **Input Validation** — Request validation and sanitization
6. **Scanner Protection** — Automated attack detection

## Transport Security

### HTTPS Enforcement

**File:** `GptActionsOrchestrator/Startup.cs`

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (!env.IsDevelopment())
    {
        app.UseHsts();  // HTTP Strict Transport Security
    }

    app.UseHttpsRedirection();  // Redirect HTTP to HTTPS
    // ... other middleware
}
```

**Production Requirements:**
- Valid TLS certificate
- HSTS preload list submission
- TLS 1.2 minimum

### Security Headers

**Middleware:** `UseNuciApiSecurity()` (from `NuciAPI.Middleware.Security`)

**Headers Applied:**
| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `X-XSS-Protection` | `1; mode=block` | Legacy XSS protection |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Control referrer info |
| `Content-Security-Policy` | `default-src 'self'` | Restrict resource loading |

## Authentication

### API Key Authentication

**Middleware:** `UseNuciApiSecurity()` (from `NuciAPI.Middleware.Security`)

**Mechanism:**
- Validates `Authorization: Bearer <api-key>` header
- Compares against configured API keys
- Rejects requests with invalid/missing keys (401 Unauthorized)

**Configuration:**
```json
{
  "securitySettings": {
    "apiKeys": [
      "key-1",
      "key-2"
    ]
  }
}
```

**File:** `GptActionsOrchestrator/Configuration/SecuritySettings.cs`

```csharp
public class SecuritySettings
{
    public List<string> ApiKeys { get; set; } = [];
}
```

### Key Rotation

**Current State:** Manual rotation via configuration update
**Recommended:** Automated rotation with key versioning

## Authorization

### Action-Level Authorization

**Current State:** No action-level authorization implemented
**All authenticated users can execute any action**

**Recommended Implementation:**
```csharp
public class ActionAuthorizationSettings
{
    public Dictionary<string, List<string>> ActionPermissions { get; set; } = [];
    // Maps action name -> list of allowed API key identifiers
}
```

## Secret Management

### Configuration-Based Secrets

**File:** `GptActionsOrchestrator/Configuration/SecuritySettings.cs`

```csharp
public class SecuritySettings
{
    public List<string> ApiKeys { get; set; } = [];
    public string GitHubToken { get; set; } = string.Empty;
    public string PersonalLogManagerApiKey { get; set; } = string.Empty;
    public string PersonalLogManagerApiSecret { get; set; } = string.Empty;
}
```

### Secret Injection

**appsettings.json (Development):**
```json
{
  "securitySettings": {
    "apiKeys": ["dev-key"],
    "gitHubToken": "ghp_xxx",
    "personalLogManagerApiKey": "plm-key",
    "personalLogManagerApiSecret": "plm-secret"
  }
}
```

**Production (Environment Variables):**
```bash
SecuritySettings__ApiKeys__0=prod-key-1
SecuritySettings__ApiKeys__1=prod-key-2
SecuritySettings__GitHubToken=ghp_xxx
SecuritySettings__PersonalLogManagerApiKey=plm-key
SecuritySettings__PersonalLogManagerApiSecret=plm-secret
```

### Secret Storage Recommendations

| Environment | Storage Method |
|-------------|----------------|
| Development | `appsettings.Development.json` (gitignored) |
| CI/CD | GitHub Actions secrets / Azure Key Vault |
| Production | Azure Key Vault / AWS Secrets Manager / HashiCorp Vault |
| Kubernetes | Sealed Secrets / External Secrets Operator |

### Secret Access in Code

**GitHub Service:**
```csharp
public GitHubService(IOptions<SecuritySettings> securitySettings)
{
    _gitHubToken = securitySettings.Value.GitHubToken;
}
```

**PLM Service (HMAC):**
```csharp
public PersonalLogManagerService(IOptions<SecuritySettings> securitySettings)
{
    _apiKey = securitySettings.Value.PersonalLogManagerApiKey;
    _apiSecret = securitySettings.Value.PersonalLogManagerApiSecret;
}
```

## Input Validation

### Request Validation

**Controller:** `GptActionsOrchestrator/Api/Controllers/ActionsController.cs`

```csharp
[HttpPost]
public async Task<IActionResult> ExecuteAction([FromBody] GetActionRequest request)
{
    if (request?.Action == null)
        return BadRequest("Action is required");

    // ... validation
}
```

**Request Model:** `GptActionsOrchestrator/Api/Requests/GetActionRequest.cs`

```csharp
public class GetActionRequest
{
    [Required]
    public GptAction Action { get; set; }
}
```

### Action Parameter Validation

**Orchestrator:** `GptActionsOrchestrator/Service/ActionsOrchestrator.cs`

```csharp
private async Task<GptActionResponse> ExecuteGitHubAction(GptAction action)
{
    // Validate required parameters
    if (!action.Parameters.ContainsKey("username"))
        return new GptActionResponse { Success = false, Error = "username required" };

    // ...
}
```

### Sanitization

**Current State:** No explicit input sanitization
**Recommended:** Add sanitization for:
- Path traversal prevention (GitHub file paths)
- SQL injection (not applicable - no SQL)
- XSS prevention (API returns JSON)

## Scanner Protection

### Middleware

**File:** `GptActionsOrchestrator/Startup.cs`

```csharp
app.UseNuciApiScannerProtection();  // From NuciAPI.Middleware.ScannerProtection
```

**Behavior:**
- Detects common vulnerability scanner patterns
- Blocks requests matching attack signatures
- Logs blocked requests with client IP
- Returns 403 Forbidden

**Detected Patterns:**
- SQL injection attempts
- Path traversal attempts
- Common exploit payloads
- Automated scanner user agents

## HMAC Authentication (Personal Log Manager)

### Implementation

**File:** `GptActionsOrchestrator/Integrations/PersonalLogManager/Client/PersonalLogManagerClient.cs`

```csharp
public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    var nonce = Guid.NewGuid().ToString("N");

    var signature = ComputeHmacSha256(
        $"{request.Method}:{request.RequestUri}:{timestamp}:{nonce}",
        _apiSecret);

    request.Headers.Add("X-PLM-Timestamp", timestamp);
    request.Headers.Add("X-PLM-Nonce", nonce);
    request.Headers.Add("X-PLM-Signature", signature);
    request.Headers.Add("X-PLM-ApiKey", _apiKey);

    return await _httpClient.SendAsync(request);
}
```

### Security Properties

| Property | Value |
|----------|-------|
| Algorithm | HMAC-SHA256 |
| Timestamp Window | ±5 minutes (server-side) |
| Nonce | Single-use (server-side tracking) |
| Key Rotation | Manual via configuration |

## CORS Policy

### Current State

**No CORS middleware configured** — Default ASP.NET Core behavior (no CORS headers)

### Recommended Configuration

```csharp
// Program.cs or Startup.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins("https://chat.openai.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors("AllowedOrigins");
```

## Rate Limiting

### Current State

**No rate limiting implemented**

### Recommended Implementation

```csharp
// Using AspNetCoreRateLimit
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 60
        }
    };
});
```

## Data Protection

### In Transit

- All external API calls use HTTPS
- PLM uses HMAC for request integrity
- GitHub uses Bearer token authentication

### At Rest

- No persistent data storage (file-based aliases only)
- Log files may contain operational data
- No encryption at rest for logs

### In Memory

- Secrets held in memory during runtime
- No secure string usage
- GC may leave secrets in memory

## Threat Model

### STRIDE Analysis

| Threat | Mitigation |
|--------|------------|
| **Spoofing** | API key authentication, HMAC for PLM |
| **Tampering** | HTTPS, HMAC request signing |
| **Repudiation** | Structured logging with timestamps |
| **Information Disclosure** | Security headers, error message sanitization |
| **Denial of Service** | Scanner protection, (rate limiting needed) |
| **Elevation of Privilege** | No privilege separation (single service) |

### Attack Surface

| Component | Exposure | Risk |
|-----------|----------|------|
| `/api/actions` | Public (with auth) | Medium |
| GitHub API | Outbound | Low |
| PLM API | Outbound (HMAC) | Low |
| Steam API | Outbound (public) | Low |
| Log files | Local filesystem | Low |

## Compliance Considerations

### GDPR

- No personal data processed (only public GitHub/Steam data)
- Logs may contain usernames (pseudonymous)
- No data subject rights implementation needed

### SOC 2

- Access control via API keys
- Audit logging via NuciLog
- Encryption in transit
- (Encryption at rest needed for logs)

## Security Testing

### Static Analysis

```bash
# Run security-focused analyzers
dotnet build --configuration Release /p:RunAnalyzers=true
```

### Dependency Scanning

```bash
# Check for vulnerable packages
dotnet list package --vulnerable --include-transitive
```

### Penetration Testing

**Recommended Areas:**
1. API key brute force
2. HMAC replay attacks
3. Path traversal in GitHub file paths
4. Rate limiting bypass
5. Error message information leakage

## Security Checklist

### Pre-Deployment

- [ ] All secrets in environment variables / secret manager
- [ ] HTTPS enforced with valid certificate
- [ ] Security headers present
- [ ] API keys rotated from development values
- [ ] Scanner protection enabled
- [ ] Error messages sanitized
- [ ] CORS policy configured
- [ ] Rate limiting implemented
- [ ] Log files protected (permissions, rotation)
- [ ] Dependency vulnerabilities scanned

### Ongoing

- [ ] Regular secret rotation
- [ ] Dependency updates
- [ ] Log review for anomalies
- [ ] Security header validation
- [ ] Penetration testing (quarterly)

## Related Documentation

- [Configuration System](configuration-system.md) - Secret configuration
- [Error Handling](error-handling.md) - Error message safety
- [Logging and Observability](logging-observability.md) - Audit logging
- [API Boundary](api-boundary.md) - Authentication middleware
- [Integration Adapters](integration-adapters.md) - HMAC implementation
- [Source Map](source-map.md) - File locations