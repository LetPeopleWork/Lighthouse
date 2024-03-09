using Microsoft.AspNetCore.Mvc;
using Lighthouse.Services.Interfaces;
using Lighthouse.Models;

namespace Lighthouse.Pages.Teams
{
    public class DeleteModel : PageModelBase<Team>
    {
        public DeleteModel(IRepository<Team> teamRepository) : base(teamRepository)
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
