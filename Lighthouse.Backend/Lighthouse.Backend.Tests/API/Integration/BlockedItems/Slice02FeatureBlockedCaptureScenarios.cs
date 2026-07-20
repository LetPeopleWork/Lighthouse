using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (portfolio-blocked-history, ADO #5524) — Slice 02 / US-02:
    /// capture feature blocked spells and surface blocked duration. Job: job-flow-coach-see-how-long-blocked.
    /// Persona: flow-coach (Priya). Driving ports: the portfolio refresh service port
    /// (IWorkItemService.UpdateFeaturesForPortfolio — real service, real EF, real dispatcher; only the
    /// connector boundary is faked, per the project infrastructure policy) and the portfolio metrics
    /// wip read port. Row-level spell semantics (open/close/re-block new spell, idempotent handlers,
    /// id-timing second pass, departure sweep + empty-refresh guard) are DELIVER inner-loop unit tests
    /// — see distill/wave-decisions.md. All scenarios [Ignore]-pending (RED-ready, ADR-025).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("portfolio-blocked-history")]
    [Category("slice-02")]
    public partial class Slice02FeatureBlockedCaptureTest
    {
        // @walking_skeleton @driving_port @us-02 @contract-shape:bounded-change (US-02 AC1+AC3)
        [Test]
        [Category("walking_skeleton")]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_that_becomes_blocked_shows_how_long_it_has_been_blocked()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            await GivenTheFeatureWasObservedNotBlocked(portfolio, "F-100");

            await WhenThePortfolioRefreshesAndTheFeatureNowMatchesTheBlockedRules(portfolio, "F-100");

            await ThenTheFeatureExposesABlockedDuration(portfolio, "F-100");
        }

        // @driving_port @us-02 @contract-shape:bounded-change (US-02 AC2 — leave clears the duration)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_that_stops_matching_the_blocked_rules_no_longer_shows_a_duration()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            await GivenTheFeatureWasObservedBlocked(portfolio, "F-100");

            await WhenThePortfolioRefreshesAndTheFeatureNoLongerMatchesTheBlockedRules(portfolio, "F-100");

            await ThenTheFeatureExposesNoBlockedDuration(portfolio, "F-100");
        }

        // @edge @us-02 @contract-shape:bounded-change (US-02 AC4 — first-observation "—": a feature
        // already blocked when capture first sees it opens NO spell and exposes no duration)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_blocked_on_its_first_observation_shows_no_duration_until_a_baseline_exists()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();

            await WhenThePortfolioRefreshesAndTheFeatureNowMatchesTheBlockedRules(portfolio, "F-100");

            await ThenTheFeatureExposesNoBlockedDuration(portfolio, "F-100");
        }

        // @driving_port @us-02 @adr-103 @contract-shape:bounded-change (DDD-2 observable behaviour change —
        // the divergent-rule-set probe ADR-103 requires as an integration test: a feature blocked only
        // by Portfolio A's rules must NOT render blocked on Portfolio B's page.)
        [Test]
        // [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_blocked_only_by_one_portfolios_rules_does_not_render_blocked_on_the_other_portfolio()
        {
            var portfolioA = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var portfolioB = GivenAPortfolioWhoseRulesBlockNothing();
            GivenASharedFeature(portfolioA, portfolioB, "F-SHARED");

            await WhenBothPortfoliosRefresh(portfolioA, portfolioB);

            await ThenTheFeatureReadsBlockedOn(portfolioA, "F-SHARED");
            await ThenTheFeatureReadsNotBlockedOn(portfolioB, "F-SHARED");
        }

        // @regression @us-02 @adr-103 @contract-shape:pure-function (Any-portfolio read projection —
        // scope-free surfaces keep today's behaviour. Pre-DDD-7 the DTO Any-computes the rule match;
        // post-DDD-7 the site Any-projects over open spells — so the scenario drives a real block edge
        // (two refreshes) to open a spell, staying green BOTH ways: the invariant the refactor must not
        // break is "a feature blocked somewhere renders blocked on the scope-free team feature list".)
        [Test]
        // [Ignore("DISTILL scaffold — REGRESSION GUARD (green at authoring, properly skipped). Enable with slice 02.")]
        public async Task The_scope_free_team_feature_list_still_reports_the_shared_feature_blocked()
        {
            var portfolioA = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var portfolioB = GivenAPortfolioWhoseRulesBlockNothing();
            var team = GivenATeam();
            GivenASharedFeature(portfolioA, portfolioB, "F-SHARED", team);
            await GivenTheFeatureWasObservedNotBlocked(portfolioA, "F-SHARED");
            await WhenThePortfolioRefreshesAndTheFeatureNowMatchesTheBlockedRules(portfolioA, "F-SHARED");

            await ThenTheTeamFeatureListReportsTheFeatureBlocked(team, "F-SHARED");
        }
    }
}
