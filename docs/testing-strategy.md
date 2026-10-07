# Testing Strategy

Comprehensive documentation of the testing approach, test structure, patterns, and conventions.

## Overview

The system uses a multi-layered testing strategy:
1. **Unit Tests** — Component-level isolation testing
2. **Integration Tests** — End-to-end HTTP API testing
3. **Test Framework** — NUnit
4. **Mocking** — Moq
5. **HTTP Testing** — ASP.NET Core WebApplicationFactory

## Test Project Structure

```
GptActionsOrchestrator.UnitTests/
├── Service/
│   └── ActionsOrchestratorTests.cs
├── Integrations/
│   └── PersonalLogManager/
│       └── PersonalLogManagerInvocationTests.cs
└── Models/
    └── GptActionTests.cs

GptActionsOrchestrator.IntegrationTests/
├── Api/
│   └── Controllers/
│       └── ActionsControllerTests.cs
├── Infrastructure/
│   ├── ActionsWebApplicationFactory.cs
│   ├── ActionRequestPathBuilder.cs
│   ├── ActionResponseReader.cs
│   ├── ActionErrorResponseReader.cs
│   ├── ActionsHttpClientFactory.cs
│   └── IntegrationTestSettings.cs
└── PersonalLogManagerInvocation.cs
```

## Unit Tests

### Framework

**Test Framework:** NUnit 3.x
**Mocking:** Moq 4.x
**Assertions:** NUnit Constraint Model

### Test Conventions

**File:** `GptActionsOrchestrator.UnitTests/Service/ActionsOrchestratorTests.cs`

```csharp
[TestFixture]
public class ActionsOrchestratorTests
{
    private Mock<IGitHubService> _gitHubServiceMock;
    private Mock<ISteamStoreService> _steamStoreServiceMock;
    private Mock<IPersonalLogManagerService> _plmServiceMock;
    private Mock<ILogger> _loggerMock;
    private Mock<IActionAliasRepository> _aliasRepositoryMock;

    private ActionsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _gitHubServiceMock = new Mock<IGitHubService>();
        _steamStoreServiceMock = new Mock<ISteamStoreService>();
        _plmServiceMock = new Mock<IPersonalLogManagerService>();
        _loggerMock = new Mock<ILogger>();
        _aliasRepositoryMock = new Mock<IActionAliasRepository>();

        _orchestrator = new ActionsOrchestrator(
            _gitHubServiceMock.Object,
            _steamStoreServiceMock.Object,
            _plmServiceMock.Object,
            _loggerMock.Object,
            _aliasRepositoryMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Verify no unexpected calls
        _gitHubServiceMock.VerifyNoOtherCalls();
        _steamStoreServiceMock.VerifyNoOtherCalls();
        _plmServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
        _aliasRepositoryMock.VerifyNoOtherCalls();
    }
}
```

### Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

**Examples:**
- `ExecuteAction_WhenGitHubAction_ReturnsGitHubData`
- `ExecuteAction_WhenAdapterThrows_ReturnsErrorResponse`
- `ResolveAlias_WhenAliasExists_ReturnsResolvedAction`

### Test Structure (AAA Pattern)

```csharp
[Test]
public async Task ExecuteAction_WhenGitHubAction_ReturnsGitHubData()
{
    // Arrange
    var action = new GptAction
    {
        Name = "github-repository",
        Parameters = new Dictionary<string, string>
        {
            ["username"] = "testuser",
            ["repository"] = "testrepo"
        }
    };

    var expectedRepo = new GitHubRepository { Name = "testrepo" };
    _gitHubServiceMock.Setup(x => x.GetRepository("testuser", "testrepo"))
        .ReturnsAsync(expectedRepo);

    // Act
    var response = await _orchestrator.ExecuteAction(action);

    // Assert
    Assert.IsTrue(response.Success);
    Assert.AreEqual(expectedRepo, response.Data);

    _gitHubServiceMock.Verify(x => x.GetRepository("testuser", "testrepo"), Times.Once);
}
```

### Mocking Patterns

#### Service Mocks

```csharp
// Setup with specific parameters
_gitHubServiceMock.Setup(x => x.GetRepository("user", "repo"))
    .ReturnsAsync(new GitHubRepository());

// Setup with any parameters
_gitHubServiceMock.Setup(x => x.GetRepository(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(new GitHubRepository());

// Setup with exception
_gitHubServiceMock.Setup(x => x.GetRepository(It.IsAny<string>(), It.IsAny<string>()))
    .ThrowsAsync(new HttpRequestException("API error"));

// Verify calls
_gitHubServiceMock.Verify(x => x.GetRepository("user", "repo"), Times.Once);
```

