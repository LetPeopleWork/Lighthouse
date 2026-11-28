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
    public class PortfolioReportMetricSeriesTest
    {
        private const int SharedId = 7;

        private static readonly ReportWindow Window = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly DateTime FirstDay = new(2026, 7, 5, 0, 0, 0, DateTimeKind.Unspecified);

        private static readonly DateTime LastDay = new(2026, 10, 2, 0, 0, 0, DateTimeKind.Unspecified);

        private static readonly PercentileValue[] Percentiles = [new(50, 6), new(70, 11), new(85, 21), new(95, 30)];

        private Mock<IRepository<Portfolio>> portfolios = null!;
        private Mock<IPortfolioMetricsService> metrics = null!;
        private Portfolio portfolio = null!;
        private PortfolioReportMetricSeries subject = null!;

        [SetUp]
        public void SetUp()
        {
            portfolio = new Portfolio { Name = "Apollo", Id = SharedId };
            portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(repository => repository.GetById(SharedId)).Returns(portfolio);

            metrics = new Mock<IPortfolioMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, FirstDay, LastDay)).Returns(Percentiles);
            metrics.Setup(service => service.GetCycleTimeDataForPortfolio(portfolio, FirstDay, LastDay)).Returns([]);

            subject = new PortfolioReportMetricSeries(portfolios.Object, metrics.Object, new FakeLighthouseClock(DateTimeOffset.UtcNow));
        }

        [Test]
        public void A_Portfolio_reads_its_own_series()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(6, 11, 21, 30, 0)));
        }

        [Test]
        public void A_Portfolio_that_no_longer_exists_is_named_gone_by_kind_and_id()
        {
            var gone = ReportOwner.Portfolio(SharedId + 1);

            Assert.That(() => subject.CycleTimePercentiles(gone, Window),
                Throws.TypeOf<ReportOwnerGoneException>().With.Property(nameof(ReportOwnerGoneException.Owner)).EqualTo(gone));
        }

        [Test]
        public void Only_the_Features_with_a_Cycle_Time_count_as_finished()
        {
            var finished = new Feature { StateCategory = StateCategories.Done, StartedDate = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc), ClosedDate = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc) };
            var stillOpen = new Feature { StateCategory = StateCategories.Doing, StartedDate = new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc) };
            metrics.Setup(service => service.GetCycleTimeDataForPortfolio(portfolio, FirstDay, LastDay)).Returns([finished, stillOpen]);

            var percentiles = subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window);

            Assert.That(percentiles.FinishedItems, Is.EqualTo(1));
        }

        [Test]
        public void A_Team_asking_the_Portfolio_series_is_told_what_it_asked_for_and_what_it_is()
        {
            Assert.That(() => subject.CycleTimePercentiles(ReportOwner.Team(SharedId), Window),
                Throws.ArgumentException.With.Message.StartWith("Only a Portfolio's series can be read here, not a Team's"));
        }

        [Test]
        public void A_Portfolio_with_no_finished_Features_reads_no_percentiles_and_no_finished_Features()
        {
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, FirstDay, LastDay)).Returns([]);

            var percentiles = subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(0, 0, 0, 0, 0)));
        }

        [Test]
        public void A_Team_never_reads_the_series_of_the_Portfolio_sharing_its_id()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => subject.CycleTimePercentiles(ReportOwner.Team(SharedId), Window), Throws.ArgumentException);
                portfolios.Verify(repository => repository.GetById(It.IsAny<int>()), Times.Never);
            }
        }
    }
}
