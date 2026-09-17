using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
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

        private const string ARefusalHereStopsAWorkingTeam =
            "One of the three things a configuration can narrow on is enough to ask a question with. A "
            + "guard that refuses this stops an installation that was refreshing fine yesterday.";

        private const string AFilterQuery = "project = FOO";

        private const string ASubFilterQuery = "fixVersion is EMPTY";

        /// <summary>What serverInfo answers for a Data Center instance - Jira still calls it "Server" there.</summary>
        private const string OnDataCenter = "Server";

        private const string OnCloud = "Cloud";

        private const string ServerInfoPath = "rest/api/2/serverInfo";

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

        /// <summary>
        /// What Jira Cloud answers on the old search endpoint, to every call, whether the query is valid or
        /// not. It is the sentence a Cloud user saw instead of the one naming what was actually wrong.
        /// </summary>
        private const string RemovedEndpointSentence =
            "The requested API has been removed. Please migrate to the /rest/api/3/search/jql API.";

        private static readonly string RemovedEndpointBody =
            $"{{\"errorMessages\":[{JsonSerializer.Serialize(RemovedEndpointSentence)}],\"errors\":{{}}}}";

        /// <summary>A refusal with no errorMessages list in it at all - nothing here names a field to correct.</summary>
        private const string ARefusalWithoutErrorMessages = "{\"warningMessages\":[]}";

        private const string ARefusalWhereErrorMessagesIsNotAList = "{\"errorMessages\":\"Expecting a field name\"}";

        /// <summary>
        /// The shape Jira answers with most often when it objects to a named field rather than to the query
        /// as a whole: the list is there but empty, and everything it has to say is under errors instead.
        /// </summary>
        private const string ARefusalWithAnEmptyErrorMessagesList = "{\"errorMessages\":[],\"errors\":{}}";

        private const string ARefusalWhereEveryMessageIsBlank = "{\"errorMessages\":[\"\",\"   \"],\"errors\":{}}";

        /// <summary>
        /// Valid JSON that is not an object, which a gateway can answer with. There are no properties on it
        /// to ask after, and asking anyway throws rather than answering.
        /// </summary>
        private const string ARefusalThatIsJsonButNotAnObject = "[\"Bad Request\"]";

        private const string AnUnknownProjectSentence =
            "A value with ID '10042' does not exist for the field 'project'.";

        private static readonly string ARefusalNamingTheFieldItRefused =
            $"{{\"errorMessages\":[],\"errors\":{{\"project\":{JsonSerializer.Serialize(AnUnknownProjectSentence)}}}}}";

        private static readonly string ARefusalSayingSomethingInBothHalves =
            $"{{\"errorMessages\":[{JsonSerializer.Serialize(JiraRejectionSentence)}],"
            + $"\"errors\":{{\"project\":{JsonSerializer.Serialize(AnUnknownProjectSentence)}}}}}";

        /// <summary>
        /// Something other than the keyed object Jira answers with under errors - which is how an older
        /// endpoint or a gateway writing its own envelope can fill that name in.
        /// </summary>
        private const string ARefusalWhereErrorsIsNotAnObject =
            "{\"errorMessages\":[],\"errors\":\"Something went wrong\"}";

        /// <summary>What a proxy or an application server standing in front of Jira answers: not JSON at all.</summary>
        private const string ARefusalThatIsNotJson =
            "<html><head><title>400 Bad Request</title></head><body><h1>Bad Request</h1></body></html>";

        private const string AMissingFieldSentence = "Field 'foo' does not exist or you do not have permission to view it.";

        private const string ASecondMissingFieldSentence = "Field 'bar' does not exist or you do not have permission to view it.";

        private static readonly string ARefusalNamingTwoFields =
            "{\"errorMessages\":["
            + $"{JsonSerializer.Serialize(AMissingFieldSentence)},{JsonSerializer.Serialize(ASecondMissingFieldSentence)}"
            + "],\"errors\":{}}";

        /// <summary>How much of a refused query Lighthouse repeats back before cutting it short.</summary>
        private const int TheLongestQueryReported = 500;

        private const string CloudSearchPath = "rest/api/3/search/jql";

        private const string LegacySearchPath = "rest/api/latest/search";

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
                async () => await BoardQueryFor(AFilterQuery, ASubFilterQuery, filterStatus));

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
                    FilterId, ASubFilterQuery, HttpStatusCode.OK, "{\"name\":\"Board filter\"}"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Message, Does.Contain("permission"));
                Assert.That(refusal.Verdict.Message, Does.Contain("account"));
            }
        }

        /// <summary>
        /// The prose is only half of what a refusal has to carry. The wizard branches on the code, puts the
        /// error next to the input it names, and a support bundle is read for the request that actually
        /// failed - and none of those three can be recovered from the sentence the user sees.
        /// </summary>
        [Test]
        public void GetBoardInformation_FilterCannotBeRead_CarriesTheCodeTheFailedRequestAndTheFieldToCorrect()
        {
            var refusal = Assert.ThrowsAsync<JiraReadException>(
                async () => await BoardQueryFor(AFilterQuery, ASubFilterQuery, HttpStatusCode.Forbidden));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Code, Is.EqualTo("board_filter_unreadable"));
                Assert.That(refusal.Verdict.TechnicalDetails, Is.EqualTo("GET rest/api/2/filter/10001 answered 403 Forbidden."));
                Assert.That(refusal.Verdict.FieldName, Is.EqualTo("DataRetrievalValue"));
                Assert.That(refusal.Verdict.Message, Does.Contain("share filter 10001"));
            }
        }

        /// <summary>
        /// A filter Jira handed over without a query in it fails for a different reason than one it would not
        /// hand over, and the administrator has to be able to tell them apart - the permissions advice that
        /// both messages carry is wasted effort on the first of them.
        /// </summary>
        [Test]
        public void GetBoardInformation_FilterCarriesNoQuery_SaysTheFilterCameBackWithoutOne()
        {
            var refusal = Assert.ThrowsAsync<JiraReadException>(
                async () => await BoardQueryWhereTheFilterAnswers(
                    FilterId, ASubFilterQuery, HttpStatusCode.OK, "{\"name\":\"Board filter\"}"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Code, Is.EqualTo("board_filter_unreadable"));
                Assert.That(refusal.Verdict.Message, Does.Contain("the filter it returned holds no query"));
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
            var query = await BoardQueryFor("project = LIGHTHOUSE AND type IN (Bug, Story)", ASubFilterQuery);

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
            var query = await BoardQueryFor(AFilterQuery, "resolution is EMPTY ORDER BY Rank");

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

        /// <summary>
        /// A filter carrying an ordering inside brackets and its own ordering at the end is the case that
        /// tells a bracket count that works from one that does not: only a count returning to zero reaches
        /// the second ordering, and only the second one may be cut. A query left whole - which is what every
        /// broken count produces - is also the right answer to the bracketed ordering on its own, so reading
        /// that case alone cannot say whether the counting happened.
        /// </summary>
        [Test]
        public async Task GetBoardInformation_OrderingSitsInsideBracketsAndAtTheEnd_StripsOnlyTheOneAtTheEnd()
        {
            var query = await BoardQueryFor("project = FOO AND (status = Open ORDER BY Rank) ORDER BY Rank ASC", subFilterJql: null);

            Assert.That(query, Is.EqualTo("(project = FOO AND (status = Open ORDER BY Rank))"));
        }

        [Test]
        public async Task GetBoardInformation_OrderingSitsTwoBracketsDeep_StillStripsTheOneAtTheEnd()
        {
            var query = await BoardQueryFor(
                "project = FOO AND ((status = Open ORDER BY Rank) OR labels is EMPTY) ORDER BY created DESC",
                subFilterJql: null);

            Assert.That(query, Is.EqualTo("(project = FOO AND ((status = Open ORDER BY Rank) OR labels is EMPTY))"));
        }

        [Test]
        public async Task GetBoardInformation_FilterWithoutSubFilter_BracketsTheFilterAlone()
        {
            var query = await BoardQueryFor(AFilterQuery, subFilterJql: null);

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
            => AHandlerAnswering(request => BuildResponse(request, boardConfiguration, filterPayload, filterStatus));

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
                _ when path.EndsWith(ServerInfoPath, StringComparison.Ordinal) => "{\"deploymentType\":\"Server\"}",
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
            => AHandlerAnswering(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (refusedPath is not null && path.Contains(refusedPath, StringComparison.Ordinal))
                {
                    return Respond(HttpStatusCode.Forbidden, "{}");
                }

                var body = path switch
                {
                    _ when path.EndsWith(ServerInfoPath, StringComparison.Ordinal) => "{\"deploymentType\":\"Server\"}",
                    _ when path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal) => ReadableBoardConfiguration,
                    _ when path.EndsWith($"rest/api/2/filter/{FilterId}", StringComparison.Ordinal) => "{\"jql\":\"project = PROJ ORDER BY Rank ASC\"}",
                    _ when path.EndsWith($"board/{BoardId}/issue", StringComparison.Ordinal) => BoardIssues,
                    _ when path.EndsWith("rest/api/latest/status", StringComparison.Ordinal) => InstanceStatuses,
                    _ => "{}",
                };

                return Respond(HttpStatusCode.OK, body);
            });

        [Test]
        public async Task ValidateTeamSettings_DataCenterRejectsTheQuery_ReportsTheSentenceJiraAnsweredWith()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody);

            Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
        }

        [Test]
        public async Task ValidateTeamSettings_CloudRejectsTheQuery_ReportsTheSentenceJiraAnsweredWith()
        {
            var result = await TeamValidationWhereSearchAnswers(OnCloud, HttpStatusCode.BadRequest, RejectedQueryBody);

            Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
        }

        [Test]
        public async Task ValidateTeamSettings_QueryRejected_DoesNotCallItAnUnexpectedError()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody);

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

            await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody, logger.Object);

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
            var handler = AHandlerWhereSearch(OnDataCenter, _ => throw new HttpRequestException("No such host is known."));
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
            var handler = ASearchThatAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var result = await connector.ValidatePortfolioSettings(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("query_rejected"));
                Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
            }
        }

        /// <summary>
        /// Not every refusal is a complaint about the query. Jira can answer without the errorMessages list,
        /// with something other than a list under that name, with the list present but holding nothing or
        /// nothing but blanks, or - when a proxy in front of Jira turns the request away - with something
        /// that is not a JSON object at all. Whatever refused in that last case never read the query, so it
        /// has nothing to say about it, and a bare status leaves the reader with nothing to act on. The query
        /// Lighthouse sent is then the whole of what is still known about the failure, and reporting an empty
        /// explanation instead would leave the reader with less than the status alone gave them.
        /// </summary>
        [TestCase(ARefusalWithoutErrorMessages)]
        [TestCase(ARefusalWhereErrorMessagesIsNotAList)]
        [TestCase(ARefusalWithAnEmptyErrorMessagesList)]
        [TestCase(ARefusalWhereEveryMessageIsBlank)]
        [TestCase(ARefusalWhereErrorsIsNotAnObject)]
        [TestCase(ARefusalThatIsJsonButNotAnObject)]
        [TestCase(ARefusalThatIsNotJson)]
        public async Task ValidateTeamSettings_RefusalNamesNothingToCorrect_ReportsTheQueryLighthouseSent(string refusalBody)
        {
            var (result, jql) = await TheRefusalOutcomeFor(refusalBody, JiraConnectorTestSetup.ATeamOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("query_rejected"));
                Assert.That(jql, Is.Not.Empty, "With no query captured, the expectation below would assert nothing.");
                Assert.That(result.TechnicalDetails, Is.EqualTo(
                    $"Jira answered 400 BadRequest without saying why. Lighthouse asked: {jql}"));
            }
        }

        /// <summary>
        /// A sentence from Jira already names what to change. Anything appended to it competes with that for
        /// the reader's attention while saying nothing Jira has not said better.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_JiraSaidWhatWasWrong_AddsNothingOfItsOwn()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody);

            Assert.That(result.TechnicalDetails, Is.EqualTo(JiraRejectionSentence));
        }

        /// <summary>
        /// A configuration narrowing on hundreds of projects assembles a query longer than any log line,
        /// panel row or validation message can show. Repeating all of it buries the status it explains.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_TheRefusedQueryIsEnormous_CutsItToWhatALogLineCanCarry()
        {
            var (result, jql) = await TheRefusalOutcomeFor(ARefusalThatIsNotJson, ATeamNarrowingOnHundredsOfProjects());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Has.Length.GreaterThan(TheLongestQueryReported));
                Assert.That(result.TechnicalDetails, Is.EqualTo(
                    "Jira answered 400 BadRequest without saying why. Lighthouse asked: "
                    + jql[..TheLongestQueryReported] + "…"));
            }
        }

        /// <summary>
        /// Jira names one problem per sentence and can name several at once. Running them together would
        /// hand the user a sentence Jira never wrote, and dropping all but one hides work still to do.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_JiraNamesTwoProblems_KeepsBothSentencesApart()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, ARefusalNamingTwoFields);

            Assert.That(result.TechnicalDetails, Is.EqualTo(
                "Field 'foo' does not exist or you do not have permission to view it."
                + " Field 'bar' does not exist or you do not have permission to view it."));
        }

        /// <summary>
        /// When Jira objects to one named field rather than to the query as a whole, it answers with an empty
        /// errorMessages list and puts the sentence under errors, keyed by the field. Reading only the list
        /// would report "Jira answered 400 without saying why" about a refusal where Jira said exactly why -
        /// and a project or a status that does not exist on the instance is the commonest way to get here.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_JiraNamedTheFieldItRefused_ReportsWhatJiraSaidAboutIt()
        {
            var result = await TeamValidationWhereSearchAnswers(
                OnDataCenter, HttpStatusCode.BadRequest, ARefusalNamingTheFieldItRefused);

            Assert.That(result.TechnicalDetails, Is.EqualTo(AnUnknownProjectSentence));
        }

        /// <summary>
        /// Jira can fill in both halves at once, and they are not two separate problems: the sentence about
        /// the request as a whole is the complete one, and the entries keyed by field are the same complaint
        /// broken up. Leading with the per-field note would hand the reader the narrower half of an answer
        /// whose wider half was right there.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_JiraSaidSomethingInBothHalves_LeadsWithWhatItSaidAboutTheQuery()
        {
            var result = await TeamValidationWhereSearchAnswers(
                OnDataCenter, HttpStatusCode.BadRequest, ARefusalSayingSomethingInBothHalves);

            Assert.That(result.TechnicalDetails, Is.EqualTo(JiraRejectionSentence));
        }

        /// <summary>
        /// The headline is what the user reads first and the field name is where the wizard puts it. Jira's
        /// own sentence goes in the detail underneath, and on its own it says nothing about what to change.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_QueryRejected_BlamesTheQueryAndNamesTheFieldToCorrect()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.BadRequest, RejectedQueryBody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Message, Is.EqualTo("Jira could not run this query."));
                Assert.That(result.FieldName, Is.EqualTo("DataRetrievalValue"));
            }
        }

        /// <summary>
        /// Every mapped name becomes its own comparison, and the clause only asks for "any of these" while
        /// the OR between them survives. Finding one comparison somewhere in the query cannot tell that
        /// apart from a clause whose parts have run together into something Jira refuses outright.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_SeveralStatesMapped_AsksForEachOfThemWithOrBetween()
        {
            var jql = await TheQueryIssuedFor(JiraConnectorTestSetup.ATeamOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Contain("AND (status = \"To Do\" OR status = \"In Progress\" OR status = \"Done\")"));
                Assert.That(jql, Does.Contain("AND (issuetype = \"Story\")"));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_JiraAnswersWithWorkItems_StillReportsTheUnchangedSuccess()
        {
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.OK, OnePageHoldingOneIssue);

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
            var result = await TeamValidationWhereSearchAnswers(OnDataCenter, HttpStatusCode.OK, OnePageHoldingNothing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("no_work_items_found"));
                Assert.That(result.Message, Is.EqualTo("No work items were found for this team configuration."));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_CloudRejectsTheQuery_NeverAsksTheEndpointCloudNoLongerHas()
        {
            var requestedUrls = new List<string>();
            var connector = JiraConnectorTestSetup.AConnectorOver(ACloudSearchThatRefuses(requestedUrls));

            await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());

            Assert.That(UrlsReaching(requestedUrls, LegacySearchPath), Is.Empty);
        }

        [Test]
        public async Task ValidateTeamSettings_CloudRejectsTheQuery_ReportsWhatTheCloudSearchAnswered()
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(ACloudSearchThatRefuses([]));

            var result = await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TechnicalDetails, Does.Contain(JiraRejectionSentence));
                Assert.That(result.TechnicalDetails, Does.Not.Contain(RemovedEndpointSentence));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_OnDataCenter_StillWalksTheLegacySearchByOffset()
        {
            var requestedUrls = new List<string>();
            var searchBodies = new List<string>();
            var handler = AHandlerWhereSearch(
                OnDataCenter,
                request =>
                {
                    searchBodies.Add(JiraConnectorTestSetup.BodyOf(request));
                    return Respond(HttpStatusCode.OK, OnePageHoldingOneIssue);
                },
                requestedUrls);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var result = await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());

            var searches = UrlsReaching(requestedUrls, LegacySearchPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(searches, Is.Not.Empty);
                Assert.That(searchBodies[0], Does.Contain("\"startAt\":0"),
                    "The offset walk starts where Data Center numbers its answers from, and it now travels in "
                    + "the request body rather than in the request line.");
                Assert.That(UrlsReaching(requestedUrls, CloudSearchPath), Is.Empty);
            }
        }

        [Test]
        public async Task ValidateTeamSettings_AStateNameCarriesADoubleQuote_LeavesTheQuoteEscaped()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.DoneStates.Clear();
            team.DoneStates.Add("Say \"Done\"");

            var jql = await TheQueryIssuedFor(team);

            Assert.That(jql, Does.Contain("status = \"Say \\\"Done\\\"\""));
        }

        [Test]
        public async Task ValidateTeamSettings_AWorkItemTypeCarriesABackslash_LeavesTheBackslashEscaped()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story\\Task");

            var jql = await TheQueryIssuedFor(team);

            Assert.That(jql, Does.Contain("issuetype = \"Story\\\\Task\""));
        }

        /// <summary>
        /// A team can reach the settings API with no states mapped, and the clause built from them is then
        /// built from nothing. Emitting the bracket pair anyway writes an expression Jira cannot parse, and
        /// a team whose refresh fails outright records nothing at all. Asking for the absent bracket pair,
        /// rather than for the clause that replaces it, is what keeps this from passing on a query that
        /// happens to contain the right substring somewhere else.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_NoStatesMapped_LeavesTheStateClauseOutRatherThanEmpty()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();

            var jql = await TheQueryIssuedFor(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Not.Contain("()"));
                Assert.That(jql, Does.Contain("AND (issuetype = \"Story\")"),
                    "positive control: a change that dropped every clause would satisfy the assertion above "
                    + "while asking Jira for the whole instance.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_NoWorkItemTypesMapped_LeavesTheTypeClauseOutRatherThanEmpty()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.WorkItemTypes.Clear();

            var jql = await TheQueryIssuedFor(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Not.Contain("()"));
                Assert.That(jql, Does.Contain("AND (status = \"To Do\" OR status = \"In Progress\" OR status = \"Done\")"),
                    "positive control: a change that dropped every clause would satisfy the assertion above "
                    + "while asking Jira for the whole instance.");
            }
        }

        /// <summary>
        /// Nothing configured leaves nothing to select on, and the query then says nothing at all. Jira reads
        /// an empty query as either every issue on the instance or none of them, depending on a setting no
        /// one here can see - and "none" is indistinguishable from a team whose work has all been deleted, so
        /// the next refresh deletes it for real.
        ///
        /// A query typed as nothing but spaces is that same configuration with something in the box. It
        /// narrows no more than an empty one does, so it has to be refused rather than sent.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_RefusesRatherThanAskingJiraForEverything(string queryOfItsOwn)
        {
            var team = ATeamThatNarrowsOnNothing();
            team.DataRetrievalValue = queryOfItsOwn;

            var requestedUrls = new List<string>();
            var handler = AHandlerWhereSearch(OnCloud, _ => Respond(HttpStatusCode.OK, OnePageHoldingOneIssue), requestedUrls);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var verdict = await connector.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(UrlsReaching(requestedUrls, CloudSearchPath), Is.Empty,
                    "A query that selects on nothing must not be issued at all - whatever Jira answers it "
                    + "with is a number of items the team's configuration never asked for.");
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Message, Does.Contain("work item type").IgnoreCase.And.Contains("state").IgnoreCase,
                    "The refusal has to name what is missing, or it reads as a broken connection and the "
                    + "administrator goes looking at the wrong screen.");
                Assert.That(verdict.FieldName, Is.Not.Null.And.Not.Empty,
                    "The settings screen highlights the input the verdict names; without one the message is "
                    + "shown against nothing.");
            }
        }

        /// <summary>
        /// The refresh path has no verdict to return, so the refusal has to leave as an exception. Answering
        /// a refresh with an empty result would be read as "the team has no work left" and remove every
        /// record it has.
        /// </summary>
        [Test]
        public void GetWorkItemsForTeam_NothingIsConfiguredAtAll_RefusesRatherThanFetchingNothing()
        {
            var team = ATeamThatNarrowsOnNothing();

            var handler = AHandlerWhereSearch(OnCloud, _ => Respond(HttpStatusCode.OK, OnePageHoldingNothing));
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            Assert.That(async () => await connector.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.InstanceOf<JiraReadException>());
        }

        /// <summary>
        /// The refusal fires only when all three of the things a configuration can narrow on are missing.
        /// One of them on its own is an ordinary, valid team - a team scoped by a JQL query of its own and
        /// nothing else, or one scoped by its types alone - and refusing any of those stops a working
        /// installation from refreshing at all. These three say where the boundary is, which a test that
        /// only ever configures nothing cannot: every such test passes just as well against a guard that
        /// refuses whenever any one thing is missing.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_OnlyAQueryOfItsOwn_AsksItRatherThanRefusing()
        {
            var team = ATeamThatNarrowsOnNothing();
            team.DataRetrievalValue = TeamQuery;

            var (verdict, jql) = await TheOutcomeFor(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(jql, Does.StartWith($"({TeamQuery})"),
                    "positive control: the operator's query has to be what the team is scoped by, or the "
                    + "refusal was avoided by asking Jira for the whole instance instead.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_OnlyWorkItemTypesMapped_AsksForThemRatherThanRefusing()
        {
            var team = ATeamThatNarrowsOnNothing();
            team.WorkItemTypes.Add("Story");

            var (verdict, jql) = await TheOutcomeFor(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(jql, Does.StartWith("(issuetype = \"Story\")"),
                    "positive control: the type clause has to survive becoming the only one.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_OnlyStatesMapped_AsksForThemRatherThanRefusing()
        {
            var team = ATeamThatNarrowsOnNothing();
            team.DoingStates.Add("In Progress");

            var (verdict, jql) = await TheOutcomeFor(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(jql, Does.StartWith("(status = \"In Progress\")"),
                    "positive control: the state clause has to survive becoming the only one.");
            }
        }

        /// <summary>
        /// The refusal is the whole of what an administrator gets: no request was made, so there is no
        /// tracker answer to fall back on. Each phrase below is the only place its sentence says what it
        /// says, so emptying any one of them fails here on its own rather than being covered by the rest.
        /// </summary>
        [TestCase("selects no work item types")]
        [TestCase("nothing for Lighthouse to ask Jira for")]
        [TestCase("depending on how the instance is set up")]
        [TestCase("neither is what was configured")]
        [TestCase("write a JQL query")]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_SaysWhatIsMissingAndWhatAnEmptyQueryWouldDo(string phrase)
        {
            var (verdict, _) = await TheOutcomeFor(ATeamThatNarrowsOnNothing());

            Assert.That(verdict.Message, Does.Contain(phrase));
        }

        /// <summary>
        /// The code is what the settings screen matches on to decide which input to highlight, and the
        /// technical detail is what an operator quotes into a support conversation. Neither is in the
        /// sentence the user reads, so neither is pinned by the phrases above.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_NamesTheRefusalAndSaysNoRequestWasMade()
        {
            var (verdict, _) = await TheOutcomeFor(ATeamThatNarrowsOnNothing());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("nothing_to_query"));
                Assert.That(verdict.TechnicalDetails, Does.Contain("assembled JQL was empty"));
            }
        }

        /// <summary>
        /// A team the settings API will happily save and which narrows nothing at all: no types, no states,
        /// no query of its own.
        /// </summary>
        private static Team ATeamThatNarrowsOnNothing()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.WorkItemTypes.Clear();
            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();
            team.DataRetrievalValue = string.Empty;

            return team;
        }

        /// <summary>
        /// The verdict the settings screen was given, and the JQL that reached Jira - empty when the
        /// configuration was refused, because a refusal issues no request at all.
        /// </summary>
        private static async Task<(ConnectionValidationResult Verdict, string Jql)> TheOutcomeFor(Team team)
        {
            var requestedUrls = new List<string>();
            var handler = AHandlerWhereSearch(OnCloud, _ => Respond(HttpStatusCode.OK, OnePageHoldingOneIssue), requestedUrls);
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var verdict = await connector.ValidateTeamSettings(team);
            var searches = UrlsReaching(requestedUrls, CloudSearchPath);

            return (verdict, searches.Count == 0 ? string.Empty : JqlOf(searches[0]));
        }

        /// <summary>
        /// A Cloud instance as it actually answers once the old search endpoint is gone: the endpoint Jira
        /// still has says what is wrong with the query, and the one it removed says only that it is removed.
        /// </summary>
        private static HttpMessageHandler ACloudSearchThatRefuses(ICollection<string> requestedUrls)
            => AHandlerWhereSearch(
                OnCloud,
                request => (request.RequestUri?.AbsolutePath ?? string.Empty).Contains(CloudSearchPath, StringComparison.Ordinal)
                    ? Respond(HttpStatusCode.BadRequest, RejectedQueryBody)
                    : Respond(HttpStatusCode.Gone, RemovedEndpointBody),
                requestedUrls);

        private static List<string> UrlsReaching(IEnumerable<string> requestedUrls, string path)
            => requestedUrls.Where(url => url.Contains(path, StringComparison.Ordinal)).ToList();

        /// <summary>The JQL the connector put on the wire, read back out of the search url it asked for.</summary>
        private static async Task<string> TheQueryIssuedFor(Team team)
            => (await TheOutcomeFor(team)).Jql;

        private static string JqlOf(string searchUrl)
        {
            var jqlParameter = searchUrl[(searchUrl.IndexOf('?', StringComparison.Ordinal) + 1)..]
                .Split('&')
                .First(parameter => parameter.StartsWith("jql=", StringComparison.Ordinal));

            return Uri.UnescapeDataString(jqlParameter["jql=".Length..]);
        }

        private static bool NamesBothTheQueryAndWhatJiraAnswered(object? loggedState)
        {
            var line = loggedState?.ToString() ?? string.Empty;

            return line.Contains(JiraRejectionOpening, StringComparison.Ordinal)
                && line.Contains(TeamQuery, StringComparison.Ordinal);
        }

        /// <summary>
        /// The verdict the settings screen was given, paired with the query that actually reached Jira - read
        /// back off the request body rather than restated, so an expectation naming the query cannot pass
        /// against a message that merely repeats some fragment the query happens to share.
        /// </summary>
        private static async Task<(ConnectionValidationResult Verdict, string Jql)> TheRefusalOutcomeFor(
            string refusalBody, Team team)
        {
            var queriesSent = new List<string>();
            var handler = AHandlerWhereSearch(
                OnDataCenter,
                request =>
                {
                    queriesSent.Add(JqlSentIn(request));
                    return Respond(HttpStatusCode.BadRequest, refusalBody);
                });
            var connector = JiraConnectorTestSetup.AConnectorOver(handler);

            var verdict = await connector.ValidateTeamSettings(team);

            return (verdict, queriesSent.Count == 0 ? string.Empty : queriesSent[0]);
        }

        private static string JqlSentIn(HttpRequestMessage request)
        {
            using var json = JsonDocument.Parse(JiraConnectorTestSetup.BodyOf(request));

            return json.RootElement.GetProperty("jql").GetString() ?? string.Empty;
        }

        private static Team ATeamNarrowingOnHundredsOfProjects()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            team.DataRetrievalValue =
                $"project in ({string.Join(", ", Enumerable.Range(0, 200).Select(index => $"PROJ{index}"))})";

            return team;
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

        private static HttpMessageHandler AHandlerWhereSearch(
            string deploymentType, Func<HttpRequestMessage, HttpResponseMessage> answerSearch)
            => AHandlerWhereSearch(deploymentType, answerSearch, []);

        /// <summary>
        /// Every url the connector asked for, in order, so a test can say which endpoints were reached and
        /// which were left alone - neither of which can be read off the answer the connector returns.
        /// </summary>
        private static HttpMessageHandler AHandlerWhereSearch(
            string deploymentType,
            Func<HttpRequestMessage, HttpResponseMessage> answerSearch,
            ICollection<string> requestedUrls)
            => AHandlerAnswering(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                requestedUrls.Add(request.RequestUri?.PathAndQuery ?? string.Empty);

                if (path.Contains("/search", StringComparison.Ordinal))
                {
                    return answerSearch(request);
                }

                var body = path switch
                {
                    _ when path.EndsWith(ServerInfoPath, StringComparison.Ordinal)
                        => $"{{\"deploymentType\":\"{deploymentType}\"}}",
                    _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal) => "[]",
                    _ => "{}",
                };

                return Respond(HttpStatusCode.OK, body);
            });

        private static HttpMessageHandler AHandlerAnswering(Func<HttpRequestMessage, HttpResponseMessage> answer)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(answer(request)));

            return mock.Object;
        }

        private static HttpResponseMessage Respond(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
