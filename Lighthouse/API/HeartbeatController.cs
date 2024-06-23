using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeartbeatController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var currentDateTime = DateTime.Now;
            return Ok($"Server heartbeat at {currentDateTime}");
        }
    }
}
