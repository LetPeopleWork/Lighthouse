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

        public bool IsEditMode => Project.Id > 0;

        [BindProperty]
        public List<int> SelectedTeams { get; set; } = new List<int>();

        public SelectList TeamsList { get; set; }

        public IActionResult OnGet(int? id)
        {
            var teams = teamRepository.GetAll();
            TeamsList = new SelectList(teams, "Id", "Name");

            if (id.HasValue)
            {
                Project = projectRepository.GetById(id.Value);

                if (Project != null)
                {
                    SelectedTeams.AddRange(Project.InvolvedTeams.Select(t => t.Id));
                }
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

            SetupInvolvedTeams();

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

        private void SetupInvolvedTeams()
        {
            Project.InvolvedTeams.Clear();
            foreach (var involvedTeamId in SelectedTeams)
            {
                var team = teamRepository.GetById(involvedTeamId);
                if (team != null)
                {
                    Project.InvolvedTeams.Add(new TeamInProject(team, Project));
                }
            }
        }
    }
}
