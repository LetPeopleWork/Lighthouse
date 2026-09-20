using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// Step definitions for the forecasted start date, over a Team whose delivery never varies.
    ///
    /// Drawing the same number every day is what turns a forecast into arithmetic: the Team takes the
    /// first day of its measured history every day and works whatever sits nearest the top of its order,
    /// so every simulated run is the same run and every percentile is the same day. That is the only way
    /// to assert which day a Feature starts rather than which week.
    /// </summary>
    public partial class Slice01StartForecastTest()
        : ForecastedStartDateAcceptanceTest(new ForecastedStartDateHost(new DrawsTheSameNumberEveryTime(), DeterministicTrials))
    {

        private const string Alpha = "Alpha";

        private const string Bravo = "Bravo";

        private const string Shared = "Shared";

        // Three a day, every day. Five items therefore finish part-way through the second day, which
        // leaves the Team capacity in that same day to pull the Feature below it - the whole point of
        // AC-1.2, and it would be lost if the sizes happened to land on a day boundary.
        private static readonly int[] ThreeADay = [3, 3, 3];

        private int TheOnlyTeamId { get; set; }

        // --- Given ---

        private async Task<Portfolio> GivenAQueueOfTwoFeaturesOnOneTeam(DateTime? alphaStartedOn = null)
        {
            var connection = GivenAConnection();
            var team = await GivenATeamThatDelivers(connection, "The Team", ThreeADay, featureWip: 1);
            TheOnlyTeamId = team.Id;

            var alpha = AFeature(Alpha, "10", (team, 5));
            var bravo = AFeature(Bravo, "20", (team, 4));

            if (alphaStartedOn is { } startedOn)
            {
                GivenTheFeatureIsAlreadyInFlight(alpha, startedOn);
            }

            return await GivenAPortfolioOf(connection, alpha, bravo);
        }

        private static void GivenTheFeatureIsAlreadyInFlight(Feature feature, DateTime startedOn)
        {
            feature.StateCategory = StateCategories.Doing;
            feature.State = "In Progress";
            feature.StartedDate = startedOn;
        }

        private async Task<(Portfolio Portfolio, int FirstTeamId, int SecondTeamId)> GivenAFeatureTwoTeamsShare()
        {
            var connection = GivenAConnection();
            var first = await GivenATeamThatDelivers(connection, "First Team", ThreeADay, featureWip: 1);
            var second = await GivenATeamThatDelivers(connection, "Second Team", [2, 2, 2], featureWip: 1);

            var shared = AFeature(Shared, "10", (first, 6), (second, 9));

            return (await GivenAPortfolioOf(connection, shared), first.Id, second.Id);
        }

        private async Task<(Portfolio Portfolio, string SilentTeamName, int SilentTeamId)> GivenAFeatureOneOfWhoseTeamsHasNeverDelivered()
        {
            const string silent = "The Silent Team";

            var connection = GivenAConnection();
            var measured = await GivenATeamThatDelivers(connection, "The Measured Team", ThreeADay, featureWip: 1);
            var neverDelivered = await GivenATeamThatDelivers(connection, silent, ThreeADay, featureWip: 1);

            GivenTheTeamHasNeverDeliveredAnything(neverDelivered);

            var shared = AFeature(Shared, "10", (measured, 6), (neverDelivered, 4));

            return (await GivenAPortfolioOf(connection, shared), silent, neverDelivered.Id);
        }
    }
}
