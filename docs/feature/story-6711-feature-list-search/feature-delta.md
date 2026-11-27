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
