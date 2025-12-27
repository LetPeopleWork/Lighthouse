using System.Runtime.InteropServices;

namespace Lighthouse.Backend.macOS
{
    public static class MacInitializer
    {
        public static void SetMacOSSpecificPaths(WebApplicationBuilder builder)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Override log path
            var logPath = Path.Combine(userProfile, "Library", "Logs", "Lighthouse", "log-.txt");
            var logDirectory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            builder.Configuration["Serilog:WriteTo:0:Args:path"] = logPath;

            // Override database path
            var dbPath = Path.Combine(userProfile, "Library", "Application Support", "Lighthouse",
                "LighthouseAppContext.db");
            var connectionString = $"Data Source={dbPath}";
            builder.Configuration["Database:ConnectionString"] = connectionString;
        }
    }
}