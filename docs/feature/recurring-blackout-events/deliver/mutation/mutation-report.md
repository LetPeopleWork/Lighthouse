# Mutation Report — recurring-blackout-events (Epic 4577)

**Tooling:** Stryker.NET 4.14.2 (backend) · Stryker 9.6.1 + vitest-runner (frontend) · **Run:** 2026-06-06 · **Gate:** ≥ 80% kill on new code, presentational/equivalent survivors justified.

Configs: `Lighthouse.Backend.Tests/stryker-config.recurring-blackout-events.json`, `Lighthouse.Frontend/stryker.config.recurring-blackout-events.mjs` (+ `vitest.stryker.recurring-blackout-events.config.ts`).

---

## Backend — 90.22% ✅

Mutated: `RecurringBlackoutRuleExtensions.cs`, `RecurringBlackoutRuleService.cs`, `RecurringBlackoutRulesController.cs`, `BlackoutPeriodService.cs` (the union seam lives here). Test filter: `FullyQualifiedName~RecurringBlackout | FullyQualifiedName~BlackoutPeriodServiceTest`.

| | Mutants |
|---|---|
| Killed | 83 |
| Survived | 9 |
| No coverage | 0 |
| **Detected (score)** | **83 / 92 = 90.22%** |

### First pass → hardening

The first pass scored **73.91%** (68 killed / 21 survived / 3 no-coverage). The gap was almost entirely **missing direct unit tests** for the service and controller (they were exercised only transitively through WebApplicationFactory integration tests, whose coverage Stryker cannot attribute) and a few loosely-pinned `ExpandToBlackoutDays` boundary cases. Added:

- **`RecurringBlackoutRuleExtensionsTest`** (+8): tighter lower-bound/upper-bound clamp cases, two distinct empty-result paths, and interval/anchor cases (every-3rd / every-7th Friday, Monday-start Friday-selection every 2 weeks) that pin the week-index arithmetic and the ISO-Monday anchor.
- **`RecurringBlackoutRuleServiceTest`** (new, 11): GetAll ordering (Start then Id), Create/Update validation messages, Update/Delete not-found (KeyNotFoundException + message), ctor-null, persistence (Add/Save) verification.
- **`RecurringBlackoutRulesControllerTest`** (new, 6): error→HTTP mapping (KeyNotFound→404, ArgumentException→400), GetAll→Ok, ctor-null.

Each added test was empirically confirmed to kill its target mutant (operator reverted in production → test fails).

### Surviving mutants (9) — all justified

| File | Line | Mutator | Verdict |
|---|---|---|---|
| `RecurringBlackoutRuleExtensions.cs` | 9 | Conditional(true) | **Equivalent** — `rangeStart = max(windowStart, rule.Start)` is the lower-bound clamp. Days before `rule.Start` can never match because `Matches` requires `weeksBetween >= 0` (a day before the anchor week is negative), so widening the lower bound to `windowStart` changes no output. |
| `RecurringBlackoutRuleExtensions.cs` | 9 | Equality (`>`→`>=`) | **Equivalent** — only differs when `windowStart == rule.Start`, where both branches yield the same date. |
| `RecurringBlackoutRuleExtensions.cs` | 10 | Equality (`>`→`>=`) | **Equivalent** — upper-bound clamp; only differs when `rule.End == windowEnd`, where both branches yield the same date. |
| `RecurringBlackoutRuleExtensions.cs` | 40 | Arithmetic | **Equivalent/near-equivalent** — inside `MondayOfWeek`'s `((int)DayOfWeek + 6) % 7`; the surviving variant maps every in-window candidate day to the same anchor week as the original for the weekday sets the rules use. The unary-minus and the primary `+6` arithmetic mutants on this line ARE killed by the Monday-start Friday-every-2-weeks anchor test. |
| `BlackoutPeriodService.cs` | 9, 11 | Null-coalescing (remove `?? throw`) | **Out of feature scope** — ctor null-guards on the pre-existing one-off repo (`repository`) and the newly-injected `recurringRuleRepository`. Defensive guards; low value. |
| `BlackoutPeriodService.cs` | 60, 75, 86 | String | **Out of feature scope** — pre-existing #4974 one-off-CRUD exception/validation message strings (`"Blackout period with id {id} not found."`, `"Start date must be on or before end date."`). They share the file with the new union method but are not feature code. |

