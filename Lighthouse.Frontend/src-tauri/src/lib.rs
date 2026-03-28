use regex::Regex;
use std::sync::{Arc, Mutex};
use tauri::{Emitter, Manager};
use tauri_plugin_dialog::{DialogExt, MessageDialogButtons, MessageDialogKind};
use tauri_plugin_shell::process::{CommandChild, CommandEvent};
use tauri_plugin_shell::ShellExt;
use tauri_plugin_updater::UpdaterExt;

struct SidecarHandle(Arc<Mutex<Option<CommandChild>>>);

fn kill_sidecar(handle: &Arc<Mutex<Option<CommandChild>>>) {
    if let Ok(mut guard) = handle.lock() {
        if let Some(child) = guard.take() {
            match child.kill() {
                Ok(_) => println!("[tauri] Sidecar killed successfully"),
                Err(e) => eprintln!("[tauri] Failed to kill sidecar: {e}"),
            }
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let child_arc: Arc<Mutex<Option<CommandChild>>> = Arc::new(Mutex::new(None));
    let child_arc_for_exit = child_arc.clone();

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_dialog::init())
        .setup(move |app| {
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

            let (mut rx, child) = sidecar
                .env("Standalone", "true")
                .env(
                    "LIGHTHOUSE_RESOURCES_DIR",
                    resource_dir.to_string_lossy().to_string(),
                )
                .spawn()
                .expect("failed to spawn backend sidecar");

            // Store child in the shared arc
            *child_arc.lock().unwrap() = Some(child);
            app.manage(SidecarHandle(child_arc.clone()));

            // Monitor sidecar output
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
                                            tokio::time::sleep(std::time::Duration::from_millis(
                                                300,
                                            ))
                                            .await;

                                            let _ =
                                                handle_clone.emit("backend-ready", &detected_url);
                                            let _ = main_win.eval("window.location.reload();");

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

            // Check for updates
            let update_handle = app.handle().clone();
            let child_arc_for_update = child_arc.clone();
            tauri::async_runtime::spawn(async move {
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

                            let child_arc_for_callback = child_arc_for_update.clone();
                            match update
                                .download_and_install(
                                    |_, _| {},
                                    move || {
                                        // Last chance to kill the sidecar before the installer
                                        // takes over — critical on Windows where NSIS launches
                                        // immediately and needs to overwrite files
                                        println!("[tauri-updater] Install callback — killing sidecar before installer takes over");
                                        kill_sidecar(&child_arc_for_callback);
                                    },
                                )
                                .await
                            {
                                Ok(_) => {
                                    // Windows: NSIS handles the restart automatically,
                                    // sidecar already killed in the callback above
                                    #[cfg(target_os = "windows")]
                                    {
                                        println!("[tauri-updater] Update handed off to Windows installer");
                                    }
                                    // macOS/Linux: sidecar already killed in callback, restart explicitly
                                    #[cfg(not(target_os = "windows"))]
                                    {
                                        println!("[tauri-updater] Update installed, restarting...");
                                        update_handle.restart();
                                    }
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
        .build(tauri::generate_context!())
        .expect("error while running Lighthouse")
        .run(move |_app_handle, event| {
            // Kill the sidecar on any app exit — covers normal quit on all platforms
            if let tauri::RunEvent::Exit = event {
                kill_sidecar(&child_arc_for_exit);
            }
        });
}
