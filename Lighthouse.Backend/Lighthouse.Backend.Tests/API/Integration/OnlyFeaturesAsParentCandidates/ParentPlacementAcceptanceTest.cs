using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates
{
    /// <summary>
    /// Acceptance harness for what a refresh does with a record whose links name more than one issue to
    /// hang it under. A scenario describes a Jira instance - its Features, its Work Items, the links between
    /// them and when each link was made - and then refreshes a Team or a Portfolio through the running
    /// application, which is the only thing that ever writes a parent.
    ///
    /// The one thing faked is the transport to Jira. The connector, the work item service, the updaters and
    /// the refresh log are production code over a real database, and what a refresh recorded is read back
    /// through Refresh History, the way an administrator reads it. The Features a Team's items are placed
    /// under are stored the way they are in production, by refreshing the Portfolio that holds them, so a
    /// Feature's created date reaches the pick from the tracker and not from a fixture.
    ///
    /// The instance is answered by query: a Team's query asks for Stories, a Portfolio's for Epics, and a
    /// refresh that only downloads what moved asks by key. A cheap refresh therefore re-reads exactly the
    /// records a scenario made move, which is what lets a scenario say what a cheap refresh did.
    ///
    /// An issue's history is listed in the order the scenario describes it, which is usually oldest first.
    /// Jira Cloud writes newest first. No scenario depends on that order: the connector replays a history by
    /// when each entry happened, and the scenarios that reverse or shuffle a history are there to prove it.
    /// </summary>
    public abstract class ParentPlacementAcceptanceTest
    {
        protected const string TheTeamName = "Payments";

        protected const string TheOtherTeamName = "Platform";

        protected const string ThePortfolioName = "Payments Q4";

        /// <summary>How the refresh log names each owner, as the administrator reads it.</summary>
        protected const string TheTeam = "Team \"Payments\"";

        protected const string ThePortfolio = "Portfolio \"Payments Q4\"";

        protected const string TheParentsOfThePortfoliosFeatures = "the parents of Portfolio \"Payments Q4\"'s Features";

        /// <summary>The phrase a Work Item reads its Feature through, and a Feature the level above it.</summary>
        protected const string TheUpwardPhrase = "is caused by";

        protected const string NoValueShown = "<the Parent Override Field shows nothing>";

        protected static readonly JiraLinkType CausedBy = new("Causes", TheUpwardPhrase, "causes");

        private static readonly JiraLinkType Relates = new("Relates", "relates to", "relates to");

        /// <summary>A Feature whose created date the scenario does not care about.</summary>
        protected static readonly DateTime SomeLongAgoDay = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);

        private const string RefreshLogRoute = "/api/latest/systeminfo/refreshlog";

        private const string WorkItemType = "Story";

        private const string FeatureType = "Epic";

        private const string TheLevelAboveType = "Initiative";

        private const string TheTeamsProject = "project = PAY";

        private const string TheOtherTeamsProject = "project = OPS";

        private const string KeyClause = "key = \"";

        private const int TheTeamsOverrideField = 4711;

        private const int TheOtherTeamsOverrideField = 4712;

        private const int ThePortfoliosOverrideField = 4713;

        private static readonly DateTime TheDayEverythingWasLastTouched = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        private readonly Dictionary<string, AnIssueOnTheTracker> issues = new(StringComparer.Ordinal);

        private readonly List<string> requestsTheTrackerAnswered = [];

        private RecordingLogger<WorkItemService> whatTheServiceWrote = new();

        private RecordingLogger<JiraWorkTrackingConnector> whatTheConnectorWrote = new();

        private List<string> warningsOfTheLastRefresh = [];

        private Team theTeam = null!;

        private Team theOtherTeam = null!;

        private Portfolio thePortfolio = null!;

        private TestWebApplicationFactory<Program>? theHost;

        private WebApplicationFactory<Program>? theRunningLighthouse;

        [SetUp]
        public void ForgetTheInstanceTheLastScenarioDescribed()
        {
            issues.Clear();
            requestsTheTrackerAnswered.Clear();
            whatTheServiceWrote = new RecordingLogger<WorkItemService>();
            whatTheConnectorWrote = new RecordingLogger<JiraWorkTrackingConnector>();
            warningsOfTheLastRefresh = [];

            theTeam = JiraConnectorTestSetup.ATeamOnJiraCloud();
            theTeam.Name = TheTeamName;
            theTeam.DataRetrievalValue = TheTeamsProject;

            theOtherTeam = JiraConnectorTestSetup.ATeamOnJiraCloud();
            theOtherTeam.Name = TheOtherTeamName;
            theOtherTeam.DataRetrievalValue = TheOtherTeamsProject;

            thePortfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            thePortfolio.Name = ThePortfolioName;
        }

        [TearDown]
        public void StopTheLighthouseThisScenarioStarted()
        {
            if (theRunningLighthouse is not null)
            {
                using (var scope = theRunningLighthouse.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
                }

                theRunningLighthouse.Dispose();
                theRunningLighthouse = null;
            }

            theHost?.Dispose();
            theHost = null;
        }

        /// <summary>A link made at a known moment, or removed at one, as the issue's history records it.</summary>
        protected sealed record LinkEvent(string Counterpart, DateTime At, bool Added, string Phrase)
        {
            public static LinkEvent Linked(string counterpart, DateTime at) => new(counterpart, at, true, TheUpwardPhrase);

            public static LinkEvent Unlinked(string counterpart, DateTime at) => new(counterpart, at, false, TheUpwardPhrase);

            /// <summary>A link of a type no parent is read from, which an issue can hold to its Feature as well.</summary>
            public static LinkEvent RelatedTo(string counterpart, DateTime at) => new(counterpart, at, true, Relates.Inward);
        }

        /// <summary>The two figures one refresh recorded, as Refresh History hands them to the screen.</summary>
        protected sealed record RefreshFigures(int? Placed, int NotPlaced, string Mode);

        protected static DateTime On(int year, int month, int day) => new(year, month, day, 9, 30, 0, DateTimeKind.Utc);

        // ----- What the administrator configured -----------------------------------------------------------

        protected void TheTeamReadsItsParentsFromTheCausedByLink()
            => TheOverrideNames(theTeam, TheTeamsOverrideField, TheUpwardPhrase);

        protected void TheOtherTeamReadsItsParentsFromTheCausedByLink()
            => TheOverrideNames(theOtherTeam, TheOtherTeamsOverrideField, TheUpwardPhrase);

        protected void ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink()
            => TheOverrideNames(thePortfolio, ThePortfoliosOverrideField, TheUpwardPhrase);

        /// <summary>
        /// The link type's own name rather than the phrase one end reads, which reads its links from both
        /// ends, so a Feature in the middle of the hierarchy sees its children as well as its parent.
        /// </summary>
        protected void ThePortfolioReadsItsFeaturesParentsFromAPhraseBothEndsShare()
            => TheOverrideNames(thePortfolio, ThePortfoliosOverrideField, CausedBy.Name);

        // ----- What the tracker holds ----------------------------------------------------------------------

        protected void ThePortfolioHoldsTheFeature(string key) => ThePortfolioHoldsTheFeature(key, SomeLongAgoDay);

        protected void ThePortfolioHoldsTheFeature(string key, DateTime createdOn)
            => TheTrackerHolds(key, FeatureType, JiraWireFormat.AnInstant(createdOn));

        /// <summary>Jira sends an empty created stamp for some migrated issues, and Lighthouse reads that as no date at all.</summary>
        protected void ThePortfolioHoldsTheFeatureWithNoCreatedDate(string key)
            => TheTrackerHolds(key, FeatureType, string.Empty);

        protected void TheTeamHoldsTheWorkItem(string key) => TheTrackerHolds(key, WorkItemType, JiraWireFormat.AnInstant(SomeLongAgoDay));

        protected void TheOtherTeamHoldsTheWorkItem(string key) => TheTrackerHolds(key, WorkItemType, JiraWireFormat.AnInstant(SomeLongAgoDay), TheOtherTeamsProject);

        /// <summary>
        /// An issue above the Portfolio's Features. No query of the Portfolio's own returns it, so it is read
        /// only when a Feature names it and the refresh fetches it by key.
        /// </summary>
        protected void TheTrackerHoldsTheIssueAbove(string key, DateTime createdOn)
            => TheTrackerHolds(key, TheLevelAboveType, JiraWireFormat.AnInstant(createdOn), owner: null);

        /// <summary>Links whose making the issue's history never recorded, as an import or an automation leaves them.</summary>
        protected void TheIssueIsCausedBy(string key, params string[] counterparts)
        {
            var issue = TheIssue(key);
            foreach (var counterpart in counterparts)
            {
                issue.UndatedLinks.Add(counterpart);
                issue.RememberTheOrderOf(counterpart);
            }
        }

        protected void TheIssueLosesItsLinkTo(string key, string counterpart)
        {
            var issue = TheIssue(key);
            issue.UndatedLinks.Remove(counterpart);
            issue.LinkHistory.Add(LinkEvent.Unlinked(counterpart, issue.Updated));
        }

        /// <summary>Links whose making, and unmaking, the issue's history recorded, in the order given.</summary>
        protected void TheIssueHasTheLinkHistory(string key, params LinkEvent[] events)
        {
            var issue = TheIssue(key);
            foreach (var linkEvent in events)
            {
                issue.LinkHistory.Add(linkEvent);
                issue.RememberTheOrderOf(linkEvent.Counterpart);
            }
        }

        /// <summary>Children reading this issue as their cause, which a reading from both ends sees as candidates too.</summary>
        protected void TheIssueCauses(string key, params string[] children)
            => TheIssue(key).DownwardLinks.AddRange(children);

        protected void TheTrackersOwnParentOf(string key, string parentKey) => TheIssue(key).OwnParent = parentKey;

        /// <summary>The same links, handed over in the opposite order, histories included, with nothing about them changed.</summary>
        protected void TheTrackerListsTheLinksInReverse(string key) => TheIssue(key).ListedInReverse = !TheIssue(key).ListedInReverse;

        /// <summary>
        /// A history long enough that the search's copy is not trusted, so the connector re-reads it on the
        /// issue's own changelog endpoint, as it already does for status changes.
        /// </summary>
        protected void TheIssuesHistoryIsTooLongForTheSearch(string key) => TheIssue(key).HistoryTooLongForTheSearch = true;

        /// <summary>The issue changed on the tracker, so the next cheap refresh downloads it again.</summary>
        protected void TheIssueMoves(string key) => TheIssue(key).Updated = TheIssue(key).Updated.AddHours(1);

        // ----- What somebody does ---------------------------------------------------------------------------

        protected Task ThePortfolioRefreshes()
            => TheRefreshRuns(provider => provider.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(thePortfolio.Id));

        protected Task TheTeamRefreshes()
            => TheRefreshRuns(provider => provider.GetRequiredService<ITeamUpdater>().TriggerUpdate(theTeam.Id));

        /// <summary>
        /// A Team in steady use, so its next refresh only downloads what moved. The first refresh on a new
        /// connection also registers the fields Jira predefines, which changes what the next one asks for,
        /// so it takes two before the cheap path is in use.
        /// </summary>
        protected async Task TheTeamIsInSteadyUse()
        {
            await TheTeamRefreshes();
            await TheTeamRefreshes();
        }

        protected async Task ThePortfolioIsInSteadyUse()
        {
            await ThePortfolioRefreshes();
            await ThePortfolioRefreshes();
        }

        protected Task TheOtherTeamRefreshes()
            => TheRefreshRuns(provider => provider.GetRequiredService<ITeamUpdater>().TriggerUpdate(theOtherTeam.Id));

        // ----- What anybody can see afterwards -------------------------------------------------------------

        protected string TheStoredParentOfTheWorkItem(string key)
        {
            var item = TheStoredWorkItem(key);
            return item is null ? $"<{key} was never stored>" : item.ParentReferenceId ?? string.Empty;
        }

        protected string TheStoredParentOfTheFeature(string key)
        {
            var feature = TheStoredFeature(key);
            return feature is null ? $"<{key} was never stored>" : feature.ParentReferenceId ?? string.Empty;
        }

        /// <summary>What the Additional Field named in Parent Override Field shows on the Work Item.</summary>
        protected string WhatTheParentOverrideFieldShowsOnTheWorkItem(string key)
            => ShownIn(TheStoredWorkItem(key), TheTeamsOverrideField);

        protected string WhatTheParentOverrideFieldShowsOnTheFeature(string key)
            => ShownIn(TheStoredFeature(key), ThePortfoliosOverrideField);

        protected Task<RefreshFigures> WhatRefreshHistoryShowsForTheTeam() => WhatRefreshHistoryShowsFor(RefreshType.Team, theTeam.Id);

        protected Task<RefreshFigures> WhatRefreshHistoryShowsForThePortfolio() => WhatRefreshHistoryShowsFor(RefreshType.Portfolio, thePortfolio.Id);

        /// <summary>Every warning the most recent refresh wrote, from the connector and from the placing alike.</summary>
        protected IReadOnlyList<string> WhatTheLastRefreshWarned() => warningsOfTheLastRefresh;

        /// <summary>
        /// The one line in which the most recent refresh named the records it placed for this owner. Each
        /// outcome costs one line per fetch, never one per record: a bulk edit that went wrong makes these by
        /// the hundred, and a line per record buries the rest of the refresh under them.
        /// </summary>
        protected string TheOneLineThatPlacedFor(string owner) => TheOneWarningStarting($"{owner} placed ");

        protected string TheOneLineThatCouldNotPlaceFor(string owner) => TheOneWarningStarting($"{owner} could not place ");

        protected int RequestsTheLastRefreshMade { get; private set; }

        // ----- The instance and the running application -----------------------------------------------------

        private static void TheOverrideNames(IWorkItemQueryOwner owner, int fieldId, string reference)
        {
            owner.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = fieldId,
                DisplayName = reference,
                Reference = reference,
            });

            owner.ParentOverrideAdditionalFieldDefinitionId = fieldId;
        }

        private void TheTrackerHolds(string key, string issueType, string created, string? owner = TheTeamsProject)
            => issues[key] = new AnIssueOnTheTracker(key, issueType, created, owner, TheDayEverythingWasLastTouched);

        private AnIssueOnTheTracker TheIssue(string key)
        {
            if (!issues.TryGetValue(key, out var issue))
            {
                Assert.Fail($"The scenario describes {key}'s links before saying the tracker holds {key} at all.");
            }

            return issue!;
        }

        private string TheOneWarningStarting(string opening)
        {
            var lines = warningsOfTheLastRefresh.FindAll(line => line.StartsWith(opening, StringComparison.Ordinal));

            Assert.That(lines, Has.Count.EqualTo(1),
                $"The refresh owes exactly one line beginning '{opening}'. It warned: {string.Join(" | ", warningsOfTheLastRefresh)}");

            return lines[0];
        }

        private WorkItem? TheStoredWorkItem(string key)
        {
            using var scope = theRunningLighthouse!.Services.CreateScope();

            return scope.ServiceProvider.GetRequiredService<IWorkItemRepository>().GetByPredicate(item => item.ReferenceId == key);
        }

        private Feature? TheStoredFeature(string key)
        {
            using var scope = theRunningLighthouse!.Services.CreateScope();

            return scope.ServiceProvider.GetRequiredService<IRepository<Feature>>().GetByPredicate(feature => feature.ReferenceId == key);
        }

        private static string ShownIn(WorkItemBase? record, int fieldId)
            => record is not null && record.AdditionalFieldValues.TryGetValue(fieldId, out var shown) && !string.IsNullOrEmpty(shown)
                ? shown
                : NoValueShown;

        /// <summary>
        /// The newest row Refresh History holds for this owner, read through the endpoint the screen reads.
        /// A Portfolio refresh asks for a forecast, which writes a row of its own, so the row is found by what
        /// it refreshed and not by being the newest one there.
        /// </summary>
        private async Task<RefreshFigures> WhatRefreshHistoryShowsFor(RefreshType type, int entityId)
        {
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            using var response = await client.GetAsync(new Uri(RefreshLogRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            using var rows = JsonDocument.Parse(body);
            var newest = rows.RootElement.ValueKind == JsonValueKind.Array
                ? rows.RootElement.EnumerateArray()
                    .Where(row => IsARowFor(row, type, entityId))
                    .OrderByDescending(row => row.GetProperty("id").GetInt32())
                    .FirstOrDefault()
                : default;

            Assert.That(newest.ValueKind, Is.EqualTo(JsonValueKind.Object),
                $"Refresh History holds no row for {type} {entityId}. It answered {(int)response.StatusCode}: {body}");

            return new RefreshFigures(
                newest.TryGetProperty("recordsPlacedFromLinksNamingMoreThanOneParent", out var placed) ? placed.GetInt32() : null,
                newest.GetProperty("recordsWhoseLinksNamedMoreThanOneParent").GetInt32(),
                newest.GetProperty("mode").ToString());
        }

        private static bool IsARowFor(JsonElement row, RefreshType type, int entityId)
        {
            var recordedType = row.GetProperty("type");
            var sameType = recordedType.ValueKind == JsonValueKind.String
                ? recordedType.GetString() == type.ToString()
                : recordedType.GetInt32() == (int)type;

            return sameType && row.GetProperty("entityId").GetInt32() == entityId;
        }

        /// <summary>
        /// Triggers one refresh and waits for the update queue to go idle, keeping what that refresh warned
        /// about and how many requests it sent. Admission is synchronous, so the work is already active by the
        /// time the polling starts.
        /// </summary>
        private async Task TheRefreshRuns(Action<IServiceProvider> trigger)
        {
            var services = TheRunningLighthouse();
            var statusStore = services.GetRequiredService<IUpdateStatusStore>();

            var serviceWarningsBefore = whatTheServiceWrote.Warnings.Count;
            var connectorWarningsBefore = whatTheConnectorWrote.Warnings.Count;
            var requestsBefore = requestsTheTrackerAnswered.Count;

            trigger(services);

            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue never went idle, so the refresh this scenario describes never finished.");
                }

                await Task.Delay(20);
            }

            warningsOfTheLastRefresh = [
                .. whatTheServiceWrote.Warnings.Skip(serviceWarningsBefore),
                .. whatTheConnectorWrote.Warnings.Skip(connectorWarningsBefore),
            ];
            RequestsTheLastRefreshMade = requestsTheTrackerAnswered.Count - requestsBefore;
        }

        /// <summary>
        /// The whole application over its own database, with the real Jira connector handed to it in place of
        /// the one the factory would build. The forecast is faked because Monte Carlo is not deterministic and
        /// nothing here is about a forecast; the licence is granted because a refresh nobody is licensed for
        /// reads no links at all. The two loggers are swapped for recording ones so a scenario reads the lines
        /// an administrator reads, in the words they were written.
        /// </summary>
        private IServiceProvider TheRunningLighthouse()
        {
            if (theRunningLighthouse is not null)
            {
                return theRunningLighthouse.Services;
            }

            theHost = new TestWebApplicationFactory<Program>();

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), whatTheConnectorWrote);
            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connector);

            var licensed = new Mock<ILicenseService>();
            licensed.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            var serviceLog = whatTheServiceWrote;

            theRunningLighthouse = theHost.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWorkTrackingConnectorFactory>();
                services.AddScoped(_ => connectorFactory.Object);

                services.RemoveAll<ILicenseService>();
                services.AddScoped(_ => licensed.Object);

                services.RemoveAll<IForecastService>();
                services.AddScoped(_ => Mock.Of<IForecastService>());

                services.AddSingleton<ILogger<WorkItemService>>(serviceLog);
            }));

            using var setupScope = theRunningLighthouse.Services.CreateScope();
            var database = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            database.Database.EnsureDeleted();
            database.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            SeedIntoStorage(setupScope.ServiceProvider, theTeam);
            SeedIntoStorage(setupScope.ServiceProvider, theOtherTeam);
            SeedIntoStorage(setupScope.ServiceProvider, thePortfolio);

            return theRunningLighthouse.Services;
        }

        private static void SeedIntoStorage<TEntity>(IServiceProvider services, TEntity entity)
            where TEntity : class, IEntity
        {
            var repository = services.GetRequiredService<IRepository<TEntity>>();

            repository.Add(entity);
            repository.Save().GetAwaiter().GetResult();
        }

        private HttpMessageHandler AJiraAnsweringForThatInstance()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(AnAnswerTo(request)));

            return mock.Object;
        }

        private HttpResponseMessage AnAnswerTo(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            lock (requestsTheTrackerAnswered)
            {
                requestsTheTrackerAnswered.Add(path);
            }

            var body = path switch
            {
                _ when path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) => JiraWireFormat.ACloudDeployment,
                _ when path.EndsWith(JiraWireFormat.CredentialCheckEndpoint, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal) => JiraWireFormat.TheFieldListDefining([]),
                _ when path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    => JiraWireFormat.TheLinkTypeListDefining([JiraWireFormat.ALinkTypeDefinition(0, CausedBy)]),
                _ when path.EndsWith("/changelog", StringComparison.Ordinal) => TheFullHistoryAt(path),
                _ when path.Contains(JiraWireFormat.SearchEndpoint, StringComparison.Ordinal)
                    => JiraWireFormat.OnePageOf(TheIssuesAskedForBy(TheQueryIn(request)).Select(issue => issue.OnTheWire())),
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static string TheQueryIn(HttpRequestMessage request)
        {
            var query = request.RequestUri?.Query ?? string.Empty;
            var jql = query.TrimStart('?').Split('&').FirstOrDefault(part => part.StartsWith("jql=", StringComparison.Ordinal)) ?? string.Empty;

            return Uri.UnescapeDataString(jql.Length > 4 ? jql[4..] : JiraConnectorTestSetup.BodyOf(request));
        }

        private List<AnIssueOnTheTracker> TheIssuesAskedForBy(string jql)
        {
            if (jql.Contains(KeyClause, StringComparison.Ordinal))
            {
                var keys = jql.Split(KeyClause, StringSplitOptions.None).Skip(1).Select(part => part[..part.IndexOf('"', StringComparison.Ordinal)]).ToHashSet(StringComparer.Ordinal);

                return [.. issues.Values.Where(issue => keys.Contains(issue.Key))];
            }

            if (jql.Contains($"\"{FeatureType}\"", StringComparison.Ordinal))
            {
                return [.. issues.Values.Where(issue => issue.IssueType == FeatureType)];
            }

            var project = jql.Contains(TheTeamsProject, StringComparison.Ordinal) ? TheTeamsProject : TheOtherTeamsProject;

            return [.. issues.Values.Where(issue => issue.IssueType == WorkItemType && issue.Owner == project)];
        }

        private string TheFullHistoryAt(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var key = segments.Length >= 2 ? segments[^2] : string.Empty;

            return JiraWireFormat.AChangelogPageHolding(issues.TryGetValue(key, out var issue) ? issue.TheHistory() : []);
        }

        /// <summary>One issue as the scenario described it, rendered the way Jira serves it each time it is asked.</summary>
        private sealed class AnIssueOnTheTracker(string key, string issueType, string created, string? owner, DateTime updated)
        {
            private readonly List<string> orderTheLinksWereDescribedIn = [];

            public string Key { get; } = key;

            public string IssueType { get; } = issueType;

            public string? Owner { get; } = owner;

            public DateTime Updated { get; set; } = updated;

            public string OwnParent { get; set; } = string.Empty;

            public List<string> UndatedLinks { get; } = [];

            public List<LinkEvent> LinkHistory { get; } = [];

            public List<string> DownwardLinks { get; } = [];

            public bool ListedInReverse { get; set; }

            public bool HistoryTooLongForTheSearch { get; set; }

            public void RememberTheOrderOf(string counterpart)
            {
                if (!orderTheLinksWereDescribedIn.Contains(counterpart))
                {
                    orderTheLinksWereDescribedIn.Add(counterpart);
                }
            }

            public List<string> TheHistory()
            {
                var history = LinkHistory.ConvertAll(linkEvent => linkEvent.Added
                    ? JiraWireFormat.ALinkAddedAt(linkEvent.At, linkEvent.Counterpart, $"This issue {linkEvent.Phrase} {linkEvent.Counterpart}")
                    : JiraWireFormat.ALinkRemovedAt(linkEvent.At, linkEvent.Counterpart, $"This issue {linkEvent.Phrase} {linkEvent.Counterpart}"));

                if (ListedInReverse)
                {
                    history.Reverse();
                }

                return history;
            }

            public string OnTheWire()
            {
                var upward = orderTheLinksWereDescribedIn.Where(StillStands).Select(CausedBy.LinkWhoseInwardIssueIs);
                var downward = DownwardLinks.Select(CausedBy.LinkWhoseOutwardIssueIs);
                var related = LinkHistory.Where(linkEvent => linkEvent.Phrase == Relates.Inward).Select(linkEvent => Relates.LinkWhoseOutwardIssueIs(linkEvent.Counterpart));
                var links = upward.Concat(downward).Concat(related).ToList();

                if (ListedInReverse)
                {
                    links.Reverse();
                }

                var history = TheHistory();
                var ownParent = OwnParent.Length == 0 ? string.Empty : ", \"parent\": {\"key\": \"" + OwnParent + "\"}";

                return JiraWireFormat.AnIssueWithItsHistory(
                    Key,
                    IssueType,
                    created,
                    JiraWireFormat.AnInstant(Updated),
                    ownParent,
                    links,
                    HistoryTooLongForTheSearch ? [] : history,
                    HistoryTooLongForTheSearch ? 31 : null);
            }

            private bool StillStands(string counterpart)
            {
                if (UndatedLinks.Contains(counterpart))
                {
                    return true;
                }

                var latest = LinkHistory
                    .Where(linkEvent => linkEvent.Counterpart == counterpart && linkEvent.Phrase == TheUpwardPhrase)
                    .MaxBy(linkEvent => linkEvent.At);

                return latest is not null && latest.Added;
            }
        }

        /// <summary>Formats a day the way the refresh log names a link's date: the UTC calendar day, invariant.</summary>
        protected static string Day(DateTime at) => at.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
