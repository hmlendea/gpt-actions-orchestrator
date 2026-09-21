using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal static class ActionResponseReader
    {
        internal static async Task<JsonDocument> ReadSuccessAsync(HttpResponseMessage response)
        {
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                responseContent);

            JsonDocument responseDocument = JsonDocument.Parse(responseContent);
            Assert.That(responseDocument.RootElement.GetProperty("success").GetBoolean());

            return responseDocument;
        }
    }
}