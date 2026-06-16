using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class FlowEfficiencyPortfolioReadApiIntegrationTest
    {
        private const string Analyzing = "Analyzing";
        private const string AwaitingApproval = "Awaiting Approval";
        private const string QueuedForBuild = "Queued for Build";
        private const string Done = "Done";

        private const double PercentTolerance = 0.6;
        private const int PortfolioWindowDisjointShiftDays = 10000;

        private static readonly string[] WorkflowDoingStates = [Analyzing, AwaitingApproval, QueuedForBuild];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;

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
            var offsetDays = (System.Threading.Interlocked.Increment(ref testDateOffset) * 400) + PortfolioWindowDisjointShiftDays;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var seeders = setupScope.ServiceProvider.GetServices<ISeeder>();
            foreach (var seeder in seeders)
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioWithKnownWaitTime_ReturnsSameShapeAndArithmeticAsTeamScope()
        {
            // US-03 parity / ADR-055: portfolio efficiency = active / total Doing-time over features in scope.
            // Total Doing 400d: Analyzing 180d (active), Awaiting Approval 120d + Queued for Build 100d (wait
            // 220d via the "Waiting" mapping) → 180/400 = 45%, identical arithmetic to the team mapping case.
            var portfolioId = SeedPortfolioWithMappingNameMarkedAsWaitState();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(InfoUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.True,
                    $"US-03 parity: a configured portfolio reports IsConfigured=true. Body: {body}");
                Assert.That(HasDataInScope(body), Is.True,
                    $"US-03 parity: with Doing-time in scope, HasDataInScope=true. Body: {body}");
                Assert.That(EfficiencyPercent(body), Is.EqualTo(45.0).Within(PercentTolerance),
                    $"US-03/D11 parity: 'Waiting' expands to both raw states (120+100=220d wait); 180/400 = 45%. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioWithNoWaitStates_ReportsNotConfiguredNeverHundredPercent()
        {
            // D3 parity: no wait states → IsConfigured=false, never 100%.
            var portfolioId = SeedPortfolioWithDoingTimeButNoWaitStates();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(InfoUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.False,
                    $"D3 parity: an unconfigured portfolio reports IsConfigured=false. Body: {body}");
                Assert.That(EfficiencyPercent(body), Is.Not.EqualTo(100.0).Within(PercentTolerance),
                    $"D3 parity: an unconfigured portfolio must NEVER read 100%. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioWithZeroDoingTimeInScope_ReportsNoData()
        {
            // D4 parity: wait states configured but zero Doing-time → HasDataInScope=false, no division error.
            var portfolioId = SeedPortfolioWithWaitStatesButNoDoingTimeInScope();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(InfoUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(HasDataInScope(body), Is.False,
                    $"D4 parity: zero Doing-time in scope reports HasDataInScope=false. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioAnonymousCaller_IsRejected()
        {
            // ADR-055: class-level RbacGuard(PortfolioRead) — an unauthenticated caller is rejected.
            var portfolioId = SeedPortfolioWithMappingNameMarkedAsWaitState();

            client.AsAnonymous();
            var response = await client.GetAsync(InfoUrl(portfolioId));

            Assert.That(
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read the portfolio Flow Efficiency tile (RbacGuard PortfolioRead). Status: {response.StatusCode}");
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioStartDateAfterEndDate_ReturnsBadRequest()
        {
            var portfolioId = SeedPortfolioWithMappingNameMarkedAsWaitState();

            client.AsPortfolioAdmin(portfolioId);
            var url = $"/api/latest/portfolios/{portfolioId}/metrics/flowEfficiencyInfo?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"ADR-055 parity: a portfolio startDate after endDate is rejected with 400. Body: {body}");
        }

        [Test]
        public async Task GetFlowEfficiency_PortfolioStartDateEqualsEndDate_IsAccepted()
        {
            var portfolioId = SeedPortfolioWithMappingNameMarkedAsWaitState();

            client.AsPortfolioAdmin(portfolioId);
            var url = $"/api/latest/portfolios/{portfolioId}/metrics/flowEfficiencyInfo?startDate={windowEnd:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"ADR-055 parity: an equal start/end date is a valid single-day window and must not be rejected. Body: {body}");
        }

        private string InfoUrl(int portfolioId)
            => $"/api/latest/portfolios/{portfolioId}/metrics/flowEfficiencyInfo?startDate={windowStart:O}&endDate={windowEnd:O}";

        private int SeedPortfolioWithMappingNameMarkedAsWaitState()
        {
            return SeedPortfolio(
                waitStates: ["Waiting"],
                stateMappings: [("Waiting", [AwaitingApproval, QueuedForBuild])],
                visits: new[]
                {
                    (Analyzing, 180.0),
                    (AwaitingApproval, 120.0),
                    (QueuedForBuild, 100.0),
                });
        }

        private int SeedPortfolioWithDoingTimeButNoWaitStates()
        {
            return SeedPortfolio(
                waitStates: [],
                stateMappings: [],
                visits: new[]
                {
                    (Analyzing, 100.0),
                    (AwaitingApproval, 80.0),
                });
        }

        private int SeedPortfolioWithWaitStatesButNoDoingTimeInScope()
        {
            return SeedPortfolio(
                waitStates: [AwaitingApproval],
                stateMappings: [],
                visits: System.Array.Empty<(string, double)>());
        }

        private int SeedPortfolio(
            string[] waitStates,
            (string Name, string[] States)[] stateMappings,
            (string State, double Days)[] visits)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolioWithConfig(sp, stateMappings);

            ApplyWaitStates(portfolio, waitStates);

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            foreach (var (state, days) in visits)
            {
                AddFeatureWithSingleDoingVisit(featureRepository, transitionRepository, portfolio, state, days);
            }

            featureRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private static void ApplyWaitStates(Portfolio portfolio, string[] waitStates)
        {
            portfolio.WaitStates = [.. waitStates];
        }

        private void AddFeatureWithSingleDoingVisit(
            IRepository<Feature> featureRepository,
            IFeatureStateTransitionRepository transitionRepository,
            Portfolio portfolio,
            string doingState,
            double days)
        {
            var enter = windowStart.AddDays(10);
            var exit = enter.AddDays(days);

            var feature = new Feature
            {
                ReferenceId = $"EPIC-{Guid.NewGuid():N}",
                Name = "Epic",
                Type = "Epic",
                State = Done,
                StateCategory = StateCategories.Done,
                CreatedDate = enter.AddDays(-1),
                StartedDate = enter,
                ClosedDate = exit,
                CurrentStateEnteredAt = null,
                Order = "1",
            };
            feature.Portfolios.Add(portfolio);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, doingState, enter);
            AddTransition(transitionRepository, feature, doingState, Done, exit);
        }

        private static void AddTransition(IFeatureStateTransitionRepository repository, Feature feature, string fromState, string toState, DateTime transitionedAt)
        {
            repository.Add(new FeatureStateTransition
            {
                FeatureId = feature.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            });
        }

        private static Portfolio AddPortfolioWithConfig(IServiceProvider sp, (string Name, string[] States)[] stateMappings)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ToDoStates = ["To Do"],
                DoingStates = [.. WorkflowDoingStates],
                DoneStates = [Done],
                StateMappings = stateMappings
                    .Select(m => new StateMapping { Name = m.Name, States = [.. m.States] })
                    .ToList(),
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        private static bool IsConfigured(string body) => Bool(body, "isConfigured");

        private static bool HasDataInScope(string body) => Bool(body, "hasDataInScope");

        private static double EfficiencyPercent(string body) => Double(body, "efficiencyPercent");

        private static bool Bool(string body, string property)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(property, out var prop)
                && prop.ValueKind == JsonValueKind.True;
        }

        private static double Double(string body, string property)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetDouble()
                : double.NaN;
        }
    }
}
