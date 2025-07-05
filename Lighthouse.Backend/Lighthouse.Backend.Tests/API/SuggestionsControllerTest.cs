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
        private Mock<IRepository<Project>> projectRepositoryMock;

        private List<Team> teams;
        private List<Project> projects;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();

            teams = new List<Team>();
            projects = new List<Project>();

            teamRepositoryMock.Setup(repo => repo.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(repo => repo.GetAll()).Returns(projects);
        }

        [Test]
        public void GetTags_NoTeams_NoProjects_ReturnsEmptyList()
        {
            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(0));
            };
        }

        [Test]
        public void GetTags_TeamWithTag_ReturnsTag()
        {
            var tag = "FirstClubInTown";
            var team = CreateTeam(tag);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(1));
                Assert.That(tags, Has.Member(tag));
            };
        }

        [Test]
        public void GetTags_TeamWithMultipleTags_ReturnsAllTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team = CreateTeam(tag1, tag2);
            var suggestionsController = CreateSubject();
            
            var response = suggestionsController.GetTags();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_TeamsWithSimilarTags_ReturnsUniqueTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team1 = CreateTeam(tag1, tag2);
            var team2 = CreateTeam(tag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_TeamsWithDifferentTags_ReturnsAllUniqueTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team1 = CreateTeam(tag1);
            var team2 = CreateTeam(tag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_NoProjects_ReturnsEmptyList()
        {
            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(0));
            };
        }

        [Test]
        public void GetTags_ProjectWithTag_ReturnsTag()
        {
            var tag = "ProjectAlpha";
            var project = CreateProject(tag);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(1));
                Assert.That(tags, Has.Member(tag));
            };
        }

        [Test]
        public void GetTags_ProjectWithMultipleTags_ReturnsAllTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project = CreateProject(tag1, tag2);
            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_ProjectsWithSimilarTags_ReturnsUniqueTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project1 = CreateProject(tag1, tag2);
            var project2 = CreateProject(tag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_ProjectsWitDifferentTags_ReturnsUniqueTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project1 = CreateProject(tag1);
            var project2 = CreateProject(tag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            };
        }

        [Test]
        public void GetTags_TeamsAndProjectsWithOverlappingTags_ReturnsUniqueCombinedTags()
        {
            var teamTag1 = "TeamTag1";
            var teamTag2 = "SharedTag";
            var projectTag1 = "ProjectTag1";
            var projectTag2 = "SharedTag";

            var team = CreateTeam(teamTag1, teamTag2);
            var project = CreateProject(projectTag1, projectTag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(3));
                Assert.That(tags, Has.Member(teamTag1));
                Assert.That(tags, Has.Member(projectTag1));
                Assert.That(tags, Has.Member(projectTag2));
            };
        }

        [Test]
        public void GetTags_TeamsAndProjectsWithDistinctTags_ReturnsAllUniqueTags()
        {
            var teamTag1 = "TeamTag1";
            var teamTag2 = "TeamTag2";
            var projectTag1 = "ProjectTag1";
            var projectTag2 = "ProjectTag2";

            var team = CreateTeam(teamTag1, teamTag2);
            var project = CreateProject(projectTag1, projectTag2);

            var suggestionsController = CreateSubject();

            var response = suggestionsController.GetTags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var itemTypes = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(itemTypes, Has.Count.EqualTo(4));
                Assert.That(itemTypes, Has.Member(teamTag1));
                Assert.That(itemTypes, Has.Member(teamTag2));
                Assert.That(itemTypes, Has.Member(projectTag1));
                Assert.That(itemTypes, Has.Member(projectTag2));
            };
        }

        [Test]
        public void GetWorkItemTypesForTeams_ReturnsAllWorkItemTypes()
        {
            var teamType1 = "User Story";
            var teamType2 = "Bug";
            var teamType3 = "Task";

            var team1 = CreateTeam("Team1");
            team1.WorkItemTypes.Add(teamType1);
            team1.WorkItemTypes.Add(teamType3);

            var team2 = CreateTeam("Team2");
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

            var team1 = CreateTeam("Team1");
            team1.WorkItemTypes.Add(teamType1);
            team1.WorkItemTypes.Add(teamType3);

            var team2 = CreateTeam("Team2");
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

        private Project CreateProject(params string[] tags)
        {
            var project = new Project
            {
                Id = 1,
                Name = "Test Project",
                Tags = tags.ToList()
            };

            projects.Add(project);

            return project;
        }

        private Team CreateTeam(params string[] tags)
        {
            var team = new Team
            {
                Id = 1,
                Name = "Test Team",
                Tags = tags.ToList()
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
