# Runtime Execution Flows

This document provides detailed causal execution traces for each action type, showing the complete path from inbound request to outbound response.

## Inbound Get Action Flow (Complete)

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as NuciAPI Middleware
    participant Controller as ActionsController
    participant Orchestrator as ActionsOrchestrator
    participant AliasRepo as JsonRepository
    participant GitHubSvc as GitHubService
    participant PLMSvc as PersonalLogManagerService
    participant SteamSvc as SteamStoreService
    participant GitHubAPI as GitHub REST API
    participant PLMAPI as Personal Log Manager API
    participant SteamAPI as Steam Storefront API

    Client->>Middleware: GET /Actions?action=...&params...
    Middleware->>Middleware: Scanner protection check
    Middleware->>Middleware: Request logging
    Middleware->>Controller: Route to ActionsController.Get()
    Controller->>Controller: API key authorisation (NuciApiAuthorisation.ApiKey)
    Controller->>Orchestrator: Get(rawQueryDictionary)
    Orchestrator->>Orchestrator: BuildParameters(rawQueryDictionary)
    Orchestrator->>AliasRepo: ContainsId(actionId)?
    alt Alias exists
        AliasRepo-->>Orchestrator: true
        Orchestrator->>AliasRepo: Get(actionId)
        AliasRepo-->>Orchestrator: GptActionAliasDataObject{TargetActionId}
        Orchestrator->>Orchestrator: Resolve canonical action ID
    else No alias
        AliasRepo-->>Orchestrator: false
        Orchestrator->>Orchestrator: Use actionId directly
    end
    Orchestrator->>Orchestrator: GptAction.FromString(canonicalId)
    Orchestrator->>Orchestrator: Switch on GptAction
    alt GitHub actions
        Orchestrator->>GitHubSvc: GetRepository/GetRepositoryFile/GetRepositoryReleases/GetUserRepositories
        GitHubSvc->>GitHubSvc: Build endpoint + headers
        GitHubSvc->>GitHubAPI: HTTP GET
        GitHubAPI-->>GitHubSvc: JSON response
        GitHubSvc->>GitHubSvc: Deserialize to model
        GitHubSvc-->>Orchestrator: Model object
    else Personal Log Manager
        Orchestrator->>PLMSvc: GetPersonalLogs(...)
        PLMSvc->>PLMSvc: Build GetPersonalLogsRequest (HMAC ordered)
        PLMSvc->>PLMSvc: NuciApiClient.SendRequestAsync()
        PLMSvc->>PLMAPI: Signed HTTP GET
        PLMAPI-->>PLMSvc: NuciApiResponse
        PLMSvc->>PLMSvc: Check IsSuccessful, extract Logs
        PLMSvc-->>Orchestrator: PersonalLogs object
    else Steam Storefront
        Orchestrator->>SteamSvc: GetAppData(appId)
        SteamSvc->>SteamSvc: Build endpoint
        SteamSvc->>SteamAPI: HTTP GET
        SteamAPI-->>SteamSvc: JSON response
        SteamSvc->>SteamSvc: Regex extract name
        SteamSvc-->>Orchestrator: SteamAppEntity (or null)
    end
    Orchestrator->>Orchestrator: new GetActionResponse{ActionName, Data}
    Orchestrator-->>Controller: GetActionResponse
    Controller-->>Middleware: NuciApiSuccessResponse
    Middleware->>Middleware: Response logging
    Middleware-->>Client: 200 OK + JSON envelope
```

## Parameter Building Flow

```mermaid
flowchart TD
    RawParams[Dictionary<string,string> rawParameters] --> Loop{For each key,value}
    Loop --> HasDot{key.Contains('.')?}
    HasDot -->|Yes| Split[Split on first '.' into parentKey, childKey]
    Split --> ParentExists{parameters.ContainsKey(parentKey)?}
    ParentExists -->|No| CreateDict[parameters[parentKey] = new Dictionary<string,string>()]
    ParentExists -->|Yes| AddChild[parameters[parentKey][childKey] = value]
    CreateDict --> AddChild
    AddChild --> Loop
    HasDot -->|No| AddScalar[parameters[key] = value]
    AddScalar --> Loop
    Loop -->|Done| Result[Dictionary<string,object> parameters]
```

**Example transformation:**
```
Input:  { "action": "GetPersonalLogs", "data.mood": "happy", "data.energy": "high" }
Output: { "action": "GetPersonalLogs", "data": { "mood": "happy", "energy": "high" } }
```

## Alias Resolution Flow

```mermaid
flowchart TD
    ActionId[action parameter value] --> ContainsCheck{aliasesRepository.ContainsId(ActionId)?}
    ContainsCheck -->|Yes| GetAlias[aliasesRepository.Get(ActionId)]
    GetAlias --> TargetId[alias.TargetActionId]
    TargetId --> FromString[GptAction.FromString(TargetId)]
    ContainsCheck -->|No| DirectResolve[GptAction.FromString(ActionId)]
    FromString --> Resolved[GptAction instance]
    DirectResolve --> Resolved
    Resolved --> Dispatch{Switch on GptAction}
