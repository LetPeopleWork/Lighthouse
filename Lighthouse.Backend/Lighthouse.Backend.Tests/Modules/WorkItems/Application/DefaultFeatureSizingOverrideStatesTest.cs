using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application
{
    /// <summary>
    /// With default Feature sizing off, the Portfolio owner expects every Feature to show the work its child
    /// Work Items really hold. A Feature in a state listed under "override real child count" is the case where
    /// today's rule replaces the real count with the default size; the switch has to switch that rule off too.
    /// Every scenario saves the settings the way the form does and refreshes the Portfolio through the service.
    /// </summary>
    [TestFixture]
    public class DefaultFeatureSizingOverrideStatesTest
    {
        private const int OceanDefaultFeatureSize = 4;
        private const string AnOverrideState = "In Progress";

        [Test]
        [TestCase(1)]
        [TestCase(OceanDefaultFeatureSize)]
        [TestCase(OceanDefaultFeatureSize + 2)]
        public async Task A_Feature_in_an_override_state_keeps_its_real_child_count_while_default_sizing_is_off(int childWorkItems)
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithOverrideState(AnOverrideState).DefaultSizing(Sizing.SwitchedOff);
            ocean.HasAFeatureInState(AnOverrideState).WithChildWorkItems(childWorkItems);

            await ocean.IsRefreshed();

            ocean.TheFeatureCarriesItsRealChildWorkItems(childWorkItems);
        }

        [Test]
        public async Task A_Feature_in_an_override_state_with_one_child_is_still_sized_by_the_default_while_default_sizing_is_on()
        {
            var ocean = Ocean.WithDefaultFeatureSize(OceanDefaultFeatureSize).WithOverrideState(AnOverrideState).DefaultSizing(Sizing.SwitchedOn);
            ocean.HasAFeatureInState(AnOverrideState).WithChildWorkItems(1);

            await ocean.IsRefreshed();

            ocean.TheFeatureIsSizedByTheDefault(OceanDefaultFeatureSize);
        }

        public enum Sizing
        {
            SwitchedOn,
            SwitchedOff,
        }

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

            private static readonly DateTime TheDayOfTheRefresh = new(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
            private static readonly JsonSerializerOptions AsTheRouteReadsIt = new() { PropertyNameCaseInsensitive = true };

            private readonly JsonObject settings = JsonNode.Parse(TheSettingsOfAnOlderClient)!.AsObject();
            private readonly List<Team> teams = [];
            private readonly List<WorkItem> workItems = [];
            private readonly Mock<IPortfolioMetricsService> history = new();
            private readonly Mock<IWorkTrackingConnector> connector = new();
            private readonly Portfolio portfolio = AJiraPortfolio();
            private int ids;
            private Feature? feature;

            private Ocean()
            {
                teams.Add(ATeam("Atlantic"));
                teams.Add(ATeam("Pacific"));
            }

            public static Ocean WithDefaultFeatureSize(int size)
            {
                var ocean = new Ocean();
                ocean.settings["defaultAmountOfWorkItemsPerFeature"] = size;
                return ocean;
            }

            public Ocean DefaultSizing(Sizing sizing)
            {
                settings["infersDefaultFeatureSize"] = sizing == Sizing.SwitchedOn;
                return this;
            }

            public Ocean WithOverrideState(string state)
            {
                settings["overrideRealChildCountStates"] = new JsonArray(state);
                return this;
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

            public async Task IsRefreshed()
            {
                portfolio.SyncWithPortfolioSettings(
                    JsonSerializer.Deserialize<PortfolioSettingDto>(settings.ToJsonString(), AsTheRouteReadsIt)!,
                    Mock.Of<IRepository<Team>>());

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

            public void TheFeatureCarriesItsRealChildWorkItems(int childWorkItems)
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(childWorkItems),
                        $"Feature {TheFeatureReference} should show its real {childWorkItems} remaining Work Items, because with default sizing off no default size replaces a real child count. Make the override-state rule select no Feature while the switch is off, so the real work stays.");
                    Assert.That(refreshed.FeatureWork.Sum(work => work.TotalWorkItems), Is.EqualTo(childWorkItems),
                        $"Feature {TheFeatureReference} should have a total of {childWorkItems}, because its Team keeps the work its child Work Items really hold. Do not clear that work while the switch is off.");
                    Assert.That(refreshed.Teams.Count(), Is.EqualTo(1),
                        $"Only the Team that owns the child Work Items should carry work for Feature {TheFeatureReference}, because no default is spread across Teams. Leave the real work with its Team and add no other.");
                    Assert.That(refreshed.IsUsingDefaultFeatureSize, Is.False,
                        $"Feature {TheFeatureReference} should not be flagged as sized by the default, because no default was applied to it. Apply no default while the switch is off.");
                    Assert.That(refreshed.Size, Is.EqualTo(childWorkItems),
                        $"Feature {TheFeatureReference} should have size {childWorkItems}, because that is its real child count. Size it from its real child Work Items.");
                }
            }

            public void TheFeatureIsSizedByTheDefault(int defaultSize)
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(defaultSize),
                        $"Feature {TheFeatureReference} should show the default size of {defaultSize}, because with default sizing on a Feature in an override state is re-sized as today. Keep applying the override rule while the switch is on.");
                    Assert.That(refreshed.IsUsingDefaultFeatureSize, Is.True,
                        $"Feature {TheFeatureReference} should be flagged as sized by the default, because that flag drives the warning that the count is an assumption. Keep flagging it while the switch is on.");
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
                ocean.UpdateTime = TheDayOfTheRefresh;
                return ocean;
            }
        }
    }
}
