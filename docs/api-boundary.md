# API Boundary

Detailed documentation of the HTTP API layer: controller, middleware pipeline, request/response models, and authorisation.

## Overview

The API boundary consists of:
- **Middleware Pipeline** — NuciAPI middleware for exception handling, scanner protection, request logging
- **Controller** — `ActionsController` handling `GET /Actions`
- **Request Model** — `GetActionRequest` binding query parameters
- **Response Model** — `GetActionResponse` wrapping provider data
- **Authorisation** — API key validation via NuciAPI

## Middleware Pipeline

**File:** `GptActionsOrchestrator/Startup.cs` → `Configure()`

### Pipeline Order (Critical)

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // 1. Exception handling (outermost - catches all downstream exceptions)
    app.UseNuciApiExceptionHandling();

    // 2. Scanner protection (blocks known malicious scanners)
    app.UseNuciApiScannerProtection();

    // 3. Request logging (logs all requests)
    app.UseNuciApiRequestLogging();

    // 4. Development exception page (only in Development)
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // 5. Standard ASP.NET Core middleware
    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    // 6. Endpoint mapping
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### Middleware Details

#### UseNuciApiExceptionHandling()
- **Package:** `NuciAPI.Middleware.ExceptionHandling`
- **Purpose:** Global exception handling and translation to standard API error responses
- **Behavior:** Catches unhandled exceptions, logs them, returns structured error response
- **Error Response Format:**
  ```json
  {
    "success": false,
    "code": "ErrorCode",
    "message": "Human-readable message"
  }
  ```

#### UseNuciApiScannerProtection()
- **Package:** `NuciAPI.Middleware.Security`
- **Purpose:** Blocks requests from known malicious scanners/bots
- **Behavior:** Inspects User-Agent and other headers, returns 403 for matched patterns

#### UseNuciApiRequestLogging()
- **Package:** `NuciAPI.Middleware.Logging`
- **Purpose:** Structured request/response logging
- **Behavior:** Logs incoming request details and outgoing response status

## Controller: ActionsController

**File:** `GptActionsOrchestrator/Api/Controllers/ActionsController.cs`

### Route and Attributes

```csharp
[Route("[controller]")]  // → /Actions
[ApiController]
public class ActionsController(...) : NuciApiController
```

**Base Class:** `NuciApiController` (from `NuciAPI.Controllers`)
- Provides `ProcessRequest()` method for standardised request handling
- Handles authorisation, model validation, and response wrapping

### Constructor

```csharp
public ActionsController(
    IActionsOrchestrator actionsOrchestrator,
    SecuritySettings securitySettings) : NuciApiController
{
    private readonly NuciApiAuthorisation authorisation =
        NuciApiAuthorisation.ApiKey(securitySettings.ApiKey);
}
```

**Dependencies:**
- `IActionsOrchestrator` — orchestration service
- `SecuritySettings` — provides `ApiKey` for authorisation

### Action Method: Get()

```csharp
[HttpGet]
public ActionResult Get([FromQuery] GetActionRequest request)
    => ProcessRequest(
        request,
        () => actionsOrchestrator.Get(Request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString()
        )),
        authorisation);
```

**Flow:**
1. Model binding: `GetActionRequest` binds `action` query parameter
2. Authorisation: `NuciApiAuthorisation.ApiKey` validates `Authorization` header
3. Query extraction: `Request.Query.ToDictionary()` converts all query params to `Dictionary<string, string>`
4. Delegate execution: `actionsOrchestrator.Get(rawParameters)`
5. Response processing: `ProcessRequest()` wraps result in standard envelope

### ProcessRequest() (from NuciApiController)

The base class `ProcessRequest<TRequest, TResponse>()` handles:
1. Request model validation
2. Authorisation verification
3. Delegate execution
4. Success response wrapping
5. Exception propagation to middleware

## Request Model: GetActionRequest

**File:** `GptActionsOrchestrator/Api/Requests/GetActionRequest.cs`

```csharp
public sealed class GetActionRequest : NuciApiRequest
{
    [FromQuery(Name = "action")]
    [JsonPropertyName("action")]
    public string GptActionName { get; set; }
}
```

