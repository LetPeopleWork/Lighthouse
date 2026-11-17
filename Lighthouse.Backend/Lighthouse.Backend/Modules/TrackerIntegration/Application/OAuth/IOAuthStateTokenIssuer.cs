using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public interface IOAuthStateTokenIssuer
    {
        string Issue(int connectionId, string providerKey);

        OAuthStateClaims Verify(string token);
    }
}
