using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// A simulated run ends when the work is done, or when nothing is left that anybody could start. Neither
    /// depends on the numbers drawn, so no data should ever reach the ceiling on how many days one run may
    /// cover. The ceiling is there for the day that reasoning turns out to be wrong: a forecast runs on a
    /// background refresh rather than behind a request, so without it a mistake would tie up a thread for
    /// good with nothing anywhere saying why.
    ///
    /// The limits are handed in rather than fixed, so this can be shown in a fraction of a second on a run
    /// covering a few dozen days instead of the hundred thousand production allows.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class ForecastRunThatCannotEndTest
    {
        private const int TheMostDaysOneRunMayCover = 25;

        private const int TheFirstTeam = 0;

        private const int TheSecondTeam = 1;

        private static readonly string[] OnlyTheFirstTeam = ["Team 1"];

        private static readonly string[] OnlyTheSecondTeam = ["Team 2"];

        private Mock<IRepository<Feature>> featureRepositoryMock;

        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        private Mock<ILogger<ForecastService>> logger;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            logger = new Mock<ILogger<ForecastService>>();
        }

        [Test]
        public async Task ARunThatPassesTheCeiling_IsGivenUpOnAndSaysWhichRunItWas()
        {
            var forecast = await TheForecastOfAPortfolioOneTeamCannotGetThrough();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatWasLoggedAsAnError(), Is.Not.Empty,
                    "The forecast gave up on runs and said nothing, so an operator reading logs has no way to " +
                    "know the dates on screen are not a forecast.");

                Assert.That(TheStartingNumberAndTheCeilingNamed(), Is.True,
                    "What was reported does not name the number the draws came from and the run they were " +
                    "taken for, so the run that would not end cannot be set going again on its own. " +
                    $"Reported: {string.Join(" | ", WhatWasLoggedAsAnError())}");

                Assert.That(forecast.TheSlowOne.Forecast.TotalTrials, Is.Zero,
                    "The Feature nobody could get through was reported as though it had been forecast.");

                Assert.That(forecast.TheOrdinaryOne.Forecast.GetProbability(85), Is.GreaterThan(0),
                    "The Portfolio's other Feature lost its forecast because a different Team could not finish.");
            }
        }

        /// <summary>
        /// One run ends one way, so a forecast holds both kinds only when some of its runs ran out of days
        /// and others had nothing left to start. That is a state a fixture cannot reach on purpose without
        /// tuning it to one exact draw, so it is asserted here on the thing that collects them.
        ///
        /// It matters because the two are reported in two different lines, and the line about a circle tells
        /// an operator to go and find one. Naming a merely slow Team in it sends them hunting for a circle
        /// that Team is not in.
        /// </summary>
        [Test]
        public void ATeamThatMerelyRanOutOfDays_IsNotNamedAmongTheOnesCaughtInACircle()
        {
            var plan = APlanForTwoTeams(out var state);

            var whatWentWrong = new WhatTheRunsCouldNotFinish();

            LeaveOnly(TheFirstTeam, state, plan);
            whatWentWrong.Note(HowTheRunEnded.RanOutOfDays, trial: 3, plan, state);

            state.StartAgain();
            LeaveOnly(TheSecondTeam, state, plan);
            whatWentWrong.Note(HowTheRunEnded.NothingLeftCouldBeStarted, trial: 4, plan, state);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatWentWrong.TeamsCaughtInACircle, Is.EqualTo(OnlyTheSecondTeam),
                    "A Team that merely ran out of days was named as caught in a circle.");

                Assert.That(whatWentWrong.TeamsThatRanOutOfDays, Is.EqualTo(OnlyTheFirstTeam),
                    "A Team caught in a circle was named as merely having run out of days.");

                Assert.That(whatWentWrong.RunsThatRanOutOfDays, Is.EqualTo(1));
                Assert.That(whatWentWrong.RunsGivenUpOn, Is.EqualTo(1));
                Assert.That(whatWentWrong.FirstRunThatRanOutOfDays, Is.EqualTo(3));
            }
        }

        /// <summary>Works every other Team's rows down to nothing, so only the one named still has work.</summary>
        private static void LeaveOnly(int teamIndex, TrialState state, ForecastRunPlan plan)
        {
            for (var row = 0; row < plan.RowCount; row++)
            {
                while (plan.TeamOf(row) != teamIndex && state.ReadyToBeWorkedOn(row, today: 1))
                {
                    state.CloseOneItemOf(row, today: 1);
                }
            }
        }

        private ForecastRunPlan APlanForTwoTeams(out TrialState state)
        {
            var first = CreateTeam(1, [1]);
            var second = CreateTeam(2, [1]);

            var rows = new List<SimulationResult>
            {
                new(first, CreateFeature(1, "F-1", first, 2), 2),
                new(second, CreateFeature(2, "F-2", second, 2), 2),
            };

            var plan = ForecastRunPlan.For(
                rows,
                new Dictionary<int, RunChartData>
                {
                    [first.Id] = ThroughputOf(first),
                    [second.Id] = ThroughputOf(second),
                },
                ForecastWaits.Nothing);

            state = new TrialState(plan);

            return plan;
        }

        private RunChartData ThroughputOf(Team team)
            => teamMetricsServiceMock.Object.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting).Throughput;

        private async Task<(Feature TheSlowOne, Feature TheOrdinaryOne)> TheForecastOfAPortfolioOneTeamCannotGetThrough()
        {
            // One item every ten days, against forty items left: this Team needs some four hundred days and
            // is given twenty-five.
            var barelyMoving = CreateTeam(1, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
            var ordinary = CreateTeam(2, [3, 4, 2, 5, 3]);

            var theSlowOne = CreateFeature(1, "F-1", barelyMoving, 40);
            var theOrdinaryOne = CreateFeature(2, "F-2", ordinary, 5);

            var features = new List<Feature> { theSlowOne, theOrdinaryOne };

            featureRepositoryMock.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new RandomNumberService(),
                logger.Object,
                teamMetricsServiceMock.Object,
                featureRepositoryMock.Object,
                new NothingWaitsForAnything(),
                new DrawsFromAPinnedStartingNumber(4242),
                new ForecastSimulationLimits(Trials: 5, MostDaysOneSimulatedRunMayCover: TheMostDaysOneRunMayCover));

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return (theSlowOne, theOrdinaryOne);
        }

        private bool TheStartingNumberAndTheCeilingNamed()
            => WhatWasLoggedAsAnError().Any(reported => reported.Contains("4242", StringComparison.Ordinal)
                && reported.Contains(TheMostDaysOneRunMayCover.ToString(), StringComparison.Ordinal));

        private List<string> WhatWasLoggedAsAnError()
            => logger.Invocations
                .Where(invocation => invocation.Arguments.Contains(LogLevel.Error))
                .Select(invocation => string.Join(" ", invocation.Arguments.Select(argument => argument?.ToString() ?? string.Empty)))
                .ToList();

        private Team CreateTeam(int id, int[] throughput)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = 1 };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private static Feature CreateFeature(int id, string referenceId, Team team, int remainingItems)
            => new(team, remainingItems)
            {
                Id = id,
                Name = referenceId,
                ReferenceId = referenceId,
            };
    }
}
