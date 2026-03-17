using System.Collections.Generic;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;

namespace GptActionsOrchestrator.Integrations.GitHub.Service
{
    public interface IGitHubService
    {
        /// <summary>
        /// Retrieves the repositories of a GitHub user. If the username is null or empty, or matches the authenticated user, retrieves the repositories of the authenticated user.
        /// </summary>
        /// <param name="username">The username of the GitHub user.</param>
        /// <returns>A read-only collection of GitHub repositories.</returns>
        IReadOnlyCollection<GitHubRepository> GetUserRepositories(
            string username);

        /// <summary>
        /// Retrieves a GitHub repository by its name and the username of its owner.
        /// </summary>
        /// <param name="username">The username of the GitHub user.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <returns>The GitHub repository.</returns>
        GitHubRepository GetRepository(
            string username,
            string repositoryName);

        /// <summary>
        /// Retrieves the content of a file in a GitHub repository.
        /// </summary>
        /// <param name="username">The username of the GitHub user.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="path">The path to the file in the repository.</param>
        /// <returns>The content of the file.</returns>
        string GetRepositoryFile(
            string username,
            string repositoryName,
            string path);

        /// <summary>
        /// Retrieves the releases of a GitHub repository.
        /// </summary>
        /// <param name="username">The username of the GitHub user.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <returns>A read-only collection of GitHub releases.</returns>
        IReadOnlyCollection<GitHubRelease> GetRepositoryReleases(
            string username,
            string repositoryName);
    }
}