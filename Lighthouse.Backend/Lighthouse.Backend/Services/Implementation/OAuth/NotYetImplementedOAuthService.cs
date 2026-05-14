using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class NotYetImplementedOAuthService : IOAuthService
    {
        private const string NotImplementedMessage = "OAuthService is wired in step 01-06; calls to IOAuthService are not supported in the current build.";

        public Task<Uri> InitiateAsync(int connectionId, CancellationToken cancellationToken)
            => throw new NotImplementedException(NotImplementedMessage);

        public Task<OAuthCompleteResult> CompleteAsync(string code, string state, CancellationToken cancellationToken)
            => throw new NotImplementedException(NotImplementedMessage);

        public Task DisconnectAsync(int connectionId, CancellationToken cancellationToken)
            => throw new NotImplementedException(NotImplementedMessage);

        public Task<string> EnsureFreshTokenAsync(int connectionId, CancellationToken cancellationToken)
            => throw new NotImplementedException(NotImplementedMessage);
    }
}
