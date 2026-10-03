using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the yardstick. Finished Work Items are the Team's history, seeded as the
    /// tracker would report them; the SLE is set the way an admin sets it, through the Team settings write.
    /// </summary>
    public partial class Slice10SleYardstickTest
    {
        private const string TheSle = "Sle";

        private const string TheCycleTimeFallback = "CycleTimeFallback";

        private const string Unavailable = "Unavailable";

        private const string TeamMeridian = "Team Meridian";

        private const string TeamEquinox = "Team Equinox";

        /// <summary>
        /// Twenty Work Items finished within the last 30 days. Sorted, the 17th is 12 days - the 85th
        /// percentile the way the product computes every cycle-time percentile.
        /// </summary>
        private static readonly int[] RecentCycleTimes = [2, 3, 4, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 15, 18, 30];

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityExpects85PercentWithinSevenDays()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn(sleProbability: 85, sleRange: 7);

        private async Task<TeamUnderTest> GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays()
            => await GivenMeridianWithAHalfSetSleAndCycleTimes85thPercentileOfTwelveDays(0, 0);

        private async Task<TeamUnderTest> GivenMeridianWithAHalfSetSleAndCycleTimes85thPercentileOfTwelveDays(int probability, int days)
        {
            var meridian = await GravityRefinesSixWorkItemsNobodyHasVotedOn(probability, days, TeamMeridian);

            SeedFinishedWorkItems(meridian,
                [.. RecentCycleTimes.Select((cycleTime, index) => new FinishedWorkItem($"MR-{100 + index}", cycleTime, FinishedDaysAgo: index + 1))]);

            return meridian;
        }

        private async Task<TeamUnderTest> GivenEquinoxHasNoSleAndHasFinishedNothing()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn(0, 0, TeamEquinox);

        private void AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(TeamUnderTest team)
            => SeedFinishedWorkItems(team,
                [.. Enumerable.Range(1, 5).Select(index => new FinishedWorkItem($"OLD-{index}", CycleTimeDays: 60, FinishedDaysAgo: 40))]);

        // --- When ---

        private async Task<JsonElement> WhenJonasOpensTheRefinementTab(TeamUnderTest team)
            => await TheTabAsSeenBy(Jonas, team);

        private async Task<JsonElement> WhenAnaOpensTheRefinementTab(TeamUnderTest team)
            => await TheTabAsSeenBy(Ana, team);

        private async Task<TeamUnderTest> WhenTheAdminSetsTheSleTo75PercentWithinTenDays(TeamUnderTest team)
            => await TheAdminHasSetTheSle(team, probability: 75, days: 10);

        // --- Then ---

        private static void ThenTheYardstickIs(JsonElement tab, string source, int? days, int? probability)
            => Assert.That(YardstickIn(tab), Is.EqualTo(new YardstickReading(source, days, probability)));

        private static void ThenBothAreShownTheSameYardstick(JsonElement first, JsonElement second)
            => Assert.That(YardstickIn(second), Is.EqualTo(YardstickIn(first)).And.EqualTo(new YardstickReading(TheCycleTimeFallback, 12, 85)));
    }
}
