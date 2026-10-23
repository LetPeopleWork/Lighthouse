# Recommendation — epic-6400-fast-with-many-features (DIVERGE → DISCUSS)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-23 · **Revision 1**, after peer review round 1 and the
maintainer's answers of 2026-10-23. Derived from `diverge/taste-evaluation.md` §4–§6.

**Taken as given**:
- DISCOVER's decisions D1–D16 and the Handoff waiver.
- Slice 1, the dev-only scaled data set and the before/after harness, is fixed and is not one of the options.

**Status carried into DISCUSS**:
- DISCOVER's G4 Viability is **CONDITIONAL** until G2 and G3 pass.
- G2 and G3 stay **PENDING** under the Handoff waiver.

The direction below is a direction, not a go on a measured case.

## Decision for DISCUSS

> **Proceed with a stated hybrid: Option 1's direction, "Scope every read to what the screen shows" (3.95 / 5, first
> in the matrix), plus Option 3's round-isolation step as slice 2.** In that step, the forecast round runs off the
> request thread pool, on a dedicated worker or long-running task with bounded parallelism. The async conversion of
> the request paths is **not** part of it.
>
> **Why a hybrid.** The surer *problem* and the surer *code change* sit in different options:
> - **The surer problem is the stall** (V1a). All nine heartbeat warnings fall inside forecast rounds, on a release
>   that already caps the simulation at half the cores. Option 3 targets it.
> - **The surer code change is the scoped reads.** The reads that scale with every Feature of the instance are
>   certain in the code (W2–W5). Option 1 targets them.
>
> Neither option alone covers both conditions in the most likely case (`taste-evaluation.md` §5 S7: 3.95 against
> 3.80). With **K3 an exit criterion** (orchestrator decision below), the hybrid is the best-scoring row that moves
> both conditions: 3.85, against Option 4's 3.05 (§6).
>
> **What changes for the user**:
> - The forecast round stops starving requests.
> - Each request reads only what its answer shows:
>   - Portfolio and Team lists read ids, names and counts.
>   - Reading one Portfolio reads one.
>   - The licence check counts once per page (a dedup borrowed from Option 2).
>   - Dependency verdicts come from a light projection of the whole graph (C2).
>   - "In progress" comes back as ids from one query.
>   - The Features list gets a lean endpoint keyed by Portfolio or Team id, with honest loading, failure and empty
>     looks.
>   - The forecast gather stops loading what it does not use.
>
> **What does not change**: new endpoints are additive, and the clients stay on `FeatureDto` and the current Features
> endpoint (C6, maintainer). Forecast results do not change (C12).

**Orchestrator decision (2026-10-23)**: **K3 is an exit criterion of this Epic.** It was taken under the
maintainer's "continue autonomously" and is flagged for the maintainer to confirm or overturn. If it is overturned,
the matrix's unfiltered winner, Option 1 alone (3.95), stands, and the isolation step returns to being the dissent's
trigger-gated slice.

**What this direction is, and is not**:
- It does **not** implement the reporter's stated mechanism. A cached, staggered shell is Option 2's mechanism.
- It removes the reason for it: the scaffold is slow because it reads data it does not show (X4).

**Go/no-go falsifier and target are separate** (H12):
- **Falsifier** for the list claim: the idle 920-Feature list on the scaled data set loads in under about **10 s**.
  If it does, the 50 s was environmental, and the list half of the job fails G1.
- **Target** K2: first Feature row under **2 s**.
- Between 2 s and 10 s, the claim stands and the target is unmet.

## 1. Top 3 (from the unfiltered matrix) and the hybrid

### Option 1 — Scope every read to what the screen shows — 3.95

- **Why it scores well**:
  - Highest DVF (4.33).
  - A precedent in the code (`FeaturePositionMap`).
  - One new concept, anchored in that precedent.
  - The first slice of it stands alone.
- **Core trade-off**:
  - Three new parts, each lean endpoint kept beside the old shape until the clients move.
  - Moves the during-a-forecast condition only in part.
- **Key risk**: A11. If the reads are a small share of the time, the screens get faster but still stall.
- **Who it is for**: the delivery lead who opens a 920-Feature Portfolio several times a day.

### Option 3 — The forecast gets its own lane — 3.60

- **Why it scores well**: it aims at the only symptom observed on the current release, and it protects requests from
  any background work.
- **Core trade-off**: it does nothing idle. In its full form, the async conversion of the request paths touches many
  callers (F 3), as well as the signatures Epic 6264's ports are about to change.
- **Key risk**: the stall comes from GC or the database rather than pool starvation.
- **Who it is for**: the operator who pressed Cancel all.

### Option 2 — Shell first, then a client cache — 3.05 (wins the tie with Option 4 on DVF, 4.00 against 3.00)

- **Why it scores well**: it is frontend only, its first step is visible at once, and it is the reporter's own
  picture.
