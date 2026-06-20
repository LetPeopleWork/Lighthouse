using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public static class SmartAuthSchemeSelector
    {
        public const string ApiKeyScheme = "LighthouseApiKey";
        public const string JwtBearerScheme = "LighthouseJwtBearer";
        public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        private const string ApiKeyHeaderName = "X-Api-Key";
        private const string BearerPrefix = "Bearer ";

        public static string Select(IHeaderDictionary headers)
        {
            if (headers.ContainsKey(ApiKeyHeaderName))
            {
                return ApiKeyScheme;
            }

            if (HasBearerToken(headers))
            {
                return JwtBearerScheme;
            }

            return CookieScheme;
        }

        private static bool HasBearerToken(IHeaderDictionary headers)
        {
            return headers.Authorization.Any(value =>
                value?.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
