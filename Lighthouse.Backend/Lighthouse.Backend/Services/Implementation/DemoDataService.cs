using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DemoDataService : IDemoDataService
    {
        private readonly List<DemoDataScenario> scenarios = new List<DemoDataScenario>();

        private readonly IRepository<Portfolio> projectRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo;
        private readonly IDemoDataFactory demoDataFactory;

        public DemoDataService(
            IRepository<Portfolio> projectRepository, IRepository<Team> teamRepository, IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo, IDemoDataFactory demoDataFactory)
        {
            this.projectRepository = projectRepository;
            this.teamRepository = teamRepository;
            this.workTrackingSystemConnectionRepo = workTrackingSystemConnectionRepo;
            this.demoDataFactory = demoDataFactory;

            scenarios.AddRange(GetFreeScenarios());
            scenarios.AddRange(GetPremiumScenarios());
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

            await AddProjectsForSceanrios(scenarios, addedTeams, workTrackingSystemConnection);
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

                var teamsForProject = teams.Where(t => scenario.Teams.Contains(t.Name)).ToList();
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

            var whenWillItBeDone = CreatesScenario(0, "When Will This Be Done?", "One Team, one project with a a set of Epics, and the question: When can we get it?");
            whenWillItBeDone.Teams.Add(DemoTeamNames.GoodThroughput);
            whenWillItBeDone.Projects.Add(DemoProjectNames.EpicForecast);
            freeScenarios.Add(whenWillItBeDone);

            var overloadedTeams = CreatesScenario(1, "Too Much WIP", "A team that is super busy, but progress is slow.");
            overloadedTeams.Teams.Add(DemoTeamNames.ConstantlyIncreasingWip);
            freeScenarios.Add(overloadedTeams);

            var productLaunch = CreatesScenario(2, "Product Launch", "Two teams, one product they work on together. When can we launch?");
            productLaunch.Teams.Add(DemoTeamNames.GoodThroughput);
            productLaunch.Teams.Add(DemoTeamNames.ConstantlyIncreasingWip);
            productLaunch.Projects.Add(DemoProjectNames.LaunchAlignment);
            freeScenarios.Add(productLaunch);

            return freeScenarios;
        }

        private List<DemoDataScenario> GetPremiumScenarios()
        {
            var premiumScenarios = new List<DemoDataScenario>();

            var scrumTeam = CreatesScenario(10, "Flow in Scrum", "A team that is focusing on delivering at the end of their Sprint. What does it do to their flow?");
            scrumTeam.IsPremium = true;

            scrumTeam.Teams.Add(DemoTeamNames.SpikedThroughput);
            premiumScenarios.Add(scrumTeam);

            var itsNotAlwaysWhatItSeems = CreatesScenario(11, "It's Not Always What It Seems", "Two teams that look a certain way on first glance. Explore all Flow Metrics to get a full picture and don't draw conclusions before.");
            itsNotAlwaysWhatItSeems.IsPremium = true;
            itsNotAlwaysWhatItSeems.Teams.Add(DemoTeamNames.OldItems);
            itsNotAlwaysWhatItSeems.Teams.Add(DemoTeamNames.PartTimeWork);
            premiumScenarios.Add(itsNotAlwaysWhatItSeems);

            var projectDependencies = CreatesScenario(12, "Dependencies", "Explore a project where we have Epics with multiple Teams involved.");
            projectDependencies.IsPremium = true;

            projectDependencies.Teams.Add(DemoTeamNames.GoodThroughput);
            projectDependencies.Teams.Add(DemoTeamNames.PartTimeWork);
            projectDependencies.Teams.Add(DemoTeamNames.OldItems);

            projectDependencies.Projects.Add(DemoProjectNames.ProjectWithDependencies);

            premiumScenarios.Add(projectDependencies);

            var quarterlyPlanning = CreatesScenario(13, "Quarterly Planning", "See how a Quarterly Planning could look like for a Team that uses Monte Carlo Forecasts");
            quarterlyPlanning.IsPremium = true;

            quarterlyPlanning.Teams.Add(DemoTeamNames.GoodThroughput);
            quarterlyPlanning.Projects.Add(DemoProjectNames.QuarterlyPlanning);

            premiumScenarios.Add(quarterlyPlanning);

            var newProductInitiative = CreatesScenario(14, "New Product Initiative", "Look at how you could forecast if you don't have any refined Features yet");
            newProductInitiative.IsPremium = true;

            newProductInitiative.Teams.Add(DemoTeamNames.GoodThroughput);
            newProductInitiative.Projects.Add(DemoProjectNames.NewProductInitiative);

            premiumScenarios.Add(newProductInitiative);

            return premiumScenarios;
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
