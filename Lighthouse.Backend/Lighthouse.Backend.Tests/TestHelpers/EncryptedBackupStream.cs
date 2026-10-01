using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A backup file the restore accepts: a zip encrypted with the password the way the product encrypts it.
    /// Its content is one placeholder file, because what a restore brings back is decided by the database
    /// provider, which every caller of this replaces.
    /// </summary>
    public static class EncryptedBackupStream
    {
        public static MemoryStream Create(string password)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"test-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllText(Path.Combine(tempDir, "test.txt"), "test content");

                var zipPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.zip");
                ZipFile.CreateFromDirectory(tempDir, zipPath);

                var salt = Encoding.UTF8.GetBytes("LighthouseDbBackup");
                var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                var encryptedPath = zipPath + ".enc";
                using (var outputStream = File.Create(encryptedPath))
                {
                    outputStream.Write(aes.IV);
                    using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                    using var inputStream = File.OpenRead(zipPath);
                    inputStream.CopyTo(cryptoStream);
                }

                var result = new MemoryStream(File.ReadAllBytes(encryptedPath));

                File.Delete(zipPath);
                File.Delete(encryptedPath);

                return result;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
    }
}
