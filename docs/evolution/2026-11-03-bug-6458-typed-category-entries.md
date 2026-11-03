# Bug #6458 (a) — Category lists could not tell a tracker state from a state mapping's name

Shipped 2026-11-03 through PR #1990 (merge `968ff50b4`, 109 commits over five slices). Part (b) of the same
Bug, the autosave of an unfinished mapping, shipped on its own on 2026-10-31
(`docs/feature/bug-6458-empty-mapping-autosave/`). The feature workspace is
`docs/feature/bug-6458-typed-category-entries/`. The design decision is
[ADR-277](../product/architecture/adr-277-category-lists-hold-raw-states-mappings-are-a-derived-view.md).

## What users get

- The To Do, Doing and Done lists hold the states the tracker reports. A state mapping is a grouping on top of
  them: one chip with a group icon, a tooltip naming its states, no cross (delete the mapping to see its states
  separately again), and dragging it moves all its states together.
- Hand edits can no longer lose a raw state or leave a ghost name behind. Before, removing one chip next to a
  mapping named like a state dropped every Work Item in that state out of WIP, Cycle Time, Throughput and
  forecasts, without a warning.
- A save is refused with a message that says how to fix it when:
  - a mapping is named like a state listed next to the mapping's own states;
  - a mapping's states sit in two categories;
  - a wait state names one state inside a mapping (name the mapping instead).
- The Work Item Aging chart, flow efficiency, the cumulative state chart, and the charts and named cycle times
  scoped to a cycle-time definition all read the mapping's name, as the history records it. Items in a mapped
  state no longer drop out of them. The as-of projections no longer file a mapped state under `Unknown`.
- On start-up, Lighthouse rewrites stored lists that still hold a mapping name into the raw states, once. An
  owner it cannot read is skipped and logged, and still reads correctly, because every read normalises.
- API v1: `toDoStates`, `doingStates` and `doneStates` hold raw states. There are new read-only
  `toDoDisplayStates`, `doingDisplayStates` and `doneDisplayStates` fields. The refusals above are 400s.
  `docs/concepts/api-versioning.md` states the contract.

## How it happened

The lists were plain strings, and the settings editor collapsed a mapping's states into the mapping's name
before saving. So the stored list held a view, not the facts. The backend then expanded any entry equal to a
mapping name, and two hand-edit sequences the Bug 6454 guards could not see lost data (`rca.md`, probes P1 and
P2). The flaw was in the data shape, not in a guard, so the maintainer sent it through DESIGN rather than
adding a third frontend check.

## The fix

| Slice | What it did |
|---|---|
| 1 | Domain view on the owner: one classifier and normaliser (`StateCategoryLists`), the display lists, category lookups by raw state and by display name; the as-of fix |
| 2 | Write path: mappings synced before lists, lists normalised on save, the shadowing, chain and category refusals, display lists on the DTO, refinement candidates from display states |
| 3 | `CategoryListNormalisationSeeder`: idempotent, skip-and-log, on SQLite and Postgres |
| 4 | Read switch: every read and connector site uses the normalised view; an architecture guard pins the only two writers of the lists |
| 5 | Frontend: the chip, its tooltip and drag, pickers from display states, the validation mirror gated on stored settings, the aging chart's loading and failure looks, the wait-state refusal (05-09) and cycle-time-definition columns from display names (05-10). The old reconciliation module is deleted |

## Decisions

- Shape (ii-b): raw-only lists, the mapping derived on read. Typed entries and a category flag per entry
  were rejected (ADR-277).
- Normalise on every read, not only through the seeder. A skipped owner keeps working, and a rolling update
  that briefly runs an old pod re-collapses nothing for good.
- The server owns the rules. The frontend mirrors them only for what the user changed, so an older owner's
  stored shape never blocks autosave.
- Maintainer, 2026-11-03: refuse a wait state that names one member of a mapping, and fix the
  cycle-time-definition columns in this feature, not as a separate Bug.
- Taken during AFK; the maintainer may revisit them:
  - a stored wait state that names one member counts the whole group as wait;
  - refinement hides a state no longer offered instead of dropping it, and the server drops it on save;
  - a mapping only partly listed is valid as a cycle-time boundary.
- No usage-data event, because this is a correction with no new user action. No Lighthouse-Clients version
  bump: the clients pass settings through as opaque JSON, and the display lists are read-only, so sending back
  fetched settings is safe.

## Quality gates

- Final local gates: backend 12179 passed, 0 failed, 2 skipped; frontend 9005 of 9005; both builds clean.
- Each slice was refactored and double-reviewed (Sonnet). The final whole-feature refactor and review fixed
  one blocker: the frontend judged legacy shapes it had not changed, which blocked autosave.
- Mutation (`mutation/results.md`): backend 87.20 % (481 mutants), 95.02 % on the kill-round files; frontend
  80.10 %, then 89.22 % after the kill round.
- Main CI run 41691382136: every gate green.

## Lessons

- **Run the builds after a rebase, even when the suites are skipped.** Skipping them cost four CI cycles:
  - an unused import;
  - Sonar S3776 and S7780;
  - main's new architecture guards on test hosts;
  - Sonar NUnit4001 and CA1862.

  PR #1993 now makes those four Sonar rules fail the local build.
- **A collapsed view stored as data cannot be uncollapsed without guessing.** Store the facts and derive the
  view, even when the view is what the user edits.
- **A mirror of server rules must judge only the user's change.** Otherwise data that the rules came after
  locks the page.

## Deferred

- A hint on a wait state whose mapping was deleted, and the wait-state refusal shown next to the Wait States
  field. Both need a sketch first.
- The surviving mutants recorded in `mutation/results.md`.
- Release-note lines, written at `/release`; the lines owed are listed in `feature-delta.md` "Owed at
  finalize".
- The Lighthouse-Clients skill text (`coaching-patterns.md`) says nothing about the state lists, so no text
  is owed there.
