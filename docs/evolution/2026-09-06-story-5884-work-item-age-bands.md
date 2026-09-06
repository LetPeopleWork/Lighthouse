# Story #5884 — Work Item Age Band column in the work item dialog

**Shipped** 2026-09-06 · ADO User Story #5884 · workspace `docs/feature/story-5884-work-item-age-bands/`

## What users get

The Work Item Age chart already paints a coloured pace zone under every in-flight dot: how this item's
age compares against how long finished items historically took to leave the state it is sitting in. That
judgement was readable only by eye, one dot at a time.

It is now a value on a row. Both work item dialogs behind the aging widget — the header's **View Data**
list and the one behind a single dot — carry an **Age Band** column reading `Below 50th`, `50th-70th`,
`70th-85th`, `85th-95th`, `Above 95th`, or `No history`. A flow coach can order the list worst-band-first,
cut it to one band through the grid's own column filter, and export it as a CSV whose band column reads in
words. Team and portfolio scope both, since `BaseMetricsView` is shared.

The evidence that prompted it: Thrivve Partners run client flow reviews on their own tool with the chart
colouring switched **off**, reading a table of bands instead. A practitioner in the target persona choosing
text over colour for exactly this judgement.

## What was built

One new pure module, one optional field on three existing types, one derived column. **Zero backend
change** — no endpoint, DTO, schema, migration, cache key or new dependency. The per-state age percentiles
the metrics API already returns are enough.

| Commit | Change |
| --- | --- |
| `04149929d` | DISCUSS + DESIGN + DISTILL plan |
| `0807f1981` | Acceptance tests scaffolded, skipped until the rule exists |
| `7815b98ca`, `2800045de` | Palette in one place; per-state ladder resolver |
| `6a7b1a8a6`, `699279c14` | Place an age against its ladder; name, colour and package the descriptor |
| `bd8b26f82` | Chart reads its zones off the shared ladder |
| `2e7482d2b`, `a9d45ef11`, `0cd70e955` | Band in the dialog; View Data wiring |
| `47a35e00a`, `1a3ff2216` | Column through to the dialog, and behind a chart dot |
| `48b0fd9b7`, `d46d060e0` | Sort, filter and export by band |
| `e9cbd7eeb`, `fb68f48f5`, `733b37aa8` | One home for the column's wording; unknown band name sorts to the bottom |
| `c9cf6fefb` | Playwright walking skeleton |
| `d1fbe88ae` | Tests closing the mutation survivors |

## Decisions worth keeping

**The band rule was extracted, not cloned** (DDD-1, [ADR-188](../product/architecture/adr-188-pace-band-ladder-shared-by-chart-geometry-and-dialog-value.md)).
`computePaceBandRects` split into a pure ladder resolver plus a geometry projector in
`utils/charts/paceBands.ts`. Carry-forward, the half-open `(lower, upper]` boundary, the case-insensitive
state match and the palette each exist exactly once, so the cell and the zone under the same dot cannot
disagree. The invariant is bounded and the bound is stated: the geometry also takes an axis domain the
classifier does not have, so what holds is that *inside* that domain the two agree. Outside it the
classifier is right and the chart's clipping is the artefact — a band is a property of the item and the
ladder, not of the viewport.

**The band label is derived from the boundary's percentile number, not read from a fixed array** (DDD-2).
The honest reason is not that ladder lengths vary today — they cannot; both metrics services hard-code
`[50, 70, 85, 95]`. It is written for configurable percentiles, already named and already deferred, under
which a positional five-entry array would silently relabel a three-boundary ladder's top band `70th-85th`
when it means `Above 85th`.

**The last Doing state's percentiles are the cycle-time percentiles, and that is honest under an Age Band
header** (D10). For the last Doing state, cumulative age at last exit *is* end-to-end cycle time — an item
leaving it is finished. The clone is correct by definition, not an approximation. Explained once in the
widget info text and the docs page; not per row, which would make the dialog assert a doubt the chart does
not.

**Sorting is index-into-the-option-label list, and the binding is structural** (DDD-5). Because that list
is also `valueOptions` and `valueGetter` returns a label, a `valueGetter` returning a rank makes every
lookup miss and the sort collapse. The wrong split fails the sort test, not only the CSV.

