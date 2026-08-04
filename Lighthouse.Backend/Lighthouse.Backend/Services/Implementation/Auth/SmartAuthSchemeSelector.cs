using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public static class SmartAuthSchemeSelector
    {
        public const string ApiKeyScheme = "LighthouseApiKey";
        public const string JwtBearerScheme = "LighthouseJwtBearer";
        public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        public const string EmbedCookieScheme = "LighthouseEmbedCookie";
        public const string EmbedCookieName = ".Lighthouse.Embed";

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

        /// <summary>
        /// ADR-130: the embed cookie routes to its own scheme, after the header-borne schemes and
        /// before the ordinary session cookie.
        /// </summary>
        public static string Select(HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var headerScheme = Select(request.Headers);
            if (headerScheme != CookieScheme)
            {
                return headerScheme;
            }

            return request.Cookies.ContainsKey(EmbedCookieName) ? EmbedCookieScheme : CookieScheme;
        }

        private static bool HasBearerToken(IHeaderDictionary headers)
        {
            return headers.Authorization.Any(value =>
                value?.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
