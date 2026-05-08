using Lighthouse.Backend.Models.Auth;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface ICurrentUserProfileService
    {
        Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    }
}