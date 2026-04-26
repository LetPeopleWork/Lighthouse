using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    /// <summary>
    /// In-memory store for CLI device-authorization sessions and issued bearer tokens.
    /// NOTE: Sessions and tokens are lost on server restart. Persistent storage can be
    /// added in a future iteration if long-lived token durability is required.
    /// </summary>
    public class CliAuthSessionService : ICliAuthSessionService
    {
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

        private sealed record SessionEntry(
            string SessionId,
            DateTime ExpiresAt,
            string? IssuedToken,
            string? ApprovedUserName);

        private sealed record TokenEntry(
            string UserName,
            DateTime ExpiresAt,
            bool Revoked);

        private readonly ConcurrentDictionary<string, SessionEntry> sessions = new();
        private readonly ConcurrentDictionary<string, TokenEntry> tokens = new();

        public (string SessionId, DateTime ExpiresAt) StartSession()
        {
            var sessionId = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.Add(SessionLifetime);
            sessions[sessionId] = new SessionEntry(sessionId, expiresAt, null, null);
            return (sessionId, expiresAt);
        }

        public bool TryApproveSession(string sessionId, string userName)
        {
            if (!sessions.TryGetValue(sessionId, out var entry))
            {
                return false;
            }

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                sessions.TryRemove(sessionId, out _);
                return false;
            }

            if (entry.IssuedToken is not null)
            {
                return true; // already approved
            }

            var token = GenerateSecureToken();
            tokens[token] = new TokenEntry(userName, DateTime.UtcNow.Add(TokenLifetime), false);
            sessions[sessionId] = entry with { IssuedToken = token, ApprovedUserName = userName };
            return true;
        }

        public CliAuthSessionPollResponse PollSession(string sessionId)
        {
            if (!sessions.TryGetValue(sessionId, out var entry))
            {
                return new CliAuthSessionPollResponse { Status = "expired" };
            }

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                sessions.TryRemove(sessionId, out _);
                return new CliAuthSessionPollResponse { Status = "expired" };
            }

            if (entry.IssuedToken is not null)
            {
                return new CliAuthSessionPollResponse
                {
                    Status = "approved",
                    Token = entry.IssuedToken,
                    UserName = entry.ApprovedUserName,
                };
            }

            return new CliAuthSessionPollResponse { Status = "pending" };
        }

        public bool ValidateToken(string token, out string? userName)
        {
            userName = null;
            if (!tokens.TryGetValue(token, out var entry))
            {
                return false;
            }

            if (entry.Revoked || entry.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            userName = entry.UserName;
            return true;
        }

        public void RevokeToken(string token)
        {
            if (tokens.TryGetValue(token, out var entry))
            {
                tokens[token] = entry with { Revoked = true };
            }
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
