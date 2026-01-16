using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class LighthouseAppContextUtcTest
    {
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> logger;
        private DbContextOptions<LighthouseAppContext> options;

        [SetUp]
        public void Setup()
        {
            cryptoService = new Mock<ICryptoService>();
            logger = new Mock<ILogger<LighthouseAppContext>>();

            // Use in-memory database for testing
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Test]
        public async Task SaveChanges_WithUtcDateTime_StoresAsUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var futureDate = DateTime.UtcNow.AddDays(30);
                var delivery = new Delivery("Test Delivery", futureDate, 1);

                context.Deliveries.Add(delivery);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedDelivery = await context.Deliveries.FirstAsync();
                Assert.That(savedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
        }

        [Test]
        public async Task SaveChanges_WithUnspecifiedDateTime_ConvertsToUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var unspecifiedDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Unspecified);
                var delivery = new Delivery("Test Delivery", unspecifiedDate, 1);

                context.Deliveries.Add(delivery);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedDelivery = await context.Deliveries.FirstAsync();
                Assert.That(savedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(savedDelivery.Date.Year, Is.EqualTo(2026));
                Assert.That(savedDelivery.Date.Month, Is.EqualTo(12));
                Assert.That(savedDelivery.Date.Day, Is.EqualTo(31));
            }
        }

        [Test]
        public async Task SaveChanges_WithLocalDateTime_ConvertsToUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var localDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Local);
                var delivery = new Delivery("Test Delivery", localDate, 1);

                context.Deliveries.Add(delivery);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedDelivery = await context.Deliveries.FirstAsync();
                Assert.That(savedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
        }

        [Test]
        public async Task SaveChanges_WorkItem_DateTimesAreUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var team = new Team { Name = "Test Team" };
                context.Teams.Add(team);
                await context.SaveChangesAsync();

                var workItem = new WorkItem
                {
                    Name = "Test Work Item",
                    Order = "1",
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    StartedDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Local),
                    ClosedDate = DateTime.UtcNow,
                    TeamId = team.Id
                };

                context.WorkItems.Add(workItem);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedWorkItem = await context.WorkItems.FirstAsync();
                Assert.That(savedWorkItem.CreatedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "CreatedDate should be UTC");
                Assert.That(savedWorkItem.StartedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "StartedDate should be UTC");
                Assert.That(savedWorkItem.ClosedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "ClosedDate should be UTC");
            }
        }

        [Test]
        public async Task SaveChanges_Feature_DateTimesAreUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var portfolio = new Portfolio { Name = "Test Portfolio" };
                context.Portfolios.Add(portfolio);
                await context.SaveChangesAsync();

                var feature = new Feature
                {
                    Name = "Test Feature",
                    Order = "1",
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    StartedDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Local),
                    ClosedDate = DateTime.UtcNow
                };
                feature.Portfolios.Add(portfolio);

                context.Features.Add(feature);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedFeature = await context.Features.FirstAsync();
                Assert.That(savedFeature.CreatedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "CreatedDate should be UTC");
                Assert.That(savedFeature.StartedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "StartedDate should be UTC");
                Assert.That(savedFeature.ClosedDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), "ClosedDate should be UTC");
            }
        }

        [Test]
        public async Task SaveChanges_Team_DateTimesAreUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var team = new Team
                {
                    Name = "Test Team",
                    ThroughputHistoryStartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    ThroughputHistoryEndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    UpdateTime = DateTime.UtcNow
                };

                context.Teams.Add(team);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedTeam = await context.Teams.FirstAsync();
                Assert.That(savedTeam.ThroughputHistoryStartDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), 
                    "ThroughputHistoryStartDate should be UTC");
                Assert.That(savedTeam.ThroughputHistoryEndDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc), 
                    "ThroughputHistoryEndDate should be UTC");
                Assert.That(savedTeam.UpdateTime.Kind, Is.EqualTo(DateTimeKind.Utc), "UpdateTime should be UTC");
            }
        }

        [Test]
        public async Task SaveChanges_Portfolio_UpdateTimeIsUtc()
        {
            // Arrange
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var portfolio = new Portfolio
                {
                    Name = "Test Portfolio",
                    UpdateTime = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified)
                };

                context.Portfolios.Add(portfolio);
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedPortfolio = await context.Portfolios.FirstAsync();
                Assert.That(savedPortfolio.UpdateTime.Kind, Is.EqualTo(DateTimeKind.Utc), "UpdateTime should be UTC");
            }
        }
    }
}
