using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface ICliAuthSessionService
    {
        (string SessionId, DateTime ExpiresAt) StartSession();

        bool TryApproveSession(string sessionId, string userName);

        CliAuthSessionPollResponse PollSession(string sessionId);

        bool ValidateToken(string token, out string? userName);

        void RevokeToken(string token);
    }
}
