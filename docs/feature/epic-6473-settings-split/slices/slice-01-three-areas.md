# Slice 01 (walking skeleton): Three settings areas, everything where it was inside them

Story US-01. job_id `job-config-admin-find-how-we-work-in-one-place`. Effort about 2 days (split the redirects from the POM work if it grows).

## Outcome a user sees

The header's "System Settings" becomes "Settings" with three areas: Organisation, Access and System. Each area uses
today's page shell and holds today's tabs whole: Organisation → Configuration; Access → System Admins, API Keys;
System → Database, Encryption, Demo Data, System Info. Old links open the same tab.

## Learning hypothesis

If System Admins see their settings grouped by who decides, they find a named setting at the first try (K1), and
nobody reports a broken bookmark (K3). Disproved if testers open the wrong area first for two or more of five settings.

## IN

- Area menu "Settings ▾" (M1), routes `/settings/:area/:tab?`, area and tab definitions in one place.
- Gating per tab exactly as today, via `useRbac()`; fallback resolved only after `isLoading` is false.
- Page heading "System Settings" becomes "Settings › {area}" (M2); header drawer on mobile lists the three areas.
- Redirects for the 14 legacy (case-insensitive) `?tab=` keys, bare `/settings`, unknown area or tab, and `/settings/license` → System.
- In-app links: Task Manager "the full log", the two demo-data links, the two licence links.
- POMs: `LighthousePage.goToSettings`, `SettingsPage.goTo*`, `RbacSettingsPage.goToAccessTab`; tab test ids kept.

## OUT

Splitting the Configuration tab and the behaviour rows' loading and failure looks (slice 02; the gap is accepted on
`main` until then, no release in between), Licence tab (slice 03), Connections (slices 04, 05), any setting change, docs and
screenshots (finalize).

## Problem

Priya Raman is a flow coach and System Admin at Northwind Logistics. To add a blackout period she scans seven tabs in
one row, past Database and Encryption, which belong to Tomasz Nowak, who runs the server.

## Who

- System Admin who sets up how the organisation works | a few times a quarter | wants to find it fast
- Platform operator | backups, encryption, log level | wants what is his apart
- Team reader with RBAC on | creates personal API keys | must not see admin tabs

## Domain examples

1. Priya opens Settings → Organisation and finds Configuration with blackout periods, as before.
2. Tomasz opens his runbook link `/settings?tab=database` and lands on System → Database.
3. Morgan Lee, a Team reader, opens Settings: Access shows API Keys only; System shows System Info without Log Level;
   no Organisation.
4. Priya, with RBAC on, follows a link to `/settings/access/system-admins`; the page waits for her access to load and
   opens System Admins, not the first tab.

## UAT scenarios

### Scenario: The three areas group today's tabs

Given Priya Raman is a System Admin of Northwind Logistics
When she opens Settings
Then she can choose Organisation, Access or System
And Organisation holds Configuration, Access holds System Admins and API Keys, System holds Database, Encryption,
Demo Data and System Info

### Scenario: A runbook link opens the same tab

Given Tomasz Nowak's runbook links "/settings?tab=database"
When he opens the link
Then he sees System, Database, with backup and restore as before

### Scenario: Every old tab key opens the tab that held the setting

Given each of the 14 keys the old page accepted, such as "rbac", "apikeys", "logs" or "demodata"
When a System Admin opens "/settings?tab=" with that key
Then the area and tab listed for it in the feature-delta Navigation table open, for example "logs" opens System,
System Info

### Scenario: A reader sees only what is theirs

Given Morgan Lee reads one Team, is no System Admin, and RBAC is on
When he opens Settings
Then he sees Access with API Keys and System with System Info without Log Level
And he sees no Organisation area

### Scenario: A deep link waits for access to load

Given Priya is a System Admin and RBAC is on
When she opens "/settings/access/system-admins" on a slow connection
Then she sees a spinner until her access has loaded, and then System Admins

### Scenario: The licence link from a connection form no longer opens a blank page

Given Tomasz is editing the Jira connection "Northwind Jira"
When he follows the link to import a licence
Then he lands on the System area

## Acceptance criteria

- [ ] Header entry "Settings" leads to three areas; each area lists exactly the tabs above.
- [ ] Tab visibility per area matches today's rules for RBAC off, System Admin and anyone else.
- [ ] Page heading names the area.
- [ ] All 14 legacy keys, bare `/settings`, unknown paths and `/settings/license` resolve as in the feature-delta table.
- [ ] No fallback redirect happens while `useRbac()` is loading; a spinner shows instead.
- [ ] In-app links point at the new paths.
- [ ] New settings paths raise no usage-data page event.

## Outcome KPIs

K1 (right area at first try) and K3 (no broken link); see feature-delta.

## Technical notes

- Gating via `useRbac()` only; nothing fetches `my-summary` directly.
- Reuse the `Settings.tsx` shell, `TabList`, `TabPanel` and every panel; no backend change.
- Enforcement tests that name `pages/Settings/System/SystemSettingsTab.tsx` move with any rename.
- Dependencies: none.
