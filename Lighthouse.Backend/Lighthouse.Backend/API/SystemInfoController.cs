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
        private readonly IRefreshLogService refreshLogService;

        public SystemInfoController(ISystemInfoService systemInfoService, IRefreshLogService refreshLogService)
        {
            this.systemInfoService = systemInfoService;
            this.refreshLogService = refreshLogService;
        }

        [HttpGet]
        [ProducesResponseType<SystemInfo>(StatusCodes.Status200OK)]
        public ActionResult<SystemInfo> GetSystemInfo()
        {
            var systemInfo = systemInfoService.GetSystemInfo();
            return Ok(systemInfo);
        }

        [HttpGet("refreshlog")]
        [ProducesResponseType<IEnumerable<RefreshLog>>(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<RefreshLog>> GetRefreshLog()
        {
            return Ok(refreshLogService.GetRefreshLogs());
        }
    }
}
