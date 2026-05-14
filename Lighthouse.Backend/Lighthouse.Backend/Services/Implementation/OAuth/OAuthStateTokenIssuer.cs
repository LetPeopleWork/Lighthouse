using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthStateTokenIssuer : IOAuthStateTokenIssuer
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
        private const int NonceByteLength = 16;
        private const char PayloadHmacSeparator = '.';

        private readonly byte[] secretBytes;
        private readonly TimeProvider timeProvider;

        public OAuthStateTokenIssuer(IServiceConfig serviceConfig, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceConfig);
            ArgumentNullException.ThrowIfNull(timeProvider);

            var secret = serviceConfig.OAuthStateSecret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "Lighthouse:OAuth:StateSecret is not configured. Set the secret in configuration or rely on the startup auto-generation path.");
            }

            secretBytes = Encoding.UTF8.GetBytes(secret);
            this.timeProvider = timeProvider;
        }

        public string Issue(int connectionId, string providerKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

            var claims = new OAuthStateClaims
            {
                ConnectionId = connectionId,
                ProviderKey = providerKey,
                Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(NonceByteLength)),
                ExpiresAt = timeProvider.GetUtcNow() + TokenLifetime,
            };

            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(claims);
            var hashBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

            return $"{Base64UrlEncode(payloadBytes)}{PayloadHmacSeparator}{Base64UrlEncode(hashBytes)}";
        }

        public OAuthStateClaims Verify(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            var separatorIndex = token.IndexOf(PayloadHmacSeparator);
            if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
            {
                throw new OAuthStateTokenInvalidException("OAuth state token is malformed.");
            }

            var payloadSegment = token[..separatorIndex];
            var hashSegment = token[(separatorIndex + 1)..];

            byte[] payloadBytes;
            byte[] suppliedHashBytes;
            try
            {
                payloadBytes = Base64UrlDecode(payloadSegment);
                suppliedHashBytes = Base64UrlDecode(hashSegment);
            }
            catch (FormatException ex)
            {
                throw new OAuthStateTokenInvalidException("OAuth state token contains invalid base64url segments.", ex);
            }

            var expectedHashBytes = HMACSHA256.HashData(secretBytes, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(expectedHashBytes, suppliedHashBytes))
            {
                throw new OAuthStateTokenInvalidException("OAuth state token HMAC mismatch.");
            }

            OAuthStateClaims? claims;
            try
            {
                claims = JsonSerializer.Deserialize<OAuthStateClaims>(payloadBytes);
            }
            catch (JsonException ex)
            {
                throw new OAuthStateTokenInvalidException("OAuth state token payload could not be deserialized.", ex);
            }

            if (claims is null)
            {
                throw new OAuthStateTokenInvalidException("OAuth state token payload deserialized to null.");
            }

            if (claims.ExpiresAt <= timeProvider.GetUtcNow())
            {
                throw new OAuthStateTokenExpiredException("OAuth state token is expired.");
            }

            return claims;
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string segment)
        {
            var padded = segment.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Convert.FromBase64String(padded);
        }
    }
}
