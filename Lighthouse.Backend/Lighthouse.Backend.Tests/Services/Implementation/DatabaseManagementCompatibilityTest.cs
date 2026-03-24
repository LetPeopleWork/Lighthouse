using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class DatabaseManagementCompatibilityTest
    {
        private static readonly string FixtureDirectory = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Services",
            "Implementation",
            "TestData");

        private static string GetBackupPassword()
        {
            var password = Environment.GetEnvironmentVariable("LighthouseBackupPassword");
            if (string.IsNullOrEmpty(password))
            {
                Assert.Ignore("LighthouseBackupPassword environment variable is not set. Skipping compatibility tests.");
            }

            return password!;
        }

        [Test]
        public void SqliteFixture_CanBeDecrypted()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_sqlite.zip");

            Assert.That(File.Exists(fixturePath), Is.True, $"Fixture file not found: {fixturePath}");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                ExtractEncryptedZip(fixturePath, extractDir, password);

                Assert.That(Directory.Exists(extractDir), Is.True);
                Assert.That(Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories).Length, Is.GreaterThan(0));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void SqliteFixture_ContainsManifest()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_sqlite.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ExtractEncryptedZip(fixturePath, extractDir, password);

                var manifestPath = Path.Combine(extractDir, "manifest.json");
                Assert.That(File.Exists(manifestPath), Is.True, "Manifest file missing from backup");

                var manifestContent = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestContent);

                Assert.That(manifest, Is.Not.Null);
                Assert.That(manifest!.ContainsKey("provider"), Is.True);
                Assert.That(manifest["provider"], Is.EqualTo("sqlite"));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void SqliteFixture_ContainsDatabaseFile()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_sqlite.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ExtractEncryptedZip(fixturePath, extractDir, password);

                var dbFiles = Directory.GetFiles(extractDir, "*.db", SearchOption.AllDirectories);
                Assert.That(dbFiles.Length, Is.GreaterThan(0), "No .db file found in SQLite backup fixture");
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void SqliteFixture_WrongPassword_ThrowsException()
        {
            GetBackupPassword(); // Ensure we only skip if env var is absent
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_sqlite.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                Assert.Throws<InvalidOperationException>(() =>
                    ExtractEncryptedZip(fixturePath, extractDir, "wrong-password-definitely-not-it"));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void PostgresFixture_CanBeDecrypted()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_postgres.zip");

            Assert.That(File.Exists(fixturePath), Is.True, $"Fixture file not found: {fixturePath}");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                ExtractEncryptedZip(fixturePath, extractDir, password);

                Assert.That(Directory.Exists(extractDir), Is.True);
                Assert.That(Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories).Length, Is.GreaterThan(0));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void PostgresFixture_ContainsManifest()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_postgres.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ExtractEncryptedZip(fixturePath, extractDir, password);

                var manifestPath = Path.Combine(extractDir, "manifest.json");
                Assert.That(File.Exists(manifestPath), Is.True, "Manifest file missing from backup");

                var manifestContent = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestContent);

                Assert.That(manifest, Is.Not.Null);
                Assert.That(manifest!.ContainsKey("provider"), Is.True);
                Assert.That(manifest!["provider"], Is.EqualTo("postgresql"));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void PostgresFixture_ContainsDumpFile()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_postgres.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ExtractEncryptedZip(fixturePath, extractDir, password);

                var dumpFiles = Directory.GetFiles(extractDir, "*.pgdump", SearchOption.AllDirectories);
                Assert.That(dumpFiles.Length, Is.GreaterThan(0), "No .pgdump file found in Postgres backup fixture");
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void PostgresFixture_WrongPassword_ThrowsException()
        {
            GetBackupPassword(); // Ensure we only skip if env var is absent
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_postgres.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                Assert.Throws<InvalidOperationException>(() =>
                    ExtractEncryptedZip(fixturePath, extractDir, "wrong-password-definitely-not-it"));
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void PostgresFixture_ManifestContainsServerVersion()
        {
            var password = GetBackupPassword();
            var fixturePath = Path.Combine(FixtureDirectory, "Lighthouse_Backup_2026-03-24_postgres.zip");

            var tempDir = CreateTempDirectory();
            try
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ExtractEncryptedZip(fixturePath, extractDir, password);

                var manifestPath = Path.Combine(extractDir, "manifest.json");
                var manifestContent = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestContent);

                Assert.That(manifest, Is.Not.Null);
                Assert.That(manifest!.ContainsKey("serverVersion"), Is.True, "Postgres backup manifest should contain serverVersion");
                Assert.That(manifest["serverVersion"], Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                CleanupDirectory(tempDir);
            }
        }

        [Test]
        public void BothFixtures_ManifestsContainCreatedAtAndAppVersion()
        {
            var password = GetBackupPassword();

            foreach (var provider in new[] { "sqlite", "postgres" })
            {
                var fixturePath = Path.Combine(FixtureDirectory, $"Lighthouse_Backup_2026-03-24_{provider}.zip");
                var tempDir = CreateTempDirectory();
                try
                {
                    var extractDir = Path.Combine(tempDir, "extracted");
                    Directory.CreateDirectory(extractDir);
                    ExtractEncryptedZip(fixturePath, extractDir, password);

                    var manifestPath = Path.Combine(extractDir, "manifest.json");
                    var manifestContent = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestContent);

                    Assert.That(manifest, Is.Not.Null, $"{provider}: manifest deserialization failed");
                    Assert.That(manifest!.ContainsKey("createdAt"), Is.True, $"{provider}: manifest missing createdAt");
                    Assert.That(manifest.ContainsKey("appVersion"), Is.True, $"{provider}: manifest missing appVersion");
                }
                finally
                {
                    CleanupDirectory(tempDir);
                }
            }
        }

        private static void ExtractEncryptedZip(string encryptedZipPath, string extractDir, string password)
        {
            var key = DeriveKey(password);
            var decryptedPath = encryptedZipPath + ".dec";

            try
            {
                DecryptFile(encryptedZipPath, decryptedPath, key);
                ZipFile.ExtractToDirectory(decryptedPath, extractDir);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Failed to decrypt the backup file. The password may be incorrect or the file may be corrupted.", ex);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidOperationException(
                    "Failed to extract the backup archive. The file may be corrupted or not a valid backup.", ex);
            }
            finally
            {
                if (File.Exists(decryptedPath))
                {
                    File.Delete(decryptedPath);
                }
            }
        }

        private static byte[] DeriveKey(string password)
        {
            var salt = Encoding.UTF8.GetBytes("LighthouseDbBackup");
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        }

        private static void DecryptFile(string inputPath, string outputPath, byte[] key)
        {
            using var inputStream = File.OpenRead(inputPath);

            var iv = new byte[16];
            var bytesRead = inputStream.Read(iv, 0, iv.Length);
            if (bytesRead < iv.Length)
            {
                throw new CryptographicException("Invalid encrypted file: too short for IV.");
            }

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var outputStream = File.Create(outputPath);
            using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cryptoStream.CopyTo(outputStream);
        }

        private static string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"compat-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        private static void CleanupDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
