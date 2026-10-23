# Options (raw) — epic-6400-fast-with-many-features (DIVERGE, Phase 3)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-23. This file only generates options. It contains no scoring
and no ranking; the evaluation is in `taste-evaluation.md`.

**Fixed frame, shared by every option**:
- **Slice 1 is a given, not an option** (Handoff waiver): the dev-only scaled data set (P1) and the before/after
  harness. It records readings (a) and (b) and the forecast-round split. Every option below starts *after* it, and
  each later slice proves its gain with it.
- **Constraints C1–C12 hold in every option**:
  - RBAC readability stays, and dependency verdicts are still computed over the whole graph.
  - The global position stays.
  - Endpoints are additive while Lighthouse-Clients reads `FeatureDto`.
  - Migrations are expand-only.
  - Forecast results do not change.
  - The configured terms are used.
  - Every changed view gets a loading look and a failure look.
- **Out of every option**: paging (a later Epic, Q11) and the tracker read (O10, a candidate separate Epic).
- **Per option, the costs are rough sizes**: S ≈ 1–2 days, M ≈ 3–5 days, L ≈ 1–2 weeks of DELIVER, at the code
  reading of `ead31f93f`.

## 1. HMW question

> How might we keep every screen of a large instance as quick to reach and read as on a small one, also while
> Lighthouse recalculates in the background?

It names no mechanism. The answer can be less work per screen, less background work, separating the two, or less
data in scope.

## 2. SCAMPER options

### S — Substitute (the read path): precomputed read models kept current by domain events

**Core idea**: the Overview, the Team and Portfolio lists and the Features list each read one flat, prepared table
(a Feature-list row, a Portfolio summary, the dependency edges). They no longer assemble those answers from the
domain graph on every request.
**Key mechanism**: new read tables (expand-only migrations, both providers). They are written by
`IDomainEventHandler`s on the events that already exist or are added (Features updated, forecast computed, Feature
moved, Portfolio or Team changed). There is a rebuild command for backfill and repair. List endpoints read the
tables with no includes.
**Key assumption**: reads far outnumber writes, and a list that lags a forecast by a moment is acceptable when it
says when it was computed. The events cover every change that the rows show.
**Closest competitor**: CQRS read models; recording rules (`competitive-research.md` §2.1).
**First after slice 1**: the Feature-list row table with its handler and rebuild, behind the Portfolio Features tab.
**Decomposition**: one projection per Epic-sized step: (i) Feature list, (ii) Portfolio and Team summaries for the
Overview, (iii) dependency edges, (iv) the forecast gather reads from (i). Each ships alone once its backfill
exists.
**Targets moved**: K1, K2, idle and during a forecast (reads are flat). K3 for reads. K4 if the gather reads the
projection.
**Risk**:
- Projection drift, where an event is missed and a row goes stale with no sign.
- The tables land before Epic 6264's data-ownership Story (S10) has decided which context owns them.
- Every migration is needed twice, once per provider.
**Rough cost**: L per projection; 3–4 projections.

### C — Combine (with Epic 6264's port Stories): ride the restructure

**Core idea**: the performance fixes are delivered as Epic 6264's Stories: S05b (verify `Func` predicate
filtering), S05d (Portfolio ports) and S12a (metrics query ports). They are re-ordered to come first and given
K1–K2 acceptance criteria.
**Key mechanism**: the intention-revealing reader methods that 6264 plans (`IPortfolioReader.…`) are written as
database-side queries that return exactly what each caller uses.
**Key assumption**: 6264's order and scope can move. The ports land soon enough for the reporter.
**Closest competitor**: none; this is a scheduling combination.
**First after slice 1**: S05b's verification, then a fix for `FeatureRepository.GetByPredicate` and `PortfolioRepository.GetById`.
**Decomposition**: as 6264's Stories, each releasable.
**Targets moved**: K1, K2 (idle). K3 and K4 only through lighter reads.
**Risk**: 6264's Stories are "behaviour-free" by charter (S05d), and S05b is "the one Story where a runtime effect
is acceptable". Bending them into performance Stories changes their contract.
**Rough cost**: M–L, shared with 6264.

