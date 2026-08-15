using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public static class LegacyDefaultEncryptionKey
    {
        public const string Id = "k-legacy-default";

        // This value is not a secret and never was. Every Lighthouse build before this release shipped it in
        // appsettings.json, so it is in every copy of the product and in the public source repository, and any
        // secret those builds stored is encrypted under it. It is compiled in here for one reason: so those
        // secrets stay readable after the upgrade. It is only ever added to a key ring behind whatever key is
        // already active, so nothing is ever encrypted under it again.
#pragma warning disable S6418
        private const string PublishedMaterial = "uH2VbF5hOW0/huLOH1Q2L0g+P3J9dG43cknQK7t9R5M=";
#pragma warning restore S6418

        public static EncryptionKeyRing AppendedTo(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            return ring.WithRetired(new EncryptionKey(Id, Convert.FromBase64String(PublishedMaterial)));
        }
    }
}
