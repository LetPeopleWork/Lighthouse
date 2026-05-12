using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/apikeys")]
    [Route("api/latest/apikeys")]
    [ApiController]
    [Authorize]
    public class ApiKeyController(
        IApiKeyService apiKeyService,
        IRbacAdministrationService rbacAdministrationService) : ControllerBase
    {
        /// <summary>
        /// Creates a new API key. The plaintext key is returned only once.
        /// </summary>
        [HttpPost]
        [EnableRateLimiting(RateLimitingConfiguration.ApiKeysPolicy)]
        [ProducesResponseType<ApiKeyCreationResult>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("API key name is required.");
            }

            var stableSubject = GetStableSubject();
            if (string.IsNullOrWhiteSpace(stableSubject))
            {
                return Forbid();
            }

            if (request.Scope is { Count: > 0 })
            {
                foreach (var scopeEntry in request.Scope)
                {
                    var requirement = MapScopeEntryToRequirement(scopeEntry);
                    if (requirement is null)
                    {
                        return BadRequest("API key scope entry is invalid.");
                    }

                    var callerCanGrant = await rbacAdministrationService.CanSatisfyRequirementAsync(
                        User,
                        requirement.Value,
                        scopeEntry.ScopeId,
                        cancellationToken);

                    if (!callerCanGrant)
                    {
                        return StatusCode(
                            StatusCodes.Status403Forbidden,
                            "Cannot issue API key with broader scope than your own permissions.");
                    }
                }
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
            var result = await apiKeyService.CreateApiKeyAsync(
                request.Name,
                request.Description ?? string.Empty,
                userName,
                stableSubject,
                request.Scope);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        private static RbacGuardRequirement? MapScopeEntryToRequirement(ApiKeyScopeDto scopeEntry)
        {
            return scopeEntry.Role switch
            {
                UserRole.SystemAdmin when scopeEntry.ScopeType == PermissionScopeType.System => RbacGuardRequirement.SystemAdmin,
                UserRole.TeamAdmin when scopeEntry.ScopeType == PermissionScopeType.Team && scopeEntry.ScopeId.HasValue => RbacGuardRequirement.TeamWrite,
                UserRole.PortfolioAdmin when scopeEntry.ScopeType == PermissionScopeType.Portfolio && scopeEntry.ScopeId.HasValue => RbacGuardRequirement.PortfolioWrite,
                UserRole.Viewer when scopeEntry.ScopeType == PermissionScopeType.Team && scopeEntry.ScopeId.HasValue => RbacGuardRequirement.TeamRead,
                UserRole.Viewer when scopeEntry.ScopeType == PermissionScopeType.Portfolio && scopeEntry.ScopeId.HasValue => RbacGuardRequirement.PortfolioRead,
                _ => null,
            };
        }

        /// <summary>
        /// Returns metadata for all API keys. Never includes the plaintext key or hash.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<IEnumerable<ApiKeyInfo>>(StatusCodes.Status200OK)]
        public IActionResult GetApiKeys()
        {
            var stableSubject = GetStableSubject();
            if (string.IsNullOrWhiteSpace(stableSubject))
            {
                return Forbid();
            }

            var keys = apiKeyService.GetApiKeysByOwnerSubject(stableSubject);
            return Ok(keys);
        }

        /// <summary>
        /// Deletes an API key by ID.
        /// </summary>
        [HttpDelete("{id}")]
        [EnableRateLimiting(RateLimitingConfiguration.ApiKeysPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteApiKey(int id)
        {
            var stableSubject = GetStableSubject();
            if (string.IsNullOrWhiteSpace(stableSubject))
            {
                return Forbid();
            }

            var deleted = await apiKeyService.DeleteApiKey(id, stableSubject);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private string? GetStableSubject()
        {
            return User.FindFirst("sub")?.Value
                ?? User.FindFirst("oid")?.Value;
        }
    }
}
