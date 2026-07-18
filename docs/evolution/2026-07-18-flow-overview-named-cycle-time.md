# Evolution: flow-overview-named-cycle-time

- **Date finalized**: 2026-07-18
- **ADO**: Story #5509 ("Named cycle times on the Flow Overview percentiles widget"). Not transitioned by this run — left for manual review.
- **Status**: Delivered on `main` (DISCUSS → DESIGN → DISTILL → DELIVER all done; 5/5 roadmap steps RED→GREEN→COMMIT). Mutation ≥ 80% on the feature surface — backend 84.2% feature-scoped, frontend 81.9% overall (`MetricsService.ts` 100%, `CycleTimePercentiles.tsx` 80.7%, `BaseMetricsView.tsx` 77.8%).
- **Workspace (history)**: `docs/feature/flow-overview-named-cycle-time/`
- **Builds on**: [multiple-cycle-times](./2026-06-09-multiple-cycle-times.md) (Epic 5251 — the named-cycle-time definitions, computation, premium gate and read endpoint this feature consumes unchanged).
- **ADRs**: [ADR-100](../product/architecture/adr-100-named-cycle-time-rag-neutrality-and-sle-anchoring.md) (RAG neutrality + SLE anchoring), [ADR-101](../product/architecture/adr-101-named-cycle-time-trend-contract-and-cache-key.md) (trend contract + cache key).

## What shipped

The **Cycle Time Percentiles** widget on Flow Overview — the page a delivery lead lands on —
now has a cycle-time selector. Epic 5251 put named cycle times on the Flow Metrics
scatterplot; this brings them to the overview, where the question "how long does this
actually take our customer?" is usually asked first.

Selecting a named definition changes four things together, which is the whole point: the
widget's 50/70/85/95 recompute over that definition's window, the RAG chip goes neutral, View
Data shows that definition's durations, and the trend footer compares the named window against
its own previous period. Team and Portfolio behave identically.

- **Slice 01 (frontend only)** — selection lifted to `BaseMetricsView` as
  `percentilesScopeDefinitionId`, mirroring the shipped `cumulativeScopeDefinitionId` pattern,
  and consumed *only* by the percentiles widget (the scatterplot keeps its own local state —
  no cross-tab coupling). The selector is a controlled MUI `Select` shaped after
  `CumulativeStateTimeScopeControl`, rendering nothing when no definitions exist and
  self-resetting when the selected definition disappears or goes `isValid:false`. No backend
  change — `getCycleTimePercentiles(id, start, end, definitionId)` already existed.
- **Slice 02 (backend + frontend)** — `cycleTimePercentilesInfo` gained an additive optional
  `definitionId` on both Team and Portfolio, with a `_Def_{id}` cache-key segment so a named
  and a default trend for the same range cannot collide.

### The decision worth remembering: neutral RAG (D11 / ADR-100)

The SLE is a *single* per-owner pair defined against the **default** started→finished window.
A named window is generally wider. Judging a wider window against the default SLE renders a
**false red** — the widget would assert a breach nobody ever agreed to. So under a named
selection Lighthouse reports `ragStatus: "none"` and explains why, rather than deriving a
verdict it cannot stand behind. Per-definition SLEs were rejected for now: they reopen the
ADR-064 config surface plus a migration, and no user has asked to *judge* a named window —
only to *see* it.

## Amendments made during DELIVER

**[A1] The explanation is the widget-header tip tooltip, not a card-body caption.** ADR-100
always specified `tipText` on the `WidgetShell` header tip. DELIVER's first pass *additionally*
rendered the same sentence as a caption inside the card; on review it was removed, because the
Overview widget is `size:"small"` and the caption plus a default-height `size="small"` Select
together clipped the 50th-percentile row — the widget was losing data in order to explain
itself. The Select's own padding is now trimmed on this widget only. No decision reversed;
the explanation's placement narrowed to hover-only, which is what the ADR said all along.

**[A2] The demonstrating definition is `Analysis to Done`, not `Lead Time (End to End)`.**
DISTILL assumed the end-to-end definition (Backlog→Done) is materially wider than the default
window. Measured against demo data during the walking skeleton, it is not: the synthesized demo
journey enters `Backlog` and `Next` on the same day, so Backlog dwell is zero and the
definition returns percentiles *identical* to Default. The plumbing is correct —
`Analysis to Done` does differ — but the E2E and the docs screenshot had to switch definitions
to demonstrate anything.

**Known demo-data limitation (open):** demo data cannot show the headline "wide named window vs
narrow default" case. Fixing it means giving demo items real Backlog dwell in
`DemoDataFactory`, which shifts every demo number and forces a broad `@screenshot`
regeneration. Deliberately out of scope for 5509 — worth a follow-up story.

## Adversarial review — four real defects, found after the feature "worked"

A high-effort review of the full feature diff surfaced seven findings, four of them genuine
bugs in code that passed every test:

