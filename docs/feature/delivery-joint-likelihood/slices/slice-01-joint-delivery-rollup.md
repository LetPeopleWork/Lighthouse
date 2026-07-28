# Slice 01 — Delivery likelihood and dates are the joint across every feature

**Story**: US-01 · **ADO**: #5587 · **Job**: `job-delivery-likelihood-covers-every-feature` · **Effort**: ≤ 1 day
**Blocked by**: nothing. **Release-bound to**: slices 02–04 (D9 — do not release the maths alone).

## Goal

Replace `Delivery.GetGoverningFeature` with a joint rollup over `(team, feature)` rows, so the header
likelihood **and** the 70/85/95 chips reflect every feature rather than one representative.

## The rule (D5)

```
rows    = the delivery's (team, feature) work pairs that still have remaining work
bucket(t) = { r in rows : r.team == t }
teamCdf(t)(d) = min over r in bucket(t) of CDF_r(d)        # comonotonic within a team
deliveryCdf(d) = product over t of teamCdf(t)(d)           # independent across teams
```

`min` only ever operates *within* a bucket; the product only ever operates *across* buckets. Every
`(team, feature)` row lands in exactly one bucket, so a shared feature is expressed once and never
re-applied.

Grounding: `ForecastService.RunMonteCarloSimulation` groups trials by team
(`simulationResults.GroupBy(s => s.Team)`), so intra-team rows share throughput draws and contend via
the random `FeatureWIP` allocation (positively correlated ⇒ `min` is the honest upper bound), while
cross-team trial streams are independent by construction (⇒ product, as ADR-110 already does).

## IN scope

- `Delivery.CalculateMetrics` — joint likelihood + joint 70/85/95 histogram. `GetGoverningFeature`
  deleted (D7).
- Reuse `JointCompletionDistribution` for the cross-bucket product (D11). A per-bucket `min`
  combinator is new; DESIGN places it.
- The ADR-112 D8 un-forecastable short-circuit runs **before** the joint computation, unchanged (D2/D8).
- Read-side only: no schema change, no DTO field, no endpoint, no migration.

## OUT of scope

- The breakdown rows — they are marginals and stay marginals.
- Sufficiency (slice 02), copy (slice 03), docs/notes (slice 04).
- Per-trial max within a team's bucket; cross-team correlation.

## The three-way discriminating fixture

F1 shared by A+B, F2 owned by B alone. At some day: `A/F1 = 0.90`, `B/F1 = 0.80`, `B/F2 = 0.95`.

| Implementation | Result |
|---|---|
| **correct (row grain)** — `0.90 × min(0.80, 0.95)` | **0.720** |
| multiplying feature CDFs — `(0.90×0.80) × 0.95` | 0.684 (B double-penalised) |
| team term from `feature.Forecast` — `(0.90×0.80) × min(0.72, 0.95)` | 0.518 (B folded into A, then multiplied again) |

Three distinct values from one fixture. Breakdown rows here are F1 = 0.72 and F2 = 0.95, so the correct
delivery number **equals** F1's row — which is why slice 03's copy must not promise "always lower".

## The canonical consistency fixture

A delivery holding **one feature shared by two teams** must be **bit-identical** to that feature's
`AggregatedWhenForecast` — likelihood, histogram and all three dates. The single-*team* version is
trivially true and proves nothing; the shared version kills all three grain traps at once and forces
reuse of the existing largest-remainder allocation rather than a parallel implementation.

## The four traps

1. **Team term from `feature.Forecast`** instead of `feature.Forecasts.Where(team == t)` → AC-01.2.
2. **`min` over feature aggregates** rather than per-team rows → AC-01.3 (same fixture).
3. **Cartesian product** of teams × features instead of enumerating actual work pairs → AC-01.6.
   `AddOrUpdateWorkForTeam` / `RemoveTeamFromFeature` make the row set genuinely sparse.
4. **A finished row inside an unfinished shared feature** → AC-01.7/01.8. Team A can have
   `RemainingWorkItems == 0` on F1 while B still has work. Note the row is normally **absent** from
   `Forecasts` (`InitializeSimulationResults` filters `> 0`), but a **stale zero-trial row** is
   reachable between forecast runs because `Forecasts` is EF-persisted. Both shapes must contribute
   CDF ≡ 1; if it is A's only row, `bucket(A)` resolves to **1**, not "cannot forecast". The exemption
   keys off remaining work, not off who owns the empty forecast — the row-level replay of the Epic 5459
   zero-trial bug ("the zero-trial filter was not behaviour-preserving", evolution doc).

## Learning hypothesis

**Confirms** the scoping if the delivery rollup is a local change at the `Delivery.CalculateMetrics`
seam and the invariant `delivery ≤ every row` holds by construction.

**Disproves** it if a second consumer infers delivery confidence from a selected representative
elsewhere in the read path — in which case the joint distribution needs to be a carried value rather
than a computed one, and DESIGN must introduce it as such rather than patching one method.

## Acceptance criteria

Full text in `feature-delta.md` US-01. Summary: AC-01.1 row-grain formula · 01.2/01.3 the three-way
fixture · 01.4 invariant with equality permitted · 01.5 bit-identity on a shared feature · 01.6 sparse
row set · 01.7/01.8 finished rows contribute 1 · 01.9 `GetGoverningFeature` deleted and #5435 stays
fixed structurally · 01.10 ADR-112 D8 preserved · 01.11 empty delivery = 0 % · 01.12 no contract change.

## Gates before commit

1. `dotnet build` zero warnings, `dotnet test` green.
2. Mutation ≥ 80 % on the changed backend surface. A test that cannot distinguish 0.720 from 0.684 and
   0.518 does not count as coverage — check the fixture discriminates before believing a survivor list.
3. `docs/ci-learnings.md` pre-applied (CA1861 inline arrays in assertions; Sonar gate is *zero new
   issues of any severity*, including INFO — CS9236 cost a cycle on Epic 5459).
4. Consult ADO #5587 directly — its `System.Description` could not be fetched during DISCUSS.
