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
            "\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default, and Ledger uses it as its own. Nothing was saved.";

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

        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_only_a_Team_uses_as_its_own_cannot_be_removed()
        {
            await WhenTheAdminRemovesTheField(CausedBy);
            var refusal = TheRefusal();

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(refusal.Message, Does.Contain("Ledger uses it as its own").And.Not.Contain("default"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(CausedBy));
            }
        }

        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_only_a_Teams_estimation_uses_cannot_be_removed()
        {
            await GivenTheOwnerUsesAsItsOwn(Ledger, OwnerSetting.Estimation, ReleaseTrain);

            await WhenTheAdminRemovesTheField(ReleaseTrain);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(TheRefusal().Message, Is.EqualTo("\"Release Train\" cannot be removed. Ledger uses it as its own. Nothing was saved."));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(ReleaseTrain));
            }
        }

        // Merchant Onboarding holds a field of its own for every setting, each a field nothing else uses.
        [TestCase(StoryPoints, TestName = "A field only a Portfolio's estimation uses cannot be removed")]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_only_a_Portfolio_uses_as_its_own_cannot_be_removed(string field)
        {
            await WhenTheAdminRemovesTheField(field);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(TheRefusal().Message, Is.EqualTo($"\"{field}\" cannot be removed. Merchant Onboarding uses it as its own. Nothing was saved."));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(field));
            }
        }

        [Test]
        [Category("ac-4.8")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_only_a_default_names_cannot_be_removed()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, ParentLink);

            await WhenTheAdminRemovesTheField(ParentLink);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(TheRefusal().Message, Does.Contain("It is the Teams' Parent Override Field default"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(ParentLink));
            }
        }

        [Test]
        [Category("ac-4.8")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_field_nobody_uses_is_removed_as_today()
        {
            await WhenTheAdminRemovesTheField(ReleaseTrain);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Not.Contain(ReleaseTrain));
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
                Assert.That(fraudSignalsPage.Source, Is.EqualTo("ConnectionDefault"), "Nothing was saved.");
            }
        }

        // A client that sends no source keeps the stored source, unless it sends a value that is set and
        // differs from what the Team reads, and then the Team owns that value.
        [TestCase(FraudSignals, CausedBy, null, "ConnectionDefault", CausedBy, TestName = "A follower sent back what it reads keeps following")]
        [TestCase(FraudSignals, null, null, "ConnectionDefault", CausedBy, TestName = "A follower sent back empty keeps following")]
        [TestCase(FraudSignals, ParentLink, null, "Own", ParentLink, TestName = "A follower sent another field owns it")]
        [TestCase(Ledger, null, null, "Own", CausedBy, TestName = "An owner sent back empty keeps its own field")]
        [TestCase(Ledger, ParentLink, null, "Own", ParentLink, TestName = "An owner sent another field owns that one")]
        [TestCase(FraudSignals, null, Source.Own, "Own", null, TestName = "A follower sent None as its own chooses None")]
        [TestCase(Ledger, ParentLink, Source.ConnectionDefault, "ConnectionDefault", CausedBy, TestName = "An owner sent as following drops its own value")]
        [Category("ac-4.9")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Team_saved_by_a_client_that_may_not_send_the_source(string team, string? sends, Source? withSource, string thenSource, string? thenReads)
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheOwnersAdminSaves(team, OwnerSetting.ParentOverrideField, sends, withSource);
            ThenTheOwnerSaveIsAccepted();
            var page = await WhatTheSettingsPageShows(team, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.Source, Is.EqualTo(thenSource));
                Assert.That(page.Reads, Is.EqualTo(thenReads));
            }
        }

        [TestCase(FraudSignals, RoundTrip.EchoingEveryProperty, "ConnectionDefault", CausedBy)]
        [TestCase(FraudSignals, RoundTrip.DroppingWhatItDoesNotKnow, "ConnectionDefault", CausedBy)]
        [TestCase(Ledger, RoundTrip.EchoingEveryProperty, "Own", CausedBy)]
        [TestCase(Ledger, RoundTrip.DroppingWhatItDoesNotKnow, "Own", CausedBy)]
        [TestCase(MerchantOnboarding, RoundTrip.EchoingEveryProperty, "Own", ParentLink, Ignore = PendingEpic6303.Slice08)]
        [TestCase(MerchantOnboarding, RoundTrip.DroppingWhatItDoesNotKnow, "Own", ParentLink, Ignore = PendingEpic6303.Slice08)]
        [TestCase(CardIssuing, RoundTrip.DroppingWhatItDoesNotKnow, "ConnectionDefault", null, Ignore = PendingEpic6303.Slice08)]
        [Category("ac-4.10")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_script_that_saves_back_what_it_read_changes_nothing(string owner, RoundTrip shape, string staysSource, string? staysReading)
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenAScriptSavesBackWhatItRead(owner, shape);
            ThenTheOwnerSaveIsAccepted();
            var page = await WhatTheSettingsPageShows(owner, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.Source, Is.EqualTo(staysSource));
                Assert.That(page.Reads, Is.EqualTo(staysReading));
            }
        }

        // Accepted when the rule was chosen: a copy read while the Team followed "Caused by" still says
        // "Caused by" after the default moved, and without a source that reads as the Team choosing it.
        [Test]
        [Category("ac-4.9")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_stale_copy_saved_back_without_its_source_makes_the_Team_own_the_old_default()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            var readBeforeTheDefaultMoved = await GivenAScriptReadTheSettingsOf(FraudSignals);
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, ParentLink);

            await WhenAScriptSavesBackItsStaleCopy(FraudSignals, readBeforeTheDefaultMoved, keepingTheSourceItRead: false);
            var page = await WhatTheSettingsPageShows(FraudSignals, OwnerSetting.ParentOverrideField);

            Assert.That((page.Source, page.Reads), Is.EqualTo(((string?)"Own", (string?)CausedBy)));
        }

        [Test]
        [Category("ac-4.9")]
        [Category("contract-shape:bounded-change")]
        public async Task A_stale_copy_saved_back_with_its_source_keeps_the_Team_following_the_new_default()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            var readBeforeTheDefaultMoved = await GivenAScriptReadTheSettingsOf(FraudSignals);
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, ParentLink);

            await WhenAScriptSavesBackItsStaleCopy(FraudSignals, readBeforeTheDefaultMoved, keepingTheSourceItRead: true);
            var page = await WhatTheSettingsPageShows(FraudSignals, OwnerSetting.ParentOverrideField);

            Assert.That((page.Source, page.Reads), Is.EqualTo(((string?)"ConnectionDefault", (string?)ParentLink)));
        }
    }
}
