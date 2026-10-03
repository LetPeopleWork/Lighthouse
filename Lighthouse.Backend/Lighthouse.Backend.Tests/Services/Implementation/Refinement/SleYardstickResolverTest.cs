using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class SleYardstickResolverTest
    {
        private static readonly DateOnly Today = new(2026, 10, 3);

        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private SleYardstickResolver subject;

        [SetUp]
        public void SetUp()
        {
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            var clockMock = new Mock<ILighthouseClock>();
            clockMock.Setup(clock => clock.Today).Returns(Today);

            subject = new SleYardstickResolver(teamMetricsServiceMock.Object, clockMock.Object);
        }

        [TestCase(85, 7)]
        [TestCase(1, 1)]
        [TestCase(50, 30)]
        [TestCase(99, 365)]
        public void ATeamWithAnSleAnswersItsOwnNumbers(int probability, int days)
        {
            var team = new Team { ServiceLevelExpectationProbability = probability, ServiceLevelExpectationRange = days };

            var yardstick = subject.For(team);

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.Sle, days, probability)));
        }

        [TestCase(0, 0)]
        [TestCase(85, 0)]
        [TestCase(0, 7)]
        public void ATeamWithoutAWholeSleFallsBackToThe85thPercentileOfItsCycleTime(int probability, int days)
        {
            var team = new Team { ServiceLevelExpectationProbability = probability, ServiceLevelExpectationRange = days };
            GivenTheCycleTimePercentiles(new PercentileValue(50, 5), new PercentileValue(70, 8), new PercentileValue(85, 12), new PercentileValue(95, 20));

            var yardstick = subject.For(team);

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.CycleTimeFallback, 12, 85)));
        }

        [Test]
        public void NoPercentilesAtAllLeaveTheYardstickWithoutANumber()
        {
            GivenTheCycleTimePercentiles();

            var yardstick = subject.For(new Team());

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.Unavailable, null, null)));
        }

        [Test]
        public void PercentilesWithoutAn85thEntryLeaveTheYardstickWithoutANumber()
        {
            GivenTheCycleTimePercentiles(new PercentileValue(50, 5), new PercentileValue(70, 8), new PercentileValue(95, 20));

            var yardstick = subject.For(new Team());

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.Unavailable, null, null)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void AnEmpty85thPercentileMeansNothingFinishedAndLeavesTheYardstickWithoutANumber(int days)
        {
            GivenTheCycleTimePercentiles(new PercentileValue(50, days), new PercentileValue(70, days), new PercentileValue(85, days), new PercentileValue(95, days));

            var yardstick = subject.For(new Team());

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.Unavailable, null, null)));
        }

        [Test]
        public void TheSmallestRealCycleTimeIsStillAFallback()
        {
            GivenTheCycleTimePercentiles(new PercentileValue(85, 1));

            var yardstick = subject.For(new Team());

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.CycleTimeFallback, 1, 85)));
        }

        [Test]
        public void TheFallbackSamplesTheTeamsRollingThroughputWindow()
        {
            var team = new Team { ThroughputHistory = 30 };
            var window = team.GetThroughputSettings(Today);
            GivenTheCycleTimePercentiles(new PercentileValue(85, 12));

            subject.For(team);

            teamMetricsServiceMock.Verify(service => service.GetCycleTimePercentilesForTeam(team, window.StartDate, window.EndDate), Times.Once);
            Assert.That(window.StartDate, Is.EqualTo(new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void TheFallbackSamplesTheTeamsFixedThroughputDates()
        {
            var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
            var team = new Team { UseFixedDatesForThroughput = true, ThroughputHistoryStartDate = start, ThroughputHistoryEndDate = end };
            GivenTheCycleTimePercentiles(new PercentileValue(85, 12));

            subject.For(team);

            teamMetricsServiceMock.Verify(service => service.GetCycleTimePercentilesForTeam(team, start, end), Times.Once);
        }

        [Test]
        public void TheFallbackUsesTheTeamsDefaultCycleTimeAndNeverANamedCycleTimeDefinition()
        {
            var team = new Team();
            GivenTheCycleTimePercentiles(new PercentileValue(85, 12));

            subject.For(team);

            teamMetricsServiceMock.Verify(
                service => service.GetNamedCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public void ATeamWithAnSleDoesNotAskForItsCycleTime()
        {
            var team = new Team { ServiceLevelExpectationProbability = 85, ServiceLevelExpectationRange = 7 };

            subject.For(team);

            teamMetricsServiceMock.Verify(
                service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        private void GivenTheCycleTimePercentiles(params PercentileValue[] percentiles)
        {
            teamMetricsServiceMock
                .Setup(service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(percentiles);
        }
    }
}
