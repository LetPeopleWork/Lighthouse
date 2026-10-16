using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Staleness
{
    /// <summary>
    /// The two thresholds an owner sets: how long a Work Item may sit in one state, and how long it may
    /// stay Blocked. Zero turns its trigger off.
    /// </summary>
    public readonly record struct StalenessThresholds(int TimeInStateDays, int BlockedDays);

    /// <summary>
    /// One reason as a reader of the WIP read sees it. Whether the state name was on the wire at all is
    /// kept apart from its value, because Blocked time belongs to no state and must leave the name off.
    /// </summary>
    public sealed record ReasonSeen(string Kind, int Days, bool HasStateName, string? StateName);

    /// <summary>What the WIP read answered about one Work Item or Feature.</summary>
    public sealed record StalenessSeen(bool HasAnAnswer, bool? IsStale, List<ReasonSeen> Reasons);

    /// <summary>
    /// Production-composition harness for the staleness reads: the real ASP.NET host, real EF, the real
    /// metrics controllers and blocked decision. Only the licence port is faked. Seeding writes preconditions
    /// (Work Items, Features, when they entered their state, when they became Blocked); what is stale
    /// is only ever read back off the WIP reads.
    /// </summary>
    public abstract class StaleWorkItemsAcceptanceTest
    {
        protected const string InProgress = "In Progress";
        protected const string Review = "Review";
        protected const string BlockedState = "Blocked";

        private static int testDateOffset;

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;

        /// <summary>
        /// The day the reads ask about. Always in the past, as every range end but today's is. Noon UTC, so
        /// the day the query string carries is the same day in any host time zone.
        /// </summary>
        protected DateTime AsOf;

        protected TimeZoneInfo Zone = null!;

        protected DateOnly AsOfDay => DateOnly.FromDateTime(AsOf);

        [SetUp]
        public void Init()
        {
            var offsetDays = Interlocked.Increment(ref testDateOffset) * 90;
            AsOf = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);

            RootFactory = new TestWebApplicationFactory<Program>();

            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                }));

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            Zone = setupScope.ServiceProvider.GetRequiredService<ILighthouseClock>().Zone;
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Time, as a person counts it ---

        /// <summary>
        /// The moment a Work Item entered something so that, on <paramref name="asOfDay"/>, it has been
        /// there <paramref name="days"/> days - the entered day and the day asked about both count, as
        /// they do for Work Item Age. Noon on the instance calendar keeps the day clear of midnight.
        /// </summary>
        protected DateTime EnteredDaysBefore(int days, DateOnly asOfDay)
            => TimeZoneInfo.ConvertTimeToUtc(asOfDay.AddDays(-(days - 1)).ToDateTime(new TimeOnly(12, 0)), Zone);

        protected DateTime EnteredDaysBefore(int days) => EnteredDaysBefore(days, AsOfDay);

        // --- Seeding (preconditions only) ---

        protected int SeedTeam(StalenessThresholds thresholds)
        {
            using var scope = Factory.Services.CreateScope();

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 0,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = [InProgress, Review, BlockedState],
                DoneStates = ["Done"],
                BlockedRuleSetJson = BlockedWhileInTheBlockedState(),
                StalenessThresholdDays = thresholds.TimeInStateDays,
                BlockedStalenessThresholdDays = thresholds.BlockedDays,
            };

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        protected int SeedWorkItem(int teamId, string referenceId, string state, DateTime? enteredStateAt)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)
                ?? throw new InvalidOperationException($"Team {teamId} not found");

            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                StateCategory = StateCategories.Doing,
                CreatedDate = AsOf.AddDays(-60),
                StartedDate = AsOf.AddDays(-50),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = enteredStateAt,
            };

            var repository = sp.GetRequiredService<IWorkItemRepository>();
            repository.Add(item);
            repository.Save().GetAwaiter().GetResult();

            return item.Id;
        }

        /// <summary>The record Lighthouse keeps of a Work Item being Blocked from a moment on.</summary>
        protected void SeedBlockedSince(int workItemId, DateTime since)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkItemBlockedTransitionRepository>();

            repository.Add(new WorkItemBlockedTransition { WorkItemId = workItemId, EnteredAt = since, LeftAt = null });
            repository.Save().GetAwaiter().GetResult();
        }

        protected int SeedPortfolio(StalenessThresholds thresholds)
        {
            using var scope = Factory.Services.CreateScope();

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                StalenessThresholdDays = thresholds.TimeInStateDays,
                BlockedStalenessThresholdDays = thresholds.BlockedDays,
            };

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        protected int SeedFeature(int portfolioId, string referenceId, string state, DateTime? enteredStateAt)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = StateCategories.Doing,
                CreatedDate = AsOf.AddDays(-60),
                StartedDate = AsOf.AddDays(-50),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = enteredStateAt,
            };
            feature.Portfolios.Add(portfolio);

            var repository = sp.GetRequiredService<IRepository<Feature>>();
            repository.Add(feature);
            repository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        protected void SeedFeatureBlockedSince(int featureId, int portfolioId, DateTime since)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFeatureBlockedTransitionRepository>();

            repository.Add(new FeatureBlockedTransition { FeatureId = featureId, PortfolioId = portfolioId, EnteredAt = since, LeftAt = null });
            repository.Save().GetAwaiter().GetResult();
        }

        private static string BlockedWhileInTheBlockedState()
            => JsonSerializer.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = RuleOperators.Equals, Value = BlockedState }],
            });

        // --- Driving port: the reads a dashboard, `lh` and the MCP tools make ---

        protected Task<ReadAnswer> ReadTeamWip(int teamId, DateTime asOf)
            => Read(Client.AsTeamAdmin(teamId), $"/api/latest/teams/{teamId}/metrics/wip?asOfDate={asOf:O}");

        protected Task<ReadAnswer> ReadTeamWipWithoutADate(int teamId)
            => Read(Client.AsTeamAdmin(teamId), $"/api/latest/teams/{teamId}/metrics/wip");

        protected Task<ReadAnswer> ReadTeamBlockedItemsOn(int teamId, DateTime day)
            => Read(Client.AsTeamAdmin(teamId), $"/api/latest/teams/{teamId}/metrics/blockedItemsAtDate?date={day:O}");

        protected Task<ReadAnswer> ReadPortfolioWip(int portfolioId, DateTime asOf)
            => Read(Client.AsPortfolioAdmin(portfolioId), $"/api/latest/portfolios/{portfolioId}/metrics/wip?asOfDate={asOf:O}");

        private static async Task<ReadAnswer> Read(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return new ReadAnswer(body);
        }

        protected readonly record struct ReadAnswer(string Body);

        // --- Reading the answer back ---

        protected static int ItemCount(ReadAnswer answer)
        {
            using var document = JsonDocument.Parse(answer.Body);
            return document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement.GetArrayLength() : -1;
        }

        protected static StalenessSeen StalenessOf(ReadAnswer answer, string referenceId)
        {
            using var document = JsonDocument.Parse(answer.Body);
            var item = document.RootElement.EnumerateArray()
                .FirstOrDefault(element => element.TryGetProperty("referenceId", out var reference) && reference.GetString() == referenceId);

            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new AssertionException($"'{referenceId}' is not on the read. Body: {answer.Body}");
            }

            var hasFlag = item.TryGetProperty("isStale", out var flag);
            var hasReasons = item.TryGetProperty("staleReasons", out var reasons);

            return new StalenessSeen(
                hasFlag || hasReasons,
                hasFlag && flag.ValueKind != JsonValueKind.Null ? flag.GetBoolean() : null,
                hasReasons && reasons.ValueKind == JsonValueKind.Array
                    ? [.. reasons.EnumerateArray().Select(ReasonAsSeen)]
                    : []);
        }

        private static ReasonSeen ReasonAsSeen(JsonElement reason)
        {
            var hasStateName = reason.TryGetProperty("stateName", out var stateName);

            return new ReasonSeen(
                reason.GetProperty("kind").GetString() ?? string.Empty,
                reason.GetProperty("days").GetInt32(),
                hasStateName,
                hasStateName ? stateName.GetString() : null);
        }

        protected static ReasonSeen TimeInState(int days, string state) => new("TimeInState", days, HasStateName: true, state);

        protected static ReasonSeen BlockedFor(int days) => new("BlockedDuration", days, HasStateName: false, null);

        protected static ReasonSeen TimeInStateWhileBlocked(int days, string state) => new("TimeInStateWhileBlocked", days, HasStateName: true, state);
    }
}
