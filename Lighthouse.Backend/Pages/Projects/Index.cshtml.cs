using Microsoft.AspNetCore.Mvc.RazorPages;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Pages.Projects
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
