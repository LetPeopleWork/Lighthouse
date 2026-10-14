using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ReportMetricSeriesByOwnerTest
    {
        private const int SharedId = 7;

        private static readonly ReportWindow Window = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private ReportMetricSeriesByOwner subject = null!;

        [SetUp]
        public void SetUp()
        {
            var clock = new FakeLighthouseClock(DateTimeOffset.UtcNow);

            var team = new Team { Name = "Team Lightspeed", Id = SharedId };
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.GetById(SharedId)).Returns(team);
            var teamMetrics = new Mock<ITeamMetricsService>();
            teamMetrics.Setup(service => service.GetCycleTimePercentilesForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(50, 1), new PercentileValue(70, 2), new PercentileValue(85, 3), new PercentileValue(95, 4)]);
            teamMetrics.Setup(service => service.GetClosedItemsForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);

            var portfolio = new Portfolio { Name = "Apollo", Id = SharedId };
            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(repository => repository.GetById(SharedId)).Returns(portfolio);
            var portfolioMetrics = new Mock<IPortfolioMetricsService>();
            portfolioMetrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(50, 10), new PercentileValue(70, 20), new PercentileValue(85, 30), new PercentileValue(95, 40)]);
            portfolioMetrics.Setup(service => service.GetCycleTimeDataForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);

            subject = new ReportMetricSeriesByOwner(
                new TeamReportMetricSeries(teams.Object, teamMetrics.Object, clock),
                new PortfolioReportMetricSeries(portfolios.Object, portfolioMetrics.Object, clock));
        }

        [Test]
        public void A_Team_owner_reads_the_Team_series()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Team(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(1, 2, 3, 4, 0)));
        }

        [Test]
        public void A_Portfolio_owner_reads_the_Portfolio_series()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(10, 20, 30, 40, 0)));
        }
    }
}
