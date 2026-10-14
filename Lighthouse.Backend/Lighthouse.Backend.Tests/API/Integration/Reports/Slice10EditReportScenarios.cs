using System.Net;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// An admin edits a report's name, how long Now is, and which of its frozen metrics it shows; every reader
    /// sees the change on their next view. Then - its window and what it froze - is never edited and never
    /// recomputed, so a metric ticked later shows Then as it was on the day of creation, and a metric the report
    /// never captured cannot be ticked at all. An edit saved over somebody else's is refused rather than
    /// silently winning. Readers cannot edit anything, Now included.
    ///
    /// Driving ports: PUT/GET /teams/{id}/reports/{reportId}, GET /teams/{id}/reports.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-10")]
    public class Slice10EditReportScenarios : ReportsAcceptanceTest
    {
        private const string SinceAugust = "Lightspeed engagement — since August";

        private static readonly DateOnly ThenStarts = new(2026, 7, 5);

        // @driving_port @real-io @us-10 @slice-10 @contract-shape:bounded-change
        [Test]
        public async Task An_admin_renames_a_report_and_every_reader_sees_the_new_name()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var saved = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), name: SinceAugust));

            TheCallerOnlyReads(lightspeed);
            var names = ReportNamesIn(await TheReportsListOf(lightspeed));
            var answer = await saved.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), answer);
                Assert.That(names, Is.EqualTo(SinceAugustAlone));
            }
        }

        // @driving_port @real-io @us-10 @us-07 @slice-10 @contract-shape:bounded-change
        [Test]
        public async Task A_changed_Now_length_is_stored_for_everyone()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var saved = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), nowLengthDays: 30));

            TheCallerOnlyReads(lightspeed);
            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(NowWindowIn(report), Is.EqualTo(new ReportWindow("2026-09-03", "2026-10-02", 30)));
                Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
            }
        }

        // @driving_port @real-io @us-10 @us-04 @slice-10 @contract-shape:bounded-change
        // Work Items that turn up for Then's days after creation must not reach the Throughput ticked later.
        [Test]
        public async Task Ticking_a_metric_that_was_frozen_but_hidden_shows_Then_as_it_was_on_the_day_of_creation()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamFinishedPerDay(lightspeed, ThenStarts, [.. Enumerable.Repeat(1, 90)]);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { Shown = [CycleTime] });
            TheTeamFinishedPerDay(lightspeed, ThenStarts, [.. Enumerable.Repeat(1, 10)]);

            using var saved = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), shown: [CycleTime, Throughput]));

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(ShownMetricsIn(report), Is.EqualTo(CycleTimeAndThroughput));
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(90, 90)));
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_metric_the_report_never_captured_cannot_be_shown()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { Shown = [CycleTime] });
            TheStoredPayloadIsReshaped(reportId, payload => WithoutTheCaptureOf(payload, Throughput));

            using var refused = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), shown: [CycleTime, Throughput]));

            await ThenItIsRefusedFor(refused, Reason.MetricNotCaptured);
            Assert.That(ShownMetricsIn(await TheReport(lightspeed, reportId)), Is.EqualTo(OnlyCycleTime));
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:unbounded-preservation
        // An edit has no Then in it; one that brings Then along anyway is saved as if Then were not there.
        [Test]
        public async Task Then_cannot_be_edited()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamFinishedPerDay(lightspeed, ThenStarts, [.. Enumerable.Repeat(1, 90)]);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            var asCreated = await TheReport(lightspeed, reportId);
            var before = TheStoredRowOf(reportId);
            var edit = AnEdit(asCreated, name: SinceAugust);
            var thenAndNow = edit["thenAndNow"]!.AsObject();
            thenAndNow["thenEndDate"] = "2026-07-31";
            thenAndNow["thenLengthDays"] = 30;

            using var saved = await EditTheReport(lightspeed, reportId, edit);

            var report = await TheReport(lightspeed, reportId);
            var after = TheStoredRowOf(reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(TextOf(report, "name"), Is.EqualTo(SinceAugust));
                Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
                Assert.That(WithoutTheMayChangeColumns(after), Is.EqualTo(WithoutTheMayChangeColumns(before)));
                Assert.That(JsonNode.DeepEquals(WithoutNowLength(after), WithoutNowLength(before)), Is.True,
                    $"the stored Then stays as it was frozen: {after[PayloadColumn]}");
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @boundary @contract-shape:bounded-change
        // A later version may have stored members this one does not know; an edit here must not strip them.
        [Test]
        public async Task An_edit_changes_the_name_the_shown_metrics_and_how_long_Now_is_and_nothing_else()
        {
            TheInstanceHasAPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            TheTeamFinishedPerDay(lightspeed, ThenStarts, [.. Enumerable.Repeat(1, 90)]);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { Shown = [CycleTime] });
            var neighbourId = await GivenTheAdminCreated(lightspeed, new NewReport { Name = "Then & Now — hand-over" });
            TheStoredPayloadIsReshaped(reportId, payload => payload["authorNotes"] = new JsonObject { [CycleTime] = "a note a later version wrote" });
            var before = TheStoredRowOf(reportId);
            var neighbourBefore = TheStoredRowOf(neighbourId);

            using var saved = await EditTheReport(lightspeed, reportId,
                AnEdit(await TheReport(lightspeed, reportId), name: SinceAugust, shown: [CycleTime, Throughput], nowLengthDays: 30));

            var after = TheStoredRowOf(reportId);
            var readBack = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(after["Name"], Is.EqualTo(SinceAugust));
                Assert.That(NowWindowIn(readBack).LengthDays, Is.EqualTo(30), "the new Now length is the one stored");
                Assert.That(after["ShownMetricKeysJson"], Is.Not.EqualTo(before["ShownMetricKeysJson"]));
                Assert.That(after["ConcurrencyToken"], Is.Not.EqualTo(before["ConcurrencyToken"]));
                Assert.That(WithoutTheMayChangeColumns(after), Is.EqualTo(WithoutTheMayChangeColumns(before)),
                    "owner, type, creation and payload version stay as they were");
                Assert.That(JsonNode.DeepEquals(WithoutNowLength(after), WithoutNowLength(before)), Is.True,
                    $"Then, every capture and the members a later version wrote stay as they were: {after[PayloadColumn]}");
                Assert.That(TheStoredRowOf(neighbourId), Is.EqualTo(neighbourBefore), "the Team's other report is not written");
            }
        }

        // @driving_port @real-io @us-10 @us-07 @slice-10 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_reader_cannot_edit_a_report_not_even_how_long_Now_is()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerOnlyReads(lightspeed);
            var asRead = await TheReport(lightspeed, reportId);
            using var refused = await EditTheReport(lightspeed, reportId, AnEdit(asRead, name: SinceAugust, nowLengthDays: 30));

            await ThenItIsForbidden(refused);
            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(report, "name"), Is.EqualTo(EngagementStart));
                Assert.That(NowWindowIn(report).LengthDays, Is.EqualTo(90));
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @error @ac-10.6 @contract-shape:unbounded-preservation
        // Two admins of the Team opened the same report; the one who saves second has not seen the first's change.
        [Test]
        public async Task A_save_over_another_admins_change_is_refused_and_overwrites_nothing()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            var openedByBoth = await TheReport(lightspeed, reportId);

            AnotherAdminOf(lightspeed);
            using var renamed = await EditTheReport(lightspeed, reportId, AnEdit(openedByBoth, name: SinceAugust));

            TheCallerAdministers(lightspeed);
            using var refused = await EditTheReport(lightspeed, reportId, AnEdit(openedByBoth, nowLengthDays: 30));

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(renamed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(TextOf(report, "name"), Is.EqualTo(SinceAugust));
                Assert.That(NowWindowIn(report).LengthDays, Is.EqualTo(90));
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @error @ac-10.6 @contract-shape:unbounded-preservation
        // An edit that does not name the version of the report it was made on could silently overwrite anybody's change.
        [Test]
        public async Task An_edit_that_does_not_name_the_version_it_was_made_on_is_refused_and_changes_nothing()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            var edit = AnEdit(await TheReport(lightspeed, reportId), name: SinceAugust, nowLengthDays: 30);
            edit.Remove("concurrencyToken");

            using var refused = await EditTheReport(lightspeed, reportId, edit);

            var answer = await refused.Content.ReadAsStringAsync();
            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), answer);
                Assert.That(TextOf(report, "name"), Is.EqualTo(EngagementStart));
                Assert.That(NowWindowIn(report).LengthDays, Is.EqualTo(90));
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:unbounded-preservation
        [TestCase("", null, Reason.NameMissing)]
        [TestCase(SinceAugust, 13, Reason.NowTooShort)]
        [TestCase(SinceAugust, 367, Reason.NowBeforeCutoff)]
        public async Task An_edit_breaking_the_rules_a_new_report_follows_is_refused_and_changes_nothing(string name, int? nowLengthDays, string reason)
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var refused = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), name: name, nowLengthDays: nowLengthDays));

            await ThenItIsRefusedFor(refused, reason);
            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(report, "name"), Is.EqualTo(EngagementStart));
                Assert.That(NowWindowIn(report).LengthDays, Is.EqualTo(90));
            }
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task An_edit_that_shows_no_metric_is_refused()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var refused = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), shown: []));

            await ThenItIsRefusedFor(refused, Reason.NoMetricShown);
            Assert.That(ShownMetricsIn(await TheReport(lightspeed, reportId)), Is.EqualTo(OnlyCycleTime));
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Editing_through_another_Teams_address_changes_nothing()
        {
            var lightspeed = ATeam(Lightspeed);
            var zenith = ATeam(Zenith);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerAdministersTheInstance();
            using var refused = await EditTheReport(zenith, reportId, AnEdit(await TheReport(lightspeed, reportId), name: SinceAugust));

            await ThenItIsNotFound(refused);
            Assert.That(TextOf(await TheReport(lightspeed, reportId), "name"), Is.EqualTo(EngagementStart));
        }

        // --- Steps ---

        private static readonly string?[] SinceAugustAlone = [SinceAugust];

        private static readonly string?[] OnlyCycleTime = [CycleTime];

        private static readonly string?[] CycleTimeAndThroughput = [CycleTime, Throughput];

        private const string PayloadColumn = "TemplatePayloadJson";

        private static readonly string[] MayChangeOnEdit = ["Name", "ShownMetricKeysJson", "ConcurrencyToken", PayloadColumn];

        private static Dictionary<string, string?> WithoutTheMayChangeColumns(Dictionary<string, string?> row)
            => row.Where(column => !MayChangeOnEdit.Contains(column.Key)).ToDictionary(column => column.Key, column => column.Value);

        /// <summary>The stored payload with the Now length taken out - the one payload member an edit may change.</summary>
        private static JsonObject WithoutNowLength(Dictionary<string, string?> row)
        {
            var payload = JsonNode.Parse(row[PayloadColumn] ?? "{}")!.AsObject();
            var nowLength = payload.FirstOrDefault(member => member.Key.Equals("nowLengthDays", StringComparison.OrdinalIgnoreCase)).Key;
            if (nowLength is not null)
            {
                payload.Remove(nowLength);
            }

            return payload;
        }
    }
}
