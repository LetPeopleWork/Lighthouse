using CMFTAspNet.Models;
using CMFTAspNet.Pages.Teams;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CMFTAspNet.Tests.Pages.Teams
{
    public class DeleteModelTest
    {
        private Mock<IRepository<Team>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public async Task OnPost_TeamIdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPostAsync(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_TeamIdSet_RemovesTeamAndSaves()
        {
            var team = new Team { Id = 42 };

            var subject = CreateSubject();

            var result = await subject.OnPostAsync(team.Id);

            repositoryMock.Verify(x => x.Remove(team.Id));
            repositoryMock.Verify(x => x.Save());
        }

        private DeleteModel CreateSubject()
        {
            return new DeleteModel(repositoryMock.Object);
        }
    }
}
