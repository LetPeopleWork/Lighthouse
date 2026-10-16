using System.Net;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Priya, System Admin of the Payments department, sets "Caused by" once on "Jira DC – Payments" as the
    /// Teams' Parent Override Field. Fraud Signals, Ops Tooling and Mobile Wallet named nothing, so they follow
    /// it; Ledger keeps the "Caused by" it chose itself; a Team that deliberately chose None keeps reading the
    /// tracker's own parent. One log entry per save names who follows. New Teams follow from the start.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6304")]
    [Category("slice-05a")]
    [Category("us-04")]
    public class Slice05aTeamsParentOverrideDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string Chargebacks = "Chargebacks";

        private static readonly string[] TheOffersOfJiraAndAzureDevOps =
        [
            "parentOverrideForTeams", "estimationForTeams",
            "parentOverrideForPortfolios", "dependencyFieldForPortfolios", "sizeEstimateFieldForPortfolios", "featureOwnerFieldForPortfolios", "estimationForPortfolios",
        ];

        private static readonly string[] TheOffersOfCsv = ["estimationForTeams", "featureOwnerFieldForPortfolios", "estimationForPortfolios"];

        private static readonly string[] NothingOffered = [];

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.1")]
        [Category("ac-4.2")]
        [Category("ac-f.1")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task One_save_on_the_connection_and_every_Team_that_named_nothing_reads_its_parents_from_the_default()
        {
            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            ThenTheSaveIsAccepted();
            await WhenTheNextScheduledUpdateOf(FraudSignals);
            var fraudSignalsPage = await WhatTheSettingsPageShows(FraudSignals, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhereTheItemOfHangs(FraudSignals), Is.EqualTo(WhatCausedByHolds), "The Team's Work Items hang under what Caused by names.");
                Assert.That(fraudSignalsPage, Is.EqualTo(((string?)"ConnectionDefault", (string?)CausedBy, (string?)CausedBy)));
                Assert.That(await HowManyFollow(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(3), "The save dialog counts the followers from this.");
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Count.EqualTo(1));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("3 Teams follow it: Fraud Signals, Ops Tooling, Mobile Wallet"));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.2")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Team_with_its_own_Parent_Override_Field_is_unaffected_by_the_default()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, ParentLink);

            await WhenTheNextScheduledUpdateOf(Ledger);
            var ledgersPage = await WhatTheSettingsPageShows(Ledger, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhereTheItemOfHangs(Ledger), Is.EqualTo(WhatCausedByHolds));
                Assert.That(ledgersPage.Source, Is.EqualTo("Own"));
                Assert.That(ledgersPage.Reads, Is.EqualTo(CausedBy));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Team_that_chose_None_keeps_the_trackers_own_parent_whatever_the_default_says()
        {
            await GivenTheTeamChoseNone(MobileWallet);
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheNextScheduledUpdateOf(MobileWallet);
            var mobileWalletsPage = await WhatTheSettingsPageShows(MobileWallet, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhereTheItemOfHangs(MobileWallet), Is.EqualTo(TheTrackersOwnParent));
                Assert.That(mobileWalletsPage, Is.EqualTo(((string?)"Own", (string?)CausedBy, (string?)null)));
                Assert.That(await HowManyFollow(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(2), "None is a choice, not following.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.5")]
        [Category("contract-shape:bounded-change")]
        public async Task A_new_Team_follows_the_default_from_the_start()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenANewTeamIsCreated(Chargebacks);
            var chargebacksPage = await WhatTheSettingsPageShows(Chargebacks, OwnerSetting.ParentOverrideField);

            Assert.That(chargebacksPage, Is.EqualTo(((string?)"ConnectionDefault", (string?)CausedBy, (string?)CausedBy)));
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.6")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_admin_without_connection_rights_sees_which_field_the_Team_follows()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            var seen = await WhatTheTeamAdminSees(OpsTooling);

            Assert.That(seen, Is.EqualTo(((string?)"ConnectionDefault", (string?)CausedBy, (string?)"Jira")),
                "The page shows \"Connection default (Caused by)\" and \"Set on the Jira connection.\"");
        }

        [Test]
        [Category("ac-4.1")]
        [Category("error")]
        [Category("active-guard")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Someone_without_connection_rights_cannot_save_the_connection()
        {
            await WhenSomeoneWithoutConnectionRightsSetsTheDefault(OpsTooling);

            ThenTheSaveIsRefusedWith(HttpStatusCode.Forbidden);
        }

        [TestCase(PaymentsAzureDevOps, TestName = "Azure DevOps offers every default")]
        [TestCase(JiraDcPayments, TestName = "Jira offers every default")]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.7")]
        [Category("contract-shape:pure-function")]
        public async Task Jira_and_Azure_DevOps_offer_every_default(string connection)
        {
            await GivenAConnectionOfEveryOtherTracker();

            Assert.That(await WhatTheConnectionOffersAsDefaults(connection), Is.Not.Null.And.EquivalentTo(TheOffersOfJiraAndAzureDevOps));
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.7")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task A_CSV_connection_offers_only_Feature_Owner_and_Estimation()
        {
            await GivenAConnectionOfEveryOtherTracker();

            Assert.That(await WhatTheConnectionOffersAsDefaults(PaymentsCsv), Is.Not.Null.And.EquivalentTo(TheOffersOfCsv));
        }

        [TestCase(PaymentsLinear, TestName = "Linear offers no default")]
        [TestCase(PaymentsServiceNow, TestName = "ServiceNow offers no default")]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.7")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task Linear_and_ServiceNow_offer_no_default(string connection)
        {
            await GivenAConnectionOfEveryOtherTracker();

            Assert.That(await WhatTheConnectionOffersAsDefaults(connection), Is.Not.Null.And.EqualTo(NothingOffered),
                "A connection that offers no default says so with an empty list.");
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.7")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_default_the_tracker_does_not_honour_is_refused_and_nothing_is_saved()
        {
            await GivenAConnectionOfEveryOtherTracker();

            await WhenTheAdminSetsTheDefaultOn(PaymentsCsv, ConnectionDefault.TeamsParentOverrideField, CausedBy);
            var csvAfterwards = await WhatTheConnectionPageReads(PaymentsCsv);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.BadRequest);
                Assert.That(csvAfterwards["defaults"], Is.InstanceOf<JsonObject>(), "The connection page reads the connection's defaults.");
                Assert.That(csvAfterwards["defaults"]?["parentOverrideForTeams"], Is.Null);
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-f.3")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_that_changes_no_default_writes_no_log_entry()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheAdminRenamesTheConnection();

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(TheLogEntriesAboutChangedDefaults(), Is.Empty);
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(CausedBy));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-f.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_default_nobody_follows_still_writes_an_entry_saying_none_follow()
        {
            await GivenEveryTeamOwnsItsParentOverrideField();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Count.EqualTo(1));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("no Team follows it"));
                Assert.That(await HowManyFollow(ConnectionDefault.TeamsParentOverrideField), Is.Zero, "With nobody following, the page saves without a dialog.");
            }
        }

        // A script that knows nothing of defaults must not wipe them by leaving them out.
        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.1")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_without_the_defaults_keeps_every_default()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenAScriptSavesTheConnectionWithoutItsDefaults();

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(CausedBy));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Is.Empty);
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.1")]
        [Category("ac-f.1")]
        [Category("contract-shape:bounded-change")]
        public async Task Clearing_a_default_stops_the_followers_reading_it_and_is_logged()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheAdminClearsTheDefault(ConnectionDefault.TeamsParentOverrideField);
            ThenTheSaveIsAccepted();
            await WhenTheNextScheduledUpdateOf(FraudSignals);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.Null);
                Assert.That(await WhereTheItemOfHangs(FraudSignals), Is.EqualTo(TheTrackersOwnParent));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Count.EqualTo(1));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("from \"Caused by\""));
            }
        }

        // The connection already carries a concurrency token; a default rides on the same save, so the admin
        // whose copy is stale is told so instead of overwriting the other admin's default.
        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.1")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_admin_whose_copy_is_stale_is_refused_and_the_first_default_stands()
        {
            var opensFirst = await GivenAnAdminOpenedTheConnection();
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenThatAdminSavesTheDefault(opensFirst, ConnectionDefault.TeamsParentOverrideField, ParentLink);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(CausedBy));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Is.Empty, "The refused save wrote nothing.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice05a)]
        [Category("ac-4.1")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_field_added_in_the_same_save_can_be_the_default()
        {
            await WhenTheAdminAddsAFieldAndMakesItTheDefault(ConnectionDefault.TeamsParentOverrideField, "Epic Link", "customfield_10008");

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo("Epic Link"));
            }
        }
    }
}
