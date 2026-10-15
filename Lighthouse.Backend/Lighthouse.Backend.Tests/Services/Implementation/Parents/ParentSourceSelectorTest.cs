using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;
using System.Collections;
using System.Reflection;

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

        private const int AnUnrelatedField = 23;

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

        [Test]
        public void HangUnder_TheOverrideNamesALinkType_ChangesTheParentAndTheFieldThatNamedItAndNothingElse()
        {
            var owner = ATeamWhoseOverridePointsAt(TheFieldTheOverridePointsAt);
            var record = ARecordWithEveryMemberFilledIn();
            var before = EveryMemberOf(record);

            ParentSourceSelector.HangUnder(owner, record, AnotherParent);

            var expected = new Dictionary<string, object?>(before)
            {
                [nameof(WorkItemBase.ParentReferenceId)] = AnotherParent,
                [AdditionalFieldMember(TheFieldTheOverridePointsAt)] = AnotherParent,
            };
            Assert.That(EveryMemberOf(record), Is.EquivalentTo(expected),
                "The stored parent and the field that named the link type move together, so the two cannot disagree; anything else changing here would rewrite part of the record nobody asked to touch.");
        }

        [Test]
        public void HangUnder_NothingIsNamedInTheOverride_ChangesTheParentAndNothingElse()
        {
            var owner = ATeamWhoseOverridePointsAt(null);
            var record = ARecordWithEveryMemberFilledIn();
            var before = EveryMemberOf(record);

            ParentSourceSelector.HangUnder(owner, record, AnotherParent);

            var expected = new Dictionary<string, object?>(before)
            {
                [nameof(WorkItemBase.ParentReferenceId)] = AnotherParent,
            };
            Assert.That(EveryMemberOf(record), Is.EquivalentTo(expected),
                "With no field named there is no box to show the key in, so writing one would invent a value for a field the Team never pointed at.");
        }

        private static WorkItemBase ARecordWithEveryMemberFilledIn()
        {
            var record = new WorkItemBase
            {
                Id = 42,
                ReferenceId = "PAY-420",
                ParentReferenceId = AParent,
                Name = "Settle the invoice run",
                Type = "Story",
                State = "In Progress",
                Tags = ["billing"],
                StateCategory = StateCategories.Doing,
                Url = "https://tracker.example/PAY-420",
                Order = "0|i0001",
                CreatedDate = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                StartedDate = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
                ClosedDate = new DateTime(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc),
                CurrentStateEnteredAt = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
                LastChangedRemote = new DateTime(2026, 3, 9, 9, 0, 0, DateTimeKind.Utc),
                ParentCandidatesFromLinks = [new ParentCandidate(AParent, null), new ParentCandidate(AnotherParent, null)],
            };
            record.AdditionalFieldValues[TheFieldTheOverridePointsAt] = AParent;
            record.AdditionalFieldValues[AnUnrelatedField] = "untouched";

            return record;
        }

        /// <summary>
        /// Every public member of the record, read by reflection so that a member added later is covered
        /// without anyone remembering to list it here. Each Additional Field value is its own entry, so a
        /// write to one field cannot hide behind the others.
        /// </summary>
        private static Dictionary<string, object?> EveryMemberOf(WorkItemBase record)
            => typeof(WorkItemBase)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != nameof(WorkItemBase.AdditionalFieldValues))
                .Select(property => (property.Name, Value: Describe(property.GetValue(record))))
                .Concat(record.AdditionalFieldValues.Select(field => (Name: AdditionalFieldMember(field.Key), Value: (object?)field.Value)))
                .ToDictionary(member => member.Name, member => member.Value);

        private static string AdditionalFieldMember(int fieldId) => $"{nameof(WorkItemBase.AdditionalFieldValues)}[{fieldId}]";

        private static object? Describe(object? value)
            => value is IEnumerable items and not string ? string.Join(", ", items.Cast<object>()) : value;

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
