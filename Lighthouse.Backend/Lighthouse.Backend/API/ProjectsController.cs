using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IProjectUpdater workItemUpdateService;
        private readonly IWorkTrackingConnectorFactory workTrackingConnectorFactory;

        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository;

        public ProjectsController(
            IRepository<Project> projectRepository,
            IRepository<Team> teamRepository,
            IProjectUpdater workItemUpdateService,
            IWorkTrackingConnectorFactory workTrackingConnectorFactory,
            IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository)
        {
            this.projectRepository = projectRepository;
            this.teamRepository = teamRepository;
            this.workItemUpdateService = workItemUpdateService;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.workTrackingSystemConnectionRepository = workTrackingSystemConnectionRepository;
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
            return this.GetEntityByIdAnExecuteAction(projectRepository, id, project =>
            {
                return new ProjectDto(project);
            });
        }

        [HttpPost("refresh/{id}")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public ActionResult UpdateFeaturesForProject(int id)
        {
            workItemUpdateService.TriggerUpdate(id);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            projectRepository.Remove(id);
            await projectRepository.Save();
            return NoContent();
        }

        [HttpGet("{id}/settings")]
        public ActionResult<ProjectSettingDto> GetProjectSettings(int id)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, id, project =>
            {
                var projectSettingDto = new ProjectSettingDto(project);
                return projectSettingDto;
            });
        }

        [HttpPut("{id}")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public async Task<ActionResult<ProjectSettingDto>> UpdateProject(int id, ProjectSettingDto projectSetting)
        {
            return await this.GetEntityByIdAnExecuteAction(projectRepository, id, async project =>
            {
                SyncProjectWithProjectSettings(project, projectSetting);

                projectRepository.Update(project);
                await projectRepository.Save();

                var updatedProjectSettingDto = new ProjectSettingDto(project);
                return updatedProjectSettingDto;
            });
        }

        [HttpPost]
        [LicenseGuard(CheckProjectConstraint = true, ProjectLimitOverride = 0)]
        public async Task<ActionResult<ProjectSettingDto>> CreateProject(ProjectSettingDto projectSetting)
        {
            projectSetting.Id = 0;
            foreach (var milestone in projectSetting.Milestones)
            {
                milestone.Id = 0;
            }

            var newProject = new Project();
            SyncProjectWithProjectSettings(newProject, projectSetting);

            projectRepository.Add(newProject);
            await projectRepository.Save();

            var projectSettingDto = new ProjectSettingDto(newProject);
            return Ok(projectSettingDto);
        }

        [HttpPost("validate")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public async Task<ActionResult<bool>> ValidateProjectSettings(ProjectSettingDto projectSettingDto)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemConnectionRepository, projectSettingDto.WorkTrackingSystemConnectionId, async workTrackingSystem =>
            {
                var project = new Project { WorkTrackingSystemConnection = workTrackingSystem };
                SyncProjectWithProjectSettings(project, projectSettingDto);

                var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(project.WorkTrackingSystemConnection.WorkTrackingSystem);

                var result = await workItemService.ValidateProjectSettings(project);

                return result;
            });
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
            project.PercentileHistoryInDays = projectSetting.PercentileHistoryInDays;
            project.SizeEstimateField = projectSetting.SizeEstimateField;
            project.OverrideRealChildCountStates = projectSetting.OverrideRealChildCountStates;

            project.WorkTrackingSystemConnectionId = projectSetting.WorkTrackingSystemConnectionId;
            project.Tags = projectSetting.Tags;
            project.FeatureOwnerField = projectSetting.FeatureOwnerField;
            project.SystemWIPLimit = projectSetting.SystemWIPLimit;

            project.ParentOverrideField = projectSetting.ParentOverrideField;

            SyncStates(project, projectSetting);
            SyncMilestones(project, projectSetting);
            SyncTeams(project, projectSetting);
            SyncServiceLevelExpectation(project, projectSetting);
            SyncBlockedItems(project, projectSetting);
        }

        private static void SyncStates(Project project, ProjectSettingDto projectSetting)
        {
            project.ToDoStates = TrimListEntries(projectSetting.ToDoStates);
            project.DoingStates = TrimListEntries(projectSetting.DoingStates);
            project.DoneStates = TrimListEntries(projectSetting.DoneStates);
        }

        private static void SyncBlockedItems(Project project, ProjectSettingDto projectSetting)
        {
            project.BlockedStates = TrimListEntries(projectSetting.BlockedStates);
            project.BlockedTags = projectSetting.BlockedTags;
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
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

            project.OwningTeam = null;
            if (projectSetting.OwningTeam != null)
            {
                project.OwningTeam = teamRepository.GetById(projectSetting.OwningTeam.Id);
            }
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

        private static void SyncServiceLevelExpectation(Project project, ProjectSettingDto projectSetting)
        {
            project.ServiceLevelExpectationProbability = projectSetting.ServiceLevelExpectationProbability;
            project.ServiceLevelExpectationRange = projectSetting.ServiceLevelExpectationRange;
        }
    }
}
