import Cocoa
import Sparkle
import UserNotifications

class AppDelegate: NSObject, NSApplicationDelegate {

    var updaterController: SPUStandardUpdaterController!
    var lighthouseProcess: Process?
    var statusItem: NSStatusItem?
    var serverURL: String = "http://localhost:5002"
    let lockfile = FileManager.default.temporaryDirectory.appendingPathComponent("lighthouse.lock").path

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)

        // Initialize Sparkle
        updaterController = SPUStandardUpdaterController(
            startingUpdater: true,
            updaterDelegate: nil,
            userDriverDelegate: nil
        )

        // Request notification permission (Your original logic)
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { granted, error in
            if let error = error { NSLog("Notification permission error: \(error)") }
        }

        setupMenuBar()
        launchLighthouse()
    }

    func setupMenuBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)

        if let button = statusItem?.button {
            if let iconURL = Bundle.main.url(forResource: "MenuBarIcon", withExtension: "png") {
                let image = NSImage(contentsOf: iconURL)
                image?.size = NSSize(width: 18, height: 18)
                image?.isTemplate = true
                button.image = image
            } else {
                // Version check for macOS 11.0+ SF Symbols
                if #available(macOS 11.0, *) {
                    button.image = NSImage(systemSymbolName: "house.fill", accessibilityDescription: "Lighthouse")
                } else {
                    // Fallback for macOS 10.15
                    button.title = "🏠"
                }
            }
        }
        updateMenu()
    }

    func updateMenu() {
        let menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Lighthouse", action: nil, keyEquivalent: ""))
        menu.addItem(.separator())

        let openItem = NSMenuItem(title: "Open in Browser", action: #selector(openInBrowser), keyEquivalent: "o")
        openItem.target = self
        menu.addItem(openItem)

        let copyItem = NSMenuItem(title: "Copy URL", action: #selector(copyURL), keyEquivalent: "c")
        copyItem.target = self
        menu.addItem(copyItem)

        menu.addItem(.separator())
        
        let updateItem = NSMenuItem(title: "Check for Updates...", action: #selector(checkForUpdates), keyEquivalent: "u")
        updateItem.target = self
        menu.addItem(updateItem)
        
        menu.addItem(.separator())
        
        let quitItem = NSMenuItem(title: "Quit Lighthouse", action: #selector(quitApp), keyEquivalent: "q")
        quitItem.target = self
        menu.addItem(quitItem)

        statusItem?.menu = menu
    }

    func launchLighthouse() {
        let bundleURL = Bundle.main.bundleURL
        let macOSURL = bundleURL.appendingPathComponent("Contents/MacOS")
        let executableURL = macOSURL.appendingPathComponent("Lighthouse")

        NSLog("Attempting to launch backend at: \(executableURL.path)")
    
        if !FileManager.default.isExecutableFile(atPath: executableURL.path) {
            NSLog("ERROR: Backend binary is not executable")
            // In CI, we might need to manually chmod if the build step missed it
            try? FileManager.default.setAttributes([.posixPermissions: 0o755], ofItemAtPath: executableURL.path)
        }

        guard FileManager.default.fileExists(atPath: executableURL.path) else {
            showError("Binary not found at \(executableURL.path)")
            return
        }

        if FileManager.default.fileExists(atPath: lockfile),
           let pidString = try? String(contentsOfFile: lockfile, encoding: .utf8),
           let pid = Int32(pidString.trimmingCharacters(in: .whitespacesAndNewlines)),
           kill(pid, 0) == 0 {
            showNotification(title: "Lighthouse", message: "Lighthouse is already running.")
            NSApp.terminate(nil)
            return
        }

        try? FileManager.default.removeItem(atPath: lockfile)

        lighthouseProcess = Process()
        lighthouseProcess?.executableURL = executableURL
        lighthouseProcess?.currentDirectoryURL = macOSURL
        lighthouseProcess?.arguments = Array(CommandLine.arguments.dropFirst())

        let pipe = Pipe()
        lighthouseProcess?.standardOutput = pipe
        lighthouseProcess?.standardError = pipe

        // Store PID
        try? "\(ProcessInfo.processInfo.processIdentifier)".write(toFile: lockfile, atomically: true, encoding: .utf8)

        do {
            try lighthouseProcess?.run()
            monitorOutput(pipe: pipe)

            DispatchQueue.global(qos: .background).async { [weak self] in
                self?.lighthouseProcess?.waitUntilExit()
                if let lock = self?.lockfile { try? FileManager.default.removeItem(atPath: lock) }
                DispatchQueue.main.async { NSApp.terminate(nil) }
            }
        } catch {
            showError("Failed to launch .NET: \(error.localizedDescription)")
            try? FileManager.default.removeItem(atPath: lockfile)
        }
    }

    func monitorOutput(pipe: Pipe) {
        pipe.fileHandleForReading.readabilityHandler = { [weak self] handle in
            let data = handle.availableData
            if let output = String(data: data, encoding: .utf8), !output.isEmpty {
                if let match = output.range(of: "Now listening on: (https?://[^\\s]+)", options: .regularExpression) {
                    let url = output[match].replacingOccurrences(of: "Now listening on: ", with: "").trimmingCharacters(in: .whitespacesAndNewlines)
                    DispatchQueue.main.async {
                        var finalUrl = url.replacingOccurrences(of: "0.0.0.0", with: "localhost")
                        finalUrl = finalUrl.replacingOccurrences(of: "*", with: "localhost")
                        self?.serverURL = finalUrl
                    }
                }
                print(output)
            }
        }
    }

    // MARK: - Actions
    @objc func openInBrowser() {
        if let url = URL(string: serverURL) { NSWorkspace.shared.open(url) }
    }

    @objc func copyURL() {
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(serverURL, forType: .string)
        showNotification(title: "URL Copied", message: "Copied to clipboard: \(serverURL)")
    }

    @objc func checkForUpdates() {
        updaterController.checkForUpdates(nil)
    }

    @objc func quitApp() {
        lighthouseProcess?.terminate()
        try? FileManager.default.removeItem(atPath: lockfile)
        NSApp.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        lighthouseProcess?.terminate()
        try? FileManager.default.removeItem(atPath: lockfile)
    }

    private func showError(_ message: String) {
        let alert = NSAlert()
        alert.messageText = "Lighthouse Error"
        alert.informativeText = message
        alert.alertStyle = .critical
        alert.runModal()
    }

    private func showNotification(title: String, message: String) {
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = message
        let request = UNNotificationRequest(identifier: UUID().uuidString, content: content, trigger: nil)
        UNUserNotificationCenter.current().add(request)
    }
}