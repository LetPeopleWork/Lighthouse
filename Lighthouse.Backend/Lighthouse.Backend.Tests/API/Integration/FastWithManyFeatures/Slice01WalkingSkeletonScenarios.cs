using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// Every slice of the Epic changes the work behind these answers and never the answers, so this stays green from
    /// the Epic's first commit to its last. The draws are held part-way through the round, rather than timed, so the
    /// reads land while it runs on any machine.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("story-6404")]
    public class Slice01WalkingSkeletonScenarios : ForecastWhileItUpdatesAcceptanceTest
    {
        private static readonly TimeSpan TheLongestAPageMayTake = TimeSpan.FromSeconds(10);

        private const string TheFeature = "Billing Migration";

        /// <summary>
        /// Seven runs in ten finish, on the fifth day; the other three never do and leave no row. The histogram is the
        /// forecast as it was computed before the Epic, recorded once and held from then on.
        /// </summary>
        private const string TheForecastAsComputedBeforeTheEpic = "400 runs [5:280]";

        [Test]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-no-stall-during-rounds")]
        [Category("contract-shape:bounded-change")]
        public async Task Martin_opens_a_Portfolio_and_its_Features_while_its_forecast_round_runs_and_reads_their_dates_once_it_ends()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);

            WhenThePortfolioForecastIsUnderWay(portfolio);
            var thePortfolio = await TheAnswerToAReadOf($"/api/latest/portfolios/{portfolio.Id}");
            var itsFeatures = await TheAnswerToAReadOf($"/api/latest/features/ids?featureIds={TheIdOf(TheFeature)}");
            var runsBegunWhileTheyWereRead = Draws.RunsBegun;
            await TheForecastIsLetGoAndSettles(portfolio);

            var feature = await TheFeatureAsTheClientSeesIt(TheFeature);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(thePortfolio, Is.EqualTo(HttpStatusCode.OK), "The Portfolio did not answer while its forecast round ran.");
                Assert.That(itsFeatures, Is.EqualTo(HttpStatusCode.OK), "Its Features did not answer while the forecast round ran.");
                Assert.That(runsBegunWhileTheyWereRead, Is.LessThan(TheRunsAForecastMakes),
                    "The round had finished before the reads landed, so this proved nothing about reading during one.");
                Assert.That(ThePercentile(feature, 50).GetProperty("expectedDate").GetString(), Is.Not.Null.And.Not.Empty,
                    "Once the round ended, the Feature should carry its forecast dates.");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("guardrail-G1")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_forecast_reads_the_same_wherever_its_round_runs()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            GivenRunsThatFinish(inEveryTen: 7);

            await RunUpdate(sp => sp.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id));

            Assert.That(TheStoredForecastOf(portfolio).Histogram, Is.EqualTo(TheForecastAsComputedBeforeTheEpic));
        }

        private async Task<HttpStatusCode?> TheAnswerToAReadOf(string route)
        {
            using var client = Factory.CreateClient();
            var read = client.GetAsync(new Uri(route, UriKind.Relative));

            if (await Task.WhenAny(read, Task.Delay(TheLongestAPageMayTake)) != read)
            {
                return null;
            }

            using var response = await read;
            return response.StatusCode;
        }

        private int TheIdOf(string featureName)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Feature>>().GetAll().Single(feature => feature.Name == featureName).Id;
        }
    }
}
