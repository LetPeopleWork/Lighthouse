using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    public class TeamDataRefreshedGoldTest : IntegrationTestBase
    {
        private static readonly RecordingForecastUpdater Recorder = new();

        public TeamDataRefreshedGoldTest()
            : base(new GoldTestWebApplicationFactory())
        {
        }

        [SetUp]
        public void ResetRecorder()
        {
            Recorder.Reset();
        }

        [Test]
        public async Task PublishTeamDataRefreshed_TriggersForecastUpdateForEachPortfolioOfTheTeam()
        {
            await SeedDatabase();
            var team = await SeedTeamWithPortfolios();
            var dispatcher = ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

            await dispatcher.PublishAsync(new TeamDataRefreshed(team.Id));

            Assert.That(Recorder.TriggeredPortfolioIds, Is.EquivalentTo(team.Portfolios.Select(p => p.Id)));
        }

        private async Task<Team> SeedTeamWithPortfolios()
        {
            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            var team = new Team
            {
                Name = "Gold Team",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira },
            };

            var firstPortfolio = new Portfolio
            {
                Name = "Gold Release A",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection A", WorkTrackingSystem = WorkTrackingSystems.Jira },
            };
            var secondPortfolio = new Portfolio
            {
                Name = "Gold Release B",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection B", WorkTrackingSystem = WorkTrackingSystems.Jira },
            };
            team.Portfolios.Add(firstPortfolio);
            team.Portfolios.Add(secondPortfolio);

            portfolioRepository.Add(firstPortfolio);
            portfolioRepository.Add(secondPortfolio);
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private sealed class RecordingForecastUpdater : IForecastUpdater
        {
            private readonly List<int> triggeredPortfolioIds = [];

            public IReadOnlyList<int> TriggeredPortfolioIds
            {
                get
                {
                    lock (triggeredPortfolioIds)
                    {
                        return triggeredPortfolioIds.ToList();
                    }
                }
            }

            public void TriggerUpdate(int id)
            {
                lock (triggeredPortfolioIds)
                {
                    triggeredPortfolioIds.Add(id);
                }
            }

            public void Reset()
            {
                lock (triggeredPortfolioIds)
                {
                    triggeredPortfolioIds.Clear();
                }
            }
        }

        private sealed class GoldTestWebApplicationFactory : TestWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IForecastUpdater>(Recorder);
                });
            }
        }
    }
}
