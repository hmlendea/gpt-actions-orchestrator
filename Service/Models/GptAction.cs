using System;
using System.Collections.Generic;
using System.Linq;

namespace GptActionsOrchestrator.Service.Models
{
    public class GptAction : IEquatable<GptAction>
    {
        static readonly Dictionary<string, GptAction> values = new()
        {
            { nameof(Unknown), new GptAction("unknown", nameof(Unknown)) },
            { nameof(GetGitHubRepositoryFile), new GptAction("github.repository.file.get", nameof(GetGitHubRepositoryFile)) },
            { nameof(GetGitHubRepositoryReadme), new GptAction("github.repository.readme.get", nameof(GetGitHubRepositoryReadme)) },
            { nameof(GetGitHubRepositoryReleases), new GptAction("github.repository.releases.get", nameof(GetGitHubRepositoryReleases)) },
            { nameof(GetGitHubUserRepositories), new GptAction("github.user.repositories.get", nameof(GetGitHubUserRepositories)) },
            { nameof(GetPersonalLogs), new GptAction("personallogmanager.logs.get", nameof(GetPersonalLogs)) },
            { nameof(GetSteamAppData), new GptAction("steam.store.app.get", nameof(GetSteamAppData)) },
        };

        public string Id { get; }

        public string Name { get; }

        GptAction(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public static GptAction Unknown => values[nameof(Unknown)];
        public static GptAction GetGitHubRepositoryFile => values[nameof(GetGitHubRepositoryFile)];
        public static GptAction GetGitHubRepositoryReadme => values[nameof(GetGitHubRepositoryReadme)];
        public static GptAction GetGitHubRepositoryReleases => values[nameof(GetGitHubRepositoryReleases)];
        public static GptAction GetGitHubUserRepositories => values[nameof(GetGitHubUserRepositories)];
        public static GptAction GetPersonalLogs => values[nameof(GetPersonalLogs)];
        public static GptAction GetSteamAppData => values[nameof(GetSteamAppData)];

        public static Array GetValues() => values.Values.ToArray();

        public bool Equals(GptAction other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((GptAction)obj);
        }

        public override int GetHashCode() => $"{nameof(GptAction)}:{Id}".GetHashCode();

        public override string ToString() => Name;

        public static GptAction FromString(string value)
        {
            if (values.ContainsKey(value))
            {
                return values[value];
            }

            if (values.Values.Any(v => v.Id == value))
            {
                return values.Values.First(v => v.Id == value);
            }

            return Unknown;
        }

        public static bool operator ==(GptAction current, GptAction other) => current.Equals(other);

        public static bool operator !=(GptAction current, GptAction other) => !current.Equals(other);

        public static implicit operator string(GptAction obj) => obj.Name;
    }
}