- **Core trade-off**:
  - The first visit to a large list still pays the full server read.
  - The stalls are untouched.
  - Invalidation is ongoing upkeep.
- **Key risk**: a cached value that reads as current.
- **Who it is for**: a repeat visitor on a slow connection.

### The hybrid (H) — 3.85 unfiltered; first under the K3 exit criterion; 3.95 under S7

- **Why**: it moves both conditions by its assumptions (T4 5). It takes the cheapest piece of Option 3, the worker
  step (S3 grades it F 4 alone), and leaves Option 3's costly part, the async request paths, as a follow-up.
- **Core trade-off**: four new parts (T1 2) and two concepts (T2 3). Slice 2 pays off only if the round's own threads
  matter (T3 3). The log's 9/9 prior makes "rounds are involved" near-certain, but the split between the round's
  threads and the requests' reads is even (§5 of the evaluation).
- **Key risk**: Q_r turns out low. Then slice 2 helps little, and the reads (slices 3–6) carry K3.

## 2. Dissenting case and the numeric triggers

**Definitions** (`taste-evaluation.md` §5). These are ratios on the scaled data set, never absolute seconds:
- **Q_r**: the round-attributable share of thread-pool queue time and heartbeat delay, measured with and without page
  traffic.
- **S_q**: the request-read share of request wall time during a round.

**Dissent A, Option 3 alone (S2: 4.40 against 3.85)**:
- **Condition**: slice 1 finds Q_r ≥ 0.6 and S_q ≤ 0.3, **and** the idle condition is already met (idle list under
  about 10 s, and Overview → Settings within K1 idle).
- **Meaning**: the Epic's reads were not the problem.
- **Action**: keep slice 2. Slices 4–5 (the Features list) become optional and are re-scored at G2. Slice 3 stays
  only if K1 is unmet.

**Dissent B, Option 3's full form (async request paths) as a follow-up Epic**:
- **Condition**: after slice 2, and again at the Epic's end, K3 still fails **and** S_q > 0.3. That means requests
  are stalling in their own synchronous reads.
- **Action**: open the async-request-paths Epic.
- **The other case**: if K3 fails and S_q ≤ 0.3, the cause lies elsewhere (GC, the database). Troubleshoot that
  first; do not open the async Epic on a guess.

## 3. Provisional slice order

Per the Handoff waiver, the order rests on code evidence and is re-ordered after the baseline.

