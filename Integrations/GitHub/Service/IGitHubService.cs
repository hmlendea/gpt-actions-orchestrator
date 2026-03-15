using System.Collections.Generic;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;

namespace GptActionsOrchestrator.Integrations.GitHub.Service
{
    public interface IGitHubService
    {
        IReadOnlyCollection<GitHubRepository> GetUserRepositories(string username);
        string GetRepositoryFile(string username, string repositoryName, string path);
    }
}