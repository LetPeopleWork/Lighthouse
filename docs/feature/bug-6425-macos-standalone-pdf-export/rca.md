# RCA — Bug #6425: Export PDF does nothing in the Standalone app on macOS

Investigated 2026-10-27 (nw-troubleshooter), verified by the orchestrator, root cause confirmed by the maintainer.

## Facts
- Fails: macOS Standalone (Tauri → WKWebView). Works: browser against the Docker image on macOS, Standalone on Linux
  (WebKitGTK) and Windows (WebView2).

## Root cause
Export PDF prints from a hidden srcdoc iframe: `browserPrintEffects.ts:61` `print: () => target.print()`, where `target`
is the frame's own window. In Tauri on macOS, `iframe.contentWindow.print()` is a silent no-op — open upstream issue
tauri-apps/tauri#13451 (`platform: macOS`, `status: upstream`; maintainers say a proper fix needs a private WebKit API).
Tauri 2.12 patches only the top-level `window.print` on macOS (`tauri-2.12.x/src/webview/plugin.rs:198`, routing to
`plugin:webview|print` → wry `NSPrintOperation` of the main webview), and the app's capability set
(`src-tauri/capabilities/default.json`) does not grant `core:webview:allow-print` either. Nothing throws, so no error shows.

Why it shipped: Epic 6298 decided to confirm Standalone printing in production; the engine probe ran on Linux only, and
macOS was never probed.

## Fix direction (maintainer, 2026-10-27)
Too niche for a host-print rework. On the macOS Standalone app only, Export PDF is disabled (report list icon and report
header button) with the tooltip:
"PDF export is not available in the macOS desktop app. To export PDFs on a Mac, run Lighthouse with Docker and open it in your browser."
Every other host is unchanged. Revisit when tauri#13451 is fixed upstream.
