using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if NET8_0_OR_GREATER && OSX
using AppKit;
using Foundation;
using CoreGraphics;
using UserNotifications;
using ObjCRuntime;
#endif

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

        public static void SetupMacOSMenuBar(WebApplication app)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }
            
#if NET8_0_OR_GREATER && OSX
            // Initialize AppKit
            NSApplication.Init();
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;

            // Initialize Sparkle
            var updaterController = InitializeSparkle();

            // Get the server URL from configuration
            var urls = app.Urls.FirstOrDefault() ?? "http://localhost:5002";
            var serverUrl = urls.Replace("0.0.0.0", "localhost").Replace("*", "localhost");

            // Create status bar item
            var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(NSStatusItemLength.Variable);

            // Set icon
            if (statusItem.Button != null)
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "MenuBarIcon.png");
                if (File.Exists(iconPath))
                {
                    var image = new NSImage(iconPath);
                    image.Size = new CGSize(18, 18);
                    image.Template = true;
                    statusItem.Button.Image = image;
                }
                else if (NSProcessInfo.ProcessInfo.IsOperatingSystemAtLeastVersion(
                             new NSOperatingSystemVersion(11, 0, 0)))
                {
                    statusItem.Button.Image = NSImage.GetSystemSymbol("house.fill", null);
                }
                else
                {
                    statusItem.Button.Title = "🏠";
                }
            }

            // Create menu
            var menu = new NSMenu();

            // Title item
            var titleItem = new NSMenuItem("Lighthouse");
            titleItem.Enabled = false;
            menu.AddItem(titleItem);
            menu.AddItem(NSMenuItem.SeparatorItem);

            // Open in Browser
            var openItem = new NSMenuItem("Open in Browser", "o",
                (sender, e) => { NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(serverUrl)); });
            menu.AddItem(openItem);

            // Copy URL
            var copyItem = new NSMenuItem("Copy URL", "c", (sender, e) =>
            {
                var pasteboard = NSPasteboard.GeneralPasteboard;
                pasteboard.ClearContents();
                pasteboard.SetStringForType(serverUrl, NSPasteboard.NSPasteboardTypeString);
                ShowNotification("URL Copied", $"Copied to clipboard: {serverUrl}");
            });
            menu.AddItem(copyItem);

            menu.AddItem(NSMenuItem.SeparatorItem);
            
            // Check for Updates (Sparkle)
            var updateItem = new NSMenuItem("Check for Updates...", "u", (sender, e) =>
            {
                CheckForUpdatesWithSparkle(updaterController);
            });
            menu.AddItem(updateItem);

            menu.AddItem(NSMenuItem.SeparatorItem);

            // Quit
            var quitItem = new NSMenuItem("Quit Lighthouse", "q", (sender, e) => { Environment.Exit(0); });
            menu.AddItem(quitItem);

            statusItem.Menu = menu;

            // Start AppKit event loop on a background thread
            Task.Run(() => { NSApplication.SharedApplication.Run(); });
#endif
        }

#if NET8_0_OR_GREATER && OSX
        private static NSObject? InitializeSparkle()
        {
            try
            {
                // Load Sparkle framework
                var sparkleFrameworkPath = Path.Combine(
                    NSBundle.MainBundle.BundlePath, 
                    "Contents", "Frameworks", "Sparkle.framework"
                );
                
                if (!Directory.Exists(sparkleFrameworkPath))
                {
                    Console.WriteLine($"⚠️ Sparkle framework not found at: {sparkleFrameworkPath}");
                    return null;
                }

                // Use Objective-C runtime to instantiate SPUStandardUpdaterController
                var sparkleClass = Class.GetHandle("SPUStandardUpdaterController");
                if (sparkleClass == IntPtr.Zero)
                {
                    Console.WriteLine("⚠️ SPUStandardUpdaterController class not found");
                    return null;
                }

                // Create instance: [[SPUStandardUpdaterController alloc] initWithStartingUpdater:YES updaterDelegate:nil userDriverDelegate:nil]
                var alloc = Runtime.GetNSObject(ObjCRuntime.Messaging.IntPtr_objc_msgSend(sparkleClass, Selector.GetHandle("alloc")));
                
                var initSelector = Selector.GetHandle("initWithStartingUpdater:updaterDelegate:userDriverDelegate:");
                var updaterController = Runtime.GetNSObject(
                    ObjCRuntime.Messaging.IntPtr_objc_msgSend_bool_IntPtr_IntPtr(
                        alloc.Handle, 
                        initSelector, 
                        true,      // startingUpdater
                        IntPtr.Zero,  // updaterDelegate
                        IntPtr.Zero   // userDriverDelegate
                    )
                );

                Console.WriteLine("✅ Sparkle initialized successfully");
                return updaterController;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Sparkle: {ex.Message}");
                return null;
            }
        }

        private static void CheckForUpdatesWithSparkle(NSObject? updaterController)
        {
            if (updaterController == null)
            {
                ShowNotification("Update Error", "Sparkle updater not available");
                return;
            }

            try
            {
                // Call [updaterController checkForUpdates:nil]
                var selector = Selector.GetHandle("checkForUpdates:");
                ObjCRuntime.Messaging.void_objc_msgSend_IntPtr(
                    updaterController.Handle, 
                    selector, 
                    IntPtr.Zero
                );
            }
            catch (Exception ex)
            {
                ShowNotification("Update Error", $"Failed to check for updates: {ex.Message}");
            }
        }

        private static void ShowNotification(string title, string message)
        {
            var content = new UNMutableNotificationContent();
            content.Title = title;
            content.Body = message;

            var request = UNNotificationRequest.FromIdentifier(
                Guid.NewGuid().ToString(),
                content,
                null
            );

            UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
        }
#endif
    }
}