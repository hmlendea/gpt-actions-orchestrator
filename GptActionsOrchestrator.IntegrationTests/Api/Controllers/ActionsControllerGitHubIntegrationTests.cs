using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using GptActionsOrchestrator.IntegrationTests.Infrastructure;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;

namespace GptActionsOrchestrator.IntegrationTests.Api.Controllers
{
    [TestFixture]
    public sealed class ActionsControllerGitHubIntegrationTests
    {
        private static string ActionParameterName => "action";

        private static string PathParameterName => "path";

        private static string ReadmePath => "README.md";

        private static string RepositoryParameterName => "repository";

        private static string UsernameParameterName => "username";

        private static IEnumerable<string> RepositoryActionValues =>
        [
            "GetGitHubRepository",
            "github.repository.get",
            "github.repo.get",
            "github.user.repo.get",
            "github.user.repository.get"
        ];

        private static IEnumerable<string> RepositoryFileActionValues =>
        [
            "GetGitHubRepositoryFile",
            "github.repository.file.get",
            "github.file.get",
            "github.repo.file.get",
            "github.user.repo.file.get",
            "github.user.repository.file.get"
        ];

        private static IEnumerable<string> RepositoryReadmeActionValues =>
        [
            "GetGitHubRepositoryReadme",
            "github.repository.readme.get",
            "github.file.readme.get",
            "github.readme.get",
            "github.repo.file.readme.get",
            "github.repo.readme.get",
            "github.repository.file.readme.get",
            "github.user.repo.file.readme.get",
            "github.user.repo.readme.get",
            "github.user.repository.file.readme.get",
            "github.user.repository.readme.get"
        ];

        private static IEnumerable<string> RepositoryReleasesActionValues =>
        [
            "GetGitHubRepositoryReleases",
            "github.repository.releases.get",
            "github.releases.get",
            "github.repo.releases.get",
            "github.user.repo.releases.get",
            "github.user.repository.releases.get"
        ];

        private static IEnumerable<string> UserRepositoriesActionValues =>
        [
            "GetGitHubUserRepositories",
            "github.user.repositories.get",
            "github.repos.get",
            "github.repositories.get",
            "github.user.repos.get"
        ];

        private ActionsWebApplicationFactory applicationFactory = null!;
        private HttpClient httpClient = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            applicationFactory = new ActionsWebApplicationFactory();
            httpClient = ActionsHttpClientFactory.CreateAuthorised(applicationFactory);
        }

