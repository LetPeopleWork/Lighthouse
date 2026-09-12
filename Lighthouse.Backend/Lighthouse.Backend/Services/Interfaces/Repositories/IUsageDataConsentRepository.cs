using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    /// <summary>
    /// Deliberately not <c>IRepository&lt;T&gt;</c>, for two independent reasons. The generic
    /// <c>GetByPredicate</c> takes a <c>Func</c> and so filters in memory over a materialised table -
    /// fine for the handful of rows in application settings, but "is anyone still consenting" would
    /// then load every consent row on the instance every time it is asked. And withdrawing consent
    /// and refreshing liveness are conditional updates whose affected-row count is the answer: done
    /// as read-then-write, a withdrawal and a concurrent refresh can race, and the losing order
    /// silently re-arms consent somebody just withdrew.
    /// </summary>
    public interface IUsageDataConsentRepository
    {
        Task AddAsync(UsageDataConsent consent, CancellationToken cancellationToken);

        Task<UsageDataConsent?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes how recently this browser was seen, but only if the stored stamp is older than
        /// <paramref name="staleBefore"/>. The throttle is what keeps a read-shaped request off the
        /// write path: without it every page load writes, and SQLite serialises writers across the
        /// whole process, so a user with several tabs open would contend with the background
        /// refreshes. The caller owns the threshold because how long consent stays alive is a product
        /// decision, not something a repository should know.
        /// </summary>
        /// <returns>The number of rows updated: 1 if the stamp moved, 0 if it was already fresh.</returns>
        Task<int> TouchAsync(string tokenHash, DateTime seenAt, DateTime staleBefore, CancellationToken cancellationToken);

        /// <summary>
        /// Withdraws a grant, and reports how many rows that affected. Callers must not turn that
        /// count into a different response for the browser: answering differently for a token that
        /// exists would make this endpoint a way of discovering which tokens are real.
        /// </summary>
        Task<int> TryRevokeAsync(string tokenHash, DateTime revokedAt, CancellationToken cancellationToken);

        /// <summary>
        /// Whether any browser still holds live consent. This is the whole of the question the emitter
        /// asks, which is why it is one indexed existence check rather than a list to be counted:
        /// withdrawing the last live grant makes it false the next time it is asked, with no separate
        /// rule anywhere that has to remember to stop sending.
        /// </summary>
        Task<bool> AnyLiveGrantAsync(DateTime livenessThreshold, CancellationToken cancellationToken);

        Task<int> PruneStaleAsync(DateTime threshold, CancellationToken cancellationToken);
    }
}
