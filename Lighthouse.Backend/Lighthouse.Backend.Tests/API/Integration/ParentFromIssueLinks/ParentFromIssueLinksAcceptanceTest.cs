using System.Globalization;
using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Acceptance harness for naming a Jira issue link type where a field name goes. A scenario describes
    /// an instance - the custom fields it defines, the link types it defines - and then drives connection
    /// validation against it, which is the door the administrator actually presses.
    ///
    /// The one thing faked is the transport to Jira, because that is the only driven port a scenario
    /// cannot reach for real. Everything between the verdict and that transport is production code: the
    /// real connector resolves the real references over the real field and link-type payloads.
    ///
    /// Every request the connector issues is recorded, because two of the promises here are about a call
    /// that must NOT happen - an instance whose references all resolve as fields pays nothing extra, and
    /// one link-type reference costs one lookup however many references share it.
    /// </summary>
    public abstract class ParentFromIssueLinksAcceptanceTest
    {
        protected const string IssueLinkTypeEndpoint = "rest/api/latest/issueLinkType";

        private const string ServerInfoEndpoint = "rest/api/2/serverInfo";

        protected const string FieldListEndpoint = "rest/api/latest/field";

        /// <summary>The one call that actually establishes who Lighthouse is signed in to Jira as.</summary>
        protected const string CredentialCheckEndpoint = "rest/api/2/myself";

        private const string SearchEndpoint = "/search";

        private readonly List<JiraField> definedFields = [];

        private readonly List<JiraLinkType> definedLinkTypes = [];

        private readonly List<string> requestedPaths = [];

        private HttpStatusCode credentialCheckStatus = HttpStatusCode.OK;

        private HttpStatusCode linkTypeListStatus = HttpStatusCode.OK;

        [SetUp]
        public void ForgetTheInstanceTheLastScenarioDescribed()
        {
            definedFields.Clear();
            definedLinkTypes.Clear();
            requestedPaths.Clear();
            credentialCheckStatus = HttpStatusCode.OK;
            linkTypeListStatus = HttpStatusCode.OK;
        }

        protected void TheInstanceDefinesTheCustomField(string name)
            => definedFields.Add(new JiraField("customfield_" + Digits(10100 + definedFields.Count), name));

        protected void TheInstanceDefinesTheLinkType(string name, string inward, string outward)
            => definedLinkTypes.Add(new JiraLinkType(Digits(10000 + definedLinkTypes.Count), name, inward, outward));

        /// <summary>
        /// Jira answers a credential it does not accept - and a request carrying no credential at all - with
        /// 200 and an empty list rather than with a refusal, so an instance that truly defines no link types
        /// and one that will not show them to this caller put the same bytes on the wire. A scenario cannot
        /// tell those two apart, and neither can Lighthouse.
        /// </summary>
        protected void TheLinkTypeListComesBackEmpty() => definedLinkTypes.Clear();

        /// <summary>
        /// Jira turning the link type list down outright, which is a different animal from answering it
        /// empty: nobody has to guess what happened, Jira said so.
        /// </summary>
        protected void TheIssueLinkTypeEndpointAnswers(HttpStatusCode status) => linkTypeListStatus = status;

        protected void TheCredentialIsRefused() => credentialCheckStatus = HttpStatusCode.Unauthorized;

        /// <summary>
        /// Where a request to that endpoint first appears in the sequence Lighthouse issued, or -1 when it
        /// never issued one, so a scenario can say which of two calls came first.
        /// </summary>
        protected int WhenTheFirstRequestReached(string endpoint)
            => requestedPaths.FindIndex(requested => requested.Contains(endpoint, StringComparison.Ordinal));

        /// <summary>
        /// One Additional Field per reference, named and referenced the same way, which is how an
        /// administrator who types a link type name into that box leaves it.
        /// </summary>
        protected async Task<ConnectionValidationResult> TheVerdictOnAConnectionAskingFor(params string[] references)
        {
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;

            foreach (var reference in references)
            {
                connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                {
                    DisplayName = reference,
                    Reference = reference,
                });
            }

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance());

            return await connector.ValidateConnection(connection);
        }

        protected int RequestsReaching(string endpoint)
            => requestedPaths.Count(requested => requested.Contains(endpoint, StringComparison.Ordinal));

        private static string Digits(int value) => value.ToString(CultureInfo.InvariantCulture);

        private HttpMessageHandler AJiraAnsweringForThatInstance()
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
                    requestedPaths.Add(path);

                    return Task.FromResult(AnAnswerTo(path));
                });

            return mock.Object;
        }

        private HttpResponseMessage AnAnswerTo(string path)
        {
            var (status, body) = path switch
            {
                _ when path.EndsWith(ServerInfoEndpoint, StringComparison.Ordinal)
                    => (HttpStatusCode.OK, "{\"deploymentType\":\"Cloud\"}"),
                _ when path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal)
                    => (credentialCheckStatus, "{\"accountId\":\"someone\"}"),
                _ when path.EndsWith(FieldListEndpoint, StringComparison.Ordinal)
                    => (HttpStatusCode.OK, TheFieldsItDefines()),
                _ when path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    => (linkTypeListStatus, TheLinkTypesItDefines()),
                _ when path.Contains(SearchEndpoint, StringComparison.Ordinal)
                    => (HttpStatusCode.OK, "{\"issues\":[],\"isLast\":true}"),
                _ => (HttpStatusCode.OK, "{}"),
            };

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>The field list as Jira Cloud writes it - every field carrying a "key" as well as an id.</summary>
        private string TheFieldsItDefines()
        {
            var fields = definedFields.Select(field =>
                "{\"id\":\"" + field.Id + "\",\"key\":\"" + field.Id + "\",\"name\":\"" + field.Name
                + "\",\"custom\":true,\"schema\":{\"type\":\"string\"}}");

            return "[" + string.Join(",", fields) + "]";
        }

        /// <summary>
        /// The link-type list as a live Cloud instance answered it: an object keyed issueLinkTypes, not a
        /// bare array and not the values-plus-isLast envelope the field and search endpoints use, and with
        /// no paging to carry.
        /// </summary>
        private string TheLinkTypesItDefines()
        {
            var linkTypes = definedLinkTypes.Select(linkType =>
                "{\"id\":\"" + linkType.Id + "\",\"name\":\"" + linkType.Name + "\",\"inward\":\"" + linkType.Inward
                + "\",\"outward\":\"" + linkType.Outward
                + "\",\"self\":\"https://jira.example.invalid/rest/api/2/issueLinkType/" + linkType.Id + "\"}");

            return "{\"issueLinkTypes\":[" + string.Join(",", linkTypes) + "]}";
        }

        private sealed record JiraField(string Id, string Name);

        private sealed record JiraLinkType(string Id, string Name, string Inward, string Outward);
    }
}
