# Wave Decisions — epic-6400-fast-with-many-features

DISCOVER decisions D1–D16 live in `feature-delta.md` (Wave: DISCOVER / [REF] Key decisions).

## DIVERGE (2026-10-23)

**Agent**: Flux (`nw-diverger`), subagent mode, autonomous · **Research depth**: lightweight (orchestrator config)
· **Work type**: brownfield feature · **Artifacts**: `diverge/job-analysis.md`, `diverge/competitive-research.md`,
`diverge/options-raw.md`, `diverge/taste-evaluation.md`, `recommendation.md`.

**Revision 1 (2026-10-23)**: after peer review round 1 (NEEDS_REVISION), the maintainer's answers to the eight
open questions, and one orchestrator decision.

### Decisions

- **[DV-1] Jobs.**
  - `job-operator-keep-lighthouse-usable-while-it-forecasts` is **reused**, wording unchanged. It is still
    under-served on a release with Epic 6311's limit (X5).
  - **New**: `job-reach-and-read-what-i-came-for-on-a-large-instance`. It merges DISCOVER's two candidates (the
    large Features list; the way to Settings).
  - Its statement carries the strategic clause: keep using Lighthouse instead of the tracker or a spreadsheet.
  - The physical function is solution-neutral.
  - Its independence from the stall is qualified: a random reading overlaps a round about 20–80% of the time.
  - Under-served outcomes, as desk estimates: OS1 16 (a page waiting during a forecast) and OS2 14 (the first
    Feature row).
  - Scale mapping: `jobs.yaml`'s 1–5 is this file's 1–10, halved and rounded.
  - Source: `diverge/job-analysis.md`.
- **[DV-2] Slice 1 is fixed** (Handoff waiver). It is **an internal enabler, not user value**. That corrects the
  waiver's wording "ships value on its own".
- **[DV-3] Matrix result.** Option 1, "Scope every read to what the screen shows", is first at **3.95**.
  - Then Option 3, "The forecast gets its own lane", 3.60; Option 2, "Shell first, then a client cache", 3.05,
    which wins the tie with Option 4 on DVF.
  - Then Precomputed read models 3.05; In-flight first plus a nudge 2.95; Forecast less, and less often 2.75.
  - Weights unchanged (DVF 30, Subtraction 15, Concept count 15, Staging 10, Speed-as-trust 30).
  - Revision 1 adds numeric T1–T3 rubrics, the Feasibility cap applied to Option 1 (F 4→3), T2 4 for Option 1,
    and a rule that an already-met condition counts as fully moved for every option.
  - Option 1 stays first under every single-change test except S2.
  - Source: `diverge/taste-evaluation.md`.
- **[DV-4] Direction: a stated hybrid.** Option 1's direction, plus Option 3's round-isolation step as slice 2: the
  forecast round runs off the request thread pool, on a dedicated worker with bounded parallelism, with no async
  conversion of the request paths.
  - The surer problem (the stalls, V1a) and the surer code change (the scoped reads, W2–W5) sit in different options.
  - The hybrid scores 3.85 unfiltered, and 3.95 under S7, the most likely case.
  - It is the best row that moves both conditions once K3 is an exit criterion (Option 4: 3.05).
  - Source: `recommendation.md`; `taste-evaluation.md` §6.
- **[DV-5] Orchestrator decision, taken under the maintainer's "continue autonomously" and flagged for the
  maintainer to confirm or overturn: K3 is an exit criterion of this Epic.** If it is overturned, Option 1 alone
  stands, and the isolation step returns to a trigger-gated slice.
- **[DV-6] Numeric triggers.** Ratios on the scaled data set:
  - Q_r is the round-attributable share of thread-pool queue time and heartbeat delay, with and without page
    traffic. "Dominates" means Q_r ≥ 0.6.
  - S_q is the request-read share of request wall time during a round. "Minor" means S_q ≤ 0.3.
  - Prior: 9 of 9 warnings inside rounds has a chance probability of p⁹ (0.002 at p = 0.5, 0.13 at p = 0.8), so
    rounds are involved. Q_r itself is an even prior.

  The triggers:
  - **Dissent A** (S2, Option 3 alone 4.40): Q_r ≥ 0.6, S_q ≤ 0.3, and idle already met. Slices 4–5 then become
    optional at G2.
  - **Dissent B**: K3 fails after slice 2 or at the end, and S_q > 0.3. Open the async-request-paths Epic. If S_q ≤
    0.3 instead, troubleshoot GC or the database first.
