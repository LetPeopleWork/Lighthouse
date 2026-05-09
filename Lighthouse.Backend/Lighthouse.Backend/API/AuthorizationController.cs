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

        [HttpGet("my-summary")]
        [ProducesResponseType<UserAuthorizationSummary>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthorizationSummary(CancellationToken cancellationToken)
        {
            var summary = await rbacAdministrationService.GetAuthorizationSummaryAsync(User, cancellationToken);
            return Ok(summary);
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

        [HttpGet("teams/{teamId:int}/members")]
        [ProducesResponseType<IReadOnlyList<RbacScopedMemberSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTeamMembers(int teamId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageTeamMembershipAsync(User, teamId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var members = await rbacAdministrationService.GetTeamMembersAsync(teamId, cancellationToken);
            return Ok(members);
        }

        [HttpPut("teams/{teamId:int}/members/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertTeamMember(
            int teamId,
            int userProfileId,
            [FromBody] ScopedMemberRoleRequest request,
            CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageTeamMembershipAsync(User, teamId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest("Role is invalid for team scope.");
            }

            var result = await rbacAdministrationService.SetTeamMemberRoleAsync(userProfileId, teamId, role, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.UserNotFound)
            {
                return NotFound(result.Message);
            }

            if (result.ErrorCode == RbacOperationErrorCodes.InvalidRoleForScope)
            {
                return BadRequest(result.Message);
            }

            return BadRequest(result.Message);
        }

        [HttpDelete("teams/{teamId:int}/members/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveTeamMember(int teamId, int userProfileId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageTeamMembershipAsync(User, teamId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.RemoveTeamMemberAsync(userProfileId, teamId, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return BadRequest(result.Message);
        }

        [HttpGet("portfolios/{portfolioId:int}/members")]
        [ProducesResponseType<IReadOnlyList<RbacScopedMemberSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPortfolioMembers(int portfolioId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManagePortfolioMembershipAsync(User, portfolioId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var members = await rbacAdministrationService.GetPortfolioMembersAsync(portfolioId, cancellationToken);
            return Ok(members);
        }

        [HttpPut("portfolios/{portfolioId:int}/members/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertPortfolioMember(
            int portfolioId,
            int userProfileId,
            [FromBody] ScopedMemberRoleRequest request,
            CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManagePortfolioMembershipAsync(User, portfolioId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest("Role is invalid for portfolio scope.");
            }

            var result = await rbacAdministrationService.SetPortfolioMemberRoleAsync(userProfileId, portfolioId, role, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.UserNotFound)
            {
                return NotFound(result.Message);
            }

            if (result.ErrorCode == RbacOperationErrorCodes.InvalidRoleForScope)
            {
                return BadRequest(result.Message);
            }

            return BadRequest(result.Message);
        }

        [HttpDelete("portfolios/{portfolioId:int}/members/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemovePortfolioMember(int portfolioId, int userProfileId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManagePortfolioMembershipAsync(User, portfolioId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.RemovePortfolioMemberAsync(userProfileId, portfolioId, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return BadRequest(result.Message);
        }
    }
}