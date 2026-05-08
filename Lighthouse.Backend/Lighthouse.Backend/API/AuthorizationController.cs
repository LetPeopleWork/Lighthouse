using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/authorization")]
    [Route("api/latest/authorization")]
    [ApiController]
    [Authorize]
    public class AuthorizationController(IRbacAdministrationService rbacAdministrationService) : ControllerBase
    {
        [HttpGet("status")]
        [ProducesResponseType<RbacStatus>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            var status = await rbacAdministrationService.GetStatusAsync(cancellationToken);
            return Ok(status);
        }

        [HttpPost("bootstrap/system-admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BootstrapCurrentUserAsSystemAdmin(CancellationToken cancellationToken)
        {
            var result = await rbacAdministrationService.BootstrapCurrentUserAsSystemAdminAsync(User, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.MissingStableSubject)
            {
                return Forbid();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.AlreadyBootstrapped)
            {
                return Conflict(result.Message);
            }

            return BadRequest(result.Message);
        }

        [HttpGet("users")]
        [ProducesResponseType<IReadOnlyList<RbacUserSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var users = await rbacAdministrationService.GetUsersAsync(cancellationToken);
            return Ok(users);
        }

        [HttpPost("system-admins/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GrantSystemAdmin(int userProfileId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.GrantSystemAdminAsync(userProfileId, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.UserNotFound)
            {
                return NotFound(result.Message);
            }

            return BadRequest(result.Message);
        }

        [HttpDelete("system-admins/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeSystemAdmin(int userProfileId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.RevokeSystemAdminAsync(userProfileId, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.LastSystemAdmin)
            {
                return BadRequest(result.Message);
            }

            return BadRequest(result.Message);
        }
    }
}