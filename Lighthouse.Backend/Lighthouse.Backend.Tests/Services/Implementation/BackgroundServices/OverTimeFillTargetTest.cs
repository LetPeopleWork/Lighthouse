using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// The last day an owner was observed is the ceiling no pass writes past, and it is a calendar day
    /// of the instance. The owner's update time is an instant, so which day it falls on depends on the
    /// instance's zone - and west of UTC, an evening refresh lands on the next UTC date.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class OverTimeFillTargetTest
    {
        private const int OwnerId = 3;

        /// <summary>20:00 on the 21st in Los Angeles, already the 22nd in UTC.</summary>
        private static readonly DateTime SeenInTheEvening = new(2026, 9, 22, 3, 0, 0, DateTimeKind.Utc);

        private static readonly DateOnly ThatEveningsDay = new(2026, 9, 21);

        private static readonly TimeZoneInfo LosAngeles = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

        [Test]
        public void ATeamLastSeenInTheEveningWestOfUtc_WasLastObservedOnThatEveningsDay()
        {
            using var services = ServicesKnowing(team: new Team { Id = OwnerId, UpdateTime = SeenInTheEvening });

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []));

            Assert.That(target?.LastObservedOn, Is.EqualTo(ThatEveningsDay));
        }

        [Test]
        public void APortfolioLastSeenInTheEveningWestOfUtc_WasLastObservedOnThatEveningsDay()
        {
            using var services = ServicesKnowing(portfolio: new Portfolio { Id = OwnerId, UpdateTime = SeenInTheEvening });

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, []));

            Assert.That(target?.LastObservedOn, Is.EqualTo(ThatEveningsDay));
        }

        [Test]
        public void ATeamThatNoLongerExists_HasNothingToFill()
        {
            using var services = ServicesKnowing();

            Assert.That(OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, [])), Is.Null);
        }

        [Test]
        public void APortfolioThatNoLongerExists_HasNothingToFill()
        {
            using var services = ServicesKnowing();

            Assert.That(OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, [])), Is.Null);
        }

        /// <summary>
        /// Another team's work and work still open say nothing about where this team's history begins.
        /// </summary>
        [Test]
        public void ATeamsHistory_BeginsOnTheEarliestDayOneOfItsOwnItemsFinished()
        {
            var team = new Team { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = 0 };
            List<WorkItem> items =
            [
                new() { TeamId = OwnerId, ClosedDate = InTheMorningOf(new DateOnly(2026, 6, 20)) },
                new() { TeamId = OwnerId, ClosedDate = InTheMorningOf(new DateOnly(2026, 5, 4)) },
                new() { TeamId = OwnerId + 1, ClosedDate = InTheMorningOf(new DateOnly(2026, 1, 1)) },
                new() { TeamId = OwnerId, ClosedDate = null },
            ];
            using var services = ServicesKnowing(team: team, workItems: items);

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []));

            Assert.That(target?.EarliestDayTheItemsSupport(), Is.EqualTo(new DateOnly(2026, 5, 4)));
        }

        [Test]
        public void ATeamThatHasNeverFinishedAnything_HasNoHistoryToFillFrom()
        {
            var team = new Team { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = 0 };
            using var services = ServicesKnowing(team: team, workItems: [new() { TeamId = OwnerId, ClosedDate = null }]);

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []));

            Assert.That(target?.EarliestDayTheItemsSupport(), Is.Null);
        }

        /// <summary>
        /// A delivery shared with another portfolio belongs to this one too; one that only another
        /// portfolio holds, and one still open, say nothing about where this portfolio's history begins.
        /// </summary>
        [Test]
        public void APortfoliosHistory_BeginsOnTheEarliestDayOneOfItsOwnDeliveriesFinished()
        {
            var portfolio = new Portfolio { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = 0 };
            var elsewhere = new Portfolio { Id = OwnerId + 1 };
            List<Feature> deliveries =
            [
                DeliveryOf([portfolio], new DateOnly(2026, 6, 20)),
                DeliveryOf([portfolio, elsewhere], new DateOnly(2026, 5, 4)),
                DeliveryOf([elsewhere], new DateOnly(2026, 1, 1)),
                DeliveryOf([portfolio], null),
            ];
            using var services = ServicesKnowing(portfolio: portfolio, deliveries: deliveries);

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, []));

            Assert.That(target?.EarliestDayTheItemsSupport(), Is.EqualTo(new DateOnly(2026, 5, 4)));
        }

        private static Feature DeliveryOf(List<Portfolio> portfolios, DateOnly? finishedOn)
        {
            var delivery = new Feature { ClosedDate = finishedOn is { } day ? InTheMorningOf(day) : null };
            delivery.Portfolios.AddRange(portfolios);

            return delivery;
        }

        private static DateTime InTheMorningOf(DateOnly day) => day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);

        private static ServiceProvider ServicesKnowing(
            Team? team = null, Portfolio? portfolio = null, List<WorkItem>? workItems = null, List<Feature>? deliveries = null)
        {
            var storedItems = new Mock<IWorkItemRepository>();
            storedItems
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => (workItems ?? []).AsQueryable().Where(predicate));

            var storedDeliveries = new Mock<IRepository<Feature>>();
            storedDeliveries
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => (deliveries ?? []).AsQueryable().Where(predicate));

            var teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.GetById(OwnerId)).Returns(team);

            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(repository => repository.GetById(OwnerId)).Returns(portfolio);

            var limitWriter = new Mock<IProcessBehaviorSnapshotWriter>();
            limitWriter.Setup(writer => writer.FamiliesFor(It.IsAny<Team>())).Returns([]);
            limitWriter.Setup(writer => writer.FamiliesFor(It.IsAny<Portfolio>())).Returns([]);

            var anHourLaterInLosAngeles = new DateTimeOffset(SeenInTheEvening.AddHours(1));

            return new ServiceCollection()
                .AddSingleton(teams.Object)
                .AddSingleton(portfolios.Object)
                .AddSingleton(Mock.Of<ITeamMetricsService>())
                .AddSingleton(Mock.Of<IPortfolioMetricsService>())
                .AddSingleton(storedItems.Object)
                .AddSingleton(storedDeliveries.Object)
                .AddSingleton(limitWriter.Object)
                .AddSingleton<ILighthouseClock>(new FakeLighthouseClock(anHourLaterInLosAngeles, LosAngeles))
                .BuildServiceProvider();
        }
    }
}
