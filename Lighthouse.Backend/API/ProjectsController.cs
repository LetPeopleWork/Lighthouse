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
        private readonly IRepository<Project> repository;
        private readonly IWorkItemCollectorService workItemCollectorService;
        private readonly IMonteCarloService monteCarloService;

        public ProjectsController(IRepository<Project> repository, IWorkItemCollectorService workItemCollectorService, IMonteCarloService monteCarloService)
        {
            this.repository = repository;
            this.workItemCollectorService = workItemCollectorService;
            this.monteCarloService = monteCarloService;
        }

        [HttpGet]
        public IEnumerable<ProjectDto> GetProjects()
        {
            var projectDtos = new List<ProjectDto>();

            var allProjects = repository.GetAll();

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
            var project = repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            return Ok(new ProjectDto(project));
        }

        [HttpPost("refresh/{id}")]
        public async Task<ActionResult> UpdateFeaturesForProject(int id)
        {
            var project = repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            await workItemCollectorService.UpdateFeaturesForProject(project);
            await repository.Save();
            await monteCarloService.UpdateForecastsForProject(project);

            return Ok(new ProjectDto(project));
        }

        [HttpDelete("{id}")]
        public void DeleteProject(int id)
        {
            repository.Remove(id);
            repository.Save();
        }

        [HttpGet("{id}/settings")]
        public ActionResult<ProjectSettingDto> GetProjectSettings(int id)
        {
            var project = repository.GetById(id);

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
            var project = repository.GetById(id);

            if (project == null)
            {
                return NotFound();
            }

            SyncProjectWithProjectSettings(project, projectSetting);

            repository.Update(project);
            await repository.Save();

            var updatedProjectSettingDto = new ProjectSettingDto(project);
            return Ok(updatedProjectSettingDto);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectSettingDto>> CreateProject(ProjectSettingDto projectSetting)
        {
            var newProject = new Project();
            SyncProjectWithProjectSettings(newProject, projectSetting);

            repository.Add(newProject);
            await repository.Save();

            var projectSettingDto = new ProjectSettingDto(newProject);
            return Ok(projectSettingDto);
        }

        private void SyncProjectWithProjectSettings(Project project, ProjectSettingDto projectSetting)
        {
            project.Name = projectSetting.Name;
            project.WorkItemTypes = projectSetting.WorkItemTypes;
            project.WorkItemQuery = projectSetting.WorkItemQuery;
            project.UnparentedItemsQuery = projectSetting.UnparentedItemsQuery;
            project.DefaultAmountOfWorkItemsPerFeature = projectSetting.DefaultAmountOfWorkItemsPerFeature;
            project.WorkTrackingSystemConnectionId = projectSetting.WorkTrackingSystemConnectionId;
            project.SizeEstimateField = projectSetting.SizeEstimateField;

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
