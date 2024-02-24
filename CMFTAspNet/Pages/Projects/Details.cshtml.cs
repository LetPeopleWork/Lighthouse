using Microsoft.AspNetCore.Mvc;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Projects
{
    public class DetailsModel : PageModelBase<Project>
    {
        private readonly IWorkItemCollectorService workItemCollectorService;

        public DetailsModel(IRepository<Project> projectRepository, IWorkItemCollectorService workItemCollectorService) : base(projectRepository)
        {
            this.workItemCollectorService = workItemCollectorService;
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

            return OnGet(id);
        }
    }
}
