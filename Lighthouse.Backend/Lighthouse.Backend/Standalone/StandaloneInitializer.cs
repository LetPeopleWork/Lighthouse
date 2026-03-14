using System.Runtime.InteropServices;
using Serilog;

namespace Lighthouse.Backend.Standalone
{
    public static class StandaloneInitializer
    {
        public static void InitializePaths(WebApplicationBuilder builder)
        {
            if (!IsStandalone())
            {
                return;
            }

            var resourcesDir = Environment.GetEnvironmentVariable("LIGHTHOUSE_RESOURCES_DIR");
            var workingDir = !string.IsNullOrEmpty(resourcesDir) && Directory.Exists(resourcesDir)
                ? resourcesDir
                : Path.GetDirectoryName(Environment.ProcessPath);

            if (!string.IsNullOrEmpty(workingDir))
            {
                Directory.SetCurrentDirectory(workingDir);
                builder.Environment.ContentRootPath = workingDir;
            }

            var appDataDir = GetAppDataDirectory();
            Directory.CreateDirectory(appDataDir);

            var logPath = Path.Combine(appDataDir, "logs", "log-.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            builder.Configuration["Serilog:WriteTo:0:Args:path"] = logPath;

            var dbPath = Path.Combine(appDataDir, "LighthouseAppContext.db");
            builder.Configuration["Database:ConnectionString"] = $"Data Source={dbPath}";
        }

        private static string GetAppDataDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userProfile, "Library", "Application Support", "Lighthouse");
            }

            // Windows and Linux
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Lighthouse"
            );
        }

        private static bool IsStandalone()
        {
            return Environment.GetEnvironmentVariable("Standalone") == "true";
        }
    }
}