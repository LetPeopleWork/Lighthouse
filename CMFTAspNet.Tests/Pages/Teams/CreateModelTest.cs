using CMFTAspNet.Models;
using CMFTAspNet.Pages.Teams;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace CMFTAspNet.Tests.Pages.Teams
{
    public class CreateModelTest
    {
        private Mock<IRepository<Team>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public void OnGet_ReturnsPage()
        {
            var subject = CreateSubject();

            var result = subject.OnGet();

            Assert.That(result, Is.InstanceOf<PageResult>());
        }

        [Test]
        public async Task OnPost_ModelNotValid_ReturnsPage()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            repositoryMock.Verify(x => x.Add(It.IsAny<Team>()), Times.Never);
            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task OnPost_ModelValid_AddsTeamAndSaves()
        {
            var subject = CreateSubject();
            subject.Team = new Team { Name = "Test" };

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            repositoryMock.Verify(x => x.Add(subject.Team));
            repositoryMock.Verify(x => x.Save());
        }

        private CreateModel CreateSubject()
        {
            return new CreateModel(repositoryMock.Object);
        }
    }
}
