using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

        private const string CheckedConnection = "Contoso Board";

        private const string OnTheKeyInForce = "PersonalAccessToken";

        private const string OnThePublishedKey = "ClientSecret";

        private const string NobodyCanRead = "ApiToken";

        private const string InTheOldFormat = "LegacyToken";

        private static readonly string[] EveryCredentialSeeded =
        [
            "pat-on-the-key-in-force",
            "secret-on-the-published-key",
            "token-nobody-can-read",
            "token-in-the-old-format",
        ];

        // Pinned rather than banned by substring. The payload used to carry nothing about stored secrets at
        // all; it now carries exactly one count, and pinning the whole set is what makes a second one an
        // explicit decision rather than something that arrives unnoticed.
        private static readonly string[] EverythingTheKeyStatePayloadCarries =
        [
            "custody",
            "canMint",
            "activeKeyId",
            "keyIds",
            "keyStorePath",
            "legacyDefaultPresent",
            "secretsUnderPublishedKey",
            "allowsStartWithUnreadableSecrets",
        ];

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private WebApplicationFactory<Program> factory = null!;

        private TestWebApplicationFactory<Program> ownKeyRootFactory = null!;

        private WebApplicationFactory<Program> ownKeyFactory = null!;

        private TestWebApplicationFactory<Program> rotatableRootFactory = null!;

        private WebApplicationFactory<Program> rotatableFactory = null!;

        private string rotatableKeyStore = null!;

        private TestWebApplicationFactory<Program> checkableRootFactory = null!;

        private WebApplicationFactory<Program> checkableFactory = null!;

        private EncryptionKey publishedKey = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);

            var ownKeyRing = ARingThisInstanceMadeForItself();
            ownKeyRootFactory = new TestWebApplicationFactory<Program>();
            ownKeyFactory = AHostRunningOn(ownKeyRootFactory, ownKeyRing, ReportedKeyStore);

            rotatableKeyStore = Directory.CreateTempSubdirectory("EncryptionControllerRotation_").FullName;
            rotatableRootFactory = new TestWebApplicationFactory<Program>();
            rotatableFactory = AHostThatCanMakeItsOwnKey(rotatableRootFactory, rotatableKeyStore);

            var checkableRing = ARingThisInstanceMadeForItself().WithLegacyDefault();
            checkableRing.TryGet(LegacyDefaultEncryptionKey.Id, out var published);
            publishedKey = published!;

            checkableRootFactory = new TestWebApplicationFactory<Program>();
            checkableFactory = AHostRunningOn(
                checkableRootFactory,
                checkableRing,
                Directory.CreateTempSubdirectory("EncryptionControllerCheck_").FullName,
                allowsStartWithUnreadableSecrets: true);

            // Migrations are skipped in the test environment, and every route on this controller now reads
            // the tables holding stored secrets - the key state included, because it counts how many are
            // still on the published key.
            WithSecretsToWalk(factory);
            WithSecretsToWalk(ownKeyFactory);
            WithSecretsToWalk(rotatableFactory);
            WithSecretsToWalk(checkableFactory);

            SeedOneOfEachStateOn(checkableFactory, checkableRing);
            SeedOneValueInTheOldFormatOn(ownKeyFactory, ownKeyRing.ActiveKey);
        }

        private static void WithSecretsToWalk(WebApplicationFactory<Program> host)
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.Migrate();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            checkableFactory.Dispose();
            checkableRootFactory.Dispose();
            rotatableFactory.Dispose();
            rotatableRootFactory.Dispose();
            ownKeyFactory.Dispose();
            ownKeyRootFactory.Dispose();
            factory.Dispose();
            rootFactory.Dispose();

            Directory.Delete(rotatableKeyStore, recursive: true);
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
                Assert.That(payload.PropertyNames, Is.EquivalentTo(EverythingTheKeyStatePayloadCarries),
                    "how many stored credentials are still on the published key is a different question from whether "
                    + "the key is held, and the two travel as separate properties so that an operator cannot read a "
                    + $"count of keys as a count of secrets; response was: {payload.Raw}");
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
        public async Task RotatingWhereTheKeyWasSuppliedToThisInstance_IsRefused_AndTheKeysHeldAreUnchanged()
        {
            using var client = factory.CreateClient().AsSystemAdmin();

            var before = IdsOn(RingOf(factory));
            using var response = await client.PostAsync(LatestRoute + "/rotate", content: null);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), body);
                Assert.That(body, Does.Contain("cannot make a new encryption key"),
                    "a refusal that does not say what was refused reads as an outage");
                Assert.That(body, Does.Contain("belongs to"),
                    "an administrator turned down without being told who owns the key has nothing to act on");
                Assert.That(IdsOn(RingOf(factory)), Is.EqualTo(before).AsCollection);
            }
        }

        [Test]
        public async Task RotatingWhereTheInstanceOwnsItsKey_MakesANewOneAndSaysWhatItMoved()
        {
            using var client = rotatableFactory.CreateClient().AsSystemAdmin();

            var before = RingOf(rotatableFactory).ActiveKey.Id;
            using var response = await client.PostAsync(LatestRoute + "/rotate", content: null);
            var payload = await ReadJsonAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), payload.Raw);
                Assert.That(payload.String("activeKeyId"), Is.Not.EqualTo(before));
                Assert.That(RingOf(rotatableFactory).TryGet(before, out _), Is.True,
                    "the key that was in force is retired, not discarded, so nothing already stored under it becomes unreadable");
                Assert.That(payload.PropertyNames, Does.Contain("movedCount").And.Contain("unreadableCount").And.Contain("byConnection"));
                AssertNoMaterialOf(RingOf(rotatableFactory), payload.Raw);
            }
        }

        [Test]
        public async Task MovingTheSecretsOntoTheKeyInForce_IsOfferedWhoeverOwnsTheKey()
        {
            using var suppliedClient = factory.CreateClient().AsSystemAdmin();
            using var ownKeyClient = rotatableFactory.CreateClient().AsSystemAdmin();

            using var supplied = await suppliedClient.PostAsync(LatestRoute + "/reencrypt", content: null);
            using var ownKey = await ownKeyClient.PostAsync(LatestRoute + "/reencrypt", content: null);

            var suppliedPayload = await ReadJsonAsync(supplied);
            var ownKeyPayload = await ReadJsonAsync(ownKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(supplied.StatusCode, Is.EqualTo(HttpStatusCode.OK), suppliedPayload.Raw);
                Assert.That(ownKey.StatusCode, Is.EqualTo(HttpStatusCode.OK), ownKeyPayload.Raw);
                Assert.That(suppliedPayload.String("activeKeyId"), Is.EqualTo(RingOf(factory).ActiveKey.Id),
                    "there is nothing to make here, only somewhere to move to");
                AssertNoMaterialOf(RingOf(factory), suppliedPayload.Raw);
                AssertNoMaterialOf(RingOf(rotatableFactory), ownKeyPayload.Raw);
            }
        }

        [Test]
        public async Task NeitherRotatingNorMoving_AnswersAnybodyWhoIsNotASystemAdministrator()
        {
            using var viewerClient = factory.CreateClient().AsViewer();

            using var rotate = await viewerClient.PostAsync(LatestRoute + "/rotate", content: null);
            using var reencrypt = await viewerClient.PostAsync(LatestRoute + "/reencrypt", content: null);

            var refusals = new[]
            {
                (await rotate.Content.ReadAsStringAsync()).ToLowerInvariant(),
                (await reencrypt.Content.ReadAsStringAsync()).ToLowerInvariant(),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rotate.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), refusals[0]);
                Assert.That(reencrypt.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), refusals[1]);

                foreach (var refusal in refusals)
                {
                    foreach (var word in WordsThatWouldNameTheKeyOrItsCustody)
                    {
                        Assert.That(refusal, Does.Not.Contain(word), $"the refusal names '{word}'");
                    }
                }
            }
        }

        [Test]
        public async Task AnEmbedSessionPrincipal_CanNeitherRotateNorMoveAnything()
        {
            using var embedHost = new ViewerEmbedTestHost();
            embedHost.SeedRbacFixture();

            var embedCookie = await embedHost.EstablishEmbedCookieAsync(ViewerEmbedTestHost.UnprovisionedViewerSubject);

            using var rotate = await ViewerEmbedTestHost.PostAsViewerAsync(
                embedHost.AuthEnabled, LatestRoute + "/rotate", embedCookie: embedCookie);
            using var reencrypt = await ViewerEmbedTestHost.PostAsViewerAsync(
                embedHost.AuthEnabled, LatestRoute + "/reencrypt", embedCookie: embedCookie);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rotate.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(reencrypt.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public async Task ASystemAdministrator_IsToldWhatEveryStoredSecretIsOn_AndWhatOwnsIt()
        {
            using var client = checkableFactory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute + "/secrets");
            var payload = await ReadJsonAsync(response);

            var secrets = payload.Objects("secrets");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), payload.Raw);
                Assert.That(secrets, Has.Count.EqualTo(4), payload.Raw);
                Assert.That(secrets.Select(secret => secret.GetProperty("connectionName").GetString()),
                    Has.All.EqualTo(CheckedConnection));
                Assert.That(secrets.Select(secret => secret.GetProperty("field").GetString()),
                    Is.EquivalentTo(new[] { OnTheKeyInForce, OnThePublishedKey, NobodyCanRead, InTheOldFormat }),
                    "a value that is not a secret is not a stored secret, so it is not something to check");
                Assert.That(payload.Int("onActiveKeyCount"), Is.EqualTo(1));
                Assert.That(payload.Int("onRetiredKeyCount"), Is.EqualTo(2),
                    "a value in the format this version replaced is still readable, and it is on an earlier key rather than broken");
                Assert.That(payload.Int("unreadableCount"), Is.EqualTo(1));
                Assert.That(payload.Int("movedCount"), Is.Zero, "a check moves nothing");
                Assert.That(
                    secrets.Single(secret => secret.GetProperty("field").GetString() == InTheOldFormat)
                        .GetProperty("state").GetString(),
                    Is.EqualTo(nameof(SecretState.LegacyCbc)));
            }
        }

        [Test]
        public async Task TheCheck_NamesTheConnectionAndFieldOfAnythingNobodyCanRead_AndCarriesNoCredential()
        {
            using var client = checkableFactory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute + "/secrets");
            var payload = await ReadJsonAsync(response);

            var unreadable = payload.Objects("secrets")
                .Single(secret => secret.GetProperty("state").GetString() == nameof(SecretState.Unreadable));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unreadable.GetProperty("connectionName").GetString(), Is.EqualTo(CheckedConnection));
                Assert.That(unreadable.GetProperty("field").GetString(), Is.EqualTo(NobodyCanRead));

                foreach (var credential in EveryCredentialSeeded)
                {
                    Assert.That(payload.Raw, Does.Not.Contain(credential),
                        "the report travels to a browser; a decrypted credential in it moves every secret somewhere nobody is guarding");
                }

                foreach (var stored in StoredValuesOn(checkableFactory))
                {
                    Assert.That(payload.Raw, Does.Not.Contain(stored), "a stored value is in the report");
                }

                AssertNoMaterialOf(RingOf(checkableFactory), payload.Raw);
            }
        }

        [Test]
        public async Task TheCheck_AnswersNobodyWhoIsNotASystemAdministrator()
        {
            using var viewerClient = checkableFactory.CreateClient().AsViewer();

            using var response = await viewerClient.GetAsync(LatestRoute + "/secrets");
            var refusal = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), refusal);

                foreach (var word in WordsThatWouldNameTheKeyOrItsCustody)
                {
                    Assert.That(refusal, Does.Not.Contain(word), $"the refusal names '{word}'");
                }
            }
        }

        /// <summary>
        /// The number an operator who has just upgraded needs, and it is on the state payload rather than
        /// behind the check because somebody who knew to press the check did not need telling. The values
        /// that could be on that key are narrowed in the database first, so an instance that has moved
        /// everything - the one that opens this page most often - decrypts nothing to be told so.
        /// </summary>
        [Test]
        public async Task ThePayload_SaysHowManySecretsAreStillOnTheKeyPublishedWithTheProduct()
        {
            using var checkableClient = checkableFactory.CreateClient().AsSystemAdmin();
            using var freshClient = factory.CreateClient().AsSystemAdmin();

            using var checkable = await checkableClient.GetAsync(LatestRoute);
            using var fresh = await freshClient.GetAsync(LatestRoute);

            var withSecrets = await ReadJsonAsync(checkable);
            var withNone = await ReadJsonAsync(fresh);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withSecrets.Int("secretsUnderPublishedKey"), Is.EqualTo(2), withSecrets.Raw);
                Assert.That(withNone.Int("secretsUnderPublishedKey"), Is.Zero,
                    "an install holding nothing under that key is never told to fix a problem it does not have");
            }
        }

        /// <summary>
        /// The install that did the right thing before this release: a key of its own, set under the
        /// setting this release retired. Its stored values carry no envelope, which is the same thing the
        /// default install's values look like — and telling this operator their credentials are public is
        /// both false and the kind of false that makes them stop believing the panel.
        /// </summary>
        [Test]
        public async Task ThePayload_DoesNotCallACredentialPublicForTheShapeItWasStoredIn()
        {
            using var client = ownKeyFactory.CreateClient().AsSystemAdmin();

            using var response = await client.GetAsync(LatestRoute);
            var payload = await ReadJsonAsync(response);

            Assert.That(payload.Int("secretsUnderPublishedKey"), Is.Zero,
                "the published key has never been able to read that value, so no number of shapes makes it public");
        }

        /// <summary>
        /// An instance started past the refusal looks entirely healthy from every other angle. The startup
        /// line says so, but a standalone operator has no console to read it in, and whoever finds the
        /// setting still in force months later is rarely the person who set it — so the settings page has
        /// to say it too, and say it every time.
        /// </summary>
        [Test]
        public async Task ThePayload_SaysWhenTheInstanceWasStartedPastTheRefusal()
        {
            using var pastTheRefusal = checkableFactory.CreateClient().AsSystemAdmin();
            using var ordinary = factory.CreateClient().AsSystemAdmin();

            using var startedPastIt = await pastTheRefusal.GetAsync(LatestRoute);
            using var startedNormally = await ordinary.GetAsync(LatestRoute);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((await ReadJsonAsync(startedPastIt)).Bool("allowsStartWithUnreadableSecrets"), Is.True);
                Assert.That((await ReadJsonAsync(startedNormally)).Bool("allowsStartWithUnreadableSecrets"), Is.False,
                    "an instance that never needed the switch is not taught to worry about a hatch it never opened");
            }
        }

        [Test]
        public async Task TheCheckRoute_AnswersOnBothTheVersionedAndTheLatestPath()
        {
            using var client = checkableFactory.CreateClient().AsSystemAdmin();

            using var versioned = await client.GetAsync(VersionedRoute + "/secrets");
            using var latest = await client.GetAsync(LatestRoute + "/secrets");

            var versionedBody = await versioned.Content.ReadAsStringAsync();
            var latestBody = await latest.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(versioned.StatusCode, Is.EqualTo(HttpStatusCode.OK), versionedBody);
                Assert.That(latestBody, Is.EqualTo(versionedBody));
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
            string keyStoreDirectory,
            bool allowsStartWithUnreadableSecrets = false)
        {
            var keyStoreSetting = new Dictionary<string, string?>
            {
                ["Encryption:KeyStorePath"] = keyStoreDirectory,
                [EncryptionKeyRingBootstrapper.StartAnywaySettingKey] = allowsStartWithUnreadableSecrets ? "true" : null,
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

        // The one host in this file that can actually rotate: it holds a key of its own and is given the
        // thing that makes keys, pointed at a real directory. Every other host here is handed its key
        // through configuration, which is exactly the custody that must be refused.
        private static WebApplicationFactory<Program> AHostThatCanMakeItsOwnKey(
            TestWebApplicationFactory<Program> root,
            string keyStoreDirectory)
        {
            return AHostRunningOn(root, ARingThisInstanceMadeForItself(), keyStoreDirectory)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IKeyRingMinter>();
                    services.AddSingleton<IKeyRingMinter>(new GeneratedKeyRingMinter(
                        keyStoreDirectory, new PhysicalKeyStoreFileSystem(), TimeProvider.System));
                }));
        }

        // One stored secret in each of the four states the check has to tell apart. The values are written
        // into the columns after the save rather than through it, because a save encrypts anything not
        // already an envelope - which would turn the value in the old format into one on the key in force
        // before the check ever saw it.
        private void SeedOneOfEachStateOn(WebApplicationFactory<Program> host, EncryptionKeyRing ring)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = CheckedConnection,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var nobodyHolds = new EncryptionKey("k-lost-forever", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

            var stored = new Dictionary<string, string>
            {
                [OnTheKeyInForce] = Under(ring.ActiveKey, EveryCredentialSeeded[0]),
                [OnThePublishedKey] = Under(publishedKey, EveryCredentialSeeded[1]),
                [NobodyCanRead] = Under(nobodyHolds, EveryCredentialSeeded[2]),
                [InTheOldFormat] = InTheFormatThisVersionReplaced(EveryCredentialSeeded[3], publishedKey),
            };

            foreach (var field in stored.Keys)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = field, Value = "placeholder", IsSecret = true });
            }

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = "Url",
                Value = "https://dev.azure.com/contoso",
                IsSecret = false,
            });

            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            foreach (var (field, storedValue) in stored)
            {
                context.Set<WorkTrackingSystemConnectionOption>()
                    .Where(option => option.WorkTrackingSystemConnectionId == connection.Id && option.Key == field)
                    .ExecuteUpdate(set => set.SetProperty(option => option.Value, storedValue));
            }
        }

        // One credential in the shape every install written before this release holds, encrypted under a
        // key that install chose for itself. Written into the column after the save, for the same reason
        // the four-state seed is: a save encrypts anything that is not already an envelope.
        private static void SeedOneValueInTheOldFormatOn(WebApplicationFactory<Program> host, EncryptionKey ownKey)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Fabrikam Board",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = InTheOldFormat,
                Value = "placeholder",
                IsSecret = true,
            });

            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            var storedValue = InTheFormatThisVersionReplaced("never-was-public", ownKey);

            context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.WorkTrackingSystemConnectionId == connection.Id)
                .ExecuteUpdate(set => set.SetProperty(option => option.Value, storedValue));
        }

        private static string Under(EncryptionKey key, string credential)
        {
            return SecretEnvelope.Protect(credential, key.Id, key.Material.Span).Format();
        }

        // What every install written before this release holds: AES-CBC, an initialisation vector in front,
        // and no key id anywhere on the value.
        private static string InTheFormatThisVersionReplaced(string credential, EncryptionKey key)
        {
            using var aes = Aes.Create();
            aes.Key = key.Material.ToArray();

            var iv = RandomNumberGenerator.GetBytes(16);
            var cipherText = aes.EncryptCbc(System.Text.Encoding.UTF8.GetBytes(credential), iv);

            return Convert.ToBase64String([.. iv, .. cipherText]);
        }

        private static List<string> StoredValuesOn(WebApplicationFactory<Program> host)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return [.. context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret)
                .Select(option => option.Value)];
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

            public int Int(string propertyName) => properties[propertyName].GetInt32();

            public List<JsonElement> Objects(string propertyName) => [.. properties[propertyName].EnumerateArray()];

            public List<string> Strings(string propertyName) =>
            [
                .. properties[propertyName].EnumerateArray().Select(entry => entry.GetString() ?? string.Empty)
            ];
        }
    }
}
