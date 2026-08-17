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
        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly SecretStateClassifier classifier;

        private readonly ILogger<CryptoService> logger;

        private readonly SecretReportLog reportLog = new();

        public CryptoService(IEncryptionKeyRingHolder keyRingHolder, ILogger<CryptoService> logger)
        {
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.keyRingHolder = keyRingHolder;
            this.logger = logger;
            classifier = new SecretStateClassifier(keyRingHolder);
        }

        public string Encrypt(string plainText)
        {
            return Encrypt(plainText, keyRingHolder.Current.ActiveKey);
        }

        public string Encrypt(string plainText, EncryptionKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return SecretEnvelope.Protect(plainText, key.Id, key.Material.Span).Format();
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

            if (secret.State == SecretState.Unreadable && reportLog.NotReportedYet(storedValue))
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
        private sealed class SecretReportLog
        {
            private const int RememberedSecretsLimit = 1000;

            private readonly HashSet<string> reported = [];

            private readonly Queue<string> reportedOldestFirst = new();

            private readonly object gate = new();

            public bool NotReportedYet(string storedValue)
            {
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(storedValue)));

                lock (gate)
                {
                    if (!reported.Add(fingerprint))
                    {
                        return false;
                    }

                    reportedOldestFirst.Enqueue(fingerprint);

                    if (reportedOldestFirst.Count > RememberedSecretsLimit)
                    {
                        reported.Remove(reportedOldestFirst.Dequeue());
                    }

                    return true;
                }
            }
        }
    }
}
