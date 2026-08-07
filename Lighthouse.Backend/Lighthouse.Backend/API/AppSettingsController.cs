using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class AppSettingsController : ControllerBase
    {
        private readonly IAppSettingService appSettingService;

        public AppSettingsController(IAppSettingService appSettingService)
        {
            this.appSettingService = appSettingService;
        }

        [HttpGet("FeatureRefresh")]
        [RbacGuard]
        public async Task<ActionResult<RefreshSettings>> GetFeatureRefreshSettings(CancellationToken cancellationToken)
        {
            var settings = appSettingService.GetFeatureRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("FeatureRefresh")]
        [RbacGuard]
        public async Task<ActionResult> UpdateFeatureRefreshSettings(RefreshSettings refreshSettings, CancellationToken cancellationToken)
        {
            await appSettingService.UpdateFeatureRefreshSettings(refreshSettings);
            return Ok();
        }

        // Deliberately NOT guarded. Every feature list reads this to name its position column, so an
        // instance administrator is not the only one who needs the answer - and the answer is which
        // ordering the instance uses, which every viewer can already see in the list itself.
        [HttpGet("FeatureOrdering")]
        public ActionResult<FeatureOrderingDto> GetFeatureOrdering()
        {
            return Ok(new FeatureOrderingDto { Policy = appSettingService.GetFeatureOrderingPolicy() });
        }

        [HttpPut("FeatureOrdering")]
        [RbacGuard]
        [LicenseGuard(RequirePremium = true)]
        public async Task<ActionResult> UpdateFeatureOrdering(FeatureOrderingDto featureOrdering)
        {
            await appSettingService.SetFeatureOrderingPolicy(featureOrdering.Policy);
            return Ok();
        }

        [HttpGet("TeamRefresh")]
        [RbacGuard]
        public async Task<ActionResult<RefreshSettings>> GetTeamDataRefreshSettings(CancellationToken cancellationToken)
        {
            var settings = appSettingService.GetTeamDataRefreshSettings();
            return Ok(settings);
        }

        [HttpPut("TeamRefresh")]
        [RbacGuard]
        public async Task<ActionResult> UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings, CancellationToken cancellationToken)
        {
            await appSettingService.UpdateTeamDataRefreshSettings(refreshSettings);
            return Ok();
        }
    }
}