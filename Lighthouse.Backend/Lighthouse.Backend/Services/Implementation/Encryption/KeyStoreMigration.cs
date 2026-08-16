namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public sealed record KeyStoreMigrationOutcome(
        bool ContentsWereCarriedOver,
        string LegacyDirectory,
        string ResolvedDirectory);

    // Earlier versions kept the key store under the application directory, which on a container is the
    // writable layer a recreate throws away. Moving it is therefore a read-both, write-new step: an
    // existing key store is carried across, never ignored and never replaced by a fresh one, because a
    // fresh one leaves every stored secret unreadable with nothing to point at as the cause.
    //
    // The old location is also the one an instance with no database file to sit beside keeps its keys in
    // today, so finding a populated directory there is not evidence of a rival instance - the same
    // deployment run once on Postgres and once on SQLite fills both. What names a key store is its key
    // ring, and two rings that are not the same key cannot both belong to this database. That is the one
    // case worth refusing to start over; everything else is carried across, which can only make more of
    // what is already stored readable.
    public static class KeyStoreMigration
    {
        public static KeyStoreMigrationOutcome CarryOverLegacyKeyStore(string resolvedDirectory, string legacyDirectory)
        {
            var nothingCarriedOver = new KeyStoreMigrationOutcome(false, legacyDirectory, resolvedDirectory);

            if (string.Equals(resolvedDirectory, legacyDirectory, StringComparison.Ordinal))
            {
                return nothingCarriedOver;
            }

            var legacyContents = ContentsOf(legacyDirectory);
            if (legacyContents.Count == 0)
            {
                return nothingCarriedOver;
            }

            var resolvedContents = ContentsOf(resolvedDirectory);

            RefuseWhenEachHoldsADifferentKeyRing(
                legacyDirectory, legacyContents, resolvedDirectory, resolvedContents);

            if (Holds(resolvedContents, GeneratedKeyRingStore.RingFileName))
            {
                return nothingCarriedOver;
            }

            var missingFromResolved = legacyContents
                .Where(relativePath => !Holds(resolvedContents, relativePath))
                .ToList();

            if (missingFromResolved.Count == 0)
            {
                return nothingCarriedOver;
            }

            CopyAcross(legacyDirectory, resolvedDirectory, missingFromResolved);

            return new KeyStoreMigrationOutcome(true, legacyDirectory, resolvedDirectory);
        }

        private static void RefuseWhenEachHoldsADifferentKeyRing(
            string legacyDirectory,
            List<string> legacyContents,
            string resolvedDirectory,
            List<string> resolvedContents)
        {
            var ring = GeneratedKeyRingStore.RingFileName;

            if (!Holds(legacyContents, ring) || !Holds(resolvedContents, ring))
            {
                return;
            }

            if (AreIdentical(Path.Combine(legacyDirectory, ring), Path.Combine(resolvedDirectory, ring)))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Two key rings were found and they are not the same key: '{legacyDirectory}' and '{resolvedDirectory}' each hold a '{ring}'. " +
                "Lighthouse will not choose between them, because the wrong choice leaves every stored secret unreadable. " +
                "Keep the store that belongs to this instance, move the other one elsewhere, and start Lighthouse again.");
        }

        private static bool Holds(List<string> contents, string relativePath)
        {
            return contents.Contains(relativePath, StringComparer.Ordinal);
        }

        private static void CopyAcross(string legacyDirectory, string resolvedDirectory, List<string> contents)
        {
            Directory.CreateDirectory(resolvedDirectory);

            foreach (var relativePath in contents)
            {
                var destination = Path.Combine(resolvedDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(legacyDirectory, relativePath), destination, overwrite: false);
            }
        }

        private static bool AreIdentical(string oneFile, string another)
        {
            return File.ReadAllBytes(oneFile).AsSpan().SequenceEqual(File.ReadAllBytes(another));
        }

        // A key store is a flat set of files today, but a stray subdirectory would be keys nobody carried
        // across, so the whole tree counts.
        private static List<string> ContentsOf(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return [];
            }

            return Directory
                .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(directory, file))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
    }
}
