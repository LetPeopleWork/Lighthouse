using Lighthouse.Backend.Models.Encryption;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // A key an external secret store owns and mounts as a file. The whole content of that file is the ring,
    // written across as many lines as whatever produced it felt like using.
    public sealed class MountedFileKeyRingSource
    {
        public const string PathSettingKey = "Encryption:KeysFile";

        private readonly string? keysFilePath;

        private readonly IKeyStoreFileSystem fileSystem;

        public MountedFileKeyRingSource(string? keysFilePath, IKeyStoreFileSystem fileSystem)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);

            this.keysFilePath = keysFilePath;
            this.fileSystem = fileSystem;
        }

        public EncryptionKeyRing? Resolve()
        {
            return ReadContents() is { } contents ? RingFrom(contents) : null;
        }

        // Reading and parsing are separate because the reload asks them separately: it re-reads on a timer and
        // has to be able to tell content it has already judged from content it has not, without parsing every
        // half minute and without a second copy of what counts as a readable file.
        public string? ReadContents()
        {
            if (string.IsNullOrWhiteSpace(keysFilePath))
            {
                return null;
            }

            if (!fileSystem.FileExists(keysFilePath))
            {
                throw new InvalidOperationException(
                    $"Encryption__KeysFile names the file '{keysFilePath}', and there is no file there. " +
                    "Lighthouse will not fall back to a key of its own, because that file would win again as " +
                    "soon as it appeared and everything written in the meantime would be unreadable. " +
                    "Mount the file, or remove the setting, and start Lighthouse again.");
            }

            return Encoding.UTF8.GetString(fileSystem.ReadAllBytes(keysFilePath)).Trim();
        }

        public EncryptionKeyRing RingFrom(string contents)
        {
            return SuppliedKeyRing.ParsedFrom(
                contents, KeyCustody.SuppliedByExternalSecret, $"the file '{keysFilePath}'");
        }
    }
}
