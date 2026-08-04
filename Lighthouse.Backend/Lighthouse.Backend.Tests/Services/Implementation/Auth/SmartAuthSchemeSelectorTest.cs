using Lighthouse.Backend.Services.Implementation.Auth;
using Microsoft.AspNetCore.Http;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [Category("epic-5305-k8s-readiness")]
    public class SmartAuthSchemeSelectorTest
    {
        [Test]
        public void Select_XApiKeyHeader_RoutesToApiKeyScheme()
        {
            var headers = new HeaderDictionary { ["X-Api-Key"] = "some-key" };

            Assert.That(SmartAuthSchemeSelector.Select(headers), Is.EqualTo(SmartAuthSchemeSelector.ApiKeyScheme));
        }

        [Test]
        public void Select_AuthorizationBearer_RoutesToJwtBearerScheme()
        {
            var headers = new HeaderDictionary { ["Authorization"] = "Bearer token-value" };

            Assert.That(SmartAuthSchemeSelector.Select(headers), Is.EqualTo(SmartAuthSchemeSelector.JwtBearerScheme));
        }

        [Test]
        public void Select_BothApiKeyAndBearer_ApiKeyTakesPrecedence()
        {
            var headers = new HeaderDictionary
            {
                ["X-Api-Key"] = "some-key",
                ["Authorization"] = "Bearer token-value",
            };

            Assert.That(SmartAuthSchemeSelector.Select(headers), Is.EqualTo(SmartAuthSchemeSelector.ApiKeyScheme));
        }

        [Test]
        public void Select_NoCredentialHeaders_RoutesToCookieScheme()
        {
            var headers = new HeaderDictionary();

            Assert.That(SmartAuthSchemeSelector.Select(headers), Is.EqualTo(SmartAuthSchemeSelector.CookieScheme));
        }

        [Test]
        public void Select_NonBearerAuthorization_RoutesToCookieScheme()
        {
            var headers = new HeaderDictionary { ["Authorization"] = "Basic dXNlcjpwYXNz" };

            Assert.That(SmartAuthSchemeSelector.Select(headers), Is.EqualTo(SmartAuthSchemeSelector.CookieScheme));
        }

        // Epic 5146 slice 02a (#5641) — ADR-130. Both directions are pinned: the two cookie names
        // are the only thing separating a cross-site embed session from an ordinary browser one.
        [Test]
        public void Select_NoRequest_Refuses()
        {
            Assert.Throws<ArgumentNullException>(() => SmartAuthSchemeSelector.Select((HttpRequest)null!));
        }

        [Test]
        public void Select_EmbedCookie_RoutesToTheEmbedCookieScheme()
        {
            var context = AContextCarrying($"{SmartAuthSchemeSelector.EmbedCookieName}=embed-value");

            Assert.That(SmartAuthSchemeSelector.Select(context.Request), Is.EqualTo(SmartAuthSchemeSelector.EmbedCookieScheme));
        }

        [Test]
        public void Select_OrdinarySessionCookie_RoutesToTheOrdinaryCookieScheme()
        {
            var context = AContextCarrying($"{SessionCookieName}=session-value");

            Assert.That(SmartAuthSchemeSelector.Select(context.Request), Is.EqualTo(SmartAuthSchemeSelector.CookieScheme),
                "an ordinary browser session must never be authenticated by the scheme that relaxed SameSite");
        }

        [Test]
        public void Select_NoCookieAtAll_RoutesToTheOrdinaryCookieScheme()
        {
            Assert.That(SmartAuthSchemeSelector.Select(new DefaultHttpContext().Request), Is.EqualTo(SmartAuthSchemeSelector.CookieScheme));
        }

        [Test]
        public void Select_ApiKeyHeaderAlongsideAnEmbedCookie_StillRoutesToTheApiKeyScheme()
        {
            var context = AContextCarrying($"{SmartAuthSchemeSelector.EmbedCookieName}=embed-value");
            context.Request.Headers["X-Api-Key"] = "some-key";

            Assert.That(SmartAuthSchemeSelector.Select(context.Request), Is.EqualTo(SmartAuthSchemeSelector.ApiKeyScheme),
                "the header-borne schemes are decided first, so a stale embed cookie cannot hijack an explicit key");
        }

        // Security review F4. Partitioning normally keeps these two apart, so this pairing only
        // arises when somebody opens an embed link at the top level - and there the identity that
        // wins must be the person's own, not the API key riding along in the other cookie.
        [Test]
        public void Select_BothCookies_PrefersTheOrdinarySessionOverTheEmbedCookie()
        {
            var context = AContextCarrying(
                $"{SessionCookieName}=session-value; {SmartAuthSchemeSelector.EmbedCookieName}=embed-value");

            Assert.That(SmartAuthSchemeSelector.Select(context.Request), Is.EqualTo(SmartAuthSchemeSelector.CookieScheme),
                "a clicked embed link must not silently replace who the signed-in person is");
        }

        private const string SessionCookieName = SmartAuthSchemeSelector.SessionCookieName;

        private static DefaultHttpContext AContextCarrying(string cookieHeader)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = cookieHeader;
            return context;
        }
    }
}
