use chrono::Utc;
use regex::Regex;
use serde::{Deserialize, Serialize};
use std::fs::{self, OpenOptions};
use std::io;
use std::path::{Path, PathBuf};
use std::sync::{Arc, Mutex};
use tauri::{Emitter, Manager};
use tauri_plugin_dialog::{DialogExt, MessageDialogButtons, MessageDialogKind};
use tauri_plugin_shell::process::{CommandChild, CommandEvent};
use tauri_plugin_shell::ShellExt;
use tauri_plugin_updater::UpdaterExt;

const STANDALONE_DISCOVERY_LOCKFILE_NAME: &str = "standalone.lock.json";

#[derive(Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct StandaloneDiscoveryContract {
    contract_version: u8,
    lighthouse_url: String,
    detected_at_utc: String,
    pid: u32,
}

fn get_standalone_app_data_dir() -> Result<PathBuf, String> {
    if cfg!(target_os = "macos") {
        let home_dir = std::env::var_os("HOME")
            .ok_or_else(|| "HOME environment variable is not set".to_string())?;
        return Ok(PathBuf::from(home_dir)
            .join("Library")
            .join("Application Support")
            .join("Lighthouse"));
    }

    if cfg!(target_os = "windows") {
        let app_data_dir = std::env::var_os("APPDATA")
            .ok_or_else(|| "APPDATA environment variable is not set".to_string())?;
        return Ok(PathBuf::from(app_data_dir).join("Lighthouse"));
    }

    if let Some(config_home) = std::env::var_os("XDG_CONFIG_HOME") {
        return Ok(PathBuf::from(config_home).join("Lighthouse"));
    }

    let home_dir = std::env::var_os("HOME")
        .ok_or_else(|| "HOME environment variable is not set".to_string())?;
    Ok(PathBuf::from(home_dir).join(".config").join("Lighthouse"))
}

fn get_standalone_lockfile_path() -> Result<PathBuf, String> {
    Ok(get_standalone_app_data_dir()?.join(STANDALONE_DISCOVERY_LOCKFILE_NAME))
}

fn is_pid_alive(pid: u32) -> bool {
    #[cfg(unix)]
    {
        Path::new(&format!("/proc/{}", pid)).exists()
    }

    #[cfg(windows)]
    {
        use std::os::windows::io::FromRawHandle;
        unsafe {
            let handle = windows_sys::Win32::System::Threading::OpenProcess(
                windows_sys::Win32::System::Threading::SYNCHRONIZE,
                0,
                pid,
            );
            if handle == 0 {
                false
            } else {
                windows_sys::Win32::Foundation::CloseHandle(handle);
                true
            }
        }
    }

    #[cfg(not(any(unix, windows)))]
    {
        // Unknown platform — assume alive to be safe
        true
    }
}

fn clear_lockfile_if_stale(lockfile_path: &Path) {
    let contents = match fs::read(lockfile_path) {
        Ok(c) => c,
        Err(_) => return, // File doesn't exist or can't be read — nothing to do
    };

    let contract = match serde_json::from_slice::<StandaloneDiscoveryContract>(&contents) {
        Ok(c) => c,
        Err(e) => {
            eprintln!("[tauri] Lockfile is unreadable/corrupt, removing ({e})");
            delete_standalone_lockfile(lockfile_path);
            return;
        }
    };

    if !is_pid_alive(contract.pid) {
        println!(
            "[tauri] Stale lockfile detected (PID {} is no longer alive), removing",
            contract.pid
        );
        delete_standalone_lockfile(lockfile_path);
    } else {
        println!(
            "[tauri] Lockfile exists and PID {} is alive — another instance may be running",
            contract.pid
        );
    }
}

fn reserve_standalone_lockfile(lockfile_path: &Path) -> Result<(), String> {
    if let Some(parent) = lockfile_path.parent() {
        fs::create_dir_all(parent).map_err(|error| {
            format!(
                "Failed to create standalone app data directory {}: {error}",
                parent.display()
            )
        })?;
    }

    OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(lockfile_path)
        .map(|_| ())
        .map_err(|error| {
            if error.kind() == io::ErrorKind::AlreadyExists {
                format!(
                    "A Lighthouse standalone instance is already running. Lockfile: {}",
                    lockfile_path.display()
                )
            } else {
                format!(
                    "Failed to reserve standalone lockfile {}: {error}",
                    lockfile_path.display()
                )
            }
        })
}

