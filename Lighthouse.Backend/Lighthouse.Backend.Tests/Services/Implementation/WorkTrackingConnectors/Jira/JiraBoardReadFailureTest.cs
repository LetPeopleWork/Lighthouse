using System.Net;
using System.Text;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// What an administrator picking a Jira board is told when Jira did not hand the boards over. A list
    /// that failed and a list that is empty were the same answer, so a wrong email read as "no boards".
    /// This class carries no Integration category on purpose: CI skips the two connector classes that do.
    /// </summary>
    [TestFixture]
    public class JiraBoardReadFailureTest
    {
        private const string PendingTheJiraBoardFix = "pending: a failed Jira board read still answers as empty";

        private const string PendingTheTestConnectionMessage = "pending: Test connection still says only that authentication failed";

        private const string PendingThePartialBoardFix = "pending: a board Jira only partly hands over still comes back with no states or no types";

        private const string BoardId = "8";

        private const string TheBoardsWorkItems = $"board/{BoardId}/issue";

        private const string TheStatusList = JiraWireFormat.StatusListEndpoint;

        private const string TheWorkItemTypeList = JiraWireFormat.IssueTypeListEndpoint;

        private const string AGatewayPage = "<html><body>Sign in to continue</body></html>";

        private const string ABoardListEntryWithoutAnId = "{\"values\":[{\"name\":\"Stories\"}]}";

        private const string AnAnswerWithoutABoardConfiguration = "{}";

        private const string WhatAnUnreadableAnswerReads = "an answer Lighthouse could not read";

        private const string WhatSilenceReads = "no answer";

        [Test]
        public async Task An_administrator_asking_for_the_boards_of_a_Jira_that_refuses_them_is_told_what_Jira_said()
        {
            var connection = AConnection();
            var subject = AWizardsControllerOver(AJira(boards: _ => Responding(HttpStatusCode.Unauthorized, "{}")), connection);

            var answer = await subject.GetBoards(connection.Id);

            Assert.That(
                MessageOfTheRefusal(answer.Result),
                Is.EqualTo("Lighthouse could not read the boards from Jira: 401 Unauthorized. Check the connection's URL and sign-in details, then try again."));
        }

        [Test]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task An_administrator_asking_for_a_board_of_a_Jira_that_refuses_it_is_told_what_Jira_said()
        {
            var connection = AConnection();
            var subject = AWizardsControllerOver(AJira(configuration: _ => Responding(HttpStatusCode.Forbidden, "{}")), connection);

            var answer = await subject.GetBoardInformation(connection.Id, BoardId);

            Assert.That(
                MessageOfTheRefusal(answer.Result),
                Is.EqualTo("Lighthouse could not read this board from Jira: 403 Forbidden. Check the connection's URL and sign-in details, then try again."));
        }

        [TestCase(HttpStatusCode.Unauthorized, "401 Unauthorized")]
        [TestCase(HttpStatusCode.Forbidden, "403 Forbidden")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
        public async Task A_board_list_Jira_answers_with_a_failure_carries_that_answer(HttpStatusCode status, string answer)
        {
            var subject = AConnectorOver(AJira(boards: _ => Responding(status, "{\"errorMessages\":[]}")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence(answer)));
        }

        [Test]
        public async Task A_board_list_from_a_Jira_that_cannot_be_reached_reads_as_no_answer()
        {
            var subject = AConnectorOver(AJira(boards: _ => throw new HttpRequestException("No such host is known.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence(WhatSilenceReads)));
        }

        [Test]
        public async Task A_board_list_that_times_out_reads_as_no_answer()
        {
            var subject = AConnectorOver(AJira(boards: _ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence(WhatSilenceReads)));
        }

        [TestCase(AGatewayPage)]
        [TestCase(ABoardListEntryWithoutAnId)]
        public async Task A_board_list_Lighthouse_cannot_read_says_so(string body)
        {
            var subject = AConnectorOver(AJira(boards: _ => Responding(HttpStatusCode.OK, body)));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence(WhatAnUnreadableAnswerReads)));
        }

        [Test]
        public async Task A_board_list_that_breaks_unexpectedly_is_a_failure_and_keeps_its_stack_trace()
        {
            var fault = AnUnexpectedFault();
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = JiraConnectorTestSetup.AConnectorOver(AJira(boards: _ => throw fault), log);

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence(WhatAnUnreadableAnswerReads)));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.Some.SameAs(fault));
            }
        }

        [Test]
        public async Task A_failure_on_the_second_page_fails_the_whole_board_list()
        {
            var subject = AConnectorOver(AJira(boards: request => IsTheSecondPage(request)
                ? Responding(HttpStatusCode.ServiceUnavailable, "{}")
                : Responding(HttpStatusCode.OK, TheFirstOfTwoPages)));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardListSentence("503 Service Unavailable")));
        }

        [Test]
        public async Task A_board_list_over_two_pages_still_offers_every_board()
        {
            var subject = AConnectorOver(AJira(boards: request => Responding(
                HttpStatusCode.OK,
                IsTheSecondPage(request) ? TheLastOfTwoPages : TheFirstOfTwoPages)));

            var boards = (await subject.GetBoards(AConnection())).ToList();

            Assert.That(boards.Select(board => board.Name), Is.EqualTo(TheBoardsOfBothPages));
        }

        [TestCase("{\"isLast\":true,\"values\":[]}")]
        [TestCase("{\"values\":[]}")]
        public async Task A_Jira_that_really_has_no_boards_for_this_account_stays_an_empty_list(string body)
        {
            var subject = AConnectorOver(AJira(boards: _ => Responding(HttpStatusCode.OK, body)));

            var boards = await subject.GetBoards(AConnection());

            Assert.That(boards, Is.Empty, "Only a failure is a failure: an account that sees no board is a real answer.");
        }

        [Test]
        public async Task A_failed_board_list_is_logged_with_what_Jira_answered_and_not_as_a_bad_credential()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = JiraConnectorTestSetup.AConnectorOver(AJira(boards: _ => Responding(HttpStatusCode.ServiceUnavailable, "{}")), log);

            await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoards(AConnection()));

            var lines = log.Everything.Select(entry => entry.Message).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.None.Contains("Authentication is not valid"));
                Assert.That(lines, Has.Some.Contains("503 Service Unavailable"));
            }
        }

        [TestCase(HttpStatusCode.Unauthorized, "401 Unauthorized")]
        [TestCase(HttpStatusCode.Forbidden, "403 Forbidden")]
        [TestCase(HttpStatusCode.NotFound, "404 Not Found")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task A_board_Jira_answers_with_a_failure_carries_that_answer(HttpStatusCode status, string answer)
        {
            var subject = AConnectorOver(AJira(configuration: _ => Responding(status, "{}")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(answer)));
        }

        [Test]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task A_board_from_a_Jira_that_cannot_be_reached_reads_as_no_answer()
        {
            var subject = AConnectorOver(AJira(configuration: _ => throw new HttpRequestException("No such host is known.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatSilenceReads)));
        }

        [Test]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task A_board_read_that_times_out_reads_as_no_answer()
        {
            var subject = AConnectorOver(AJira(configuration: _ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatSilenceReads)));
        }

        [TestCase(AGatewayPage)]
        [TestCase(AnAnswerWithoutABoardConfiguration)]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task A_board_Lighthouse_cannot_read_says_so(string body)
        {
            var subject = AConnectorOver(AJira(configuration: _ => Responding(HttpStatusCode.OK, body)));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatAnUnreadableAnswerReads)));
        }

        [Test]
        [Ignore(PendingTheJiraBoardFix)]
        public async Task A_board_read_that_breaks_unexpectedly_is_a_failure_and_keeps_its_stack_trace()
        {
            var fault = AnUnexpectedFault();
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = JiraConnectorTestSetup.AConnectorOver(AJira(configuration: _ => throw fault), log);

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatAnUnreadableAnswerReads)));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.Some.SameAs(fault));
            }
        }

        [TestCase(TheStatusList, HttpStatusCode.Forbidden, "403 Forbidden")]
        [TestCase(TheStatusList, HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
        [TestCase(TheBoardsWorkItems, HttpStatusCode.Forbidden, "403 Forbidden")]
        [TestCase(TheBoardsWorkItems, HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
        [TestCase(TheWorkItemTypeList, HttpStatusCode.Forbidden, "403 Forbidden")]
        [TestCase(TheWorkItemTypeList, HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_board_Jira_hands_over_only_in_part_carries_the_answer_of_the_part_it_refused(string part, HttpStatusCode status, string answer)
        {
            var subject = AConnectorOver(ABoardWhere(part, () => Responding(status, "{}")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(answer)));
        }

        [TestCase(TheStatusList, AGatewayPage)]
        [TestCase(TheStatusList, "{}")]
        [TestCase(TheBoardsWorkItems, AGatewayPage)]
        [TestCase(TheBoardsWorkItems, "{}")]
        [TestCase(TheWorkItemTypeList, AGatewayPage)]
        [TestCase(TheWorkItemTypeList, "{}")]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_part_of_a_board_Lighthouse_cannot_read_says_so(string part, string body)
        {
            var subject = AConnectorOver(ABoardWhere(part, () => Responding(HttpStatusCode.OK, body)));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatAnUnreadableAnswerReads)));
        }

        [TestCase(TheStatusList)]
        [TestCase(TheBoardsWorkItems)]
        [TestCase(TheWorkItemTypeList)]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_part_of_a_board_that_cannot_be_reached_reads_as_no_answer(string part)
        {
            var subject = AConnectorOver(ABoardWhere(part, () => throw new HttpRequestException("No such host is known.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatSilenceReads)));
        }

        [TestCase(TheStatusList)]
        [TestCase(TheBoardsWorkItems)]
        [TestCase(TheWorkItemTypeList)]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_part_of_a_board_that_times_out_reads_as_no_answer(string part)
        {
            var subject = AConnectorOver(ABoardWhere(part, () => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.")));

            var failure = await Assert.ThrowsAsync<JiraReadException>(() => subject.GetBoardInformation(AConnection(), BoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo(TheBoardSentence(WhatSilenceReads)));
        }

        [Test]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_board_Jira_hands_over_whole_still_gives_its_states_and_types()
        {
            var subject = AConnectorOver(StubTransport.AnsweringWith(request => TheHealthyAnswerAt(request.RequestUri?.AbsolutePath ?? string.Empty)));

            var board = await subject.GetBoardInformation(AConnection(), BoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(string.Join(",", board.ToDoStates), Is.EqualTo("To Do"));
                Assert.That(string.Join(",", board.DoingStates), Is.EqualTo("In Progress"));
                Assert.That(string.Join(",", board.DoneStates), Is.EqualTo("Done"));
                Assert.That(string.Join(",", board.WorkItemTypes), Is.EqualTo("Story"));
            }
        }

        [Test]
        [Ignore(PendingThePartialBoardFix)]
        public async Task A_board_that_holds_no_work_items_yet_is_a_real_answer_and_not_a_failure()
        {
            var subject = AConnectorOver(ABoardWhere(TheBoardsWorkItems, () => Responding(HttpStatusCode.OK, "{\"issues\":[]}")));

            var board = await subject.GetBoardInformation(AConnection(), BoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.Empty);
                Assert.That(string.Join(",", board.ToDoStates), Is.EqualTo("To Do"));
            }
        }

        [Test]
        [Ignore(PendingTheTestConnectionMessage)]
        public async Task Test_connection_on_a_refused_credential_says_to_check_all_the_sign_in_details()
        {
            var subject = AConnectorOver(AJira(myself: _ => Responding(HttpStatusCode.Unauthorized, "{}")));

            var verdict = await subject.ValidateConnection(AConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Message,
                    Is.EqualTo("Authentication failed for Jira. Check all of this connection's sign-in details."));
                Assert.That(verdict.Code, Is.EqualTo("authentication_failed"));
                Assert.That(verdict.TechnicalDetails, Is.EqualTo("Jira returned 401 Unauthorized."));
                Assert.That(verdict.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.ApiToken));
            }
        }

        private const string AConfigurationOfThreeStatuses = "{\"columnConfig\":{\"columns\":[{\"statuses\":[{\"id\":\"1\"},{\"id\":\"3\"},{\"id\":\"5\"}]}]}}";

        private const string OneStoryOnTheBoard = "{\"issues\":[{\"fields\":{\"issuetype\":{\"id\":\"10001\",\"name\":\"Story\"}}}]}";

        private const string TheFirstOfTwoPages = "{\"isLast\":false,\"values\":[{\"id\":1,\"name\":\"Stories\"}]}";

        private const string TheLastOfTwoPages = "{\"isLast\":true,\"values\":[{\"id\":2,\"name\":\"Bugs\"}]}";

        private static readonly string[] TheBoardsOfBothPages = ["Stories", "Bugs"];

        private static InvalidOperationException AnUnexpectedFault() => new("Sequence contains no elements");

        private static string TheBoardListSentence(string answer)
            => $"Lighthouse could not read the boards from Jira: {answer}. Check the connection's URL and sign-in details, then try again.";

        private static string TheBoardSentence(string answer)
            => $"Lighthouse could not read this board from Jira: {answer}. Check the connection's URL and sign-in details, then try again.";

        private static bool IsTheSecondPage(HttpRequestMessage request)
            => !(request.RequestUri?.Query.Contains("startAt=0", StringComparison.Ordinal) ?? false);

        private static WorkTrackingSystemConnection AConnection()
            => JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;

        private static JiraWorkTrackingConnector AConnectorOver(HttpMessageHandler handler)
            => JiraConnectorTestSetup.AConnectorOver(handler);

        private static WizardsController AWizardsControllerOver(HttpMessageHandler handler, WorkTrackingSystemConnection connection)
        {
            var connections = new Mock<IRepository<WorkTrackingSystemConnection>>();
            connections.Setup(repository => repository.GetById(connection.Id)).Returns(connection);

            return new WizardsController(
                AConnectorOver(handler),
                Mock.Of<IAzureDevOpsWorkTrackingConnector>(),
                Mock.Of<ILinearWorkTrackingConnector>(),
                Mock.Of<IServiceNowWorkTrackingConnector>(),
                connections.Object);
        }

        private static string MessageOfTheRefusal(ActionResult? result)
        {
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>(),
                "A read that failed has to reach the picker as a refusal; an empty answer is what hid it.");

            return ((ConnectionValidationResult)((BadRequestObjectResult)result!).Value!).Message;
        }

        /// <summary>A Jira whose three reads of interest answer as the test says, and which answers everything else as a healthy Cloud site.</summary>
        private static HttpMessageHandler AJira(
            Func<HttpRequestMessage, HttpResponseMessage>? boards = null,
            Func<HttpRequestMessage, HttpResponseMessage>? configuration = null,
            Func<HttpRequestMessage, HttpResponseMessage>? myself = null)
            => StubTransport.AnsweringWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (configuration is not null && path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal))
                {
                    return configuration(request);
                }

                if (boards is not null && path.EndsWith("rest/agile/latest/board", StringComparison.Ordinal))
                {
                    return boards(request);
                }

                if (myself is not null && path.EndsWith(JiraRestRoutes.Myself, StringComparison.Ordinal))
                {
                    return myself(request);
                }

                return path.EndsWith("serverInfo", StringComparison.Ordinal)
                    ? Responding(HttpStatusCode.OK, "{\"deploymentType\":\"Cloud\"}")
                    : Responding(HttpStatusCode.OK, "{}");
            });

        /// <summary>A board Jira hands over whole, except for the one part the test answers itself.</summary>
        private static HttpMessageHandler ABoardWhere(string part, Func<HttpResponseMessage> answer)
            => StubTransport.AnsweringWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                return path.EndsWith(part, StringComparison.Ordinal) ? answer() : TheHealthyAnswerAt(path);
            });

        private static HttpResponseMessage TheHealthyAnswerAt(string path)
        {
            var body = JiraWireFormat.TheNameListAnsweredAt(path) ?? path switch
            {
                _ when path.EndsWith("serverInfo", StringComparison.Ordinal) => JiraWireFormat.ACloudDeployment,
                _ when path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal) => AConfigurationOfThreeStatuses,
                _ when path.EndsWith(TheBoardsWorkItems, StringComparison.Ordinal) => OneStoryOnTheBoard,
                _ => "{}",
            };

            return Responding(HttpStatusCode.OK, body);
        }

        private static HttpResponseMessage Responding(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
