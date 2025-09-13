---
title: Logs
layout: home
parent: Settings
nav_order: 6
---

# Logs
We really hope you don't need this...but then again, Software is complex and it's very possible that once you run into a problem and need some more details on what was going on (or we ask for this info to better find the problem).  
For this case, you can check the *Logs*.

![Logs](../assets/settings/logs.png)

## Log Level
Log levels describe the level of detail that should end up in the log. The more "sensitive", the more messages will be added. Following *Log Levels* can be selected:

| Level | Description |
|-------|-------------|
| Verbose | Most detailed logging level, includes all messages |
| Debug | Includes detailed information useful for debugging |
| Information | General information about application flow |
| Warning | Potentially harmful situations that aren't errors |
| Error | Error events that might still allow the application to continue |
| Fatal | Very severe errors that will terminate the application |

{: .recommendation}
For normal operations, we recommend to keep the log level at *Warning* or *Information*. *Debug* and *Verbose* should only be used selectively to analyze errors, as otherwise the log will grow a lot, and most likely you'll have too much information in there for it to be truly useful.

## View Logs
You can check the *Live Logs* in the built-in Log View. This also supports a *Search* with syntax highlighting, for example if you want to find a specific feature by name or ID.

{: .note}
The latest logs are the **bottom** of the Log Viewer. So for newer things you have to scroll down

The logs won't be updated automatically. If you want to get the latest ones, hit the *Refresh* button and the viewer will reload the latest ones.

## Download
For a quick glance, the built-in [Log Viewer](#view-logs) is nice. However, for some extended analysis or to provide Logs to other people (for example as part of a bug report), you can download the full Log file. Simply hit the *Download* button, and the file will be downloaded to your computer.