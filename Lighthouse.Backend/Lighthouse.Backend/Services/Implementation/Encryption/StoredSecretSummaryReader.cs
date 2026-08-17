using Lighthouse.Backend.Services.Interfaces.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The three facts are worked out differently and are kept apart behind this: one asks the published key
    // whether it can read a value, one reads the name off the front of one, and one asks every key on the
    // ring. Putting them together here rather than merging them keeps each testable on its own, and keeps
    // the panel from having to know that "what is exposed", "which keys matter" and "is there anything to
    // move" are three different questions about the same rows.
    public sealed class StoredSecretSummaryReader : IStoredSecretSummary
    {
        private readonly IPublishedKeySecretCount publishedKeySecrets;

        private readonly IReferencedKeyIds referencedKeys;

        private readonly IReadableSecretsNotOnTheActiveKey movable;

        public StoredSecretSummaryReader(
            IPublishedKeySecretCount publishedKeySecrets,
            IReferencedKeyIds referencedKeys,
            IReadableSecretsNotOnTheActiveKey movable)
        {
            ArgumentNullException.ThrowIfNull(publishedKeySecrets);
            ArgumentNullException.ThrowIfNull(referencedKeys);
            ArgumentNullException.ThrowIfNull(movable);

            this.publishedKeySecrets = publishedKeySecrets;
            this.referencedKeys = referencedKeys;
            this.movable = movable;
        }

        public async Task<StoredSecretSummary> ReadAsync(CancellationToken cancellationToken = default)
        {
            return new StoredSecretSummary(
                await publishedKeySecrets.CountAsync(cancellationToken),
                await referencedKeys.ReadAsync(cancellationToken),
                await movable.CountAsync(cancellationToken));
        }
    }
}
