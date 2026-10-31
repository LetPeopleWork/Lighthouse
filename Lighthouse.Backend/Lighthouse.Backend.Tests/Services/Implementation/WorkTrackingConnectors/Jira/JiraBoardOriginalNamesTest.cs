using System.Net;
using System.Text;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Load from board for an account that reads Jira in German. Statuses are sorted by the category's key,
    /// which is the same for every language, and every name is reported as an English account sees it.
    /// </summary>
    [TestFixture]
    public class JiraBoardOriginalNamesTest
    {
        private const string BoardId = "42";

        private static readonly string[] TheBoardsStatusIds = ["1", "3", "4", "5", "9"];

        [Test]
        [Ignore("Board load still sorts by the translated category name")]
        public async Task A_German_account_gets_the_To_Do_Doing_and_Done_states_an_English_account_gets()
        {
            var board = await LoadingTheBoard(AnInstance(JiraWireFormat.AGermanReadersStatuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "QA & Testing" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done", "Canceled" }));
            }
        }

        [Test]
        public async Task An_English_account_gets_the_same_states()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("1", "To Do", "To Do", "new", "To Do"),
                JiraWireFormat.AStatus("3", "In Progress", "In Progress", "indeterminate", "In Progress"),
                JiraWireFormat.AStatus("4", "QA & Testing", "QA & Testing", "indeterminate", "In Progress"),
                JiraWireFormat.AStatus("5", "Done", "Done", "done", "Done"),
                JiraWireFormat.AStatus("9", "Canceled", "Canceled", "done", "Done"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "QA & Testing" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done", "Canceled" }));
            }
        }

        [Test]
        public async Task A_custom_status_an_administrator_named_in_German_is_listed_as_it_is_next_to_the_English_names_for_an_English_account()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("3", "In Progress", "In Progress", "indeterminate", "In Progress"),
                JiraWireFormat.AStatus("4", "In Prüfung", "In Prüfung", "indeterminate", "In Progress"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "In Prüfung" }));
        }

        [Test]
        [Ignore("Board load still sorts by the translated category name")]
        public async Task A_custom_status_an_administrator_named_in_German_is_listed_as_it_is_next_to_the_English_names_for_a_German_account()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("3", "In Arbeit", "In Progress", "indeterminate", "In Arbeit"),
                JiraWireFormat.AStatus("4", "In Prüfung", "In Prüfung", "indeterminate", "In Arbeit"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "In Prüfung" }));
        }

        [Test]
        [Ignore("Board load still sorts by the translated category name")]
        public async Task A_deployment_that_cannot_be_identified_still_gets_the_original_names()
        {
            var board = await LoadingTheBoard(AnInstance(JiraWireFormat.AGermanReadersStatuses, issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                boardIssues: [("10001", "Geschichte")], deployment: null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "QA & Testing" }));
                Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story" }));
            }
        }

        [Test]
        public async Task A_Data_Center_board_reports_the_type_names_its_issues_carry_and_asks_for_no_type_list()
        {
            var paths = new List<string>();

            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                boardIssues: [("10001", "Story"), ("10002", "Bug")],
                deployment: "Server",
                onRequest: request => paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story", "Bug" }));
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal)), Is.Zero);
            }
        }

        [Test]
        public async Task A_status_without_an_original_name_is_reported_by_the_name_Jira_gave_it()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("1", "To Do", null, "new", "To Do"),
                JiraWireFormat.AStatus("3", "In Progress", null, "indeterminate", "In Progress"),
                JiraWireFormat.AStatus("5", "Done", null, "done", "Done"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done" }));
            }
        }

        [Test]
        public async Task A_Data_Center_status_without_a_category_key_is_sorted_by_its_category_name_as_before()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("1", "To Do", null, null, "To Do"),
                JiraWireFormat.AStatus("3", "In Progress", null, null, "In Progress"),
                JiraWireFormat.AStatus("5", "Done", null, null, "Done"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done" }));
            }
        }

        [Test]
        [Ignore("Board load does not de-duplicate state names yet")]
        public async Task A_state_name_shared_by_two_statuses_is_listed_once_ignoring_case_with_the_first_spelling()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("3", "In Arbeit", "In Progress", "indeterminate", "In Arbeit"),
                JiraWireFormat.AStatus("4", "In Arbeit", "in progress", "indeterminate", "In Arbeit"),
                JiraWireFormat.AStatus("5", "Fertig", "Done", "done", "Fertig"),
                JiraWireFormat.AStatus("9", "Fertig", "Done", "done", "Fertig"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done" }));
            }
        }

        [Test]
        [Ignore("Board load still sorts by the translated category name")]
        public async Task A_name_that_sits_in_two_categories_is_listed_in_each_of_them()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("3", "Review", "Review", "indeterminate", "In Arbeit"),
                JiraWireFormat.AStatus("5", "Review", "Review", "done", "Fertig"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "Review" }));
                Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Review" }));
            }
        }

        [Test]
        public async Task A_status_in_a_category_Lighthouse_does_not_know_is_left_out()
        {
            var statuses = new[]
            {
                JiraWireFormat.AStatus("1", "Zu erledigen", "To Do", "new", "Zu erledigen"),
                JiraWireFormat.AStatus("3", "Parked", "Parked", "undefined", "Keine Kategorie"),
            };

            var board = await LoadingTheBoard(AnInstance(statuses));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.DoingStates, Is.Empty);
                Assert.That(board.DoneStates, Is.Empty);
            }
        }

        [Test]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task The_work_item_types_are_the_original_names_each_once()
        {
            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                boardIssues: [("10001", "Geschichte"), ("10002", "Fehler"), ("10001", "Geschichte")]));

            Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story", "Bug" }));
        }

        [Test]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task Two_types_with_the_same_original_name_are_listed_once_ignoring_case()
        {
            var issueTypes = new[]
            {
                JiraWireFormat.AnIssueType("10001", "Geschichte", "Story"),
                JiraWireFormat.AnIssueType("10050", "Geschichte", "story"),
            };

            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                issueTypes: issueTypes,
                boardIssues: [("10001", "Geschichte"), ("10050", "Geschichte")]));

            Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story" }));
        }

        [Test]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task A_type_id_the_list_still_lacks_after_one_more_read_is_left_out_with_a_warning_naming_the_id()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var paths = new List<string>();

            var board = await LoadingTheBoard(
                AnInstance(
                    JiraWireFormat.AGermanReadersStatuses,
                    issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                    boardIssues: [("10001", "Geschichte"), ("99999", "Aufgabe")],
                    onRequest: request => paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty)),
                log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story" }), "A translated name must never stand in for the original.");
                Assert.That(log.Warnings.Any(line => line.Contains("99999", StringComparison.Ordinal)), Is.True);
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal)), Is.EqualTo(2));
            }
        }

        [Test]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task A_type_id_that_appears_after_one_more_read_is_reported_by_its_original_name()
        {
            var paths = new List<string>();

            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                boardIssues: [("10001", "Geschichte"), ("10077", "Aufgabe")],
                issueTypesOnReread: (HttpStatusCode.OK, JiraWireFormat.TheIssueTypeListOf(
                    [.. JiraWireFormat.AGermanReadersIssueTypes, JiraWireFormat.AnIssueType("10077", "Aufgabe", "Task")])),
                onRequest: request => paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story", "Task" }));
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal)), Is.EqualTo(2));
            }
        }

        [TestCase(HttpStatusCode.Forbidden, "[]", "403 (Forbidden)")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "<html>Service Unavailable</html>", "503 (Service Unavailable)")]
        [TestCase(HttpStatusCode.OK, "[]", "200 (OK)")]
        [TestCase(HttpStatusCode.OK, "<html>Sign in</html>", "200 (OK)")]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task A_work_item_type_list_that_cannot_be_read_gives_no_types_and_a_warning_naming_the_answer(
            HttpStatusCode answer, string body, string answerInTheWarning)
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();

            var board = await LoadingTheBoard(
                AnInstance(
                    JiraWireFormat.AGermanReadersStatuses,
                    issueTypesAnswer: (answer, body),
                    boardIssues: [("10001", "Geschichte"), ("10002", "Fehler")]),
                log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.Empty, "A translated name stored as if it were the original is the bug this feature removes.");
                Assert.That(log.Warnings.Any(line => line.Contains(answerInTheWarning, StringComparison.Ordinal)), Is.True);
            }
        }

        [Test]
        [Ignore("Board work item types are still the names in the board's issues")]
        public async Task A_status_list_that_cannot_be_read_gives_no_states_but_the_types_are_still_original()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();

            var board = await LoadingTheBoard(
                AnInstance(
                    JiraWireFormat.AGermanReadersStatuses,
                    issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                    boardIssues: [("10001", "Geschichte")],
                    statusesAnswer: (HttpStatusCode.Forbidden, "{}")),
                log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.Empty);
                Assert.That(board.DoingStates, Is.Empty);
                Assert.That(board.DoneStates, Is.Empty);
                Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Story" }));
                Assert.That(log.Warnings.Any(line => line.Contains("403 (Forbidden)", StringComparison.Ordinal)), Is.True);
            }
        }

        [Test]
        [Ignore("Board load does not check the status ids against the list yet")]
        public async Task A_status_id_the_list_still_lacks_after_one_more_read_is_left_out_with_a_warning_naming_the_id()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var paths = new List<string>();

            var board = await LoadingTheBoard(
                AnInstance(
                    JiraWireFormat.AnEnglishStatuses,
                    boardStatusIds: ["1", "3", "5", "777"],
                    onRequest: request => paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty)),
                log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates.Concat(board.DoingStates).Concat(board.DoneStates), Is.EqualTo(new List<string> { "To Do", "In Progress", "Done" }));
                Assert.That(log.Warnings.Any(line => line.Contains("777", StringComparison.Ordinal)), Is.True, "A status dropped without a word is the silent bug this feature removes.");
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.StatusListEndpoint, StringComparison.Ordinal)), Is.EqualTo(2));
            }
        }

        [Test]
        [Ignore("Board load does not check the status ids against the list yet")]
        public async Task A_status_id_that_appears_after_one_more_read_is_listed_by_its_original_name()
        {
            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AnEnglishStatuses,
                boardStatusIds: ["1", "3", "5", "10155"],
                statusesOnReread: (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(
                    JiraWireFormat.AnEnglishStatuses.Append(JiraWireFormat.AStatus("10155", "In Prüfung", "In Prüfung", "indeterminate", "In Progress"))))));

            Assert.That(board.DoingStates, Is.EqualTo(new List<string> { "In Progress", "In Prüfung" }));
        }

        [Test]
        [Ignore("The board load still reads the lists on its own")]
        public async Task Loading_the_board_reads_the_status_list_once_and_the_work_item_type_list_once()
        {
            var paths = new List<string>();

            await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                issueTypes: JiraWireFormat.AGermanReadersIssueTypes,
                boardIssues: [("10001", "Geschichte"), ("10002", "Fehler")],
                onRequest: request => paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.StatusListEndpoint, StringComparison.Ordinal)), Is.EqualTo(1));
                Assert.That(paths.Count(path => path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal)), Is.EqualTo(1));
            }
        }

        [Test]
        [Ignore("The board load and the refresh do not share one read of each list yet")]
        public async Task A_board_load_then_a_refresh_on_one_connector_reads_each_list_once()
        {
            var instance = new JiraInstanceStub()
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-2", "10001", "Geschichte", "3", "In Arbeit"));
            instance.BoardId = BoardId;
            instance.BoardStatusIds = TheBoardsStatusIds;
            instance.BoardIssues = [("10001", "Geschichte")];
            var connector = JiraConnectorTestSetup.AConnectorOver(instance.Handler());
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            await connector.GetBoardInformation(team.WorkTrackingSystemConnection, BoardId);
            var items = await connector.GetWorkItemsForTeam(team, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Single().State, Is.EqualTo("In Progress"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(1));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task A_Data_Center_board_reports_German_payload_type_names_unchanged()
        {
            var board = await LoadingTheBoard(AnInstance(
                JiraWireFormat.AGermanReadersStatuses,
                boardIssues: [("10001", "Geschichte"), ("10002", "Fehler")],
                deployment: "Server"));

            Assert.That(board.WorkItemTypes, Is.EqualTo(new List<string> { "Geschichte", "Fehler" }));
        }

        [Test]
        public async Task Loading_the_board_only_reads()
        {
            var requests = new List<HttpMethod>();

            await LoadingTheBoard(AnInstance(JiraWireFormat.AGermanReadersStatuses, onRequest: request => requests.Add(request.Method)));

            Assert.That(requests, Is.Not.Empty.And.All.EqualTo(HttpMethod.Get));
        }

        private static async Task<BoardInformation> LoadingTheBoard(
            HttpMessageHandler handler, RecordingLogger<JiraWorkTrackingConnector>? log = null)
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(handler, log ?? new RecordingLogger<JiraWorkTrackingConnector>());
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            return await connector.GetBoardInformation(team.WorkTrackingSystemConnection, BoardId);
        }

        private static HttpMessageHandler AnInstance(
            IEnumerable<string> statuses,
            IEnumerable<string>? issueTypes = null,
            (string Id, string Name)[]? boardIssues = null,
            (HttpStatusCode Status, string Body)? statusesAnswer = null,
            (HttpStatusCode Status, string Body)? issueTypesAnswer = null,
            Action<HttpRequestMessage>? onRequest = null,
            string? deployment = "Cloud",
            (HttpStatusCode Status, string Body)? issueTypesOnReread = null,
            string[]? boardStatusIds = null,
            (HttpStatusCode Status, string Body)? statusesOnReread = null)
        {
            var issueTypeReads = 0;
            var statusReads = 0;
            var columns = string.Join(",", (boardStatusIds ?? TheBoardsStatusIds).Select(id => "{\"statuses\":[{\"id\":\"" + id + "\"}]}"));
            var configuration = "{\"columnConfig\":{\"columns\":[" + columns + "]}}";
            var issues = string.Join(",", (boardIssues ?? [("10001", "Story")]).Select(
                issue => "{\"fields\":{\"issuetype\":{\"id\":\"" + issue.Id + "\",\"name\":\"" + issue.Name + "\"}}}"));

            return StubTransport.AnsweringWith(request =>
            {
                onRequest?.Invoke(request);

                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.EndsWith(JiraWireFormat.StatusListEndpoint, StringComparison.Ordinal))
                {
                    statusReads++;

                    return statusReads >= 2 && statusesOnReread is { } laterStatuses
                        ? Answer(laterStatuses)
                        : Answer(statusesAnswer ?? (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(statuses)));
                }

                if (path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal))
                {
                    issueTypeReads++;

                    return issueTypeReads >= 2 && issueTypesOnReread is { } later
                        ? Answer(later)
                        : Answer(issueTypesAnswer ?? (HttpStatusCode.OK, JiraWireFormat.TheIssueTypeListOf(issueTypes ?? JiraWireFormat.AnEnglishIssueTypes)));
                }

                if (path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) && deployment is null)
                {
                    return Answer((HttpStatusCode.InternalServerError, "{}"));
                }

                var body = path switch
                {
                    _ when path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) => $"{{\"deploymentType\":\"{deployment}\"}}",
                    _ when path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal) => configuration,
                    _ when path.EndsWith($"board/{BoardId}/issue", StringComparison.Ordinal) => "{\"issues\":[" + issues + "]}",
                    _ => "{}",
                };

                return Answer((HttpStatusCode.OK, body));
            });
        }

        private static HttpResponseMessage Answer((HttpStatusCode Status, string Body) answer)
            => new(answer.Status) { Content = new StringContent(answer.Body, Encoding.UTF8, "application/json") };
    }
}
