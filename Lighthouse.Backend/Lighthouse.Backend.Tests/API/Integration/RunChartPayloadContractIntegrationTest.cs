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
    /// <summary>
    /// Bug #5857: the run chart endpoints served the EF entity as their wire format, so converting
    /// CycleTime and WorkItemAge into methods removed both fields from every response without a
    /// single test noticing. These assertions read the serialised body rather than a C# object
    /// graph, because that is the only place the defect is visible.
    /// </summary>
    [TestFixture]
    public class RunChartPayloadContractIntegrationTest
    {
        private const string ClosedItemReferenceId = "C-1";
        private const string InProgressItemReferenceId = "P-1";
        private const string RegressionGuardTag = "regression-guard";
        private const int ClosedItemStartOffset = 3;
        private const int ClosedItemCloseOffset = 7;
        private const int InProgressItemStartOffset = 2;
        private const int WindowLengthInDays = 13;
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

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int seededTeamId;
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
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [TestCase("throughput")]
        [TestCase("arrivals")]
        [TestCase("wipOverTime")]
        public async Task GetRunChart_SeededTeam_EveryItemCarriesTheWorkItemDialogFields(string endpoint)
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);
            var items = ReadItems(document);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items, Is.Not.Empty, $"Expected the seeded items to show up in {endpoint}. Body: {body}");

                foreach (var item in items)
                {
                    foreach (var field in RequiredItemFields)
                    {
                        Assert.That(item.TryGetProperty(field, out _), Is.True, $"Item {ReferenceIdOf(item)} in {endpoint} is missing '{field}'. Body: {body}");
                    }
                }
            }
        }

        [Test]
        public async Task GetThroughput_ClosedItem_ReportsTheInclusiveDaySpanAsCycleTime()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetRunChartBody("throughput");

            using var document = JsonDocument.Parse(body);
            var closedItem = FindItem(ReadItems(document), ClosedItemReferenceId);

            Assert.That(closedItem.GetProperty("cycleTime").GetInt32(), Is.EqualTo(ExpectedClosedItemCycleTime), $"Body: {body}");
        }

        [Test]
        public async Task GetWorkInProgressOverTime_InProgressItem_ReportsItsAgeAtTheEndOfTheRange()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetRunChartBody("wipOverTime");

            using var document = JsonDocument.Parse(body);
            var inProgressItem = FindItem(ReadItems(document), InProgressItemReferenceId);

            Assert.That(inProgressItem.GetProperty("workItemAge").GetInt32(), Is.EqualTo(ExpectedInProgressItemAgeAtWindowEnd), $"Body: {body}");
        }

        [Test]
        public async Task GetThroughputAndCycleTimeData_TheSameItem_AgreeOnCycleTime()
        {
            client.AsTeamAdmin(seededTeamId);

            var throughputBody = await GetRunChartBody("throughput");
            var cycleTimeBody = await GetRunChartBody("cycleTimeData");

            using var throughputDocument = JsonDocument.Parse(throughputBody);
            using var cycleTimeDocument = JsonDocument.Parse(cycleTimeBody);

            var fromRunChart = FindItem(ReadItems(throughputDocument), ClosedItemReferenceId).GetProperty("cycleTime").GetInt32();
            var fromScatterPlot = FindItem([.. cycleTimeDocument.RootElement.EnumerateArray()], ClosedItemReferenceId).GetProperty("cycleTime").GetInt32();

            Assert.That(fromRunChart, Is.EqualTo(fromScatterPlot), $"Run chart body: {throughputBody}; Cycle time body: {cycleTimeBody}");
        }

        [TestCase("throughput")]
        [TestCase("arrivals")]
        [TestCase("wipOverTime")]
        public async Task GetRunChart_SeededTeam_KeepsTheRunChartEnvelope(string endpoint)
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetRunChartBody(endpoint);

            using var document = JsonDocument.Parse(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("history").GetInt32(), Is.EqualTo(WindowLengthInDays + 1), $"Body: {body}");
                Assert.That(document.RootElement.TryGetProperty("total", out _), Is.True, $"Body: {body}");
                Assert.That(document.RootElement.TryGetProperty("blackoutDayIndices", out _), Is.True, $"Body: {body}");
            }
        }

        [Test]
        public async Task GetThroughput_SeededTeam_CarriesTheTagsTheFilterRulesEvaluate()
        {
            client.AsTeamAdmin(seededTeamId);

            var body = await GetRunChartBody("throughput");

            using var document = JsonDocument.Parse(body);
            var tags = FindItem(ReadItems(document), ClosedItemReferenceId).GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToList();

            Assert.That(tags, Does.Contain(RegressionGuardTag), $"Body: {body}");
        }

        private async Task<string> GetRunChartBody(string endpoint)
        {
            var url = $"/api/latest/teams/{seededTeamId}/metrics/{endpoint}?startDate={windowStart:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return body;
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
    }
}