        [SetUp]
        public void SetUp()
            => applicationFactory.GitHubServiceMock.Reset();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient.Dispose();
            applicationFactory.Dispose();
        }

        [TestCaseSource(nameof(GetRepositoryRequestCases))]
        public async Task GivenARepositoryActionWithDiverseParameters_WhenRequestingTheAction_ThenTheRepositoryIsReturned(
            string actionValue,
            string? username,
            string? repositoryName)
        {
            GitHubRepository expectedRepository = new()
            {
                Name = "Narivia",
                Description = "Praise the Sun!",
                Language = "C#",
                StargazersCount = 613,
                Topics = ["Dark Souls III", "Jolly cooperation!"],
                IsArchived = false,
                IsPrivate = true,
                IsFork = false
            };
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepository(username!, repositoryName!))
                .Returns(expectedRepository);
            Dictionary<string, string?> parameters = BuildRepositoryParameters(
                actionValue,
                username,
                repositoryName);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);
            JsonElement responseRoot = responseDocument.RootElement;
            JsonElement responseData = responseRoot.GetProperty("data");

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseRoot.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubRepository"));
                Assert.That(responseData.GetProperty("name").GetString(), Is.EqualTo("Narivia"));
                Assert.That(responseData.GetProperty("description").GetString(), Is.EqualTo("Praise the Sun!"));
                Assert.That(responseData.GetProperty("language").GetString(), Is.EqualTo("C#"));
                Assert.That(responseData.GetProperty("stargazers_count").GetInt32(), Is.EqualTo(613));
                Assert.That(responseData.GetProperty("topics").GetArrayLength(), Is.EqualTo(2));
                Assert.That(responseData.GetProperty("archived").GetBoolean(), Is.False);
                Assert.That(responseData.GetProperty("private").GetBoolean());
                Assert.That(responseData.GetProperty("fork").GetBoolean(), Is.False);
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepository(username!, repositoryName!),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetRepositoryFileRequestCases))]
        public async Task GivenARepositoryFileActionWithDiverseParameters_WhenRequestingTheAction_ThenTheFileIsReturned(
            string actionValue,
            string? username,
            string? repositoryName,
            string? path)
        {
            string expectedContent = "What a beautiful day to stay inside and write tests";
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepositoryFile(username!, repositoryName!, path!))
                .Returns(expectedContent);
            Dictionary<string, string?> parameters = BuildRepositoryFileParameters(
                actionValue,
                username,
                repositoryName,
                path);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubRepositoryFile"));
                Assert.That(
                    responseDocument.RootElement.GetProperty("data").GetString(),
                    Is.EqualTo(expectedContent));
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepositoryFile(username!, repositoryName!, path!),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetRepositoryReadmeRequestCases))]
        public async Task GivenARepositoryReadmeActionWithDiverseParameters_WhenRequestingTheAction_ThenTheReadmeIsReturned(
            string actionValue,
            string? username,
            string? repositoryName)
        {
            string expectedContent = "# Solaire of Astora";
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepositoryFile(username!, repositoryName!, ReadmePath))
                .Returns(expectedContent);
            Dictionary<string, string?> parameters = BuildRepositoryParameters(
                actionValue,
                username,
                repositoryName);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubRepositoryReadme"));
                Assert.That(
                    responseDocument.RootElement.GetProperty("data").GetString(),
                    Is.EqualTo(expectedContent));
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepositoryFile(username!, repositoryName!, ReadmePath),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetRepositoryReleasesRequestCases))]
        public async Task GivenARepositoryReleasesActionWithDiverseParameters_WhenRequestingTheAction_ThenTheReleasesAreReturned(
            string actionValue,
            string? username,
            string? repositoryName)
        {
            IEnumerable<GitHubRelease> expectedReleases =
            [
                new GitHubRelease
                {
                    TagName = "v6.13.0",
                    Name = "Praise the Sun!",
                    Body = "Jolly cooperation!",
                    IsDraft = false,
                    IsPrerelease = false
                },
                new GitHubRelease
                {
                    TagName = "v8.73.0",
                    Name = "The cake is a lie",
                    Body = "This was a triumph",
                    IsDraft = true,
                    IsPrerelease = true
                }
            ];
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetRepositoryReleases(username!, repositoryName!))
                .Returns(expectedReleases);
            Dictionary<string, string?> parameters = BuildRepositoryParameters(
                actionValue,
                username,
                repositoryName);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);
            JsonElement releasesData = responseDocument.RootElement.GetProperty("data");

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubRepositoryReleases"));
                Assert.That(releasesData.GetArrayLength(), Is.EqualTo(2));
                Assert.That(releasesData[0].GetProperty("tag_name").GetString(), Is.EqualTo("v6.13.0"));
                Assert.That(releasesData[0].GetProperty("draft").GetBoolean(), Is.False);
                Assert.That(releasesData[1].GetProperty("tag_name").GetString(), Is.EqualTo("v8.73.0"));
                Assert.That(releasesData[1].GetProperty("prerelease").GetBoolean());
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetRepositoryReleases(username!, repositoryName!),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetUserRepositoriesRequestCases))]
        public async Task GivenAUserRepositoriesActionWithDiverseParameters_WhenRequestingTheAction_ThenTheRepositoriesAreReturned(
            string actionValue,
            string? username)
        {
            IEnumerable<GitHubRepository> expectedRepositories =
            [
                new GitHubRepository
                {
                    Name = "SokoGrump",
                    Description = "All roads lead to Rome",
                    Language = "C#"
                },
                new GitHubRepository
                {
                    Name = "Terraria",
                    Description = "There be dragons",
                    Language = "C++"
                }
            ];
            applicationFactory.GitHubServiceMock
                .Setup(service => service.GetUserRepositories(username!))
                .Returns(expectedRepositories);
            Dictionary<string, string?> parameters = BuildUserRepositoriesParameters(
                actionValue,
                username);

            using HttpResponseMessage response = await httpClient.GetAsync(
                ActionRequestPathBuilder.Build(parameters));
            using JsonDocument responseDocument = await ActionResponseReader.ReadSuccessAsync(response);
            JsonElement repositoriesData = responseDocument.RootElement.GetProperty("data");

            Assert.Multiple(() =>
            {
                Assert.That(
                    responseDocument.RootElement.GetProperty("action").GetString(),
                    Is.EqualTo("GetGitHubUserRepositories"));
                Assert.That(repositoriesData.GetArrayLength(), Is.EqualTo(2));
                Assert.That(repositoriesData[0].GetProperty("name").GetString(), Is.EqualTo("SokoGrump"));
                Assert.That(repositoriesData[1].GetProperty("name").GetString(), Is.EqualTo("Terraria"));
            });
            applicationFactory.GitHubServiceMock.Verify(
                service => service.GetUserRepositories(username!),
                Times.Once);
            applicationFactory.GitHubServiceMock.VerifyNoOtherCalls();
        }

        private static IEnumerable<TestCaseData> GetRepositoryRequestCases()
        {
            foreach (string actionValue in RepositoryActionValues)
            {
                yield return new TestCaseData(actionValue, "Angetenar", "Narivia");
                yield return new TestCaseData(actionValue, "DummyUser", "Among Us");
                yield return new TestCaseData(actionValue, "IlarionPintilie", "Dark Souls III");
                yield return new TestCaseData(actionValue, "solaire_of_astora", "RuneScape");
                yield return new TestCaseData(actionValue, "zezima", "eRepublik");
                yield return new TestCaseData(actionValue, "Ionuț Karr", "Çupișan");
                yield return new TestCaseData(actionValue, string.Empty, string.Empty);
                yield return new TestCaseData(actionValue, "   ", "  ");
                yield return new TestCaseData(actionValue, "A&B", "SokoGrump?variant=613");
                yield return new TestCaseData(actionValue, null, null);
            }
        }

        private static IEnumerable<TestCaseData> GetRepositoryFileRequestCases()
        {
            foreach (string actionValue in RepositoryFileActionValues)
            {
                yield return new TestCaseData(actionValue, "Angetenar", "Narivia", "README.md");
                yield return new TestCaseData(actionValue, "DummyUser", "Among Us", "Program.cs");
                yield return new TestCaseData(actionValue, "IlarionPintilie", "Dark Souls III", "Data/2012-09-05.json");
                yield return new TestCaseData(actionValue, "solaire_of_astora", "RuneScape", "Praise the Sun!.md");
                yield return new TestCaseData(actionValue, "Ionuț Karr", "Çupișan", "Strada Nucilor/Crăciun Fericit!.txt");
                yield return new TestCaseData(actionValue, "A&B", "SokoGrump?variant=613", "A+B/C&D?.txt");
                yield return new TestCaseData(actionValue, string.Empty, string.Empty, string.Empty);
                yield return new TestCaseData(actionValue, "   ", "  ", " ");
                yield return new TestCaseData(actionValue, null, null, null);
            }
        }

        private static IEnumerable<TestCaseData> GetRepositoryReadmeRequestCases()
        {
            foreach (string actionValue in RepositoryReadmeActionValues)
            {
                yield return new TestCaseData(actionValue, "Angetenar", "Narivia");
                yield return new TestCaseData(actionValue, "solaire_of_astora", "Dark Souls III");
                yield return new TestCaseData(actionValue, "Ionuț Karr", "Çupișan");
                yield return new TestCaseData(actionValue, "A&B", "SokoGrump?variant=613");
                yield return new TestCaseData(actionValue, string.Empty, string.Empty);
                yield return new TestCaseData(actionValue, null, null);
            }
        }

        private static IEnumerable<TestCaseData> GetRepositoryReleasesRequestCases()
        {
            foreach (string actionValue in RepositoryReleasesActionValues)
            {
                yield return new TestCaseData(actionValue, "Angetenar", "Narivia");
                yield return new TestCaseData(actionValue, "DummyUser", "Among Us");
                yield return new TestCaseData(actionValue, "solaire_of_astora", "Dark Souls III");
                yield return new TestCaseData(actionValue, "Ionuț Karr", "Çupișan");
                yield return new TestCaseData(actionValue, string.Empty, string.Empty);
                yield return new TestCaseData(actionValue, null, null);
            }
        }

        private static IEnumerable<TestCaseData> GetUserRepositoriesRequestCases()
        {
            foreach (string actionValue in UserRepositoriesActionValues)
            {
                yield return new TestCaseData(actionValue, "Angetenar");
                yield return new TestCaseData(actionValue, "DummyUser");
                yield return new TestCaseData(actionValue, "IlarionPintilie");
                yield return new TestCaseData(actionValue, "solaire_of_astora");
                yield return new TestCaseData(actionValue, "zezima");
                yield return new TestCaseData(actionValue, "Ionuț Karr");
                yield return new TestCaseData(actionValue, "A&B?variant=613");
                yield return new TestCaseData(actionValue, string.Empty);
                yield return new TestCaseData(actionValue, "   ");
                yield return new TestCaseData(actionValue, null);
            }
        }

        private static Dictionary<string, string?> BuildRepositoryParameters(
            string actionValue,
            string? username,
            string? repositoryName)
        {
            Dictionary<string, string?> parameters = new()
            {
                [ActionParameterName] = actionValue
            };

            AddParameter(parameters, UsernameParameterName, username);
            AddParameter(parameters, RepositoryParameterName, repositoryName);

            return parameters;
        }

        private static Dictionary<string, string?> BuildRepositoryFileParameters(
            string actionValue,
            string? username,
            string? repositoryName,
            string? path)
        {
            Dictionary<string, string?> parameters = BuildRepositoryParameters(
                actionValue,
                username,
                repositoryName);
            AddParameter(parameters, PathParameterName, path);

            return parameters;
        }

        private static Dictionary<string, string?> BuildUserRepositoriesParameters(
            string actionValue,
            string? username)
        {
            Dictionary<string, string?> parameters = new()
            {
                [ActionParameterName] = actionValue
            };
            AddParameter(parameters, UsernameParameterName, username);

            return parameters;
        }

        private static void AddParameter(
            IDictionary<string, string?> parameters,
            string parameterName,
            string? parameterValue)
        {
            if (parameterValue is not null)
            {
                parameters[parameterName] = parameterValue;
            }
        }
    }
}