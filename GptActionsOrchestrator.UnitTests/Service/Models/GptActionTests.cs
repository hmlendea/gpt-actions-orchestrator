using System;

using NUnit.Framework;

using GptActionsOrchestrator.Service.Models;

namespace GptActionsOrchestrator.UnitTests.Service.Models
{
    [TestFixture]
    public sealed class GptActionTests
    {
        // ── FromString ────────────────────────────────────────────────────────

        [TestCase("Unknown")]
        [TestCase("GetGitHubRepository")]
        [TestCase("GetGitHubRepositoryFile")]
        [TestCase("GetGitHubRepositoryReadme")]
        [TestCase("GetGitHubRepositoryReleases")]
        [TestCase("GetGitHubUserRepositories")]
        [TestCase("GetPersonalLogs")]
        [TestCase("GetSteamAppData")]
        public void GivenValidActionName_WhenFromStringIsCalled_ThenMatchingActionIsReturned(string name)
        {
            GptAction result = GptAction.FromString(name);

            Assert.That(result.Name, Is.EqualTo(name));
        }

        [TestCase("unknown")]
        [TestCase("github.repository.get")]
        [TestCase("github.repository.file.get")]
        [TestCase("github.repository.readme.get")]
        [TestCase("github.repository.releases.get")]
        [TestCase("github.user.repositories.get")]
        [TestCase("personallogmanager.logs.get")]
        [TestCase("steam.store.app.get")]
        public void GivenValidActionId_WhenFromStringIsCalled_ThenMatchingActionIsReturned(string id)
        {
            GptAction result = GptAction.FromString(id);

            Assert.That(result.Id, Is.EqualTo(id));
        }

        [TestCase("solaire_of_astora")]
        [TestCase("GetUnknownAction")]
        [TestCase("")]
        public void GivenUnrecognisedValue_WhenFromStringIsCalled_ThenUnknownActionIsReturned(string value)
        {
            GptAction result = GptAction.FromString(value);

            Assert.That(result, Is.EqualTo(GptAction.Unknown));
        }

        [TestCase("Unknown", "unknown")]
        [TestCase("GetGitHubRepository", "github.repository.get")]
        [TestCase("GetGitHubRepositoryFile", "github.repository.file.get")]
        [TestCase("GetGitHubRepositoryReadme", "github.repository.readme.get")]
        [TestCase("GetGitHubRepositoryReleases", "github.repository.releases.get")]
        [TestCase("GetGitHubUserRepositories", "github.user.repositories.get")]
        [TestCase("GetPersonalLogs", "personallogmanager.logs.get")]
        [TestCase("GetSteamAppData", "steam.store.app.get")]
        public void GivenActionName_WhenFromStringIsCalled_ThenCorrectIdIsReturned(string name, string expectedId)
        {
            GptAction action = GptAction.FromString(name);

            Assert.That(action.Id, Is.EqualTo(expectedId));
        }

        // ── GetValues ─────────────────────────────────────────────────────────

        [Test]
        public void GivenAllRegisteredActions_WhenGetValuesIsCalled_ThenEightActionsAreReturned()
        {
            Array values = GptAction.GetValues();

            Assert.That(values, Has.Length.EqualTo(8));
        }

        // ── Equals ────────────────────────────────────────────────────────────

        [Test]
        public void GivenSameInstance_WhenEqualsIsCalled_ThenTrueIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;

            bool isEqual = action.Equals(action);

            Assert.That(isEqual);
        }

        [Test]
        public void GivenEquivalentAction_WhenEqualsIsCalled_ThenTrueIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.FromString("GetGitHubRepository");

            bool isEqual = first.Equals(second);

            Assert.That(isEqual);
        }

        [Test]
        public void GivenDifferentAction_WhenEqualsIsCalled_ThenFalseIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepositoryFile;

            bool isEqual = first.Equals(second);

            Assert.That(isEqual, Is.False);
        }

        [Test]
        public void GivenNullGptAction_WhenEqualsIsCalled_ThenFalseIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;

            bool isEqual = action.Equals((GptAction)null);

            Assert.That(isEqual, Is.False);
        }

        [Test]
        public void GivenNullObject_WhenEqualsIsCalled_ThenFalseIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;

            bool isEqual = action.Equals((object)null);

            Assert.That(isEqual, Is.False);
        }

        [Test]
        public void GivenObjectOfDifferentType_WhenEqualsIsCalled_ThenFalseIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;

            bool isEqual = action.Equals("GetGitHubRepository");

            Assert.That(isEqual, Is.False);
        }

        // ── GetHashCode ───────────────────────────────────────────────────────

        [Test]
        public void GivenSameAction_WhenGetHashCodeIsCalledTwice_ThenSameValueIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;
            int firstHash = action.GetHashCode();
            int secondHash = action.GetHashCode();

            Assert.That(firstHash, Is.EqualTo(secondHash));
        }

        [Test]
        public void GivenDifferentActions_WhenGetHashCodeIsCalled_ThenDifferentValuesAreReturned()
        {
            int firstHash = GptAction.GetGitHubRepository.GetHashCode();
            int secondHash = GptAction.GetPersonalLogs.GetHashCode();

            Assert.That(firstHash, Is.Not.EqualTo(secondHash));
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Test]
        public void GivenAnyAction_WhenToStringIsCalled_ThenNameIsReturned()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.ToString(), Is.EqualTo("GetGitHubRepository"));
        }

        // ── Implicit string conversion ─────────────────────────────────────────

        [Test]
        public void GivenAnyAction_WhenImplicitlyConvertedToString_ThenNameIsReturned()
        {
            GptAction action = GptAction.GetPersonalLogs;
            string actionAsString = action;

            Assert.That(actionAsString, Is.EqualTo("GetPersonalLogs"));
        }

        // ── == operator ───────────────────────────────────────────────────────

        [Test]
        public void GivenSameAction_WhenEqualityOperatorIsApplied_ThenTrueIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepository;

            Assert.That(first == second);
        }

        [Test]
        public void GivenDifferentActions_WhenEqualityOperatorIsApplied_ThenFalseIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetSteamAppData;

            Assert.That(first == second, Is.False);
        }

        // ── != operator ───────────────────────────────────────────────────────

        [Test]
        public void GivenSameAction_WhenInequalityOperatorIsApplied_ThenFalseIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepository;

            Assert.That(first != second, Is.False);
        }

        [Test]
        public void GivenDifferentActions_WhenInequalityOperatorIsApplied_ThenTrueIsReturned()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetSteamAppData;

            Assert.That(first != second);
        }
    }
}
