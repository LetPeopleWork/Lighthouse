import Cocoa
import Sparkle
import UserNotifications

class AppDelegate: NSObject, NSApplicationDelegate {

    var updaterController: SPUStandardUpdaterController!
    var lighthouseProcess: Process?
    var statusItem: NSStatusItem?
    var serverURL: String = "http://localhost:5002"
    let lockfile = "/tmp/lighthouse.lock"

    func applicationDidFinishLaunching(_ notification: Notification) {
        // FIX 1: Explicitly set to accessory mode so the tray icon appears
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

    // MARK: - Menu Bar logic
    func setupMenuBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)

        if let button = statusItem?.button {
            // FIX 2: Correct path for the icon in the Resources folder
            if let iconURL = Bundle.main.url(forResource: "MenuBarIcon", withExtension: "png") {
                let image = NSImage(contentsOf: iconURL)
                image?.size = NSSize(width: 18, height: 18)
                image?.isTemplate = true
                button.image = image
            } else {
                button.image = NSImage(systemSymbolName: "house.fill", accessibilityDescription: "Lighthouse")
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
        menu.addItem(NSMenuItem(title: "Check for Updates...", action: #selector(checkForUpdates), keyEquivalent: "u")).target = self
        
        menu.addItem(.separator())
        menu.addItem(NSMenuItem(title: "Quit Lighthouse", action: #selector(quitApp), keyEquivalent: "q")).target = self

        statusItem?.menu = menu
    }

    // MARK: - Process Management
    func launchLighthouse() {
        // FIX 3: Correct pathing for the .NET binary
        let bundleURL = Bundle.main.bundleURL
        let macOSURL = bundleURL.appendingPathComponent("Contents/MacOS")
        let executableURL = macOSURL.appendingPathComponent("Lighthouse")

        guard FileManager.default.fileExists(atPath: executableURL.path) else {
            showError("Binary not found at \(executableURL.path)")
            return
        }

        // Your original Lockfile check
        if FileManager.default.fileExists(atPath: lockfile),
           let pidString = try? String(contentsOfFile: lockfile),
           let pid = Int32(pidString.trimmingCharacters(in: .whitespacesAndNewlines)),
           kill(pid, 0) == 0 {
            showNotification(title: "Lighthouse", message: "Lighthouse is already running.")
            NSApp.terminate(nil)
            return
        }

        try? FileManager.default.removeItem(atPath: lockfile)

        lighthouseProcess = Process()
        lighthouseProcess?.executableURL = executableURL
        
        // FIX 4: Set Working Directory so .NET finds appsettings/wwwroot
        lighthouseProcess?.currentDirectoryURL = macOSURL
        lighthouseProcess?.arguments = Array(CommandLine.arguments.dropFirst())

        let pipe = Pipe()
        lighthouseProcess?.standardOutput = pipe
        lighthouseProcess?.standardError = pipe

        // Store PID in lockfile
        try? "\(ProcessInfo.processInfo.processIdentifier)".write(toFile: lockfile, atomically: true, encoding: .utf8)

        do {
            try lighthouseProcess?.run()
            monitorOutput(pipe: pipe)

            // Handle clean exit
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