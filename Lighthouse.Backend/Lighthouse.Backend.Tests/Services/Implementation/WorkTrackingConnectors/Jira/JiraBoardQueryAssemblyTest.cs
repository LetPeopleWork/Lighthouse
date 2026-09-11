using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models.Validation;
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

        [Test]
        public async Task GetBoardInformation_FilterUnreadable_DoesNotProduceAQueryStartingWithAnd()
        {
            var query = await BoardQueryFor("project = FOO", "fixVersion is EMPTY", HttpStatusCode.Forbidden);

            Assert.That(query, Is.EqualTo("(fixVersion is EMPTY)"));
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

        private static async Task<string> BoardQueryFor(
            string? filterJql,
            string? subFilterJql,
            HttpStatusCode filterStatus = HttpStatusCode.OK)
        {
            var boardConfiguration = BuildBoardConfiguration(filterJql is null ? null : FilterId, subFilterJql);
            var handler = CreateHandler(boardConfiguration, filterJql, filterStatus);

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

        private static HttpMessageHandler CreateHandler(string boardConfiguration, string? filterJql, HttpStatusCode filterStatus)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (request, _) => Task.FromResult(BuildResponse(request, boardConfiguration, filterJql, filterStatus)));

            return mock.Object;
        }

        private static HttpResponseMessage BuildResponse(
            HttpRequestMessage request, string boardConfiguration, string? filterJql, HttpStatusCode filterStatus)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith($"rest/api/2/filter/{FilterId}", StringComparison.Ordinal))
            {
                return Respond(filterStatus, $"{{\"jql\":{JsonSerializer.Serialize(filterJql ?? string.Empty)}}}");
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