**Inheritance:** `NuciApiRequest` (from `NuciAPI.Requests`)
- Provides base properties for API requests
- May include correlation IDs, timestamps, etc.

**Binding:**
- `[FromQuery(Name = "action")]` — binds from `?action=` query parameter
- `[JsonPropertyName("action")]` — JSON serialization name (for documentation)

**Validation:** Handled by `ApiController` base class and `ProcessRequest()`

## Response Model: GetActionResponse

**File:** `GptActionsOrchestrator/Api/Responses/GetActionResponse.cs`

```csharp
public sealed class GetActionResponse : NuciApiSuccessResponse
{
    [FromQuery(Name = "action")]
    [JsonPropertyName("action")]
    public string GptActionName { get; set; }

    public object Data { get; set; }
}
```

**Inheritance:** `NuciApiSuccessResponse` (from `NuciAPI.Responses`)
- Provides `success: true` property
- May include metadata (timestamp, correlation ID, etc.)

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `GptActionName` | string | Name of the action processed (from `GptAction.ToString()`) |
| `Data` | object | Provider-specific response data (varies by action) |

**Serialization:** Standard ASP.NET Core JSON serialization

### Example Responses

#### GitHub Repository
```json
{
  "success": true,
  "action": "GetGitHubRepository",
  "data": {
    "name": "gpt-actions-orchestrator",
    "description": "GPT Actions Orchestrator...",
    "language": "C#",
    "stargazersCount": 42,
    "topics": ["dotnet", "gpt", "actions"],
    "isArchived": false,
    "isPrivate": false,
    "isFork": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "pushedAt": "2024-12-01T14:22:00Z"
  }
}
```

#### Personal Logs
```json
{
  "success": true,
  "action": "GetPersonalLogs",
  "data": {
    "logs": [
      "2024-12-01: Completed task A",
      "2024-12-01: Started task B"
    ],
    "count": 2
  }
}
```

#### Steam App Data
```json
{
  "success": true,
  "action": "GetSteamAppData",
  "data": {
    "id": "613",
    "name": "Solaire's Quest"
  }
}
```

#### Error Response (from middleware)
```json
{
  "success": false,
  "code": "Unauthorized",
  "message": "Invalid API key"
}
```

## Authorisation

### Inbound: API Key

**Mechanism:** `NuciApiAuthorisation.ApiKey(securitySettings.ApiKey)`

**Header:** `Authorization: Bearer <api-key>`

**Configuration:** `securitySettings.apiKey` in `appsettings.json`

**Validation:** Performed by `NuciApiController.ProcessRequest()` before delegate execution

**Failure Response:** 401 Unauthorized with standard error envelope

### Outbound: Per-Provider

| Provider | Auth Type | Configuration |
|----------|-----------|---------------|
| GitHub | Bearer token (optional) | `gitHubSettings.apiKey` |
| Personal Log Manager | Bearer + HMAC | `personalLogManagerSettings.apiKey`, `hmacSigningKey` |
| Steam Storefront | None | N/A |

## Query Parameter Handling

### Supported Parameters by Action

| Action | Required Parameters | Optional Parameters |
|--------|---------------------|---------------------|
| `GetGitHubRepository` | `action`, `username`, `repository` | — |
| `GetGitHubRepositoryFile` | `action`, `username`, `repository`, `path` | — |
| `GetGitHubRepositoryReadme` | `action`, `username`, `repository` | — |
| `GetGitHubRepositoryReleases` | `action`, `username`, `repository` | — |
| `GetGitHubUserRepositories` | `action`, `username` | — |
| `GetPersonalLogs` | `action` | `date_beginning`, `date_end`, `template`, `localisation`, `data.*`, `count` |
| `GetSteamAppData` | `action`, `appId` | — |

### Nested Parameters (data.*)

The orchestrator converts dotted keys to nested dictionaries:
```
?action=GetPersonalLogs&data.mood=happy&data.energy=high
```
Becomes:
```csharp
parameters["data"] = new Dictionary<string, string>
{
    { "mood", "happy" },
    { "energy", "high" }
};
```

### Parameter Encoding

