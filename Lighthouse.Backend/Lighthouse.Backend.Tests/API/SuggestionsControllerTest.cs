using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class SuggestionsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> projectRepositoryMock;

        private List<Team> teams;
        private List<Portfolio> projects;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Portfolio>>();

            teams = new List<Team>();
            projects = new List<Portfolio>();

            teamRepositoryMock.Setup(repo => repo.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(repo => repo.GetAll()).Returns(projects);
        }

        [Test]
        public void GetWorkItemTypesForTeams_ReturnsAllWorkItemTypes()
        {
            var teamType1 = "User Story";
            var teamType2 = "Bug";
            var teamType3 = "Task";

            var team1 = CreateTeam();
            team1.WorkItemTypes.Add(teamType1);
            team1.WorkItemTypes.Add(teamType3);

            var team2 = CreateTeam();
            team2.WorkItemTypes.Add(teamType2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetWorkItemTypesForTeams();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var itemTypes = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(itemTypes, Has.Count.EqualTo(3));
                Assert.That(itemTypes, Has.Member(teamType1));
                Assert.That(itemTypes, Has.Member(teamType2));
                Assert.That(itemTypes, Has.Member(teamType3));
            };
        }

        [Test]
        public void GetWorkItemTypesForTeams_DoesNotIncludeItemsTwice_ReturnsAllWorkItemTypes()
        {
            var teamType1 = "User Story";
            var teamType2 = "Bug";
            var teamType3 = "Task";

            var team1 = CreateTeam();
            team1.WorkItemTypes.Add(teamType1);
            team1.WorkItemTypes.Add(teamType3);

            var team2 = CreateTeam();
            team2.WorkItemTypes.Add(teamType1);
            team2.WorkItemTypes.Add(teamType2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetWorkItemTypesForTeams();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var itemTypes = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(itemTypes, Has.Count.EqualTo(3));
                Assert.That(itemTypes, Has.Member(teamType1));
                Assert.That(itemTypes, Has.Member(teamType2));
                Assert.That(itemTypes, Has.Member(teamType3));
            };
        }

        [Test]
        public void GetWorkItemTypesForProjects_ReturnsAllWorkItemTypes()
        {
            var projectType1 = "Epic";
            var projectType2 = "Feature";

            var project1 = CreateProject();
            project1.WorkItemTypes.Add(projectType1);

            var project2 = CreateProject();
            project2.WorkItemTypes.Add(projectType2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetWorkItemTypesForProjects();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                
                var itemTypes = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(itemTypes, Has.Count.EqualTo(2));
                Assert.That(itemTypes, Has.Member(projectType1));
                Assert.That(itemTypes, Has.Member(projectType2));
            };
        }

        [Test]
        public void GetWorkItemTypesForProjects_DoesNotIncludeItemsTwice_ReturnsAllWorkItemTypes()
        {
            var projectType1 = "Epic";
            var projectType2 = "Feature";

            var project1 = CreateProject();
            project1.WorkItemTypes.Add(projectType1);
            project1.WorkItemTypes.Add(projectType2);

            var project2 = CreateProject();
            project2.WorkItemTypes.Add(projectType2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetWorkItemTypesForProjects();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var itemTypes = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(itemTypes, Has.Count.EqualTo(2));
                Assert.That(itemTypes, Has.Member(projectType1));
                Assert.That(itemTypes, Has.Member(projectType2));
            };
        }

        [Test]
        public void GetStatesForTeams_ReturnsStates()
        {
            var toDoStates = new List<string> { "New", "Planned" };
            var doingStates = new List<string> { "Active", "Resolved" };
            var doneStates = new List<string> { "Done", "Closed" };

            var team = CreateTeam();
            team.ToDoStates.Clear();
            team.ToDoStates.AddRange(toDoStates);

            team.DoingStates.Clear();
            team.DoingStates.AddRange(doingStates);

            team.DoneStates.Clear();
            team.DoneStates.AddRange(doneStates);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetStatesForTeams();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var statesCollection = (StatesCollectionDto)((OkObjectResult)response.Result).Value;

                Assert.That(statesCollection.ToDoStates, Is.EquivalentTo(toDoStates));
                Assert.That(statesCollection.DoingStates, Is.EquivalentTo(doingStates));
                Assert.That(statesCollection.DoneStates, Is.EquivalentTo(doneStates));
            };
        }

        [Test]
        public void GetStatesForTeams_SkipsDuplicates()
        {
            var toDoStates = new List<string> { "New", "Planned" };
            var doingStates = new List<string> { "Active", "Resolved" };
            var doneStates = new List<string> { "Done", "Closed" };

            var team1 = CreateTeam();
            team1.ToDoStates.Clear();
            team1.ToDoStates.AddRange(toDoStates);

            team1.DoingStates.Clear();
            team1.DoingStates.AddRange(doingStates);

            team1.DoneStates.Clear();
            team1.DoneStates.AddRange(doneStates);

            var team2 = CreateTeam();

            team2.ToDoStates.Clear();
            team2.ToDoStates.AddRange(toDoStates);

            team2.DoingStates.Clear();
            team2.DoingStates.AddRange(doingStates);

            team2.DoneStates.Clear();
            team2.DoneStates.AddRange(doneStates);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetStatesForTeams();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var statesCollection = (StatesCollectionDto)((OkObjectResult)response.Result).Value;

                Assert.That(statesCollection.ToDoStates, Is.EquivalentTo(toDoStates));
                Assert.That(statesCollection.DoingStates, Is.EquivalentTo(doingStates));
                Assert.That(statesCollection.DoneStates, Is.EquivalentTo(doneStates));
            };
        }

        [Test]
        public void GetStatesForProjects_ReturnsStates()
        {
            var toDoStates = new List<string> { "New", "Planned" };
            var doingStates = new List<string> { "Active", "Resolved" };
            var doneStates = new List<string> { "Done", "Closed" };

            var project = CreateProject();
            project.ToDoStates.Clear();
            project.ToDoStates.AddRange(toDoStates);

            project.DoingStates.Clear();
            project.DoingStates.AddRange(doingStates);

            project.DoneStates.Clear();
            project.DoneStates.AddRange(doneStates);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetStatesForProjects();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var statesCollection = (StatesCollectionDto)((OkObjectResult)response.Result).Value;

                Assert.That(statesCollection.ToDoStates, Is.EquivalentTo(toDoStates));
                Assert.That(statesCollection.DoingStates, Is.EquivalentTo(doingStates));
                Assert.That(statesCollection.DoneStates, Is.EquivalentTo(doneStates));
            };
        }

        [Test]
        public void GetStatesForProjects_SkipsDuplicates()
        {
            var toDoStates = new List<string> { "New", "Planned" };
            var doingStates = new List<string> { "Active", "Resolved" };
            var doneStates = new List<string> { "Done", "Closed" };

            var project1 = CreateProject();
            project1.ToDoStates.Clear();
            project1.ToDoStates.AddRange(toDoStates);

            project1.DoingStates.Clear();
            project1.DoingStates.AddRange(doingStates);

            project1.DoneStates.Clear();
            project1.DoneStates.AddRange(doneStates);

            var project2 = CreateProject();

            project2.ToDoStates.Clear();
            project2.ToDoStates.AddRange(toDoStates);

            project2.DoingStates.Clear();
            project2.DoingStates.AddRange(doingStates);

            project2.DoneStates.Clear();
            project2.DoneStates.AddRange(doneStates);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetStatesForProjects();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var statesCollection = (StatesCollectionDto)((OkObjectResult)response.Result).Value;

                Assert.That(statesCollection.ToDoStates, Is.EquivalentTo(toDoStates));
                Assert.That(statesCollection.DoingStates, Is.EquivalentTo(doingStates));
                Assert.That(statesCollection.DoneStates, Is.EquivalentTo(doneStates));
            };
        }

        private Portfolio CreateProject()
        {
            var project = new Portfolio
            {
                Id = 1,
                Name = "Test Project"
            };

            projects.Add(project);

            return project;
        }

        private Team CreateTeam()
        {
            var team = new Team
            {
                Id = 1,
                Name = "Test Team"
            };

            teams.Add(team);

            return team;
        }

        private SuggestionsController CreateSubject()
        {
            return new SuggestionsController(
                Mock.Of<ILogger<SuggestionsController>>(),
                teamRepositoryMock.Object,
                projectRepositoryMock.Object);
        }
    }
}
