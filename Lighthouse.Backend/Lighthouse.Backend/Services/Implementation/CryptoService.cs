using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;

namespace Lighthouse.Backend.Services.Implementation
{
    public class CryptoService : ICryptoService
    {
        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly SecretStateClassifier classifier;

        public CryptoService(IEncryptionKeyRingHolder keyRingHolder)
        {
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.keyRingHolder = keyRingHolder;
            classifier = new SecretStateClassifier(keyRingHolder);
        }

        public string Encrypt(string plainText)
        {
            var activeKey = keyRingHolder.Current.ActiveKey;

            return SecretEnvelope.Protect(plainText, activeKey.Id, activeKey.Material.Span).Format();
        }

        public string Decrypt(string cipherText)
        {
            // This is the last version in which a stored secret nobody can decrypt is still handed back
            // exactly as it was found, which is what keeps this change invisible to everything already
            // working. The next one turns that same case into a failure instead of a credential
            return Read(cipherText).PlainText ?? cipherText;
        }

        public SecretReadResult Read(string storedValue)
        {
            return classifier.Classify(storedValue);
        }
    }
}
