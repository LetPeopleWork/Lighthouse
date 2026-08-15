using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation
{
    public class CryptoService : ICryptoService
    {
        private const int RememberedSecretsLimit = 1000;

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly SecretStateClassifier classifier;

        private readonly ILogger<CryptoService> logger;

        private readonly HashSet<string> reportedSecrets = [];

        private readonly Queue<string> reportedSecretsOldestFirst = new();

        private readonly object reportedSecretsGate = new();

        public CryptoService(IEncryptionKeyRingHolder keyRingHolder, ILogger<CryptoService> logger)
        {
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.keyRingHolder = keyRingHolder;
            this.logger = logger;
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
            var secret = classifier.Classify(storedValue);

            if (secret.State == SecretState.Unreadable && NotReportedYet(storedValue))
            {
                logger.LogWarning(
                    "A stored secret cannot be read. State: {SecretState}. Key it names: {ClaimedKeyId}.",
                    secret.State,
                    secret.KeyId);
            }

            return secret;
        }

        // A failing sync retries, so without this the same broken secret would fill the log with a
        // thousand copies of one sentence and bury everything else. What is remembered is a hash, because
        // the stored value itself must never be held anywhere it could be read out or written down, and
        // the count is capped because the values arriving here are whatever is in the database.
        private bool NotReportedYet(string storedValue)
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(storedValue)));

            lock (reportedSecretsGate)
            {
                if (!reportedSecrets.Add(fingerprint))
                {
                    return false;
                }

                reportedSecretsOldestFirst.Enqueue(fingerprint);

                if (reportedSecretsOldestFirst.Count > RememberedSecretsLimit)
                {
                    reportedSecrets.Remove(reportedSecretsOldestFirst.Dequeue());
                }

                return true;
            }
        }
    }
}
