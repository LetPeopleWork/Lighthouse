# Taste Evaluation — epic-6400-fast-with-many-features (DIVERGE, Phase 4)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-23 · **Revision 1**, after peer review round 1. Inputs: the
curated 6 in `options-raw.md`. Scoring started only after `options-raw.md` was complete.

**What changed in revision 1**:
- Numeric rubrics for T1–T3, and every option re-scored against them.
- The Feasibility rule now applies to Option 1 as well, so its F drops from 4 to 3.
- A rule for a condition that is already met.
- S2 restated, and S7 added: the most likely case.
- "Dominate" and "minor" defined as ratios.
- The prior that the in-round heartbeat readings give.
- A scored hybrid row, and the orchestrator's exit-criterion filter (§6).

**On locking.** In the original pass, §1 (weights and rules) was written before §3 (scores), in the same session.
Nothing was committed between the two, so the order cannot be proven from history; it is stated here.

The weights did not change in revision 1. The new rubric levels in §1 were written before re-scoring.

## 1. Weights and rules (locked before scoring)

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default. The evidence is thin (one instance; G2 and G3 pending), so cost and the reality of the problem must not be swamped by taste. |
| T1 Subtraction | 15% | Below the default 20%. Counts the **moving parts added to the code and the product**. |
| T2 Concept count | 15% | Below the default 20%. Counts new concepts for the **user and the maintainer** together. |
| T3 Staging | 10% | Below the default 15%. Few options add UI, so this is read as staging. |
| T4 Speed-as-trust | 30% | Above the developer-tool 25%, because speed *is* this Epic's job (`job-analysis.md` OS1–OS3). |

**T1 Subtraction: moving parts added**

| Score | Meaning |
|---|---|
| 5 | Removes or narrows only; nothing new persists. |
| 4 | One new part, with no ongoing upkeep. |
| 3 | Two or three new parts, with no ongoing upkeep. A temporary duplicate kept until the clients move counts as a part, not as upkeep. |
| 2 | A part that needs ongoing upkeep (invalidation rules, a second change check kept in step with the fingerprint), or four or more parts. |
| 1 | A new subsystem with its own lifecycle (backfill, rebuild, repair). |

**T2 Concept count: user plus maintainer**

| Score | Meaning |
|---|---|
| 5 | None. |
| 4 | One, anchored in something that already exists. |
| 3 | Two, or one with no anchor. |
| 2 | Three or more. |
| 1 | A new mental model. |

**T3 Staging: the first slice after the measurement**

| Score | Meaning |
|---|---|
| 5 | Delivers user-visible value alone, needs no later slice to be correct, and defers all complexity. |
| 4 | Delivers value alone; a later slice depends on it but is small. |
| 3 | Delivers value only if a condition that slice 1 measures holds. |
| 2 | Value only once two or more slices land together. |
| 1 | Nothing until the whole option lands. |

**T4 Speed-as-trust: two symptom conditions.** **Idle** means K1 and K2 with no forecast running; **during a
forecast** means K1, K2 and K3.

| Score | Meaning |
|---|---|
| 5 | Moves both conditions fully. |
| 4 | Moves one fully and the other partially. |
| 3 | Moves one fully and the other not at all. |
| 2 | Moves part of one, or only under a data-shape condition. |
| 1 | Moves neither. |

**Rules for T4**:
- T4 scores an option **as if its key assumption holds**.
- **An already-met condition**, meaning slice 1 shows a condition is within its target or its symptom is
  falsified, counts as **fully moved for every option**. The rule is applied the same way to all options in every
  sensitivity test.

**Feasibility rule**:
- An option scores **at most 4** on Feasibility unless its mechanism already has a precedent in this code base.
- An option that needs a change across many callers scores **at most 3**. Examples: async conversion; new reader
  methods for several repository callers with C1 RBAC readability re-proved per endpoint; new tables in both
  providers.

## 2. DVF filter

| # | Option | D | F | V | Total | Avg | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Scope every read to what the screen shows | 5 | 3 | 5 | 13 | 4.33 | D: W2–W5, the instance-wide reads behind both the list and the scaffold; X4. F: projections have a precedent (`FeaturePositionMap`), but several callers of `GetById` and `GetByPredicate` change, and RBAC readability must be re-proved per endpoint, so 3 under the rule (revision 1; was 4). V: helps every instance; aligned with Epic 6264's ports. |
| 2 | Shell first, then a client cache | 3 | 5 | 4 | 12 | 4.00 | D: the reporter's stated mechanism for the scaffold, but neither the first list visit nor the stalls. F: TanStack Query is installed and in use. V: fewer requests everywhere; invalidation to maintain. |
| 3 | The forecast gets its own lane | 5 | 3 | 4 | 12 | 4.00 | D: the only symptom observed on the current release (V1a). F: in its full form (async request paths), many callers, so 3. The worker step alone is 4 (S3). V: protects against any future heavy background work. |
| 4 | Precomputed read models kept current by domain events | 4 | 2 | 3 | 9 | 3.00 | F: new tables in both providers, handlers, backfill, drift. V: decides data ownership ahead of Epic 6264's S10. |
| 5 | Forecast less, and less often | 4 | 3 | 4 | 11 | 3.67 | D: rounds of 264–483 s, several an hour; A14 unvalidated. F: the check must never miss a change (C12); coalescing touches queue order. |
| 6 | In-flight first, and a nudge on size | 2 | 5 | 3 | 10 | 3.33 | D: the maintainer decided the warning is a nudge, not the fix (V7); the gain depends on an unknown done ratio. |

