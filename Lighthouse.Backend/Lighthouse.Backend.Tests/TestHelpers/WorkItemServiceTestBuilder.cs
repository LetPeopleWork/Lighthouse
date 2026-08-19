using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// The one place that knows how to construct a <see cref="WorkItemService"/>. Seven fixtures used to
    /// spell out all thirteen constructor arguments each, so Epic #5687's single new dependency had to be
    /// added in seven files - which is how a collaborator no test cares about becomes seven diffs.
    ///
    /// A fixture names only the seams its own assertions read; everything else takes the value all seven
    /// call sites already passed verbatim. <see cref="FeatureOrderingTestHelper.FollowingTheTracker"/> in
    /// particular is the real seam rather than a mock, because a bare mock hands back an empty sequence
    /// and quietly guts the caller.
    /// </summary>
    public sealed class WorkItemServiceTestBuilder
    {
        private IWorkTrackingConnectorFactory workTrackingConnectorFactory = Mock.Of<IWorkTrackingConnectorFactory>();
        private IRepository<Feature> featureRepository = Mock.Of<IRepository<Feature>>();
        private IWorkItemRepository workItemRepository = Mock.Of<IWorkItemRepository>();
        private IPortfolioMetricsService portfolioMetricsService = Mock.Of<IPortfolioMetricsService>();
        private IRepository<Team> teamRepository = Mock.Of<IRepository<Team>>();
        private IWorkItemStateTransitionRepository stateTransitionRepository = Mock.Of<IWorkItemStateTransitionRepository>();
        private IFeatureStateTransitionRepository featureStateTransitionRepository = Mock.Of<IFeatureStateTransitionRepository>();
        private IDomainEventDispatcher domainEventDispatcher = Mock.Of<IDomainEventDispatcher>();

        /// <summary>
        /// The connector the service is to talk to. Wrapping it in a factory is what every call site did by
        /// hand, and no fixture has ever cared which factory answered - only which connector it handed back.
        /// </summary>
        public WorkItemServiceTestBuilder WithConnector(IWorkTrackingConnector connector)
        {
            var factoryMock = new Mock<IWorkTrackingConnectorFactory>();
            factoryMock
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connector);

            workTrackingConnectorFactory = factoryMock.Object;
            return this;
        }

        public WorkItemServiceTestBuilder WithConnectorFactory(IWorkTrackingConnectorFactory factory)
        {
            workTrackingConnectorFactory = factory;
            return this;
        }

        public WorkItemServiceTestBuilder WithFeatureRepository(IRepository<Feature> repository)
        {
            featureRepository = repository;
            return this;
        }

        public WorkItemServiceTestBuilder WithWorkItemRepository(IWorkItemRepository repository)
        {
            workItemRepository = repository;
            return this;
        }

        public WorkItemServiceTestBuilder WithPortfolioMetricsService(IPortfolioMetricsService service)
        {
            portfolioMetricsService = service;
            return this;
        }

        public WorkItemServiceTestBuilder WithTeamRepository(IRepository<Team> repository)
        {
            teamRepository = repository;
            return this;
        }

        public WorkItemServiceTestBuilder WithStateTransitionRepository(IWorkItemStateTransitionRepository repository)
        {
            stateTransitionRepository = repository;
            return this;
        }

        public WorkItemServiceTestBuilder WithFeatureStateTransitionRepository(IFeatureStateTransitionRepository repository)
        {
            featureStateTransitionRepository = repository;
            return this;
        }

        public WorkItemServiceTestBuilder WithDomainEventDispatcher(IDomainEventDispatcher dispatcher)
        {
            domainEventDispatcher = dispatcher;
            return this;
        }

        public WorkItemService Build()
            => new(
                Mock.Of<ILogger<WorkItemService>>(),
                workTrackingConnectorFactory,
                featureRepository,
                workItemRepository,
                portfolioMetricsService,
                teamRepository,
                stateTransitionRepository,
                featureStateTransitionRepository,
                domainEventDispatcher,
                new BlockedItemService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider()),
                NoOpenBlockedSpells(),
                FeatureOrderingTestHelper.FollowingTheTracker(),
                Mock.Of<IRepository<OptionalFeature>>(),
                new DependencyReconciler(),
                // The real one: it only reads what the refresh already holds and writes a log line, so a
                // fixture that faked it would be hiding the one thing it does.
                new DependencyRefreshReporter(
                    new DependencyHonourPolicy(), Mock.Of<ILogger<DependencyRefreshReporter>>()));

        /// <summary>No portfolio has a blocked spell already running, which is the state every fixture assumed.</summary>
        private static IFeatureBlockedTransitionRepository NoOpenBlockedSpells()
            => Mock.Of<IFeatureBlockedTransitionRepository>(
                repository => repository.GetOpenSpellsForPortfolio(It.IsAny<int>()) == new Dictionary<int, FeatureBlockedTransition>());
    }
}
