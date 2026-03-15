using System.Collections.Generic;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Service
{
    public interface IPersonalLogManagerService
    {
        public PersonalLogs GetPersonalLogs(
            string dateBeginning,
            string dateEnd,
            string template,
            string localisation,
            Dictionary<string, string> data,
            string count);
    }
}
