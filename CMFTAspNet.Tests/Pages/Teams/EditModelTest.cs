using CMFTAspNet.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CMFTAspNet.Pages.Teams;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Tests.Pages.Teams
{
    public class EditModelTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public async Task OnPostAsync_ModelStateInvalid_DoesNotUpdateAsync()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            teamRepositoryMock.Verify(x => x.Save(), Times.Never());
        }

        [Test]
        public async Task OnPostAsync_ModelStateValid_UpdatesTeam()
        {
            var team = new Team { Id = 12, FeatureWIP = 2 };

            var subject = CreateSubject();
            subject.Entity = team;

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            teamRepositoryMock.Verify(x => x.Update(team), Times.Once());
            teamRepositoryMock.Verify(x => x.Save(), Times.Once());
        }

        private EditModel CreateSubject()
        {
            return new EditModel(teamRepositoryMock.Object);
        }
    }
}
