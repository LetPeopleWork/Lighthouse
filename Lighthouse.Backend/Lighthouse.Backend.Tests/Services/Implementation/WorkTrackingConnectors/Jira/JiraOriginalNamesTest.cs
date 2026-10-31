using System.Text.Json;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The one rule that turns what a German account reads into what an English account reads: look the id
    /// up in the instance's lists. An id a list that was read fine does not hold is reported as unknown and
    /// never given a name; the payload's own name is used only where there is nothing to look up (no id) or
    /// where the list entry has no original name of its own.
    /// </summary>
    [TestFixture]
    public class JiraOriginalNamesTest
    {
        [Test]
        public void A_status_the_list_knows_is_reported_by_its_original_name()
        {
            var names = AGermanReadersNames();

            Assert.That(names.StatusNameFor("3", "In Arbeit"), Is.EqualTo("In Progress"));
        }

        [Test]
        public void A_work_item_type_the_list_knows_is_reported_by_its_original_name()
        {
            var names = AGermanReadersNames();

            Assert.That(names.IssueTypeNameFor("10001", "Geschichte"), Is.EqualTo("Story"));
        }

        [Test]
        public void An_entry_without_an_original_name_is_reported_by_the_name_of_the_entry()
        {
            var names = NamesFrom(
                JiraWireFormat.TheStatusListOf([JiraWireFormat.AStatus("3", "In Progress", null, "indeterminate", "In Progress")]),
                JiraWireFormat.TheIssueTypeListOf([JiraWireFormat.AnIssueType("10001", "Story", null)]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusNameFor("3", "In Arbeit"), Is.EqualTo("In Progress"));
                Assert.That(names.IssueTypeNameFor("10001", "Geschichte"), Is.EqualTo("Story"));
            }
        }

        [Test]
        public void Status_ids_the_list_does_not_hold_are_reported_as_unknown_once_each()
        {
            var names = AGermanReadersNames();

            Assert.That(names.StatusIdsItDoesNotKnow(["3", "777", "5", "777", null]), Is.EqualTo(new List<string> { "777" }));
        }

        [Test]
        public void Work_item_type_ids_the_list_does_not_hold_are_reported_as_unknown_once_each()
        {
            var names = AGermanReadersNames();

            Assert.That(names.IssueTypeIdsItDoesNotKnow(["10001", "99999", "99999"]), Is.EqualTo(new List<string> { "99999" }));
        }

        [Test]
        public void Only_the_ids_the_lists_lack_are_reported_as_unknown()
        {
            var names = AGermanReadersNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusIdsItDoesNotKnow(["1", "3", "5"]), Is.Empty);
                Assert.That(names.StatusIdsItDoesNotKnow(["1", "777"]), Is.EqualTo(new List<string> { "777" }));
                Assert.That(names.IssueTypeIdsItDoesNotKnow(["10001", "10002"]), Is.Empty);
                Assert.That(names.IssueTypeIdsItDoesNotKnow(["10001", "99999"]), Is.EqualTo(new List<string> { "99999" }));
            }
        }

        [Test]
        public void A_non_null_id_the_lists_lack_is_never_answered_with_the_name_the_payload_carried()
        {
            var names = AGermanReadersNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusNameFor("777", "Wird überprüft"), Is.Null);
                Assert.That(names.IssueTypeNameFor("99999", "Aufgabe"), Is.Null);
            }
        }

        [Test]
        public void A_payload_without_an_id_keeps_its_name()
        {
            var names = AGermanReadersNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusNameFor(null, "In Arbeit"), Is.EqualTo("In Arbeit"));
                Assert.That(names.IssueTypeNameFor(null, "Geschichte"), Is.EqualTo("Geschichte"));
            }
        }

        [Test]
        public void A_status_id_is_never_looked_up_among_the_work_item_types()
        {
            var names = NamesFrom(
                JiraWireFormat.TheStatusListOf([JiraWireFormat.AStatus("10001", "Fertig", "Done", "done", "Fertig")]),
                JiraWireFormat.TheIssueTypeListOf([JiraWireFormat.AnIssueType("10001", "Geschichte", "Story")]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusNameFor("10001", "Fertig"), Is.EqualTo("Done"));
                Assert.That(names.IssueTypeNameFor("10001", "Geschichte"), Is.EqualTo("Story"));
            }
        }

        [Test]
        public void Names_read_as_reported_leave_every_name_as_the_payload_carried_it_and_know_every_id()
        {
            var names = JiraOriginalNames.AsReported;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names.StatusNameFor("3", "In Arbeit"), Is.EqualTo("In Arbeit"));
                Assert.That(names.IssueTypeNameFor("10001", "Geschichte"), Is.EqualTo("Geschichte"));
                Assert.That(names.StatusIdsItDoesNotKnow(["3"]), Is.Empty);
                Assert.That(names.IssueTypeIdsItDoesNotKnow(["10001"]), Is.Empty);
            }
        }

        private static JiraOriginalNames AGermanReadersNames()
            => NamesFrom(
                JiraWireFormat.TheStatusListOf(JiraWireFormat.AGermanReadersStatuses),
                JiraWireFormat.TheIssueTypeListOf(JiraWireFormat.AGermanReadersIssueTypes));

        private static JiraOriginalNames NamesFrom(string statusList, string issueTypeList)
        {
            using var statuses = JsonDocument.Parse(statusList);
            using var issueTypes = JsonDocument.Parse(issueTypeList);

            return JiraOriginalNames.FromLists(statuses.RootElement, issueTypes.RootElement);
        }
    }
}
