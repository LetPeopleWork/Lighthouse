---
title: Run As Service
layout: home
parent: Standard Installation
grand_parent: Installation
nav_order: 1
---

If you don't want to manually start Lighthouse on startup, you can register it to run as a service instead. That way it will run automatically in the background, fetching continuously the latest data and re-forecast.

- TOC
{:toc}

### Windows
To run `Lighthouse.exe` as a Windows service, you can use the `New-Service` cmdlet in PowerShell. Follow these steps:

1. Open PowerShell as an Administrator.
2. Execute the following command to create a new service:

```powershell
New-Service -Name "LighthouseService" -BinaryPathName "C:\path\to\Lighthouse.exe" -DisplayName "Lighthouse Service" -Description "Service to run Lighthouse application" -StartupType Automatic
```

Replace `C:\path\to\Lighthouse.exe` with the actual path to your `Lighthouse.exe` file.

3. Start the service with the following command:

```powershell
Start-Service -Name "LighthouseService"
```

4. To ensure the service starts automatically after a reboot, you can check its status with:

```powershell
Get-Service -Name "LighthouseService"
```

This will run `Lighthouse.exe` as a Windows service, ensuring it starts automatically and runs in the background.

{: .note }
Make sure to stop the Lighthouse service before updating to avoid any conflicts.

### Linux
To run `Lighthouse` as a service on Linux using `systemd`, follow these steps:

1. Create a new service file for Lighthouse. Open a terminal and run:

```bash
sudo nano /etc/systemd/system/lighthouse.service
```

2. Add the following content to the file:

```ini
[Unit]
Description=Lighthouse Service
After=network.target

[Service]
ExecStart=/path/to/Lighthouse
Restart=always
User=nobody
Group=nogroup

[Install]
WantedBy=multi-user.target
```

Replace `/path/to/Lighthouse` with the actual path to your `Lighthouse` executable.

3. Save and close the file.

4. Reload the systemd manager configuration to apply the changes:

```bash
sudo systemctl daemon-reload
```

5. Enable the Lighthouse service to start on boot:

```bash
sudo systemctl enable lighthouse.service
```

6. Start the Lighthouse service:

```bash
sudo systemctl start lighthouse.service
```

7. Check the status of the service to ensure it is running correctly:

```bash
sudo systemctl status lighthouse.service
```

This will run `Lighthouse` as a service on Linux, ensuring it starts automatically and runs in the background.


{: .note }
Make sure to stop the Lighthouse service before updating to avoid any conflicts.

```bash
sudo systemctl stop lighthouse.service
```

#### Uninstall
To remove the Lighthouse service, execute these commands:

```bash
sudo systemctl stop lighthouse.service
sudo systemctl disable lighthouse.service
sudo rm /etc/systemd/system/lighthouse.service
sudo systemctl daemon-reload
```

### MacOS
To run `Lighthouse` as a service on MacOS using `launchd`, follow these steps:

1. Create a new plist file for Lighthouse. Open a terminal and run:

```bash
sudo nano /Library/LaunchDaemons/com.lighthouse.plist
```

2. Add the following content to the file:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
   <key>Label</key>
   <string>com.lighthouse</string>
   <key>ProgramArguments</key>
   <array>
      <string>/path/to/Lighthouse</string>
   </array>
   <key>RunAtLoad</key>
   <true/>
   <key>KeepAlive</key>
   <true/>
   <key>StandardOutPath</key>
   <string>/var/log/lighthouse.log</string>
   <key>StandardErrorPath</key>
   <string>/var/log/lighthouse.err</string>
</dict>
</plist>
```

Replace `/path/to/Lighthouse` with the actual path to your `Lighthouse` executable.

3. Save and close the file.

4. Load the new service:

```bash
sudo launchctl load /Library/LaunchDaemons/com.lighthouse.plist
```

5. Start the Lighthouse service:

```bash
sudo launchctl start com.lighthouse
```

6. Check the status of the service to ensure it is running correctly:

```bash
sudo launchctl list | grep com.lighthouse
```

This will run `Lighthouse` as a service on MacOS, ensuring it starts automatically and runs in the background.

{: .note }
Make sure to stop the Lighthouse service before updating to avoid any conflicts.
