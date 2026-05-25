using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class StalenessThresholdPersistenceTest
    {
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> logger;
        private DbContextOptions<LighthouseAppContext> options;

        [SetUp]
        public void Setup()
        {
            cryptoService = new Mock<ICryptoService>();
            logger = new Mock<ILogger<LighthouseAppContext>>();

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object);
            context.Database.EnsureCreated();
        }

        [Test]
        public async Task PersistsStalenessThresholdDays_DefaultsZeroForTeamAndPortfolio_RoundTripsThroughContext()
        {
            int teamId;
            int portfolioId;

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var team = new Team { Name = "Test Team" };
                var portfolio = new Portfolio { Name = "Test Portfolio" };
                context.Teams.Add(team);
                context.Portfolios.Add(portfolio);
                await context.SaveChangesAsync();
                teamId = team.Id;
                portfolioId = portfolio.Id;
            }

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedTeam = await context.Teams.SingleAsync(t => t.Id == teamId);
                var savedPortfolio = await context.Portfolios.SingleAsync(p => p.Id == portfolioId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(savedTeam.StalenessThresholdDays, Is.Zero);
                    Assert.That(savedPortfolio.StalenessThresholdDays, Is.Zero);
                }
            }
        }
    }
}
