using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerPersonalLogManagerIntegrationTests
    {
        private static string ActionParameterName => "action";

        private static string CountParameterName => "count";

        private static string DataParameterPrefix => "data.";

        private static string DateBeginningParameterName => "date_beginning";

        private static string DateEndParameterName => "date_end";

        private static string LocalisationParameterName => "localisation";

        private static string TemplateParameterName => "template";

        private static IEnumerable<string> PersonalLogsActionValues =>
        [
            "GetPersonalLogs",
            "personallogmanager.logs.get",
            "personal.logs.get",
            "personallogmanager.log.get"
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
            => applicationFactory.PersonalLogManagerServiceMock.Reset();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient.Dispose();
            applicationFactory.Dispose();
        }

        [TestCaseSource(nameof(GetScalarParameterRequestCases))]
        public async Task GivenAPersonalLogsActionWithDiverseScalarParameters_WhenRequestingTheAction_ThenTheParametersAreForwarded(
            string actionValue,
            string? dateBeginning,
            string? dateEnd,
            string? template,
            string? localisation,
            string? count)
        {
            PersonalLogManagerInvocation invocation = ConfigurePersonalLogsResponse();
            Dictionary<string, string?> parameters = BuildPersonalLogsParameters(
                actionValue,
                dateBeginning,
                dateEnd,
                template,
                localisation,
                count);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);
            JsonElement responseRoot = responseDocument.RootElement;
            JsonElement responseData = responseRoot.GetProperty("data");

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseRoot.GetProperty("action").GetString(),
                    Is.EqualTo("GetPersonalLogs"));
                Assert.That(responseData.GetProperty("logs").GetArrayLength(), Is.EqualTo(3));
                Assert.That(responseData.GetProperty("count").GetInt32(), Is.EqualTo(3));
                Assert.That(invocation.DateBeginning, Is.EqualTo(dateBeginning));
                Assert.That(invocation.DateEnd, Is.EqualTo(dateEnd));
                Assert.That(invocation.Template, Is.EqualTo(template));
                Assert.That(invocation.Localisation, Is.EqualTo(localisation));
                Assert.That(invocation.Count, Is.EqualTo(count));
                Assert.That(invocation.Data, Is.Null);
            });
            applicationFactory.PersonalLogManagerServiceMock.Verify(
                service => service.GetPersonalLogs(
                    dateBeginning!,
                    dateEnd!,
                    template!,
                    localisation!,
                    null!,
                    count!),
                Times.Once);
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetNestedDataRequestCases))]
        public async Task GivenAPersonalLogsActionWithDiverseNestedData_WhenRequestingTheAction_ThenTheDataDictionaryIsReconstructed(
            string actionValue,
            IEnumerable<KeyValuePair<string, string>> dataParameters)
        {
            PersonalLogManagerInvocation invocation = ConfigurePersonalLogsResponse();
            Dictionary<string, string> expectedData = dataParameters.ToDictionary(
                dataParameter => dataParameter.Key,
                dataParameter => dataParameter.Value);
            Dictionary<string, string?> parameters = BuildPersonalLogsParameters(
                actionValue,
                "2012-09-05",
                "2012-09-05",
                "The Lord of the Rings",
                "ro-RO",
                "613");

            foreach (KeyValuePair<string, string> dataParameter in dataParameters)
            {
                parameters[$"{DataParameterPrefix}{dataParameter.Key}"] = dataParameter.Value;
            }

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetPersonalLogs"));
                Assert.That(invocation.Data, Is.EquivalentTo(expectedData));
            });
            applicationFactory.PersonalLogManagerServiceMock.Verify(
                service => service.GetPersonalLogs(
                    "2012-09-05",
                    "2012-09-05",
                    "The Lord of the Rings",
                    "ro-RO",
                    It.Is<Dictionary<string, string>>(data =>
                        data.Count == expectedData.Count &&
                        data.All(dataPair => expectedData.Contains(dataPair))),
                    "613"),
                Times.Once);
            applicationFactory.PersonalLogManagerServiceMock.VerifyNoOtherCalls();
        }

        private PersonalLogManagerInvocation ConfigurePersonalLogsResponse()
        {
            PersonalLogManagerInvocation invocation = new();
            PersonalLogs personalLogs = new()
            {
                Logs =
                [
                    "Praise the Sun!",
                    "The cake is a lie",
                    "What a beautiful day to stay inside and write tests"
                ]
            };
            applicationFactory.PersonalLogManagerServiceMock
                .Setup(service => service.GetPersonalLogs(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>()))
                .Callback((
                    string dateBeginning,
                    string dateEnd,
                    string template,
                    string localisation,
                    Dictionary<string, string> data,
                    string count) =>
                {
                    invocation.DateBeginning = dateBeginning;
                    invocation.DateEnd = dateEnd;
                    invocation.Template = template;
                    invocation.Localisation = localisation;
                    invocation.Data = data;
                    invocation.Count = count;
                })
                .Returns(personalLogs);

            return invocation;
        }

        private static IEnumerable<TestCaseData> GetScalarParameterRequestCases()
        {
            foreach (string actionValue in PersonalLogsActionValues)
            {
                yield return new TestCaseData(
                    actionValue,
                    "2012-09-05",
                    "2012-09-05",
                    "The Lord of the Rings",
                    "ro-RO",
                    "613");
                yield return new TestCaseData(actionValue, null, null, null, null, null);
                yield return new TestCaseData(
                    actionValue,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty);
                yield return new TestCaseData(actionValue, " ", "  ", "   ", "    ", "     ");
                yield return new TestCaseData(
                    actionValue,
                    "Când o' zbura porcu'",
                    "Crăciun Fericit!",
                    "Glorie Nucilandiei!",
                    "Çupișan",
                    "4");
                yield return new TestCaseData(
                    actionValue,
                    "A&B",
                    "What happens in Vegas, stays in Vegas",
                    "Solaire?variant=613",
                    "ro+RO",
                    "8");
                yield return new TestCaseData(actionValue, "2012-09-05", null, "daily", "ro", "0");
                yield return new TestCaseData(actionValue, null, "2012-09-05", "weekly", "en", "-4");
                yield return new TestCaseData(actionValue, "4", "8", "16", "32", "42");
                yield return new TestCaseData(actionValue, "48", "64", "96", "128", "256");
                yield return new TestCaseData(actionValue, "512", "613", "873", "1024", "2048");
                yield return new TestCaseData(actionValue, "4096", "8192", "Solaire", "Astora", "2147483647");
                yield return new TestCaseData(
                    actionValue,
                    "0001-01-01",
                    "9999-12-31",
                    "Aaaaaargghh",
                    "Nucilandia",
                    "999999999999999999999999999999999999");
                yield return new TestCaseData(
                    actionValue,
                    "not-a-date",
                    "undefined is not a function",
                    "404: Not Found",
                    "NLV",
                    "Solaire of Astora");
                yield return new TestCaseData(
                    actionValue,
                    "2012-09-05T00:00:00.0000000Z",
                    "2012-09-05T23:59:59.9999999Z",
                    "daily report",
                    "en-GB",
                    "8192");
            }
        }

        private static IEnumerable<TestCaseData> GetNestedDataRequestCases()
        {
            foreach (string actionValue in PersonalLogsActionValues)
            {
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string> { ["name"] = "Ilarion Pintilie" });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string>
                    {
                        ["name"] = "Solaire of Astora",
                        ["city"] = "Cluj-Napoca",
                        ["country"] = "Astora"
                    });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string>
                    {
                        ["phrase"] = "Crăciun Fericit!",
                        ["city"] = "Çupișan"
                    });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string>
                    {
                        ["profile.mood"] = "Jolly cooperation!",
                        ["profile.location.city"] = "Horidava"
                    });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string> { [string.Empty] = "The cake is a lie" });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string> { ["A&B?"] = "Solaire+Astora&count=613" });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string> { ["   "] = "  " });
                yield return new TestCaseData(
                    actionValue,
                    new Dictionary<string, string>
                    {
                        ["4"] = "8",
                        ["16"] = "32",
                        ["42"] = "613",
                        ["8192"] = string.Empty
                    });
            }
        }

        private static Dictionary<string, string?> BuildPersonalLogsParameters(
            string actionValue,
            string? dateBeginning,
            string? dateEnd,
            string? template,
            string? localisation,
            string? count)
        {
            Dictionary<string, string?> parameters = new()
            {
                [ActionParameterName] = actionValue
            };

            AddParameter(parameters, DateBeginningParameterName, dateBeginning);
            AddParameter(parameters, DateEndParameterName, dateEnd);
            AddParameter(parameters, TemplateParameterName, template);
            AddParameter(parameters, LocalisationParameterName, localisation);
            AddParameter(parameters, CountParameterName, count);

            return parameters;
        }

        private static void AddParameter(
            IDictionary<string, string?> parameters,
            string parameterName,
            string? parameterValue)
        {
            if (parameterValue is not null)
            {
                parameters[parameterName] = parameterValue;
            }
        }
    }
}