#### Logger Mocks

```csharp
// Verify log calls
_loggerMock.Verify(
    x => x.Info(
        MyOperation.GitHubRepositoryRetrieval,
        OperationStatus.Started,
        It.IsAny<IEnumerable<LogInfo>>()),
    Times.Once);

_loggerMock.Verify(
    x => x.Error(
        MyOperation.GitHubRepositoryRetrieval,
        OperationStatus.Failure,
        It.IsAny<Exception>(),
        It.IsAny<IEnumerable<LogInfo>>()),
    Times.Once);
```

#### Alias Repository Mocks

```csharp
_aliasRepositoryMock.Setup(x => x.GetAlias("alias-name"))
    .ReturnsAsync(new GptActionAliasDataObject
    {
        Alias = "alias-name",
        Action = "github-repository",
        Parameters = new Dictionary<string, string>
        {
            ["username"] = "default-user"
        }
    });
```

## Integration Tests

### Test Infrastructure

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionsWebApplicationFactory.cs`

```csharp
public class ActionsWebApplicationFactory : WebApplicationFactory<Startup>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real services with mocks
            services.RemoveAll<IGitHubService>();
            services.AddSingleton(Mock.Of<IGitHubService>());

            services.RemoveAll<ISteamStoreService>();
            services.AddSingleton(Mock.Of<ISteamStoreService>());

            services.RemoveAll<IPersonalLogManagerService>();
            services.AddSingleton(Mock.Of<IPersonalLogManagerService>());

            // Suppress logging
            services.RemoveAll<ILogger>();
            services.AddSingleton(Mock.Of<ILogger>());
        });
    }
}
```

### HTTP Client Factory

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionsHttpClientFactory.cs`

```csharp
public static class ActionsHttpClientFactory
{
    public static HttpClient CreateClient(WebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-api-key");
        return client;
    }
}
```

