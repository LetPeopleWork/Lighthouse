using System.Runtime.InteropServices;

namespace Lighthouse.Backend.macOS
{
    public static class MacInitializer
    {
        private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";

        // --- Objective-C Runtime Imports ---
        [DllImport(ObjCRuntime, EntryPoint = "objc_getClass")]
        private static extern IntPtr GetClass(string name);

        [DllImport(ObjCRuntime, EntryPoint = "sel_registerName")]
        private static extern IntPtr RegisterSelector(string name);

        [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libSystem.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        private const int RTLD_LAZY = 0x1;

        public static void Initialize(WebApplicationBuilder builder)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }
            
            SetMacOSSpecificPaths(builder);
            CheckForUpdates();
        }

        private static void SetMacOSSpecificPaths(WebApplicationBuilder builder)
        {
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

        public static void CheckForUpdates()
        {
            try
            {
                // Use the process path to ensure we are relative to the actual .app bundle
                var processPath = Environment.ProcessPath; 
                if (string.IsNullOrEmpty(processPath)) return;

                string binaryDir = Path.GetDirectoryName(processPath);

                if (string.IsNullOrEmpty(binaryDir))
                {
                    return;
                }

                // Contents/MacOS/../Frameworks/Sparkle.framework/Sparkle
                string frameworkPath = Path.Combine(binaryDir, "..", "Frameworks", "Sparkle.framework", "Sparkle");

                IntPtr handle = dlopen(frameworkPath, RTLD_LAZY);
                if (handle == IntPtr.Zero)
                {
                    // Fallback for development environments where the structure might differ
                    Console.WriteLine("Sparkle.framework not found in bundle, skipping update check.");
                    return;
                }
                
                Console.WriteLine("Checking for updates...");

                IntPtr suUpdaterClass = GetClass("SUUpdater");
                if (suUpdaterClass == IntPtr.Zero) return;

                IntPtr sharedUpdaterSelector = RegisterSelector("sharedUpdater");
                // [SUUpdater sharedUpdater]
                IntPtr updaterInstance = SendMessage(suUpdaterClass, sharedUpdaterSelector);

                if (updaterInstance != IntPtr.Zero)
                {
                    IntPtr checkSelector = RegisterSelector("checkForUpdatesInBackground");
                    // [updater checkForUpdatesInBackground]
                    SendMessage(updaterInstance, checkSelector);
                }
            }
            catch (Exception ex)
            {
                // Log to Console as Serilog might not be fully initialized yet
                Console.WriteLine($"Sparkle Error: {ex.Message}");
            }
        }
    }
}