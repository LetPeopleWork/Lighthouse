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

        private static readonly string[] CycleTimeAndThroughput = [CycleTimeReportMetric.MetricKey, ThroughputReportMetric.MetricKey];

        private static readonly string[] NoMetric = [];

        private static readonly string[] NotInTheCatalog = ["velocity"];

        private static readonly string[] CycleTimeShownTwice = [CycleTimeReportMetric.MetricKey, CycleTimeReportMetric.MetricKey];

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

        [TestCase(13, ReportRefusalCodes.NowTooShort)]
        [TestCase(367, ReportRefusalCodes.NowBeforeCutoff)]
        [TestCase(int.MaxValue, ReportRefusalCodes.NowTooLong)]
        public void An_edit_to_a_Now_a_new_report_could_not_have_is_refused_and_changes_nothing(int nowLengthDays, string expectedRefusal)
        {
            var report = AReportWithALaterVersionsMember();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, nowLengthDays), Calendar);

            ThenItIsRefused(edited, expectedRefusal, report, stored);
        }

        [TestCaseSource(nameof(ShownListsANewReportCouldNotHave))]
        public void An_edit_showing_metrics_a_new_report_could_not_show_is_refused_and_changes_nothing(string[] shown, string expectedRefusal)
        {
            var report = AReportWithALaterVersionsMember();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, shown, report.ConcurrencyToken, 30), Calendar);

            ThenItIsRefused(edited, expectedRefusal, report, stored);
        }

        [Test]
        public void A_metric_the_report_never_captured_cannot_be_shown()
        {
            var report = AReportWithALaterVersionsMember();
            var payload = JsonNode.Parse(report.TemplatePayloadJson)!.AsObject();
            payload["captures"]!.AsObject().Remove(ThroughputReportMetric.MetricKey);
            report.TemplatePayloadJson = payload.ToJsonString();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeAndThroughput, report.ConcurrencyToken, 30), Calendar);

            ThenItIsRefused(edited, ReportRefusalCodes.MetricNotCaptured, report, stored);
        }

        [Test]
        public void A_metric_captured_at_creation_but_hidden_can_be_shown()
        {
            var report = AReportWithALaterVersionsMember();

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeAndThroughput, report.ConcurrencyToken, 30), Calendar);

            Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Done));
        }

        private static IEnumerable<TestCaseData> ShownListsANewReportCouldNotHave()
        {
            yield return new TestCaseData(NoMetric, ReportRefusalCodes.NoMetricShown);
            yield return new TestCaseData(NotInTheCatalog, ReportRefusalCodes.UnknownMetric);
            yield return new TestCaseData(CycleTimeShownTwice, ReportRefusalCodes.DuplicateMetric);
        }

        private static void ThenItIsRefused(ReportOutcome<string> edited, string expectedRefusal, Report report, string stored)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(edited.Code, Is.EqualTo(expectedRefusal));
                Assert.That(edited.Value, Is.Null);
                Assert.That(report.TemplatePayloadJson, Is.EqualTo(stored));
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
            series.Setup(s => s.DailyThroughput(Lightspeed, It.IsAny<ReportWindow>())).Returns((ReportOwner _, ReportWindow window) => [.. Enumerable.Repeat(1, window.LengthDays)]);
            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            var catalog = new ReportMetricCatalog([new CycleTimeReportMetric(series.Object), new ThroughputReportMetric(series.Object)]);
            return new ThenAndNowTemplate(catalog, clock, NullLogger<ThenAndNowTemplate>.Instance);
        }
    }
}
