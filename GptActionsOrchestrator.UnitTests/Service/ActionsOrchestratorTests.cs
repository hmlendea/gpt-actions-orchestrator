using System;
using System.Collections.Generic;

using Moq;

using NSubstitute;

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
        IGitHubService gitHubService;
        IPersonalLogManagerService personalLogManagerService;
        ISteamStoreService steamStoreService;
        Mock<IFileRepository<GptActionAliasDataObject>> aliasesRepositoryMock;
        ActionsOrchestrator orchestrator;

        [SetUp]
        public void SetUp()
        {
            gitHubService = Substitute.For<IGitHubService>();
            personalLogManagerService = Substitute.For<IPersonalLogManagerService>();
            steamStoreService = Substitute.For<ISteamStoreService>();
            aliasesRepositoryMock = new Mock<IFileRepository<GptActionAliasDataObject>>();

            orchestrator = new ActionsOrchestrator(
                gitHubService,
                personalLogManagerService,
                steamStoreService,
                aliasesRepositoryMock.Object);

            aliasesRepositoryMock
                .Setup(x => x.ContainsId(It.IsAny<string>()))
                .Returns(false);
        }

        // ── GetGitHubRepository ───────────────────────────────────────────────

        [Test]
        public void GivenGetGitHubRepositoryParameters_WhenGetIsCalled_ThenResponseContainsCorrectActionName()
        {
            gitHubService.GetRepository(Arg.Any<string>(), Arg.Any<string>())
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
            gitHubService.GetRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GitHubRepository());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepository" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubService.Received(1).GetRepository("IlarionPintilie", "test-repo");
        }

        [Test]
        public void GivenGetGitHubRepositoryParameters_WhenGetIsCalled_ThenResponseDataContainsRepository()
        {
            GitHubRepository expectedRepository = new() { Name = "test-repo", Language = "C#" };
            gitHubService.GetRepository("IlarionPintilie", "test-repo")
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
            gitHubService.GetRepositoryFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(string.Empty);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryFile" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" },
                { "path", "src/Program.cs" }
            });

            gitHubService.Received(1).GetRepositoryFile("IlarionPintilie", "test-repo", "src/Program.cs");
        }

        [Test]
        public void GivenGetGitHubRepositoryFileParameters_WhenGetIsCalled_ThenResponseDataContainsFileContent()
        {
            string expectedContent = "using System;";
            gitHubService.GetRepositoryFile("IlarionPintilie", "test-repo", "src/Program.cs")
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
            gitHubService.GetRepositoryFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(string.Empty);

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReadme" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubService.Received(1).GetRepositoryFile("IlarionPintilie", "test-repo", "README.md");
        }

        [Test]
        public void GivenGetGitHubRepositoryReadmeParameters_WhenGetIsCalled_ThenResponseDataContainsReadmeContent()
        {
            string expectedReadme = "# Test Repo";
            gitHubService.GetRepositoryFile("IlarionPintilie", "test-repo", "README.md")
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
            gitHubService.GetRepositoryReleases(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<GitHubRelease>());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubRepositoryReleases" },
                { "username", "IlarionPintilie" },
                { "repository", "test-repo" }
            });

            gitHubService.Received(1).GetRepositoryReleases("IlarionPintilie", "test-repo");
        }

        [Test]
        public void GivenGetGitHubRepositoryReleasesParameters_WhenGetIsCalled_ThenResponseDataContainsReleases()
        {
            List<GitHubRelease> expectedReleases = new() { new() { Name = "v1.0.0", TagName = "v1.0.0" } };
            gitHubService.GetRepositoryReleases("IlarionPintilie", "test-repo")
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
            gitHubService.GetUserRepositories(Arg.Any<string>())
                .Returns(new List<GitHubRepository>());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetGitHubUserRepositories" },
                { "username", "IlarionPintilie" }
            });

            gitHubService.Received(1).GetUserRepositories("IlarionPintilie");
        }

        [Test]
        public void GivenGetGitHubUserRepositoriesParameters_WhenGetIsCalled_ThenResponseDataContainsRepositories()
        {
            List<GitHubRepository> expectedRepositories = new() { new() { Name = "my-repo", Language = "C#" } };
            gitHubService.GetUserRepositories("IlarionPintilie")
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
            personalLogManagerService.GetPersonalLogs(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<string>())
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

            personalLogManagerService.Received(1).GetPersonalLogs(
                "2012-09-05",
                "2012-09-05",
                "daily",
                "ro",
                Arg.Any<Dictionary<string, string>>(),
                "613");
        }

        [Test]
        public void GivenGetPersonalLogsParameters_WhenGetIsCalled_ThenResponseDataContainsPersonalLogs()
        {
            PersonalLogs expectedLogs = new() { Logs = new() { "Log entry 1", "Log entry 2" } };
            personalLogManagerService.GetPersonalLogs(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<string>())
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
            personalLogManagerService.GetPersonalLogs(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<Dictionary<string, string>>(data => capturedData = data),
                Arg.Any<string>())
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
            steamStoreService.GetAppData(Arg.Any<string>())
                .Returns(new SteamAppEntity());

            orchestrator.Get(new Dictionary<string, string>
            {
                { "action", "GetSteamAppData" },
                { "appId", "613" }
            });

            steamStoreService.Received(1).GetAppData("613");
        }

        [Test]
        public void GivenGetSteamAppDataParameters_WhenGetIsCalled_ThenResponseDataContainsSteamAppEntity()
        {
            SteamAppEntity expectedApp = new() { Id = "613", Name = "Solaire's Quest" };
            steamStoreService.GetAppData("613")
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
        {
            Assert.That(
                () => orchestrator.Get(new Dictionary<string, string>
                {
                    { "action", "solaire_of_astora" }
                }),
                Throws.TypeOf<NotImplementedException>());
        }

        [Test]
        public void GivenMissingActionParameter_WhenGetIsCalled_ThenNotImplementedExceptionIsThrown()
        {
            Assert.That(
                () => orchestrator.Get(new Dictionary<string, string>()),
                Throws.TypeOf<NotImplementedException>());
        }

        [Test]
        public void GivenActionSpecifiedById_WhenGetIsCalled_ThenCorrectServiceIsDispatched()
        {
            GitHubRepository expectedRepository = new() { Name = "test-repo" };
            gitHubService.GetRepository(Arg.Any<string>(), Arg.Any<string>())
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
