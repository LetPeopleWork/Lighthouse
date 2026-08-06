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
        private const int DefaultTokenLifetimeSeconds = 60;
        private const int TokenIdByteLength = 16;
        private const int SecretByteLength = 32;
        private const char TokenSeparator = '.';

        public async Task<EmbedSessionTokenMintResult> MintAsync(int apiKeyId, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await repository.PruneSpentAsync(now, cancellationToken);

            var tokenId = GenerateUrlSafeValue(TokenIdByteLength);
            var secret = GenerateUrlSafeValue(SecretByteLength);
            var expiresAt = now.AddSeconds(ResolveTokenLifetimeSeconds());

            await repository.AddAsync(
                new EmbedSessionToken
                {
                    TokenId = tokenId,
                    SecretHash = HashSecret(secret),
                    ApiKeyId = apiKeyId,
                    CreatedAt = now,
                    ExpiresAt = expiresAt,
                },
                cancellationToken);

            logger.LogDebug("Minted embed session token for API key {ApiKeyId}, expiring at {ExpiresAt}", apiKeyId, expiresAt);

            return new EmbedSessionTokenMintResult
            {
                Token = $"{tokenId}{TokenSeparator}{secret}",
                ExpiresAt = expiresAt,
            };
        }

        public async Task<EmbedSessionTokenRedemption> RedeemAsync(string? token, CancellationToken cancellationToken)
        {
            if (!TrySplit(token, out var tokenId, out var secret))
            {
                return EmbedSessionTokenRedemption.Refused;
            }

            var stored = await repository.FindByTokenIdAsync(tokenId, cancellationToken);

            // A row without an API key is a viewer-path row (ADR-132 D63); this entry point only redeems the API-key path.
            if (stored is null || stored.ApiKeyId is not int apiKeyId || !SecretMatches(secret, stored.SecretHash))
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

            return new EmbedSessionTokenRedemption(true, apiKeyId);
        }

        public async Task RevokeAllAsync(int apiKeyId, CancellationToken cancellationToken)
        {
            var revoked = await repository.RevokeOutstandingForApiKeyAsync(
                apiKeyId,
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);

            logger.LogInformation("Revoked {Count} outstanding embed session tokens for API key {ApiKeyId}", revoked, apiKeyId);
        }

        private int ResolveTokenLifetimeSeconds()
        {
            var configured = embedConfiguration.CurrentValue.TokenLifetimeSeconds;
            return configured > 0 ? configured : DefaultTokenLifetimeSeconds;
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
