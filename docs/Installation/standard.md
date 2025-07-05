---
title: Standard Installation
layout: home
parent: Installation and Configuration
nav_order: 1
---

If you can't or don't want to use [Docker](./docker.html), you can also run Lighthouse on your system directly.

- TOC
{:toc}

## Prerequisites
The packages provided by Lighthouse have everything included you need to run it, so there are no prerequisites.

Lighthouse runs on Windows, MacOs, and Linux based systems.

## Download Lighthouse
Download the latest version of Lighthouse for your operating system from the [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest).
Download the zip file, and extract it to the location you want to run the application from.

## Run Lighthouse
Once downloaded, you can run the the `Lighthouse` application:
- `Lighthouse.exe` on Windows
- `Lighthouse` on MacOS and Linux

{: .note}
On Linux and MacOS it may be that the file *Lighthouse* is not appearing as executable (so it would show as a document). In such a case, you have to make it executable by running:  
`sudo chmod +x Lighthouse`

In order to run it, open a terminal, navigate to the folder where the Lighthouse executable is located, and run it through the terminal:
```bash
# Navigate to Lighthouse directory
cd /c/Users/benja/Downloads/Lighthouse-linux-x64

# Make executable
sudo chmod +x Lighthouse

# You will have to type in your password here, either on the consolre or via the UI
# After this you will be able to run Lighthouse:
./Lighthouse
```

{: .recommendation}
On Windows it should work by simply double-clicking the *Lighthouse.exe*.

Your terminal will start showing the logmessages and you should something similar to this:

![Starting Lighthouse](../assets/installation/startup.png)

By default, Lighthouse will start running on the system on port 5001. If everything worked as expected, you can open the app now in your browser via [https://localhost:5001](https://localhost:5001).

You should see the (empty) landing page:
![Landing Page](../assets/installation/landingpage.png)



## Updating Lighthouse
If a new version is released, you will see an indication on the lower right edge in the footer. If you click on it, a dialog will pop up and you see the release notes of all newer versions of yours.

{: .note }
As the published packages do not include the database, you will keep your data. Lighthouse will in normal circumstances always support migrations to newer versions, so you will not lose any data.

{: .recommendation}
We recommend that you stay on the latest versions. We continuously update Lighthouse with new features and bug fixes, and we only offer support if you're on the latest version.

If you want to update Lighthouse, you have the following three options.

### Automatic Update
Through the "New Releases" dialog, you get the option to directly install the latest version. If you click it, Lighthouse will fetch the latest released version, replace the files, and restart. Once it's done, you'll see a dialog, and you're ready to go with the latest version.

{: .note}
This is not supported on docker. For Docker, please just use the latest container.

### Replace Files
You can simply replace the files in the directory. Download and extract the latest version, and copy/paste them into your Lighthouse directory. Override all existing files.

{: .note }
You must make sure to stop Lighthouse from running before updating.

### Installation and Update Scripts
If you don't want to manually download it, you can also use the following scripts, which will look for the latest released version and will download and extract it from the directory you run the script from.

The scripts are part of the packages, so you can execute them from the installation directory.

#### Windows
You can run the Powershell script [update_windows.ps1](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_windows.ps1). In order to do so, you need to have PowerShell 5.1 or later which is normally installed at your system.

You can also directly download the latest version into your current directory by executing the following command in your terminal:

```powershell
iwr 'https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/main/Scripts/update_windows.ps1' | iex
```
  
#### Linux
For Linux, there is a bash script called [update_linux.sh](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_linux.sh). It requires unzip to be installed, which you can do by running the following command:

`sudo apt-get install unzip`

You can also directly download the latest version into your current directory by executing the following command in your terminal:

```bash
curl -sSL https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/main/Scripts/update_linux.sh | bash
```

#### MacOS
For MacOS, there is a bash script called [update_mac.sh](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_mac.sh). It requires unzip to be installed, which is usually pre-installed on MacOS.

You can also directly download the latest version into your current directory by executing the following command in your terminal:

```bash
curl -sSL https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/main/Scripts/update_mac.sh | bash
```

## Troubleshoot Startup Issues
If you follow the instructions, but Lighthouse is not available on the above port, something didn't go as expected.

In such a case, please inspect the logs in the terminal and try to spot an `Error`. They often tell already what the issue may be.

{: .note}
Now we don't expect you to understand that gibberish, but if you can provide us those logs (for example through our [Slack Channel](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A)), the chances are we can support you quite well and try to resolve the issue.

Following is a list of observed problems together with some potential solutions.

#### Address already in use
```bash
10:26:11 - ERROR - Host: Hosting failed to start
System.IO.IOException: Failed to bind to address http://[::]:5000: address already in use
```
This means that the specified port is already used by another application. This may be another instance of Lighthouse (did you stop all other instances?), or by chance another tool is using the same port (we've seen for example *AirPlay Receiver* using Port 5000 which is Lighthouse default port). If the port is blocked and you can't change/stop the other application that is using it, you can also adjust the port that Lighthouse is using. Check the [Configuration Options](configuration.html#http--https-url) for more details.

## Register Lighthouse as a Service
Using this approach, you'll have to restart Lighthouse after every restart. What you can do instead is to register it as a service, that way it will run automatically in the background.

See [Run as Service](./service.html) for more details.