- **[DV-7] Provisional slice order** (Handoff waiver):
  1. Measurement (enabler).
  2. Forecast round off the request pool. This is K3, and the first user-visible value.
  3. Scaffold reads. K1 idle; the Settings half is conditional on A12.
  4. The Features list's instance-wide parts (projection, in-progress), with response shapes unchanged.
  5. The lean Features list, with loading, failure and empty looks. Depends on 4.
  6. A lighter forecast gather. It touches C12; acceptance: identical outputs and an identical Story 6354
     fingerprint on SQLite and PostgreSQL.
  7. The too-many-Features nudge at 100.

  What may move:
  - Slice 1 is fixed first, slice 2 right after it, and slice 7 last.
  - Slices 3, 4 and 6 swap freely.
  - Slice 5 must follow slice 4.
  - If the falsifier fires, slice 5 moves to just before slice 7.

  Every slice that changes a screen's fetch has its loading and failure look stated, and every other slice says
  "N/A, because …" (`recommendation.md` §3).
- **[DV-8] Falsifier and target are separate.**
  - The falsifier is the idle 920-Feature list under about 10 s. It fails the list claim.
  - The target is K2, the first row under 2 s.
  - Between the two, the claim stands and the target is unmet.
- **[DV-9] The overlap with Epic 6264** is recorded for 6264's DESIGN to pick up; 6264's files are not edited.
  - S05b stays 6264's.
  - This Epic adds only new, database-side reader methods for its hot callers. That covers the
    `PortfolioRepository.GetById` readers and the `FeatureRepository.GetByPredicate` callers it touches. 6264's ports
    are left unchanged.
  - 6400 lands first; S05c–e adopt the readers, with names fitted to S01's glossary.
  - The S05a ratchet only shrinks: no new `GetAllByPredicate` call outside the adapters, and no new controller
    dependency on a repository.
  - Slice 6 leaves S13's Feature → Forecast navigation and its cascade in place.
  - Source: `recommendation.md` §4.
- **[DV-10] Mechanisms stay DESIGN's**: how the projection is built, how the gather's save replaces histograms, the
  endpoint shapes, and the worker.
- **[DV-11] Candidate follow-up Epics**:
  - async request paths (Dissent B);
  - forecast less often;
  - client cache;
  - the tracker read (O10);
  - paging.

  Read models wait for 6264's data ownership.

### Maintainer answers to the DIVERGE questions (2026-10-23)

1. Nudge: threshold 100; a log line plus a non-blocking note; last slice. Yes.
2. Epic 6264 overlap: default accepted ("ok fair"). Sequencing as in DV-9.
3. Clients: keep it simple. New endpoints are additive; the clients stay on `FeatureDto` and the current Features
   endpoint.
4. Hide-completed stays client-side. Yes.
5. K1's Settings half, K3 and K4 accepted; figures set after the baseline.
6. Harness readings as CI budgets: not in this Epic.
7. Journey: seed `epic-6400-fast-with-many-features.yaml` and link the 6311 journey's error path.
8. Usage data, for DEVOPS: extend the existing "A Portfolio tab was opened" and "A Team tab was opened" events with
   a **closed size-bucket enum** of the Features count, for example `Under100` / `100To399` / `400Plus`; DEVOPS fixes
   the buckets. Never the raw count, never which Portfolio or Team.

### Still for DISCUSS

- Confirm DV-5 (K3 as an exit criterion) with the maintainer.
- The nudge copy.
- The journey's content.
- The slice briefs, carrying the loading and failure looks for DESIGN to pin.
- DISCOVER's G4 is **CONDITIONAL**, and G2 and G3 are **PENDING**, all under the Handoff waiver.

### Gate status

DIVERGE-G1 PASS · DIVERGE-G2 PASS (Nave not checked; Linear known from secondary sources) · DIVERGE-G3 PASS (all 15
pairs; two differ only partly on one count) · DIVERGE-G4 PASS. Peer review: round 1 NEEDS_REVISION; revision 1 is
applied and awaits re-review.

## DISCUSS (2026-10-23)

**Agent**: Luna (`nw-product-owner`), subagent mode, autonomous. Decisions D17–D31, the stories, KPIs, DoR and the
checklist live in `feature-delta.md` (Wave: DISCUSS sections); briefs in `slices/slice-01..06`; journey SSOT
`docs/product/journeys/epic-6400-fast-with-many-features.yaml`.

## Scope Assessment: PASS by decision — 6 Stories + 1 precursor Task, 4 modules, estimated ~10½ days

Two oversize signals fire (4 modules; outcomes that could ship apart). Kept as one Epic because every slice ships
alone and the K's share one harness (D31).

- Slice-composition gate: the measurement is Task 01a inside Story 01, not a slice (D17); slices renumbered 01–06
  (D18).
- K3 stays an exit criterion, flagged for the maintainer (D19).
- No existing response shape changes; no clients release (D20).
- DoR: PASSED, 9 of 9.

## DESIGN (2026-10-23)

**Agent**: Morgan (`nw-solution-architect`), PROPOSE mode, autonomous subagent. Decisions DDD-1..DDD-24, C4 L1/L2/L3,
the loading and failure looks and the architecture summary live in `feature-delta.md` (Wave: DESIGN sections); ADRs
ADR-257..ADR-262 in `docs/product/architecture/`; upstream changes UC-1..UC-5 in `design/upstream-changes.md`; the
brief's `## Application Architecture — epic-6400-…` section.