### Request/Response Helpers

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionRequestPathBuilder.cs`

```csharp
public static class ActionRequestPathBuilder
{
    public static StringContent Build(GptAction action)
    {
        var request = new GetActionRequest { Action = action };
        var json = JsonSerializer.Serialize(request);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
```

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionResponseReader.cs`

```csharp
public static class ActionResponseReader
{
    public static async Task<GptActionResponse> Read(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GptActionResponse>(json);
    }
}
```

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/ActionErrorResponseReader.cs`

```csharp
public static class ActionErrorResponseReader
{
    public static async Task<string> Read(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("error").GetProperty("message").GetString();
    }
}
```

### Integration Test Example

**File:** `GptActionsOrchestrator.IntegrationTests/Api/Controllers/ActionsControllerTests.cs`

```csharp
public class ActionsControllerTests : IClassFixture<ActionsWebApplicationFactory>
{
    private readonly ActionsWebApplicationFactory _factory;

    public ActionsControllerTests(ActionsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteAction_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = ActionsHttpClientFactory.CreateClient(_factory);
        var action = new GptAction
        {
            Name = "github-repository",
            Parameters = new Dictionary<string, string>
            {
                ["username"] = "testuser",
                ["repository"] = "testrepo"
            }
        };
        var content = ActionRequestPathBuilder.Build(action);

        // Act
        var response = await client.PostAsync("/api/actions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ActionResponseReader.Read(response);
        result.Success.Should().BeTrue();
    }
}
```

## Test Configuration

### Integration Test Settings

**File:** `GptActionsOrchestrator.IntegrationTests/Infrastructure/IntegrationTestSettings.cs`

```csharp
public class IntegrationTestSettings
{
    public const string ApiKey = "test-api-key";
    public const string BaseUrl = "https://localhost:5001";
}
```

### Test Data

**File:** `GptActionsOrchestrator.IntegrationTests/PersonalLogManagerInvocation.cs`

```csharp
public static class PersonalLogManagerInvocation
{
    public static PersonalLogManagerInvocationData GetPersonalLogsInvocation =>
        new PersonalLogManagerInvocationData
        {
            Action = "personal-log-manager-get-personal-logs",
            Parameters = new Dictionary<string, string>
            {
                ["template"] = "default",
                ["dateBeginning"] = "2024-01-01",
                ["dateEnd"] = "2024-12-31",
                ["localisation"] = "en-US"
            }
        };
}
```

## Running Tests

### Unit Tests

```bash
# Run all unit tests
dotnet test GptActionsOrchestrator.UnitTests/

# Run specific test class
dotnet test GptActionsOrchestrator.UnitTests/ --filter "FullyQualifiedName~ActionsOrchestratorTests"

# Run with coverage
dotnet test GptActionsOrchestrator.UnitTests/ --collect:"XPlat Code Coverage"
```

### Integration Tests

```bash
# Run all integration tests
dotnet test GptActionsOrchestrator.IntegrationTests/

# Run specific test
dotnet test GptActionsOrchestrator.IntegrationTests/ --filter "FullyQualifiedName~ActionsControllerTests"
```

### All Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

## Test Coverage

### Unit Test Coverage

| Component | Coverage | Notes |
|-----------|----------|-------|
| ActionsOrchestrator | High | All dispatch branches tested |
| GptAction model | High | Validation and alias resolution |
| PersonalLogManagerInvocation | Medium | HMAC signing tested |

### Integration Test Coverage

| Endpoint | Coverage | Notes |
|----------|----------|-------|
| POST /api/actions | High | All action types tested |
| Authentication | High | Valid/invalid API key |
| Error handling | Medium | Adapter failures tested |

## Test Data Conventions

### Standard Test Values

| Field | Value |
|-------|-------|
| Username | `testuser` |
| Repository | `testrepo` |
| App ID | `123456` |
| Template | `default` |
| Date Beginning | `2024-01-01` |
| Date End | `2024-12-31` |
| Localisation | `en-US` |
| API Key | `test-api-key` |

### Test Data Builders

```csharp
public static class GptActionBuilder
{
    public static GptAction GitHubRepository(string username = "testuser", string repository = "testrepo") =>
        new GptAction
        {
            Name = "github-repository",
            Parameters = new Dictionary<string, string>
            {
                ["username"] = username,
                ["repository"] = repository
            }
        };

    public static GptAction SteamAppData(string appId = "123456") =>
        new GptAction
        {
            Name = "steam-app-data",
            Parameters = new Dictionary<string, string>
            {
                ["appId"] = appId
            }
        };

    public static GptAction PersonalLogs(string template = "default") =>
        new GptAction
        {
            Name = "personal-log-manager-get-personal-logs",
            Parameters = new Dictionary<string, string>
            {
                ["template"] = template,
                ["dateBeginning"] = "2024-01-01",
                ["dateEnd"] = "2024-12-31",
                ["localisation"] = "en-US"
            }
        };
}
```

## Mocking Strategy

### What to Mock

| Dependency | Mocked | Reason |
|------------|--------|--------|
| IGitHubService | Yes | External API |
| ISteamStoreService | Yes | External API |
| IPersonalLogManagerService | Yes | External API |
| IActionAliasRepository | Yes | File I/O |
| ILogger | Yes | Suppress output |
| IConfiguration | No | Use real config |
| IOptions&lt;T&gt; | No | Use real options |

### Mock Verification

```csharp
// Verify exact call count
_serviceMock.Verify(x => x.Method(It.IsAny<string>()), Times.Once);

// Verify no calls
_serviceMock.Verify(x => x.Method(It.IsAny<string>()), Times.Never);

// Verify no other calls
_serviceMock.VerifyNoOtherCalls();

// Verify call with specific parameters
_serviceMock.Verify(x => x.Method("expected-value"), Times.Once);
```

## Continuous Integration

### GitHub Actions

**File:** `.github/workflows/dotnet.yml`

```yaml
name: .NET Build and Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run unit tests
      run: dotnet test GptActionsOrchestrator.UnitTests/ --no-build

    - name: Run integration tests
      run: dotnet test GptActionsOrchestrator.IntegrationTests/ --no-build
```

## Test Best Practices

### Do

- Use descriptive test names following `MethodName_StateUnderTest_ExpectedBehavior`
- Test one behavior per test method
- Use `VerifyNoOtherCalls()` to catch unexpected interactions
- Mock external dependencies, not internal logic
- Use `It.IsAny<T>()` for parameters not relevant to the test
- Clean up resources in `TearDown`
- Test both success and failure paths

### Don't

- Don't test implementation details (private methods)
- Don't use real external services in unit tests
- Don't share state between tests
- Don't use magic strings/numbers (use constants)
- Don't ignore exceptions in tests
- Don't test framework behavior (e.g., ASP.NET Core routing)

## Related Documentation

- [Architecture Overview](architecture-overview.md) - Testable design
- [Orchestration Engine](orchestration-engine.md) - Orchestrator testing
- [Integration Adapters](integration-adapters.md) - Adapter testing
- [API Boundary](api-boundary.md) - Controller testing
- [Source Map](source-map.md) - Test file locations