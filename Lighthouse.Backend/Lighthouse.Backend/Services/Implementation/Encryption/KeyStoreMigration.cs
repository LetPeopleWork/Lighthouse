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
            if (resolvedContents.Count == 0)
            {
                CopyAcross(legacyDirectory, resolvedDirectory, legacyContents);
                return new KeyStoreMigrationOutcome(true, legacyDirectory, resolvedDirectory);
            }

            if (!ResolvedHoldsEveryLegacyKey(legacyDirectory, legacyContents, resolvedDirectory, resolvedContents))
            {
                throw new InvalidOperationException(
                    $"A key store was found at '{legacyDirectory}' holding keys the one in use at '{resolvedDirectory}' does not. " +
                    "Lighthouse will not choose between them, because the wrong choice leaves every stored secret unreadable. " +
                    "Keep the store that belongs to this instance, move the other one elsewhere, and start Lighthouse again.");
            }

            return nothingCarriedOver;
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

        // Every carry-over leaves the legacy store where it was, and the instance then writes its own key
        // into the resolved one - so from the second start onwards the resolved store holds strictly more
        // than the legacy store does. What has to hold is that nothing in the legacy store would be lost by
        // walking away from it, not that the two are identical.
        private static bool ResolvedHoldsEveryLegacyKey(
            string legacyDirectory,
            List<string> legacyContents,
            string resolvedDirectory,
            List<string> resolvedContents)
        {
            return legacyContents.TrueForAll(relativePath =>
                resolvedContents.Contains(relativePath, StringComparer.Ordinal)
                && AreIdentical(
                    Path.Combine(legacyDirectory, relativePath),
                    Path.Combine(resolvedDirectory, relativePath)));
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
