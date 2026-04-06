using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
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
        IRepository<Portfolio> portfolioRepository,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
        ITeamUpdater teamUpdateService,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        ILicenseService licenseService,
        IRepository<BlackoutPeriod> blackoutPeriodRepository)
        : ControllerBase
    {
        [HttpGet]
        public IEnumerable<TeamDto> GetTeams()
        {
            var teamDtos = new List<TeamDto>();

            var allTeams = teamRepository.GetAll().ToList();
            var allPortfolios = portfolioRepository.GetAll().ToList();
            var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();

            foreach (var team in allTeams)
            {
                var teamDto = team.CreateTeamDto(allPortfolios);
                var throughputSettings = team.GetThroughputSettings();
                teamDto.HasThroughputBlackoutOverlap = blackoutPeriods.HasOverlapWithDateRange(throughputSettings.StartDate, throughputSettings.EndDate);

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
            var baselineValidation = BaselineValidationService.Validate(
                teamSetting.ProcessBehaviourChartBaselineStartDate,
                teamSetting.ProcessBehaviourChartBaselineEndDate,
                teamSetting.DoneItemsCutoffDays);

            if (!baselineValidation.IsValid)
            {
                return BadRequest(baselineValidation.ErrorMessage);
            }

            var stateMappingValidation = StateMappingValidator.ValidateSettings(teamSetting);
            if (!stateMappingValidation.IsValid)
            {
                return BadRequest(stateMappingValidation.Errors);
            }

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
        public async Task<ActionResult<object>> ValidateTeamSettings(TeamSettingDto teamSettingDto)
        {
            var workTrackingSystem = workTrackingSystemConnectionRepository.GetById(teamSettingDto.WorkTrackingSystemConnectionId);
            if (workTrackingSystem == null)
            {
                return NotFound();
            }

            var team = new Team { WorkTrackingSystemConnection = workTrackingSystem };
            team.SyncTeamWithTeamSettings(teamSettingDto);

            var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            var validationResult = await workItemService.ValidateTeamSettings(team);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult);
            }

            return Ok(validationResult);
        }
    }
}
