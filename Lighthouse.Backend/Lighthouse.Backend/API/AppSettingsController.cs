using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [RbacGuard]
    public class AppSettingsController : ControllerBase
    {
        private readonly IAppSettingService appSettingService;

        public AppSettingsController(IAppSettingService appSettingService)
        {
            this.appSettingService = appSettingService;
        }

        [HttpGet("FeatureRefresh")]
        public async Task<ActionResult<RefreshSettings>> GetFeatureRefreshSettings(CancellationToken cancellationToken)
        {
            var settings = appSettingService.GetFeatureRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("FeatureRefresh")]
        public async Task<ActionResult> UpdateFeatureRefreshSettings(RefreshSettings refreshSettings, CancellationToken cancellationToken)
        {
            await appSettingService.UpdateFeatureRefreshSettings(refreshSettings);
            return Ok();
        }

        [HttpGet("TeamRefresh")]
        public async Task<ActionResult<RefreshSettings>> GetTeamDataRefreshSettings(CancellationToken cancellationToken)
        {
            var settings = appSettingService.GetTeamDataRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("TeamRefresh")]
        public async Task<ActionResult> UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings, CancellationToken cancellationToken)
        {
            await appSettingService.UpdateTeamDataRefreshSettings(refreshSettings);
            return Ok();
        }
    }
}