using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemInfoController : ControllerBase
    {
        private readonly ISystemInfoService systemInfoService;

        public SystemInfoController(ISystemInfoService systemInfoService)
        {
            this.systemInfoService = systemInfoService;
        }

        [HttpGet]
        [ProducesResponseType<SystemInfo>(StatusCodes.Status200OK)]
        public ActionResult<SystemInfo> GetSystemInfo()
        {
            var systemInfo = systemInfoService.GetSystemInfo();
            return Ok(systemInfo);
        }
    }
}
