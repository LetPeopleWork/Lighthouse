using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
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

        // Guarded where GetSystemInfo is not: the refresh history is instance-wide operational
        // detail, while GetSystemInfo carries the version and auth status the app shell needs for
        // every signed-in user. [Authorize] alone asks only that the caller be somebody, and after
        // ADR-137 that includes any viewer who reaches the Jira frame.
        [HttpGet("refreshlog")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        [ProducesResponseType<IEnumerable<RefreshLog>>(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<RefreshLog>> GetRefreshLog()
        {
            return Ok(refreshLogService.GetRefreshLogs());
        }
    }
}
