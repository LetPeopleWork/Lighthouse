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
