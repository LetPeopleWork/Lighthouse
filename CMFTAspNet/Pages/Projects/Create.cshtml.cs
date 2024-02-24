using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMFTAspNet.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Projects
{
    public class CreateModel : PageModel
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Team> teamRepository;

        public CreateModel(IRepository<Project> projectRepository, IRepository<Team> teamRepository)
        {
            this.projectRepository = projectRepository;
            this.teamRepository = teamRepository;
        }

        [BindProperty]
        public Project Project { get; set; } = default!;

        [BindProperty]
        public List<int> SelectedTeams { get; set; } = new List<int>();

        public SelectList TeamsList { get; set; }

        public IActionResult OnGet()
        {
            var teams = teamRepository.GetAll();
            TeamsList = new SelectList(teams, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var involvedTeamId in SelectedTeams)
            {
                var team = teamRepository.GetById(involvedTeamId);
                if (team != null)
                {
                    Project.InvolvedTeams.Add(team);
                }
            }

            projectRepository.Add(Project);
            await projectRepository.Save();

            return RedirectToPage("./Index");
        }
    }
}
