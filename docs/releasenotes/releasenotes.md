---
title: Release Notes
layout: home
nav_order: 95
---

# Lighthouse v26.4.19.8

## Metrics Improvements
We've worked on the feedback we got from the UX overhaul, and refined our Metrics further. Following changes were made in this release.

### Simplified Metrics Categories
The available categories in the Metrics section have been consolidated from six categories to four:
| Before | After |
|---|---|
| Flow Overview | **Flow Overview** (unchanged) |
| Cycle Time | Merged into **Flow Metrics** |
| Throughput | Merged into **Flow Metrics** |
| WIP & Aging | Merged into **Flow Metrics** |
| Predictability | **Predictability** (unchanged) |
| Portfolio & Features | **Portfolio & Features** (unchanged) |

Each chart is now appearing exactly in one category. While the *Flow Overview* will show you the status at a quick glance, the *Flow Metrics* will allow you to dive deeper and get into details.

### Arrivals Run Chart and Process Behaviour Chart
The Metrics Dashboard now includes **Arrivals** as a first-class metric for both Teams and Portfolios:

- **Arrivals Run Chart**: Shows the daily count of work items started over the selected date range. Complements the Throughput Run Chart by visualizing the intake side of flow.
- **Arrivals Process Behaviour Chart**: Applies XmR-chart analysis to the arrival rate, highlighting special-cause variation in how many items enter the system per day.

The Run Chart is part of the Flow Metrics category, while the PBC can be found in the **Predictability** category alongside the other PBCs.

The Arrivals Run Chart includes a two-factor status indicator that checks both arrivals-versus-departures balance and whether arrivals are continuous or batched.

![Arrivals Run Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/arrivals.png)

