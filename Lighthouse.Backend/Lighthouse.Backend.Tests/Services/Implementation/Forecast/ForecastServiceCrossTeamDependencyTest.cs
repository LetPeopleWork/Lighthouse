using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// What a wait on another Team's work does to a date - the kind of dependency most real ones are, and
    /// the one Lighthouse used to leave out.
    ///
    /// Every Team here delivers the same amount every single day, so a date is not a sample of anything: it
    /// can be worked out by hand and asserted exactly. That is on purpose. A Team held up by another Team is
    /// the place a wrong answer would look most ordinary, and a comparison with a tolerance around it could
    /// not tell "right" from "wrong by less than the tolerance".
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class ForecastServiceCrossTeamDependencyTest
    {
        private const string TheOneThatWaits = "F-1";
        private const string TheOneWaitedOn = "F-2";
        private const string TheThirdOne = "F-3";

        private static readonly int[] ThePercentilesLighthouseShows = [50, 70, 85, 95];

        private Mock<IRepository<Feature>> featureRepositoryMock;

        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        /// <summary>
        /// The whole point of the slice, and its arithmetic is worth spelling out. The Team waited on gets
        /// through six items at one a day and is done on day six. The Team waiting has four items of its own
        /// and could have been done on day four, but may not start until day six is over, so it finishes on
        /// day ten.
        ///
        /// Ten rather than six is also what says the six days it spent waiting were not banked. A Team that
        /// could start nothing today has not delivered today; carrying that day forward would hand the wait
        /// back exactly the time it cost, and the date would come out looking perfectly ordinary.
        /// </summary>
        [Test]
        public async Task AFeatureWaitingOnAnotherTeamsWork_StartsOnlyWhenThatWorkIsDone()
        {
            var withNothingWaiting = await TheDatesWith(NothingWaits, TwoTeamsOneEach);
            var withTheWait = await TheDatesWith(WaitingOn(TheOneThatWaits, TheOneWaitedOn), TwoTeamsOneEach);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EveryPercentileOf(withNothingWaiting, TheOneWaitedOn), Is.All.EqualTo(6));
                Assert.That(EveryPercentileOf(withNothingWaiting, TheOneThatWaits), Is.All.EqualTo(4));

                Assert.That(EveryPercentileOf(withTheWait, TheOneWaitedOn), Is.All.EqualTo(6),
                    "The Feature waited on was itself delayed, so the wait is doing something other than " +
                    $"holding back the Feature that waits. Read: {Read(withTheWait)}");

                Assert.That(EveryPercentileOf(withTheWait, TheOneThatWaits), Is.All.EqualTo(10),
                    "The Feature waiting on another Team's work did not sit behind it and then take its own " +
                    "four days. Six would mean the wait was ignored; seven would mean the days spent waiting " +
                    $"were banked and handed back. Read: {Read(withTheWait)}");
            }
        }

        /// <summary>
        /// A shared clock shares time, never delivery. The Team waited on gets through nine items at three a
        /// day and is done on day three; the Team waiting has four items at one a day and finishes on day
        /// seven. Had it picked up the other Team's rate on release it would have finished on day five, and
        /// a forecast quietly borrowing another Team's delivery is the worst bug this change could have.
        /// </summary>
        [Test]
        public async Task ATeamHeldUpByAFasterOne_StillDeliversAtItsOwnRate()
        {
            var dates = await TheDatesWith(WaitingOn(TheOneThatWaits, TheOneWaitedOn), OneTeamThreeTimesFaster);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EveryPercentileOf(dates, TheOneWaitedOn), Is.All.EqualTo(3));

                Assert.That(EveryPercentileOf(dates, TheOneThatWaits), Is.All.EqualTo(7),
                    "The Team that was waiting delivered at a rate that is not its own once it was released. " +
                    $"Read: {Read(dates)}");
            }
        }

        /// <summary>
        /// A Feature three Teams are working is finished when the last of them is done, not the first. The
        /// three get through two, six and three items at one a day, so the Feature is done on day six; the
        /// Feature waiting then takes its own four days and lands on day ten. Released by the first Team to
        /// stop, it would have landed on day six.
        /// </summary>
        [Test]
        public async Task WaitingOnAFeatureSeveralTeamsAreWorking_WaitsForTheLastOfThemRatherThanTheFirst()
        {
            var dates = await TheDatesWith(WaitingOn(TheOneThatWaits, TheOneWaitedOn), TheOneWaitedOnIsSharedByThreeTeams);

            Assert.That(EveryPercentileOf(dates, TheOneThatWaits), Is.All.EqualTo(10),
                "The Feature waiting was released before every Team working what it waits on had finished. " +
                $"Read: {Read(dates)}");
        }

        /// <summary>
        /// A circle across two Teams is a circle like any other. It cannot work its way out - what a Feature
        /// waits on clears only when somebody finishes it - so the run has to end rather than count simulated
        /// days on a background thread for good, and somebody has to be told.
        /// </summary>
        [Test]
        public async Task ACircleThatCrossesTeams_StopsTheRunAndSaysSo()
        {
            var logger = new Mock<ILogger<ForecastService>>();

            var dates = await TheDatesWith(
                WaitingOn(TheOneThatWaits, TheOneWaitedOn).And(TheOneWaitedOn, TheOneThatWaits),
                TwoTeamsOneEach,
                logger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EveryPercentileOf(dates, TheThirdOne), Is.All.GreaterThan(0),
                    "The Feature in no circle still had to be forecast.");

                Assert.That(TheErrorsLogged(logger), Is.Not.Empty,
                    "The run gave up on its simulated runs and said nothing, so an operator reading logs has " +
                    "no way to know the dates on screen are not a forecast.");
            }
        }

        /// <summary>
        /// A Feature on a Team with nothing measured has no place in the run at all, so there is nothing
        /// there to wait for. The decision that produces waits drops such a wait and says so, which is where
        /// the reader learns about it; what matters here is only that a forecast handed one anyway still
        /// reaches an end rather than waiting for something that will never arrive.
        /// </summary>
        [Test]
        public async Task WaitingOnAFeatureWhoseTeamHasNothingMeasured_StillReachesAnEnd()
        {
            var dates = await TheDatesWith(WaitingOn(TheOneThatWaits, TheOneWaitedOn), TheOneWaitedOnHasNothingMeasured);

            Assert.That(EveryPercentileOf(dates, TheOneThatWaits), Is.All.EqualTo(4),
                $"The forecast did not reach an end, or held the waiting Feature back. Read: {Read(dates)}");
        }

        private static ForecastWaits NothingWaits => ForecastWaits.Nothing;

        private static IEnumerable<int> EveryPercentileOf(Dictionary<string, Dictionary<int, int>> dates, string referenceId)
            => ThePercentilesLighthouseShows.Select(percentile => dates[referenceId][percentile]);

        private static List<object[]> TheErrorsLogged(Mock<ILogger<ForecastService>> logger)
            => logger.Invocations
                .Where(invocation => invocation.Arguments.Contains(LogLevel.Error))
                .Select(invocation => invocation.Arguments.ToArray())
                .ToList();

        private Task<Dictionary<string, Dictionary<int, int>>> TheDatesWith(
            ForecastWaits waits, Func<List<Feature>> theFeatures)
            => TheDatesWith(waits, theFeatures, new Mock<ILogger<ForecastService>>());

        private async Task<Dictionary<string, Dictionary<int, int>>> TheDatesWith(
            ForecastWaits waits, Func<List<Feature>> theFeatures, Mock<ILogger<ForecastService>> logger)
        {
            var features = theFeatures();
            featureRepositoryMock.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new RandomNumberService(),
                logger.Object,
                teamMetricsServiceMock.Object,
                featureRepositoryMock.Object,
                new WaitsHandedStraightToTheForecast(waits),
                new DrawsFromAPinnedStartingNumber(20260824),
                ForecastSimulationLimits.Default);

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return features.ToDictionary(
                feature => feature.ReferenceId,
                feature => ThePercentilesLighthouseShows.ToDictionary(
                    percentile => percentile,
                    feature.Forecast.GetProbability));
        }

        private List<Feature> TwoTeamsOneEach()
        {
            var waiting = CreateTeam(1, OneItemEveryDay);
            var waitedOn = CreateTeam(2, OneItemEveryDay);
            var elsewhere = CreateTeam(3, OneItemEveryDay);

            return
            [
                CreateFeature(1, TheOneThatWaits, [(waiting, 4)]),
                CreateFeature(2, TheOneWaitedOn, [(waitedOn, 6)]),
                CreateFeature(3, TheThirdOne, [(elsewhere, 2)]),
            ];
        }

        private List<Feature> OneTeamThreeTimesFaster()
        {
            var waiting = CreateTeam(1, OneItemEveryDay);
            var waitedOn = CreateTeam(2, ThreeItemsEveryDay);

            return
            [
                CreateFeature(1, TheOneThatWaits, [(waiting, 4)]),
                CreateFeature(2, TheOneWaitedOn, [(waitedOn, 9)]),
            ];
        }

        private List<Feature> TheOneWaitedOnIsSharedByThreeTeams()
        {
            var waiting = CreateTeam(1, OneItemEveryDay);
            var quickest = CreateTeam(2, OneItemEveryDay);
            var slowest = CreateTeam(3, OneItemEveryDay);
            var inBetween = CreateTeam(4, OneItemEveryDay);

            return
            [
                CreateFeature(1, TheOneThatWaits, [(waiting, 4)]),
                CreateFeature(2, TheOneWaitedOn, [(quickest, 2), (slowest, 6), (inBetween, 3)]),
            ];
        }

        private List<Feature> TheOneWaitedOnHasNothingMeasured()
        {
            var waiting = CreateTeam(1, OneItemEveryDay);
            var nothingMeasured = CreateTeam(2, NothingEverDelivered);

            return
            [
                CreateFeature(1, TheOneThatWaits, [(waiting, 4)]),
                CreateFeature(2, TheOneWaitedOn, [(nothingMeasured, 6)]),
            ];
        }

        private static int[] OneItemEveryDay => [1];

        private static int[] ThreeItemsEveryDay => [3];

        private static int[] NothingEverDelivered => [0, 0, 0];

        private Team CreateTeam(int id, int[] throughput)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = 1 };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private static Feature CreateFeature(int id, string referenceId, (Team team, int remainingItems)[] work)
            => new(work.Select(entry => (entry.team, entry.remainingItems, entry.remainingItems)))
            {
                Id = id,
                Name = referenceId,
                ReferenceId = referenceId,
            };

        private static Waits WaitingOn(string dependent, string blocker) => Waits.On(dependent, blocker);

        private static string Read(Dictionary<string, Dictionary<int, int>> dates)
            => string.Join(" | ", dates.Select(feature =>
                $"{feature.Key}: {string.Join("/", feature.Value.Select(percentile => $"{percentile.Key}%={percentile.Value}"))}"));
    }
}
