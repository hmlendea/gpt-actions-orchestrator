using System;

using NUnit.Framework;

using GptActionsOrchestrator.Service.Models;

namespace GptActionsOrchestrator.UnitTests.Service.Models
{
    [TestFixture]
    public sealed class GptActionTests
    {
        [TestCase("Unknown")]
        [TestCase("GetGitHubRepository")]
        [TestCase("GetGitHubRepositoryFile")]
        [TestCase("GetGitHubRepositoryReadme")]
        [TestCase("GetGitHubRepositoryReleases")]
        [TestCase("GetGitHubUserRepositories")]
        [TestCase("GetPersonalLogs")]
        [TestCase("GetSteamAppData")]
        public void FromString_WhenCalledWithValidName_ReturnsActionWithMatchingName(string name)
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
        public void FromString_WhenCalledWithValidId_ReturnsActionWithMatchingId(string id)
        {
            GptAction result = GptAction.FromString(id);

            Assert.That(result.Id, Is.EqualTo(id));
        }

        [TestCase("solaire_of_astora")]
        [TestCase("GetUnknownAction")]
        [TestCase("")]
        public void FromString_WhenCalledWithUnrecognisedValue_ReturnsUnknown(string value)
        {
            GptAction result = GptAction.FromString(value);

            Assert.That(result, Is.EqualTo(GptAction.Unknown));
        }

        [Test]
        public void Equals_WhenCalledWithSameInstance_ReturnsTrue()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.Equals(action), Is.True);
        }

        [Test]
        public void Equals_WhenCalledWithEquivalentActionFromFromString_ReturnsTrue()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.FromString("GetGitHubRepository");

            Assert.That(first.Equals(second), Is.True);
        }

        [Test]
        public void Equals_WhenCalledWithDifferentAction_ReturnsFalse()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepositoryFile;

            Assert.That(first.Equals(second), Is.False);
        }

        [Test]
        public void Equals_WhenCalledWithNullGptAction_ReturnsFalse()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.Equals((GptAction)null), Is.False);
        }

        [Test]
        public void Equals_WhenCalledWithNullObject_ReturnsFalse()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.Equals((object)null), Is.False);
        }

        [Test]
        public void Equals_WhenCalledWithObjectOfDifferentType_ReturnsFalse()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.Equals("GetGitHubRepository"), Is.False);
        }

        [Test]
        public void GetHashCode_WhenCalledTwiceOnSameAction_ReturnsSameValue()
        {
            GptAction action = GptAction.GetGitHubRepository;
            int firstHash = action.GetHashCode();
            int secondHash = action.GetHashCode();

            Assert.That(firstHash, Is.EqualTo(secondHash));
        }

        [Test]
        public void GetHashCode_WhenCalledOnDifferentActions_ReturnsDifferentValues()
        {
            int firstHash = GptAction.GetGitHubRepository.GetHashCode();
            int secondHash = GptAction.GetPersonalLogs.GetHashCode();

            Assert.That(firstHash, Is.Not.EqualTo(secondHash));
        }

        [Test]
        public void ToString_WhenCalled_ReturnsName()
        {
            GptAction action = GptAction.GetGitHubRepository;

            Assert.That(action.ToString(), Is.EqualTo("GetGitHubRepository"));
        }

        [Test]
        public void GetValues_WhenCalled_ReturnsAllEightActions()
        {
            Array values = GptAction.GetValues();

            Assert.That(values.Length, Is.EqualTo(8));
        }

        [Test]
        public void ImplicitStringConversion_WhenApplied_ReturnsName()
        {
            GptAction action = GptAction.GetPersonalLogs;
            string actionAsString = action;

            Assert.That(actionAsString, Is.EqualTo("GetPersonalLogs"));
        }

        [Test]
        public void EqualityOperator_WhenComparingSameAction_ReturnsTrue()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepository;

            Assert.That(first == second, Is.True);
        }

        [Test]
        public void EqualityOperator_WhenComparingDifferentActions_ReturnsFalse()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetSteamAppData;

            Assert.That(first == second, Is.False);
        }

        [Test]
        public void InequalityOperator_WhenComparingSameAction_ReturnsFalse()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetGitHubRepository;

            Assert.That(first != second, Is.False);
        }

        [Test]
        public void InequalityOperator_WhenComparingDifferentActions_ReturnsTrue()
        {
            GptAction first = GptAction.GetGitHubRepository;
            GptAction second = GptAction.GetSteamAppData;

            Assert.That(first != second, Is.True);
        }

        [TestCase("Unknown", "unknown")]
        [TestCase("GetGitHubRepository", "github.repository.get")]
        [TestCase("GetGitHubRepositoryFile", "github.repository.file.get")]
        [TestCase("GetGitHubRepositoryReadme", "github.repository.readme.get")]
        [TestCase("GetGitHubRepositoryReleases", "github.repository.releases.get")]
        [TestCase("GetGitHubUserRepositories", "github.user.repositories.get")]
        [TestCase("GetPersonalLogs", "personallogmanager.logs.get")]
        [TestCase("GetSteamAppData", "steam.store.app.get")]
        public void StaticProperties_EachActionHasCorrectId(string name, string expectedId)
        {
            GptAction action = GptAction.FromString(name);

            Assert.That(action.Id, Is.EqualTo(expectedId));
        }
    }
}
