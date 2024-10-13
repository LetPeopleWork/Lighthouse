using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class CryptoServiceTests
    {
        private CryptoService subject;

        [SetUp]
        public void SetUp()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "EncryptionSettings:EncryptionKey", "aXhZdXd5+OeT8kjKP2gB7UdqMEB3RY4LQMI2yffxDEw=\r\n" }
            };

            var configuation = TestConfiguration.SetupTestConfiguration(inMemorySettings);

            subject = new CryptoService(configuation);
        }

        [Test]
        public void Encrypt_ShouldEncryptText()
        {
            var plainText = "Hello, World!";

            var encryptedText = subject.Encrypt(plainText);

            Assert.That(encryptedText, Is.Not.EqualTo(plainText));
        }

        [Test]
        public void Decrypt_ShouldDecryptText()
        {
            var plainText = "Hello, World!";
            var encryptedText = subject.Encrypt(plainText);

            var decryptedText = subject.Decrypt(encryptedText);

            Assert.That(decryptedText, Is.EqualTo(plainText));
        }

        [Test]
        public void Decrypt_InvalidCipherText_ShouldReturnOriginalText()
        {
            var invalidCipherText = "invalid_base64_string";

            var result = subject.Decrypt(invalidCipherText);

            Assert.That(result, Is.EqualTo(invalidCipherText));
        }

        [Test]
        public void Constructor_InvalidKey_ShouldThrowException()
        {
            var invalidBase64Key = "short_key";

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "EncryptionSettings:EncryptionKey", invalidBase64Key }
            };

            var configuation = TestConfiguration.SetupTestConfiguration(inMemorySettings);

            Assert.Throws<FormatException>(() => new CryptoService(configuation));
        }
    }
}
