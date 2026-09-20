using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// The day the simulation begins work on a Feature, read off forecasts that really ran.
    ///
    /// Every team here delivers the same number every day and works whatever sits nearest the top of its
    /// order, so each simulated run is the same run and a percentile is a day rather than a spread. That
    /// is what lets these assert which day rather than roughly when - the questions about spread are
    /// answered by the acceptance scenarios, which sample for real.
    /// </summary>
    [TestFixture]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public class ForecastServiceStartDayTest
    {
        private const int Trials = 50;

        private const string Ahead = "AHEAD";
        private const string Alpha = "ALPHA";
        private const string Bravo = "BRAVO";
        private const string Shared = "SHARED";

        // Three a day, so five items finish part-way through the second day and leave the team capacity
        // in that same day to pull whatever is below.
        private static readonly int[] ThreeADay = [3, 3, 3];
        private static readonly int[] TwoADay = [2, 2, 2];

        // A team the run admits no row for, because there is nothing to draw its delivery from.
        private static readonly int[] NothingMeasured = [];

        private Mock<IRepository<Feature>> featureRepository;
        private Mock<ITeamMetricsService> teamMetrics;

        [SetUp]
        public void Setup()
        {
            featureRepository = new Mock<IRepository<Feature>>();
            teamMetrics = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task AFeatureAtTheTopOfTheOrder_StartsOnTheFirstDay()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));

            await TheForecastRunsOver(alpha);

            Assert.That(TheDayItStarts(alpha), Is.EqualTo(1));
        }

        /// <summary>
        /// The Epic was framed on the assumption that the next Feature begins the day after the one above
        /// it ends. It does not: a team re-reads what it may work on after every item it delivers, so one
        /// that closes a Feature with capacity left in the day starts the next immediately.
        /// </summary>
        [Test]
        public async Task TheNextFeature_StartsTheDayTheOneAboveItFinishes()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));
            var bravo = AFeature(2, Bravo, (team, 4));

            await TheForecastRunsOver(alpha, bravo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheDayItFinishes(alpha), Is.EqualTo(2));
                Assert.That(TheDayItStarts(bravo), Is.EqualTo(2), "the same day, not the day after");
            }
        }

        /// <summary>
        /// A Feature is started once. Without that, every day a team works it would count as another
        /// start and the distribution would hold more entries than there were runs - which reads as a
        /// Feature that starts over and over and moves every percentile with it.
        /// </summary>
        [Test]
        public async Task AFeatureWorkedOnManyDays_IsRecordedAsStartingOnce()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 12));

            await TheForecastRunsOver(alpha);

            Assert.That(TheStartOf(alpha).TotalTrials, Is.EqualTo(Trials),
                "one start per run, no more and no fewer");
        }

        /// <summary>
        /// The marker that says a Feature has begun has to be cleared between runs. Left set, only the
        /// first run would ever record anything and the distribution would be built from one sample.
        /// </summary>
        [Test]
        public async Task EveryRun_RecordsItsOwnStart()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));
            var bravo = AFeature(2, Bravo, (team, 4));

            await TheForecastRunsOver(alpha, bravo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStartOf(alpha).TotalTrials, Is.EqualTo(Trials));
                Assert.That(TheStartOf(bravo).TotalTrials, Is.EqualTo(Trials));
            }
        }

        /// <summary>
        /// A Feature two teams share has begun as soon as either of them pulls an item, so its own start
        /// is the earlier of the two and not the later one, and not whichever row happens to come first.
        /// Here the second team reaches it on day one while the first is still finishing what is above it.
        /// </summary>
        [Test]
        public async Task AFeatureTwoTeamsShare_StartsWhenTheFirstOfThemStarts()
        {
            var busy = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var free = ATeamDelivering(2, TwoADay, featureWip: 1);

            var ahead = AFeature(1, Ahead, (busy, 5));
            var shared = AFeature(2, Shared, (busy, 6), (free, 4));

            await TheForecastRunsOver(ahead, shared);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheDayItStarts(shared, free), Is.EqualTo(1), "nothing is above it for this team");
                Assert.That(TheDayItStarts(shared, busy), Is.EqualTo(2), "this team is finishing the one above it");
                Assert.That(TheDayItStarts(shared), Is.EqualTo(1), "the earlier of the two, not the later");
            }
        }

        [Test]
        public async Task AFeatureOneTeamWorks_ReportsTheSameDayAtBothGrains()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));

            await TheForecastRunsOver(alpha);

            Assert.That(TheDayItStarts(alpha, team), Is.EqualTo(TheDayItStarts(alpha)));
        }

        /// <summary>
        /// The completion forecast is the most load-bearing number in the product, and a start row folded
        /// into the collection it aggregates would drag every completion date earlier without anything
        /// throwing. Asserted on the collection rather than on the dates, because that is the guarantee.
        /// </summary>
        [Test]
        public async Task StartDistributions_AreNotInTheCollectionTheCompletionForecastReads()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));

            await TheForecastRunsOver(alpha);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha.Forecasts, Has.Count.EqualTo(1), "one completion row, for the one team");
                Assert.That(alpha.StartForecasts, Has.Count.EqualTo(2), "one per team, plus the Feature's own");
                Assert.That(alpha.Forecast.GetProbability(85), Is.EqualTo(2),
                    "five items at three a day finishes on the second day, and nothing about that moved");
            }
        }

        /// <summary>
        /// A Feature the run admits no row for keeps nothing from the run before. Left alone it would go
        /// on reporting a day that came from a world it is no longer in - a team whose measured delivery
        /// has since gone.
        /// </summary>
        [Test]
        public async Task AFeatureTheRunCannotForecast_HasItsStartDistributionsCleared()
        {
            var silent = ATeamDelivering(9, NothingMeasured, featureWip: 1);
            var alpha = AFeature(1, Alpha, (silent, 5));
            alpha.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [7] = 1 }, null)]);

            await TheForecastRunsOver(alpha);

            Assert.That(alpha.StartForecasts, Is.Empty);
        }

        [Test]
        public async Task ThePerTeamRow_NamesItsTeam()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var alpha = AFeature(1, Alpha, (team, 5));

            await TheForecastRunsOver(alpha);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha.StartForecasts.Count(forecast => forecast.TeamId == team.Id), Is.EqualTo(1));
                Assert.That(alpha.StartForecasts.Count(forecast => forecast.TeamId is null), Is.EqualTo(1));
            }
        }

        // --- the plan's row-to-Feature index, which the recorder is indexed by ---

        [Test]
        public void ThePlan_NumbersEachFeatureOnceHoweverManyRowsItHas()
        {
            var busy = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var free = ATeamDelivering(2, TwoADay, featureWip: 1);
            var shared = AFeature(1, Shared, (busy, 6), (free, 4));

            var plan = APlanOver(shared);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(plan.RowCount, Is.EqualTo(2));
                Assert.That(plan.FeatureCount, Is.EqualTo(1));
                Assert.That(plan.FeatureOf(0), Is.EqualTo(plan.FeatureOf(1)), "both rows are the same Feature");
                Assert.That(plan.FeatureAt(plan.FeatureOf(0)), Is.SameAs(shared));
            }
        }

        /// <summary>
        /// Two Features that have never been imported share an empty reference id. Numbering by the
        /// Feature itself rather than by that id is what stops them being folded together and reported as
        /// starting on each other's day.
        /// </summary>
        [Test]
        public void ThePlan_KeepsTwoFeaturesApartEvenWithoutReferenceIds()
        {
            var team = ATeamDelivering(1, ThreeADay, featureWip: 1);
            var first = AFeature(1, string.Empty, (team, 3));
            var second = AFeature(2, string.Empty, (team, 3));

            var plan = APlanOver(first, second);

            Assert.That(plan.FeatureCount, Is.EqualTo(2));
        }

        // --- helpers ---

        private Team ATeamDelivering(int id, int[] history, int featureWip)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = featureWip };

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(team, It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(RunChartDataGenerator.GenerateRunChartData(history)), false, null));

            return team;
        }

        private static Feature AFeature(int id, string referenceId, params (Team team, int remaining)[] work)
            => new(work.Select(entry => (entry.team, entry.remaining, entry.remaining)))
            {
                Id = id,
                ReferenceId = referenceId,
                Name = referenceId,
            };

        private async Task TheForecastRunsOver(params Feature[] features)
        {
            featureRepository.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetrics.Object,
                featureRepository.Object,
                new NothingWaitsForAnything(),
                new DrawsTheSameNumberEveryTime(),
                new ForecastSimulationLimits(Trials, ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

            await forecastService.UpdateForecastsForPortfolio(portfolio);
        }

        private ForecastRunPlan APlanOver(params Feature[] features)
        {
            var rows = features
                .SelectMany(feature => feature.FeatureWork.Select(work => new SimulationResult(work.Team, feature, work.RemainingWorkItems)))
                .ToList();

            var throughput = rows
                .Select(row => row.Team)
                .Distinct()
                .ToDictionary(team => team.Id, team => teamMetrics.Object.GetForecastThroughputStatus(team).Throughput);

            return ForecastRunPlan.For(rows, throughput, Lighthouse.Backend.Models.Dependencies.ForecastWaits.Nothing);
        }

        private static StartForecast TheStartOf(Feature feature, Team? team = null)
            => feature.StartForecasts.Single(forecast => forecast.TeamId == team?.Id);

        private static int TheDayItStarts(Feature feature, Team? team = null)
            => TheStartOf(feature, team).GetProbability(85);

        private static int TheDayItFinishes(Feature feature) => feature.Forecast.GetProbability(85);
    }
}
