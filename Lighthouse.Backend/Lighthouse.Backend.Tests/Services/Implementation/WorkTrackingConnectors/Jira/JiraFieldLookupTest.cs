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

        private const string OnePageHoldingOneIssue =
            "{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{\"key\":\"PROJ-1\",\"fields\":{"
            + "\"summary\":\"An issue\",\"created\":\"2026-08-01T09:00:00.000+0000\","
            + "\"status\":{\"name\":\"In Progress\"},\"issuetype\":{\"name\":\"Story\"}}}]}";

        private static readonly string[] TheIssueOnThePage = ["PROJ-1"];

        [Test]
        public async Task ValidateConnection_OnDataCenter_UnmatchedField_SaysWhichFieldIsMissing()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(UnmatchedFieldReference, DataCenterFieldList);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"));
                Assert.That(verdict.Message, Does.Contain(UnmatchedFieldReference));
            }
        }

        [Test]
        public async Task ValidateConnection_OnCloud_UnmatchedField_StillSaysWhichFieldIsMissing()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(UnmatchedFieldReference, CloudFieldList);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"));
                Assert.That(verdict.Message, Does.Contain(UnmatchedFieldReference));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_OnDataCenterWithoutAFlaggedField_ReadsTheTeamAnyway()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            var connector = JiraConnectorTestSetup.AConnectorOver(AHandlerServing(DataCenterFieldList));

            var workItems = await connector.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(workItems.Select(workItem => workItem.ReferenceId), Is.EquivalentTo(TheIssueOnThePage));
        }

        private static async Task<ConnectionValidationResult> TheVerdictOnAConnectionAskingFor(
            string fieldReference, string fieldList)
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

        private static HttpMessageHandler AHandlerServing(string fieldList)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(Answer(request, fieldList)));

            return mock.Object;
        }

        private static HttpResponseMessage Answer(HttpRequestMessage request, string fieldList)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            var body = path switch
            {
                _ when path.EndsWith(ServerInfoPath, StringComparison.Ordinal) => $"{{\"deploymentType\":\"{OnDataCenter}\"}}",
                _ when path.EndsWith(FieldListPath, StringComparison.Ordinal) => fieldList,
                _ when path.EndsWith(MyselfPath, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.Contains("/search", StringComparison.Ordinal) => OnePageHoldingOneIssue,
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
