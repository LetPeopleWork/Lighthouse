using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The query a board hands to a new team is welded together from two halves Jira stores apart: the saved
    /// filter behind the board, and the board's own sub-filter. Either half can be missing or unreadable, and
    /// neither arrives as a self-contained expression. These tests read the assembled string itself rather
    /// than a success flag, because a query Jira happily accepts can still select the wrong work items.
    ///
    /// The rest of the fixture covers what the user is told when Jira will not run the query at all. Jira
    /// names the character it tripped over, and that sentence is the only part of the failure anyone can act
    /// on, so these tests read the reported text rather than a failure flag for the same reason.
    /// </summary>
    [TestFixture]
    public class JiraBoardQueryAssemblyTest
    {
        private const string BoardId = "42";
        private const string FilterId = "10001";

        private const string TeamQuery = "project = PROJ";

        private const string JiraRejectionSentence =
            "Error in the JQL Query: Expecting a field name but got 'AND'. You must surround 'AND' in quotation marks to use it as a field name. (line 1, character 3)";

        /// <summary>
        /// The log carries Jira's answer byte for byte, and Jira escapes every apostrophe in it, so the
        /// quoted field name only reads back as a quoted field name once the JSON has been decoded. A log
        /// assertion therefore has to match the part of the sentence that survives the escaping.
        /// </summary>
        private const string JiraRejectionOpening = "Error in the JQL Query: Expecting a field name but got";

        private const string OnePageHoldingNothing = "{\"startAt\":0,\"maxResults\":50,\"total\":0,\"issues\":[]}";

        private const string OnePageHoldingOneIssue =
            "{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{\"key\":\"PROJ-1\",\"fields\":{"
            + "\"summary\":\"An issue\",\"created\":\"2026-08-01T09:00:00.000+0000\","
            + "\"status\":{\"name\":\"In Progress\"},\"issuetype\":{\"name\":\"Story\"}}}]}";

        private static readonly string RejectedQueryBody =
            $"{{\"errorMessages\":[{JsonSerializer.Serialize(JiraRejectionSentence)}],\"errors\":{{}}}}";

        private static readonly string ReadableBoardConfiguration =
            "{\"columnConfig\":{\"columns\":["
            + "{\"statuses\":[{\"id\":\"1\"}]},"
            + "{\"statuses\":[{\"id\":\"3\"}]},"
            + "{\"statuses\":[{\"id\":\"5\"}]}"
            + $"]}},\"filter\":{{\"id\":\"{FilterId}\"}},\"subQuery\":{{\"query\":\"fixVersion is EMPTY\"}}}}";

        private const string BoardIssues =
            "{\"issues\":["
            + "{\"fields\":{\"issuetype\":{\"name\":\"Story\"}}},"
            + "{\"fields\":{\"issuetype\":{\"name\":\"Bug\"}}},"
            + "{\"fields\":{\"issuetype\":{\"name\":\"Story\"}}}"
            + "]}";

        private const string InstanceStatuses =
            "["
            + "{\"id\":\"1\",\"name\":\"To Do\",\"statusCategory\":{\"name\":\"To Do\"}},"
            + "{\"id\":\"3\",\"name\":\"In Progress\",\"statusCategory\":{\"name\":\"In Progress\"}},"
            + "{\"id\":\"5\",\"name\":\"Done\",\"statusCategory\":{\"name\":\"Done\"}},"
            + "{\"id\":\"9\",\"name\":\"Cancelled\",\"statusCategory\":{\"name\":\"Done\"}}"
            + "]";

        [TestCase(HttpStatusCode.Forbidden, "403")]
        [TestCase(HttpStatusCode.NotFound, "404")]
        public void GetBoardInformation_FilterCannotBeRead_RefusesTheBoardInsteadOfScopingItToEverything(
            HttpStatusCode filterStatus, string expectedStatusInMessage)
        {
            var refusal = Assert.ThrowsAsync<JiraReadException>(
                async () => await BoardQueryFor("project = FOO", "fixVersion is EMPTY", filterStatus));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Message, Does.Contain(expectedStatusInMessage));
                Assert.That(refusal.Verdict.Message, Does.Contain("permission"));
                Assert.That(refusal.Verdict.Message, Does.Contain("account"));
                Assert.That(refusal.Verdict.IsValid, Is.False);
            }
        }

        [Test]
        public void GetBoardInformation_FilterCarriesNoQuery_RefusesTheBoardTheSameWay()
        {
            var refusal = Assert.ThrowsAsync<JiraReadException>(
                async () => await BoardQueryWhereTheFilterAnswers(
                    FilterId, "fixVersion is EMPTY", HttpStatusCode.OK, "{\"name\":\"Board filter\"}"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Message, Does.Contain("permission"));
                Assert.That(refusal.Verdict.Message, Does.Contain("account"));
            }
        }

        [Test]
        public async Task GetBoardInformation_FilterIsOnlyAnOrdering_DoesNotProduceAQueryStartingWithAnd()
        {
            var query = await BoardQueryFor("ORDER BY Rank ASC", "fixVersion in unreleasedVersions() OR fixVersion is EMPTY");

            Assert.That(query, Is.EqualTo("(fixVersion in unreleasedVersions() OR fixVersion is EMPTY)"));
        }

        [Test]
        public async Task GetBoardInformation_FilterAndSubFilterBothPresent_BracketsEachHalf()
        {
            var query = await BoardQueryFor("project = LIGHTHOUSE AND type IN (Bug, Story)", "fixVersion is EMPTY");

            Assert.That(query, Is.EqualTo("(project = LIGHTHOUSE AND type IN (Bug, Story)) AND (fixVersion is EMPTY)"));
        }

        [Test]
        public async Task GetBoardInformation_FilterHasTopLevelOr_KeepsBothOperandsUnderTheSubFilter()
        {
            var query = await BoardQueryFor("project = ALPHA OR project = BETA", "status != Done");

            Assert.That(query, Is.EqualTo("(project = ALPHA OR project = BETA) AND (status != Done)"));
        }

        [Test]
        public async Task GetBoardInformation_SubFilterCarriesAnOrdering_StripsItLikeTheFilter()
        {
            var query = await BoardQueryFor("project = FOO", "resolution is EMPTY ORDER BY Rank");

            Assert.That(query, Is.EqualTo("(project = FOO) AND (resolution is EMPTY)"));
        }

        [Test]
        public async Task GetBoardInformation_OrderingNestedInBrackets_LeavesBracketsBalanced()
        {
            var query = await BoardQueryFor("project = FOO AND (status = Open ORDER BY Rank)", subFilterJql: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(query.Count(character => character == '('), Is.EqualTo(query.Count(character => character == ')')));
                Assert.That(query, Is.EqualTo("(project = FOO AND (status = Open ORDER BY Rank))"));
            }
        }

        [Test]
        public async Task GetBoardInformation_FilterWithoutSubFilter_BracketsTheFilterAlone()
        {
            var query = await BoardQueryFor("project = FOO", subFilterJql: null);

            Assert.That(query, Is.EqualTo("(project = FOO)"));
        }

        [Test]
        public async Task GetBoardInformation_NeitherFilterNorSubFilter_ProducesAnEmptyQuery()
        {
            var query = await BoardQueryFor(filterJql: null, subFilterJql: null);

            Assert.That(query, Is.Empty);
        }

        private static Task<string> BoardQueryFor(
            string? filterJql,
            string? subFilterJql,
            HttpStatusCode filterStatus = HttpStatusCode.OK)
            => BoardQueryWhereTheFilterAnswers(
                filterJql is null ? null : FilterId,
                subFilterJql,
                filterStatus,
                $"{{\"jql\":{JsonSerializer.Serialize(filterJql ?? string.Empty)}}}");

        private static async Task<string> BoardQueryWhereTheFilterAnswers(
            string? filterId,
            string? subFilterJql,
            HttpStatusCode filterStatus,
            string filterPayload)
        {
            var boardConfiguration = BuildBoardConfiguration(filterId, subFilterJql);
            var handler = CreateHandler(boardConfiguration, filterPayload, filterStatus);

            var connector = JiraConnectorTestSetup.AConnectorOver(handler);
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            var boardInformation = await connector.GetBoardInformation(team.WorkTrackingSystemConnection, BoardId);

            return boardInformation.DataRetrievalValue;
        }

        private static string BuildBoardConfiguration(string? filterId, string? subFilterJql)
        {
            var filterPart = filterId is null
                ? string.Empty
                : $",\"filter\":{{\"id\":{JsonSerializer.Serialize(filterId)}}}";

            var subQueryPart = subFilterJql is null
                ? string.Empty
                : $",\"subQuery\":{{\"query\":{JsonSerializer.Serialize(subFilterJql)}}}";

            return $"{{\"columnConfig\":{{\"columns\":[]}}{filterPart}{subQueryPart}}}";
        }

        private static HttpMessageHandler CreateHandler(string boardConfiguration, string filterPayload, HttpStatusCode filterStatus)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (request, _) => Task.FromResult(BuildResponse(request, boardConfiguration, filterPayload, filterStatus)));

            return mock.Object;
        }

        private static HttpResponseMessage BuildResponse(
            HttpRequestMessage request, string boardConfiguration, string filterPayload, HttpStatusCode filterStatus)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith($"rest/api/2/filter/{FilterId}", StringComparison.Ordinal))
            {
                return Respond(filterStatus, filterPayload);
            }

            var body = path switch
            {
                _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal) => "{\"deploymentType\":\"Server\"}",
                _ when path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal) => boardConfiguration,
                _ when path.EndsWith($"board/{BoardId}/issue", StringComparison.Ordinal) => "{\"issues\":[]}",
                _ when path.EndsWith("status", StringComparison.Ordinal) => "[]",
                _ when path.EndsWith("field", StringComparison.Ordinal) => "[]",
                _ => "{}",
            };

            return Respond(HttpStatusCode.OK, body);
        }

        [Test]
        public async Task GetBoardInformation_EveryPartReadable_ReportsTheBoardsQueryTypesAndStates()
        {
            var boardInformation = await BoardReadOver(AReadableBoardExcept(refusedPath: null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(boardInformation.DataRetrievalValue, Is.EqualTo("(project = PROJ) AND (fixVersion is EMPTY)"));
                Assert.That(string.Join(",", boardInformation.WorkItemTypes), Is.EqualTo("Story,Bug"));
                Assert.That(string.Join(",", boardInformation.ToDoStates), Is.EqualTo("To Do"));
                Assert.That(string.Join(",", boardInformation.DoingStates), Is.EqualTo("In Progress"));
                Assert.That(string.Join(",", boardInformation.DoneStates), Is.EqualTo("Done"));
            }
        }

        [TestCase($"board/{BoardId}/configuration")]
        [TestCase($"board/{BoardId}/issue")]
        [TestCase("rest/api/latest/status")]
        public async Task GetBoardInformation_PartOfTheBoardCannotBeRead_LogsWhatJiraAnsweredBeforeCarryingOn(string refusedPath)
        {
            var logger = new Mock<ILogger<JiraWorkTrackingConnector>>();

            await BoardReadOver(AReadableBoardExcept(refusedPath), logger.Object);

            logger.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => NamesTheStatusAndTheReason(state)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        private static bool NamesTheStatusAndTheReason(object? loggedState)
        {
            var line = loggedState?.ToString() ?? string.Empty;

            return line.Contains("403", StringComparison.Ordinal)
                && line.Contains("Forbidden", StringComparison.Ordinal);
        }

        private static Task<BoardInformation> BoardReadOver(HttpMessageHandler handler)
            => BoardReadOver(handler, Mock.Of<ILogger<JiraWorkTrackingConnector>>());

        private static async Task<BoardInformation> BoardReadOver(
            HttpMessageHandler handler, ILogger<JiraWorkTrackingConnector> logger)
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(handler, logger);
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            return await connector.GetBoardInformation(team.WorkTrackingSystemConnection, BoardId);
        }

        /// <summary>
        /// A board every endpoint answers for, save one. Refusing a single endpoint is how each of the reads
        /// that used to degrade in silence gets exercised without disturbing the others.
        /// </summary>
        private static HttpMessageHandler AReadableBoardExcept(string? refusedPath)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                    if (refusedPath is not null && path.Contains(refusedPath, StringComparison.Ordinal))
                    {
                        return Task.FromResult(Respond(HttpStatusCode.Forbidden, "{}"));
                    }

                    var body = path switch
                    {
                        _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal) => "{\"deploymentType\":\"Server\"}",
                        _ when path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal) => ReadableBoardConfiguration,
                        _ when path.EndsWith($"rest/api/2/filter/{FilterId}", StringComparison.Ordinal) => "{\"jql\":\"project = PROJ ORDER BY Rank ASC\"}",
                        _ when path.EndsWith($"board/{BoardId}/issue", StringComparison.Ordinal) => BoardIssues,
                        _ when path.EndsWith("rest/api/latest/status", StringComparison.Ordinal) => InstanceStatuses,
                        _ => "{}",
                    };

                    return Task.FromResult(Respond(HttpStatusCode.OK, body));
                });

            return mock.Object;
        }

        [Test]
        public async Task ValidateTeamSettings_DataCenterRejectsTheQuery_ReportsTheSentenceJiraAnsweredWith()
        {
            var result = await TeamValidationWhereSearchAnswers("Server", HttpStatusCode.BadRequest, RejectedQueryBody);

            Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
        }

        [Test]
        public async Task ValidateTeamSettings_CloudRejectsTheQuery_ReportsTheSentenceJiraAnsweredWith()
        {
            var result = await TeamValidationWhereSearchAnswers("Cloud", HttpStatusCode.BadRequest, RejectedQueryBody);

            Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
        }

        [Test]
        public async Task ValidateTeamSettings_QueryRejected_DoesNotCallItAnUnexpectedError()
        {
            var result = await TeamValidationWhereSearchAnswers("Server", HttpStatusCode.BadRequest, RejectedQueryBody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("query_rejected"));
                Assert.That(result.Message, Does.Not.Contain("unexpected"));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_QueryRejected_LogsTheQueryAndWhatJiraAnsweredAtWarning()
        {
            var logger = new Mock<ILogger<JiraWorkTrackingConnector>>();

            await TeamValidationWhereSearchAnswers("Server", HttpStatusCode.BadRequest, RejectedQueryBody, logger.Object);

            logger.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => NamesBothTheQueryAndWhatJiraAnswered(state)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task ValidateTeamSettings_JiraCouldNotBeReached_KeepsTheUnexpectedErrorWording()
        {
            var handler = AHandlerWhereSearch("Server", _ => throw new HttpRequestException("No such host is known."));
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var result = await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("validation_failed"));
                Assert.That(result.Message, Is.EqualTo("Team validation failed due to an unexpected error."));
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_QueryRejected_ReportsTheSentenceJiraAnsweredWith()
        {
            var handler = ASearchThatAnswers("Server", HttpStatusCode.BadRequest, RejectedQueryBody);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var result = await connector.ValidatePortfolioSettings(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("query_rejected"));
                Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_JiraAnswersWithWorkItems_StillReportsTheUnchangedSuccess()
        {
            var result = await TeamValidationWhereSearchAnswers("Server", HttpStatusCode.OK, OnePageHoldingOneIssue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
                Assert.That(result.Message, Is.EqualTo("Connection validated successfully."));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_JiraAnswersWithNothing_StillReportsNoWorkItemsFound()
        {
            var result = await TeamValidationWhereSearchAnswers("Server", HttpStatusCode.OK, OnePageHoldingNothing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("no_work_items_found"));
                Assert.That(result.Message, Is.EqualTo("No work items were found for this team configuration."));
            }
        }

        private static bool NamesBothTheQueryAndWhatJiraAnswered(object? loggedState)
        {
            var line = loggedState?.ToString() ?? string.Empty;

            return line.Contains(JiraRejectionOpening, StringComparison.Ordinal)
                && line.Contains(TeamQuery, StringComparison.Ordinal);
        }

        private static Task<ConnectionValidationResult> TeamValidationWhereSearchAnswers(
            string deploymentType, HttpStatusCode searchStatus, string searchBody)
            => TeamValidationWhereSearchAnswers(
                deploymentType, searchStatus, searchBody, Mock.Of<ILogger<JiraWorkTrackingConnector>>());

        private static async Task<ConnectionValidationResult> TeamValidationWhereSearchAnswers(
            string deploymentType, HttpStatusCode searchStatus, string searchBody, ILogger<JiraWorkTrackingConnector> logger)
        {
            var handler = ASearchThatAnswers(deploymentType, searchStatus, searchBody);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler, logger);

            return await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());
        }

        private static HttpMessageHandler ASearchThatAnswers(string deploymentType, HttpStatusCode searchStatus, string searchBody)
            => AHandlerWhereSearch(deploymentType, _ => Respond(searchStatus, searchBody));

        /// <summary>
        /// Both deployments answer the search the same way here, so a test reads the same reported text whether
        /// the Cloud walk carries the rejection out itself or falls back to the Data Center endpoint first.
        /// </summary>
        private static HttpMessageHandler AHandlerWhereSearch(
            string deploymentType, Func<HttpRequestMessage, HttpResponseMessage> answerSearch)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                    if (path.Contains("/search", StringComparison.Ordinal))
                    {
                        return Task.FromResult(answerSearch(request));
                    }

                    var body = path switch
                    {
                        _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                            => $"{{\"deploymentType\":\"{deploymentType}\"}}",
                        _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal) => "[]",
                        _ => "{}",
                    };

                    return Task.FromResult(Respond(HttpStatusCode.OK, body));
                });

            return mock.Object;
        }

        private static HttpResponseMessage Respond(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
