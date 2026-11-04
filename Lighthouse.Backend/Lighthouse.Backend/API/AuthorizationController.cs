using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/authorization")]
    [Route("api/latest/authorization")]
    [ApiController]
    [Authorize]
    public class AuthorizationController(IRbacAdministrationService rbacAdministrationService) : ControllerBase
    {
        [SignedInOnly("Says whether access control is switched on and set up, which the interface needs from every signed-in user to decide what to show.")]
        [HttpGet("status")]
        [ProducesResponseType<RbacStatus>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            var status = await rbacAdministrationService.GetStatusAsync(cancellationToken);
            return Ok(status);
        }

        [SignedInOnly("Returns only the roles and rights of the caller who asks, never anybody else's.")]
        [HttpGet("my-summary")]
        [ProducesResponseType<UserAuthorizationSummary>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthorizationSummary(CancellationToken cancellationToken)
        {
            var summary = await rbacAdministrationService.GetAuthorizationSummaryAsync(User, cancellationToken);
            return Ok(summary);
        }

        [CheckedInHandler("The service refuses unless the instance has no System Admin yet, so only the first System Admin can ever be made this way.")]
        [HttpPost("bootstrap/system-admin")]
        [EnableRateLimiting(RateLimitingConfiguration.BootstrapSystemAdminPolicy)]
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

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
        [HttpDelete("users/{userProfileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int userProfileId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.DeleteUserAsync(userProfileId, cancellationToken);
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Team, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Team, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Team, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Portfolio, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Portfolio, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage the membership of this Portfolio, otherwise it is refused.")]
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

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
        [HttpGet("group-mappings")]
        [ProducesResponseType<IReadOnlyList<RbacGroupMappingSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetGroupMappings(CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var mappings = await rbacAdministrationService.GetGroupMappingsAsync(cancellationToken);
            return Ok(mappings);
        }

        [CheckedInHandler("Answers only when the caller may manage the membership of this Team, otherwise it is refused.")]
        [HttpGet("teams/{teamId:int}/group-mappings")]
        [ProducesResponseType<IReadOnlyList<RbacGroupMappingSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTeamGroupMappings(int teamId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageTeamMembershipAsync(User, teamId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var mappings = await rbacAdministrationService.GetTeamGroupMappingsAsync(teamId, cancellationToken);
            return Ok(mappings);
        }

        [CheckedInHandler("Answers only when the caller may manage the membership of this Portfolio, otherwise it is refused.")]
        [HttpGet("portfolios/{portfolioId:int}/group-mappings")]
        [ProducesResponseType<IReadOnlyList<RbacGroupMappingSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPortfolioGroupMappings(int portfolioId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManagePortfolioMembershipAsync(User, portfolioId, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var mappings = await rbacAdministrationService.GetPortfolioGroupMappingsAsync(portfolioId, cancellationToken);
            return Ok(mappings);
        }

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
        [HttpPost("group-mappings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateGroupMapping(
            [FromBody] RbacGroupMappingRequest request,
            CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest("Role is invalid for group mapping.");
            }

            if (!Enum.TryParse<PermissionScopeType>(request.ScopeType, true, out var scopeType))
            {
                return BadRequest("Scope type is invalid for group mapping.");
            }

            var result = await rbacAdministrationService.CreateGroupMappingAsync(
                request.GroupValue,
                role,
                scopeType,
                request.ScopeId,
                cancellationToken);

            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.InvalidScopeForRole)
            {
                return BadRequest(result.Message);
            }

            return BadRequest(result.Message);
        }

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
        [HttpPut("group-mappings/{mappingId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateGroupMappingRole(
            int mappingId,
            [FromBody] RbacGroupMappingRoleUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest("Role is invalid for group mapping.");
            }

            var result = await rbacAdministrationService.UpdateGroupMappingRoleAsync(
                mappingId,
                role,
                request.ConcurrencyToken,
                cancellationToken);

            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.GroupMappingNotFound)
            {
                return NotFound(result.Message);
            }

            return BadRequest(result.Message);
        }

        [CheckedInHandler("Answers only when the caller may manage access for the whole instance, otherwise it is refused.")]
        [HttpDelete("group-mappings/{mappingId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveGroupMapping(int mappingId, CancellationToken cancellationToken)
        {
            var canManage = await rbacAdministrationService.CanManageRbacAsync(User, cancellationToken);
            if (!canManage)
            {
                return Forbid();
            }

            var result = await rbacAdministrationService.RemoveGroupMappingAsync(mappingId, cancellationToken);
            if (result.Succeeded)
            {
                return NoContent();
            }

            if (result.ErrorCode == RbacOperationErrorCodes.GroupMappingNotFound)
            {
                return NotFound(result.Message);
            }

            return BadRequest(result.Message);
        }
    }
}