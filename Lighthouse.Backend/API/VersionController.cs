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

        [HttpGet]
        public IActionResult GetVersion()
        {
            var version = lighthouseReleaseService.GetCurrentVersion();

            if (string.IsNullOrEmpty(version))
            {
                return NotFound("404");
            }

            return Ok(version);
        }
    }
}
