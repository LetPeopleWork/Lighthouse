using Microsoft.AspNetCore.Mvc;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Projects
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
            monteCarloService.ForecastFeatures(project.Features);
            await Repository.Save();

            return OnGet(id);
        }
    }
}
