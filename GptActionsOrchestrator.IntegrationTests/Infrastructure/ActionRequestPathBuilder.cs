using System.Collections.Generic;

using Microsoft.AspNetCore.WebUtilities;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal static class ActionRequestPathBuilder
    {
        internal static string Build(IEnumerable<KeyValuePair<string, string?>> parameters)
            => QueryHelpers.AddQueryString(IntegrationTestSettings.ActionsPath, parameters);
    }
}