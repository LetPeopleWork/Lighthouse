---
title: Settings
layout: home
nav_order: 50
has_children: true
---

# Settings

Lighthouse has various options to fine-tune it based on your needs. This section covers all the configurable aspects of Lighthouse to help you customize the application according to your organization's workflow and requirements.

The **Settings** menu in the header opens three areas, and the menu shows only the ones you may open. Each area is a page of its own with a row of tabs, at `/settings/<area>/<tab>`. Old addresses keep working: a bookmark to `/settings?tab=database` or to the earlier single Settings page opens the matching tab in its new area.

| Area | For | Tabs |
|------|-----|------|
| **Organization** | How we work | Terminology, Calendar, Refresh & ordering |
| **Access** | Who may do what | System-wide access, API Keys |
| **System** | Running this server | Connections, Database, Encryption, Demo Data, Usage Data, System Info, Logs, Refresh History, License |

Most tabs are for System Administrators only. *API Keys*, *System Info*, *Logs*, *Refresh History* and *License* are open to everyone, and *System-wide access* shows only once [RBAC](rbac.html) is enabled.

## Configuration Areas

### [Organization](organization.html)
#### [Configuration](configuration.html)
The settings that shape how your organization works in Lighthouse:
- **Terminology**: your own words for Feature, Work Item, Team and the rest
- **Calendar**: Blackout Periods and Recurring Rules
- **Refresh & ordering**: periodic refresh settings and who owns the order of your Features

### [Access](access.html)
#### [API Keys](apikeys.html)
Create and manage API keys for the Lighthouse CLI, MCP servers, and other automation clients. This is especially relevant when you follow the [AI and Automation](../aiintegration.html) setup and your Lighthouse instance requires authentication.

#### [Role-Based Access Control](rbac.html)
Configure who can read, edit, and create teams and portfolios once [authentication](../Installation/authentication.html) is enabled. Covers the four roles (System Admin, Team Admin, Portfolio Admin, Viewer), the first-time bootstrap, direct grants, SSO group mappings, team / portfolio creation rights, and the Emergency Admin recovery path. It lives on the *System-wide access* tab.

### [System](system.html)
#### [Work Tracking Systems](worktrackingsystems.html)
The *Connections* tab: the connections to Jira, Azure DevOps, Linear, ServiceNow and the other systems Lighthouse reads from.

#### [Database Management](databasemanagement.html)
Backup, Restore, and Clear your database.

#### [Secret Encryption Key](encryption.html)
Which key encrypts the credentials Lighthouse stores, and how to move them onto a new one.

#### [Demo Data](demodata.html)
Load sample scenarios to explore Lighthouse features or for training purposes. Various scenarios are available covering different team situations and use cases.

#### [Usage Data](usagedata.html)
What Lighthouse can send about the instance itself — the five fields, what is never sent, who holds
it and for how long, what is stored on your own server to remember your answer, and how to change
your mind. Off until somebody agrees to it.

#### [System Info](systeminfo.html)
Detailed information about your system, the Refresh History, and the logging information for troubleshooting and monitoring Lighthouse operations, including log level configuration and log viewing capabilities.

#### License
The license status, with import and clear for System Administrators. See [Licensing](../licensing/licensing.html#license-information-in-lighthouse).

### [Task Manager](taskmanager.html)
What Lighthouse is refreshing right now, whether every connection to your Work Tracking Systems still
works, and the last few things that went wrong. Reached from the header rather than this section, and
shown to System Administrators only. Includes how to stop a refresh or a forecast that is under way, or everything at once.
