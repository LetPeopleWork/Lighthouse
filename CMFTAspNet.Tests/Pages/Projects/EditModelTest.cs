using CMFTAspNet.Models;
using CMFTAspNet.Pages.Projects;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CMFTAspNet.Tests.Pages.Projects
{
    public class EditModelTest
    {
        private Mock<IRepository<Project>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Project>>();
        }

        [Test]
        public async Task OnPostAsync_ModelStateInvalid_DoesNotUpdateAsync()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            repositoryMock.Verify(x => x.Save(), Times.Never());
        }

        [Test]
        public async Task OnPostAsync_ModelStateValid_UpdatesTeam()
        {
            var project = new Project { Id = 12, SearchTerm = "search by" };

            var subject = CreateSubject();
            subject.Entity = project;

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            repositoryMock.Verify(x => x.Update(project), Times.Once());
            repositoryMock.Verify(x => x.Save(), Times.Once());
        }

        private EditModel CreateSubject()
        {
            return new EditModel(repositoryMock.Object);
        }
    }
}
