using System;
using System.Collections.Generic;

using Moq;

// Removed NSubstitute usage; replaced with Moq.

using NuciDAL.Repositories;

using NUnit.Framework;

using GptActionsOrchestrator.Api.Responses;
using GptActionsOrchestrator.DataAccess.DataObjects;
using GptActionsOrchestrator.Integrations.GitHub.Service;
using GptActionsOrchestrator.Integrations.GitHub.Service.Models;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;
using GptActionsOrchestrator.Service;

namespace GptActionsOrchestrator.UnitTests.Service
{
    [TestFixture]
    public sealed class ActionsOrchestratorTests
    {
        Mock<IGitHubService> gitHubServiceMock;
        Mock<IPersonalLogManagerService> personalLogManagerServiceMock;
        Mock<ISteamStoreService> steamStoreServiceMock;
        Mock<IFileRepository<GptActionAliasDataObject>> aliasesRepositoryMock;
        ActionsOrchestrator orchestrator;

        [SetUp]
        public void SetUp()
        {
            gitHubServiceMock = new Mock<IGitHubService>();
            personalLogManagerServiceMock = new Mock<IPersonalLogManagerService>();
            steamStoreServiceMock = new Mock<ISteamStoreService>();
            aliasesRepositoryMock = new Mock<IFileRepository<GptActionAliasDataObject>>();

            orchestrator = new ActionsOrchestrator(
                gitHubServiceMock.Object,
                personalLogManagerServiceMock.Object,
                steamStoreServiceMock.Object,
                aliasesRepositoryMock.Object);

            aliasesRepositoryMock
                .Setup(x => x.ContainsId(It.IsAny<string>()))
                .Returns(false);
        }

        // ── GetGitHubRepository ───────────────────────────────────────────────

        [Test]
        public void GivenGetGitHubRepositoryParameters_WhenGetIsCalled_ThenResponseContainsCorrectActionName()
        {
            gitHubServiceMock
                .Setup(x => x.GetRepository(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new GitHubRepository());

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepository" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            Assert.That(response.GptActionName, Is.EqualTo("GetGitHubRepository"));
        }

        [Test]
        public void GivenGetGitHubRepositoryParameters_WhenGetIsCalled_ThenGitHubServiceIsCalledWithCorrectParameters()
        {
            gitHubServiceMock
                .Setup(x => x.GetRepository(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new GitHubRepository());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepository" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubServiceMock.Verify(
                x => x.GetRepository("IlarionPintilie", "test-repo"),
                Times.Once);
        }

        [Test]
        public void GivenGetGitHubRepositoryParameters_WhenGetIsCalled_ThenResponseDataContainsRepository()
        {
            GitHubRepository expectedRepository = new() { Name = "test-repo", Language = "C#" };

            gitHubServiceMock
                .Setup(x => x.GetRepository("IlarionPintilie", "test-repo"))
                .Returns(expectedRepository);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepository" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedRepository));
        }

        // ── GetGitHubRepositoryFile ───────────────────────────────────────────

        [Test]
        public void GivenGetGitHubRepositoryFileParameters_WhenGetIsCalled_ThenGitHubServiceIsCalledWithCorrectParameters()
        {
            gitHubServiceMock
                .Setup(x => x.GetRepositoryFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(string.Empty);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryFile" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" },
                { "path", "src/Program.cs" }
            });

            gitHubServiceMock.Verify(
                x => x.GetRepositoryFile(
                    "IlarionPintilie",
                    "test-repo",
                    "src/Program.cs"),
                Times.Once);
        }

