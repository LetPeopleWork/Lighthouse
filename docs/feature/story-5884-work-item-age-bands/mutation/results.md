# Mutation testing — 5884 (Work Item Age bands in the work item dialog)

Run 2026-09-06 against `main` @ `733b37aa8`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | N/A | — | — | — | — | — |
| Frontend (StrykerJS 9.6.1) | **90.86 %** | 186 | 169 | 15 | 0 | 4 m 19 s |

Configs: `stryker.5884.frontend.json`, `vitest.stryker.mutation.ts`.

The score above is the run's own result. The triage below closed seven of the fifteen survivors and
the two no-coverage mutants with six new tests and two extra assertions on existing ones; each kill
was confirmed by applying that exact mutation to the source by hand and watching the named test go
red. **Stryker was not run a second time**, so no re-measured score is claimed. On the same 186
mutants the suite would now kill 176 and leave 8 standing, all of them recorded below as equivalent
or presentational.

## Backend

N/A — this feature changed no backend file. The band is derived in the browser from the per-state age
percentiles the metrics API already returns; no endpoint, DTO or query changed.

## Frontend

| file | tested | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| `utils/charts/paceBands.ts` | 117 | 109 | 6 | 2 |
| `components/Common/WorkItemsDialog/WorkItemsDialog.tsx` | 47 | 40 | 7 | 0 |
| `components/Common/Charts/WorkItemAgingChart.tsx` | 21 | 19 | 2 | 0 |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 1 | 1 | 0 | 0 |

### Closed by this pass

- **`paceBands.ts:57`, two survivors on picking the widest ladder** — `longest &&
  longest.percentiles.length >= ladder.percentiles.length` survived both `→ longest && true` and
  `>= → >`. The first stops a later, longer ladder ever displacing the incumbent; the second changes
  which of two equally long ladders wins, and since the band names are read off the percentile
  *numbers*, that changes the words the column offers. Every fixture in the suite happened to put the
  longest ladder first, so neither showed. Two tests in `paceBands.test.ts` now pin it: one where the
  later state is cut at four boundaries and the earlier at two (kills `&& true`), one where two states
  are cut at two boundaries each but at different ones — 50/95 against 60/90 (kills `>= → >`).

- **`paceBands.ts:188-189`, the empty-ladders branch of `paceBandOptionLabels`, three mutants** —
  `if (!widest)` survived being forced to `false`, its body survived being emptied, and
  `return [NO_HISTORY_BAND_LABEL]` survived becoming `return []`. The last two were reported as
  no-coverage: the branch is unreachable through `buildAgeBandColumnDescriptor`, which returns
  `undefined` before it can be called. But `paceBandOptionLabels` is exported and carries its own
  contract, and one direct call with no ladders at all now asserts it answers with the no-history name
  alone. All three die on it.

- **`paceBands.ts:159`, two survivors on `rank === undefined || !ladder`** — `→ &&` and
  `→ false || !ladder`. Through today's call graph the two halves are not independently reachable:
  `classifyPaceBand` returns `undefined` exactly when `ladderForState` finds nothing, so the only
  caller always supplies both or neither. The function is exported with two independent parameters
  though, and the guard is what keeps the label honest if a rank ever goes missing for some other
  reason — a NaN age, say. One added assertion, an absent rank against a state that *does* have a
  ladder, kills both.

- **`WorkItemAgingChart.tsx:404`, the dependency array emptied** — `[perStatePercentileValues,
  doingStates, workItemAgeTerm, workItemsTerm] → []`. Nothing re-rendered the chart with different
  percentiles, so the memo was never asked to recompute. Switching team or date range with a stale
  band column left behind is the real consequence. A rerender that swaps Review's history from
  8/12/17/24 days to 30/40/50/60 now asserts the same nineteen-day item moves from `85th-95th` to
  `Below 50th`.

- **`WorkItemsDialog.tsx:134`, `worstFirst * (firstRank - secondRank) → /`** — the sign survives for
  every non-zero difference, so ordering *across* bands is unaffected and the existing ordering tests
  could not see it. The one case that differs is a tie: the correct form returns `0`, the mutant
  returns `±Infinity`, and two rows in the same band stop holding the order they arrived in. Confirmed
  observable in the grid, not merely in the comparator: a test on the two `Above 95th` items now
  asserts ZEN-388 stays above ZEN-401 after a worst-first sort.

