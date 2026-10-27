using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ReportOwnerLookupTest
    {
        private const int SharedId = 7;

        private ReportOwnerLookup subject = null!;

        [SetUp]
        public void SetUp()
        {
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.GetById(SharedId)).Returns(new Team { Id = SharedId, Name = "Team Lightspeed", DoneItemsCutoffDays = 180 });

            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(repository => repository.GetById(SharedId)).Returns(new Portfolio { Id = SharedId, Name = "Project Apollo", DoneItemsCutoffDays = 365 });

            teams.Setup(repository => repository.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new[] { new Team { Id = SharedId, Name = "Team Lightspeed", DoneItemsCutoffDays = 180 } }.AsQueryable());
            portfolios.Setup(repository => repository.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<Portfolio, bool>>>()))
                .Returns(new[] { new Portfolio { Id = SharedId, Name = "Project Apollo", DoneItemsCutoffDays = 365 } }.AsQueryable());

            subject = new ReportOwnerLookup(teams.Object, portfolios.Object);
        }

        [Test]
        public void A_Team_is_found_among_the_Teams()
        {
            Assert.That(subject.Find(ReportOwner.Team(SharedId)), Is.EqualTo(new ReportOwnerFacts(180)));
        }

        [Test]
        public void A_Portfolio_is_found_among_the_Portfolios_never_as_the_Team_sharing_its_id()
        {
            Assert.That(subject.Find(ReportOwner.Portfolio(SharedId)), Is.EqualTo(new ReportOwnerFacts(365)));
        }

        [Test]
        public void Every_owner_is_listed_with_its_kind_id_name_and_cutoff_without_loading_the_owners()
        {
            Assert.That(subject.All(), Is.EqualTo(new[]
            {
                new ReportOwnerRow(ReportOwner.Team(SharedId), "Team Lightspeed", 180),
                new ReportOwnerRow(ReportOwner.Portfolio(SharedId), "Project Apollo", 365),
            }));
        }

        [Test]
        public void An_owner_that_does_not_exist_is_not_found()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Find(ReportOwner.Team(SharedId + 1)), Is.Null);
                Assert.That(subject.Find(ReportOwner.Portfolio(SharedId + 1)), Is.Null);
            }
        }
    }
}
