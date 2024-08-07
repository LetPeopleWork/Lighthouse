﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class ProjectRepositoryTest : IntegrationTestBase
    {
        public ProjectRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task GetProjectById_ProjectHasInvolvedTeams_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };

            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Key", Value = "Value" });


            var project = new Project { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var team = new Team { Name = "Team1", WorkTrackingSystemConnection = workTrackingSystemConnection };

            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            var feature = new Feature { Name = "Feature", Order = "12" };
            feature.FeatureWork.Add(new FeatureWork(team, 12, 37, feature));

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);
            
            Assert.Multiple(() =>
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.InvolvedTeams.ToList(), Has.Count.EqualTo(1));
                Assert.That(foundProject.InvolvedTeams.Single().WorkTrackingSystemConnection.Options, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task GetProjectById_ProjectHasFeatures_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };

            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Key", Value = "Value" });

            var project = new Project { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var feature = new Feature { Name = "MyFeature", Order = "12" };

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.Features, Has.Count.EqualTo(1));
            });
        }

        private ProjectRepository CreateSubject()
        {
            return new ProjectRepository(DatabaseContext, Mock.Of<ILogger<ProjectRepository>>());
        }
    }
}
