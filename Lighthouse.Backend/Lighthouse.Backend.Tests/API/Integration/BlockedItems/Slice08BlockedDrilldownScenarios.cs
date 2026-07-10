using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 08 / B1: drill from a point on the blocked
    /// trend into the items that were blocked at that date. Job: job-flow-coach-drill-into-blocked-trend-point.
    /// Persona: flow-coach. Driving port: NEW team-metrics blockedItemsAtDate read endpoint (ADR-099).
    /// Membership is RECONSTRUCTED from WorkItemBlockedTransition interval overlap — no persisted
    /// membership, BlockedCountSnapshot unchanged. All scenarios [Ignore]-pending (RED-ready, ADR-025) —
    /// DELIVER enables one at a time. The chart bar-click wiring + WorkItemsDialog render are FE
    /// (Vitest / Playwright) concerns; here we drive the reconstruct endpoint.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-08")]
    public partial class Slice08BlockedDrilldownTest : BlockedItemsAcceptanceTest
    {
        // @driving_port @us-eb1 (happy: reconstruct membership at a past date from transition intervals)
        [Test]
        public async Task Items_blocked_at_a_past_date_are_reconstructed_from_transition_intervals()
        {
            var team = GivenATeam();
            var t = DateOnly.FromDateTime(SyncDay.AddDays(-10));
            // BLK-1 and BLK-2 have a blocked spell that COVERS t; OPEN closed its spell before t.
            GivenABlockedItemWithTransition(team, "BLK-1", enteredAt: SyncDay.AddDays(-14), leftAt: null);
            GivenABlockedItemWithTransition(team, "BLK-2", enteredAt: SyncDay.AddDays(-12), leftAt: SyncDay.AddDays(-5));
            GivenABlockedItemWithTransition(team, "OPEN", enteredAt: SyncDay.AddDays(-20), leftAt: SyncDay.AddDays(-15));

            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, t);

            ThenTheDialogListsExactly(response, "BLK-1", "BLK-2");
        }

        // @driving_port @us-eb1 (happy: latest bar reconstructs from live IsBlocked)
        [Test]
        public async Task The_latest_date_reconstructs_from_the_live_blocked_set()
        {
            var team = GivenATeam();
            GivenABlockedItemWithTransition(team, "NOW-1", enteredAt: SyncDay.AddDays(-3), leftAt: null);

            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, DateOnly.FromDateTime(SyncDay));

            ThenTheDialogListsExactly(response, "NOW-1");
        }

        // @invariant @us-eb1 (post-review bug fix: the latest-date set is the OPEN blocked population —
        // To Do + In Progress — matching the bar/overview count; a Done item that still matches the rule
        // must NOT appear, and a blocked To Do item MUST appear)
        [Test]
        public async Task The_latest_date_lists_to_do_and_in_progress_blocked_items_but_not_done()
        {
            var team = GivenATeam();
            GivenABlockedItemInStateCategory(team, "TODO-BLK", StateCategories.ToDo);
            GivenABlockedItemInStateCategory(team, "DOING-BLK", StateCategories.Doing);
            GivenABlockedItemInStateCategory(team, "DONE-BLK", StateCategories.Done);

            // Drill at the real "today" (UtcNow.Date) so the endpoint takes the live latest-date branch —
            // the fixed test SyncDay (2026-06-15) is in the past and would route to interval reconstruction.
            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, DateOnly.FromDateTime(DateTime.UtcNow.Date));

            ThenTheDialogListsExactly(response, "TODO-BLK", "DOING-BLK");
        }

        // @edge @us-eb1 (no items blocked at the clicked date -> honest empty, not an error)
        [Test]
        public async Task A_date_with_no_blocked_items_returns_an_empty_dialog()
        {
            var team = GivenATeam();
            GivenABlockedItemWithTransition(team, "PAST", enteredAt: SyncDay.AddDays(-20), leftAt: SyncDay.AddDays(-18));

            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, DateOnly.FromDateTime(SyncDay.AddDays(-2)));

            ThenTheDialogIsEmpty(response);
        }

        // @invariant @us-eb1 (reconciliation: reconstructed count == BlockedCountSnapshot for that date)
        [Test]
        public async Task The_reconstructed_membership_count_reconciles_with_the_snapshot_count()
        {
            var team = GivenATeam();
            var t = DateOnly.FromDateTime(SyncDay.AddDays(-7));
            GivenABlockedItemWithTransition(team, "R-1", enteredAt: SyncDay.AddDays(-9), leftAt: null);
            GivenABlockedItemWithTransition(team, "R-2", enteredAt: SyncDay.AddDays(-8), leftAt: null);
            GivenBlockedCountSnapshots(team, new List<(DateOnly Date, int Count)> { (t, 2) });

            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, t);

            ThenTheReconstructedCountMatchesTheSnapshot(response, expectedCount: 2);
        }

        // @edge @us-eb1 (a date before capture started -> honest partial set, never a fabricated full history)
        [Test]
        public async Task A_date_before_capture_started_is_served_as_a_partial_set()
        {
            var team = GivenATeam();
            var beforeCapture = DateOnly.FromDateTime(SyncDay.AddDays(-40));
            // The only captured spell began at -14d — nothing is reconstructable at -40d.
            GivenABlockedItemWithTransition(team, "LATE", enteredAt: SyncDay.AddDays(-14), leftAt: null);

            var response = await WhenTheCoachDrillsIntoTheBlockedTrendAt(team, beforeCapture);

            ThenTheDialogIsEmpty(response);
        }
    }
}
