using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Mvc.Testing;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal static class ActionsHttpClientFactory
    {
        internal static HttpClient Create(ActionsWebApplicationFactory applicationFactory)
        {
            HttpClient httpClient = applicationFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = IntegrationTestSettings.ServerBaseAddress
            });
            httpClient.DefaultRequestHeaders.Add(
                IntegrationTestSettings.ForwardedForHeaderName,
                IntegrationTestSettings.ClientIpAddress);

            return httpClient;
        }

        internal static HttpClient CreateAuthorised(ActionsWebApplicationFactory applicationFactory)
        {
            HttpClient httpClient = Create(applicationFactory);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                IntegrationTestSettings.BearerScheme,
                IntegrationTestSettings.ApiKey);

            return httpClient;
        }
    }
}