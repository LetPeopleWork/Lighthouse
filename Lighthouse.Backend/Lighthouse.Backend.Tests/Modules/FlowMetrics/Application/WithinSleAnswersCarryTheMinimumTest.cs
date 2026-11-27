using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    [TestFixture]
    public class WithinSleAnswersCarryTheMinimumTest
    {
        [Test]
        public void TheWidgetAnswer_SaysHowManyClosedWorkItemsAShareNeeds()
        {
            var teamMetrics = new Mock<ITeamMetricsService>();
            teamMetrics
                .Setup(metrics => metrics.GetCycleTimeDataForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .Returns([]);
            var query = new WithinSleInfoQuery(
                teamMetrics.Object,
                Mock.Of<IPortfolioMetricsService>(),
                new FakeLighthouseClock(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero)));

            var answer = query.GetForTeam(new Team { Id = 1, Name = "Team" }, new DateTime(2026, 11, 1), new DateTime(2026, 11, 27));

            Assert.That(answer.MinimumClosed, Is.EqualTo(5));
        }

        [Test]
        public void TheChartAnswer_SaysHowManyClosedWorkItemsADayNeeds()
        {
            var query = new WithinSleOverTimeSeriesQuery(
                Mock.Of<IWithinSleSnapshotRepository>(snapshots =>
                    snapshots.GetSeries(It.IsAny<int>(), It.IsAny<OwnerType>(), It.IsAny<int>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()) == new List<WithinSleSnapshot>()),
                Mock.Of<IRepository<Team>>(),
                Mock.Of<IRepository<Portfolio>>());

            var answer = query.GetSeries(1, OwnerType.Team, 30, null, null);

            Assert.That(answer.MinimumClosed, Is.EqualTo(5));
        }
    }
}
