using Lighthouse.Backend.Models.Encryption;
using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Four answers, for the same reason the presence probe has three: the question is asked while the
    // application is still being built, so "I could not ask" has to be tellable apart from "I asked and the
    // answer is none". Only one of these is worth stopping a start over.
    public enum StoredSecretReadability
    {
        CannotTell,
        NothingStored,
        SomethingReadable,
        NothingReadable,
    }

    public interface IStoredSecretReadabilityProbe
    {
        StoredSecretReadability Look(EncryptionKeyRing ring);
    }

    // An instance that starts on the wrong key looks exactly like an instance that starts on the right one:
    // the banner is cheerful, every page loads, and the first anyone hears of it is a sync failing hours
    // later against a credential nobody touched. This is the one question that tells those two apart, and it
    // can only be asked once the key is known - so it is asked after resolution rather than during it.
    public static class KeyThatReadsNothing
    {
        public const string Refusal =
            "This instance has stored credentials and not one of them can be read with the key it started " +
            "on, so this is not the key they were written under. Nothing has been changed and nothing is " +
            "lost - the credentials are still there, encrypted under the key they were written with. Set " +
            "Encryption__Key to the key this instance was using before, or set Encryption__KeyStorePath to " +
            "the key store that belongs to this database, and start Lighthouse again.";
    }

    // Reads the stored values through a raw connection for the same reason the presence probe does: there is
    // no database context yet. Every secret is asked about, because the answer that matters is whether even
    // one of them can still be read - one is enough to say the key is the right one.
    public sealed class DatabaseSecretReadabilityProbe : IStoredSecretReadabilityProbe
    {
        private static readonly string[] StoredSecretQueries =
        [
            """SELECT "Value" FROM "WorkTrackingSystemConnectionOption" WHERE "IsSecret" = true AND "Value" IS NOT NULL AND "Value" <> ''""",
            """SELECT "AccessToken" FROM "OAuthCredentials" WHERE "AccessToken" IS NOT NULL AND "AccessToken" <> ''""",
            """SELECT "RefreshToken" FROM "OAuthCredentials" WHERE "RefreshToken" IS NOT NULL AND "RefreshToken" <> ''""",
        ];

        private readonly Func<DbConnection> connectionFactory;

        public DatabaseSecretReadabilityProbe(Func<DbConnection> connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);

            this.connectionFactory = connectionFactory;
        }

        public StoredSecretReadability Look(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            var classifier = new SecretStateClassifier(new EncryptionKeyRingHolder(ring));

            try
            {
                using var connection = connectionFactory();
                connection.Open();

                var stored = 0;

                foreach (var value in StoredSecretQueries.SelectMany(query => ValuesFrom(connection, query)))
                {
                    stored++;

                    if (classifier.Classify(value).State != SecretState.Unreadable)
                    {
                        return StoredSecretReadability.SomethingReadable;
                    }
                }

                return stored == 0 ? StoredSecretReadability.NothingStored : StoredSecretReadability.NothingReadable;
            }
            // A database that will not answer is not an answer. It refuses in two different ways - the
            // server saying no, and there being no server configured to ask at all - and this question is
            // asked on every start, so treating either as "nothing here can be read" would stop instances
            // that are perfectly fine.
            catch (Exception failedToAsk) when (failedToAsk is DbException or InvalidOperationException)
            {
                return StoredSecretReadability.CannotTell;
            }
        }

        private static IEnumerable<string> ValuesFrom(DbConnection connection, string query)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    yield return reader.GetString(0);
                }
            }
        }
    }
}
