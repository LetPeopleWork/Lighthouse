# Bug #5145 "Pace Percentiles Wrong" — RCA v2 (REOPENED)

**Status:** Second RCA. Prior "Option A" fix (cumulative reached-at-least population + skip-imputation + non-decreasing clamp, ADR-047 / D12-revised, shipped 2026-06-01) is **insufficient**. Chris on the DEV build (2026-06-04): "looks better, but still wrong. This needs a proper RCA and we may have to adjust our approach in general."
**Method:** Toyota 5-Whys, multi-causal, evidence-first. Every behavioural claim cites `file:line` against the CURRENT code on `main`.
**Verdict (1 sentence):** The shipped metric still computes each column over a *cohort-dependent, partly-synthetic* population (cumulative "reached" set + the `EarliestAtOrAfterExit` imputation) and the last column is structurally a different metric/population than the all-items `CycleTime` lines, so neither the user's "elapsed-since-StartedDate at the LAST exit of state S over the items that LEFT S" model nor the last-column-alignment promise is actually realised — the clamp only hides the worst drops.

---

## 1. The user's intended model (the NEW target spec)

For each doing state S, in configured order:
- **Population** = items that **left S** (had an exit transition `FromState == S`). If an item left S several times (rework / backward flow), use the **LAST** such exit.
- **Per-item observation** = `(moment item LEFT S, last exit) − item.StartedDate` — total elapsed wall time from "work started" to "exit of S". NOT time-in-state.
- **Percentiles** of those observations per state.
- **Visual monotonicity** = if state S's value is lower than the column to its left (misconfigured order / backward flow), **clamp up** so it never drops below the previous column (at worst equal). The clamp is a *display* guard, not the metric.
- **Connector-agnostic** — operate only on normalized (mapped) transitions + `StartedDate`. No `switch (WorkTrackingSystem)`.

The four divergences below are measured against THIS spec.

---

## 2. Proof of defect — concrete transition trace through the CURRENT code

### Fixture (Jira-style rework / backward flow, mapped doing-state space)

`team.DoingStates = ["In Progress", "In Review", "Waiting"]` (configured order).
Mapped transitions are already in mapped-state space and `FromState==ToState` rows are dropped (`WorkItemStateTransitionMapper.cs:17`). Assume "Waiting for feedback" maps to mapped name **"Waiting"**; everything below is mapped.

Three completed items, all `StartedDate = Day 0`, all close (reach Done):

| Item | StartedDate | Transition path (mapped, ordered) | ClosedDate | CycleTime* |
|------|-------------|-----------------------------------|-----------|-----------|
| **A** | Day 0 | InProgress→InReview (d3), InReview→Waiting (d5), Waiting→InReview (d7), InReview→Done (d9) | Day 9 | 10 |
| **B** | Day 0 | InProgress→InReview (d2), InReview→Done (d4) | Day 4 | 5 |
| **C** | Day 0 | InProgress→Done (d1) | Day 1 | 2 |

\* `CycleTime = (ClosedDate.Date − StartedDate.Date).TotalDays + 1` (`WorkItemBase.cs:53-71,93-96`). So CycleTimes = **{2, 5, 10}**.
Note B and C **never enter "Waiting"**; C **never enters "In Review"** either. This is normal — items close from different states.

### Step A — axis construction
`BuildWorkflowStateOrder(team.DoingStates, items)` (`BaseMetricsService.cs:31-47`) starts with `["In Progress","In Review","Waiting"]` then appends observed `FromState`s not already known. Observed exit `FromState`s here = {In Progress, In Review, Waiting} — all already configured, so axis = `["In Progress","In Review","Waiting"]`. (Had any *unmapped* status leaked a distinct `FromState`, e.g. a raw "Waiting for feedback" not mapped to "Waiting", it would be **appended as a 4th column** here — see §3 Branch B.)

### Step B — `RestrictToMappedDoingStates` (`BaseMetricsService.cs:77-94`)
`mappedDoingStates = team.DoingStates` (3 states). `observedStates = {InProgress, InReview, Waiting}`. `observedCoveredByConfig = 3`, `3*2 > 3` → `configCoversObservedWorkflow = true` → `paceStates = ["In Progress","In Review","Waiting"]`. Clean here. (The fallback misfires only when config covers ≤50% of observed states — §3 Branch B.)

