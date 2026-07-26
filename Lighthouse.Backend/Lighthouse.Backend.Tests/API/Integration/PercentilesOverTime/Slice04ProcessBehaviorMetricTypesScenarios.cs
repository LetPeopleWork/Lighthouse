using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance scenarios (Epic 5427) — Slice 04: every process-behaviour metric family is
    /// recorded and readable over time, not only Throughput. Milestone-4 Scenario 13 ("a delivery lead
    /// reads dated limits for each metric family", US-05 AC1/AC2) and Scenario 14 (Feature Size is a
    /// portfolio family, US-05 AC3) — at the READ PORT, because the demo backfill stays Throughput-only
    /// this slice and the browser therefore has no history for the other five families to plot.
    /// Driving port: the SHIPPED team + portfolio <c>process-behavior-over-time</c> endpoints, exercised
    /// over the real ASP.NET host on real SQLite. No controller change was needed for any family — that
    /// is what these scenarios verify.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5427-percentiles-over-time")]
    [Category("slice-04")]
    public partial class Slice04ProcessBehaviorMetricTypesTest
    {
        // @driving_port @scenario-13 (US-05 AC1 — each team family reads back its OWN dated triple)
        // @edge (US-05 AC2 — a neighbour team's rows for the SAME family never leak in)
        [TestCase(ProcessBehaviorMetricType.Throughput)]
        [TestCase(ProcessBehaviorMetricType.WorkItemAge)]
        [TestCase(ProcessBehaviorMetricType.Wip)]
        [TestCase(ProcessBehaviorMetricType.CycleTime)]
        [TestCase(ProcessBehaviorMetricType.Arrivals)]
        public async Task The_delivery_lead_reads_the_dated_limits_a_team_recorded_for_each_metric_family(ProcessBehaviorMetricType family)
        {
            var teamId = GivenATeam();
            var neighbourTeamId = GivenATeam();
            var recorded = GivenPersistedLimits(teamId, OwnerType.Team, family);
            GivenPersistedLimits(neighbourTeamId, OwnerType.Team, family, extraSpread: NeighbourSpread);

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, family);

            ThenTheDatedLimitTripleComesBackOrderedByDate(response, recorded);
        }

        // @driving_port @scenario-13 (US-05 AC1 — the portfolio twin, including portfolio-only Feature Size)
        // @edge (US-05 AC2 — a neighbour portfolio's rows for the SAME family never leak in)
        [TestCase(ProcessBehaviorMetricType.Throughput)]
        [TestCase(ProcessBehaviorMetricType.WorkItemAge)]
        [TestCase(ProcessBehaviorMetricType.Wip)]
        [TestCase(ProcessBehaviorMetricType.CycleTime)]
        [TestCase(ProcessBehaviorMetricType.Arrivals)]
        [TestCase(ProcessBehaviorMetricType.FeatureSize)]
        public async Task The_delivery_lead_reads_the_dated_limits_a_portfolio_recorded_for_each_metric_family(ProcessBehaviorMetricType family)
        {
            var portfolioId = GivenAPortfolio();
            var neighbourPortfolioId = GivenAPortfolio();
            var recorded = GivenPersistedLimits(portfolioId, OwnerType.Portfolio, family);
            GivenPersistedLimits(neighbourPortfolioId, OwnerType.Portfolio, family, extraSpread: NeighbourSpread);

            var response = await WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(portfolioId, family);

            ThenTheDatedLimitTripleComesBackOrderedByDate(response, recorded);
        }

        // @driving_port @scenario-13 (US-05 AC2 — the discriminator: two families on the SAME owner and the
        // SAME days, carrying different triples. A family-blind read returns both runs and fails here.)
        [TestCase(ProcessBehaviorMetricType.Throughput, ProcessBehaviorMetricType.WorkItemAge)]
        [TestCase(ProcessBehaviorMetricType.Wip, ProcessBehaviorMetricType.CycleTime)]
        [TestCase(ProcessBehaviorMetricType.Arrivals, ProcessBehaviorMetricType.Throughput)]
        public async Task Each_metric_family_reads_its_own_series_when_another_family_was_recorded_on_the_same_days(
            ProcessBehaviorMetricType family,
            ProcessBehaviorMetricType otherFamily)
        {
            var teamId = GivenATeam();
            var recorded = GivenPersistedLimits(teamId, OwnerType.Team, family);
            var recordedForTheOtherFamily = GivenPersistedLimits(teamId, OwnerType.Team, otherFamily);

            var response = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, family);
            var otherResponse = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, otherFamily);

            ThenEachFamilyKeepsItsOwnSeries(response, recorded, otherResponse, recordedForTheOtherFamily);
        }

        // @edge @scenario-14 (US-05 AC3 / decision D8 — Feature Size is portfolio-only, enforced at the
        // toggle and structurally in the recorder, NOT at the wire: a team asking for it passes
        // Enum.IsDefined and gets the honest empty state because nothing is ever recorded for a team.)
        [Test]
        public async Task Feature_size_is_a_portfolio_family_and_a_team_asking_for_it_gets_the_honest_empty_state()
        {
            var portfolioId = GivenAPortfolio();
            var teamId = GivenATeam();
            var recorded = GivenPersistedLimits(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.FeatureSize);

            var portfolioResponse = await WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(portfolioId, ProcessBehaviorMetricType.FeatureSize);
            var teamResponse = await WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(teamId, ProcessBehaviorMetricType.FeatureSize);

            ThenTheDatedLimitTripleComesBackOrderedByDate(portfolioResponse, recorded);
            ThenTheWidgetGetsTheHonestEmptyState(teamResponse);
        }
    }
}
