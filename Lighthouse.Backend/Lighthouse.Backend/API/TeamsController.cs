using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Feature> featureRepository;
        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository;
        private readonly ITeamUpdateService teamUpdateService;
        private readonly IWorkItemServiceFactory workItemServiceFactory;

        public TeamsController(
            IRepository<Team> teamRepository,
            IRepository<Project> projectRepository,
            IRepository<Feature> featureRepository,
            IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
            ITeamUpdateService teamUpdateService,
            IWorkItemServiceFactory workItemServiceFactory)
        {
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
            this.featureRepository = featureRepository;
            this.workTrackingSystemConnectionRepository = workTrackingSystemConnectionRepository;
            this.teamUpdateService = teamUpdateService;
            this.workItemServiceFactory = workItemServiceFactory;
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
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            var allProjects = projectRepository.GetAll().ToList();
            var allFeatures = featureRepository.GetAll().ToList();

            return Ok(CreateTeamDto(allProjects, allFeatures, team));
        }

        [HttpPost("{id}")]
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            teamRepository.Remove(id);
            await teamRepository.Save();
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<TeamSettingDto>> CreateTeam(TeamSettingDto teamSetting)
        {
            var newTeam = new Team();
            SyncTeamWithTeamSettings(newTeam, teamSetting);

            teamRepository.Add(newTeam);
            await teamRepository.Save();

            var teamSettingDto = new TeamSettingDto(newTeam);
            return Ok(teamSettingDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TeamSettingDto>> UpdateTeam(int id, TeamSettingDto teamSetting)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            SyncTeamWithTeamSettings(team, teamSetting);

            teamRepository.Update(team);
            await teamRepository.Save();

            var teamSettingDto = new TeamSettingDto(team);
            return Ok(teamSettingDto);
        }

        [HttpGet("{id}/settings")]
        public ActionResult<TeamSettingDto> GetTeamSettings(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            var teamSettingDto = new TeamSettingDto(team);

            return Ok(teamSettingDto);
        }

        [HttpPost("validate")]
        public async Task<ActionResult<bool>> ValidateTeamSettings(TeamSettingDto teamSettingDto)
        {
            var workTrackingSystem = workTrackingSystemConnectionRepository.GetById(teamSettingDto.WorkTrackingSystemConnectionId);

            if (workTrackingSystem == null)
            {
                return NotFound(false);
            }

            var team = new Team { WorkTrackingSystemConnection = workTrackingSystem };
            SyncTeamWithTeamSettings(team, teamSettingDto);

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            var result = await workItemService.ValidateTeamSettings(team);

            return Ok(result);
        }

        [HttpGet("{teamId}/workitems")]
        public ActionResult<IEnumerable<WorkItemDto>> GetWorkItems(int teamId)
        {
            var team = teamRepository.GetById(teamId);
            if (team == null)
            {
                return NotFound();
            }

            var random = new Random();
            var workItems = new List<WorkItemDto>();

            for (int i = 0; i < 20; i++)
            {
                var startDate = DateTime.Now.AddDays(-random.Next(0, 30));
                var completedDate = startDate.AddDays(random.Next(0, 10));

                workItems.Add(new WorkItemDto
                {
                    Id = i + 1,
                    Name = $"WorkItem {i + 1}",
                    WorkItemReference = $"WI-{random.Next(1000, 9999)}",
                    Url = $"http://example.com/workitem/{i + 1}",
                    StartedDate = startDate,
                    ClosedDate = completedDate
                });
            }

            return Ok(workItems);
        }

        private static void SyncTeamWithTeamSettings(Team team, TeamSettingDto teamSetting)
        {
            team.Name = teamSetting.Name;
            team.WorkItemQuery = teamSetting.WorkItemQuery;
            team.AdditionalRelatedField = teamSetting.RelationCustomField;
            team.FeatureWIP = teamSetting.FeatureWIP;
            team.UseFixedDatesForThroughput = teamSetting.UseFixedDatesForThroughput;
            team.ThroughputHistory = teamSetting.ThroughputHistory;
            team.ThroughputHistoryStartDate = teamSetting.ThroughputHistoryStartDate;
            team.ThroughputHistoryEndDate = teamSetting.ThroughputHistoryEndDate;
            team.WorkItemTypes = teamSetting.WorkItemTypes;
            team.ToDoStates = teamSetting.ToDoStates;
            team.DoingStates = teamSetting.DoingStates;
            team.DoneStates = teamSetting.DoneStates;
            team.WorkTrackingSystemConnectionId = teamSetting.WorkTrackingSystemConnectionId;
            team.AutomaticallyAdjustFeatureWIP = teamSetting.AutomaticallyAdjustFeatureWIP;
        }

        private static TeamDto CreateTeamDto(List<Project> allProjects, List<Feature> allFeatures, Team team)
        {
            var teamDto = new TeamDto(team);

            var teamProjects = allProjects.Where(p => p.Teams.Any(t => t.Id == team.Id)).ToList();

            var features = new List<Feature>();

            foreach (var feature in allFeatures.Where(f => f.FeatureWork.Exists(rw => rw.TeamId == team.Id)))
            {
                features.Add(feature);
            }

            teamDto.Projects.AddRange(teamProjects.Select(p => new ProjectDto(p)));
            teamDto.Features.AddRange(features.Select(f => new FeatureDto(f)));
            return teamDto;
        }
    }
}
