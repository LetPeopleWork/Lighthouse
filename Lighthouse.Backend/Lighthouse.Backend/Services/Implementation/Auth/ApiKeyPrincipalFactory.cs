using Lighthouse.Backend.Models.Auth;
using System.Globalization;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    // ADR-129 D29: both the X-Api-Key handler and the embed redemption path build their principal
    // here. Independent construction would drift silently, and a dropped api_key_id fails OPEN.
    public static class ApiKeyPrincipalFactory
    {
        public const string ApiKeyIdClaimType = "api_key_id";
        public const string AuthMethodClaimType = "auth_method";
        public const string AuthMethodValue = "api-key";

        // ADR-137 D59: what 01-05 gates the stored group snapshot on. A viewer principal is rebuilt
        // from a subject and carries no live group claims.
        public const string AuthMethodEmbedValue = "embed";
        public const string SubjectClaimType = "sub";
        public const string NameClaimType = "name";
        public const string ApiKeyUserName = "api-key-user";

        public static ClaimsPrincipal Create(ApiKeyValidationResult validationResult, string authenticationScheme)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            var identity = new ClaimsIdentity(BuildClaims(validationResult), authenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        // ADR-137: the viewer path builds its principal here too, and for the same reason the key
        // path does. The class stays pure — no repository, no clock, no HttpContext.
        public static ClaimsPrincipal Create(string subject, string? displayName, string authenticationScheme)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            var claims = new List<Claim>
            {
                new(AuthMethodClaimType, AuthMethodEmbedValue),
                new(SubjectClaimType, subject),
            };

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                claims.Add(new Claim(NameClaimType, displayName));
                claims.Add(new Claim(ClaimTypes.Name, displayName));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationScheme));
        }

        private static List<Claim> BuildClaims(ApiKeyValidationResult validationResult)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, ApiKeyUserName),
                new(AuthMethodClaimType, AuthMethodValue),
            };

            if (validationResult.ApiKeyId.HasValue)
            {
                claims.Add(new Claim(
                    ApiKeyIdClaimType,
                    validationResult.ApiKeyId.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (validationResult.OwnerResolutionState != ApiKeyOwnerResolutionState.Resolved
                || string.IsNullOrWhiteSpace(validationResult.OwnerSubject))
            {
                return claims;
            }

            claims.Add(new Claim(SubjectClaimType, validationResult.OwnerSubject));

            if (!string.IsNullOrWhiteSpace(validationResult.OwnerDisplayName))
            {
                claims.Add(new Claim(NameClaimType, validationResult.OwnerDisplayName));
            }

            return claims;
        }
    }
}
