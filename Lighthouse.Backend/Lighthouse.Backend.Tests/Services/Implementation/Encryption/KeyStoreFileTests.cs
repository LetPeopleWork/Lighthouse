using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// Who may read the files Lighthouse writes into a key store. Real files in a real temp directory,
    /// because the whole finding was that the mode the platform applies and the mode Lighthouse applied
    /// were different, and a filesystem double would have agreed with whatever either of them did.
    /// </summary>
    public class KeyStoreFileTests
    {
        private const UnixFileMode Owner = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        private DirectoryInfo keyStore = null!;

        [SetUp]
        public void SetUp()
        {
            keyStore = Directory.CreateTempSubdirectory("KeyStoreFileTests_");
        }

        [TearDown]
        public void TearDown()
        {
            keyStore.Delete(recursive: true);
        }

        [Test]
        public void Write_AFileItCreates_CanBeReadOnlyByTheAccountThatWroteIt()
        {
            var path = Path.Combine(keyStore.FullName, "encryption-keyring.protected");

            KeyStoreFile.Write(path, RandomNumberGenerator.GetBytes(64));

            AssertReadableOnlyByItsOwner(path);
        }

        // The ring file is written aside and moved into place, so the mode that survives is the one the
        // temporary file was created with. A mode set on the destination instead would be lost by the move.
        [Test]
        public void Write_AFileMovedIntoPlaceAfterwards_KeepsTheModeItWasCreatedWith()
        {
            var staging = Path.Combine(keyStore.FullName, "encryption-keyring.protected.writing");
            var destination = Path.Combine(keyStore.FullName, "encryption-keyring.protected");

            KeyStoreFile.Write(staging, RandomNumberGenerator.GetBytes(64));
            File.Move(staging, destination, overwrite: true);

            AssertReadableOnlyByItsOwner(destination);
        }

        // An instance that already has a key store rewrites the ring only when it rotates. Writing over a
        // file that is already there has to close it too, or an install that upgrades keeps the wider mode
        // it was created with for as long as it never rotates.
        [Test]
        public void Write_OverAFileThatIsAlreadyThere_ClosesItRatherThanKeepingWhatItHad()
        {
            var path = Path.Combine(keyStore.FullName, "oauth-state-secret.protected");

            File.WriteAllBytes(path, RandomNumberGenerator.GetBytes(16));
            OpenItToEverybody(path);

            KeyStoreFile.Write(path, RandomNumberGenerator.GetBytes(64));

            AssertReadableOnlyByItsOwner(path);
        }

        [Test]
        public void Write_WhatItWroteDown_ReadsBackAsWhatItWasGiven()
        {
            var path = Path.Combine(keyStore.FullName, "encryption-keyring.protected");
            var contents = RandomNumberGenerator.GetBytes(128);

            KeyStoreFile.Write(path, contents);

            Assert.That(File.ReadAllBytes(path), Is.EqualTo(contents));
        }

        [Test]
        public void Write_WithoutSomewhereToWriteOrSomethingToWrite_RefusesRatherThanCreatingAnEmptyFile()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => KeyStoreFile.Write(null!, []), Throws.InstanceOf<ArgumentException>());
                Assert.That(() => KeyStoreFile.Write("   ", []), Throws.InstanceOf<ArgumentException>());
                Assert.That(() => KeyStoreFile.Write(Path.Combine(keyStore.FullName, "x"), null!), Throws.ArgumentNullException);
            }
        }

        private static void OpenItToEverybody(string path)
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, Owner | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }
        }

        // Windows has no mode to compare, and asserting nothing there is deliberate: a silently skipped
        // assertion is how the open mode this test exists for survived four slices unnoticed, so the
        // platform that does have a mode always asserts it.
        private static void AssertReadableOnlyByItsOwner(string path)
        {
            Assert.That(File.Exists(path), Is.True);

            if (!OperatingSystem.IsWindows())
            {
                Assert.That(File.GetUnixFileMode(path), Is.EqualTo(Owner));
            }
        }
    }
}
