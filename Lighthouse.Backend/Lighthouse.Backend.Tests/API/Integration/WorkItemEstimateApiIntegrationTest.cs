using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Every row a Work Items dialog can list carries the estimate the Estimation chart would plot it
    /// at, read from its owner's estimation field. These read the serialised body, because the field
    /// only exists for the browser.
    /// </summary>
    [TestFixture]
    public class WorkItemEstimateApiIntegrationTest
    {
        private const string Pending = "Not built yet: the estimate on each Work Item row. Unskip one at a time while building it.";

        private const string EstimateField = "estimate";
        private const string ValueField = "value";
        private const string DisplayValueField = "displayValue";
        private const string UnitField = "unit";
        private const string StoryPoints = "Story Points";
        private const string BlockedTag = "blocked-guard";
        private const int WindowLengthInDays = 13;

        private const string Throughput = "throughput";
        private const string Arrivals = "arrivals";
        private const string Started = "started";
        private const string WipOverTime = "wipOverTime";
        private const string CycleTimeData = "cycleTimeData";
        private const string Wip = "wip";
        private const string BlockedItemsAtDate = "blockedItemsAtDate";
        private const string AllFeaturesForSizeChart = "allFeaturesForSizeChart";
        private const string FeaturesInProgress = "featuresInProgress";

        // The estimated Portfolio's Features whose category is in its list, so the chart can place them.
        private static readonly string[] MappedFeatures = ["FTR-1", "FC-2", "FP-1"];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;

        private int estimatedTeamId;
        private int plainTeamId;
        private int estimatedPortfolioId;
        private int plainPortfolioId;
        private int parentFeatureId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
            client.AsSystemAdmin();

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

            windowEnd = DateTime.UtcNow.Date.AddHours(12);
            windowStart = windowEnd.AddDays(-WindowLengthInDays);

            SeedTwoTeamsAndTwoPortfolios();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
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

        // @us-01 @slice-01a @driving_port @real-io @kpi @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task Team_EveryItemTheEstimationChartPlots_ListsTheSameEstimateInItsRow()
        {
            await AssertEveryPlottedItemListsItsPointsEstimate($"/api/latest/teams/{estimatedTeamId}/metrics");
        }

        // @us-01 @slice-01a @driving_port @real-io @kpi @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task Portfolio_EveryFeatureTheEstimationChartPlots_ListsTheSameEstimateInItsRow()
        {
            await AssertEveryPlottedItemListsItsPointsEstimate($"/api/latest/portfolios/{estimatedPortfolioId}/metrics");
        }

        // @us-01 @slice-01a @driving_port @real-io @contract-shape:pure-function
        [TestCase(Throughput)]
        [TestCase(Arrivals)]
        [TestCase(WipOverTime)]
        [TestCase(CycleTimeData)]
        [TestCase(Wip)]
        [TestCase(BlockedItemsAtDate)]
        [Ignore(Pending)]
        public async Task Team_WithAnEstimationField_EveryRowCarriesAnEstimateInTheTeamsUnit(string endpoint)
        {
            var (rows, body) = await RowsOf($"/api/latest/teams/{estimatedTeamId}/metrics", endpoint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.Not.Empty, $"The seeded items must show up in {endpoint}. Body: {body}");
                foreach (var row in rows)
                {
                    Assert.That(TextOf(EstimateOf(row), UnitField), Is.EqualTo(StoryPoints), $"{ReferenceOf(row)} in {endpoint}. Body: {body}");
                }
            }
        }

        // @us-01 @slice-01a @driving_port @real-io @contract-shape:pure-function
        [TestCase(Throughput)]
        [TestCase(Started)]
        [TestCase(Arrivals)]
        [TestCase(WipOverTime)]
        [TestCase(CycleTimeData)]
        [TestCase(Wip)]
        [TestCase(AllFeaturesForSizeChart)]
        [TestCase(BlockedItemsAtDate)]
        [Ignore(Pending)]
        public async Task Portfolio_WithAnEstimationField_EveryFeatureCarriesAnEstimate(string endpoint)
        {
            var (rows, body) = await RowsOf($"/api/latest/portfolios/{estimatedPortfolioId}/metrics", endpoint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.Not.Empty, $"The seeded Features must show up in {endpoint}. Body: {body}");
                foreach (var row in rows)
                {
                    var estimate = EstimateOf(row);
                    Assert.That(estimate?.ValueKind, Is.EqualTo(JsonValueKind.Object), $"{ReferenceOf(row)} in {endpoint}. Body: {body}");
                    if (ReferenceOf(row) is { } reference && MappedFeatures.Contains(reference))
                    {
                        Assert.That(TextOf(estimate, DisplayValueField), Is.Not.Null, $"{ReferenceOf(row)} in {endpoint}. Body: {body}");
                        Assert.That(NumberOf(estimate, ValueField), Is.Not.Null, $"{ReferenceOf(row)} in {endpoint}. Body: {body}");
                    }
                }
            }
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [TestCase(Throughput)]
        [TestCase(CycleTimeData)]
        [TestCase(Wip)]
        [Ignore(Pending)]
        public async Task Team_WithoutAnEstimationField_EveryRowSaysSo(string endpoint)
        {
            var (rows, body) = await RowsOf($"/api/latest/teams/{plainTeamId}/metrics", endpoint);

            AssertEveryRowSaysEstimationIsNotSetUp(rows, endpoint, body);
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [TestCase(Throughput)]
        [TestCase(CycleTimeData)]
        [TestCase(Wip)]
        [Ignore(Pending)]
        public async Task Portfolio_WithoutAnEstimationField_EveryFeatureSaysSo(string endpoint)
        {
            var (rows, body) = await RowsOf($"/api/latest/portfolios/{plainPortfolioId}/metrics", endpoint);

            AssertEveryRowSaysEstimationIsNotSetUp(rows, endpoint, body);
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task Team_ItemWithNothingTheChartCouldPlace_HasAnEstimateWithoutADisplayValue()
        {
            var (rows, body) = await RowsOf($"/api/latest/teams/{estimatedTeamId}/metrics", CycleTimeData);
            var estimate = EstimateOf(FindRow(rows, "C-3", body));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(estimate, DisplayValueField), Is.EqualTo(JsonValueKind.Null), body);
                Assert.That(KindOf(estimate, ValueField), Is.EqualTo(JsonValueKind.Null), body);
                Assert.That(TextOf(estimate, UnitField), Is.EqualTo(StoryPoints), body);
            }
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task Portfolio_FeatureWithACategoryMissingFromTheList_HasAnEstimateWithoutADisplayValue()
        {
            var (rows, body) = await RowsOf($"/api/latest/portfolios/{estimatedPortfolioId}/metrics", CycleTimeData);
            var estimate = EstimateOf(FindRow(rows, "FC-3", body));

            Assert.That(KindOf(estimate, DisplayValueField), Is.EqualTo(JsonValueKind.Null), body);
        }

        // @us-01 @slice-01a @driving_port @real-io @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task FeatureChildItems_EachCarriesTheEstimateOfItsOwnTeam()
        {
            var body = await GetBody($"/api/latest/features/{parentFeatureId}/workitems");
            using var document = JsonDocument.Parse(body);
            var rows = document.RootElement.EnumerateArray().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(EstimateOf(FindRow(rows, "P-1", body)), DisplayValueField), Is.EqualTo("5"), body);
                Assert.That(EstimateOf(FindRow(rows, "P-9", body))?.ValueKind, Is.EqualTo(JsonValueKind.Null), body);
            }
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task TeamFeaturesInProgress_HaveNoSingleOwnerToAsk_SoCarryNoEstimate()
        {
            var (rows, body) = await RowsOf($"/api/latest/teams/{estimatedTeamId}/metrics", FeaturesInProgress);

            AssertEveryRowSaysEstimationIsNotSetUp(rows, FeaturesInProgress, body);
        }

        // @us-01 @slice-01a @driving_port @real-io @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task FeaturesByIds_HaveNoSingleOwnerToAsk_SoCarryNoEstimate()
        {
            var body = await GetBody($"/api/latest/features/ids?featureIds={parentFeatureId}");
            using var document = JsonDocument.Parse(body);

            AssertEveryRowSaysEstimationIsNotSetUp(document.RootElement.EnumerateArray().ToList(), "features/ids", body);
        }

        private async Task AssertEveryPlottedItemListsItsPointsEstimate(string metricsPath)
        {
            var chartBody = await GetBody($"{metricsPath}/estimationVsCycleTime?startDate={windowStart:O}&endDate={windowEnd:O}");
            using var chart = JsonDocument.Parse(chartBody);
            var points = chart.RootElement.GetProperty("dataPoints").EnumerateArray().ToList();

            var (rows, rowsBody) = await RowsOf(metricsPath, CycleTimeData);
            var rowsById = rows.ToDictionary(row => row.GetProperty("id").GetInt32());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(points, Has.Count.GreaterThanOrEqualTo(2), $"The chart must plot the seeded items. Body: {chartBody}");
                foreach (var point in points)
                {
                    foreach (var id in point.GetProperty("workItemIds").EnumerateArray().Select(element => element.GetInt32()))
                    {
                        Assert.That(rowsById.TryGetValue(id, out var row), Is.True, $"Plotted item {id} has no row. Body: {rowsBody}");
                        var estimate = EstimateOf(row);
                        var plottedLabel = TextOf(point, "estimationDisplayValue");
                        var plottedValue = NumberOf(point, "estimationNumericValue");
                        Assert.That(plottedLabel, Is.Not.Null, $"Point of {id} has no label. Body: {chartBody}");
                        Assert.That(plottedValue, Is.Not.Null, $"Point of {id} has no value. Body: {chartBody}");
                        Assert.That(TextOf(estimate, DisplayValueField), Is.EqualTo(plottedLabel), rowsBody);
                        Assert.That(NumberOf(estimate, ValueField), Is.EqualTo(plottedValue), rowsBody);
                    }
                }
            }
        }

        private static void AssertEveryRowSaysEstimationIsNotSetUp(List<JsonElement> rows, string endpoint, string body)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.Not.Empty, $"The seeded rows must show up in {endpoint}. Body: {body}");
                foreach (var row in rows)
                {
                    Assert.That(KindOf(row, EstimateField), Is.EqualTo(JsonValueKind.Null),
                        $"{ReferenceOf(row)} in {endpoint} must carry \"estimate\": null. Body: {body}");
                }
            }
        }

        // Each reader records a missing field as a failure and hands back nothing, rather than throwing,
        // so one run of a scope reports every row that is wrong instead of stopping at the first.
        private static JsonElement? EstimateOf(JsonElement? row)
        {
            var estimate = PropertyOf(row, EstimateField);
            Assert.That(estimate, Is.Not.Null, $"{ReferenceOf(row)} carries no estimate field.");
            return estimate;
        }

        private static JsonElement? PropertyOf(JsonElement? element, string field)
        {
            return element is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(field, out var property)
                ? property
                : null;
        }

        private static JsonValueKind KindOf(JsonElement? element, string field)
        {
            return PropertyOf(element, field)?.ValueKind ?? JsonValueKind.Undefined;
        }

        private static string? TextOf(JsonElement? element, string field)
        {
            return PropertyOf(element, field) is { ValueKind: JsonValueKind.String } text ? text.GetString() : null;
        }

        private static double? NumberOf(JsonElement? element, string field)
        {
            return PropertyOf(element, field) is { ValueKind: JsonValueKind.Number } number ? number.GetDouble() : null;
        }

        private static string? ReferenceOf(JsonElement? row)
        {
            return TextOf(row, "referenceId");
        }

        private static JsonElement? FindRow(List<JsonElement> rows, string referenceId, string body)
        {
            var matches = rows.Where(row => ReferenceOf(row) == referenceId).ToList();
            Assert.That(matches, Is.Not.Empty, $"No row {referenceId}. Body: {body}");
            return matches.Count > 0 ? matches[0] : null;
        }

        private async Task<(List<JsonElement> Rows, string Body)> RowsOf(string metricsPath, string endpoint)
        {
            var query = endpoint switch
            {
                Wip or FeaturesInProgress => $"asOfDate={windowEnd:O}",
                BlockedItemsAtDate => $"date={windowEnd:O}",
                _ => $"startDate={windowStart:O}&endDate={windowEnd:O}",
            };
            var body = await GetBody($"{metricsPath}/{endpoint}?{query}");
            using var document = JsonDocument.Parse(body);

            var rows = new List<JsonElement>();
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                rows.AddRange(document.RootElement.EnumerateArray().Select(row => row.Clone()));
            }
            else
            {
                foreach (var bucket in document.RootElement.GetProperty("workItemsPerUnitOfTime").EnumerateObject())
                {
                    rows.AddRange(bucket.Value.EnumerateArray().Select(row => row.Clone()));
                }
            }

            return (rows, body);
        }

        private async Task<string> GetBody(string path)
        {
            var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{path}: {body}");

            return body;
        }

        private static string BlockedRuleSetMatching(string tagsFieldKey)
        {
            return WorkItemRuleSetJson.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions =
                [
                    new WorkItemRuleCondition
                    {
                        FieldKey = tagsFieldKey,
                        Operator = RuleOperators.Contains,
                        Value = BlockedTag,
                    },
                ],
            });
        }

        private void SeedTwoTeamsAndTwoPortfolios()
        {
            using var scope = factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var estimationField = new AdditionalFieldDefinition { DisplayName = StoryPoints, Reference = "customfield_10016" };
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            connection.AdditionalFieldDefinitions.Add(estimationField);

            var estimatedTeam = new Team
            {
                Name = "Team Zenith",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                BlockedRuleSetJson = BlockedRuleSetMatching("workitem.tags"),
                EstimationUnit = StoryPoints,
            };
            var plainTeam = new Team
            {
                Name = "Team Pulsar",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
            };

            var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(estimatedTeam);
            teamRepository.Add(plainTeam);
            teamRepository.Save().GetAwaiter().GetResult();

            var fieldId = estimationField.Id;
            estimatedTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            teamRepository.Update(estimatedTeam);
            teamRepository.Save().GetAwaiter().GetResult();

            estimatedTeamId = estimatedTeam.Id;
            plainTeamId = plainTeam.Id;

            var workItemRepository = serviceProvider.GetRequiredService<IWorkItemRepository>();
            workItemRepository.Add(ClosedItem(estimatedTeam, "C-1", fieldId, "3", closesOnDay: 7));
            workItemRepository.Add(ClosedItem(estimatedTeam, "C-2", fieldId, "8", closesOnDay: 5));
            workItemRepository.Add(ClosedItem(estimatedTeam, "C-3", fieldId, "", closesOnDay: 6));
            workItemRepository.Add(InProgressItem(estimatedTeam, "P-1", fieldId, "5"));
            workItemRepository.Add(ClosedItem(plainTeam, "C-9", fieldId, "3", closesOnDay: 7));
            workItemRepository.Add(InProgressItem(plainTeam, "P-9", fieldId, "2"));
            workItemRepository.Save().GetAwaiter().GetResult();

            var estimatedPortfolio = new Portfolio
            {
                Name = "Ocean Explorer",
                WorkTrackingSystemConnection = connection,
                BlockedRuleSetJson = BlockedRuleSetMatching("feature.tags"),
                EstimationAdditionalFieldDefinitionId = fieldId,
                UseNonNumericEstimation = true,
                EstimationCategoryValues = ["S", "M", "L"],
            };
            var plainPortfolio = new Portfolio
            {
                Name = "Orion",
                WorkTrackingSystemConnection = connection,
            };

            var portfolioRepository = serviceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(estimatedPortfolio);
            portfolioRepository.Add(plainPortfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            estimatedPortfolioId = estimatedPortfolio.Id;
            plainPortfolioId = plainPortfolio.Id;

            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();
            var parentFeature = ClosedFeature(estimatedPortfolio, estimatedTeam, "FTR-1", fieldId, "M", closesOnDay: 9);
            featureRepository.Add(parentFeature);
            featureRepository.Add(ClosedFeature(estimatedPortfolio, estimatedTeam, "FC-2", fieldId, "L", closesOnDay: 8));
            featureRepository.Add(ClosedFeature(estimatedPortfolio, estimatedTeam, "FC-3", fieldId, "XXL", closesOnDay: 10));
            featureRepository.Add(InProgressFeature(estimatedPortfolio, estimatedTeam, "FP-1", fieldId, "S"));
            featureRepository.Add(ClosedFeature(plainPortfolio, plainTeam, "FC-9", fieldId, "M", closesOnDay: 9));
            featureRepository.Add(InProgressFeature(plainPortfolio, plainTeam, "FP-9", fieldId, "S"));
            featureRepository.Save().GetAwaiter().GetResult();

            parentFeatureId = parentFeature.Id;
        }

        private WorkItem ClosedItem(Team team, string referenceId, int fieldId, string estimate, int closesOnDay)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Work on {referenceId}",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(2),
                ClosedDate = windowStart.AddDays(closesOnDay),
                ParentReferenceId = "FTR-1",
                Order = referenceId,
            };
            item.AdditionalFieldValues[fieldId] = estimate;
            return item;
        }

        private WorkItem InProgressItem(Team team, string referenceId, int fieldId, string estimate)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Work on {referenceId}",
                Type = "User Story",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(3),
                ParentReferenceId = "FTR-1",
                Tags = [BlockedTag],
                Order = referenceId,
            };
            item.AdditionalFieldValues[fieldId] = estimate;
            return item;
        }

        private Feature ClosedFeature(Portfolio portfolio, Team team, string referenceId, int fieldId, string estimate, int closesOnDay)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(1),
                ClosedDate = windowStart.AddDays(closesOnDay),
                Order = referenceId,
                OwningTeam = team.Name,
            };
            feature.AdditionalFieldValues[fieldId] = estimate;
            feature.Portfolios.Add(portfolio);
            feature.FeatureWork.Add(new FeatureWork(team, 0, 3, feature));
            return feature;
        }

        private Feature InProgressFeature(Portfolio portfolio, Team team, string referenceId, int fieldId, string estimate)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart,
                StartedDate = windowStart.AddDays(2),
                Tags = [BlockedTag],
                Order = referenceId,
                OwningTeam = team.Name,
            };
            feature.AdditionalFieldValues[fieldId] = estimate;
            feature.Portfolios.Add(portfolio);
            feature.FeatureWork.Add(new FeatureWork(team, 2, 4, feature));
            return feature;
        }
    }
}
