using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    /// <summary>
    /// A search result carries the status and the work item type in the language of the account that asked,
    /// with an id and no original name. The history Jira sends with it is already in the original names.
    /// </summary>
    [TestFixture]
    public class IssueFactoryOriginalNamesTest
    {
        private static readonly DateTime StartedAt = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime ClosedAt = new(2026, 3, 9, 15, 30, 0, DateTimeKind.Utc);

        [Test]
        public void A_German_payload_gives_the_state_and_type_an_English_account_would_see()
        {
            var issue = TheIssueIn(AGermanIssue(), AGermanReadersNames());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.State, Is.EqualTo("Done"));
                Assert.That(issue.IssueType, Is.EqualTo("Story"));
            }
        }

        [Test]
        public void Started_and_closed_dates_are_the_same_whichever_language_the_account_reads()
        {
            var read = TheIssueIn(AGermanIssue(), AGermanReadersNames());
            var readInEnglish = TheIssueIn(AnEnglishIssue(), JiraOriginalNames.AsReported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readInEnglish.ClosedDate, Is.Not.Null, "The comparison means nothing if the English read finds no dates.");
                Assert.That(read.StartedDate, Is.EqualTo(readInEnglish.StartedDate));
                Assert.That(read.ClosedDate, Is.EqualTo(readInEnglish.ClosedDate));
            }
        }

        [Test]
        public void The_history_is_left_as_Jira_wrote_it()
        {
            var read = TheIssueIn(AGermanIssue(), AGermanReadersNames());

            Assert.That(
                read.StateTransitions.Select(transition => (transition.FromState, transition.ToState)),
                Is.EqualTo(new List<(string, string)> { ("To Do", "In Progress"), ("In Progress", "Done") }));
        }

        [Test]
        public void A_payload_that_carries_no_id_keeps_its_names_because_there_is_nothing_to_look_up()
        {
            var issue = TheIssueIn(JiraWireFormat.AnIssueCarryingNamesOnly("PAY-7", "Geschichte", "Fertig"), AGermanReadersNames());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.State, Is.EqualTo("Fertig"));
                Assert.That(issue.IssueType, Is.EqualTo("Geschichte"));
            }
        }

        [Test]
        public void A_non_null_id_the_lists_lack_is_never_given_the_name_the_payload_carried()
        {
            var issue = TheIssueIn(
                JiraWireFormat.AnIssueSeenIn("PAY-8", "99999", "Aufgabe", "777", "Wird überprüft"),
                AGermanReadersNames());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.State, Is.Null);
                Assert.That(issue.IssueType, Is.Null);
            }
        }

        [Test]
        public void Reading_without_original_names_keeps_what_the_payload_carried()
        {
            var issue = TheIssueIn(AGermanIssue(), originalNames: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.State, Is.EqualTo("Fertig"));
                Assert.That(issue.IssueType, Is.EqualTo("Geschichte"));
            }
        }

        private static string AGermanIssue()
            => JiraWireFormat.AnIssueSeenIn("PAY-7", "10001", "Geschichte", "5", "Fertig", TheHistory());

        private static string AnEnglishIssue()
            => JiraWireFormat.AnIssueSeenIn("PAY-7", "10001", "Story", "5", "Done", TheHistory());

        private static string[] TheHistory()
            =>
            [
                JiraWireFormat.AStatusChangeAt(StartedAt, "To Do", "In Progress"),
                JiraWireFormat.AStatusChangeAt(ClosedAt, "In Progress", "Done"),
            ];

        private static JiraOriginalNames AGermanReadersNames()
        {
            using var statuses = JsonDocument.Parse(JiraWireFormat.TheStatusListOf(JiraWireFormat.AGermanReadersStatuses));
            using var issueTypes = JsonDocument.Parse(JiraWireFormat.TheIssueTypeListOf(JiraWireFormat.AGermanReadersIssueTypes));

            return JiraOriginalNames.FromLists(statuses.RootElement, issueTypes.RootElement);
        }

        private static Issue TheIssueIn(string json, JiraOriginalNames? originalNames)
        {
            using var document = JsonDocument.Parse(json);

            return new IssueFactory(Mock.Of<ILogger<IssueFactory>>())
                .CreateIssueFromJson(document.RootElement, JiraConnectorTestSetup.ATeamOnJiraCloud(), originalNames: originalNames);
        }
    }
}
