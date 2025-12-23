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
    [Route("api/projects")] // Backward compatibility
    [ApiController]
    public class PortfoliosController : ControllerBase
    {
        private readonly IRepository<Portfolio> projectRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IProjectUpdater workItemUpdateService;
        private readonly IWorkTrackingConnectorFactory workTrackingConnectorFactory;

        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository;

        public PortfoliosController(
            IRepository<Portfolio> projectRepository,
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
        public IEnumerable<PortfolioDto> GetProjects()
        {
            var projectDtos = new List<PortfolioDto>();

            var allProjects = projectRepository.GetAll();

            foreach (var project in allProjects)
            {
                var projectDto = new PortfolioDto(project);
                projectDtos.Add(projectDto);
            }

            return projectDtos;
        }

        [HttpGet("{id}")]
        public ActionResult<PortfolioDto> Get(int id)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, id, project =>
            {
                return new PortfolioDto(project);
            });
        }

        [HttpPost("refresh/{id}")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public ActionResult UpdateFeaturesForProject(int id)
        {
            workItemUpdateService.TriggerUpdate(id);

            return Ok();
        }

        [HttpPost("refresh-all")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult UpdateAllProjects()
        {
            var projects = projectRepository.GetAll();

            foreach (var project in projects)
            {
                UpdateFeaturesForProject(project.Id);
            }

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
        public ActionResult<PortfolioSettingDto> GetProjectSettings(int id)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, id, project =>
            {
                var projectSettingDto = new PortfolioSettingDto(project);
                return projectSettingDto;
            });
        }

        [HttpPut("{id}")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public async Task<ActionResult<PortfolioSettingDto>> UpdateProject(int id, PortfolioSettingDto portfolioSetting)
        {
            return await this.GetEntityByIdAnExecuteAction(projectRepository, id, async project =>
            {
                SyncProjectWithProjectSettings(project, portfolioSetting);

                projectRepository.Update(project);
                await projectRepository.Save();

                var updatedProjectSettingDto = new PortfolioSettingDto(project);
                return updatedProjectSettingDto;
            });
        }

        [HttpPost]
        [LicenseGuard(CheckProjectConstraint = true, ProjectLimitOverride = 0)]
        public async Task<ActionResult<PortfolioSettingDto>> CreateProject(PortfolioSettingDto portfolioSetting)
        {
            portfolioSetting.Id = 0;

            var newProject = new Portfolio();
            SyncProjectWithProjectSettings(newProject, portfolioSetting);

            projectRepository.Add(newProject);
            await projectRepository.Save();

            var projectSettingDto = new PortfolioSettingDto(newProject);
            return Ok(projectSettingDto);
        }

        [HttpPost("validate")]
        [LicenseGuard(CheckProjectConstraint = true)]
        public async Task<ActionResult<bool>> ValidateProjectSettings(PortfolioSettingDto portfolioSettingDto)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemConnectionRepository, portfolioSettingDto.WorkTrackingSystemConnectionId, async workTrackingSystem =>
            {
                var project = new Portfolio { WorkTrackingSystemConnection = workTrackingSystem };
                SyncProjectWithProjectSettings(project, portfolioSettingDto);

                var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(project.WorkTrackingSystemConnection.WorkTrackingSystem);

                var result = await workItemService.ValidatePortfolioSettings(project);

                return result;
            });
        }

        private void SyncProjectWithProjectSettings(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.Name = portfolioSetting.Name;
            project.WorkItemTypes = portfolioSetting.WorkItemTypes;
            project.WorkItemQuery = portfolioSetting.WorkItemQuery;
            project.UnparentedItemsQuery = portfolioSetting.UnparentedItemsQuery?.Trim();

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = portfolioSetting.UsePercentileToCalculateDefaultAmountOfWorkItems;
            project.DefaultAmountOfWorkItemsPerFeature = portfolioSetting.DefaultAmountOfWorkItemsPerFeature;
            project.DefaultWorkItemPercentile = portfolioSetting.DefaultWorkItemPercentile;
            project.PercentileHistoryInDays = portfolioSetting.PercentileHistoryInDays;
            project.SizeEstimateField = portfolioSetting.SizeEstimateField;
            project.OverrideRealChildCountStates = portfolioSetting.OverrideRealChildCountStates;
            project.DoneItemsCutoffDays = portfolioSetting.DoneItemsCutoffDays;

            project.WorkTrackingSystemConnectionId = portfolioSetting.WorkTrackingSystemConnectionId;
            project.Tags = portfolioSetting.Tags;
            project.FeatureOwnerField = portfolioSetting.FeatureOwnerField;
            project.SystemWIPLimit = portfolioSetting.SystemWIPLimit;

            project.ParentOverrideField = portfolioSetting.ParentOverrideField;

            SyncStates(project, portfolioSetting);
            SyncTeams(project, portfolioSetting);
            SyncServiceLevelExpectation(project, portfolioSetting);
            SyncBlockedItems(project, portfolioSetting);
        }

        private static void SyncStates(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.ToDoStates = TrimListEntries(portfolioSetting.ToDoStates);
            project.DoingStates = TrimListEntries(portfolioSetting.DoingStates);
            project.DoneStates = TrimListEntries(portfolioSetting.DoneStates);
        }

        private static void SyncBlockedItems(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.BlockedStates = TrimListEntries(portfolioSetting.BlockedStates);
            project.BlockedTags = portfolioSetting.BlockedTags;
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }

        private void SyncTeams(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            var teams = new List<Team>();
            foreach (var teamDto in portfolioSetting.InvolvedTeams)
            {
                var team = teamRepository.GetById(teamDto.Id);
                if (team != null)
                {
                    teams.Add(team);
                }
            }

            project.UpdateTeams(teams);

            project.OwningTeam = null;
            if (portfolioSetting.OwningTeam != null)
            {
                project.OwningTeam = teamRepository.GetById(portfolioSetting.OwningTeam.Id);
            }
        }

        private static void SyncServiceLevelExpectation(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.ServiceLevelExpectationProbability = portfolioSetting.ServiceLevelExpectationProbability;
            project.ServiceLevelExpectationRange = portfolioSetting.ServiceLevelExpectationRange;
        }
    }
}
