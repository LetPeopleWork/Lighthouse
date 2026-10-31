using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A status list or work item type list Lighthouse cannot trust stops the refresh. The administrator
    /// reads why in the Activity section, so the sentence, the code and the detail are pinned here.
    /// </summary>
    [TestFixture]
    public class JiraOriginalNamesRefusedListTest
    {
        private const string StatusSentence =
            "Lighthouse could not read the list of statuses on this Jira instance, so it cannot tell which state each work item is in.";

        private const string TypeSentence =
            "Lighthouse could not read the list of work item types on this Jira instance, so it cannot tell which type each work item is.";

        private const string ServiceUnavailableBody = "{\"message\":\"Service is down\"}";

        private const string ForbiddenBody = "{\"errorMessages\":[\"You do not have permission\"]}";

        private const string AGatewayPage = "<html><body>Sign in to continue</body></html>";

        private const string ListEndpointOfStatuses = "rest/api/latest/status";

        private const string ListEndpointOfTypes = "rest/api/latest/issuetype";

        [TestCase(true, HttpStatusCode.ServiceUnavailable, ServiceUnavailableBody, 503, "ServiceUnavailable")]
        [TestCase(false, HttpStatusCode.Forbidden, ForbiddenBody, 403, "Forbidden")]
        [TestCase(true, HttpStatusCode.OK, "[]", 200, "OK")]
        [TestCase(false, HttpStatusCode.OK, "[]", 200, "OK")]
        [TestCase(true, HttpStatusCode.OK, AGatewayPage, 200, "OK")]
        [TestCase(false, HttpStatusCode.OK, "{}", 200, "OK")]
        public async Task A_list_that_cannot_be_read_stops_a_Team_refresh_with_the_sentence_the_administrator_reads(
            bool statusList, HttpStatusCode answer, string body, int code, string status)
        {
            var instance = AnInstanceRefusing(statusList, answer, body);

            var stopped = await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler()).GetWorkItemsForTeam(ATeam(), CancellationToken.None));

            Assert.That(
                stopped!.Verdict.Message,
                Is.EqualTo($"{Sentence(statusList)} Jira answered {code} ({status}). Nothing was changed; the Team keeps the work items from its last refresh."));
        }

        [TestCase(true, HttpStatusCode.ServiceUnavailable, ServiceUnavailableBody, 503, "ServiceUnavailable")]
        [TestCase(false, HttpStatusCode.OK, "[]", 200, "OK")]
        public async Task A_list_that_cannot_be_read_stops_a_Portfolio_refresh_without_saying_what_was_kept(
            bool statusList, HttpStatusCode answer, string body, int code, string status)
        {
            var instance = AnInstanceRefusing(statusList, answer, body);

            var stopped = await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler()).GetFeaturesForProject(APortfolio(), CancellationToken.None));

            Assert.That(
                stopped!.Verdict.Message,
                Is.EqualTo($"{Sentence(statusList)} Jira answered {code} ({status})."));
        }

        [TestCase(true, "Team")]
        [TestCase(false, "Team")]
        [TestCase(true, "Portfolio")]
        [TestCase(false, "Portfolio")]
        [TestCase(true, "Validation")]
        [TestCase(false, "Validation")]
        public async Task An_id_the_list_still_lacks_after_one_more_read_stops_the_refresh_naming_the_id(bool statusList, string owner)
        {
            var instance = AnInstanceWhoseSearchReportsAnIdTheListsLack(statusList);

            var stopped = await Assert.ThrowsAsync<JiraReadException>(() => RunningAgainst(instance, owner));

            var keptStateSentence = owner == "Team" ? " Nothing was changed; the Team keeps the work items from its last refresh." : string.Empty;
            var sentence = statusList
                ? "Jira reported a status (id 777) that is not in its list of statuses on this Jira instance, so Lighthouse cannot tell which state each work item is in."
                : "Jira reported a work item type (id 777) that is not in its list of work item types on this Jira instance, so Lighthouse cannot tell which type each work item is.";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopped!.Verdict.Message, Is.EqualTo(sentence + keptStateSentence));
                Assert.That(stopped.Verdict.Code, Is.EqualTo(statusList ? "status_id_not_in_list" : "issue_type_id_not_in_list"));
                Assert.That(stopped.Verdict.TechnicalDetails, Does.Contain("777").And.Contain("read twice"));
                Assert.That(stopped.Verdict.FieldName, Is.Null);
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(statusList ? 2 : 1));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(statusList ? 1 : 2));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task An_id_the_list_still_lacks_is_logged_at_error_naming_the_id(bool statusList)
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var instance = AnInstanceWhoseSearchReportsAnIdTheListsLack(statusList);

            await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler(), log).GetWorkItemsForTeam(ATeam(), CancellationToken.None));

            Assert.That(log.Written(LogLevel.Error).Any(line => line.Contains("777", StringComparison.Ordinal)), Is.True);
        }

        private static JiraInstanceStub AnInstanceWhoseSearchReportsAnIdTheListsLack(bool statusList)
            => new JiraInstanceStub()
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(statusList
                    ? JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Geschichte", "777", "Wird überprüft")
                    : JiraWireFormat.AnIssueSeenIn("PAY-1", "777", "Aufgabe", "3", "In Arbeit"));

        private static async Task RunningAgainst(JiraInstanceStub instance, string owner)
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(instance.Handler());

            switch (owner)
            {
                case "Team":
                    await connector.GetWorkItemsForTeam(ATeam(), CancellationToken.None);
                    break;
                case "Portfolio":
                    await connector.GetFeaturesForProject(APortfolio(), CancellationToken.None);
                    break;
                default:
                    var verdict = await connector.ValidateTeamSettings(ATeam());
                    throw new JiraReadException(verdict);
            }
        }

        [TestCase(true, HttpStatusCode.Forbidden, ForbiddenBody, 403, "Forbidden")]
        [TestCase(false, HttpStatusCode.OK, "[]", 200, "OK")]
        public async Task A_list_that_cannot_be_read_fails_the_settings_check_without_a_last_refresh_to_keep(
            bool statusList, HttpStatusCode answer, string body, int code, string status)
        {
            var instance = AnInstanceRefusing(statusList, answer, body);

            var verdict = await JiraConnectorTestSetup.AConnectorOver(instance.Handler()).ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Message, Is.EqualTo($"{Sentence(statusList)} Jira answered {code} ({status})."));
            }
        }

        [TestCase(true, HttpStatusCode.ServiceUnavailable, ServiceUnavailableBody, "status_list_unreadable", ListEndpointOfStatuses, "503 ServiceUnavailable")]
        [TestCase(false, HttpStatusCode.Forbidden, ForbiddenBody, "issue_type_list_unreadable", ListEndpointOfTypes, "403 Forbidden")]
        [TestCase(true, HttpStatusCode.OK, "[]", "status_list_unreadable", ListEndpointOfStatuses, "200 OK")]
        public async Task The_refusal_carries_its_code_and_the_request_with_what_Jira_said_and_names_no_input(
            bool statusList, HttpStatusCode answer, string body, string expectedCode, string route, string answered)
        {
            var instance = AnInstanceRefusing(statusList, answer, body);

            var stopped = await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler()).GetWorkItemsForTeam(ATeam(), CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopped!.Verdict.Code, Is.EqualTo(expectedCode));
                Assert.That(stopped.Verdict.TechnicalDetails, Is.EqualTo($"GET {route} answered {answered}. {body}"));
                Assert.That(stopped.Verdict.FieldName, Is.Null);
            }
        }

        [TestCase(true, HttpStatusCode.ServiceUnavailable, ServiceUnavailableBody)]
        [TestCase(false, HttpStatusCode.OK, AGatewayPage)]
        public async Task The_refusal_is_logged_at_error_with_the_request_and_Jiras_answer(bool statusList, HttpStatusCode answer, string body)
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var instance = AnInstanceRefusing(statusList, answer, body);

            await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler(), log).GetWorkItemsForTeam(ATeam(), CancellationToken.None));

            var route = statusList ? ListEndpointOfStatuses : ListEndpointOfTypes;
            var errors = log.Written(LogLevel.Error);

            Assert.That(
                errors.Any(line => line.Contains($"GET {route} answered {(int)answer} {answer}.", StringComparison.Ordinal)
                    && line.Contains(body, StringComparison.Ordinal)),
                Is.True,
                "The Error line has to name the request and carry Jira's answer, or support cannot tell a proxy page from a refusal.");
        }

        [Test]
        public async Task A_refusal_stops_the_refresh_before_any_issue_is_turned_into_a_work_item()
        {
            var instance = AnInstanceRefusing(statusList: true, HttpStatusCode.ServiceUnavailable, ServiceUnavailableBody)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Geschichte", "1", "Zu erledigen"));

            await Assert.ThrowsAsync<JiraReadException>(
                () => JiraConnectorTestSetup.AConnectorOver(instance.Handler()).GetWorkItemsForTeam(ATeam(), CancellationToken.None));

            Assert.That(instance.SearchBodies, Is.Empty, "A refresh that cannot tell the states must not even download the work items.");
        }

        private static string Sentence(bool statusList) => statusList ? StatusSentence : TypeSentence;

        private static JiraInstanceStub AnInstanceRefusing(bool statusList, HttpStatusCode answer, string body)
        {
            var instance = new JiraInstanceStub().Serving(JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do"));

            if (statusList)
            {
                instance.StatusList = (answer, body);
            }
            else
            {
                instance.IssueTypeList = (answer, body);
            }

            return instance;
        }

        private static Team ATeam() => JiraConnectorTestSetup.ATeamOnJiraCloud();

        private static Portfolio APortfolio() => JiraConnectorTestSetup.APortfolioOnJiraCloud();
    }
}
