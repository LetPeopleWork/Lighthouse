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

        // Paired with the cookie name configured for the ordinary scheme in Program.cs. Declared here
        // rather than shared from there because that block is deliberately left untouched (K4); a test
        // asserts the two agree, so the duplication cannot drift silently.
        public const string SessionCookieName = ".Lighthouse.Session";

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
        /// ADR-130: the embed cookie routes to its own scheme, after the header-borne schemes.
        /// Security review F4: an ordinary session outranks it. Both cookies normally live in
        /// different partitions and never meet, but opening an embed link at the top level puts them
        /// in the same jar - and there the person's own login must win, or a single clicked link
        /// silently replaces who they are with the identity of somebody's API key.
        /// </summary>
        public static string Select(HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var headerScheme = Select(request.Headers);
            if (headerScheme != CookieScheme)
            {
                return headerScheme;
            }

            if (request.Cookies.ContainsKey(SessionCookieName))
            {
                return CookieScheme;
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
