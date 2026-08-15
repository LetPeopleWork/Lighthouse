using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Three answers rather than two. The question is asked before the application has a database context,
    // before migrations have created a schema, and - where the database is a server somewhere else - possibly
    // before anything is listening. "I could not tell" is a different answer from "there is nothing stored
    // here", and the two lead to opposite decisions, so the third value exists rather than leaving each
    // caller to invent a meaning for a false.
    public enum StoredSecretPresence
    {
        CannotTell,
        HoldsNone,
        HoldsAtLeastOne,
    }

    public interface IStoredSecretPresenceProbe
    {
        StoredSecretPresence Look();
    }

    // Asks the database directly rather than through Entity Framework, because it runs while the application
    // is still being built and no context exists yet. The connection is handed over as something to make on
    // demand, so the caller decides which provider is being spoken to and a test can watch what becomes of it.
    public sealed class DatabaseSecretPresenceProbe : IStoredSecretPresenceProbe
    {
        private static readonly string[] SecretPresenceQueries =
        [
            """SELECT 1 FROM "WorkTrackingSystemConnectionOption" WHERE "IsSecret" = true AND "Value" IS NOT NULL AND "Value" <> '' LIMIT 1""",
            """SELECT 1 FROM "OAuthCredentials" WHERE ("AccessToken" IS NOT NULL AND "AccessToken" <> '') OR ("RefreshToken" IS NOT NULL AND "RefreshToken" <> '') LIMIT 1""",
        ];

        private readonly Func<DbConnection> connectionFactory;

        public DatabaseSecretPresenceProbe(Func<DbConnection> connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);

            this.connectionFactory = connectionFactory;
        }

        // This is the one read in the whole of encryption that is allowed to fail quietly, and it needs to be.
        // A database that is not up yet, or that has no tables yet, is not an empty database - and reading it
        // as one would refuse to start an instance that has been running for a year, because its server was
        // slow to answer. So anything that goes wrong here means "I could not tell", which the caller treats
        // the same way as "there are secrets here": start, and say loudly which key is in use.
        public StoredSecretPresence Look()
        {
            try
            {
                using var connection = connectionFactory();
                connection.Open();

                return Array.Exists(SecretPresenceQueries, query => AnythingStoredIn(connection, query))
                    ? StoredSecretPresence.HoldsAtLeastOne
                    : StoredSecretPresence.HoldsNone;
            }
            catch (DbException)
            {
                return StoredSecretPresence.CannotTell;
            }
        }

        private static bool AnythingStoredIn(DbConnection connection, string query)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;

            return command.ExecuteScalar() is not null;
        }
    }
}
