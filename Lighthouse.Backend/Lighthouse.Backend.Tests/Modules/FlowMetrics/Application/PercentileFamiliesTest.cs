using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    /// <summary>
    /// Which percentile charts an owner records and where each is read from. Cycle time is read over
    /// the window that ends on the day; work item age is read as of the day alone, whatever window start
    /// it is handed.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class PercentileFamiliesTest
    {
        private const int CycleTimeP50 = 11;

        private const int WorkItemAgeP50 = 22;

        private static readonly DateTime WindowStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime WindowEnd = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ATeamRecordsItsCycleTimeOverTheWindowAndItsWorkItemAgeAsOfTheWindowEnd()
        {
            var team = new Team { Id = 1 };
            var metrics = new Mock<ITeamMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForTeam(team, WindowStart, WindowEnd)).Returns([new(50, CycleTimeP50)]);
            metrics.Setup(service => service.GetWorkItemAgePercentilesForTeam(team, WindowEnd)).Returns([new(50, WorkItemAgeP50)]);

            var families = PercentileFamilies.For(team, metrics.Object);

            AssertTheFamilies(families);
        }

        [Test]
        public void APortfolioRecordsItsCycleTimeOverTheWindowAndItsWorkItemAgeAsOfTheWindowEnd()
        {
            var portfolio = new Portfolio { Id = 1 };
            var metrics = new Mock<IPortfolioMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(portfolio, WindowStart, WindowEnd)).Returns([new(50, CycleTimeP50)]);
            metrics.Setup(service => service.GetWorkItemAgePercentilesForPortfolio(portfolio, WindowEnd)).Returns([new(50, WorkItemAgeP50)]);

            var families = PercentileFamilies.For(portfolio, metrics.Object);

            AssertTheFamilies(families);
        }

        private static void AssertTheFamilies(IReadOnlyList<PercentileFamilyReader> families)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(families.Select(family => family.MetricType), Is.EqualTo(new[] { MetricType.CycleTime, MetricType.WorkItemAge }));
                Assert.That(Read(families, MetricType.CycleTime, WindowStart), Is.EqualTo(CycleTimeP50));
                Assert.That(Read(families, MetricType.WorkItemAge, WindowStart), Is.EqualTo(WorkItemAgeP50));
                Assert.That(Read(families, MetricType.WorkItemAge, WindowStart.AddDays(-60)), Is.EqualTo(WorkItemAgeP50), "work item age is read as of the window end, so the window start changes nothing");
            }
        }

        private static int Read(IReadOnlyList<PercentileFamilyReader> families, MetricType metricType, DateTime windowStart)
            => families.Single(family => family.MetricType == metricType).ReadPercentiles(windowStart, WindowEnd).Single(value => value.Percentile == 50).Value;
    }
}
