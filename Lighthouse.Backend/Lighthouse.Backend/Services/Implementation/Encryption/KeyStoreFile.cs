namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Every file Lighthouse writes into a key store is created here, so the answer to "who may read this"
    // is given once rather than at each place a file happens to be written.
    //
    // The key that wraps the others is written by the platform, which closes it to its owner. The two
    // Lighthouse writes itself were left at whatever the process default was, which on an ordinary Linux
    // host means every account on the machine can read them. They are wrapped, so that was a missing layer
    // rather than an open door - but a directory holding three files of key material, two of them open and
    // one closed, tells an operator something about the boundary that is not true.
    public static class KeyStoreFile
    {
        private const UnixFileMode ReadableOnlyByItsOwner = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        private const string StagingFileSuffix = ".writing";

        public static void Write(string path, byte[] contents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(contents);

            WriteContents(path, contents);
            CloseItToEverybodyElse(path);
        }

        // Two boots sharing a key store both find no secret there and both make one. Written straight to
        // its final name, the second of them fails outright on a file the first still has open, and
        // whichever wrote last leaves the other holding a secret the file no longer contains - so one of
        // them cannot finish a sign-in the other started. Staged under a name only this write knows and
        // then moved into place, the move alone decides which secret the file holds, and the boot that
        // lost is told so, so it can read back what the winner left instead of keeping its own.
        public static bool WriteIfAbsent(string path, byte[] contents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(contents);

            var staging = $"{path}.{Guid.NewGuid():n}{StagingFileSuffix}";

            WriteContents(staging, contents);
            CloseItToEverybodyElse(staging);

            try
            {
                File.Move(staging, path, overwrite: false);
                return true;
            }
            catch (IOException)
            {
                File.Delete(staging);
                return false;
            }
        }

        // Created closed where the platform can create a file closed, so it is never briefly readable while
        // the contents are still being written.
        private static void WriteContents(string path, byte[] contents)
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
            };

            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = ReadableOnlyByItsOwner;
            }

            using var file = new FileStream(path, options);
            file.Write(contents);
        }

        // Asked again after the write, because a mode given at creation is not applied to a file that was
        // already there - so without this, an instance upgrading from a version that wrote its key store
        // open would keep the open mode for as long as it never rotated.
        //
        // Windows has no mode to set, and asking for one throws rather than being ignored. Who may read a
        // key store file there is decided by the directory it sits in.
        //
        // A filesystem that has no notion of who may read a file cannot be given one, and several that
        // people really do keep a key store on are like that - a volume shared from a Windows host, an
        // exFAT disk, some network mounts. Asking them refuses. Refusing to start over it would trade a
        // working instance for a layer that was never load-bearing: the file is wrapped either way, and
        // the key that unwraps it is written by the platform, which faces the same filesystem and makes
        // the same concession. So the mode is asked for and not insisted on.
        private static void CloseItToEverybodyElse(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                File.SetUnixFileMode(path, ReadableOnlyByItsOwner);
            }
            catch (Exception theFilesystemHasNoAnswer) when (theFilesystemHasNoAnswer is IOException or UnauthorizedAccessException)
            {
                // Nothing to do about it here, and nowhere to say it: everything that resolves or keeps a
                // key is barred from writing to a log so that no line can carry key material by accident.
            }
        }
    }
}
