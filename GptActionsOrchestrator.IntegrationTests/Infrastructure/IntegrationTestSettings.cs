using System;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal static class IntegrationTestSettings
    {
        internal static string ActionsPath => "/Actions";

        internal static string ApiKey => "TestPassword!";

        internal static string AuthorisationHeaderName => "Authorization";

        internal static string BearerScheme => "Bearer";

        internal static string ClientIpAddress => "127.0.0.1";

        internal static string ForwardedForHeaderName => "X-Forwarded-For";

        internal static Uri ServerBaseAddress => new("https://localhost");
    }
}