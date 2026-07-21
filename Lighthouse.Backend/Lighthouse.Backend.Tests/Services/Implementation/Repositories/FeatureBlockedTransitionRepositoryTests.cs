using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class FeatureBlockedTransitionRepositoryTests : IntegrationTestBase
    {
        [Test]
        public async Task GetOpenSpell_ReturnsTheOpenSpell_ForMatchingFeatureAndPortfolio()
        {
            var portfolio = await GivenPersistedPortfolio();
            var feature = await GivenPersistedFeature();
            var subject = CreateSubject();

            subject.Add(BlockedSpell(feature.Id, portfolio.Id, Utc(2026, 6, 10, 9), null));
            await subject.Save();

            var openSpell = subject.GetOpenSpell(portfolio.Id, feature.Id);

            Assert.That(openSpell, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(openSpell!.FeatureId, Is.EqualTo(feature.Id));
                Assert.That(openSpell.PortfolioId, Is.EqualTo(portfolio.Id));
                Assert.That(openSpell.EnteredAt, Is.EqualTo(Utc(2026, 6, 10, 9)));
                Assert.That(openSpell.LeftAt, Is.Null);
            }
        }

        [Test]
        public async Task GetOpenSpell_ReturnsNull_WhenTheSpellHasBeenClosed()
        {
            var portfolio = await GivenPersistedPortfolio();
            var feature = await GivenPersistedFeature();
            var subject = CreateSubject();

            var spell = BlockedSpell(feature.Id, portfolio.Id, Utc(2026, 6, 10, 9), null);
            subject.Add(spell);
            await subject.Save();

            spell.LeftAt = Utc(2026, 6, 12, 9);
            subject.Update(spell);
            await subject.Save();

            var openSpell = subject.GetOpenSpell(portfolio.Id, feature.Id);

            Assert.That(openSpell, Is.Null);
        }

        [Test]
        public async Task GetOpenSpell_IsPortfolioScoped_AndInvisibleToAnotherPortfolio()
        {
            var blockedPortfolio = await GivenPersistedPortfolio();
            var otherPortfolio = await GivenPersistedPortfolio();
            var feature = await GivenPersistedFeature();
            var subject = CreateSubject();

            subject.Add(BlockedSpell(feature.Id, blockedPortfolio.Id, Utc(2026, 6, 10, 9), null));
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.GetOpenSpell(blockedPortfolio.Id, feature.Id), Is.Not.Null);
                Assert.That(subject.GetOpenSpell(otherPortfolio.Id, feature.Id), Is.Null);
            }
        }

        [Test]
        public async Task GetOpenSpell_ReturnsTheReopenedSpell_AfterThePreviousSpellWasClosed()
        {
            var portfolio = await GivenPersistedPortfolio();
            var feature = await GivenPersistedFeature();
            var subject = CreateSubject();

            var firstSpell = BlockedSpell(feature.Id, portfolio.Id, Utc(2026, 6, 10, 9), Utc(2026, 6, 11, 9));
            subject.Add(firstSpell);
            await subject.Save();

            var reopenedSpell = BlockedSpell(feature.Id, portfolio.Id, Utc(2026, 6, 13, 9), null);
            subject.Add(reopenedSpell);
            await subject.Save();

            var openSpell = subject.GetOpenSpell(portfolio.Id, feature.Id);

            Assert.That(openSpell, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(openSpell!.Id, Is.EqualTo(reopenedSpell.Id));
                Assert.That(openSpell.EnteredAt, Is.EqualTo(Utc(2026, 6, 13, 9)));
            }
        }

        [Test]
        public async Task GetOpenSpellsForPortfolio_ReturnsOpenSpellsKeyedByFeatureId_ExcludingClosedSpellsAndOtherPortfolios()
        {
            var portfolio = await GivenPersistedPortfolio();
            var otherPortfolio = await GivenPersistedPortfolio();
            var blockedFeature = await GivenPersistedFeature();
            var closedFeature = await GivenPersistedFeature();
            var otherPortfolioFeature = await GivenPersistedFeature();
            var subject = CreateSubject();

            subject.Add(BlockedSpell(blockedFeature.Id, portfolio.Id, Utc(2026, 6, 10, 9), null));
            subject.Add(BlockedSpell(closedFeature.Id, portfolio.Id, Utc(2026, 6, 9, 9), Utc(2026, 6, 11, 9)));
            subject.Add(BlockedSpell(otherPortfolioFeature.Id, otherPortfolio.Id, Utc(2026, 6, 10, 9), null));
            await subject.Save();

            var openSpells = subject.GetOpenSpellsForPortfolio(portfolio.Id);

            var expectedOpenFeatureIds = new[] { blockedFeature.Id };
            Assert.That(openSpells.Keys, Is.EquivalentTo(expectedOpenFeatureIds));
            Assert.That(openSpells[blockedFeature.Id].EnteredAt, Is.EqualTo(Utc(2026, 6, 10, 9)));
        }

        private static FeatureBlockedTransition BlockedSpell(int featureId, int portfolioId, DateTime enteredAt, DateTime? leftAt)
        {
            return new FeatureBlockedTransition
            {
                FeatureId = featureId,
                PortfolioId = portfolioId,
                EnteredAt = enteredAt,
                LeftAt = leftAt,
            };
        }

        private static DateTime Utc(int year, int month, int day, int hour)
        {
            return new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc);
        }

        private async Task<Portfolio> GivenPersistedPortfolio()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "key", Value = "value" });

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var portfolioRepository = new PortfolioRepository(DatabaseContext, Mock.Of<ILogger<PortfolioRepository>>());
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio;
        }

        private async Task<Feature> GivenPersistedFeature()
        {
            var feature = new Feature(new WorkItemBase
            {
                ReferenceId = $"F-{Guid.NewGuid():N}",
                Name = "Feature",
                Type = "Feature",
                State = "Doing",
                StateCategory = StateCategories.Doing,
                Order = "1",
                Url = "https://letpeople.work/feature",
            });

            var featureRepository = new FeatureRepository(DatabaseContext, Mock.Of<ILogger<FeatureRepository>>());
            featureRepository.Add(feature);
            await featureRepository.Save();

            return feature;
        }

        private FeatureBlockedTransitionRepository CreateSubject()
        {
            return new FeatureBlockedTransitionRepository(DatabaseContext, Mock.Of<ILogger<FeatureBlockedTransitionRepository>>());
        }
    }
}
