using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Teams
{
    public class CreateModel : PageModel
    {
        private readonly IRepository<Team> teamRepository;

        public CreateModel(IRepository<Team> teamRepository)
        {
            this.teamRepository = teamRepository;
        }

        [BindProperty]
        public Team Team { get; set; } = default!;

        public IActionResult OnGet(int? id)
        {
            if (id.HasValue)
            {
                Team = teamRepository.GetById(id.Value);
            }
            else
            {
                Team = new Team();
            }

            return Team != null ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Team.Id > 0)
            {
                teamRepository.Update(Team);
            }
            else
            {
                teamRepository.Add(Team);
            }

            await teamRepository.Save();

            return RedirectToPage("./Details", new { id = Team.Id });
        }
    }
}
