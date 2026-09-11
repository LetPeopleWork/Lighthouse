using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The query a board hands to a new team is welded together from two halves Jira stores apart: the saved
    /// filter behind the board, and the board's own sub-filter. Either half can be missing or unreadable, and
    /// neither arrives as a self-contained expression. These tests read the assembled string itself rather
    /// than a success flag, because a query Jira happily accepts can still select the wrong work items.
    /// </summary>
    [TestFixture]
    public class JiraBoardQueryAssemblyTest
    {
        private const string BoardId = "42";
        private const string FilterId = "10001";

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

        private static HttpResponseMessage Respond(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
