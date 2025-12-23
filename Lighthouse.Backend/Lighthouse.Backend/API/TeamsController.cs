using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> projectRepository;
        private readonly IRepository<Feature> featureRepository;
        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository;
        private readonly IWorkItemRepository workItemRepository;
        private readonly ITeamUpdater teamUpdateService;
        private readonly IWorkTrackingConnectorFactory workTrackingConnectorFactory;
        private readonly ILicenseService licenseService;

        public TeamsController(
            IRepository<Team> teamRepository,
            IRepository<Portfolio> projectRepository,
            IRepository<Feature> featureRepository,
            IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
            IWorkItemRepository workItemRepository,
            ITeamUpdater teamUpdateService,
            IWorkTrackingConnectorFactory workTrackingConnectorFactory,
            ILicenseService licenseService)
        {
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
            this.featureRepository = featureRepository;
            this.workTrackingSystemConnectionRepository = workTrackingSystemConnectionRepository;
            this.workItemRepository = workItemRepository;
            this.teamUpdateService = teamUpdateService;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.licenseService = licenseService;
        }

        [HttpGet]
        public IEnumerable<TeamDto> GetTeams()
        {
            var teamDtos = new List<TeamDto>();

            var allTeams = teamRepository.GetAll().ToList();
            var allProjects = projectRepository.GetAll().ToList();
            var allFeatures = featureRepository.GetAll().ToList();

            foreach (var team in allTeams)
            {
                var teamDto = CreateTeamDto(allProjects, allFeatures, team);

                teamDtos.Add(teamDto);
            }

            return teamDtos;
        }

        [HttpGet("{id}")]
        public ActionResult<TeamDto> GetTeam(int id)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, id, team =>
            {
                var allProjects = projectRepository.GetAll().ToList();
                var allFeatures = featureRepository.GetAll().ToList();

                return CreateTeamDto(allProjects, allFeatures, team);
            });
        }

        [HttpPost("{id}")]
        [LicenseGuard(CheckTeamConstraint = true)]
        public ActionResult UpdateTeamData(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound(null);
            }

            teamUpdateService.TriggerUpdate(team.Id);

            return Ok();
        }

        [HttpPost("update-all")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult UpdateAllTeams()
        {
            var teams = teamRepository.GetAll();

            foreach (var team in teams)
            {
                UpdateTeamData(team.Id);
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            teamRepository.Remove(id);
            await teamRepository.Save();
            return NoContent();
        }

        [HttpPost]
        [LicenseGuard(CheckTeamConstraint = true, TeamLimitOverride = 2)]
        public async Task<ActionResult<TeamSettingDto>> CreateTeam(TeamSettingDto teamSetting)
        {
            teamSetting.Id = 0;
            var newTeam = new Team();
            SyncTeamWithTeamSettings(newTeam, teamSetting);

            if (!licenseService.CanUsePremiumFeatures())
            {
                var existingCsvTeams = teamRepository.GetAll()
                    .Where(t => t.WorkTrackingSystemConnection.WorkTrackingSystem == WorkTrackingSystems.Csv);

                var csvWorkTrackingSystems = workTrackingSystemConnectionRepository.GetAll()
                    .Where(wts => wts.WorkTrackingSystem == WorkTrackingSystems.Csv)
                    .Select(wts => wts.Id);

                if (csvWorkTrackingSystems.Contains(teamSetting.WorkTrackingSystemConnectionId) && existingCsvTeams.Any())
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Message = "Only 1 Team with the CSV Provider is allowed - Use the licensed version to get unlimited Teams."
                    });
                }
            }

            teamRepository.Add(newTeam);
            await teamRepository.Save();

            var teamSettingDto = new TeamSettingDto(newTeam);
            return Ok(teamSettingDto);
        }

        [HttpPut("{id}")]
        [LicenseGuard(CheckTeamConstraint = true)]
        public async Task<ActionResult<TeamSettingDto>> UpdateTeam(int id, TeamSettingDto teamSetting)
        {
            return await this.GetEntityByIdAnExecuteAction(teamRepository, id, async team =>
            {
                if (WorkItemRelatedSettingsChanged(team, teamSetting))
                {
                    workItemRepository.RemoveWorkItemsForTeam(team.Id);
                    await workItemRepository.Save();
                }

                SyncTeamWithTeamSettings(team, teamSetting);

                team.ResetUpdateTime();

                teamRepository.Update(team);
                await teamRepository.Save();

                var teamSettingDto = new TeamSettingDto(team);
                return teamSettingDto;
            });
        }

        [HttpGet("{id}/settings")]
        public ActionResult<TeamSettingDto> GetTeamSettings(int id)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, id, team =>
            {
                return new TeamSettingDto(team);
            });
        }

        [HttpPost("validate")]
        [LicenseGuard(CheckTeamConstraint = true)]
        public async Task<ActionResult<bool>> ValidateTeamSettings(TeamSettingDto teamSettingDto)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemConnectionRepository, teamSettingDto.WorkTrackingSystemConnectionId, async workTrackingSystem =>
            {
                var team = new Team { WorkTrackingSystemConnection = workTrackingSystem };
                SyncTeamWithTeamSettings(team, teamSettingDto);

                var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

                return await workItemService.ValidateTeamSettings(team);
            });
        }

        private static bool WorkItemRelatedSettingsChanged(Team team, TeamSettingDto teamSetting)
        {
            var queryChanged = team.WorkItemQuery != teamSetting.WorkItemQuery;
            var connectionChanged = team.WorkTrackingSystemConnectionId != teamSetting.WorkTrackingSystemConnectionId;
            var workItemTypesChanged = !team.WorkItemTypes.OrderBy(x => x).SequenceEqual(teamSetting.WorkItemTypes.OrderBy(x => x));
            var statesChanged =
                !team.ToDoStates.OrderBy(x => x).SequenceEqual(teamSetting.ToDoStates.OrderBy(x => x)) ||
                !team.DoingStates.OrderBy(x => x).SequenceEqual(teamSetting.DoingStates.OrderBy(x => x)) ||
                !team.DoneStates.OrderBy(x => x).SequenceEqual(teamSetting.DoneStates.OrderBy(x => x));

            return queryChanged || connectionChanged || workItemTypesChanged || statesChanged;
        }

        private static void SyncTeamWithTeamSettings(Team team, TeamSettingDto teamSetting)
        {
            team.Name = teamSetting.Name;
            team.WorkItemQuery = teamSetting.WorkItemQuery;
            team.ParentOverrideField = teamSetting.ParentOverrideField;
            team.FeatureWIP = teamSetting.FeatureWIP;
            team.UseFixedDatesForThroughput = teamSetting.UseFixedDatesForThroughput;
            team.ThroughputHistory = teamSetting.ThroughputHistory;
            team.ThroughputHistoryStartDate = teamSetting.ThroughputHistoryStartDate.HasValue ? DateTime.SpecifyKind(teamSetting.ThroughputHistoryStartDate.Value, DateTimeKind.Utc) : null;
            team.ThroughputHistoryEndDate = teamSetting.ThroughputHistoryEndDate.HasValue ? DateTime.SpecifyKind(teamSetting.ThroughputHistoryEndDate.Value, DateTimeKind.Utc) : null;
            team.WorkItemTypes = teamSetting.WorkItemTypes;
            team.WorkTrackingSystemConnectionId = teamSetting.WorkTrackingSystemConnectionId;
            team.AutomaticallyAdjustFeatureWIP = teamSetting.AutomaticallyAdjustFeatureWIP;
            team.DoneItemsCutoffDays = teamSetting.DoneItemsCutoffDays;
            team.Tags = teamSetting.Tags;
            team.SystemWIPLimit = teamSetting.SystemWIPLimit;

            SyncStates(team, teamSetting);
            SyncServiceLevelExpectation(team, teamSetting);
            SyncBlockedItems(team, teamSetting);
        }

        private static void SyncStates(Team team, TeamSettingDto teamSetting)
        {
            team.ToDoStates = TrimListEntries(teamSetting.ToDoStates);
            team.DoingStates = TrimListEntries(teamSetting.DoingStates);
            team.DoneStates = TrimListEntries(teamSetting.DoneStates);
        }

        private static void SyncBlockedItems(Team team, TeamSettingDto teamSetting)
        {
            team.BlockedStates = TrimListEntries(teamSetting.BlockedStates);
            team.BlockedTags = TrimListEntries(teamSetting.BlockedTags);
        }

        private static void SyncServiceLevelExpectation(Team team, TeamSettingDto teamSetting)
        {
            team.ServiceLevelExpectationProbability = teamSetting.ServiceLevelExpectationProbability;
            team.ServiceLevelExpectationRange = teamSetting.ServiceLevelExpectationRange;
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }

        private static TeamDto CreateTeamDto(List<Portfolio> allProjects, List<Feature> allFeatures, Team team)
        {
            var teamDto = new TeamDto(team);

            var projects = allProjects.Where(p => p.Teams.Any(t => t.Id == team.Id)).Select(t => new EntityReferenceDto(t.Id, t.Name));
            var features = allFeatures.Where(f => f.FeatureWork.Exists(rw => rw.TeamId == team.Id)).Select(f => new EntityReferenceDto(f.Id, f.Name));

            teamDto.Projects.AddRange(projects);
            teamDto.Features.AddRange(features);
            return teamDto;
        }
    }
}