        [Test]
        public void GivenGetGitHubRepositoryFileParameters_WhenGetIsCalled_ThenResponseDataContainsFileContent()
        {
            string expectedContent = "using System;";

            gitHubServiceMock
                .Setup(x => x.GetRepositoryFile(
                    "IlarionPintilie",
                    "test-repo",
                    "src/Program.cs"))
                .Returns(expectedContent);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryFile" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" },
                { "path", "src/Program.cs" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedContent));
        }

        // ── GetGitHubRepositoryReadme ─────────────────────────────────────────

        [Test]
        public void GivenGetGitHubRepositoryReadmeParameters_WhenGetIsCalled_ThenGetRepositoryFileIsCalledWithReadmeMdPath()
        {
            gitHubServiceMock
                .Setup(x => x.GetRepositoryFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(string.Empty);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReadme" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubServiceMock.Verify(
                x => x.GetRepositoryFile(
                    "IlarionPintilie",
                    "test-repo",
                    "README.md"),
                Times.Once  );
        }

        [Test]
        public void GivenGetGitHubRepositoryReadmeParameters_WhenGetIsCalled_ThenResponseDataContainsReadmeContent()
        {
            string expectedReadme = "# Test Repo";

            gitHubServiceMock
                .Setup(x => x.GetRepositoryFile(
                    "IlarionPintilie",
                    "test-repo",
                    "README.md"))
                .Returns(expectedReadme);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReadme" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedReadme));
        }

        // ── GetGitHubRepositoryReleases ───────────────────────────────────────

        [Test]
        public void GivenGetGitHubRepositoryReleasesParameters_WhenGetIsCalled_ThenGitHubServiceIsCalledWithCorrectParameters()
        {
            gitHubServiceMock
                .Setup(x => x.GetRepositoryReleases(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns([]);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReleases" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubServiceMock.Verify(x => x.GetRepositoryReleases("IlarionPintilie", "test-repo"), Times.Once);
        }

        [Test]
        public void GivenGetGitHubRepositoryReleasesParameters_WhenGetIsCalled_ThenResponseDataContainsReleases()
        {
            List<GitHubRelease> expectedReleases = [new() { Name = "v1.0.0", TagName = "v1.0.0" }];
            gitHubServiceMock
                .Setup(service => service.GetRepositoryReleases("IlarionPintilie", "test-repo"))
                .Returns(expectedReleases);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReleases" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedReleases));
        }

        // ── GetGitHubUserRepositories ─────────────────────────────────────────

        [Test]
        public void GivenGetGitHubUserRepositoriesParameters_WhenGetIsCalled_ThenGitHubServiceIsCalledWithCorrectUsername()
        {
            gitHubServiceMock
                .Setup(x => x.GetUserRepositories(It.IsAny<string>()))
                .Returns([]);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubUserRepositories" },
                { "username", "IlarionPintilie" }
            });

            gitHubServiceMock.Verify(
                x => x.GetUserRepositories("IlarionPintilie"),
                Times.Once);
        }

        [Test]
        public void GivenGetGitHubUserRepositoriesParameters_WhenGetIsCalled_ThenResponseDataContainsRepositories()
        {
            List<GitHubRepository> expectedRepositories =
            [
                new() { Name = "my-repo", Language = "C#" }
            ];

            gitHubServiceMock
                .Setup(x => x.GetUserRepositories("IlarionPintilie"))
                .Returns(expectedRepositories);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubUserRepositories" },
                { "username", "IlarionPintilie" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedRepositories));
        }

        // ── GetPersonalLogs ───────────────────────────────────────────────────

        [Test]
        public void GivenGetPersonalLogsParameters_WhenGetIsCalled_ThenPersonalLogManagerServiceIsCalledWithCorrectParameters()
        {
            personalLogManagerServiceMock
                .Setup(x => x.GetPersonalLogs(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>()))
                .Returns(new PersonalLogs());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetPersonalLogs" },
                { "date_beginning", "2012-09-05" },
                { "date_end", "2012-09-05" },
                { "template", "daily" },
                { "localisation", "ro" },
                { "count", "613" }
            });

            personalLogManagerServiceMock.Verify(
                x => x.GetPersonalLogs(
                    "2012-09-05",
                    "2012-09-05",
                    "daily",
                    "ro",
                    It.IsAny<Dictionary<string, string>>(),
                    "613"),
                Times.Once);
        }

        [Test]
        public void GivenGetPersonalLogsParameters_WhenGetIsCalled_ThenResponseDataContainsPersonalLogs()
        {
            PersonalLogs expectedLogs = new()
            {
                Logs = ["Log entry 1", "Log entry 2"]
            };

            personalLogManagerServiceMock
                .Setup(x => x.GetPersonalLogs(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>()))
                .Returns(expectedLogs);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetPersonalLogs" },
                { "date_beginning", "2012-09-05" },
                { "date_end", "2012-09-05" },
                { "template", "daily" },
                { "localisation", "ro" },
                { "count", "613" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedLogs));
        }

        [Test]
        public void GivenGetPersonalLogsWithNestedDataParameters_WhenGetIsCalled_ThenDataDictionaryIsPassedToService()
        {
            Dictionary<string, string> capturedData = null;
            personalLogManagerServiceMock
                .Setup(service => service.GetPersonalLogs(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>()))
                .Callback((
                    string dateBeginning,
                    string dateEnd,
                    string template,
                    string localisation,
                    Dictionary<string, string> data,
                    string count) => capturedData = data)
                .Returns(new PersonalLogs());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetPersonalLogs" },
                { "date_beginning", "2012-09-05" },
                { "date_end", "2012-09-05" },
                { "template", "daily" },
                { "localisation", "ro" },
                { "count", "613" },
                { "data.mood", "happy" },
                { "data.energy", "high" }
            });

            Assert.That(capturedData, Is.Not.Null);
            Assert.That(capturedData["mood"], Is.EqualTo("happy"));
            Assert.That(capturedData["energy"], Is.EqualTo("high"));
        }

        // ── GetSteamAppData ───────────────────────────────────────────────────

        [Test]
        public void GivenGetSteamAppDataParameters_WhenGetIsCalled_ThenSteamStoreServiceIsCalledWithCorrectAppId()
        {
            steamStoreServiceMock
                .Setup(x => x.GetAppData(It.IsAny<string>()))
                .Returns(new SteamAppEntity());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetSteamAppData" },
                { "appId", "613" }
            });

            steamStoreServiceMock.Verify(x => x.GetAppData("613"), Times.Once);
        }

        [Test]
        public void GivenGetSteamAppDataParameters_WhenGetIsCalled_ThenResponseDataContainsSteamAppEntity()
        {
            SteamAppEntity expectedApp = new() { Id = "613", Name = "Solaire's Quest" };

            steamStoreServiceMock
                .Setup(x => x.GetAppData("613"))
                .Returns(expectedApp);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetSteamAppData" },
                { "appId", "613" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedApp));
        }

        // ── Unknown / missing action ──────────────────────────────────────────

        [Test]
        public void GivenUnknownActionName_WhenGetIsCalled_ThenNotImplementedExceptionIsThrown()
            => Assert.That(
                () => orchestrator.Get(new Dictionary<string, string>
                {
                    { "action", "solaire_of_astora" }
                }),
                Throws.TypeOf<NotImplementedException>());

        [Test]
        public void GivenMissingActionParameter_WhenGetIsCalled_ThenNotImplementedExceptionIsThrown()
            => Assert.That(
                () => orchestrator.Get([]),
                Throws.TypeOf<NotImplementedException>());

        [Test]
        public void GivenActionSpecifiedById_WhenGetIsCalled_ThenCorrectServiceIsDispatched()
        {
            GitHubRepository expectedRepository = new() { Name = "test-repo" };

            gitHubServiceMock
                .Setup(x => x.GetRepository(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(expectedRepository);

            GetActionResponse response = orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "github.repository.get" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            Assert.That(response.Data, Is.EqualTo(expectedRepository));
        }
    }
}
