using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// A Team admin creates a Then &amp; Now report: Then is the last 90 days ending today, frozen the moment the
    /// report is created, and Now is the same length ending on whatever day the report is read. The first panel
    /// is {Cycle Time}: the 85th, 70th and 50th percentiles on both sides, each with how many finished Work
    /// Items it rests on. A value with nothing to rest on is "—" with a reason, never 0.
    ///
    /// Every reader of the Team sees the report; only its admin creates one; nobody reaches a report through
    /// another Team's address. A report goes when its Team goes. And this version has to live alongside rows a
    /// later version writes, because it is what an instance rolls back to.
    ///
    /// Driving ports: GET/POST /teams/{id}/reports, GET /teams/{id}/reports/templates, GET /teams/{id}/reports/{reportId},
    /// DELETE /teams/{id}, GET /logs/problems.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-01")]
    public class Slice01TeamReportScenarios : ReportsAcceptanceTest
    {
        private const string LaterTemplate = "signal";

        private const string DroppedLater = "LS-17";

        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-5878-K1-report-created @contract-shape:bounded-change
        [Test]
        public async Task A_coach_freezes_the_Cycle_Time_of_the_last_ninety_days_for_a_Team()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();

            TheCallerAdministers(lightspeed);
            using var created = await CreateAReport(lightspeed, new NewReport());
            var reportId = await ThenItWasCreated(created);

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(report, "name"), Is.EqualTo(EngagementStart));
                Assert.That(TextOf(report, "templateKey"), Is.EqualTo(ThenAndNow));
                Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(21, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P70), Is.EqualTo(Shown(11, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P50), Is.EqualTo(Shown(6, 20)));
                Assert.That(NowValueIn(report, CycleTime, P85), Is.EqualTo(Shown(21, 20)));
                Assert.That(NowValueIn(report, CycleTime, P70), Is.EqualTo(Shown(11, 20)));
                Assert.That(NowValueIn(report, CycleTime, P50), Is.EqualTo(Shown(6, 20)));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:pure-function
        [Test]
        public async Task Every_reader_of_the_Team_finds_the_report_in_its_list()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerOnlyReads(lightspeed);
            var reports = ReportsIn(await TheReportsListOf(lightspeed));

            Assert.That(reports, Has.Count.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(reports[0], "name"), Is.EqualTo(EngagementStart));
                Assert.That(TextOf(reports[0], "templateKey"), Is.EqualTo(ThenAndNow));
                Assert.That(TextOf(reports[0], "createdOn"), Is.EqualTo(TodayOnTheWire));
            }
        }

        // @driving_port @real-io @us-01 @us-02 @slice-01 @contract-shape:pure-function
        // The wizard's date picker and default name are bounded by the instance's day, never the viewer's clock.
        [Test]
        public async Task The_types_of_report_offered_name_today_and_the_earliest_day_Then_may_start()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            var templates = await TheTypesOfReportFor(lightspeed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(templates, "today"), Is.EqualTo(TodayOnTheWire));
                Assert.That(TextOf(templates, "earliestStart"), Is.EqualTo("2025-10-02"));
                Assert.That(templates.GetProperty("minimumLengthDays").GetInt32(), Is.EqualTo(14));
                Assert.That(MetricKeysOfThenAndNowIn(templates), Does.Contain(CycleTime));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:pure-function
        [Test]
        public async Task Now_moves_with_the_calendar_while_Then_stays_as_it_was_frozen()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TodayIs(new DateOnly(2026, 10, 30));
            GivenLightspeedFinishedTenQuickerWorkItemsInOctober(lightspeed);

            TheCallerOnlyReads(lightspeed);
            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
                Assert.That(NowWindowIn(report), Is.EqualTo(new ReportWindow("2026-08-02", "2026-10-30", 90)));
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(21, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P70), Is.EqualTo(Shown(11, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P50), Is.EqualTo(Shown(6, 20)));
                Assert.That(NowValueIn(report, CycleTime, P85), Is.EqualTo(Shown(12, 10)));
                Assert.That(NowValueIn(report, CycleTime, P70), Is.EqualTo(Shown(9, 10)));
                Assert.That(NowValueIn(report, CycleTime, P50), Is.EqualTo(Shown(6, 10)));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
        // Rebuilding Then without LS-17 would give an 85th percentile of 17 days and a 70th of 9, on 19 Work Items,
        // so 21 and 11 on 20 can only be the frozen values.
        [Test]
        public async Task The_frozen_Then_does_not_move_when_the_tracker_later_drops_a_Work_Item()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheTrackerNoLongerHolds(DroppedLater);

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(21, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P70), Is.EqualTo(Shown(11, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P50), Is.EqualTo(Shown(6, 20)));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:pure-function
        [Test]
        public async Task Thin_data_shows_the_value_with_its_sample_size()
        {
            var pulsar = ATeam("Team Pulsar");
            TheTeamFinished(pulsar,
                new Finished(new DateOnly(2026, 8, 3), 9),
                new Finished(new DateOnly(2026, 8, 20), 18),
                new Finished(new DateOnly(2026, 9, 14), 25));

            var reportId = await GivenTheAdminCreated(pulsar, new NewReport());

            var report = await TheReport(pulsar, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(18, 3)));
                Assert.That(ThenValueIn(report, CycleTime, P70), Is.EqualTo(Shown(18, 3)));
                Assert.That(ThenValueIn(report, CycleTime, P50), Is.EqualTo(Shown(9, 3)));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:pure-function
        [Test]
        public async Task No_finished_Work_Items_show_a_dash_and_the_reason_never_a_zero()
        {
            var nova = ATeam("Team Nova");

            var reportId = await GivenTheAdminCreated(nova, new NewReport());

            var report = await TheReport(nova, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenItIsADashFor(ThenValueIn(report, CycleTime, P85), Reason.NoFinishedItems);
                ThenItIsADashFor(ThenValueIn(report, CycleTime, P70), Reason.NoFinishedItems);
                ThenItIsADashFor(ThenValueIn(report, CycleTime, P50), Reason.NoFinishedItems);
                ThenItIsADashFor(NowValueIn(report, CycleTime, P85), Reason.NoFinishedItems);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_reader_cannot_create_a_report()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerOnlyReads(lightspeed);
            var listed = await TheReportsListOf(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { Name = "Then & Now — a reader's own" });

            await ThenItIsForbidden(refused);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReportNamesIn(listed), Is.EqualTo(TheEngagementStartAlone), "the reader reads the list, so the refusal is about writing");
                Assert.That(StoredReports(), Is.EqualTo(1));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // Without a role on the Team, its reports are not there at all, the way the Team itself is not.
        [Test]
        public async Task Somebody_with_no_role_on_the_Team_cannot_see_that_it_has_reports()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerHasNoRoleOnIt();
            using var list = await AskForTheReportsListOf(lightspeed);
            using var report = await AskForTheReport(lightspeed, reportId);

            await ThenItIsNotFound(list);
            await ThenItIsNotFound(report);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:pure-function
        [Test]
        public async Task A_report_reached_through_another_Teams_address_is_not_found()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var zenith = ATeam(Zenith);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerAdministersTheInstance();
            using var throughItsOwnTeam = await AskForTheReport(lightspeed, reportId);
            using var throughZenith = await AskForTheReport(zenith, reportId);

            Assert.That(throughItsOwnTeam.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "the same caller opens it through Lightspeed, so the refusal below is about the address");
            await ThenItIsNotFound(throughZenith);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:pure-function
        [Test]
        public async Task A_report_that_does_not_exist_is_not_found()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var list = await AskForTheReportsListOf(lightspeed);
            using var report = await AskForTheReport(lightspeed, 4711);

            Assert.That(list.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the Team's reports can be read at all");
            await ThenItIsNotFound(report);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        [TestCase("")]
        [TestCase("   ")]
        public async Task A_report_without_a_name_is_refused_and_nothing_is_created(string name)
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { Name = name });

            await ThenItIsRefusedFor(refused, Reason.NameMissing);
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_type_of_report_this_version_does_not_offer_is_refused()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { TemplateKey = LaterTemplate });

            await ThenItIsRefusedFor(refused, Reason.TemplateNotSupported);
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:bounded-change
        // Half past one in the morning in the instance's zone is still the previous day in UTC.
        [Test]
        public async Task Then_ending_today_means_the_instances_today_just_after_midnight()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            Clock.SetInstant(new DateTimeOffset(2026, 10, 1, 23, 30, 0, TimeSpan.Zero));

            TheCallerAdministers(lightspeed);
            var templates = await TheTypesOfReportFor(lightspeed);
            var today = DateOnly.ParseExact(TextOf(templates, "today") ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var created = await CreateAReport(lightspeed, new NewReport { ThenEnd = today });

            Assert.That(TextOf(templates, "today"), Is.EqualTo(TodayOnTheWire));
            var report = await TheReport(lightspeed, await ThenItWasCreated(created));
            Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
        }

        // @driving_port @real-io @us-08 @slice-01 @contract-shape:bounded-change
        // The reports go at the database, so an older version deleting the Team takes them along as well.
        [Test]
        public async Task Deleting_a_Team_deletes_its_reports()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            await GivenTheAdminCreated(lightspeed, new NewReport());
            var storedBefore = StoredReports();

            TheCallerAdministersTheInstance();
            using var deleted = await DeleteTheOwner(lightspeed);
            var answer = await deleted.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted.IsSuccessStatusCode, Is.True, answer);
                Assert.That(storedBefore, Is.EqualTo(1));
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // An instance rolled back from a later version still holds that version's reports.
        [Test]
        public async Task A_report_of_a_type_this_version_does_not_know_is_listed_but_opening_it_is_refused_never_a_crash()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredReportBecomesOfType(reportId, LaterTemplate);

            var listed = ReportsIn(await TheReportsListOf(lightspeed));
            using var opened = await AskForTheReport(lightspeed, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed.Select(report => TextOf(report, "name")), Is.EqualTo(TheEngagementStartAlone));
                Assert.That(listed.Select(report => report.TryGetProperty("thenAndNow", out _)), Is.All.False,
                    "a type this version cannot read carries no Then & Now part");
            }

            await ThenItIsRefusedWith(opened, HttpStatusCode.UnprocessableEntity, Reason.TemplateNotSupported);
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task A_report_carrying_values_this_version_does_not_know_still_opens()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredPayloadIsReshaped(reportId, payload => payload["authorNotes"] = new JsonObject { [CycleTime] = "a note a later version wrote" });

            var report = await TheReport(lightspeed, reportId);

            Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(21, 20)));
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:bounded-change
        // The operator learns why a report will not open; the name a coach gave it never reaches the log.
        [Test]
        public async Task The_operator_is_warned_when_a_stored_report_cannot_be_read_and_the_warning_does_not_name_it()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredReportBecomesOfType(reportId, LaterTemplate);

            using var opened = await AskForTheReport(lightspeed, reportId);
            TheCallerAdministersTheInstance();
            var problems = (await ThePlaceOperatorsReadAboutProblems()).EnumerateArray()
                .Where(problem => TextOf(problem, "level") == "Warning" && (TextOf(problem, "message") ?? string.Empty).Contains(LaterTemplate))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity), "the report this version cannot read is refused, not crashed on");
                Assert.That(problems, Is.Not.Empty, "nothing tells the operator why the report will not open");
                Assert.That(problems.Select(problem => TextOf(problem, "message")), Has.None.Contains(EngagementStart));
            }
        }

        // Crafter-written: until the window refusals arrive, a create that cannot even name its Then window is
        // still a 400 that stores nothing, never a 500.
        [TestCase("thenEndDate", null)]
        [TestCase("thenEndDate", "2 Oct 2026")]
        [TestCase("thenLengthDays", null)]
        [TestCase("thenLengthDays", "ninety")]
        public async Task A_create_without_a_readable_Then_window_is_refused_and_stores_nothing(string member, string? sent)
        {
            var lightspeed = ATeam(Lightspeed);
            var request = new NewReport().ToJson();
            var thenAndNow = request["thenAndNow"]!.AsObject();
            thenAndNow.Remove(member);
            if (sent is not null)
            {
                thenAndNow[member] = sent;
            }

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReportFromRawRequest(lightspeed, request);
            var answer = await refused.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), answer);
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // Crafter-written: a window reaching back past the first day the calendar can name is refused before
        // anything is stored, never saved and then crashed on.
        [TestCase(800_000, 90, Reason.ThenTooLong)]
        [TestCase(90, 800_000, Reason.NowBeforeCutoff)]
        public async Task A_window_longer_than_the_calendar_reaches_is_refused_and_stores_nothing(int thenLengthDays, int nowLengthDays, string reason)
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { ThenLengthDays = thenLengthDays, NowLengthDays = nowLengthDays });
            var answer = await refused.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), answer);
                Assert.That(CodeIn(answer), Is.EqualTo(reason));
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // Crafter-written: a stored Now length no window can hold is a report this version cannot read.
        [Test]
        public async Task A_stored_report_whose_Now_the_calendar_cannot_reach_is_refused_with_a_warning_not_a_crash()
        {
            var lightspeed = GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredPayloadIsReshaped(reportId, payload => payload["nowLengthDays"] = 800_000);

            using var opened = await AskForTheReport(lightspeed, reportId);
            var answer = await opened.Content.ReadAsStringAsync();
            TheCallerAdministersTheInstance();
            var warnings = (await ThePlaceOperatorsReadAboutProblems()).EnumerateArray()
                .Where(problem => TextOf(problem, "level") == "Warning" && (TextOf(problem, "message") ?? string.Empty).Contains($"Stored report {reportId} "))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity), answer);
                Assert.That(CodeIn(answer), Is.EqualTo(Reason.TemplateNotSupported));
                Assert.That(warnings, Is.Not.Empty, "nothing tells the operator why the report will not open");
            }
        }

        // Crafter-written: the store holds names of up to 200 characters; a longer one is refused, not crashed on.
        [Test]
        public async Task A_name_longer_than_the_store_holds_is_refused_and_stores_nothing()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { Name = new string('n', 201) });
            var answer = await refused.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), answer);
                Assert.That(CodeIn(answer), Is.EqualTo(NameTooLong));
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // Crafter-written
        [Test]
        public async Task A_name_as_long_as_the_store_holds_is_created()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var created = await CreateAReport(lightspeed, new NewReport { Name = new string('n', 200) });

            await ThenItWasCreated(created);
        }

        // Crafter-written: a type this version does not offer is refused as such, whatever else the request lacks.
        [Test]
        public async Task A_type_this_version_does_not_offer_is_refused_as_such_even_without_a_Then_window()
        {
            var lightspeed = ATeam(Lightspeed);
            var request = new JsonObject
            {
                ["templateKey"] = LaterTemplate,
                ["name"] = "x",
                ["shownMetricKeys"] = new JsonArray(CycleTime),
            };

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReportFromRawRequest(lightspeed, request);
            var answer = await refused.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), answer);
                Assert.That(CodeIn(answer), Is.EqualTo(Reason.TemplateNotSupported));
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // --- Steps ---

        private const string NameTooLong = "name-too-long";

        private static string? CodeIn(string answer)
        {
            using var document = JsonDocument.Parse(answer);
            return TextOf(document.RootElement, "code");
        }

        private static readonly string?[] TheEngagementStartAlone = [EngagementStart];

        private static readonly int[] LightspeedsCycleTimes = [2, 3, 3, 4, 4, 5, 5, 6, 6, 6, 7, 8, 9, 11, 13, 17, 21, 24, 30, 35];

        private static readonly int[] OctobersQuickerCycleTimes = [3, 4, 4, 5, 6, 7, 9, 12, 14, 15];

        /// <summary>
        /// Twenty Work Items finished one a day from 6 to 25 Jul 2026, LS-1 to LS-20, taking 2 to 35 days: the
        /// 85th percentile is 21 days (LS-17), the 70th 11 and the 50th 6. All of them fall inside a Then of 90
        /// days ending 2 Oct, and all of them fall out of a Now of 90 days ending 30 Oct.
        /// </summary>
        private OwnerUnderTest GivenLightspeedFinishedTwentyWorkItemsSinceEarlyJuly()
        {
            var lightspeed = ATeam(Lightspeed);
            var firstDay = new DateOnly(2026, 7, 6);

            TheTeamFinished(lightspeed, [.. LightspeedsCycleTimes.Select((took, i) => new Finished(firstDay.AddDays(i), took, $"LS-{i + 1}"))]);
            return lightspeed;
        }

        /// <summary>Ten Work Items finished from 5 to 14 Oct 2026, quicker than Then's: 85th 12, 70th 9, 50th 6.</summary>
        private void GivenLightspeedFinishedTenQuickerWorkItemsInOctober(OwnerUnderTest lightspeed)
        {
            var firstDay = new DateOnly(2026, 10, 5);
            TheTeamFinished(lightspeed, [.. OctobersQuickerCycleTimes.Select((took, i) => new Finished(firstDay.AddDays(i), took))]);
        }
    }
}
