using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// An Additional Field that a default, or any Team or Portfolio as its own value, points at cannot be
    /// removed: the save is refused, says who uses it, and nothing is saved. A script written before sources
    /// existed reads a Team's settings and saves them back; whatever shape it saves them in, no Team changes
    /// what it follows or what it reads. One consequence is accepted and pinned here: a script that saves back a
    /// copy read before the default moved makes that Team own the old value.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6304")]
    [Category("slice-05b")]
    [Category("us-04")]
    public class Slice05bReferencedFieldsAndOlderPayloadsScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string TheApprovedRefusal =
            "\"Caused by\" cannot be removed. It is the Teams' Parent sources default, and Ledger uses it as its own. Nothing was saved.";

        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("driving_port")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_the_default_and_a_Team_use_cannot_be_removed_and_the_refusal_says_who()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheAdminRemovesTheField(CausedBy);
            var refusal = TheRefusal();

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(refusal.Code, Is.EqualTo("additional-field-in-use"));
                Assert.That(refusal.Message, Is.EqualTo(TheApprovedRefusal));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(CausedBy), "Nothing was saved.");
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(CausedBy));
            }
        }

        // The owners are looked up when the save commits, not when it was opened, so a Team that started using
        // the field in between still blocks its removal.
        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Team_that_starts_using_the_field_while_the_removal_is_being_saved_still_blocks_it()
        {
            GivenWhileTheConnectionSaveIsOpen(OpsTooling, startsUsing: ReleaseTrain);

            await WhenTheAdminRemovesTheField(ReleaseTrain);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(TheRefusal().Message, Does.Contain("Ops Tooling uses it as its own"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(ReleaseTrain));
            }
        }

        // The admin never saw the save that landed after they opened the page, so telling them the field cannot be
        // removed would answer a question about a connection they are not looking at. They are told to reload first.
        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_stale_copy_that_removes_a_field_in_use_is_refused_as_stale()
        {
            var openedEarlier = await GivenAnAdminOpenedTheConnection();
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, ParentLink);

            await WhenThatAdminRemovesTheField(openedEarlier, CausedBy);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(TheRefusal().Code, Is.EqualTo("concurrency-conflict"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(CausedBy));
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.TeamsParentOverrideField), Is.EqualTo(ParentLink));
            }
        }

        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Team_cannot_name_a_field_its_connection_does_not_have()
        {
            await WhenTheOwnersAdminSavesAFieldOf(FraudSignals, JiraDcTreasury, TreasuryCausedBy);
            var fraudSignalsPage = await WhatTheSettingsPageShows(FraudSignals, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.BadRequest);
                Assert.That(TheRefusal().Code, Is.EqualTo("team-field-not-on-connection"));
                Assert.That(fraudSignalsPage.Source, Is.EqualTo("ConnectionDefault"), "Nothing was saved.");
            }
        }
    }
}
