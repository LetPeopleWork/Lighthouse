using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CMFTAspNet.Pages.Projects;

namespace CMFTAspNet.Tests.Pages.Projects
{
    public class CreateModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public void OnGet_LoadsAllAvailableTeams_ReturnsPage()
        {
            var subject = CreateSubject();
            var availableTeams = SetupTeams();

            var result = subject.OnGet();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.TeamsList.ToList(), Has.Count.EqualTo(availableTeams.Count));
            });
        }

        [Test]
        public async Task OnPost_ModelNotValid_ReturnsPage()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            projectRepositoryMock.Verify(x => x.Add(It.IsAny<Project>()), Times.Never);
            projectRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task OnPost_ModelValid_AddsProjectAndSaves()
        {
            var subject = CreateSubject();
            subject.Project = new Project { Name = "Test" };

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            projectRepositoryMock.Verify(x => x.Add(subject.Project));
            projectRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task OnPost_InvolvedTeamsSelected_GetsInvolvedTeamsAndAddsToProject()
        {
            var subject = CreateSubject();
            subject.Project = new Project { Name = "Test" };
            var selectedTeam = SetupTeams().Single(x => x.Id == 42);

            subject.SelectedTeams.Add(42);

            teamRepositoryMock.Setup(x => x.GetById(42)).Returns(selectedTeam);

            var result = await subject.OnPostAsync();

            Assert.That(subject.Project.InvolvedTeams, Has.Count.EqualTo(1));
            Assert.That(subject.Project.InvolvedTeams.Single(), Is.EqualTo(selectedTeam));
        }

        private CreateModel CreateSubject()
        {
            return new CreateModel(projectRepositoryMock.Object, teamRepositoryMock.Object);
        }

        private List<Team> SetupTeams()
        {
            var teams = new List<Team>
            {
                new Team { Id = 12, Name = "Team1" },
                new Team { Id = 42, Name = "Team2" },
            };


            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);

            return teams;
        }
    }
}
