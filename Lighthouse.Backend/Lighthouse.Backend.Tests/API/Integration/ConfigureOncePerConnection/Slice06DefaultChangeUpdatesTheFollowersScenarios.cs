namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Priya changes the Teams' Parent Override Field default from "Caused by" to "Parent Link" and chooses Save
    /// and update. The three Teams that follow it start updating at once, each downloading everything, because
    /// what they ask the tracker for has changed; Ledger, which owns its value, does not. Save only queues
    /// nothing, and the followers pick the change up at their next scheduled update. Either way one log entry
    /// says what changed, who follows, and how many updates were queued.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6304")]
    [Category("slice-06")]
    [Category("us-05")]
    public class Slice06DefaultChangeUpdatesTheFollowersScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string TheApprovedLogEntry =
            "Connection \"Jira DC – Payments\" changed the Teams' Parent Override Field default from \"Caused by\" to \"Parent Link\"; "
            + "3 Teams follow it: Fraud Signals, Ops Tooling, Mobile Wallet; 3 updates queued.";

        private static readonly string[] EachFollowerDownloadsEverything = ["Fraud Signals Full", "Ops Tooling Full", "Mobile Wallet Full"];

        private static readonly string[] OpsToolingDownloadsEverything = ["Ops Tooling Full"];

        private static readonly string[] OnlyTheApprovedLogEntry = [TheApprovedLogEntry];

        [Test]
        [Ignore(PendingEpic6303.Slice06)]
        [Category("ac-5.1")]
        [Category("ac-5.3")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
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
        [Ignore(PendingEpic6303.Slice06)]
        [Category("ac-5.2")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Save_only_starts_no_update_and_still_names_the_followers()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, ParentLink, startUpdates: false);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.Empty);
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Count.EqualTo(1));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("3 Teams follow it: Fraud Signals, Ops Tooling, Mobile Wallet"));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice06)]
        [Category("ac-5.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
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

        [Test]
        [Ignore(PendingEpic6303.Slice06)]
        [Category("ac-5.1")]
        [Category("ac-f.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task When_nobody_follows_the_default_nothing_updates_and_the_entry_says_none_follow()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            await GivenEveryTeamOwnsItsParentOverrideField();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.Empty);
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("no Team follows it"));
            }
        }

        // A changed default is not a changed field: the page sends startUpdates with no field changed, and that
        // must not start the update of every Team and Portfolio the way a field change does.
        [Test]
        [Ignore(PendingEpic6303.Slice06)]
        [Category("ac-5.1")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_changed_default_does_not_update_the_owners_or_the_Portfolios()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, ParentLink, startUpdates: true);
            var ran = await WhatRanSinceTheSave(TheThreeFollowers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Has.Some.StartsWith(FraudSignals), "The followers did update.");
                Assert.That(ran, Has.None.StartsWith(Ledger).And.None.StartsWith(CardIssuing).And.None.StartsWith(MerchantOnboarding));
            }
        }
    }
}
