using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Domain;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Platform.Host;
using Microsoft.Extensions.Logging;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application
{
    /// <summary>
    /// Default Feature sizing is the Portfolio owner's choice whether Lighthouse invents work for a Feature
    /// that has no child Work Items. The owner makes it in the Portfolio's settings (the same payload the
    /// settings form saves) and the next refresh of the Portfolio honours it, so every scenario here saves
    /// settings the way the form does and then refreshes the Portfolio through the service.
    /// </summary>
    [TestFixture]
    public class DefaultFeatureSizingTest
    {
        private const int OceanDefaultFeatureSize = 4;
        private const int TheEstimateTheTrackerHolds = 9;
        private const string AnOverrideState = "Analysis in Progress";

        [Test]
        [TestCase(Sizing.Untouched)]
        [TestCase(Sizing.SwitchedOn)]
        public async Task A_Feature_without_child_Work_Items_is_sized_by_the_default_while_default_sizing_is_on(Sizing sizing)
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(sizing);
            ocean.HasAFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();

            ocean.TheFeatureCarries(remaining: OceanDefaultFeatureSize, acrossTeams: 2, sizedByTheDefault: true);
        }

        [Test]
        public async Task A_Feature_without_child_Work_Items_takes_the_tracker_estimate_while_default_sizing_is_on()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOn);
            ocean.HasAFeatureWithoutChildWorkItems(estimate: TheEstimateTheTrackerHolds);

            await ocean.IsRefreshed();

            ocean.TheFeatureCarries(remaining: TheEstimateTheTrackerHolds, acrossTeams: 2, sizedByTheDefault: true);
        }

        [Test]
        public async Task A_Feature_in_an_override_state_with_one_child_is_sized_by_the_default_while_default_sizing_is_on()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOn).WithOverrideState(AnOverrideState);
            ocean.HasAFeatureInState(AnOverrideState).WithChildWorkItems(1);

            await ocean.IsRefreshed();

            ocean.TheFeatureCarries(remaining: OceanDefaultFeatureSize, acrossTeams: 1, sizedByTheDefault: true);
        }

        [Test]
        [TestCase(0)]
        [TestCase(TheEstimateTheTrackerHolds)]
        public async Task A_Feature_without_child_Work_Items_counts_as_zero_for_every_Team_while_default_sizing_is_off(int estimateTheTrackerHolds)
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOff);
            ocean.HasAFeatureWithoutChildWorkItems(estimate: estimateTheTrackerHolds);

            await ocean.IsRefreshed();

            ocean.TheFeatureCarriesNoWorkForAnyTeam();
        }

        [Test]
        public async Task A_finished_Feature_without_child_Work_Items_stays_empty_while_default_sizing_is_off()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOff);
            ocean.HasAFinishedFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();

            ocean.TheFeatureCarriesNoWorkForAnyTeam();
        }

        [Test]
        public async Task A_Feature_without_child_Work_Items_counts_as_zero_when_the_default_comes_from_history_while_default_sizing_is_off()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOff).CalculatingTheDefaultFromHistory();
            ocean.HasAFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();

            ocean.TheFeatureCarriesNoWorkForAnyTeam();
        }

        [Test]
        public async Task The_forecast_takes_a_Feature_without_child_Work_Items_as_zero_while_default_sizing_is_off()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOff);
            ocean.HasAFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();
            await ocean.IsForecast();

            ocean.TheFeatureIsForecastToNeedNoTime();
        }

        [Test]
        public async Task The_Delivery_forecast_counts_a_Feature_without_child_Work_Items_as_zero_while_default_sizing_is_off()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithTwoTeams().DefaultSizing(Sizing.SwitchedOff);
            ocean.HasAFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();
            await ocean.IsForecast();

            ocean.ADeliveryHoldingTheFeatureIsExpectedToCompleteToday();
        }

        [Test]
        public void Switching_default_sizing_off_and_on_keeps_the_stored_sizing_settings()
        {
            var ocean = Ocean.WithDefaultFeatureSize(7).DefaultSizing(Sizing.SwitchedOff)
                .WithOverrideState(AnOverrideState).WithSizeEstimateFieldMappedTo(8);

            ocean.TheOwnerSavesTheSettings();
            ocean.TheStoredSizingSettingsAre(defaultFeatureSize: 7, overrideState: AnOverrideState, sizeEstimateField: 8, defaultSizingIsOn: false);

            ocean.DefaultSizing(Sizing.SwitchedOn).TheOwnerSavesTheSettings();
            ocean.TheStoredSizingSettingsAre(defaultFeatureSize: 7, overrideState: AnOverrideState, sizeEstimateField: 8, defaultSizingIsOn: true);
        }

        public enum Sizing
        {
            Untouched,
            SwitchedOn,
            SwitchedOff,
        }

        /// <summary>
        /// Portfolio Ocean as the owner configures it and as the service refreshes it, built from scratch for
        /// each scenario. The sizing switch is only ever set the way a client sets it: a field of the settings
        /// payload, absent when the client does not know it.
        /// </summary>
        private sealed class Ocean
        {
            private const string TheFeatureReference = "F-1";

            private const string TheSettingsOfAnOlderClient = """
                {
                  "id": 0,
                  "name": "Ocean",
                  "workItemTypes": ["Feature"],
                  "toDoStates": ["New"],
                  "doingStates": ["Active"],
                  "doneStates": ["Done"],
                  "overrideRealChildCountStates": [],
                  "dataRetrievalValue": "type = Feature",
                  "usePercentileToCalculateDefaultAmountOfWorkItems": false,
                  "defaultWorkItemPercentile": 85,
                  "percentileHistoryInDays": 90,
                  "defaultAmountOfWorkItemsPerFeature": 25,
                  "owningTeam": null,
                  "workTrackingSystemConnectionId": 1,
                  "serviceLevelExpectationProbability": 80,
                  "serviceLevelExpectationRange": 25,
                  "systemWIPLimit": 2,
                  "stalenessThresholdDays": 0,
                  "blockedStalenessThresholdDays": 0,
                  "doneItemsCutoffDays": 365,
                  "stateMappings": [],
                  "cycleTimeDefinitions": [],
                  "waitStates": [],
                  "estimationCategoryValues": [],
                  "involvedTeams": []
                }
                """;

            private static readonly JsonSerializerOptions AsTheRouteReadsIt = new() { PropertyNameCaseInsensitive = true };
            private static readonly JsonSerializerOptions AsTheFormReadsIt = new(JsonSerializerDefaults.Web);

            private readonly JsonObject settings = JsonNode.Parse(TheSettingsOfAnOlderClient)!.AsObject();
            private readonly List<Team> teams = [];
            private readonly List<WorkItem> workItems = [];
            private readonly Mock<IPortfolioMetricsService> history = new();
            private readonly Mock<IWorkTrackingConnector> connector = new();
            private readonly Portfolio portfolio = AJiraPortfolio();
            private int ids;
            private Feature? feature;

            private static readonly DateOnly Today = new(2026, 11, 25);
            private readonly Mock<IFeatureRepository> featureRepository = new();

            public async Task IsForecast()
            {
                var teamMetrics = new Mock<ITeamMetricsService>();
                foreach (var team in teams)
                {
                    var theTeam = team;
                    teamMetrics
                        .Setup(metrics => metrics.GetForecastThroughputStatus(theTeam, It.IsAny<ThroughputFilterMode>()))
                        .Returns(new ForecastThroughputStatus(new RunChartData(RunChartDataGenerator.GenerateRunChartData([3, 3, 3])), false, null));
                    theTeam.FeatureWIP = 1;
                }

                featureRepository.Holds(portfolio.Features);

                var forecastService = new ForecastService(
                    new RandomNumberService(),
                    Mock.Of<ILogger<ForecastService>>(),
                    teamMetrics.Object,
                    featureRepository.Object,
                    Mock.Of<IPortfolioRepository>(),
                    new NothingWaitsForAnything(),
                    new ForecastSimulation(new DrawsTheSameNumberEveryTime(), new ForecastSimulationLimits(50, ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover)),
                    TimeProvider.System);

                await forecastService.UpdateForecastsForPortfolio(portfolio, Today);
            }

            public void TheFeatureIsForecastToNeedNoTime()
            {
                Assert.That(TheRefreshedFeature().Forecast.GetProbability(85), Is.LessThanOrEqualTo(0),
                    "Feature F should be forecast to need no days, because it counts as 0 remaining Work Items and the forecast takes that as it comes. Give it no invented work, so the forecast has nothing to wait for.");
            }

            public void ADeliveryHoldingTheFeatureIsExpectedToCompleteToday()
            {
                var delivery = new Delivery("Ocean release", Today.ToDateTime(TimeOnly.MinValue), portfolio.Id);
                delivery.ReplaceFeatures([TheRefreshedFeature()]);

                var metrics = delivery.CalculateMetrics(Today, [], 85);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(100.0),
                        "A Delivery holding only Feature F should be certain to complete on time, because F counts as 0 remaining and the forecast adds no special handling for it. Leave F without invented work.");
                    Assert.That(metrics.WhenDistribution.Single().ExpectedDate.Date, Is.EqualTo(Today.ToDateTime(TimeOnly.MinValue)),
                        "A Delivery holding only Feature F should be expected to complete today, because F counts as 0 remaining. Leave F without invented work so the Delivery has nothing left to wait for.");
                }
            }

            private Ocean()
            {
            }

            public static Ocean WithDefaultFeatureSize(int size)
            {
                var ocean = new Ocean();
                ocean.settings["defaultAmountOfWorkItemsPerFeature"] = size;
                return ocean;
            }

            public Ocean WithTwoTeams()
            {
                teams.Add(ATeam("Atlantic"));
                teams.Add(ATeam("Pacific"));
                return this;
            }

            public Ocean DefaultSizing(Sizing sizing)
            {
                settings.Remove("infersDefaultFeatureSize");
                if (sizing != Sizing.Untouched)
                {
                    settings["infersDefaultFeatureSize"] = sizing == Sizing.SwitchedOn;
                }

                return this;
            }

            public Ocean WithOverrideState(string state)
            {
                settings["overrideRealChildCountStates"] = new JsonArray(state);
                return this;
            }

            public Ocean WithSizeEstimateFieldMappedTo(int additionalFieldId)
            {
                settings["sizeEstimateSource"] = "Own";
                settings["sizeEstimateAdditionalFieldDefinitionId"] = additionalFieldId;
                return this;
            }

            public Ocean CalculatingTheDefaultFromHistory()
            {
                settings["usePercentileToCalculateDefaultAmountOfWorkItems"] = true;
                history
                    .Setup(metrics => metrics.GetCycleTimeDataForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .Returns([new Feature { Id = 1 }]);
                return this;
            }

            public void HasAFeatureWithoutChildWorkItems(int estimate = 0)
            {
                feature = new Feature { ReferenceId = TheFeatureReference, EstimatedSize = estimate };
                if (estimate > 0)
                {
                    WithSizeEstimateFieldMappedTo(7);
                }
            }

            public void HasAFinishedFeatureWithoutChildWorkItems()
            {
                feature = new Feature { ReferenceId = TheFeatureReference, StateCategory = StateCategories.Done };
            }

            public Ocean HasAFeatureInState(string state)
            {
                feature = new Feature { ReferenceId = TheFeatureReference, State = state };
                return this;
            }

            public void WithChildWorkItems(int count)
            {
                for (var child = 0; child < count; child++)
                {
                    workItems.Add(new WorkItem
                    {
                        Id = ids++,
                        ReferenceId = $"child-{child}",
                        ParentReferenceId = TheFeatureReference,
                        StateCategory = StateCategories.Doing,
                        Team = teams[0],
                        TeamId = teams[0].Id,
                    });
                }
            }

            public void TheOwnerSavesTheSettings()
            {
                portfolio.SyncWithPortfolioSettings(
                    JsonSerializer.Deserialize<PortfolioSettingDto>(settings.ToJsonString(), AsTheRouteReadsIt)!,
                    Mock.Of<IRepository<Team>>());
            }

            public async Task IsRefreshed()
            {
                TheOwnerSavesTheSettings();

                var workItemRepository = new Mock<IWorkItemRepository>();
                workItemRepository
                    .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                    .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
                var teamRepository = new Mock<IRepository<Team>>();
                teamRepository.Setup(repository => repository.GetAll()).Returns(() => teams);
                var featureStateTransitions = new Mock<IFeatureStateTransitionRepository>();
                featureStateTransitions
                    .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                    .Returns(new List<FeatureStateTransition>().AsQueryable());
                connector.As<IFeatureSource>()
                    .Setup(source => source.GetFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([feature!]);

                var service = new WorkItemServiceTestBuilder()
                    .WithConnector(connector.Object)
                    .WithWorkItemRepository(workItemRepository.Object)
                    .WithPortfolioMetricsService(history.Object)
                    .WithTeamRepository(teamRepository.Object)
                    .WithFeatureStateTransitionRepository(featureStateTransitions.Object)
                    .Build();

                await service.UpdateFeaturesForPortfolio(portfolio);
            }

            public void TheFeatureCarries(int remaining, int acrossTeams, bool sizedByTheDefault)
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(remaining),
                        $"Feature F should show {remaining} remaining Work Items, because that is what its sizing rule gives it. Size it by the Portfolio's sizing settings and its real child Work Items.");
                    Assert.That(refreshed.Teams.Count(), Is.EqualTo(acrossTeams),
                        $"Feature F's remaining work should sit with {acrossTeams} Team(s), because the work is split across the Teams that carry it.");
                    Assert.That(refreshed.IsUsingDefaultFeatureSize, Is.EqualTo(sizedByTheDefault),
                        $"Feature F should {(sizedByTheDefault ? string.Empty : "not ")}be flagged as sized by the default, because that flag drives today's warning that the count is an assumption.");
                }
            }

            public void TheFeatureCarriesNoWorkForAnyTeam()
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork.Sum(work => work.RemainingWorkItems), Is.Zero,
                        "Feature F should show 0 remaining Work Items, because with default sizing off Lighthouse invents no count for a Feature without child Work Items, and forecasts take that 0 as it comes.");
                    Assert.That(refreshed.FeatureWork.Sum(work => work.TotalWorkItems), Is.Zero,
                        "Feature F should have no total either, because a total with no remaining work would read as '0 of N'.");
                    Assert.That(refreshed.Teams, Is.Empty,
                        "No Team should carry work for Feature F, because the work would be invented. Leave the Feature without work for any Team.");
                    Assert.That(refreshed.IsUsingDefaultFeatureSize, Is.False,
                        "Feature F should not be flagged as sized by the default, because no default was applied to it.");
                    Assert.That(refreshed.Size, Is.Zero,
                        "Feature F should have size 0, because it has no child Work Items.");
                }
            }

            public void TheStoredSizingSettingsAre(int defaultFeatureSize, string overrideState, int sizeEstimateField, bool defaultSizingIsOn)
            {
                var read = new PortfolioSettingDto(portfolio);
                var theSwitchTheFormReads = JsonSerializer.SerializeToNode(read, AsTheFormReadsIt)!["infersDefaultFeatureSize"]?.GetValue<bool>();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(read.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(defaultFeatureSize),
                        "The Default Feature Size should stay as saved, because the owner gets it back when switching default sizing on again.");
                    Assert.That(read.OverrideRealChildCountStates, Is.EqualTo(new[] { overrideState }),
                        "The override states should stay as saved, because they are only greyed out while the switch is off.");
                    Assert.That(read.SizeEstimateAdditionalFieldDefinitionId, Is.EqualTo(sizeEstimateField),
                        "The size-estimate field mapping should stay as saved, because it is only greyed out while the switch is off.");
                    Assert.That(theSwitchTheFormReads, Is.EqualTo(defaultSizingIsOn),
                        $"The settings the form reads should carry infersDefaultFeatureSize={defaultSizingIsOn.ToString().ToLowerInvariant()}, because that is what the owner saved. Carry the switch on the settings payload.");
                }
            }

            private Feature TheRefreshedFeature() => portfolio.Features.Single(refreshed => refreshed.ReferenceId == TheFeatureReference);

            private Team ATeam(string name)
            {
                var team = new Team { Name = name, Id = ids++ };
                team.WorkItemTypes.Add("User Story");
                team.WorkTrackingSystemConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
                return team;
            }

            private static Portfolio AJiraPortfolio()
            {
                var ocean = new Portfolio { Id = 100, Name = "Ocean" };
                ocean.WorkItemTypes.Add("Feature");
                ocean.WorkTrackingSystemConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
                ocean.UpdateTime = DateTime.UtcNow;
                return ocean;
            }
        }
    }
}
