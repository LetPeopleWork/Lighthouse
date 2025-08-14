using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppSettingsController : ControllerBase
    {
        private readonly IAppSettingService appSettingService;

        public AppSettingsController(IAppSettingService appSettingService)
        {
            this.appSettingService = appSettingService;
        }

        [HttpGet("FeatureRefresh")]
        public ActionResult<RefreshSettings> GetFeatureRefreshSettings()
        {
            var settings = appSettingService.GetFeaturRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("FeatureRefresh")]
        public async Task<ActionResult> UpdateFeatureRefreshSettings(RefreshSettings refreshSettings)
        {
            await appSettingService.UpdateFeatureRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("TeamRefresh")]
        public ActionResult<RefreshSettings> GetTeamDataRefreshSettings()
        {
            var settings = appSettingService.GetTeamDataRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("TeamRefresh")]
        public async Task<ActionResult> UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings)
        {
            await appSettingService.UpdateTeamDataRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("DefaultTeamSettings")]
        public ActionResult<TeamSettingDto> GetDefaultTeamSettings()
        {
            var settings = appSettingService.GetDefaultTeamSettings();
            return Ok(settings);
        }

        [HttpPut("DefaultTeamSettings")]
        public async Task<ActionResult> UpdateDefaultTeamSettings(TeamSettingDto defaultTeamSetting)
        {
            await appSettingService.UpdateDefaultTeamSettings(defaultTeamSetting);
            return Ok();
        }

        [HttpGet("DefaultProjectSettings")]
        public ActionResult<ProjectSettingDto> GetDefaultProjectSettings()
        {
            var settings = appSettingService.GetDefaultProjectSettings();
            return Ok(settings);
        }

        [HttpPut("DefaultProjectSettings")]
        public async Task<ActionResult> UpdateDefaultProjectSettings(ProjectSettingDto defaultProjectSettings)
        {
            await appSettingService.UpdateDefaultProjectSettings(defaultProjectSettings);
            return Ok();
        }

        [HttpGet("WorkTrackingSystemSettings")]
        public ActionResult<WorkTrackingSystemSettings> GetWorkTrackingSystemSettings()
        {
            var settings = appSettingService.GetWorkTrackingSystemSettings();
            return Ok(settings);
        }

        [HttpPut("WorkTrackingSystemSettings")]
        public async Task<ActionResult> UpdateWorkTrackingSystemSettings(WorkTrackingSystemSettings workTrackingSystemSettings)
        {
            await appSettingService.UpdateWorkTrackingSystemSettings(workTrackingSystemSettings);
            return Ok();
        }
    }
}