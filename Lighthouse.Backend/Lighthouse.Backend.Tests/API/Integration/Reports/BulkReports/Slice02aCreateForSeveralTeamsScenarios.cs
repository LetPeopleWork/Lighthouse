using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// One configuration, created for several Teams in one request. The server goes Team by Team, in the order
    /// asked, through the same path as the single wizard, and answers every Team: created with its new report, or
    /// not created with one reason from the closed list. It checks the caller's Admin right on each Team when the
    /// run reaches it, so a Team deleted, or a right lost, while the run is under way is one row, never a failed
    /// run. A run from a Community instance is refused as a whole. Running the same creation again makes second
    /// copies; nothing is de-duplicated.
    ///
    /// Driving ports: POST /api/latest/reports/bulk, GET /api/latest/reports/creation-options; the per-owner
    /// GET /teams/{id}/reports tells what a run left behind.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("slice-02a")]
    public class Slice02aCreateForSeveralTeamsScenarios : BulkReportsAcceptanceTest
    {
        private static readonly string[] BothCreated = [Created, Created];

        private static readonly string[] EveryMetric = [CycleTime, Throughput, Wip, WorkItemAge];

        private static readonly string[] OnlyThenAndNow = [ThenAndNow];

        private static readonly string[] OnlyOceanExplorer = ["Ocean Explorer"];

        private static readonly string[] MembersThatDifferPerOwner = ["id", "ownerId", "ownerName", "concurrencyToken"];

        // @driving_port @real-io @us-02a @slice-02a @kpi-OUT-6350-K2-bulk-creation-used @contract-shape:bounded-change
        [Test]
        public async Task A_coach_creates_the_same_comparison_for_two_Teams_in_one_go()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            TheCallerIsAdminOf(zenith, gravity);

            using var run = await CreateForEach(TheQuartersComparison, zenith, gravity);
            var outcomes = await TheOutcomesOf(run);

            var zenithsReports = await ReportsOn(zenith);
            var gravitysReports = await ReportsOn(gravity);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Is.EqualTo(BothCreated));
                Assert.That(outcomes.Select(outcome => $"{outcome.OwnerKind}:{outcome.OwnerId}"), Is.EqualTo(new[] { OwnerKey(zenith), OwnerKey(gravity) }));
                Assert.That(zenithsReports.Select(report => TextOf(report, "name")), Is.EqualTo(new[] { TheQuarter }));
                Assert.That(gravitysReports.Select(report => TextOf(report, "name")), Is.EqualTo(new[] { TheQuarter }));
                Assert.That(ThenWindowIn(zenithsReports[0]), Is.EqualTo(ThenWindowIn(gravitysReports[0])));
                Assert.That(NowWindowIn(zenithsReports[0]), Is.EqualTo(NowWindowIn(gravitysReports[0])));
                Assert.That(ThenWindowIn(zenithsReports[0]), Is.EqualTo(new ReportWindow("2026-05-01", "2026-07-29", 90)));
                Assert.That(ShownMetricsIn(zenithsReports[0]), Is.EquivalentTo(EveryMetric));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @contract-shape:bounded-change
        // The run must make exactly the report the single wizard makes, so the comparison reads the same whichever way it was set up.
        [Test]
        public async Task Each_report_made_in_bulk_equals_the_one_the_single_wizard_makes_from_the_same_inputs()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            TheTeamFinished(zenith, new Finished(new DateOnly(2026, 6, 10), 4), new Finished(new DateOnly(2026, 9, 1), 6));
            TheTeamFinished(gravity, new Finished(new DateOnly(2026, 6, 10), 4), new Finished(new DateOnly(2026, 9, 1), 6));
            TheCallerIsAdminOf(zenith, gravity);

            using var run = await CreateForEach(TheQuartersComparison, zenith);
            await TheOutcomesOf(run);
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);

            var madeInBulk = WithoutWhatDiffersPerOwner((await ReportsOn(zenith))[0]);
            var madeAlone = WithoutWhatDiffersPerOwner((await ReportsOn(gravity))[0]);

            Assert.That(madeInBulk, Is.EqualTo(madeAlone));
        }

        // @driving_port @real-io @us-02a @slice-02a @error @contract-shape:bounded-change
        [Test]
        public async Task A_Team_whose_kept_history_is_too_short_is_skipped_with_the_day_its_history_starts()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var equinox = ATeamKeepingFinishedWorkSince("Equinox", EquinoxKeepsFinishedWorkSince);
            TheCallerIsAdminOf(zenith, gravity, equinox);
            var whatTheSingleCreateSays = await TheHistoryStartTheSingleCreateNames(equinox, TheQuartersComparison);

            using var run = await CreateForEach(TheQuartersComparison, zenith, gravity, equinox);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Is.EqualTo(new[] { Created, Created, NotCreated }));
                Assert.That(outcomes[2].Code, Is.EqualTo(Reason.ThenBeforeCutoff));
                Assert.That(outcomes[2].EarliestStart, Is.Not.Null.And.EqualTo(whatTheSingleCreateSays));
                Assert.That(await ReportNamesOn(equinox), Is.Empty);
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @edge @contract-shape:unbounded-preservation
        [Test]
        public async Task A_run_where_every_Team_is_refused_answers_each_with_its_reason_and_writes_nothing()
        {
            TheInstanceHasAPremiumLicence();
            var equinox = ATeamKeepingFinishedWorkSince("Equinox", EquinoxKeepsFinishedWorkSince);
            var voyager = ATeamKeepingFinishedWorkSince("Voyager", EquinoxKeepsFinishedWorkSince);
            TheCallerIsAdminOf(equinox, voyager);

            using var run = await CreateForEach(TheQuartersComparison, equinox, voyager);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Has.All.EqualTo(NotCreated));
                Assert.That(outcomes.Select(outcome => outcome.Code), Has.All.EqualTo(Reason.ThenBeforeCutoff));
                Assert.That(outcomes.Select(outcome => outcome.EarliestStart), Has.All.Not.Null);
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:unbounded-preservation
        [Test]
        public async Task A_Team_she_only_reads_is_not_created_on_and_the_others_are()
        {
            TheInstanceHasAPremiumLicence();
            var gravity = ATeam("Gravity");
            var pulsar = ATeam("Pulsar");
            TheCallerIsAdminOf(gravity);
            TheCallerOnlyReadsEach(pulsar);

            using var run = await CreateForEach(TheQuartersComparison, gravity, pulsar);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                    Is.EqualTo(new[] { (Created, (string?)null), (NotCreated, RunReason.NotAdmin) }));
                Assert.That(await ReportNamesOn(pulsar), Is.Empty);
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:bounded-change
        // A Team deleted, an Admin right lost and a licence lapsing while the run is under way each cost one row.
        [Test]
        public async Task Changes_during_the_run_are_refused_owner_by_owner()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var voyager = ATeam("Voyager");
            var lightspeed = ATeam("Lightspeed");
            await GivenTheOwnerHolds(lightspeed, TheMonthsComparison);
            TheCallerIsAdminOf(zenith, gravity, voyager, lightspeed);
            TheCallerStopsBeingAdminWhenTheRunReaches(gravity);
            TheOwnerIsDeletedWhenTheRunReaches(voyager);
            ThePremiumLicenceLapsesWhenTheRunReaches(lightspeed);

            using var run = await CreateForEach(TheQuartersComparison, zenith, gravity, voyager, lightspeed);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)), Is.EqualTo(new[]
                {
                    (Created, (string?)null),
                    (NotCreated, RunReason.NotAdmin),
                    (NotCreated, RunReason.OwnerNotFound),
                    (NotCreated, Reason.ReportCapReached),
                }));
                Assert.That(await ReportNamesOn(zenith), Is.EqualTo(new[] { TheQuarter }));
                Assert.That(await ReportNamesOn(gravity), Is.Empty);
                Assert.That(await ReportNamesOn(lightspeed), Is.EqualTo(new[] { TheMonth }));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @adapter-integration @contract-shape:bounded-change
        // Needs a database that holds its foreign keys: on one that does not, the orphan row would simply be saved.
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task An_owner_deleted_between_lookup_and_save_fails_only_its_own_row()
        {
            TheInstanceHasAPremiumLicence();
            var gravity = ATeam("Gravity");
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(gravity, zenith);
            TheOwnerIsDeletedBetweenLookupAndSave(gravity);

            using var run = await CreateForEach(TheQuartersComparison, gravity, zenith);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                    Is.EqualTo(new[] { (NotCreated, RunReason.NotSaved), (Created, (string?)null) }));
                Assert.That(await ReportNamesOn(zenith), Is.EqualTo(new[] { TheQuarter }));
                Assert.That(StoredReports(), Is.EqualTo(1));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task A_Community_instance_is_refused_the_whole_run_and_nothing_is_written()
        {
            TheInstanceHasNoPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            TheCallerIsAdminOf(zenith, gravity);

            using var run = await CreateForEach(TheQuartersComparison, zenith, gravity);

            await ThenItIsForbidden(run);
            Assert.That(StoredReports(), Is.Zero);
        }

        // @driving_port @real-io @us-02a @slice-02a @error @edge @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task More_than_50_owners_in_one_run_are_refused_whole()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var request = ABulkRun(TheQuartersComparison, [zenith]);
            request["owners"] = new JsonArray([.. Enumerable.Range(1, Ceiling + 1).Select(id => (JsonNode)new JsonObject { ["ownerKind"] = "Team", ["ownerId"] = 1000 + id })]);

            using var run = await CreateForEachFromRawRequest(request);

            await ThenTheWholeRequestIsRefusedAndNothingIsWritten(run);
        }

        // @driving_port @real-io @us-02a @slice-02a @edge @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task Exactly_50_owners_are_answered_owner_by_owner()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var request = ABulkRun(TheQuartersComparison, [zenith]);
            var gone = Enumerable.Range(1, Ceiling - 1).Select(id => (JsonNode)new JsonObject { ["ownerKind"] = "Team", ["ownerId"] = 1000 + id });
            request["owners"] = new JsonArray([AnOwnerOnTheWire(zenith), .. gone]);

            using var run = await CreateForEachFromRawRequest(request);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Has.Count.EqualTo(Ceiling));
                Assert.That(outcomes[0].Status, Is.EqualTo(Created));
                Assert.That(outcomes.Skip(1).Select(outcome => outcome.Code), Has.All.EqualTo(RunReason.OwnerNotFound));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @edge @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task A_Team_named_twice_is_created_once_and_answered_in_the_order_first_asked()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            TheCallerIsAdminOf(zenith, gravity);

            using var run = await CreateForEach(TheQuartersComparison, gravity, zenith, gravity);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => $"{outcome.OwnerKind}:{outcome.OwnerId}"), Is.EqualTo(new[] { OwnerKey(gravity), OwnerKey(zenith) }));
                Assert.That(await ReportNamesOn(gravity), Has.Count.EqualTo(1));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @contract-shape:unbounded-preservation
        [TestCase("no owner at all")]
        [TestCase("an owner of no known kind")]
        [TestCase("a Then & Now without its Then")]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task A_malformed_run_is_refused_whole_and_nothing_is_written(string whatIsWrong)
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(zenith);
            var request = ABulkRun(TheQuartersComparison, [zenith]);
            switch (whatIsWrong)
            {
                case "no owner at all":
                    request["owners"] = new JsonArray();
                    break;
                case "an owner of no known kind":
                    request["owners"] = new JsonArray(new JsonObject { ["ownerKind"] = "Delivery", ["ownerId"] = zenith.Id });
                    break;
                default:
                    request["thenAndNow"] = new JsonObject { ["nowLengthDays"] = 90 };
                    break;
            }

            using var run = await CreateForEachFromRawRequest(request);

            await ThenTheWholeRequestIsRefusedAndNothingIsWritten(run);
        }

        // @driving_port @real-io @us-02a @slice-02a @edge @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task Running_the_same_creation_twice_gives_every_Team_a_second_copy()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            TheCallerIsAdminOf(zenith, gravity);

            using (var first = await CreateForEach(TheQuartersComparison, zenith, gravity))
            {
                await TheOutcomesOf(first);
            }

            using var second = await CreateForEach(TheQuartersComparison, zenith, gravity);
            var outcomes = await TheOutcomesOf(second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Is.EqualTo(BothCreated));
                Assert.That(await ReportNamesOn(zenith), Is.EqualTo(new[] { TheQuarter, TheQuarter }));
                Assert.That(await ReportNamesOn(gravity), Is.EqualTo(new[] { TheQuarter, TheQuarter }));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @contract-shape:pure-function
        [Test]
        public async Task A_created_row_names_the_new_report_which_opens_at_its_owner()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(zenith);

            using var run = await CreateForEach(TheQuartersComparison, zenith);
            var outcome = (await TheOutcomesOf(run)).Single();

            var report = await TheReport(zenith, outcome.ReportId ?? -1);

            Assert.That(TextOf(report, "name"), Is.EqualTo(TheQuarter));
        }

        // @driving_port @real-io @us-02a @us-04 @slice-02a @contract-shape:pure-function
        [Test]
        public async Task The_creation_options_offer_the_type_its_metrics_the_window_bounds_and_every_readable_owner()
        {
            var zenith = ATeam("Zenith");
            var oceanExplorer = APortfolio("Ocean Explorer");
            TheCallerOnlyReadsEach(zenith, oceanExplorer);

            var options = await TheCreationOptions();

            var owners = options.GetProperty("owners").EnumerateArray()
                .Select(owner => $"{TextOf(owner, "ownerKind")?.ToLowerInvariant()}:{NumberOf(owner, "ownerId")}:{TextOf(owner, "ownerName")}")
                .ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(options, "today"), Is.EqualTo(Iso(TodayInTheExamples)));
                Assert.That(NumberOf(options, "minimumLengthDays"), Is.EqualTo(14));
                Assert.That(options.GetProperty("templates").EnumerateArray().Select(template => TextOf(template, "templateKey")), Is.EqualTo(OnlyThenAndNow));
                Assert.That(owners, Is.EquivalentTo(new[] { $"{OwnerKey(zenith)}:Zenith", $"{OwnerKey(oceanExplorer)}:Ocean Explorer" }));
            }
        }

        // @driving_port @real-io @us-02a @slice-02a @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
        [Test]
        public async Task The_creation_options_leave_out_an_owner_the_caller_cannot_read()
        {
            var oceanExplorer = APortfolio("Ocean Explorer");
            APortfolio("Apollo");
            TheCallerOnlyReadsEach(oceanExplorer);

            var options = await TheCreationOptions();

            Assert.That(options.GetProperty("owners").EnumerateArray().Select(owner => TextOf(owner, "ownerName")), Is.EqualTo(OnlyOceanExplorer));
        }

        private static string WithoutWhatDiffersPerOwner(JsonElement report)
        {
            var node = JsonNode.Parse(report.GetRawText())!.AsObject();
            foreach (var member in MembersThatDifferPerOwner)
            {
                node.Remove(member);
            }

            return node.ToJsonString();
        }
    }
}
