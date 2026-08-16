using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

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
        [HttpGet]
        [ProducesResponseType<SystemInfo>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SystemInfo>> GetSystemInfo(CancellationToken cancellationToken)
        {
            var systemInfo = systemInfoService.GetSystemInfo();

            return Ok(await MaySeeEverything(cancellationToken)
                ? systemInfo
                : systemInfo.WithoutWhatOnlyAnAdministratorMaySee());
        }

        // Nobody is not an administrator. Asking about a caller who never signed in would answer a
        // question about a user that does not exist, so the answer is settled here instead - withheld,
        // which is the safe way round for a route that deliberately lets anonymous callers through in
        // deployments that have no authentication at all.
        //
        // And a question that cannot be answered is not a yes. Deciding who is an administrator reaches
        // the database, while everything else on this response is read from configuration and the
        // running process - so a database that will not answer used to leave this endpoint working and
        // must keep doing so. It is what the application shell fetches before it can draw anything at
        // all, and failing it would take the whole interface down to withhold two fields.
        private async Task<bool> MaySeeEverything(CancellationToken cancellationToken)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            try
            {
                return await rbac.CanSatisfyRequirementAsync(
                    User, RbacGuardRequirement.SystemAdmin, cancellationToken: cancellationToken);
            }
            // Said out loud on the way past. Withholding is the safe answer to a question that could not
            // be asked, but it is the same answer a genuine fault in the permission check would produce -
            // and an authorisation bug that only ever shows up as a missing row is one nobody reports.
            catch (Exception couldNotBeAsked) when (couldNotBeAsked is DbException or InvalidOperationException)
            {
                logger.LogWarning(
                    couldNotBeAsked,
                    "Could not work out whether this caller administers the instance, so the system information was answered without what only an administrator may see.");

                return false;
            }
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
