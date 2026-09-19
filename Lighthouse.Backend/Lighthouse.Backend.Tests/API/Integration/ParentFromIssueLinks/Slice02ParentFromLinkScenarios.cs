using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Acceptance scenarios - Slice 02: taking an item's parent from the link type the override names.
    /// Driving port: a Team refresh, which is what actually writes the parent every forecast is drawn
    /// from. Nothing here is guessed - an administrator names a link type in Parent Override Field exactly
    /// as they would name a field, and only a reference that resolved as a link type reads links at all.
    ///
    /// The one thing inferred is which end of the link the parent sits on. Jira writes a link once and
    /// serves it from both ends, so the counterpart is the parent whichever end an item holds, and an
    /// instance where a Work Item names its Feature reads the same as one where a Feature names its Work
    /// Items.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6028-parent-from-issue-links")]
    [Category("slice-02")]
    public partial class Slice02ParentFromLinkTest
    {
        [Test]
        public async Task The_child_holding_the_outward_end_takes_its_parent_from_that_link()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheChild), Is.EqualTo(TheParent),
                "An administrator who named a link type in Parent Override Field expects the issue on the other end of that link to become the parent. Until this reads, the feature does nothing at all.");
        }

        /// <summary>
        /// The same link, seen from the other issue. Jira serves one link from both ends, so an instance
        /// that hangs children off parents and one that hangs parents off children put the two shapes on
        /// the wire - and nobody is asked which of the two their instance uses.
        /// </summary>
        [Test]
        public async Task The_child_holding_the_inward_end_takes_its_parent_from_that_link_too()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseInwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheChild), Is.EqualTo(TheParent),
                "Reading only one end would leave half of every instance with no parents and no explanation, because which end an item holds is not something an administrator chose.");
        }

        [Test]
        public async Task No_matching_link_means_no_parent_and_no_warning()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChildWithNothingMatching, AnotherLinkTypeTheInstanceDefines, TheParent);

            var refreshed = await TheTeamIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(refreshed, TheChildWithNothingMatching), Is.Empty,
                    "A link of some other type says nothing about a parent, and taking one anyway would file work under something it does not belong to.");
                NothingWasWrittenToTheLogAsAWarning();
            }
        }

        /// <summary>
        /// A Portfolio names the link carrying a Feature to the level above it, which is a different link
        /// from the one a Team names. Both grains are served from the same place in the connector, and this
        /// is what keeps them there: separate them again and one of the two stops reading links.
        /// </summary>
        [Test]
        public async Task The_same_mechanism_at_Portfolio_grain()
        {
            TheParentOverrideNames(ALinkTypeAPortfolioNames.Name);
            TheIssueHasOneLinkWhoseInwardIssueIs(TheFeature, ALinkTypeAPortfolioNames, TheLevelAboveTheFeature);

            var refreshed = await ThePortfolioIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheFeature), Is.EqualTo(TheLevelAboveTheFeature),
                "A Portfolio whose Features never acquire a parent draws every forecast above them from nothing, and an administrator who set the same box on both grains has no way to tell which of the two ignored it.");
        }

        /// <summary>
        /// What the override resolved to is written into the Additional Field that named it, so the answer to
        /// "did it point at the right thing?" is on the screen rather than in the tracker.
        /// </summary>
        [Test]
        public async Task The_resolved_key_is_visible_without_opening_the_tracker()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseInwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheAdditionalFieldValueForTheOverrideOf(refreshed, TheChild), Is.EqualTo(TheParent),
                "An administrator who typed a link type into a box that has always taken a field name has nothing to check their guess against until the key it found is shown back to them.");
        }

        /// <summary>
        /// An item the links said nothing about must stay unparented rather than pick up the parent the
        /// tracker hangs on its own field. That field is the very thing the override was set to stop reading,
        /// and a parent taken from it would look exactly like one the links produced.
        /// </summary>
        [Test]
        public async Task An_item_with_no_matching_link_does_not_fall_back_to_the_field_the_override_replaced()
        {
            TheInstanceDefinesTheCustomField(TheFieldDataCenterHangsParentsOn);
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueNamesAParentInTheDataCenterFieldAndHasNoMatchingLink(TheChildWithNothingMatching, TheParentDataCenterWouldHaveNamed);

            var refreshed = await TheTeamIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(refreshed, TheChildWithNothingMatching), Is.Empty,
                    "Filing this item under the parent the tracker's own field names would be a wrong parent that looks right, which is the one failure this setting exists to prevent.");
                Assert.That(TheAdditionalFieldValueForTheOverrideOf(refreshed, TheChildWithNothingMatching), Is.Empty,
                    "Showing a key the refresh did not act on would send an administrator looking for a parent that was never stored.");
            }
        }

        [Test]
        public async Task A_reference_naming_a_real_field_still_reads_that_field()
        {
            TheInstanceDefinesTheCustomField(ACustomField);
            TheParentOverrideNames(ACustomField);
            TheIssueCarriesTheFieldValue(TheChild, TheIdThatFieldCarries, AParentTypedIntoThatField);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheChild), Is.EqualTo(AParentTypedIntoThatField),
                "Giving the setting a second thing it can name must not change what it does for everyone who named a field, which is everyone using it today.");
        }

        [Test]
        public async Task A_Team_with_no_parent_override_keeps_the_parent_Jira_itself_names()
        {
            TheIssueHasJirasOwnParent(TheChild, TheParentJiraItselfNames);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheChild), Is.EqualTo(TheParentJiraItselfNames),
                "Most Teams have never touched this setting, and a refresh must go on reading the parent Jira reports for them.");
        }

        /// <summary>
        /// A token stops being accepted at some point in its life, and Jira does not refuse the caller when
        /// it does - it answers as it would answer a stranger, and a stranger is shown no link types. So an
        /// empty list means "this instance defines none" and "whoever asked is not signed in" equally well,
        /// and only asking who is signed in tells the two apart.
        /// </summary>
        [Test]
        public void A_credential_Jira_no_longer_accepts_stops_the_refresh_rather_than_emptying_it()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);
            TheCredentialIsNoLongerAccepted();

            Assert.That(async () => await TheTeamIsRefreshed(), Throws.InstanceOf<JiraReadException>(),
                "A refresh that carries on hands every item back with no parent, and an item handed back overwrites the parent already stored for it - so an expired token empties the hierarchy it could not read, and what is left looks exactly like correct data.");
        }

        [Test]
        public async Task The_refresh_that_stopped_says_the_credential_is_why()
        {
            TheParentOverrideNames(ALinkTypeTheInstanceDefines.Name);
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);
            TheCredentialIsNoLongerAccepted();

            var refusal = await WhatTheRefreshRefusedWith();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.Message, Does.Contain("credential").IgnoreCase,
                    $"An administrator sent to look at the link type configuration spends the afternoon correcting something that was never wrong. Lighthouse said: {refusal?.Message}");
                TheRefreshWarnedTheOperatorAboutIt();
            }
        }

        /// <summary>
        /// The list changes about once a year and a refresh reads it on every fetch it makes. A correctly
        /// configured connection using this feature has a reference the field list cannot resolve by
        /// definition, so without this every Team and every Portfolio pays that round trip on every cycle,
        /// forever.
        /// </summary>
        [Test]
        public async Task One_refresh_asks_what_link_types_exist_once_however_many_fetches_it_makes()
        {
            TheParentOverrideNames(ALinkTypeAPortfolioNames.Name);
            TheIssueHasOneLinkWhoseInwardIssueIs(TheFeature, ALinkTypeAPortfolioNames, TheLevelAboveTheFeature);

            await ThePortfolioAndTheFeaturesAboveItAreRefreshed();

            Assert.That(HowOftenTheInstanceWasAskedForItsLinkTypes(), Is.EqualTo(1),
                "Two fetches of one refresh read the same list, and asking twice for an answer that changes about once a year is a round trip spent on every cycle of every connection using this feature.");
        }

        /// <summary>
        /// The other half of that saving: nothing remembered may outlive the refresh that remembered it.
        /// An administrator who renames a link type to make the override match it has no way to know a
        /// cache is what stopped it taking effect, and no way to clear one.
        /// </summary>
        [Test]
        public async Task A_link_type_renamed_between_refreshes_is_read_under_its_new_name_on_the_next_one()
        {
            TheParentOverrideNames(TheNameALinkTypeIsRenamedTo);
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);

            var beforeTheRename = await TheTeamIsRefreshed();
            TheInstanceRenamesTheLinkType(ALinkTypeTheInstanceDefines, TheNameALinkTypeIsRenamedTo);
            var afterTheRename = await TheTeamIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(beforeTheRename, TheChild), Is.Empty,
                    "No link type carried that name yet, so a parent appearing here would mean the override matched something nobody named.");
                Assert.That(TheParentOf(afterTheRename, TheChild), Is.EqualTo(TheParent),
                    "The instance now defines exactly what the override names, and a refresh still reading the previous cycle's list leaves the administrator staring at a setting that is right and does nothing.");
            }
        }

        /// <summary>
        /// Both numbers are measured here rather than one of them written down. A figure typed into a test
        /// is right on the day it is typed and silently wrong the first time paging or the query changes,
        /// and nothing fails when it goes stale - so the refresh nobody configured is measured too, and the
        /// two are compared.
        /// </summary>
        [Test]
        public async Task Reading_links_costs_the_search_endpoint_nothing()
        {
            TheInstanceDefinesTheFieldEveryRefreshAlreadyReads();
            TheIssueHasOneLinkWhoseOutwardIssueIs(TheChild, ALinkTypeTheInstanceDefines, TheParent);

            var asEveryTeamRefreshedBeforeThisFeature = await WhatOneTeamRefreshAsksForWithTheBoxLeftEmpty();
            var withALinkTypeNamed = await WhatOneTeamRefreshAsksForWith(ALinkTypeTheInstanceDefines.Name);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withALinkTypeNamed.Searches, Is.EqualTo(asEveryTeamRefreshedBeforeThisFeature.Searches),
                    "The link entries ride along on issues the search already downloads in full, so reading them is free. One more search per refresh is a cost every Team on every cycle pays for a feature most of them never turned on.");
                Assert.That(asEveryTeamRefreshedBeforeThisFeature.LinkTypeReads, Is.Zero,
                    "A Team that named no link type has nothing to resolve, and charging it a round trip anyway would slow down every instance for a setting it never touched.");
                Assert.That(withALinkTypeNamed.LinkTypeReads, Is.EqualTo(1),
                    "The list changes about once a year. Reading it more than once in a refresh is a round trip spent on every cycle of every connection using this feature.");
                Assert.That(withALinkTypeNamed.CredentialChecks, Is.EqualTo(1),
                    "Asking who is signed in is what tells an instance defining no link types apart from a token Jira stopped accepting, and one such question per refresh is the whole price of that distinction.");
            }
        }

        /// <summary>
        /// The Portfolio's copy of the guard that stops a refresh falling back to the field the tracker
        /// hangs parents on. The guard is written out once per grain, so the Team's copy being pinned says
        /// nothing about this one - and a Feature filed under the wrong level above it reads exactly like a
        /// Feature filed correctly.
        /// </summary>
        [Test]
        public async Task A_Feature_with_no_matching_link_does_not_fall_back_to_the_field_the_override_replaced()
        {
            TheInstanceDefinesTheCustomField(TheFieldDataCenterHangsFeatureParentsOn);
            TheParentOverrideNames(ALinkTypeAPortfolioNames.Name);
            TheIssueNamesAParentInTheDataCenterFieldAndHasNoMatchingLink(TheFeatureWithNothingMatching, TheLevelAboveDataCenterWouldHaveNamed);

            var refreshed = await ThePortfolioIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheFeatureWithNothingMatching), Is.Empty,
                "An administrator who named a link type said the tracker's own parent field is not where this Portfolio's hierarchy lives. Reading it anyway files the Feature under something it does not belong to, and every forecast above it is then drawn from work nobody put there.");
        }

        /// <summary>
        /// What the whole feature is for, at the grain a customer sees it. A Portfolio whose Features each
        /// show the same made-up size, because nothing tells Lighthouse which Work Items belong to which
        /// Feature - and then the same Portfolio after an administrator names the link type those items
        /// were drawn with all along.
        /// </summary>
        [Test]
        public async Task Features_stop_being_sized_by_the_Portfolio_default()
        {
            APortfolioOfFeaturesWhoseChildrenLinkToThemBy(ALinkTypeTheInstanceDefines);

            await TheTeamAndThePortfolioAreRefreshed();

            Assert.That(WhichFeaturesAreSizedByThePortfolioDefault(), Is.EquivalentTo(HowManyItemsLinkToEachFeature().Keys),
                "This is the state the customer reported: the items are linked to their Features on the tracker, and Lighthouse sizes every Feature by one number typed into Portfolio settings because it cannot see the link.");

            TheAdministratorNamesTheLinkTypeOnBothGrains(ALinkTypeTheInstanceDefines);

            await TheTeamAndThePortfolioAreRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhichFeaturesAreSizedByThePortfolioDefault(), Is.Empty,
                    "A Feature still on the default size is one whose children were not found, and it carries the no-children warning while every forecast drawn from it is a guess at a number nobody measured.");
                Assert.That(HowManyChildrenEachFeatureEndedUpWith(), Is.EquivalentTo(HowManyItemsLinkToEachFeature()),
                    "Counting the right number of children for the wrong Feature forecasts both of them wrong while looking entirely plausible on the screen.");
            }
        }
    }
}
