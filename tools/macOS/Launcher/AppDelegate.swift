import Cocoa
import Sparkle

class AppDelegate: NSObject, NSApplicationDelegate {
    var updaterController: SPUStandardUpdaterController!
    var lighthouseProcess: Process?
    var statusItem: NSStatusItem?
    var serverURL: String = "http://localhost:5002" // Default, will be detected
    
    func applicationDidFinishLaunching(_ notification: Notification) {
        // Initialize Sparkle for automatic updates
        updaterController = SPUStandardUpdaterController(
            startingUpdater: true,
            updaterDelegate: nil,
            userDriverDelegate: nil
        )
        
        // Create menu bar icon
        setupMenuBar()
        
        // Launch the Lighthouse .NET app
        launchLighthouse()
        
        // Try to detect the actual server URL
        detectServerURL()
    }
    
    func setupMenuBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        
        if let button = statusItem?.button {
            // Load the menu bar icon from Resources
            let bundle = Bundle.main
            if let resourcePath = bundle.resourcePath,
            let image = NSImage(contentsOfFile: "\(resourcePath)/MenuBarIcon.png") {
                image.size = NSSize(width: 18, height: 18)
                image.isTemplate = true  // Makes it adapt to light/dark mode
                button.image = image
            } else {
                // Fallback to SF Symbol
                if let image = NSImage(systemSymbolName: "lighthouse.fill", accessibilityDescription: "Lighthouse") {
                    button.image = image
                } else {
                    button.title = "🏠"
                }
            }
        }
        
