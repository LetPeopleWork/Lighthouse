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
    }
}
