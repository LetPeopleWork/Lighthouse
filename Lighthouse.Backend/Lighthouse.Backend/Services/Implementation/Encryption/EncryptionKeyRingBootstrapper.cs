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

            if (LegacyDefaultEncryptionKey.Matches(parsed.ActiveKey.Material.Span))
            {
                throw new InvalidOperationException(PublishedKeyAsTheActiveKey.RefusalFrom(source));
            }

            return new EncryptionKeyRing(custody, [parsed.ActiveKey, .. parsed.RetiredKeys]);
        }
    }

    // What an instance is told when the key it was handed to write with is the key published with the
    // product. Said at the only moment it can still be said: everything about such an instance looks
    // healthy afterwards, including the panel that exists to warn about exactly this, because the name a
    // supplied key wears is derived from its own bytes and matches nothing anybody thought to check.
    // The refusal is about the first entry of a ring, which is the only one anything is written under.
    // Behind an active key the same material stays welcome, and has to: it is how an instance that
    // upgrades keeps reading what it already stored.
    public static class PublishedKeyAsTheActiveKey
    {
        public static string RefusalFrom(string source)
        {
            return
                $"The encryption key supplied by {source} is the key published with Lighthouse. That key " +
                "ships inside every copy of the product and can be read out of the public source, so a " +
                "credential written under it would not be protected at all, and Lighthouse will not " +
                "start on it. Nothing has been changed and nothing is lost: everything already stored " +
                "stays readable, because Lighthouse always keeps that key for reading. Set " +
                "Encryption__Key to a key of your own, or remove the setting and let Lighthouse make " +
                "one, and start Lighthouse again.";
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
        // Long on purpose. An operator pasting this into a compose file at the worst moment of their week
        // should not be able to mistake it for a repair: it starts an instance that still cannot read a
        // single one of its credentials.
        public const string StartAnywaySettingKey = "Encryption:StartEvenIfNothingStoredCanBeRead";

        private readonly ConfiguredKeyRingSource configuration;

        private readonly MountedFileKeyRingSource mountedFile;

        private readonly GeneratedKeyRingStore generated;

        private readonly KeyStoreLocation keyStore;

        private readonly IStoredSecretPresenceProbe storedSecrets;

        private readonly IStoredSecretReadabilityProbe storedSecretReadability;

        private readonly bool startEvenIfNothingStoredCanBeRead;

        public EncryptionKeyRingBootstrapper(
            ConfiguredKeyRingSource configuration,
            MountedFileKeyRingSource mountedFile,
            GeneratedKeyRingStore generated,
            KeyStoreLocation keyStore,
            IStoredSecretPresenceProbe storedSecrets,
            IStoredSecretReadabilityProbe storedSecretReadability,
            bool startEvenIfNothingStoredCanBeRead = false)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(mountedFile);
            ArgumentNullException.ThrowIfNull(generated);
            ArgumentNullException.ThrowIfNull(keyStore);
            ArgumentNullException.ThrowIfNull(storedSecrets);
            ArgumentNullException.ThrowIfNull(storedSecretReadability);

            this.configuration = configuration;
            this.mountedFile = mountedFile;
            this.generated = generated;
            this.keyStore = keyStore;
            this.storedSecrets = storedSecrets;
            this.storedSecretReadability = storedSecretReadability;
            this.startEvenIfNothingStoredCanBeRead = startEvenIfNothingStoredCanBeRead;
        }

        // The order is the whole design, and it is written down once, here. Note that a key someone else
        // supplied does not merely win - it stops Lighthouse from creating one at all. An instance that made
        // its own key while an operator was also supplying one would lose that argument again on the very
        // next start, because the supplied key comes first here too, and by then every secret written under
        // the key it made would be unreadable.
        // Anything that goes wrong on the way out is raised, never swallowed: an instance carrying on
        // without the key it believes it has fails later, somewhere unrelated, with nothing to point at.
        // Whichever line answered, the key published with the product goes on the end afterwards. Every
        // Lighthouse before this release encrypted with it, so an instance that upgrades has to be able to
        // read what it already stored; the end of the ring is a place it can only ever read from.
        public EncryptionKeyRing Resolve()
        {
            var resolved = (configuration.Resolve()
                ?? mountedFile.Resolve()
                ?? generated.ReadExisting()
                ?? NoKeyAnywhereYet())
                .WithLegacyDefault();

            RefuseWhenNothingStoredCanBeRead(resolved);

            return resolved;
        }

        // Asked last, because it can only be asked once there is a key to ask about, and it cannot change
        // which key was chosen - it only says whether that key was the right one. One readable secret is
        // enough to answer yes; anything the database will not tell us is not an answer and does not stop a
        // start, for the same reason the presence probe is allowed to fail quietly.
        //
        // This is the one refusal an operator can be let past, and it is let past here rather than around
        // Resolve, so that every other refusal keeps firing - a switch that let past all of them would be
        // a way to run with no protection at all rather than a way back into a locked room. Where it is
        // set, the question is not asked either: the answer cannot change which key was resolved or what
        // happens next, and asking costs a read of every stored secret on the instance least able to
        // afford one.
        private void RefuseWhenNothingStoredCanBeRead(EncryptionKeyRing resolved)
        {
            if (startEvenIfNothingStoredCanBeRead)
            {
                return;
            }

            var finding = storedSecretReadability.Look(resolved);

            if (finding.Readability != StoredSecretReadability.NothingReadable)
            {
                return;
            }

            throw new InvalidOperationException(KeyThatReadsNothing.RefusalFor(resolved, finding.KeyIdsSeen));
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
