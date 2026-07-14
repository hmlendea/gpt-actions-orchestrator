using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NuciAPI.Requests;

namespace GptActionsOrchestrator.Api.Requests
{
    public sealed class GetActionRequest : NuciApiRequest
    {
        [FromQuery(Name = "action")]
        [JsonPropertyName("action")]
        public string GptActionName { get; set; }
    }
}