The elimination rule (DVF total below 6) eliminates **none**.

## 3. Taste scoring (against the §1 rubrics)

| # | T1 | T2 | T3 | T4 | Justification |
|---|---|---|---|---|---|
| 1 | 3 | 4 | 4 | 4 | T1: three parts (lean endpoints, kept beside the old shape until the clients move; the dependency projection; database-side reader methods). T2: one, a lean read beside the full shape, anchored in `FeaturePositionMap` (advisory A2; was 5). T3: the scaffold slice delivers value alone, and the lean list depends on the small instance-wide slice. T4: idle fully, during a forecast partially. |
| 2 | 2 | 3 | 5 | 2 | T1: cache invalidation is ongoing upkeep. T2: a stale-but-updating marker for users (anchored in `WidgetShell`'s dimmed state) plus query keys for maintainers. T3: deduplicating the shared queries ships alone and is visible at once. T4: part of idle (K1, return visits only). |
| 3 | 4 | 4 | 3 | 3 | T1: one part, the lane. T2: one, anchored in the update queue's lanes. T3: pays off only if the round's own threads drive the stalls (a measured condition). T4: during a forecast fully, idle not at all. |
| 4 | 1 | 2 | 2 | 5 | T1: a subsystem with backfill, rebuild and repair. T2: lag, eventual consistency, projection ownership. T3: a projection, its handlers and its backfill must all land. T4: both conditions. |
| 5 | 2 | 3 | 3 | 2 | T1: a second change check kept in step with the fingerprint (upkeep) plus a coalescing window. T2: two maintainer concepts. T3: pays off only if most rounds are unchanged (A14). T4: part of during a forecast. |
| 6 | 4 | 3 | 3 | 2 | T1: the filter narrows; the note is one part. T2: "N done hidden" plus a size warning. T3: depends on the done ratio. T4: part of idle K2, under a data-shape condition. |

## 4. Weighted matrix

Final = 0.30·DVF + 0.15·T1 + 0.15·T2 + 0.10·T3 + 0.30·T4

| Rank | Option | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|
| 1 | **1 Scope every read to what the screen shows** | 4.33 | 3 | 4 | 4 | 4 | **3.95** |
| 2 | 3 The forecast gets its own lane | 4.00 | 4 | 4 | 3 | 3 | **3.60** |
| 3= | 2 Shell first, then a client cache | 4.00 | 2 | 3 | 5 | 2 | **3.05** |
| 3= | 4 Precomputed read models | 3.00 | 1 | 2 | 2 | 5 | 3.05 |
| 5 | 6 In-flight first, and a nudge on size | 3.33 | 4 | 3 | 3 | 2 | 2.95 |
| 6 | 5 Forecast less, and less often | 3.67 | 2 | 3 | 3 | 2 | 2.75 |

Arithmetic for #1: 0.30·4.33 + 0.15·3 + 0.15·4 + 0.10·4 + 0.30·4 = 1.30 + 0.45 + 0.60 + 0.40 + 1.20 = 3.95.

**Tie for third**: Options 2 and 4 both total 3.05. The tie is broken by the higher DVF (2: 4.00 against 4: 3.00).
DVF is the filter lens, so the rule adds nothing new, but it was stated after the tie appeared, and it is
labelled that way.

**Rank holds**:
- Option 1 stays first after the H8 Feasibility change and the A2 concept change.
- The reviewer's estimate of about 4.10–4.25 left T1 at 4. The T1 rubric puts it at 3, which gives 3.95.

## 5. Sensitivity (one change at a time; the already-met rule applied to every option)

**Definitions.** Forecast timings are used by ratio only (DISCOVER calibration), never as absolute seconds.

- **Q_r, the round-attributable share.** Run a forecast round on the scaled data set with no page traffic, and an
  identical round (same data, same seed) with the scripted page traffic. Q_r is the thread-pool
  queue-length-seconds of the first divided by those of the second; heartbeat-delay seconds are taken the same way.
  **"The round dominates the stalls" means Q_r ≥ 0.6.**
- **S_q, the request-read share.** During a round with traffic, S_q is the time requests spend in their own
  database reads divided by total request wall time. **"Request reads are minor" means S_q ≤ 0.3.**
  - Between 0.3 and 0.5, S_q counts as **not minor**. That is the cautious side, because the reads are fixed by this
    Epic anyway.

**Prior from the log.** All 9 of the 9 heartbeat warnings fall inside forecast rounds. Suppose the warnings were
unrelated to rounds, and rounds filled a share p of the time. Then all nine landing inside rounds by chance has
probability p⁹:
- 0.002 at p = 0.5;
- 0.13 at p = 0.8, the top of the 20–80% occupancy range (OS2).

So rounds are involved with high confidence. The log cannot say whether the round's own threads or the requests'
reads during the round do the stalling. The prior for Q_r ≥ 0.6 is therefore even, about 50%.

| Test | Change | Result | Winner |
|---|---|---|---|
| S1 | The skill's default weights (30/20/20/15/15) | 1: 3.90 · 3: 3.70 · 2: 3.25 · 6: 3.15 · 5: 2.85 · 4: 2.55 | 1; top 3 = 1, 3, 2 |
| S2 | **Q_r ≥ 0.6 and S_q ≤ 0.3, and the idle condition is already met** (idle list under ~10 s, and Overview → Settings within K1 idle). 1: D 5→4; T4 stays 4 (idle met counts fully; during a forecast partial). 3: F 3→4 (cause known); T3 3→4; T4 3→5 (idle met counts fully; during a forecast fully). | 1: **3.85** · 3: **4.40** | **3** |
| S3 | Option 3 reduced to the worker step (no async conversion): F 3→4 | 3: 3.70 | 1 |
| S4 | Option 2's dedup counted as fully moving idle K1: T4 2→3 | 2: 3.35 | 1 |
| S5 | Read-model drift solved for free: Option 4 F 2→3 | 4: 3.15 | 1 |
| S6 | Most Features of a large Portfolio are done: Option 6 D 2→3, T4 2→3 | 6: 3.35 | 1 |
| S7 | **The most likely case: Q_r ≥ 0.6, and the idle list stays slow (~50 s).** 1: unchanged (T4 4). 3: F 3→4, T3 3→4; T4 stays 3 (during a forecast fully, idle not at all). | 1: **3.95** · 3: **3.80** | **1, by 0.15** |

**What S7 says**: in the most likely case, neither option alone covers both conditions:
- Option 1 leaves the round's own threads, and so K3, partly unmet.
- Option 3 leaves the idle list untouched.

That is the case for a combination (§6).

## 6. The hybrid and the exit-criterion filter

**Orchestrator decision, 2026-10-23.** It was taken under the maintainer's "continue autonomously", and the
maintainer may overturn it: **K3 is an exit criterion of this Epic.** Under that decision, an option is acceptable
only if, by its own assumption, it moves the during-a-forecast condition **fully** (T4 ≥ 3 on that condition) and
does not give up idle.

| Row | DVF | T1 | T2 | T3 | T4 | Weighted | Notes |
|---|---|---|---|---|---|---|---|
| **H: Option 1 + Option 3's round-isolation step** (the worker only, no async request paths) | 4.33 (D 5, F 3, V 5) | 2 (four parts) | 3 (two: the lean read, the lane) | 3 (the first slice after measurement is the isolation step, which pays off only if Q_r is high) | 5 | **3.85** | Under S7: T3 → 4, giving **3.95** (ties Option 1) |
| 1 alone | 4.33 | 3 | 4 | 4 | 4 | 3.95 | during a forecast only partially, so it **fails the K3 filter** |
| 3 alone | 4.00 | 4 | 4 | 3 | 3 | 3.60 | idle not moved; the idle list claim stands until falsified, so it fails the "does not give up idle" part |
| 4 alone | 3.00 | 1 | 2 | 2 | 5 | 3.05 | passes the filter |

The arithmetic for H: 1.30 + 0.30 + 0.45 + 0.30 + 1.50 = 3.85.

**Result**:
- The hybrid does **not** win the unfiltered matrix: Option 1 alone is 0.10 higher.
- It **wins once K3 is an exit criterion**: 3.85 against Option 4's 3.05, the only other row that moves both
  conditions.
- The surer problem (the stalls, V1a) and the surer code change (the scoped reads, W2–W5) belong to different
  options. The hybrid takes the cheapest part of each.

## 7. Gate G4

- **PASS**: DVF applied; no option eliminated.
- **PASS**: weights locked before scoring. Revision 1 adds rubric levels, not weights; see the note on locking at the
  top.
- **PASS**: all six options, and the hybrid, scored on all criteria.
- **PASS**: the recommendation traces to §4 (direction) and §6 (hybrid under the exit criterion). The dissent comes
  from §5 S2, with Q_r and S_q as its numeric trigger.
