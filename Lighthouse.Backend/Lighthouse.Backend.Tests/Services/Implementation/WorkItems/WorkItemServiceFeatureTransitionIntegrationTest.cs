using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class WorkItemServiceFeatureTransitionIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task UpdateFeaturesForPortfolio_FeatureWithStateHistory_DerivesCurrentStateEnteredAtFromLatestMatchingTransition()
        {
            var portfolio = await GivenPersistedPortfolio(rawState: "Doing", mappedState: "In Progress");

            var earlierEntry = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
            var latestEntry = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

            var rawTransitions = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Doing", TransitionedAt = earlierEntry },
                new() { FromState = "Doing", ToState = "Done", TransitionedAt = earlierEntry.AddHours(2) },
                new() { FromState = "Done", ToState = "Doing", TransitionedAt = latestEntry },
            };

            var incoming = GivenIncomingFeature(portfolio, "F-100", rawTransitions);

            var subject = CreateSubject(portfolio, incoming);

            await subject.UpdateFeaturesForPortfolio(portfolio);

            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            var transitionRepository = new FeatureStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<FeatureStateTransitionRepository>>());

            var persistedFeature = featureRepository.GetByPredicate(f => f.ReferenceId == "F-100");
            var persistedTransitions = transitionRepository.GetAll().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedFeature, Is.Not.Null);
                Assert.That(persistedFeature!.CurrentStateEnteredAt, Is.EqualTo(latestEntry));
                Assert.That(persistedTransitions, Has.Count.EqualTo(3));
                Assert.That(persistedTransitions, Has.All.Matches<FeatureStateTransition>(t => t.FeatureId == persistedFeature.Id));
                Assert.That(persistedTransitions, Has.All.Matches<FeatureStateTransition>(t => t.FeatureId > 0));
            }
        }

        [Test]
        public async Task UpdateFeaturesForPortfolio_ReRunForUnchangedFeature_IsIdempotent()
        {
            var portfolio = await GivenPersistedPortfolio(rawState: "Doing", mappedState: "In Progress");

            var enteredCurrentState = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
            var rawTransitions = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Doing", TransitionedAt = enteredCurrentState },
            };

            var firstIncoming = GivenIncomingFeature(portfolio, "F-200", rawTransitions);
            var firstSubject = CreateSubject(portfolio, firstIncoming);
            await firstSubject.UpdateFeaturesForPortfolio(portfolio);

            var secondIncoming = GivenIncomingFeature(portfolio, "F-200", rawTransitions);
            var secondSubject = CreateSubject(portfolio, secondIncoming);
            await secondSubject.UpdateFeaturesForPortfolio(portfolio);

            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            var transitionRepository = new FeatureStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<FeatureStateTransitionRepository>>());

            var persistedFeature = featureRepository.GetByPredicate(f => f.ReferenceId == "F-200");
            var persistedTransitions = transitionRepository.GetAll().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedTransitions, Has.Count.EqualTo(1));
                Assert.That(persistedFeature!.CurrentStateEnteredAt, Is.EqualTo(enteredCurrentState));
            }
        }

        [Test]
        public async Task UpdateFeaturesForPortfolio_FirstObservedFeatureWithNoHistory_LeavesCurrentStateEnteredAtNull()
        {
            var portfolio = await GivenPersistedPortfolio(rawState: "Doing", mappedState: "In Progress");

            var incoming = GivenIncomingFeature(portfolio, "F-300", []);

            var subject = CreateSubject(portfolio, incoming);

            await subject.UpdateFeaturesForPortfolio(portfolio);

            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            var transitionRepository = new FeatureStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<FeatureStateTransitionRepository>>());

            var persistedFeature = featureRepository.GetByPredicate(f => f.ReferenceId == "F-300");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedFeature!.CurrentStateEnteredAt, Is.Null);
                Assert.That(transitionRepository.GetAll(), Is.Empty);
            }
        }

        private Feature GivenIncomingFeature(Portfolio portfolio, string referenceId, List<WorkItemStateTransition> rawTransitions)
        {
            return new Feature(new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Feature",
                State = portfolio.MapRawStateToMappedName("Doing"),
                StateCategory = StateCategories.Doing,
                Order = referenceId,
                SyncedTransitions = WorkItemStateTransitionMapper.MapToMappedStates(rawTransitions, portfolio),
            });
        }

        private async Task<Portfolio> GivenPersistedPortfolio(string rawState, string mappedState)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                StateMappings = [new StateMapping { Name = mappedState, States = [rawState] }],
            };
            portfolio.WorkItemTypes.Add("Feature");

            var portfolioRepository = new PortfolioRepository(DatabaseContext, Mock.Of<ILogger<PortfolioRepository>>());
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio;
        }

        private WorkItemService CreateSubject(Portfolio portfolio, params Feature[] incomingFeatures)
        {
            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).ReturnsAsync(incomingFeatures.ToList());
            connectorMock.Setup(x => x.GetParentFeaturesDetails(portfolio, It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(connectorMock.Object);

            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
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
                featureTransitionRepository,
                Mock.Of<Backend.Services.Interfaces.DomainEvents.IDomainEventDispatcher>());
        }
    }
}
