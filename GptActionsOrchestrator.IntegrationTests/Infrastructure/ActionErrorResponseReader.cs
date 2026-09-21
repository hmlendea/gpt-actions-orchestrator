using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal static class ActionErrorResponseReader
    {
        internal static async Task<JsonDocument> ReadAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatusCode,
            string expectedErrorCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.That(
                response.StatusCode,
                Is.EqualTo(expectedStatusCode),
                responseContent);

            JsonDocument responseDocument = JsonDocument.Parse(responseContent);
            JsonElement responseRoot = responseDocument.RootElement;

            Assert.Multiple(() =>
            {
                Assert.That(responseRoot.GetProperty("success").GetBoolean(), Is.False);
                Assert.That(responseRoot.GetProperty("code").GetString(), Is.EqualTo(expectedErrorCode));
            });

            return responseDocument;
        }
    }
}