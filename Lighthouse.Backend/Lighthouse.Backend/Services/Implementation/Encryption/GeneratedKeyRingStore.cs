using Lighthouse.Backend.Models.Encryption;
using Microsoft.AspNetCore.DataProtection;
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
            File.WriteAllBytes(path, contents);
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

        public EncryptionKeyRing Mint()
        {
            var ring = new EncryptionKeyRing(
                KeyCustody.GeneratedForThisInstance,
                new EncryptionKey(MintedKeyId(), RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength)));

            Write(KeyRingSerializer.Format(ring));

            return ring;
        }

        // The name says when the key was made, so an operator reading two of them can tell which is which.
        private string MintedKeyId()
        {
            var madeOn = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return $"k-{madeOn}-01";
        }

        // Written aside and then moved into place, so a process that dies part-way leaves the key that was
        // already there rather than half of a new one. Then read straight back, unwrapped and compared:
        // some filesystems accept a write, report success and hand back something else afterwards, and a key
        // that cannot be read back tomorrow takes every secret written under it today with it. Better to
        // refuse to start than to encrypt anything under a key this machine may not keep.
        private void Write(string canonicalRing)
        {
            var temporaryPath = RingFilePath + TemporaryFileSuffix;

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
