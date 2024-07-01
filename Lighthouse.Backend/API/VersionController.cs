using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public VersionController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetVersion()
        {
            var version = configuration.GetValue<string>("LighthouseVersion");

            if (string.IsNullOrEmpty(version))
            {
                return NotFound("404");
            }

            return Ok(version);
        }
    }
}
