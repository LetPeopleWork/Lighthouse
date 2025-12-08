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

By default, Lighthouse will start running on port 5001 for HTTPS. The HTTP port is 5000 on Windows and Linux, or 5002 on macOS (to avoid conflicts with AirPlay Receiver). If everything worked as expected, you can open the app now in your browser:
- Windows/Linux: [https://localhost:5001](https://localhost:5001) or [http://localhost:5000](http://localhost:5000)
- macOS: [https://localhost:5001](https://localhost:5001) or [http://localhost:5002](http://localhost:5002)

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
You can simply replace the files in the directory. Download and extract the latest version, and copy/paste them into your Lighthouse folder. Override all existing files.

{: .note }
You must make sure to stop Lighthouse from running before updating.

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
This means that the specified port is already used by another application. This may be another instance of Lighthouse (did you stop all other instances?), or by chance another tool is using the same port. On macOS, port 5000 is commonly used by the *AirPlay Receiver* service, which is why Lighthouse defaults to port 5002 on macOS. If the port is still blocked and you can't change/stop the other application that is using it, you can adjust the port that Lighthouse is using. Check the [Configuration Options](configuration.html#http--https-url) for more details.

## Register Lighthouse as a Service
Using this approach, you'll have to restart Lighthouse after every restart. What you can do instead is to register it as a service, that way it will run automatically in the background.

See [Run as Service](./service.html) for more details.