        updateMenu()
    }
    
    func updateMenu() {
        let menu = NSMenu()
        
        menu.addItem(NSMenuItem(title: "Lighthouse", action: nil, keyEquivalent: ""))
        menu.addItem(NSMenuItem.separator())
        
        let openItem = NSMenuItem(title: "Open in Browser", action: #selector(openInBrowser), keyEquivalent: "o")
        openItem.target = self
        menu.addItem(openItem)
        
        let copyItem = NSMenuItem(title: "Copy URL", action: #selector(copyURL), keyEquivalent: "c")
        copyItem.target = self
        menu.addItem(copyItem)
        
        menu.addItem(NSMenuItem.separator())
        
        let checkUpdateItem = NSMenuItem(title: "Check for Updates...", action: #selector(checkForUpdates), keyEquivalent: "u")
        checkUpdateItem.target = self
        menu.addItem(checkUpdateItem)
        
        menu.addItem(NSMenuItem.separator())
        
        let quitItem = NSMenuItem(title: "Quit Lighthouse", action: #selector(quitApp), keyEquivalent: "q")
        quitItem.target = self
        menu.addItem(quitItem)
        
        statusItem?.menu = menu
    }
    
    func launchLighthouse() {
        let bundle = Bundle.main
        guard let executablePath = bundle.path(forResource: "Lighthouse", ofType: nil, inDirectory: "MacOS") else {
            NSLog("Failed to find Lighthouse executable")
            showError("Failed to find Lighthouse executable. Please reinstall the application.")
            NSApp.terminate(nil)
            return
        }
        
        // Check for lockfile to prevent multiple instances
        let lockfile = "/tmp/lighthouse.lock"
        if FileManager.default.fileExists(atPath: lockfile) {
            if let pidString = try? String(contentsOfFile: lockfile, encoding: .utf8),
               let pid = Int32(pidString.trimmingCharacters(in: .whitespacesAndNewlines)) {
                // Check if process is still running
                if kill(pid, 0) == 0 {
                    showNotification(
                        title: "Lighthouse",
                        message: "Lighthouse is already running."
                    )
                    NSApp.terminate(nil)
                    return
                }
            }
            // Remove stale lockfile
            try? FileManager.default.removeItem(atPath: lockfile)
        }
        
        lighthouseProcess = Process()
        lighthouseProcess?.executableURL = URL(fileURLWithPath: executablePath)
        
        // Forward any command line arguments
        lighthouseProcess?.arguments = Array(CommandLine.arguments.dropFirst())
        
        // Capture output to detect the URL
        let pipe = Pipe()
        lighthouseProcess?.standardOutput = pipe
        lighthouseProcess?.standardError = pipe
        
        // Write PID to lockfile
        try? "\(ProcessInfo.processInfo.processIdentifier)".write(
            toFile: lockfile,
            atomically: true,
            encoding: .utf8
        )
        
        do {
            try lighthouseProcess?.run()
            
            // Monitor output for URL in background
            monitorOutput(pipe: pipe)
            
            // Monitor the process in background
            DispatchQueue.global(qos: .background).async { [weak self] in
                self?.lighthouseProcess?.waitUntilExit()
                
                // Clean up lockfile
                try? FileManager.default.removeItem(atPath: lockfile)
                
                DispatchQueue.main.async {
                    self?.showNotification(
                        title: "Lighthouse",
                        message: "Lighthouse has stopped."
                    )
                    NSApp.terminate(nil)
                }
            }
        } catch {
            NSLog("Failed to launch Lighthouse: \(error)")
            showError("Failed to launch Lighthouse: \(error.localizedDescription)")
            try? FileManager.default.removeItem(atPath: lockfile)
            NSApp.terminate(nil)
        }
    }
    
    func monitorOutput(pipe: Pipe) {
        let handle = pipe.fileHandleForReading
        
        handle.readabilityHandler = { [weak self] fileHandle in
            let data = fileHandle.availableData
            if data.count > 0, let output = String(data: data, encoding: .utf8) {
                // Look for ASP.NET Core server startup messages
                // Common patterns:
                // "Now listening on: http://localhost:5002"
                // "Now listening on: http://[::]:5002"
                // "Now listening on: http://0.0.0.0:5002"
                if let match = output.range(of: "Now listening on: (https?://[^\\s]+)", options: .regularExpression) {
                    let urlString = String(output[match])
                        .replacingOccurrences(of: "Now listening on: ", with: "")
                        .trimmingCharacters(in: .whitespacesAndNewlines)
                    
                    DispatchQueue.main.async {
                        self?.updateServerURL(urlString)
                    }
                }
                
                // Also forward output to Console for debugging
                print(output, terminator: "")
            }
        }
    }
    
    func updateServerURL(_ urlString: String) {
        var normalizedURL = urlString
        
        // Convert various bind addresses to localhost
        normalizedURL = normalizedURL.replacingOccurrences(of: "0.0.0.0", with: "localhost")
        normalizedURL = normalizedURL.replacingOccurrences(of: "[::1]", with: "localhost")
        normalizedURL = normalizedURL.replacingOccurrences(of: "[::]", with: "localhost")
        normalizedURL = normalizedURL.replacingOccurrences(of: "*", with: "localhost")
        
        self.serverURL = normalizedURL
        NSLog("Detected server URL: \(serverURL)")
    }
    
    func detectServerURL() {
        // As a fallback, try common environment variables and appsettings
        // This is a backup in case output monitoring fails
        
        // Check environment variables
        if let urlFromEnv = ProcessInfo.processInfo.environment["ASPNETCORE_URLS"] {
            let urls = urlFromEnv.split(separator: ";")
            if let firstURL = urls.first {
                updateServerURL(String(firstURL))
                return
            }
        }
        
        // Try to read appsettings.json (best effort)
        let bundle = Bundle.main
        if let settingsPath = bundle.path(forResource: "appsettings", ofType: "json", inDirectory: "MacOS"),
           let settingsData = try? Data(contentsOf: URL(fileURLWithPath: settingsPath)),
           let json = try? JSONSerialization.jsonObject(with: settingsData) as? [String: Any],
           let urls = json["Urls"] as? String ?? json["urls"] as? String {
            let urlList = urls.split(separator: ";")
            if let firstURL = urlList.first {
                updateServerURL(String(firstURL))
            }
        }
    }
    
    @objc func openInBrowser() {
        guard let url = URL(string: serverURL) else {
            showError("Invalid server URL: \(serverURL)")
            return
        }
        NSWorkspace.shared.open(url)
    }
    
    @objc func copyURL() {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(serverURL, forType: .string)
        
        showNotification(
            title: "URL Copied",
            message: "Server URL copied to clipboard: \(serverURL)"
        )
    }
    
    @objc func checkForUpdates() {
        updaterController.checkForUpdates(nil)
    }
    
    @objc func quitApp() {
        lighthouseProcess?.terminate()
        
        // Clean up lockfile
        try? FileManager.default.removeItem(atPath: "/tmp/lighthouse.lock")
        
        NSApp.terminate(nil)
    }
    
    func applicationWillTerminate(_ notification: Notification) {
        // Clean up: terminate the Lighthouse process if still running
        lighthouseProcess?.terminate()
        
        // Remove lockfile
        try? FileManager.default.removeItem(atPath: "/tmp/lighthouse.lock")
    }
    
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        // Keep running even if no windows (we're a menu bar app)
        return false
    }
    
    private func showError(_ message: String) {
        let alert = NSAlert()
        alert.messageText = "Lighthouse Error"
        alert.informativeText = message
        alert.alertStyle = .critical
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }
    
    private func showNotification(title: String, message: String) {
        let notification = NSUserNotification()
        notification.title = title
        notification.informativeText = message
        notification.soundName = NSUserNotificationDefaultSoundName
        NSUserNotificationCenter.default.deliver(notification)
    }
}