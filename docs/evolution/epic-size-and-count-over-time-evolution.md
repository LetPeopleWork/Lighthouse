<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — epic-size-and-count-over-time (Finalize)

**Feature ID**: `epic-size-and-count-over-time`
**Epic**: ADO #5585 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5585)
**Stories**: #5614 (01) · #5615 (02) · #5616 (03) · #5617 (04) · #5618 (05) · #5619 (06) · Bug #5620
**Customer**: Chris (reported); LetPeopleWork
**Waves shipped**: DISCUSS (2026-07-31) → DESIGN (2026-07-31) → DELIVER (2026-08-01/02)
**Planning baseline**: `f8de5008b`
**HEAD at finalize**: `e1a28767f` — Lighthouse repo, pushed
**Clients HEAD**: `46b2f61` — `lighthouse-clients` repo, pushed
**Status**: All six slices shipped and pushed; CI green on both pushes; mutation above the 80% gate on
every slice that ran it. One item deliberately left open — see *Open at finalize*.

---

## Feature summary

A delivery's Metrics tab could show that scope grew, but not **which** epic grew or **when**. This
feature adds an *Epics over Time* chart — a stacked bar per recorded day, one band per epic sized by its
item count, with an epic-count line on its own right-hand axis — plus hatching for epics whose size is
still the portfolio default rather than counted items, legend filtering across both delivery charts, a
fix to the burnup's estimated line, and the first exposure of delivery metrics history outside the
browser (client, CLI, MCP).

## Business context

Chris asked to see epic size and epic count over time. The DISCUSS wave found the two halves of that
request sit on very different ground, and saying so plainly shaped the whole epic:

- **Epic count already had real history.** `DeliveryMetricSnapshot.FeatureBreakdownJson` has held one
  entry per epic per day since Epic 3993, so the count line was derivable retroactively — the one series
  in this store that is *not* forward-only, contradicting the blanket forward-only framing in the
  delivery-metrics journey docs.
- **Epic size was computed and thrown away.** `Delivery.ToFeatureMetric` already calculated `totalItems`
  and discarded it. Recording it needed two optional fields on an existing JSON column — no new table, no
  EF migration — and both parsers had to keep tolerating the old four-field shape.

That asymmetry is now stated in the user docs rather than left for a reader to trip over: the count line
reaches back, the bars begin the day Lighthouse started recording sizes.

## What shipped

| Slice | Story | What |
|---|---|---|
| 01 | #5614 | Epic-count line on a new card; Metrics tab re-gridded to 2×2 |
| 02 | #5615 | Per-epic size recorded and drawn as stacked bars |
| 03 | #5616 | Hatched bands where the size is still a default estimate |
| 04 | #5617 | One collapsible legend for both delivery charts, click-to-isolate |
| 05 | #5618 | Burnup: the estimated line stays readable over the Done area |
| 06 | #5619 | `metrics-history` reaches the client, the CLI and MCP |
| — | #5620 | Pre-existing 500 on a null per-epic likelihood, fixed inside slice 02 |

**Mutation**: slice 02 80.95% · slice 03 83.33% · slice 04 80.75% · slice 05 86.76%.

## Key decisions

**ADR-119 was reopened on evidence, not argument.** Its per-epic `::actual`/`::estimated` series split
was withdrawn mid-delivery in favour of one series per epic with the renderer keyed on `seriesId` +
`dataIndex` — the option the ADR's own *Alternatives considered* had rejected. What changed was
shipping slices 01-02: the null-twin tooltip problem the ADR listed as a mere consequence turned up for
real, and under the split it would have been the steady state rather than an edge case.

**The burnup fix was diagnosed wrong at DESIGN, and the code disproved it.** DESIGN held that the
estimated line was painted *underneath* the Done area's fill. MUI-X composes `AreaPlot` before
`LinePlot`, so every line already paints above every area — verified in the library source and then in
the rendered DOM, where `data-series="estimated"` is the last path emitted. The prescribed z-order fix
would have changed nothing. The real cause was the Done area filling at full opacity, leaving a 2px
dashed line with nothing to read against; the fix thins that fill. The slice's own learning hypothesis
had named this branch in advance, which is the reason it was caught rather than shipped.

