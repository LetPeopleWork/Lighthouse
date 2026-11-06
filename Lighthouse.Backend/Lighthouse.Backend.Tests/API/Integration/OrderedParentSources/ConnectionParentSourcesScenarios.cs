using System.Net;
using Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OrderedParentSources
{
    /// <summary>
    /// The parent sources a connection holds as the default for its Teams and for its Portfolios. They run
    /// through the running application on the harness of the neighbouring connection-defaults scenarios, where a
    /// System Admin saves a connection and the Teams that follow it update. The harness saves a default as a list
    /// and reads one back by name; the neighbours that arrange the old single-field default move to the list in
    /// the same step as the code they exercise.
    ///
    /// The scenarios speak the words the browser sends: <c>parentSourcesForTeams</c>, <c>parentSourcesForPortfolios</c>
    /// and, on an owner, <c>parentSources</c>. The harness's stand-in tracker hangs an item under the first field it
    /// is handed, so "reads the new order" is shown by which field the tracker was asked to read first. An old
    /// single default shown as the field then the tracker's own parent is pinned by the migration class. The rule
    /// that a field in use cannot be removed from its connection is the field-in-use scenario in this class.
    /// </summary>
    [TestFixture]
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
                Assert.That(await TheSourcesTheSettingsOfShow(CardIssuing), Is.EqualTo(new[] { ParentLink }).AsCollection, "Card Issuing follows the Portfolios' default.");
                Assert.That(listed, Is.EquivalentTo(TheThreeFollowers), "Ledger owns its list, so nothing about the default reaches it.");
                Assert.That(listed, Does.Not.Contain(CardIssuing), "A Portfolio follows the Portfolios' default, which this save did not change.");
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
        /// Given Ledger owns a list holding "Caused by" first, or second, or the Teams default does, when a System
        /// Admin removes "Caused by" from the connection, then each save is refused with the field's name and who
        /// still uses it, and nothing is removed.
        /// </summary>
        [Test]
        public async Task A_field_cannot_be_removed_from_its_connection_while_any_list_holds_it_in_any_place()
        {
            await GivenTheTeamOwnsTheList(Ledger, CausedBy, ParentLink);
            var ownerFirst = await TheRefusalOfRemovingCausedBy();

            await GivenTheTeamOwnsTheList(Ledger, ParentLink, CausedBy);
            var ownerSecond = await TheRefusalOfRemovingCausedBy();

            await GivenTheTeamOwnsTheList(Ledger, ParentLink);
            await GivenTheTeamsListIs(CausedBy, ParentLink);
            var defaultFirst = await TheRefusalOfRemovingCausedBy();

            await GivenTheTeamsListIs(ParentLink, CausedBy);
            var defaultSecond = await TheRefusalOfRemovingCausedBy();

            using (Assert.EnterMultipleScope())
            {
                foreach (var (place, refusal) in new[] { ("owner, first", ownerFirst), ("owner, second", ownerSecond), ("default, first", defaultFirst), ("default, second", defaultSecond) })
                {
                    Assert.That(refusal.Status, Is.EqualTo(HttpStatusCode.Conflict), place);
                    Assert.That(refusal.Code, Is.EqualTo("additional-field-in-use"), place);
                    Assert.That(refusal.Message, Does.StartWith("\"Caused by\" cannot be removed."), place);
                }

                Assert.That(ownerFirst.Message, Does.Contain("Ledger uses it as its own"));
                Assert.That(ownerSecond.Message, Does.Contain("Ledger uses it as its own"));
                Assert.That(defaultFirst.Message, Does.Contain("It is the Teams' Parent sources default"));
                Assert.That(defaultSecond.Message, Does.Contain("It is the Teams' Parent sources default"));
                Assert.That(await TheFieldsOfTheConnection(JiraDcPayments), Does.Contain(CausedBy), "Nothing was saved.");
            }
        }

        /// <summary>
        /// Given the Teams default holds "Caused by", "Parent Link" and the tracker's own parent and Ledger owns a list,
        /// when an old page or script that knows only the single field saves "Release Train" as it, with no list, then
        /// the default becomes "Release Train" then the tracker's own parent, the three followers update, and Ledger's
        /// list is untouched and Ledger is not updated.
        /// </summary>
        [Test]
        public async Task An_old_client_that_sets_only_the_single_field_replaces_the_list_with_that_field_and_the_tracker_s_own_parent()
        {
            await GivenTheTeamsListIs(CausedBy, ParentLink, TheTrackersOwnParentSource);
            await GivenTheTeamOwnsTheList(Ledger, ParentLink, CausedBy);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenAnOldClientSetsTheTeamsDefaultTo(ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            var listed = await WhatTheTaskManagerListsSoonAfterTheSave(expected: 3);
            await WhatRanSinceTheSave(TheThreeFollowers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments), Is.EqualTo(new[] { ReleaseTrain, TheTrackersOwnParentSource }).AsCollection);
                Assert.That(listed, Is.EquivalentTo(TheThreeFollowers), "Ledger owns its list, so nothing about the default reaches it.");
                Assert.That(await TheSourcesTheSettingsOfShow(FraudSignals), Is.EqualTo(new[] { ReleaseTrain, TheTrackersOwnParentSource }).AsCollection);
                Assert.That(await TheSourcesTheSettingsOfShow(Ledger), Is.EqualTo(new[] { ParentLink, CausedBy }).AsCollection);
            }
        }

        /// <summary>
        /// Given the Teams default holds "Caused by", "Parent Link" and the tracker's own parent, when an old page or
        /// script that has never heard of the default saves the connection with neither the list nor the old number,
        /// then the default is as it was and no Team or Portfolio is updated.
        /// </summary>
        [Test]
        public async Task An_old_client_that_sends_neither_the_list_nor_the_single_field_changes_nothing()
        {
            await GivenTheTeamsListIs(CausedBy, ParentLink, TheTrackersOwnParentSource);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenAnOldClientSendsNothingOfTheTeamsDefault(startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments), Is.EqualTo(new[] { CausedBy, ParentLink, TheTrackersOwnParentSource }).AsCollection);
                Assert.That(ran, Is.Empty);
            }
        }

        private async Task<(HttpStatusCode Status, string? Code, string? Message)> TheRefusalOfRemovingCausedBy()
        {
            await WhenTheAdminRemovesTheField(CausedBy);
            var (code, message) = TheRefusal();

            return (SaveAnswer, code, message);
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

        /// <summary>
        /// Given a connection, when a System Admin saves the Portfolios default as a list, then it reads back in
        /// the same order with the old number as its first field, and the Teams default stays as it was.
        /// </summary>
        [Test]
        public async Task The_Portfolios_default_saves_and_reads_back_as_a_list_and_leaves_the_Teams_default_alone()
        {
            await GivenTheTeamsListIs(CausedBy);

            await GivenThePortfoliosListIs(ParentLink, CausedBy, TheTrackersOwnParentSource);
            var portfolios = await TheSourcesOfThePortfoliosDefaultOn(JiraDcPayments);
            var teams = await TheSourcesOfTheTeamsDefaultOn(JiraDcPayments);
            var oldNumber = await TheOldNumberTheConnectionStillCarriesForPortfolios(JiraDcPayments);
            var firstField = await TheNumberOfTheField(JiraDcPayments, ParentLink);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolios, Is.EqualTo(new[] { ParentLink, CausedBy, TheTrackersOwnParentSource }).AsCollection);
                Assert.That(oldNumber, Is.EqualTo(firstField));
                Assert.That(teams, Is.EqualTo(new[] { CausedBy }).AsCollection);
            }
        }
    }
}
