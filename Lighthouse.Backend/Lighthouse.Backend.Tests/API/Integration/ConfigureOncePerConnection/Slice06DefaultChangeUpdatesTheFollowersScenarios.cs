namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Priya changes the Teams' Parent sources default from "Caused by" to "Parent Link" and chooses Save
    /// and update. The three Teams that follow it start updating at once, each downloading everything, because
    /// what they ask the tracker for has changed; Ledger, which owns its value, does not. Save only queues
    /// nothing, and the followers pick the change up at their next scheduled update. Either way one log entry
    /// says what changed, who follows, and how many updates were queued.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class Slice06DefaultChangeUpdatesTheFollowersScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string TheApprovedLogEntry =
            "Connection \"Jira DC – Payments\" changed the Teams' Parent sources default from \"Caused by\" to \"Parent Link\"; "
            + "3 Teams follow it: Fraud Signals, Ops Tooling, Mobile Wallet; 3 updates queued; parent sources moved 3 Teams.";

        private static readonly string[] EachFollowerDownloadsEverything = ["Fraud Signals Full", "Ops Tooling Full", "Mobile Wallet Full"];

        private static readonly string[] OpsToolingDownloadsEverything = ["Ops Tooling Full"];

        private static readonly string[] OnlyTheApprovedLogEntry = [TheApprovedLogEntry];

        [Test]
        public async Task Save_and_update_updates_exactly_the_Teams_that_follow_the_changed_default()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, ParentLink, startUpdates: true);
            ThenTheSaveIsAccepted();
            var listed = await WhatTheTaskManagerListsSoonAfterTheSave(expected: 3);
            var ran = await WhatRanSinceTheSave(TheThreeFollowers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Is.EquivalentTo(TheThreeFollowers), "Ledger owns its value and nothing else reads this default.");
                Assert.That(ran, Is.EquivalentTo(EachFollowerDownloadsEverything));
                Assert.That(await WhereTheItemOfHangs(FraudSignals), Is.EqualTo(WhatParentLinkHolds));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Is.EqualTo(OnlyTheApprovedLogEntry));
            }
        }

        [Test]
        public async Task After_save_only_a_follower_picks_the_change_up_at_its_next_scheduled_update()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();
            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, ParentLink, startUpdates: false);

            await WhenTheNextScheduledUpdateOf(OpsTooling);
            var ran = await WhatRanSinceTheSave(OpsTooling);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.EqualTo(OpsToolingDownloadsEverything), "What it asks the tracker for moved, so it downloads everything once.");
                Assert.That(await WhereTheItemOfHangs(OpsTooling), Is.EqualTo(WhatParentLinkHolds));
            }
        }

    }
}
