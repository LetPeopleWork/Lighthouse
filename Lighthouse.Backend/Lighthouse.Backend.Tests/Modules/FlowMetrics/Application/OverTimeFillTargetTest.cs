using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
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

        private const int ThirtyDays = 30;

        private static readonly DateOnly TheEdge = ThatEveningsDay.AddDays(-ThirtyDays);

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

        /// <summary>
        /// An owner that keeps finished work for thirty days, last seen on the 21st, still holds work from
        /// the 22nd of August on. A reading whose window starts the day before that is averaged over
        /// work that is partly gone, so it comes back empty without the numbers being asked for.
        /// </summary>
        [Test]
        public void ATeamsPercentileFamilies_ReadNothingForAWindowReachingPastWhatItKeeps_AndReadAWindowStartingOnTheEdge()
        {
            var owner = new Team { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            var metrics = new Mock<ITeamMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForTeam(owner, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(50, 4)]);
            using var services = ServicesKnowing(team: owner, teamMetrics: metrics.Object);

            var family = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []))!
                .PercentileFamilies.Single(reader => reader.MetricType == MetricType.CycleTime);
            var pastTheEdge = family.ReadPercentiles(Midnight(TheEdge.AddDays(-1)), Midnight(ThatEveningsDay));
            var onTheEdge = family.ReadPercentiles(Midnight(TheEdge), Midnight(ThatEveningsDay));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pastTheEdge, Is.Empty, "A window reaching back past the work the team keeps was read, so it would be averaged over work that is gone.");
                Assert.That(onTheEdge.Single().Value, Is.EqualTo(4), "A window starting on the oldest kept day was refused.");
            }
        }

        [Test]
        public void APortfoliosPercentileFamilies_ReadNothingForAWindowReachingPastWhatItKeeps_AndReadAWindowStartingOnTheEdge()
        {
            var owner = new Portfolio { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            var metrics = new Mock<IPortfolioMetricsService>();
            metrics.Setup(service => service.GetCycleTimePercentilesForPortfolio(owner, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(50, 4)]);
            using var services = ServicesKnowing(portfolio: owner, portfolioMetrics: metrics.Object);

            var family = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, []))!
                .PercentileFamilies.Single(reader => reader.MetricType == MetricType.CycleTime);
            var pastTheEdge = family.ReadPercentiles(Midnight(TheEdge.AddDays(-1)), Midnight(ThatEveningsDay));
            var onTheEdge = family.ReadPercentiles(Midnight(TheEdge), Midnight(ThatEveningsDay));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pastTheEdge, Is.Empty, "A window reaching back past the work the portfolio keeps was read, so it would be averaged over work that is gone.");
                Assert.That(onTheEdge.Single().Value, Is.EqualTo(4), "A window starting on the oldest kept day was refused.");
            }
        }

        [Test]
        public void ATeamsLimitFamilies_AreNotReadyForAStretchReachingPastWhatItKeeps_AndReadyForOneStartingOnTheEdge()
        {
            var team = new Team { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            using var services = ServicesKnowing(team: team, limitFamilies: [AReadyThroughputFamily()]);

            var family = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []))!.ProcessBehaviorFamilies.Single();

            AssertLimitsAreRefusedPastTheEdgeAndReadOnIt(family);
        }

        [Test]
        public void APortfoliosLimitFamilies_AreNotReadyForAStretchReachingPastWhatItKeeps_AndReadyForOneStartingOnTheEdge()
        {
            var portfolio = new Portfolio { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            using var services = ServicesKnowing(portfolio: portfolio, limitFamilies: [AReadyThroughputFamily()]);

            var family = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, []))!.ProcessBehaviorFamilies.Single();

            AssertLimitsAreRefusedPastTheEdgeAndReadOnIt(family);
        }

        /// <summary>
        /// Items that finished long before the edge are deleted at the next refresh, so the first day the
        /// history can support is the edge and not the day the oldest of them finished on.
        /// </summary>
        [Test]
        public void ATeamThatKeepsWorkForThirtyDays_HasNoHistoryBeforeTheEdge()
        {
            var team = new Team { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            using var services = ServicesKnowing(team: team, workItems: [new() { TeamId = OwnerId, ClosedDate = InTheMorningOf(TheEdge.AddDays(-60)) }]);

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Team, []));

            Assert.That(target?.EarliestDayTheItemsSupport(), Is.EqualTo(TheEdge));
        }

        [Test]
        public void APortfolioThatKeepsWorkForThirtyDays_HasNoHistoryBeforeTheEdge()
        {
            var portfolio = new Portfolio { Id = OwnerId, UpdateTime = SeenInTheEvening, DoneItemsCutoffDays = ThirtyDays };
            using var services = ServicesKnowing(portfolio: portfolio, deliveries: [DeliveryOf([portfolio], TheEdge.AddDays(-60))]);

            var target = OverTimeFillTarget.For(services, new OverTimeFillRequest(OwnerId, OwnerType.Portfolio, []));

            Assert.That(target?.EarliestDayTheItemsSupport(), Is.EqualTo(TheEdge));
        }

        private static DateTime Midnight(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        private static ProcessBehaviorFamilyReader AReadyThroughputFamily()
            => new(ProcessBehaviorMetricType.Throughput, 30, (_, _, _) => new ProcessBehaviourChart { Status = BaselineStatus.Ready });

        private static void AssertLimitsAreRefusedPastTheEdgeAndReadOnIt(ProcessBehaviorFamilyReader family)
        {
            var pastTheEdge = family.ReadChart(Midnight(TheEdge.AddDays(-1)), Midnight(ThatEveningsDay), null);
            var onTheEdge = family.ReadChart(Midnight(TheEdge), Midnight(ThatEveningsDay), null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pastTheEdge.Status, Is.EqualTo(BaselineStatus.BaselineInvalid), "Limits drawn from work that is partly gone were read as ready.");
                Assert.That(onTheEdge.Status, Is.EqualTo(BaselineStatus.Ready), "Limits for a stretch starting on the oldest kept day were refused.");
            }
        }

        private static Feature DeliveryOf(List<Portfolio> portfolios, DateOnly? finishedOn)
        {
            var delivery = new Feature { ClosedDate = finishedOn is { } day ? InTheMorningOf(day) : null };
            delivery.Portfolios.AddRange(portfolios);

            return delivery;
        }

        private static DateTime InTheMorningOf(DateOnly day) => day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);

        private static ServiceProvider ServicesKnowing(
            Team? team = null, Portfolio? portfolio = null, List<WorkItem>? workItems = null, List<Feature>? deliveries = null,
            ITeamMetricsService? teamMetrics = null, IPortfolioMetricsService? portfolioMetrics = null,
            IReadOnlyList<ProcessBehaviorFamilyReader>? limitFamilies = null)
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
            limitWriter.Setup(writer => writer.FamiliesFor(It.IsAny<Team>())).Returns(limitFamilies ?? []);
            limitWriter.Setup(writer => writer.FamiliesFor(It.IsAny<Portfolio>())).Returns(limitFamilies ?? []);

            var anHourLaterInLosAngeles = new DateTimeOffset(SeenInTheEvening.AddHours(1));

            return new ServiceCollection()
                .AddSingleton(teams.Object)
                .AddSingleton(portfolios.Object)
                .AddSingleton(teamMetrics ?? Mock.Of<ITeamMetricsService>())
                .AddSingleton(portfolioMetrics ?? Mock.Of<IPortfolioMetricsService>())
                .AddSingleton(storedItems.Object)
                .AddSingleton(storedDeliveries.Object)
                .AddSingleton(limitWriter.Object)
                .AddSingleton<ILighthouseClock>(new FakeLighthouseClock(anHourLaterInLosAngeles, LosAngeles))
                .BuildServiceProvider();
        }
    }
}
