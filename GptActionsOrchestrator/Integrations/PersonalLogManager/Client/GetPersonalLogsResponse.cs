using System.Collections.Generic;
using System.Text.Json.Serialization;
using NuciAPI.Responses;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Client
{
    public sealed class GetPersonalLogsResponse : NuciApiSuccessResponse
    {
        [JsonPropertyName("logs")]
        public List<string> Logs { get; set; } = [];

        [JsonPropertyName("count")]
        public int Count => Logs.Count;
    }
}