```

**Alias file structure** (`Data/gpt-action-aliases.json`):
```json
[
  { "id": "github.file.get", "targetActionId": "github.repository.file.get" },
  { "id": "github.repo.get", "targetActionId": "github.repository.get" },
  { "id": "personal.logs.get", "targetActionId": "personallogmanager.logs.get" }
]
```

## GitHub Repository Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as GitHubService
    participant Http as HttpClient
    participant API as GitHub REST API

    Orch->>Svc: GetRepository(username, repositoryName)
    Svc->>Svc: isAuthenticatedUserRequested = username empty or matches settings.Username
    Svc->>Svc: effectiveUsername = isAuthenticatedUserRequested ? settings.Username : username
    Svc->>Svc: Log Started (MyOperation.GitHubRepositoryRetrieval, Username, Repository)
    Svc->>Svc: encodedOwner = Uri.EscapeDataString(effectiveUsername)
    Svc->>Svc: encodedRepository = Uri.EscapeDataString(repositoryName)
    Svc->>Svc: endpoint = $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}"
    Svc->>Http: GetStringAsync(endpoint).Result
    Http->>API: GET /repos/{owner}/{repo}
    API-->>Http: JSON repository object
    Http-->>Svc: JSON string
    Svc->>Svc: FromJson<GitHubRepository>()
    Svc->>Svc: Log Success (Count=1)
    Svc-->>Orch: GitHubRepository model
```

**Key implementation details:**
- `ApiBaseUrl = "https://api.github.com"`
- `ApiVersion = "2022-11-28"` (sent as `X-GitHub-Api-Version` header)
- Accept header: `application/vnd.github+json`
- Optional Bearer auth from `GitHubSettings.ApiKey`
- Pagination for user repositories and releases (100 per page)

## GitHub Repository File Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as GitHubService
    participant Http as HttpClient
    participant API as GitHub REST API

    Orch->>Svc: GetRepositoryFile(username, repositoryName, path)
    Svc->>Svc: Log Started (Username, Repository, Path)
    Svc->>Svc: encodedOwner = Uri.EscapeDataString(username)
    Svc->>Svc: encodedRepository = Uri.EscapeDataString(repositoryName)
    Svc->>Svc: pathParts = path.Split('/', RemoveEmptyEntries)
    Svc->>Svc: encodedPath = string.Join("/", pathParts.Select(Uri.EscapeDataString))
    Svc->>Svc: request = new HttpRequestMessage(GET, $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}/contents/{encodedPath}?ref=HEAD")
    Svc->>Svc: request.Headers.Accept = application/vnd.github.raw+json
    Svc->>Http: SendAsync(request).Result
    Http->>API: GET /repos/{owner}/{repo}/contents/{path}?ref=HEAD
    API-->>Http: Raw file content
    Http-->>Svc: string content
    Svc->>Svc: Log Success
    Svc-->>Orch: string file content
```

**Note:** Uses `application/vnd.github.raw+json` Accept header to get raw file content directly.

## GitHub Repository Releases Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as GitHubService
    participant Http as HttpClient
    participant API as GitHub REST API

    Orch->>Svc: GetRepositoryReleases(username, repositoryName)
    Svc->>Svc: Log Started (Username, Repository)
    Svc->>Svc: encodedOwner = Uri.EscapeDataString(username)
    Svc->>Svc: encodedRepository = Uri.EscapeDataString(repositoryName)
    Svc->>Svc: releases = []
    Svc->>Svc: page = 1
    loop while true
        Svc->>Svc: endpoint = $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}/releases?per_page=100&page={page}"
        Svc->>Http: GetStringAsync(endpoint).Result
        Http->>API: GET /repos/{owner}/{repo}/releases?per_page=100&page={page}
        API-->>Http: JSON array of releases
        Http-->>Svc: JSON string
        Svc->>Svc: pageItems = FromJson<List<GitHubRelease>>()
        Svc->>Svc: if pageItems null or empty: break
        Svc->>Svc: releases.AddRange(pageItems)
        Svc->>Svc: page++
    end
    Svc->>Svc: Log Success (Count=releases.Count)
    Svc-->>Orch: IEnumerable<GitHubRelease>
```

