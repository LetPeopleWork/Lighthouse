using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Pages.Projects
{
    public class CreateModel : PageModel
    {
        private readonly IRepository<Project> projectRepository;

        public CreateModel(IRepository<Project> projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        [BindProperty]
        public Project Project { get; set; } = default!;

        public bool IsEditMode => Project.Id > 0;

        public IActionResult OnGet(int? id)
        {
            if (id.HasValue)
            {
                Project = projectRepository.GetById(id.Value);
            }
            else
            {
                Project = new Project();
            }

            return Project != null ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (IsEditMode)
            {
                projectRepository.Update(Project);
            }
            else
            {
                projectRepository.Add(Project);
            }

            await projectRepository.Save();

            return RedirectToPage("./Details", new { id = Project.Id });
        }
    }
}
