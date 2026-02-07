using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/teams/{teamId:int}")]
    [ApiController]
    public class TeamController(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> projectRepository,
        IRepository<Feature> featureRepository,
        IWorkItemRepository workItemRepository,
        ITeamUpdater teamUpdateService)
        : ControllerBase
    {
        [HttpGet]
        public ActionResult<TeamDto> GetTeam(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                var allProjects = projectRepository.GetAll().ToList();
                var allFeatures = featureRepository.GetAll().ToList();

                return team.CreateTeamDto(allProjects, allFeatures);
            });
        }

        [HttpPost]
        [LicenseGuard(CheckTeamConstraint = true)]
        public ActionResult UpdateTeamData(int teamId)
        {
            var team = teamRepository.GetById(teamId);

            if (team == null)
            {
                return NotFound(null);
            }

            teamUpdateService.TriggerUpdate(team.Id);

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            teamRepository.Remove(teamId);
            await teamRepository.Save();
            return NoContent();
        }

        [HttpPut]
        [LicenseGuard(CheckTeamConstraint = true)]
        public async Task<ActionResult<TeamSettingDto>> UpdateTeam(int teamId, TeamSettingDto teamSetting)
        {
            var baselineValidation = BaselineValidationService.Validate(
                teamSetting.ProcessBehaviourChartBaselineStartDate,
                teamSetting.ProcessBehaviourChartBaselineEndDate,
                teamSetting.DoneItemsCutoffDays);

            if (!baselineValidation.IsValid)
            {
                return BadRequest(baselineValidation.ErrorMessage);
            }

            return await this.GetEntityByIdAnExecuteAction(teamRepository, teamId, async team =>
            {
                if (team.WorkItemRelatedSettingsChanged(teamSetting))
                {
                    workItemRepository.RemoveWorkItemsForTeam(team.Id);
                    await workItemRepository.Save();
                }

                team.SyncTeamWithTeamSettings(teamSetting);

                teamRepository.Update(team);
                await teamRepository.Save();

                var teamSettingDto = new TeamSettingDto(team);
                return teamSettingDto;
            });
        }

        [HttpGet("settings")]
        public ActionResult<TeamSettingDto> GetTeamSettings(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team => new TeamSettingDto(team));
        }
    }
}
