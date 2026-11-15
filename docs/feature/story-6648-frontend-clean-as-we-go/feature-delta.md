# Feature: story-6648-frontend-clean-as-we-go

Story #6648 · Clean as we go: move the slowest frontend page and dialog tests down to functions, hooks and single
components · unparented, tagged Continuous Improvement (its Epic, #6506 "Fast, reliable test suites", is Closed). The
brief is the ADO description of #6648 (acceptance criteria and "What NOT to do"); it is not copied here.

Follows on from T13 [#6561](../story-6561-t13-slowest-frontend-test-files/feature-delta.md), which designed slices B
(`ReportsPage.removal`, `ReportsPage.combinedPdf`) and C (`OverviewDashboard`, `BaseMetricsView.loading`), delivered
slice A only, and left B and C "to be picked up later as their own item" (its "Maintainer decisions at DELIVER start",
2026-11-07). The backend twin is #6647.

Picked up 2026-11-15 on the maintainer's word as its own item (option B: the top 3 files, named then as
`ReportsPage.removal`, `OverviewDashboard`, `ReportsPage.combinedPdf`), not folded into a feature.

## Wave: DISCUSS / [REF] Scope and cross-cutting

DISCOVER and DIVERGE: not run. T4 #6462 and T13 measured the problem, and the lever is FE7's, so no solution direction is
open. DISCUSS is the #6648 ADO description plus this section, as T13 did for the same contract on other files. A fuller
DISCUSS pass can still be asked for.

- job_id: infrastructure-only. Size **M** (two to three files, one shared test kit; T13 sized the same work M).
- Contexts touched: none. Only test files change, unless the maintainer says yes to T13's one named production extraction
  (Q7 below). ADR-270 and `ARCHITECTURE.md` section 4: no change.
- RBAC: N/A, because no permission, policy or gated control changes; the gating tests keep asserting what they assert.
- Lighthouse-Clients (CLI/MCP): N/A, because no API, DTO or route changes.
- Website: N/A, because nothing a user sees changes.
- Usage-data event: N/A, because the item is behaviour-free and adds no user action.
- UI sketch: N/A, because no pixel changes.
- Terminology: rewritten tests keep asserting the seeded default terms.
- Context map: no change. Release notes: none.
- Before a feature Epic: not applicable; this item is not a feature Epic. Overlap with Story 6535 (arrivals in reports,
  open in DESIGN): 6535 changes the report panels (`ArrivalsPanel`, `PanelRegistry`), none of the files below. Re-read
  each file at slice start; a conflict with trunk stops the slice (no autonomous rebase).

## Wave: DISCUSS / [REF] Inherited

Link, do not copy:

- T13's DESIGN for slices B and C, per-file readings, Reuse Analysis, decisions D-6561-1 to D-6561-20, Q1 to Q7:
  `../story-6561-t13-slowest-frontend-test-files/feature-delta.md` ("Per-file reading", "Slice B", "Slice C",
  "Reuse Analysis", "Risks and open questions"). Its measurement scripts (`check-behaviour-free.sh`,
  `run-touched-files.sh`) live in its `measurement/` and `baseline/` folders.
- FE7's behaviour-free contract for test rewrites: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md`.
- The budget: `Scripts/test-timings/test-time-budget.json` (frontend 54 s, set by T13's budget commit `37cc1a614`) and
  `check_budget.py`.
- Test-writing skill: `.claude/skills/test-writing/SKILL.md` and `references/frontend.md` (rules 1 to 4 and 8 carry the how).

## Definition of Ready (re-run for this item)

| # | Item | Answer |
|---|---|---|
| 1 | Problem stated in user or developer terms | Yes: the slowest page and dialog tests prove rules through a whole render |
| 2 | Acceptance criteria observable | Yes: per-file medians from the `test-timings-frontend` CSVs, the mapping note, deliberate breaks |
| 3 | Behaviour-free | Yes, with T13's Q7 extraction as the only possible production change, and only on a yes |
| 4 | Dependencies met | T4 #6462 and T13 #6561 Done. FE5 #6393 (`BaseMetricsView` split): still New, so `BaseMetricsView.tsx` stays untouched |
| 5 | Baseline re-counted | Yes, below |
| 6 | Sized | M |
| 7 | Ratchet named | The frontend per-file budget (54 s) only goes down; `slowTests.ts` entries only go away |
| 8 | Cross-cutting answered | Yes, above |
| 9 | Open questions listed | Yes, below; Q-6648-1 needs the maintainer before DESIGN closes |

## Baseline re-count (2026-11-15)

Method as #6648 states it: per-file sum of per-test seconds from the `test-timings-frontend` CSV, median over runs. Five
complete `main` runs that ran the frontend tests: 43591054526, 43595152117, 43599549188, 43619017023, 43685403479
(newest `ddbf42937`). Per-run totals 682 to 1,060 s, so the runner spread is wide; judge files by the median only.

| File | #6648 brief: s, tests, slowest | Now: s (median of 5), spread, tests, slowest | Rank brief → now |
|---|---|---|---|
| `src/pages/Reports/ReportsPage.removal.test.tsx` | 17.1, 21, 1.44 | 16.6, 9.3, 21, 1.30 | 1 → 1 |
| `src/pages/Overview/OverviewDashboard.test.tsx` | 16.4, 51, 1.13 | **12.9**, 8.1, **46**, 0.78 | 2 → **9** |
| `src/pages/Reports/ReportsPage.combinedPdf.test.tsx` | 15.9, 18, 1.65 | 15.2, 10.1, 18, 1.75 | 3 → 4 |
| `src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx` | 15.0, 24, 1.63 | 15.5, 7.5, 24, 1.75 | 4 → 2 |
| `src/pages/Common/Reports/ReportsTab.test.tsx` | 11.6 (outside top 10) | **15.4**, 8.1, 47, 1.30 | 11+ → **3** |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | 14.8, 133, 0.71 | 14.8, 9.1, 133, 0.69 | 6 → 5 |
| `src/components/Common/DataGrid/DataGridBase.paging.test.tsx` | 14.8, 28, 2.48 | 13.9, 9.8, 28, 2.37 | 5 → 6 |

What moved:

- `OverviewDashboard` lost 5 tests and about 3.5 s when the connections moved to System (`0d86ead6e`, then
  `5a95d8a08`). T13's slice C plan for it read "header buttons, alerts, the stepper **and the connections section**"
  as the 28 tests to move, so that plan is stale and needs a fresh reading if the file stays in scope.
- `ReportsTab.test` rose from 11.6 to 15.4 s with no commit to the file since 2026-11-08. Its 4-run figure in the brief
  and this 5-run figure come from different run sets; the change may be runner spread (8.1 s here), not a regression.
- The list is still flat: ranks 2 to 9 lie between 15.5 and 12.9 s.

Frontend per-file budget: 54 s; largest file now 16.6 s, so the budget is not near.

## Open questions

| # | Question | Recommendation | Needs the maintainer |
|---|---|---|---|
| Q-6648-1 | The maintainer named the brief's top 3. Today's top 3 is `removal`, `RefinementView.comments`, `ReportsTab.test`; `OverviewDashboard` is 9th | Keep the two Reports page files (T13's slice B is designed and ready); decide the third: today's rank 2 or 3, or Overview as named | **Yes**, before DESIGN |
| Q7 (T13) | One `refactor(reports)` commit extracting `failedAmong` and `leaveOutPlanOf` from `useReportsPdfExport` into `printFailures.ts` | Ask at DESIGN; without it, `combinedPdf`'s cut rests on a cheaper `rowOf` and two merges (T13's "Skip-path plan") | Yes, at DESIGN |
| Q6 (T13) | Overview fallback: a pure function out of the page for the header gating | Only if Overview stays in scope and its spike saves too little | Only then |

## Checklist

- [ ] Behaviour-free: production behaviour and pixels unchanged; no production file changes unless Q7 is a yes
- [ ] Each file's time profile recorded before its rewrite
- [ ] No rule loses its test: every removed test is mapped to the test that now proves it (commit body or a note)
- [ ] Each moved rule proven by a deliberate break (delete or invert the production line, see red, restore, `git diff` clean)
- [ ] Remaining renders shared per wiring; loading and failure looks kept where the view has them
- [ ] Moves separate from edits: a pure move commit before any edit commit
- [ ] Ratchet: no `slowTests.ts` entry or budget raised; the 54 s budget lowered with run ids when the new slowest file allows
- [ ] Coverage of the touched directories not lower (`pnpm test:coverage`, opt-in for exactly this reason)
- [ ] Tests pass alone, in order and shuffled
- [ ] `pnpm test --changed` and `pnpm test:fast --changed` inside the work; `pnpm test` and `pnpm build` clean at the end
- [ ] Mutation last, on frozen code: Stryker on `printFailures.ts` if Q7 is taken; otherwise N/A with the reason
- [ ] The #6648 offender table refreshed from fresh CSVs when the item closes
- [ ] No internal references in test comments; Feature and Work Item wording in test names
