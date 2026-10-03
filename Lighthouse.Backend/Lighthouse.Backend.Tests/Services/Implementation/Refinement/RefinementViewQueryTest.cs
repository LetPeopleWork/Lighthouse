using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class RefinementViewQueryTest
    {
        private const int TeamId = 12;

        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private RefinementViewQuery subject;

        [SetUp]
        public void SetUp()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamMetricsServiceMock
                .Setup(service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(85, 12)]);

            var workItemRepositoryMock = new Mock<IWorkItemRepository>();
            workItemRepositoryMock
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns(Enumerable.Empty<WorkItem>().AsQueryable());

            var clockMock = new Mock<ILighthouseClock>();
            clockMock.Setup(clock => clock.Today).Returns(new DateOnly(2026, 10, 3));

            subject = new RefinementViewQuery(
                teamRepositoryMock.Object,
                new RefinementList(workItemRepositoryMock.Object),
                new SleYardstickResolver(teamMetricsServiceMock.Object, clockMock.Object));
        }

        [Test]
        public void ATeamWithoutRefinementStatesHasNoYardstickAndItsCycleTimeIsNeverWorkedOut()
        {
            GivenTheTeam(new Team { Id = TeamId });

            var view = subject.ForTeam(TeamId);

            Assert.That(view?.Yardstick, Is.EqualTo(Yardstick.None));
            teamMetricsServiceMock.Verify(
                service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Test]
        public void ATeamThatRefinesWithoutAnSleGetsItsCycleTimeFallback()
        {
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
            });

            var view = subject.ForTeam(TeamId);

            Assert.That(view?.Yardstick, Is.EqualTo(new Yardstick(YardstickSource.CycleTimeFallback, 12, 85)));
        }

        private void GivenTheTeam(Team team)
            => teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);
    }
}
