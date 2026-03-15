using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GptActionsOrchestrator.Integrations.GitHub.Service.Models
{
    public sealed class GitHubRepository
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("topics")]
        public IReadOnlyCollection<string> Topics { get; set; }

        [JsonPropertyName("archived")]
        public bool IsArchived { get; set; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("fork")]
        public bool IsFork { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("pushed_at")]
        public DateTimeOffset PushedAt { get; set; }
    }
}