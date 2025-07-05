using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILighthouseReleaseService lighthouseReleaseService;

        public VersionController(ILighthouseReleaseService lighthouseReleaseService)
        {
            this.lighthouseReleaseService = lighthouseReleaseService;
        }

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
        public async Task<ActionResult<bool>> InstallUpdate()
        {
            var result = await lighthouseReleaseService.InstallUpdate();
            return Ok(result);
        }
    }
}
