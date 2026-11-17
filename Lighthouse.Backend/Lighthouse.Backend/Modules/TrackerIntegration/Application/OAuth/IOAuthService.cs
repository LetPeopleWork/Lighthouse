using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public interface IOAuthService
    {
        Task<Uri> InitiateAsync(int connectionId, CancellationToken cancellationToken);

        Task<OAuthCompleteResult> CompleteAsync(string code, string state, CancellationToken cancellationToken);

        Task DisconnectAsync(int connectionId, CancellationToken cancellationToken);

        Task<string> EnsureFreshTokenAsync(int connectionId, CancellationToken cancellationToken);
    }
}
