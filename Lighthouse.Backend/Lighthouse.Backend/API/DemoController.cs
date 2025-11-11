using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        private readonly IDemoDataService demoDataService;
        private readonly IRepository<Team> teamRepo;
        private readonly ITeamUpdater teamUpdater;
        private readonly IRepository<Project> projectRepo;
        private readonly IProjectUpdater projectUpdater;

        public DemoController(
            IDemoDataService demoDataService, IRepository<Team> teamRepo, ITeamUpdater teamUpdater, IRepository<Project> projectRepo, IProjectUpdater projectUpdater)
        {
            this.demoDataService = demoDataService;
            this.teamRepo = teamRepo;
            this.teamUpdater = teamUpdater;
            this.projectRepo = projectRepo;
            this.projectUpdater = projectUpdater;
        }

        [HttpGet("scenarios")]
        public ActionResult<List<DemoDataScenario>> GetScenarios()
        {
            var scenarios = demoDataService.GetAllScenarios();
            return Ok(scenarios);
        }

        [HttpPost("scenarios/{id}/load")]
        public async Task<ActionResult> LoadScenario(int id)
        {
            var scenarios = demoDataService.GetAllScenarios();

            var scenario = scenarios.SingleOrDefault(scenario => scenario.Id == id);

            if (scenario == null)
            {
                return NotFound();
            }

            await LoadScenarios(scenario);

            return Ok();
        }

        private async Task LoadScenarios(params DemoDataScenario[] scenarios)
        {
            await demoDataService.LoadScenarios(scenarios);
            UpdateTeams();
            UpdateProjects();
        }

        private void UpdateProjects()
        {
            var projects = projectRepo.GetAll().ToList();
            foreach (var project in projects)
            {
                projectUpdater.TriggerUpdate(project.Id);
            }
        }

        private void UpdateTeams()
        {
            var teams = teamRepo.GetAll().ToList();
            foreach (var team in teams)
            {
                teamUpdater.TriggerUpdate(team.Id);
            }
        }
    }
}
