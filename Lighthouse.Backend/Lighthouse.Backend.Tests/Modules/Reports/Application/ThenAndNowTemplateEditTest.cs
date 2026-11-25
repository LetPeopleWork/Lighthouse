using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Application.Metrics;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.Reports.Application
{
    [TestFixture]
    public class ThenAndNowTemplateEditTest
    {
        private const string Name = "Engagement start";

        private const int StoredByAnOlderVersion = 45;

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly ReportCalendar Calendar = new(Today, 365);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        private static readonly string[] CycleTimeAndThroughput = [CycleTimeReportMetric.MetricKey, ThroughputReportMetric.MetricKey];

        private static readonly string[] NoMetric = [];

        private static readonly string[] NotInTheCatalog = ["velocity"];

        private static readonly string[] CycleTimeShownTwice = [CycleTimeReportMetric.MetricKey, CycleTimeReportMetric.MetricKey];

        [TestCase(null)]
        [TestCase(90)]
        public void An_edit_writes_no_window_member_and_keeps_every_member_including_a_Now_length_an_older_version_stored_and_one_this_version_does_not_know(int? requestedNowLengthDays)
        {
            var report = AReportWithALaterVersionsMember();
            var before = JsonNode.Parse(report.TemplatePayloadJson)!.AsObject();

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, requestedNowLengthDays), Calendar);

            var after = JsonNode.Parse(edited.Value ?? "{}")!.AsObject();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                    "WHAT: the edit was not accepted. WHY: a Now length that is absent or equal to Then's is accepted. HOW: accept it and write nothing for the window.");
                Assert.That(after["nowLengthDays"]!.GetValue<int>(), Is.EqualTo(StoredByAnOlderVersion),
                    "WHAT: the Now length an older version stored changed. WHY: Now is derived and the stored member is left unread and unchanged. HOW: write no Now length on Edit.");
                Assert.That(JsonNode.DeepEquals(after, before), Is.True,
                    $"WHAT: the edit changed the stored payload: {edited.Value}. WHY: only the name and shown metrics change, and those are not in the payload. HOW: return the payload as it was.");
            }
        }

        [TestCase(45)]
        [TestCase(30)]
        [TestCase(13)]
        [TestCase(367)]
        [TestCase(int.MaxValue)]
        public void An_edit_naming_a_Now_length_other_than_Thens_is_refused_and_changes_nothing(int requestedNowLengthDays)
        {
            var report = AReportWithALaterVersionsMember();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, requestedNowLengthDays), Calendar);

            ThenItIsRefused(edited, ReportRefusalCodes.NowLengthDiffersFromThen, report, stored);
        }

        [Test]
        public void An_edit_never_checks_Then_against_the_kept_history_again()
        {
            var report = AReportWithALaterVersionsMember();

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, 90), new ReportCalendar(Today.AddDays(800), 365));

            Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                $"WHAT: the edit was {edited.Status} {edited.Code} once Then lay far before the oldest kept day. WHY: an old report stays editable. HOW: do not check Then or Now against the kept history on Edit.");
        }

        [TestCaseSource(nameof(ShownListsANewReportCouldNotHave))]
        public void An_edit_showing_metrics_a_new_report_could_not_show_is_refused_and_changes_nothing(string[] shown, string expectedRefusal)
        {
            var report = AReportWithALaterVersionsMember();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, shown, report.ConcurrencyToken, null), Calendar);

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

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeAndThroughput, report.ConcurrencyToken, null), Calendar);

            ThenItIsRefused(edited, ReportRefusalCodes.MetricNotCaptured, report, stored);
        }

        [Test]
        public void A_metric_captured_at_creation_but_hidden_can_be_shown()
        {
            var report = AReportWithALaterVersionsMember();

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeAndThroughput, report.ConcurrencyToken, null), Calendar);

            Assert.That(edited.Status, Is.EqualTo(ReportOutcomeStatus.Done));
        }

        [Test]
        public void A_report_whose_payload_holds_no_captures_cannot_show_any_metric()
        {
            var report = AReportWithALaterVersionsMember();
            var payload = JsonNode.Parse(report.TemplatePayloadJson)!.AsObject();
            payload.Remove("captures");
            report.TemplatePayloadJson = payload.ToJsonString();
            var stored = report.TemplatePayloadJson;

            var edited = Subject().Edit(report, new ReportEdit(Name, CycleTimeShown, report.ConcurrencyToken, null), Calendar);

            ThenItIsRefused(edited, ReportRefusalCodes.MetricNotCaptured, report, stored);
        }

        private static IEnumerable<TestCaseData> ShownListsANewReportCouldNotHave()
        {
            yield return new TestCaseData(NoMetric, ReportRefusalCodes.NoMetricShown);
            yield return new TestCaseData(NotInTheCatalog, ReportRefusalCodes.UnknownMetric);
            yield return new TestCaseData(new[] { CycleTimeReportMetric.MetricKey, "velocity" }, ReportRefusalCodes.UnknownMetric);
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
            payload["nowLengthDays"] = StoredByAnOlderVersion;
            payload["authorNotes"] = new JsonObject { [CycleTimeReportMetric.MetricKey] = "a note a later version wrote" };

            var report = Report.For(Lightspeed, ThenAndNowTemplate.TemplateKey, Name, new DateTime(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc));
            report.TemplatePayloadJson = payload.ToJsonString();
            return report;
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
