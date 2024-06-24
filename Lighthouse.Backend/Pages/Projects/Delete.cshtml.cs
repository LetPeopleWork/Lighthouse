using Microsoft.AspNetCore.Mvc;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Pages.Projects
{
    public class DeleteModel : PageModelBase<Project>
    {
        public DeleteModel(IRepository<Project> repository) : base(repository)
        {
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            Repository.Remove(id.Value);
            await Repository.Save();

            return RedirectToPage("./Index");
        }
    }
}
