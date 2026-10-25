using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListRowsTest
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        private static readonly string[] OnlyStrategy = ["Strategy"];

        private Mock<IBlockedItemService> blockedMock = null!;
        private Portfolio platform = null!;
        private Portfolio strategy = null!;
        private Feature checkout = null!;

        [SetUp]
        public void SetUp()
        {
            platform = new Portfolio { Id = 1, Name = "Platform" };
            strategy = new Portfolio { Id = 2, Name = "Strategy" };

            checkout = new Feature(new WorkItemBase { ReferenceId = "NW-1", Name = "Checkout redesign", State = "In Progress", StateCategory = StateCategories.Doing })
            {
                Id = 1,
            };
            checkout.Portfolios.Add(platform);
            checkout.Portfolios.Add(strategy);

            blockedMock = new Mock<IBlockedItemService>();
        }

        [Test]
        public void A_Feature_held_by_two_Portfolios_is_blocked_when_only_one_of_them_blocks_it()
        {
            blockedMock.Setup(blocked => blocked.IsBlocked(checkout, strategy)).Returns(true);

            var list = TheRows().Of(ARead(FeatureMoveVerdict.Allowed));

            Assert.That(list.Features.Single().IsBlocked, Is.True);
        }

        [Test]
        public void A_row_names_the_Portfolios_that_stand_in_the_way_of_moving_it()
        {
            var refused = new FeatureMoveVerdict(false, FeatureMoveVerdict.NoWriteOnEveryPortfolio, [strategy]);

            var list = TheRows().Of(ARead(refused));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Features.Single().CanMove, Is.False);
                Assert.That(list.Features.Single().BlockingPortfolios.Select(portfolio => portfolio.Name), Is.EqualTo(OnlyStrategy));
            }
        }

        private FeatureListRows TheRows()
        {
            var blackouts = new Mock<IBlackoutPeriodService>();
            blackouts.Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);

            return new FeatureListRows(blackouts.Object, blockedMock.Object, new FakeLighthouseClock(Now));
        }

        private FeatureListRead ARead(FeatureMoveVerdict verdict)
            => new(
                [checkout],
                [1, 2],
                new Dictionary<int, int> { [1] = 1 },
                new Dictionary<int, FeatureMoveVerdict> { [1] = verdict },
                new DependenciesAsRead([], [1, 2], []),
                [],
                []);
    }
}