**One colour per epic across the tab.** The fever chart coloured by position in its own feature list
while the size chart used a sorted map over its own, so the same epic was two colours on one Metrics tab.
Both now read a shared map keyed on **every** epic in the recorded breakdown — not either chart's subset,
because the fever chart drops un-forecastable epics and the size chart drops sizeless ones, so a
per-chart map cannot agree even using the same palette function.

**The client stays lossless; only its consumers summarise** (ADR-121). A 90-day window over fifteen
epics is a four-figure count of breakdown objects plus a forecast distribution per day. The CLI and the
MCP tool project to one row per day by default, but the library returns the payload whole — dropping
data is the caller's choice, not the library's.

## Lessons learned

**Do not dispatch a reviewer against a tree with a mutation run in flight.** StrykerJS runs
`inPlace: true`. A full-slice review of slice 04 read the instrumented copies and returned thirteen
findings including four "blockers" and a REJECTED verdict — *all of them false*. It cited
`DeliveryEpicSizeChart.tsx:619` in a 300-line file, reported AC-4.5 and AC-4.6 as untested when both
scenarios existed, and proposed a comment fix identical to the comment already present.

**Mutation survivors are worth reading, not just counting.** Slice 05's first pass left three mutants
inside `estimatedItemCount && estimatedItemCount > 0`. No test could have killed them: the truthiness
check already excludes `0` and `null`, so `> 0` was unreachable. The fix was deleting the redundant
clause — adding tests would have cemented the redundancy instead.

**A test that cannot fail is worse than no test.** The fever chart's new colour scenario passed against
both the fix and its revert, because its fixture held only forecastable epics, making the shared map and
a per-chart map identical. Reviewers caught it; the rewrite was verified by reverting the production line
and watching it go red. The same discipline caught a loose `0 < fillOpacity < 1` assertion that `0.9`
also satisfied.

**Bar elements are not line elements.** MUI-X bar utility classes are generated from `MuiBarChart`
(`MuiBarChart-element`), not `MuiBarElement-*`, and bars carry no `data-series` attribute — so the
`MuiLineChart-line[data-series=…]` selector convention used elsewhere in this repo does not transfer.
Both the hatch design (ADR-119) and the screenshot E2E hit this.

## Issues encountered

- **Bug #5620**, found during DESIGN by reading types rather than at runtime: `DeliveryFeatureMetric.Likelihood`
  is `double?` but its DTO was non-nullable `double`, so one un-forecastable epic threw a `JsonException`
  and 500'd the **whole** delivery's metrics-history. Fixed inside slice 02 with a round-trip test.
- **Slice 04's `04-01` step is logged FAIL** in `deliver/execution-log.json`: an AC-4.6 scenario was
  unsatisfiable as written (it asserted the last `ChartsContainer` call belonged to an untouched sibling
  chart that React never re-renders). It was resolved in `f4c41ea8c` / `2c5d7dd81`, but the log was never
  updated — so the execution log understates completion. Slices 05 and 06 never entered the log at all,
  having been delivered directly rather than through `nw-execute`.

## Open at finalize

- **The `0.3` Done-area fill opacity is underived.** `appColors.primary.main` is `#30574e`; at 30% it is
  a pale wash on the light theme and dark on the dark one. The risk that it trades an unreadable *line*
  for an unreadable *area* (AC-5.2) can only be settled by eye. Benjamin accepted it as "ok for now".
- **Clients release not cut.** `.changeset/delivery-metrics-history.md` is committed and pushed;
  `pnpm release:version` is a deliberate manual step still owed before the clients' release gate.

## Permanent artifacts

- ADRs: `docs/product/architecture/adr-119…adr-122` (already permanent — no migration needed)
- User docs: `docs/portfolios/detail.md` § *Epics over Time*
- Screenshot: `docs/assets/features/deliveryEpicSize.png`, regenerated alongside the four existing
  delivery shots via the existing delivery over-time `@screenshot` test
- Mutation records: `docs/feature/epic-size-and-count-over-time/mutation/results-slice-0{2,3,4,5}.md`
