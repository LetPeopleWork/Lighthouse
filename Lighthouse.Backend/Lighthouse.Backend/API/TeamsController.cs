using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
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
    public class TeamsController(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> projectRepository,
        IRepository<Feature> featureRepository,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
        ITeamUpdater teamUpdateService,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        ILicenseService licenseService)
        : ControllerBase
    {
        [HttpGet]
        public IEnumerable<TeamDto> GetTeams()
        {
            var teamDtos = new List<TeamDto>();

            var allTeams = teamRepository.GetAll().ToList();
            var allProjects = projectRepository.GetAll().ToList();
            var allFeatures = featureRepository.GetAll().ToList();

            foreach (var team in allTeams)
            {
                var teamDto = team.CreateTeamDto(allProjects, allFeatures);

                teamDtos.Add(teamDto);
            }

            return teamDtos;
        }

        [HttpPost("update-all")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult UpdateAllTeams()
        {
            var teams = teamRepository.GetAll();

            foreach (var team in teams)
            {
                teamUpdateService.TriggerUpdate(team.Id);
            }

            return Ok();
        }

        [HttpPost]
        [LicenseGuard(CheckTeamConstraint = true, TeamLimitOverride = 2)]
        public async Task<ActionResult<TeamSettingDto>> CreateTeam(TeamSettingDto teamSetting)
        {
            teamSetting.Id = 0;
            var newTeam = new Team();
            newTeam.SyncTeamWithTeamSettings(teamSetting);

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

        [HttpPost("validate")]
        [LicenseGuard(CheckTeamConstraint = true)]
        public async Task<ActionResult<bool>> ValidateTeamSettings(TeamSettingDto teamSettingDto)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemConnectionRepository, teamSettingDto.WorkTrackingSystemConnectionId, async workTrackingSystem =>
            {
                var team = new Team { WorkTrackingSystemConnection = workTrackingSystem };
                team.SyncTeamWithTeamSettings(teamSettingDto);

                var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

                return await workItemService.ValidateTeamSettings(team);
            });
        }
    }
}
