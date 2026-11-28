using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lighthouse.Backend.Tests.Contracts
{
    /// <summary>
    /// What a browser gets on the wire from the real Lighthouse pipeline: built frontend files and safe API answers
    /// compressed when it offers br or gzip, the same bytes once decoded, the plain file when it offers nothing, the
    /// page shell still never cached, and no compressed answer to a request that can echo a secret.
    /// Which answers may be compressed is asserted row by row without a host; this pins the order and the headers.
    /// </summary>
    public class ResponseCompressionWireContractTest : WireContractTest
    {
        private const string Script = "/assets/app.js";

        private const string CurrentVersion = "/api/latest/version/current";

        private readonly string webRoot = Path.Combine(Path.GetTempPath(), $"lighthouse-compression-{Guid.NewGuid():N}");

        private static readonly string[] OnlyBrotli = ["br"];

        private byte[] scriptBytes = [];

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
        {
            Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
            File.WriteAllText(Path.Combine(webRoot, "index.html"), "<!doctype html><html lang=\"en\"><body><div id=\"root\"></div><script type=\"module\" src=\"/assets/app.js\"></script></body></html>");

            var script = new StringBuilder();
            for (var line = 0; line < 120; line++)
            {
                script.Append("export function feature").Append(line).Append("(items) { return items.filter((item) => item.state === \"Doing\").map((item) => item.name); }\n");
            }

            scriptBytes = Encoding.UTF8.GetBytes(script.ToString());
            File.WriteAllBytes(Path.Combine(webRoot, "assets", "app.js"), scriptBytes);

            return unchanged.WithWebHostBuilder(builder => builder.UseWebRoot(webRoot));
        }

        [OneTimeTearDown]
        public void RemoveTheWebRoot()
        {
            if (Directory.Exists(webRoot))
            {
                Directory.Delete(webRoot, true);
            }
        }

        [TestCase("br")]
        [TestCase("gzip")]
        public async Task A_browser_that_accepts_compression_gets_the_script_compressed_and_the_same_file_once_decoded(string encoding)
        {
            using var response = await Get(Script, encoding);
            var decoded = await Decoded(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {Script} answered {(int)response.StatusCode}. WHY: the built frontend files are served from the web root. HOW: keep the static file middleware serving the web root.");
                Assert.That(response.Content.Headers.ContentEncoding, Is.EquivalentTo((string[])[encoding]), $"WHAT: the script came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}] to Accept-Encoding {encoding}. WHY: uncompressed JavaScript is most of a slow first load. HOW: compress static files with Brotli and Gzip, placing response compression before the static files.");
                Assert.That(response.Headers.Vary, Does.Contain("Accept-Encoding"), "WHAT: the compressed script has no Vary Accept-Encoding. WHY: a cache in between must not hand the compressed copy to a client that cannot read it. HOW: let the compression middleware add Vary.");
                Assert.That(decoded, Is.EqualTo(scriptBytes), "WHAT: the decoded script differs from the file. WHY: the browser must run exactly the file that was built. HOW: compress the file's bytes without changing them.");
            }
        }

        [Test]
        public async Task A_browser_that_does_not_accept_compression_gets_the_plain_script()
        {
            using var response = await Get(Script, null);
            var body = await response.Content.ReadAsByteArrayAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Content.Headers.ContentEncoding, Is.Empty, $"WHAT: the script came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}] to a request without Accept-Encoding. WHY: a client that asked for nothing cannot decode it. HOW: compress only when the client offers br or gzip.");
                Assert.That(body, Is.EqualTo(scriptBytes), "WHAT: the plain script differs from the file. WHY: a browser without compression still needs working files. HOW: serve the file as it is.");
            }
        }

        [Test]
        public async Task The_page_shell_is_compressed_and_still_never_cached()
        {
            using var response = await Get("/", "br");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Content.Headers.ContentEncoding, Is.EquivalentTo(OnlyBrotli), $"WHAT: GET / came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}] to Accept-Encoding br. WHY: the page shell is part of every cold load. HOW: place response compression before the default files and static files.");
                Assert.That(response.Headers.CacheControl?.NoCache, Is.True, $"WHAT: GET / came with Cache-Control '{response.Headers.CacheControl}'. WHY: a cached shell keeps users on an old version. HOW: keep the static file options that mark index.html no-cache, no-store, must-revalidate.");
                Assert.That(response.Headers.CacheControl?.NoStore, Is.True, $"WHAT: GET / came with Cache-Control '{response.Headers.CacheControl}', without no-store. WHY: a stored shell keeps users on an old version. HOW: keep the index.html no-store rule.");
                Assert.That(response.Headers.CacheControl?.MustRevalidate, Is.True, $"WHAT: GET / came with Cache-Control '{response.Headers.CacheControl}', without must-revalidate. WHY: the shell must be checked on every load. HOW: keep the index.html must-revalidate rule.");
            }
        }

        [Test]
        public async Task A_safe_api_answer_is_compressed_with_brotli_when_the_browser_offers_gzip_and_br()
        {
            using var response = await Get(CurrentVersion, "gzip, br");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {CurrentVersion} answered {(int)response.StatusCode}. WHY: the version is answered to anyone. HOW: keep the route.");
                Assert.That(response.Content.Headers.ContentEncoding, Is.EquivalentTo(OnlyBrotli), $"WHAT: the API answer came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}] to Accept-Encoding gzip, br. WHY: safe API answers take fewer bytes compressed, and Brotli is the smaller one. HOW: compress API answers, with the Brotli provider registered before Gzip.");
                Assert.That(response.Headers.Vary, Does.Contain("Accept-Encoding"), "WHAT: the compressed API answer has no Vary Accept-Encoding. WHY: a cache in between must not hand it to a client that cannot read it. HOW: let the compression middleware add Vary.");
            }
        }

        [Test]
        public async Task A_safe_api_answer_to_a_client_that_asks_for_no_compression_is_plain()
        {
            using var response = await Get(CurrentVersion, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {CurrentVersion} answered {(int)response.StatusCode}. WHY: the command-line client reads the version from here. HOW: keep the route.");
                Assert.That(response.Content.Headers.ContentEncoding, Is.Empty, $"WHAT: the API answer came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}] to a request without Accept-Encoding. WHY: a client that asked for nothing cannot decode it. HOW: compress only when the client offers br or gzip.");
            }
        }

        [Test]
        public async Task A_new_api_key_is_never_sent_compressed()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/latest/apikeys")
            {
                Content = JsonContent.Create(new { name = "compression check", description = "a key whose answer echoes it" }),
            };
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "br");

            using var response = await Client.SendAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"WHAT: creating an API key answered {(int)response.StatusCode}. WHY: the answer must carry the new key for this check to mean anything. HOW: keep the API key creation route.");
                Assert.That(response.Content.Headers.ContentEncoding, Is.Empty, $"WHAT: the new API key came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}]. WHY: a compressed answer that holds a secret can be guessed from its size. HOW: never compress an answer to a POST, PUT, PATCH or DELETE.");
            }
        }

        [Test]
        public async Task The_update_hub_handshake_is_never_sent_compressed()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/updateNotificationHub/negotiate?negotiateVersion=1");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "br");

            using var response = await Client.SendAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: the hub handshake answered {(int)response.StatusCode}. WHY: the answer must carry the connection token for this check to mean anything. HOW: keep the hub mapped on its route.");
                Assert.That(response.Content.Headers.ContentEncoding, Is.Empty, $"WHAT: the hub handshake came with Content-Encoding [{string.Join(", ", response.Content.Headers.ContentEncoding)}]. WHY: its answer carries a connection token. HOW: exclude the hub route and everything under it from compression.");
            }
        }

        private async Task<HttpResponseMessage> Get(string route, string? acceptEncoding)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, route);
            if (acceptEncoding is not null)
            {
                request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
            }

            return await Client.SendAsync(request);
        }

        private static async Task<byte[]> Decoded(HttpResponseMessage response)
        {
            await using var body = await response.Content.ReadAsStreamAsync();
            await using Stream decoder = response.Content.Headers.ContentEncoding.FirstOrDefault() switch
            {
                "br" => new BrotliStream(body, CompressionMode.Decompress),
                "gzip" => new GZipStream(body, CompressionMode.Decompress),
                _ => body,
            };
            using var bytes = new MemoryStream();
            await decoder.CopyToAsync(bytes);
            return bytes.ToArray();
        }
    }
}
