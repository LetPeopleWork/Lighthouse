using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class LighthouseAppContextOrphanSweepTest
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
        }

        [Test]
        public async Task SaveChangesAsync_ModifiedFeatureWithUnloadedPortfoliosCollection_DoesNotDeleteFeature()
        {
            int featureId;

            using (var seedContext = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var portfolio = new Portfolio { Name = "Linked Portfolio" };
                seedContext.Portfolios.Add(portfolio);
                await seedContext.SaveChangesAsync();

                var feature = new Feature
                {
                    Name = "Linked Feature",
                    ReferenceId = "FEAT-1",
                    Order = "1",
                };
                feature.Portfolios.Add(portfolio);
                seedContext.Features.Add(feature);
                await seedContext.SaveChangesAsync();

                featureId = feature.Id;
            }

            EntityState stateAfterMutation;
            using (var mutateContext = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var feature = await mutateContext.Features.SingleAsync(f => f.Id == featureId);
                feature.Name = "Mutated";

                await mutateContext.SaveChangesAsync();

                stateAfterMutation = mutateContext.Entry(feature).State;
            }

            using (var verifyContext = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var features = await verifyContext.Features.ToListAsync();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(features, Has.Count.EqualTo(1));
                    Assert.That(features[0].Name, Is.EqualTo("Mutated"));
                    Assert.That(stateAfterMutation, Is.Not.EqualTo(EntityState.Deleted));
                    Assert.That(stateAfterMutation, Is.Not.EqualTo(EntityState.Detached));
                }
            }
        }
    }
}
