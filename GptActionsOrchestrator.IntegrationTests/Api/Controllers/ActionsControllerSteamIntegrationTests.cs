using System.Collections.Generic;
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
    public sealed class ActionsControllerSteamIntegrationTests
    {
        private static string ActionParameterName => "action";

        private static string AppIdParameterName => "appId";

        private static IEnumerable<string> SteamActionValues =>
        [
            "GetSteamAppData",
            "steam.store.app.get",
            "steam.app.data.get",
            "steam.app.get",
            "steam.store.app.data.get"
        ];

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
            => applicationFactory.SteamStoreServiceMock.Reset();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient.Dispose();
            applicationFactory.Dispose();
        }

        [TestCaseSource(nameof(GetSteamRequestCases))]
        public async Task GivenASteamActionWithADiverseAppId_WhenRequestingTheAction_ThenTheAppDataIsReturned(
            string actionValue,
            string? appId)
        {
            SteamAppEntity expectedApp = new()
            {
                Id = "613",
                Name = "Dark Souls III"
            };
            applicationFactory.SteamStoreServiceMock
                .Setup(service => service.GetAppData(appId!))
                .Returns(expectedApp);
            Dictionary<string, string?> parameters = BuildSteamParameters(actionValue, appId);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);
            JsonElement responseRoot = responseDocument.RootElement;
            JsonElement responseData = responseRoot.GetProperty("data");

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseRoot.GetProperty("action").GetString(),
                    Is.EqualTo("GetSteamAppData"));
                Assert.That(responseData.GetProperty("id").GetString(), Is.EqualTo("613"));
                Assert.That(responseData.GetProperty("name").GetString(), Is.EqualTo("Dark Souls III"));
            });
            applicationFactory.SteamStoreServiceMock.Verify(
                service => service.GetAppData(appId!),
                Times.Once);
            applicationFactory.SteamStoreServiceMock.VerifyNoOtherCalls();
        }

        private static IEnumerable<TestCaseData> GetSteamRequestCases()
        {
            foreach (string actionValue in SteamActionValues)
            {
                yield return new TestCaseData(actionValue, "0");
                yield return new TestCaseData(actionValue, "-4");
                yield return new TestCaseData(actionValue, "4");
                yield return new TestCaseData(actionValue, "8");
                yield return new TestCaseData(actionValue, "16");
                yield return new TestCaseData(actionValue, "32");
                yield return new TestCaseData(actionValue, "42");
                yield return new TestCaseData(actionValue, "48");
                yield return new TestCaseData(actionValue, "64");
                yield return new TestCaseData(actionValue, "96");
                yield return new TestCaseData(actionValue, "128");
                yield return new TestCaseData(actionValue, "256");
                yield return new TestCaseData(actionValue, "512");
                yield return new TestCaseData(actionValue, "613");
                yield return new TestCaseData(actionValue, "873");
                yield return new TestCaseData(actionValue, "1024");
                yield return new TestCaseData(actionValue, "2048");
                yield return new TestCaseData(actionValue, "4096");
                yield return new TestCaseData(actionValue, "8192");
                yield return new TestCaseData(actionValue, "2147483647");
                yield return new TestCaseData(actionValue, "999999999999999999999999999999999999");
                yield return new TestCaseData(actionValue, "Dark Souls III");
                yield return new TestCaseData(actionValue, "Çupișan");
                yield return new TestCaseData(actionValue, "A&B?variant=613");
                yield return new TestCaseData(actionValue, string.Empty);
                yield return new TestCaseData(actionValue, "   ");
                yield return new TestCaseData(actionValue, null);
            }
        }

        private static Dictionary<string, string?> BuildSteamParameters(
            string actionValue,
            string? appId)
        {
            Dictionary<string, string?> parameters = new()
            {
                [ActionParameterName] = actionValue
            };

            if (appId is not null)
            {
                parameters[AppIdParameterName] = appId;
            }

            return parameters;
        }
    }
}