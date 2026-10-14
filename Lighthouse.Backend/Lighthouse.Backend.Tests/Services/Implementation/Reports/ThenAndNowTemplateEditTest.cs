using System.Text.Json.Nodes;
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
    public class ThenAndNowTemplateEditTest
    {
        private const string Name = "Engagement start";

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly ReportCalendar Calendar = new(Today, 365);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        [TestCase(14)]
        [TestCase(30)]
        [TestCase(365)]
        public void An_edit_sets_the_Now_length_and_keeps_every_other_member_including_one_this_version_does_not_know(int nowLengthDays)
        {
            var report = AReportWithALaterVersionsMember();
            var before = JsonNode.Parse(report.TemplatePayloadJson)!.AsObject();

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, nowLengthDays), Calendar);

            var after = JsonNode.Parse(edited.Value ?? "{}")!.AsObject();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Done));
                Assert.That(after["nowLengthDays"]!.GetValue<int>(), Is.EqualTo(nowLengthDays));
                Assert.That(JsonNode.DeepEquals(WithoutNowLength(after), WithoutNowLength(before)), Is.True, edited.Value);
            }
        }

        private static Report AReportWithALaterVersionsMember()
        {
            var frozen = Subject().Freeze(
                Lightspeed,
                new NewReport(ThenAndNowTemplate.TemplateKey, Name, CycleTimeShown, new NewThenAndNow(Today, 90, 90)),
                Calendar);
            var payload = JsonNode.Parse(frozen.Value!)!.AsObject();
            payload["authorNotes"] = new JsonObject { [CycleTimeReportMetric.MetricKey] = "a note a later version wrote" };

            var report = Report.For(Lightspeed, ThenAndNowTemplate.TemplateKey, Name, new DateTime(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc));
            report.TemplatePayloadJson = payload.ToJsonString();
            return report;
        }

        private static JsonObject WithoutNowLength(JsonObject payload)
        {
            var copy = payload.DeepClone().AsObject();
            copy.Remove("nowLengthDays");
            return copy;
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