- Slice 01: a background round's own frames run on a dedicated `ForecastLane` (ADR-257): one hop with
  `StartNew(…, lane.Scheduler).Unwrap()`, no `ConfigureAwait(false)` in round code, the queue loop and driver
  continuations stay on the pool, the lane is disposed after the hosted services stop. Task 01a's harness lives in
  `Lighthouse.EndToEndTests/perf/`, builds the scaled set over the REST API, and adds controls C and C0 with fixed
  thresholds that classify the stalls as pool, GC or CPU; the D27 order follows the branch.
- Slices 02, 03, 05: readers that leave histograms out (ADR-258, ADR-261); licence gates share queries and refuse only
  the create actions while unknown, edits as today (ADR-259); the forecast write-back resolves against a fresh,
  no-tracking full read of the Portfolio's Features after the save, only with a write-back mapping (ADR-261).
- Slice 04: owner-keyed list endpoint with `FeatureDto`-named rows (ADR-260), 403 for an unreadable owner and filtered
  inside; it keeps every completion histogram and drops per-Team start histograms; looks reuse `DataGridBase` and
  `WidgetBody`.
- Slice 06: `PortfolioSize` and a `COUNT` endpoint (ADR-262); the note reads "as of its last update" and has a
  one-line failure look.
- No new package, no new external call, no migration unless a measured index is needed.
- Questions for the maintainer: Q-M1 (licence gates: refuse creation only while unknown), Q-M2 (a reference id stored
  twice: first in forecast order plus a warning), Q-M3 (slice 06's failure line and "as of" label), Q-M4 (K4's −30%
  computed-round figure, set at D27).
- Review: `nw-solution-architect-reviewer` iteration 1 returned NEEDS_REVISION (one blocker, six high); Revision 1
  applied the same day. The blocker (B1, which read the write-back resolves against) took the fresh full read after
  the save; the alternatives (hand over the gathered Features; keep the full tracked `GetById`) are rejected in
  ADR-261. One finding (narrow the list's completion read) was answered with evidence instead: `Feature.Forecast`
  aggregates every per-Team completion forecast, so no narrower row exists.

## DEVOPS (2026-10-23)

**Agent**: Apex (`nw-platform-architect`), subagent mode, autonomous; held after DEVOPS at the maintainer's request
(DISTILL not started). Configuration from project precedent (Story 6354, Epic 6311): existing self-hosted
distribution, GitHub Actions existing workflows, Serilog + opt-in PostHog, release-based, usage data only, trunk-based,
mutation per-feature. Artifacts: `feature-delta.md` (Wave: DEVOPS sections), `environments.yaml`,
`docs/product/kpi-contracts.yaml` (`OUT-6400-*`).

- **[DO-1] Usage data**: no new event; `featuresHeld` (`features_held`) on `TeamTabOpened` and `PortfolioTabOpened`,
  closed enum `UpTo100 = 0`, `From101To399 = 1`, `From400 = 2`, `Unknown = 3`. Boundaries follow the nudge (> 100) and
  the 100 / 400 / 920 points. Computed in the browser from the owner payload, sent at the existing 5 s dwell, `Unknown`
  only when the owner never arrived while the page was open. Server: refused on every other event, accepted absent on
  the two tab events (older bundles). Justified by K5 and the population claim, which the name alone cannot count.
- **[DO-2] KPI instruments**: K1/K2 Playwright marks and server split in the dev-only harness; K3 harness log scan +
  `dotnet-counters`, the lane probe as a hard CI precondition, and the reporter's next log as the production reading;
  K4 the Debug round split and Refresh History durations; K5 PostHog; G1–G4 hard CI gates.
- **[DO-3] Harness stays out of CI**: CI runs `playwright test` on `testDir ./tests` only; `perf/` gets its own script
  and is added to the E2E `tsconfig` include and the Biome path so it is linted locally; outputs gitignored.
- **[DO-4] CI**: no new workflow; Program.cs in slices 01 and 04 forces every live-connector suite; Postgres via
  `SharedPostgresContainer`; budget watch per slice; analyzer rules pre-listed (CA1001/S3881, CA2008, S8733, S107,
  NUnit1028/2045, Release-only ArchUnit edges); a source-scan test for `ConfigureAwait(false)` on the round path.
- **[DO-5] Mutation**: per slice, whole files on the backend, spans on the frontend, acceptance suite excluded;
  `TeamMetricsService` excluded (996 lines) unless DELIVER extracts the in-progress resolution.
- **[DO-6] Deployment**: calver release, no flag, no knob (the lane follows `MostRunsAtOnce`), no migration unless
  DDD-20's index; rollback = previous release; the banner's lane line is what operators see.
- **Flags against DESIGN**: F1–F8 (feature-delta). **Maintainer questions**: (1) accept the band absent from older
  bundles (default yes); (2) the three bands, no `From1000` (default as written).
