using System.Collections.Generic;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models
{
    public sealed class PersonalLogs
    {
        public List<string> Logs { get; set; } = [];

        public int Count => Logs.Count;
    }
}
