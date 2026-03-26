using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class AuthConfigurationValidator : IAuthConfigurationValidator
    {
        public AuthConfigurationValidationResult Validate(AuthenticationConfiguration configuration)
        {
            if (!configuration.Enabled)
            {
                return AuthConfigurationValidationResult.Valid();
            }

            if (string.IsNullOrWhiteSpace(configuration.Authority))
            {
                return AuthConfigurationValidationResult.Invalid("Authority is required when authentication is enabled.");
            }

            if (!Uri.TryCreate(configuration.Authority, UriKind.Absolute, out var authorityUri))
            {
                return AuthConfigurationValidationResult.Invalid("Authority must be a valid absolute URL.");
            }

            if (configuration.RequireHttpsMetadata && !string.Equals(authorityUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                return AuthConfigurationValidationResult.Invalid("Authority must use HTTPS.");
            }

            if (string.IsNullOrWhiteSpace(configuration.ClientId))
            {
                return AuthConfigurationValidationResult.Invalid("ClientId is required when authentication is enabled.");
            }

            if (configuration.Scopes.Count == 0)
            {
                return AuthConfigurationValidationResult.Invalid("Scopes must not be empty when authentication is enabled.");
            }

            if (!configuration.Scopes.Contains("openid", StringComparer.OrdinalIgnoreCase))
            {
                return AuthConfigurationValidationResult.Invalid("Scopes must include 'openid' for OIDC authentication.");
            }

            return AuthConfigurationValidationResult.Valid();
        }
    }
}
