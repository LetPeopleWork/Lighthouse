using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (portfolio-blocked-history, ADO #5524) — Slice 01 / US-01:
    /// stop demo blocked history from corrupting team blocked reads. Job: job-delivery-lead-see-blocked-trend.
    /// Persona: flow-coach on a demo/evaluation instance. Driving ports: PortfolioFeaturesRefreshed
    /// domain-event dispatch (runs the real DemoBlockedHistoryBackfillHandler on real SQLite) and the
    /// team metrics wip / blockedItemsAtDate / portfolio blockedCountHistory read ports.
    /// All scenarios [Ignore]-pending (RED-ready, ADR-025) — DELIVER enables one at a time.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("portfolio-blocked-history")]
    [Category("slice-01")]
    public partial class Slice01DemoBackfillTeamHistoryTest
    {
        // @walking_skeleton @driving_port @us-01 @contract-shape:bounded-change (US-01 AC1)
        // REGRESSION CLASS: RED today — the phantom spell corrupts the historic team read.
        [Test]
        [Category("walking_skeleton")]
        public async Task A_team_work_item_sharing_an_id_with_a_blocked_demo_feature_reads_not_blocked_on_a_past_range()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            var (featureId, workItemId) = GivenABlockedDemoFeatureAndANeverBlockedWorkItemSharingAnId(portfolio, team);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            await ThenTheWorkItemReadsNotBlockedOnAPastRange(team, workItemId);
        }

        // @driving_port @us-01 @contract-shape:bounded-change (US-01 AC2)
        [Test]
        public async Task The_team_drill_through_at_a_past_date_excludes_the_colliding_id()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            var (featureId, workItemId) = GivenABlockedDemoFeatureAndANeverBlockedWorkItemSharingAnId(portfolio, team);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            await ThenTheTeamDrillThroughExcludesTheWorkItem(team, workItemId);
        }

        // @us-01 @invariant @contract-shape:unbounded-preservation (US-01 AC3 — keyspace purity: with the
        // FK enforced, the literal "row without a WorkItem" invariant is unsurpassable, so the portfolio-
        // owner assertion is the meaningful form: a portfolio refresh writes ZERO rows into the team
        // keyspace. ADR-102 enforcement row; cross-checked again by slice 05 AC1.)
        [Test]
        public async Task The_demo_portfolio_refresh_writes_no_blocked_spells_into_the_team_keyspace()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            var (featureId, workItemId) = GivenABlockedDemoFeatureAndANeverBlockedWorkItemSharingAnId(portfolio, team);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenTheTeamKeyspaceHasNoSpells();
        }

        // @us-01 @error @real-io @contract-shape:bounded-change (US-01 AC4, DESIGN amendment — the FK-bite
        // branch: a demo feature id with NO matching work item aborts the whole backfill on a real FK-
        // enforcing database. RED only on SQLite; passes for broken AND fixed code on EF InMemory.)
        [Test]
        [Category("real-io")]
        public async Task Backdated_portfolio_snapshots_land_even_when_a_demo_feature_id_collides_with_no_work_item()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            GivenABlockedDemoFeatureAndAnIdBeyondEveryWorkItem(portfolio);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            ThenThePortfolioHasBackdatedSnapshots(portfolio);
        }

        // @us-01 @regression @contract-shape:bounded-change (US-01 AC4 — snapshot backfill is retained;
        // passes today and must keep passing: colliding-id branch where the insert succeeds.)
        [Test]
        public async Task The_demo_portfolio_blocked_trend_still_renders_its_backdated_history()
        {
            var portfolio = GivenADemoPortfolioWhoseRulesBlockAFeatureState();
            var team = GivenATeam();
            var (featureId, workItemId) = GivenABlockedDemoFeatureAndANeverBlockedWorkItemSharingAnId(portfolio, team);

            await WhenTheDemoPortfolioRefreshes(portfolio);

            await ThenThePortfolioTrendShowsBackdatedBlockedCounts(portfolio);
        }
    }
}
