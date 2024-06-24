using Lighthouse.Backend.Models;
using Lighthouse.Backend.Pages.Teams;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.Pages.Teams
{
    public class IndexModelTest
    {
        [Test]
        public void OnGet_ListsAllAvailableTeams()
        {
            var team1 = new Team { Name = "Senna" };
            var team2 = new Team { Name = "Prost" };

            var expectedTeams = new List<Team>()
            {
                team1,
                team2,
            };

            var teamsRepositoryMock = new Mock<IRepository<Team>>();
            teamsRepositoryMock.Setup(x => x.GetAll()).Returns(expectedTeams);

            var subject = new IndexModel(teamsRepositoryMock.Object);

            // Act
            subject.OnGet();

            CollectionAssert.AreEqual(expectedTeams, subject.Teams);
        }
    }
}