- All query parameters are URL-decoded by ASP.NET Core before reaching controller
- No additional encoding/decoding in orchestrator
- GitHub adapter re-encodes path components with `Uri.EscapeDataString()`

## HTTP Details

### Request
```
GET /Actions?action=GetGitHubRepository&username=hmlendea&repository=gpt-actions-orchestrator
Authorization: Bearer <api-key>
Accept: application/json
```

### Successful Response
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "action": "GetGitHubRepository",
  "data": { ... }
}
```

### Error Responses

| Status | Code | Scenario |
|--------|------|----------|
| 400 | `BadRequest` | Missing action parameter, invalid parameters |
| 401 | `Unauthorized` | Missing or invalid API key |
| 403 | `Forbidden` | Scanner protection triggered |
| 404 | `NotFound` | Route not found (not /Actions) |
| 500 | `InternalServerError` | Unhandled exception in orchestrator or adapters |
| 502 | `BadGateway` | Upstream API error (if middleware translates) |

## Content Negotiation

- **Request:** No specific content type required (GET with query params)
- **Response:** `application/json` (default ASP.NET Core JSON formatter)
- **Accept header:** Ignored (always returns JSON)

## Rate Limiting

**Not implemented** at application level. Relies on:
- Upstream API rate limits (GitHub, Steam)
- Infrastructure-level rate limiting (reverse proxy, WAF)

## CORS

**Not configured** in `Startup.cs`. Default ASP.NET Core behavior (no CORS headers).
Add `app.UseCors()` if cross-origin requests needed.

## Request/Response Size Limits

**Default ASP.NET Core limits:**
- Request body: Not applicable (GET requests)
- Query string: ~2KB (ASP.NET Core default)
- Response: No explicit limit

## Versioning

**No API versioning** implemented. Single endpoint with action-based routing.
- Breaking changes require new action IDs
- Aliases provide backward compatibility for renamed actions

## OpenAPI/Swagger

**Not configured.** No Swagger/OpenAPI generation in current setup.
Would require:
- `Swashbuckle.AspNetCore` package
- `services.AddSwaggerGen()` in `ConfigureServices`
- `app.UseSwagger()` / `app.UseSwaggerUI()` in `Configure`

## Testing the API Boundary

### Integration Tests

**Location:** `GptActionsOrchestrator.IntegrationTests/`

**Test Host:** `ActionsWebApplicationFactory` (custom `WebApplicationFactory<Program>`)

**Key Features:**
- Hosts real service with actual middleware pipeline
- Preserves: middleware, authorisation, routing, controller, alias repository, orchestration, serialisation
- Mocks: outbound provider interfaces (`IGitHubService`, etc.) and logger

**Test Client Creation:**
```csharp
// Unauthenticated client
var client = ActionsHttpClientFactory.Create(factory);

// Authenticated client
var client = ActionsHttpClientFactory.CreateAuthorised(factory);
```

**Request Building:**
```csharp
var path = ActionRequestPathBuilder.Build(new[]
{
    new KeyValuePair<string, string?>("action", "GetGitHubRepository"),
    new KeyValuePair<string, string?>("username", "test"),
    new KeyValuePair<string, string?>("repository", "repo")
});
```

**Response Reading:**
```csharp
var response = await client.GetAsync(path);
var document = await ActionResponseReader.ReadSuccessAsync(response);
```

### Unit Tests

**Location:** `GptActionsOrchestrator.UnitTests/Service/ActionsOrchestratorTests.cs`

**Focus:** Orchestrator logic, not HTTP pipeline
- Mocks all integration services
- Tests parameter building, alias resolution, dispatch, response construction

## Related Documentation

- [Architecture Overview](architecture-overview.md) - High-level context
- [Runtime Execution Flows](runtime-flows.md) - Complete request/response traces
- [Orchestration Engine](orchestration-engine.md) - Dispatch logic
- [Component Reference](component-reference.md) - Component details
- [Configuration System](configuration-system.md) - Settings and API key config
- [Error Handling](error-handling.md) - Exception translation
- [Testing Strategy](testing-strategy.md) - Test architecture
- [Source Map](source-map.md) - File locations