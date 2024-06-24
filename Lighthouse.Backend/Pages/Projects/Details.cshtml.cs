using Microsoft.AspNetCore.Mvc;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Pages.Projects
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

        public async Task<IActionResult> OnPostRefreshFeatures(int? id)
        {
            return await GetProjectByIdAndExecuteAction(id, async (Project project) =>
            {
                await workItemCollectorService.UpdateFeaturesForProject(project);
                await Repository.Save();

                await UpdateForecastsForInvolvedTeams(project);
            });
        }

        private async Task UpdateForecastsForInvolvedTeams(Project project)
        {
            foreach (var team in project.InvolvedTeams)
            {
                await monteCarloService.ForecastFeaturesForTeam(team);
            }
        }

        private async Task<IActionResult> GetProjectByIdAndExecuteAction(int? id, Func<Project, Task> action)
        {
            var project = GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            await action(project);

            return OnGet(id);
        }
    }
}
