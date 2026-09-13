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
        /// Records that this browser was shown the dialog without having asked for it, so the next
        /// window is measured from the question rather than from an answer that never changed.
        /// Without it a browser that closes the dialog is due again on the very next request, and
        /// every request after that.
        /// </summary>
        /// <returns>How many rows that affected. Callers must not turn the count into a different
        /// response, for the reason withdrawal does not either.</returns>
        Task<int> TryMarkAskedAsync(string tokenHash, DateTime askedAt, CancellationToken cancellationToken);

        /// <summary>
        /// Forgets browsers that stopped visiting, so the table does not grow by one row for every
        /// browser ever shown the dialog.
        /// </summary>
        /// <param name="lastSeenBefore">How long ago a browser has to have been seen to count as gone.</param>
        /// <param name="owedNothingSince">The instant a refusal has to predate to have had its
        /// re-ask fall due. A row still owed its promised question is kept whatever its age -
        /// removing it means the question arrives early on the next visit, which is the promise the
        /// dialog's own copy makes and this would break.</param>
        Task<int> PruneStaleAsync(
            DateTime lastSeenBefore, DateTime owedNothingSince, CancellationToken cancellationToken);
    }
}
