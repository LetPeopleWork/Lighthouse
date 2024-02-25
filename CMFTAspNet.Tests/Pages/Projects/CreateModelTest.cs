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
        public void OnGet_NoId_LoadsAllAvailableTeams_ReturnsPage()
        {
            var subject = CreateSubject();
            var availableTeams = SetupTeams();

            var result = subject.OnGet(null);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.TeamsList.ToList(), Has.Count.EqualTo(availableTeams.Count));
            });
        }

        [Test]
        public void OnGet_ExistingProject_LoadsProjectAndSetsUpInvolvedTeams_ReturnsPage()
        {
            var subject = CreateSubject();
            var availableTeams = SetupTeams();
            var project = new Project { Name = "MyProject", Id = 12 };
            project.InvolvedTeams.Add(availableTeams.Last());

            projectRepositoryMock.Setup(x => x.GetById(12)).Returns(project);

            // Act
            var result = subject.OnGet(12);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.Project, Is.EqualTo(project));
                CollectionAssert.Contains(subject.SelectedTeams, project.InvolvedTeams.Last().Id);
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

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            projectRepositoryMock.Verify(x => x.Update(subject.Project));
            projectRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task OnPost_CreateNew_InvolvedTeamsSelected_GetsInvolvedTeamsAndAddsToProject()
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
