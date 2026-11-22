using System.Data.Common;

namespace Lighthouse.Backend.Platform.Encryption
{
    // One column that holds an encrypted secret, and what giving up a value in it means: a Connection option keeps its
    // row with the value emptied, so the Connection asks for the token again, while an OAuth credential is useless
    // without either of its tokens and goes as a whole row.
    public sealed class StoredSecretLocation
    {
        private readonly string query;

        private readonly string removal;

        private StoredSecretLocation(string table, string column, string onlyRowsWhere, string removal)
        {
            query = $"""SELECT "{column}", "Id" FROM "{table}" WHERE {onlyRowsWhere}"{column}" IS NOT NULL AND "{column}" <> ''""";
            this.removal = removal;
        }

        public static StoredSecretLocation EmptiedWhenGivenUp(string table, string column, string onlyRowsWhere)
            => new(table, column, onlyRowsWhere, $"""UPDATE "{table}" SET "{column}" = '' WHERE "Id" = @id""");

        public static StoredSecretLocation DeletedWhenGivenUp(string table, string column)
            => new(table, column, string.Empty, $"""DELETE FROM "{table}" WHERE "Id" = @id""");

        public IEnumerable<StoredSecret> SecretsIn(DbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            return ReadFrom(connection);
        }

        public void GiveUp(DbConnection connection, DbTransaction transaction, StoredSecret secret)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(transaction);
            ArgumentNullException.ThrowIfNull(secret);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = removal;

            var id = command.CreateParameter();
            id.ParameterName = "@id";
            id.Value = secret.RowId;
            command.Parameters.Add(id);

            command.ExecuteNonQuery();
        }

        private IEnumerable<StoredSecret> ReadFrom(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    yield return new StoredSecret(this, reader.GetInt32(1), reader.GetString(0));
                }
            }
        }
    }

    public sealed record StoredSecret(StoredSecretLocation Location, int RowId, string Value);

    // Every column a stored secret lives in. The start-up check and the removal after a restore both read this list,
    // so a secret column added here is checked and removed alike.
    public static class StoredSecretLocations
    {
        public static IReadOnlyList<StoredSecretLocation> All { get; } =
        [
            StoredSecretLocation.EmptiedWhenGivenUp("WorkTrackingSystemConnectionOption", "Value", "\"IsSecret\" = true AND "),
            StoredSecretLocation.DeletedWhenGivenUp("OAuthCredentials", "AccessToken"),
            StoredSecretLocation.DeletedWhenGivenUp("OAuthCredentials", "RefreshToken"),
        ];
    }
}