### A — Adapt (from Linear and TanStack's stale-while-revalidate): shell first, then a client cache

**Core idea**: Lighthouse opens on its navigation and the cached lists of Portfolios, Teams and connections at once.
Repeat visits show the last answer, marked "updating", while the fresh one loads. One shared request answers each
question per page, not one per hook.
**Key mechanism**:
- Keyed TanStack Query queries for the licence status, the authorization summary (inside `useRbac()`, C4) and the
  Portfolio, Team and Feature lists.
- `useLicenseRestrictions()` reads a shared query instead of firing its own `/license` + `/teams` + `/portfolios`
  (W3).
- A completed update invalidates only the affected keys instead of re-running the whole detail chain (W6).
- `WidgetShell`-style status looks on the list: loading, dimmed-and-updating, could-not-load, and empty (W7).
**Key assumption**: the wait is mostly repeated and duplicated requests, plus refetch on every update. Showing the
last answer for a moment is acceptable when it is marked as updating.
**Closest competitor**: Linear's bootstrap-then-delta (the cheap half); TanStack Query
(`competitive-research.md` §1.4).
**First after slice 1**: one shared licence and authorization query per page (6 P\* → 2 per Overview open), plus
the Features list's failure and empty looks.
**Decomposition**: (i) dedup of the shared hooks; (ii) Features list on a keyed query with honest states;
(iii) targeted invalidation on update-completed; (iv) a persisted shell cache across reloads. Frontend only, except
for an optional counts endpoint.
**Targets moved**: K1 (strongly, also on first visit through dedup). K2 on return visits only; the first visit to
the 920-Feature list still pays the full server read. K3 and K4 not touched.
**Risk**: a stale value shown as current breaks the "Design for a slow connection" rule if a marker is missed. The
server cost per first read stays as it is.
**Rough cost**: S–M per step; 3–4 steps.

### M — Magnify (the foreground): the forecast gets its own lane

**Core idea**: whatever a forecast round costs, it never takes a thread a request needs. Pages answer at idle speed
while the round runs, and the round itself may take a little longer.
**Key mechanism**:
- The round runs on a dedicated, bounded worker: a long-running task with its own threads and lower priority, and
  its own `DbContext` scope. It no longer runs on shared pool threads.
- The gather and save become async and `AsNoTracking` where they only read.
- The request paths named in W2–W6 move to async EF (`ToListAsync`), so a slow read waits without holding a pool
  thread.
- A later, optional step runs the forecast in a separate process or container.
**Key assumption**: the stalls are pool starvation, caused by forecast work and synchronous reads. They are not
database locks (SQLite runs in WAL, W9) or GC pauses. Step 6 of slice 1 tests this.
**Closest competitor**: Microsoft's thread-pool starvation guidance; background job runners
(`competitive-research.md` §2.2).
**First after slice 1**: move the round's simulation, gather and save onto the dedicated worker (the part slice 1
shows dominant).
**Decomposition**: (i) a dedicated worker for the round; (ii) async gather and save; (iii) async EF on the
Portfolio and Team list and detail reads; (iv) async EF on the Features list reads; (v) optional: an out-of-process
worker (its own Epic).
**Targets moved**: K3 directly; K1 and K2 *during* a forecast. Idle K1 and K2 not touched. K4 unchanged, or a
little longer.
**Risk**:
- If the cause is GC or the database, isolation does not help.
- Async conversion is wide (many callers of synchronous repository methods), and it touches Epic 6264's port
  signatures.
**Rough cost**: M for the worker; L for the async conversion of the request paths.

### P — Put to other use (the measurement harness): a performance budget that every slice must meet

