using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The question is which key wrote a stored value, and the shape of the value cannot answer it. An
    // install that set a key of its own before this release stores values that look exactly like the ones
    // the published key wrote, and telling that operator their credentials are public is both false and
    // the kind of false that makes them stop believing the panel. So the values that could be on that key
    // are narrowed in the database - an envelope naming it, or anything written before the envelope
    // format existed - and then that key is asked to read them.
    //
    // The settings page asks this on every load, so the narrowing matters: an instance that has moved
    // everything onto a key of its own has nothing left to ask about and decrypts nothing at all. What is
    // left to read is bounded by the number of credentials the operator is being told to move.
    //
    // The narrowing can only be relied on because that key is refused as the key secrets are written
    // under, wherever it is supplied from. Without that refusal a value could carry an envelope naming
    // some other key while holding this one's bytes, and no predicate over the stored text would find it.
    public sealed class PublishedKeySecretCount : IPublishedKeySecretCount
    {
        // Stryker disable once String: the separator narrows nothing on its own - every id this could
        // then also match would still be handed to the same key to read, and rejected there.
        private static readonly string PublishedKeyPrefix = SecretEnvelope.Prefix + LegacyDefaultEncryptionKey.Id + ".";

        private readonly LighthouseAppContext context;

        public PublishedKeySecretCount(LighthouseAppContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            this.context = context;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var options = await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret
                    && !string.IsNullOrEmpty(option.Value)
                    && (option.Value.StartsWith(PublishedKeyPrefix) || !option.Value.StartsWith(SecretEnvelope.Prefix)))
                .Select(option => option.Value)
                .ToListAsync(cancellationToken);

            // Stryker disable once Logical: dropping the emptiness guard changes which rows are dragged
            // out of the database, never the answer - an empty column is not something that key can read.
            var accessTokens = await context.Set<OAuthCredential>()
                .Where(credential => !string.IsNullOrEmpty(credential.AccessToken)
                    && (credential.AccessToken.StartsWith(PublishedKeyPrefix) || !credential.AccessToken.StartsWith(SecretEnvelope.Prefix)))
                .Select(credential => credential.AccessToken)
                .ToListAsync(cancellationToken);

            // Stryker disable once Logical: same as above - the guard is about what is fetched, and the
            // count is decided afterwards by what that key can read.
            var refreshTokens = await context.Set<OAuthCredential>()
                .Where(credential => !string.IsNullOrEmpty(credential.RefreshToken)
                    && (credential.RefreshToken.StartsWith(PublishedKeyPrefix) || !credential.RefreshToken.StartsWith(SecretEnvelope.Prefix)))
                .Select(credential => credential.RefreshToken)
                .ToListAsync(cancellationToken);

            return options.Concat(accessTokens).Concat(refreshTokens).Count(LegacyDefaultEncryptionKey.CanRead);
        }
    }
}
