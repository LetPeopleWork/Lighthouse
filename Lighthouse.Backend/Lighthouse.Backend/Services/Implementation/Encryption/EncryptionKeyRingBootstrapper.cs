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

    // What an instance is told when it has nowhere it can keep a key of its own. Both sentences describe the
    // same situation and name the same two ways out of it; the only difference is whether anything is
    // already stored that refusing to start would take away.
    public static class NoDurableKeyStore
    {
        private const string WaysOut =
            "Set Encryption__Key to a key of your own, or set Encryption__KeyStorePath to a directory on a " +
            "volume that outlives this container, and start Lighthouse again.";

        public const string Warning =
            "This instance has nowhere to keep an encryption key that would still be there after a restart, " +
            "so it is running on the key published with the product. That key ships inside every copy of " +
            "Lighthouse and can be read out of the public source, so the credentials stored here are no " +
            "better protected than the source code is. Everything already stored keeps working. " + WaysOut;

        public const string Refusal =
            "This instance has nowhere to keep an encryption key that would still be there after a restart, " +
            "and it has nothing stored yet, so Lighthouse will not start on the key published with the " +
            "product. That key ships inside every copy of Lighthouse and can be read out of the public " +
            "source, so a credential stored under it would not be protected at all. " + WaysOut;
    }

    public sealed class EncryptionKeyRingBootstrapper
    {
        private readonly ConfiguredKeyRingSource configuration;

        private readonly MountedFileKeyRingSource mountedFile;

        private readonly GeneratedKeyRingStore generated;

        private readonly KeyStoreLocation keyStore;

        private readonly IStoredSecretPresenceProbe storedSecrets;

        public EncryptionKeyRingBootstrapper(
            ConfiguredKeyRingSource configuration,
            MountedFileKeyRingSource mountedFile,
            GeneratedKeyRingStore generated,
            KeyStoreLocation keyStore,
            IStoredSecretPresenceProbe storedSecrets)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(mountedFile);
            ArgumentNullException.ThrowIfNull(generated);
            ArgumentNullException.ThrowIfNull(keyStore);
            ArgumentNullException.ThrowIfNull(storedSecrets);

            this.configuration = configuration;
            this.mountedFile = mountedFile;
            this.generated = generated;
            this.keyStore = keyStore;
            this.storedSecrets = storedSecrets;
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
                ?? NoKeyAnywhereYet();
        }

        // The last resort, and the only branch that asks the database anything. A key made where nothing
        // promises to keep it is gone on the next start, taking every secret written under it, so this is
        // where making one stops. Whether that is fatal depends on whether anything is stored yet, and the
        // database is asked only here - so every deployment that got its key from one of the lines above
        // starts without needing the database to be reachable at all.
        private EncryptionKeyRing NoKeyAnywhereYet()
        {
            if (keyStore.MintingIsPermitted)
            {
                return generated.Mint();
            }

            if (storedSecrets.Look() == StoredSecretPresence.HoldsNone)
            {
                throw new InvalidOperationException(NoDurableKeyStore.Refusal);
            }

            return PublishedDefaultOnly();
        }

        // An instance in this position is already on the key published with the product: that is what every
        // secret it holds was written under, and it has no other key it could still read tomorrow. The
        // published key is compiled in as something to add behind an active key, so asking for it on its own
        // means appending it to a ring and taking it straight back off. That ring is thrown away here and
        // never encrypts anything.
        private static EncryptionKeyRing PublishedDefaultOnly()
        {
            var thrownAway = new EncryptionKeyRing(
                new EncryptionKey("k-not-in-use", new byte[EncryptionKey.MaterialLength]));

            return new EncryptionKeyRing(
                KeyCustody.NoDurableStore, thrownAway.WithLegacyDefault().RetiredKeys[0]);
        }
    }
}
