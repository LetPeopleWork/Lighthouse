using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Access.Application;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.Access.Application
{
    [TestFixture]
    public class FeatureMoveAuthorizationTest
    {
        private Mock<IRbacAdministrationService> rbac = null!;
        private FeatureMoveAuthorization subject = null!;

        [SetUp]
        public void SetUp()
        {
            rbac = new Mock<IRbacAdministrationService>();
            subject = new FeatureMoveAuthorization(rbac.Object);
        }

        private void WritableIs(IReadOnlyList<int>? writable) =>
            rbac.Setup(r => r.GetWritablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(writable!);

        private static Feature FeatureIn(int id, params int[] portfolioIds)
        {
            var feature = new Feature { Id = id };
            foreach (var portfolioId in portfolioIds)
            {
                feature.Portfolios.Add(new Portfolio { Id = portfolioId, Name = $"Portfolio {portfolioId}" });
            }

            return feature;
        }

        [Test]
        public async Task A_Feature_in_no_Portfolio_cannot_be_moved_by_anyone()
        {
            WritableIs([]);

            var verdicts = await subject.GetVerdictsAsync(new ClaimsPrincipal(), [FeatureIn(1)], new HashSet<int>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdicts[1].CanMove, Is.False);
                Assert.That(verdicts[1].BlockReason, Is.EqualTo(FeatureMoveVerdict.NotInAnyPortfolio));
                Assert.That(verdicts[1].BlockingPortfolios, Is.Empty);
            }
        }

        [Test]
        public async Task When_the_access_check_names_no_writable_set_every_Portfolio_counts_as_writable()
        {
            WritableIs(null);

            var verdicts = await subject.GetVerdictsAsync(new ClaimsPrincipal(), [FeatureIn(1, 10, 11)], new HashSet<int>());

            Assert.That(verdicts[1].CanMove, Is.True);
        }

        [Test]
        public async Task A_Feature_is_blocked_by_a_Portfolio_the_caller_cannot_write_and_only_readable_ones_are_named()
        {
            WritableIs([10]);

            var verdicts = await subject.GetVerdictsAsync(new ClaimsPrincipal(), [FeatureIn(1, 10, 11, 12)], new HashSet<int> { 11 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdicts[1].CanMove, Is.False);
                Assert.That(verdicts[1].BlockReason, Is.EqualTo(FeatureMoveVerdict.NoWriteOnEveryPortfolio));
                Assert.That(verdicts[1].BlockingPortfolios.Select(p => p.Id), Is.EqualTo([11]));
            }
        }
    }
}
