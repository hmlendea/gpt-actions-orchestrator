using System;
using System.Collections.Generic;

using NSubstitute;

using NUnit.Framework;

using GptActionsOrchestrator.Api.Responses;
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
        private IGitHubService gitHubService;
        private IPersonalLogManagerService personalLogManagerService;
        private ISteamStoreService steamStoreService;
        private ActionsOrchestrator orchestrator;

        [SetUp]
        public void SetUp()
        {
            gitHubService = Substitute.For<IGitHubService>();
            personalLogManagerService = Substitute.For<IPersonalLogManagerService>();
            steamStoreService = Substitute.For<ISteamStoreService>();
            orchestrator = new ActionsOrchestrator(gitHubService, personalLogManagerService, steamStoreService);
        }

        [Test]
        public void Get_WhenActionIsGetGitHubRepository_ReturnsResponseWithCorrectActionName()
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
        public void Get_WhenActionIsGetGitHubRepository_CallsGitHubServiceWithCorrectParameters()
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
        public void Get_WhenActionIsGetGitHubRepository_ReturnsRepositoryDataInResponse()
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

        [Test]
        public void Get_WhenActionIsGetGitHubRepositoryFile_CallsGitHubServiceWithCorrectParameters()
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
        public void Get_WhenActionIsGetGitHubRepositoryFile_ReturnsFileContentInResponse()
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

        [Test]
        public void Get_WhenActionIsGetGitHubRepositoryReadme_CallsGetRepositoryFileWithReadmeMdPath()
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
        public void Get_WhenActionIsGetGitHubRepositoryReadme_ReturnsReadmeContentInResponse()
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

        [Test]
        public void Get_WhenActionIsGetGitHubRepositoryReleases_CallsGitHubServiceWithCorrectParameters()
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
        public void Get_WhenActionIsGetGitHubRepositoryReleases_ReturnsReleasesInResponse()
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

        [Test]
        public void Get_WhenActionIsGetGitHubUserRepositories_CallsGitHubServiceWithCorrectUsername()
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
        public void Get_WhenActionIsGetGitHubUserRepositories_ReturnsRepositoriesInResponse()
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

        [Test]
        public void Get_WhenActionIsGetPersonalLogs_CallsPersonalLogManagerServiceWithCorrectParameters()
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
        public void Get_WhenActionIsGetPersonalLogs_ReturnsPersonalLogsInResponse()
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
        public void Get_WhenActionIsGetPersonalLogsWithNestedDataParameters_PassesDataDictionaryToService()
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

        [Test]
        public void Get_WhenActionIsGetSteamAppData_CallsSteamStoreServiceWithCorrectAppId()
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
        public void Get_WhenActionIsGetSteamAppData_ReturnsSteamAppEntityInResponse()
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

        [Test]
        public void Get_WhenActionIsUnknown_ThrowsNotImplementedException()
        {
            Assert.That(
                () => orchestrator.Get(new Dictionary<string, string>
                {
                    { "action", "solaire_of_astora" }
                }),
                Throws.TypeOf<NotImplementedException>());
        }

        [Test]
        public void Get_WhenActionParameterIsAbsent_ThrowsNotImplementedException()
        {
            Assert.That(
                () => orchestrator.Get(new Dictionary<string, string>()),
                Throws.TypeOf<NotImplementedException>());
        }

        [Test]
        public void Get_WhenActionIsSpecifiedByActionId_CorrectlyDispatchesToService()
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
