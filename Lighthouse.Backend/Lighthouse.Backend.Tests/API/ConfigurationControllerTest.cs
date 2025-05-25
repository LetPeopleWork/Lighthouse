using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API
{
    public class ConfigurationControllerTest
    {
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemRepositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Project>> projectRepositoryMock;

        private readonly List<WorkTrackingSystemConnection> workTrackingSystems = new List<WorkTrackingSystemConnection>();
        private readonly List<Team> teams = new List<Team>();
        private readonly List<Project> projects = new List<Project>();

        [SetUp]
        public void Setup()
        {
            workTrackingSystemRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();

            workTrackingSystems.Clear();
            workTrackingSystemRepositoryMock.Setup(x => x.GetAll()).Returns(workTrackingSystems);

            teams.Clear();
            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);

            projects.Clear();
            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);
        }

        [Test]
        public void ExportConfiguration_NoWorkTrackingSystems_ReturnsEmptyConfigurationExport()
        {
            var subject = CreateSubject();
            
            var response = subject.ExportConfiguration();
            
            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);

                Assert.That(configuration.WorkTrackingSystems, Is.Empty);
                Assert.That(configuration.Teams, Is.Empty);
                Assert.That(configuration.Projects, Is.Empty);
            });
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_IncludesSystemInExport()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();

            var subject = CreateSubject();
            
            var response = subject.ExportConfiguration();
            
            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];

                Assert.That(exportedWorkTrackingSystem.Id, Is.EqualTo(1));
                Assert.That(exportedWorkTrackingSystem.Name, Is.EqualTo("Test System"));
                Assert.That(exportedWorkTrackingSystem.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Jira));
                Assert.That(exportedWorkTrackingSystem.Options, Is.Empty);
            });
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_IncludesSystemOptions()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option1", Value = "Value1" });
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option2", Value = "Value2" });

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];
                Assert.That(exportedWorkTrackingSystem.Options, Has.Count.EqualTo(2));

                Assert.That(exportedWorkTrackingSystem.Options[0].Key, Is.EqualTo("Option1"));
                Assert.That(exportedWorkTrackingSystem.Options[0].Value, Is.EqualTo("Value1"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Key, Is.EqualTo("Option2"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Value, Is.EqualTo("Value2"));
            });
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_HidesSecretValueOptions()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option1", Value = "Value1" });
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option2", Value = "Value2", IsSecret = true });

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];
                Assert.That(exportedWorkTrackingSystem.Options, Has.Count.EqualTo(2));

                Assert.That(exportedWorkTrackingSystem.Options[0].Key, Is.EqualTo("Option1"));
                Assert.That(exportedWorkTrackingSystem.Options[0].Value, Is.EqualTo("Value1"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Key, Is.EqualTo("Option2"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Value, Is.Empty);
            });
        }

        [Test]
        public void ExportConfiguration_WithTeam_IncludesInExport()
        {
            var team = new Team { 
                Id = 1,
                Name = "Test Team",
                WorkItemQuery = "SELECT * FROM WorkItems WHERE TeamId = 1",
                UseFixedDatesForThroughput = false,
                ThroughputHistory = 63,
                WorkItemTypes = new List<string> { "User Story", "Task" },
                ToDoStates = new List<string> { "To Do", "New" },
                DoingStates = new List<string> { "Analysis", "Active", "Testing" },
                DoneStates = new List<string> { "Done", "Closed" },
                WorkTrackingSystemConnectionId = 12,
                Tags = new List<string> { "Tag1", "Tag2" },
                FeatureWIP = 3,
                AutomaticallyAdjustFeatureWIP = true,
                ServiceLevelExpectationProbability = 73,
                ServiceLevelExpectationRange = 14,
            };
            teams.Add(team);

            var subject = CreateSubject();
            var response = subject.ExportConfiguration();

            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.Teams, Has.Count.EqualTo(1));
                
                var exportedTeam = configuration.Teams[0];

                Assert.That(exportedTeam.Id, Is.EqualTo(1));
                Assert.That(exportedTeam.Name, Is.EqualTo("Test Team"));
                Assert.That(exportedTeam.WorkItemQuery, Is.EqualTo("SELECT * FROM WorkItems WHERE TeamId = 1"));
                Assert.That(exportedTeam.UseFixedDatesForThroughput, Is.False);
                Assert.That(exportedTeam.ThroughputHistory, Is.EqualTo(63));
                Assert.That(exportedTeam.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(exportedTeam.WorkItemTypes[0], Is.EqualTo("User Story"));
                Assert.That(exportedTeam.WorkItemTypes[1], Is.EqualTo("Task"));
                Assert.That(exportedTeam.ToDoStates, Has.Count.EqualTo(2));
                Assert.That(exportedTeam.ToDoStates[0], Is.EqualTo("To Do"));
                Assert.That(exportedTeam.ToDoStates[1], Is.EqualTo("New"));
                Assert.That(exportedTeam.DoingStates, Has.Count.EqualTo(3));
                Assert.That(exportedTeam.DoingStates[0], Is.EqualTo("Analysis"));
                Assert.That(exportedTeam.DoingStates[1], Is.EqualTo("Active"));
                Assert.That(exportedTeam.DoingStates[2], Is.EqualTo("Testing"));
                Assert.That(exportedTeam.DoneStates, Has.Count.EqualTo(2));
                Assert.That(exportedTeam.DoneStates[0], Is.EqualTo("Done"));
                Assert.That(exportedTeam.DoneStates[1], Is.EqualTo("Closed"));
                Assert.That(exportedTeam.WorkTrackingSystemConnectionId, Is.EqualTo(12));
                Assert.That(exportedTeam.Tags, Has.Count.EqualTo(2));
                Assert.That(exportedTeam.Tags[0], Is.EqualTo("Tag1"));
                Assert.That(exportedTeam.Tags[1], Is.EqualTo("Tag2"));
                Assert.That(exportedTeam.FeatureWIP, Is.EqualTo(3));
                Assert.That(exportedTeam.AutomaticallyAdjustFeatureWIP, Is.True);
                Assert.That(exportedTeam.ServiceLevelExpectationProbability, Is.EqualTo(73));
                Assert.That(exportedTeam.ServiceLevelExpectationRange, Is.EqualTo(14));
            });
        }

        [Test]
        public void ExportConfiguration_WithProject_IncludesInExport()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            var team = new Team
            {
                Id = 1,
                Name = "Test Team",
                WorkTrackingSystemConnectionId = workTrackingSystem.Id
            };
            teams.Add(team);

            var project = new Project
            {
                Id = 1,
                Name = "Project A",
                WorkItemQuery = "SELECT * FROM WorkItems WHERE ProjectId = 1",
                WorkItemTypes = new List<string> { "Epic", "Feature" },
                ToDoStates = new List<string> { "To Do", "New" },
                DoingStates = new List<string> { "In Progress", "Testing" },
                DoneStates = new List<string> { "Done", "Closed" },
                WorkTrackingSystemConnectionId = workTrackingSystem.Id,
                Tags = new List<string> { "ProjectTag1", "ProjectTag2" },
                UnparentedItemsQuery = "SELECT * FROM UnparentedItems WHERE ProjectId = 1",
                UsePercentileToCalculateDefaultAmountOfWorkItems = false,
                DefaultAmountOfWorkItemsPerFeature = 14,
                SizeEstimateField = "SizeEstimate",
                OverrideRealChildCountStates = new List<string> { "In Progress", "Testing" },
                OwningTeamId = team.Id,
                OwningTeam = team,
                FeatureOwnerField = "FeatureOwner",
                ServiceLevelExpectationProbability = 85,
                ServiceLevelExpectationRange = 21
            };

            project.Milestones.Add(new Milestone { Id = 1, Name = "Milestone 1", Date = DateTime.UtcNow.AddDays(30) });

            project.Teams.Add(team);

            projects.Add(project);

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();
            
            Assert.Multiple(() =>
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.Projects, Has.Count.EqualTo(1));
                var project = configuration.Projects[0];

                Assert.That(project.Id, Is.EqualTo(1));
                Assert.That(project.Name, Is.EqualTo("Project A"));
                Assert.That(project.WorkItemQuery, Is.EqualTo("SELECT * FROM WorkItems WHERE ProjectId = 1"));
                Assert.That(project.InvolvedTeams, Has.Count.EqualTo(1));
                Assert.That(project.InvolvedTeams[0].Id, Is.EqualTo(1));
                Assert.That(project.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(project.WorkItemTypes[0], Is.EqualTo("Epic"));
                Assert.That(project.WorkItemTypes[1], Is.EqualTo("Feature"));
                Assert.That(project.ToDoStates, Has.Count.EqualTo(2));
                Assert.That(project.ToDoStates[0], Is.EqualTo("To Do"));
                Assert.That(project.ToDoStates[1], Is.EqualTo("New"));
                Assert.That(project.DoingStates, Has.Count.EqualTo(2));
                Assert.That(project.DoingStates[0], Is.EqualTo("In Progress"));
                Assert.That(project.DoingStates[1], Is.EqualTo("Testing"));
                Assert.That(project.DoneStates, Has.Count.EqualTo(2));
                Assert.That(project.DoneStates[0], Is.EqualTo("Done"));
                Assert.That(project.DoneStates[1], Is.EqualTo("Closed"));
                Assert.That(project.WorkTrackingSystemConnectionId, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(project.Tags, Has.Count.EqualTo(2));
                Assert.That(project.Tags[0], Is.EqualTo("ProjectTag1"));
                Assert.That(project.Tags[1], Is.EqualTo("ProjectTag2"));
                Assert.That(project.UnparentedItemsQuery, Is.EqualTo("SELECT * FROM UnparentedItems WHERE ProjectId = 1"));
                Assert.That(project.UsePercentileToCalculateDefaultAmountOfWorkItems, Is.False);
                Assert.That(project.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(14));
                Assert.That(project.SizeEstimateField, Is.EqualTo("SizeEstimate"));
                Assert.That(project.OverrideRealChildCountStates, Has.Count.EqualTo(2));
                Assert.That(project.OverrideRealChildCountStates[0], Is.EqualTo("In Progress"));
                Assert.That(project.OverrideRealChildCountStates[1], Is.EqualTo("Testing"));
                Assert.That(project.OwningTeam.Id, Is.EqualTo(team.Id));
                Assert.That(project.FeatureOwnerField, Is.EqualTo("FeatureOwner"));
                Assert.That(project.ServiceLevelExpectationProbability, Is.EqualTo(85));
                Assert.That(project.ServiceLevelExpectationRange, Is.EqualTo(21));

                Assert.That(project.Milestones, Has.Count.EqualTo(1));
                Assert.That(project.Milestones[0].Id, Is.EqualTo(1));
                Assert.That(project.Milestones[0].Name, Is.EqualTo("Milestone 1"));
                Assert.That(project.Milestones[0].Date.Date, Is.EqualTo(DateTime.UtcNow.AddDays(30).Date));
            });
        }

        private WorkTrackingSystemConnection AddWorkTrackingSystemConnection()
        {
            var workTrackingSystem = new WorkTrackingSystemConnection
            {
                Id = 1,
                Name = "Test System",
                WorkTrackingSystem = WorkTrackingSystems.Jira
            };

            workTrackingSystems.Add(workTrackingSystem);

            return workTrackingSystem;
        }

        private ConfigurationExport ParseExportResponse(IActionResult response)
        {
            Assert.That(response, Is.InstanceOf<FileContentResult>());
            var fileResult = response as FileContentResult;
            Assert.That(fileResult.ContentType, Is.EqualTo("text/json"));
            var json = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
            return JsonSerializer.Deserialize<ConfigurationExport>(json) ?? throw new InvalidOperationException("Could not parse response");
        }

        private ConfigurationController CreateSubject()
        {
            return new ConfigurationController(Mock.Of<ILogger<ConfigurationController>>(), workTrackingSystemRepositoryMock.Object, teamRepositoryMock.Object, projectRepositoryMock.Object);
        }
    }
}
