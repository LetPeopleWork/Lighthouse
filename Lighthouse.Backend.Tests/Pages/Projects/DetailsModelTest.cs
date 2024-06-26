﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Lighthouse.Backend.Pages.Projects;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Pages.Projects
{
    public class DetailsModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public async Task OnPost_IdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPostRefreshFeatures(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            projectRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task OnPost_ProjectDoesNotExist_ReturnsNotFoundAsync()
        {
            projectRepositoryMock.Setup(x => x.GetById(12)).Returns((Project)null);
            var subject = CreateSubject();

            var result = await subject.OnPostRefreshFeatures(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_ProjectExists_UpdatesFeaturesForProject()
        {
            var project = SetupProject();
            var team = new Team();
            var feautre = new Feature { Project = project };
            feautre.RemainingWork.Add(new RemainingWork { Feature = feautre, Team = team });

            project.Features.Add(feautre);

            var subject = CreateSubject();

            var result = await subject.OnPostRefreshFeatures(12);

            Assert.That(result, Is.InstanceOf<PageResult>());
            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project), Times.Once());
            monteCarloServiceMock.Verify(x => x.ForecastFeaturesForTeam(team), Times.Once());
        }

        private DetailsModel CreateSubject()
        {
            return new DetailsModel(projectRepositoryMock.Object, workItemCollectorServiceMock.Object, monteCarloServiceMock.Object);
        }

        private Project SetupProject()
        {
            var project = new Project { Id = 2, Name = "Team" };
            projectRepositoryMock.Setup(x => x.GetById(12)).Returns(project);

            return project;
        }
    }
}
