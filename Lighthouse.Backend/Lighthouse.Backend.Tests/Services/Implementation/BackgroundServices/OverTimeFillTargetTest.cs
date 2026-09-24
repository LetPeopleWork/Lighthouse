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

        private static ServiceProvider ServicesKnowing(Team? team = null, Portfolio? portfolio = null)
        {
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
                .AddSingleton(Mock.Of<IWorkItemRepository>())
                .AddSingleton(Mock.Of<IRepository<Feature>>())
                .AddSingleton(limitWriter.Object)
                .AddSingleton<ILighthouseClock>(new FakeLighthouseClock(anHourLaterInLosAngeles, LosAngeles))
                .BuildServiceProvider();
        }
    }
}
