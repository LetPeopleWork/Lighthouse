using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Tests.Modules.Reports.Application
{
    public class TeamReportMetricSeriesTest
    {
        private const int SharedId = 7;

        private static readonly ReportWindow Window = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly DateTime FirstDay = new(2026, 7, 5, 0, 0, 0, DateTimeKind.Unspecified);

        private static readonly DateTime LastDay = new(2026, 10, 2, 0, 0, 0, DateTimeKind.Unspecified);

        private static readonly PercentileValue[] Percentiles = [new(50, 6), new(70, 11), new(85, 21), new(95, 30)];

        private Mock<IRepository<Team>> teams = null!;
        private TeamReportMetricSeries subject = null!;

        [SetUp]
        public void SetUp()
        {
            var team = new Team { Name = "Team Lightspeed", Id = SharedId };
            teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.GetById(SharedId)).Returns(team);

            var metrics = new Mock<ITeamMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForTeam(team, FirstDay, LastDay)).Returns(Percentiles);
            metrics.Setup(service => service.GetClosedItemsForTeam(team, FirstDay, LastDay)).Returns([]);

            subject = new TeamReportMetricSeries(teams.Object, metrics.Object, new FakeLighthouseClock(DateTimeOffset.UtcNow));
        }

        [Test]
        public void A_Team_reads_its_own_series()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Team(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(6, 11, 21, 30, 0)));
        }

        [Test]
        public void Only_the_closed_items_with_a_Cycle_Time_count_as_finished()
        {
            var team = new Team { Name = "Team Lightspeed", Id = SharedId };
            var finished = new WorkItem { StateCategory = StateCategories.Done, StartedDate = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc), ClosedDate = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc) };
            var stillOpen = new WorkItem { StateCategory = StateCategories.Doing, StartedDate = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc) };
            var metrics = new Mock<ITeamMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForTeam(team, FirstDay, LastDay)).Returns(Percentiles);
            metrics.Setup(service => service.GetClosedItemsForTeam(team, FirstDay, LastDay)).Returns([finished, stillOpen]);
            teams.Setup(repository => repository.GetById(SharedId)).Returns(team);
            var reading = new TeamReportMetricSeries(teams.Object, metrics.Object, new FakeLighthouseClock(DateTimeOffset.UtcNow));

            var percentiles = reading.CycleTimePercentiles(ReportOwner.Team(SharedId), Window);

            Assert.That(percentiles.FinishedItems, Is.EqualTo(1));
        }

        [Test]
        public void A_Portfolio_asking_the_Team_series_is_told_what_it_asked_for_and_what_it_is()
        {
            Assert.That(() => subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window),
                Throws.ArgumentException.With.Message.StartWith("Only a Team's series can be read here, not a Portfolio's"));
        }

        [Test]
        public void The_95th_is_the_one_the_Metrics_tab_gives_for_the_95th_percentile()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Team(SharedId), Window);

            Assert.That(percentiles.P95, Is.EqualTo(30));
        }

        [Test]
        public void A_Team_that_no_longer_exists_is_named_gone_by_kind_and_id()
        {
            var gone = ReportOwner.Team(SharedId + 1);

            Assert.That(() => subject.CycleTimePercentiles(gone, Window),
                Throws.TypeOf<ReportOwnerGoneException>().With.Property(nameof(ReportOwnerGoneException.Owner)).EqualTo(gone));
        }

        [Test]
        public void A_gone_Team_is_described_by_its_kind_and_id_alone()
        {
            var gone = ReportOwner.Team(SharedId + 1);

            Assert.That(() => subject.CycleTimePercentiles(gone, Window),
                Throws.TypeOf<ReportOwnerGoneException>().With.Message.EqualTo($"Team {SharedId + 1} does not exist"));
        }

        [Test]
        public void A_Portfolio_never_reads_the_series_of_the_Team_sharing_its_id()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window), Throws.ArgumentException);
                teams.Verify(repository => repository.GetById(It.IsAny<int>()), Times.Never);
            }
        }
    }
}
