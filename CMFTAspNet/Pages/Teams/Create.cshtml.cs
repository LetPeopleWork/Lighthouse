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

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Team Team { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            teamRepository.Add(Team);
            await teamRepository.Save();

            return RedirectToPage("./Index");
        }
    }
}
