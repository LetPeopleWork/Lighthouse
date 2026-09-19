using System.Net;
using System.Text;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Step definitions for the third slice. The door pressed here is a Team refresh, as in the second
    /// slice, but the instance behind it is a different one: the items on it carry more than one link of
    /// the type the override names, which is the case the second slice's instance never contains.
    ///
    /// The two harnesses stand side by side rather than one deriving from the other because what they
    /// describe does not overlap - a scenario about which of several candidates is refused has nothing to
    /// say about which end of a link a parent sits on, and the reverse. What must not be written twice is
    /// the instance itself, and it is not: every body put on the wire comes from JiraWireFormat, so a
    /// scenario here cannot drift onto a Jira the other harnesses would not recognise.
    ///
    /// The one thing faked is the transport to Jira. Everything between the refresh and it is production
    /// code: the real connector resolves the real reference over the real field and link-type payloads and
    /// reads the real link entries off the real issue payloads.
    /// </summary>
    public partial class Slice03AmbiguityRefusalTest
    {
        private const string TheAmbiguousItem = "PROJ-7";

        private const string TheItemWithOneCandidate = "PROJ-8";

        private const string OneCandidate = "EPIC-1";

        private const string TheOtherCandidate = "EPIC-4";

        /// <summary>A second item nobody could place, so that a refresh carrying more than one of them can be described.</summary>
        private const string TheOtherAmbiguousItem = "PROJ-9";

        private const string ACandidateOfTheOtherAmbiguousItem = "EPIC-2";

        private const string TheOtherCandidateOfTheOtherAmbiguousItem = "EPIC-5";

        /// <summary>A third one, so that what a refresh reports about them is a count and not a yes or no.</summary>
        private const string AThirdAmbiguousItem = "PROJ-10";

        private const string ACandidateOfTheThirdAmbiguousItem = "EPIC-3";

        private const string TheOtherCandidateOfTheThirdAmbiguousItem = "EPIC-6";

        /// <summary>
        /// Neither of the keys the ambiguous item points at, so a parent that leaked across from it cannot
        /// be mistaken for the one this item's own single link names.
        /// </summary>
        private const string TheParentTheItemWithOneCandidateTakes = "EPIC-7";

        /// <summary>What this instance calls the items a Team delivers, which is the Team's only work item type.</summary>
        private const string TheTypeThisInstanceCallsItsWorkItems = "Story";

        /// <summary>Any row number will do; it only has to be the one the Team's setting points at.</summary>
        private const int TheAdditionalFieldTheOverridePointsAt = 4711;

        private const string ServerInfoEndpoint = JiraWireFormat.ServerInfoEndpoint;

        private const string CredentialCheckEndpoint = JiraWireFormat.CredentialCheckEndpoint;

        private const string FieldListEndpoint = JiraWireFormat.FieldListEndpoint;

        private const string IssueLinkTypeEndpoint = JiraWireFormat.IssueLinkTypeEndpoint;

        private const string SearchEndpoint = JiraWireFormat.SearchEndpoint;

        private static readonly JiraLinkType TheLinkTypeTheOverrideNames = new("Caused by", "was caused by", "causes");

        private readonly List<string> issuesTheInstanceServes = [];

        private Mock<ILogger<JiraWorkTrackingConnector>> whatTheRefreshWroteToTheLog = new();

        private string whatTheParentOverrideNames = string.Empty;

        private TestWebApplicationFactory<Program>? theHostTheRefreshRanIn;

        private WebApplicationFactory<Program>? theRunningLighthouse;

        [SetUp]
        public void ForgetTheInstanceTheLastScenarioDescribed()
        {
            issuesTheInstanceServes.Clear();
            whatTheRefreshWroteToTheLog = new Mock<ILogger<JiraWorkTrackingConnector>>();
            whatTheParentOverrideNames = string.Empty;
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

            theHostTheRefreshRanIn?.Dispose();
            theHostTheRefreshRanIn = null;
        }

        /// <summary>
        /// What an administrator typed into Parent Override Field. One Additional Field named and
        /// referenced the same way, which is how that box is filled in.
        /// </summary>
        private void TheParentOverrideNames(string reference) => whatTheParentOverrideNames = reference;

        /// <summary>
        /// An item pointing at each of these issues by a link of the type the override names. The entries
        /// alternate between the two ends because Jira writes a link once and serves it from both, so a
        /// real instance offers its candidates in a mixture of the two shapes.
        /// </summary>
        private void TheIssueHasLinksOfTheNamedTypeTo(string key, params string[] counterpartKeys)
        {
            var links = counterpartKeys.Select((counterpartKey, position) => position % 2 == 0
                ? TheLinkTypeTheOverrideNames.LinkWhoseOutwardIssueIs(counterpartKey)
                : TheLinkTypeTheOverrideNames.LinkWhoseInwardIssueIs(counterpartKey));

            issuesTheInstanceServes.Add(AnIssue(key, [.. links]));
        }

        /// <summary>
        /// One relationship recorded twice - the same issue named from each end. That is how a tidied-up
        /// tracker actually looks, and it is not an item with two parents to choose between.
        /// </summary>
        private void TheIssueHasTwoLinksOfTheNamedTypeToTheSameIssue(string key, string counterpartKey)
            => issuesTheInstanceServes.Add(AnIssue(
                key,
                TheLinkTypeTheOverrideNames.LinkWhoseOutwardIssueIs(counterpartKey),
                TheLinkTypeTheOverrideNames.LinkWhoseInwardIssueIs(counterpartKey)));

        private async Task<List<WorkItem>> TheTeamIsRefreshed()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(team);

            var connector = JiraConnectorTestSetup.AConnectorOver(
                AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return [.. await connector.GetWorkItemsForTeam(team, CancellationToken.None)];
        }

        /// <summary>
        /// The other door onto the same instance. A Portfolio fetches its own records and hangs them under
        /// their own parents, so whatever a Team refresh says about an item nobody could place has to be said
        /// here too - an administrator who only runs Portfolios would otherwise never be told at all.
        /// </summary>
        private async Task<List<Feature>> ThePortfolioIsRefreshed()
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(portfolio);

            var connector = JiraConnectorTestSetup.AConnectorOver(
                AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object);

            return await connector.GetFeaturesForProject(portfolio, CancellationToken.None);
        }

        /// <summary>
        /// The same instance, read by the whole running application rather than by a connector a fixture
        /// holds: the scheduled refresh, the work item service, the updater and the refresh log, all
        /// production code over a real database. Only the transport to Jira is faked, exactly as above -
        /// so a number that arrives in refresh history got there from links the connector really read,
        /// and not from a fixture that put it there.
        ///
        /// Returns the id of the Team whose row the scenario then reads.
        /// </summary>
        private async Task<int> TheTeamIsRefreshedByTheRunningApplication()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(team);

            var services = TheRunningLighthouse();
            SeedIntoStorage(services, team);

            await TheRefreshRuns(services, provider => provider.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id));

            return team.Id;
        }

        /// <summary>
        /// The other door onto the same refresh path. An administrator who runs Portfolios and not Teams
        /// reads the same refresh history, so the count has to reach their rows too.
        /// </summary>
        private async Task<int> ThePortfolioIsRefreshedByTheRunningApplication()
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            WhatTheAdministratorTypedIntoTheOverrideIsSetOn(portfolio);

            var services = TheRunningLighthouse();
            SeedIntoStorage(services, portfolio);

            await TheRefreshRuns(services, provider => provider.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id));

            return portfolio.Id;
        }

        /// <summary>
        /// What the most recent refresh of this entity recorded. A Portfolio refresh asks for a forecast,
        /// which writes a row of its own, so the row is looked up by what it refreshed and not by being
        /// the only one there.
        /// </summary>
        private RefreshLog TheRowTheRefreshRecorded(RefreshType type, int entityId)
        {
            using var scope = theRunningLighthouse!.Services.CreateScope();

            var recorded = scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .Where(row => row.Type == type && row.EntityId == entityId)
                .OrderByDescending(row => row.Id)
                .FirstOrDefault();

            Assert.That(recorded, Is.Not.Null, $"The refresh of {type} {entityId} recorded nothing at all.");

            return recorded!;
        }

        /// <summary>
        /// The whole application over its own database, with the real Jira connector handed to it in
        /// place of the one the factory would build. The forecast is faked because Monte Carlo is not
        /// deterministic and nothing here is about a forecast; the licence is granted because a refresh
        /// nobody is licensed for reads no links at all.
        /// </summary>
        private IServiceProvider TheRunningLighthouse()
        {
            if (theRunningLighthouse is not null)
            {
                return theRunningLighthouse.Services;
            }

            theHostTheRefreshRanIn = new TestWebApplicationFactory<Program>();

            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(JiraConnectorTestSetup.AConnectorOver(
                    AJiraAnsweringForThatInstance(), whatTheRefreshWroteToTheLog.Object));

            var licensed = new Mock<ILicenseService>();
            licensed.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            theRunningLighthouse = theHostTheRefreshRanIn.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWorkTrackingConnectorFactory>();
                services.AddScoped(_ => connectorFactory.Object);

                services.RemoveAll<ILicenseService>();
                services.AddScoped(_ => licensed.Object);

                services.RemoveAll<IForecastService>();
                services.AddScoped(_ => Mock.Of<IForecastService>());
            }));

            using var setupScope = theRunningLighthouse.Services.CreateScope();
            var database = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            database.Database.EnsureDeleted();
            database.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            return theRunningLighthouse.Services;
        }

        private static void SeedIntoStorage<TEntity>(IServiceProvider services, TEntity entity)
            where TEntity : class, IEntity
        {
            using var scope = services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();

            repository.Add(entity);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Triggers one refresh and waits for the update queue to go idle. Admission is synchronous, so
        /// the work is already active by the time the polling below starts.
        /// </summary>
        private static async Task TheRefreshRuns(IServiceProvider services, Action<IServiceProvider> trigger)
        {
            var statusStore = services.GetRequiredService<IUpdateStatusStore>();

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
        }

        private void WhatTheAdministratorTypedIntoTheOverrideIsSetOn(IWorkItemQueryOwner owner)
        {
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
            var item = refreshed.Find(refreshedItem => refreshedItem.ReferenceId == key);

            return item is null ? $"<{key} did not come back from the refresh at all>" : item.ParentReferenceId;
        }

        private static List<string> WhichItemsCameBack(List<WorkItem> refreshed)
            => refreshed.ConvertAll(item => item.ReferenceId);

        private void NothingWasWrittenToTheLogAsAWarning()
            => whatTheRefreshWroteToTheLog.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never,
                "A warning an administrator cannot act on is one they learn to scroll past, and the next one - about an item that really does have two parents to choose between - goes past with it.");

        /// <summary>
        /// The warnings a refresh wrote, in the words it wrote them. Moq keeps the arguments of every call it
        /// saw, and the third of them is the state the formatter turns into the line an administrator reads -
        /// so this is that line, rather than a description of it that could agree with a message saying
        /// nothing useful.
        /// </summary>
        private List<string> WhatTheRefreshWarnedAbout()
            => [.. whatTheRefreshWroteToTheLog.Invocations
                .Where(call => call.Arguments.Count > 2 && Equals(call.Arguments[0], LogLevel.Warning))
                .Select(call => call.Arguments[2]?.ToString() ?? string.Empty)];

        /// <summary>
        /// The single warning the refresh wrote. Counting is the point: one line per item turns a tracker
        /// where a bulk edit went wrong into hundreds of lines, and a log that long is one nobody reads to
        /// the end - so the item that really needed attention is the one that gets missed.
        /// </summary>
        private string TheOneWarningTheRefreshWrote()
        {
            var warnings = WhatTheRefreshWarnedAbout();

            Assert.That(warnings, Has.Count.EqualTo(1),
                $"A refresh with items nobody could place has exactly one thing to say about them. It said: {string.Join(" | ", warnings)}");

            return warnings[0];
        }

        /// <summary>
        /// The one warning that names a given item. A Portfolio refresh has other things it may warn about -
        /// link names that matched nothing, for one - and those are not this scenario's business; what is,
        /// is that the item nobody could place is spoken of once and not once per link.
        /// </summary>
        private string TheOneWarningNaming(string key)
        {
            var warnings = WhatTheRefreshWarnedAbout().FindAll(warning => warning.Contains(key, StringComparison.Ordinal));

            Assert.That(warnings, Has.Count.EqualTo(1),
                $"{key} could not be placed, so the refresh owes exactly one line naming it. The warnings it wrote were: {string.Join(" | ", WhatTheRefreshWarnedAbout())}");

            return warnings[0];
        }

        private static string AnIssue(string key, params string[] links)
            => JiraWireFormat.AnIssueCarrying(key, TheTypeThisInstanceCallsItsWorkItems, string.Empty, links);

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
                _ when path.EndsWith(ServerInfoEndpoint, StringComparison.Ordinal) => JiraWireFormat.ACloudDeployment,
                _ when path.EndsWith(CredentialCheckEndpoint, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.EndsWith(FieldListEndpoint, StringComparison.Ordinal) => JiraWireFormat.TheFieldListDefining([]),
                _ when path.EndsWith(IssueLinkTypeEndpoint, StringComparison.Ordinal) => TheLinkTypesItDefines(),
                _ when path.Contains(SearchEndpoint, StringComparison.Ordinal) => JiraWireFormat.OnePageOf(issuesTheInstanceServes),
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static string TheLinkTypesItDefines()
            => JiraWireFormat.TheLinkTypeListDefining([JiraWireFormat.ALinkTypeDefinition(0, TheLinkTypeTheOverrideNames)]);
    }
}
