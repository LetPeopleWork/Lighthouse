using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Every transport hands over the same one-line spelling of a ring, so every transport parses it the same
    // way, and only the answer to "who is keeping this key?" differs between them.
    internal static class SuppliedKeyRing
    {
        public static EncryptionKeyRing ParsedFrom(string supplied, KeyCustody custody, string source)
        {
            if (!KeyRingSerializer.TryParse(supplied, out var parsed, out var defect))
            {
                throw new InvalidOperationException($"{defect} It was supplied by {source}.");
            }

            return new EncryptionKeyRing(custody, [parsed.ActiveKey, .. parsed.RetiredKeys]);
        }
    }

    public sealed class EncryptionKeyRingBootstrapper
    {
        private readonly ConfiguredKeyRingSource configuration;

        private readonly MountedFileKeyRingSource mountedFile;

        private readonly GeneratedKeyRingStore generated;

        public EncryptionKeyRingBootstrapper(
            ConfiguredKeyRingSource configuration,
            MountedFileKeyRingSource mountedFile,
            GeneratedKeyRingStore generated)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(mountedFile);
            ArgumentNullException.ThrowIfNull(generated);

            this.configuration = configuration;
            this.mountedFile = mountedFile;
            this.generated = generated;
        }

        // The order is the whole design, and it is written down once, here. Note that a key someone else
        // supplied does not merely win - it stops Lighthouse from creating one at all. An instance that made
        // its own key while an operator was also supplying one would lose that argument again on the very
        // next start, because the supplied key comes first here too, and by then every secret written under
        // the key it made would be unreadable.
        // Anything that goes wrong on the way out is raised, never swallowed: an instance carrying on
        // without the key it believes it has fails later, somewhere unrelated, with nothing to point at.
        public EncryptionKeyRing Resolve()
        {
            return configuration.Resolve()
                ?? mountedFile.Resolve()
                ?? generated.ReadExisting()
                ?? generated.Mint();
        }
    }
}
