using System.Collections.Generic;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal sealed class PersonalLogManagerInvocation
    {
        internal string? DateBeginning { get; set; }

        internal string? DateEnd { get; set; }

        internal string? Template { get; set; }

        internal string? Localisation { get; set; }

        internal Dictionary<string, string>? Data { get; set; }

        internal string? Count { get; set; }
    }
}