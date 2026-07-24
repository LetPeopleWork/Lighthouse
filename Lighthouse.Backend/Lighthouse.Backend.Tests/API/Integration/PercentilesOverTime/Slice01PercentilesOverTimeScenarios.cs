using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance scenarios (Epic 5427) — Slice 01: read the Percentiles Over Time trend.
    /// Walking-skeleton Scenario 1 (read path): a flow coach opens the widget and reads a dated CT-30
    /// percentiles trend served from the persisted snapshot table — no live recompute of historical days.
    /// Driving ports: the team + portfolio metrics percentiles-over-time read endpoints.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5427-percentiles-over-time")]
    [Category("slice-01")]
    public partial class Slice01PercentilesOverTimeTest
    {
        // @driving_port @scenario-1 (team read path)
        [Test]
        public async Task The_flow_coach_reads_a_dated_cycle_time_percentiles_trend_for_a_team()
        {
            var teamId = GivenATeam();
            GivenPersistedCycleTimePercentiles(teamId, OwnerType.Team, horizon: 30, new List<PercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-21)), P50: 3, P70: 4, P85: 6, P95: 8),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 4, P70: 5, P85: 7, P95: 10),
            });

            var response = await WhenTheFlowCoachReadsTheTeamTrend(teamId, horizon: 30);

            ThenTheDatedPercentilesTrendComesBackOrderedByDate(response, new List<PercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-21)), P50: 3, P70: 4, P85: 6, P95: 8),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 4, P70: 5, P85: 7, P95: 10),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
            });
        }

        // @edge @scenario-1 (honest empty-state contract)
        [Test]
        public async Task A_team_with_no_snapshots_sees_an_empty_series()
        {
            var teamId = GivenATeam();

            var response = await WhenTheFlowCoachReadsTheTeamTrend(teamId, horizon: 30);

            ThenTheSeriesIsEmpty(response);
        }

        // @driving_port @scenario-1 (portfolio read path — the twin endpoint)
        [Test]
        public async Task The_flow_coach_reads_a_dated_cycle_time_percentiles_trend_for_a_portfolio()
        {
            var portfolioId = GivenAPortfolio();
            GivenPersistedCycleTimePercentiles(portfolioId, OwnerType.Portfolio, horizon: 30, new List<PercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 8, P70: 11, P85: 15, P95: 20),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 9, P70: 12, P85: 16, P95: 22),
            });

            var response = await WhenTheFlowCoachReadsThePortfolioTrend(portfolioId, horizon: 30);

            ThenTheDatedPercentilesTrendComesBackOrderedByDate(response, new List<PercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 8, P70: 11, P85: 15, P95: 20),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 9, P70: 12, P85: 16, P95: 22),
            });
        }
    }
}
