using Lighthouse.Backend.Models.Authorization;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Interfaces.Authorization
{
    public interface IRbacAdministrationService
    {
        Task<RbacStatus> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<bool> IsRbacEnforcedAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<int>> GetReadableTeamIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> teamIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<int>> GetReadablePortfolioIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> portfolioIds,
            CancellationToken cancellationToken = default);

        Task<bool> CanReadTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default);

        Task<bool> CanWriteTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default);

        Task<bool> CanReadPortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default);

        Task<bool> CanWritePortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> GrantSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RevokeSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<bool> CanManageRbacAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    }
}