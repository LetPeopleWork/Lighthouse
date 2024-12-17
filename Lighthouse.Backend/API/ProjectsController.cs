using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IWorkItemCollectorService workItemCollectorService;
        private readonly IMonteCarloService monteCarloService;

        public ProjectsController(IRepository<Project> projectRepository, IRepository<Team> teamRepository, IWorkItemCollectorService workItemCollectorService, IMonteCarloService monteCarloService)
        {
            this.projectRepository = projectRepository;
            this.teamRepository = teamRepository;
            this.workItemCollectorService = workItemCollectorService;
            this.monteCarloService = monteCarloService;
        }

        [HttpGet]
        public IEnumerable<ProjectDto> GetProjects()
        {
            var projectDtos = new List<ProjectDto>();

            var allProjects = projectRepository.GetAll();

            foreach (var project in allProjects)
            {
                var projectDto = new ProjectDto(project);
                projectDtos.Add(projectDto);
            }

            return projectDtos;
        }

        [HttpGet("{id}")]
        public ActionResult<ProjectDto> Get(int id)
        {
            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            return Ok(new ProjectDto(project));
        }

        [HttpPost("refresh/{id}")]
        public async Task<ActionResult> UpdateFeaturesForProject(int id)
        {
            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            await workItemCollectorService.UpdateFeaturesForProject(project);
            await projectRepository.Save();
            await monteCarloService.UpdateForecastsForProject(project);

            return Ok(new ProjectDto(project));
        }

        [HttpDelete("{id}")]
        public void DeleteProject(int id)
        {
            projectRepository.Remove(id);
            projectRepository.Save();
        }

        [HttpGet("{id}/settings")]
        public ActionResult<ProjectSettingDto> GetProjectSettings(int id)
        {
            var project = projectRepository.GetById(id);

            if (project == null)
            {
                return NotFound();
            }

            var projectSettingDto = new ProjectSettingDto(project);
            return Ok(projectSettingDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectSettingDto>> UpdateProject(int id, ProjectSettingDto projectSetting)
        {
            var project = projectRepository.GetById(id);

            if (project == null)
            {
                return NotFound();
            }

            SyncProjectWithProjectSettings(project, projectSetting);

            projectRepository.Update(project);
            await projectRepository.Save();

            var updatedProjectSettingDto = new ProjectSettingDto(project);
            return Ok(updatedProjectSettingDto);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectSettingDto>> CreateProject(ProjectSettingDto projectSetting)
        {
            var newProject = new Project();
            SyncProjectWithProjectSettings(newProject, projectSetting);

            projectRepository.Add(newProject);
            await projectRepository.Save();

            var projectSettingDto = new ProjectSettingDto(newProject);
            return Ok(projectSettingDto);
        }

        private void SyncProjectWithProjectSettings(Project project, ProjectSettingDto projectSetting)
        {
            project.Name = projectSetting.Name;
            project.WorkItemTypes = projectSetting.WorkItemTypes;
            project.WorkItemQuery = projectSetting.WorkItemQuery;
            project.UnparentedItemsQuery = projectSetting.UnparentedItemsQuery;

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = projectSetting.UsePercentileToCalculateDefaultAmountOfWorkItems;
            project.DefaultAmountOfWorkItemsPerFeature = projectSetting.DefaultAmountOfWorkItemsPerFeature;
            project.DefaultWorkItemPercentile = projectSetting.DefaultWorkItemPercentile;
            project.HistoricalFeaturesWorkItemQuery = projectSetting.HistoricalFeaturesWorkItemQuery;
            project.SizeEstimateField = projectSetting.SizeEstimateField;
            project.OverrideRealChildCountStates = projectSetting.OverrideRealChildCountStates;

            project.WorkTrackingSystemConnectionId = projectSetting.WorkTrackingSystemConnectionId;

            SyncStates(project, projectSetting);
            SyncMilestones(project, projectSetting);
            SyncTeams(project, projectSetting);
        }

        private static void SyncStates(Project project, ProjectSettingDto projectSetting)
        {
            project.ToDoStates = projectSetting.ToDoStates;
            project.DoingStates = projectSetting.DoingStates;
            project.DoneStates = projectSetting.DoneStates;
        }

        private void SyncTeams(Project project, ProjectSettingDto projectSetting)
        {
            var teams = new List<Team>();
            foreach (var teamDto in projectSetting.InvolvedTeams)
            {
                var team = teamRepository.GetById(teamDto.Id);
                if (team != null)
                {
                    teams.Add(team);
                }
            }

            project.UpdateTeams(teams);
        }

        private static void SyncMilestones(Project project, ProjectSettingDto projectSetting)
        {
            project.Milestones.Clear();
            foreach (var milestone in projectSetting.Milestones)
            {
                project.Milestones.Add(new Milestone
                {
                    Id = milestone.Id,
                    Name = milestone.Name,
                    Date = milestone.Date,
                    Project = project,
                    ProjectId = project.Id,
                });
            }
        }
    }
}
