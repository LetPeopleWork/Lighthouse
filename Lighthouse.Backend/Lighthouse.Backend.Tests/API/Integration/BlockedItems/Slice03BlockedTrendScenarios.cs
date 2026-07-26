using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 03: blocked-items over-time trend.
    /// Job: job-delivery-lead-see-blocked-trend. Persona: delivery-lead-rte. Driving port: new team
    /// metrics blockedCountHistory read endpoint.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-03")]
    public partial class Slice03BlockedTrendTest
    {
        // @driving_port @us-03
        [Test]
        public async Task The_blocked_count_trend_is_available_over_time()
        {
            var team = GivenATeam();
            GivenBlockedCountSnapshots(team, new List<(DateOnly Date, int Count)>
            {
                (DateOnly.FromDateTime(SyncDay.AddDays(-21)), 3),
                (DateOnly.FromDateTime(SyncDay.AddDays(-14)), 6),
                (DateOnly.FromDateTime(SyncDay.AddDays(-7)), 9),
            });

            var response = await WhenTheDeliveryLeadOpensTheBlockedTrend(team);

            ThenTheTrendIsAvailable(response);
        }

        // @edge @us-03 (honest forward-only empty state)
        [Test]
        public async Task A_new_team_sees_an_honest_empty_trend()
        {
            var team = GivenATeam();

            var response = await WhenTheDeliveryLeadOpensTheBlockedTrend(team);

            ThenTheTrendShowsTheForwardOnlyEmptyState(response);
        }

        // @regression @us-03 (Bug 5522 — days with no recorded snapshot carry the last known count
        // forward; the seed is the latest snapshot BEFORE the window, and nothing is fabricated ahead
        // of the first-ever record)
        [Test]
        public async Task The_blocked_trend_interpolates_missing_days_from_the_last_known_count()
        {
            var team = GivenATeam();
            GivenBlockedCountSnapshots(team, new List<(DateOnly Date, int Count)>
            {
                (DateOnly.FromDateTime(SyncDay.AddDays(-22)), 2),
                (DateOnly.FromDateTime(SyncDay.AddDays(-14)), 6),
            });

            var response = await WhenTheDeliveryLeadOpensTheBlockedTrend(team);

            ThenTheTrendInterpolatesMissingDaysFromTheLastKnownCount(response);
        }
    }
}
