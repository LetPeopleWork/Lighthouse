using Lighthouse.Backend.Models;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Interfaces.Authorization
{
    /// <summary>
    /// The move rule, in one place: a Feature may be moved only by someone who may write <b>every</b>
    /// Portfolio it belongs to, and a Feature that belongs to none may be moved by nobody (ADR-136).
    /// <para>
    /// <see cref="RbacGuardAttribute"/> cannot express it — the guard resolves a single scope id from a
    /// route key, and this rule is a conjunction over a set the route does not carry.
    /// </para>
    /// </summary>
    public interface IFeatureMoveAuthorization
    {
        /// <summary>
        /// One verdict per Feature, resolved with a single write-scope lookup rather than one per row.
        /// <paramref name="readablePortfolioIds"/> filters what a refusal may <i>name</i>: a Portfolio the
        /// caller cannot read is never named, because naming it would say that it exists (ADR-136 §3).
        /// </summary>
        Task<IReadOnlyDictionary<int, FeatureMoveVerdict>> GetVerdictsAsync(
            ClaimsPrincipal user,
            IReadOnlyCollection<Feature> features,
            ISet<int> readablePortfolioIds,
            CancellationToken cancellationToken = default);
    }
}
