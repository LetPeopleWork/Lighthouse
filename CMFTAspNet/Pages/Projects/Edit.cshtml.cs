using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Projects
{
    public class EditModel : PageModelBase<Project>
    {
        public EditModel(IRepository<Project> projectRepository) : base(projectRepository)
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Repository.Update(Entity);
            await Repository.Save();

            return RedirectToPage("./Index");
        }
    }
}
