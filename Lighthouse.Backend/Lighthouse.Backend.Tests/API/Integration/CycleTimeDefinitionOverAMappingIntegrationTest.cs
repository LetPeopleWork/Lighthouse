using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// A named cycle time and the chart scoped to it, for a Team or a Portfolio whose Doing list has states grouped by a
    /// mapping. An item's history records the mapping's name, never the states inside it, so the definition's
    /// boundaries and the chart's columns are read the way the charts show the states: the mapping once, in place of
    /// its states.
    /// </summary>
    [TestFixture(OwnerKind.Team)]
    [TestFixture(OwnerKind.Portfolio)]
    public class CycleTimeDefinitionOverAMappingIntegrationTest(OwnerKind kind)
    {

        private const string Backlog = "Backlog";
        private const string Analysis = "Analysis";
        private const string CodeReview = "Code Review";
        private const string Testing = "Testing";
        private const string Waiting = "Waiting";
        private const string Review = "Review";
        private const string Done = "Done";
        private const string Phase = "Phase";
        private const string Unlisted = "Ghost";

        private const int ReviewToDoneDefinitionId = 1;
        private const int TestingToDoneDefinitionId = 2;
        private const int AnalysisToDoneDefinitionId = 3;
        private const int AnalysisToReviewDefinitionId = 4;
        private const int CodeReviewToDoneDefinitionId = 5;
        private const int PhaseToDoneDefinitionId = 6;

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime workStart;
        private int ownerId;

        private static string[] Listed(params string[] states) => states;

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [SetUp]
        public void Init()
        {
            // Metrics are cached for the whole process by owner id and date range, and owner ids repeat once the
            // database is recreated, so every test reads its own far-away window.
            var offsetDays = (System.Threading.Interlocked.Increment(ref testDateOffset) * 400) + 160000;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            workStart = windowStart.AddDays(20);

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using var teardownScope = factory.Services.CreateScope();
            teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>().Database.EnsureDeleted();
        }

        [Test]
        public async Task NamedCycleTimeStartingAtAMapping_CountsFromWhenTheItemEnteredTheMapping()
        {
            SeedOwnerWithReviewMapping();

            Assert.That(await MedianNamedCycleTime(ReviewToDoneDefinitionId), Is.EqualTo(7),
                "The item entered Review on day 4 and Done on day 10: seven calendar days.");
        }

        [Test]
        public async Task NamedCycleTimeStartingAtOneStateInsideAMapping_CountsFromWhenTheItemEnteredTheMapping()
        {
            SeedOwnerWithReviewMapping();

            Assert.That(await MedianNamedCycleTime(TestingToDoneDefinitionId), Is.EqualTo(7),
                "Testing is grouped by Review and the history only knows Review, so the clock starts when the item entered Review.");
        }

        [Test]
        public async Task NamedCycleTimeEndingAtAMapping_StopsWhenTheItemEnteredTheMapping()
        {
            SeedOwnerWithReviewMapping();

            Assert.That(await MedianNamedCycleTime(AnalysisToReviewDefinitionId), Is.EqualTo(3),
                "The item entered Analysis on day 2 and Review on day 4: three calendar days, not the nine until Done.");
        }

        [Test]
        public async Task NamedCycleTimeAroundAMapping_KeepsTheTimeTheItemSpentInsideIt()
        {
            SeedOwnerWithReviewMapping();

            Assert.That(await MedianNamedCycleTime(AnalysisToDoneDefinitionId), Is.EqualTo(9),
                "The item entered Analysis on day 2 and Done on day 10; the six days in Review count.");
        }

        [Test]
        public async Task ChartScopedAroundAMapping_ShowsTheMappingOnceInPlaceOfItsStates()
        {
            SeedOwnerWithReviewMapping();

            var body = await ScopedChart(AnalysisToDoneDefinitionId);

            Assert.That(StateNames(body), Is.EqualTo(Listed(Analysis, Review, Waiting)), body);
        }

        [Test]
        public async Task ChartScopedAroundAMapping_GivesTheMappingTheTimeTheItemSpentInIt()
        {
            SeedOwnerWithReviewMapping();

            var body = await ScopedChart(AnalysisToDoneDefinitionId);

            Assert.That(StateBar(body, Review), Is.GreaterThan(0), body);
        }

        [Test]
        public async Task ChartScopedFromOneStateInsideAMapping_StartsAtTheMapping()
        {
            SeedOwnerWithReviewMapping();

            var body = await ScopedChart(TestingToDoneDefinitionId);

            Assert.That(StateNames(body), Is.EqualTo(Listed(Review, Waiting)), body);
        }

        [Test]
        public async Task NamedCycleTimeWithoutMappings_CountsFromTheRawStateAsBefore()
        {
            SeedOwnerWithoutMappings();

            Assert.That(await MedianNamedCycleTime(CodeReviewToDoneDefinitionId), Is.EqualTo(7),
                "The item entered Code Review on day 4 and Done on day 10.");
        }

        [Test]
        public async Task ChartScopedWithoutMappings_ShowsEveryStateAsBefore()
        {
            SeedOwnerWithoutMappings();

            var body = await ScopedChart(AnalysisToDoneDefinitionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateNames(body), Is.EqualTo(Listed(Analysis, CodeReview, Testing, Waiting)), body);
                Assert.That(StateBar(body, CodeReview), Is.GreaterThan(0), body);
                Assert.That(StateBar(body, Testing), Is.GreaterThan(0), body);
            }
        }

        [Test]
        public async Task NamedCycleTimeStartingAtAMappingWithOneStateListedNowhere_CountsFromWhenTheItemEnteredTheMapping()
        {
            SeedOwner(
                [new StateMapping { Name = Phase, States = [CodeReview, Unlisted] }],
                [(Backlog, Analysis, 2), (Analysis, Phase, 4), (Phase, Done, 10)]);

            Assert.That(await MedianNamedCycleTime(PhaseToDoneDefinitionId), Is.EqualTo(7),
                "Code Review is listed, so the charts show Phase in Doing and the definition is read over it, although Ghost is listed nowhere.");
        }

        private async Task<int> MedianNamedCycleTime(int definitionId)
        {
            var response = await client.GetAsync($"{MetricsRoute}/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}&definitionId={definitionId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var median = document.RootElement.EnumerateArray().FirstOrDefault(entry => entry.GetProperty("percentile").GetInt32() == 50);
            Assert.That(median.ValueKind, Is.EqualTo(JsonValueKind.Object), $"No 50th percentile in {body}");
            return median.GetProperty("value").GetInt32();
        }

        private async Task<string> ScopedChart(int definitionId)
        {
            var response = await client.GetAsync($"{MetricsRoute}/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}&definitionId={definitionId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        private string MetricsRoute => $"/api/latest/{(kind == OwnerKind.Team ? "teams" : "portfolios")}/{ownerId}/metrics";

        private static double StateBar(string body, string state)
        {
            using var document = JsonDocument.Parse(body);
            var row = document.RootElement.GetProperty("states").EnumerateArray()
                .FirstOrDefault(candidate => string.Equals(candidate.GetProperty("state").GetString(), state, StringComparison.OrdinalIgnoreCase));
            return row.ValueKind == JsonValueKind.Object ? row.GetProperty("totalDays").GetDouble() : 0;
        }

        private static string[] StateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.GetProperty("states").EnumerateArray().Select(row => row.GetProperty("state").GetString() ?? string.Empty)];
        }

        private static List<CycleTimeDefinition> Definitions() =>
        [
            new() { Id = ReviewToDoneDefinitionId, Name = "Review to Done", StartState = Review, EndState = Done },
            new() { Id = TestingToDoneDefinitionId, Name = "Testing to Done", StartState = Testing, EndState = Done },
            new() { Id = AnalysisToDoneDefinitionId, Name = "Analysis to Done", StartState = Analysis, EndState = Done },
            new() { Id = AnalysisToReviewDefinitionId, Name = "Analysis to Review", StartState = Analysis, EndState = Review },
            new() { Id = CodeReviewToDoneDefinitionId, Name = "Code Review to Done", StartState = CodeReview, EndState = Done },
            new() { Id = PhaseToDoneDefinitionId, Name = "Phase to Done", StartState = Phase, EndState = Done },
        ];

        // The item's history carries the mapping's name, as the connectors record it once the states are grouped.
        private void SeedOwnerWithReviewMapping()
            => SeedOwner(
                [new StateMapping { Name = Review, States = [CodeReview, Testing] }],
                [(Backlog, Analysis, 2), (Analysis, Review, 4), (Review, Done, 10)]);

        private void SeedOwnerWithoutMappings()
            => SeedOwner([], [(Backlog, Analysis, 2), (Analysis, CodeReview, 4), (CodeReview, Testing, 7), (Testing, Done, 10)]);

        private void SeedOwner(List<StateMapping> mappings, (string From, string To, int Day)[] history)
        {
            using var scope = factory.Services.CreateScope();
            var services = scope.ServiceProvider;
            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };

            if (kind == OwnerKind.Team)
            {
                var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection, DoneItemsCutoffDays = 0 };
                ownerId = SaveOwner(services.GetRequiredService<IRepository<Team>>(), team, mappings);
                SeedTeamItem(services, team, history);
                client.AsTeamAdmin(ownerId);
            }
            else
            {
                var portfolio = new Portfolio { Name = $"Portfolio {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };
                ownerId = SaveOwner(services.GetRequiredService<IRepository<Portfolio>>(), portfolio, mappings);
                SeedPortfolioFeature(services, portfolio, history);
                client.AsPortfolioAdmin(ownerId);
            }
        }

        private static int SaveOwner<TOwner>(IRepository<TOwner> repository, TOwner owner, List<StateMapping> mappings)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            owner.ToDoStates = [Backlog];
            owner.DoingStates = [Analysis, CodeReview, Testing, Waiting];
            owner.DoneStates = [Done];
            owner.StateMappings = mappings;
            owner.CycleTimeDefinitions = Definitions();
            repository.Add(owner);
            repository.Save().GetAwaiter().GetResult();
            return owner.Id;
        }

        private void SeedTeamItem(IServiceProvider services, Team team, (string From, string To, int Day)[] history)
        {
            var itemRepository = services.GetRequiredService<IWorkItemRepository>();
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "MAP-1",
                Name = "Story MAP-1",
                Type = "Story",
                State = Done,
                StateCategory = StateCategories.Done,
                Url = "https://example.test/items/MAP-1",
                CreatedDate = workStart.AddDays(-1),
                StartedDate = workStart.AddDays(2),
                ClosedDate = workStart.AddDays(10),
                Order = "MAP-1",
            };
            itemRepository.Add(item);
            itemRepository.Save().GetAwaiter().GetResult();

            var transitionRepository = services.GetRequiredService<IWorkItemStateTransitionRepository>();
            foreach (var (from, to, day) in history)
            {
                transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = from, ToState = to, TransitionedAt = workStart.AddDays(day) });
            }

            transitionRepository.Save().GetAwaiter().GetResult();
        }

        private void SeedPortfolioFeature(IServiceProvider services, Portfolio portfolio, (string From, string To, int Day)[] history)
        {
            var featureRepository = services.GetRequiredService<IRepository<Feature>>();
            var feature = new Feature
            {
                ReferenceId = "MAP-1",
                Name = "Epic MAP-1",
                Type = "Epic",
                State = Done,
                StateCategory = StateCategories.Done,
                CreatedDate = workStart.AddDays(-1),
                StartedDate = workStart.AddDays(2),
                ClosedDate = workStart.AddDays(10),
                Order = "MAP-1",
            };
            feature.Portfolios.Add(portfolio);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            var transitionRepository = services.GetRequiredService<IFeatureStateTransitionRepository>();
            foreach (var (from, to, day) in history)
            {
                transitionRepository.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = from, ToState = to, TransitionedAt = workStart.AddDays(day) });
            }

            transitionRepository.Save().GetAwaiter().GetResult();
        }
    }
}
