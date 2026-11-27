# Story 6711 — Search on the feature list

ADO User Story 6711 "Improve Search on Feature List". Product facts live in `handoff.json` (nwdes); this file holds
Lighthouse's own records: the maintainer decisions and the checklists.

## Waves

- DISCOVER, DIVERGE: skipped by the maintainer in as many words (2026-11-27).
- DISCUSS: 2026-11-27.

## Maintainer decisions (DISCUSS, 2026-11-27)

- **Where:** the search box goes on every page that shows the full feature list (`FeatureListDataGrid`): the
  Features page, a Portfolio's Features tab, a Team's Features tab and a Delivery's Work Items tab. The Delivery
  dialog feature pickers (`FeatureGrid`), the Refinement grid and the Archived features grid are out of scope.
- **What it matches:** every visible column, as the cell reads, ignoring case, part of a word is enough.
- **Name column filter:** also matches the Feature's ID, as the cell shows "ID: name".
- **Parent column filter:** already matches both the parent's ID and name (its value is the shown "ID: name"
  text); no change.
- **Remembered?** No. The box is empty each time the page opens.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | N/A, because the search only narrows rows the user can already see on that page; it adds no action, page or data. |
| Lighthouse-Clients CLI / MCP | N/A, because no API route or DTO changes; the search runs in the browser over the list already loaded. |
| Website marketing surface | N/A, because a search box on an existing list is a usability fix, not a capability worth a line or page on the website. The release note carries it (tag `Release Notes` on the Story). |
| Context map | Touches **Work Items** only (the feature list), frontend only. No new glossary term ("search" is a UI word, not a domain concept), no new concept, no domain event published or reacted to. UI surface after ADR-271: the unit's **Items (today: Features) tab** on Teams and Portfolios, plus the top-level Features page and the Delivery's Work Items tab; nothing moves surface. ADR-270 and `ARCHITECTURE.md` §4 need no change. |

## Maintainer decisions (start of AFK, 2026-11-27)

- **Rigor profile:** `standard` for all four slices (Sonnet crafter, acceptance designer and architect; Haiku
  reviewers and examiner).
- **UI sketch (agreed):** the search box sits at the left of the existing grid toolbar row, on the same line as the
  column, layout and export icons. Placeholder `Search Features` (the configured term), with a clear button (✕).
  When nothing matches, the grid says `No Features match "<text>".` (configured term).

  ```
  ┌─────────────────────────────────────────────────────────┐
  │ [🔍 Search Features          ✕]        ▥  ⊞  ⧉  ⤓      │
  ├──────────┬──────────────────────┬────────┬──────────────┤
  │ Order    │ Feature Name         │ State  │ Parent       │
  │ 1        │ PROJ-12: Checkout    │ Doing  │ PROJ-3: Shop │
  └──────────┴──────────────────────┴────────┴──────────────┘
  No match:      No Features match "zzz".
  ```
- **AFK options:** each slice wraps up with refactor and review; mutation runs once at the end. Nothing is pushed.

## DESIGN (2026-11-27, AFK propose mode)

Design facts live in `handoff.json` (rendered to `architecture.md`). Choices taken on the recommended option:

- **MUI's own quick filter** through `apiRef.setQuickFilterValues`, the whole trimmed text as one case-insensitive
  substring. It combines with column filters and goes back to page 1 natively. The text is a `useState` in
  `DataGridBase` (never persisted); the box is an opt-in `search` prop, so the other eight grids are unchanged.
- **"As the cell reads":** a column is searched by its shown text (its `valueFormatter`, or a plain string/number
  value). Searchable: Name (ID and name), Parent, State, Portfolios, the Delivery Team column. **Not searchable**
  (no text value): Progress, Forecasted Start, Forecasted Completion, Likelihood, Warnings, Active Work.
  *Revisit point for the maintainer:* D2 said "every visible column"; the forecast dates could be made searchable
  later through a shown-text formatter. The story's named fields are all covered.
- **No-match message** through the grid's no-results label, `No Features match "<text>".`; the empty-list message
  stays as it is.
- **Loading / failure:** no new look. The box is inside the grid and stays usable while the grid's loading overlay
  shows; where a page already replaces the grid with its spinner or could-not-load look, there is no box.
- **Name column:** S1 shows it through a `valueFormatter`; S4 makes the shown "ID: name" its value with a sort that
  stays by name. The Refinement grid shares the column, so its Name filter also matches the ID.
- No formal model: a stateless row filter. Pure matcher gets property tests; the rest is component tests. No new E2E.

## DEVOPS (2026-11-27, AFK)

- **Platform risk:** none. Frontend change inside the existing deployable; no new service, setting, migration or
  external call.
- **Usage-data event:** `FeatureListSearched = 31`, name only, no property. It is reported once per feature list
  per page visit, the first time its search box holds text, so typing does not send one event per keystroke and a
  visit counts once. It carries nothing: not which list, not which Team or Portfolio, not the search text or its
  length, not how many Features matched. It answers the outcome question "do people use the search box?".
  Emitted by S1 (the slice that first makes the search usable), which also adds it to
  `UsageDataEventName.cs`, the frontend `UsageDataService.ts` name map and `docs/settings/usagedata.md`.