## GitHub User Repositories Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as GitHubService
    participant Http as HttpClient
    participant API as GitHub REST API

    Orch->>Svc: GetUserRepositories(username)
    Svc->>Svc: isAuthenticatedUserRequested = username empty or matches settings.Username
    Svc->>Svc: effectiveUsername = isAuthenticatedUserRequested ? settings.Username : username
    Svc->>Svc: Log Started (Username)
    Svc->>Svc: repositories = []
    Svc->>Svc: page = 1
    loop while true
        alt isAuthenticatedUserRequested
            Svc->>Svc: endpoint = $"{ApiBaseUrl}/user/repos?visibility=all&affiliation=owner,collaborator,organization_member&sort=updated&per_page=100&page={page}"
        else
            Svc->>Svc: endpoint = $"{ApiBaseUrl}/users/{Uri.EscapeDataString(username)}/repos?sort=updated&per_page=100&page={page}"
        end
        Svc->>Http: GetStringAsync(endpoint).Result
        Http->>API: GET /user/repos or /users/{username}/repos
        API-->>Http: JSON array of repositories
        Http-->>Svc: JSON string
        Svc->>Svc: pageItems = FromJson<List<GitHubRepository>>()
        Svc->>Svc: if pageItems null or empty: break
        Svc->>Svc: repositories.AddRange(pageItems)
        Svc->>Svc: page++
    end
    Svc->>Svc: Log Success (Count=repositories.Count)
    Svc-->>Orch: IEnumerable<GitHubRepository>
```

## Personal Log Manager Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as PersonalLogManagerService
    participant Client as NuciApiClient
    participant API as Personal Log Manager API

    Orch->>Svc: GetPersonalLogs(dateBeginning, dateEnd, template, localisation, data, count)
    Svc->>Svc: Log Started (Template, DateBeginning, DateEnd, Localisation, Count)
    Svc->>Svc: authorisation = { ClientId, BearerToken, HmacSharedSecretKey }
    Svc->>Svc: request = BuildRequest(dateBeginning, dateEnd, template, localisation, data, count)
    Svc->>Client: SendRequestAsync<GetPersonalLogsRequest, GetPersonalLogsResponse>(GET, request, authorisation, "PersonalLog")
    Client->>Client: Build HMAC signature using ordered properties (HmacOrder attributes)
    Client->>API: Signed GET request
    API-->>Client: NuciApiResponse
    Client-->>Svc: NuciApiResponse
    Svc->>Svc: if !response.IsSuccessful: throw Exception(response.Message)
    Svc->>Svc: personalLogs = new PersonalLogs { Logs = response.Logs }
    Svc->>Svc: Log Success
    Svc-->>Orch: PersonalLogs
```

### BuildRequest Details

```mermaid
flowchart TD
    Input[dateBeginning, dateEnd, template, localisation, data, count] --> DateRegex[BuildDateRangeRegex]
    DateRegex --> RegexResult{dateBeginning && dateEnd both provided?}
    RegexResult -->|Both empty| ReturnNull[return null]
    RegexResult -->|One empty| ThrowArgEx[throw ArgumentException]
    RegexResult -->|Both provided| ParseDates[Parse as DateOnly yyyy-MM-dd]
    ParseDates --> ValidateOrder{end >= start?}
    ValidateOrder -->|No| ThrowArgEx2[throw ArgumentException]
    ValidateOrder -->|Yes| BuildList[Generate list of dates from start to end]
    BuildList --> JoinRegex[return "(" + string.Join("|", dates) + ")"]
    JoinRegex --> Request[GetPersonalLogsRequest]
    Request --> SetDate[request.Date = regexResult]
    Request --> SetTemplate[request.Template = template]
    Request --> SetLocalisation{localisation empty?}
    SetLocalisation -->|Yes| DefaultLoc[request.Localisation = "ro"]
    SetLocalisation -->|No| UseLoc[request.Localisation = localisation]
    Request --> SetData[request.Data = data]
    Request --> SetCount{count empty?}
    SetCount -->|Yes| DefaultCount[request.Count = 1000]
    SetCount -->|No| ParseCount[request.Count = int.Parse(count)]
    DefaultCount --> Return[return request]
    UseLoc --> Return
    ParseCount --> Return
    DefaultLoc --> Return
```

**HMAC property order** (from `GetPersonalLogsRequest`):
1. `Date` (HmacOrder=1)
2. `Time` (HmacOrder=2) — not set by orchestrator
3. `Template` (HmacOrder=3)
4. `Localisation` (HmacOrder=4)
5. `Data` (HmacOrder=5)
6. `Count` (HmacOrder=6)

## Steam Storefront App Data Retrieval Flow

