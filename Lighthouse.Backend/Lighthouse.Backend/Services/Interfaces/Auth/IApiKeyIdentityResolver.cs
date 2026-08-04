using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    /// <summary>
    /// Resolves the identity behind an already-authenticated API key id, so the embed redemption
    /// path can feed <c>ApiKeyPrincipalFactory</c> the same input the header path feeds it without
    /// holding the plaintext key.
    /// </summary>
    public interface IApiKeyIdentityResolver
    {
        ApiKeyValidationResult? ResolveByApiKeyId(int apiKeyId);
    }
}