**Core idea**: the harness from slice 1 becomes a standing budget. Each screen and each forecast round gets a
recorded budget (requests per open, rows read, server time at the 920-Feature scale point). A test fails when a
change exceeds it. The Epic then fixes whatever breaks the budget, worst first.
**Key mechanism**: provider-backed tests that count EF commands and rows per endpoint on the scaled data set (like
the S05b verification), plus a Playwright request-count check per page. Budgets ratchet down slice by slice.
**Key assumption**: regressions, not only today's debt, are the long-term risk. A budget that is enforced steers
the work better than a fixed slice list.
**Closest competitor**: performance budgets in web tooling (no product cited; lightweight depth).
**First after slice 1**: budgets recorded for the four opens and a forecast round, with the current values as the
ceiling.
**Decomposition**: (i) budget tests; then each fix slice lowers one budget.
**Targets moved**: none directly. It moves whichever K the fixes target, and keeps them held.
**Risk**: slow or flaky perf tests in CI; a scaled data set in CI costs build minutes.
**Rough cost**: M for the budget tests; the fixes are priced separately.

### E — Eliminate (the data in scope): in-flight first, and a nudge on size

**Core idea**: a large Portfolio's Features list opens on the Features still in flight or not started. Done Features
come on request ("Show N done"). Portfolios above a threshold carry a non-blocking note on their size, with what it
costs (Features count, last forecast duration).
**Key mechanism**:
- The list endpoint takes a done/not-done filter in the query, so hide-completed moves from the client to the
  database.
- The forecast round already forecasts only remaining work; its gather is scoped to not-done Features where the
  inputs allow it.
- A log line and a validation note at the threshold (default 100, for DISCUSS).
**Key assumption**: most Features of a 920-Feature Portfolio are done, and readers mostly look at the ones in
flight. A size note leads some admins to narrow the query (A10).
**Closest competitor**: Jira boards' 5,000 cap with a warning; Jira Plans' "exclude completed work"
(`competitive-research.md` §1.1, §1.2).
**First after slice 1**: the not-done filter on the Features list endpoint, defaulting to what the list's
hide-completed toggle says today.
**Decomposition**: (i) the server-side done filter; (ii) a gather scoped to not-done where possible; (iii) the size
note.
**Targets moved**: K2, by the ratio of done Features. K4 by the same ratio, if the gather can be scoped. K1 and K3
not touched.
**Risk**:
- The maintainer's decision says the warning is a nudge, not the fix (V7).
- If most Features are in flight, nothing moves.
- It sits close to paging, which is out of scope.
**Rough cost**: S–M.

### R — Reverse (who asks): the answer comes to the reader

**Core idea**: instead of opening the 920-Feature list, the reader asks for one Feature's or one Portfolio's dates
through the Lighthouse-Clients CLI/MCP, or receives a short digest of what changed since the last round.
**Key mechanism**: a narrow read endpoint per Feature or Portfolio summary, plus a clients release; optionally a
digest.
**Key assumption**: most visits to the large list look up a few Features.
**Closest competitor**: AI-assistant dashboards over MCP (Epic 6298 research).
**First after slice 1**: a one-Feature forecast read in the clients.
**Decomposition**: a clients feature, outside this Epic's screens.
**Targets moved**: none of K1–K4. It avoids K2 for the readers it serves.
**Risk**: it does nothing for the operator's stalls or for readers who scan the whole list.
**Rough cost**: M plus a clients release.

## 3. Crazy 8s supplements

### X1 — Scope every read to what the screen shows

**Core idea**: the same screens, with each request reading only the columns and rows its answer uses.
**Key mechanism**:
- Lean list endpoints for Portfolios and Teams (id, name, counts).
- `GetById` filters in the database (W3b).
- `GetByPredicate` takes an expression, or the hot callers get intention-revealing methods.
- The dependency graph comes from a light projection (ids, references, Portfolios; maintainer, Q8).
- "In progress" is answered as ids from one query (W5).
- A Features-list endpoint keyed by Portfolio or Team id returns only the shown columns, with parents folded in
  (W6, W6c).
- The forecast's gather stops loading what the simulation and fingerprint do not use, such as the old histogram
  rows it is about to replace. How the save then replaces them is DESIGN's call.
