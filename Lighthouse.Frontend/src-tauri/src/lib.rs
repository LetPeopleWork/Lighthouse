use regex::Regex;
use tauri::{Emitter, Manager};
use tauri_plugin_dialog::{DialogExt, MessageDialogButtons, MessageDialogKind};
use tauri_plugin_shell::process::CommandEvent;
use tauri_plugin_shell::ShellExt;
use tauri_plugin_updater::UpdaterExt;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_dialog::init())
        .setup(|app| {
            let shell = app.shell();
            let handle = app.handle().clone();

            let sidecar = shell
                .sidecar("Lighthouse.Backend")
                .expect("failed to create sidecar command")
                .args(["--urls", "http://127.0.0.1:0"]);

            let resource_dir = handle
                .path()
                .resource_dir()
                .expect("failed to get resource dir");

            // 1. Spawn the sidecar
            // Note: child is not cloned; it stays in this scope.
            let (mut rx, child) = sidecar
                .env("Standalone", "true")
                .env(
                    "LIGHTHOUSE_RESOURCES_DIR",
                    resource_dir.to_string_lossy().to_string(),
                )
                .spawn()
                .expect("failed to spawn backend sidecar");

            // 2. Monitor sidecar output
            tauri::async_runtime::spawn(async move {
                let re = Regex::new(r"Url\s+:\s+(http://[^\s]+)").unwrap();

                while let Some(event) = rx.recv().await {
                    match event {
                        CommandEvent::Stdout(line) => {
                            let out = String::from_utf8_lossy(&line);
                            print!("{}", out);

                            if let Some(caps) = re.captures(&out) {
                                if let Some(matched_url) = caps.get(1) {
                                    let detected_url = matched_url.as_str().to_string();
                                    let handle_clone = handle.clone();

                                    tauri::async_runtime::spawn(async move {
                                        tokio::time::sleep(std::time::Duration::from_millis(500))
                                            .await;

                                        if let Some(main_win) =
                                            handle_clone.get_webview_window("main")
                                        {
                                            // 2. Give the JS a few ms to save to sessionStorage before the reload kills the state
                                            tokio::time::sleep(std::time::Duration::from_millis(
                                                300,
                                            ))
                                            .await;

                                            // 1. First, tell the app the URL (to save to sessionStorage)
                                            let _ =
                                                handle_clone.emit("backend-ready", &detected_url);
                                            let _ = main_win.eval("window.location.reload();");

                                            // 3. Reveal and refresh
                                            main_win.show().unwrap();
                                            main_win.set_focus().unwrap();
                                        }
                                    });
                                }
                            }
                        }
                        CommandEvent::Stderr(line) => {
                            eprint!("{}", String::from_utf8_lossy(&line));
                        }
                        CommandEvent::Terminated(payload) => {
                            println!("\n[Tauri] Backend process terminated: {:?}", payload);
                            break;
                        }
                        _ => {}
                    }
                }
            });

            // 3. Keep the child alive
            // We store the child in Tauri's state so it lives as long as the app.
            // When Tauri shuts down, this state is dropped, and 'child' is killed.
            app.manage(child);

            // 4. Check for updates (prompt-first)
            let update_handle = app.handle().clone();
            tauri::async_runtime::spawn(async move {
                // Give the app time to fully start before checking
                tokio::time::sleep(std::time::Duration::from_secs(10)).await;

                let updater = match update_handle.updater() {
                    Ok(updater) => updater,
                    Err(e) => {
                        eprintln!("[tauri-updater] Failed to create updater: {e}");
                        return;
                    }
                };

                match updater.check().await {
                    Ok(Some(update)) => {
                        println!(
                            "[tauri-updater] Update available: {} -> {}",
                            update.current_version, update.version
                        );
                        let dialog_handle = update_handle.clone();
                        let version = update.version.clone();
                        let confirmed = tokio::task::spawn_blocking(move || -> bool {
                            dialog_handle
                                .dialog()
                                .message(format!(
                                    "A new version of Lighthouse ({}) is available.\n\nWould you like to update now?",
                                    version
                                ))
                                .title("Lighthouse Update Available")
                                .kind(MessageDialogKind::Info)
                                .buttons(MessageDialogButtons::OkCancelCustom(
                                    "Update Now".into(),
                                    "Later".into(),
                                ))
                                .blocking_show()
                        })
                        .await
                        .unwrap_or(false);

                        if confirmed {
                            println!("[tauri-updater] User confirmed update, downloading...");
                            match update.download_and_install(|_, _| {}, || {}).await {
                                Ok(_) => {
                                    println!("[tauri-updater] Update installed, restarting...");
                                    update_handle.restart();
                                }
                                Err(e) => {
                                    eprintln!("[tauri-updater] Failed to install update: {e}");
                                    let err_handle = update_handle.clone();
                                    let err_msg = format!("Update installation failed:\n\n{}", e);
                                    let _ = tokio::task::spawn_blocking(move || {
                                        err_handle
                                            .dialog()
                                            .message(err_msg)
                                            .title("Update Failed")
                                            .kind(MessageDialogKind::Error)
                                            .blocking_show();
                                    })
                                    .await;
                                }
                            }
                        } else {
                            println!("[tauri-updater] User deferred update");
                        }
                    }
                    Ok(None) => {
                        println!("[tauri-updater] No update available");
                    }
                    Err(e) => {
                        eprintln!("[tauri-updater] Update check failed: {e}");
                    }
                }
            });

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running Lighthouse");
}
