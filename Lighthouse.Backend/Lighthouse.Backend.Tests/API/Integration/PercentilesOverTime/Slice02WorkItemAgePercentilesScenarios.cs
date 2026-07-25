using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance scenarios (Epic 5427) — Slice 02: Work Item Age percentiles over time.
    /// Milestone-2 Scenario "Age percentiles recorded by the shared pipeline are served from the same
    /// endpoint family" (read-after-record, US-03 AC2) + "honest empty series" (US-03 AC3, backend half).
    /// Driving ports: the team + portfolio percentiles-over-time read endpoints (now metric-family aware)
    /// and the metrics-refresh event the shared recording pipeline hangs off.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5427-percentiles-over-time")]
    [Category("slice-02")]
    public partial class Slice02WorkItemAgePercentilesTest
    {
        // @driving_port @scenario-8 (read-after-record — the shared pipeline's age rows are served back)
        [Test]
        public async Task The_flow_coach_reads_back_the_age_percentiles_the_metrics_refresh_just_recorded()
        {
            var teamId = GivenATeam();
            GivenAnItemInProgressSinceDays(teamId, "AGE-1", ageInDays: 4);

            await WhenTheTeamMetricsRefreshCompletes(teamId);

            var response = await WhenTheFlowCoachReadsTheTeamAgeTrend(teamId);

            ThenTheDatedAgePercentilesTrendComesBackOrderedByDate(response, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(DateTime.Today), P50: 4, P70: 4, P85: 4, P95: 4),
            });
        }

        // @driving_port @scenario-8 (additive contract — a slice-01-shaped request is untouched)
        [Test]
        public async Task A_client_on_the_slice_one_contract_still_reads_the_identical_cycle_time_series()
        {
            var teamId = GivenATeam();
            GivenPersistedCycleTimePercentiles(teamId, OwnerType.Team, horizon: 30, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 4, P70: 5, P85: 7, P95: 10),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
            });
            GivenPersistedWorkItemAgePercentiles(teamId, OwnerType.Team, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 21, P70: 25, P85: 30, P95: 40),
            });

            var response = await WhenAClientOnTheSliceOneContractReadsTheTeamTrend(teamId, horizon: 30);

            ThenTheDatedCycleTimePercentilesTrendComesBackOrderedByDate(response, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 4, P70: 5, P85: 7, P95: 10),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
            });
        }

        // @edge @scenario-8 (age has no horizon dimension — a stale horizon must not mis-serve the tab)
        [Test]
        public async Task An_age_request_that_still_carries_a_horizon_gets_the_single_age_series()
        {
            var teamId = GivenATeam();
            GivenPersistedWorkItemAgePercentiles(teamId, OwnerType.Team, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 6, P70: 8, P85: 11, P95: 14),
            });

            var response = await WhenTheFlowCoachReadsTheTeamAgeTrend(teamId, horizon: 30);

            ThenTheDatedAgePercentilesTrendComesBackOrderedByDate(response, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 6, P70: 8, P85: 11, P95: 14),
            });
        }

        // @edge @scenario-9 (honest forward-only empty state — never the neighbouring CT rows)
        [Test]
        public async Task A_team_with_no_recorded_age_percentiles_sees_an_empty_series()
        {
            var teamId = GivenATeam();
            GivenPersistedCycleTimePercentiles(teamId, OwnerType.Team, horizon: 30, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
            });

            var response = await WhenTheFlowCoachReadsTheTeamAgeTrend(teamId);

            ThenTheSeriesIsEmpty(response);
        }

        // @driving_port @scenario-8 (portfolio read path — the twin endpoint)
        [Test]
        public async Task The_flow_coach_reads_a_dated_age_percentiles_trend_for_a_portfolio()
        {
            var portfolioId = GivenAPortfolio();
            GivenPersistedWorkItemAgePercentiles(portfolioId, OwnerType.Portfolio, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 12, P70: 16, P85: 21, P95: 28),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 10, P70: 14, P85: 18, P95: 24),
            });

            var response = await WhenTheFlowCoachReadsThePortfolioAgeTrend(portfolioId);

            ThenTheDatedAgePercentilesTrendComesBackOrderedByDate(response, new List<AgePercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 10, P70: 14, P85: 18, P95: 24),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 12, P70: 16, P85: 21, P95: 28),
            });
        }
    }
}
