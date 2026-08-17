using Lighthouse.Backend.Models.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The store is handed the filesystem it works on rather than calling File itself, because the failure it
    // exists to catch is a filesystem that accepts a write, reports success and hands back something else -
    // and no filesystem a test can reach behaves that way on demand.
    public interface IKeyStoreFileSystem
    {
        bool FileExists(string path);

        byte[] ReadAllBytes(string path);

        void WriteAllBytes(string path, byte[] contents);

        void Move(string sourcePath, string destinationPath);
    }

    public sealed class PhysicalKeyStoreFileSystem : IKeyStoreFileSystem
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public void WriteAllBytes(string path, byte[] contents)
        {
            KeyStoreFile.Write(path, contents);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath, overwrite: true);
        }
    }

    // The key this instance made for itself, kept where only this instance can read it: the file is wrapped
    // with Data Protection, whose own keys sit in the same directory.
    public sealed class GeneratedKeyRingStore
    {
        public const string RingFileName = "encryption-keyring.protected";

        public const string TemporaryFileSuffix = ".writing";

        private const int MostKeysMadeInOneDay = 99;

        private const string ProtectorPurpose = "Lighthouse.Encryption.KeyRing.v1";

        private readonly IKeyStoreFileSystem fileSystem;

        private readonly IDataProtector protector;

        private readonly TimeProvider timeProvider;

        public GeneratedKeyRingStore(
            string keyStoreDirectory,
            IDataProtectionProvider dataProtection,
            IKeyStoreFileSystem fileSystem,
            TimeProvider timeProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyStoreDirectory);
            ArgumentNullException.ThrowIfNull(dataProtection);
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.fileSystem = fileSystem;
            this.timeProvider = timeProvider;

            protector = dataProtection.CreateProtector(ProtectorPurpose);
            RingFilePath = Path.Combine(keyStoreDirectory, RingFileName);
        }

        public string RingFilePath { get; }

        public EncryptionKeyRing? ReadExisting()
        {
            if (!fileSystem.FileExists(RingFilePath))
            {
                return null;
            }

            var stored = Unwrapped(fileSystem.ReadAllBytes(RingFilePath))
                ?? throw new InvalidOperationException(
                    $"The encryption key file '{RingFilePath}' is there and could not be read with this " +
                    "instance's data protection keys. Lighthouse will not write a replacement, because a new " +
                    "key would leave every secret already stored unreadable. Restore the key store that " +
                    "belongs to this database, or supply the key through Encryption__Key, and start " +
                    "Lighthouse again.");

            return SuppliedKeyRing.ParsedFrom(
                stored, KeyCustody.GeneratedForThisInstance, $"the file '{RingFilePath}'");
        }

        // The wrapping keys that protect the ring file live on disk beside it, which is not necessarily
        // where the application keeps its own - a deployment with more than one replica keeps those in Redis
        // so that a cookie issued by one pod can be read by another. Wrapping the ring with those instead
        // would write a file the next start cannot unwrap.
        public static ServiceProvider ProtectionKeptBesideTheKeyStore(string keyStoreDirectory)
        {
            return new ServiceCollection()
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoreDirectory))
                .Services
                .BuildServiceProvider();
        }

        public EncryptionKeyRing Mint()
        {
            return Persisted(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey(existing: null)));
        }

        // The new key goes in front and everything that was there goes behind it, so what was written under
        // the old keys stays readable while nothing is ever written under them again.
        public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
        {
            ArgumentNullException.ThrowIfNull(existing);

            return Persisted(new EncryptionKeyRing(
                existing.Custody,
                [NewKey(existing), existing.ActiveKey, .. existing.RetiredKeys]));
        }

        private EncryptionKeyRing Persisted(EncryptionKeyRing ring)
        {
            Write(KeyRingSerializer.Format(ring));

            return ring;
        }

        private EncryptionKey NewKey(EncryptionKeyRing? existing)
        {
            return new EncryptionKey(UnusedKeyId(existing), RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));
        }

        // The name says when the key was made, so an operator reading two of them can tell which is which,
        // and it counts up within the day because rotating twice in one afternoon is exactly what someone
        // containing an exposure does - and a ring naming one key twice cannot be spelled at all.
        private string UnusedKeyId(EncryptionKeyRing? existing)
        {
            var madeOn = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            for (var madeTodayBefore = 1; madeTodayBefore <= MostKeysMadeInOneDay; madeTodayBefore++)
            {
                var name = string.Create(CultureInfo.InvariantCulture, $"k-{madeOn}-{madeTodayBefore:00}");

                if (existing is null || !existing.TryGet(name, out _))
                {
                    return name;
                }
            }

            throw new MintingNotPermittedException(
                $"This instance has already made {MostKeysMadeInOneDay} keys today, and every name a key made " +
                "today could be given is taken. Nothing is wrong with the keys it has and nothing has been " +
                "changed; rotating again is worth waiting until tomorrow for.");
        }

        // Written aside and then moved into place, so a process that dies part-way leaves the key that was
        // already there rather than half of a new one. Then read straight back, unwrapped and compared:
        // some filesystems accept a write, report success and hand back something else afterwards, and a key
        // that cannot be read back tomorrow takes every secret written under it today with it. Better to
        // refuse to start than to encrypt anything under a key this machine may not keep.
        private void Write(string canonicalRing)
        {
            // Named apart per write, so two processes sharing a key store cannot each write the other's
            // bytes into one staging file and then accuse the filesystem of losing what it was given.
            var temporaryPath = $"{RingFilePath}.{Guid.NewGuid():n}{TemporaryFileSuffix}";

            fileSystem.WriteAllBytes(temporaryPath, protector.Protect(Encoding.UTF8.GetBytes(canonicalRing)));
            fileSystem.Move(temporaryPath, RingFilePath);

            var readBack = Unwrapped(fileSystem.ReadAllBytes(RingFilePath));

            if (!string.Equals(readBack, canonicalRing, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The encryption key was written to '{RingFilePath}' and did not read back as what was " +
                    "written, so this filesystem cannot be trusted to keep it. Lighthouse will not start on a " +
                    "key it may lose, because every secret written under it would become unreadable. Put the " +
                    "key store on storage that keeps what it is given, or supply the key through " +
                    "Encryption__Key, and start Lighthouse again.");
            }
        }

        private string? Unwrapped(byte[] stored)
        {
            try
            {
                return Encoding.UTF8.GetString(protector.Unprotect(stored));
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}
