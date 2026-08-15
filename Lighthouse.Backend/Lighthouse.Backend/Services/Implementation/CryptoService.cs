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

        // A secret nobody can read is not a secret, and handing the stored bytes back in its place is how a
        // wrong encryption key came to look like a work tracking system rejecting an expired token. Callers
        // get a failure they cannot mistake for a credential.
        public string Decrypt(string cipherText)
        {
            var secret = Read(cipherText);

            return secret.PlainText ?? throw new UnreadableSecretException(secret.State, secret.KeyId);
        }

        public SecretReadResult Read(string storedValue)
        {
            return classifier.Classify(storedValue);
        }
    }
}
