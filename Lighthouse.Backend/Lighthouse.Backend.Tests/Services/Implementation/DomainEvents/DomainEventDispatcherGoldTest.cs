using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    public class DomainEventDispatcherGoldTest : IntegrationTestBase
    {
        private static readonly ProbeHandlerState ProbeState = new();

        public DomainEventDispatcherGoldTest()
            : base(new GoldTestWebApplicationFactory())
        {
        }

        [SetUp]
        public void ResetProbe()
        {
            ProbeState.Reset();
        }

        [Test]
        public async Task PublishPortfolioFeaturesRefreshed_RunsAllHandlers_SurvivesThrowingHandler_AndReDrivesOnRepublish()
        {
            await SeedDatabase();
            var portfolio = await SeedPortfolio();
            var dispatcher = ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

            await dispatcher.PublishAsync(new PortfolioFeaturesRefreshed(portfolio.Id));

            var portfolioAfterFirstPublish = ReloadPortfolio(portfolio.Id);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProbeState.HandledPortfolioIds, Is.EqualTo(new[] { portfolio.Id }), "the non-throwing probe handler ran despite a sibling throwing");
                Assert.That(portfolioAfterFirstPublish, Is.Not.Null, "the committed portfolio fact survives a throwing handler");
            }

            await dispatcher.PublishAsync(new PortfolioFeaturesRefreshed(portfolio.Id));

            Assert.That(ProbeState.HandledPortfolioIds, Is.EqualTo(new[] { portfolio.Id, portfolio.Id }), "the next re-sync re-drives the reaction");
        }

        private async Task<Portfolio> SeedPortfolio()
        {
            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = new Portfolio
            {
                Name = "Gold Release",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira },
            };
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();
            return portfolio;
        }

        private Portfolio? ReloadPortfolio(int id)
        {
            return ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(id);
        }

        private sealed class ProbeHandlerState
        {
            private readonly List<int> handledPortfolioIds = [];

            public IReadOnlyList<int> HandledPortfolioIds
            {
                get
                {
                    lock (handledPortfolioIds)
                    {
                        return handledPortfolioIds.ToList();
                    }
                }
            }

            public void Record(int portfolioId)
            {
                lock (handledPortfolioIds)
                {
                    handledPortfolioIds.Add(portfolioId);
                }
            }

            public void Reset()
            {
                lock (handledPortfolioIds)
                {
                    handledPortfolioIds.Clear();
                }
            }
        }

        private sealed class ProbeHandler(ProbeHandlerState state) : IDomainEventHandler<PortfolioFeaturesRefreshed>
        {
            public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
            {
                state.Record(domainEvent.PortfolioId);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingProbeHandler : IDomainEventHandler<PortfolioFeaturesRefreshed>
        {
            public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("gold-test handler boom");
            }
        }

        private sealed class GoldTestWebApplicationFactory : TestWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(ProbeState);
                    services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, ThrowingProbeHandler>();
                    services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, ProbeHandler>();
                });
            }
        }
    }
}
