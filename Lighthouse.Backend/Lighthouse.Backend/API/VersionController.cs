using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Distribution;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILighthouseReleaseService lighthouseReleaseService;

        public VersionController(ILighthouseReleaseService lighthouseReleaseService)
        {
            this.lighthouseReleaseService = lighthouseReleaseService;
        }

        // The Jira app and the platform's served-version probe both call this from outside Lighthouse
        // with no session, against instances nobody here upgrades, so it has to keep answering anonymously.
        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetCurrentVersion()
        {
            var version = lighthouseReleaseService.GetCurrentVersion();

            if (string.IsNullOrEmpty(version))
            {
                return NotFound("404");
            }

            return Ok(version);
        }

        [HttpGet("hasupdate")]
        public async Task<ActionResult> IsUpdateAvailable()
        {
            var isUpdateAvailable = await lighthouseReleaseService.UpdateAvailable();
            return Ok(isUpdateAvailable);
        }

        [HttpGet("new")]
        public async Task<ActionResult<LighthouseRelease[]>> GetNewReleases()
        {
            var lighthouseReleases = await lighthouseReleaseService.GetNewReleases();

            if (!lighthouseReleases.Any())
            {
                return NotFound();
            }

            return Ok(lighthouseReleases);
        }

        [HttpGet("updateSupported")]
        public ActionResult<bool> IsUpdateSupported()
        {
            var isSupported = lighthouseReleaseService.IsUpdateSupported();
            return Ok(isSupported);
        }

        [HttpPost("installUpdate")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult<bool>> InstallUpdate()
        {
            var result = await lighthouseReleaseService.InstallUpdate();
            return Ok(result);
        }

        [HttpGet("distribution")]
        [ProducesResponseType<DistributionInfo>(StatusCodes.Status200OK)]
        public ActionResult<DistributionInfo> GetDistributionInfo()
        {
            var distributionInfo = lighthouseReleaseService.GetDistributionInfo();
            return Ok(distributionInfo);
        }
    }
}
