# DELIVER wave decisions — feature-size-closed-date-toggle

ADO #5036 — "Feature Size / View by Closed Date Toggle". 9 Vitest milestones landed plus one user-feedback fix; the Playwright spec planned in DISTILL E2E-01 was deliberately not shipped.

---

## DDD-01: Lean DELIVER orchestration (one crafter dispatch per layer)

DISTILL DWD-09 already authorised skipping the 4-parallel-reviewer ceremony for this single-component UI slice. The orchestrator further consolidated step-level dispatch: a single `@nw-software-crafter` Task implemented all 9 Vitest milestones in TDD order. The user-feedback fix (DDD-09) was applied directly by the orchestrator with explicit RED → GREEN.

Rationale: the alternative (10 separate crafter dispatches) would have multiplied prompt-template overhead without adding TDD discipline. The crafter's own RED→GREEN→COMMIT cycle gave per-milestone granularity (visible in the commits `2ce226c3..b5b387b0`), and each commit's diff stayed small enough to be reviewed at the file level if needed.

## DDD-02: Toggle aria-label kept as `Y-axis mode`

The toggle now controls both X and Y semantics (per DWD-03), so the original aria-label `Y-axis mode` is technically a misnomer. Renaming it would have required a synchronous update to every existing Vitest assertion that queries `aria-label="Y-axis mode"` plus any Playwright POM that depends on the same string. The crafter kept the label stable and confined the semantic change to the new option (`Closed Date`).

Carried forward: if a future slice surfaces a separate X-axis control (e.g. a date-range filter), revisit the aria-label split then.

## DDD-03: M-08 regex widened for locale-portable CI

DISTILL acceptance-tests.md M-08 specified the regex `/^Mar 2026$|^03\/2026$|^Mar 10, 2026$/`. The crafter widened it to assert "contains a month token AND `2026`, and is not a raw epoch number" because the reused `dateValueFormatter` calls `toLocaleDateString()`, whose exact output depends on the host locale. CI runners may have non-English locales; the original regex would have been brittle.

The contract preserved: M-08 still verifies "X-axis labels are human-readable, not raw timestamps" — exactly the assertion DISTILL articulated. Documented in commit `f818a371`.

## DDD-04: `today` captured at component mount (per DWD-05)

`useMemo(() => Date.now(), [])` captures the "today" pin once at mount. This matches DWD-05's confirmed-by-user requirement: stable during chart interaction, not re-evaluated on each render or toggle click. The pin is used both as the X coordinate for unclosed items and (per DDD-09) as the closed-date X-axis `max`.

## DDD-05: Percentile orientation flipped via single mock extension

DWD-06 specified that percentile reference lines become horizontal (`y={...}`) in Closed Date mode. The Vitest mock for `@mui/x-charts` already accepted the `x` prop; the crafter extended the same mock to also forward `y` (and the existing `data-x-formatter-sample` / `data-axis` capture attributes). One shared mock, no duplication — per the brief's anti-pitfall.

## DDD-06: Playwright E2E (DISTILL E2E-01) deliberately not shipped

DISTILL declared a single Playwright spec at `Lighthouse.EndToEndTests/tests/specs/metrics/FeatureSizeChartToggle.spec.ts` to prove the toggle wiring reaches the user via a real React tree in a real browser. After the slice landed and was manually verified by the user against a started backend, the decision was made to drop the E2E entirely.

Rationale (per the user, applying `feedback_ci_and_e2e_minimalism`): the 9 Vitest milestones already pin every observable behaviour — toggle rendering with the right option count, click-to-switch mode, axis swap, today-pin, percentile orientation flip, X-axis formatter, X-axis max-at-today, and the round-trip preservation. The E2E would add "real browser proves the click bubbles up" at a multi-minute cost (full backend seed + ADO sync + Playwright browser launch + DOM stabilisation polling), repeating the same assertions the Vitest layer already enforces in milliseconds.

A draft of the spec + the `MetricsWidget` POM extensions (`getFeatureSizeChartToggle`, `clickFeatureSizeMode`) was written and type-checked but never committed. The drafts are recoverable from this conversation if a future E2E-only regression (e.g. MUI ToggleButton ARIA semantics changing on upgrade) warrants reinstating them.

Back-propagation note: `docs/feature/feature-size-closed-date-toggle/distill/acceptance-tests.md` still lists E2E-01 as part of the slice handoff. The DISTILL spec is left as-is for historical traceability; this DELIVER decision overrides it for the as-shipped scope. Future readers of the DISTILL file should consult this DDD-06 before assuming an E2E exists.

