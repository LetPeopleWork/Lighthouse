# Story 6710 — Expandable query field

**Story**: ADO #6710 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6710), no parent Epic.
**Route**: working directly, size S. The maintainer skipped DISCOVER, DIVERGE, DISCUSS, DESIGN and DEVOPS
explicitly on 2026-11-27 ("1a").

## What shipped

The query field (WIQL, JQL and the other trackers' query) used to be a fixed four-line box, so a long
query had to be scrolled inside it. It now starts at four lines, grows with the query, and scrolls only
past twenty lines. The new `QueryTextField` (`Lighthouse.Frontend/src/components/Common/BaseSettings/`)
is used by both places that edit a query: the team and portfolio settings (`GeneralSettingsComponent`)
and the create wizards (`CreateWizardShell`).

UI decision (maintainer, 2026-11-27, option 2A of three): grow with the content, using MUI's own
`minRows`/`maxRows`. Rejected: a drag handle (B) and an "expand" dialog (C, a new control).

Follow-up decision (maintainer, 2026-11-27, "b"): a read-only field stays at a fixed four lines. A
read-only field holds what a connector filled in, such as the demo teams' whole CSV file, and letting it
grow to twenty lines would push the rest of the settings form down for no gain.

## Skipped waves, answered in one line each

- DISCUSS: Configuration context only; no new term, concept or domain event. RBAC: none, the field keeps
  its existing read-only rule. Clients (CLI, MCP): N/A, no API change. Website: N/A.
- DESIGN: frontend only, no port or adapter. Loading and failure looks: N/A, the field fetches nothing.
- DEVOPS: no platform risk. Usage-data event: N/A, a usability change with nothing to count as adoption.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A: no page under `docs/` describes the size of the query field. |
| Screenshots | N/A: no `@screenshot` captures the query field. The shots taken on the team and portfolio settings capture single sections (cycle times, blocked rule, Estimation, parent sources, refinement, wait states), not the page. |
| Demo data | N/A: nothing new to show. |
| Website assets | N/A: no asset under `docs/assets/` changes. |
| Clients (CLI, MCP) | N/A: no API change. |
| Context map, `ARCHITECTURE.md` | N/A: no context, concept or edge changes. |
| Usage-data event | N/A, see DEVOPS above. |
| Mutation | Not run with Stryker: the change is one small component of two row limits and one read-only branch. Instead, four manual breaks, all seen red: a fixed `rows={4}` (stays at 40px where 120px is expected), a looser `maxRows` (grows to 350px past the 200px cap), a read-only field that grows (200px where 40px is expected) and a field that is no longer read-only. |
| Release notes | N/A; written at /release. The Story carries the Release Notes tag. |

**Commit**: `7571f7568` (`feat(settings): query field grows with a long query (#6710)`).

## Left open

The two call sites are covered by their existing tests (label and value), not by a test that would fail
if one of them went back to a plain fixed-height `TextField`.

The full `pnpm test` run passed (710 files, 10,431 tests), but the time budget named one offender,
`DashboardHeader.test.tsx` ("shows label and formatted date range…", about 4 s alone). This change does
not render it; `ModifyTeamSettings.queryCommit`, which does render the query field, takes the same
1.3–1.5 s with the old and the new component.
