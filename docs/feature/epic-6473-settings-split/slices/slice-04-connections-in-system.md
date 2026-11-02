# Slice 04: Connections live in System

Story US-04. job_id `job-operator-run-the-instance-apart-from-how-we-work`. Effort about 1 day. Depends on slice 01.
Decided by M4 (2026-11-02).

## Outcome a user sees

System gets a Connections tab, its first tab. It holds what the Overview's "{Work Tracking Systems}" section holds
today: the list with its columns, Edit and Delete, the reconnect banners, the empty state ("No {Work Tracking System}
found. Load Demo Data or Check the documentation") and the "Add {Work Tracking System}" button. The Overview loses the
section and the button and keeps Portfolios and Teams.

## Learning hypothesis

If connections sit with the instance's other settings, admins look for them in System and nobody asks where the
connections went (K1). Disproved if testers open the Overview first to fix a connection.

## IN

- Move the section out of `OverviewDashboard` into System → Connections, reusing `DataGridBase` (same columns, same
  `storageKey`), `ReconnectBanner`, the delete dialog and the empty-state alert. The filter box stays on the Overview and no longer narrows connections;
  System → Connections has no text filter (maintainer, DESIGN MD4). The Overview keeps today's `ReconnectBanner`s, and
  the same banner also shows on System → Connections (MD2).
- Loading look: lone spinner. Failure looks: today's connections-refused alert (403) and the inline "Could not load the {Work Tracking Systems}."
  alert otherwise, never the empty state.
- The Overview still asks whether a connection exists, for the onboarding stepper and the disabled "Add {team}".
- Page objects: a System connections page object takes over `addConnection`, `editConnection`, `deleteConnection`
  from `OverviewPage`; the eight specs that call them switch to it.

## OUT

Entry points, return paths and old URLs (slice 05); any change to the connection pages themselves; mapping (U3);
Any new banner (MD2: today's banner stays on the Overview as it is).

## Problem

Tomasz Nowak runs the Northwind Logistics instance. To rotate the Jira token he scrolls the Overview past 2 Portfolios
and 12 Teams to the connections at the bottom, while everything else he looks after is under Settings → System.

## Who

- Platform operator | rotates credentials, fixes broken connections | wants them with the server's settings
- System Admin setting up a new instance | adds the first connection | follows the onboarding stepper

## Domain examples

1. Tomasz opens Settings → System → Connections, sees "Northwind Jira" and "Northwind ADO", and opens Edit on the first.
2. Priya deletes the retired connection "Old CSV import" from System → Connections; the delete dialog is the same.
3. A fresh instance: System → Connections shows "No {Work Tracking System} found. Load Demo Data or Check the
   documentation", and the Load Demo Data link opens System → Demo Data.
4. Morgan Lee, no System Admin, sees no Connections tab, as he saw no connections section before.

## UAT scenarios

### Scenario: The connections are listed under System

Given Northwind Logistics has the connections "Northwind Jira" and "Northwind ADO"
When Tomasz opens Settings, System, Connections
Then he sees both, each with Edit and Delete as on the Overview before

### Scenario: The Overview no longer lists connections

Given Priya is a System Admin
When she opens the Overview
Then she sees the Portfolios and Teams, and no connections section and no button to add a connection

### Scenario: A fresh instance says how to start

Given an instance with no connection
When Priya opens Settings, System, Connections
Then she is told no connection exists, with a link to load demo data and a link to the documentation

### Scenario: A list that cannot be loaded says so

Given the connection list cannot be loaded
When Tomasz opens Settings, System, Connections
Then he sees that the connections could not be loaded, not "no connection found"

### Scenario: A reader sees no connections

Given Morgan Lee is no System Admin and RBAC is on
When he opens Settings, System
Then he sees no Connections tab

## Acceptance criteria

- [ ] System → Connections holds the list, Edit, Delete, reconnect banners, empty state and the Add button, unchanged except that it has no text filter (MD4).
- [ ] The Overview still shows a reconnect banner for each connection that needs reconnecting.
- [ ] The Overview shows no connections section and no Add connection button.
- [ ] Loading and failure looks as above; the empty state never shows while loading or after a failure.
- [ ] The tab is visible to System Admins only.

## Outcome KPIs

K1 (right area at first try, now including "fix a connection") and K2; see feature-delta.

## Technical notes

- Connection terms are configurable (`WORK_TRACKING_SYSTEM`, `WORK_TRACKING_SYSTEMS`): copy uses them.
- No API change; the list keeps today's System Admin guard.
