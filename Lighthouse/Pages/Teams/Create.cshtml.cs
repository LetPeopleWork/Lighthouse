using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Pages.Teams
{
    public class CreateModel : PageModel
    {
        private readonly IRepository<Team> teamRepository;
        private readonly IThroughputService throughputService;

        public CreateModel(IRepository<Team> teamRepository, IThroughputService throughputService)
        {
            this.teamRepository = teamRepository;
            this.throughputService = throughputService;
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

            await throughputService.UpdateThroughput(Team);
            await teamRepository.Save();

            return RedirectToPage("./Details", new { id = Team.Id });
        }
    }
}
