using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.WebUtilities;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerSmokeTests
    {
        private ActionsWebApplicationFactory applicationFactory = null!;
        private HttpClient httpClient = null!;

        [SetUp]
        public void SetUp()
        {
            applicationFactory = new ActionsWebApplicationFactory();
            httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
        }

        [TearDown]
        public void TearDown()
        {
            httpClient.Dispose();
            applicationFactory.Dispose();
        }

        [Test]
        public async Task GivenAnAuthorisedCanonicalRepositoryRequest_WhenRequestingTheAction_ThenTheRepositoryIsReturned()
        {
            string username = "Angetenar";
            string repositoryName = "Narivia";
            GitHubRepository expectedRepository = new()
            {
                Name = repositoryName,
                Description = "Praise the Sun!",
                Language = "C#"
            };
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepository(username, repositoryName))
                .Returns(expectedRepository);
            Dictionary<string, string?> parameters = new()
            {
                ["action"] = "github.repository.get",
                ["username"] = username,
                ["repository"] = repositoryName
            };
            string requestPath = QueryHelpers.AddQueryString(
                IntegrationTestSettings.ActionsPath,
                parameters);

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                responseContent);

            using JsonDocument responseDocument = JsonDocument.Parse(responseContent);

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubRepository"));
                Assert.That(
                    responseDocument.RootElement.GetProperty("data").GetProperty("name").GetString(),
                    Is.EqualTo(repositoryName));
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepository(username, repositoryName),
                Times.Once);
        }
    }
}