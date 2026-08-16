using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Counted by looking at the stored values, never by decrypting them. The settings page asks this on
    // every load, and an instance that has never rotated is exactly the one holding every credential it
    // has under the published key - so a count that decrypted would make the page slowest for the
    // operator who most needs to see it.
    //
    // Two shapes are counted. An envelope naming the published key is the obvious one. The other is
    // anything written before the envelope format existed: an upgraded install carries no key id on its
    // stored values at all, and the published key is the only one that ever read them.
    public sealed class PublishedKeySecretCount : IPublishedKeySecretCount
    {
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
                .CountAsync(
                    option => option.IsSecret
                        && !string.IsNullOrEmpty(option.Value)
                        && (option.Value.StartsWith(PublishedKeyPrefix) || !option.Value.StartsWith(SecretEnvelope.Prefix)),
                    cancellationToken);

            var accessTokens = await context.Set<OAuthCredential>()
                .CountAsync(
                    credential => !string.IsNullOrEmpty(credential.AccessToken)
                        && (credential.AccessToken.StartsWith(PublishedKeyPrefix) || !credential.AccessToken.StartsWith(SecretEnvelope.Prefix)),
                    cancellationToken);

            var refreshTokens = await context.Set<OAuthCredential>()
                .CountAsync(
                    credential => !string.IsNullOrEmpty(credential.RefreshToken)
                        && (credential.RefreshToken.StartsWith(PublishedKeyPrefix) || !credential.RefreshToken.StartsWith(SecretEnvelope.Prefix)),
                    cancellationToken);

            return options + accessTokens + refreshTokens;
        }
    }
}
