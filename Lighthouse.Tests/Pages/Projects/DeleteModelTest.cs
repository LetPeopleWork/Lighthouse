using Lighthouse.Models;
using Lighthouse.Pages.Projects;
using Lighthouse.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Tests.Pages.Projects
{
    public class DeleteModelTest
    {
        private Mock<IRepository<Project>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Project>>();
        }

        [Test]
        public async Task OnPost_ProjectIdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPostAsync(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_ProjectIdSet_RemovesProjectAndSaves()
        {
            var project = new Project { Id = 42 };

            var subject = CreateSubject();

            var result = await subject.OnPostAsync(project.Id);

            repositoryMock.Verify(x => x.Remove(project.Id));
            repositoryMock.Verify(x => x.Save());
        }

        private DeleteModel CreateSubject()
        {
            return new DeleteModel(repositoryMock.Object);
        }
    }
}
