using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A database with the product's schema already in it, built once and meant to be copied for each test
    /// instead of creating the schema again every time. The file name carries a hash of the schema, so a
    /// change to the model never picks up a template from an earlier build.
    /// </summary>
    internal static class TestDatabaseTemplate
    {
        private static readonly Lazy<string> ForThisProcess =
            new(() => EnsureBuilt(AppContext.BaseDirectory), LazyThreadSafetyMode.ExecutionAndPublication);

        public static string Path => ForThisProcess.Value;

        private const int CopyAttempts = 5;

        private const int BuildAttempts = 5;

        // The template is a write-ahead-log database that was checkpointed into its main file, so that file
        // alone holds the whole schema and only it is copied; a copied log file would not belong to the copy.
        // Another process scanning the directory can hold the file for a moment, so a failed copy is tried
        // again a few times before giving up. A copy that never succeeds leaves nothing behind, so a later
        // scope cannot open a truncated file.
        internal static void CopyTo(string destination, Action<string, string>? copyFile = null)
        {
            copyFile ??= (source, target) => File.Copy(source, target);
            for (var attempt = 1; attempt < CopyAttempts; attempt++)
            {
                try
                {
                    copyFile(Path, destination);
                    return;
                }
                catch (IOException)
                {
                    File.Delete(destination);
                    Thread.Sleep(20 * attempt);
                }
            }

            try
            {
                copyFile(Path, destination);
            }
            catch (IOException exception)
            {
                File.Delete(destination);
                throw new IOException($"Could not copy the schema template to '{destination}' after {CopyAttempts} attempts.", exception);
            }
        }

        // Built under a temporary name and moved into place only when complete, so the final name never
        // exists half written. When another process has already moved its copy in, ours is thrown away.
        // A build that fails on the disk is tried again a few times, because the result is kept for the
        // whole process and a failure here would otherwise fail every test that follows.
        internal static string EnsureBuilt(string directory, Action<string, string>? moveIntoPlace = null)
        {
            moveIntoPlace ??= (source, target) => File.Move(source, target, overwrite: false);
            Directory.CreateDirectory(directory);
            var finalPath = System.IO.Path.Combine(directory, $"template_{SchemaHash()}.db");
            var attempt = 0;
            while (true)
            {
                attempt++;
                if (File.Exists(finalPath))
                {
                    return finalPath;
                }

                try
                {
                    BuildAndMoveInto(finalPath, directory, moveIntoPlace);
                    return finalPath;
                }
                catch (IOException) when (attempt < BuildAttempts)
                {
                    Thread.Sleep(20 * attempt);
                }
            }
        }

        private static void BuildAndMoveInto(string finalPath, string directory, Action<string, string> moveIntoPlace)
        {
            var temporaryPath = System.IO.Path.Combine(directory, $"template_{Guid.NewGuid():N}.tmp");
            try
            {
                using (var context = NewContext(temporaryPath))
                {
                    context.Database.EnsureCreated();
                    context.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE)");
                }

                moveIntoPlace(temporaryPath, finalPath);
            }
            catch (IOException) when (File.Exists(finalPath))
            {
            }
            finally
            {
                DeleteWithLogFiles(temporaryPath);
            }
        }

        private static string SchemaHash()
        {
            using var context = NewContext(":memory:");
            var script = context.Database.GenerateCreateScript();
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script)))[..16].ToLowerInvariant();
        }

        private static LighthouseAppContext NewContext(string path)
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite($"DataSource={path};Pooling=False", DatabaseConfigurator.ConfigureSqlite)
                .Options;
            return new LighthouseAppContext(options, new FakeCryptoService(), NullLogger<LighthouseAppContext>.Instance);
        }

        private static void DeleteWithLogFiles(string path)
        {
            foreach (var file in new[] { path, $"{path}-wal", $"{path}-shm", $"{path}-journal" })
            {
                File.Delete(file);
            }
        }
    }
}
