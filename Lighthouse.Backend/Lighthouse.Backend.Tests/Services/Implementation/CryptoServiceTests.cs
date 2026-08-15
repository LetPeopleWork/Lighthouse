using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class CryptoServiceTests
    {
        private const string ConfiguredKeyBase64 = "aXhZdXd5+OeT8kjKP2gB7UdqMEB3RY4LQMI2yffxDEw=";

        private const string AnotherKeyBase64 = "jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=";

        private const string EncryptionKeyConfigKey = "Encryption:Key";

        private static readonly string[] OnlyThePublishedKey = [LegacyDefaultEncryptionKey.Id];

        private const string DerivedKeyIdPrefix = "k-cfg-";

        private const int DerivedKeyIdLength = 14;

        private const string ActiveKeyId = "key-active";

        private const string UnknownKeyId = "key-not-on-the-ring";

        private const string Credential = "Hello, World!";

        private const int KeyIdField = 1;

        private const int IvLength = 16;

        private const int CiphertextField = 3;

        private const string StateProperty = "SecretState";

        private const string ClaimedKeyIdProperty = "ClaimedKeyId";

        private const string MessageTemplateProperty = "{OriginalFormat}";

        private const int RepeatedReads = 50;

        private const int RememberedSecretsLimit = 1000;

        private const int DistinctValuesFarBeyondTheLimit = 5000;

        private static readonly string[] ExpectedWarningProperties = [StateProperty, ClaimedKeyIdProperty];

        private static readonly byte[] ActiveKeyMaterial = Convert.FromBase64String(ConfiguredKeyBase64);

        private static readonly byte[] OffRingKeyMaterial = Convert.FromBase64String(AnotherKeyBase64);

        // Everywhere else in this file the initialisation vector is random, but here it is fixed so the
        // bytes are identical on every run. A wrong key's garbage clears the padding and printability
        // checks by chance roughly once in a thousand tries, and a test that fails one run in a thousand
        // teaches a reader to ignore it.
        private static readonly byte[] UnreadableBlobInitialisationVector = new byte[IvLength];

        private CryptoService subject;

        private Mock<ILogger<CryptoService>> loggerMock;

        [SetUp]
        public void SetUp()
        {
            var ring = new EncryptionKeyRing(new EncryptionKey(ActiveKeyId, ActiveKeyMaterial));
            loggerMock = new Mock<ILogger<CryptoService>>();

            subject = new CryptoService(new EncryptionKeyRingHolder(ring), loggerMock.Object);
        }

        [Test]
        public void Encrypt_ShouldEncryptText()
        {
            var encryptedText = subject.Encrypt(Credential);

            Assert.That(encryptedText, Is.Not.EqualTo(Credential));
        }

        [Test]
        public void Encrypt_ShouldWriteAnEnvelopeNamingTheActiveKey()
        {
            var storedValue = subject.Encrypt(Credential);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedValue, Does.StartWith(SecretEnvelope.Prefix));
                Assert.That(storedValue.Split('.')[KeyIdField], Is.EqualTo(ActiveKeyId));
                Assert.That(storedValue, Does.Not.Contain(Credential));
            }
        }

        [Test]
        public void Decrypt_ShouldDecryptText()
        {
            var encryptedText = subject.Encrypt(Credential);

            var decryptedText = subject.Decrypt(encryptedText);

            Assert.That(decryptedText, Is.EqualTo(Credential));
        }

        [Test]
        public void Decrypt_ValueWrittenByThePreviousImplementation_ShouldReturnTheCredentialUnchanged()
        {
            var storedValue = LegacyCbcBlob(Credential, ActiveKeyMaterial);

            var decryptedText = subject.Decrypt(storedValue);

            Assert.That(decryptedText, Is.EqualTo(Credential));
        }

        [TestCaseSource(nameof(UnreadableStoredValues))]
        public void Decrypt_UnreadableValue_Throws(string storedValue, string? claimedKeyId)
        {
            var exception = Assert.Throws<UnreadableSecretException>(() => subject.Decrypt(storedValue));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(exception.ClaimedKeyId, Is.EqualTo(claimedKeyId));
                Assert.That(exception.Message, Does.Not.Contain(Credential));
                Assert.That(exception.Message, Does.Not.Contain(storedValue));
                Assert.That(exception.Message, Does.Not.Contain(ConfiguredKeyBase64));
            }
        }

        [Test]
        public void Read_ValueWrittenByEncrypt_ShouldReportAnEnvelopeWithThePlainTextAndKeyId()
        {
            var result = subject.Read(subject.Encrypt(Credential));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(ActiveKeyId));
            }
        }

        [Test]
        public void Read_ValueWrittenByThePreviousImplementation_ShouldReportLegacyCbcWithThePlainText()
        {
            var result = subject.Read(LegacyCbcBlob(Credential, ActiveKeyMaterial));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyCbc));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(ActiveKeyId));
            }
        }

        [Test]
        public void Read_ValueThatWasNeverEncrypted_ShouldReportLegacyPlaintext()
        {
            var result = subject.Read(Credential);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyPlaintext));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.Null);
            }
        }

        [Test]
        public void Read_TheSameUnreadableValueOverAndOver_ShouldReportItOnce()
        {
            var storedValue = EnvelopeWithABrokenTag();

            for (var attempt = 0; attempt < RepeatedReads; attempt++)
            {
                subject.Read(storedValue);
            }

            Assert.That(WarningStates(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Read_TwoDifferentUnreadableValues_ShouldReportBoth()
        {
            subject.Read(EnvelopeWithABrokenTag());
            subject.Read(EncryptedUnder(UnknownKeyId, OffRingKeyMaterial));

            Assert.That(WarningStates(), Has.Count.EqualTo(2));
        }

        [Test]
        public void Read_MoreUnreadableValuesThanItCanRemember_ShouldForgetTheOldestAndKeepTheNewest()
        {
            var storedValues = Enumerable.Range(0, DistinctValuesFarBeyondTheLimit)
                .Select(index => EncryptedUnder($"key-off-ring-{index}", OffRingKeyMaterial))
                .ToList();

            foreach (var storedValue in storedValues)
            {
                subject.Read(storedValue);
            }

            subject.Read(storedValues[^1]);
            subject.Read(storedValues[DistinctValuesFarBeyondTheLimit - RememberedSecretsLimit]);
            var afterRereadingWhatIsStillRemembered = WarningStates().Count;

            subject.Read(storedValues[DistinctValuesFarBeyondTheLimit - RememberedSecretsLimit - 1]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterRereadingWhatIsStillRemembered, Is.EqualTo(DistinctValuesFarBeyondTheLimit));
                Assert.That(WarningStates(), Has.Count.EqualTo(DistinctValuesFarBeyondTheLimit + 1));
            }
        }

        [Test]
        public void Read_UnreadableValue_ShouldReportOnlyTheStateAndTheKeyTheValueClaims()
        {
            subject.Read(EncryptedUnder(UnknownKeyId, OffRingKeyMaterial));

            var properties = TheOnlyWarningsProperties();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    properties.Select(property => property.Key).Where(name => name != MessageTemplateProperty),
                    Is.EqualTo(ExpectedWarningProperties));
                Assert.That(properties.Single(property => property.Key == StateProperty).Value, Is.EqualTo(SecretState.Unreadable));
                Assert.That(properties.Single(property => property.Key == ClaimedKeyIdProperty).Value, Is.EqualTo(UnknownKeyId));
            }
        }

        [Test]
        public void Read_UnreadableValue_ShouldWriteDownNothingAboutTheKeyTheStoredValueOrTheCredential()
        {
            var storedValue = EncryptedUnder(UnknownKeyId, OffRingKeyMaterial);

            subject.Read(storedValue);

            var everythingWritten = string.Join(" | ", EverythingLogged());
            var fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(storedValue));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everythingWritten, Does.Not.Contain(Credential));
                Assert.That(everythingWritten, Does.Not.Contain(storedValue));
                Assert.That(everythingWritten, Does.Not.Contain(storedValue.Split('.')[CiphertextField]));
                Assert.That(everythingWritten, Does.Not.Contain(ConfiguredKeyBase64));
                Assert.That(everythingWritten, Does.Not.Contain(AnotherKeyBase64));
                Assert.That(everythingWritten, Does.Not.Contain(Convert.ToHexString(fingerprint)));
                Assert.That(everythingWritten, Does.Not.Contain(Convert.ToBase64String(fingerprint)));
            }
        }

        [Test]
        public void Read_ValueItCanRead_ShouldReportNothing()
        {
            subject.Read(subject.Encrypt(Credential));
            subject.Read(LegacyCbcBlob(Credential, ActiveKeyMaterial));
            subject.Read(Credential);

            Assert.That(WarningStates(), Is.Empty);
        }

        [Test]
        public void EnsureEncryptionKeyRing_ShouldRegisterTheConfiguredKeyAsTheOnlyActiveKey()
        {
            var ring = ResolveRing(ConfiguredKeyBase64);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(ActiveKeyMaterial));
                Assert.That(ring.RetiredKeys.Select(key => key.Id), Is.EqualTo(OnlyThePublishedKey));
                Assert.That(ring.ActiveKey.Id, Is.Not.EqualTo(LegacyDefaultEncryptionKey.Id));
                Assert.That(ring.ActiveKey.Id, Does.StartWith(DerivedKeyIdPrefix));
                Assert.That(ring.ActiveKey.Id, Has.Length.EqualTo(DerivedKeyIdLength));
            }
        }

        [Test]
        public void EnsureEncryptionKeyRing_SameMaterialResolvedTwice_ShouldDeriveTheSameKeyId()
        {
            var first = ResolveRing(ConfiguredKeyBase64);
            var second = ResolveRing(ConfiguredKeyBase64);

            Assert.That(second.ActiveKey.Id, Is.EqualTo(first.ActiveKey.Id));
        }

        [Test]
        public void EnsureEncryptionKeyRing_DifferentMaterial_ShouldDeriveADifferentKeyId()
        {
            var configured = ResolveRing(ConfiguredKeyBase64);
            var another = ResolveRing(AnotherKeyBase64);

            Assert.That(another.ActiveKey.Id, Is.Not.EqualTo(configured.ActiveKey.Id));
        }

        [Test]
        public void EnsureEncryptionKeyRing_ShouldWriteNothingItResolvedBackIntoConfiguration()
        {
            var builder = ResolveInto(ConfiguredKeyBase64);
            var ring = RingOf(builder);

            Assert.That(((IConfigurationRoot)builder.Configuration).GetDebugView(), Does.Not.Contain(ring.ActiveKey.Id));
        }

        [Test]
        public void EnsureEncryptionKeyRing_ConfiguredKeyIsNotBase64_ShouldThrow()
        {
            Assert.That(
                () => ResolveInto("short_key"),
                Throws.InvalidOperationException.With.Message.Contains("could not be decoded as base64"));
        }

        [Test]
        public void EnsureEncryptionKeyRing_ConfiguredKeyIsTheWrongLength_ShouldThrow()
        {
            var tooShort = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            Assert.That(
                () => ResolveInto(tooShort),
                Throws.InvalidOperationException.With.Message.Contains("carries 16 bytes of key material"));
        }

        private List<object> WarningStates()
        {
            return [.. loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log) && (LogLevel)invocation.Arguments[0] == LogLevel.Warning)
                .Select(invocation => invocation.Arguments[2])];
        }

        private List<KeyValuePair<string, object?>> TheOnlyWarningsProperties()
        {
            return [.. (IReadOnlyList<KeyValuePair<string, object?>>)WarningStates().Single()];
        }

        // Everything the service handed the logging pipeline, at any level: the rendered line and each
        // structured property separately, because a credential that leaked into a property would never
        // show up in a test that only read the rendered line.
        private List<string> EverythingLogged()
        {
            return [.. loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .SelectMany(invocation => Rendered(invocation.Arguments[2]))];
        }

        private static List<string> Rendered(object? state)
        {
            var written = new List<string> { state?.ToString() ?? string.Empty };

            if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
            {
                written.AddRange(properties.Select(property => $"{property.Key}={property.Value}"));
            }

            return written;
        }

        private static TestCaseData[] UnreadableStoredValues()
        {
            return
            [
                new TestCaseData(EnvelopeWithABrokenTag(), ActiveKeyId)
                    .SetName("Decrypt_UnreadableValue_Throws(an envelope whose tag does not verify)"),
                new TestCaseData(EncryptedUnder(UnknownKeyId, OffRingKeyMaterial), UnknownKeyId)
                    .SetName("Decrypt_UnreadableValue_Throws(an envelope naming a key the ring does not hold)"),
                new TestCaseData(LegacyCbcBlob(Credential, OffRingKeyMaterial, UnreadableBlobInitialisationVector), null)
                    .SetName("Decrypt_UnreadableValue_Throws(a legacy blob no key on the ring reads)"),
            ];
        }

        // The tag authenticates the ciphertext, so changing one character of it is the shortest way to
        // write down "this stored value is no longer what was sealed". The first character is chosen
        // because every one of its bits is significant, whereas the last one carries padding bits that a
        // strict base64url decoder would reject outright, turning the value into something that never
        // reaches the tag check at all.
        private static string EnvelopeWithABrokenTag()
        {
            var fields = EncryptedUnder(ActiveKeyId, ActiveKeyMaterial).Split('.');

            fields[CiphertextField] = AlterFirstCharacter(fields[CiphertextField]);

            return string.Join('.', fields);
        }

        private static string EncryptedUnder(string keyId, byte[] keyMaterial)
        {
            return SecretEnvelope.Protect(Credential, keyId, keyMaterial).Format();
        }

        private static string AlterFirstCharacter(string field)
        {
            var altered = field.ToCharArray();
            altered[0] = altered[0] == 'A' ? 'B' : 'A';

            return new string(altered);
        }

        private static EncryptionKeyRing ResolveRing(string configuredKey)
        {
            return RingOf(ResolveInto(configuredKey));
        }

        private static WebApplicationBuilder ResolveInto(string configuredKey)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [EncryptionKeyConfigKey] = configuredKey,
            });

            Backend.Program.EnsureEncryptionKeyRing(builder, ADurableKeyStore());

            return builder;
        }

        // Somewhere the instance would be allowed to keep a key of its own. Every case here supplies one
        // through configuration instead, which wins outright, so nothing is ever written there.
        private static KeyStoreLocation ADurableKeyStore()
        {
            return new KeyStoreLocation(
                Directory.CreateTempSubdirectory("CryptoServiceTests_").FullName,
                KeyStoreCase.ExplicitKeyStorePath);
        }

        private static EncryptionKeyRing RingOf(WebApplicationBuilder builder)
        {
            var holder = (IEncryptionKeyRingHolder)builder.Services
                .Single(descriptor => descriptor.ServiceType == typeof(IEncryptionKeyRingHolder))
                .ImplementationInstance!;

            return holder.Current;
        }

        // Reproduces byte for byte what the previous implementation wrote: base64 of the initialisation
        // vector followed by AES-CBC ciphertext with PKCS7 padding, carrying no version, no key id and no
        // authentication tag.
        private static string LegacyCbcBlob(string plainText, byte[] keyMaterial)
        {
            return LegacyCbcBlob(plainText, keyMaterial, RandomNumberGenerator.GetBytes(IvLength));
        }

        private static string LegacyCbcBlob(string plainText, byte[] keyMaterial, byte[] initialisationVector)
        {
            using var aes = Aes.Create();
            aes.Key = keyMaterial;

            var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), initialisationVector);

            return Convert.ToBase64String([.. initialisationVector, .. cipher]);
        }
    }
}
