using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class TeamsController(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
        ITeamUpdater teamUpdateService,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        ILicenseService licenseService,
        IRepository<BlackoutPeriod> blackoutPeriodRepository,
        IRbacAdministrationService rbacAdministrationService)
        : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<TeamDto>> GetTeams()
        {
            var teamDtos = new List<TeamDto>();

            var allTeams = teamRepository.GetAll().ToList();
            var teamIds = allTeams.Select(t => t.Id).ToArray();
            var readableTeamIds = await rbacAdministrationService
                .GetReadableTeamIdsAsync(User, teamIds, HttpContext?.RequestAborted ?? default)
                .ConfigureAwait(false);
            var allPortfolios = portfolioRepository.GetAll().ToList();
            var portfolioIds = allPortfolios.Select(p => p.Id).ToArray();
            var readablePortfolioIdSet = await rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext?.RequestAborted ?? default)
                .ConfigureAwait(false);
            var effectiveReadableTeamIds = readableTeamIds ?? teamIds;
            var readableTeamIdSet = effectiveReadableTeamIds.ToHashSet();
            var effectiveReadablePortfolioIds = readablePortfolioIdSet ?? portfolioIds;
            var readablePortfolioIdsSet = effectiveReadablePortfolioIds
                .ToHashSet();
            var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();

            foreach (var team in allTeams.Where(team => readableTeamIdSet.Contains(team.Id)))
            {
                var teamDto = team.CreateTeamDto(allPortfolios, readablePortfolioIdsSet);
                var throughputSettings = team.GetThroughputSettings();
                teamDto.HasThroughputBlackoutOverlap = blackoutPeriods.HasOverlapWithDateRange(throughputSettings.StartDate, throughputSettings.EndDate);

                teamDtos.Add(teamDto);
            }

            return teamDtos;
        }

        [HttpPost("update-all")]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
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
        [RbacGuard(RbacGuardRequirement.CanCreateTeam)]
        public async Task<ActionResult<TeamSettingDto>> CreateTeam(TeamSettingDto teamSetting, CancellationToken cancellationToken = default)
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

            await rbacAdministrationService.EnsureCreatorTeamAdminAsync(User, newTeam.Id, cancellationToken);

            var teamSettingDto = new TeamSettingDto(newTeam);
            return Ok(teamSettingDto);
        }

        [HttpPost("validate")]
        [LicenseGuard(CheckTeamConstraint = true)]
        public async Task<ActionResult<object>> ValidateTeamSettings(TeamSettingDto teamSettingDto, CancellationToken cancellationToken = default)
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
