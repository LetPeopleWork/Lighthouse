use tauri::{Manager, Emitter};
use tauri_plugin_shell::ShellExt;
use tauri_plugin_shell::process::CommandEvent;
use regex::Regex;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            let shell = app.shell();
            let handle = app.handle().clone();

            // 1. Define the sidecar. 
            // We pass "--urls http://127.0.0.1:0" to force .NET to pick a random port.
            let sidecar = shell
                .sidecar("Lighthouse.Backend")
                .expect("failed to create sidecar command")
                .args(["--urls", "http://127.0.0.1:0"]);

            let resource_dir = app
                .path()
                .resource_dir()
                .expect("failed to get resource dir");

            // 2. Spawn the sidecar
            let (mut rx, _child) = sidecar
                .env("Standalone", "true")
                .env("LIGHTHOUSE_RESOURCES_DIR", resource_dir.to_str().unwrap())
                .spawn()
                .expect("failed to spawn backend sidecar");

            // 3. Monitor sidecar output in a background task
            tauri::async_runtime::spawn(async move {
                // Regex to find the URL in your C# PrintSystemInfo banner
                // Matches "Url             : http://127.0.0.1:54321"
                let re = Regex::new(r"Url\s+:\s+(http://[^\s]+)").unwrap();

                while let Some(event) = rx.recv().await {
                    match event {
                        CommandEvent::Stdout(line) => {
                            let out = String::from_utf8_lossy(&line);

                            println!("[RAW BACKEND]: {}", out);
                            
                            // Always print to the terminal for debugging
                            print!("{}", out);

                            // Check if this line contains the assigned URL
                            if let Some(caps) = re.captures(&out) {
                                if let Some(matched_url) = caps.get(1) {
                                    let detected_url = matched_url.as_str().to_string();
                                    
                                    // Log it in Rust console
                                    println!("\n[Tauri] 🎯 Backend dynamic port detected: {}", detected_url);
                                    
                                    // Emit to Frontend (TS/JS)
                                    let _ = handle.emit("backend-ready", detected_url);
                                }
                            }
                        }
                        CommandEvent::Stderr(line) => {
                            let err = String::from_utf8_lossy(&line);
                            eprint!("[backend stderr] {}", err);
                        }
                        CommandEvent::Terminated(payload) => {
                            println!("\n[backend] process terminated: {:?}", payload);
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