using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        private readonly JsonSerializerOptions deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        [SetUp]
        public void Setup()
        {
            workTrackingSystemRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();

            workTrackingSystems.Clear();
            workTrackingSystemRepositoryMock.Setup(x => x.GetAll()).Returns(workTrackingSystems);
            workTrackingSystemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkTrackingSystemConnection, bool>>>()))
                .Returns((Expression<Func<WorkTrackingSystemConnection, bool>> predicate) => workTrackingSystems.Where(predicate.Compile()).AsQueryable());

            workTrackingSystemRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((int id) => workTrackingSystems.SingleOrDefault(wts => wts.Id == id));

            teams.Clear();
            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.Where(predicate.Compile()).AsQueryable());

            projects.Clear();
            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);
            projectRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Project, bool>>>()))
                .Returns((Expression<Func<Project, bool>> predicate) => projects.Where(predicate.Compile()).AsQueryable());
        }

        [Test]
        public void ExportConfiguration_NoWorkTrackingSystems_ReturnsEmptyConfigurationExport()
        {
            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);

                Assert.That(configuration.WorkTrackingSystems, Is.Empty);
                Assert.That(configuration.Teams, Is.Empty);
                Assert.That(configuration.Projects, Is.Empty);
            };
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_IncludesSystemInExport()
        {
            AddWorkTrackingSystemConnection();

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];

                Assert.That(exportedWorkTrackingSystem.Id, Is.EqualTo(1));
                Assert.That(exportedWorkTrackingSystem.Name, Is.EqualTo("Test System"));
                Assert.That(exportedWorkTrackingSystem.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Jira));
                Assert.That(exportedWorkTrackingSystem.Options, Is.Empty);
            };
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_ExcludesCsvSystemInExport()
        {
            AddWorkTrackingSystemConnection();
            var csvWorkTrackingSystem = new WorkTrackingSystemConnection
            {
                Id = 12,
                Name = "CSV",
                WorkTrackingSystem = WorkTrackingSystems.Csv
            };
            workTrackingSystems.Add(csvWorkTrackingSystem);

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];

                Assert.That(exportedWorkTrackingSystem.Id, Is.EqualTo(1));
                Assert.That(exportedWorkTrackingSystem.Name, Is.EqualTo("Test System"));
                Assert.That(exportedWorkTrackingSystem.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Jira));
                Assert.That(exportedWorkTrackingSystem.Options, Is.Empty);
            };
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_IncludesSystemOptions()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option1", Value = "Value1" });
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option2", Value = "Value2" });

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];
                Assert.That(exportedWorkTrackingSystem.Options, Has.Count.EqualTo(2));

                Assert.That(exportedWorkTrackingSystem.Options[0].Key, Is.EqualTo("Option1"));
                Assert.That(exportedWorkTrackingSystem.Options[0].Value, Is.EqualTo("Value1"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Key, Is.EqualTo("Option2"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Value, Is.EqualTo("Value2"));
            };
        }

        [Test]
        public void ExportConfiguration_WithWorkTrackingSystem_HidesSecretValueOptions()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option1", Value = "Value1" });
            workTrackingSystem.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option2", Value = "Value2", IsSecret = true });

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.WorkTrackingSystems, Has.Count.EqualTo(1));

                var exportedWorkTrackingSystem = configuration.WorkTrackingSystems[0];
                Assert.That(exportedWorkTrackingSystem.Options, Has.Count.EqualTo(2));

                Assert.That(exportedWorkTrackingSystem.Options[0].Key, Is.EqualTo("Option1"));
                Assert.That(exportedWorkTrackingSystem.Options[0].Value, Is.EqualTo("Value1"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Key, Is.EqualTo("Option2"));
                Assert.That(exportedWorkTrackingSystem.Options[1].Value, Is.Empty);
            };
        }

        [Test]
        public void ExportConfiguration_WithTeam_IncludesInExport()
        {
            var workTrackingSystemConnection = AddWorkTrackingSystemConnection();

            var team = new Team
            {
                Id = 1,
                Name = "Test Team",
                WorkItemQuery = "SELECT * FROM WorkItems WHERE TeamId = 1",
                UseFixedDatesForThroughput = false,
                ThroughputHistory = 63,
                WorkItemTypes = new List<string> { "User Story", "Task" },
                ToDoStates = new List<string> { "To Do", "New" },
                DoingStates = new List<string> { "Analysis", "Active", "Testing" },
                DoneStates = new List<string> { "Done", "Closed" },
                WorkTrackingSystemConnectionId = workTrackingSystemConnection.Id,
                Tags = new List<string> { "Tag1", "Tag2" },
                FeatureWIP = 3,
                AutomaticallyAdjustFeatureWIP = true,
                ServiceLevelExpectationProbability = 73,
                ServiceLevelExpectationRange = 14,
                SystemWIPLimit = 5,
            };
            teams.Add(team);

            var subject = CreateSubject();
            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
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
                Assert.That(exportedTeam.WorkTrackingSystemConnectionId, Is.EqualTo(workTrackingSystemConnection.Id));
                Assert.That(exportedTeam.Tags, Has.Count.EqualTo(2));
                Assert.That(exportedTeam.Tags[0], Is.EqualTo("Tag1"));
                Assert.That(exportedTeam.Tags[1], Is.EqualTo("Tag2"));
                Assert.That(exportedTeam.FeatureWIP, Is.EqualTo(3));
                Assert.That(exportedTeam.AutomaticallyAdjustFeatureWIP, Is.True);
                Assert.That(exportedTeam.ServiceLevelExpectationProbability, Is.EqualTo(73));
                Assert.That(exportedTeam.ServiceLevelExpectationRange, Is.EqualTo(14));
                Assert.That(exportedTeam.SystemWIPLimit, Is.EqualTo(5));
            };
        }

        [Test]
        public void ExportConfiguration_WithTeam_IgnoresIfUsesCsv()
        {
            var workTrackingSystem = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Csv,
                Id = 12
            };
            workTrackingSystems.Add(workTrackingSystem);

            var team = new Team
            {
                Id = 1,
                Name = "CSV Team",
                WorkItemQuery = "",
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
                SystemWIPLimit = 5,
            };

            teams.Add(team);

            var subject = CreateSubject();
            var response = subject.ExportConfiguration();

            var configuration = ParseExportResponse(response);
            Assert.That(configuration.Teams, Has.Count.EqualTo(0));
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
                ServiceLevelExpectationRange = 21,
                SystemWIPLimit = 1,
            };

            project.Milestones.Add(new Milestone { Id = 1, Name = "Milestone 1", Date = DateTime.UtcNow.AddDays(30) });

            project.Teams.Add(team);

            projects.Add(project);

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            using (Assert.EnterMultipleScope())
            {
                var configuration = ParseExportResponse(response);
                Assert.That(configuration.Projects, Has.Count.EqualTo(1));
                var projectToVerify = configuration.Projects[0];

                Assert.That(projectToVerify.Id, Is.EqualTo(1));
                Assert.That(projectToVerify.Name, Is.EqualTo("Project A"));
                Assert.That(projectToVerify.WorkItemQuery, Is.EqualTo("SELECT * FROM WorkItems WHERE ProjectId = 1"));
                Assert.That(projectToVerify.InvolvedTeams, Has.Count.EqualTo(1));
                Assert.That(projectToVerify.InvolvedTeams[0].Id, Is.EqualTo(1));
                Assert.That(projectToVerify.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.WorkItemTypes[0], Is.EqualTo("Epic"));
                Assert.That(projectToVerify.WorkItemTypes[1], Is.EqualTo("Feature"));
                Assert.That(projectToVerify.ToDoStates, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.ToDoStates[0], Is.EqualTo("To Do"));
                Assert.That(projectToVerify.ToDoStates[1], Is.EqualTo("New"));
                Assert.That(projectToVerify.DoingStates, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.DoingStates[0], Is.EqualTo("In Progress"));
                Assert.That(projectToVerify.DoingStates[1], Is.EqualTo("Testing"));
                Assert.That(projectToVerify.DoneStates, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.DoneStates[0], Is.EqualTo("Done"));
                Assert.That(projectToVerify.DoneStates[1], Is.EqualTo("Closed"));
                Assert.That(projectToVerify.WorkTrackingSystemConnectionId, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(projectToVerify.Tags, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.Tags[0], Is.EqualTo("ProjectTag1"));
                Assert.That(projectToVerify.Tags[1], Is.EqualTo("ProjectTag2"));
                Assert.That(projectToVerify.UnparentedItemsQuery, Is.EqualTo("SELECT * FROM UnparentedItems WHERE ProjectId = 1"));
                Assert.That(projectToVerify.UsePercentileToCalculateDefaultAmountOfWorkItems, Is.False);
                Assert.That(projectToVerify.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(14));
                Assert.That(projectToVerify.SizeEstimateField, Is.EqualTo("SizeEstimate"));
                Assert.That(projectToVerify.OverrideRealChildCountStates, Has.Count.EqualTo(2));
                Assert.That(projectToVerify.OverrideRealChildCountStates[0], Is.EqualTo("In Progress"));
                Assert.That(projectToVerify.OverrideRealChildCountStates[1], Is.EqualTo("Testing"));
                Assert.That(projectToVerify.OwningTeam.Id, Is.EqualTo(team.Id));
                Assert.That(projectToVerify.FeatureOwnerField, Is.EqualTo("FeatureOwner"));
                Assert.That(projectToVerify.ServiceLevelExpectationProbability, Is.EqualTo(85));
                Assert.That(projectToVerify.ServiceLevelExpectationRange, Is.EqualTo(21));
                Assert.That(projectToVerify.SystemWIPLimit, Is.EqualTo(1));

                Assert.That(projectToVerify.Milestones, Has.Count.EqualTo(1));
                Assert.That(projectToVerify.Milestones[0].Id, Is.EqualTo(1));
                Assert.That(projectToVerify.Milestones[0].Name, Is.EqualTo("Milestone 1"));
                Assert.That(projectToVerify.Milestones[0].Date.Date, Is.EqualTo(DateTime.UtcNow.AddDays(30).Date));
            };
        }

        [Test]
        public void ExportConfiguration_WithProject_IgnoresIfUsesCsv()
        {
            var workTrackingSystem = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Csv,
                Id = 12
            };
            workTrackingSystems.Add(workTrackingSystem);

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
                ServiceLevelExpectationRange = 21,
                SystemWIPLimit = 1,
            };

            project.Milestones.Add(new Milestone { Id = 1, Name = "Milestone 1", Date = DateTime.UtcNow.AddDays(30) });

            project.Teams.Add(team);

            projects.Add(project);

            var subject = CreateSubject();

            var response = subject.ExportConfiguration();

            var configuration = ParseExportResponse(response);
            Assert.That(configuration.Projects, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task DeleteConfiguration_RemovesExistingConfiguration()
        {
            var subject = CreateSubject();
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            teams.Add(team);

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            projects.Add(project);

            await subject.DeleteConfiguration();

            projectRepositoryMock.Verify(x => x.Remove(project.Id), Times.Once);
            projectRepositoryMock.Verify(x => x.Save(), Times.Once);

            teamRepositoryMock.Verify(x => x.Remove(team.Id), Times.Once);
            teamRepositoryMock.Verify(x => x.Save(), Times.Once);

            workTrackingSystemRepositoryMock.Verify(x => x.Remove(workTrackingSystem.Id), Times.Once);
            workTrackingSystemRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task DeleteConfiguration_DoesNotRemoveCsvWorkTrackingSystemConnection()
        {
            var subject = CreateSubject();
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystem.WorkTrackingSystem = WorkTrackingSystems.Csv;

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            teams.Add(team);

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            projects.Add(project);

            await subject.DeleteConfiguration();

            workTrackingSystemRepositoryMock.Verify(x => x.Remove(workTrackingSystem.Id), Times.Never);
            workTrackingSystemRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public void ValidateConfiguration_GivenNoConfig_ReturnsAllItemsAreNew()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystems.Clear();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            project.Teams.Add(team);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Teams = new List<TeamSettingDto> { new TeamSettingDto(team) },
                Projects = new List<ProjectSettingDto> { new ProjectSettingDto(project) },
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());

                var validationResult = okResult.Value as ConfigurationValidationDto;
                Assert.That(validationResult.WorkTrackingSystems, Has.Count.EqualTo(1));
                Assert.That(validationResult.WorkTrackingSystems[0].Id, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(validationResult.WorkTrackingSystems[0].Status, Is.EqualTo(ValidationStatus.New));
                Assert.That(validationResult.WorkTrackingSystems[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Teams, Has.Count.EqualTo(1));
                Assert.That(validationResult.Teams[0].Id, Is.EqualTo(team.Id));
                Assert.That(validationResult.Teams[0].Status, Is.EqualTo(ValidationStatus.New));
                Assert.That(validationResult.Teams[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Projects, Has.Count.EqualTo(1));
                Assert.That(validationResult.Projects[0].Id, Is.EqualTo(project.Id));
                Assert.That(validationResult.Projects[0].Status, Is.EqualTo(ValidationStatus.New));
                Assert.That(validationResult.Projects[0].ErrorMessage, Is.Empty);
            };
        }

        [Test]
        public void ValidateConfiguration_GivenExistingConfig_ReturnsAllNeedUpdate()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = 0 };
            teams.Add(team);

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = 0 };
            project.Teams.Add(team);
            projects.Add(project);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) { Id = 0 } },
                Teams = new List<TeamSettingDto> { new TeamSettingDto(team) { Id = 1 } },
                Projects = new List<ProjectSettingDto> { new ProjectSettingDto(project) { Id = 0 } },
            };

            var response = subject.ValidateConfiguration(configuration);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.WorkTrackingSystems, Has.Count.EqualTo(1));
                Assert.That(validationResult.WorkTrackingSystems[0].Id, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(validationResult.WorkTrackingSystems[0].Status, Is.EqualTo(ValidationStatus.Update));
                Assert.That(validationResult.WorkTrackingSystems[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Teams, Has.Count.EqualTo(1));
                Assert.That(validationResult.Teams[0].Id, Is.EqualTo(team.Id));
                Assert.That(validationResult.Teams[0].Status, Is.EqualTo(ValidationStatus.Update));
                Assert.That(validationResult.Teams[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Projects, Has.Count.EqualTo(1));
                Assert.That(validationResult.Projects[0].Id, Is.EqualTo(project.Id));
                Assert.That(validationResult.Projects[0].Status, Is.EqualTo(ValidationStatus.Update));
                Assert.That(validationResult.Teams[0].ErrorMessage, Is.Empty);
            };
        }

        [Test]
        public void ValidateConfiguration_TeamLinksToNotYetExistingWorkTrackingSystem_ReturnsValid()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystems.Clear();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            teams.Add(team);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Teams = new List<TeamSettingDto> { new TeamSettingDto(team) },
                Projects = new List<ProjectSettingDto>(),
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.WorkTrackingSystems, Has.Count.EqualTo(1));
                Assert.That(validationResult.WorkTrackingSystems[0].Id, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(validationResult.WorkTrackingSystems[0].Status, Is.EqualTo(ValidationStatus.New));
                Assert.That(validationResult.WorkTrackingSystems[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Teams, Has.Count.EqualTo(1));
                Assert.That(validationResult.Teams[0].Id, Is.EqualTo(team.Id));
                Assert.That(validationResult.Teams[0].Status, Is.EqualTo(ValidationStatus.Update));
                Assert.That(validationResult.Teams[0].ErrorMessage, Is.Empty);
            };
        }

        [Test]
        public void ValidateConfiguration_TeamLinksToWorkTrackingSystemNotInConfiguration_ReturnsError()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = 1886 };
            teams.Add(team);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Teams = new List<TeamSettingDto> { new TeamSettingDto(team) },
                Projects = new List<ProjectSettingDto>(),
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.Teams, Has.Count.EqualTo(1));
                Assert.That(validationResult.Teams[0].Id, Is.EqualTo(team.Id));
                Assert.That(validationResult.Teams[0].Status, Is.EqualTo(ValidationStatus.Error));
                Assert.That(validationResult.Teams[0].ErrorMessage, Is.EqualTo("Work Tracking System Not Found"));
            };
        }

        [Test]
        public void ValidateConfiguration_ProjectLinksToWorkTrackingSystemNotInConfiguration_ReturnsError()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();
            workTrackingSystems.Clear();

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = 1337 };
            projects.Add(project);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Projects = new List<ProjectSettingDto> { new ProjectSettingDto(project) },
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.WorkTrackingSystems, Has.Count.EqualTo(1));
                Assert.That(validationResult.WorkTrackingSystems[0].Id, Is.EqualTo(workTrackingSystem.Id));
                Assert.That(validationResult.WorkTrackingSystems[0].Status, Is.EqualTo(ValidationStatus.New));
                Assert.That(validationResult.WorkTrackingSystems[0].ErrorMessage, Is.Empty);

                Assert.That(validationResult.Projects, Has.Count.EqualTo(1));
                Assert.That(validationResult.Projects[0].Id, Is.EqualTo(project.Id));
                Assert.That(validationResult.Projects[0].Status, Is.EqualTo(ValidationStatus.Error));
                Assert.That(validationResult.Projects[0].ErrorMessage, Is.EqualTo("Work Tracking System Not Found"));
            };
        }

        [Test]
        public void ValidateConfiguration_ProjectLinksToTeamNotInConfiguration_ReturnsError()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            project.Teams.Add(team);
            projects.Add(project);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Projects = new List<ProjectSettingDto> { new ProjectSettingDto(project) },
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.Projects, Has.Count.EqualTo(1));
                Assert.That(validationResult.Projects[0].Id, Is.EqualTo(project.Id));
                Assert.That(validationResult.Projects[0].Status, Is.EqualTo(ValidationStatus.Error));
                Assert.That(validationResult.Projects[0].ErrorMessage, Is.EqualTo("Involved Team Not Found"));
            };
        }

        [Test]
        public void ValidateConfiguration_ProjectLinksToOwningTeamNotInConfiguration_ReturnsError()
        {
            var workTrackingSystem = AddWorkTrackingSystemConnection();

            var team = new Team { Id = 1, Name = "Test Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            teams.Add(team);

            var team2 = new Team { Id = 2, Name = "Another Team", WorkTrackingSystemConnectionId = workTrackingSystem.Id };

            var project = new Project { Id = 1, Name = "Test Project", WorkTrackingSystemConnectionId = workTrackingSystem.Id };
            project.Teams.Add(team);
            project.OwningTeam = team2;

            projects.Add(project);

            var subject = CreateSubject();

            var configuration = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto> { new WorkTrackingSystemConnectionDto(workTrackingSystem) },
                Projects = new List<ProjectSettingDto> { new ProjectSettingDto(project) },
            };

            var response = subject.ValidateConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.Value, Is.InstanceOf<ConfigurationValidationDto>());
                var validationResult = okResult.Value as ConfigurationValidationDto;

                Assert.That(validationResult.Projects, Has.Count.EqualTo(1));
                Assert.That(validationResult.Projects[0].Id, Is.EqualTo(project.Id));
                Assert.That(validationResult.Projects[0].Status, Is.EqualTo(ValidationStatus.Error));
                Assert.That(validationResult.Projects[0].ErrorMessage, Is.EqualTo("Owning Team must be involved in the project"));
            };
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
            Assert.That(fileResult.ContentType, Is.EqualTo("application/json"));
            var json = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

            return JsonSerializer.Deserialize<ConfigurationExport>(json, deserializeOptions) ?? throw new InvalidOperationException("Could not parse response");
        }

        private ConfigurationController CreateSubject()
        {
            return new ConfigurationController(Mock.Of<ILogger<ConfigurationController>>(), workTrackingSystemRepositoryMock.Object, teamRepositoryMock.Object, projectRepositoryMock.Object);
        }
    }
}
