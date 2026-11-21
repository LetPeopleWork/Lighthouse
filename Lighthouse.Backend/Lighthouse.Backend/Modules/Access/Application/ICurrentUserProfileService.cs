using Lighthouse.Backend.Models.Auth;
using System.Security.Claims;

namespace Lighthouse.Backend.Modules.Access.Application
{
    public interface ICurrentUserProfileService
    {
        Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    }
}