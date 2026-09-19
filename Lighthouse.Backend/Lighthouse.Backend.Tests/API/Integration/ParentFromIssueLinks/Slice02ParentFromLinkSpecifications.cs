using System.Globalization;
using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Step definitions for the second slice. The door pressed here is a Team refresh rather than
    /// connection validation, and a refresh has to be handed issues - which the validation harness beside
    /// this one deliberately cannot do, because a scenario about a verdict has nothing to say about what
    /// the instance holds. So the transport is described again here, serving the same endpoints plus the
    /// search a refresh actually reads.
    ///
    /// The one thing faked is the transport to Jira. Everything between the refresh and it is production
    /// code: the real connector resolves the real reference over the real field and link-type payloads and
    /// reads the real link entries off the real issue payloads.
    /// </summary>
    public partial class Slice02ParentFromLinkTest
    {
        private const string TheChild = "PROJ-7";

        private const string TheChildWithNothingMatching = "PROJ-8";

        private const string TheParent = "EPIC-1";

        /// <summary>The issue the Team scenarios call a parent, seen again as the record a Portfolio refreshes.</summary>
        private const string TheFeature = TheParent;

        private const string TheLevelAboveTheFeature = "INIT-9";

        private const string TheParentJiraItselfNames = "EPIC-2";

        /// <summary>
        /// The field Jira Data Center hangs an item's parent on, and which a refresh reads when nothing
        /// was named in Parent Override Field.
        /// </summary>
        private const string TheFieldDataCenterHangsParentsOn = "Epic Link";

        private const string TheParentDataCenterWouldHaveNamed = "EPIC-4";

        private const string ACustomField = "Story Points";

        /// <summary>The id the field list below hands the first custom field a scenario defines.</summary>
        private const string TheIdThatFieldCarries = "customfield_10100";

        private const string AParentTypedIntoThatField = "EPIC-3";

        /// <summary>Any row number will do; it only has to be the one the Team's setting points at.</summary>
        private const int TheAdditionalFieldTheOverridePointsAt = 4711;

        private const string ServerInfoEndpoint = "rest/api/2/serverInfo";

        private const string CredentialCheckEndpoint = "rest/api/2/myself";

        private const string FieldListEndpoint = "rest/api/latest/field";

        private const string IssueLinkTypeEndpoint = "rest/api/latest/issueLinkType";

        private const string SearchEndpoint = "/search";

        private static readonly JiraLinkType ALinkTypeTheInstanceDefines = new("Caused by", "was caused by", "causes");

        /// <summary>A live instance ships this one reading the same phrase in both directions.</summary>
        private static readonly JiraLinkType AnotherLinkTypeTheInstanceDefines = new("Relates", "relates to", "relates to");

        /// <summary>
        /// A third type, because the link carrying a Feature to the level above it is not the link carrying
        /// a Work Item to its Feature, and each grain names its own.
        /// </summary>
        private static readonly JiraLinkType ALinkTypeAPortfolioNames = new("Belongs to", "belongs to", "owns");

        private readonly List<string> issuesTheInstanceServes = [];

        private readonly List<string> customFieldsTheInstanceDefines = [];

        private readonly List<JiraLinkType> linkTypesTheInstanceDefines = [];

        private Mock<ILogger<JiraWorkTrackingConnector>> whatTheRefreshWroteToTheLog = new();

        private string? whatTheParentOverrideNames;

        [SetUp]
        public void ForgetTheInstanceTheLastScenarioDescribed()
        {
            issuesTheInstanceServes.Clear();
            customFieldsTheInstanceDefines.Clear();
            linkTypesTheInstanceDefines.Clear();
            linkTypesTheInstanceDefines.Add(ALinkTypeTheInstanceDefines);
            linkTypesTheInstanceDefines.Add(AnotherLinkTypeTheInstanceDefines);
            linkTypesTheInstanceDefines.Add(ALinkTypeAPortfolioNames);
            whatTheRefreshWroteToTheLog = new Mock<ILogger<JiraWorkTrackingConnector>>();
            whatTheParentOverrideNames = null;
        }

        /// <summary>
        /// What an administrator typed into Parent Override Field. One Additional Field named and
        /// referenced the same way, which is how that box is filled in.
        /// </summary>
        private void TheParentOverrideNames(string reference) => whatTheParentOverrideNames = reference;

        private void TheInstanceDefinesTheCustomField(string name) => customFieldsTheInstanceDefines.Add(name);

        private void TheIssueHasOneLinkWhoseOutwardIssueIs(string key, JiraLinkType linkType, string counterpartKey)
            => issuesTheInstanceServes.Add(AnIssue(key, string.Empty, linkType.LinkWhoseOutwardIssueIs(counterpartKey)));

        private void TheIssueHasOneLinkWhoseInwardIssueIs(string key, JiraLinkType linkType, string counterpartKey)
            => issuesTheInstanceServes.Add(AnIssue(key, string.Empty, linkType.LinkWhoseInwardIssueIs(counterpartKey)));

        private void TheIssueCarriesTheFieldValue(string key, string fieldId, string value)
            => issuesTheInstanceServes.Add(AnIssue(key, $", \"{fieldId}\": \"{value}\""));

        /// <summary>An issue whose parent Jira itself names, which is where a refresh has always read it.</summary>
        private void TheIssueHasJirasOwnParent(string key, string parentKey)
            => issuesTheInstanceServes.Add(AnIssue(key, $", \"parent\": {{\"key\": \"{parentKey}\"}}"));

        /// <summary>
        /// An issue whose links say nothing about a parent while the field a Data Center refresh would
        /// otherwise have read names one.
        /// </summary>
        private void TheIssueNamesAParentInTheDataCenterFieldAndHasNoMatchingLink(string key, string parentKey)
            => issuesTheInstanceServes.Add(AnIssue(
                key,
                $", \"{TheIdThatFieldCarries}\": \"{parentKey}\"",
                AnotherLinkTypeTheInstanceDefines.LinkWhoseOutwardIssueIs(parentKey)));

        private async Task<List<WorkItem>> TheTeamIsRefreshed()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(team);

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return [.. await connector.GetWorkItemsForTeam(team, CancellationToken.None)];
        }

        private async Task<List<Feature>> ThePortfolioIsRefreshed()
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(portfolio);

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return await connector.GetFeaturesForProject(portfolio, CancellationToken.None);
        }

        private void WhatTheAdministratorTypedIntoTheOverrideIsSetOn(IWorkItemQueryOwner owner)
        {
            if (whatTheParentOverrideNames is null)
            {
                return;
            }

            owner.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = TheAdditionalFieldTheOverridePointsAt,
                DisplayName = whatTheParentOverrideNames,
                Reference = whatTheParentOverrideNames,
            });

            owner.ParentOverrideAdditionalFieldDefinitionId = TheAdditionalFieldTheOverridePointsAt;
        }

        private static string TheParentOf<TRecord>(List<TRecord> refreshed, string key)
            where TRecord : WorkItemBase
        {
            var item = TheRecordFor(refreshed, key);

            return item is null ? DidNotComeBack(key) : item.ParentReferenceId;
        }

        /// <summary>What an administrator reads in the Additional Field they named, on the stored record.</summary>
        private static string TheAdditionalFieldValueForTheOverrideOf<TRecord>(List<TRecord> refreshed, string key)
            where TRecord : WorkItemBase
        {
            var item = TheRecordFor(refreshed, key);

            if (item is null)
            {
                return DidNotComeBack(key);
            }

            return item.AdditionalFieldValues.TryGetValue(TheAdditionalFieldTheOverridePointsAt, out var value)
                ? value ?? string.Empty
                : $"<{key} carries no value at all for the Additional Field the override names>";
        }

        private static TRecord? TheRecordFor<TRecord>(List<TRecord> refreshed, string key)
            where TRecord : WorkItemBase
            => refreshed.Find(refreshedItem => refreshedItem.ReferenceId == key);

        private static string DidNotComeBack(string key) => $"<{key} did not come back from the refresh at all>";

        private void NothingWasWrittenToTheLogAsAWarning()
            => whatTheRefreshWroteToTheLog.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never,
                "An item whose links say nothing about a parent is the ordinary case, not a misconfiguration, and a warning on every such item buries the ones that mean something.");

        private static string AnIssue(string key, string furtherFields, params string[] links)
            => "{\"key\": \"" + key + "\", \"fields\": {"
                + "\"summary\": \"" + key + " summary\""
                + ", \"issuetype\": {\"name\": \"Story\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                + ", \"updated\": \"2026-01-02T00:00:00.000+0000\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]"
                + furtherFields + "}}";

        private HttpMessageHandler AJiraAnsweringForThatInstance()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(
                    (request, _) => Task.FromResult(AnAnswerTo(request.RequestUri?.AbsolutePath ?? string.Empty)));

            return mock.Object;
        }

        private HttpResponseMessage AnAnswerTo(string path)
        {
            var body = path switch
            {
                _ when path.EndsWith(ServerInfoEndpoint, StringComparison.Ordinal) => "{\"deploymentType\":\"Cloud\"}",
                _ when path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.EndsWith(FieldListEndpoint, StringComparison.Ordinal) => TheFieldsItDefines(),
                _ when path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal) => TheLinkTypesItDefines(),
                _ when path.Contains(SearchEndpoint, StringComparison.Ordinal) => TheIssuesItServes(),
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private string TheIssuesItServes()
            => "{\"issues\":[" + string.Join(",", issuesTheInstanceServes) + "],\"isLast\":true}";

        /// <summary>The field list as Jira Cloud writes it - every field carrying a "key" as well as an id.</summary>
        private string TheFieldsItDefines()
        {
            var fields = customFieldsTheInstanceDefines.Select((name, index) =>
            {
                var id = "customfield_" + (10100 + index).ToString(CultureInfo.InvariantCulture);

                return "{\"id\":\"" + id + "\",\"key\":\"" + id + "\",\"name\":\"" + name
                    + "\",\"custom\":true,\"schema\":{\"type\":\"string\"}}";
            });

            return "[" + string.Join(",", fields) + "]";
        }

        /// <summary>The link-type list as a live Cloud instance answered it: an object keyed issueLinkTypes.</summary>
        private string TheLinkTypesItDefines()
        {
            var linkTypes = linkTypesTheInstanceDefines.Select((linkType, index) =>
                "{\"id\":\"" + (10000 + index).ToString(CultureInfo.InvariantCulture) + "\""
                + ",\"name\":\"" + linkType.Name + "\""
                + ",\"inward\":\"" + linkType.Inward + "\""
                + ",\"outward\":\"" + linkType.Outward + "\"}");

            return "{\"issueLinkTypes\":[" + string.Join(",", linkTypes) + "]}";
        }
    }
}