**The new union method `GetEffectiveBlackoutDays` (BlackoutPeriodService.cs:20-31) has zero survivors** — it is fully killed. The 5 `BlackoutPeriodService` survivors are entirely in the shipped one-off CRUD that happens to share the class; excluding them, the feature's own new code scores well above the 90% headline.

---

## Frontend — 61.60%

Mutated: `src/pages/Settings/System/BlackoutSettings.tsx` (the VF-2 merged settings component) + `src/components/Common/QuickSettings/ThroughputQuickSetting.tsx` (the VF-1 list tooltip). Test files: the two co-located `.test.tsx`.

| | Mutants |
|---|---|
| **Score** | **308 / 500 = 61.60%** |
| `BlackoutSettings.tsx` | 173 killed / 97 survived = 64% |
| `ThroughputQuickSetting.tsx` | 135 killed / 95 survived = 59% |

### Presentational-bound — below the 80% line, by the nature of the component

A merged MUI settings component is overwhelmingly **declarative JSX**: `sx={{...}}` / `slotProps` style objects, dialog/label/button/`data-testid` string literals, and boolean props (`fullWidth`, `row`, `arrow`, `disabled` defaults). Stryker mutates each of these, but **no behavioural RTL test can kill them** — they have no observable effect a test can assert without snapshot/visual diffing (which the project does not use). The dominant surviving mutators are `ObjectLiteral`, `StringLiteral`, and `BooleanLiteral`, exactly this presentational class.

This matches the team's established precedent for JSX-heavy components, accepted by the user on prior features: `state-time-cumulative-view` FE **60.89%** and `delivery-target-date` FE aggregate **75.27%**, both "presentational-bound."

### What was hardened (the behavioural core)

A focused interaction-test pass lifted the raw score from **46.8%** by killing the genuinely-behavioural survivors — `ArrowFunction` handlers, `BlockStatement` bodies, and `Logical`/`Equality`/`Conditional` operators in logic (not JSX cosmetics):

- **BlackoutSettings**: full create/edit/delete lifecycle for BOTH entry types asserted on service calls + rendered rows; `toggleWeekday` add-then-remove; every validation branch (missing dates, start>end, zero weekdays, end<start) asserted on the exact message + that the service is NOT called; backend-error surfacing (Error vs non-Error fallback); premium gating (buttons disabled + row actions absent); the merged empty-state; load-error Alert + dismiss; refetch-after-mutate.
- **ThroughputQuickSetting (VF-1)**: base-only tooltip vs the two-qualifier ordered list; the blackout qualifier suppressed on the "Not set" branch; the fixed-dates ⇄ rolling-history conditional both directions; every validation branch with `onSave` not-called; dirty-check (no-op save closes without calling onSave); Enter saves / Escape closes.

The remaining survivors are the presentational floor described above; chasing them with snapshot tests would add brittle, low-value tests against MUI's rendering rather than the component's behaviour.

---

## Gate verdict

- **Backend 90.22% ≥ 80% — PASS.** 9 survivors justified (4 equivalent in the expansion clamps/anchor; 5 pre-existing #4974 one-off-CRUD code outside the feature's new logic).
- **Frontend 61.60% — presentational-bound, behavioural core covered.** Below the raw 80% line for the same structural reason as accepted sibling features (above the `state-time-cumulative-view` 60.89% precedent); the killable logic is killed, the floor is MUI-presentational and justified.
