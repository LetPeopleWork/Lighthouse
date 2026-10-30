namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Teams and Portfolios in one bulk creation, on the same windows. Every rule a Team meets in a run, a
    /// Portfolio meets too: Admin of it, a kept history long enough, still there when the run reaches it.
    ///
    /// Driving ports: POST /api/latest/reports/bulk, GET /api/latest/reports/creation-options; the per-owner
    /// GET /portfolios/{id}/reports tells what a run left behind.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("slice-03")]
    public class Slice03PortfoliosJoinTheSelectionScenarios : BulkReportsAcceptanceTest
    {
        private static readonly string[] OnlyTheQuarter = [TheQuarter];

        [Test]
        public async Task Teams_and_a_Portfolio_get_the_same_comparison_in_one_run()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var oceanExplorer = APortfolio("Ocean Explorer");
            TheCallerIsAdminOf(zenith, gravity, oceanExplorer);

            using var run = await CreateForEach(TheQuartersComparison, zenith, gravity, oceanExplorer);
            var outcomes = await TheOutcomesOf(run);

            var portfoliosReport = (await ReportsOn(oceanExplorer)).Single();
            var zenithsReport = (await ReportsOn(zenith)).Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Has.All.EqualTo(Created));
                Assert.That(outcomes[2].OwnerKind, Is.EqualTo("portfolio"));
                Assert.That(TextOf(portfoliosReport, "name"), Is.EqualTo(TheQuarter));
                Assert.That(ThenWindowIn(portfoliosReport), Is.EqualTo(ThenWindowIn(zenithsReport)));
                Assert.That(NowWindowIn(portfoliosReport), Is.EqualTo(NowWindowIn(zenithsReport)));
            }
        }

        [Test]
        public async Task A_Portfolio_she_only_reads_is_not_created_on()
        {
            TheInstanceHasAPremiumLicence();
            var oceanExplorer = APortfolio("Ocean Explorer");
            var apollo = APortfolio("Apollo");
            TheCallerIsAdminOf(oceanExplorer);
            TheCallerOnlyReadsEach(apollo);

            using var run = await CreateForEach(TheQuartersComparison, oceanExplorer, apollo);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Code), Is.EqualTo(new[] { null, RunReason.NotAdmin }));
                Assert.That(await ReportNamesOn(apollo), Is.Empty);
            }
        }

        [Test]
        public async Task A_run_of_Portfolios_only_creates_on_each()
        {
            TheInstanceHasAPremiumLicence();
            var oceanExplorer = APortfolio("Ocean Explorer");
            var neuroLinkCity = APortfolio("NeuroLink City");
            TheCallerIsAdminOf(oceanExplorer, neuroLinkCity);

            using var run = await CreateForEach(TheQuartersComparison, oceanExplorer, neuroLinkCity);
            await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await ReportNamesOn(oceanExplorer), Is.EqualTo(OnlyTheQuarter));
                Assert.That(await ReportNamesOn(neuroLinkCity), Is.EqualTo(OnlyTheQuarter));
            }
        }

        [Test]
        public async Task A_Portfolio_whose_kept_history_is_too_short_is_skipped_with_the_day_its_history_starts()
        {
            TheInstanceHasAPremiumLicence();
            var oceanExplorer = APortfolioKeepingFinishedWorkSince("Ocean Explorer", EquinoxKeepsFinishedWorkSince);
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(oceanExplorer, zenith);
            var whatTheSingleCreateSays = await TheHistoryStartTheSingleCreateNames(oceanExplorer, TheQuartersComparison);

            using var run = await CreateForEach(TheQuartersComparison, oceanExplorer, zenith);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes[0].Code, Is.EqualTo(Reason.ThenBeforeCutoff));
                Assert.That(outcomes[0].EarliestStart, Is.Not.Null.And.EqualTo(whatTheSingleCreateSays));
                Assert.That(outcomes[1].Status, Is.EqualTo(Created));
            }
        }

        [Test]
        public async Task A_Portfolio_deleted_while_the_run_is_under_way_is_one_row()
        {
            TheInstanceHasAPremiumLicence();
            var oceanExplorer = APortfolio("Ocean Explorer");
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(oceanExplorer, zenith);
            TheOwnerIsDeletedWhenTheRunReaches(oceanExplorer);

            using var run = await CreateForEach(TheQuartersComparison, oceanExplorer, zenith);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                Is.EqualTo(new[] { (NotCreated, RunReason.OwnerNotFound), (Created, (string?)null) }));
        }

        [Test]
        public async Task Each_owners_history_start_in_the_creation_options_is_the_one_its_own_refusal_names()
        {
            var equinox = ATeamKeepingFinishedWorkSince("Equinox", EquinoxKeepsFinishedWorkSince);
            var oceanExplorer = APortfolioKeepingFinishedWorkSince("Ocean Explorer", EquinoxKeepsFinishedWorkSince);
            var teamsRefusal = await TheHistoryStartTheSingleCreateNames(equinox, TheQuartersComparison);
            var portfoliosRefusal = await TheHistoryStartTheSingleCreateNames(oceanExplorer, TheQuartersComparison);

            var options = await TheCreationOptions();
            var starts = options.GetProperty("owners").EnumerateArray()
                .ToDictionary(owner => TextOf(owner, "ownerName") ?? "", owner => TextOf(owner, "earliestStart"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(starts["Equinox"], Is.Not.Null.And.EqualTo(teamsRefusal));
                Assert.That(starts["Ocean Explorer"], Is.Not.Null.And.EqualTo(portfoliosRefusal));
            }
        }
    }
}
