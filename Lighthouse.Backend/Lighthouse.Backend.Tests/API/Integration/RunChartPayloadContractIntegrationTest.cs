using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Bug #5857: the run chart endpoints served the EF entity as their wire format, so converting
    /// CycleTime and WorkItemAge into methods removed both fields from every response without a
    /// single test noticing. These assertions read the serialised body rather than a C# object
    /// graph, because that is the only place the defect is visible.
    /// </summary>
    [TestFixture]
    public class RunChartPayloadContractIntegrationTest
    {
        private const string ThroughputEndpoint = "throughput";
        private const string ArrivalsEndpoint = "arrivals";
        private const string StartedEndpoint = "started";
        private const string WipOverTimeEndpoint = "wipOverTime";
        private const string CycleTimeDataEndpoint = "cycleTimeData";

        private const string ClosedItemReferenceId = "C-1";
        private const string InProgressItemReferenceId = "P-1";
        private const string ClosedFeatureReferenceId = "FC-1";
        private const string InProgressFeatureReferenceId = "FP-1";
        private const string RegressionGuardTag = "regression-guard";
        private const string OwningTeamName = "Platform Group";
        private const int ClosedItemStartOffset = 3;
        private const int ClosedItemCloseOffset = 7;
        private const int InProgressItemStartOffset = 2;
        private const int WindowLengthInDays = 13;
        private const int ClosedFeatureSize = 4;
        private const int InProgressFeatureSize = 6;
        private const int ExpectedClosedItemCycleTime = ClosedItemCloseOffset - ClosedItemStartOffset + 1;
        private const int ExpectedInProgressItemAgeAtWindowEnd = WindowLengthInDays - InProgressItemStartOffset + 1;

        private static readonly string[] RequiredItemFields =
        [
            "cycleTime",
            "workItemAge",
            "isBlocked",
            "tags",
            "additionalFieldValues",
        ];

        /// <summary>
        /// A portfolio run chart holds Features, which are more than the base item. The Work Item dialog
        /// only draws its "Owned by" column when the payload carries owningTeam, so a payload serialised
        /// as the base type loses that column with no error anywhere.
        /// </summary>
        private static readonly string[] RequiredFeatureFields =
        [
            "cycleTime",
            "workItemAge",
            "isBlocked",
            "tags",
            "additionalFieldValues",
            "size",
            "owningTeam",
        ];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int seededTeamId;
        private int seededPortfolioId;
        private DateTime windowStart;
        private DateTime windowEnd;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();

            using (var setupScope = factory.Services.CreateScope())
            {
                var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();

                foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
                {
                    seeder.Seed().GetAwaiter().GetResult();
                }
            }

            // Midday rather than midnight: the seeded instants are reduced to calendar days in the
            // instance time zone, and midday lands on the same day in every zone the product supports.
            windowEnd = DateTime.UtcNow.Date.AddDays(-1).AddHours(12);
            windowStart = windowEnd.AddDays(-WindowLengthInDays);

            SeedTeamWithOneClosedAndOneInProgressItem();
            SeedPortfolioWithOneClosedAndOneInProgressFeature();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

                // Portfolio metrics are memoised per portfolio and date range, and the ids restart from 1
                // once the database is recreated, so the next test would otherwise read this test's answers.
                var portfolioMetricsService = teardownScope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>();
                foreach (var portfolio in dbContext.Portfolios.ToList())
                {
                    portfolioMetricsService.InvalidatePortfolioMetrics(portfolio);
                }

                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [TestCase(ThroughputEndpoint)]
        [TestCase(ArrivalsEndpoint)]
        [TestCase(WipOverTimeEndpoint)]
        public async Task GetRunChart_SeededTeam_EveryItemCarriesTheWorkItemDialogFields(string endpoint)
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetTeamRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);

            AssertEveryItemCarries(ReadItems(document), RequiredItemFields, endpoint, body);
        }

        [TestCase(ThroughputEndpoint)]
        [TestCase(StartedEndpoint)]
        [TestCase(ArrivalsEndpoint)]
        [TestCase(WipOverTimeEndpoint)]
        public async Task GetRunChart_SeededPortfolio_EveryFeatureCarriesTheWorkItemDialogFields(string endpoint)
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);

            AssertEveryItemCarries(ReadItems(document), RequiredFeatureFields, endpoint, body);
        }

        [Test]
        public async Task GetThroughput_ClosedItem_ReportsTheInclusiveDaySpanAsCycleTime()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetTeamRunChartBody(ThroughputEndpoint);

            using var document = JsonDocument.Parse(body);
            var closedItem = FindItem(ReadItems(document), ClosedItemReferenceId);

            Assert.That(closedItem.GetProperty("cycleTime").GetInt32(), Is.EqualTo(ExpectedClosedItemCycleTime), $"Body: {body}");
        }

        [Test]
        public async Task GetThroughput_ClosedFeature_ReportsTheInclusiveDaySpanAsCycleTime()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(ThroughputEndpoint);

            using var document = JsonDocument.Parse(body);
            var closedFeature = FindItem(ReadItems(document), ClosedFeatureReferenceId);

            Assert.That(closedFeature.GetProperty("cycleTime").GetInt32(), Is.EqualTo(ExpectedClosedItemCycleTime), $"Body: {body}");
        }

        [Test]
        public async Task GetWorkInProgressOverTime_InProgressItem_ReportsItsAgeAtTheEndOfTheRange()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetTeamRunChartBody(WipOverTimeEndpoint);

            using var document = JsonDocument.Parse(body);
            var inProgressItem = FindItem(ReadItems(document), InProgressItemReferenceId);

            Assert.That(inProgressItem.GetProperty("workItemAge").GetInt32(), Is.EqualTo(ExpectedInProgressItemAgeAtWindowEnd), $"Body: {body}");
        }

        [Test]
        public async Task GetFeaturesInProgressOverTime_InProgressFeature_ReportsItsAgeAtTheEndOfTheRange()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(WipOverTimeEndpoint);

            using var document = JsonDocument.Parse(body);
            var inProgressFeature = FindItem(ReadItems(document), InProgressFeatureReferenceId);

            Assert.That(inProgressFeature.GetProperty("workItemAge").GetInt32(), Is.EqualTo(ExpectedInProgressItemAgeAtWindowEnd), $"Body: {body}");
        }

        [Test]
        public async Task GetThroughputAndCycleTimeData_TheSameItem_AgreeOnCycleTime()
        {
            client.AsTeamAdmin(seededTeamId);

            var throughputBody = await GetTeamRunChartBody(ThroughputEndpoint);
            var cycleTimeBody = await GetTeamRunChartBody(CycleTimeDataEndpoint);

            using var throughputDocument = JsonDocument.Parse(throughputBody);
            using var cycleTimeDocument = JsonDocument.Parse(cycleTimeBody);

            var fromRunChart = FindItem(ReadItems(throughputDocument), ClosedItemReferenceId).GetProperty("cycleTime").GetInt32();
            var fromScatterPlot = FindItem([.. cycleTimeDocument.RootElement.EnumerateArray()], ClosedItemReferenceId).GetProperty("cycleTime").GetInt32();

            Assert.That(fromRunChart, Is.EqualTo(fromScatterPlot), $"Run chart body: {throughputBody}; Cycle time body: {cycleTimeBody}");
        }

        [Test]
        public async Task GetThroughputAndCycleTimeData_TheSameFeature_AgreeOnCycleTime()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var throughputBody = await GetPortfolioRunChartBody(ThroughputEndpoint);
            var cycleTimeBody = await GetPortfolioRunChartBody(CycleTimeDataEndpoint);

            using var throughputDocument = JsonDocument.Parse(throughputBody);
            using var cycleTimeDocument = JsonDocument.Parse(cycleTimeBody);

            var fromRunChart = FindItem(ReadItems(throughputDocument), ClosedFeatureReferenceId).GetProperty("cycleTime").GetInt32();
            var fromScatterPlot = FindItem([.. cycleTimeDocument.RootElement.EnumerateArray()], ClosedFeatureReferenceId).GetProperty("cycleTime").GetInt32();

            Assert.That(fromRunChart, Is.EqualTo(fromScatterPlot), $"Run chart body: {throughputBody}; Cycle time body: {cycleTimeBody}");
        }

        [TestCase(ThroughputEndpoint)]
        [TestCase(ArrivalsEndpoint)]
        [TestCase(WipOverTimeEndpoint)]
        public async Task GetRunChart_SeededTeam_KeepsTheRunChartEnvelope(string endpoint)
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetTeamRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);

            AssertRunChartEnvelope(document, body);
        }

        [TestCase(ThroughputEndpoint)]
        [TestCase(StartedEndpoint)]
        [TestCase(ArrivalsEndpoint)]
        [TestCase(WipOverTimeEndpoint)]
        public async Task GetRunChart_SeededPortfolio_KeepsTheRunChartEnvelope(string endpoint)
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);

            AssertRunChartEnvelope(document, body);
        }

        [Test]
        public async Task GetThroughput_SeededTeam_CarriesTheTagsTheFilterRulesEvaluate()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetTeamRunChartBody(ThroughputEndpoint);

            using var document = JsonDocument.Parse(body);
            var tags = FindItem(ReadItems(document), ClosedItemReferenceId).GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToList();

            Assert.That(tags, Does.Contain(RegressionGuardTag), $"Body: {body}");
        }

        [Test]
        public async Task GetThroughput_SeededPortfolio_CarriesTheSizeAndOwningTeamTheDialogColumnsRead()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(ThroughputEndpoint);

            using var document = JsonDocument.Parse(body);
            var closedFeature = FindItem(ReadItems(document), ClosedFeatureReferenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedFeature.GetProperty("size").GetInt32(), Is.EqualTo(ClosedFeatureSize), $"Body: {body}");
                Assert.That(closedFeature.GetProperty("owningTeam").GetString(), Is.EqualTo(OwningTeamName), $"Body: {body}");
            }
        }

        [Test]
        public async Task GetFeaturesInProgressOverTime_SeededPortfolio_ReportsEachFeaturesOwnSize()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var body = await GetPortfolioRunChartBody(WipOverTimeEndpoint);

            using var document = JsonDocument.Parse(body);
            var items = ReadItems(document);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FindItem(items, ClosedFeatureReferenceId).GetProperty("size").GetInt32(), Is.EqualTo(ClosedFeatureSize), $"Body: {body}");
                Assert.That(FindItem(items, InProgressFeatureReferenceId).GetProperty("size").GetInt32(), Is.EqualTo(InProgressFeatureSize), $"Body: {body}");
            }
        }

        private async Task<string> GetTeamRunChartBody(string endpoint)
        {
            return await GetBody($"/api/latest/teams/{seededTeamId}/metrics/{endpoint}");
        }

        private async Task<string> GetPortfolioRunChartBody(string endpoint)
        {
            return await GetBody($"/api/latest/portfolios/{seededPortfolioId}/metrics/{endpoint}");
        }

        private async Task<string> GetBody(string path)
        {
            var response = await client.GetAsync($"{path}?startDate={windowStart:O}&endDate={windowEnd:O}");
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return body;
        }

        private static void AssertEveryItemCarries(List<JsonElement> items, string[] requiredFields, string endpoint, string body)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(items, Is.Not.Empty, $"Expected the seeded items to show up in {endpoint}. Body: {body}");

                foreach (var item in items)
                {
                    foreach (var field in requiredFields)
                    {
                        Assert.That(item.TryGetProperty(field, out _), Is.True, $"Item {ReferenceIdOf(item)} in {endpoint} is missing '{field}'. Body: {body}");
                    }
                }
            }
        }

        private static void AssertRunChartEnvelope(JsonDocument document, string body)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("history").GetInt32(), Is.EqualTo(WindowLengthInDays + 1), $"Body: {body}");
                Assert.That(document.RootElement.TryGetProperty("total", out _), Is.True, $"Body: {body}");
                Assert.That(document.RootElement.TryGetProperty("blackoutDayIndices", out _), Is.True, $"Body: {body}");
            }
        }

        private static List<JsonElement> ReadItems(JsonDocument document)
        {
            var items = new List<JsonElement>();

            foreach (var bucket in document.RootElement.GetProperty("workItemsPerUnitOfTime").EnumerateObject())
            {
                items.AddRange(bucket.Value.EnumerateArray());
            }

            return items;
        }

        private static JsonElement FindItem(List<JsonElement> items, string referenceId)
        {
            var matches = items.Where(item => ReferenceIdOf(item) == referenceId).ToList();

            Assert.That(matches, Is.Not.Empty, $"No item with referenceId {referenceId} in the payload.");

            return matches[0];
        }

        private static string? ReferenceIdOf(JsonElement item)
        {
            return item.TryGetProperty("referenceId", out var referenceId) ? referenceId.GetString() : null;
        }

        private void SeedTeamWithOneClosedAndOneInProgressItem()
        {
            using var scope = factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

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
            };

            var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;

            var workItemRepository = serviceProvider.GetRequiredService<IWorkItemRepository>();

            workItemRepository.Add(new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = ClosedItemReferenceId,
                Name = "Closed item",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(ClosedItemStartOffset),
                ClosedDate = windowStart.AddDays(ClosedItemCloseOffset),
                Tags = [RegressionGuardTag],
                Order = ClosedItemReferenceId,
            });

            workItemRepository.Add(new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = InProgressItemReferenceId,
                Name = "In progress item",
                Type = "User Story",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(InProgressItemStartOffset),
                Tags = [RegressionGuardTag],
                Order = InProgressItemReferenceId,
            });

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private void SeedPortfolioWithOneClosedAndOneInProgressFeature()
        {
            using var scope = factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var portfolioRepository = serviceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            seededPortfolioId = portfolio.Id;

            var team = serviceProvider.GetRequiredService<IRepository<Team>>().GetById(seededTeamId)!;
            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();

            var closedFeature = new Feature
            {
                ReferenceId = ClosedFeatureReferenceId,
                Name = "Closed feature",
                Type = "Epic",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(ClosedItemStartOffset),
                ClosedDate = windowStart.AddDays(ClosedItemCloseOffset),
                Tags = [RegressionGuardTag],
                Order = ClosedFeatureReferenceId,
                OwningTeam = OwningTeamName,
            };
            closedFeature.Portfolios.Add(portfolio);
            closedFeature.FeatureWork.Add(new FeatureWork(team, 0, ClosedFeatureSize, closedFeature));
            featureRepository.Add(closedFeature);

            var inProgressFeature = new Feature
            {
                ReferenceId = InProgressFeatureReferenceId,
                Name = "In progress feature",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(InProgressItemStartOffset),
                Tags = [RegressionGuardTag],
                Order = InProgressFeatureReferenceId,
                OwningTeam = OwningTeamName,
            };
            inProgressFeature.Portfolios.Add(portfolio);
            inProgressFeature.FeatureWork.Add(new FeatureWork(team, InProgressFeatureSize, InProgressFeatureSize, inProgressFeature));
            featureRepository.Add(inProgressFeature);

            featureRepository.Save().GetAwaiter().GetResult();
        }
    }
}
