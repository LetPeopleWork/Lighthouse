using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance scenarios (Epic 5427) — Slice 03: Throughput process-behaviour limits over time,
    /// read-path half. Milestone-3 Scenario 10 ("Delivery lead reads dated Throughput process-behaviour
    /// limits", US-04) and Scenario 12 ("A fresh team's PBC Over Time widget shows the honest empty
    /// state", US-04, API half — the UI half lands in 03-05/03-06).
    /// Driving port: the new team + portfolio <c>process-behavior-over-time</c> read endpoints, exercised
    /// over the real ASP.NET host on real SQLite.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5427-percentiles-over-time")]
    [Category("slice-03")]
    public partial class Slice03ThroughputProcessBehaviorTest
    {
        // @driving_port @scenario-10 (the dated limit triple — three lines plotted across the date range)
        // @edge @scenario-12 (dayCount 0 — the forward-only empty state at the API level)
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public async Task The_delivery_lead_reads_the_dated_throughput_limits_a_team_has_recorded(int recordedDayCount)
        {
            var teamId = GivenATeam();
            var recorded = ARunOfRecordedDays(SyncDay, recordedDayCount);
            GivenPersistedThroughputLimits(teamId, OwnerType.Team, recorded);

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId);

            ThenTheDatedLimitTripleComesBackOrderedByDate(response, recorded);
        }

        // @edge @scenario-12 (a fresh team — never a broken chart, never another owner's rows)
        [Test]
        public async Task A_fresh_team_sees_the_honest_empty_state_rather_than_a_neighbours_limits()
        {
            var freshTeamId = GivenAFreshTeamWithNoRecordedLimits();
            var neighbourTeamId = GivenATeam();
            GivenPersistedThroughputLimits(neighbourTeamId, OwnerType.Team, ARunOfRecordedDays(SyncDay, 3));

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(freshTeamId);

            ThenTheWidgetGetsTheForwardOnlyEmptyState(response);
        }

        // @driving_port @scenario-10 (portfolio read path — the twin endpoint, same parameter shape)
        [Test]
        public async Task The_delivery_lead_reads_the_dated_throughput_limits_a_portfolio_has_recorded()
        {
            var portfolioId = GivenAPortfolio();
            var teamId = GivenATeam();
            var recorded = ARunOfRecordedDays(SyncDay, 3);
            GivenPersistedThroughputLimits(portfolioId, OwnerType.Portfolio, recorded);
            GivenPersistedThroughputLimits(teamId, OwnerType.Team, ARunOfRecordedDays(SyncDay, 2));

            var response = await WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(portfolioId);

            ThenTheDatedLimitTripleComesBackOrderedByDate(response, recorded);
        }

        // @driving_port @scenario-10 (the type parameter defaults to Throughput — the only family so far)
        [Test]
        public async Task A_request_that_omits_the_type_gets_the_throughput_limits()
        {
            var teamId = GivenATeam();
            var recorded = ARunOfRecordedDays(SyncDay, 2);
            GivenPersistedThroughputLimits(teamId, OwnerType.Team, recorded);

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, type: null);

            ThenTheDatedLimitTripleComesBackOrderedByDate(response, recorded);
        }

        // @edge @scenario-12 (an unknown family must be a 400 — an empty 200 would lie about the reason)
        // "99" is the in-range-integer-but-undefined case, which reaches the controller's Enum.IsDefined
        // guard; the NAME case is rejected one layer earlier, at the model binder. Both are 400, which is
        // why the Then asserts the STATUS only and the two cases can share it.
        [TestCase("99")]
        [TestCase(UnknownFamilyName)]
        public async Task An_unknown_metric_family_is_rejected_rather_than_answered_with_an_empty_series(string unknownType)
        {
            var teamId = GivenATeam();
            GivenPersistedThroughputLimits(teamId, OwnerType.Team, ARunOfRecordedDays(SyncDay, 2));

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, unknownType);

            ThenTheRequestIsRejectedAsUnsupported(response);
        }

        [TestCase("99")]
        [TestCase(UnknownFamilyName)]
        public async Task An_unknown_metric_family_is_rejected_on_the_portfolio_endpoint_too(string unknownType)
        {
            var portfolioId = GivenAPortfolio();

            var response = await WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(portfolioId, unknownType);

            ThenTheRequestIsRejectedAsUnsupported(response);
        }

        // @edge (the sentinel guards itself — epic-5427 slice-04 turned the previous sentinel "CycleTime"
        // into a real family, which would have made the two rejection cases above pass for the wrong
        // reason. A future slice that appends a family matching the sentinel now fails HERE, loudly.)
        [Test]
        public void The_unknown_family_sentinel_is_genuinely_not_a_declared_family()
        {
            Assert.That(Enum.TryParse<ProcessBehaviorMetricType>(UnknownFamilyName, out _), Is.False,
                $"'{UnknownFamilyName}' is the sentinel for an UNKNOWN metric family — once it parses, the rejection scenarios above stop testing rejection and start testing a happy path");
        }

        // @driving_port @scenario-10 (additive contract — the shipped endpoint is not perturbed)
        [Test]
        public async Task A_client_on_the_shipped_percentiles_contract_still_reads_the_identical_series()
        {
            var teamId = GivenATeam();
            var percentiles = new List<PercentilePoint>
            {
                new(DateOnly.FromDateTime(SyncDay.AddDays(-14)), P50: 4, P70: 5, P85: 7, P95: 10),
                new(DateOnly.FromDateTime(SyncDay.AddDays(-7)), P50: 5, P70: 7, P85: 9, P95: 12),
            };
            GivenPersistedCycleTimePercentiles(teamId, OwnerType.Team, horizon: 30, percentiles);
            GivenPersistedThroughputLimits(teamId, OwnerType.Team, ARunOfRecordedDays(SyncDay, 3));

            var response = await GetTeamPercentilesOverTime(teamId, horizon: 30);

            ThenTheShippedPercentilesPayloadIsUnchanged(response, percentiles);
        }
    }
}
