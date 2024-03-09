using Microsoft.AspNetCore.Mvc.RazorPages;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Pages.Projects
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
