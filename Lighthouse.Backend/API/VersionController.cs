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

        [HttpGet("latest")]
        public async Task<ActionResult<LighthouseRelease>> GetLatestRelease()
        {
            var lighthouseRelease = await lighthouseReleaseService.GetLatestRelease();

            if (lighthouseRelease == null)
            {
                return NotFound();
            }
            
            return Ok(lighthouseRelease);
        }
    }
}
