use tauri_plugin_shell::ShellExt;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            let shell = app.shell();

            let sidecar = shell.sidecar("Lighthouse.Backend")
                .expect("failed to create sidecar command");

            let (mut rx, _child) = sidecar
                .args(["--Standalone=true"])
                .spawn()
                .expect("failed to spawn backend sidecar");

            // Log sidecar output in background
            tauri::async_runtime::spawn(async move {
                use tauri_plugin_shell::process::CommandEvent;
                while let Some(event) = rx.recv().await {
                    match event {
                        CommandEvent::Stdout(line) => {
                            let line = String::from_utf8_lossy(&line);
                            println!("[backend stdout] {}", line);
                        }
                        CommandEvent::Stderr(line) => {
                            let line = String::from_utf8_lossy(&line);
                            eprintln!("[backend stderr] {}", line);
                        }
                        CommandEvent::Terminated(payload) => {
                            println!("[backend] process terminated: {:?}", payload);
                            break;
                        }
                        _ => {}
                    }
                }
            });

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running Lighthouse");
}
