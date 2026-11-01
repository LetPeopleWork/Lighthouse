using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class SuggestionsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> projectRepositoryMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;

        private List<Team> teams;
        private List<Portfolio> projects;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Portfolio>>();
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();

            teams = new List<Team>();
            projects = new List<Portfolio>();

            teamRepositoryMock.Setup(repo => repo.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(repo => repo.GetAll()).Returns(projects);
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IEnumerable<int> ids, CancellationToken _) => ids.Distinct().ToArray());
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IEnumerable<int> ids, CancellationToken _) => ids.Distinct().ToArray());
        }

        [Test]
        public void SuggestionsController_HasAuthorizeAttribute()
        {
            var attribute = typeof(SuggestionsController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public async Task GetWorkItemTypesForTeams_ReturnsAllWorkItemTypes()
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

            var response = await suggestionsController.GetWorkItemTypesForTeams();

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
        public async Task GetWorkItemTypesForTeams_DoesNotIncludeItemsTwice_ReturnsAllWorkItemTypes()
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

            var response = await suggestionsController.GetWorkItemTypesForTeams();

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
        public async Task GetWorkItemTypesForTeams_FiltersUnreadableTeams()
        {
            var visibleTeam = CreateTeam();
            visibleTeam.Id = 1;
            visibleTeam.WorkItemTypes.Clear();
            visibleTeam.WorkItemTypes.Add("Story");

            var hiddenTeam = CreateTeam();
            hiddenTeam.Id = 2;
            hiddenTeam.WorkItemTypes.Clear();
            hiddenTeam.WorkItemTypes.Add("Secret Type");

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visibleTeam.Id]);

            var subject = CreateSubject();
            var response = await subject.GetWorkItemTypesForTeams();

            var itemTypes = (List<string>)((OkObjectResult)response.Result!).Value!;
            Assert.That(itemTypes, Is.EqualTo(["Story"]));
        }

        [Test]
        public async Task GetWorkItemTypesForProjects_ReturnsAllWorkItemTypes()
        {
            var projectType1 = "Epic";
            var projectType2 = "Feature";

            var project1 = CreateProject();
            project1.WorkItemTypes.Add(projectType1);

            var project2 = CreateProject();
            project2.WorkItemTypes.Add(projectType2);

            var suggestionsController = CreateSubject();

            var response = await suggestionsController.GetWorkItemTypesForProjects();

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
        public async Task GetWorkItemTypesForProjects_DoesNotIncludeItemsTwice_ReturnsAllWorkItemTypes()
        {
            var projectType1 = "Epic";
            var projectType2 = "Feature";

            var project1 = CreateProject();
            project1.WorkItemTypes.Add(projectType1);
            project1.WorkItemTypes.Add(projectType2);

            var project2 = CreateProject();
            project2.WorkItemTypes.Add(projectType2);

            var suggestionsController = CreateSubject();

            var response = await suggestionsController.GetWorkItemTypesForProjects();

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
        public async Task GetStatesForTeams_ReturnsStates()
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

            var response = await suggestionsController.GetStatesForTeams();

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
        public async Task GetStatesForTeams_SkipsDuplicates()
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

            var response = await suggestionsController.GetStatesForTeams();

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
        public async Task GetStatesForProjects_ReturnsStates()
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

            var response = await suggestionsController.GetStatesForProjects();

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
        public async Task GetStatesForProjects_SkipsDuplicates()
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

            var response = await suggestionsController.GetStatesForProjects();

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
        public async Task GetStatesForProjects_FiltersUnreadableProjects()
        {
            var visibleProject = CreateProject();
            visibleProject.Id = 1;
            visibleProject.ToDoStates.Clear();
            visibleProject.ToDoStates.Add("Visible");

            var hiddenProject = CreateProject();
            hiddenProject.Id = 2;
            hiddenProject.ToDoStates.Clear();
            hiddenProject.ToDoStates.Add("Hidden");

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visibleProject.Id]);

            var subject = CreateSubject();
            var response = await subject.GetStatesForProjects();
            var states = (StatesCollectionDto)((OkObjectResult)response.Result!).Value!;

            Assert.That(states.ToDoStates, Is.EqualTo(["Visible"]));
        }

        [Test]
        public async Task GetWorkItemTypesForTeams_NamesDifferingOnlyInCase_AreListedOnceWithTheFirstSpelling()
        {
            var first = CreateTeam();
            first.WorkItemTypes.Clear();
            first.WorkItemTypes.AddRange(["Story", "Bug"]);
            var second = CreateTeam();
            second.Id = 2;
            second.WorkItemTypes.Clear();
            second.WorkItemTypes.AddRange(["story", "BUG", "Task"]);

            var response = await CreateSubject().GetWorkItemTypesForTeams();

            Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(new List<string> { "Story", "Bug", "Task" }));
        }

        [Test]
        public async Task GetWorkItemTypesForProjects_NamesDifferingOnlyInCase_AreListedOnceWithTheFirstSpelling()
        {
            var first = CreateProject();
            first.WorkItemTypes.Clear();
            first.WorkItemTypes.Add("Epic");
            var second = CreateProject();
            second.Id = 2;
            second.WorkItemTypes.Clear();
            second.WorkItemTypes.AddRange(["epic", "Initiative"]);

            var response = await CreateSubject().GetWorkItemTypesForProjects();

            Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(new List<string> { "Epic", "Initiative" }));
        }

        [Test]
        public async Task GetStatesForTeams_NamesDifferingOnlyInCase_AreListedOnceInEachListWithTheFirstSpelling()
        {
            var first = CreateTeam();
            SetStates(first, ["To Do"], ["In Progress"], ["Done"]);
            var second = CreateTeam();
            second.Id = 2;
            SetStates(second, ["to do", "Backlog"], ["in progress", "IN REVIEW"], ["DONE", "Closed"]);

            var response = await CreateSubject().GetStatesForTeams();
            var states = (StatesCollectionDto)((OkObjectResult)response.Result!).Value!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(states.ToDoStates, Is.EqualTo(new List<string> { "To Do", "Backlog" }));
                Assert.That(states.DoingStates, Is.EqualTo(new List<string> { "In Progress", "IN REVIEW" }));
                Assert.That(states.DoneStates, Is.EqualTo(new List<string> { "Done", "Closed" }));
            }
        }

        [Test]
        public async Task GetStatesForProjects_NamesDifferingOnlyInCase_AreListedOnceInEachListWithTheFirstSpelling()
        {
            var first = CreateProject();
            SetStates(first, ["To Do"], ["In Progress"], ["Done"]);
            var second = CreateProject();
            second.Id = 2;
            SetStates(second, ["TO DO"], ["in progress", "Review"], ["done"]);

            var response = await CreateSubject().GetStatesForProjects();
            var states = (StatesCollectionDto)((OkObjectResult)response.Result!).Value!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(states.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                Assert.That(states.DoingStates, Is.EqualTo(new List<string> { "In Progress", "Review" }));
                Assert.That(states.DoneStates, Is.EqualTo(new List<string> { "Done" }));
            }
        }

        [Test]
        public async Task GetWorkItemTypesForTeams_NamesDifferingOnlyInCase_KeepTheSpellingOfTheTeamWithTheLowestIdWhateverOrderTheRepositoryReturns()
        {
            var newer = CreateTeam();
            newer.Id = 2;
            newer.WorkItemTypes.Clear();
            newer.WorkItemTypes.Add("story");
            var oldest = CreateTeam();
            oldest.Id = 1;
            oldest.WorkItemTypes.Clear();
            oldest.WorkItemTypes.Add("Story");

            var response = await CreateSubject().GetWorkItemTypesForTeams();

            Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(new List<string> { "Story" }));
        }

        [Test]
        public async Task GetWorkItemTypesForProjects_NamesDifferingOnlyInCase_KeepTheSpellingOfThePortfolioWithTheLowestIdWhateverOrderTheRepositoryReturns()
        {
            var newer = CreateProject();
            newer.Id = 2;
            newer.WorkItemTypes.Clear();
            newer.WorkItemTypes.Add("epic");
            var oldest = CreateProject();
            oldest.Id = 1;
            oldest.WorkItemTypes.Clear();
            oldest.WorkItemTypes.Add("Epic");

            var response = await CreateSubject().GetWorkItemTypesForProjects();

            Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(new List<string> { "Epic" }));
        }

        private static void SetStates(WorkTrackingSystemOptionsOwner owner, string[] toDo, string[] doing, string[] done)
        {
            owner.ToDoStates.Clear();
            owner.ToDoStates.AddRange(toDo);
            owner.DoingStates.Clear();
            owner.DoingStates.AddRange(doing);
            owner.DoneStates.Clear();
            owner.DoneStates.AddRange(done);
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
                projectRepositoryMock.Object,
                rbacAdministrationServiceMock.Object);
        }
    }
}
