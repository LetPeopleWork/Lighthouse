using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class WorkTrackingSystemConnectionUsageTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            WithTeams();
            WithPortfolios();
        }

        [Test]
        public void ByConnection_NoTeamsOrPortfolios_IsEmpty()
        {
            var usage = CreateSubject().ByConnection();

            Assert.That(usage, Is.Empty);
        }

        [Test]
        public void ByConnection_CountsTeamsAndPortfoliosPerConnection()
        {
            WithTeams(1, 1, 2);
            WithPortfolios(1, 3, 3, 3);

            var usage = CreateSubject().ByConnection();

            Assert.That(usage, Is.EquivalentTo(new Dictionary<int, ConnectionUsage>
            {
                [1] = new(2, 1),
                [2] = new(1, 0),
                [3] = new(0, 3),
            }));
        }

        private void WithTeams(params int[] connectionIds)
        {
            var teams = connectionIds.Select(id => new Team { WorkTrackingSystemConnectionId = id }).ToList();
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
        }

        private void WithPortfolios(params int[] connectionIds)
        {
            var portfolios = connectionIds.Select(id => new Portfolio { WorkTrackingSystemConnectionId = id }).ToList();
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns((Expression<Func<Portfolio, bool>> predicate) => portfolios.AsQueryable().Where(predicate));
        }

        private WorkTrackingSystemConnectionUsage CreateSubject()
        {
            return new WorkTrackingSystemConnectionUsage(teamRepositoryMock.Object, portfolioRepositoryMock.Object);
        }
    }
}
