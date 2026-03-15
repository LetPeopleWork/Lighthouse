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

            // List of places to look for appsettings.json
            var searchPaths = new List<string>();
            if (!string.IsNullOrEmpty(resourcesDir))
            {
                searchPaths.Add(resourcesDir);
                searchPaths.Add(Path.Combine(resourcesDir, "resources")); // Common Tauri structure
            }
            searchPaths.Add(Path.GetDirectoryName(Environment.ProcessPath)!);

            // Find the first directory that actually contains the config
            var workingDir = searchPaths
                .FirstOrDefault(p => Directory.Exists(p) && File.Exists(Path.Combine(p, "appsettings.json")));

            if (!string.IsNullOrEmpty(workingDir))
            {
                Console.WriteLine($"DEBUG: Resolved Working Dir to: {workingDir}");
                builder.Configuration.SetBasePath(workingDir);
                builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Directory.SetCurrentDirectory(workingDir);
                builder.Environment.ContentRootPath = workingDir;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var existingDyldPath = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? "";
                    var newDyldPath = string.IsNullOrEmpty(existingDyldPath)
                        ? workingDir
                        : $"{workingDir}:{existingDyldPath}";
                    Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", newDyldPath);
                    Console.WriteLine($"DEBUG: Set DYLD_LIBRARY_PATH to: {newDyldPath}");
                }
            }
            else
            {
                // This will trigger the FATAL error with a better message if we still can't find it
                throw new FileNotFoundException($"Could not find appsettings.json in any search paths: {string.Join(", ", searchPaths)}");
            }

            var appDataDir = GetAppDataDirectory();
            var logPath = Path.Combine(appDataDir, "logs", "log-.txt");
            var dbPath = Path.Combine(appDataDir, "LighthouseAppContext.db");

            Directory.CreateDirectory(appDataDir);
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            builder.Configuration["Serilog:WriteTo:0:Args:path"] = logPath;
            builder.Configuration["Database:ConnectionString"] = $"Data Source={dbPath}";
            builder.Configuration["Serilog:WriteTo:0:Args:path"] = logPath;
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