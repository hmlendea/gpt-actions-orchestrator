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
    public sealed class ActionsControllerTransportIntegrationTests
    {
        private static string SteamQuery =>
            "?action=steam.store.app.get&appId=613";

        [TestCase("/Actions")]
        [TestCase("/actions")]
        [TestCase("/ACTIONS")]
        [TestCase("/Actions/")]
        public async Task GivenAValidRouteVariant_WhenRequestingAnAction_ThenTheRouteIsMatched(
            string route)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            ConfigureSteamResponse(applicationFactory);
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);

            using HttpResponseMessage response = await httpClient.GetAsync($"{route}{SteamQuery}");
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetSteamAppData"));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData("613"),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("POST")]
        [TestCase("PUT")]
        [TestCase("PATCH")]
        [TestCase("DELETE")]
        [TestCase("HEAD")]
        [TestCase("OPTIONS")]
        public async Task GivenAnUnsupportedHttpMethod_WhenRequestingTheActionsRoute_ThenMethodNotAllowedIsReturned(
            string method)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
            using HttpRequestMessage request = new(new HttpMethod(method), $"/Actions{SteamQuery}");

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("/Missing")]
        [TestCase("/api/Actions")]
        [TestCase("/Actions/613")]
        [TestCase("/Action")]
        [TestCase("/Actions.json")]
        public async Task GivenAnUnknownRoute_WhenRequestingTheRoute_ThenNotFoundIsReturned(
            string route)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);

            using HttpResponseMessage response = await httpClient.GetAsync($"{route}{SteamQuery}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("X-Forwarded-For", "127.0.0.2")]
        [TestCase("X-Forwarded-For", "invalid, 127.0.0.3")]
        [TestCase("X-Forwarded-For", "127.0.0.4:613")]
        [TestCase("Forwarded", "for=127.0.0.5;proto=https")]
        [TestCase("Forwarded", "for=\"[::1]\";proto=https")]
        [TestCase("X-Real-IP", "::1")]
        [TestCase("CF-Connecting-IP", "127.0.0.6")]
        [TestCase("True-Client-IP", "127.0.0.7")]
        public async Task GivenAValidForwardingHeader_WhenRequestingAnAction_ThenTheClientAddressIsAccepted(
            string headerName,
            string headerValue)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            ConfigureSteamResponse(applicationFactory);
            using HttpClient httpClient = ActionsHttpClientFactory.Create(applicationFactory);
            httpClient.DefaultRequestHeaders.Remove(IntegrationTestSettings.ForwardedForHeaderName);
            httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                IntegrationTestSettings.AuthorisationHeaderName,
                $"{IntegrationTestSettings.BearerScheme} {IntegrationTestSettings.ApiKey}");

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"{IntegrationTestSettings.ActionsPath}{SteamQuery}");
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.That(
                responseDocument.RootElement.GetProperty("action").GetString(),
                Is.EqualTo("GetSteamAppData"));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData("613"),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenNoClientAddress_WhenRequestingAnAction_ThenBadRequestIsReturned()
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
            httpClient.DefaultRequestHeaders.Remove(IntegrationTestSettings.ForwardedForHeaderName);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"{IntegrationTestSettings.ActionsPath}{SteamQuery}");
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.BadRequest,
                "BAD_REQUEST");

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Does.Contain("valid IP address"));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("/appsettings.json", null, null)]
        [TestCase("/wp-admin/", null, null)]
        [TestCase("/.git/config", null, null)]
        [TestCase("/Actions", "From", "oai-searchbot(at)openai.com")]
        [TestCase("/Actions", "User-Agent", "SecurityScanner/1.0")]
        [TestCase("/Actions", "User-Agent", "InternetMeasurement/1.0")]
        [TestCase("/Actions", "sec-ch-ua", ".Not/A)Brand")]
        public async Task GivenAForbiddenScannerSignature_WhenRequestingAResource_ThenForbiddenIsReturned(
            string route,
            string? headerName,
            string? headerValue)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);

            if (headerName is not null && headerValue is not null)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(headerName, headerValue);
            }

            using HttpResponseMessage response = await httpClient.GetAsync($"{route}{SteamQuery}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("file=../../etc/passwd")]
        [TestCase("page=gravitysmtp-settings")]
        [TestCase("rest_route=/wp/v2/users")]
        [TestCase("XDEBUG_SESSION_START=phpstorm")]
        [TestCase("app_vl=TestPassword!")]
        public async Task GivenAForbiddenQuerySignature_WhenRequestingAnAction_ThenForbiddenIsReturned(
            string forbiddenQuery)
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
            string requestPath = $"/Actions?{forbiddenQuery}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAnAddressThatTriggeredScannerProtection_WhenRequestingAValidAction_ThenTheAddressRemainsForbidden()
        {
            using ActionsWebApplicationFactory applicationFactory = new();
            using HttpClient httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);

            using HttpResponseMessage forbiddenResponse = await httpClient.GetAsync("/appsettings.json");
            using HttpResponseMessage subsequentResponse = await httpClient.GetAsync(
                $"{IntegrationTestSettings.ActionsPath}{SteamQuery}");

            Assert.Multiple(() =>
            {
                Assert.That(forbiddenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(subsequentResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            });
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        private static void ConfigureSteamResponse(ActionsWebApplicationFactory applicationFactory)
            => applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData("613"))
                .Returns(new SteamAppEntity
                {
                    Id = "613",
                    Name = "Dark Souls III"
                });
    }
}