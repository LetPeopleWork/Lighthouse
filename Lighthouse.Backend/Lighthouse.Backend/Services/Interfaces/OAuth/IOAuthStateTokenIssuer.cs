using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Services.Interfaces.OAuth
{
    public interface IOAuthStateTokenIssuer
    {
        string Issue(int connectionId, string providerKey);

        OAuthStateClaims Verify(string token);
    }
}
