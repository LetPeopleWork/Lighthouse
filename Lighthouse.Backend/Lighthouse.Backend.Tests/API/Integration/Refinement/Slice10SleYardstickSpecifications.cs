using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the yardstick. Today is Wednesday 7 October 2026. Finished Work Items are the Team's
    /// history, each finished the given number of days before today, as the tracker would report them; the SLE is
    /// what the Team stores. The tab is read as the browser receives it, before it is written out as JSON.
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

        /// <summary>
        /// Team Meridian's recent history: the twenty cycle times above, finished one a day over the last twenty
        /// days. The wire contract of the Refinement tab sets up the same history, so it reads it from here.
        /// </summary>
        internal static readonly FinishedWorkItem[] MeridiansRecentlyFinishedWorkItems =
            [.. RecentCycleTimes.Select((cycleTime, index) => new FinishedWorkItem($"MR-{100 + index}", cycleTime, FinishedDaysAgo: index + 1))];

        /// <summary>The keys Jonas's and Ana's browsers keep, so each reads the tab as a reader of their own.</summary>
        private static readonly string JonasesBrowserKey = Guid.NewGuid().ToString("N");

        private static readonly string AnasBrowserKey = Guid.NewGuid().ToString("N");

        // --- Given ---

        private int GivenGravityExpects85PercentWithinSevenDays()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            return Givens.GravityRefinesFiveWorkItems(sleProbability: 85, sleRange: 7);
        }

        private int GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays()
            => MeridianWithAnSleAndCycleTimes85thPercentileOfTwelveDays(0, 0);

        private int GivenMeridianWithAHalfSetSleAndCycleTimes85thPercentileOfTwelveDays(int probability, int days)
            => MeridianWithAnSleAndCycleTimes85thPercentileOfTwelveDays(probability, days);

        private int MeridianWithAnSleAndCycleTimes85thPercentileOfTwelveDays(int probability, int days)
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            var meridian = Givens.GravityRefinesFiveWorkItems(TeamMeridian, probability, days);
            Givens.TheTeamFinished(meridian, MeridiansRecentlyFinishedWorkItems);
            return meridian;
        }

        private int GivenEquinoxHasNoSleAndHasFinishedNothing()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            return Givens.GravityRefinesFiveWorkItems(TeamEquinox, sleProbability: 0, sleRange: 0);
        }

        private void AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(int team)
            => FiveVerySlowWorkItemsFinished(team, daysAgo: 40);

        private void AndFiveVerySlowWorkItemsFinishedOnTheFirstDayOfTheThroughputWindow(int team)
            => FiveVerySlowWorkItemsFinished(team, daysAgo: ThroughputHistoryDays - 1);

        private void AndFiveVerySlowWorkItemsFinishedTheDayBeforeTheThroughputWindowOpens(int team)
            => FiveVerySlowWorkItemsFinished(team, daysAgo: ThroughputHistoryDays);

        private void FiveVerySlowWorkItemsFinished(int team, int daysAgo)
            => Givens.TheTeamFinished(team,
                [.. Enumerable.Range(1, 5).Select(index => new FinishedWorkItem($"SLOW-{index}", CycleTimeDays: 60, FinishedDaysAgo: daysAgo))]);

        /// <summary>The day the scenarios on a web host started on, nine in the morning.</summary>
        private void GivenTodayIsWednesdayTheSeventhOfOctober() => TodayIs(2026, 10, 7);

        // --- When ---

        private RefinementViewDto WhenJonasOpensTheRefinementTab(int team) => TheTabAsReadBy(JonasesBrowserKey, team);

        private RefinementViewDto WhenAnaOpensTheRefinementTab(int team) => TheTabAsReadBy(AnasBrowserKey, team);

        private RefinementViewDto TheTabAsReadBy(string browserKey, int team)
        {
            using var scope = NewScope();
            var view = scope.ServiceProvider.GetRequiredService<IRefinementViewQuery>().ForTeam(team, browserKey)
                ?? throw new InvalidOperationException($"no Team {team} is stored, so it has no Refinement tab");
            return new RefinementViewDto(view);
        }

        // --- Then ---

        private static void ThenTheYardstickIs(RefinementViewDto tab, string source, int? days, int? probability)
            => Assert.That(YardstickIn(tab), Is.EqualTo(new YardstickReading(source, days, probability)),
                "every voter answers against this yardstick: the Team's SLE when it has a whole one, else the 85th percentile of "
                + "the cycle times finished inside the Throughput window, else no number; the tab must name its source and both numbers");

        private static void ThenBothAreShownTheSameYardstick(RefinementViewDto first, RefinementViewDto second)
            => Assert.That(YardstickIn(second), Is.EqualTo(YardstickIn(first)).And.EqualTo(new YardstickReading(TheCycleTimeFallback, 12, 85)),
                "the yardstick is the Team's, not the reader's, so two voters must be shown the same fallback of 12 days at 85%");
    }
}
