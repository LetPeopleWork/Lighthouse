using Lighthouse.Backend.Models.Encryption;
using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Gives up the stored secrets a key ring cannot read, so a database restored from another installation starts
    // instead of being refused. Only a value the ring cannot read goes: a readable or empty value stays as it is,
    // and so does every row that holds no secret.
    public sealed class UnreadableStoredSecrets
    {
        private readonly SecretStateClassifier classifier;

        public UnreadableStoredSecrets(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            classifier = new SecretStateClassifier(new EncryptionKeyRingHolder(ring));
        }

        public void RemoveFrom(DbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            connection.Open();

            // Read everything before changing anything: removing a row while its table is still being read is not
            // something every provider allows.
            var unreadable = StoredSecretLocations.All
                .SelectMany(location => location.SecretsIn(connection))
                .Where(secret => classifier.Classify(secret.Value).State == SecretState.Unreadable)
                .ToList();

            using var transaction = connection.BeginTransaction();

            foreach (var secret in unreadable)
            {
                secret.Location.GiveUp(connection, transaction, secret);
            }

            transaction.Commit();
        }
    }
}
