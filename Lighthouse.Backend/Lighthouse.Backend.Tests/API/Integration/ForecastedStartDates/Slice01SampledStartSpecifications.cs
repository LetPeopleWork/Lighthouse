using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// Step definitions for the scenarios that need a forecast to vary.
    ///
    /// The delivery histories here are chosen so that no Feature can empty inside a single day: the most
    /// the Team ever delivers in one day is smaller than any one Feature. Without that, a Feature finishing
    /// early would free its place and let the Feature below it begin on the very first day too - and the
    /// scenario about Feature WIP would pass or fail on how the sizes happened to divide rather than on
    /// the setting it is about.
    /// </summary>
    public partial class Slice01SampledStartTest()
        : ForecastedStartDateAcceptanceTest(new ForecastedStartDateHost(new DrawsFromAPinnedStartingNumber(PinnedStartingNumber), SampledTrials))
    {

        private const string Downstream = "Downstream";

        private const string Wall = "Wall";

        private const long PinnedStartingNumber = 6033;

        // Eighteen to twenty-two a day, against Features of twenty-five. Enough capacity that every
        // Feature inside the WIP is reached on the first day in far more than 85% of runs, and never
        // enough for one of them to finish there.
        private static readonly int[] AboutTwentyADay = [18, 20, 22];

        private const int TooBigToFinishInADay = 25;

        private static readonly string[] TheRanksBelowTheFirst = ["Rank 2", "Rank 3", "Rank 4", "Rank 5"];

        // --- Given ---

        private async Task<Portfolio> GivenFiveFeaturesOnOneTeamAt(int featureWip)
        {
            var connection = GivenAConnection();
            var team = await GivenATeamThatDelivers(connection, "The Team", AboutTwentyADay, featureWip);

            var ranked = Enumerable
                .Range(1, 5)
                .Select(rank => AFeature($"Rank {rank}", $"{rank * 10}", (team, TooBigToFinishInADay)))
                .ToArray();

            return await GivenAPortfolioOf(connection, ranked);
        }

        /// <summary>
        /// One Feature two Teams share, and both of their rows wait on a Feature a third Team owns. A row
        /// learns that another Team's work is finished the following day, so both Teams begin on the same
        /// day in every run - which is precisely the co-movement a formula over their separate
        /// distributions cannot see.
        /// </summary>
        private async Task<(Portfolio Portfolio, int FirstTeamId, int SecondTeamId)> GivenTwoTeamsWaitingOnTheSameUpstreamFeature()
        {
            var connection = GivenAConnection();

            var upstream = await GivenATeamThatDelivers(connection, "Upstream Team", [2, 3, 4], featureWip: 1);
            var first = await GivenATeamThatDelivers(connection, "First Team", [5, 6, 7], featureWip: 1);
            var second = await GivenATeamThatDelivers(connection, "Second Team", [4, 5, 6], featureWip: 1);

            var wall = AFeature(Wall, "10", (upstream, 8));
            var downstream = AFeature(Downstream, "20", (first, 6), (second, 7));

            var portfolio = await GivenAPortfolioOf(connection, wall, downstream);

            await GivenTheFeatureWaitsOn(downstream, wall);

            // Forecast once before the scenario's own run, because a dependency is only acted on when
            // both Features can be forecast and neither can until it has forecast rows to be judged by.
            // Any instance a user is looking at has refreshed before; one that has never refreshed is a
            // different situation, and not the one this scenario is about.
            await WhenTheForecastRuns(portfolio);
            GivenTheForecastIsNowWaitingFor(downstream, wall);

            return (portfolio, first.Id, second.Id);
        }

        // --- Then ---

        private static DateTime? TheForecastedStart(JsonElement features, string name, int percentile)
            => TheForecastedStart(TheFeatureNamed(features, name), percentile);
    }
}
