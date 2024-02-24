using Microsoft.AspNetCore.Mvc.RazorPages;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly IRepository<Project> projectRepository;

        public IndexModel(IRepository<Project> projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public IList<Project> Projects { get;set; } = default!;

        public void OnGet()
        {
            Projects = projectRepository.GetAll().ToList();
        }
    }
}
