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
            var seeded = await SeedTeamWithPortfolios();
            var dispatcher = ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

            await dispatcher.PublishAsync(new TeamDataRefreshed(seeded.TeamId));

            Assert.That(
                Recorder.TriggeredPortfolioIds,
                Is.EquivalentTo(new[] { seeded.FirstPortfolioId, seeded.SecondPortfolioId }));
        }

        private async Task<SeededTeam> SeedTeamWithPortfolios()
        {
            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            var team = new Team
            {
                Name = "Gold Team",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira },
            };

            teamRepository.Add(team);
            await teamRepository.Save();

            var firstPortfolio = PortfolioWorkedOnBy(team, "Gold Release A", "Connection A");
            var secondPortfolio = PortfolioWorkedOnBy(team, "Gold Release B", "Connection B");

            portfolioRepository.Add(firstPortfolio);
            portfolioRepository.Add(secondPortfolio);
            await portfolioRepository.Save();

            return new SeededTeam(team.Id, firstPortfolio.Id, secondPortfolio.Id);
        }

        /// <summary>
        /// A team reaches a portfolio by working on one of its features, and by nothing else. Anything
        /// that attaches the two directly is invisible to every read in production.
        /// </summary>
        private static Portfolio PortfolioWorkedOnBy(Team team, string portfolioName, string connectionName)
        {
            var portfolio = new Portfolio
            {
                Name = portfolioName,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = connectionName, WorkTrackingSystem = WorkTrackingSystems.Jira },
            };

            var feature = new Feature { Name = $"{portfolioName} Feature", Order = "1" };
            feature.FeatureWork.Add(new FeatureWork(team, 5, 5, feature));
            portfolio.Features.Add(feature);

            return portfolio;
        }

        private sealed record SeededTeam(int TeamId, int FirstPortfolioId, int SecondPortfolioId);

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
