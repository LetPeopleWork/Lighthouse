using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
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
        private readonly IRbacAdministrationService rbac;
        private readonly ILogger<SystemInfoController> logger;

        public SystemInfoController(
            ISystemInfoService systemInfoService,
            IRefreshLogService refreshLogService,
            IRbacAdministrationService rbac,
            ILogger<SystemInfoController> logger)
        {
            this.systemInfoService = systemInfoService;
            this.refreshLogService = refreshLogService;
            this.rbac = rbac;
            this.logger = logger;
        }

        // The route stays open to anybody signed in, because the shell cannot render without what is on
        // it. What changes is that two of its fields are answered only to somebody who could have asked
        // for them directly. The question is asked once and the record decides which fields that covers,
        // so a field added later inherits the answer.
        //
        // An instance not enforcing access control satisfies this for everyone, and that is right: with
        // nobody to tell apart there is nobody to withhold it from, and a standalone operator is exactly
        // who needs to see which key they are on.
        [CheckedInHandler("Open to everybody signed in because the interface needs it, but two details are withheld unless the caller is a System Admin.")]
        [HttpGet]
        [ProducesResponseType<SystemInfo>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SystemInfo>> GetSystemInfo(CancellationToken cancellationToken)
        {
            var systemInfo = systemInfoService.GetSystemInfo();

            return Ok(await rbac.ConfirmsSystemAdminAsync(User, logger, cancellationToken)
                ? systemInfo
                : systemInfo.WithoutWhatOnlyAnAdministratorMaySee());
        }

        // Guarded where GetSystemInfo is not: the refresh history is instance-wide operational
        // detail, while GetSystemInfo carries the version and auth status the app shell needs for
        // every signed-in user. [Authorize] alone asks only that the caller be somebody, and that
        // includes any viewer who opens Lighthouse embedded inside Jira.
        [HttpGet("refreshlog")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        [ProducesResponseType<IEnumerable<RefreshLogDto>>(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<RefreshLogDto>> GetRefreshLog()
        {
            return Ok(RefreshLogDto.ListFor(refreshLogService.GetRefreshLogs()));
        }
    }
}
