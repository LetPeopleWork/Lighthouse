using Microsoft.AspNetCore.Mvc;
using Lighthouse.Models;
using Lighthouse.Services.Implementation;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Pages.Projects
{
    public class DetailsModel : PageModelBase<Project>
    {
        private readonly IWorkItemCollectorService workItemCollectorService;
        private readonly IMonteCarloService monteCarloService;

        public DetailsModel(IRepository<Project> projectRepository, IWorkItemCollectorService workItemCollectorService, IMonteCarloService monteCarloService) : base(projectRepository)
        {
            this.workItemCollectorService = workItemCollectorService;
            this.monteCarloService = monteCarloService;
        }

        public async Task<IActionResult> OnPost(int? id)
        {
            var project = GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            await workItemCollectorService.UpdateFeaturesForProject(project);
            await Repository.Save();

            foreach (var team in project.InvolvedTeams)
            {
                await monteCarloService.ForecastFeaturesForTeam(team);
            }            

            return OnGet(id);
        }
    }
}