### Step C — per-state observations (`GroupCumulativeObservationsByState` + `CumulativeAgeObservationForItemAtState`, `BaseMetricsService.cs:96-181`)
For each state the code keeps an item iff `ReachedState(S)` = `Any(FromState==S || ToState==S)` (`:157-161`), then takes `lastExit = Max(TransitionedAt where FromState==S)` (`:144-148`), else falls back to `EarliestAtOrAfterExit` imputation (`:150,163-181`). Observation = `CumulativeAgeAtExit(StartedDate, exitMoment)` = `(exit.Date − started.Date).TotalDays + 1` (`:219-222`).

**In Progress** (position 0):
- A: last `FromState==InProgress` exit = d3 → `(3−0)+1 = 4`.
- B: exit = d2 → `3`. C: exit = d1 → `2`. → observations = **{4, 3, 2}**.

**In Review** (position 1):
- A: reached (yes). last `FromState==InReview` exit = max(d5, d9) = **d9** → `(9−0)+1 = 10`.
- B: last `FromState==InReview` exit = d4 → `5`.
- C: `ReachedState(InReview)`? C has only InProgress→Done; `FromState`/`ToState` never == "In Review" → **excluded**. → observations = **{10, 5}**.

**Waiting** (position 2):
- A: `ReachedState(Waiting)` yes. last `FromState==Waiting` exit = d7 → `(7−0)+1 = 8`.
- B: reached Waiting? No → excluded. C: no → excluded. → observations = **{8}** (only A).

### Step D — percentiles (nearest-rank, `PercentileCalculator.cs:5-18`, `index = floor(p/100·n) − 1`, clamped to [0,n−1])

| State | obs (sorted) | n | p50 | p70 | p85 | p95 |
|-------|-------------|---|-----|-----|-----|-----|
| In Progress | 2,3,4 | 3 | idx0→**2** | idx1→**3** | idx1→**3** | idx2→**4** |
| In Review | 5,10 | 2 | idx0→**5** | idx0→**5** | idx0→**5** | idx1→**10** |
| Waiting | 8 | 1 | **8** | **8** | **8** | **8** |

### Step E — non-decreasing clamp per rank (`ClampPercentilesNonDecreasing.cs:183-208`)

| rank | InProg | InReview | Waiting |
|------|--------|----------|---------|
| p50 | 2 | max(5,2)=5 | max(8,5)=8 |
| p70 | 3 | max(5,3)=5 | max(8,5)=8 |
| p85 | 3 | max(5,3)=5 | max(8,5)=8 |
| p95 | 4 | max(10,4)=10 | max(8,10)=**10** |

### Step F — where it STILL goes wrong (proof, not assertion)

1. **Last column ≠ cycle-time lines.** CycleTime percentiles over ALL closed items {2,5,10}: p50=5, p70=5, p85=10, p95=10 (`TeamMetricsService.cs:307-324`). The **last column "Waiting"** = clamped {8,8,8,10}. p50 line=5 vs Waiting p50=8; p85 line=10 vs Waiting p85=8. **They don't line up** — and they *cannot*, because "Waiting" was visited by **only item A** (n=1), whereas the lines are over all 3 items. **Evidence:** Waiting population = {A} (Step C); cycle-time population = {A,B,C} (`:314`). Different populations → equality only by coincidence. This is exactly Chris's "last column must match the horizontal lines."

2. **The clamp is silently producing a value that is no item's percentile.** Waiting p95 was 8 (the only observation), clamped UP to 10 to not drop below In Review. So the rendered top boundary of the last column is **10** — which happens to equal the cycle-time p95 line — but for the *wrong reason* (clamp coincidence), while p50/p70/p85 of the same column are 8 ≠ 5. The column is internally incoherent: top edge agrees with the lines, lower edges don't. This is the visual "almost looks right but still wrong."

3. **`EarliestAtOrAfterExit` imputation injects synthetic observations the user never asked for.** It only fired here for items that `ReachedState` via a `ToState==S` with no matching `FromState==S` exit (none in this fixture), but it is *live*: for any item that entered S and then closed *from* S without a recorded `FromState==S` row, or reached S only as a `ToState`, the code synthesises an exit from a *later or equal-position* state's transition (`:163-181`). The user's model says: **population = items that LEFT S** — i.e. `FromState==S` only. Imputation **admits items that never left S** into S's population, perturbing its percentiles. **Evidence:** `:150` `exitMoment = lastExit ?? EarliestAtOrAfterExit(...)` and `:139` `ReachedState` gate uses `ToState==S` too.

