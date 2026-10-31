using System.Text.Json;
using System.Text.Json.Nodes;

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

        private const string TheAnswerTheBrowserIsCheckedAgainst = "Lighthouse.Frontend/src/tests/bulkCreationAnswer.json";

        private static readonly string[] IdsThatDifferPerDatabase = ["ownerId", "reportId"];

        [Test]
        public async Task Every_row_names_its_owner_whether_or_not_a_report_was_created_and_a_gone_owner_has_no_name()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var apollo = APortfolioKeepingFinishedWorkSince("Apollo", EquinoxKeepsFinishedWorkSince);
            var gravity = ATeam("Gravity");
            TheCallerIsAdminOf(zenith, apollo, gravity);
            TheOwnerIsDeletedWhenTheRunReaches(gravity);

            using var run = await CreateForEach(TheQuartersComparison, zenith, apollo, gravity);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.OwnerName)),
                Is.EqualTo(new[] { (Created, "Zenith"), (NotCreated, "Apollo"), (NotCreated, (string?)null) }));
        }

        // The browser's result dialog is tested against a copy of this answer, so the copy has to stay what the server really sends.
        [Test]
        public async Task The_answer_is_the_one_the_browsers_result_dialog_is_tested_against()
        {
            TheInstanceHasAPremiumLicence();
            var zenith = ATeam("Zenith");
            var apollo = APortfolioKeepingFinishedWorkSince("Apollo", EquinoxKeepsFinishedWorkSince);
            TheCallerIsAdminOf(zenith, apollo);

            using var run = await CreateForEach(TheQuartersComparison, zenith, apollo);
            var answer = await AnswerOf(run);

            var path = Path.Combine(RepositoryRoot(), TheAnswerTheBrowserIsCheckedAgainst.Replace('/', Path.DirectorySeparatorChar));
            using var copy = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            Assert.That(WithTheIdsBlanked(answer), Is.EqualTo(WithTheIdsBlanked(copy.RootElement)));
        }

        private static string WithTheIdsBlanked(JsonElement answer)
        {
            var blanked = JsonNode.Parse(answer.GetRawText())!;
            foreach (var outcome in blanked["outcomes"]!.AsArray().OfType<JsonObject>())
            {
                foreach (var id in IdsThatDifferPerDatabase.Where(id => outcome[id] is not null))
                {
                    outcome[id] = 0;
                }
            }

            return blanked.ToJsonString();
        }

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