| # | Slice | Evidence | Moves | Loading look / failure look (CLAUDE.md, no silent N/A) |
|---|---|---|---|---|
| 1 | **Measurement**: dev-only scaled data set, before/after harness, readings (a) and (b), the forecast-round split with thread-pool and GC counters, and Q_r and S_q | Handoff waiver, P1 | — | N/A, because it is dev-only and changes no screen. **It is an internal enabler, not user value** (H1). |
| 2 | **The forecast round off the request pool**: a dedicated worker or long-running task with bounded parallelism; gather, simulate and save run there | V1a, W8; orchestrator decision | **K3** (exit criterion); K1 and K2 during a forecast | N/A, because no screen's fetch or response changes; only where the round runs. **First user-visible value.** |
| 3 | **Scaffold reads**: Portfolio and Team lists read ids, names and counts; one shared licence-count answer for every `useLicenseRestrictions()`; reading one Portfolio filters in the database | W2, W3, W3b | K1 idle. **The Settings half of K1 is conditional**: it rests on A12, which is unproven today and may be the stall itself. It is credited only if slice 1's server/client split, or a no-forecast reading, shows the server's lists hold Settings back. | **Loading**: the Overview keeps its body spinner while its lists are on the way (W1); no previous answer is shown. **Failure**: DESIGN checks what the Overview and each `useLicenseRestrictions()` consumer show today when the call fails. A failed list must read as "could not load", not as "no Portfolios" (`WidgetShell`'s message). A failed count must not read as "under the licence limit"; DESIGN picks between disabling the gated action with a reason and retrying. |
| 4 | **The Features list's instance-wide parts**: dependency verdicts from a light projection of the whole graph (C2); "in progress" answered from one query | W4, W5 | K2; K3 in part | N/A, because the screen's requests and response shapes are unchanged in this slice; only the server's work behind them changes. |
| 5 | **Lean Features list**: an endpoint keyed by Portfolio or Team id, only the shown columns, parents folded in, "in progress" as ids. **Depends on slice 4.** | W6, W6b, W6c, W7, C5 | K2 | **Loading**: the grid's loading placeholders until the first answer. After a completed update, the rows stay, dimmed under a small spinner, and a late answer for an older request never lands (keyed query). **Failure**: a could-not-load message where the grid was. **Empty**: a "no Features" message in the configured term. Today all three look like loading forever (W7). Sketched at the start of DISTILL. |
| 6 | **Lighter forecast gather**: the gather stops loading what the simulation and fingerprint do not use, so a round Story 6354 skips is cheap too. **It touches C12.** | W8b, X7 | K4; K3 in part | N/A, because no screen's fetch changes. **Acceptance**: on both SQLite and PostgreSQL, the forecast outputs (percentiles and histogram rows) and Story 6354's input fingerprint are identical before and after, for the same data and seed. |
| — | **G2 re-score**: after slice 1 (Q_r, S_q, falsifier); **K3 checked** after slice 2 and at the Epic's end | Handoff waiver | — | — |
| 7 | **Too-many-Features nudge**: a log line and a non-blocking note at 100 Features | O7, A10, V7; maintainer | — (a nudge) | **Loading**: no note until the Portfolio's count is known. **Failure**: no note; a missing nudge cannot be misread as data. Sketched at the start of DISTILL. |

**What can swap (H4)**:
- **Fixed**: slice 1 first; slice 2 right after it (orchestrator decision); slice 7 last (maintainer).
- **Swap freely** among themselves after the baseline: **3, 4, 6**.
- **Slice 5 must follow slice 4**, because it consumes the projection and the ids.
- If the falsifier fires, slice 5 moves to just before slice 7.

**Slice 1 is not user value** (H1, a correction to DISCOVER's waiver wording "ships value on its own"). It is the
Epic's internal enabler. The Epic still ships on its own, because slice 2 follows it.

## 4. Overlap with Epic 6264 (an item for 6264's DESIGN to pick up; 6264's files are not edited)

Epic 6264 is at DISCUSS-done. Its S05a–S05e and S13 touch the same code.

- **S05b stays owned by 6264.** That is the `Func` predicate verification and its fix.
- **This Epic only adds new, database-side reader methods** for its hot callers: the `PortfolioRepository.GetById`
  readers, and the `FeatureRepository.GetByPredicate` callers it touches (the Features list, "in progress", the
  forecast gather). It does not change or remove 6264's planned ports, or the generic repository methods.
- **Sequencing**: 6400's slices land first. 6264's S05c–S05e then adopt the readers. Their names are chosen to fit
  6264's S01 glossary once it exists; until then, the names state intent and avoid `IQueryable`, `Expression` and
  `Func` in signatures, as S05c–e require.
- **The S05a ratchet only shrinks.** This Epic must not add a `GetAllByPredicate` call outside the persistence
  adapters, and must not add a controller dependency on a repository. `FeaturesController`, `TeamMetricsService` and
  `PortfolioMetricsService` are on S05a's baseline, and every change here shrinks or keeps their entries, never adds
  one. New reads reach controllers through an application service or query port.
- **S13** makes Forecast its own aggregate, and it is 6264's measured performance payoff. Slice 6 overlaps with it.
  It reads less through a persistence-adapter method, and leaves the Feature → Forecast navigation and its cascade in
  place, so S13's aggregate decision stays open.

## 5. Follow-up Epics (candidates, not this Epic)

| Candidate | Opened when | Source |
|---|---|---|
| Async request paths (Option 3 in full) | Dissent B: K3 fails after slice 2 or at the end, and S_q > 0.3 | dissent |
| Forecast less often: a change check before the gather, and coalesced rounds (Option 5) | K4 still matters after slice 6 and Story 6354 are released | Option 5 |
| Client cache and stale-while-revalidate (Option 2 beyond the licence dedup) | return-visit time still matters after slices 3–5 | Option 2 |
| Faster large reads from the tracker (O10) | already a DISCOVER candidate | D12 |
| Paging | maintainer | D15 |

Read models (Option 4) are not recommended until Epic 6264's data-ownership Story has decided which context owns
them.

## 6. Maintainer answers (2026-10-23), and what DISCUSS still owns

| # | Question | Answer |
|---|---|---|
| 1 | Nudge | Threshold 100; a log line plus a non-blocking note; last slice. **Copy is DISCUSS's.** |
| 2 | Epic 6264 overlap | Default accepted ("ok fair"). Sequencing and rules as in §4. |
| 3 | Clients | Keep it simple: new endpoints are additive, and the clients stay on `FeatureDto` and the current Features endpoint. |
| 4 | Hide-completed | Stays client-side. |
| 5 | K1's Settings half, K3, K4 | Accepted; figures set after the baseline. K1's Settings half is credited only as in slice 3. |
| 6 | CI budgets | Not in this Epic. |
| 7 | Journey | Seed `epic-6400-fast-with-many-features.yaml` and link the 6311 journey's error path. |
| 8 | Usage data (for DEVOPS) | Extend the existing **"A Portfolio tab was opened"** and **"A Team tab was opened"** events (`docs/settings/usagedata.md`) with a **closed size-bucket enum** of the Features count, for example `Under100`, `100To399`, `400Plus`; DEVOPS fixes the buckets. Never the raw count, and never which Portfolio or Team. |

**Still for DISCUSS**:
- the nudge copy;
- whether the orchestrator's K3 exit-criterion decision stands (flag it to the maintainer);
- the journey's content;
- the slice briefs, including the loading and failure looks above for DESIGN to pin.
