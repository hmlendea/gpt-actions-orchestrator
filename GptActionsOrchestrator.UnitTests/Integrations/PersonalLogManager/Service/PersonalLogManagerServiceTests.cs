using System;

using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;

using NUnit.Framework;

namespace GptActionsOrchestrator.UnitTests.Integrations.PersonalLogManager.Service
{
    [TestFixture]
    public sealed class PersonalLogManagerServiceTests
    {
        [Test]
        public void GivenNoDateRange_WhenBuildingTheDateRangeRegex_ThenNullIsReturned()
            => Assert.That(
                PersonalLogManagerService.BuildDateRangeRegex(null, null),
                Is.Null);

        [Test]
        public void GivenAPartialDateRange_WhenBuildingTheDateRangeRegex_ThenAnArgumentExceptionIsThrown()
            => Assert.That(
                () => PersonalLogManagerService.BuildDateRangeRegex("2012-09-05", null),
                Throws.TypeOf<ArgumentException>());

        [Test]
        public void GivenAValidDateRange_WhenBuildingTheDateRangeRegex_ThenARegexMatchingTheDatesIsReturned()
            => Assert.That(
                PersonalLogManagerService.BuildDateRangeRegex("2012-09-05", "2012-09-05"),
                Is.EqualTo("(2012-09-05)"));
    }
}