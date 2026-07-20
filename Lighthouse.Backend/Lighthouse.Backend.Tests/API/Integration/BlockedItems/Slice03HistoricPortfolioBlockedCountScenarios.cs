using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (portfolio-blocked-history, ADO #5524) — Slice 03 / US-03:
    /// honest Blocked count on a historic portfolio range. Job: job-delivery-lead-trust-portfolio-blocked-history.
    /// Persona: delivery-lead-rte. Driving port: portfolio metrics wip read with asOfDate in the past.
    /// Precondition spells are seeded into the FEATURE keyspace (scaffold entity per ADR-102) — captured
    /// history is input state, never the expected output (Critical Rule 7). All scenarios
    /// [Ignore]-pending (RED-ready, ADR-025).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("portfolio-blocked-history")]
    [Category("slice-03")]
    public partial class Slice03HistoricPortfolioBlockedCountTest
    {
        // @walking_skeleton @driving_port @us-03 @contract-shape:pure-function (US-03 AC1)
        [Test]
        [Category("walking_skeleton")]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_blocked_during_a_past_window_but_clear_today_reads_blocked_for_that_window()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var featureId = GivenAFeatureThatIsNotBlockedToday(portfolio, "F-THEN");
            GivenAClosedBlockedSpellCovering(featureId, portfolio, SyncDay.AddDays(-9), SyncDay.AddDays(-2));

            var item = await WhenTheLeadReadsThePortfolioWipAt(portfolio, SyncDay.AddDays(-5));

            ThenTheFeatureReadsBlockedWithTheSpellStart(item);
        }

        // @driving_port @us-03 @error @contract-shape:pure-function (US-03 AC2 — the inverse error:
        // blocked only today must NOT leak into ranges that closed before the spell began)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_blocked_only_today_reads_not_blocked_on_a_range_that_closed_before_its_spell_began()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var featureId = GivenAFeatureBlockedToday(portfolio, "F-NOW");
            GivenAnOpenBlockedSpellThatBegan(featureId, portfolio, SyncDay.AddDays(-1));

            var item = await WhenTheLeadReadsThePortfolioWipAt(portfolio, SyncDay.AddDays(-5));

            ThenTheFeatureReadsNotBlocked(item);
        }

        // @driving_port @us-03 @contract-shape:pure-function (US-03 AC3 — guard shape: today answers live)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task Todays_read_still_answers_from_the_live_rule_set()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var featureId = GivenAFeatureBlockedToday(portfolio, "F-LIVE");
            GivenAnOpenBlockedSpellThatBegan(featureId, portfolio, SyncDay.AddDays(-1));

            var item = await WhenTheLeadReadsThePortfolioWipAt(portfolio, DateTime.UtcNow);

            ThenTheFeatureReadsBlocked(item);
        }

        // @edge @us-03 @contract-shape:pure-function (US-03 AC4a — no capture history at all: pre-capture
        // feature falls back to the live rule, the only honest answer available)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_with_no_capture_history_falls_back_to_the_live_rule()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            GivenAFeatureBlockedToday(portfolio, "F-LEGACY");

            var item = await WhenTheLeadReadsThePortfolioWipAt(portfolio, SyncDay.AddDays(-5));

            ThenTheFeatureReadsBlocked(item);
        }

        // @edge @us-03 @contract-shape:pure-function (US-03 AC4b — the correctness cliff: WITH history
        // and no covering spell, absence of a spell is evidence, not a gap — never fall back to live)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_with_history_but_no_spell_covering_the_date_reads_not_blocked()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var featureId = GivenAFeatureBlockedToday(portfolio, "F-EVIDENCE");
            GivenAClosedBlockedSpellCovering(featureId, portfolio, SyncDay.AddDays(-20), SyncDay.AddDays(-15));

            var item = await WhenTheLeadReadsThePortfolioWipAt(portfolio, SyncDay.AddDays(-5));

            ThenTheFeatureReadsNotBlocked(item);
        }

        // @property @us-03 @contract-shape:pure-function (parity KPI / journey integration_validation:
        // the same spell shape asserted through BOTH controllers over the same past range must produce
        // the same answer — divergence is a failing test, not a nuance)
        [Test]
        [Category("property")]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task The_same_spell_shape_answers_identically_on_team_and_portfolio_over_the_same_past_range()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            var featureId = GivenAFeatureThatIsNotBlockedToday(portfolio, "F-PARITY");
            var workItemId = GivenAWorkItemThatIsNotBlockedToday(team, "WI-PARITY");
            GivenAClosedBlockedSpellCovering(featureId, portfolio, SyncDay.AddDays(-9), SyncDay.AddDays(-2));
            GivenAMirroredTeamSpellCovering(workItemId, SyncDay.AddDays(-9), SyncDay.AddDays(-2));

            var featureItem = await WhenTheLeadReadsThePortfolioWipAt(portfolio, SyncDay.AddDays(-5));
            var workItem = await WhenTheLeadReadsTheTeamWipAt(team, SyncDay.AddDays(-5));

            ThenBothReadsAgreeOnBlockedAndBlockedSince(featureItem, workItem, SyncDay.AddDays(-9));
        }
    }
}
