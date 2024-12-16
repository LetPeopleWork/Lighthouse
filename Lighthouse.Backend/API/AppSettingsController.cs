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

        [HttpGet("ThroughputRefresh")]
        public ActionResult<RefreshSettings> GetThroughputRefreshSettings()
        {
            var settings = appSettingService.GetThroughputRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("ThroughputRefresh")]
        public async Task<ActionResult> UpdateThroughputRefreshSettings(RefreshSettings refreshSettings)
        {
            await appSettingService.UpdateThroughputRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("ForecastRefresh")]
        public ActionResult<RefreshSettings> GetForecastRefreshSettings()
        {
            var settings = appSettingService.GetForecastRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("ForecastRefresh")]
        public async Task<ActionResult> UpdateForecastRefreshSettings(RefreshSettings refreshSettings)
        {
            await appSettingService.UpdateForecastRefreshSettings(refreshSettings);
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

        [HttpGet("DataRetentionSettings")]
        public ActionResult<DataRetentionSettings> GetDataRetentionSettings()
        {
            var settings = appSettingService.GetDataRetentionSettings();
            return Ok(settings);
        }

        [HttpPut("DataRetentionSettings")]
        public async Task<ActionResult> UpdateDataRetentionSettings(DataRetentionSettings dataRetentionSettings)
        {
            await appSettingService.UpdateDataRetentionSettings(dataRetentionSettings);
            return Ok();
        }
    }
}