- New endpoints are additive (C6). Old shapes stay for the clients.
**Key assumption**: most of the time per open is reads that scale with every Feature of the instance (A11). The
same reads, made lighter, also free pool time during a forecast.
**Closest competitor**: ActionableAgile's reduced fetch; `FeaturePositionMap`'s three-column projection in our own
code (`competitive-research.md` §1.3, §2.1).
**First after slice 1**: the scaffold reads.
- Lean Portfolio and Team lists.
- One licence-counts answer shared by every `useLicenseRestrictions()`. This request dedup is **borrowed from
  Option 2's** first step, served here by a cheap server count.
- `GetById` filtered in the database.

Evidence: W2, W3, W3b.
**Decomposition**: (i) scaffold reads; (ii) the Features list's instance-wide parts (dependency projection,
in-progress ids); (iii) a lean Features-list endpoint with the list's failure and empty looks; (iv) a lighter
forecast gather. Each step is independently releasable. They are **not** all re-orderable: (iii) consumes the
projection and the in-progress ids from (ii), so (iii) must follow (ii). Revision 1 corrects this; it used to say
every step could be re-ordered.
**Targets moved**: K1 and K2 idle (directly). K3 through less pool time per request and a lighter gather. K4
through the gather.
**Risk**:
- If the stalls are the simulation itself, reads alone do not meet K3.
- It overlaps with Epic 6264's S05b and S05d on the same repository methods.
**Rough cost**: S–M per slice; 4 slices.

### X2 — Forecast less, and less often

**Core idea**: a forecast round that would change nothing costs almost nothing, and rounds that overlap merge into
one.
**Key mechanism**:
- A cheap "did anything change?" check runs **before** the gather. It reads the involved Teams' last-update stamps
  and the Portfolio's recorded fingerprint, so Story 6354's skip also skips the gather (W8b, X7).
- The update queue coalesces rounds that would cover the same Teams within a short window, for example a Team
  update followed by a Portfolio update.
- Results are unchanged (C12): a round that runs still covers every Feature of the involved Teams, because Features
  of one Team share its throughput.
**Key assumption**: most rounds change little, and the round's length and frequency drive the stalls (A14).
**Closest competitor**: Jira Plans bounding the calculation (`competitive-research.md` §1.2).
**First after slice 1**: the pre-gather change check.
**Decomposition**: (i) the pre-gather check; (ii) coalescing in the update queue; (iii) a save that writes only
forecasts that moved.
**Targets moved**: K4 directly. K3 by fewer and shorter stall windows. K1 and K2 idle not touched.
**Risk**:
- A cheap check that misses a real change leaves a stale forecast, an invariant 6354 guarded with its fingerprint.
- Coalescing changes when a forecast lands.
**Rough cost**: M.

### X3 — Stream the list

**Core idea**: the Features list renders rows as the server produces them, so the first rows appear long before
the last.
**Key mechanism**: a streamed JSON response (`IAsyncEnumerable`) read incrementally by the client.
**Key assumption**: time to the first row matters more than total time, and the server can produce rows in order
without reading everything first.

### X4 — Make it a deployment problem

**Core idea**: document a minimum host size for large instances, recommend PostgreSQL above a size, and split the
forecast into a sidecar container in the Helm chart.
**Key mechanism**: documentation and Helm values.
**Key assumption**: the reporter's host is undersized.

## 4. Curation to 6

| Generated | Fate | Why |
|---|---|---|
| S Read models | **Kept as Option 4** | Distinct mechanism (precompute at write time), assumption (eventual consistency is acceptable) and cost (L, new tables). |
| C Ride 6264 | **Merged into Option 1** | Same mechanism as X1 (database-side scoped reads); it differs only in which Epic owns the Stories. The ownership question is carried to DISCUSS. |
| A Shell + client cache | **Kept as Option 2** | Client-side mechanism; assumes duplication and refetch dominate; frontend-only cost. |
| M Own lane | **Kept as Option 3** | Isolation, not cost reduction; assumes starvation; cost is wide async conversion. |
| P Budget ratchet | **Merged into the fixed frame** | It is a guardrail on top of any option, not a direction. Slice 1's harness carries it. DISCUSS decides whether its readings become CI budgets. |
| E In-flight first + nudge | **Kept as Option 6** | Reduces N, not cost per item; assumes most Features are done; small cost. |
| R Answer comes to the reader | **Set aside** | It serves a different circumstance (asking about one Feature) and none of K1–K4. Recorded as a disruption in `job-analysis.md` §4. |
| X1 Scope every read | **Kept as Option 1** | Query-level reduction; assumes over-reading dominates (A11). |
| X2 Forecast less | **Kept as Option 5** | Reduces background work volume; assumes rounds change little. |
| X3 Stream the list | **Merged into Option 1** | Time to first row is addressed by reading less; a streamed response still reads the whole table first under W4 and W5. |
| X4 Deployment problem | **Set aside** | Host size is unknown (Open question 2), and the code facts (W2–W8) hold on any host. |