4. **The "skipped color" is the small-sample face.** In Review n=2 → p50=p70=p85=5 (three equal percentiles). Adjacent equal values → zero-height bands → the FE drops them (`WorkItemAgingChart.tsx:143 if (height===0) return []`). With `carriedPercentiles` carry-forward (`:105-110`) a state with NO observations even *inherits* the previous column's bands, so a genuinely-absent column looks identical to its neighbour ("starting over" / "reset"). **Evidence:** `:108-110` overwrite `carriedPercentiles` only when `ownPercentiles` exists, else the prior column's percentiles are reused for geometry.

**Conclusion of the trace:** the shipped Option A reduced the *gross* drops (the clamp works) but did not change the fact that **each column is a different cohort** and the **last column is a sub-population of the closed items**, so the last-column-vs-lines invariant is still violated and the columns are internally inconsistent.

---

## 3. Five-Whys root-cause chains (multi-causal, evidence at each level)

PROBLEM: After Option A, pace-percentile bands still render "wrong" on Jira boards — last column doesn't line up with cycle-time lines; columns look internally inconsistent / "reset".

### Branch A — Last column can never equal the cycle-time lines (the PRIMARY root cause)

- **WHY 1A:** The rightmost band doesn't match the horizontal cycle-time percentile lines. [Evidence: Step F.1 — Waiting={A} p50=8 vs line p50=5; `WorkItemAgingChart.tsx:566-581` plots `percentileValues` (cycle-time) as the lines, `:608-612` plots `perStatePercentileValues` (per-state) as bands.]
- **WHY 2A:** The last column's population is "items that reached/left the last *configured doing* state," but completed items close from many different states, so that population is a **strict subset** of all closed items. [Evidence: Step C — only A visited Waiting; `CumulativeAgeObservationForItemAtState:139` gates on `ReachedState(lastState)`.]
- **WHY 3A:** The cycle-time lines are computed over a **different, larger** population — every closed item with `CycleTime>0` — by a different code path. [Evidence: `TeamMetricsService.cs:313-322` percentiles of `i.CycleTime` over `GetWorkItemsClosedInDateRange`; no coupling to the last doing state.]
- **WHY 4A:** D12 (and Option A's revision) *assumed* "the last Doing column's population ≈ the completed-items population" so the two would align. That assumption is false whenever items close from non-terminal doing states (the norm on real boards, universal on Jira). [Evidence: prior doc `bug-5145-design-escalation.md:31,47` explicitly bets on this approximation; fixture item B/C falsify it.]
- **WHY 5A → ROOT CAUSE A:** **The chart juxtaposes two metrics defined over two different populations and promises they coincide.** "Per-state cumulative-age percentiles for the last doing state" and "cycle-time percentiles over all closed items" are only equal when *every* closed item exits through the last doing state — which the data model neither guarantees nor enforces. The alignment promise is a **category error**, not a tuning bug.

### Branch B — Population/axis pollution: imputation + majority-coverage fallback + reached-vs-left

- **WHY 1B:** Columns are internally inconsistent (top edge agrees with lines, lower edges don't) and occasionally an unexpected/non-doing column appears or a column "resets." [Evidence: Step F.2/F.4; carry-forward `WorkItemAgingChart.tsx:105-110`.]
- **WHY 2B:** Each column's population is not "items that LEFT S." It is "items that **reached** S" (`ToState==S` counts) **plus** synthetic observations from `EarliestAtOrAfterExit`, and the *set of columns* can include non-doing observed states when config coverage ≤50%. [Evidence: `ReachedState:157-161` uses `||ToState==targetState`; imputation `:150,163-181`; `RestrictToMappedDoingStates:88-93` returns the FULL `doingStatesInWorkflowOrder` (which `BuildWorkflowStateOrder:38-44` padded with observed `FromState`s, incl. unmapped) when `observedCoveredByConfig*2 <= observedStates.Count`.]
- **WHY 3B:** These three "clever" mechanisms were added to *manufacture* monotonicity/coverage rather than to realise the user's population definition. Reached-set makes downstream a superset (helps the clamp); imputation fills missing exits so a reached item still scores; the majority fallback tries to keep columns when config looks incomplete. [Evidence: prior design `bug-5145-design-escalation.md:47` ("make each state's observation population cumulative — every item that reached at least that state"); code at `:139,150,88-93` implements exactly that.]
- **WHY 4B:** The Option-A redesign optimised for "bands rise and don't drop" (the *visual* symptom) instead of "each column = percentiles over the items that left that state" (the *spec*). It treated monotonicity as the requirement, not as a display guard on a correctly-defined metric. [Evidence: ADR-047 / `feature-delta.md` D12 framing; the running-max clamp `:183-208` plus cumulative population are both monotonicity machinery.]
- **WHY 5B → ROOT CAUSE B:** **The metric population was redefined to serve monotonicity (cumulative "reached" + imputation + coverage fallback) rather than to match the user's "items that left S, last exit, elapsed since StartedDate" definition.** The extra machinery changes the numbers away from the spec and re-introduces wrongness (synthetic/borrowed observations, surprise columns), which the clamp then partially masks.

### Branch C — The clamp masks rather than reveals (contributing, not root)

- **WHY 1C:** The build "looks better but still wrong" — gross drops gone, residual wrongness remains. [Evidence: Chris 2026-06-04; Step F.2 the clamp lifted Waiting p95 8→10.]
- **WHY 2C:** `ClampPercentilesNonDecreasing` forces a per-rank running max, so a column's rendered value may be **no item's** percentile. [Evidence: `:201-207` `Math.Max(percentile.Value, runningMax)`.]
- **WHY 3C:** The clamp runs over `percentilesByState` AFTER `Where(observationsByState.ContainsKey(state))` drops empty states (`ComputeAgeInStatePercentiles:60-65`), so a state with zero observations is **absent** from the clamp input → the FE carry-forward (`:105-110`) fills the visual gap with the previous column → "reset/skip" appearance survives the clamp. [Evidence: `:61` filter; FE `:108-110`.]
- **WHY 4C:** A presentation clamp was applied to a mis-defined metric. Clamping a wrong number yields a *monotonic* wrong number. The prior doc itself flagged this as "semantically dishonest" (its §4 interim mitigation) yet a backend variant shipped. [Evidence: `bug-5145-design-escalation.md:50`.]
- **WHY 5C → ROOT CAUSE C (shared with A/B):** **Clamping is correct as a final display guard but was made load-bearing for correctness.** It cannot fix a population/category error; it only hides its largest visual symptom, which is why the bug reopened.

### CROSS-VALIDATION
- **A + B + C consistent:** A (category error: last column is a sub-population of the lines) and B (population redefined for monotonicity, not spec) are independent contributors that both push the rendered values away from the spec; C explains why the prior fix *appeared* to help yet reopened. No contradiction: A is about *cross-metric* alignment, B about *intra-metric* population, C about *masking*.
- **All symptoms explained:** "drop on the right" → B (cohort-dependent populations) reduced but residually present, plus C carry-forward gaps; "skips a color" → B small-sample equal-percentiles (F.4); "last column mismatch" → A (category error). ✔
- **Backwards check:** If A holds, then for any board where some items close before the last doing state (B,C in fixture), last-column population ⊊ closed items ⇒ percentiles differ ⇒ lines don't match. Observed. ✔ If B holds, removing imputation/reached/fallback should change the numbers toward the spec. (Validated by recomputing the trace under the proposed model below.) ✔

---

## 4. Proposed fix — realise the user's model with MINIMUM machinery

### 4.1 Redefine the per-state observation (the core change), connector-agnostic

For state S, in `team.DoingStates` order, over completed items in window with `StartedDate.HasValue`:

```
observationsForState(S) =
    items
      .Where(i => i.SyncedTransitions.Any(t => t.FromState == S))      // LEFT S — no ToState, no imputation
      .Select(i => lastExit = Max(t.TransitionedAt where t.FromState == S))
      .Select(i => CumulativeAgeAtExit(i.StartedDate, lastExit))        // elapsed since StartedDate, last exit
```

- **Population = items that LEFT S** (`FromState==S`). This is literally the user's spec.
- **Last exit** already implemented at `:144-148`; **keep** it.
- `CumulativeAgeAtExit` (`:219-221`) already = elapsed-since-StartedDate at exit; **keep** it (but see 4.4 on the `+1`/`.Date` alignment).

### 4.2 REMOVE (the "clever" pieces that fight the spec)

| Piece | File:line | Action | Why |
|-------|-----------|--------|-----|
| `EarliestAtOrAfterExit` imputation | `BaseMetricsService.cs:150,163-181` | **REMOVE** | Injects synthetic observations for items that never left S. Spec is `FromState==S` only. (RC-B) |
| `ReachedState` `\|\| ToState==S` | `:157-161` | **REMOVE the `ToState` arm** (collapse: an item is in S's population iff it has a `FromState==S` exit) | Spec population is "left S," not "reached S." (RC-B) |
| `RestrictToMappedDoingStates` >50% majority fallback | `:77-94` | **REMOVE the fallback**; the pace axis is **exactly `team.DoingStates`**, full stop | Non-doing/unmapped observed states must never become columns. The user wants configured doing states in configured order, nothing else. (RC-B) |
| `BuildWorkflowStateOrder` padding **for the pace path** | `:31-47` used at `TeamMetricsService.cs:334` | **Stop using it for pace** — pass `team.DoingStates` directly as the ordered axis | Padding with observed `FromState`s is what admits unmapped columns. Keep `BuildWorkflowStateOrder` for any *other* caller, but the pace caller uses raw `team.DoingStates`. (RC-B) |

Result: `ComputeAgeInStatePercentiles` takes one ordered list (`team.DoingStates`), buckets `FromState==S` observations, computes percentiles, then clamps.

### 4.3 KEEP (genuinely correct)

- `ClampPercentilesNonDecreasing` (`:183-208`) — **keep as the final DISPLAY guard**, exactly the user's "clamp up, never drop below previous, at worst equal." But move it to run over **all `team.DoingStates` in order** (including states with zero observations carrying forward the running max), so the clamp — not the FE carry-forward — owns monotonicity. This closes RC-C's gap where empty states are dropped before the clamp (`:60-61`). Concretely: build a percentile row for every configured doing state (empty → inherit running max), so the FE never has to invent a column.
- `CumulativeAgeAtExit` elapsed-since-StartedDate — keep.
- Nearest-rank `PercentileCalculator` — keep (small-sample equal-percentile bands are honest; see 4.5).

### 4.4 Make the last column line up with the cycle-time lines — honestly

The category error (RC-A) means **the per-state metric for the configured-last doing state will generally NOT equal the all-closed-items cycle-time lines** — because items close from earlier states. Two honest options; recommend **Option 1**:

- **Option 1 (recommended) — define the implicit terminal column as "Done".** Add a virtual final observation for every closed item: `FromState == <lastDoingState> → Done` may not exist, but **every closed item has a `ClosedDate`**. Compute the terminal column as `CumulativeAgeAtExit(StartedDate, ClosedDate)` over **all** closed items — i.e. exactly `CycleTime` (modulo the `.Date`/`+1` convention, which already matches: `CumulativeAgeAtExit:221` = `WorkItemBase.GetDateDifference:93-95` = `(end.Date−start.Date).Days+1`). Render this as the rightmost column. **Now the last column IS the cycle-time distribution by construction**, so it lines up with the lines exactly. The per-doing-state columns remain the "left S" percentiles. This realises Chris's invariant truthfully without a coincidence.
  - Off-by-one check: `CycleTime` uses `StartedDate ?? CreatedDate` (`WorkItemBase.cs:59`) while pace uses `StartedDate` and filters `StartedDate.HasValue` (`:102`). For the terminal column, anchor on the **same** `StartedDate ?? CreatedDate` and the same `+1`/`.Date` truncation as `CycleTime` so the column equals the lines to the day. This is the only numeric-parity subtlety.
- **Option 2 — drop the alignment promise.** Keep only per-doing-state columns and state in the legend that the lines are whole-cycle-time references, not column tops. Honest but less satisfying; the user explicitly wants alignment, so prefer Option 1.

### 4.5 Small-sample "skipped color"

Accept it: when n is small and adjacent percentiles coincide, the zero-height band is correct (`WorkItemAgingChart.tsx:143`). With 4.3's "row per configured state + clamp owns monotonicity," remove the FE `carriedPercentiles` carry-forward (`:105-110`) so an absent column is rendered from its own (clamped, inherited-via-backend) row rather than silently borrowing the neighbour's geometry — eliminating the "reset" illusion. (Backend now always emits a row per doing state, so the FE no longer needs to invent one.)

### Recomputed trace under the proposed model (validation)
Per-doing-state ("left S"): In Progress {4,3,2}; In Review {10,5} (A,B only — both LEFT In Review); Waiting {8} (A only). Terminal "Done" column = CycleTime over {A,B,C} = {10,5,2} → p50=5,p70=5,p85=10,p95=10 = **exactly the lines**. The last column now matches by construction (RC-A resolved); per-state columns carry only real "left S" observations (RC-B resolved); clamp guards display over all columns incl. terminal (RC-C resolved).

---

## 5. Files affected + risk

| File | Change | Risk |
|------|--------|------|
| `Services/Implementation/BaseMetricsService.cs` | Remove imputation (`:150,163-181`), `ToState` arm (`:157-161`), majority fallback (`:77-94`); make `ComputeAgeInStatePercentiles` take `team.DoingStates` as the ordered axis; emit a row per configured state; add terminal "Done"=CycleTime column; keep+relocate clamp over all columns | **HIGH** — shared by Team+Portfolio pace; numbers change for every board. Mutation/tests must be rewritten. |
| `Services/Implementation/TeamMetricsService.cs:326-338` | Pass `team.DoingStates` (not `BuildWorkflowStateOrder`) to the pace compute; supply ClosedDate/CycleTime population for terminal column; bump/invalidate `AgeInStatePercentiles_{…}` cache key (`:330`) on deploy | MED — cache-key & EF: pace reads transitions only (no persisted-model change), but invalidate cache. |
| `Services/Implementation/PortfolioMetricsService.cs:~274` | Mirror the Team change | MED — must stay in lockstep. |
| `WorkItemStateTransitionMapper.cs` | **No change** — already mapped-space + drops `FromState==ToState` (`:17`). Stays connector-agnostic; no `switch`. | LOW |
| `Lighthouse.Frontend/.../WorkItemAgingChart.tsx` | Remove `carriedPercentiles` carry-forward (`:105-110`); render terminal column; lines unchanged | MED — `computePaceBandRects` + its FE tests. |
| `Lighthouse.Backend.Tests/API/Integration/AgeInStatePercentilesReadApiIntegrationTest.cs` | Replace synthetic monotonic fixture (`:102-130,226-257`) with the §2 rework/backward-flow + close-from-mid-state fixtures; assert last column == cycle-time lines and per-state == "left S" percentiles | MED — the masking test (prior RCA §"synthetic test masks the flaw") must go. |
| `WorkItemAgingChart.test.tsx` | Non-monotonic input, equal-adjacent-percentile, absent-column-no-carry cases | LOW |

### Regression surface / sibling-chart isolation
- **Sibling cumulative-state-time chart is SAFE.** It uses a **separate** order builder `BuildCumulativeWorkflowStateOrder(team)` = `[.. team.DoingStates]` (`TeamMetricsService.cs:434-437`) and its own `ComputeCumulativeStateTime` / `CompletedVisits` path (`BaseMetricsService.cs:224-373`). It does **not** call `BuildWorkflowStateOrder`, `RestrictToMappedDoingStates`, `CumulativeAgeObservationForItemAtState`, or the clamp. Removing the pace-only machinery does not touch it — **verify** no other caller of `BuildWorkflowStateOrder` exists before pruning (grep: only the pace path uses it for the axis).
- **EF / persisted model:** pace reads `SyncedTransitions` ([NotMapped], `:50-51`) — no migration. Only the `AgeInStatePercentiles_{…}` cache must invalidate on deploy.
- **Validation gates:** InMemory tests miss data-shape regressions — require a **live Jira walking-skeleton** check (per prior risk note) plus the new non-linear fixtures. SonarCloud new-violations gate and the ArchUnit "metrics read transitions only via `IWorkItemStateTransitionRepository`" rule must stay green.

**Overall risk: HIGH** (cross-board numeric change on two services), mitigated because the change *removes* code paths (imputation/fallback/padding) and the terminal-column equality is provable, not tuned.

---

## 6. Where this RCA contradicts the prior one
- Prior RCA bet that a **cumulative "reached-at-least"** population would make the last column ≈ completed items. This RCA shows that bet is the **category error (RC-A)**: "reached the last *doing* state" ≠ "closed," so it never aligns; alignment requires a **terminal Done = CycleTime** column (§4.4 Option 1), not a cumulative reach set.
- Prior RCA kept imputation + a coverage fallback as enabling machinery; this RCA identifies them as **RC-B** — active sources of wrongness — and removes them.
- Prior RCA treated the clamp as an acceptable monotonicity provider; this RCA keeps it strictly as a **display guard over all configured columns** and shows the prior placement (after empty-state filtering, `:60-61`) leaks the "reset" symptom into the FE (RC-C).
