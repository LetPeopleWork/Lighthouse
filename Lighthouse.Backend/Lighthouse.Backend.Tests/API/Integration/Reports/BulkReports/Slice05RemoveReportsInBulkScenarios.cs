namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Several reports removed in one request, on any mix of Teams and Portfolios. The server finds each report's
    /// owner itself and checks the caller's Admin right on it when the run reaches that report; it answers every
    /// report: removed, or not removed with its reason. A report somebody else already removed counts as removed,
    /// marked so it is never counted as this run's deletion. Removal is Community.
    ///
    /// Driving ports: POST /api/latest/reports/bulk-removal; GET /api/latest/reports and the per-owner lists tell
    /// what is left.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("slice-05")]
    public class Slice05RemoveReportsInBulkScenarios : BulkReportsAcceptanceTest
    {
        private const string TrialRun = "Then & Now — trial run";

        private static readonly NewReport ATrialReport = TheMonthsComparison with { Name = TrialRun };

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_coach_removes_reports_on_two_Teams_and_a_Portfolio_in_one_go_on_a_Community_instance()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var oceanExplorer = APortfolio("Ocean Explorer");
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            var onGravity = await GivenTheOwnerHolds(gravity, ATrialReport);
            var onOceanExplorer = await GivenTheOwnerHolds(oceanExplorer, ATrialReport);
            await GivenTheOwnerHolds(zenith, TheQuartersComparison);
            ThePremiumLicenceLapses();
            TheCallerIsAdminOf(zenith, gravity, oceanExplorer);

            using var run = await RemoveEach(onZenith, onGravity, onOceanExplorer);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.ReportId, outcome.Status, outcome.Code)), Is.EqualTo(new[]
                {
                    ((int?)onZenith, Removed, (string?)null),
                    ((int?)onGravity, Removed, (string?)null),
                    ((int?)onOceanExplorer, Removed, (string?)null),
                }));
                Assert.That(ReportNamesIn(await EveryReportTheCallerCanRead()), Is.EqualTo(new[] { TheQuarter }));
                Assert.That(await ReportNamesOn(gravity), Is.Empty);
                Assert.That(await ReportNamesOn(oceanExplorer), Is.Empty);
            }
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_report_on_an_owner_she_only_reads_is_not_removed_and_stays()
        {
            var gravity = ATeam("Gravity");
            var pulsar = ATeam("Pulsar");
            var onGravity = await GivenTheOwnerHolds(gravity, ATrialReport);
            var onPulsar = await GivenTheOwnerHolds(pulsar, ATrialReport);
            TheCallerIsAdminOf(gravity);
            TheCallerOnlyReadsEach(pulsar);

            using var run = await RemoveEach(onGravity, onPulsar);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                    Is.EqualTo(new[] { (Removed, (string?)null), (NotRemoved, RunReason.NotAdmin) }));
                Assert.That(await ReportNamesOn(pulsar), Is.EqualTo(new[] { TrialRun }));
            }
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_report_somebody_else_removed_first_counts_as_removed_and_says_it_was_already_gone()
        {
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            var onGravity = await GivenTheOwnerHolds(gravity, ATrialReport);
            AnotherAdminOf(gravity);
            using (var removedByTomas = await DeleteTheReport(gravity, onGravity))
            {
                Assert.That(removedByTomas.IsSuccessStatusCode, Is.True);
            }

            TheCallerIsAdminOf(zenith, gravity);

            using var run = await RemoveEach(onZenith, onGravity);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                Is.EqualTo(new[] { (Removed, (string?)null), (Removed, RunReason.AlreadyGone) }));
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task Losing_Admin_while_the_run_is_under_way_is_answered_for_that_report_alone()
        {
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var onGravity = await GivenTheOwnerHolds(gravity, ATrialReport);
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            TheCallerIsAdminOf(zenith, gravity);
            TheCallerStopsBeingAdminWhenTheRunReaches(gravity);

            using var run = await RemoveEach(onGravity, onZenith);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                    Is.EqualTo(new[] { (NotRemoved, RunReason.NotAdmin), (Removed, (string?)null) }));
                Assert.That(await ReportNamesOn(gravity), Is.EqualTo(new[] { TrialRun }));
            }
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_report_whose_owner_was_deleted_counts_as_already_gone()
        {
            var voyager = ATeam("Voyager");
            var zenith = ATeam("Zenith");
            var onVoyager = await GivenTheOwnerHolds(voyager, ATrialReport);
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            TheCallerIsAdminOf(voyager, zenith);
            TheOwnerIsDeleted(voyager);

            using var run = await RemoveEach(onVoyager, onZenith);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                Is.EqualTo(new[] { (Removed, RunReason.AlreadyGone), (Removed, (string?)null) }));
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task More_than_50_reports_are_refused_whole_and_nothing_is_removed()
        {
            var zenith = ATeam("Zenith");
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            TheCallerIsAdminOf(zenith);
            CountTheStoredReports();

            using var run = await RemoveEach([onZenith, .. Enumerable.Range(1000, Ceiling)]);

            await ThenTheWholeRequestIsRefusedAndNothingIsWritten(run);
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_removal_naming_no_report_is_refused()
        {
            var zenith = ATeam("Zenith");
            await GivenTheOwnerHolds(zenith, ATrialReport);
            TheCallerIsAdminOf(zenith);
            CountTheStoredReports();

            using var run = await RemoveEach();

            await ThenTheWholeRequestIsRefusedAndNothingIsWritten(run);
        }

        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task A_report_named_twice_is_removed_once_and_answered_once()
        {
            var zenith = ATeam("Zenith");
            var onZenith = await GivenTheOwnerHolds(zenith, ATrialReport);
            TheCallerIsAdminOf(zenith);

            using var run = await RemoveEach(onZenith, onZenith);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => (outcome.ReportId, outcome.Status, outcome.Code)),
                Is.EqualTo(new[] { ((int?)onZenith, Removed, (string?)null) }));
        }
    }
}
