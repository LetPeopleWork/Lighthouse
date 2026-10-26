using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// Cost is asserted as what the database handed back, never as time, so these scenarios do not flake on a
    /// loaded machine; the goldens keep the bodies the clients parse exactly as they are.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-02")]
    [Category("us-02")]
    [Category("story-6406")]
    public class Slice02OverviewReadsNamesAndCountsScenarios : FastWithManyFeaturesAcceptanceTest
    {
        private const string ThePortfoliosList = "the Portfolios list";
        private const string TheTeamsList = "the Teams list";
        private const string OneTeam = "one Team";
        private const string OnePortfolio = "one Portfolio";
        private const string OnePortfoliosSettings = "one Portfolio's settings";

        private const int FeaturesOfNorthwindPayments = 2;

        private const string AConcurrencyToken = "concurrencyToken";

        /// <summary>
        /// Every body the Overview and the detail pages read, held against the copy approved before the slice.
        /// Lighthouse-Clients and an open browser parse these, so not one field may move.
        /// </summary>
        [TestCase(ThePortfoliosList)]
        [TestCase(TheTeamsList)]
        [TestCase(OneTeam)]
        [TestCase(OnePortfolio)]
        [TestCase(OnePortfoliosSettings)]
        [Category("driving_port")]
        [Category("guardrail-G3")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Every_name_and_count_the_Overview_and_the_detail_pages_read_is_exactly_as_before(string whatIsOpened)
        {
            var northwind = GivenTheNorthwindInstance();

            var body = await TheBodyOf(TheRouteTo(whatIsOpened, northwind));

            ApprovedBodies.Verify(body, ApprovedBodiesFolder, $"slice02-{TheApprovedNameOf(whatIsOpened)}", AConcurrencyToken);
        }

        [TestCase(ThePortfoliosList)]
        [TestCase(TheTeamsList)]
        [TestCase(OneTeam)]
        [TestCase(OnePortfolio)]
        [TestCase(OnePortfoliosSettings)]
        [Ignore(PendingEpic6400.Slice02)]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-navigation-and-settings-answer")]
        [Category("contract-shape:pure-function")]
        public async Task Naming_and_counting_Portfolios_and_Teams_reads_no_forecast_at_all(string whatIsOpened)
        {
            var northwind = GivenTheNorthwindInstance();

            await TheAnswerCountingWhatTheDatabaseHandsBack(TheRouteTo(whatIsOpened, northwind));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Materialised.Of<ForecastBase>(), Is.Zero,
                    $"Opening {whatIsOpened} read forecasts, which it never shows. Read: {Materialised.Describe()}");
                Assert.That(Materialised.Of<IndividualSimulationResult>(), Is.Zero,
                    $"Opening {whatIsOpened} read forecast histograms, which it never shows. Read: {Materialised.Describe()}");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("kpi-OUT-6400-navigation-and-settings-answer")]
        [Category("contract-shape:pure-function")]
        public async Task Opening_Northwind_Payments_reads_Northwind_Payments_and_its_Features_not_the_rest_of_the_instance()
        {
            var northwind = GivenTheNorthwindInstance();

            var answer = await TheAnswerCountingWhatTheDatabaseHandsBack($"/api/latest/portfolios/{northwind.Payments}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.GetProperty("name").GetString(), Is.EqualTo("Northwind Payments"));
                Assert.That(Materialised.Of<Portfolio>(), Is.EqualTo(1),
                    $"Opening one Portfolio read other Portfolios too. Read: {Materialised.Describe()}");
                Assert.That(Materialised.Of<Feature>(), Is.EqualTo(FeaturesOfNorthwindPayments),
                    $"Opening Northwind Payments read Features it does not hold. Read: {Materialised.Describe()}");
            }
        }

        private static string TheRouteTo(string whatIsOpened, Northwind northwind) => whatIsOpened switch
        {
            ThePortfoliosList => "/api/latest/portfolios",
            TheTeamsList => "/api/latest/teams",
            OneTeam => $"/api/latest/teams/{northwind.Atlas}",
            OnePortfolio => $"/api/latest/portfolios/{northwind.Platform}",
            OnePortfoliosSettings => $"/api/latest/portfolios/{northwind.Platform}/settings",
            _ => throw new ArgumentOutOfRangeException(nameof(whatIsOpened), whatIsOpened, "Not a page this slice reads."),
        };

        private static string TheApprovedNameOf(string whatIsOpened) => whatIsOpened switch
        {
            ThePortfoliosList => "portfolios",
            TheTeamsList => "teams",
            OneTeam => "team-atlas",
            OnePortfolio => "portfolio-northwind-platform",
            OnePortfoliosSettings => "portfolio-northwind-platform-settings",
            _ => throw new ArgumentOutOfRangeException(nameof(whatIsOpened), whatIsOpened, "Not a page this slice reads."),
        };
    }
}
