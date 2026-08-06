using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class EmbedSessionTokenService(
        IEmbedSessionTokenRepository repository,
        IOptionsMonitor<EmbedConfiguration> embedConfiguration,
        TimeProvider timeProvider,
        ILogger<EmbedSessionTokenService> logger) : IEmbedSessionTokenService
    {
        private const int TokenIdByteLength = 16;
        private const int SecretByteLength = 32;
        private const char TokenSeparator = '.';

        // The test surface and the operator's alert both key on the name, so the message stays free to change.
        private static readonly EventId NonceReplayedEvent = new(0, "EmbedHandshakeNonceReplayed");

        public Task RecordHandshakeGrantAsync(string? subject, string nonce, CancellationToken cancellationToken)
        {
            // CK_EmbedSessionTokens_GrantOrRefusal requires a grant row to carry a secret hash, but
            // nobody may hold the secret until the outcome is claimed — D68's consumption writes the
            // one the viewer actually receives.
            return RecordHandshakeOutcomeAsync(
                new EmbedSessionToken
                {
                    TokenId = GenerateUrlSafeValue(TokenIdByteLength),
                    SecretHash = HashSecret(GenerateUrlSafeValue(SecretByteLength)),
                    Subject = subject,
                },
                nonce,
                cancellationToken);
        }

        public Task RecordHandshakeRefusalAsync(
            string? subject,
            string nonce,
            string refusalCode,
            CancellationToken cancellationToken)
        {
            return RecordHandshakeOutcomeAsync(
                new EmbedSessionToken
                {
                    RefusalCode = refusalCode,
                    Subject = subject,
                },
                nonce,
                cancellationToken);
        }

        public async Task<EmbedHandshakeOutcome> ConsumeHandshakeAsync(string? nonce, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nonce))
            {
                return EmbedHandshakeOutcome.Unresolved;
            }

            var nonceHash = HashSecret(nonce);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var stored = await repository.FindByHandshakeNonceHashAsync(nonceHash, cancellationToken);

            // D45: never resolved, never existed and long expired are one answer, because a channel
            // that can tell them apart is an oracle for live sessions.
            if (stored is null || stored.ExpiresAt <= now)
            {
                return EmbedHandshakeOutcome.Unresolved;
            }

            if (stored.HandshakeConsumedAt is not null)
            {
                LogNonceReplayed();
                return EmbedHandshakeOutcome.Unresolved;
            }

            if (stored.TokenId is { Length: > 0 } tokenId)
            {
                return await ConsumeGrantAsync(tokenId, nonceHash, now, cancellationToken);
            }

            if (stored.RefusalCode is { Length: > 0 } refusalCode)
            {
                return await ConsumeRefusalAsync(refusalCode, nonceHash, now, cancellationToken);
            }

            return EmbedHandshakeOutcome.Unresolved;
        }

        public async Task<EmbedSessionTokenRedemption> RedeemAsync(string? token, CancellationToken cancellationToken)
        {
            if (!TrySplit(token, out var tokenId, out var secret))
            {
                return EmbedSessionTokenRedemption.Refused;
            }

            var stored = await repository.FindByTokenIdAsync(tokenId, cancellationToken);

            // ADR-137 D63: a grant row names either an API key or a viewer. One that names neither is
            // redeemable by nobody, and letting it through would sign a caller in as no-one.
            if (stored is null || !SecretMatches(secret, stored.SecretHash) || !NamesAnIdentity(stored))
            {
                return EmbedSessionTokenRedemption.Refused;
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var affectedRows = await repository.TryMarkRedeemedAsync(tokenId, now, cancellationToken);

            if (affectedRows != 1)
            {
                logger.LogWarning("Embed session token redemption refused: expired, revoked or already spent");
                return EmbedSessionTokenRedemption.Refused;
            }

            return new EmbedSessionTokenRedemption(true, stored.ApiKeyId, stored.Subject);
        }

        private async Task<EmbedHandshakeOutcome> ConsumeGrantAsync(
            string tokenId,
            string nonceHash,
            DateTime now,
            CancellationToken cancellationToken)
        {
            // D71: the digest written at resolution has no plaintext anywhere, so a grant row that
            // leaks before the poll is unredeemable — the secret only exists once this poll mints it.
            var secret = GenerateUrlSafeValue(SecretByteLength);

            // DQ-2: the row leaves the 300-second outcome window for the 60-second token window, so
            // the expiry advertised here is the one TryMarkRedeemedAsync actually enforces.
            var expiresAt = now.AddSeconds(ResolveTokenLifetimeSeconds());

            var affectedRows = await repository.TryConsumeHandshakeGrantAsync(
                nonceHash, now, HashSecret(secret), expiresAt, cancellationToken);

            if (affectedRows != 1)
            {
                LogNonceReplayed();
                return EmbedHandshakeOutcome.Unresolved;
            }

            return new EmbedHandshakeOutcome($"{tokenId}{TokenSeparator}{secret}", expiresAt, null);
        }

        private async Task<EmbedHandshakeOutcome> ConsumeRefusalAsync(
            string refusalCode,
            string nonceHash,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var affectedRows = await repository.TryConsumeHandshakeRefusalAsync(nonceHash, now, cancellationToken);

            if (affectedRows != 1)
            {
                LogNonceReplayed();
                return EmbedHandshakeOutcome.Unresolved;
            }

            return new EmbedHandshakeOutcome(null, null, refusalCode);
        }

        // D62: an invisible impersonation becomes a visible anomaly for one log line. N1 still applies
        // — neither the nonce nor its hash may appear here.
        private void LogNonceReplayed()
        {
            logger.LogWarning(NonceReplayedEvent, "An embed handshake nonce was read again after it had been spent");
        }

        private async Task RecordHandshakeOutcomeAsync(
            EmbedSessionToken outcome,
            string nonce,
            CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            outcome.HandshakeNonceHash = HashSecret(nonce);
            outcome.CreatedAt = now;
            outcome.ExpiresAt = now.AddSeconds(ResolveHandshakeOutcomeLifetimeSeconds());

            await repository.AddAsync(outcome, cancellationToken);
        }

        private int ResolveTokenLifetimeSeconds()
        {
            var configured = embedConfiguration.CurrentValue.TokenLifetimeSeconds;
            return configured > 0 ? configured : EmbedConfiguration.DefaultTokenLifetimeSeconds;
        }

        private int ResolveHandshakeOutcomeLifetimeSeconds()
        {
            var configured = embedConfiguration.CurrentValue.HandshakeOutcomeLifetimeSeconds;
            return configured > 0 ? configured : EmbedConfiguration.DefaultHandshakeOutcomeLifetimeSeconds;
        }

        private static bool NamesAnIdentity(EmbedSessionToken stored)
        {
            return stored.ApiKeyId is not null || !string.IsNullOrWhiteSpace(stored.Subject);
        }

        private static bool TrySplit(string? token, out string tokenId, out string secret)
        {
            tokenId = string.Empty;
            secret = string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split(TokenSeparator);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                return false;
            }

            tokenId = parts[0];
            secret = parts[1];
            return true;
        }

        // The secret is 256-bit random, so a fast digest is correct here and a password KDF is not
        // (ADR-129). Comparison stays constant-time.
        private static string HashSecret(string secret)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
        }

        private static bool SecretMatches(string presentedSecret, string? storedHash)
        {
            if (storedHash is null)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashSecret(presentedSecret)),
                Encoding.UTF8.GetBytes(storedHash));
        }

        private static string GenerateUrlSafeValue(int byteLength)
        {
            return Base64UrlEncode(RandomNumberGenerator.GetBytes(byteLength));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