### Total Throughput and Total Arrivals Info Widgets
To give a decent overview, both the Throughput as well as your arrivles (items you've started) are now visualized in an individual Metric on the *Flow Overview* page.

![Total Throughput](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/totalThroughput.png)

### Removal of Started vs. Closed Widget
As we have an individual widget for Total Throughput and Total Arrivals, this widget was obsolete and was removed.

### Feature Size Percentiles
For Portfolios, you can now also see in the *Flow Overview* page a summary of your Feature Sizes (similar to the Cycle Time Scatterplot). This allows you to see how big your features are at a quick glance.

![Feature Size Percentiles](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/featureSizePercentiles.png)

### Trend Indicators
All Flow Overview info widgets now display trend indicators comparing the current date range to a prior period:

- **WIP Overview**, **Features Worked On** (Teams), and **Total Work Item Age** compare snapshot values at the start and end of the selected range.
- **Predictability Score**, **Cycle Time Percentiles**, **Total Throughput**, **Total Arrivals**, and **Feature Size Percentiles** compare the current period against an equal-length window immediately before the start date.
- Percentile tooltip rows now use a `previous → **current**` format with the current-period value emphasized.

The trends are neutral, and simply indicate whether you things are increasing or decreasing compared to a previous period.

### Time Filter for Widgets
While previously the WIP Overview, Features Worked On, and Total Work Item Age were simply ignoring any time selected, and showed the current state, they will now resolve their values as of the selected end date. Changing the date range will change the displayed counts to reflect the system at that point in time.

**Note:** The Blocked Items widget does *not* support this (yet)

### Other Metrics Improvements
Apart from the above, the following things related to metrics were improved:
- The Predictability Score was adjusted to give more accurate results. Previously, with an increase of the input, the score had a tendency to be too optimistic
- Removed the Info icon when a baseline was missing in the PBCs - it will be reflected in the RAG status instead
- The Control Limits as well as the Average Lines for PBCs are now rounded to not have any decimals
- Total Work Item Age over Time RAG status was showing the wrong values

## Redesigned Team Forecast
The Team Forecast has been redesigned to make forecasting faster and more intuitive:

- **Auto-run forecast**: The forecast now runs automatically as you type — no more "Forecast" button.
- **Smart quick-pick chips**: Select your remaining work directly from contextual suggestions. WIP and Backlog counts are pulled automatically from your team's current state so you can pick a sensible starting point with one click.
- **Feature Selection**: Switch to *Feature Mode* and select Features from your backlog. It will automatically use the remaining work of all selected Features to run the forecast.
- **Date shortcuts**: Choose common target dates instantly — *End of week*, *End of month*, *+1 week*, or *+2 weeks* — without opening the date picker.

![Feature Mode](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/teamdetail.png)

## Bug Fixes and Other Improvements
- **Product Board**: Various references to the newly introduced [Product Board](https://ideas.letpeople.work) were added
- **Load all Azure DevOps Boards**: Previously only the first 100 boards on any Azure DevOps Projects were shown. Now all boards are loaded.
- **Show Work Items for Features in Delivery**: You can now, similar to the Team and Portfolio Feature view, see what Work Items belong to a feature when expanding the Delivery View
- **Update Manual Deliveries**: Fixed issue that prevented any update to a delivery that used manual feature selection mode

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [Myriam Greger](https://www.linkedin.com/in/myriam-greger/)
- [Nick Brown](https://www.linkedin.com/in/nicolasjmbrown/)
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/) 
- [Sascha Lucius](https://www.linkedin.com/in/sascha-lucius/)
- [Chandan Bala](https://www.linkedin.com/in/chandan-bala-7251b9242/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.4.7.1...v26.4.19.8)

# Lighthouse v26.4.7.1

## Simplified Onboarding

Getting started with Lighthouse is now faster and more guided. The creation flow for **Work Tracking Systems**, **Teams**, and **Portfolios** has been redesigned as a focused step-by-step process — surfacing only what you need to get connected, and deferring advanced settings to the edit view.

![Work Tracking System Setup](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/concepts/worktrackingsystem_type_selection.png)

Key changes:

- **Connection type first**: Choose your provider before filling in any fields. Each authentication method now includes a short description so you know which one to pick.
- **Validate on create**: The separate *Validate* button is gone. Lighthouse now validates your connection automatically when you press *Create* and reports specifically what went wrong if it fails.
- **Improved error messages**: When a connection fails validation, Lighthouse tells you *why* — whether the URL is unreachable, the credentials are rejected, or a referenced additional field is invalid.
- **Guided Team and Portfolio flows**: After picking a connection and entering a name, the wizard walks you through data retrieval, work item types, and states in sequence — and offers to create immediately once enough information is ready.

Advanced options (additional fields, sync settings, state mappings) remain fully accessible after creation in the edit view.

## Metrics Dashboard Overhaulded

The Metrics Dashboard has been significantly redesigned to make it easier to navigate and interpret your flow data at a glance.

![Metrics Dashboard Overview](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/metricsoverview.png)

### Redesigned Dashboard Categories

The Metrics Dashboard now organizes widgets into **six focused categories**, each answering a specific question about your delivery system:

| Category | Question |
|---|---|
| **Flow Overview** | How is my system doing at a glance? |
| **Cycle Time** | How long do items take? |
| **Throughput** | How much are we delivering? |
| **WIP & Aging** | Where is work getting stuck? |
| **Predictability** | Can we trust our forecasts? |
| **Portfolio & Features** | How do features flow through the system? |

Each category chip now displays an **icon** and shows a **tooltip** on hover explaining what the category helps you understand.

### Widget Header

Widgets now feature improved header actions:

- **Info button (ℹ️):** Click the info icon on any widget to see a brief description of what the widget shows and a *Learn More* link to the full documentation.
- **View Data button (📊):** Click the table icon on any widget to open a dialog showing all of the work items that feed that widget. This replaces the previous click-anywhere-on-the-card behavior for the Cycle Time Percentiles and Started vs. Closed widgets — those widgets no longer open a full-dataset dialog on card click. Charts that support context-specific drill-ins (e.g. clicking a single bar, bubble, or data point) continue to work as before.
- **Inline RAG chip:** Hover over the chip to see the actionable guidance tip.

### RAG Status Indicators

Every widget on the Metrics Dashboard shows a **Red / Amber / Green (RAG)** status indicator. RAG status is computed from live data and your team's configuration (SLE, System WIP Limit, Feature WIP, blocked indicators, etc.) so you can spot issues at a glance without interpreting each chart individually.

**How it works:**
- **Red** = action required (missing configuration, threshold exceeded, or process signal).
- **Amber** = attention needed (approaching limits or moderate changes).
- **Green** = within healthy operating range.
- Toggle the *Show Tips* button in the dashboard header to show or hide the RAG chips.

## Other UX Improvements

### Improved State Mapping UX

Configuring state mappings on teams and portfolios is now significantly more intuitive:

- **Source states are picked from Doing**: You can only select states that are currently in the *Doing* list — no freeform typing required.
- **Auto-sync with Doing**: Adding a mapping automatically replaces the included states in your Doing list with the new mapping name. Removing the mapping restores the original states.
- **No duplicates**: A state can only belong to one mapping at a time, keeping your configuration clean.

A reminder is shown after saving to indicate that a data refresh is needed for changes to take effect.

### Feature Progress Bar Simplified

On the Feature List, if a feature is tracked by only a single team, Lighthouse now shows a single progress bar instead of displaying both a team bar and an identical total bar. This removes redundant information and makes the list easier to read at a glance.

### More Specific Validation Messages

When a connection fails validation, Lighthouse now reports specifically what went wrong — whether the URL is wrong, credentials are invalid, or there is a problem with a referenced additional field. This makes it easier to identify and fix issues without guessing.

### Settings No Longer Reload Unexpectedly

Settings pages no longer reload while you are actively editing them due to background data updates. Your in-progress changes are preserved while the app continues to refresh data in the background.

### Automatic Browser Cache Invalidation

When a new version of Lighthouse is deployed, browsers now automatically load the updated interface instead of serving a stale cached version. No manual hard-refresh is needed after an update.

## ⚠️ Breaking Change: Tags Removed from Teams and Portfolios

The **Tags** field has been removed from Team and Portfolio configuration. If you previously used tags to organise or filter your teams and portfolios, this capability is no longer available.

## Open-Source Software (OSS) Attribution

The *System Info* page in System Settings now includes an **OSS Attribution** section listing all open-source components bundled with Lighthouse, along with their versions and licenses. This makes it straightforward to review every third-party dependency that Lighthouse ships with.

## Bug Fixes and Other Improvements

- **Standalone real-time updates fixed**: The SignalR connection in the Standalone (desktop) edition was broken, preventing live updates from appearing in the UI without a manual page reload. This has been fixed.
- **Write-back with duplicate work items**: When a work item appeared multiple times across queries, write-back would fail. Lighthouse now handles duplicates correctly.
- Updated various third-party dependencies.

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [Myriam Greger](https://www.linkedin.com/in/myriam-greger/)
- [Nick Brown](https://www.linkedin.com/in/nicolasjmbrown/)
- [Liz Rettig](https://www.linkedin.com/in/lizrettig-agilecoach/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.3.28.14...v26.4.7.1)

# Lighthouse v26.3.28.14

## Authentication Support

Lighthouse now supports **OpenID Connect (OIDC)** authentication to protect your instance. When enabled, users must sign in through your identity provider before accessing Lighthouse — unauthenticated requests are redirected to the sign-in screen.

![Sign In Screen](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/authentication/signin.png)

Key highlights:

- **Fail-closed design**: If authentication is enabled but misconfigured (e.g. no `Authority` set), Lighthouse displays an error screen and refuses to start, rather than accidentally leaving the app unprotected.
- **Session management**: Sessions last 8 hours by default, configurable via `SessionLifetimeMinutes`. Users see a **Session Expired** screen when their session ends.
- **Provider guides** are available for [Keycloak](https://docs.lighthouse.letpeople.work/Installation/authentication.html#keycloak), [Microsoft Entra ID](https://docs.lighthouse.letpeople.work/Installation/authentication.html#microsoft-entra-id), [Google](https://docs.lighthouse.letpeople.work/Installation/authentication.html#google), and [Auth0](https://docs.lighthouse.letpeople.work/Installation/authentication.html#auth0).

See the [Authentication documentation](https://docs.lighthouse.letpeople.work/Installation/authentication.html) for the full configuration reference and setup instructions.

**Note:** Authentication is a **Premium** feature. A valid Premium license is required to use it.

## Linear Integration — Production Release
The Linear integration has moved from preview to a fully supported production provider. If you were using Linear during the preview phase, you will need to reconfigure your teams and portfolios. While we are not supporting 100% the same features as with Jira and Azure DevOps yet (for example, additional fields are not yet available), the basic functionality now works.

Here is what changed:

**Teams:** Linear teams are now configured using a team selection wizard instead of typing team names manually. The wizard lists all available teams from your connected Linear workspace. Work item types are fixed to *Issue* and do not require manual configuration.

**Portfolios:** Linear portfolios automatically retrieve all projects in the authenticated workspace as Lighthouse features. No query or work item type configuration is needed. Just create a portfolio with a Linear connection, configure your states, and you're ready to go.

**Hierarchy:** Lighthouse now maps the full Linear hierarchy:
- **Issues** (team work items) roll up to **Projects** (portfolio features)
- **Projects** roll up to **Initiatives** (parent features)

This means your Monte Carlo forecasts and metrics correctly reflect the Linear project structure. Initiative names, statuses, and URLs are fetched directly from the Linear API.

See the [Linear documentation](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/linear.html) for updated setup instructions.


## State Mappings for Teams and Portfolios
Lighthouse now supports **State Mappings** — a way to rename or group raw provider states into meaningful Lighthouse states before placing them in To Do, Doing, or Done. If you create a state mapping, it can be used like a regular state, and all the *source states* will be transformed into the mapped state.

You can use that to:
- Group multiple states into one: For example, group *Code Review*, *QA Review*, and *Design Review* into a single *In Review* state and place it in Doing.
- *Rename* a single state: For example, map your provider's *In Dev* state to *Development* and then use *Development* in your Doing list.

**How it works:**
1. Open team or portfolio settings and scroll to the **State Mappings** section (below the To Do / Doing / Done state lists).
2. Add one or more mappings — give each a name and select the source states it should contain.
3. Use the mapped name in your To Do, Doing, or Done configuration just like any other state.

## Refresh History
In the System Settings, you can now see statistics to the last 30 refreshes that you did for each of your teams and portfolios:

![Throughput with Blackout](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/settings/RefreshHistory.png)

This includes how many items were fetched and how long it took to execute it. This may be useful to track Lighthouse performance over time, to see if it's degrading as you onboard more users, teams, and portfolios or not.

## Other Improvements and Bug Fixes
- Cycle Time Dates No Longer Appear in the Future for Positive-Offset Timezones.
- Standalone version will now ensure that all processes are stopped when exiting. This was not the case before, and could prevent updates as files were still in use.
- When running Lighthouse in Docker, the logs were not displayed in the UI, but a generic *Logs not found* message. This has been addressed, logs show up now.
- Updated various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)
- [Liz Rettig](https://www.linkedin.com/in/lizrettig-agilecoach/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.3.25.6...v26.3.28.14)


# Lighthouse v26.3.25.6

## Blackout Periods
Lighthouse now supports **Blackout Periods** — a way to mark specific dates or date ranges as non-working days directly in your configuration. Common use cases include public holidays, company off-days, or any planned period where your team is not delivering work.

Blackout periods affect Lighthouse in two key areas:

**Forecasting**: When running a Monte Carlo Simulation, Lighthouse skips blackout days entirely. The simulation does not count those days as working days and does not sample throughput for them. This means your forecasts automatically account for known non-working periods — no manual throughput adjustment or buffer needed.

**Metrics charts**: Blackout days are highlighted with a hatched overlay in the following charts, so you can immediately tell why throughput was zero or WIP dipped on those days:
- Throughput Run Chart
- WIP Over Time
- Total Work Item Age Run Chart
- Process Behaviour Charts (all variants)
- Cycle Time Scatterplot

![Throughput with Blackout](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/throughput_blackout.png)

Blackout periods are configured globally in *System Settings* → *Configuration* → *Blackout Periods* and apply to all teams and portfolios. See the [Configuration documentation](https://docs.lighthouse.letpeople.work/settings/configuration.html#blackout-periods) for details.


![Throughput with Blackout](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/settings/blackoutPeriodsSection.png)

## Database Management

Lighthouse now includes built-in **Database Management** capabilities, available under *System Settings* → *Database Management*. You can create encrypted backups of your database, restore from a previous backup, or clear all data — directly from the UI.

⚠️ **This feature is replacing the previously used Configuration Export/Import mechanism. Please be aware that your exported configurations cannot be loaded anymore with this version ** ⚠️

**Key capabilities:**

- **Backup**: Creates an AES-encrypted ZIP archive of your database. The backup is password-protected using PBKDF2 key derivation (SHA-256, 100 000 iterations). Downloads are available as a single file.
- **Restore**: Upload a previously created backup to replace the current database.
- **Clear**: Removes all data from the database, giving you a clean slate.

![Database Management](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/settings/databasemanagement.png)

**Provider support:**

| Provider | Backup format | Requirements |
|---|---|---|
| SQLite | Raw database files | None — works out of the box |
| PostgreSQL | `pg_dump` / `pg_restore` | `postgresql-client` tools must be installed (included in the official Docker image) |

**Operational notes:**
- While a database operation is in progress, all other data-modifying operations (team updates, forecasts, etc.) are blocked to prevent data corruption.
- Backup passwords are never stored — keep them in a safe place.

## Restructure Feature View
The feature  view used to have icon indicators for:
- Using the default size instead of the real number of child items
- Being actively worked on by a team

These icons were part of the *Name* column, and thus you could not search and/or sort by them. With this release, we introduced dedicated columns for:
- Warnings
- Active Work in Progress

![New Feature Columns](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/FeatureWarningColumn.png)

Furthermore, the following change was made to the *Hide Completed* functionality. Previously, it only filtered by Feature state. So if your Feature was in a done state, it would not be displayed anymore. Now, next to this filter, it also checks the remaining work. If the Feature is in a done state, but has child items that are not done yet, it will still be displayed. Furthermore, in this scenario, we also display a warning (in the new column), as this is an indicator that something is wrong. This is important as having remaining items will impact your forecast, so either the Feature state is wrong, or the child items were not update. The warning is a nudge to *get your backlog in order*.

## Other Improvements and Bug Fixes
- In the previous versions, you could enable the *MCP Feature* without a license. During startup, it was crashing as it should only be available with a license, effectively logging users out. This is now handled, so that enabling the feature is only allowed with a license, and if you somehow get into the other state anyway, Lighthouse will still manage to start.
- Standalone version will restart after an update has been applied
- Updated various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
- [Manuel Opitz](https://www.linkedin.com/in/manuel-opitz-3812351a9/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.3.20.4...v26.3.25.6)


# Lighthouse v26.3.20.4

## Standalone Apps

Lighthouse now ships as a **native desktop application** for Windows, macOS, and Linux — no server setup, no browser, no terminal required.

![Standalone Application](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/installation/standalone.png)

The Standalone edition is ideal for individual users who want a self-contained Lighthouse experience on their own machine. It launches like any other desktop app and manages its own backend automatically.

**What's included:**
- Full Lighthouse feature set in a native window
- Built-in automatic updater — the app checks for new releases on startup and prompts you before installing
- No network configuration required

**Available packages:**

| Platform | Package | Notes |
|---|---|---|
| **Windows** | NSIS Installer (`.exe`) and MSI Installer (`.msi`) | Recommended — installs to Program Files with auto-updater and uninstaller |
| **macOS** | App Image (`.dmg`) and App Bundle (`.zip`) | Runs natively on both Apple Silicon and Intel; signed, notarized, and auto-updates |
| **Linux** | AppImage (`.AppImage`) | Single-file, runs on most distributions without installation |

For full installation instructions and guidance on choosing between Standalone and Server editions, see the [Installation docs](https://docs.lighthouse.letpeople.work/Installation/installation.html).

## Scoped Access Token Support for Jira Connections
Lighthouse now supports **Scoped Access Tokens** as an authentication method for Jira Cloud connections. Unlike regular API tokens — which inherit all permissions of the user who created them — scoped tokens let you grant only the specific permissions Lighthouse needs. This is especially useful in organizations using **service accounts**, where regular API tokens are not supported.

![Scoped Token Access](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/ScopedAccessToken.png)

The required scopes for read-only access are `read:jira-user` and `read:jira-work`. If you use the [Write Back](https://docs.lighthouse.letpeople.work/concepts/writeback.html) feature, you additionally need `write:jira-work`.

{: .note}
Due to a Jira restriction, the boards endpoint does not support scoped token authentication. If you are using a scoped token, Lighthouse will show "No Boards available" and the Board Wizard cannot be used. You can either configure your teams and portfolios manually, or use a personal API token for the initial setup and switch to a scoped token afterwards.

For full setup instructions, see the [Jira documentation](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/jira.html#jira-cloud-scoped-access-token).

## System Info
The *Logs* tab in System Settings has been renamed to **System Info**. In addition to the existing log viewer, it now displays details about your running Lighthouse instance — such as version information and other system properties. Clicking any of the displayed values copies it to your clipboard, making it easy to share when reporting issues or reaching out for support. A direct link to the API documentation (Swagger) is also available from this page.

![System Info](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/settings/systeminfo.png)


## Other Improvements and Bug Fixes
- Fixed an issue where deleting a team caused an error in case you deleted a Portfolio that included this team
- Removed fixed ports from config - plese check [the configuration](https://docs.lighthouse.letpeople.work/Installation/configuration.html#http--https-url) on how to set up https and override ports.
- Updated various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [András Körmendi](https://www.linkedin.com/in/andras-kormendi/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Manuel Opitz](https://www.linkedin.com/in/manuel-opitz-3812351a9/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.3.13.16...v26.3.20.4)

# Lighthouse v26.3.13.16

## Invert Percentiles for New Item Forecasting
The percentile display for the *New Item Forecasting* feature has been inverted. Previously, the percentiles were shown in a way that could be confusing. They are now displayed in a more intuitive order, making it easier to interpret the forecast results at a glance. This means, you read it now as follows: There is a *yy% chance* that we create *x items or less*

This means that the higher the percentiles, the higher the number. The less risk you want to have, the more items you should be expecting.

![Work Item Creation Forecast](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/creationforecast.png)

## Other Improvements and Bug Fixes
- Fixed an issue where the *Parent Override* setting was not being applied correctly on Azure DevOps connections and caused the update of teams to get stuck
- Fixed an issue where *Involved Teams* were not respecting the Ownership Settings configured on a Portfolio
- Fixed an issue where the automatic update of Portfolios was not refreshing the Features, causing stale data to be displayed
- Fixed an issue in *MCS Backtesting* where the start date of the backtesting period was incorrectly included in both the Historical Throughput and the Actual Throughput charts
- Updated various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.3.9.4...v26.3.13.16)

# Lighthouse v26.3.9.4

## Improvements for Write Back to Work Tracking Systems Functionality
After shipping the first version of the sync functionality, this version brings small improvements based on the first feedback:
- Additional Fields were not properly resolved, causing on Jira to fail writing back if you used the *key* of a field instead of the *id*. This is fixed now and you can reference your additional fields as you like
- Instead of updating all the items always, Lighthouse will check if the value that it's going to write differs from what's already there. A write operation is only happening if the values are different, reducing the time for updates and the load on the system.

Furthmore, we looked into disabling notifications on Jira on an update from Lighthouse. However, there is no realiable way to suppress the notifications if someone put themselves as a watcher on an Issue. Please be aware that an update operation may trigger a couple of emails if people subscribed to it...We're looking for potential workarounds for this issue and have updated the documentation with a warning.

## MCS Backtesting
The MCS Backtesting now allows you to select fixed dates instead of only a rolling window. This will make it easier to backtest with your actual settings, for example if you exclude a specific time period (like Christmas). The default setting will be based on your teams Forecast Configuration - if you have a rolling window it will be set to this, if you use fixed dates, it will automatically pick your current configuration of those.

Furthmore, you will now be able to inspect the actual Throughput in a dedicated Run Chart, that was added as a third tab next to the MCS Backtesting Result and the Historical Throughput Run Chart.

## Other Improvements and Bug Fixes
- The Setting for the PBC Baseline is now properly labeled, as it was *Enable PBC* before, indicating it would not be enabled without a baseline
- There is a *Clear* Button to clear the PBC Baseline
- Improved various third party packages to their latest version


## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.2.28.12...v26.3.9.4)

# Lighthouse v26.2.28.12

## Write Back to Work Tracking Systems [Premium Only]
Lighthouse can now write data back to your work tracking systems. You can configure mappings on your connections to automatically update fields in Azure DevOps, Jira, or other supported systems when forecasts or features are updated. This enables teams to keep their work tracking systems in sync with Lighthouse's forecasting data without manual intervention.

![Sync Mapping](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/DeliveryDateMapping.jpg)

Currently, you can write back:
- Work Item Age and Cycle Time (Team and Portfolio)
- Feature Size (Portfolio only)
- Forecasts per Feature (Portfolio only)

The values are written to your system after every update of your Team and Portfolio.

## Work Tracking Systems Adjustments
Work tracking system connections have been moved from the Settings page to the Overview page, making them more accessible. The Overview now shows your configured connections alongside portfolios and teams, with full create, edit, and delete capabilities. Onboarding prerequisites are enforced: you must create a connection before adding a team, and a team before adding a portfolio.

For new systems, an *Onboarding* is displayed to guide them through their first steps:
*Create Connection* --> *Create Team* --> *Create Portfolio*

![Landing Page](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/installation/landingpage.png)

As the Work Tracking Systems contain more and more functionality (including the Sync functionality introduced in this release), they are displayed as a full page now instead of just a dialog. The side effect of this is, that you **cannot** create Work Tracking Systems as part of the Team or Portfolio creation.

## Involved Teams in Portfolios
Before, users had to manually select all teams that contributed to a Portfolio. This was adjusted, and Lighthouse is now automatically inferring which Teams are contributing any work to a Portfolio. This should simplify the setup, and allows you to simply add new teams (or new work to a team) and it will automatically be picked up as contributor to a Portfolio.

## Other Improvements and Bug Fixes
- The PBCs for Feature Size and Cycle Time were showing *Invalid Date* depending on your region - this should be working now in all regions
- There were situations where Teams would stop updating, and were not deletable anymore. We tried to correct the root cause of this (although we are not 100% sure, as we could never reproduce it on our test environment)
- The tab next to *Overview* is now called *System Settings*, to more clearly distinguish it from Team and Portfolio Settings
- The Cut-Off date for Work Items on Team Level is now, by default, the same value as for Portfolios: 365 days
- Update of various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/) 
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Manuel Opitz](https://www.linkedin.com/in/manuel-opitz-3812351a9/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.2.22.1...v26.2.28.12)

# Lighthouse v26.2.22.1

## Visualizing Estimations
You can now configure an estimation field on your Team and Portfolio settings and visualize how estimates correlate with cycle time. Configure numeric estimates (e.g., story points) or categorical estimates (e.g., T-shirt sizes like XS/S/M/L/XL) with explicit ordering.

Once configured, a new **Estimation vs. Cycle Time** scatter plot appears in your Metrics tab, showing estimation values on the x-axis and cycle time on the y-axis. Click any data point to drill into the underlying work items.

![Estimation vs. Cycle Time](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/estimationVsCycleTime.png)

### Feature Size Chart – Estimation Y-Axis Toggle
When an estimation field is configured, the **Feature Size** chart now lets you toggle the y-axis between estimation values and cycle time directly on the chart. If no estimation field is set, the chart continues to use cycle time as before.

![Feature Size Chart With Estimates](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/FeatureSizeEstimation.png)

## Process Behaviour Chart Improvements
After releasing the PBC feature, we got some feedback and refined the functionality as follows.

You don't need to set a baseline anymore. While it's still recommended, the PBCs will show up when no baseline is set and use the selected timeframe for the metrics also as a baseline. In that case, a small info icon will appear on the chart which states the missing baseline.

For the Cycle Time PBC, we've adjusted the tooltip so that instead of the Cycle Time value, you see the specific item that is represented in that dot.

Furthermore, there is a new PBC for Portfolios for the Feature Size:

![Feature Size Process Behaviour Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/featureSizeProcessBehaviourChart.png)

Use this to see which Features were within the normal variability, and which ones are an indication of a special cause.

## Additional Field Improvements
So far, the additional fields had to match the casing specified by your Work Tracking System. This lead to situations where for example "fixVersion" and "Fix version" where valid, while "FixVersion" was not. This has changed in this version, as casing is ignored, which should make the additional field setup a lot simpler.

Furthermore, Jira Fields that can have multiple values (for example *labels* or *fixVersions*) are now stored properly. Before, Lighthouse would store a lot of additional information (the whole json object), which now is properly parsed and only the actual data is stored. If multiple values are specified in a field, Lighthouse stores it separated by *,*.

## Other Improvements and Bug Fixes
- You can now click on the *Total* Feature Progress and see the details of the work items assigned to that specific features in both the Team and Portfolio Feature View
- The Feature Tab for Teams was always disabled after selecting another tab, preventing moving back to it. This is fixed and the Features tab is only disabled, if the team has no Features assigned.
- Update of various third party dependencies

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Manuel Opitz](https://www.linkedin.com/in/manuel-opitz-3812351a9/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.2.11.7...v26.2.22.1)

# Lighthouse v26.2.11.7

## Process Behaviour Charts
Process Behaviour Charts (PBCs) help you understand whether changes in your system are likely just normal variability, or whether you are seeing a special cause (something worth investigating). Lighthouse now supports PBCs for both Teams and Portfolios, for Throughput, Cycle Time, Work In Progress, and Work Item Age.

In order to get the chart, you must configure a *baseline* for your PBC. You do this in the [Team Settings](https://docs.lighthouse.letpeople.work/teams/edit.html#process-behaviour-chart-baseline) or [Portfolio Settings](https://docs.lighthouse.letpeople.work/portfolios/edit.html#process-behaviour-chart-baseline). Once you did this, the charts will appear in your [Metrics tab](https://docs.lighthouse.letpeople.work/metrics/widgets.html#process-behaviour-charts):

![Cycle Time Process Behaviour Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/cycleTimePbc.png)

## Adjustments to Additional Fields Configuration
If you add or modify the additional fields, you must make sure to refresh your teams and portfolios before the fields are loaded. This was not obvious, thus we added a small info box about it.

In case of using an *Option Field* in Jira as additional field, Lighthouse was fetching too much information (a whole json object). This is fixed in this version and you get the exact value that is selected.

On top of that, you can now also specify the "id" of a field, next to the already supported "Key" and "Name" properties. This is useful as (some?) versions of Jira Data Center do not expose a key property. In most cases, id and key will actually be the same property (at least in Jira Cloud), so not much will change if you do not use Jira Data Center.

Furthermore, we decided to make the additional fields part of the premium features. The additional fields allow to deal with customization from your Jira instance and will mainly be used for advanced features or quality of life improvements. For both those things we do have the premium model in place.

Starting with this release, you are restricted to a single additional field if you are on the community version. This allows you to test various scenarios and features, to make sure it works with your work tracking system, while giving us a marketable feature to sell premium licenses.

## Other Improvements and Bug Fixes
- The *Feature List* for deliveries now also allows you to Hide completed Features
- Update of various third party components

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.2.7.3...v26.2.11.7)


# Lighthouse v26.2.7.3

## Rule-Based Delivery Feature Assignment [**Premium Only**]
You can now automatically assign Features to Deliveries using rule-based expressions instead of manual selection. Define rules using available fields (Type, State, Tags, any Additional Field) with operators like equals, contains, or not equals to create dynamic feature selection.

![Rule Based Delivery](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/delivery_rule_based.png)

When creating or editing a Delivery, switch to the expression editor to define your rules. Multiple rules are combined with AND logic. The system validates your expression and shows matching features before saving. Deliveries using expressions automatically update when the Portfolio refreshes, adding new matching features and removing those that no longer match.

This feature is ideal for teams using FixVersions (Jira), Area Paths (Azure DevOps), or custom tags to organize their release planning.

## MCS Backtesting Improvements
The Forecast Backtesting visualization has been enhanced with improved clarity and usability:
- Actual completion date now shown as a clear vertical line instead of an additional bar
- Added the average forecast as a comparison value
- Improved layout and spacing for the Predictability Score display
- Fixed issue where backtesting time frames were incorrectly calculated

These changes make it easier to evaluate forecast accuracy and understand the relationship between predictions and actual outcomes.

## Changes to Premium Features
- **Terminology Configuration** is now a premium feature
- **New Work Item Prediction** (forecasting arrival of work) is now available in the Community Edition

## Feature Management Improvements
Managing features across teams and portfolios has been streamlined:
- Completed features are now hidden by default to reduce clutter
- Feature state is now visible in the feature list instead of just showing the State Category as an icon
- Removed the "Updated on" column to simplify the view
- Feature tabs are disabled on Teams that aren't part of any Portfolio

## Other Improvements
- Manual forecasts now allow you to specify either a target date OR remaining items, not requiring both
- System startup now displays `localhost` URLs instead of `[::]` for better clarity
- Fixed issue where Total Work Item Age widget didn't match the Work Item Age Run Chart
- Fixed compatibility issue loading configurations from older Lighthouse versions
- Resolved Jira custom field handling issue that could cause connection problems in case you had duplicate names of custom fields (thanks Jira...)

## Contributions ❤️
Special thanks to everyone who contributed feedback for this release:

- [Nick Brown](https://www.linkedin.com/in/nicolasjmbrown/)
- [Ben Richards](https://www.linkedin.com/in/iambenrichards/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.1.30.10...v26.2.7.3)

# Lighthouse v26.1.30.10

## MCS Back-Testing
You can now validate Lighthouse's Monte Carlo Simulation forecasts against historical data using the new **Forecast Backtesting** feature. This helps build confidence in the forecasting system by comparing predicted outcomes with actual historical throughput.

![Forecast Backtesting](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/backtest.png)

Navigate to any Team's **Forecasts** tab to find the new *Forecast Backtesting* section. Select a historical time period and run a backtest to see:
- How many items the MCS predicted would be completed (at 50%, 70%, 85%, and 95% confidence levels)
- The actual number of items that were completed during that period

This feature empowers teams to assess the reliability of Lighthouse forecasts using their own historical data.

## Delivery Improvements
The *Feature Selection* for the Delivery creation now features two new improvements:
- A *Select All* button that will select all Features that are currently in view (this will respect any filter you've set in the grid - only the visible features will be selected)
- The state of a feature is now shown as a dedicated column

## Spotlighting Charts
You can now *spotlight* a chart in the Metrics section. By doing so, you'll get an expanded view of this chart. This is especially useful when teaching about flow metrics or when you want to highlight something specific.

![Spotlighting](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/SpotlightedChart.png)

## Other Improvements
- The various "reload" buttons have now more distinct icons so they are not easily mixed up
- If percentiles on the Feature Size, Cycle Time Scatterplot, or Work Item Aging chart are overlapping, by default only the *higher* percentile will be shown
- Updated various third party dependencies

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Marat Kiniabulatov](https://www.linkedin.com/in/maratkinyabulatov/)
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.1.17.4...v26.1.30.10)


# Lighthouse v26.1.17.4

## Jira and Azure DevOps Board Wizards
If you use a Jira or Azure DevOps connection, Teams and Portfolios can now fetch some settings through a Board Wizard from their existing Boards.
The Wizard will display all available boards. If you chose a board, it will read:
- The JQL/WIQL for this board
- The work item types on this board
- The states with a state mapping (by category)

![Jira Wizard](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/concepts/jira_wizard.png)

This one-time sync should simplify the initial creation of your Teams and Portfolios!

## Delivery Improvements
After releasing the Delivery functionality, we got various feedback and improved the following things:
- The *Delivery Header* is now showing the 70, 85, and 95% likely dates
- All Deliveries are now ordered by their Delivery date, starting with the earliest first
- You can now see and select *Done* Features when you create or edit a delivery. The done Features are displayed in ~strikethrough~
- The Delivery Creation Dialog can now be resized
- The label of the "Delivery Date" Textbox was not readable and was improved
- Instead of the "internal" ID of the Features, we display now the ID from your System (e.g. the Key if you are on Jira)
- If deliverie dates were in the past, the likelihood was not correct. Now it's either 0% (if there is pending work) or 100% (if everything is done). There can't be anything in between for deliveries that are in the past

![Delivery Header](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/Delivery_Header.png)

## Other Improvements
- Updated various third party dependencies
- Attempted to improve Date handling, so that we do not get "one-off" issues depedning on the timezone you are in

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Anoop A Parapurath](https://www.linkedin.com/in/anoop-a-parapurath-137a3b4/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Chandan Bala](https://www.linkedin.com/in/chandan-bala-7251b9242/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Ann K Brea](https://www.linkedin.com/in/annkbrea/)
- [Nick Brown](https://www.linkedin.com/in/nicolasjmbrown/)
- [Paul Brown](https://www.linkedin.com/in/paulisthrivving/)
- [Myriam Greger](https://www.linkedin.com/in/myriam-greger/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.01.09.006...v26.1.17.4)

# Lighthouse v26.1.9.6

This release was focused on improving the additional field functionality of the Work Tracking Systems. Following changes related to this were implemented:
- Username is not shown in "Other Options" anymore if you choose Jira Data Center Auth Option
- You don't need to specify the token (or other secrets) if you modify only the additional fields
- For Jira Data Center, it will now automatically use the 'Epic Link' (for Teams) and 'Parent Link' (for Portfolios) fields to try to set the parent. If this convention is followed, no specific additional field and override is needed
- For Azure DevOps, you can now also specify the Field Name, not only the reference. Example: Previously you had to specify 'Microsoft.VSTS.Scheduling.Size', while now you could also refer to it simply via 'Size'. Both options work, reference or name. No migration is needed if you have already something specified.

## Other Improvements
- Updated various third party dependencies
- Added a system info on startup that shows you the details of your Lighthouse instance:

![System Info](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/systeminfodisplay.png)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v26.1.5.1620...v26.01.09.006)


# Lighthouse v26.1.5.1620
This is a hotfix release that attempts to fix an issue that caused the validation to fail for Azure DevOps Work Tracking Connections if they had *any* additional field defined.
The check for the available fields is now changed, which should make it work for all environments, independent of user rights.

Special thanks to [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/) for reporting this and supporting with debugging!

⚠️ **Important: Please read through the release notes of version 26.4.1.1559 to understand the impact of upgrading to this version!**

# Lighthouse v26.1.4.1559

**⚠️⚠️ This release contains breaking changes - please read carefully ⚠️⚠️**

This release was focused on a lot of infrastructure work in and around the Work Tracking System Connection. While the impact on the functionality right now is not as big, it will make future enhancements easier. This will include onboarding of new systems, supporting different authentication options, and dealing with the special configurations of the systems within their organization.

The Work Tracking System Connection configuration now looks like this:

![Work Tracking System Connection Config](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/concepts/worktrackingsystem_Jira.png)

# Authentication Options
If a system supports different means for Authentication (right now only Jira), you can select the option. This will also adjust the options you need to specify. This is a big UX improvement over the previous approach where all options where visible, and depending on whether you filled in some field or not it would chose the authentication option.

**Note:** Existing systems will be working as before and all this data will be migrated

# Additional Fields Configuration
For each supported Work Tracking System Connection, you can now add *Additional Fields* that should be fetched for all Work Items and Features. This allows to have data specific to your system and configuration in Lighthouse. Currently those additional fields are only used for *Parent Overrides*, *Size Estimates*, and *Feature Owner* definition (see below). However, this will be expanded in future.

Currently supported are Azure DevOps and Jira. They both come with a set of predefined additional fields:
- Azure DevOps: Area Path, Iteration Path, Size
- Jira: Fix Version, Component, Sprint

You can modify and remove those defaults as you please.

**Note:** This only applies to newly created system. Migrated Work Tracking System Connection will not have any additional fields by default.

Upon Validation of the system, It will check the Additional Fields. If a field cannot be found (e.g. due to a typo), the validation will fail. Please see respective documentation for [Jira](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/jira.html) or [Azure DevOps](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/azuredevops.html#additional-fields).

# Options
At the bottom of the Work Tracking System Connections, you can find now options. So far only Jira and Azure DevOps offer additional options. It's the *Request Timeout* that was previously specified in the Work Tracking System Settings page. This is now specific per connection. If you had an existing value set, it will automatically be migrated.

## Known Unpleasantries
As of now, you will have to provide the Authentication Token even if you don't change the authentication in the Work Tracking System Connection Dialog. We plan on improve this in future.

## Parent Override Field ⚠️
While previously you could define any field in the *Parent Override Field* with free text, you now can chose from the additional fields. Instead of free text, you will get a selection

⚠️ **If you had an override specified before, this change will not be migrated. Please manually add your additional field and set it up through the Team and Portfolio settings.** ⚠️

## Size Estimate Field ⚠️
While previously you could define any field in the *Size Estimate* in the Portfolio Settings, you now can chose from the additional fields. Instead of free text, you will get a selection.

⚠️ **If you had a Size Estimate specified before, this change will not be migrated. Please manually add your additional field and set it up through the Portfolio settings** ⚠️

## Feature Owner Field ⚠️
While previously you could define any field in the *Featuer Owner* in the Portfolio Settings, you now can chose from the additional fields. Instead of free text, you will get a selection.

⚠️ **If you had a Featuer Owner specified before, this change will not be migrated. Please manually add your additional field and set it up through the Portfolio settings** ⚠️

# Additional Work Tracking System Connection Related Changes
- The UI will now display the field for the Query only after selecting a work tracking system and show a tailored description (for example "JQL Query")
- You can now specify CSV content directly, without the need of having a file uploaded
- The File Upload button for CSV looks slightly different as it's done in a more generic way


# Removal of Unparented Work Item Queries ⚠️
⚠️ The "Unparented Work Item Query" that could be specified per Portfolio has been removed. All "Unparented Features" are also removed. This due to the fact that the functionality was hard to maintain and rarely used. Furthermore, it doesn't fit in the new design with Deliveries anymore. In future, there may be a replacement of this functionality, but for now, it is removed without a successor.

If you were heavily relying on this, please reach out to us for feedback.

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.12.28.1246...v26.1.4.1559)

# Lighthouse v25.12.28.1246

## New macOS App Bundle & Deployment Changes
Lighthouse for macOS is now delivered in a proper **app bundle structure**, aligning with standard macOS application conventions.

You can now download Lighthouse for macOS as either a **zip** or a **dmg file** (recommended for easiest installation). Both formats are fully **signed and notarized by Apple**, ensuring maximum security and trust for all users.

![Apple App Installation](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/installation/AppleAppInstallation.jpeg)

⚠️ **Important:** Updates are now handled differently on macOS. All users must manually update to this version. Future updates will require the new app bundle structure - automatic updates from older versions will not work. Please download and install the latest version from our website to continue receiving updates and security fixes.

## Rework Settings for Portfolios and Teams
In order to change settings for Teams and Portfolios, you don't need to change the page anymore. Instead, you'll see the settings as dedicated tab. This should make it easier to change the settings and see the impact.

Furthermore, the notion of *Quick Settings* was introduced. For selected settings, you now see a *Quick Settings Toolbar* across all the tabs of a team and portfolio. On click, you can directly change these settings.

The following *Quick Settings* are available:
- Throughput Forecasting Configuration: Select which Throughput Period should be used for the forecasting of this team (Team only)
- Service Level Expectation Configuration
- System WIP Configuration
- Feature WIP: For a team, just change that individual teams Feature WIP. For Portfolios, change the Feature WIP of all involved teams.

Changing the Throughput Forecasting Configuration or the Feature WIP will trigger an automatic re-forecast for the affected team.

![Quick Settings](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/QuickSettings.png)

## Other Improvements
- Added a "Cut Off" for Teams and Portfolios that will define where to cut off "done" items. This should help to reduce the load on the server as we will not fetch items that were done 5 years ago. This is configurable in the settings, and will default to 180 days (teams) and 365 days (portfolio).
- The "Update All" button in the Overview page was removed, and instead it's displayed in the header. Additionally, it will now be disabled if updates are running, and visualize how many update tasks are still ongoing.
- Updated various third party dependencies

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.12.19.1534...v25.12.28.1246)

# Lighthouse v25.12.19.1534

## Deliveries

Deliveries are named milestone dates for a portfolio that group a set of Features intended to be released together. They help you communicate target dates, track which features belong to a delivery, and see delivery-level progress alongside feature forecasts.

⚠️ **Milestones have been removed and replaced with Deliveries.** Deliveries are the new way to group features for release milestones.

Deliveries appear in their own view for each Portfolio, including target date and the list of included features.

![Delivery Details](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/delivery_detail.png)

The delivery row shows the delivery name and date, an expand control to reveal included features and their statuses, and action buttons to edit or delete the delivery. Expanding a delivery displays each feature and its forecasted completion so you can assess delivery risk and progress at a glance.

Use deliveries to communicate release milestones and to group related features — this makes it easier to discuss release risk and progress with stakeholders.

Deliveries are also shown in the overview page for each Portfolio.

**Note:** If you are using the community edition, you can create one delivery. With a premium license, you get unlimited deliveries.

## Digital Code Signing
The versions for Windows and macOS are now digitally signed. This means you can trust that the executables are coming from LetPeopleWork GmbH. On Windows, this means that you won't see the "Unknown Publisher" warning anymore, but can verify that the executable is trustworthy.

![Digital Signature on Windows](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/digitalsignature.jpg)

On macOS, you may still get a warning (after the download), but you could check through the commandline that it's signed and notarized through Apple. In a next release, this will improve also on macOS.

## Bug Fixes and other Improvements
- Update of third party software to latest versions
- Work Distribution Chart details displayed Cycle Time for all Items. This lead to always showing 0 for in Progress work. For those item, now the Item Age is displayed.
- If you had an expired license, the update to a newer license was causing an error. This is fixed now.
- Reworked the tab structure in the *Teams* view, splitting *Forecasts* and *Features* into dedicated tabs.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/) 
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.12.7.1118...v25.12.19.1534)

# Lighthouse v25.12.7.1118

## Rename Project to Portfolio
As a preparation for future changes, we started renaming what used to be called *Project* to **Portfolio**.

The driver behind this change is that a Project has a dedicated start and end date. While you can use Lighthouse for this, it would mean, you will lose all the historic data (your metrics) for a new project.

We are aware that names have specific meanings in every context, so we also made sure you can override the name for *Portfolio* through the System Settings (so you can call it *Initiative*, *Project*, or whatever you like and makes sense in your context).

⚠️ While this sounds like a simple change, we started cleaning up our code and also urls. While urls that point to */projects* will still work, eventually they will be turned off. If you find other things that don't work as before, please let us know. We did our best to verify everything, but can't rule out we missed something.

## Cloning of Teams and Portfolios
You can now *clone* existing Teams or **Portfolios** with a single click through the Overview. Just click on the clone icon, and you will get a copy of the settings that you can adjust to your liking.

![Clone Teams](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/clone.png)

⚠️ This functionality makes setting up *Default Teams* and *Default Projects* obsolete. In order to reduce our effort, we decided to **remove** this functionality with this release. If you want to quickly create new teams or portfolios based on a default, please use the clone functionality.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.11.23.1453...v25.12.7.1118)

# Lighthouse v25.11.23.1453

## Data Grid Enhancements
We've further improved the data grid component with additional convenience functions and column visibility controls. This includes:
- The possibility to show and hide columns
- The option to re-arrange the column order
- Improved the resizing of columns
- Storing those settings per user and table

This should improve the usability of the datagrids!

## Jira Integration Improvements
Lighthouse now supports using Jira's *Flagged* field to mark work items as blocked, providing better integration with Jira workflows for identifying and tracking impediments. If you are using the *Flag* feature, you can simply add the keyword *Flagged* as a *Blocking Label* to your configuration, and Lighthouse will automatically detect it.

## Categorization for Items in Scatterplots
The Work Item Aging, Cycle Time, and Feature Size chart are all now displaying the different types of items with a specific color. The Feature Size Chart is categorizing by *Status Category*, while the other two are differentiating by *Item Type*. The categories are visualized on top as a legend, and can be toggled on and off through clicking.

![Cycle Time Scatterplot](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/cycleScatter.png)

## Context for Work Distribution Chart
The Work Distribution chart now includes tabular data views for detailed insights into underlying items. This gives some more context to the pie chart that visualizes the distribution. Additionally, the colors are aligned with the colors used for seperating items in the scatterplots (see above).

![Work Distribution Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/workDistribution.png)

## Continuous Improvements & Bug Fixes
- Fixed issue with Work Item Chart and Capitalization that could cause display inconsistencies.
- Resolved Docker startup failures due to missing .NET version compatibility.
- Update of various third party packages.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/) 
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Lorenzo Santoro](https://www.linkedin.com/in/lorenzo-santoro-57172626/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.11.15.1404...v25.11.23.1453)

# Lighthouse v25.11.15.1404

## Work Distribution Chart
The Work Distribution chart provides a visual breakdown of how work items are distributed across their parent work items (such as Features, Epics, or Initiatives). This pie chart helps you understand where your team’s effort is focused.

![Work Distribution Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/workDistribution.png)

Hovering over the chart will show you which item had how many child items. Clicking on it will bring up a dialog with additional information.

## Additional License Options
We've extended the options in the license dialog for our premium users. You can now do the following things.

### Clear a License
If you want to free up the license slot you can clear the license. Alternatively, if you have an expired license and don't want to see the warning, you can also clear the information.

### Renew your License
Starting with the last month of your license validity, you can *Renew* your license. Clicking on the renew button will bring you directly to our homepage and prefill the dialog to order a new license, valid from the date your current one expires.

### Email Support
Through the ✉️ emoji, you can send us an email with your favorite mail client. Moreover, we'll prefill some information that will help us to better support you!

You can find all details in the [documentation](https://docs.lighthouse.letpeople.work/licensing/licensing.html).

## New Data Grid & Export
We've replaced many custom tables we had in Lighthouse with a common component that we can reuse across the application. This will simplify the maintenance while bringing you a unified design, as well as some more options. You can now sort & filter based on the various columns!

![Closed Items](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/closeditemsdialog.png)

For premium users, the data grid also offers the possibility to export the data in the table in one click:
- You can copy it to your clipboard, so you can paste it easily in chats or emails
- You can export it as a csv file, if you want to do some custom analysis on it

**Note:** While we did our best to make sure everything works as it did before, we may have missed a few things. If you experience side effects, or things simply not working anymore related to the new tables, please reach out!

## Continuous Improvements & Bug Fixes
- Removed "Load all Scenarios" from Demo Data as the demo data was fine tuned for individual scenarios and this leads to messy data
- Adjusted Work Item Age over Time Chart to take different time zones into account
- You can now directly link to the metrics of a project or team. The link will also include the selected time frame. This allows easy sharing with a colleague if you look at a specific time frame.
- Update of various third party packages.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/)
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.11.6.540...v25.11.15.1404)

# Older Releases
See [Github](https://github.com/letpeoplework/lighthouse/releases) for older release notes.