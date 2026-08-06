using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    /// <summary>
    /// ADR-137 D57: a read-only view over user profiles, offered to callers that must re-resolve a
    /// subject without ever creating one. There is deliberately no create method here — a validator
    /// handed <c>ICurrentUserProfileService</c> instead would resurrect a deleted viewer on their
    /// very next request, turning deletion into a no-op that looks like it works.
    /// </summary>
    public interface IUserProfileLookup
    {
        Task<UserProfile?> FindBySubjectAsync(string subject, CancellationToken cancellationToken);
    }
}