- **`WorkItemsDialog.tsx:144`, the fallback text colour dropped** — `judgementCell(…,
  "text.secondary") → ""`. The existing "leaves an item with nothing to compare against muted and
  unpainted" test checked only that the cell carries no band colour and no background wash, so the
  *muted* half of its own name went unasserted. One added line pins the secondary text colour, which
  is what stops a no-history row reading with the same weight as one that does carry a judgement.

### Accepted survivors

- **`WorkItemsDialog.tsx:82`, `:84`, `:85`, `:89`, `:90` — five presentational literals.**
  `padding: "4px 8px"`, `display: "inline-flex"`, `alignItems: "center"`, the whole `style` object,
  and `backgroundColor: … : "transparent"`. Pure CSS values with no behavioural consequence; the cell
  asserts on its text and on the two colours that carry meaning. Pinning inline-flex or an 8-pixel
  padding would freeze layout choices that are free to change.

- **`paceBands.ts:278`, `rank < 0 ? undefined : … → false ? undefined : …`.** Equivalent, and verified
  rather than assumed: `paceBandColorForRank` is handed `-1` for the no-history name and `-2` for a
  name the list never carried, and both take `PACE_BAND_COLORS_LOW_TO_HIGH[Math.min(rank, 4)]` — a
  negative index into a fixed-length array, which is `undefined`. The two tests that cover those cases
  ran against the mutant and stayed green. The `rank < 0` guard is belt-and-braces: it says out loud
  what the palette lookup happens to do anyway. It is not being deleted — the array's behaviour at a
  negative index is not the kind of thing a reader should have to know to follow this line.

- **`WorkItemAgingChart.tsx:340`, `const NO_PERCENTILES: IPercentileValue[] = [] → ["Stryker was
  here"]`.** Equivalent, and this one contradicts the initial triage, which read it as the default for
  `perStatePercentileValues`. It is not — it defaults `workItemAgePercentileValues`, whose only
  consumer is `.filter((p) => p.value > 0)`. A bare string has no `value`, `undefined > 0` is false,
  and the mutated default filters itself away to the same empty list. Applied by hand, all 73 chart
  tests stayed green. There is no test that could kill it without asserting on a value the component
  never exposes.

### Not mutated

- **`components/Common/DataGrid/types.ts`** — the story added `valueOptions?: string[]` to
  `DataGridColumn`, restated there because `GridColDef` is a union and `Omit` over it drops the key.
  A type-only field emits no JavaScript, so there is nothing for Stryker to mutate. It is not in the
  config's `mutate` list for that reason.
- **`pages/Common/MetricsView/WidgetShell.tsx`** — in the `mutate` list at `:24`, `:47-48` and `:384`,
  and absent from the per-file table because those three spans produced zero mutants: a type import,
  a documented optional prop, and one JSX attribute forwarding the descriptor untouched.

### Running it again

Two path constraints, both of which cost a run to discover.

**Invoke from `Lighthouse.Frontend/`, pointing back into `docs/`:**

```
pnpm exec stryker run ../docs/feature/story-5884-work-item-age-bands/mutation/stryker.5884.frontend.json
```

The skill documents a `docs/…` path that only resolves when a `docs` symlink exists inside the
frontend directory. There is none here, and one must not be created — Biome walks such a symlink and
reformats the documentation tree.

**`vitest.stryker.mutation.ts` has to sit at the frontend root, and that copy stays untracked.** Its
`setupFiles: "./setupTests.ts"` resolves relative to the config file's own directory, so from `docs/`
it points at a file that does not exist; from `docs/` Node cannot resolve `vite` either. So the run
needs a duplicate at `Lighthouse.Frontend/vitest.stryker.mutation.ts`, which `**/vitest.stryker*.ts`
in `.gitignore` keeps out of the tree, as it should.

That same pattern used to swallow the copy in *this* directory too, which is the one that has to be
committed — it is the record of what was run. `.gitignore` now carries an exception for
`docs/feature/*/mutation/vitest.stryker*.ts`, so the next feature does not have to discover this and
reach for `git add -f`, which is how the equivalent file for US 5611 got here.
