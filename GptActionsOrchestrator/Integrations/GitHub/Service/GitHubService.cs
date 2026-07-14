using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using GptActionsOrchestrator.Integrations.GitHub.Configuration;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;
using GptActionsOrchestrator.Logging;

using NuciExtensions;
using NuciLog.Core;
using NuciWeb.HTTP;

namespace GptActionsOrchestrator.Integrations.GitHub.Service
{
    public sealed class GitHubService(GitHubSettings gitHubSettings, ILogger logger) : IGitHubService
    {
        private static string ApiBaseUrl => "https://api.github.com";
        private static string ApiVersion => "2022-11-28";

        private readonly HttpClient httpClient = CreateHttpClient(gitHubSettings);

        public IEnumerable<GitHubRepository> GetUserRepositories(string username)
        {
            bool isAuthenticatedUserRequested =
                string.IsNullOrWhiteSpace(username) ||
                string.Equals(username, gitHubSettings.Username, StringComparison.OrdinalIgnoreCase);

            string effectiveUsername = username;

            if (isAuthenticatedUserRequested)
            {
                effectiveUsername = gitHubSettings.Username;
            }

            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, effectiveUsername)
            ];

            logger.Info(
                MyOperation.GitHubUserRepositoriesRetrieval,
                OperationStatus.Started,
                logInfos);

            try
            {
                List<GitHubRepository> repositories = [];
                int page = 1;

                while (true)
                {
                    string endpoint = $"{ApiBaseUrl}/users/{Uri.EscapeDataString(username)}/repos?sort=updated&per_page=100&page={page}";

                    if (isAuthenticatedUserRequested)
                    {
                        endpoint = $"{ApiBaseUrl}/user/repos?visibility=all&affiliation=owner,collaborator,organization_member&sort=updated&per_page=100&page={page}";
                    }

                    List<GitHubRepository> pageItems = httpClient
                        .GetStringAsync(endpoint).Result
                        .FromJson<List<GitHubRepository>>();

                    if (pageItems is null || pageItems.Count == 0)
                    {
                        break;
                    }

                    repositories.AddRange(pageItems);
                    page += 1;
                }

                logger.Debug(
                    MyOperation.GitHubUserRepositoriesRetrieval,
                    OperationStatus.Success,
                    logInfos,
                    new LogInfo(MyLogInfoKey.Count, repositories.Count));

                return repositories;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.GitHubUserRepositoriesRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        public GitHubRepository GetRepository(
            string username,
            string repositoryName)
        {
            bool isAuthenticatedUserRequested =
                string.IsNullOrWhiteSpace(username) ||
                string.Equals(username, gitHubSettings.Username, StringComparison.OrdinalIgnoreCase);

            string effectiveUsername = username;

            if (isAuthenticatedUserRequested)
            {
                effectiveUsername = gitHubSettings.Username;
            }

            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, effectiveUsername),
                new(MyLogInfoKey.Repository, repositoryName)
            ];

            logger.Info(
                MyOperation.GitHubRepositoryRetrieval,
                OperationStatus.Started,
                logInfos);

            try
            {
                string encodedOwner = Uri.EscapeDataString(effectiveUsername);
                string encodedRepository = Uri.EscapeDataString(repositoryName);

                string endpoint = $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}";

                GitHubRepository repository = httpClient
                    .GetStringAsync(endpoint).Result
                    .FromJson<GitHubRepository>();

                logger.Debug(
                    MyOperation.GitHubRepositoryRetrieval,
                    OperationStatus.Success,
                    logInfos);

                return repository;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.GitHubRepositoryRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        public string GetRepositoryFile(
            string username,
            string repositoryName,
            string path)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, username),
                new(MyLogInfoKey.Repository, repositoryName),
                new(MyLogInfoKey.Path, path)
            ];

            logger.Info(
                MyOperation.GitHubRepositoryFileContentRetrieval,
                OperationStatus.Started,
                logInfos);

            try
            {
                string encodedOwner = Uri.EscapeDataString(username);
                string encodedRepository = Uri.EscapeDataString(repositoryName);

                string[] pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string encodedPath = string.Join("/", pathParts.Select(Uri.EscapeDataString));

                using HttpRequestMessage request = new(
                    HttpMethod.Get,
                    $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}/contents/{encodedPath}?ref=HEAD");

                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));

                using HttpResponseMessage response = httpClient.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();

                string content = response.Content.ReadAsStringAsync().Result;

                logger.Debug(
                    MyOperation.GitHubRepositoryFileContentRetrieval,
                    OperationStatus.Success,
                    logInfos);

                return content;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.GitHubRepositoryFileContentRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        public IEnumerable<GitHubRelease> GetRepositoryReleases(
            string username,
            string repositoryName)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, username),
                new(MyLogInfoKey.Repository, repositoryName)
            ];

            logger.Info(
                MyOperation.GitHubRepositoryReleasesRetrieval,
                OperationStatus.Started,
                logInfos);

            try
            {
                string encodedOwner = Uri.EscapeDataString(username);
                string encodedRepository = Uri.EscapeDataString(repositoryName);

                List<GitHubRelease> releases = [];
                int page = 1;

                while (true)
                {
                    string endpoint =
                        $"{ApiBaseUrl}/repos/{encodedOwner}/{encodedRepository}/releases?per_page=100&page={page}";

                    List<GitHubRelease> pageItems = httpClient
                        .GetStringAsync(endpoint).Result
                        .FromJson<List<GitHubRelease>>();

                    if (pageItems is null || pageItems.Count == 0)
                    {
                        break;
                    }

                    releases.AddRange(pageItems);
                    page += 1;
                }

                logger.Debug(
                    MyOperation.GitHubRepositoryReleasesRetrieval,
                    OperationStatus.Success,
                    logInfos,
                    new LogInfo(MyLogInfoKey.Count, releases.Count));

                return releases;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.GitHubRepositoryReleasesRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        private static HttpClient CreateHttpClient(GitHubSettings settings)
        {
            HttpClient client = HttpClientCreator.Create();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", ApiVersion);

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            }

            return client;
        }
    }
}