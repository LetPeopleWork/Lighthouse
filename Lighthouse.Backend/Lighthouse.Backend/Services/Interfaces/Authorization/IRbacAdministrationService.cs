using Lighthouse.Backend.Models.Authorization;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Interfaces.Authorization
{
    public interface IRbacAdministrationService
    {
        Task<RbacStatus> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> GrantSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RevokeSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<bool> CanManageRbacAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    }
}