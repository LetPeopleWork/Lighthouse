using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application
{
    /// <summary>
    /// A Feature without child Work Items whose size works out to 0 has no work for any Team, so no Team lists
    /// it on its Features tab, while it still belongs to its Portfolio. Every scenario saves the Portfolio's
    /// settings the way the settings form does and then refreshes the Portfolio through the service.
    /// </summary>
    [TestFixture]
    public class ZeroDefaultFeatureSizeTest
    {
        private const int TheEstimateTheTrackerHolds = 9;

        [Test]
        [TestCase(Placement.AnyTeamMayTakeIt)]
        [TestCase(Placement.PortfolioHasAnOwningTeam)]
        [TestCase(Placement.FeatureOwnerFieldNamesATeam)]
        public async Task A_Feature_whose_default_size_is_zero_is_not_listed_on_any_Team_but_stays_in_its_Portfolio(Placement placement)
        {
            var ocean = Ocean.WithDefaultFeatureSizeZero().WithTwoTeams().PlacedBy(placement);
            ocean.HasAFeatureWithoutChildWorkItems();

            await ocean.IsRefreshed();

            ocean.TheFeatureStaysInThePortfolioSizedByTheDefaultWithNoWorkForAnyTeam();
        }

        // Guard: already true today, it keeps the fix from dropping work a tracker estimate really gives.
        [Test]
        [TestCase(Placement.AnyTeamMayTakeIt, 2)]
        [TestCase(Placement.PortfolioHasAnOwningTeam, 1)]
        [TestCase(Placement.FeatureOwnerFieldNamesATeam, 1)]
        public async Task A_Feature_with_a_tracker_estimate_above_zero_still_carries_it_for_its_Teams_while_the_default_size_is_zero(Placement placement, int teamsThatCarryIt)
        {
            var ocean = Ocean.WithDefaultFeatureSizeZero().WithTwoTeams().PlacedBy(placement);
            ocean.HasAFeatureWithoutChildWorkItems(estimate: TheEstimateTheTrackerHolds);

            await ocean.IsRefreshed();

            ocean.TheFeatureCarries(remaining: TheEstimateTheTrackerHolds, acrossTeams: teamsThatCarryIt);
        }

        public enum Placement
        {
            AnyTeamMayTakeIt,
            PortfolioHasAnOwningTeam,
            FeatureOwnerFieldNamesATeam,
        }

        private sealed class Ocean
        {
            private const string TheFeatureReference = "F-1";
            private const int TheFeatureOwnerFieldId = 5;
            private const int TheSizeEstimateFieldId = 7;

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
                  "defaultAmountOfWorkItemsPerFeature": 0,
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
                  "involvedTeams": [],
                  "infersDefaultFeatureSize": true
                }
                """;

            private static readonly JsonSerializerOptions AsTheRouteReadsIt = new() { PropertyNameCaseInsensitive = true };

            private readonly JsonObject settings = JsonNode.Parse(TheSettingsOfAnOlderClient)!.AsObject();
            private readonly List<Team> teams = [];
            private readonly Mock<IPortfolioMetricsService> history = new();
            private readonly Mock<IWorkTrackingConnector> connector = new();
            private readonly Portfolio portfolio = AJiraPortfolio();
            private int ids;
            private Feature? feature;
            private Team? owningTeam;
            private string? featureOwnerValue;

            private Ocean()
            {
            }

            public static Ocean WithDefaultFeatureSizeZero() => new();

            public Ocean WithTwoTeams()
            {
                teams.Add(ATeam("Atlantic"));
                teams.Add(ATeam("Pacific"));
                return this;
            }

            public Ocean PlacedBy(Placement placement)
            {
                switch (placement)
                {
                    case Placement.PortfolioHasAnOwningTeam:
                        owningTeam = teams[1];
                        settings["owningTeam"] = new JsonObject { ["id"] = owningTeam.Id, ["name"] = owningTeam.Name };
                        break;
                    case Placement.FeatureOwnerFieldNamesATeam:
                        settings["featureOwnerSource"] = "Own";
                        settings["featureOwnerAdditionalFieldDefinitionId"] = TheFeatureOwnerFieldId;
                        featureOwnerValue = teams[0].Name;
                        break;
                }

                return this;
            }

            public void HasAFeatureWithoutChildWorkItems(int estimate = 0)
            {
                feature = new Feature { ReferenceId = TheFeatureReference, EstimatedSize = estimate };
                if (estimate > 0)
                {
                    settings["sizeEstimateSource"] = "Own";
                    settings["sizeEstimateAdditionalFieldDefinitionId"] = TheSizeEstimateFieldId;
                }

                if (featureOwnerValue != null)
                {
                    feature.AdditionalFieldValues[TheFeatureOwnerFieldId] = featureOwnerValue;
                }
            }

            public async Task IsRefreshed()
            {
                var teamRepositoryForSettings = new Mock<IRepository<Team>>();
                teamRepositoryForSettings.Setup(repository => repository.GetById(It.IsAny<int>())).Returns(owningTeam);
                portfolio.SyncWithPortfolioSettings(
                    JsonSerializer.Deserialize<PortfolioSettingDto>(settings.ToJsonString(), AsTheRouteReadsIt)!,
                    teamRepositoryForSettings.Object);

                var workItemRepository = new Mock<IWorkItemRepository>();
                workItemRepository
                    .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                    .Returns(new List<WorkItem>().AsQueryable());
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

            public void TheFeatureStaysInThePortfolioSizedByTheDefaultWithNoWorkForAnyTeam()
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork, Is.Empty,
                        "Feature F should have no work row for any Team, because its default size works out to 0 and a 0 of 0 row would list it on a Team that has no work for it. Skip the Team assignment when the size works out to 0.");
                    Assert.That(refreshed.Teams, Is.Empty,
                        "No Team should carry Feature F, because no Team has work for it. Leave it off every Team's Features.");
                    Assert.That(refreshed.IsUsingDefaultFeatureSize, Is.True,
                        "Feature F should stay flagged as sized by the default, because its size still came from the default. Keep the flag when no Team gets work.");
                }
            }

            public void TheFeatureCarries(int remaining, int acrossTeams)
            {
                var refreshed = TheRefreshedFeature();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshed.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(remaining),
                        $"Feature F should show {remaining} remaining Work Items, because that is the estimate its tracker holds. Do not drop work an estimate above 0 gives.");
                    Assert.That(refreshed.Teams.Count(), Is.EqualTo(acrossTeams),
                        $"Feature F's work should sit with {acrossTeams} Team(s), because the estimate is shared among the Teams that take the Feature.");
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
