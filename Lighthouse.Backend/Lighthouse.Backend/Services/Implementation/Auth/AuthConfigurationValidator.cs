using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class AuthConfigurationValidator : IAuthConfigurationValidator
    {
        public AuthConfigurationValidationResult Validate(
            AuthenticationConfiguration authenticationConfiguration,
            AuthorizationConfiguration authorizationConfiguration)
        {
            if (authorizationConfiguration.Enabled && !authenticationConfiguration.Enabled)
            {
                return AuthConfigurationValidationResult.Invalid("Authorization cannot be enabled when authentication is disabled.");
            }

            if (!authenticationConfiguration.Enabled)
            {
                return AuthConfigurationValidationResult.Valid();
            }

            if (string.IsNullOrWhiteSpace(authenticationConfiguration.Authority))
            {
                return AuthConfigurationValidationResult.Invalid("Authority is required when authentication is enabled.");
            }

            if (!Uri.TryCreate(authenticationConfiguration.Authority, UriKind.Absolute, out var authorityUri))
            {
                return AuthConfigurationValidationResult.Invalid("Authority must be a valid absolute URL.");
            }

            if (authenticationConfiguration.RequireHttpsMetadata && !string.Equals(authorityUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                return AuthConfigurationValidationResult.Invalid("Authority must use HTTPS.");
            }

            if (string.IsNullOrWhiteSpace(authenticationConfiguration.ClientId))
            {
                return AuthConfigurationValidationResult.Invalid("ClientId is required when authentication is enabled.");
            }

            if (authenticationConfiguration.Scopes.Count == 0)
            {
                return AuthConfigurationValidationResult.Invalid("Scopes must not be empty when authentication is enabled.");
            }

            if (!authenticationConfiguration.Scopes.Contains("openid", StringComparer.OrdinalIgnoreCase))
            {
                return AuthConfigurationValidationResult.Invalid("Scopes must include 'openid' for OIDC authentication.");
            }

            return AuthConfigurationValidationResult.Valid();
        }
    }
}
