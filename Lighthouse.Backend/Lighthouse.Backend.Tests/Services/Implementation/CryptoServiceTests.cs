using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class CryptoServiceTests
    {
        private const string ConfiguredKeyBase64 = "aXhZdXd5+OeT8kjKP2gB7UdqMEB3RY4LQMI2yffxDEw=";

        private const string AnotherKeyBase64 = "jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=";

        private const string EncryptionKeyConfigKey = "EncryptionSettings:EncryptionKey";

        private const string DerivedKeyIdPrefix = "k-cfg-";

        private const int DerivedKeyIdLength = 14;

        private const string ActiveKeyId = "key-active";

        private const string Credential = "Hello, World!";

        private const int KeyIdField = 1;

        private const int IvLength = 16;

        private static readonly byte[] ActiveKeyMaterial = Convert.FromBase64String(ConfiguredKeyBase64);

        private CryptoService subject;

        [SetUp]
        public void SetUp()
        {
            var ring = new EncryptionKeyRing(new EncryptionKey(ActiveKeyId, ActiveKeyMaterial));

            subject = new CryptoService(new EncryptionKeyRingHolder(ring));
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

        [Test]
        public void Decrypt_InvalidCipherText_ShouldReturnOriginalText()
        {
            var invalidCipherText = "invalid_base64_string";

            var result = subject.Decrypt(invalidCipherText);

            Assert.That(result, Is.EqualTo(invalidCipherText));
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
        public void EnsureEncryptionKeyRing_ShouldRegisterTheConfiguredKeyAsTheOnlyActiveKey()
        {
            var ring = ResolveRing(ConfiguredKeyBase64);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(ActiveKeyMaterial));
                Assert.That(ring.RetiredKeys, Is.Empty);
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
            Assert.Throws<FormatException>(() => ResolveInto("short_key"));
        }

        [Test]
        public void EnsureEncryptionKeyRing_ConfiguredKeyIsTheWrongLength_ShouldThrow()
        {
            var tooShort = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            Assert.Throws<InvalidOperationException>(() => ResolveInto(tooShort));
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

            Backend.Program.EnsureEncryptionKeyRing(builder);

            return builder;
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
            using var aes = Aes.Create();
            aes.Key = keyMaterial;

            var initialisationVector = RandomNumberGenerator.GetBytes(IvLength);
            var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), initialisationVector);

            return Convert.ToBase64String([.. initialisationVector, .. cipher]);
        }
    }
}
