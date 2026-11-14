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
        [Test]
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

        [Test]
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
                Assert.That(TheRefusal().Code, Is.EqualTo("connection-default-not-supported"));
                Assert.That(csvAfterwards["defaults"], Is.InstanceOf<JsonObject>(), "The connection page reads the connection's defaults.");
                Assert.That(csvAfterwards["defaults"]?["parentOverrideForTeams"], Is.Null);
            }
        }

        // The connection already carries a concurrency token; a default rides on the same save, so the admin
        // whose copy is stale is told so instead of overwriting the other admin's default.
        [Test]
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
    }
}
