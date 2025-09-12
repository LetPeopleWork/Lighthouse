using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DemoDataService : IDemoDataService
    {
        private static readonly List<DemoDataScenario> scenarios = new List<DemoDataScenario>();

        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo;
        private readonly IDemoDataFactory demoDataFactory;

        public DemoDataService(
            IRepository<Project> projectRepository, IRepository<Team> teamRepository, IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo, IDemoDataFactory demoDataFactory)
        {
            scenarios.Clear();
            scenarios.AddRange(GetFreeScenarios());
            this.projectRepository = projectRepository;
            this.teamRepository = teamRepository;
            this.workTrackingSystemConnectionRepo = workTrackingSystemConnectionRepo;
            this.demoDataFactory = demoDataFactory;
        }

        public IEnumerable<DemoDataScenario> GetAllScenarios()
        {
            return scenarios;
        }

        public async Task LoadScenarios(params DemoDataScenario[] scenarios)
        {
            await ClearExistingData();

            var workTrackingSystemConnection = await AddDemoWorkTrackingSystemConnection();
            var addedTeams = await AddTeamsForScenarios(scenarios, workTrackingSystemConnection);

            //await AddProjectsForSceanrios(scenarios, addedTeams, workTrackingSystemConnection);
        }

        private async Task AddProjectsForSceanrios(IEnumerable<DemoDataScenario> scenarios, List<Team> teams, WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var addedProjects = new List<string>();

            foreach (var scenario in scenarios)
            {
                AddProjectsForScenario(teams, workTrackingSystemConnection, addedProjects, scenario);
            }

            await projectRepository.Save();
        }

        private void AddProjectsForScenario(List<Team> teams, WorkTrackingSystemConnection workTrackingSystemConnection, List<string> addedProjects, DemoDataScenario scenario)
        {
            var projectNames = scenario.Projects.Distinct();

            var notAddedProjects = projectNames.Where(p => !addedProjects.Contains(p)).ToList();

            foreach (var projectName in notAddedProjects)
            {
                var project = demoDataFactory.CreateDemoProject(projectName);
                project.WorkTrackingSystemConnection = workTrackingSystemConnection;
                project.WorkTrackingSystemConnectionId = workTrackingSystemConnection.Id;

                var teamsForProject = teams.Where(t => scenario.Projects.Contains(t.Name)).ToList();
                project.UpdateTeams(teamsForProject);

                projectRepository.Add(project);

                addedProjects.Add(projectName);
            }
        }

        private async Task<List<Team>> AddTeamsForScenarios(IEnumerable<DemoDataScenario> scenarios, WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var teams = new List<Team>();
            var teamNames = scenarios.SelectMany(s => s.Teams).Distinct();

            foreach (var teamName in teamNames)
            {
                var team = demoDataFactory.CreateDemoTeam(teamName);
                
                team.WorkTrackingSystemConnection = workTrackingSystemConnection;
                team.WorkTrackingSystemConnectionId = workTrackingSystemConnection.Id;

                teams.Add(team);

                teamRepository.Add(team);
            }

            await teamRepository.Save();

            return teams;
        }

        private async Task<WorkTrackingSystemConnection> AddDemoWorkTrackingSystemConnection()
        {
            var demoWorkTrackingSystemConnection = demoDataFactory.CreateDemoWorkTrackingSystemConnection();
            workTrackingSystemConnectionRepo.Add(demoWorkTrackingSystemConnection);

            await workTrackingSystemConnectionRepo.Save();

            return demoWorkTrackingSystemConnection;
        }

        private async Task ClearExistingData()
        {
            await ClearProjects();
            await ClearTeams();
            await ClearWorkTrackingSystemConnections();
        }

        private async Task ClearWorkTrackingSystemConnections()
        {
            var connections = workTrackingSystemConnectionRepo.GetAll();
            foreach (var connection in connections)
            {
                workTrackingSystemConnectionRepo.Remove(connection.Id);
            }

            await workTrackingSystemConnectionRepo.Save();
        }

        private async Task ClearTeams()
        {
            var teams = teamRepository.GetAll();
            foreach (var team in teams)
            {
                teamRepository.Remove(team.Id);
            }

            await teamRepository.Save();
        }

        private async Task ClearProjects()
        {
            var projects = projectRepository.GetAll();
            foreach (var project in projects)
            {
                projectRepository.Remove(project.Id);
            }

            await projectRepository.Save();
        }

        private List<DemoDataScenario> GetFreeScenarios()
        {
            var freeScenarios = new List<DemoDataScenario>();

            var whenWillItBeDone = CreatesScenario(0, "When Will This Be Done?", "One Team, a set of Epics, and the question: When can we get it?");
            whenWillItBeDone.Teams.Add(DemoTeamNames.GoodThroughput);
            whenWillItBeDone.Projects.Add(DemoProjectNames.EpicForecast);
            freeScenarios.Add(whenWillItBeDone);

            var overloadedTeams = CreatesScenario(1, "Crash Override", "We're super busy, but somehow everything is slow...");
            overloadedTeams.Teams.Add(DemoTeamNames.ConstantlyIncreasingWip);
            overloadedTeams.Projects.Add(DemoProjectNames.OverloadedWip);
            freeScenarios.Add(overloadedTeams);

            var waterMelon = CreatesScenario(2, "Watermelon", "Show the flaw over averages in forecasting");
            waterMelon.Teams.Add(DemoTeamNames.GoodThroughput);
            waterMelon.Projects.Add(DemoProjectNames.HiddenRisk);
            freeScenarios.Add(waterMelon);

            var productLaunch = CreatesScenario(3, "Product Launch", "Shows how we can deal with fixed dates and use the power of forecasting to show what's possible");
            productLaunch.Teams.Add(DemoTeamNames.GoodThroughput);
            productLaunch.Teams.Add(DemoTeamNames.ImprovedOverTime);
            productLaunch.Projects.Add(DemoProjectNames.LaunchAlignment);
            freeScenarios.Add(productLaunch);

            return freeScenarios;
        }

        private DemoDataScenario CreatesScenario(int id, string title, string description)
        {
            return new DemoDataScenario
            {
                Id = id,
                Title = title,
                Description = description,
                IsPremium = false,
            };
        }
    }
}
