---
title: Logs
layout: home
parent: System
grand_parent: Settings
nav_order: 7
---

# Logs
Found on **Settings → System → Logs**, open to everyone.

We really hope you don't need this...but then again, Software is complex and it's very possible that once you run into a problem and need some more details on what was going on (or we ask for this info to better find the problem).  
For this case, you can check the *Logs*.

{: .note}
If you are here because something looks wrong *right now*, the [Task Manager](taskmanager.html) in the header lists the recent warnings and errors on their own, without the rest of the log around them — and it links back to this page when you need the full picture.

## Connection default changes
When a save changes a default that Teams or Portfolios follow, the log gets one Information entry for each changed default. It names the connection, the default, the field before and after, the Teams or Portfolios that follow it, and how many updates the save queued. For Parent sources it names the sources in their order, and it ends with how many Teams or Portfolios the change moved:

```
Connection "Jira Cloud" changed the Teams' Parent sources default from "Caused by" to "Epic Link"; 3 Teams follow it: Payments, Platform, Checkout; 3 updates queued; parent sources moved 3 Teams.
```

An Estimation change queues no update, so its entry says *no update queued*. See [Defaults for Teams and Portfolios](worktrackingsystems.html#defaults-for-teams-and-portfolios).

## Jira Release sweeps
To offer or follow a Jira Release, Lighthouse reads the Releases of every project the connection can see. Each of these reads (a *sweep*) writes one Information entry with how long Jira took and how much it read, so you can tell whether a slow Delivery dialog or a slow refresh is waiting on Jira:

```
Jira Release sweep for the Picker took 3912 ms on this DataCenter connection: kept 48 of 1260 versions read over 31 pages from 27 projects; complete: True, projects skipped: 0
```

| Figure | Meaning |
|--------|---------|
| *for the …* | Why the Releases were read (see below) |
| *took … ms* | How long the whole sweep took, from connecting to Jira to the last page of versions |
| *on this … connection* | `Cloud` or `DataCenter`. `Unknown` means Lighthouse did not learn which kind of Jira this is: either Jira could not be reached, or it answered without saying which kind it is. In the second case the rest of the line still carries real counts |
| *kept … of … versions read* | How many Releases were kept as choices, out of all the versions Jira returned |
| *over … pages* | How many pages of versions Jira was asked for |
| *from … projects* | How many projects Jira listed for the connection |
| *complete* | `True` when every project was read; `False` when the list is missing something |
| *projects skipped* | How many projects could not be read, for example because Jira refused them |

If Jira was reached and the entry still says `complete: False` with `projects skipped: 0`, the list of projects itself could not be read completely, so *from … projects* counts only the projects that were listed.

The sweep is named for what asked for it:

| Purpose | What asked for it |
|---------|-------------------|
| Picker | The list of Releases offered when you choose one in the Delivery dialog. A list read in the last few minutes is reused, and then no entry is written. |
| Preview | The preview shown once you pick a Release, with its date and the Features it carries |
| Save | Saving a Delivery that follows a Release, when it is created or changed |
| Refresh | The regular Portfolio refresh that keeps Deliveries following a Release up to date |

The entry names no host, connection, project or Release, so it can be shared as it is. Find it in the [Log Viewer](#view-logs) by searching for *Jira Release sweep*, or in the [downloaded log](#download). It is written at *Information* level, so it only shows while the [Log Level](#log-level) is *Information* or more detailed.


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

## What an Update Writes

At *Information*, every completed update writes a single summary line that says what it did:

```
Update completed | Team 'Zenith' | mode=Full | scanned=4013 | fetched=4013 | duration=291043ms | success=True
```

| Field | Meaning |
|-------|---------|
| `mode` | How much had to be fetched. `Full` downloads every record the query returned; `Delta` downloads only the ones that changed since the last update. See [Faster Updates](refresh.html#faster-updates) |
| `scanned` | How many records the work tracking system returned |
| `fetched` | How many of those were downloaded in full |
| `duration` | How long the update took, in milliseconds |
| `success` | Whether it completed. A failed update still writes this line, followed by the error |
| `forecast` | Only when the update led to a forecast: how long it took, in milliseconds. `skipped after 850ms (inputs unchanged)` means nothing the forecast reads had changed, so the existing forecast was confirmed rather than recalculated. `cancelled after` and `failed after` say how a forecast that did not finish ended |

This is the line to read if you want to know whether your refresh interval is affordable, or which Team or Portfolio is the expensive one.

{: .note}
The detail of *what* an update touched — every Work Item added, updated or removed, every Feature it extrapolated — is written at *Debug*, not *Information*. Nothing was removed; if you are chasing a specific Work Item through an update, switch to *Debug* and you will find it. An update that is skipped because it is not due yet writes nothing at *Information* at all.

## View Logs
You can check the *Live Logs* in the built-in Log View. This also supports a *Search* with syntax highlighting, for example if you want to find a specific feature by name or ID.

{: .note}
The latest logs are the **bottom** of the Log Viewer. So for newer things you have to scroll down

The logs won't be updated automatically. If you want to get the latest ones, hit the *Refresh* button and the viewer will reload the latest ones.

## Download
For a quick glance, the built-in [Log Viewer](#view-logs) is nice. However, for some extended analysis or to provide Logs to other people (for example as part of a bug report), you can download the full Log file. Simply hit the *Download* button, and the file will be downloaded to your computer.
