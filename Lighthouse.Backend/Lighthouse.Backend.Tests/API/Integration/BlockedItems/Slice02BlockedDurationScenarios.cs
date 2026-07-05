using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 02: per-item blocked duration.
    /// Job: job-flow-coach-see-how-long-blocked. Persona: flow-coach (Priya). Driving port: team metrics
    /// WIP read (WorkItemDto.blockedSince). All scenarios [Ignore]-pending — enable one at a time in DELIVER.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-02")]
    public partial class Slice02BlockedDurationTest
    {
        // @driving_port @us-02
        [Test]
        public async Task A_newly_blocked_item_shows_how_long_it_has_been_blocked()
        {
            var team = GivenATeamThatTreatsAStateAsBlocked("Blocked");
            GivenABlockedItemObservedDaysAgo(team, "PHX-204", "Blocked", daysAgo: 2);

            var item = await WhenPriyaOpensTheTeamView(team, "PHX-204");

            ThenTheItemExposesABlockedDuration(item);
        }

        // @edge @us-02 (unblocked item clears its duration)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-02 (spell closes on unblock)")]
        public async Task An_unblocked_item_no_longer_shows_a_blocked_duration()
        {
            var team = GivenATeamThatTreatsAStateAsBlocked("Blocked");
            GivenAnItemThatIsNotBlocked(team, "PHX-204");

            var item = await WhenPriyaOpensTheTeamView(team, "PHX-204");

            ThenTheItemExposesNoBlockedDuration(item);
        }

        // @edge @us-02 (first-observation baseline)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-02 (first-observation shows no fabricated history)")]
        public async Task A_first_observation_blocked_item_shows_no_duration_until_a_baseline_exists()
        {
            var team = GivenATeamThatTreatsAStateAsBlocked("Blocked");
            GivenABlockedItemFirstObservedThisSync(team, "PHX-110", "Blocked");

            var item = await WhenPriyaOpensTheTeamView(team, "PHX-110");

            ThenTheItemExposesNoBlockedDuration(item);
        }

        // @property @us-02 (duration derives only from rule-matched items — single definition)
        [Test]
        [Category("property")]
        [Ignore("DISTILL pending — enable in DELIVER slice-02 (blockedSince only for IsBlocked items)")]
        public async Task An_item_that_does_not_match_the_blocked_rules_has_no_blocked_duration()
        {
            var team = GivenATeamThatTreatsAStateAsBlocked("Blocked");
            GivenABlockedItemObservedDaysAgo(team, "PHX-77", blockedState: "In Progress", daysAgo: 20);

            var item = await WhenPriyaOpensTheTeamView(team, "PHX-77");

            ThenTheItemExposesNoBlockedDuration(item);
        }
    }
}
