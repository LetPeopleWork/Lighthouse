using Microsoft.Data.Sqlite;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public enum KeyStoreCase
    {
        ExplicitKeyStorePath,
        ConfiguredDataProtectionPath,
        BesideTheDatabaseFile,
        DefaultLocationNoDurableStore,
    }

    // Where the key lands, and how that was decided. The two are inseparable: the same directory is a place
    // an operator promised to keep when they named it, and a place nobody vouched for when it was merely
    // fallen back to. Only the second forbids creating a key that a restart could lose.
    public sealed record KeyStoreLocation(string Directory, KeyStoreCase Case)
    {
        public bool MintingIsPermitted => Case != KeyStoreCase.DefaultLocationNoDurableStore;
    }

    public static class KeyStoreResolver
    {
        private const string SqliteProvider = "sqlite";

        private const string DefaultDirectoryName = "data-protection-keys";

        private const string DirectoryNameBesideTheDatabase = "keys";

        private const string InMemoryDataSource = ":memory:";

        private static readonly char[] PathSeparators = ['/', '\\'];

        public static KeyStoreLocation Resolve(
            string? encryptionKeyStorePath,
            string? dataProtectionKeyStorePath,
            string? databaseProvider,
            string? databaseConnectionString,
            string contentRootPath)
        {
            if (!string.IsNullOrWhiteSpace(encryptionKeyStorePath))
            {
                return new KeyStoreLocation(encryptionKeyStorePath, KeyStoreCase.ExplicitKeyStorePath);
            }

            if (!string.IsNullOrWhiteSpace(dataProtectionKeyStorePath))
            {
                return new KeyStoreLocation(dataProtectionKeyStorePath, KeyStoreCase.ConfiguredDataProtectionPath);
            }

            var databaseDirectory = DirectoryHoldingTheDatabaseFile(
                databaseProvider, databaseConnectionString, contentRootPath);

            return databaseDirectory is null
                ? new KeyStoreLocation(
                    Path.Combine(contentRootPath, DefaultDirectoryName),
                    KeyStoreCase.DefaultLocationNoDurableStore)
                : new KeyStoreLocation(
                    Path.Combine(databaseDirectory, DirectoryNameBesideTheDatabase),
                    KeyStoreCase.BesideTheDatabaseFile);
        }

        // Null whenever the deployment has no database file to sit beside at all: anything that is not
        // SQLite, and an in-memory database.
        private static string? DirectoryHoldingTheDatabaseFile(
            string? databaseProvider,
            string? databaseConnectionString,
            string contentRootPath)
        {
            if (!string.Equals(databaseProvider, SqliteProvider, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(databaseConnectionString))
            {
                return null;
            }

            // The same parser the database itself is opened with, so the key cannot end up beside a file the
            // application never opens.
            var dataSource = new SqliteConnectionStringBuilder(databaseConnectionString).DataSource;

            if (string.IsNullOrWhiteSpace(dataSource) || IsHeldInMemory(dataSource))
            {
                return null;
            }

            // A path that names no directory is written wherever the application runs from, which for a
            // container is a filesystem that dies with the container. A key beside such a database is exactly
            // as short-lived as the database, so destroying one destroys the other and no secret is ever left
            // behind unreadable - which is the only thing keeping the key elsewhere would protect against.
            var databaseFilePath = IsAbsolute(dataSource)
                ? dataSource
                : Path.Combine(contentRootPath, dataSource);

            return ParentDirectoryOf(databaseFilePath);
        }

        private static bool IsHeldInMemory(string dataSource)
        {
            return string.Equals(dataSource, InMemoryDataSource, StringComparison.OrdinalIgnoreCase);
        }

        // Both path conventions are recognised by reading the text rather than by asking the operating
        // system, which only knows its own. Asking would let one configuration resolve differently on two
        // machines: a Windows path read on Linux is not recognised as absolute, so it would be joined onto
        // the content root and the key would land beside a database that is not there.
        private static bool IsAbsolute(string dataSource)
        {
            return dataSource.StartsWith('/')
                || dataSource.StartsWith('\\')
                || HasDriveLetterPrefix(dataSource);
        }

        private static bool HasDriveLetterPrefix(string dataSource)
        {
            return dataSource.Length > 2
                && char.IsAsciiLetter(dataSource[0])
                && dataSource[1] == ':'
                && dataSource[2] is '/' or '\\';
        }

        private static string? ParentDirectoryOf(string dataSource)
        {
            var lastSeparator = dataSource.LastIndexOfAny(PathSeparators);

            return lastSeparator switch
            {
                < 0 => null,
                0 => dataSource[..1],
                _ => dataSource[..lastSeparator],
            };
        }
    }
}
