using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (portfolio-blocked-history, ADO #5524) — Slice 04 / US-04:
    /// drill into a past portfolio blocked bar. Job: job-flow-coach-drill-into-blocked-trend-point.
    /// Persona: flow-coach (Priya). Driving port: portfolio metrics blockedItemsAtDate read.
    /// Membership is RECONSTRUCTED from FEATURE-keyspace spell intervals — read-only, never persisted
    /// (blockedMembershipAtDate source_of_truth). The ADR-099 guard's log line itself is a DELIVER
    /// inner-loop assertion; here we pin the observable contract the guard protects. All scenarios
    /// [Ignore]-pending (RED-ready, ADR-025).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("portfolio-blocked-history")]
    [Category("slice-04")]
    public partial class Slice04PortfolioBlockedDrillThroughTest
    {
        // @walking_skeleton @driving_port @us-04 @contract-shape:pure-function (US-04 AC1)
        [Test]
        [Category("walking_skeleton")]
        [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task Clicking_a_past_bar_lists_the_features_whose_spell_covered_that_date()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            var covered = GivenAFeatureWithASpellCovering(portfolio, "F-COVERED", SyncDay.AddDays(-14), SyncDay.AddDays(-5));
            GivenAFeatureWithASpellCovering(portfolio, "F-CLOSED-EARLY", SyncDay.AddDays(-20), SyncDay.AddDays(-15));
            var openCovered = GivenAFeatureWithASpellCovering(portfolio, "F-STILL-OPEN", SyncDay.AddDays(-12), null);

            var response = await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, SyncDay.AddDays(-10));

            ThenTheDialogListsExactly(response, "F-COVERED", "F-STILL-OPEN");
        }

        // @driving_port @us-04 @contract-shape:pure-function (US-04 AC2 — latest/today bar reconstructs
        // from the live rule set; covers the slice-08 carried finding: drive a TODAY-dated request)
        [Test]
        [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task The_latest_bar_reconstructs_from_the_live_blocked_set()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            GivenAFeatureInState(portfolio, "F-LIVE-BLOCKED", "Blocked");
            GivenAFeatureInState(portfolio, "F-LIVE-CLEAR", "In Progress");

            var response = await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, DateOnly.FromDateTime(DateTime.UtcNow.Date));

            ThenTheDialogListsExactly(response, "F-LIVE-BLOCKED");
        }

        // @edge @us-04 @contract-shape:pure-function (US-04 AC3 — pre-capture date: honest partial set,
        // never a fabricated full history; the completeness note copy is a FE concern)
        [Test]
        [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_date_before_capture_started_returns_the_reconstructable_set()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            GivenAFeatureWithASpellCovering(portfolio, "F-LATE", SyncDay.AddDays(-14), null);

            var response = await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, DateOnly.FromDateTime(SyncDay.AddDays(-40)));

            ThenTheDialogIsEmpty(response);
        }

        // @invariant @us-04 @adr-099 @contract-shape:pure-function (US-04 AC4 — reconciliation-quiet probe:
        // reconstructed membership count equals the captured snapshot for the same date, so the guard
        // has nothing to log. A divergence here means capture is genuinely broken.)
        [Test]
        [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task The_reconstructed_membership_count_reconciles_with_the_captured_snapshot()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            GivenAFeatureWithASpellCovering(portfolio, "R-1", SyncDay.AddDays(-9), null);
            GivenAFeatureWithASpellCovering(portfolio, "R-2", SyncDay.AddDays(-8), null);
            GivenACapturedSnapshotOf(portfolio, DateOnly.FromDateTime(SyncDay.AddDays(-7)), blockedCount: 2);

            var response = await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, SyncDay.AddDays(-7));

            ThenTheReconstructedCountIs(response, 2);
        }

        // @edge @us-04 @adr-104 @contract-shape:pure-function (departure — read-side defensive
        // intersection mirrors the team shape: a feature no longer in the portfolio is excluded from
        // the drill-through even for dates inside its spell. The sweep that closes its spell row is a
        // DELIVER inner-loop pin; see distill/wave-decisions.md.)
        [Test]
        [Ignore("DISTILL scaffold — RED pending DELIVER (ADR-025). Enable one at a time.")]
        public async Task A_feature_that_left_the_portfolio_does_not_appear_even_on_dates_inside_its_spell()
        {
            var portfolio = GivenAPortfolioWhoseRulesBlockAFeatureState();
            GivenADepartedFeatureWithASpellCovering(portfolio, "F-GONE", SyncDay.AddDays(-14), SyncDay.AddDays(-5));

            var response = await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, SyncDay.AddDays(-10));

            ThenTheDialogIsEmpty(response);
        }
    }
}
