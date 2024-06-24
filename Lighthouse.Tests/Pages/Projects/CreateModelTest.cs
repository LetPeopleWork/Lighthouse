using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Lighthouse.Backend.Pages.Projects;

namespace Lighthouse.Backend.Tests.Pages.Projects
{
    public class CreateModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
        }

        [Test]
        public void OnGet_ExistingProject_LoadsProject_ReturnsPage()
        {
            var subject = CreateSubject();
            var project = new Project { Name = "MyProject", Id = 12 };

            projectRepositoryMock.Setup(x => x.GetById(12)).Returns(project);

            // Act
            var result = subject.OnGet(12);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.Project, Is.EqualTo(project));
            });
        }

        [Test]
        public async Task OnPost_CreateNew_ModelNotValid_ReturnsPage()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            projectRepositoryMock.Verify(x => x.Add(It.IsAny<Project>()), Times.Never);
            projectRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task OnPost_CreateNew_ModelValid_AddsProjectAndSaves()
        {
            var subject = CreateSubject();
            subject.Project = new Project { Name = "Test" };

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            projectRepositoryMock.Verify(x => x.Add(subject.Project));
            projectRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task OnPost_EditExisting_ModelValid_UpdatesProjectAndSaves()
        {
            var subject = CreateSubject();
            subject.Project = new Project { Name = "Test", Id = 12 };

            var result = await subject.OnPostAsync();
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

                var pageResult = (RedirectToPageResult)result;

                Assert.That(pageResult.PageName, Is.EqualTo("./Details"));

                var routeValues = pageResult.RouteValues ?? throw new InvalidOperationException("Route Values is null");

                Assert.That(routeValues, Has.Count.EqualTo(1));

                var routeValue = routeValues.Single();

                Assert.That(routeValue.Key, Is.EqualTo("id"));
                Assert.That(routeValue.Value, Is.EqualTo(12));

            });

            projectRepositoryMock.Verify(x => x.Update(subject.Project));
            projectRepositoryMock.Verify(x => x.Save());
        }

        private CreateModel CreateSubject()
        {
            return new CreateModel(projectRepositoryMock.Object);
        }
    }
}
