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

        [Test]
        public void FollowersByConnection_CountsOnlyTheOwnersThatFollowTheirParentOverrideDefault()
        {
            WithTheseTeams(
                FollowingTeam(1), FollowingTeam(1), TeamOwning(1, fieldId: 7), TeamOwning(1, fieldId: null),
                FollowingTeam(2), TeamOwning(2, fieldId: 7));
            WithPortfolios(1, 2, 3);

            var followers = CreateSubject().FollowersByConnection();

            Assert.That(followers, Is.EquivalentTo(new Dictionary<int, ConnectionFollowers>
            {
                [1] = new(2, 1, 1, 1, 1),
                [2] = new(1, 1, 1, 1, 1),
                [3] = new(0, 1, 1, 1, 1),
            }));
        }

        [Test]
        public void FollowerGroupsByConnection_CountsEachOwnerOnceUnderTheDefaultsItFollows()
        {
            WithTheseTeams(FollowingTeam(1), FollowingTeam(1), TeamOwning(1, fieldId: 7));
            WithThesePortfolios(
                new Portfolio { WorkTrackingSystemConnectionId = 1 },
                new Portfolio { WorkTrackingSystemConnectionId = 1 },
                new Portfolio { WorkTrackingSystemConnectionId = 1 },
                PortfolioOwningItsDependencyField(1),
                PortfolioOwningEveryField(2));

            var groups = CreateSubject().FollowerGroupsByConnection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groups.Keys, Is.EquivalentTo(OnlyTheFirstConnection));
                Assert.That(groups[1].Select(group => (string.Join("+", group.Follows), group.Count)), Is.EquivalentTo(new[]
                {
                    ("ParentOverrideForTeams", 2),
                    ("ParentOverrideForPortfolios+DependencyFieldForPortfolios+SizeEstimateFieldForPortfolios+FeatureOwnerFieldForPortfolios", 3),
                    ("ParentOverrideForPortfolios+SizeEstimateFieldForPortfolios+FeatureOwnerFieldForPortfolios", 1),
                }));
                Assert.That(CreateSubject().FollowersByConnection()[1], Is.EqualTo(new ConnectionFollowers(2, 4, 3, 4, 4)));
            }
        }

        private static readonly int[] OnlyTheFirstConnection = [1];

        private static Portfolio PortfolioOwningItsDependencyField(int connectionId)
        {
            var portfolio = new Portfolio { WorkTrackingSystemConnectionId = connectionId };
            portfolio.OwnDependencyField(7);
            return portfolio;
        }

        private static Portfolio PortfolioOwningEveryField(int connectionId)
        {
            var portfolio = PortfolioOwningItsDependencyField(connectionId);
            portfolio.OwnParentOverride(null);
            portfolio.OwnSizeEstimateField(null);
            portfolio.OwnFeatureOwnerField(null);
            return portfolio;
        }

        private void WithThesePortfolios(params Portfolio[] portfolios)
        {
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns((Expression<Func<Portfolio, bool>> predicate) => portfolios.AsQueryable().Where(predicate));
        }

        [Test]
        public void FollowersByConnection_ConnectionWhoseTeamsAllOwnTheirValue_IsNotListed()
        {
            WithTheseTeams(TeamOwning(4, fieldId: 7), TeamOwning(4, fieldId: null));

            var followers = CreateSubject().FollowersByConnection();

            Assert.That(followers, Is.Empty);
        }

        private static Team FollowingTeam(int connectionId)
        {
            var team = new Team { WorkTrackingSystemConnectionId = connectionId };
            team.FollowParentOverrideDefault();
            return team;
        }

        private static Team TeamOwning(int connectionId, int? fieldId)
        {
            var team = new Team { WorkTrackingSystemConnectionId = connectionId };
            team.OwnParentOverride(fieldId);
            return team;
        }

        private void WithTheseTeams(params Team[] teams)
        {
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
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
