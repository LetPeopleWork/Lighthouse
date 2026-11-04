using System.Net;
using Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OrderedParentSources
{
    /// <summary>
    /// The parent sources a connection holds as the default for its Teams. They run through the running
    /// application on the harness of the neighbouring connection-defaults scenarios, where a System Admin saves a
    /// connection and the Teams that follow it update. The harness has gained the helpers that save a default as a
    /// list and read one back by name; the neighbours that arrange the old single-field default move to the list
    /// in the same step as the code they exercise.
    ///
    /// The scenarios are written against the planned shape of the connection's defaults and the owner's settings
    /// (<c>parentSourcesForTeams</c> and <c>parentSources</c>, in the same words the browser sends). What is
    /// missing until the slice builds it: those two members, the follower queueing that reads the list, and the
    /// harness's stand-in tracker, which still hangs an item under the first field it is handed, so "reads the new
    /// order" is shown by which field the tracker was asked to read first. An old single default shown as the field
    /// then the tracker's own parent is pinned by the migration class. The rule that a field in use cannot be
    /// removed from its connection is the field-in-use scenario in this class.
    /// </summary>
    [TestFixture]
    [Ignore("Story 6501: needs the connection's parentSourcesForTeams default, the owner's parentSources and the list-aware follower queueing.")]
    public class ConnectionParentSourcesScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string Chargebacks = "Chargebacks";

        /// <summary>
        /// Given three Teams follow the Jira connection's default for Teams and Ledger owns "Parent Link" then
        /// "Caused by", when a System Admin saves "Parent Link", "Caused by" and the tracker's own parent as that
        /// default and chooses Save and update, then exactly the three followers update and read the new order,
        /// Ledger reads its own list unchanged and is not updated, and the Portfolios' default, which is separate,
        /// is untouched.
        /// </summary>
        [Test]
        public async Task A_change_of_the_default_updates_exactly_the_Teams_that_follow_it_and_leaves_a_Team_with_its_own_list_alone()
        {
            await GivenTheTeamsListIs(CausedBy);
            await GivenThePortfoliosListIs(ParentLink);
            await GivenTheTeamOwnsTheList(Ledger, ParentLink, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheTeamsListTo(true, ParentLink, CausedBy, TheTrackersOwnParentSource);
            ThenTheSaveIsAccepted();
            var listed = await WhatTheTaskManagerListsSoonAfterTheSave(expected: 3);
            var ran = await WhatRanSinceTheSave(TheThreeFollowers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Is.EquivalentTo(TheThreeFollowers), "Ledger owns its list, so nothing about the default reaches it.");
                Assert.That(ran, Is.EquivalentTo(TheThreeFollowers.Select(follower => $"{follower} Full")));
                Assert.That(await TheSourcesTheSettingsOfShow(FraudSignals), Is.EqualTo(new[] { ParentLink, CausedBy, TheTrackersOwnParentSource }).AsCollection);
                Assert.That(WhatTheTrackerWasAskedToReadFor(FraudSignals, OwnerSetting.ParentOverrideField), Is.EqualTo(ParentLink));
                Assert.That(await TheSourcesTheSettingsOfShow(Ledger), Is.EqualTo(new[] { ParentLink, CausedBy }).AsCollection);
                Assert.That(await TheSourcesOfThePortfoliosDefaultOn(JiraDcPayments), Is.EqualTo(new[] { ParentLink }).AsCollection);
            }
        }

        /// <summary>
        /// Given the connection default for Teams is "Caused by" then "Parent Link", when a Team is created without
        /// choosing a Parent source, then it follows the default, its settings page shows that list, and its first
        /// update reads the list.
        /// </summary>
        [Test]
        public async Task A_new_Team_follows_the_default_and_reads_its_list_on_the_first_update()
        {
            await GivenTheTeamsListIs(CausedBy, ParentLink);

            await WhenANewTeamIsCreated(Chargebacks);
            await WhenTheNextScheduledUpdateOf(Chargebacks);
            var page = await WhatTheSettingsPageShows(Chargebacks, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.Source, Is.EqualTo("ConnectionDefault"));
                Assert.That(await TheSourcesTheSettingsOfShow(Chargebacks), Is.EqualTo(new[] { CausedBy, ParentLink }).AsCollection);
                Assert.That(WhatTheTrackerWasAskedToReadFor(Chargebacks, OwnerSetting.ParentOverrideField), Is.EqualTo(CausedBy));
            }
        }

        /// <summary>
        /// Given Ledger owns "Parent Link" then "Caused by", when a System Admin removes "Caused by" from the
        /// connection, then the save is refused naming Ledger, whichever place in its list the field holds, and
        /// nothing is removed; and once Ledger no longer lists it but the Teams default does, the save is refused
        /// naming the default.
        /// </summary>
        [Test]
        public async Task A_field_cannot_be_removed_from_its_connection_while_any_list_holds_it_in_any_place()
        {
            await GivenTheTeamOwnsTheList(Ledger, ParentLink, CausedBy);

            await WhenTheAdminRemovesTheField(CausedBy);
            var ownersRefusal = TheRefusal();
            var statusForTheOwner = SaveAnswer;

            await GivenTheTeamOwnsTheList(Ledger, ParentLink);
            await GivenTheTeamsListIs(ParentLink, CausedBy);
            await WhenTheAdminRemovesTheField(CausedBy);
            var defaultsRefusal = TheRefusal();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(statusForTheOwner, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(ownersRefusal.Code, Is.EqualTo("additional-field-in-use"));
                Assert.That(ownersRefusal.Message, Does.Contain("Ledger uses it as its own"));
                ThenTheSaveIsRefusedWith(HttpStatusCode.Conflict);
                Assert.That(defaultsRefusal.Message, Does.Contain("Teams").And.Contain("default"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(CausedBy), "Nothing was saved.");
            }
        }

        /// <summary>
        /// Given a connection, when a System Admin saves the Teams default as a list and reads the connection back,
        /// then the list comes back in the same order and the old single number on the wire is its first field;
        /// a later save that leaves the list out leaves the default as it was, and one that sends an explicit null
        /// clears it together with the old number.
        /// </summary>
        [Test]
        public async Task A_System_Admin_saves_the_default_as_a_list_and_reads_it_back_and_a_save_that_leaves_it_out_changes_nothing()
        {
            var whileTheDefaultIsUnset = await TheSourcesTheSettingsOfShow(FraudSignals);

            await WhenTheAdminSetsTheTeamsListTo(false, ParentLink, CausedBy);
            ThenTheSaveIsAccepted();
            var afterSaving = await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments);
            var oldNumber = await TheOldNumberTheConnectionStillCarriesForTeams(JiraDcPayments);
            var firstField = await TheNumberOfTheField(JiraDcPayments, ParentLink);

            await WhenAScriptSavesTheConnectionLeavingTheTeamsListOut();
            ThenTheSaveIsAccepted();
            var afterTheScript = await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments);

            await WhenTheAdminClearsTheTeamsList();
            ThenTheSaveIsAccepted();
            var afterClearing = await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments);
            var numberAfterClearing = await TheOldNumberTheConnectionStillCarriesForTeams(JiraDcPayments);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileTheDefaultIsUnset, Is.EqualTo(new[] { TheTrackersOwnParentSource }).AsCollection,
                    "A Team that follows a default nobody has set reads the tracker's own parent alone, as it always did.");
                Assert.That(afterSaving, Is.EqualTo(new[] { ParentLink, CausedBy }).AsCollection);
                Assert.That(oldNumber, Is.EqualTo(firstField), "An older page or script reads only this number, so it must be the first field of the list.");
                Assert.That(afterTheScript, Is.EqualTo(new[] { ParentLink, CausedBy }).AsCollection, "A save that never mentions the list must not clear it.");
                Assert.That(afterClearing, Is.Empty);
                Assert.That(numberAfterClearing, Is.Null);
            }
        }
    }
}
