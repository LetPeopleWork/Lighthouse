using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Parents
{
    /// <summary>
    /// The rule a record's parent obeys, with no tracker in sight: a record hangs under at most one thing,
    /// so several candidates are an answer of their own rather than a shortlist to pick from. The
    /// scenarios beside this drive the same rule through a real Jira refresh; these say what it is.
    /// </summary>
    [TestFixture]
    public class ParentSourceSelectorTest
    {
        private const int TheFieldTheOverridePointsAt = 17;

        private const string AParent = "EPIC-1";

        private const string AnotherParent = "EPIC-2";

        [Test]
        public void ReadsTheTrackersOwnParent_NothingIsNamedInTheOverride_SaysSo()
        {
            var owner = ATeamWhoseOverridePointsAt(null);

            Assert.That(ParentSourceSelector.ReadsTheTrackersOwnParent(owner), Is.True,
                "A Team that never touched the setting is every Team using Lighthouse today, and its parents still come from the tracker.");
        }

        [Test]
        public void ReadsTheTrackersOwnParent_SomethingIsNamedInTheOverride_SaysSo()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);

            Assert.That(ParentSourceSelector.ReadsTheTrackersOwnParent(owner), Is.False,
                "Naming something in the override declares it authoritative, so the tracker's own answer stops being read.");
        }

        [Test]
        public void TheParentOf_NothingIsNamedInTheOverride_YieldsNothingWhateverTheLinksSay()
        {
            var owner = ATeamWhoseOverridePointsAt(null);
            var workItem = ARecordCarrying(AParent);

            var parent = ParentSourceSelector.TheParentOf(
                owner, workItem, ParentSource.ALinkTypeTheOverrideNames, ParentResolution.From([AnotherParent]));

            Assert.That(parent, Is.Empty,
                "Nothing was named, so nothing overrides what the tracker reported - not a field value left over from a setting somebody cleared, and not a link nobody asked to be read.");
        }

        [Test]
        public void TheParentOf_TheOverrideNamesAField_ReadsThatFieldsValue()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);
            var workItem = ARecordCarrying(AParent);

            var parent = ParentSourceSelector.TheParentOf(
                owner, workItem, ParentSource.AFieldTheOverrideNames, ParentResolution.From([AnotherParent]));

            Assert.That(parent, Is.EqualTo(AParent),
                "A named field is authoritative, and links are not consulted while one is named - reading them here would let a link quietly outrank what somebody typed.");
        }

        [Test]
        public void TheParentOf_TheOverrideNamesAFieldTheRecordDoesNotCarry_YieldsNothing()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);

            var parent = ParentSourceSelector.TheParentOf(
                owner, new WorkItemBase(), ParentSource.AFieldTheOverrideNames, default);

            Assert.That(parent, Is.Empty,
                "A record stored before the field was configured carries no entry for it, and that is an empty answer rather than a failure.");
        }

        [Test]
        public void TheParentOf_TheOverrideNamesALinkTypeAndOneIssueAnswers_ReadsThatIssue()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);

            var parent = ParentSourceSelector.TheParentOf(
                owner, ARecordCarrying(AnotherParent), ParentSource.ALinkTypeTheOverrideNames, ParentResolution.From([AParent]));

            Assert.That(parent, Is.EqualTo(AParent),
                "The issue on the other end of the matching link is the parent, and it is read instead of the field value the record happens to carry, not alongside it.");
        }

        [Test]
        public void TheParentOf_TheOverrideNamesALinkTypeAndTwoIssuesAnswer_YieldsNothing()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);

            var parent = ParentSourceSelector.TheParentOf(
                owner, new WorkItemBase(), ParentSource.ALinkTypeTheOverrideNames, ParentResolution.From([AParent, AnotherParent]));

            Assert.That(parent, Is.Empty,
                "A record hangs under one thing, so two candidates cannot be narrowed by keeping one. A wrongly chosen parent moves work under something it does not belong to and looks exactly like correct data.");
        }

        [Test]
        public void TheParentOf_TheOverrideNamesALinkTypeAndNothingAnswers_YieldsNothing()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);

            var parent = ParentSourceSelector.TheParentOf(
                owner, new WorkItemBase(), ParentSource.ALinkTypeTheOverrideNames, default);

            Assert.That(parent, Is.Empty,
                "A record with no link of that type simply has no parent to report, which is the ordinary case rather than a problem.");
        }

        private static Team ATeamWhoseOverridePointsAt(int? additionalFieldDefinitionId)
            => new() { ParentOverrideAdditionalFieldDefinitionId = additionalFieldDefinitionId };

        private static WorkItemBase ARecordCarrying(string parentTypedIntoTheNamedField)
        {
            var workItem = new WorkItemBase();
            workItem.AdditionalFieldValues[TheFieldTheOverridePointsAt] = parentTypedIntoTheNamedField;

            return workItem;
        }
    }
}
