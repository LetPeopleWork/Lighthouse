using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Platform.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Platform.Host
{
    /// <summary>
    /// Which answers Lighthouse may send compressed. A compressed answer that holds a secret next to text an attacker
    /// can put there lets the attacker guess the secret from the answer's size, so only safe-method answers of
    /// compressible types are compressed, and never an answer of an endpoint marked as never compressed, whatever its
    /// route is called. A static file answers with no endpoint at all and counts as unmarked.
    /// The rule is asked the way the compression middleware asks it: does the request accept compression, may this
    /// answer be compressed, and is there an encoding both sides know.
    /// </summary>
    public class BreachSafeResponseCompressionProviderTest
    {
        private const string Json = "application/json; charset=utf-8";

        public enum AnsweringEndpoint
        {
            None,
            Unmarked,
            Marked,
            MarkedAmongOtherMetadata,
        }

        [TestCase("GET", Json, "br", AnsweringEndpoint.None)]
        [TestCase("HEAD", "text/javascript", "gzip", AnsweringEndpoint.None)]
        [TestCase("GET", Json, "br", AnsweringEndpoint.Unmarked)]
        public void A_safe_answer_to_a_client_that_accepts_compression_is_compressed(string method, string contentType, string acceptEncoding, AnsweringEndpoint endpoint)
        {
            var rule = TheRuleWithSettings([]);

            var compressed = Compresses(rule, method, contentType, acceptEncoding, endpoint);

            Assert.That(compressed, Is.True, $"WHAT: {method} answering {contentType} to Accept-Encoding {acceptEncoding} from endpoint '{endpoint}' is sent uncompressed. WHY: a safe-method answer of a compressible type from an unmarked endpoint, or a static file with no endpoint, carries no secret, and sending it compressed is what makes a page load faster on a slow connection. HOW: compress GET and HEAD answers of the default compressible types unless the answering endpoint's metadata holds the never-compress marker.");
        }

        [TestCase("POST", Json, "br", AnsweringEndpoint.Unmarked)]
        [TestCase("PUT", Json, "br", AnsweringEndpoint.Unmarked)]
        [TestCase("DELETE", Json, "br", AnsweringEndpoint.Unmarked)]
        [TestCase("GET", "image/png", "br", AnsweringEndpoint.None)]
        [TestCase("GET", "application/problem+json", "br", AnsweringEndpoint.Unmarked)]
        [TestCase("GET", Json, null, AnsweringEndpoint.Unmarked)]
        public void An_unsafe_or_uncompressible_or_not_asked_for_answer_is_sent_as_it_is(string method, string contentType, string? acceptEncoding, AnsweringEndpoint endpoint)
        {
            var rule = TheRuleWithSettings([]);

            var compressed = Compresses(rule, method, contentType, acceptEncoding, endpoint);

            Assert.That(compressed, Is.False, $"WHAT: {method} answering {contentType} to Accept-Encoding '{acceptEncoding}' from endpoint '{endpoint}' is sent compressed. WHY: an unsafe-method answer can echo a secret such as a new API key, images and problem answers are not on the compressible list, and a client that did not ask for compression must get the plain bytes. HOW: answer no unless the method is GET or HEAD, the type is compressible and the client offers br or gzip.");
        }

        [TestCase("text/plain", AnsweringEndpoint.Marked)]
        [TestCase(Json, AnsweringEndpoint.MarkedAmongOtherMetadata)]
        public void An_answer_of_an_endpoint_marked_never_compress_is_sent_as_it_is_whatever_its_route(string contentType, AnsweringEndpoint endpoint)
        {
            var rule = TheRuleWithSettings([]);

            var compressed = Compresses(rule, "GET", contentType, "br", endpoint);

            Assert.That(compressed, Is.False, $"WHAT: a GET answering {contentType} to Accept-Encoding br from an endpoint '{endpoint}' with the never-compress marker is sent compressed. WHY: the hub, the logs and the embed handshake can hold secrets next to attacker text, and a renamed route must not drop their exclusion. HOW: answer no when the answering endpoint's metadata holds {nameof(NeverCompressResponseAttribute)}, whatever the request path is.");
        }

        [Test]
        public void Turning_compression_off_sends_a_safe_answer_as_it_is()
        {
            var rule = TheRuleWithSettings(new Dictionary<string, string?> { ["ResponseCompression:Enabled"] = "false" });

            var compressed = Compresses(rule, "GET", Json, "br", AnsweringEndpoint.Unmarked);

            Assert.That(compressed, Is.False, "WHAT: with ResponseCompression:Enabled false a GET of JSON to Accept-Encoding br is sent compressed. WHY: an operator turns compression off with this switch, for example behind a proxy that compresses itself. HOW: answer no for every request when the switch is off.");
        }

        private static BreachSafeResponseCompressionProvider TheRuleWithSettings(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var theSwitch = configuration.GetSection(ResponseCompressionConfiguration.SectionName).Get<ResponseCompressionConfiguration>() ?? new ResponseCompressionConfiguration();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddSingleton(theSwitch);

            using var provider = services.BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<BreachSafeResponseCompressionProvider>(provider);
        }

        private static bool Compresses(BreachSafeResponseCompressionProvider rule, string method, string contentType, string? acceptEncoding, AnsweringEndpoint endpoint)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Path = "/api/latest/a-route-no-list-names";
            if (acceptEncoding is not null)
            {
                context.Request.Headers.AcceptEncoding = acceptEncoding;
            }

            if (endpoint != AnsweringEndpoint.None)
            {
                context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(MetadataOf(endpoint)), endpoint.ToString()));
            }

            context.Response.ContentType = contentType;

            return rule.CheckRequestAcceptsCompression(context)
                && rule.ShouldCompressResponse(context)
                && rule.GetCompressionProvider(context) is not null;
        }

        private static object[] MetadataOf(AnsweringEndpoint endpoint) => endpoint switch
        {
            AnsweringEndpoint.Unmarked => [new AuthorizeAttribute()],
            AnsweringEndpoint.Marked => [new NeverCompressResponseAttribute()],
            AnsweringEndpoint.MarkedAmongOtherMetadata => [new AuthorizeAttribute(), new NeverCompressResponseAttribute(), new AllowAnonymousAttribute()],
            _ => [],
        };
    }
}
