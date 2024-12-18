using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
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


        [HttpGet("{id}/throughput")]
        public ActionResult GetThroughputForTeam(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            return Ok(team.RawThroughput);
        }

        [HttpPost("{id}")]
        public async Task<ActionResult> UpdateTeamData(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            await teamUpdateService.UpdateTeam(team);

            await teamRepository.Save();

            return Ok();
        }

        [HttpDelete("{id}")]
        public void DeleteTeam(int id)
        {
            teamRepository.Remove(id);
            teamRepository.Save();
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

        private static void SyncTeamWithTeamSettings(Team team, TeamSettingDto teamSetting)
        {
            team.Name = teamSetting.Name;
            team.WorkItemQuery = teamSetting.WorkItemQuery;
            team.AdditionalRelatedField = teamSetting.RelationCustomField;
            team.FeatureWIP = teamSetting.FeatureWIP;
            team.ThroughputHistory = teamSetting.ThroughputHistory;
            team.WorkItemTypes = teamSetting.WorkItemTypes;
            team.ToDoStates = teamSetting.ToDoStates;
            team.DoingStates = teamSetting.DoingStates;
            team.DoneStates = teamSetting.DoneStates;
            team.WorkTrackingSystemConnectionId = teamSetting.WorkTrackingSystemConnectionId;
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
