using Microsoft.AspNetCore.Mvc;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Models;

namespace CMFTAspNet.Pages.Teams
{
    public class EditModel : PageModelBase<Team>
    {
        public EditModel(IRepository<Team> teamRepository) : base(teamRepository)
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
