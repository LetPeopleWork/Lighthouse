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
    /// compressible types are compressed, and never the update hub, the logs or the embed handshake.
    /// The rule is asked the way the compression middleware asks it: does the request accept compression, may this
    /// answer be compressed, and is there an encoding both sides know. The rule is looked up by the name the design
    /// gives it, so that this file builds before the rule exists and fails on its answers instead.
    /// </summary>
    public class BreachSafeResponseCompressionProviderTest
    {
        private const string TheRuleName = "Lighthouse.Backend.Platform.Host.BreachSafeResponseCompressionProvider";

        private const string TheSwitchName = "Lighthouse.Backend.Configuration.ResponseCompressionConfiguration";

        private const string Json = "application/json; charset=utf-8";

        [TestCase("GET", "/api/latest/teams", Json, "br")]
        [TestCase("HEAD", "/assets/app.js", "text/javascript", "gzip")]
        [TestCase("GET", "/api/latest/logsx", Json, "br")]
        public void A_safe_answer_to_a_client_that_accepts_compression_is_compressed(string method, string path, string contentType, string acceptEncoding)
        {
            var rule = TheRuleWithSettings([]);

            var compressed = Compresses(rule, method, path, contentType, acceptEncoding);

            Assert.That(compressed, Is.True, $"WHAT: {method} {path} answering {contentType} to Accept-Encoding {acceptEncoding} is sent uncompressed. WHY: a safe-method answer of a compressible type carries no secret, and sending it compressed is what makes a page load faster on a slow connection; a route that only starts with the letters of an excluded one is not excluded. HOW: compress GET and HEAD answers of the default compressible types unless a path segment prefix names the hub, the logs or the embed handshake.");
        }

        [TestCase("POST", "/api/latest/apikeys", Json, "br")]
        [TestCase("PUT", "/api/latest/teams/1", Json, "br")]
        [TestCase("DELETE", "/api/latest/teams/1", Json, "br")]
        [TestCase("GET", "/assets/logo.png", "image/png", "br")]
        [TestCase("GET", "/api/latest/teams/99", "application/problem+json", "br")]
        [TestCase("GET", "/api/updateNotificationHub", "text/plain", "br")]
        [TestCase("GET", "/API/UPDATENOTIFICATIONHUB", "text/plain", "br")]
        [TestCase("POST", "/api/updateNotificationHub/negotiate", Json, "br")]
        [TestCase("GET", "/api/latest/logs", Json, "br")]
        [TestCase("GET", "/api/v1/Logs/download", "text/plain", "br")]
        [TestCase("GET", "/api/latest/embed/handshake/n1", Json, "br")]
        [TestCase("GET", "/api/latest/teams", Json, null)]
        public void An_answer_that_could_carry_a_secret_or_is_not_asked_for_compressed_is_sent_as_it_is(string method, string path, string contentType, string? acceptEncoding)
        {
            var rule = TheRuleWithSettings([]);

            var compressed = Compresses(rule, method, path, contentType, acceptEncoding);

            Assert.That(compressed, Is.False, $"WHAT: {method} {path} answering {contentType} to Accept-Encoding '{acceptEncoding}' is sent compressed. WHY: an unsafe-method answer can echo a secret such as a new API key, the hub, logs and embed handshake answers can hold secrets next to attacker text, images and problem answers are not on the compressible list, and a client that did not ask for compression must get the plain bytes. HOW: answer no unless the method is GET or HEAD, the type is compressible, the client offers br or gzip and no excluded path segment prefix matches, ignoring case.");
        }

        [Test]
        public void Turning_compression_off_sends_a_safe_answer_as_it_is()
        {
            var rule = TheRuleWithSettings(new Dictionary<string, string?> { ["ResponseCompression:Enabled"] = "false" });

            var compressed = Compresses(rule, "GET", "/api/latest/teams", Json, "br");

            Assert.That(compressed, Is.False, "WHAT: with ResponseCompression:Enabled false a GET of JSON to Accept-Encoding br is sent compressed. WHY: an operator turns compression off with this switch, for example behind a proxy that compresses itself. HOW: answer no for every request when the switch is off.");
        }

        private static IResponseCompressionProvider TheRuleWithSettings(Dictionary<string, string?> settings)
        {
            var lighthouse = typeof(Program).Assembly;
            var ruleType = lighthouse.GetType(TheRuleName);
            var switchType = lighthouse.GetType(TheSwitchName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleType, Is.Not.Null, $"WHAT: Lighthouse has no compression rule {TheRuleName}. WHY: without it every answer goes out uncompressed, or a default rule compresses answers that carry secrets. HOW: add the rule as a ResponseCompressionProvider that decides which answers may be compressed.");
                Assert.That(switchType, Is.Not.Null, $"WHAT: Lighthouse has no compression switch {TheSwitchName}. WHY: an operator must be able to turn compression off with ResponseCompression:Enabled. HOW: add the configuration section, enabled when absent.");
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var theSwitch = configuration.GetSection("ResponseCompression").Get(switchType!) ?? Activator.CreateInstance(switchType!)!;

            var services = new ServiceCollection();
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddSingleton(switchType!, theSwitch);

            using var provider = services.BuildServiceProvider();
            return (IResponseCompressionProvider)ActivatorUtilities.CreateInstance(provider, ruleType!);
        }

        private static bool Compresses(IResponseCompressionProvider rule, string method, string path, string contentType, string? acceptEncoding)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Path = path;
            if (acceptEncoding is not null)
            {
                context.Request.Headers.AcceptEncoding = acceptEncoding;
            }

            context.Response.ContentType = contentType;

            return rule.CheckRequestAcceptsCompression(context)
                && rule.ShouldCompressResponse(context)
                && rule.GetCompressionProvider(context) is not null;
        }
    }
}
