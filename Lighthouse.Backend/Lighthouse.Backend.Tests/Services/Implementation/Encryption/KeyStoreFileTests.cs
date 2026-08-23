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

        private static readonly string[] OnlyTheSecretItself = ["oauth-state-secret.protected"];

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

        // Two boots sharing a key store both find no secret there and both make one. Written straight to
        // its final name, one of them fails outright on a file the other still has open, and the fixture
        // it was starting dies with an error about encryption that has nothing to do with what it tests.
        // So the write says whether it was the one that got there first, and never writes over what it
        // found: the loser can read back the secret the winner left instead of keeping one the file no
        // longer holds - a state in which one of the two cannot finish a sign-in the other started.
        [Test]
        public void WriteIfAbsent_WhenNothingIsThereYet_WritesItAndSaysItDid()
        {
            var path = Path.Combine(keyStore.FullName, "oauth-state-secret.protected");
            var contents = RandomNumberGenerator.GetBytes(64);

            var wroteIt = KeyStoreFile.WriteIfAbsent(path, contents);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wroteIt, Is.True);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(contents));
            }
        }

        [Test]
        public void WriteIfAbsent_WhenSomethingIsAlreadyThere_LeavesItAloneAndSaysSo()
        {
            var path = Path.Combine(keyStore.FullName, "oauth-state-secret.protected");
            var whatTheWinnerWrote = RandomNumberGenerator.GetBytes(64);
            File.WriteAllBytes(path, whatTheWinnerWrote);

            var wroteIt = KeyStoreFile.WriteIfAbsent(path, RandomNumberGenerator.GetBytes(64));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wroteIt, Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(whatTheWinnerWrote));
            }
        }

        // The staging file is named apart per write so two of them cannot collide, which also means a
        // write that leaves one behind leaves a key store filling up with files nobody ever reads.
        [Test]
        public void WriteIfAbsent_LeavesNothingBehindItInTheKeyStore_WhicheverOfTheTwoItWas()
        {
            var path = Path.Combine(keyStore.FullName, "oauth-state-secret.protected");

            KeyStoreFile.WriteIfAbsent(path, RandomNumberGenerator.GetBytes(64));
            KeyStoreFile.WriteIfAbsent(path, RandomNumberGenerator.GetBytes(64));

            Assert.That(
                keyStore.GetFiles().Select(file => file.Name),
                Is.EqualTo(OnlyTheSecretItself));
        }

        [Test]
        public void WriteIfAbsent_AFileItCreates_CanBeReadOnlyByTheAccountThatWroteIt()
        {
            var path = Path.Combine(keyStore.FullName, "oauth-state-secret.protected");

            KeyStoreFile.WriteIfAbsent(path, RandomNumberGenerator.GetBytes(64));

            AssertReadableOnlyByItsOwner(path);
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
