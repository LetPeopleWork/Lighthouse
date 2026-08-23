using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// What waiting on another Feature does to a date, read off forecasts that really ran. Every comparison
    /// here is between two runs of the same build against the same pinned starting number: production draws
    /// from a fresh source each run by deliberate choice, so two unpinned runs over identical data return
    /// different percentiles and any assertion over them would pass on sampling noise alone.
    ///
    /// One Team throughout, and a work-in-progress limit of two over three Features. That is the smallest
    /// shape in which leaving a Feature out of the running has anywhere to send the capacity: with a limit
    /// of one the Team works down the list in order whatever is waiting, and nothing can be observed.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    public class ForecastServiceDependencyTest
    {
        private const int TheSeedEveryComparisonShares = 20260823;

        private const int TheWorkInProgressLimit = 2;

        private static readonly int[] TheThroughputEveryRunForecastsFrom =
            [2, 0, 1, 3, 1, 0, 2, 1, 0, 4, 1, 1, 0, 2, 1, 0, 1, 3, 0, 1];

        private static readonly int[] ThePercentilesLighthouseShows = [50, 70, 85, 95];

        private const string First = "F-1";
        private const string Second = "F-2";
        private const string Third = "F-3";

        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        /// <summary>
        /// Asserted at every percentile rather than in each simulated run, because the run-by-run detail is
        /// not in the output. It says the same thing: a Feature that can only finish after another one has
        /// a completion distribution that is nowhere to the left of it.
        /// </summary>
        [Test]
        public async Task AFeatureWaitingOnAnother_NeverFinishesBeforeIt()
        {
            var dates = await TheDatesWith(WaitingOn(Second, First));

            Assert.That(NeverFinishesBefore(dates, Second, First), Is.True,
                $"F-2 waits on F-1 and finished earlier in some of the runs. Read: {Read(dates)}");
        }

        /// <summary>
        /// The scenario that tells accounting for a dependency apart from postponing a date afterwards. A
        /// postponement moves the Feature that is waiting and leaves everything else alone; leaving it out of
        /// the running gives the capacity it could not use to the Features below it, and they move in.
        /// </summary>
        [Test]
        public async Task TheCapacityAWaitingFeatureCouldNotUse_GoesToTheOneBelowIt()
        {
            var withNothingWaiting = await TheDatesWith(ForecastWaits.Nothing);
            var withTheWait = await TheDatesWith(WaitingOn(Second, First));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withTheWait[Second][85], Is.GreaterThan(withNothingWaiting[Second][85]),
                    $"F-2 waits on F-1 and its date did not move out. Before: {Read(withNothingWaiting)} After: {Read(withTheWait)}");

                Assert.That(withTheWait[Third][85], Is.LessThan(withNothingWaiting[Third][85]),
                    "F-3 sits below the Feature that had to wait and did not move in, so the capacity went " +
                    $"nowhere. Before: {Read(withNothingWaiting)} After: {Read(withTheWait)}");
            }
        }

        /// <summary>
        /// Two runs of one build, so nothing here can go stale against a number somebody wrote down. The
        /// second assertion is what stops the first passing vacuously: a build in which the waits never
        /// reach the simulation satisfies "unchanged" perfectly.
        /// </summary>
        [Test]
        public async Task WithNothingWaitingOnAnything_EveryPercentileIsWhatItAlwaysWas()
        {
            var oneRun = await TheDatesWith(ForecastWaits.Nothing);
            var theSameRunAgain = await TheDatesWith(ForecastWaits.Nothing);
            var aRunWithAWaitInIt = await TheDatesWith(WaitingOn(Second, First));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Read(theSameRunAgain), Is.EqualTo(Read(oneRun)),
                    "Two runs of one build against one starting number produced different dates, so nothing " +
                    "compared in this fixture means anything.");

                Assert.That(Read(aRunWithAWaitInIt), Is.Not.EqualTo(Read(oneRun)),
                    "Recording a wait changed no date at all, so the waits are not reaching the simulation " +
                    "and every other assertion here is passing over a mechanic that is not running.");
            }
        }

        /// <summary>
        /// A Feature waited on that has no work left is already finished, so it holds nothing up. The second
        /// half is what stops this passing on a build where the mechanic is simply absent.
        /// </summary>
        [Test]
        public async Task WaitingOnAFeatureWithNoWorkLeft_HoldsNothingUp()
        {
            var withNothingWaiting = await TheDatesWith(ForecastWaits.Nothing, TheFirstIsFinished);
            var waitingOnTheFinishedOne = await TheDatesWith(WaitingOn(Second, First), TheFirstIsFinished);
            var waitingOnOneWithWorkLeft = await TheDatesWith(WaitingOn(Second, Third), TheFirstIsFinished);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Read(waitingOnTheFinishedOne), Is.EqualTo(Read(withNothingWaiting)),
                    "F-1 has nothing left to do, so waiting on it is waiting on nothing and no date may move.");

                Assert.That(Read(waitingOnOneWithWorkLeft), Is.Not.EqualTo(Read(withNothingWaiting)),
                    "A wait on a Feature that does still have work left moved nothing either, so the mechanic " +
                    "was not running while it left the finished one alone.");
            }
        }

        /// <summary>
        /// A circle can only reach the simulation if the one decision that produces these waits has broken.
        /// The loop cannot work its way out of one - what a Feature waits on clears only when somebody
        /// finishes it - so it stops rather than counting simulated days on a background thread forever.
        /// </summary>
        [Test]
        public async Task WaitsThatGoRoundInACircle_StopTheRunRatherThanSpinningForever()
        {
            var logger = new Mock<ILogger<ForecastService>>();

            var dates = await TheDatesWith(
                WaitingOn(Second, First).And(First, Second), TheThreeFeatures, logger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dates[Third][85], Is.GreaterThan(0),
                    "F-3 is in no circle and still had to be forecast.");

                Assert.That(TheErrorsLogged(logger), Is.Not.Empty,
                    "The run gave up on some of its trials and said nothing, so an operator reading logs has " +
                    "no way to know the dates on screen are not a forecast.");
            }
        }

        /// <summary>
        /// A Feature can hold two rows for one Team - duplicate pairs are a state the store can be in, and
        /// the Feature model guards against them elsewhere for the same reason. Both ends have to survive
        /// it: every row of the Feature waited on has to reach zero before the wait clears, and every row of
        /// the Feature waiting has to be held while it has not. Taking one row per Feature on either side
        /// lets work happen while what it waits on is unfinished, and the date that comes out of that looks
        /// exactly like any other date.
        /// </summary>
        [Test]
        public async Task AFeatureHoldingTwoRowsForOneTeam_IsWaitedOnUntilBothOfThemAreDone()
        {
            var dates = await TheDatesWith(WaitingOn(Second, First), TheFirstIsCountedTwice);

            Assert.That(NeverFinishesBefore(dates, Second, First), Is.True,
                "F-2 finished before F-1 was done, so only one of F-1's two rows is being waited on. " +
                $"Read: {Read(dates)}");
        }

        /// <summary>
        /// Dominance at every percentile. A Feature that can only finish after another one finished no
        /// earlier than it in any run, so its whole completion distribution is nowhere to the left of the
        /// other's - which is a claim about the wait rather than about how much work is in the run, and
        /// therefore not satisfied by a fixture merely having more to get through.
        /// </summary>
        private static bool NeverFinishesBefore(
            Dictionary<string, Dictionary<int, int>> dates, string waiting, string waitedOn)
            => ThePercentilesLighthouseShows.All(percentile => dates[waiting][percentile] >= dates[waitedOn][percentile]);

        private static List<object[]> TheErrorsLogged(Mock<ILogger<ForecastService>> logger)
            => logger.Invocations
                .Where(invocation => invocation.Arguments.Contains(LogLevel.Error))
                .Select(invocation => invocation.Arguments.ToArray())
                .ToList();

        private Task<Dictionary<string, Dictionary<int, int>>> TheDatesWith(ForecastWaits waits)
            => TheDatesWith(waits, TheThreeFeatures);

        private Task<Dictionary<string, Dictionary<int, int>>> TheDatesWith(
            ForecastWaits waits, Func<Team, List<Feature>> theFeatures)
            => TheDatesWith(waits, theFeatures, new Mock<ILogger<ForecastService>>());

        private async Task<Dictionary<string, Dictionary<int, int>>> TheDatesWith(
            ForecastWaits waits, Func<Team, List<Feature>> theFeatures, Mock<ILogger<ForecastService>> logger)
        {
            var team = new Team { Id = 1, Name = "Team", FeatureWIP = TheWorkInProgressLimit };
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(TheThroughputEveryRunForecastsFrom));

            teamMetricsServiceMock
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(throughput, false, null));

            var features = theFeatures(team);
            featureRepositoryMock.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new SeededRandomNumberService(TheSeedEveryComparisonShares),
                logger.Object,
                teamMetricsServiceMock.Object,
                featureRepositoryMock.Object,
                new WaitsHandedStraightToTheForecast(waits),
                new DrawsFromAPinnedStartingNumber(TheSeedEveryComparisonShares));

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return features.ToDictionary(
                feature => feature.ReferenceId,
                feature => ThePercentilesLighthouseShows.ToDictionary(
                    percentile => percentile,
                    feature.Forecast.GetProbability));
        }

        private static List<Feature> TheThreeFeatures(Team team) => TheFeatures(team, [7, 5, 9]);

        private static List<Feature> TheFirstIsFinished(Team team) => TheFeatures(team, [0, 5, 9]);

        /// <summary>
        /// F-1 holds nearly all of its work on a second row for the same Team. The first row is one item, so
        /// a run that waits on only one of them releases F-2 almost immediately - which is what makes this
        /// fixture able to tell the two apart at all. A fixture whose first row carried most of the work
        /// would land F-2 late either way and prove nothing.
        ///
        /// The row is added the way the store holds one rather than through AddOrUpdateWorkForTeam, which
        /// exists to heal exactly this and would undo the fixture.
        /// </summary>
        private static List<Feature> TheFirstIsCountedTwice(Team team)
        {
            var features = TheFeatures(team, [1, 3, 9]);
            var first = features.Single(candidate => candidate.ReferenceId == First);

            first.FeatureWork.Add(new FeatureWork(team, 15, 15, first));

            return features;
        }

        private static List<Feature> TheFeatures(Team team, int[] remainingWorkPerFeature)
            => remainingWorkPerFeature
                .Select((remainingItems, index) => new Feature(team, remainingItems)
                {
                    Id = index + 1,
                    Name = $"Feature {index + 1}",
                    ReferenceId = $"F-{index + 1}",
                })
                .ToList();

        private static Waits WaitingOn(string dependent, string blocker) => new Waits().And(dependent, blocker);

        private static string Read(Dictionary<string, Dictionary<int, int>> dates)
            => string.Join(" | ", dates.Select(feature =>
                $"{feature.Key}: {string.Join("/", feature.Value.Select(percentile => $"{percentile.Key}%={percentile.Value}"))}"));

        /// <summary>
        /// Waits written out by hand, so a scenario says which Features wait on which and nothing else has to
        /// be true for it to run. What decides them has its own tests; this one is about what the simulation
        /// does once it has been told.
        /// </summary>
        private sealed class Waits
        {
            private readonly List<DependencyVerdict> honoured = [];

            public Waits And(string dependent, string blocker)
            {
                honoured.Add(new DependencyVerdict(dependent, blocker, reason: null, blockerPositionedBelow: false));
                return this;
            }

            public static implicit operator ForecastWaits(Waits waits)
                => ForecastWaits.From(new HonouredDependencies(waits.honoured));
        }

        private sealed class WaitsHandedStraightToTheForecast(ForecastWaits waits) : IWhatTheForecastWaitsFor
        {
            public ForecastWaits Of(IReadOnlyCollection<Feature> featuresBeingForecast) => waits;
        }
    }
}
