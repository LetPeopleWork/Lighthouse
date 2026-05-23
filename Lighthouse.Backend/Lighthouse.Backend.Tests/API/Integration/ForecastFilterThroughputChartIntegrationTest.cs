using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DELIVER wave — drives US-05 (PBC chart ?view=raw|filtered query param per DDD-5),
    /// invariant #1 (default Raw behaviour preserved), US-07 (premium gate downgrade).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class ForecastFilterThroughputChartIntegrationTest
    {
        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededTeamId;
        private DateTime baselineStart;
        private DateTime baselineEnd;
        private DateTime displayStart;
        private DateTime displayEnd;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using (var setupScope = factory.Services.CreateScope())
            {
                var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();

                var seeders = setupScope.ServiceProvider.GetServices<ISeeder>();
                foreach (var seeder in seeders)
                {
                    seeder.Seed().GetAwaiter().GetResult();
                }
            }

            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 60;
            baselineEnd = DateTime.UtcNow.Date.AddDays(-1 - offsetDays);
            baselineStart = baselineEnd.AddDays(-29);
            displayEnd = DateTime.UtcNow.Date.AddDays(-offsetDays);
            displayStart = displayEnd.AddDays(-29);

            SeedTeamWithThroughput();
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

        [Test]
        public async Task GetThroughputPbc_PremiumTenantTeamWithFilterAndViewFiltered_ReturnsFilteredCounts()
        {
            client.AsTeamAdmin(seededTeamId);

            var filteredResponse = await GetPbc(view: "filtered");
            var filteredBody = await filteredResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(filteredResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), filteredBody);
                var filteredTotal = SumDataPointYValues(filteredBody);
                Assert.That(filteredTotal, Is.EqualTo(3), $"Expected the 3 User Story items to remain after the Bug-excluding filter. Body: {filteredBody}");
            }
        }

        [Test]
        public async Task GetThroughputPbc_PremiumTenantTeamWithFilterAndViewRaw_ReturnsUnfilteredCounts()
        {
            client.AsTeamAdmin(seededTeamId);

            var rawResponse = await GetPbc(view: "raw");
            var rawBody = await rawResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rawResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), rawBody);
                var rawTotal = SumDataPointYValues(rawBody);
                Assert.That(rawTotal, Is.EqualTo(5), $"Expected all 5 items unfiltered (2 Bugs + 3 User Stories). Body: {rawBody}");
            }
        }

        [Test]
        public async Task GetThroughputPbc_PremiumTenantTeamWithFilterAndQueryParamOmitted_DefaultsToRaw()
        {
            client.AsTeamAdmin(seededTeamId);

            var defaultResponse = await GetPbc(view: null);
            var defaultBody = await defaultResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), defaultBody);
                var total = SumDataPointYValues(defaultBody);
                Assert.That(total, Is.EqualTo(5), $"Omitted ?view= must preserve today's unfiltered behaviour (D1). Body: {defaultBody}");
            }
        }

        [Test]
        public async Task GetThroughputPbc_NonPremiumTenantTeamWithViewFiltered_SilentlyReturnsRaw()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            client.AsTeamAdmin(seededTeamId);

            var response = await GetPbc(view: "filtered");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var total = SumDataPointYValues(body);
                Assert.That(total, Is.EqualTo(5), $"Non-premium tenant must ignore the filter (US-07). Body: {body}");
            }
        }

        [Test]
        public async Task GetThroughput_PremiumTenantTeamWithFilter_ReturnsPerItemGranularPayloadForClientSideFilter()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await client.GetAsync(BuildRunChartUrl());
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);

                Assert.That(document.RootElement.TryGetProperty("workItemsPerUnitOfTime", out var workItemsPerUnit), Is.True, "Run Chart payload must expose workItemsPerUnitOfTime for client-side filtering (DDD-5).");

                var hasGranularWorkItem = false;
                foreach (var bucket in workItemsPerUnit.EnumerateObject())
                {
                    foreach (var item in bucket.Value.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var typeProp) && !string.IsNullOrEmpty(typeProp.GetString()))
                        {
                            hasGranularWorkItem = true;
                            break;
                        }
                    }

                    if (hasGranularWorkItem)
                    {
                        break;
                    }
                }

                Assert.That(hasGranularWorkItem, Is.True, $"Run Chart payload must carry rule-evaluable WorkItemBase data (type field) per day. Body: {body}");
            }
        }

        private Task<HttpResponseMessage> GetPbc(string? view)
        {
            var url = $"/api/latest/teams/{seededTeamId}/metrics/throughput/pbc?startDate={displayStart:O}&endDate={displayEnd:O}";
            if (view != null)
            {
                url += $"&view={view}";
            }

            return client.GetAsync(url);
        }

        private string BuildRunChartUrl()
        {
            return $"/api/latest/teams/{seededTeamId}/metrics/throughput?startDate={displayStart:O}&endDate={displayEnd:O}";
        }

        private static int SumDataPointYValues(string body)
        {
            using var document = JsonDocument.Parse(body);
            var dataPoints = document.RootElement.GetProperty("dataPoints");
            var total = 0;
            foreach (var point in dataPoints.EnumerateArray())
            {
                total += (int)point.GetProperty("yValue").GetDouble();
            }
            return total;
        }

        private void SeedTeamWithThroughput()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var ruleSet = """{"Version":1,"Conditions":[{"FieldKey":"workitem.type","Operator":"equals","Value":"Bug"}]}""";

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ForecastFilterRuleSetJson = ruleSet,
                ProcessBehaviourChartBaselineStartDate = baselineStart,
                ProcessBehaviourChartBaselineEndDate = baselineEnd,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            AddClosedWorkItem(workItemRepository, team, "Bug", displayStart.AddDays(5), referenceId: "B-1");
            AddClosedWorkItem(workItemRepository, team, "Bug", displayStart.AddDays(7), referenceId: "B-2");
            AddClosedWorkItem(workItemRepository, team, "User Story", displayStart.AddDays(3), referenceId: "S-1");
            AddClosedWorkItem(workItemRepository, team, "User Story", displayStart.AddDays(8), referenceId: "S-2");
            AddClosedWorkItem(workItemRepository, team, "User Story", displayStart.AddDays(15), referenceId: "S-3");

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private static void AddClosedWorkItem(IWorkItemRepository repository, Team team, string type, DateTime closedDate, string referenceId)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"{type} {referenceId}",
                Type = type,
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = closedDate.AddDays(-5),
                StartedDate = closedDate.AddDays(-3),
                ClosedDate = closedDate,
                Order = referenceId,
            };

            repository.Add(item);
        }
    }
}
