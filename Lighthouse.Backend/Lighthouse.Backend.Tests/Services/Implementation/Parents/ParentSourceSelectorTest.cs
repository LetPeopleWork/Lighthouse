using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;
using System.Collections;
using System.Reflection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Parents
{
    /// <summary>
    /// What applying a choice leaves on a record, with no tracker in sight: the parent and the shown value of
    /// a link entry, and nothing else. The scenarios beside this drive the same rule through a real Jira
    /// refresh; these say what it touches.
    /// </summary>
    [TestFixture]
    public class ParentSourceSelectorTest
    {
        private const int TheFieldTheLinkEntryNames = 17;

        private const string AParent = "EPIC-1";

        private const string AnotherParent = "EPIC-2";

        private const int AnUnrelatedField = 23;

        [Test]
        public void HangUnder_a_link_entry_won_changes_the_parent_and_the_field_of_that_entry_and_nothing_else()
        {
            var record = ARecordWithEveryMemberFilledIn();
            var before = EveryMemberOf(record);

            ParentSourceSelector.HangUnder(record, record.ParentReadings, new ParentChoice(AnotherParent, 0, false));

            var expected = new Dictionary<string, object?>(before)
            {
                [nameof(WorkItemBase.ParentReferenceId)] = AnotherParent,
                [AdditionalFieldMember(TheFieldTheLinkEntryNames)] = AnotherParent,
            };
            Assert.That(EveryMemberOf(record), Is.EquivalentTo(expected),
                "The stored parent and the field that named the link type move together, so the two cannot disagree; anything else changing here would rewrite part of the record nobody asked to touch.");
        }

        [Test]
        public void HangUnder_a_list_with_no_link_entry_changes_the_parent_and_nothing_else()
        {
            var record = ARecordWithEveryMemberFilledIn();
            var readings = ParentReadings.Of(new ParentReadingAt(ParentSourceEntry.TrackerParent, ParentReading.TrackersOwn(AnotherParent)));
            var before = EveryMemberOf(record);

            ParentSourceSelector.HangUnder(record, readings, new ParentChoice(AnotherParent, 0, false));

            var expected = new Dictionary<string, object?>(before)
            {
                [nameof(WorkItemBase.ParentReferenceId)] = AnotherParent,
            };
            Assert.That(EveryMemberOf(record), Is.EquivalentTo(expected),
                "With no link entry there is no box to show the key in, so writing one would invent a value for a field nobody pointed at.");
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
                ParentReadings = ParentReadings.Of(new ParentReadingAt(
                    ParentSourceEntry.Field(TheFieldTheLinkEntryNames),
                    ParentReading.LinkCandidates(ParentResolution.From([AParent, AnotherParent])))),
            };
            record.AdditionalFieldValues[TheFieldTheLinkEntryNames] = AParent;
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
    }
}
