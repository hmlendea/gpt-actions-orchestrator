using System.Collections.Generic;
using GptActionsOrchestrator.Api.Responses;

namespace GptActionsOrchestrator.Service
{
    public interface IActionsOrchestrator
    {
        public GetActionResponse Get(Dictionary<string, string> parameters);
    }
}