## DDD-07: No DES audit log, no telemetry events

DISTILL DWD-12 declared the Python-centric DES infrastructure unavailable in this repo. Per the precedent set by `portfolio-delete-serialise`, no `execution-log.json` was created, no density telemetry was emitted, and the deliver phase did not run `des-init-log` / `des-verify-integrity`.

The audit trail is the git commit history: 11 conventional commits on `worktree-wild-bouncing-lemon`, one per milestone plus the user-feedback fix plus one Biome style commit.

## DDD-08: Quality gates run locally before push

- `pnpm test` — 2856 / 2856 across 224 files (FeatureSizeScatterPlotChart 48 → 59).
- `pnpm build` — clean (Biome zero-warnings, tsc, Vite bundling).
- `dotnet build` — 0 warnings, 0 errors (slice did not touch backend).
- Manual browser verification on `http://localhost:5169` — closed-date axis labels, today-pin behaviour, and category-chip × axis-pin interaction confirmed by the user.

## DDD-09: User-feedback fix — pin X-axis to today, drop Today reference line

Manual browser test surfaced two issues with the as-shipped Closed Date view:

1. When unclosed (To Do / In Progress) items were toggled off via the legend chips, the X axis auto-collapsed to the latest closed-date and "today" dropped off the right edge. The user expected today to remain the rightmost point regardless of which category chips are active.
2. The dashed "Today" vertical line + label (originally specified by DWD-05) became visual noise once the axis is pinned to today — the right edge of the chart already conveys the same information.

Fix applied with explicit RED → GREEN:

- Added `max: today` to the closed-date X-axis time scale so the right edge is fixed regardless of visible categories.
- Removed the `<ChartsReferenceLine x={today} label="Today" />` block.
- Two new Vitest scenarios pin the new behaviour: *"Closed Date X axis right edge stays at today even when only past-closed items are visible"* and *"Closed Date mode draws no Today reference line"*.
- WS-01 and M-07 had their now-obsolete Today-line assertions removed in the same commit.

Committed as `3444607c` — `fix(metrics): pin Closed Date X-axis to today, drop Today reference line`. Vitest 57 → 59 tests.

Back-propagation note: DISTILL DWD-05 specified the Today reference line. This decision overrides it. The DISTILL file is left as-is for historical traceability; future readers should consult this DDD-09.

## Files modified

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.tsx` | +287 / -133 lines initially, then DDD-09 fix: +1 / -13 lines (pin X max to today, drop Today reference line) |
| `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx` | +449 lines initially, then DDD-09 fix: +47 / -25 lines (2 new tests, remove Today-line assertions from WS-01 + M-07) |

## Commits on this branch

```
3444607c fix(metrics): pin Closed Date X-axis to today, drop Today reference line
b5b387b0 style(metrics): apply Biome auto-format to FeatureSizeScatterPlotChart tests
f818a371 test(metrics): M-08 Closed Date X axis renders human-readable labels
2b9ae8b6 test(metrics): M-07 all-unclosed dataset stacks at today
384a36a3 test(metrics): M-05 round-trip through Closed Date preserves data
8633a5f6 feat(metrics): WS-01 Closed Date mode swaps axes and pins unclosed items at today
8e2ece27 test(metrics): M-01 toggle renders 3 options when estimation unit configured
2b589e9f feat(metrics): M-02 toggle visible without estimation unit (Cycle Time / Closed Date)
5b748b7d test(metrics): M-06 add empty data set regression guard
4233e8f4 test(metrics): M-04 add Estimation mode regression guard
2ce226c3 test(metrics): M-03 add Cycle Time mode regression guard
```

## Outstanding before push

1. Optionally commit this DELIVER workspace doc per `feedback_finalize_workspace_commit`.
2. Push to `main` (trunk-based per `feedback_trunk_based_development`) — the auto-classifier may block `git push HEAD:main` even within auto mode; re-authorize per `feedback_classifier_main_push_and_ado_reason`.
3. Watch CI green; on success transition ADO #5036 `Active` → `Resolved` with `System.Reason` set in the same call.

## Back-propagation to DISTILL

Two DISTILL decisions were overridden during DELIVER. Both are recorded in this file (DDD-06 overrides E2E-01; DDD-09 overrides DWD-05) rather than mutating the historical DISTILL document. Future readers consulting `distill/acceptance-tests.md` should treat DDD-06 and DDD-09 here as the as-shipped truth.
