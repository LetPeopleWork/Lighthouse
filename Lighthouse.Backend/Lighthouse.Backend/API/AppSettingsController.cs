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
            var settings = appSettingService.GetFeatureRefreshSettings();
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
    }
}