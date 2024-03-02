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
            }
            else
            {
                Project = new Project();
            }

            SelectedTeams.AddRange(Project.InvolvedTeams.Select(t => t.TeamId));

            return Project != null ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            RemoveValidErrorsOnMilestons();

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

            SetupMilestones();
            SetupInvolvedTeams();

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

        private void SetupMilestones()
        {
            foreach (var milestone in Project.Milestones)
            {
                milestone.Project = Project;
                milestone.ProjectId = Project.Id;
            }
        }

        private void RemoveValidErrorsOnMilestons()
        {
            var milestonesWithErrors = ModelState.Where(x => x.Value.Errors.Any() && x.Key.StartsWith("Project.Milestones"));

            foreach (var item in milestonesWithErrors)
            {
                ModelState.Remove(item.Key);
            }
        }
    }
}
