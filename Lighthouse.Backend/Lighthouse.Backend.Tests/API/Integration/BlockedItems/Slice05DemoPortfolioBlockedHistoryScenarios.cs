using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (portfolio-blocked-history, ADO #5524) — Slice 05 / US-05:
    /// demo portfolio blocked history worth clicking. Job: job-delivery-lead-see-blocked-trend.
    /// Persona: prospect evaluating Lighthouse from demo data. Driving port: PortfolioFeaturesRefreshed
    /// domain-event dispatch (real dispatcher, real DemoBlockedHistoryBackfillHandler, real SQLite)
    /// plus the portfolio drill-through read port for the "bars worth clicking" claim. All scenarios
    /// [Ignore]-pending (RED-ready, ADR-025).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("portfolio-blocked-history")]
    [Category("slice-05")]
    public partial class Slice05DemoPortfolioBlockedHistoryTest
    {
        // @walking_skeleton @driving_port @us-05 @contract-shape:bounded-change (US-05 AC1)
        [Test]
        [Category("walking_skeleton")]
        public async Task A_freshly_refreshed_demo_portfolio_gains_backdated_feature_blocked_spells_in_the_feature_keyspace()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenTwoBlockedDemoFeatures(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheFeatureKeyspaceHoldsBackdatedSpellsForBothFeatures(portfolio);
        }

        // @invariant @us-05 @us-01 @contract-shape:unbounded-preservation (US-05 AC1 — slice 01's
        // keyspace invariant must still hold after the demo backfill is re-added. Work items are seeded
        // with colliding ids so that TODAY's broken handler actually lands rows in the team table —
        // without the collision the enforced FK aborts the write and the scenario would be vacuously green.)
        [Test]
        public async Task The_demo_backfill_writes_nothing_into_the_team_keyspace()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            GivenACollidingWorkItemPair(team);
            GivenTwoBlockedDemoFeatures(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheTeamKeyspaceHasNoSpells();
        }

        // @us-05 @contract-shape:bounded-change (US-05 AC2 — idempotent: a second refresh adds nothing.
        // Asserted non-vacuously: spells exist after the first refresh AND the second adds none.)
        [Test]
        public async Task Backfilling_twice_leaves_the_feature_keyspace_unchanged()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenTwoBlockedDemoFeatures(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);
            var spellsAfterFirstRefresh = ReadFeatureSpells(portfolio.PortfolioId);
            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheFeatureKeyspaceIsUnchangedAndNonEmpty(portfolio, spellsAfterFirstRefresh);
        }

        // @error @us-05 @contract-shape:bounded-change (the slice's named ship-broken risk: after slice 01,
        // demo portfolios ALREADY have backdated snapshots but no feature spells — the legacy snapshot-based
        // idempotency guard would short-circuit and synthesize nothing on exactly the instances targeted)
        [Test]
        public async Task A_demo_portfolio_backfilled_before_the_feature_keyspace_existed_still_gains_feature_spells()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenTwoBlockedDemoFeatures(portfolio);
            GivenBackdatedSnapshotsButNoFeatureSpells(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheFeatureKeyspaceHoldsBackdatedSpellsForBothFeatures(portfolio);
        }

        // @edge @us-05 @contract-shape:bounded-change (US-05 AC4 — never claim a feature was blocked
        // before it was started; the StartedDate cap survives the retarget. Asserted non-vacuously:
        // at least one spell must exist for the cap check to mean anything.)
        [Test]
        public async Task No_backdated_spell_predates_the_features_start()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenARecentlyStartedBlockedDemoFeature(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenSpellsExistAndEverySpellStartsOnOrAfterItsFeatureStarted(portfolio);
        }

        // @driving_port @us-05 @contract-shape:pure-function (US-05 AC3 — bars worth clicking: a backdated
        // bar drills into the demo features blocked that day. Depends on slice 04's port; scaffolded here
        // because the claim belongs to this slice's elevator pitch.)
        [Test]
        public async Task A_backdated_bar_on_the_demo_portfolio_chart_drills_into_real_features()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenTwoBlockedDemoFeatures(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            await ThenYesterdaysBarListsTheBackfilledFeatures(portfolio);
        }

        // @edge @us-05 @contract-shape:unbounded-preservation (US-05 AC2 — the demo gate is the whole
        // safety argument: non-demo portfolios are untouched. Green today; pins the gate against the
        // retarget.)
        [Test]
        public async Task A_non_demo_portfolio_gains_no_backdated_spells()
        {
            var portfolio = GivenANonDemoPortfolioWhoseRulesBlockAFeatureState();
            GivenTwoBlockedDemoFeatures(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheFeatureKeyspaceIsEmpty(portfolio);
        }
    }
}
