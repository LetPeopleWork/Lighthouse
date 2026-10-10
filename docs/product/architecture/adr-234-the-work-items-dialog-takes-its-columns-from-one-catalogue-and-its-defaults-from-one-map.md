# ADR-234: The Work Items Dialog Takes Its Columns From One Catalogue and Its Defaults From One Map

**Status**: Proposed
**Date**: 2026-10-10
**Feature**: story-6248-work-items-dialog-context-columns (ADO User Story #6248)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE, for maintainer confirmation. The catalogue,
per-context layout and the locked defaults table were decided by the maintainer in DISCUSS (2026-10-10).

---

## Context

The Work Items dialog is opened from 16 places: ten charts, every widget's *View Data*, the Cumulative State Time
drill-down, the Delivery section and timeline, and both feature lists. Each caller adds at most one column of its own
(`highlightColumn`), which holds Cycle Time, Age, Size or Days Contributed depending on who opened it, and every
dialog persists its layout under one shared grid key. Hiding a column in one dialog hides it in all of them, and the
one context column's width carries across its four meanings. Rows already carry dates, ages, parent, blocked-since
and time in state that the dialog never shows.

DISCUSS locked: a full catalogue where the opening context picks the defaults (the rest reachable through the grid's
*Manage columns*), layout remembered per context, and a defaults table declared in one place so that changing a
context's defaults is a one-line edit. The maintainer expects to adjust that table after reviewing it in use.

Quality attributes, in order: **modifiability** of the defaults (one edit, reaching existing users),
**correctness** (no layout leaks between contexts; no column offered that the rows cannot fill), **no regression**
of the judgement cells (SLE colouring, age band, SLE risk, time in state, warnings).

## Decision

1. **One map, one entry per context.** A single frontend module declares, for each context (one row of the locked
   table: closed items, estimation, arrivals, in progress, aging, blocked, stale, work distribution, Feature size, a
   Feature's child items, Delivery timeline, Cumulative State Time; a thirteenth for the started-and-closed list is
   proposed), which columns are visible besides ID, Name, Type
   and State, which one sorts (descending, carrying today's highlight treatment), and whether the catalogue is offered
   at all. Callers name a context instead of building a column.
2. **One catalogue.** A second module builds every column the dialog knows. Row-backed columns are offered when the
   rows can fill them (the estimate only when the owner has an estimation field; Size and the forecast only for
   Feature rows, where Owned by stays a fixed column as today). Columns that need something only the caller knows (age band, SLE risk, warnings, days
   contributed, named cycle times, the day an age is measured on) are offered only when the caller passes that input,
   as the existing descriptors already work. A context's default that the rows cannot fill is dropped silently.
3. **Layout per context and owner kind.** The grid's storage key is the context id plus Team or Portfolio, so each
   keeps its own visibility, order and widths, and a Team's dialog never changes a Portfolio's. The dialogs grouped
   under one context (for example the Throughput bar and the Cycle Time scatter, which both list closed items) share
   one layout, as they share one set of defaults. Whether that grouping is fine enough is for the maintainer to
   confirm; making it per call site is a key change only.
4. **Hidden by default, overrides persisted.** The shared grid component accepts a default visibility model. The
   effective model is the defaults overlaid with what the user changed, and only the user's changes are stored.
   *Reset layout* returns to the defaults. A column the user never touched follows the map, including after the map
   changes; a column the user turned on or off stays as they left it; a column added to the catalogue later starts
   hidden. Every write of the visibility model, including the one that forces a non-hideable column back on, goes
   through the same "store only what differs" rule.

## Alternatives considered

- **Keep a caller-built column, add more caller-built columns.** Rejected: it is the current design multiplied; the
  defaults would live in 16 places and could not be reviewed or changed as one table.
- **One context per call site (about 30 keys), each pointing at a row's defaults.** Rejected for now: the one-line
  edit becomes "find every site of this row", and a user hides the same column once per screen rather than once per
  meaning. The storage key alone can be made finer later.
- **Persist the whole visibility model, seeded with the defaults** (the grid's current behaviour, with a seed).
  Rejected: the first change a user makes stores every default, so later edits to the map never reach that user,
  which defeats the purpose of the one-place table.
- **Revive the unused `useColumnVisibility` hook.** Rejected: an unpersisted hidden-list beside the grid's own model
  gives two sources of truth for visibility.
- **Mark synthetic rows (Cumulative State Time) with their own row type.** Rejected in favour of the context's
  "no catalogue" flag, which touches one entry instead of every consumer of the row type.

## Consequences

- Positive: changing a context's defaults is one entry, and it reaches every user who did not override that column.
  No layout leaks between contexts. The dialog's column code moves out of a 563-line component into small builders,
  which keeps cognitive complexity in reach.
- Positive: compile-time enforcement. Contexts are a closed union, the shared run charts take a required context, and
  removing `highlightColumn` from the props at the end of the story makes any missed caller a build error.
- Negative: dialogs that share a context share a layout; splitting needs a key change and starts those users from
  the defaults.
- Negative: the old shared layout key is no longer read and is left in browsers (well under 1 KB).
- Negative: other grids gain an optional prop on the shared grid component; with none passed they behave as before,
  which a test pins.
