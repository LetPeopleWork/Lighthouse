# Epic 6473 — Settings split by who decides

**Finished:** 2026-11-14 · ADO Epic #6473 · Stories #6478–#6482, Bug #6498

## The problem

Settings was one page with seven tabs in a row. It mixed how an organisation works (terminology, calendar, refresh and
ordering) with how the instance is run (database, encryption, demo data, usage data, logs) and with who may do what
(System Admins, API Keys). Connections lived on the Overview and the license had no page of its own. A platform
operator could rename a Team by accident, and a Team lead looked for the calendar next to the encryption key.

## What changed

- **One Settings menu, three areas** (`/settings/<area>/<tab>`), each tab shown only to who may open it:
  - **Organization:** Terminology, Calendar, Refresh & ordering (including the Feature ordering switch).
  - **Access:** System-wide access (only with RBAC on) and API Keys.
  - **System:** Connections, Database, Encryption, Demo Data, Usage Data (the "Never send usage data" veto), System
    Info, Logs, Refresh History, License.
- **Old addresses still work.** `/settings?tab=…` and the earlier slugs redirect to the new tab; a tab the person may
  not open lands on the nearest one they may.
- **Connections moved to System → Connections**, with their create and edit pages under it. Every entry point (the
  onboarding stepper, the "create a connection first" hint, the Task Manager) leads there. The Overview keeps a
  connect-first hint, and says so when the connections cannot be read instead of pretending there are none.
- **The license got its own tab** under System; the header popover stays.

Slices: S1 three areas (4b5406c07), S2 Organization tabs and Usage Data under System (472d458b3), S3 License and Logs
tabs (aa22bbced), S4 Connections in System (0d86ead6e), S5 every way to a connection (b32e8a76e), plus their review
fixes. Decisions are in `docs/feature/epic-6473-settings-split/feature-delta.md`.

## Gates

- Whole-candidate review: accepted. EXAMINE: accepted, 12 of 12 expectations met.
- Mutation (frontend only): 80.50 % (421 killed, 96 survived, 6 no coverage) after one kill round from 69.60 %.
  See `docs/feature/epic-6473-settings-split/mutation/results.md`.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done.** 31 pages under `docs/` plus `ARCHITECTURE.md` name the three areas and the new paths; `settings/settings.md` has the area and tab table and the redirect note. File names and anchors kept. |
| Screenshots | **Done, except the four Auth shots.** Re-run one test at a time against a clean backend with the premium licence: configuration, blackout periods, system info, encryption, database, demo data, licensing; 15 PNGs refreshed. Two new `@screenshot` tests: the License tab (`settings/license.png`, now shown in `licensing/licensing.md`) and System → Connections (`settings/worktrackingsystems.png`, which had no producing test). **Deferred:** `rbac.png`, `rbac_groupmapping.png`, `apikeys.png`, `apikeys_create.png` come from `AuthScreenshots.spec.ts` (`@auth`), which needs the Keycloak stack in `examples/keycloak/`; they still show the old navigation. `RefreshHistory.png` has no producing test and stays as it is. |
| Demo data | **N/A**, because no data shape changes. |
| Website assets | **N/A, no change needed.** The website hot-links `docs/assets/` from `@main` and the screenshot file names stayed, so refreshed images reach it. The website has no "System Settings" copy (grepped). |
| Clients (CLI, MCP) | **N/A**, because no route, payload or permission changed. The clients only link to `settings/usagedata.html`, which still exists. No client release. |
| Context map, `ARCHITECTURE.md` | **Done.** ADR-271 carries a dated "done" note after the Decision 2 table and marks the U1 step done; ADR-270 needs no change (no new context, term or edge — the Epic moved UI surfaces only). `ARCHITECTURE.md` describes the behaviour settings on their two tabs. |
| Usage-data event | **N/A**, by the maintainer's DEVOPS decision: a navigation change, nothing new to count. `UsageDataEventName` and `docs/settings/usagedata.md`'s event list are unchanged. |
| Mutation | **Done**, 80.50 % on the frontend; the backend changed no production code. |
| Release notes | N/A; written at /release. |

## Knowingly left open

- **D17:** which Team and Feature refresh settings are needed at all is to be rethought. Proposed as its own ADO item.
- `licensing/licensing.md` still lists "export and import the configuration through the settings" as a Premium limit,
  although no such control exists. Left for a licensing check rather than changed here.
