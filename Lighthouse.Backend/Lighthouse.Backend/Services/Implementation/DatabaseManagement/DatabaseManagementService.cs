using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public class DatabaseManagementService(
        IDatabaseManagementProvider provider,
        DatabaseMaintenanceGate gate,
        DatabaseOperationTracker tracker,
        ILogger<DatabaseManagementService> logger,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        IEncryptionKeyRingHolder keyRingHolder)
        : IDatabaseManagementService
    {
        private const string CredentialsUnreadableReason =
            "None of the stored credentials in this backup can be read with this installation's encryption key. Nothing has been changed.";

        private readonly Dictionary<string, string> backupArtifacts = new();

        public DatabaseCapabilityStatus GetCapabilityStatus()
        {
            return new DatabaseCapabilityStatus(
                Provider: provider.ProviderName,
                IsOperationBlocked: gate.IsBlocked,
                BlockedReason: gate.BlockedReason,
                IsToolingAvailable: provider.IsToolingAvailable(),
                ToolingGuidanceMessage: provider.GetToolingGuidanceMessage(),
                ToolingGuidanceUrl: provider.GetToolingGuidanceUrl(),
                ActiveOperation: tracker.GetLatestStatus());
        }

        public DatabaseOperationStatus? GetOperationStatus(string operationId)
        {
            return tracker.GetStatus(operationId);
        }

        public async Task<DatabaseOperationStatus> CreateBackup(string password)
        {
            var operationId = GenerateOperationId();

            var acquisition = gate.TryAcquire(DatabaseOperationType.Backup, operationId);
            if (!acquisition.Acquired)
            {
                logger.LogWarning("Backup operation {OperationId} blocked: {Reason}", operationId, acquisition.BlockedReason);
                return new DatabaseOperationStatus(operationId, DatabaseOperationType.Backup, DatabaseOperationState.Failed, acquisition.BlockedReason);
            }

            _ = tracker.StartOperation(operationId, DatabaseOperationType.Backup);

            try
            {
                tracker.TransitionTo(operationId, DatabaseOperationState.Executing);
                logger.LogInformation("Starting backup operation {OperationId} for provider {Provider}", operationId, provider.ProviderName);

                var tempDir = CreateTempDirectory();

                try
                {
                    _ = await provider.CreateBackup(tempDir);
                    var manifest = CreateManifest();
                    var manifestPath = Path.Combine(tempDir, "manifest.json");
                    await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

                    var zipPath = await CreateEncryptedZip(tempDir, password);
                    backupArtifacts[operationId] = zipPath;

                    tracker.TransitionTo(operationId, DatabaseOperationState.Completed);
                    logger.LogInformation("Backup operation {OperationId} completed successfully", operationId);

                    return tracker.GetStatus(operationId)!;
                }
                finally
                {
                    CleanupDirectory(tempDir);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backup operation {OperationId} failed", operationId);
                tracker.TransitionToFailed(operationId, ex.Message);
                return tracker.GetStatus(operationId)!;
            }
            finally
            {
                gate.Release(operationId);
            }
        }

        public async Task<DatabaseOperationStatus> RestoreBackup(Stream backupFile, string password, UnreadableCredentials onUnreadable = UnreadableCredentials.StopRestore)
        {
            var operationId = GenerateOperationId();

            var acquisition = gate.TryAcquire(DatabaseOperationType.Restore, operationId);
            if (!acquisition.Acquired)
            {
                logger.LogWarning("Restore operation {OperationId} blocked: {Reason}", operationId, acquisition.BlockedReason);
                return new DatabaseOperationStatus(operationId, DatabaseOperationType.Restore, DatabaseOperationState.Failed, acquisition.BlockedReason);
            }

            _ = tracker.StartOperation(operationId, DatabaseOperationType.Restore);

            try
            {
                tracker.TransitionTo(operationId, DatabaseOperationState.Executing);
                logger.LogInformation("Starting restore operation {OperationId} for provider {Provider}", operationId, provider.ProviderName);

                var tempDir = CreateTempDirectory();

                try
                {
                    var extractDir = await UnzipBackup(backupFile, password, tempDir);

                    var nothingReadable = await NoStoredCredentialCanBeRead(extractDir);

                    if (nothingReadable && onUnreadable == UnreadableCredentials.StopRestore)
                    {
                        logger.LogWarning("Restore operation {OperationId} stopped: this installation's key reads none of the backup's stored credentials", operationId);
                        tracker.TransitionToFailed(operationId, CredentialsUnreadableReason, DatabaseOperationFailure.CredentialsUnreadable);
                        return tracker.GetStatus(operationId)!;
                    }

                    await provider.RestoreBackup(extractDir);

                    if (nothingReadable)
                    {
                        RemoveTheCredentialsNoKeyHereCanRead(operationId);
                    }

                    MigrateAndSeedDatabase();

                    tracker.TransitionTo(operationId, DatabaseOperationState.Completed);
                    logger.LogInformation("Restore operation {OperationId} completed successfully", operationId);

                    return tracker.GetStatus(operationId)!;
                }
                finally
                {
                    CleanupDirectory(tempDir);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Restore operation {OperationId} failed", operationId);
                tracker.TransitionToFailed(operationId, ex.Message);
                return tracker.GetStatus(operationId)!;
            }
            finally
            {
                gate.Release(operationId);
            }
        }

        // Restoring a backup whose credentials the running key reads none of leaves a database the next start refuses,
        // so that one answer stops the restore; any other answer, including not being able to tell, lets it go ahead,
        // exactly as the start itself decides.
        private async Task<bool> NoStoredCredentialCanBeRead(string extractDir)
        {
            await using var staged = await provider.StageBackup(extractDir);

            var finding = new DatabaseSecretReadabilityProbe(staged.OpenConnection).Look(keyRingHolder.Current);

            return finding.Readability == StoredSecretReadability.NothingReadable;
        }

        // The operator chose to restore anyway, so the credentials the running key cannot read are given up before the
        // next start sees them and refuses; each one is entered again in its Connection.
        private void RemoveTheCredentialsNoKeyHereCanRead(string operationId)
        {
            logger.LogWarning("Restore operation {OperationId} removes the stored credentials this installation's key cannot read, as the operator chose", operationId);

            using var live = provider.CreateConnection();
            new UnreadableStoredSecrets(keyRingHolder.Current).RemoveFrom(live);
        }

        private static async Task<string> UnzipBackup(Stream backupFile, string password, string tempDir)
        {
            var zipPath = Path.Combine(tempDir, "restore.zip");
            await using (var fileStream = File.Create(zipPath))
            {
                await backupFile.CopyToAsync(fileStream);
            }

            var extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);
            ExtractEncryptedZip(zipPath, extractDir, password);
            return extractDir;
        }

        public async Task<DatabaseOperationStatus> ClearDatabase()
        {
            var operationId = GenerateOperationId();

            var acquisition = gate.TryAcquire(DatabaseOperationType.Clear, operationId);
            if (!acquisition.Acquired)
            {
                logger.LogWarning("Clear operation {OperationId} blocked: {Reason}", operationId, acquisition.BlockedReason);
                return new DatabaseOperationStatus(operationId, DatabaseOperationType.Clear, DatabaseOperationState.Failed, acquisition.BlockedReason);
            }

            _ = tracker.StartOperation(operationId, DatabaseOperationType.Clear);

            try
            {
                tracker.TransitionTo(operationId, DatabaseOperationState.Executing);
                logger.LogInformation("Starting clear operation {OperationId} for provider {Provider}", operationId, provider.ProviderName);

                await provider.ClearDatabase();

                MigrateAndSeedDatabase();

                tracker.TransitionTo(operationId, DatabaseOperationState.Completed);
                logger.LogInformation("Clear operation {OperationId} completed successfully", operationId);

                return tracker.GetStatus(operationId)!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clear operation {OperationId} failed", operationId);
                tracker.TransitionToFailed(operationId, ex.Message);
                return tracker.GetStatus(operationId)!;
            }
            finally
            {
                gate.Release(operationId);
            }
        }

        public Stream GetBackupArtifact(string operationId)
        {
            if (!backupArtifacts.TryGetValue(operationId, out var path) || !File.Exists(path))
            {
                throw new FileNotFoundException($"Backup artifact for operation {operationId} not found.");
            }

            return File.OpenRead(path);
        }

        private void MigrateAndSeedDatabase()
        {
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            try
            {
                provider.RecycleConnection();

                DatabaseConfigurator.ApplyMigrations(scopedProvider);

                DatabaseConfigurator.SeedDatabase(scopedProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database migration and seeding after restore");
            }
        }

        private static string GenerateOperationId()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }

        private Dictionary<string, string> CreateManifest()
        {
            var manifest = new Dictionary<string, string>
            {
                ["provider"] = provider.ProviderName,
                ["createdAt"] = timeProvider.GetUtcNow().UtcDateTime.ToString("O"),
                ["appVersion"] = GetAppVersion(),
            };

            var serverVersion = provider.GetServerVersion();
            if (serverVersion != null)
            {
                manifest["serverVersion"] = serverVersion;
            }

            return manifest;
        }

        private static string GetAppVersion()
        {
            return typeof(DatabaseManagementService).Assembly.GetName().Version?.ToString() ?? "unknown";
        }

        private static string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"lighthouse-db-{Guid.NewGuid():N}");
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

        private static async Task<string> CreateEncryptedZip(string sourceDir, string password)
        {
            var zipPath = Path.Combine(Path.GetTempPath(), $"lighthouse-backup-{Guid.NewGuid():N}.zip");

            // Create unencrypted ZIP first
            var tempZipPath = zipPath + ".tmp";
            await ZipFile.CreateFromDirectoryAsync(sourceDir, tempZipPath);

            // Encrypt with AES
            var key = DeriveKey(password);
            await EncryptFile(tempZipPath, zipPath, key);

            // Clean up plaintext intermediate
            File.Delete(tempZipPath);

            return zipPath;
        }

        private static void ExtractEncryptedZip(string encryptedZipPath, string extractDir, string password)
        {
            var key = DeriveKey(password);
            var decryptedPath = encryptedZipPath + ".dec";

            try
            {
                DecryptFile(encryptedZipPath, decryptedPath, key);
                ExtractToDirectorySafely(decryptedPath, extractDir);
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

        private static async Task EncryptFile(string inputPath, string outputPath, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            await using var outputStream = File.Create(outputPath);
            await outputStream.WriteAsync(aes.IV);

            await using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await using var inputStream = File.OpenRead(inputPath);
            await inputStream.CopyToAsync(cryptoStream);
        }

        private static void DecryptFile(string inputPath, string outputPath, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;

            using var inputStream = File.OpenRead(inputPath);

            var iv = new byte[16];
            var bytesRead = inputStream.Read(iv, 0, 16);
            if (bytesRead < 16)
            {
                throw new InvalidOperationException("Invalid backup file: too short to contain encryption header.");
            }

            aes.IV = iv;

            using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var outputStream = File.Create(outputPath);
            cryptoStream.CopyTo(outputStream);
        }

        private static void ExtractToDirectorySafely(string zipPath, string destinationDir)
        {
            var fullDestination = Path.GetFullPath(destinationDir);

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                // Skip directory entries
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var destinationPath = Path.GetFullPath(Path.Combine(fullDestination, entry.FullName));

                // Ensure the entry resolves inside the destination directory
                if (!destinationPath.StartsWith(fullDestination + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Zip Slip detected: {entry.FullName}");

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }
    }
}
