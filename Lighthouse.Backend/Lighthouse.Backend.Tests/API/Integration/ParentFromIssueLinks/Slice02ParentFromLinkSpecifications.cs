using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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

        /// <summary>
        /// The field Jira Data Center hangs a Feature's parent on. It is a different field from the one the
        /// Team scenarios read, and the guard that stops a refresh falling back to it is written out once per
        /// grain rather than shared.
        /// </summary>
        private const string TheFieldDataCenterHangsFeatureParentsOn = "Parent Link";

        private const string TheFeatureWithNothingMatching = "EPIC-8";

        private const string TheLevelAboveDataCenterWouldHaveNamed = "INIT-4";

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

        /// <summary>What an administrator renames a link type to between two refreshes.</summary>
        private const string TheNameALinkTypeIsRenamedTo = "Caused by (renamed)";

        private readonly List<string> issuesTheInstanceServes = [];

        private readonly List<string> customFieldsTheInstanceDefines = [];

        private readonly List<JiraLinkType> linkTypesTheInstanceDefines = [];

        /// <summary>Every path the stub was asked for, in the order it was asked, across a whole scenario.</summary>
        private readonly List<string> whatTheRefreshesAskedFor = [];

        /// <summary>
        /// Which issues a given query finds, for the scenarios describing two grains at once. A Portfolio's
        /// Features and a Team's Work Items are two different answers to two different queries, and the
        /// query is the only thing in the request that tells them apart. A scenario that says nothing here
        /// gets the whole instance back for every query, which is what every scenario written before this
        /// one expects.
        /// </summary>
        private readonly Dictionary<string, List<string>> whatEachQueryFinds = [];

        private readonly List<WorkItem> whatStorageHoldsForTheWorkItems = [];

        private readonly List<Feature> whatStorageHoldsForTheFeatures = [];

        private Mock<ILogger<JiraWorkTrackingConnector>> whatTheRefreshWroteToTheLog = new();

        private string? whatTheParentOverrideNames;

        private bool theCredentialIsStillAccepted = true;

        /// <summary>
        /// The one Team every refresh in a scenario fetches. A scenario refreshing twice is describing the
        /// same Team seen on two cycles, so it has to keep its connection id - the connector holds resolved
        /// field names against it - while getting a connector of its own each time, the way a refresh does.
        /// </summary>
        private Team? theTeamEveryRefreshFetches;

        private Team? theTeamDeliveringTheWork;

        private Portfolio? thePortfolioBeingSized;

        [SetUp]
        public void ForgetTheInstanceTheLastScenarioDescribed()
        {
            issuesTheInstanceServes.Clear();
            customFieldsTheInstanceDefines.Clear();
            linkTypesTheInstanceDefines.Clear();
            linkTypesTheInstanceDefines.Add(ALinkTypeTheInstanceDefines);
            linkTypesTheInstanceDefines.Add(AnotherLinkTypeTheInstanceDefines);
            linkTypesTheInstanceDefines.Add(ALinkTypeAPortfolioNames);
            whatTheRefreshesAskedFor.Clear();
            whatEachQueryFinds.Clear();
            whatStorageHoldsForTheWorkItems.Clear();
            whatStorageHoldsForTheFeatures.Clear();
            whatTheRefreshWroteToTheLog = new Mock<ILogger<JiraWorkTrackingConnector>>();
            whatTheParentOverrideNames = null;
            theCredentialIsStillAccepted = true;
            theTeamEveryRefreshFetches = null;
            theTeamDeliveringTheWork = null;
            thePortfolioBeingSized = null;
        }

        /// <summary>
        /// The token this connection signs in with has stopped being accepted. Jira does not refuse the
        /// questions a refresh asks when that happens - it answers them as it would answer a stranger, and
        /// a stranger is shown no link types at all.
        /// </summary>
        private void TheCredentialIsNoLongerAccepted() => theCredentialIsStillAccepted = false;

        /// <summary>
        /// What an administrator typed into Parent Override Field. One Additional Field named and
        /// referenced the same way, which is how that box is filled in.
        /// </summary>
        private void TheParentOverrideNames(string reference) => whatTheParentOverrideNames = reference;

        private void TheInstanceDefinesTheCustomField(string name) => customFieldsTheInstanceDefines.Add(name);

        /// <summary>
        /// The field Lighthouse reads on every Jira refresh whether or not anybody configured anything. It
        /// has to be described for a cost measurement to mean what it says: what sends a refresh looking at
        /// link types is a reference the field list could not resolve, and on an instance missing this one
        /// that describes a Team nobody ever touched.
        /// </summary>
        private void TheInstanceDefinesTheFieldEveryRefreshAlreadyReads()
            => TheInstanceDefinesTheCustomField(JiraFieldNames.FlaggedName);

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
            if (theTeamEveryRefreshFetches is null)
            {
                theTeamEveryRefreshFetches = JiraConnectorTestSetup.ATeamOnJiraCloud();
                WhatTheAdministratorTypedIntoTheOverrideIsSetOn(theTeamEveryRefreshFetches);
            }

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return [.. await connector.GetWorkItemsForTeam(theTeamEveryRefreshFetches, CancellationToken.None)];
        }

        /// <summary>
        /// The refresh, run for whatever it refuses with rather than for what it returns. A scenario about
        /// the reason an operator is given has to read the refusal without also being the scenario that
        /// pins there being one.
        /// </summary>
        private async Task<JiraReadException?> WhatTheRefreshRefusedWith()
        {
            try
            {
                await TheTeamIsRefreshed();

                return null;
            }
            catch (JiraReadException refusal)
            {
                return refusal;
            }
        }

        private async Task<List<Feature>> ThePortfolioIsRefreshed()
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return await connector.GetFeaturesForProject(APortfolioCarryingTheOverride(), CancellationToken.None);
        }

        /// <summary>
        /// One refresh of a Portfolio as the update actually runs it: the Features the query returns, and
        /// then the Features those hang under, which are fetched separately because they are not in the
        /// query. Two fetches, one connector - the same connector a refresh builds once and uses throughout.
        /// </summary>
        private async Task ThePortfolioAndTheFeaturesAboveItAreRefreshed()
        {
            var portfolio = APortfolioCarryingTheOverride();
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            await connector.GetFeaturesForProject(portfolio, CancellationToken.None);
            await connector.GetParentFeaturesDetails(portfolio, [TheLevelAboveTheFeature], CancellationToken.None);
        }

        private Portfolio APortfolioCarryingTheOverride()
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(portfolio);

            return portfolio;
        }

        private int HowOftenTheInstanceWasAskedForItsLinkTypes()
            => whatTheRefreshesAskedFor.FindAll(path => path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal)).Count;

        /// <summary>
        /// An administrator renames a link type in one place and every issue carrying one reads the new name
        /// at once, so the entries already described are rewritten alongside the list the instance answers
        /// with.
        /// </summary>
        private void TheInstanceRenamesTheLinkType(JiraLinkType linkType, string newName)
        {
            linkTypesTheInstanceDefines[linkTypesTheInstanceDefines.IndexOf(linkType)] = linkType with { Name = newName };

            for (var index = 0; index < issuesTheInstanceServes.Count; index++)
            {
                issuesTheInstanceServes[index] = issuesTheInstanceServes[index].Replace(
                    $"\"name\": \"{linkType.Name}\"", $"\"name\": \"{newName}\"", StringComparison.Ordinal);
            }
        }

        private Task<RequestTally> WhatOneTeamRefreshAsksForWith(string reference) => WhatOneTeamRefreshCosts(reference);

        /// <summary>A refresh of a Team nobody has configured, which is what every Team did before this feature.</summary>
        private Task<RequestTally> WhatOneTeamRefreshAsksForWithTheBoxLeftEmpty() => WhatOneTeamRefreshCosts(null);

        /// <summary>
        /// One Team refresh, counted on its own. The Team is built again so that the setting is the only
        /// thing that differs between two measurements taken in the same scenario.
        /// </summary>
        private async Task<RequestTally> WhatOneTeamRefreshCosts(string? reference)
        {
            whatTheParentOverrideNames = reference;
            theTeamEveryRefreshFetches = null;

            var alreadyAsked = whatTheRefreshesAskedFor.Count;
            await TheTeamIsRefreshed();

            return TallyOf(whatTheRefreshesAskedFor.Skip(alreadyAsked));
        }

        private static RequestTally TallyOf(IEnumerable<string> paths)
        {
            var asked = paths.ToList();

            return new RequestTally(
                asked.Count(path => path.Contains(SearchEndpoint, StringComparison.Ordinal)),
                asked.Count(path => path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal)),
                asked.Count(path => path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal)));
        }

        // --- A Portfolio whose Features are sized by the Work Items that link to them ---

        private const string TheQueryThatFindsTheTeamsWorkItems = "project = CHILDWORK";

        private const string TheQueryThatFindsThePortfoliosFeatures = "project = FEATUREWORK";

        /// <summary>
        /// The Portfolio's Features and how many Work Items link to each. Three different counts, because
        /// a child count read off the wrong Feature comes out right by accident whenever they all match.
        /// </summary>
        private static readonly (string Feature, int Children)[] TheFeaturesAndHowManyItemsLinkToEach =
            [("FEAT-1", 1), ("FEAT-2", 2), ("FEAT-3", 3)];

        private void APortfolioOfFeaturesWhoseChildrenLinkToThemBy(JiraLinkType linkType)
        {
            var features = new List<string>();
            var children = new List<string>();

            foreach (var (feature, childCount) in TheFeaturesAndHowManyItemsLinkToEach)
            {
                features.Add(AnIssue(feature, string.Empty));

                for (var child = 1; child <= childCount; child++)
                {
                    children.Add(AnIssue($"{feature}-{child}", string.Empty, linkType.LinkWhoseOutwardIssueIs(feature)));
                }
            }

            whatEachQueryFinds[TheQueryThatFindsThePortfoliosFeatures] = features;
            whatEachQueryFinds[TheQueryThatFindsTheTeamsWorkItems] = children;
        }

        /// <summary>
        /// The administrator fills the box in, on both grains, between two refreshes - which is the only
        /// way to see the before and the after of one instance.
        /// </summary>
        private void TheAdministratorNamesTheLinkTypeOnBothGrains(JiraLinkType linkType)
        {
            whatTheParentOverrideNames = linkType.Name;
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(TheTeamDeliveringTheWork());
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(ThePortfolioBeingSized());
        }

        /// <summary>
        /// One cycle of each grain, in the order the update runs them. What is left under a Feature is the
        /// sum of its Teams' open work, so the Portfolio can only be sized once the Team has stored some.
        /// </summary>
        private async Task TheTeamAndThePortfolioAreRefreshed()
        {
            await TheUpdateAsOneRefreshRunsIt().UpdateWorkItemsForTeam(TheTeamDeliveringTheWork());
            await TheUpdateAsOneRefreshRunsIt().UpdateFeaturesForPortfolio(ThePortfolioBeingSized());
        }

        /// <summary>
        /// The real remaining-work pass over the real Jira connector, with lists standing in for the
        /// database. A connector per call, because a refresh builds one and whatever it remembered about
        /// the instance may not outlive it.
        /// </summary>
        private WorkItemService TheUpdateAsOneRefreshRunsIt()
            => new WorkItemServiceTestBuilder()
                .WithConnector(JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object))
                .WithFeatureRepository(StorageForTheFeatures())
                .WithWorkItemRepository(StorageForTheWorkItems())
                .WithTeamRepository(TheOnlyTeamOnTheInstance())
                .Build();

        private Team TheTeamDeliveringTheWork()
        {
            if (theTeamDeliveringTheWork is null)
            {
                theTeamDeliveringTheWork = JiraConnectorTestSetup.ATeamOnJiraCloud();
                theTeamDeliveringTheWork.DataRetrievalValue = TheQueryThatFindsTheTeamsWorkItems;
            }

            return theTeamDeliveringTheWork;
        }

        private Portfolio ThePortfolioBeingSized()
        {
            if (thePortfolioBeingSized is null)
            {
                thePortfolioBeingSized = JiraConnectorTestSetup.APortfolioOnJiraCloud();
                thePortfolioBeingSized.DataRetrievalValue = TheQueryThatFindsThePortfoliosFeatures;
            }

            return thePortfolioBeingSized;
        }

        private IWorkItemRepository StorageForTheWorkItems()
        {
            var storage = new Mock<IWorkItemRepository>();

            storage
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> matches)
                    => whatStorageHoldsForTheWorkItems.Where(matches.Compile()).AsQueryable());

            storage
                .Setup(repository => repository.Add(It.IsAny<WorkItem>()))
                .Callback((WorkItem item) => whatStorageHoldsForTheWorkItems.Add(item));

            return storage.Object;
        }

        private IRepository<Feature> StorageForTheFeatures()
        {
            var storage = new Mock<IRepository<Feature>>();

            storage
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> matches) => whatStorageHoldsForTheFeatures.Find(feature => matches(feature)));

            storage
                .Setup(repository => repository.Add(It.IsAny<Feature>()))
                .Callback((Feature feature) => whatStorageHoldsForTheFeatures.Add(feature));

            return storage.Object;
        }

        /// <summary>
        /// The one Team on the instance. While the Features have no children of their own, the Portfolio's
        /// default size is split across whoever this answers with, and an instance with nobody on it makes
        /// the before-state read as an empty Portfolio rather than as an unsized one.
        /// </summary>
        private IRepository<Team> TheOnlyTeamOnTheInstance()
        {
            var storage = new Mock<IRepository<Team>>();
            storage.Setup(repository => repository.GetAll()).Returns(() => new List<Team> { TheTeamDeliveringTheWork() });

            return storage.Object;
        }

        private List<string> WhichFeaturesAreSizedByThePortfolioDefault()
            => ThePortfolioBeingSized().Features
                .FindAll(feature => feature.IsUsingDefaultFeatureSize)
                .ConvertAll(feature => feature.ReferenceId);

        private Dictionary<string, int> HowManyChildrenEachFeatureEndedUpWith()
            => ThePortfolioBeingSized().Features.ToDictionary(
                feature => feature.ReferenceId,
                feature => feature.FeatureWork.Sum(work => work.TotalWorkItems));

        private static Dictionary<string, int> HowManyItemsLinkToEachFeature()
            => TheFeaturesAndHowManyItemsLinkToEach.ToDictionary(pair => pair.Feature, pair => pair.Children);

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

        private void TheRefreshWarnedTheOperatorAboutIt()
            => whatTheRefreshWroteToTheLog.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                "A refresh that stopped part way leaves an instance whose hierarchy simply stops moving, and nobody watches a connection that has never reported anything wrong - the log is the only place that turns it into something to act on.");

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
                    (request, _) => Task.FromResult(AnAnswerTo(
                        request.RequestUri?.AbsolutePath ?? string.Empty,
                        Uri.UnescapeDataString(request.RequestUri?.Query ?? string.Empty))));

            return mock.Object;
        }

        private HttpResponseMessage AnAnswerTo(string path, string query)
        {
            whatTheRefreshesAskedFor.Add(path);

            if (!theCredentialIsStillAccepted && path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"errorMessages\":[\"Client must be authenticated\"]}", Encoding.UTF8, "application/json"),
                };
            }

            var body = path switch
            {
                _ when path.EndsWith(ServerInfoEndpoint, StringComparison.Ordinal) => "{\"deploymentType\":\"Cloud\"}",
                _ when path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.EndsWith(FieldListEndpoint, StringComparison.Ordinal) => TheFieldsItDefines(),
                _ when path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal) => TheLinkTypesItDefines(),
                _ when path.Contains(SearchEndpoint, StringComparison.Ordinal) => TheIssuesItServes(query),
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private string TheIssuesItServes(string query)
        {
            foreach (var (whatTheQueryAsksFor, issues) in whatEachQueryFinds)
            {
                if (query.Contains(whatTheQueryAsksFor, StringComparison.Ordinal))
                {
                    return OnePageOf(issues);
                }
            }

            return OnePageOf(issuesTheInstanceServes);
        }

        private static string OnePageOf(List<string> issues)
            => "{\"issues\":[" + string.Join(",", issues) + "],\"isLast\":true}";

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

        /// <summary>
        /// The link-type list as a live Cloud instance answered it: an object keyed issueLinkTypes. A caller
        /// whose credential is not accepted is answered as a stranger would be - 200, and nothing in it -
        /// rather than refused, which is the whole reason an empty list cannot be read on its own.
        /// </summary>
        private string TheLinkTypesItDefines()
        {
            if (!theCredentialIsStillAccepted)
            {
                return "{\"issueLinkTypes\":[]}";
            }

            var linkTypes = linkTypesTheInstanceDefines.Select((linkType, index) =>
                "{\"id\":\"" + (10000 + index).ToString(CultureInfo.InvariantCulture) + "\""
                + ",\"name\":\"" + linkType.Name + "\""
                + ",\"inward\":\"" + linkType.Inward + "\""
                + ",\"outward\":\"" + linkType.Outward + "\"}");

            return "{\"issueLinkTypes\":[" + string.Join(",", linkTypes) + "]}";
        }

        /// <summary>
        /// What one refresh asked the instance for, by endpoint. The link-type read and the credential
        /// probe are counted apart from the search deliberately: they are the two round trips this feature
        /// genuinely adds, and folding them into the search count is how an extra request stops being
        /// visible to the scenario that was written to see it.
        /// </summary>
        private readonly record struct RequestTally(int Searches, int LinkTypeReads, int CredentialChecks);
    }
}
