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
        public const string SubjectClaimType = "sub";
        public const string NameClaimType = "name";
        public const string ApiKeyUserName = "api-key-user";

        public static ClaimsPrincipal Create(ApiKeyValidationResult validationResult, string authenticationScheme)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            var identity = new ClaimsIdentity(BuildClaims(validationResult), authenticationScheme);
            return new ClaimsPrincipal(identity);
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