```mermaid
sequenceDiagram
    participant Orch as ActionsOrchestrator
    participant Svc as SteamStoreService
    participant Http as HttpClient
    participant API as Steam Storefront API

    Orch->>Svc: GetAppData(appId)
    Svc->>Svc: Log Started (AppId)
    Svc->>Svc: endpoint = $"{StorefrontApiUrl}/appdetails?appids={appId}&cc={StorefrontApiCountry}&filters={StorefrontApiFilters}"
    Svc->>Http: GetStringAsync(endpoint).Result
    Http->>API: GET /api/appdetails?appids={appId}&cc=RO&filters=basic
    API-->>Http: JSON response
    Http-->>Svc: JSON string
    Svc->>Svc: steamAppEntity = new SteamAppEntity()
    Svc->>Svc: match = Regex.Match(responseContent, AppNamePattern)
    Svc->>Svc: steamAppEntity.Id = appId
    Svc->>Svc: steamAppEntity.Name = match.Groups[1].Value
    Svc->>Svc: Log Success
    Svc-->>Orch: SteamAppEntity (or null if exception)
```

**Constants:**
- `StorefrontApiUrl = "http://store.steampowered.com/api"`
- `StorefrontApiCountry = "RO"`
- `StorefrontApiFilters = "basic"`
- `AppNamePattern = "\"name\": *\"([^\"]*)\""`

**Failure semantics:** Catches all exceptions, logs error, returns `null` (does not rethrow)

## Response Construction Flow

```mermaid
flowchart TD
    Orchestrator[ActionsOrchestrator.Get] --> ActionResolved[GptAction resolved]
    ActionResolved --> Dispatch[Switch on GptAction]
    Dispatch --> AdapterCall[Call integration service]
    AdapterCall --> AdapterResult[object data]
    AdapterResult --> BuildResponse[new GetActionResponse]
    BuildResponse --> SetActionName[GptActionName = action.ToString()]
    BuildResponse --> SetData[Data = data]
    BuildResponse --> Return[return GetActionResponse]
    Return --> Controller[ActionsController.Get]
    Controller --> ProcessRequest[ProcessRequest with authorisation]
    ProcessRequest --> NuciApiSuccessResponse[NuciApiSuccessResponse envelope]
    NuciApiSuccessResponse --> Serialization[JSON serialisation]
    Serialization --> Client[HTTP 200 response]
```

**Response envelope** (via `NuciApiSuccessResponse`):
```json
{
  "success": true,
  "action": "GetGitHubRepository",
  "data": { ...provider-specific payload... }
}
```

## Error Propagation Flow

```mermaid
flowchart TD
    Adapter[Integration Adapter] --> Exception{Exception thrown?}
    Exception -->|GitHub/PLM| LogError[Log Error with context]
    LogError --> Rethrow[throw]
    Rethrow --> Orchestrator[ActionsOrchestrator.Get]
    Orchestrator --> Controller[ActionsController.Get]
    Controller --> Middleware[NuciAPI Exception Middleware]
    Middleware --> Translate[Translate to API error response]
    Translate --> Client[HTTP error response]
    
    Exception -->|Steam| LogErrorSteam[Log Error with context]
    LogErrorSteam --> ReturnNull[return null]
    ReturnNull --> Orchestrator2[ActionsOrchestrator.Get]
    Orchestrator2 --> BuildResponse2[GetActionResponse with Data=null]
    BuildResponse2 --> Controller2[ActionsController.Get]
    Controller2 --> SuccessResponse[NuciApiSuccessResponse with null data]
    SuccessResponse --> Client2[HTTP 200 with null data]
    
    Orchestrator --> UnknownAction{Unknown GptAction?}
    UnknownAction -->|Yes| ThrowNotImpl[throw NotImplementedException]
    ThrowNotImpl --> Controller3[ActionsController.Get]
    Controller3 --> Middleware2[NuciAPI Exception Middleware]
    Middleware2 --> Client3[HTTP error response]
```

## Concurrency and Resource Flow

```mermaid
flowchart LR
    Request[Inbound Request] --> ThreadPool[ASP.NET Core Thread Pool]
    ThreadPool --> Controller[ActionsController]
    Controller --> Orchestrator[ActionsOrchestrator]
    Orchestrator --> SyncWait[.Result on async HTTP]
    SyncWait --> ThreadBlocked[Thread blocked during I/O]
    ThreadBlocked --> Adapter[Integration Service]
    Adapter --> HttpClient[HttpClient]
    HttpClient --> ExternalAPI[External API]
    ExternalAPI --> HttpClient
    HttpClient --> Adapter
    Adapter --> Orchestrator
    Orchestrator --> Controller
    Controller --> ThreadPool
    ThreadPool --> Response[Outbound Response]
```

**Implication:** Each request thread remains occupied during outbound I/O. Throughput constrained by thread pool size and external API latency.

## Related Documentation

- [Architecture Overview](architecture-overview.md) — high-level context
- [Orchestration Engine](orchestration-engine.md) — dispatch logic detail
- [Integration Adapters](integration-adapters.md) — adapter implementations
- [Error Handling](error-handling.md) — exception semantics
- [Component Reference](component-reference.md) — component details