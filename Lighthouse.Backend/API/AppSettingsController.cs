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
        public ActionResult UpdateFeatureRefreshSettings(RefreshSettings refreshSettings)
        {
            appSettingService.UpdateFeatureRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("ThroughputRefresh")]
        public ActionResult<RefreshSettings> GetThroughputRefreshSettings()
        {
            var settings = appSettingService.GetThroughputRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("ThroughputRefresh")]
        public ActionResult UpdateThroughputRefreshSettings(RefreshSettings refreshSettings)
        {
            appSettingService.UpdateThroughputRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("ForecastRefresh")]
        public ActionResult<RefreshSettings> GetForecastRefreshSettings()
        {
            var settings = appSettingService.GetForecastRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("ForecastRefresh")]
        public ActionResult UpdateForecastRefreshSettings(RefreshSettings refreshSettings)
        {
            appSettingService.UpdateForecastRefreshSettings(refreshSettings);
            return Ok();
        }
    }
}
