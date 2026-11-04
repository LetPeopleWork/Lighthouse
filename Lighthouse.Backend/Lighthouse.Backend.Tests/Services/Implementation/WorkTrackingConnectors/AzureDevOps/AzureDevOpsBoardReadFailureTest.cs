using System.Net;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using WorkBoard = Microsoft.TeamFoundation.Work.WebApi.Board;
using WorkBoardColumn = Microsoft.TeamFoundation.Work.WebApi.BoardColumn;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// What an administrator picking an Azure DevOps board is told when Azure DevOps did not hand the boards
    /// over. The list is read project by project, so a rejected token used to end as an empty list twice
    /// over: once per project, and once around the whole read. Any project that fails fails the whole list,
    /// because a partial list reads as the complete one.
    /// This class carries no Integration category on purpose: CI skips the connector class that does.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsBoardReadFailureTest
    {
        private const string WhatAzureDevOpsSaid = "VS30063: You are not authorized to access https://dev.azure.com";

        private const string WhatAzureDevOpsSaidEndingInAFullStop = WhatAzureDevOpsSaid + ".";

        private const string WhatSilenceReads = "no answer";

        private const string WhatAnUnreadableAnswerReads = "an answer Lighthouse could not read";

        private const string TheFirstHalfOfTheListSentence = "Lighthouse could not read the boards from Azure DevOps: ";

        private const string TheFirstHalfOfTheBoardSentence = "Lighthouse could not read this board from Azure DevOps: ";

        private const string TheLastHalfOfTheSentences = ". Check the connection's URL and sign-in details, then try again.";

        private const string ABoardId = "e7b3c1df-8d70-4943-98a7-ef00c7a0c523|67811f97-d952-419d-927b-6f5eaf47aadd";

        private const string AWorkingProject = "Working Project";

        private const string AFailingProject = "Failing Project";

        private const string AnotherFailingProject = "Refused Project";

        private const string ABoardOfTheWorkingProject = "Stories";

        private const string TheDetailsOfABoardWithNoTypesOrNoStates = $"Board '{ABoardId}' came back with no work item types or no states";

        [Test]
        public async Task An_administrator_asking_for_the_boards_of_an_Azure_DevOps_that_refuses_them_is_told_what_it_said()
        {
            var organisation = new AnOrganisation { ProjectListFailure = ARefusal() };
            var connection = AConnection();

            var answer = await AWizardsControllerOver(organisation, connection).GetBoards(connection.Id);

            Assert.That(
                MessageOfTheRefusal(answer.Result),
                Is.EqualTo("Lighthouse could not read the boards from Azure DevOps: VS30063: You are not authorized to access https://dev.azure.com. Check the connection's URL and sign-in details, then try again."));
        }

        [Test]
        public async Task An_administrator_asking_for_a_board_of_an_Azure_DevOps_that_refuses_it_is_told_what_it_said()
        {
            var organisation = new AnOrganisation { BoardFailure = ARefusal() };
            var connection = AConnection();

            var answer = await AWizardsControllerOver(organisation, connection).GetBoardInformation(connection.Id, ABoardId);

            Assert.That(
                MessageOfTheRefusal(answer.Result),
                Is.EqualTo("Lighthouse could not read this board from Azure DevOps: VS30063: You are not authorized to access https://dev.azure.com. Check the connection's URL and sign-in details, then try again."));
        }

        [Test]
        public async Task A_rejected_token_on_the_only_project_fails_the_list_and_names_the_project()
        {
            var organisation = new AnOrganisation().WithAProjectThatFails(AFailingProject, ARefusal());

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            var message = failure!.Verdict.Message;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.StartWith(TheFirstHalfOfTheListSentence));
                Assert.That(message, Does.EndWith(TheLastHalfOfTheSentences));
                Assert.That(message, Does.Contain(AFailingProject));
                Assert.That(message, Does.Contain(WhatAzureDevOpsSaid));
            }
        }

        [Test]
        public async Task One_project_failing_among_others_fails_the_whole_list_and_names_only_that_project()
        {
            var organisation = new AnOrganisation()
                .WithAProject(AWorkingProject, ABoardOfTheWorkingProject)
                .WithAProjectThatFails(AFailingProject, ARefusal());

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            var message = failure!.Verdict.Message;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.StartWith(TheFirstHalfOfTheListSentence));
                Assert.That(message, Does.EndWith(TheLastHalfOfTheSentences));
                Assert.That(message, Does.Contain(AFailingProject));
                Assert.That(message, Does.Contain(WhatAzureDevOpsSaid));
                Assert.That(message, Does.Not.Contain(AWorkingProject),
                    "The project that answered is not what the administrator has to go and look at.");
                Assert.That(message, Does.Not.Contain(ABoardOfTheWorkingProject));
            }
        }

        [Test]
        public async Task Two_projects_failing_are_both_named()
        {
            var organisation = new AnOrganisation()
                .WithAProject(AWorkingProject, ABoardOfTheWorkingProject)
                .WithAProjectThatFails(AFailingProject, ARefusal())
                .WithAProjectThatFails(AnotherFailingProject, ARefusal());

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Verdict.Message,
                    Is.EqualTo($"{TheFirstHalfOfTheListSentence}{AFailingProject}: {WhatAzureDevOpsSaid}; {AnotherFailingProject}: {WhatAzureDevOpsSaid}{TheLastHalfOfTheSentences}"));
                Assert.That(failure.Verdict.Code, Is.EqualTo("board_list_unreadable"));
                Assert.That(
                    failure.Verdict.TechnicalDetails,
                    Is.EqualTo($"{AFailingProject}: {WhatAzureDevOpsSaid} | {AnotherFailingProject}: {WhatAzureDevOpsSaid}"));
            }
        }

        [Test]
        public async Task A_status_Azure_DevOps_sent_without_a_sentence_of_its_own_still_reaches_the_picker()
        {
            var organisation = new AnOrganisation()
                .WithAProjectThatFails(AFailingProject, new VssServiceResponseException(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable", null));

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Does.Contain("503"),
                "A proxy page carries no error contract, so the number is the only thing telling one failure from another.");
        }

        [Test]
        public async Task Each_failing_project_keeps_its_line_in_the_error_log()
        {
            var log = new RecordingLogger<AzureDevOpsWorkTrackingConnector>();
            var organisation = new AnOrganisation()
                .WithAProject(AWorkingProject, ABoardOfTheWorkingProject)
                .WithAProjectThatFails(AFailingProject, ARefusal());

            await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector(log).GetBoards(AConnection()));

            var errors = log.Written(LogLevel.Error).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(errors, Has.Count.EqualTo(1));
                Assert.That(errors, Has.All.Contains(AFailingProject));
            }
        }

        [TestCaseSource(nameof(WaysOfGettingNoAnswer))]
        public async Task A_project_list_nobody_answered_reads_as_no_answer(Exception silence)
        {
            var organisation = new AnOrganisation { ProjectListFailure = silence };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheListSentence}{WhatSilenceReads}{TheLastHalfOfTheSentences}"));
        }

        [TestCaseSource(nameof(WaysOfGettingNoAnswer))]
        public async Task A_project_nobody_answered_for_reads_as_no_answer_and_is_named(Exception silence)
        {
            var organisation = new AnOrganisation().WithAProjectThatFails(AFailingProject, silence);

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Does.Contain(WhatSilenceReads));
                Assert.That(failure.Verdict.Message, Does.Contain(AFailingProject));
            }
        }

        [TestCaseSource(nameof(WaysOfGettingNoAnswer))]
        public async Task A_board_nobody_answered_for_reads_as_no_answer(Exception silence)
        {
            var organisation = new AnOrganisation { BoardFailure = silence };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatSilenceReads}{TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task A_project_list_that_breaks_unexpectedly_is_a_failure_and_keeps_its_stack_trace()
        {
            var fault = AnUnexpectedFault();
            var log = new RecordingLogger<AzureDevOpsWorkTrackingConnector>();
            var organisation = new AnOrganisation { ProjectListFailure = fault };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector(log).GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheListSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.Some.SameAs(fault));
            }
        }

        [Test]
        public async Task A_project_that_breaks_unexpectedly_fails_the_list_and_keeps_its_stack_trace()
        {
            var fault = AnUnexpectedFault();
            var log = new RecordingLogger<AzureDevOpsWorkTrackingConnector>();
            var organisation = new AnOrganisation()
                .WithAProject(AWorkingProject, ABoardOfTheWorkingProject)
                .WithAProjectThatFails(AFailingProject, fault);

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector(log).GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Does.Contain(AFailingProject));
                Assert.That(failure.Verdict.Message, Does.Contain(WhatAnUnreadableAnswerReads));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.Some.SameAs(fault));
            }
        }

        [Test]
        public async Task A_board_read_that_breaks_unexpectedly_is_a_failure_and_keeps_its_stack_trace()
        {
            var fault = AnUnexpectedFault();
            var log = new RecordingLogger<AzureDevOpsWorkTrackingConnector>();
            var organisation = new AnOrganisation { BoardFailure = fault };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector(log).GetBoardInformation(AConnection(), ABoardId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.Some.SameAs(fault));
            }
        }

        [Test]
        public async Task A_full_stop_ending_what_Azure_DevOps_said_about_the_project_list_is_not_doubled()
        {
            var organisation = new AnOrganisation { ProjectListFailure = ARefusalEndingInAFullStop() };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheListSentence}{WhatAzureDevOpsSaid}{TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task A_full_stop_ending_what_Azure_DevOps_said_about_a_project_is_not_doubled()
        {
            var organisation = new AnOrganisation().WithAProjectThatFails(AFailingProject, ARefusalEndingInAFullStop());

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Does.Contain(WhatAzureDevOpsSaid));
                Assert.That(failure.Verdict.Message, Does.Not.Contain(".."));
            }
        }

        [Test]
        public async Task A_full_stop_ending_what_Azure_DevOps_said_about_a_board_is_not_doubled()
        {
            var organisation = new AnOrganisation { BoardFailure = ARefusalEndingInAFullStop() };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAzureDevOpsSaid}{TheLastHalfOfTheSentences}"));
        }

        [TestCase("e7b3c1df-8d70-4943-98a7-ef00c7a0c523")]
        [TestCase("e7b3c1df-8d70-4943-98a7-ef00c7a0c523-e7b3c1df-8d70-4943-98a7-ef00c7a0c523")]
        [TestCase("first|second|third")]
        [TestCase("Jambalaya")]
        [TestCase("|")]
        [TestCase("x|")]
        [TestCase("|y")]
        [TestCase("a|b")]
        [TestCase("e7b3c1df-8d70-4943-98a7-ef00c7a0c523|x")]
        [TestCase("x|e7b3c1df-8d70-4943-98a7-ef00c7a0c523")]
        public async Task A_malformed_board_id_is_a_failed_read_and_never_an_empty_board(string malformedBoardId)
        {
            var organisation = new AnOrganisation();

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), malformedBoardId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Verdict.Message,
                    Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}the board id is not one Azure DevOps gave{TheLastHalfOfTheSentences}"),
                    "An empty board would enable Confirm and write nothing, and a sentence with no answer in it says nothing either.");
                Assert.That(failure.Verdict.Code, Is.EqualTo("board_unreadable"));
                Assert.That(failure.Verdict.TechnicalDetails, Is.EqualTo($"Board id '{malformedBoardId}' does not hold a project and a board"));
            }
        }

        [Test]
        public async Task A_board_Azure_DevOps_answers_with_no_columns_is_a_failed_read_and_never_an_empty_board()
        {
            var organisation = new AnOrganisation { BoardAnswer = ABoard() };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
                Assert.That(failure.Verdict.TechnicalDetails, Is.EqualTo(TheDetailsOfABoardWithNoTypesOrNoStates));
            }
        }

        [Test]
        public async Task A_board_whose_columns_map_no_states_is_a_failed_read_and_never_an_empty_board()
        {
            var organisation = new AnOrganisation { BoardAnswer = ABoard(new WorkBoardColumn { ColumnType = BoardColumnType.Incoming, StateMappings = new Dictionary<string, string>() }) };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
                Assert.That(failure.Verdict.TechnicalDetails, Is.EqualTo(TheDetailsOfABoardWithNoTypesOrNoStates));
            }
        }

        [Test]
        public async Task A_board_with_work_item_types_but_no_column_that_maps_them_to_a_state_is_a_failed_read()
        {
            var organisation = new AnOrganisation { BoardAnswer = ABoard(AColumn((BoardColumnType)99, "Active")) };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task A_board_whose_only_states_are_doing_states_is_still_read()
        {
            var organisation = new AnOrganisation { BoardAnswer = ABoard(AColumn(BoardColumnType.InProgress, "Active")) };

            var board = await organisation.Connector().GetBoardInformation(AConnection(), ABoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.Empty);
                Assert.That(board.DoingStates, Is.EqualTo(Just("Active")));
                Assert.That(board.DoneStates, Is.Empty);
            }
        }

        [Test]
        public async Task A_board_whose_only_states_are_done_states_is_still_read()
        {
            var organisation = new AnOrganisation { BoardAnswer = ABoard(AColumn(BoardColumnType.Outgoing, "Closed")) };

            var board = await organisation.Connector().GetBoardInformation(AConnection(), ABoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.ToDoStates, Is.Empty);
                Assert.That(board.DoingStates, Is.Empty);
                Assert.That(board.DoneStates, Is.EqualTo(Just("Closed")));
            }
        }

        [Test]
        public async Task A_normal_board_is_read_into_its_work_item_types_and_states()
        {
            var organisation = new AnOrganisation
            {
                BoardAnswer = ABoard(
                    AColumn(BoardColumnType.Incoming, "New"),
                    AColumn(BoardColumnType.InProgress, "Active"),
                    AColumn(BoardColumnType.Outgoing, "Closed")),
            };

            var board = await organisation.Connector().GetBoardInformation(AConnection(), ABoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(board.WorkItemTypes, Is.EqualTo(Just("User Story")));
                Assert.That(board.ToDoStates, Is.EqualTo(Just("New")));
                Assert.That(board.DoingStates, Is.EqualTo(Just("Active")));
                Assert.That(board.DoneStates, Is.EqualTo(Just("Closed")));
            }
        }

        [Test]
        public async Task A_status_Azure_DevOps_sent_with_an_empty_message_reads_as_the_status_alone()
        {
            var organisation = new AnOrganisation { BoardFailure = new VssServiceResponseException(HttpStatusCode.ServiceUnavailable, string.Empty, null) };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}503 (ServiceUnavailable){TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task A_message_ending_in_a_full_stop_and_a_space_is_not_left_with_a_stray_full_stop()
        {
            var organisation = new AnOrganisation { BoardFailure = new VssUnauthorizedException(WhatAzureDevOpsSaid + ". ") };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAzureDevOpsSaid}{TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task A_refusal_with_no_message_at_all_reads_as_an_unreadable_answer()
        {
            var organisation = new AnOrganisation { BoardFailure = new VssUnauthorizedException(string.Empty) };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.Message, Is.EqualTo($"{TheFirstHalfOfTheBoardSentence}{WhatAnUnreadableAnswerReads}{TheLastHalfOfTheSentences}"));
        }

        [Test]
        public async Task The_technical_details_of_a_board_list_that_broke_unexpectedly_carry_no_raw_exception_text()
        {
            var organisation = new AnOrganisation { ProjectListFailure = AnUnexpectedFault() };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(() => organisation.Connector().GetBoards(AConnection()));

            Assert.That(failure!.Verdict.TechnicalDetails, Is.EqualTo($"{nameof(InvalidOperationException)}: {WhatAnUnreadableAnswerReads}"));
        }

        [Test]
        public async Task The_technical_details_of_a_board_that_broke_unexpectedly_carry_no_raw_exception_text()
        {
            var organisation = new AnOrganisation { BoardFailure = AnUnexpectedFault() };

            var failure = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                () => organisation.Connector().GetBoardInformation(AConnection(), ABoardId));

            Assert.That(failure!.Verdict.TechnicalDetails, Is.EqualTo($"{nameof(InvalidOperationException)}: {WhatAnUnreadableAnswerReads}"));
        }

        [Test]
        public async Task An_organisation_with_no_projects_stays_an_empty_list()
        {
            var boards = await new AnOrganisation().Connector().GetBoards(AConnection());

            Assert.That(boards, Is.Empty);
        }

        [Test]
        public async Task Projects_without_boards_stay_an_empty_list()
        {
            var organisation = new AnOrganisation().WithAProject(AWorkingProject);

            var boards = await organisation.Connector().GetBoards(AConnection());

            Assert.That(boards, Is.Empty, "Only a failure is a failure: a project with no board is a real answer.");
        }

        [Test]
        public async Task Boards_of_every_project_that_answers_are_offered()
        {
            var organisation = new AnOrganisation()
                .WithAProject(AWorkingProject, ABoardOfTheWorkingProject)
                .WithAProject("Second Project", "Epics");

            var boards = (await organisation.Connector().GetBoards(AConnection())).ToList();

            Assert.That(
                boards.Select(board => board.Name),
                Is.EquivalentTo(new[] { $"{AWorkingProject} - {ABoardOfTheWorkingProject}", "Second Project - Epics" }));
        }

        private static IEnumerable<Exception> WaysOfGettingNoAnswer()
        {
            yield return new HttpRequestException("No such host is known. (dev.azure.com:443)");
            yield return new TimeoutException("The HTTP request timed out after 00:01:40.");
            yield return new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");
        }

        private static VssUnauthorizedException ARefusal() => new(WhatAzureDevOpsSaid);

        private static VssUnauthorizedException ARefusalEndingInAFullStop() => new(WhatAzureDevOpsSaidEndingInAFullStop);

        private static InvalidOperationException AnUnexpectedFault() => new("Sequence contains no elements");

        private static List<string> Just(string only) => [only];

        private static WorkBoard ABoard(params WorkBoardColumn[] columns)
            => new() { Columns = columns.ToList(), Links = ALinkToTheTeam() };

        private static WorkBoardColumn AColumn(BoardColumnType type, string state)
            => new() { ColumnType = type, StateMappings = new Dictionary<string, string> { ["User Story"] = state } };

        private static ReferenceLinks ALinkToTheTeam()
        {
            var links = new ReferenceLinks();
            links.AddLink("team", "https://dev.azure.com/lighthouse-test/project/_apis/teams/team-id");

            return links;
        }

        private static WorkTrackingSystemConnection AConnection()
            => new()
            {
                Id = 64990,
                Name = "Azure DevOps of the board failure tests",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

        private static WizardsController AWizardsControllerOver(AnOrganisation organisation, WorkTrackingSystemConnection connection)
        {
            var connections = new Mock<IRepository<WorkTrackingSystemConnection>>();
            connections.Setup(repository => repository.GetById(connection.Id)).Returns(connection);

            return new WizardsController(
                Mock.Of<IJiraWorkTrackingConnector>(),
                organisation.Connector(),
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

        /// <summary>
        /// An organisation as the project and board clients see it. Only the clients are substituted, so the
        /// walk over the projects, the throttle and everything the connector does with a failure is the
        /// production path.
        /// </summary>
        private sealed class AnOrganisation
        {
            private static readonly Uri Address = new("https://dev.azure.com/lighthouse-test");

            private readonly List<TeamProjectReference> projects = [];

            private readonly Mock<WorkHttpClient> work = new(Address, new VssCredentials());

            private readonly Mock<ProjectHttpClient> projectClient = new(Address, new VssCredentials());

            public Exception? ProjectListFailure { get; init; }

            public Exception? BoardFailure { get; init; }

            public WorkBoard? BoardAnswer { get; init; }

            public AnOrganisation WithAProject(string name, params string[] boardNames)
            {
                var project = AddProject(name);
                var boards = boardNames.Select(boardName => new BoardReference { Id = Guid.NewGuid(), Name = boardName }).ToList();

                work.Setup(client => client.GetBoardsAsync(ForProject(project), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(boards);

                return this;
            }

            public AnOrganisation WithAProjectThatFails(string name, Exception failure)
            {
                var project = AddProject(name);

                work.Setup(client => client.GetBoardsAsync(ForProject(project), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(failure);

                return this;
            }

            public ConnectorOverFakeClients Connector(ILogger<AzureDevOpsWorkTrackingConnector>? logger = null)
            {
                var answeredProjects = projectClient.Setup(client => client.GetProjects(
                    It.IsAny<ProjectState?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<bool?>()));

                if (ProjectListFailure is null)
                {
                    answeredProjects.ReturnsAsync(new PagedList<TeamProjectReference>(projects, null));
                }
                else
                {
                    answeredProjects.ThrowsAsync(ProjectListFailure);
                }

                if (BoardFailure is not null)
                {
                    work.Setup(client => client.GetBoardAsync(It.IsAny<TeamContext>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(BoardFailure);
                }

                if (BoardAnswer is not null)
                {
                    work.Setup(client => client.GetBoardAsync(It.IsAny<TeamContext>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(BoardAnswer);
                    work.Setup(client => client.GetTeamFieldValuesAsync(It.IsAny<TeamContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new TeamFieldValues
                        {
                            Field = new FieldReference { ReferenceName = "System.AreaPath" },
                            Values = [new TeamFieldValue { Value = "Project", IncludeChildren = true }],
                        });
                }

                return new ConnectorOverFakeClients(work.Object, projectClient.Object, logger);
            }

            private TeamProjectReference AddProject(string name)
            {
                var project = new TeamProjectReference { Id = Guid.NewGuid(), Name = name };
                projects.Add(project);

                return project;
            }

            private static TeamContext ForProject(TeamProjectReference project)
                => It.Is<TeamContext>(context => context.ProjectId == project.Id);
        }

        private sealed class ConnectorOverFakeClients(
            WorkHttpClient work,
            ProjectHttpClient projects,
            ILogger<AzureDevOpsWorkTrackingConnector>? logger)
            : AzureDevOpsWorkTrackingConnector(
                logger ?? Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(),
                Mock.Of<IWorkTrackingAuthStrategyFactory>(),
                TimeProvider.System)
        {
            internal override Task<TClient> GetClientAsync<TClient>(WorkTrackingSystemConnection workTrackingSystemConnection)
                => Task.FromResult((TClient)(typeof(TClient) == typeof(WorkHttpClient) ? (object)work : projects));
        }
    }
}
