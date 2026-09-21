using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerQueryIntegrationTests
    {
        private ActionsWebApplicationFactory applicationFactory = null!;
        private HttpClient httpClient = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            applicationFactory = new ActionsWebApplicationFactory();
            httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
        }

        [SetUp]
        public void SetUp()
        {
            applicationFactory.GitHubServiceMock.Reset();
            applicationFactory.PersonalLogManagerServiceMock.Reset();
            applicationFactory.SteamStoreServiceMock.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient.Dispose();
            applicationFactory.Dispose();
        }

        [TestCase("username", "Angetenar", "DummyUser", "Angetenar,DummyUser")]
        [TestCase("username", "", "zezima", "zezima")]
        [TestCase("username", "Ionuț Karr", "Çupișan", "Ionuț Karr,Çupișan")]
        public async Task GivenDuplicateScalarParameters_WhenRequestingAnAction_ThenTheValuesAreCommaJoined(
            string parameterName,
            string firstValue,
            string secondValue,
            string expectedValue)
        {
            string? forwardedUsername = null;
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepository(It.IsAny<string>(), "Narivia"))
                .Callback((string username, string repositoryName) => forwardedUsername = username)
                .Returns(new GitHubRepository { Name = "Narivia" });
            string requestPath =
                $"/Actions?action=github.repository.get&{parameterName}={Uri.EscapeDataString(firstValue)}" +
                $"&{parameterName}={Uri.EscapeDataString(secondValue)}&repository=Narivia";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetGitHubRepository"));
            Assert.That(forwardedUsername, Is.EqualTo(expectedValue));
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepository(It.IsAny<string>(), "Narivia"),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("4", "613", "4,613")]
        [TestCase("", "8192", "8192")]
        [TestCase("Dark Souls III", "Çupișan", "Dark Souls III,Çupișan")]
        public async Task GivenDuplicateAppIds_WhenRequestingASteamAction_ThenTheValuesAreCommaJoined(
            string firstAppId,
            string secondAppId,
            string expectedAppId)
        {
            string? forwardedAppId = null;
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData(It.IsAny<string>()))
                .Callback((string appId) => forwardedAppId = appId)
                .Returns(new SteamAppEntity { Id = expectedAppId, Name = "Dark Souls III" });
            string requestPath =
                $"/Actions?action=steam.store.app.get&appId={Uri.EscapeDataString(firstAppId)}" +
                $"&appId={Uri.EscapeDataString(secondAppId)}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("data").GetProperty("id").GetString(),
                Is.EqualTo(expectedAppId));
            Assert.That(forwardedAppId, Is.EqualTo(expectedAppId));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData(It.IsAny<string>()),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenDuplicateActionParameters_WhenRequestingAnAction_ThenTheCombinedActionIsUnsupported()
        {
            string requestPath =
                "/Actions?action=github.repository.get&action=steam.store.app.get";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.NotImplemented,
                "NOT_IMPLEMENTED");

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("The 'unknown' action is not supported."));
            VerifyNoProviderCalls();
        }

        [TestCase("Action")]
        [TestCase("ACTION")]
        [TestCase("aCtIoN")]
        public async Task GivenAnActionParameterWithDifferentCasing_WhenRequestingAnAction_ThenTheActionIsUnsupported(
            string actionParameterName)
        {
            string requestPath = $"/Actions?{actionParameterName}=steam.store.app.get&appId=613";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.NotImplemented,
                "NOT_IMPLEMENTED");

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("The 'unknown' action is not supported."));
            VerifyNoProviderCalls();
        }

        [TestCase("AppId")]
        [TestCase("appID")]
        [TestCase("APPID")]
        public async Task GivenAnAppIdParameterWithDifferentCasing_WhenRequestingASteamAction_ThenNullIsForwarded(
            string appIdParameterName)
        {
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData(null!))
                .Returns(new SteamAppEntity { Id = "613", Name = "Dark Souls III" });
            string requestPath = $"/Actions?action=steam.store.app.get&{appIdParameterName}=613";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetSteamAppData"));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData(null!),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAdditionalParameters_WhenRequestingAnAction_ThenUnrecognisedParametersAreIgnored()
        {
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData("613"))
                .Returns(new SteamAppEntity { Id = "613", Name = "Dark Souls III" });
            string requestPath =
                "/Actions?action=steam.store.app.get&appId=613" +
                "&username=Angetenar&repository=Narivia&path=README.md" +
                "&data.phrase=Praise%20the%20Sun!&unrecognised=8192";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetSteamAppData"));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData("613"),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("Praise the Sun!", "Jolly cooperation!", "Praise the Sun!,Jolly cooperation!")]
        [TestCase("", "The cake is a lie", "The cake is a lie")]
        [TestCase("Crăciun Fericit!", "Çupișan", "Crăciun Fericit!,Çupișan")]
        public async Task GivenDuplicateNestedDataParameters_WhenRequestingPersonalLogs_ThenTheValuesAreCommaJoined(
            string firstValue,
            string secondValue,
            string expectedValue)
        {
            Dictionary<string, string>? forwardedData = null;
            applicationFactory.PersonalLogManagerServiceMock
                .Setup(service => service.GetPersonalLogs(
                    null!,
                    null!,
                    null!,
                    null!,
                    It.IsAny<Dictionary<string, string>>(),
                    null!))
                .Callback((
                    string dateBeginning,
                    string dateEnd,
                    string template,
                    string localisation,
                    Dictionary<string, string> data,
                    string count) => forwardedData = data)
                .Returns(new PersonalLogs());
            string requestPath =
                "/Actions?action=personallogmanager.logs.get" +
                $"&data.phrase={Uri.EscapeDataString(firstValue)}" +
                $"&data.phrase={Uri.EscapeDataString(secondValue)}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetPersonalLogs"));
            Assert.That(forwardedData, Is.Not.Null);
            Assert.That(forwardedData!["phrase"], Is.EqualTo(expectedValue));
            applicationFactory.PersonalLogManagerServiceMock.VerifyAll();
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAScalarParameterBeforeItsNestedParameter_WhenRequestingPersonalLogs_ThenInternalServerErrorIsReturned()
        {
            string requestPath =
                "/Actions?action=personallogmanager.logs.get&data=plain&data.phrase=Praise%20the%20Sun!";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR");

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("An internal server error has occurred."));
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenANestedParameterBeforeItsScalarParameter_WhenRequestingPersonalLogs_ThenNullDataIsForwarded()
        {
            applicationFactory.PersonalLogManagerServiceMock
                .Setup(service => service.GetPersonalLogs(
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!))
                .Returns(new PersonalLogs());
            string requestPath =
                "/Actions?action=personallogmanager.logs.get&data.phrase=Praise%20the%20Sun!&data=plain";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetPersonalLogs"));
            applicationFactory.PersonalLogManagerServiceMock.Verify(
                service => service.GetPersonalLogs(
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!),
                Times.Once);
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
        }

        private void VerifyNoProviderCalls()
        {
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }
    }
}