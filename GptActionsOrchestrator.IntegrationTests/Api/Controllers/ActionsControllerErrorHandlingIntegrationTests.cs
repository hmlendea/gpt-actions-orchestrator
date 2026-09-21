using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerErrorHandlingIntegrationTests
    {
        private static int ClientClosedRequestStatusCode => 499;

        private static string NotImplementedCode => "NOT_IMPLEMENTED";

        private static string SteamRequestPath =>
            "/Actions?action=steam.store.app.get&appId=613";

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

        [TestCaseSource(nameof(GetUnsupportedActionCases))]
        public async Task GivenAnUnsupportedAction_WhenRequestingTheAction_ThenNotImplementedIsReturned(
            string? actionValue)
        {
            Dictionary<string, string?> parameters = [];

            if (actionValue is not null)
            {
                parameters["action"] = actionValue;
            }

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                HttpStatusCode.NotImplemented,
                NotImplementedCode);

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("The 'unknown' action is not supported."));
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetProviderExceptionCases))]
        public async Task GivenAProviderException_WhenRequestingTheAction_ThenTheExceptionIsMappedToTheApiContract(
            Exception providerException,
            int expectedStatusCode,
            string expectedErrorCode,
            string expectedMessage)
        {
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData("613"))
                .Throws(providerException);

            using HttpResponseMessage response = await httpClient.GetAsync(SteamRequestPath);
            using JsonDocument responseDocument = await ActionErrorResponseReader.ReadAsync(
                response,
                (HttpStatusCode)expectedStatusCode,
                expectedErrorCode);

            Assert.That(
                responseDocument.RootElement.GetProperty("message").GetString(),
                Is.EqualTo(expectedMessage));
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData("613"),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        private static IEnumerable<TestCaseData> GetUnsupportedActionCases()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData(string.Empty);
            yield return new TestCaseData(" ");
            yield return new TestCaseData("   ");
            yield return new TestCaseData("unknown");
            yield return new TestCaseData("Unknown");
            yield return new TestCaseData("GetUnknownAction");
            yield return new TestCaseData("getgithubrepository");
            yield return new TestCaseData("GETGITHUBREPOSITORY");
            yield return new TestCaseData("github.repository.GET");
            yield return new TestCaseData("github.repository.get ");
            yield return new TestCaseData(" github.repository.get");
            yield return new TestCaseData("github/repository/get");
            yield return new TestCaseData("github..repository..get");
            yield return new TestCaseData("steam.store.app.get?");
            yield return new TestCaseData("personal.logs.get#fragment");
            yield return new TestCaseData("Solaire of Astora");
            yield return new TestCaseData("Crăciun Fericit!");
            yield return new TestCaseData("Çupișan");
            yield return new TestCaseData("613");
            yield return new TestCaseData("-4");
            yield return new TestCaseData("A&B?variant=613");
            yield return new TestCaseData(new string('A', 4096));
        }

        private static IEnumerable<TestCaseData> GetProviderExceptionCases()
        {
            yield return BuildProviderExceptionCase(
                new BadHttpRequestException("The app ID request is malformed."),
                HttpStatusCode.BadRequest,
                "BAD_REQUEST");
            yield return BuildProviderExceptionCase(
                new FormatException("The app ID format is invalid."),
                HttpStatusCode.BadRequest,
                "BAD_REQUEST");
            yield return BuildProviderExceptionCase(
                new ArgumentException("The app ID argument is invalid.", "appId"),
                HttpStatusCode.BadRequest,
                "BAD_REQUEST");
            yield return BuildProviderExceptionCase(
                new ValidationException("The app ID has failed validation."),
                HttpStatusCode.BadRequest,
                "BAD_REQUEST");
            yield return BuildProviderExceptionCase(
                new SecurityException("The provider has denied access."),
                HttpStatusCode.Forbidden,
                "UNAUTHORISED",
                "You do not have the required permission to perform this action.");
            yield return BuildProviderExceptionCase(
                new UnauthorizedAccessException("The provider has denied access."),
                HttpStatusCode.Forbidden,
                "UNAUTHORISED",
                "You do not have the required permission to perform this action.");
            yield return BuildProviderExceptionCase(
                new HttpRequestException("The Steam dependency is unavailable."),
                HttpStatusCode.ServiceUnavailable,
                "SERVICE_DEPENDENCY_UNAVAILABLE",
                "A service dependency is currently unavailable.");
            yield return BuildProviderExceptionCase(
                new TaskCanceledException("The Steam dependency request was cancelled."),
                HttpStatusCode.ServiceUnavailable,
                "SERVICE_DEPENDENCY_UNAVAILABLE",
                "A service dependency is currently unavailable.");
            yield return BuildProviderExceptionCase(
                new TimeoutException("The Steam dependency has timed out."),
                HttpStatusCode.ServiceUnavailable,
                "SERVICE_DEPENDENCY_UNAVAILABLE",
                "A service dependency is currently unavailable.");
            yield return BuildProviderExceptionCase(
                new AuthenticationException("The provider credentials are invalid."),
                HttpStatusCode.Unauthorized,
                "AUTHENTICATION_FAILURE",
                "The authentication has failed.");
            yield return BuildProviderExceptionCase(
                new KeyNotFoundException("The Steam application was not found."),
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                "The requested resource was not found.");
            yield return BuildProviderExceptionCase(
                new NotImplementedException("The Steam operation is not implemented."),
                HttpStatusCode.NotImplemented,
                "NOT_IMPLEMENTED");
            yield return BuildProviderExceptionCase(
                new OperationCanceledException("The client has cancelled the request."),
                (HttpStatusCode)ClientClosedRequestStatusCode,
                "CLIENT_CLOSED_THE_REQUEST",
                "The client has closed the request.");
            yield return BuildProviderExceptionCase(
                new InvalidOperationException("The provider has failed unexpectedly."),
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An internal server error has occurred.");
        }

        private static TestCaseData BuildProviderExceptionCase(
            Exception providerException,
            HttpStatusCode expectedStatusCode,
            string expectedErrorCode)
            => BuildProviderExceptionCase(
                providerException,
                expectedStatusCode,
                expectedErrorCode,
                providerException.Message);

        private static TestCaseData BuildProviderExceptionCase(
            Exception providerException,
            HttpStatusCode expectedStatusCode,
            string expectedErrorCode,
            string expectedMessage)
            => new TestCaseData(
                providerException,
                (int)expectedStatusCode,
                expectedErrorCode,
                expectedMessage);
    }
}