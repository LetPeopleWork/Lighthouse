using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Jira Cloud and Jira Data Center answer the field-list endpoint with different objects: Cloud gives
    /// every field a "key", Data Center gives none of them one. Both shapes are read by the same lookup,
    /// so a Data Center field list is the only way to see what happens when a field reference matches
    /// nothing - which is the situation the administrator is supposed to be told about by name.
    ///
    /// The field list is also the read Jira is most likely to refuse outright: it needs project-browse
    /// permission to return anything, and it is far larger than anything else Lighthouse asks for, so a
    /// proxy in front of Jira can cut it off on its own. The refusal tests here pin what the administrator
    /// is told when that happens, and which input on the screen the message is pinned to.
    /// </summary>
    [TestFixture]
    public class JiraFieldLookupTest
    {
        private const string UnmatchedFieldReference = "MamboJambo";

        /// <summary>What serverInfo answers for a Data Center instance - Jira still calls it "Server" there.</summary>
        private const string OnDataCenter = "Server";

        private const string ServerInfoPath = "rest/api/2/serverInfo";

        private const string FieldListPath = "rest/api/latest/field";

        private const string MyselfPath = "rest/api/2/myself";

        private const string SearchPath = "/search";

        private const string TheAdditionalFieldsInput = "Additional Fields";

        private const string FieldListUnreadable = "field_list_unreadable";

        /// <summary>
        /// A field list as Jira Data Center returns it. No object carries a "key", and nothing here is
        /// named "Flagged" or holds the id Jira Cloud gives the flag, so every lookup this fixture makes
        /// has to walk the whole array and come back empty-handed.
        /// </summary>
        private const string DataCenterFieldList =
            "[{\"id\":\"summary\",\"name\":\"Summary\",\"custom\":false,\"schema\":{\"type\":\"string\"}},"
            + "{\"id\":\"customfield_10100\",\"name\":\"Story Points\",\"custom\":true,\"schema\":{\"type\":\"number\"}}]";

        /// <summary>The same two fields as Jira Cloud returns them - each one also carrying a "key".</summary>
        private const string CloudFieldList =
            "[{\"id\":\"summary\",\"key\":\"summary\",\"name\":\"Summary\",\"custom\":false,\"schema\":{\"type\":\"string\"}},"
            + "{\"id\":\"customfield_10100\",\"key\":\"customfield_10100\",\"name\":\"Story Points\",\"custom\":true,\"schema\":{\"type\":\"number\"}}]";

        private const string JirasOwnSentence = "You do not have permission to view fields.";

        private const string ARefusalCarryingJirasOwnSentence =
            "{\"errorMessages\":[\"" + JirasOwnSentence + "\"],\"errors\":{}}";

        private const string OnePageHoldingOneIssue =
            "{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{\"key\":\"PROJ-1\",\"fields\":{"
            + "\"summary\":\"An issue\",\"created\":\"2026-08-01T09:00:00.000+0000\","
            + "\"status\":{\"name\":\"In Progress\"},\"issuetype\":{\"name\":\"Story\"}}}]}";

        private static readonly string[] TheIssueOnThePage = ["PROJ-1"];

        private static readonly StubAnswer AnAuthenticatedUser = new(HttpStatusCode.OK, "{\"accountId\":\"someone\"}");

        [Test]
        public async Task ValidateConnection_OnDataCenter_UnmatchedField_SaysWhichFieldIsMissing()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(UnmatchedFieldReference, AFieldListOf(DataCenterFieldList));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"));
                Assert.That(verdict.Message, Does.Contain(UnmatchedFieldReference));
            }
        }

        [Test]
        public async Task ValidateConnection_OnCloud_UnmatchedField_StillSaysWhichFieldIsMissing()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(UnmatchedFieldReference, AFieldListOf(CloudFieldList));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"));
                Assert.That(verdict.Message, Does.Contain(UnmatchedFieldReference));
            }
        }

        [Test]
        public async Task ValidateConnection_FieldListRefused_NamesTheFieldListAndNotTheUrl()
        {
            var verdict = await TheVerdictWhenJiraRefusesTheFieldListWith(HttpStatusCode.Forbidden);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo(FieldListUnreadable));
                Assert.That(verdict.FieldName, Is.EqualTo(TheAdditionalFieldsInput));
                Assert.That(verdict.Message, Does.Not.Contain("provided URL"));
            }
        }

        /// <summary>
        /// This sentence is the whole point of the verdict - it is what an administrator who cannot get
        /// past the connection screen reads, and the only place the two things they can act on are named.
        /// Pinning the phrases it is built from, rather than the sentence as a whole, leaves the wording
        /// free to improve while keeping the message from silently emptying out.
        /// </summary>
        [Test]
        public async Task ValidateConnection_FieldListRefused_SaysWhatJiraAnsweredAndWhatToTry()
        {
            var verdict = await TheVerdictWhenJiraRefusesTheFieldListWith(HttpStatusCode.Forbidden);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Message, Does.Contain("could not read the list of fields"));
                Assert.That(verdict.Message, Does.Contain("Jira answered 403 (Forbidden)"));
                Assert.That(verdict.Message, Does.Contain("browse at least one project"));
                Assert.That(verdict.Message, Does.Contain("a proxy in front of Jira"));
                Assert.That(verdict.Message, Does.Contain("is much larger than the others"));
            }
        }

        [Test]
        public async Task ValidateConnection_FieldListRefused_CarriesJirasOwnSentence()
        {
            var verdict = await TheVerdictWhenJiraRefusesTheFieldListWith(HttpStatusCode.Forbidden);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.TechnicalDetails, Does.Contain(FieldListPath));
                Assert.That(verdict.TechnicalDetails, Does.Contain("answered 403 Forbidden"));
                Assert.That(verdict.TechnicalDetails, Does.Contain(JirasOwnSentence));
            }
        }

        [Test]
        public async Task ValidateConnection_MyselfRefused_StillNamesTheCredential()
        {
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;
            var connector = JiraConnectorTestSetup.AConnectorOver(AHandlerServing(
                AFieldListOf(CloudFieldList), new StubAnswer(HttpStatusCode.Unauthorized, "{\"errorMessages\":[]}")));

            var verdict = await connector.ValidateConnection(connection);

            Assert.That(verdict.Code, Is.EqualTo("authentication_failed"));
        }

        [Test]
        public async Task ValidateConnection_JiraUnreachable_StillNamesTheUrl()
        {
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;
            var connector = JiraConnectorTestSetup.AConnectorOver(AHandlerThatCannotReachJira());

            var verdict = await connector.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("connection_failed"));
                Assert.That(verdict.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.Url));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_FieldListRefused_DoesNotSayUnexpectedError()
        {
            var requestedUrls = new List<string>();
            var connector = AConnectorRefusedTheFieldListWith(HttpStatusCode.BadGateway, requestedUrls);

            var verdict = await connector.ValidateTeamSettings(JiraConnectorTestSetup.ATeamOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo(FieldListUnreadable));
                Assert.That(UrlsReaching(requestedUrls, SearchPath), Is.Empty);
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_FieldListRefused_DoesNotSayUnexpectedError()
        {
            var requestedUrls = new List<string>();
            var connector = AConnectorRefusedTheFieldListWith(HttpStatusCode.BadGateway, requestedUrls);

            var verdict = await connector.ValidatePortfolioSettings(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo(FieldListUnreadable));
                Assert.That(UrlsReaching(requestedUrls, SearchPath), Is.Empty);
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_OnDataCenterWithoutAFlaggedField_ReadsTheTeamAnyway()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            var connector = JiraConnectorTestSetup.AConnectorOver(AHandlerServing(AFieldListOf(DataCenterFieldList)));

            var workItems = await connector.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(workItems.Select(workItem => workItem.ReferenceId), Is.EquivalentTo(TheIssueOnThePage));
        }

        private static List<string> UrlsReaching(List<string> requestedUrls, string path)
            => requestedUrls.Where(url => url.Contains(path, StringComparison.Ordinal)).ToList();

        private static async Task<ConnectionValidationResult> TheVerdictOnAConnectionAskingFor(
            string fieldReference, StubAnswer fieldList)
        {
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = fieldReference,
                Reference = fieldReference,
            });

            var connector = JiraConnectorTestSetup.AConnectorOver(AHandlerServing(fieldList));

            return await connector.ValidateConnection(connection);
        }

        private static Task<ConnectionValidationResult> TheVerdictWhenJiraRefusesTheFieldListWith(HttpStatusCode status)
            => TheVerdictOnAConnectionAskingFor(
                UnmatchedFieldReference, new StubAnswer(status, ARefusalCarryingJirasOwnSentence));

        private static JiraWorkTrackingConnector AConnectorRefusedTheFieldListWith(
            HttpStatusCode status, List<string> requestedUrls)
            => JiraConnectorTestSetup.AConnectorOver(AHandlerServing(
                new StubAnswer(status, ARefusalCarryingJirasOwnSentence), requestedUrls: requestedUrls));

        private static StubAnswer AFieldListOf(string fieldList) => new(HttpStatusCode.OK, fieldList);

        private static HttpMessageHandler AHandlerServing(
            StubAnswer fieldList, StubAnswer? myself = null, List<string>? requestedUrls = null)
        {
            var whoIsSignedIn = myself ?? AnAuthenticatedUser;

            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    requestedUrls?.Add(request.RequestUri?.ToString() ?? string.Empty);

                    return Task.FromResult(Answer(request, fieldList, whoIsSignedIn));
                });

            return mock.Object;
        }

        private static HttpMessageHandler AHandlerThatCannotReachJira()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("No such host is known."));

            return mock.Object;
        }

        private static HttpResponseMessage Answer(HttpRequestMessage request, StubAnswer fieldList, StubAnswer myself)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            var answer = path switch
            {
                _ when path.EndsWith(ServerInfoPath, StringComparison.Ordinal)
                    => new StubAnswer(HttpStatusCode.OK, $"{{\"deploymentType\":\"{OnDataCenter}\"}}"),
                _ when path.EndsWith(FieldListPath, StringComparison.Ordinal) => fieldList,
                _ when path.EndsWith(MyselfPath, StringComparison.Ordinal) => myself,
                _ when path.Contains(SearchPath, StringComparison.Ordinal)
                    => new StubAnswer(HttpStatusCode.OK, OnePageHoldingOneIssue),
                _ => new StubAnswer(HttpStatusCode.OK, "{}"),
            };

            return new HttpResponseMessage(answer.Status)
            {
                Content = new StringContent(answer.Body, Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>One status and one body, so a test can say what Jira answers on a single endpoint.</summary>
        private sealed record StubAnswer(HttpStatusCode Status, string Body);
    }
}
