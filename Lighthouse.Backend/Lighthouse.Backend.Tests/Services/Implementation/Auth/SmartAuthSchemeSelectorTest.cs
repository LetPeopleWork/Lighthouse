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
    }
}
