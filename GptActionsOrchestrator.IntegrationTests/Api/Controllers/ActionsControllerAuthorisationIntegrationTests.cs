using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerAuthorisationIntegrationTests
    {
        private static string AuthenticationFailureCode => "AUTHENTICATION_FAILURE";

        private static string SteamRequestPath =>
            "/Actions?action=steam.store.app.get&appId=613";

        private ActionsWebApplicationFactory applicationFactory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
            => applicationFactory = new ActionsWebApplicationFactory();

        [SetUp]
        public void SetUp()
            => applicationFactory.SteamStoreServiceMock.Reset();

        [OneTimeTearDown]
        public void OneTimeTearDown()
            => applicationFactory.Dispose();

        [TestCase("TestPassword!")]
        [TestCase("Bearer TestPassword!")]
        [TestCase("bearer TestPassword!")]
        [TestCase("BEARER TestPassword!")]
        [TestCase("Bearer    TestPassword!")]
        public async Task GivenAValidAuthorisationHeader_WhenRequestingAnAction_ThenTheRequestIsAuthorised(
            string authorisationHeaderValue)
        {
            SteamAppEntity expectedApp = new()
            {
                Id = "613",
                Name = "Dark Souls III"
            };
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData("613"))
                .Returns(expectedApp);
            using HttpClient httpClient = CreateClient(authorisationHeaderValue);

            using HttpResponseMessage response = await httpClient.GetAsync(SteamRequestPath);
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetSteamAppData"));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData("613"),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Bearer")]
        [TestCase("Bearer ")]
        [TestCase("Bearer P@ssw0rd!")]
        [TestCase("Basic TestPassword!")]
        [TestCase("TestPassword!x")]
        [TestCase("testpassword!")]
        [TestCase("ApiKey TestPassword!")]
        [TestCase("Bearer 1234567890")]
        public async Task GivenAnInvalidAuthorisationHeader_WhenRequestingAnAction_ThenAuthenticationFails(
            string? authorisationHeaderValue)
        {
            using HttpClient httpClient = CreateClient(authorisationHeaderValue);

            using HttpResponseMessage response = await httpClient.GetAsync(SteamRequestPath);
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.Unauthorized,
                AuthenticationFailureCode);

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("The authentication has failed."));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        private HttpClient CreateClient(string? authorisationHeaderValue)
        {
            HttpClient httpClient = ActionsHttpClientFactory.Create(applicationFactory);

            if (authorisationHeaderValue is not null)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    IntegrationTestSettings.AuthorisationHeaderName,
                    authorisationHeaderValue);
            }

            return httpClient;
        }
    }
}