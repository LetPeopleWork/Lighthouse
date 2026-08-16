using Lighthouse.Backend.Services.Interfaces.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The two facts are worked out differently and are kept apart behind this: one asks the published key
    // whether it can read a value, the other reads the name off the front of one. Putting them together
    // here rather than merging them keeps each testable on its own, and keeps the panel from having to
    // know that the answer to "what is exposed" and the answer to "which keys matter" come from
    // different questions about the same rows.
    public sealed class StoredSecretSummaryReader : IStoredSecretSummary
    {
        private readonly IPublishedKeySecretCount publishedKeySecrets;

        private readonly IReferencedKeyIds referencedKeys;

        public StoredSecretSummaryReader(IPublishedKeySecretCount publishedKeySecrets, IReferencedKeyIds referencedKeys)
        {
            ArgumentNullException.ThrowIfNull(publishedKeySecrets);
            ArgumentNullException.ThrowIfNull(referencedKeys);

            this.publishedKeySecrets = publishedKeySecrets;
            this.referencedKeys = referencedKeys;
        }

        public async Task<StoredSecretSummary> ReadAsync(CancellationToken cancellationToken = default)
        {
            return new StoredSecretSummary(
                await publishedKeySecrets.CountAsync(cancellationToken),
                await referencedKeys.ReadAsync(cancellationToken));
        }
    }
}
