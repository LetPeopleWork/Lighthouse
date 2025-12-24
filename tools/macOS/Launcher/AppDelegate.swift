import Cocoa
import Sparkle
import UserNotifications

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

        // Request notification permission
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { granted, error in
            if let error = error {
                NSLog("Notification permission error: \(error)")
            }
        }

        // Create menu bar icon
        setupMenuBar()

        // Launch the Lighthouse .NET app
        launchLighthouse()

        // Try to detect the actual server URL
        detectServerURL()
    }

    // MARK: - Menu Bar

    func setupMenuBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)

        if let button = statusItem?.button {
            let bundle = Bundle.main
            if let resourcePath = bundle.resourcePath,
               let image = NSImage(contentsOfFile: "\(resourcePath)/MenuBarIcon.png") {
                image.size = NSSize(width: 18, height: 18)
                image.isTemplate = true
                button.image = image
            } else if let image = NSImage(systemSymbolName: "lighthouse.fill",
                                          accessibilityDescription: "Lighthouse") {
                button.image = image
            } else {
                button.title = "🏠"
            }
        }

        updateMenu()
    }

    func updateMenu() {
        let menu = NSMenu()

        menu.addItem(NSMenuItem(title: "Lighthouse", action: nil, keyEquivalent: ""))
        menu.addItem(.separator())

        let openItem = NSMenuItem(title: "Open in Browser",
                                  action: #selector(openInBrowser),
                                  keyEquivalent: "o")
        openItem.target = self
        menu.addItem(openItem)

        let copyItem = NSMenuItem(title: "Copy URL",
                                  action: #selector(copyURL),
                                  keyEquivalent: "c")
        copyItem.target = self
        menu.addItem(copyItem)

        menu.addItem(.separator())

        let checkUpdateItem = NSMenuItem(title: "Check for Updates...",
                                         action: #selector(checkForUpdates),
                                         keyEquivalent: "u")
        checkUpdateItem.target = self
        menu.addItem(checkUpdateItem)

        menu.addItem(.separator())

        let quitItem = NSMenuItem(title: "Quit Lighthouse",
                                  action: #selector(quitApp),
                                  keyEquivalent: "q")
        quitItem.target = self
        menu.addItem(quitItem)

        statusItem?.menu = menu
    }

    // MARK: - Lighthouse Process

    func launchLighthouse() {
        let bundle = Bundle.main
        guard let executablePath = bundle.path(
            forResource: "Lighthouse",
            ofType: nil,
            inDirectory: "MacOS"
        ) else {
            showError("Failed to find Lighthouse executable. Please reinstall the application.")
            NSApp.terminate(nil)
            return
        }

        let lockfile = "/tmp/lighthouse.lock"

        if FileManager.default.fileExists(atPath: lockfile),
           let pidString = try? String(contentsOfFile: lockfile),
           let pid = Int32(pidString.trimmingCharacters(in: .whitespacesAndNewlines)),
           kill(pid, 0) == 0 {

            showNotification(
                title: "Lighthouse",
                message: "Lighthouse is already running."
            )
            NSApp.terminate(nil)
            return
        }

        try? FileManager.default.removeItem(atPath: lockfile)

        lighthouseProcess = Process()
        lighthouseProcess?.executableURL = URL(fileURLWithPath: executablePath)
        lighthouseProcess?.arguments = Array(CommandLine.arguments.dropFirst())

        let pipe = Pipe()
        lighthouseProcess?.standardOutput = pipe
        lighthouseProcess?.standardError = pipe

        try? "\(ProcessInfo.processInfo.processIdentifier)".write(
            toFile: lockfile,
            atomically: true,
            encoding: .utf8
        )

        do {
            try lighthouseProcess?.run()
            monitorOutput(pipe: pipe)

            DispatchQueue.global(qos: .background).async { [weak self] in
                self?.lighthouseProcess?.waitUntilExit()
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
            showError("Failed to launch Lighthouse: \(error.localizedDescription)")
            try? FileManager.default.removeItem(atPath: lockfile)
            NSApp.terminate(nil)
        }
    }

    func monitorOutput(pipe: Pipe) {
        let handle = pipe.fileHandleForReading

        handle.readabilityHandler = { [weak self] fileHandle in
            let data = fileHandle.availableData
            guard data.count > 0,
                  let output = String(data: data, encoding: .utf8) else { return }

            if let match = output.range(
                of: "Now listening on: (https?://[^\\s]+)",
                options: .regularExpression
            ) {
                let urlString = output[match]
                    .replacingOccurrences(of: "Now listening on: ", with: "")
                    .trimmingCharacters(in: .whitespacesAndNewlines)

                DispatchQueue.main.async {
                    self?.updateServerURL(urlString)
                }
            }

            print(output, terminator: "")
        }
    }

    func updateServerURL(_ urlString: String) {
        var url = urlString
        url = url.replacingOccurrences(of: "0.0.0.0", with: "localhost")
        url = url.replacingOccurrences(of: "[::1]", with: "localhost")
        url = url.replacingOccurrences(of: "[::]", with: "localhost")
        url = url.replacingOccurrences(of: "*", with: "localhost")

        serverURL = url
        NSLog("Detected server URL: \(serverURL)")
    }

    func detectServerURL() {
        if let env = ProcessInfo.processInfo.environment["ASPNETCORE_URLS"],
           let first = env.split(separator: ";").first {
            updateServerURL(String(first))
        }
    }

    // MARK: - Actions

    @objc func openInBrowser() {
        guard let url = URL(string: serverURL) else {
            showError("Invalid server URL: \(serverURL)")
            return
        }
        NSWorkspace.shared.open(url)
    }

    @objc func copyURL() {
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(serverURL, forType: .string)

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
        try? FileManager.default.removeItem(atPath: "/tmp/lighthouse.lock")
        NSApp.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        lighthouseProcess?.terminate()
        try? FileManager.default.removeItem(atPath: "/tmp/lighthouse.lock")
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        false
    }

    // MARK: - Notifications & Errors

    private func showError(_ message: String) {
        let alert = NSAlert()
        alert.messageText = "Lighthouse Error"
        alert.informativeText = message
        alert.alertStyle = .critical
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }

    private func showNotification(title: String, message: String) {
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = message
        content.sound = .default

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )

        UNUserNotificationCenter.current().add(request)
    }
}
