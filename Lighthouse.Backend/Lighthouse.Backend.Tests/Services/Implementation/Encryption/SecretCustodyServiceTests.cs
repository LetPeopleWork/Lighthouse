using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Data.Common;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The only thing in this feature that writes a stored credential, so it is tested against a real
    /// database rather than an in-memory stand-in: the guarded write is the entire mechanism, and a
    /// simulated one would prove the simulation. The in-memory provider does not implement it at all.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class SecretCustodyServiceTests
    {
        private const string PersonalAccessToken = "PersonalAccessToken";

        private const string ClientSecret = "ClientSecret";

        private const string Contoso = "Contoso Board";

        private static readonly EncryptionKey OldKey = new("k-2025-11-02-01", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey NewKey = new("k-2026-08-16-01", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private static readonly EncryptionKey NewerKey = new("k-2026-08-16-02", Convert.FromBase64String("MTIzNDU2Nzg5MGFiY2RlZmdoaWpmb29iYXJiYXpxdXg="));

        private static readonly EncryptionKey KeyNobodyHolds = new("k-lost-forever", Convert.FromBase64String("bG9zdGtleWxvc3RrZXlsb3N0a2V5bG9zdGtleWxvc3Q="));

        private string databaseFile = null!;

        private ServiceProvider provider = null!;

        private CryptoService crypto = null!;

        private int connectionId;

        private int credentialId;

        private OneSecretPassAtATime oneAtATime = null!;

        private CountsEveryLookup lookups = null!;

        [SetUp]
        public async Task SetUp()
        {
            databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-custody-{Guid.NewGuid():N}.db");
            crypto = CryptoOver(NewKey, OldKey);
            oneAtATime = new OneSecretPassAtATime();
            lookups = new CountsEveryLookup();
            provider = BuildProvider($"Data Source={databaseFile}", crypto, lookups);

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            (connectionId, credentialId) = await SeedAsync(context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await provider.DisposeAsync();
            oneAtATime.Dispose();
            SqliteConnection.ClearAllPools();

            if (File.Exists(databaseFile))
            {
                File.Delete(databaseFile);
            }
        }

        [Test]
        public async Task ASecretUnderARetiredKey_IsMovedOntoTheKeyInForce_AndStillReadsAsTheSameCredential()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var report = await CustodyService().ReEncryptAsync();

            var stored = await StoredOptionAsync(PersonalAccessToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(1));
                Assert.That(crypto.Read(stored).KeyId, Is.EqualTo(NewKey.Id));
                Assert.That(crypto.Read(stored).PlainText, Is.EqualTo("contoso-pat"));
            }
        }

        [Test]
        public async Task ASecretAlreadyUnderTheKeyInForce_IsNotACandidate()
        {
            await StoreAsync(PersonalAccessToken, Under(NewKey, "contoso-pat"));
            var before = await StoredOptionAsync(PersonalAccessToken);

            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Is.Empty,
                    "asking the database for what is left to do is the whole of the resumability, so a finished instance has nothing to walk");
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        [Test]
        public async Task APassWhoseKeysAreReplacedWhileItRuns_LeavesEverySecretItMovedUnderOneKey()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            var report = await Rotating(holder, ReplacingTheRingAfter(0, cryptoService, holder, NewerKey, NewKey, OldKey), new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            var reader = new CryptoService(holder, NullLogger<CryptoService>.Instance);
            var pat = reader.Read(await StoredOptionAsync(PersonalAccessToken));
            var clientSecret = reader.Read(await StoredOptionAsync(ClientSecret));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pat.KeyId, Is.EqualTo(clientSecret.KeyId),
                    "a pass split across two keys is one an operator cannot reason about, whichever of the two they still hold");
                Assert.That(report.ActiveKeyId, Is.EqualTo(pat.KeyId),
                    "the key the report names is the key the credentials it names are actually under");
                Assert.That(report.KeysChangedWhileItRan, Is.True);
            }
        }

        [Test]
        public async Task KeysReplacedAndPutBackWhileAPassRuns_StrandNothingInTheWindow()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var held = new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey);
            var holder = new EncryptionKeyRingHolder(held);
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            // Replaced before the first credential is written and put back before the second, so what is
            // held at the end is what was held at the start and the end-of-pass comparison has nothing to
            // report. Nothing looks twice. A credential written inside that window under whatever was
            // briefly in force would sit on a key this instance never carries again - lost silently, by a
            // pass reporting a clean finish, which is the one outcome this slice exists to prevent.
            var replacedThenPutBack = new WriterThatGetsThereFirst(
                new WriterThatGetsThereFirst(
                    cryptoService,
                    () =>
                    {
                        holder.Replace(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewerKey, NewKey, OldKey));
                        return Task.CompletedTask;
                    },
                    0),
                () =>
                {
                    holder.Replace(held);
                    return Task.CompletedTask;
                },
                1);

            var report = await Rotating(holder, replacedThenPutBack, new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cryptoService.Read(await StoredOptionAsync(PersonalAccessToken)).PlainText, Is.EqualTo("contoso-pat"));
                Assert.That(cryptoService.Read(await StoredOptionAsync(ClientSecret)).PlainText, Is.EqualTo("contoso-secret"));
                Assert.That(report.UnreadableCount, Is.Zero);
            }
        }

        [Test]
        public async Task AKeyAddedBehindTheOneInForce_StillNamesWhatTheFirstLookAlreadyMoved()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            // A key added behind the one in force. What is held is not what was held, so the pass looks a
            // second time - but the key credentials are written under has not moved, so nothing it just
            // moved is a candidate again and the second look sees none of them. They are named once, by the
            // first look, and that is the only thing carrying them into what the operator reads.
            var report = await Rotating(
                    holder,
                    ReplacingTheRingAfter(0, cryptoService, holder, NewKey, OldKey, NewerKey),
                    new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.KeysChangedWhileItRan, Is.True);
                Assert.That(report.MovedCount, Is.EqualTo(2));
                Assert.That(
                    report.Secrets.Select(secret => secret.Field),
                    Is.EquivalentTo(new[] { PersonalAccessToken, ClientSecret }),
                    "each credential is named once - a second look that saw none of them must not drop them, and must not name them twice");
            }
        }

        [Test]
        public async Task WhenTheKeysChange_EachCredentialIsNamedOnceWithItsOwnOutcome()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            // The two looks overlap without matching. The credential that moved is on the key in force and
            // the second look does not ask about it again; the one nobody can read is still where it was, so
            // the second look finds it a second time. Both have to reach the operator, each once and each
            // with what actually happened to it - which is the whole job of reconciling the two looks.
            var report = await Rotating(
                    holder,
                    ReplacingTheRingAfter(0, cryptoService, holder, NewKey, OldKey, NewerKey),
                    new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Has.Count.EqualTo(2));
                Assert.That(report.MovedCount, Is.EqualTo(1));
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
                Assert.That(
                    report.Secrets.Single(secret => secret.Outcome == SecretMoveOutcome.Moved).Field,
                    Is.EqualTo(PersonalAccessToken));
                Assert.That(
                    report.Secrets.Single(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead).Field,
                    Is.EqualTo(ClientSecret));
            }
        }

        [Test]
        public async Task ACredentialThePassNeverLookedAt_OnAKeyThatHasGone_IsStillNamed()
        {
            // Already on the key in force, so the filter takes it off the list before anything is replaced.
            await StoreAsync(PersonalAccessToken, Under(NewKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            var report = await Rotating(holder, ReplacingTheRingAfter(0, cryptoService, holder, NewerKey), new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            var stranded = report.Secrets.Where(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stranded.Select(secret => secret.Field), Does.Contain(PersonalAccessToken),
                    "a credential the filter excluded against a key that has since gone is unreadable, and naming it is the only way an operator hears about it");
                Assert.That(report.UnreadableCount, Is.EqualTo(stranded.Count));
                Assert.That(report.KeysChangedWhileItRan, Is.True);
            }
        }

        [Test]
        public async Task APassWhoseKeysHeldStill_SaysNothingAboutThemHavingChanged()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.KeysChangedWhileItRan, Is.False);
                Assert.That(report.MovedCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task APassRunAgainAfterTheKeysChanged_FinishesItAndSaysNothingAboutAChange()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            var disturbed = await Rotating(holder, ReplacingTheRingAfter(0, cryptoService, holder, NewerKey, NewKey, OldKey), new AMinterThatMints(NewerKey))
                .ReEncryptAsync();

            var again = await Rotating(holder, cryptoService, new AMinterThatMints(NewerKey)).ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disturbed.KeysChangedWhileItRan, Is.True);
                Assert.That(again.KeysChangedWhileItRan, Is.False);
                Assert.That(again.Secrets, Is.Empty,
                    "advice to run it again is only honest if running it again leaves nothing behind");
                Assert.That(cryptoService.Read(await StoredOptionAsync(PersonalAccessToken)).KeyId, Is.EqualTo(NewerKey.Id));
                Assert.That(cryptoService.Read(await StoredOptionAsync(ClientSecret)).KeyId, Is.EqualTo(NewerKey.Id));
            }
        }

        [Test]
        public async Task ACheckThatOnlyLooks_SaysTheKeysChangedAndNamesOneItReadAgainst()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, NewKey, OldKey));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            var report = await Rotating(holder, new ReaderThatReplacesTheRing(cryptoService, holder, NewerKey, NewKey, OldKey), new AMinterThatMints(NewerKey))
                .InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.KeysChangedWhileItRan, Is.True);
                Assert.That(report.ActiveKeyId, Is.EqualTo(NewerKey.Id),
                    "a check labelled with a key it stopped reading against sends an operator to count secrets under the wrong one");
            }
        }

        [Test]
        public async Task ASecretNobodyCanRead_IsLeftByteForByte_AndIsNamedByConnectionAndField()
        {
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var before = await StoredOptionAsync(ClientSecret);
            var report = await CustodyService().ReEncryptAsync();

            var named = report.Secrets.Single(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(before),
                    "a value nobody can decrypt is a value nobody can re-encrypt, and writing over it destroys the only copy");
                Assert.That(named.ConnectionName, Is.EqualTo(Contoso));
                Assert.That(named.Field, Is.EqualTo(ClientSecret));
                Assert.That(report.MovedCount, Is.EqualTo(1), "the rest of the secrets are still moved");
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task AValueThatWasNeverEncrypted_IsLeftAlone_AndReportedRatherThanEncrypted()
        {
            await StoreAsync(ClientSecret, "a-secret-somebody-typed-in-before-any-of-this");

            var before = await StoredOptionAsync(ClientSecret);
            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(before));
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.NotEncrypted));
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.Zero);
            }
        }

        [Test]
        public async Task ARowSomebodyElseRewroteInBetween_IsCountedAsAlreadyMoved_AndThePassCarriesOn()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var refreshed = Under(NewKey, "written-by-somebody-else");
            var interfering = new WriterThatGetsThereFirst(crypto, async () => await StoreAsync(PersonalAccessToken, refreshed));

            var report = await CustodyService(interfering).ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(refreshed),
                    "the write the pass would have made is exactly the one that must not happen");
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.MovedByAnotherWriter));
                Assert.That(report.UnreadableCount, Is.Zero, "a row somebody else wrote is not a failure");
                Assert.That(report.Secrets, Has.Count.EqualTo(2), "the pass carries on to the rest");
            }
        }

        /// <summary>
        /// The same guard as the connection option above, on the column that actually gets rewritten by a
        /// token refresh. A predicate that matched on the row rather than on the value would put the token
        /// the refresh replaced back over the one it just obtained, and the only way back from that is
        /// re-authorising with the work tracking system.
        /// </summary>
        [Test]
        public async Task AnAccessTokenRewrittenBetweenTheReadAndTheWrite_IsNotWrittenOver()
        {
            var refreshTokenAsStored = Under(OldKey, "the-refresh-token");
            await StoreCredentialAsync(Under(OldKey, "the-access-token"), refreshTokenAsStored);

            var refreshed = Under(NewKey, "obtained-by-a-refresh");
            var interfering = new WriterThatGetsThereFirst(
                crypto,
                async () => await StoreCredentialAsync(refreshed, refreshTokenAsStored));

            var report = await CustodyService(interfering).ReEncryptAsync();
            var credential = await StoredCredentialAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(credential.AccessToken, Is.EqualTo(refreshed),
                    "the pass must not write over a value it did not read");
                Assert.That(crypto.Read(credential.RefreshToken).PlainText, Is.EqualTo("the-refresh-token"),
                    "the other token on the same row is still moved");
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.MovedByAnotherWriter));
            }
        }

        [Test]
        public async Task ARefreshTokenRewrittenBetweenTheReadAndTheWrite_IsNotWrittenOver()
        {
            var accessTokenAsStored = Under(OldKey, "the-access-token");
            await StoreCredentialAsync(accessTokenAsStored, Under(OldKey, "the-refresh-token"));

            var refreshed = Under(NewKey, "obtained-by-a-refresh");
            var interfering = new WriterThatGetsThereFirst(
                crypto,
                async () => await StoreCredentialAsync(accessTokenAsStored, refreshed),
                letThrough: 1);

            await CustodyService(interfering).ReEncryptAsync();
            var credential = await StoredCredentialAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(credential.RefreshToken, Is.EqualTo(refreshed),
                    "a refresh token is the one credential that cannot be recovered without going back to the tracker");
                Assert.That(crypto.Read(credential.AccessToken).PlainText, Is.EqualTo("the-access-token"));
            }
        }

        /// <summary>
        /// A credential row carries two tokens and they do not have to be on the same key. Asking about only
        /// one of them leaves the other stranded on a retired key with nothing ever coming back for it.
        /// </summary>
        [Test]
        public async Task ACredentialWithOnlyOneTokenLeftBehind_StillHasThatOneMoved()
        {
            var alreadyCurrent = Under(NewKey, "already-current");
            await StoreCredentialAsync(alreadyCurrent, Under(OldKey, "left-behind"));

            var report = await CustodyService().ReEncryptAsync();
            var credential = await StoredCredentialAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(1));
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Field == SecretCustodyService.RefreshTokenField));
                Assert.That(crypto.Read(credential.RefreshToken).KeyId, Is.EqualTo(NewKey.Id));
                Assert.That(credential.AccessToken, Is.EqualTo(alreadyCurrent),
                    "the token that was already current is not rewritten");
            }
        }

        [Test]
        public async Task ACredentialWithOnlyItsAccessTokenLeftBehind_StillHasThatOneMoved()
        {
            await StoreCredentialAsync(Under(OldKey, "left-behind"), Under(NewKey, "already-current"));

            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(1));
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Field == SecretCustodyService.AccessTokenField));
                Assert.That(crypto.Read((await StoredCredentialAsync()).AccessToken).PlainText, Is.EqualTo("left-behind"));
            }
        }

        [Test]
        public async Task AnEmptyToken_IsNotSomethingToMove()
        {
            await StoreCredentialAsync(string.Empty, Under(OldKey, "the-refresh-token"));

            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(1), "there is nothing stored in an empty column to move or to report");
                Assert.That(report.Secrets, Has.None.Matches<StoredSecretRecord>(
                    secret => secret.Field == SecretCustodyService.AccessTokenField));
                Assert.That((await StoredCredentialAsync()).AccessToken, Is.Empty);
            }
        }

        [Test]
        public async Task RunningItAgain_MovesNothing_AndReportsTheSameTotals()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            await CustodyService().ReEncryptAsync();

            var second = await CustodyService().ReEncryptAsync();
            var storedAfterSecond = await StoredOptionAsync(PersonalAccessToken);
            var third = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.MovedCount, Is.Zero);
                Assert.That(third.MovedCount, Is.Zero);
                Assert.That(third.UnreadableCount, Is.EqualTo(second.UnreadableCount));
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(storedAfterSecond));
            }
        }

        [Test]
        public async Task ACancelledRun_LeavesEverySecretReadable_AndARerunCompletesTheRemainder()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            using var stopAfterTheFirst = new CancellationTokenSource();
            var interfering = new WriterThatGetsThereFirst(
                crypto,
                () =>
                {
                    stopAfterTheFirst.Cancel();
                    return Task.CompletedTask;
                },
                letThrough: 1);

            Assert.That(
                async () => await CustodyService(interfering).ReEncryptAsync(stopAfterTheFirst.Token),
                Throws.InstanceOf<OperationCanceledException>());

            var everyStoredSecret = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            var completing = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everyStoredSecret.Select(stored => crypto.Read(stored).State),
                    Is.All.EqualTo(SecretState.Envelope),
                    "both keys are held, so an interrupted pass leaves every credential readable whichever key it is under");
                Assert.That(completing.MovedCount, Is.EqualTo(1), "the second run moves exactly what the first one did not");
                Assert.That(crypto.Read(await StoredOptionAsync(ClientSecret)).PlainText, Is.EqualTo("contoso-secret"));
            }
        }

        [Test]
        public async Task Inspecting_LeavesEveryStoredValueExactlyAsItFoundIt()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            var before = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            var report = await CustodyService().InspectAsync();

            var after = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after, Is.EqualTo(before).AsCollection);
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.EqualTo(1), "looking still says what cannot be read");
            }
        }

        /// <summary>
        /// The predicate that makes a rotation resumable - ask the database what is left to do - is exactly
        /// the wrong one for a check, which is asked what IS. Filtering the finished rows out of a check
        /// would leave a freshly rotated instance reporting nothing at all.
        /// </summary>
        [Test]
        public async Task ACheck_SeesEveryStoredSecret_IncludingTheOnesAlreadyOnTheKeyInForce()
        {
            var report = await CustodyService().InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Has.Count.EqualTo(4),
                    "two connection options and two tokens are stored, all of them already on the key in force");
                Assert.That(report.OnActiveKeyCount, Is.EqualTo(4));
                Assert.That(report.Secrets.Select(secret => secret.ConnectionName), Has.All.EqualTo(Contoso));
                Assert.That(report.Secrets.Select(secret => secret.Field), Is.EquivalentTo(new[]
                {
                    PersonalAccessToken,
                    ClientSecret,
                    SecretCustodyService.AccessTokenField,
                    SecretCustodyService.RefreshTokenField,
                }));
            }
        }

        [Test]
        public async Task ACheck_NamesTheKeyEachSecretIsUnder_AndTellsTheFourStatesApart()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, "not-encrypted-at-all");
            await StoreCredentialAsync(Under(KeyNobodyHolds, "unrecoverable"), Under(NewKey, "seed"));

            var report = await CustodyService().InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.OnActiveKeyCount, Is.EqualTo(1));
                Assert.That(report.OnRetiredKeyCount, Is.EqualTo(1));
                Assert.That(report.PlaintextCount, Is.EqualTo(1));
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
                Assert.That(report.Secrets.Single(secret => secret.Field == PersonalAccessToken).KeyId, Is.EqualTo(OldKey.Id));
                Assert.That(report.Secrets.Single(secret => secret.Field == ClientSecret).State, Is.EqualTo(SecretState.LegacyPlaintext));
            }
        }

        /// <summary>
        /// Two keys minted on the same day differ only by the counter at the end, so one key's id is a
        /// prefix of the next one's the moment the count passes nine. Matching on the id without the
        /// separator that follows it would make everything stored under k-2026-08-16-1 look like it was
        /// already on k-2026-08-16-11, and a rotation would walk straight past it.
        /// </summary>
        [Test]
        public async Task AKeyWhoseIdBeginsWithTheKeyInForcesId_IsNotMistakenForIt()
        {
            var inForce = new EncryptionKey("k-2026-08-16-1", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));
            var theEleventh = new EncryptionKey("k-2026-08-16-11", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

            var holder = new EncryptionKeyRingHolder(
                new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, inForce, theEleventh));
            var cryptoService = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            await StoreAsync(PersonalAccessToken, SecretEnvelope.Protect("contoso-pat", theEleventh.Id, theEleventh.Material.Span).Format());

            var report = await Rotating(holder, cryptoService, new AMinterThatMints(inForce)).ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets.Select(secret => secret.Field), Does.Contain(PersonalAccessToken),
                    "the stored value only looks like it is already on the key in force if the separator after the key id is dropped");
                Assert.That(cryptoService.Read(await StoredOptionAsync(PersonalAccessToken)).KeyId, Is.EqualTo(inForce.Id));
            }
        }

        [Test]
        public void ACheckThatWasCancelledBeforeItBegan_StopsRatherThanReportingOnEverything()
        {
            using var alreadyCancelled = new CancellationTokenSource();
            alreadyCancelled.Cancel();

            Assert.That(
                async () => await CustodyService().InspectAsync(alreadyCancelled.Token),
                Throws.InstanceOf<OperationCanceledException>(),
                "a walk over every stored secret on a large instance has to be abandonable when the request that asked for it is gone");
        }

        [Test]
        public async Task ACheck_NamesAnUnreadableSecret_ByTheConnectionAndFieldThatOwnIt()
        {
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            var report = await CustodyService().InspectAsync();

            var named = report.Secrets.Single(secret => secret.State == SecretState.Unreadable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.ConnectionName, Is.EqualTo(Contoso));
                Assert.That(named.Field, Is.EqualTo(ClientSecret));
                Assert.That(report.MovedCount, Is.Zero, "a check moves nothing, so a summary that talks about moving is talking about nothing");
            }
        }

        [Test]
        public async Task CheckingTwiceInARow_SaysTheSameThing_AndChangesNothing()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var service = CustodyService();

            var first = await service.InspectAsync();
            var before = await StoredOptionAsync(PersonalAccessToken);
            var second = await service.InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.Secrets, Has.Count.EqualTo(first.Secrets.Count));
                Assert.That(second.OnActiveKeyCount, Is.EqualTo(first.OnActiveKeyCount));
                Assert.That(second.OnRetiredKeyCount, Is.EqualTo(first.OnRetiredKeyCount));
                Assert.That(second.UnreadableCount, Is.EqualTo(first.UnreadableCount));
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        [Test]
        public async Task ACheckStraightAfterARotation_ReportsEveryReadableSecretOnTheKeyInForce()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var (holder, cryptoService) = AnInstanceThatOwnsItsKey();
            var service = Rotating(holder, cryptoService, new AMinterThatMints(NewerKey));

            await service.RotateAsync();
            var report = await service.InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Has.Count.EqualTo(4), "a check after a rotation still sees every stored secret");
                Assert.That(report.ActiveKeyId, Is.EqualTo(NewerKey.Id));
                Assert.That(report.OnActiveKeyCount, Is.EqualTo(4));
                Assert.That(report.OnRetiredKeyCount, Is.Zero);
            }
        }

        [Test]
        public async Task ACheckOnAnInstanceHoldingNothing_ReportsNoSecrets_AndStillNamesTheKeyInForce()
        {
            await StoreAsync(PersonalAccessToken, string.Empty);
            await StoreAsync(ClientSecret, string.Empty);
            await StoreCredentialAsync(string.Empty, string.Empty);

            var report = await CustodyService().InspectAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Is.Empty);
                Assert.That(report.ActiveKeyId, Is.EqualTo(NewKey.Id),
                    "having nothing stored is an ordinary state, not a failure, and the answer still says which key is in force");
                Assert.That(report.UnreadableCount, Is.Zero);
            }
        }

        /// <summary>
        /// The check has to finish inside a request on the largest instance anyone runs. What makes that
        /// hold is that the whole walk is two lookups whatever the instance holds; a lookup per secret
        /// would turn a large install into a timeout, and it would pass every test written on a small one.
        /// </summary>
        [Test]
        public async Task WhatItCostsToLookThemAllUp_DoesNotGrowWithHowManyThereAre()
        {
            var overASmallInstance = await LookupsWhileCheckingAsync();

            await StoreManyMoreSecretsAsync(40);

            var overALargeOne = await LookupsWhileCheckingAsync();

            Assert.That(overALargeOne, Is.EqualTo(overASmallInstance),
                $"checking 4 secrets cost {overASmallInstance} lookups and checking 44 cost {overALargeOne}");
        }

        [Test]
        public async Task TheStoredOAuthTokens_AreMovedToo_AndNamedByTheirOwnFields()
        {
            await StoreCredentialAsync(Under(OldKey, "the-access-token"), Under(OldKey, "the-refresh-token"));

            var report = await CustodyService().ReEncryptAsync();

            var credential = await StoredCredentialAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(2));
                Assert.That(crypto.Read(credential.AccessToken).PlainText, Is.EqualTo("the-access-token"));
                Assert.That(crypto.Read(credential.RefreshToken).PlainText, Is.EqualTo("the-refresh-token"));
                Assert.That(report.Secrets.Select(secret => secret.Field),
                    Is.EquivalentTo(new[] { SecretCustodyService.AccessTokenField, SecretCustodyService.RefreshTokenField }));
            }
        }

        /// <summary>
        /// A database that will not take the write is a different thing from a credential nobody can read,
        /// and the two must not arrive at an operator looking the same: one is a token to re-enter, the
        /// other is a pass to run again. A read-only database is the one way to produce a refused write on
        /// demand, on every platform, without holding a lock and hoping for a timeout.
        /// </summary>
        [Test]
        public async Task ADatabaseThatWillNotTakeTheWrite_IsReportedAsThat_AndNotAsASecretNobodyCanRead()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            var before = await StoredOptionAsync(PersonalAccessToken);

            await using var readOnly = BuildProvider($"Data Source={databaseFile};Mode=ReadOnly", crypto);
            using var scope = readOnly.CreateScope();

            var report = await new SecretCustodyService(
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>(),
                crypto,
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey, OldKey)),
                new AMinterThatMints(NewerKey),
                oneAtATime).ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Has.Some.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.CouldNotBeWritten));
                Assert.That(report.UnreadableCount, Is.Zero,
                    "nothing is wrong with the credential, so nobody should be sent to reissue it");
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        /// <summary>
        /// Moving a secret is not a change an administrator made, so it must not reject one they are in the
        /// middle of making. The pass writes outside the save pipeline, which is what keeps the concurrency
        /// token an open form is holding still valid.
        /// </summary>
        [Test]
        public async Task ARotation_DoesNotRejectAnEditSomebodyAlreadyHadOpen()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var opened = await ConnectionAsync();
            var tokenTheFormIsHolding = opened.ConcurrencyToken;

            await CustodyService().ReEncryptAsync();

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var saving = await context.WorkTrackingSystemConnections.SingleAsync(connection => connection.Id == connectionId);
            saving.Name = "Contoso Board, renamed while the rotation ran";
            context.ApplyConcurrencyTokenForEdit(saving, tokenTheFormIsHolding);

            Assert.That(async () => await context.SaveChangesAsync(), Throws.Nothing,
                "a rotation is not a semantic edit, so an administrator must not have their save turned down because one ran");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((await ConnectionAsync()).Name, Is.EqualTo("Contoso Board, renamed while the rotation ran"));
                Assert.That(crypto.Read(await StoredOptionAsync(PersonalAccessToken)).PlainText, Is.EqualTo("contoso-pat"),
                    "the secret nobody retyped is still readable after both the move and the save");
            }
        }

        [Test]
        public async Task Rotating_MakesAKeyAndMovesEverythingOntoIt_AndKeepsTheKeyThatWasInForce()
        {
            var (holder, rotating) = AnInstanceThatOwnsItsKey();
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var report = await Rotating(holder, rotating, new AMinterThatMints(NewerKey)).RotateAsync();

            var stored = await StoredOptionAsync(PersonalAccessToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(NewerKey.Id));
                Assert.That(holder.Current.TryGet(NewKey.Id, out _), Is.True, "the key that was in force is retired, not discarded");
                Assert.That(holder.Current.TryGet(OldKey.Id, out _), Is.True);
                Assert.That(report.MovedCount, Is.EqualTo(4), "both connection options and both OAuth tokens move onto the new key");
                Assert.That(rotating.Read(stored).KeyId, Is.EqualTo(NewerKey.Id));
                Assert.That(rotating.Read(stored).PlainText, Is.EqualTo("contoso-pat"));
            }
        }

        [Test]
        public async Task RotatingWhereAnOperatorOwnsTheKey_IsRefused_AndNothingIsWritten()
        {
            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByConfiguration, NewKey, OldKey));
            var supplied = new CryptoService(holder, NullLogger<CryptoService>.Instance);
            var minter = new AKeyOnlyItsOwnerCanReplace(KeyCustody.SuppliedByConfiguration);

            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            var before = await StoredOptionAsync(PersonalAccessToken);

            Assert.That(
                async () => await Rotating(holder, supplied, minter).RotateAsync(),
                Throws.InstanceOf<MintingNotPermittedException>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(NewKey.Id), "the keys held are exactly the keys that were held");
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        [Test]
        public async Task AKeyThatCannotBeProved_IsNeverUsed_AndNoSecretIsWritten()
        {
            var (holder, rotating) = AnInstanceThatOwnsItsKey();
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            var before = await StoredOptionAsync(PersonalAccessToken);

            Assert.That(
                async () => await Rotating(holder, rotating, new AMinterThatCannotKeepIt()).RotateAsync(),
                Throws.InvalidOperationException);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(NewKey.Id));
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        /// <summary>
        /// The ring only ever grows. Anything that could be read a moment before a rotation can still be
        /// read after it, which is what lets a request that loaded a credential before the pass started go
        /// on using it - and what lets an interrupted pass leave a working instance.
        /// </summary>
        [Test]
        public async Task ARotation_MovesEverythingOffThePublishedKey_AndStillHoldsEveryKeyItHeldBefore()
        {
            var (holder, rotating) = AnInstanceStillHoldingThePublishedKey(out var published);
            var heldBefore = IdsOn(holder.Current);

            await StoreAsync(PersonalAccessToken, Under(published, "written-before-any-of-this"));
            await StoreAsync(ClientSecret, Under(published, "also-written-before"));

            var report = await Rotating(holder, rotating, new AMinterThatMints(NewerKey)).RotateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(4), "the two under the published key and the two the OAuth credential holds");
                Assert.That(rotating.Read(await StoredOptionAsync(PersonalAccessToken)).KeyId, Is.EqualTo(NewerKey.Id),
                    "nothing is stored under the published key any more");
                Assert.That(IdsOn(holder.Current), Is.SupersetOf(heldBefore),
                    "a key taken off the ring is a credential an in-flight request is holding that nobody can read any more");
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(NewerKey.Id));
            }
        }

        [Test]
        public async Task ARotation_LeavesASecretUnderThePublishedKeyThatNobodyCanRead_ExactlyAsItIs()
        {
            var (holder, rotating) = AnInstanceStillHoldingThePublishedKey(out var published);

            await StoreAsync(PersonalAccessToken, Under(published, "written-before-any-of-this"));
            await StoreAsync(ClientSecret, SecretEnvelope.Protect("lost", LegacyDefaultEncryptionKey.Id, KeyNobodyHolds.Material.Span).Format());

            var before = await StoredOptionAsync(ClientSecret);
            var report = await Rotating(holder, rotating, new AMinterThatMints(NewerKey)).RotateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(before));
                Assert.That(holder.Current.TryGet(LegacyDefaultEncryptionKey.Id, out _), Is.True,
                    "one secret still names it, so letting it go would put that credential out of reach for good");
            }
        }

        /// <summary>
        /// Saving a connection hands back every secret it holds, including ones nobody retyped. A value no
        /// key here can open is still the credential, encrypted under a key that exists somewhere, and
        /// restoring that key store brings it back. Encrypting it wraps ciphertext nobody can read inside
        /// ciphertext they can, which destroys the only copy and then reports the row as healthy - so an
        /// ordinary save of an unrelated field is enough to lose a credential for good.
        /// </summary>
        [Test]
        public async Task SavingAConnection_LeavesASecretItCannotRead_ExactlyAsItIs()
        {
            var beyondReach = Under(KeyNobodyHolds, "written-under-a-key-this-instance-lost");
            await StoreAsync(ClientSecret, beyondReach);

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = await context.WorkTrackingSystemConnections
                .Include(stored => stored.Options)
                .SingleAsync(stored => stored.Id == connectionId);

            connection.Name = "Renamed, without anybody touching a credential";
            context.WorkTrackingSystemConnections.Update(connection);
            await context.SaveChangesAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(beyondReach),
                    "restoring the key store that belongs to this database has to bring the credential back, and it cannot if a save wrapped it");
                Assert.That(crypto.Read(await StoredOptionAsync(ClientSecret)).State, Is.EqualTo(SecretState.Unreadable),
                    "it must also still say it cannot be read, rather than looking healthy from now on");
            }
        }

        /// <summary>
        /// The one interleaving that can destroy a credential outright. Two rotations each taking their own
        /// snapshot of the ring would mint two keys named after the same day, write one key store over the
        /// other, and leave every secret moved onto the losing key unreadable by anything, anywhere.
        /// </summary>
        [Test]
        public async Task TwoRotationsAtOnce_DoNotLeaveASecretUnderAKeyNobodyHolds()
        {
            var (holder, rotating) = AnInstanceThatOwnsItsKey();
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var minter = new AMinterThatMintsADifferentKeyEachTime();

            await Task.WhenAll(
                Rotating(holder, rotating, minter).RotateAsync(),
                Rotating(holder, rotating, minter).RotateAsync());

            var stored = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(minter.TimesAsked, Is.EqualTo(2), "both rotations ran; one did not simply fail");
                Assert.That(minter.EveryKeyItMade.Select(key => key.Id).Distinct().Count(), Is.EqualTo(2),
                    "two keys minted from the same snapshot would be given the same name and different material");

                foreach (var value in stored)
                {
                    Assert.That(rotating.Read(value).State, Is.EqualTo(SecretState.Envelope),
                        "every secret is still readable with a key the ring holds");
                }
            }
        }

        [Test]
        public void ThePass_RefusesEveryThingItCannotWorkWithout()
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey));
            var minter = new AMinterThatMints(NewerKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => new SecretCustodyService(null!, crypto, holder, minter, oneAtATime), Throws.ArgumentNullException);
                Assert.That(() => new SecretCustodyService(context, null!, holder, minter, oneAtATime), Throws.ArgumentNullException);
                Assert.That(() => new SecretCustodyService(context, crypto, null!, minter, oneAtATime), Throws.ArgumentNullException);
                Assert.That(() => new SecretCustodyService(context, crypto, holder, null!, oneAtATime), Throws.ArgumentNullException);
                Assert.That(() => new SecretCustodyService(context, crypto, holder, minter, null!), Throws.ArgumentNullException);
                Assert.That(async () => await oneAtATime.RunAsync<int>(null!, CancellationToken.None), Throws.ArgumentNullException);
            }
        }

        /// <summary>
        /// An administrator turned down has to be told who the key belongs to, because that is the only
        /// thing that tells them where to go and do something about it. One refusal for all three would
        /// send a Kubernetes operator looking in a settings file.
        /// </summary>
        [Test]
        public void TheRefusalToMakeAKey_NamesWhoTheKeyBelongsTo()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(RefusalFor(KeyCustody.SuppliedByConfiguration), Does.Contain("configuration").And.Contain("belongs to"));
                Assert.That(RefusalFor(KeyCustody.SuppliedByExternalSecret), Does.Contain("mounted secret").And.Contain("belongs to"));
                Assert.That(RefusalFor(KeyCustody.NoDurableStore), Does.Contain("nowhere it could keep a key"));

                foreach (var custody in Enum.GetValues<KeyCustody>())
                {
                    Assert.That(RefusalFor(custody), Does.Contain("start Lighthouse again"),
                        $"the refusal for {custody} says who owns the key but not what to do next");
                }
            }
        }

        private static string RefusalFor(KeyCustody custody)
        {
            var refused = Assert.Throws<MintingNotPermittedException>(
                () => new AKeyOnlyItsOwnerCanReplace(custody).MintOnto(
                    new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey)));

            return refused!.Message;
        }

        private (EncryptionKeyRingHolder Holder, CryptoService Crypto) AnInstanceThatOwnsItsKey()
        {
            var holder = new EncryptionKeyRingHolder(
                new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey, OldKey));

            return (holder, new CryptoService(holder, NullLogger<CryptoService>.Instance));
        }

        private static (EncryptionKeyRingHolder Holder, CryptoService Crypto) AnInstanceStillHoldingThePublishedKey(
            out EncryptionKey published)
        {
            var ring = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey).WithLegacyDefault();
            ring.TryGet(LegacyDefaultEncryptionKey.Id, out var legacy);
            published = legacy!;

            var holder = new EncryptionKeyRingHolder(ring);

            return (holder, new CryptoService(holder, NullLogger<CryptoService>.Instance));
        }

        private SecretCustodyService Rotating(
            IEncryptionKeyRingHolder holder, ICryptoService cryptoService, IKeyRingMinter minter)
        {
            var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return new SecretCustodyService(context, cryptoService, holder, minter, oneAtATime);
        }

        private static List<string> IdsOn(EncryptionKeyRing ring)
        {
            return [ring.ActiveKey.Id, .. ring.RetiredKeys.Select(key => key.Id)];
        }

        private SecretCustodyService CustodyService(ICryptoService? cryptoService = null)
        {
            var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return new SecretCustodyService(
                context,
                cryptoService ?? crypto,
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey, OldKey)),
                new AMinterThatMints(NewerKey),
                oneAtATime);
        }

        // Written straight into the column rather than through a save, because a save encrypts anything it
        // cannot already read - which is exactly the shapes these tests are about.
        private async Task StoreAsync(string field, string storedValue)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.WorkTrackingSystemConnectionId == connectionId && option.Key == field)
                .ExecuteUpdateAsync(set => set.SetProperty(option => option.Value, storedValue));
        }

        private async Task StoreCredentialAsync(string accessToken, string refreshToken)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            await context.Set<OAuthCredential>()
                .Where(credential => credential.Id == credentialId)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(credential => credential.AccessToken, accessToken)
                    .SetProperty(credential => credential.RefreshToken, refreshToken));
        }

        private async Task<string> StoredOptionAsync(string field)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await context.Set<WorkTrackingSystemConnectionOption>()
                .AsNoTracking()
                .Where(option => option.WorkTrackingSystemConnectionId == connectionId && option.Key == field)
                .Select(option => option.Value)
                .SingleAsync();
        }

        private async Task<WorkTrackingSystemConnection> ConnectionAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await context.WorkTrackingSystemConnections
                .AsNoTracking()
                .SingleAsync(connection => connection.Id == connectionId);
        }

        private async Task<OAuthCredential> StoredCredentialAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await context.Set<OAuthCredential>().AsNoTracking().SingleAsync(credential => credential.Id == credentialId);
        }

        private static string Under(EncryptionKey key, string credential)
        {
            return SecretEnvelope.Protect(credential, key.Id, key.Material.Span).Format();
        }

        // Stands where the reload timer would: an operator replaces the mounted keys file part-way through
        // a pass, which is the pair of actions this feature invites rather than a strange thing to do.
        private static WriterThatGetsThereFirst ReplacingTheRingAfter(
            int rows, ICryptoService inner, EncryptionKeyRingHolder holder, params EncryptionKey[] replacement)
        {
            return new WriterThatGetsThereFirst(
                inner,
                () =>
                {
                    holder.Replace(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, replacement));
                    return Task.CompletedTask;
                },
                rows);
        }

        private static CryptoService CryptoOver(params EncryptionKey[] keys)
        {
            return new CryptoService(
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, keys)),
                NullLogger<CryptoService>.Instance);
        }

        // Seeded with values already under the key in force, so nothing is a candidate until a test
        // deliberately puts one there.
        private async Task<(int ConnectionId, int CredentialId)> SeedAsync(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = Contoso,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = PersonalAccessToken, Value = Under(NewKey, "seed"), IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = ClientSecret, Value = Under(NewKey, "seed"), IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Url", Value = "https://dev.azure.com/contoso", IsSecret = false });

            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = Under(NewKey, "seed"),
                RefreshToken = Under(NewKey, "seed"),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            context.Set<OAuthCredential>().Add(credential);
            await context.SaveChangesAsync();

            return (connection.Id, credential.Id);
        }

        private static ServiceProvider BuildProvider(
            string connectionString, ICryptoService cryptoService, CountsEveryLookup? counter = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(cryptoService);
            services.AddDbContext<LighthouseAppContext>(options =>
            {
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));

                if (counter is not null)
                {
                    options.AddInterceptors(counter);
                }
            });

            return services.BuildServiceProvider();
        }

        private async Task<int> LookupsWhileCheckingAsync()
        {
            var service = CustodyService();

            lookups.Reset();
            await service.InspectAsync();

            return lookups.Count;
        }

        private async Task StoreManyMoreSecretsAsync(int howMany)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = await context.WorkTrackingSystemConnections
                .Include(worktracking => worktracking.Options)
                .SingleAsync(worktracking => worktracking.Id == connectionId);

            for (var index = 0; index < howMany; index++)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = $"Secret{index}",
                    Value = Under(NewKey, $"credential-{index}"),
                    IsSecret = true,
                });
            }

            await context.SaveChangesAsync();
        }

        // Counting the round trips rather than timing the walk: a stopwatch on a build agent measures the
        // agent, and the thing that would actually break a large instance is asking once per secret.
        private sealed class CountsEveryLookup : DbCommandInterceptor
        {
            private int count;

            public int Count => count;

            public void Reset()
            {
                Interlocked.Exchange(ref count, 0);
            }

            public override InterceptionResult<DbDataReader> ReaderExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                Interlocked.Increment(ref count);

                return base.ReaderExecuting(command, eventData, result);
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref count);

                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
        }

        private sealed class AMinterThatMints : IKeyRingMinter
        {
            private readonly EncryptionKey minted;

            public AMinterThatMints(EncryptionKey minted)
            {
                this.minted = minted;
            }

            // Drops the published key exactly as the real one does, so what a rotation ends up holding is
            // the same here as in production.
            public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
            {
                var kept = existing.Without(LegacyDefaultEncryptionKey.Id);

                return new EncryptionKeyRing(kept.Custody, [minted, kept.ActiveKey, .. kept.RetiredKeys]);
            }
        }

        // Names each key the way the real one does: from what the ring it was handed already holds. Two
        // mints taken from the same snapshot therefore ask for the same name and get different material,
        // which is exactly the state that loses a credential.
        private sealed class AMinterThatMintsADifferentKeyEachTime : IKeyRingMinter
        {
            private readonly List<EncryptionKey> made = [];

            public int TimesAsked { get; private set; }

            public IReadOnlyList<EncryptionKey> EveryKeyItMade => made;

            public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
            {
                TimesAsked++;

                var kept = existing.Without(LegacyDefaultEncryptionKey.Id);
                var name = Enumerable.Range(1, 99)
                    .Select(madeToday => $"k-2026-08-16-{madeToday:00}")
                    .First(candidate => !kept.TryGet(candidate, out _));

                var key = new EncryptionKey(name, RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));
                made.Add(key);

                return new EncryptionKeyRing(kept.Custody, [key, kept.ActiveKey, .. kept.RetiredKeys]);
            }
        }

        // A filesystem that accepts the write, reports success and hands back something else afterwards.
        private sealed class AMinterThatCannotKeepIt : IKeyRingMinter
        {
            public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
            {
                throw new InvalidOperationException(
                    "The encryption key did not read back as what was written, so this filesystem cannot be trusted to keep it.");
            }
        }

        /// <summary>
        /// Stands where a token refresh would: the pass encrypts the value it read immediately before it
        /// writes, so this is the one moment at which somebody else can get to the row first.
        /// </summary>
        private sealed class WriterThatGetsThereFirst : ICryptoService
        {
            private readonly ICryptoService inner;

            private readonly Func<Task> getThereFirst;

            private readonly int letThrough;

            private int reached;

            public WriterThatGetsThereFirst(ICryptoService inner, Func<Task> getThereFirst, int letThrough = 0)
            {
                this.inner = inner;
                this.getThereFirst = getThereFirst;
                this.letThrough = letThrough;
            }

            public string Encrypt(string plainText)
            {
                GetThereFirstIfItIsTime();

                return inner.Encrypt(plainText);
            }

            public string Encrypt(string plainText, EncryptionKey key)
            {
                GetThereFirstIfItIsTime();

                return inner.Encrypt(plainText, key);
            }

            public string Decrypt(string cipherText)
            {
                return inner.Decrypt(cipherText);
            }

            public SecretReadResult Read(string storedValue)
            {
                return inner.Read(storedValue);
            }

            private void GetThereFirstIfItIsTime()
            {
                if (reached++ == letThrough)
                {
                    getThereFirst().GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Stands where the reload timer would during a check that only looks. Nothing is encrypted on that
        /// path, so between one row being read and the next is the only moment a replacement can land in.
        /// </summary>
        private sealed class ReaderThatReplacesTheRing : ICryptoService
        {
            private readonly ICryptoService inner;

            private readonly EncryptionKeyRingHolder holder;

            private readonly EncryptionKey[] replacement;

            private bool replaced;

            public ReaderThatReplacesTheRing(
                ICryptoService inner, EncryptionKeyRingHolder holder, params EncryptionKey[] replacement)
            {
                this.inner = inner;
                this.holder = holder;
                this.replacement = replacement;
            }

            public string Encrypt(string plainText)
            {
                return inner.Encrypt(plainText);
            }

            public string Encrypt(string plainText, EncryptionKey key)
            {
                return inner.Encrypt(plainText, key);
            }

            public string Decrypt(string cipherText)
            {
                return inner.Decrypt(cipherText);
            }

            public SecretReadResult Read(string storedValue)
            {
                var secret = inner.Read(storedValue);

                if (!replaced)
                {
                    replaced = true;
                    holder.Replace(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, replacement));
                }

                return secret;
            }
        }
    }
}
