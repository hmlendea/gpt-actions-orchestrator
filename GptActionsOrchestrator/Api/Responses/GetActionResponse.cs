using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NuciAPI.Responses;

namespace GptActionsOrchestrator.Api.Responses
{
    public sealed class GetActionResponse : NuciApiSuccessResponse
    {
        [FromQuery(Name = "action")]
        [JsonPropertyName("action")]
        public string GptActionName { get; set; }

        public object Data { get; set; }
    }
}