### Curated 6

1. **Scope every read to what the screen shows** (X1, with C merged)
2. **Shell first, then a client cache** (A)
3. **The forecast gets its own lane** (M)
4. **Precomputed read models kept current by domain events** (S)
5. **Forecast less, and less often** (X2)
6. **In-flight first, and a nudge on size** (E)

### Diversity test

**Revision 1**: all 15 pairs are covered.

The keys used in the table:

| # | Mechanism | Key assumption | Cost |
|---|---|---|---|
| 1 | scope each read at query time | over-reading dominates (A11) | S–M ×4, backend, no schema |
| 2 | client cache and request dedup | duplication and refetch dominate | S–M ×3–4, frontend |
| 3 | separate the round from the request pool | the round starves the pool | M worker + L async |
| 4 | precompute at write time | reads ≫ writes | L per projection, new tables |
| 5 | fewer and cheaper rounds | most rounds change little | M, queue and check |
| 6 | fewer rows in scope | most Features are done | S–M once, filter and note |

| Pair | Mechanism differs? | Assumption differs? | Cost profile differs? |
|---|---|---|---|
| 1 vs 2 | yes: the server reads less vs the client asks less often | yes: per-read cost vs duplication | yes: backend vs frontend |
| 1 vs 3 | yes: less work vs separated work | yes: over-reading vs starvation | yes: per endpoint vs worker + async |
| 1 vs 4 | yes: query-time vs write-time | yes: over-reading vs reads ≫ writes | yes: no schema vs new tables |
| 1 vs 5 | yes: request reads vs background rounds | yes: over-reading vs rounds unchanged | yes: endpoints vs queue + check |
| 1 vs 6 | **partly**: both shrink what one request reads. Option 1 drops columns and instance-wide joins for every row; Option 6 drops rows | yes: the instance-wide cost vs the done ratio | yes: four backend slices vs one filter + note |
| 2 vs 3 | yes | yes: refetch vs starvation | yes: frontend vs backend worker |
| 2 vs 4 | yes: client cache vs server read table | **partly**: both accept a marked lag; the primary assumptions differ (repeat visits vs reads ≫ writes) | yes: frontend vs schema |
| 2 vs 5 | yes | yes | yes: frontend vs queue |
| 2 vs 6 | yes: cache answers vs narrow answers | yes: repeat visits vs done ratio | yes: frontend vs filter + note |
| 3 vs 4 | yes: isolate vs precompute | yes: starvation vs reads ≫ writes | yes: worker vs tables |
| 3 vs 5 | yes: separate the round vs shrink the round | yes: starvation from any round vs rounds unchanged | yes: worker + async vs queue + check |
| 3 vs 6 | yes | yes | yes: worker vs filter |
| 4 vs 5 | yes: precompute reads vs skip computes | yes | yes: tables vs queue |
| 4 vs 6 | yes | yes | yes: L vs S–M |
| 5 vs 6 | yes: skip unchanged rounds vs shrink the data in scope | yes: rounds unchanged vs done ratio | yes: M vs S–M |

**Result**:
- All 15 pairs differ in cost profile.
- 14 of 15 differ in mechanism; **1 vs 6 differs only partly** (columns and joins vs rows).
- 14 of 15 differ in key assumption; **2 vs 4 differs only partly** (both accept a marked lag).

No pair shares all three counts, so none is a variation of another, and none is merged. **Gate G3: PASS.**
