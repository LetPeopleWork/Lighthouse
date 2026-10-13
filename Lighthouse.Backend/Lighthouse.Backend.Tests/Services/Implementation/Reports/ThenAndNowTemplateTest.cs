using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ThenAndNowTemplateTest
    {
        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        [TestCase(1, null)]
        [TestCase(0, ReportRefusalCodes.NowTooShort)]
        public void A_Now_of_at_least_one_day_is_accepted(int nowLengthDays, string? expectedRefusal)
        {
            var frozen = Subject().Freeze(
                Lightspeed,
                new NewReport(ThenAndNowTemplate.TemplateKey, "Engagement start", CycleTimeShown, new NewThenAndNow(Today, 90, nowLengthDays)),
                new ReportCalendar(Today, 365));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(frozen.Code, Is.EqualTo(expectedRefusal));
                Assert.That(ThenAndNowPayload.FromJson(frozen.Value ?? "null")?.NowLengthDays, Is.EqualTo(expectedRefusal is null ? nowLengthDays : null));
            }
        }

        private static ThenAndNowTemplate Subject()
        {
            var series = new Mock<IReportMetricSeries>();
            series.Setup(s => s.CycleTimePercentiles(Lightspeed, It.IsAny<ReportWindow>())).Returns(new CycleTimePercentiles(6, 11, 21, 20));
            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            return new ThenAndNowTemplate(new ReportMetricCatalog([new CycleTimeReportMetric(series.Object)]), clock);
        }
    }
}
