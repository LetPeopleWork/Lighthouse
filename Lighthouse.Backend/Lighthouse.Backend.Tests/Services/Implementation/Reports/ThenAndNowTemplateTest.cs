using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ThenAndNowTemplateTest
    {
        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        private static readonly string[] CycleTimeShownTwice = [CycleTimeReportMetric.MetricKey, CycleTimeReportMetric.MetricKey];

        [TestCase(14, null)]
        [TestCase(13, ReportRefusalCodes.NowTooShort)]
        public void A_Now_of_at_least_fourteen_days_is_accepted(int nowLengthDays, string? expectedRefusal)
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

        [Test]
        public void A_stored_report_that_repeats_a_shown_metric_reads_it_back_once()
        {
            var calendar = new ReportCalendar(Today, 365);
            var subject = Subject();
            var frozen = subject.Freeze(
                Lightspeed,
                new NewReport(ThenAndNowTemplate.TemplateKey, "Engagement start", CycleTimeShown, new NewThenAndNow(Today, 90, 90)),
                calendar);
            var report = Report.For(Lightspeed, ThenAndNowTemplate.TemplateKey, "Engagement start", new DateTime(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc));
            report.TemplatePayloadJson = frozen.Value ?? "{}";
            report.ShownMetricKeysJson = JsonSerializer.Serialize(CycleTimeShownTwice);

            var read = subject.Read(report, calendar);

            Assert.That(read.ShownMetricKeys, Is.EqualTo(CycleTimeShown));
        }

        private static ThenAndNowTemplate Subject()
        {
            var series = new Mock<IReportMetricSeries>();
            series.Setup(s => s.CycleTimePercentiles(Lightspeed, It.IsAny<ReportWindow>())).Returns(new CycleTimePercentiles(6, 11, 21, 30, 20));
            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            return new ThenAndNowTemplate(new ReportMetricCatalog([new CycleTimeReportMetric(series.Object)]), clock, NullLogger<ThenAndNowTemplate>.Instance);
        }
    }
}
