using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL (portfolio-blocked-history, ADO #5524). Shared production-composition harness for the
    /// portfolio blocked-history acceptance suite — the portfolio twin of <see cref="BlockedItemsAcceptanceTest"/>.
    /// This is the single source of truth (Mandate-12) for HOW scenarios reach the system: through the
    /// real ASP.NET host on real SQLite (Pillar 3 — <see cref="WebApplicationFactory{TEntryPoint}"/>),
    /// via the portfolio metrics read ports, the portfolio refresh service port
    /// (<see cref="IWorkItemService.UpdateFeaturesForPortfolio"/>) and the domain-event dispatch port
    /// (<see cref="IDomainEventDispatcher"/>). Only external/non-deterministic ports are substituted:
    /// <see cref="ILicenseService"/> and the work-tracking connector boundary (per
    /// docs/architecture/atdd-infrastructure-policy.md — service-seam ATs drive the REAL WorkItemService
    /// + real EF; only the connector is faked). SQLite is mandatory: FK-dependent assertions pass
    /// identically for broken and fixed code on EF InMemory (ADR-102, ci-learnings candidate OQ-5).
    /// </summary>
    public abstract class PortfolioBlockedHistoryAcceptanceTest
    {
        private static int testDateOffset;

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected DateTime SyncDay;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 90;
            SyncDay = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);

            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => []);
            ConnectorMock
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(() => []);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(ConnectorMock.Object);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);

                        services.RemoveAll<IWorkTrackingConnectorFactory>();
                        services.AddScoped(_ => connectorFactoryMock.Object);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<Lighthouse.Backend.Services.Interfaces.Seeding.ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only — never the expected output; see Critical Rule 7 No Fixture Theater) ---

        protected SeededPortfolio SeedPortfolio(bool blockedOnState = true, bool demoConnection = false, string blockedState = "Blocked")
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo,
                Value = demoConnection ? bool.TrueString : bool.FalseString,
            });

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                BlockedRuleSetJson = blockedOnState ? BlockedByFeatureStateRuleSetJson(blockedState) : null,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress", "Blocked", "On Hold"],
                DoneStates = ["Done"],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return new SeededPortfolio(portfolio.Id, connection.Id);
        }

        protected SeededTeamForPortfolio SeedTeam()
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress", "Blocked", "On Hold"],
                DoneStates = ["Done"],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return new SeededTeamForPortfolio(team.Id, connection.Id);
        }

        private static string BlockedByFeatureStateRuleSetJson(string state)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = "feature.state", Operator = RuleOperators.Equals, Value = state },
                ],
            };

            return JsonSerializer.Serialize(ruleSet);
        }

        protected int SeedFeature(
            int portfolioId,
            string referenceId,
            string state,
            StateCategories stateCategory = StateCategories.Doing,
            DateTime? startedDate = null,
            List<string>? tags = null,
            int? additionalPortfolioId = null)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = portfolioRepository.GetById(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = stateCategory,
                CreatedDate = SyncDay.AddDays(-60),
                StartedDate = startedDate ?? SyncDay.AddDays(-30),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = SyncDay.AddDays(-10),
                Tags = tags ?? [],
            };

            feature.Portfolios.Add(portfolio);

            if (additionalPortfolioId.HasValue)
            {
                var additional = portfolioRepository.GetById(additionalPortfolioId.Value)
                    ?? throw new InvalidOperationException($"Portfolio {additionalPortfolioId} not found");
                feature.Portfolios.Add(additional);
            }

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        protected void SeedFeatureWork(int featureId, int teamId, int remainingItems = 5, int totalItems = 10)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var feature = featureRepository.GetById(featureId)
                ?? throw new InvalidOperationException($"Feature {featureId} not found");

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            var team = teamRepository.GetById(teamId)
                ?? throw new InvalidOperationException($"Team {teamId} not found");

            feature.AddOrUpdateWorkForTeam(team, remainingItems, totalItems);
            featureRepository.Save().GetAwaiter().GetResult();
        }

        protected int SeedWorkItem(
            int teamId,
            string referenceId,
            string state,
            StateCategories stateCategory = StateCategories.Doing,
            DateTime? startedDate = null)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            var team = teamRepository.GetById(teamId)
                ?? throw new InvalidOperationException($"Team {teamId} not found");

            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                StateCategory = stateCategory,
                CreatedDate = SyncDay.AddDays(-60),
                StartedDate = startedDate ?? SyncDay.AddDays(-30),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = SyncDay.AddDays(-10),
                Tags = [],
            };

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            return item.Id;
        }

        protected int SeedStandaloneFeature(string referenceId, string state, StateCategories stateCategory = StateCategories.Doing)
        {
            // A feature with NO portfolio membership — the "departed feature" shape: its spell rows
            // still reference the portfolio, but the feature itself is no longer returned by the
            // connector and is not in portfolio.Features.
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = stateCategory,
                CreatedDate = SyncDay.AddDays(-60),
                StartedDate = SyncDay.AddDays(-30),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = SyncDay.AddDays(-10),
                Tags = [],
            };

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        protected void SeedFeatureBlockedSpell(int featureId, int portfolioId, DateTime enteredAt, DateTime? leftAt)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.FeatureBlockedTransitions.Add(new FeatureBlockedTransition
            {
                FeatureId = featureId,
                PortfolioId = portfolioId,
                EnteredAt = DateTime.SpecifyKind(enteredAt, DateTimeKind.Utc),
                LeftAt = leftAt.HasValue ? DateTime.SpecifyKind(leftAt.Value, DateTimeKind.Utc) : null,
            });

            context.SaveChanges();
        }

        protected void SeedWorkItemBlockedSpell(int workItemId, DateTime enteredAt, DateTime? leftAt)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.WorkItemBlockedTransitions.Add(new WorkItemBlockedTransition
            {
                WorkItemId = workItemId,
                EnteredAt = DateTime.SpecifyKind(enteredAt, DateTimeKind.Utc),
                LeftAt = leftAt.HasValue ? DateTime.SpecifyKind(leftAt.Value, DateTimeKind.Utc) : null,
            });

            context.SaveChanges();
        }

        protected void SeedBlockedCountSnapshot(int ownerId, OwnerType ownerType, DateOnly recordedAt, int blockedCount)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.BlockedCountSnapshots.Add(new BlockedCountSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                BlockedCount = blockedCount,
            });

            context.SaveChanges();
        }

        // --- Driving-port interactions ---

        protected async Task DrivePortfolioRefresh(int portfolioId, List<Feature> connectorFeatures)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => connectorFeatures.Select(f => new Feature(f)).ToList());

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // Load the portfolio (with its connection) in the SAME scope as the service, mirroring how
            // PortfolioUpdater hands a tracked portfolio to UpdateFeaturesForPortfolio in production.
            var context = sp.GetRequiredService<LighthouseAppContext>();
            var portfolio = await context.Portfolios
                .Include(p => p.WorkTrackingSystemConnection)
                .SingleAsync(p => p.Id == portfolioId);

            var workItemService = sp.GetRequiredService<IWorkItemService>();
            await workItemService.UpdateFeaturesForPortfolio(portfolio);
        }

        protected async Task DispatchPortfolioFeaturesRefreshed(int portfolioId)
        {
            var dispatcher = Factory.Services.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.PublishAsync(new PortfolioFeaturesRefreshed(portfolioId));
        }

        protected static Feature ConnectorFeature(string referenceId, string state, StateCategories stateCategory = StateCategories.Doing, DateTime? startedDate = null)
        {
            return new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = stateCategory,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                StartedDate = startedDate ?? new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                Tags = [],
                SyncedTransitions = [],
            };
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioWip(int portfolioId, DateTime asOfDate)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/wip?asOfDate={asOfDate:O}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioBlockedItemsAtDate(int portfolioId, DateOnly date)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/blockedItemsAtDate?date={date:yyyy-MM-dd}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioBlockedCountHistory(int portfolioId, DateOnly startDate, DateOnly endDate)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/blockedCountHistory?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamWip(int teamId, DateTime asOfDate)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/wip?asOfDate={asOfDate:O}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamBlockedItemsAtDate(int teamId, DateOnly date)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/blockedItemsAtDate?date={date:yyyy-MM-dd}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamFeaturesInProgress(int teamId, DateTime asOfDate)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/featuresInProgress?asOfDate={asOfDate:O}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // --- Read-side row assertions (rows, never absence-of-throw: the dispatcher swallows handler errors) ---

        protected List<FeatureBlockedTransition> ReadFeatureSpells(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return context.FeatureBlockedTransitions
                .Where(t => t.PortfolioId == portfolioId)
                .OrderBy(t => t.EnteredAt)
                .ToList();
        }

        protected List<WorkItemBlockedTransition> ReadAllWorkItemSpells()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return context.WorkItemBlockedTransitions.ToList();
        }

        protected List<BlockedCountSnapshot> ReadBlockedCountSnapshots(int ownerId, OwnerType ownerType)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return context.BlockedCountSnapshots
                .Where(s => s.OwnerId == ownerId && s.OwnerType == ownerType)
                .OrderBy(s => s.RecordedAt)
                .ToList();
        }

        // --- JSON assertion helpers ---

        protected static JsonElement ItemByReference(string body, string referenceId)
        {
            using var document = JsonDocument.Parse(body);
            var clone = document.RootElement.Clone();
            Assert.That(clone.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Expected an item array. Body: {body}");
            foreach (var element in clone.EnumerateArray())
            {
                if (element.TryGetProperty("referenceId", out var refProp) && refProp.GetString() == referenceId)
                {
                    return element;
                }
            }

            throw new AssertionException($"Item {referenceId} not found in read surface. Body: {body}");
        }

        protected static List<string> ReferencesIn(string body)
        {
            using var document = JsonDocument.Parse(body);
            var references = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("referenceId", out var refProp))
                {
                    references.Add(refProp.GetString() ?? string.Empty);
                }
            }

            return references;
        }

        protected readonly record struct SeededPortfolio(int PortfolioId, int ConnectionId);

        protected readonly record struct SeededTeamForPortfolio(int TeamId, int ConnectionId);
    }
}
