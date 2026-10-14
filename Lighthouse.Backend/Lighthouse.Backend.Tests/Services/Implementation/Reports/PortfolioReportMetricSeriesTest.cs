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
    public class PortfolioReportMetricSeriesTest
    {
        private const int SharedId = 7;

        private static readonly ReportWindow Window = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

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
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Percentiles);
            metrics.Setup(service => service.GetCycleTimeDataForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);

            subject = new PortfolioReportMetricSeries(portfolios.Object, metrics.Object, new FakeLighthouseClock(DateTimeOffset.UtcNow));
        }

        [Test]
        public void A_Portfolio_reads_its_own_series()
        {
            var percentiles = subject.CycleTimePercentiles(ReportOwner.Portfolio(SharedId), Window);

            Assert.That(percentiles, Is.EqualTo(new CycleTimePercentiles(6, 11, 21, 30, 0)));
        }

        [Test]
        public void A_Portfolio_with_no_finished_Features_reads_no_percentiles_and_no_finished_Features()
        {
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);

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