fn write_standalone_lockfile(
    lockfile_path: &Path,
    detected_url: &str,
    pid: u32,
) -> Result<(), String> {
    let payload = StandaloneDiscoveryContract {
        contract_version: 1,
        lighthouse_url: detected_url.to_string(),
        detected_at_utc: Utc::now().to_rfc3339_opts(chrono::SecondsFormat::Millis, true),
        pid,
    };
    let serialized_payload = serde_json::to_vec_pretty(&payload)
        .map_err(|error| format!("Failed to serialize standalone discovery contract: {error}"))?;
    let temp_path = lockfile_path.with_extension("tmp");

    fs::write(&temp_path, serialized_payload).map_err(|error| {
        format!(
            "Failed to write standalone lockfile {}: {error}",
            temp_path.display()
        )
    })?;
    fs::rename(&temp_path, lockfile_path).map_err(|error| {
        format!(
            "Failed to finalize standalone lockfile {}: {error}",
            lockfile_path.display()
        )
    })
}

fn delete_standalone_lockfile(lockfile_path: &Path) {
    match fs::remove_file(lockfile_path) {
        Ok(_) => println!(
            "[tauri] Standalone lockfile deleted: {}",
            lockfile_path.display()
        ),
        Err(error) if error.kind() == io::ErrorKind::NotFound => {}
        Err(error) => eprintln!(
            "[tauri] Failed to delete standalone lockfile {}: {error}",
            lockfile_path.display()
        ),
    }
}

fn kill_sidecar(handle: &Arc<Mutex<Option<CommandChild>>>, lockfile_path: &Path) {
    if let Ok(mut guard) = handle.lock() {
        if let Some(child) = guard.take() {
            match child.kill() {
                Ok(_) => println!("[tauri] Sidecar killed successfully"),
                Err(e) => eprintln!("[tauri] Failed to kill sidecar: {e}"),
            }
        }
    }

    delete_standalone_lockfile(lockfile_path);
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let child_arc: Arc<Mutex<Option<CommandChild>>> = Arc::new(Mutex::new(None));
    let pid_arc: Arc<Mutex<Option<u32>>> = Arc::new(Mutex::new(None));
    let lockfile_path = get_standalone_lockfile_path()
        .expect("failed to resolve standalone discovery lockfile path");
    let child_arc_for_exit = child_arc.clone();
    let lockfile_path_for_exit = lockfile_path.clone();

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_dialog::init())
        .setup(move |app| -> Result<(), Box<dyn std::error::Error>> {
            let shell = app.shell();
            let handle = app.handle().clone();
            let lockfile_path = lockfile_path.clone();

            // Remove stale lockfile if the previously recorded PID is no longer alive
            clear_lockfile_if_stale(&lockfile_path);

            if let Err(error) = reserve_standalone_lockfile(&lockfile_path) {
                app.dialog()
                    .message(&error)
                    .title("Lighthouse Already Running")
                    .kind(MessageDialogKind::Error)
                    .blocking_show();

                return Err(io::Error::new(io::ErrorKind::AlreadyExists, error).into());
            }

            let sidecar = shell
                .sidecar("Lighthouse.Backend")
                .expect("failed to create sidecar command")
                .args(["--urls", "http://127.0.0.1:0"]);

            let resource_dir = handle
                .path()
                .resource_dir()
                .expect("failed to get resource dir");

            let spawn_result = sidecar
                .env("Standalone", "true")
                .env(
                    "LIGHTHOUSE_RESOURCES_DIR",
                    resource_dir.to_string_lossy().to_string(),
                )
                .spawn();

            let (mut rx, child) = match spawn_result {
                Ok(result) => result,
                Err(error) => {
                    delete_standalone_lockfile(&lockfile_path);
                    return Err(error.into());
                }
            };

            // Store child PID before moving child into the arc
            let sidecar_pid = child.pid();
            *pid_arc.lock().unwrap() = Some(sidecar_pid);

            // Store child in the shared arc
            *child_arc.lock().unwrap() = Some(child);

            // Monitor sidecar output
            let lockfile_path_for_monitor = lockfile_path.clone();
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

                                    if let Err(error) = write_standalone_lockfile(
                                        &lockfile_path_for_monitor,
                                        &detected_url,
                                        sidecar_pid,
                                    ) {
                                        eprintln!(
                                            "[tauri] Failed to write standalone lockfile: {error}"
                                        );
                                    }

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
                            delete_standalone_lockfile(&lockfile_path_for_monitor);
                            break;
                        }
                        _ => {}
                    }
                }
            });

            // Check for updates
            let update_handle = app.handle().clone();
            let child_arc_for_update = child_arc.clone();
            let lockfile_path_for_update = lockfile_path.clone();
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
                                        kill_sidecar(
                                            &child_arc_for_callback,
                                            &lockfile_path_for_update,
                                        );
                                    },
                                )
                                .await
                            {
                                Ok(_) => {
                                    // Windows: NSIS handles the restart automatically,
                                    // sidecar already killed in the callback above
                                    #[cfg(target_os = "windows")]
                                    {
                                        println!(
                                            "[tauri-updater] Update handed off to Windows installer"
                                        );
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
                kill_sidecar(&child_arc_for_exit, &lockfile_path_for_exit);
            }
        });
}
