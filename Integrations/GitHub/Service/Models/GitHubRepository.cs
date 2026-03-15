using System.Text.Json.Serialization;

namespace GptActionsOrchestrator.Integrations.GitHub.Service.Models
{
    public sealed class GitHubRepository
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("archived")]
        public bool IsArchived { get; set; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; set; }
    }
}