**A band name the offered list never carried sorts to the bottom, both directions** (`733b37aa8`). The
option list is built from one state's ladder while a row's name comes from its own state. Today they always
agree; let a team choose per-state percentiles and they need not. The position lookup answered "not found",
which is smaller than every real rank, so such a row jumped to the head of the column — ahead of the
`No history` sentinel that exists precisely to stop that.

**Two shipped grid behaviours contradict locked acceptance criteria, and were raised rather than absorbed**
(DDD-12, DDD-13, `design/upstream-changes.md`). Export ignores an active column filter, and the filter model
is declared as persisted but never written. The maintainer chose to narrow the acceptance criterion rather
than swap `getSortedRowIds()` for the filtered selector — a one-line change that would silently alter what
every grid in the product writes to disk. `useDataGridExport`'s `visibleRows` variable and its comment still
claim filters apply, and still do not. That is its own piece of work.

**On a persisted layout the column arrives last, and that is accepted** (DDD-14). `WorkItemsDialog`
hard-codes one `storageKey` across all sixteen of its render sites, and `DataGridBase` appends an unknown
column to the end. Bumping the key would discard every user's widths and visibility across those sixteen
dialogs to move one column the user can drag; Reset Layout restores the declared position.

## Corrected during delivery

**DDD-8 claimed more than it proves.** The chart-dialog agreement property compares the two functions'
outputs; it never asserts the geometry calls the shared resolver. It passed unchanged against the
pre-refactor chart, where both sides implemented the rule independently — so step `01-04`'s RED gate was
unattainable and was logged `FAIL` rather than manufactured by editing the test. What the property does
guarantee is the thing that reaches a user: any behavioural divergence reds. A re-inlining that stays
correct slips past it, which is a maintainability regression, not a defect. Gating the call itself needs an
import-level or lint rule.

## Mutation testing

StrykerJS scored **90.86%** on 186 mutants (169 killed, 15 survived, 2 uncovered), past the 80% bar on the
first run. A triage pass closed seven survivors and both uncovered mutants with six tests and two
assertions, each kill confirmed by applying that exact mutation by hand. **Stryker was not re-run**, so no
second score is claimed. Two accepted survivors were verified equivalent rather than assumed — the
`rank < 0` guard duplicates what a negative array index already does, and `NO_PERCENTILES` defaults a list
whose only consumer filters on `.value > 0`. Per-survivor reasoning is in
`docs/feature/story-5884-work-item-age-bands/mutation/results.md`.

Backend mutation testing is N/A: no backend file changed.

## Delivery-log reconciliation

`deliver/execution-log.json` is missing two events, and the gap is bookkeeping rather than unfinished work.
Step `02-05`'s COMMIT was never logged — the session was killed one second after it committed — and `01-05`
logged only a failing COMMIT. Both commits exist and are green: `02-05` is `d1fbe88ae`, `01-05` is
`2e7482d2b`. Every step from `01-01` to `02-05` maps to a commit. The log was deliberately left as written
rather than backfilled on a dead session's behalf.

Two sessions were briefly running against this checkout at once, after `unsnooze` auto-resumed a
usage-limited session into a detached tmux while a second session was reading the same files. Nothing was
lost, but the working tree changed underneath a reader mid-task. Worth knowing before editing a shared
checkout after any gap.

## Verified before release

- `pnpm test` — 350 files, 4800 tests green
- `pnpm build` — clean, Biome clean via the `prebuild` hook
- Playwright walking skeleton reads the band in a real browser (`c9cf6fefb`)

## Still open

- **Docs and screenshot.** The Work Item Age metric page needs the Age Band section, including the note
  that the last Doing state's band is the cycle-time distribution, plus a screenshot.
- **The before-timing for the shortlist KPI** must be captured on ADO #5884 *before* the release note is
  drafted. Measured afterwards the number cannot be recovered — nobody can un-see the column.
- **The flipside-table verdict.** This feature was chosen over a separate table view on the bet that it
  would answer whether that view is still wanted. An unanswered question means the bet was never settled,
  which is a different outcome from the feature failing. The commitment to record a yes/no on #5884 is due
  before release; the KPI expires 2026-10-31.
