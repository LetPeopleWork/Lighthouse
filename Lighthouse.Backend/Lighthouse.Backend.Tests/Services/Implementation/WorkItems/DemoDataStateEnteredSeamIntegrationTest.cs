using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class DemoDataStateEnteredSeamIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task UpdateWorkItemsForTeam_TeamZenithDemoData_DerivesCurrentStateEnteredAtForInProgressItems_IncludingBlockedAndStale()
        {
            var team = await GivenPersistedDemoTeam("Team Zenith");

            var subject = CreateSubject(team);

            await subject.UpdateWorkItemsForTeam(team);

            var inProgressItems = LoadInProgressItems(team);
            var blockedAndStale = inProgressItems.SingleOrDefault(wi => wi.IsBlocked);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inProgressItems, Is.Not.Empty);
                Assert.That(inProgressItems, Has.All.Matches<WorkItem>(wi => wi.CurrentStateEnteredAt != null),
                    "Every in-progress demo item must carry a derived CurrentStateEnteredAt sourced from the StateEnteredDate column.");
                Assert.That(blockedAndStale, Is.Not.Null,
                    "Team Zenith must contain a blocked in-progress demo item so the E2E can prove blocked-excludes-stale.");
                Assert.That(blockedAndStale!.CurrentStateEnteredAt!.Value.Date, Is.LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(-7)),
                    "The blocked in-progress demo item must be clearly stale (entered its state well over a week ago).");
            }
        }

        [Test]
        public async Task DemoScenarioZero_EveryCompletedItem_HasInterpolatedJourney_SoLastDoingStatePaceBandEqualsCycleTimePercentiles()
        {
            await SeedDatabase();

            var demoFactory = new DemoDataFactory(GetWorkTrackingSystemFactory());
            var connection = await GivenPersistedDemoConnection(demoFactory);
            var team = await GivenPersistedDemoTeam(demoFactory, connection, "Team Zenith");
            var portfolio = await GivenPersistedDemoPortfolio(demoFactory, connection, "Project Apollo");

            var workItemService = CreateSubject(team);
            await workItemService.UpdateWorkItemsForTeam(team);
            await workItemService.UpdateFeaturesForPortfolio(portfolio);

            var windowStart = DateTime.UtcNow.Date.AddDays(-90);
            var windowEnd = DateTime.UtcNow.Date;

            var teamBands = GetTeamMetricsService().GetAgeInStatePercentilesForTeam(team, windowStart, windowEnd).ToList();
            var teamCycleTime = GetTeamMetricsService().GetCycleTimePercentilesForTeam(team, windowStart, windowEnd).ToList();

            var portfolioBands = GetPortfolioMetricsService().GetAgeInStatePercentilesForPortfolio(portfolio, windowStart, windowEnd).ToList();
            var portfolioCycleTime = GetPortfolioMetricsService().GetCycleTimePercentilesForPortfolio(portfolio, windowStart, windowEnd).ToList();

            using (Assert.EnterMultipleScope())
            {
                AssertRisingBandsExist(teamBands, "Team Zenith");
                AssertRisingBandsExist(portfolioBands, "Project Apollo");

                AssertLastDoingStateMatchesCycleTime(teamBands, teamCycleTime, team.DoingStates[^1], "Team Zenith");
                AssertLastDoingStateMatchesCycleTime(portfolioBands, portfolioCycleTime, portfolio.DoingStates[^1], "Project Apollo");
            }
        }

        private static void AssertLastDoingStateMatchesCycleTime(
            IReadOnlyList<AgeInStatePercentilesDto> bands,
            IReadOnlyList<PercentileValue> cycleTimePercentiles,
            string lastDoingState,
            string owner)
        {
            var lastStateBand = bands.SingleOrDefault(band => band.State == lastDoingState);

            Assert.That(lastStateBand, Is.Not.Null,
                $"{owner} must expose a pace band for its final Doing state '{lastDoingState}'; every completed item must traverse it so its population matches the cycle-time population.");

            var bandByPercentile = lastStateBand!.Percentiles.ToDictionary(p => p.Percentile, p => p.Value);

            foreach (var cycleTimePercentile in cycleTimePercentiles)
            {
                Assert.That(bandByPercentile, Does.ContainKey(cycleTimePercentile.Percentile));
                Assert.That(bandByPercentile[cycleTimePercentile.Percentile], Is.EqualTo(cycleTimePercentile.Value),
                    $"{owner}: cumulative age at exit of the final Doing state '{lastDoingState}' equals ClosedDate − StartedDate = cycle time, so the {cycleTimePercentile.Percentile}th pace-band value must equal the {cycleTimePercentile.Percentile}th cycle-time value exactly.");
            }
        }

        private static void AssertRisingBandsExist(IReadOnlyList<AgeInStatePercentilesDto> bands, string owner)
        {
            var populatedBands = bands.Where(band => band.Percentiles.Any(p => p.Value > 0)).ToList();

            Assert.That(populatedBands, Is.Not.Empty,
                $"{owner} demo data must expose at least one state with a non-empty pace band; an empty result means completed demo items have no multi-state exit transitions.");

            Assert.That(populatedBands, Has.All.Matches<AgeInStatePercentilesDto>(IsMonotonicallyRising),
                $"{owner} pace bands must rise monotonically across percentiles, reflecting cumulative age at state exit.");
        }

        private static bool IsMonotonicallyRising(AgeInStatePercentilesDto band)
        {
            var orderedByPercentile = band.Percentiles.OrderBy(p => p.Percentile).Select(p => p.Value).ToList();
            return orderedByPercentile.Zip(orderedByPercentile.Skip(1), (lower, higher) => higher >= lower).All(rising => rising);
        }

        private async Task<Team> GivenPersistedDemoTeam(string teamName)
        {
            var demoFactory = new DemoDataFactory(GetWorkTrackingSystemFactory());
            var team = demoFactory.CreateDemoTeam(teamName);
            team.WorkTrackingSystemConnection = demoFactory.CreateDemoWorkTrackingSystemConnection();

            var teamRepository = new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private async Task<WorkTrackingSystemConnection> GivenPersistedDemoConnection(DemoDataFactory demoFactory)
        {
            var connection = demoFactory.CreateDemoWorkTrackingSystemConnection();

            var connectionRepository = new WorkTrackingSystemConnectionRepository(DatabaseContext, Mock.Of<ILogger<WorkTrackingSystemConnectionRepository>>());
            connectionRepository.Add(connection);
            await connectionRepository.Save();

            return connection;
        }

        private async Task<Team> GivenPersistedDemoTeam(DemoDataFactory demoFactory, WorkTrackingSystemConnection connection, string teamName)
        {
            var team = demoFactory.CreateDemoTeam(teamName);
            team.WorkTrackingSystemConnection = connection;
            team.WorkTrackingSystemConnectionId = connection.Id;

            var teamRepository = new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private async Task<Portfolio> GivenPersistedDemoPortfolio(DemoDataFactory demoFactory, WorkTrackingSystemConnection connection, string portfolioName)
        {
            var portfolio = demoFactory.CreateDemoProject(portfolioName);
            portfolio.WorkTrackingSystemConnection = connection;
            portfolio.WorkTrackingSystemConnectionId = connection.Id;

            var portfolioRepository = new PortfolioRepository(DatabaseContext, Mock.Of<ILogger<PortfolioRepository>>());
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio;
        }

        private List<WorkItem> LoadInProgressItems(Team team)
        {
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            return workItemRepository
                .GetAllByPredicate(wi => wi.TeamId == team.Id && wi.StateCategory == StateCategories.Doing)
                .ToList();
        }

        private IWorkTrackingSystemFactory GetWorkTrackingSystemFactory()
        {
            return ServiceProvider.GetService<IWorkTrackingSystemFactory>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Factory");
        }

        private ITeamMetricsService GetTeamMetricsService()
        {
            return ServiceProvider.GetService<ITeamMetricsService>() ?? throw new InvalidOperationException("Could not resolve Team Metrics Service");
        }

        private IPortfolioMetricsService GetPortfolioMetricsService()
        {
            return ServiceProvider.GetService<IPortfolioMetricsService>() ?? throw new InvalidOperationException("Could not resolve Portfolio Metrics Service");
        }

        private WorkItemService CreateSubject(IWorkItemQueryOwner owner)
        {
            var realConnector = ServiceProvider.GetService<IWorkTrackingConnectorFactory>()!
                .GetWorkTrackingConnector(WorkTrackingSystems.Csv);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(realConnector);

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());
            var featureTransitionRepository = new FeatureStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<FeatureStateTransitionRepository>>());

            return new WorkItemService(
                Mock.Of<ILogger<WorkItemService>>(),
                connectorFactoryMock.Object,
                featureRepository,
                workItemRepository,
                Mock.Of<IPortfolioMetricsService>(),
                Mock.Of<IRepository<Team>>(),
                transitionRepository,
                featureTransitionRepository);
        }
    }
}
