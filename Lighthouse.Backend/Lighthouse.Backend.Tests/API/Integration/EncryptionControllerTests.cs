using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// The one route this slice owns, and the two things it must never do: hand out key material, and
    /// answer anyone who is not a System Administrator. The embed-session case is the reason this lives
    /// on a controller of its own - a framed viewer is signed in, so being signed in cannot be the bar.
    /// </summary>
    [TestFixture]
    public class EncryptionControllerTests
    {
        private const string VersionedRoute = "/api/v1/encryption";

        private const string LatestRoute = "/api/latest/encryption";

        private const string SystemInfoRoute = "/api/latest/systeminfo";

        private const string MintedKeyId = "k-2026-08-15-01";

        private const string OlderKeyId = "k-2025-11-02-01";

        // The property set the system information response carried before key custody existed. An
        // addition here is a disclosure to every signed-in caller, including a framed viewer.
        private static readonly string[] EverythingSystemInfoDiscloses =
        [
            "os",
            "runtime",
            "architecture",
            "processId",
            "databaseProvider",
            "databaseConnection",
            "logPath",
            "authenticationEnabled",
            "authorizationEnabled",
            "emergencyAdminSubjects",
            "baseUrl",
            "installTimestamp",
        ];

        private static readonly string[] WordsThatWouldNameTheKeyOrItsCustody =
        [
            "encryption",
            "keystore",
            "keyring",
            "custody",
            "canmint",
            "activekey",
            "legacydefault",
            "k-legacy-default",
            "generatedforthisinstance",
            "suppliedbyconfiguration",
            "suppliedbyexternalsecret",
            "nodurablestore",
        ];

        private static readonly string ReportedKeyStore = Path.Combine(Path.GetTempPath(), "lighthouse-key-store-under-test");

        private static readonly string[] ExpectedOwnKeyIds = [MintedKeyId, OlderKeyId];

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private WebApplicationFactory<Program> factory = null!;

        private TestWebApplicationFactory<Program> ownKeyRootFactory = null!;

        private WebApplicationFactory<Program> ownKeyFactory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);

            ownKeyRootFactory = new TestWebApplicationFactory<Program>();
            ownKeyFactory = AHostRunningOn(ownKeyRootFactory, ARingThisInstanceMadeForItself(), ReportedKeyStore);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ownKeyFactory.Dispose();
            ownKeyRootFactory.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task ASystemAdministrator_LearnsWhichKeyIsInForceWhichAreHeldAndWhereTheyAreKept()
        {
            using var client = factory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute);
            var payload = await ReadJsonAsync(response);
            var ring = RingOf(factory);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), payload.Raw);
                Assert.That(payload.String("custody"), Is.EqualTo(ring.Custody.ToString()));
                Assert.That(payload.String("activeKeyId"), Is.EqualTo(ring.ActiveKey.Id));
                Assert.That(payload.Strings("keyIds"), Is.EqualTo(IdsOn(ring)).AsCollection);
                Assert.That(payload.String("keyStorePath"), Is.Not.Empty,
                    "an operator who cannot see where the key is kept cannot back it up");
            }
        }

        /// <summary>
        /// Read against two instances that differ in where their key came from. One reading on one
        /// instance would pass equally well against a hard-coded answer.
        /// </summary>
        [Test]
        public async Task ThePayload_SaysWhetherThisInstanceIsAbleToMakeANewKeyAtAll()
        {
            using var suppliedClient = factory.CreateClient().AsSystemAdmin();
            using var ownKeyClient = ownKeyFactory.CreateClient().AsSystemAdmin();

            using var suppliedResponse = await suppliedClient.GetAsync(LatestRoute);
            using var ownKeyResponse = await ownKeyClient.GetAsync(LatestRoute);

            var supplied = await ReadJsonAsync(suppliedResponse);
            var ownKey = await ReadJsonAsync(ownKeyResponse);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(supplied.String("custody"), Is.EqualTo(nameof(KeyCustody.SuppliedByConfiguration)), supplied.Raw);
                Assert.That(supplied.Bool("canMint"), Is.False,
                    "a key the operator supplied wins the resolution order again on the next start, so a key "
                    + "minted over it would take every secret written under it out of reach");

                Assert.That(ownKey.String("custody"), Is.EqualTo(nameof(KeyCustody.GeneratedForThisInstance)), ownKey.Raw);
                Assert.That(ownKey.Bool("canMint"), Is.True,
                    "an instance that wrote its own key can write another and still find it after a restart");
                Assert.That(ownKey.String("activeKeyId"), Is.EqualTo(MintedKeyId));
                Assert.That(ownKey.Strings("keyIds"), Is.EqualTo(ExpectedOwnKeyIds).AsCollection);
                Assert.That(ownKey.String("keyStorePath"), Is.EqualTo(ReportedKeyStore),
                    "the path is resolved from the key-store settings rather than reported from somewhere of its own");
            }
        }

        [Test]
        public async Task ThePayload_SaysWhetherTheKeyPublishedWithTheProductIsStillHeld()
        {
            using var client = factory.CreateClient().AsSystemAdmin();
            using var ownKeyClient = ownKeyFactory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute);
            using var ownKeyResponse = await ownKeyClient.GetAsync(LatestRoute);

            var payload = await ReadJsonAsync(response);
            var ownKey = await ReadJsonAsync(ownKeyResponse);
            var keyIds = payload.Strings("keyIds");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.Bool("legacyDefaultPresent"), Is.True.And.EqualTo(keyIds.Contains(LegacyDefaultEncryptionKey.Id)),
                    "an upgraded instance keeps the published key behind its own so that what it already stored stays readable");
                Assert.That(ownKey.Bool("legacyDefaultPresent"), Is.False,
                    "this ring does not hold the published key, and the flag is a statement about the ring");
                Assert.That(payload.PropertyNames, Has.None.Matches<string>(name => name.Contains("ecret", StringComparison.OrdinalIgnoreCase)),
                    "how many stored credentials are still on the old key is a different question with a different answer; "
                    + "answering it here would make an operator read a count of keys as a count of secrets");
            }
        }

        [Test]
        public async Task ThePayload_CarriesNoKeyMaterialInAnyEncodingOrFragment()
        {
            using var client = factory.CreateClient().AsSystemAdmin();
            using var ownKeyClient = ownKeyFactory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute);
            using var ownKeyResponse = await ownKeyClient.GetAsync(LatestRoute);

            var raw = await response.Content.ReadAsStringAsync();
            var ownKeyRaw = await ownKeyResponse.Content.ReadAsStringAsync();

            var readings = new List<(string Raw, EncryptionKeyRing Ring)>
            {
                (raw, RingOf(factory)),
                (ownKeyRaw, RingOf(ownKeyFactory)),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), raw);
                Assert.That(ownKeyResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), ownKeyRaw);

                foreach (var reading in readings)
                {
                    AssertNoMaterialOf(reading.Ring, reading.Raw);
                }
            }
        }

        [Test]
        public async Task ASignedInNonAdministrator_IsRefused()
        {
            using var client = factory.CreateClient().AsViewer();

            using var response = await client.GetAsync(LatestRoute);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
        }

        /// <summary>
        /// The load-bearing one. A viewer inside an embedded frame holds a real Lighthouse session, so a
        /// guard that asks only for a signed-in caller would let them read the instance's key state.
        /// </summary>
        [Test]
        public async Task AnEmbedSessionPrincipal_IsRefused()
        {
            using var embedHost = new ViewerEmbedTestHost();
            embedHost.SeedRbacFixture();

            var embedCookie = await embedHost.EstablishEmbedCookieAsync(ViewerEmbedTestHost.UnprovisionedViewerSubject);

            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                embedHost.AuthEnabled, LatestRoute, embedCookie: embedCookie);
            var body = await response.Content.ReadAsStringAsync();

            var lowered = body.ToLowerInvariant();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    $"a framed viewer is signed in, and being signed in must not be enough to read key state; body was: {body}");

                foreach (var word in WordsThatWouldNameTheKeyOrItsCustody)
                {
                    Assert.That(lowered, Does.Not.Contain(word),
                        $"the refusal names '{word}', describing what it is refusing access to");
                }
            }
        }

        [Test]
        public async Task NeitherRefusal_NamesTheKeySourceTheActiveKeyOrTheKeyStoreLocation()
        {
            using var adminClient = factory.CreateClient().AsSystemAdmin();
            using var viewerClient = factory.CreateClient().AsViewer();

            using var granted = await adminClient.GetAsync(LatestRoute);
            using var refused = await viewerClient.GetAsync(LatestRoute);

            var payload = await ReadJsonAsync(granted);
            var refusal = await refused.Content.ReadAsStringAsync();
            var lowered = refusal.ToLowerInvariant();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lowered, Does.Not.Contain(payload.String("activeKeyId").ToLowerInvariant()));
                Assert.That(lowered, Does.Not.Contain(payload.String("keyStorePath").ToLowerInvariant()));
                Assert.That(lowered, Does.Not.Contain(payload.String("custody").ToLowerInvariant()));

                foreach (var word in WordsThatWouldNameTheKeyOrItsCustody)
                {
                    Assert.That(lowered, Does.Not.Contain(word), $"the refusal names '{word}'");
                }
            }
        }

        [Test]
        public async Task SystemInformation_CarriesExactlyThePropertySetItCarriedBeforeKeyCustodyExisted()
        {
            using var client = factory.CreateClient().AsViewer();

            using var response = await client.GetAsync(SystemInfoRoute);
            var payload = await ReadJsonAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), payload.Raw);
                Assert.That(payload.PropertyNames, Is.EquivalentTo(EverythingSystemInfoDiscloses),
                    $"every caller who can sign in reads this, framed viewers included; response was: {payload.Raw}");
            }
        }

        [Test]
        public async Task SystemInformation_NamesNoKeyNoKeySourceAndNoKeyStoreLocation()
        {
            using var adminClient = factory.CreateClient().AsSystemAdmin();
            using var viewerClient = factory.CreateClient().AsViewer();

            using var keyState = await adminClient.GetAsync(LatestRoute);
            using var systemInfo = await viewerClient.GetAsync(SystemInfoRoute);

            var payload = await ReadJsonAsync(keyState);
            var lowered = (await systemInfo.Content.ReadAsStringAsync()).ToLowerInvariant();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lowered, Does.Not.Contain(payload.String("activeKeyId").ToLowerInvariant()));
                Assert.That(lowered, Does.Not.Contain(payload.String("keyStorePath").ToLowerInvariant()));

                foreach (var word in WordsThatWouldNameTheKeyOrItsCustody)
                {
                    Assert.That(lowered, Does.Not.Contain(word), $"system information names '{word}'");
                }
            }
        }

        [Test]
        public async Task TheRoute_AnswersOnBothTheVersionedAndTheLatestPath()
        {
            using var client = factory.CreateClient().AsSystemAdmin();

            using var versioned = await client.GetAsync(VersionedRoute);
            using var latest = await client.GetAsync(LatestRoute);

            var versionedBody = await versioned.Content.ReadAsStringAsync();
            var latestBody = await latest.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(versioned.StatusCode, Is.EqualTo(HttpStatusCode.OK), versionedBody);
                Assert.That(latest.StatusCode, Is.EqualTo(HttpStatusCode.OK), latestBody);
                Assert.That(versionedBody, Is.EqualTo(latestBody));
            }
        }

        // Every test host in this project is handed its key through configuration, so the custody that
        // decides whether minting is possible cannot be changed by settings alone. It is changed where the
        // controller actually reads it: the ring the application is holding.
        private static WebApplicationFactory<Program> AHostRunningOn(
            TestWebApplicationFactory<Program> root,
            EncryptionKeyRing ring,
            string keyStoreDirectory)
        {
            var keyStoreSetting = new Dictionary<string, string?>
            {
                ["Encryption:KeyStorePath"] = keyStoreDirectory,
            };

            return TestWebApplicationFactory<Program>.WithTestAuthentication(root)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(keyStoreSetting);
                    });

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IEncryptionKeyRingHolder>();
                        services.AddSingleton<IEncryptionKeyRingHolder>(new EncryptionKeyRingHolder(ring));
                    });
                });
        }

        private static EncryptionKeyRing ARingThisInstanceMadeForItself()
        {
            return new EncryptionKeyRing(
                KeyCustody.GeneratedForThisInstance,
                new EncryptionKey(MintedKeyId, RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength)),
                new EncryptionKey(OlderKeyId, RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength)));
        }

        private static void AssertNoMaterialOf(EncryptionKeyRing ring, string raw)
        {
            foreach (var key in KeysOn(ring))
            {
                var base64 = Convert.ToBase64String(key.Material.Span);

                Assert.That(raw, Does.Not.Contain(base64), $"the material of '{key.Id}' is in the response");
                Assert.That(raw, Does.Not.Contain(Convert.ToHexString(key.Material.Span)),
                    $"the material of '{key.Id}' is in the response, hex encoded");

                foreach (var fragment in FragmentsOf(base64))
                {
                    Assert.That(raw, Does.Not.Contain(fragment),
                        $"a fragment of the material of '{key.Id}' is in the response");
                }
            }
        }

        private static EncryptionKeyRing RingOf(WebApplicationFactory<Program> host)
        {
            return host.Services.GetRequiredService<IEncryptionKeyRingHolder>().Current;
        }

        private static List<string> IdsOn(EncryptionKeyRing ring)
        {
            return [.. KeysOn(ring).Select(key => key.Id)];
        }

        private static List<EncryptionKey> KeysOn(EncryptionKeyRing ring)
        {
            return [ring.ActiveKey, .. ring.RetiredKeys];
        }

        // Every window of eight characters, so a response that leaked half a key, or the same bytes split
        // across two properties, is caught as surely as one that leaked the whole thing.
        private static List<string> FragmentsOf(string encoded)
        {
            const int fragmentLength = 8;

            return [.. Enumerable
                .Range(0, Math.Max(0, encoded.Length - fragmentLength + 1))
                .Select(start => encoded.Substring(start, fragmentLength))];
        }

        private static async Task<JsonReading> ReadJsonAsync(HttpResponseMessage response)
        {
            return new JsonReading(await response.Content.ReadAsStringAsync());
        }

        private sealed class JsonReading
        {
            private readonly Dictionary<string, JsonElement> properties;

            public JsonReading(string raw)
            {
                Raw = raw;

                using var document = JsonDocument.Parse(raw);
                properties = document.RootElement.EnumerateObject()
                    .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            }

            public string Raw { get; }

            public List<string> PropertyNames => [.. properties.Keys];

            public string String(string propertyName) => properties[propertyName].GetString() ?? string.Empty;

            public bool Bool(string propertyName) => properties[propertyName].GetBoolean();

            public List<string> Strings(string propertyName) =>
            [
                .. properties[propertyName].EnumerateArray().Select(entry => entry.GetString() ?? string.Empty)
            ];
        }
    }
}