1. **A false improving trend.** `BuildCycleTimePercentilesInfoDto` only skipped the comparison
   when *both* periods were empty. A named definition with nothing closed this period but data
   last period rendered an empty percentile table under a green "faster" chip — the current
   median falls back to 0, and 0 beats any real previous median. Only the named path could
   produce an empty current list (the default path always emits four values), so this was new
   with `definitionId`. Reproduced RED, then fixed to skip on an empty *current* period.
2. **A silently lying widget.** Both named fetches were fire-and-forget over a
   `withErrorHandling` that rethrows, so a failed request became an unhandled rejection and
   left the *Default* numbers on screen under a named heading. Now reverts to Default,
   mirroring the invalid-definition self-reset.
3. **A stale-response race.** No sequencing, so a slow earlier definition could overwrite a
   fast later one. Fixed with a generation counter; the entity/window reset bumps it too.
4. **An E2E that raced its own subject.** The scope label flips synchronously on click while
   the refetch is in flight, so the assertion could read the previous scope's table. It passed
   only because of [A2] — the definition returned Default's values anyway. `selectScope` now
   awaits the `definitionId` response. Suite went 22.7s → 14.3s and stopped depending on timing.

**Accepted, not fixed:** an unresolvable definition id yields a neutral RAG with Default View
Data. Neutral is the *safe* side there — the alternative is exactly the SLE-anchored false red
this feature exists to prevent — and the selector's self-reset corrects it within a tick. This
is the one remaining real mutation survivor (`BaseMetricsView.tsx:542`); killing it properly
means fixing the inconsistency, not testing around it.

## Mutation testing — what it actually caught

Feature-scoped backend kill rate started at **38.7%**. The survivors were not cosmetic:

- Nothing pinned the comparison window, so flipping the sign on either offset — comparing
  against a window in the **future** — went undetected.
- Nothing pinned the cache key's date segment, so collapsing it such that every date range
  shared one entry also went undetected.
- An existing test asserted only that two endpoints returned the **same** status for an unknown
  `definitionId`. Two matching 500s would have satisfied it. Now asserts 200 on both.

New tests cover the comparison-window boundaries via the rendered labels, detail-row pairing,
the em-dash for a named definition with no previous series, an invalid definition (a boundary
state that left the workflow, which a `definition == null` guard alone would still compute),
and cache isolation across date ranges — with Portfolio copies, since it carries its own copy
of the same arithmetic. Backend reached **84.2%**.

Two frontend survivors were left alive deliberately as **equivalent mutants**: the generation
counter's `++`→`--` and `+=1`→`-=1`. The counter only has to *change* to invalidate an
in-flight response, so the direction carries no meaning; a test killing them would assert
arithmetic, not behaviour.

## Lessons

- **A green suite is not evidence of correct behaviour.** Every defect above lived in code that
  passed the full 3444-test backend suite and 3573-test frontend suite. Mutation testing and an
  adversarial diff read found them; the tests did not.
- **Relative assertions can pass while everything is broken.** "Endpoint A returns the same
  status as endpoint B" is satisfied by two 500s. Assert the absolute expectation.
- **Demo data is a product surface.** A feature whose headline case cannot be demonstrated on
  demo data is only half-shipped, even when the code is correct.
- **Check that the ledger's rules are actually enforced.** CI failed on `new_violations = 6`
  (5× NUnit4002 `Is.EqualTo(0)`→`Is.Zero`, 1× CA1861 inline expected-array) — both rules already
  documented in `docs/ci-learnings.md`, both invisible to a local `dotnet build` because they
  are INFO severity, and the pre-commit hook installed in this working copy is secrets-only. The
  grep that would have caught them in seconds was run *after* the failure. Recorded as NUnit4002
  Recurrence 1 and CA1861 Recurrence 6 — the latter noting this is the second time CA1861 has
  arrived specifically via mutation-driven test-strengthening.
- **Stryker config traps cost a full cycle each.** Stryker.NET has no `{a..b}` line-span support
  (whole files only — hence `score_ranges.py` to narrow the report to the feature's lines, since
  `TeamMetricsService` spans many unrelated features), and Stryker-JS wants `:323-333`, not
  `:323:333`, which silently mutates nothing and reports a confident, meaningless number.

## Permanent artifacts

- ADR-100, ADR-101 — `docs/product/architecture/` (already permanent; not migrated)
- Architecture delta — `docs/product/architecture/brief.md` §flow-overview-named-cycle-time
- User docs — `docs/metrics/widgets.md` § "Named Cycle Times", with
  `docs/assets/features/metrics/percentilesNamedCycleTime.png`
- E2E — `Lighthouse.EndToEndTests/tests/specs/flow/NamedCycleTimePercentiles.spec.ts`,
  POM `CycleTimePercentilesWidget` in `tests/models/metrics/MetricsPage.ts`, plus the
  `@screenshot` case in `tests/specs/screenshots/Screenshots.spec.ts`
- Backend contract tests — `NamedCycleTimeTrendInfoApiIntegrationTest.cs`
