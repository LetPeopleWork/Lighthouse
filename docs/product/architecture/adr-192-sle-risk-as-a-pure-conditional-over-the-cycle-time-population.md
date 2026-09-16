# ADR-192: SLE Risk as a Pure Conditional Over the Same Closed Population the Cycle-Time Percentiles Read

**Status**: Accepted (2026-09-16 — Morgan, DESIGN wave, interaction mode PROPOSE). No code implements it yet; `epic-4127-sle-risk` slice 01 (ADO Story #6016) is the first commit that will.

**Feature**: `epic-4127-sle-risk` — ADO Epic #4127, "Show SLE Probability for In Progress Items"

**Decider**: Morgan (Solution Architect)

## Context

Epic #4127 asks for the chance an in-flight item has of breaching the Service Level Expectation its team published. DISCUSS locked the arithmetic (D1) as the empirical conditional

```
risk = count(T > R AND T >= a) / count(T >= a)
```

over the cycle times `T` of the work finished in the window, where `a` is the item's age and `R` the team's `ServiceLevelExpectationRange`. The `ServiceLevelExpectationProbability` half of the pair is deliberately not an input (D2); it is a property of the population and `ragRules.ts` already reports it there.

Four surfaces will show that number before the Epic is done — a dialog column (slice 01), a card chip (02), chart background zones (03) and a field written into the user's own tracker (04). DISCUSS D5 therefore required one computation serving all four, on the grounds that a drift between Lighthouse and a user's own Jira field is worse than a drift between two Lighthouse surfaces, and cannot be seen side by side to be caught.

Three existing decisions bound this one.

**ADR-065** settled the same shape of question for `workItemAgePercentiles`, and settled it against the frontend on the record: the user overrode a client-side recommendation with *"WIA percentiles should be calculated in the BACKEND… We want to do as little production work in the frontend."* It also established the machinery a new percentile-shaped read reuses — `GetWipSnapshotForTeam(team, endDate)` for the in-flight population, `GetFromCacheIfExists` for the cache, the class-level `[RbacGuard(TeamRead)]`, and the `startDate > endDate ⇒ 400` guard.

**ADR-018, upheld by ADR-021 and ADR-024**, is the standing refusal to introduce a shared per-state aggregation service. Its reasoning is specific and matters here: it rejected sharing because the two would-be consumers had *superficially identical signatures reasoning about completely different inclusion rules*, and a shared surface would hide that difference exactly where it mattered.

**ADR-188** is the nearest precedent in the other direction. For #5884 it extracted one pace-band ladder read by both the chart's geometry and the dialog's cell, because the two had to agree by construction rather than by resemblance.

The open question DESIGN must answer is where the arithmetic lives, what crosses the wire, and whether the standing no-sharing chain forbids a shared calculator here.

## Decision

**A pure calculator owned by neither caller, a team-scoped read endpoint mirroring `cycleTimePercentiles`, and a new two-field DTO.**

### 1. The arithmetic is a pure function, and it is the only definition

```
SleRiskCalculator.Risk(int ageInDays, int targetRangeInDays, IReadOnlyCollection<int> closedCycleTimes)
    -> int?        a whole percentage, or null when nothing finished ran as long as the item already has
```

No repository, no clock, no service provider. Two callers with identical semantics and different windows: `TeamMetricsService` passes the caller's `startDate`/`endDate`; `WriteBackTriggerService` (slice 04) passes the team's configured history and `Clock.Today`. The age is an input, never computed inside — which keeps the as-of-date convention the caller's business.

**ADR-018 does not forbid this, and the distinction is the whole of its reasoning.** ADR-018 rejected a shared service whose two consumers had *different inclusion rules behind identical signatures*. Here the two consumers have the *same* rule and differ only in the window they hand it — which is the case ADR-018 names as the right time to share ("at that point the two consumers' EXACT semantic needs are known concretely"). They are known concretely: they are the same. This is ADR-188's situation, not ADR-018's.

### 2. `TeamMetricsService` only — not `BaseMetricsService`

```
TeamMetricsService.GetSleRiskForTeam(Team team, DateTime startDate, DateTime endDate)
    -> IEnumerable<SleRiskDto>
```

`BaseMetricsService` is shared by the team and portfolio services. Putting the method there would make the portfolio twin a one-line addition, and DISCUSS D4 put portfolios out of scope for a reason that does not expire: a `Feature` belongs to a `List<Portfolio>`, each with its own target and its own history, so one feature has several answers and write-back has one field. A scope decision that costs one line to break is not a scope decision.

### 3. Evidence and population are both reused, not re-selected

- **Evidence** — `GetWorkItemsClosedInDateRange(team, startDate, endDate)` then `CycleTime(Clock.Zone)` with the same `> 0` filter, i.e. **the identical expression `GetCycleTimePercentilesForTeam` already evaluates**. The risk and the percentile lines on the same page therefore describe the same work by construction; a second selection would let them disagree about which items count.
- **Population** — `GetWipSnapshotForTeam(team, endDate)`, ADR-065's choice and the same set that feeds `/metrics/wip` and the aging chart's dots. The dialog's rows and the chart's dots are then the same items, which slices 02 and 03 depend on without being able to check.

Ages are already as-of the range's end rather than today (`ProjectStateAsOf`, and D16 of `widget-loose-ends`), so the answer is a function of `(team, startDate, endDate)` alone and the cache key `SleRisk_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}` is complete.

### 4. Wire contract

```
GET /api/{version}/teams/{teamId:int}/metrics/sleRisk?startDate&endDate
    [RbacGuard(TeamRead)]  (existing class-level guard)   ⇒ IEnumerable<SleRiskDto>

public sealed record SleRiskDto(string ReferenceId, int? Risk);
```

- **`ReferenceId`, not the numeric `Id`** — write-back addresses items by `ReferenceId` (`WriteBackFieldUpdate.WorkItemId = workItem.ReferenceId`), so one key serves both consumers and slice 04 needs no second join.
- **`Risk` is a nullable whole percentage.** `null` is the beyond-history case. Not `0`, not `100`, not an omitted entry: the item is still listed, and its answer is absent. A number there would read as certainty, and an omitted entry would be indistinguishable from an item that is not in progress.
- **A new DTO, not an existing one.** `PercentileValue` models percentile → value and this is neither. Widening `WorkItemDto` would change a payload shared across many call sites and, per the ADR-062 family, oblige a Lighthouse-Clients version gate.
- **A team with no target returns an empty collection**, not a list of nulls and not a 400. There is no promise, so there is nothing to be at risk of breaking.

### 5. No premium gate, no RBAC change, no client wrapper

The read rides the existing class-level `[RbacGuard(TeamRead)]`, exactly as ADR-065 §5 established, and carries no `ILicenseService` check. #5884's age bands — the nearest analogue — ship free; the premium boundary in this product is export and write-back, and slice 04 inherits the gate that already guards write-back.

**No CLI or MCP wrapper is added**, so no `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry is owed. This is a consequence of the absent wrapper, not of the route being new: ADR-065 §4 is explicit that a new endpoint 404s opaquely on an old server, so the moment a wrapper is added it must be version-gated.

### 6. Frontend: a descriptor, mirroring ADR-188's

`buildSleRiskColumnDescriptor` in `utils/charts/sleRisk.ts` returns a `SleRiskColumnDescriptor` the dialog consumes through an optional prop, exactly as `AgeBandColumnDescriptor` does. The dialog learns what a cycle time is no more than it learned what a percentile was. The descriptor carries `labelFor` (the column's value, so the export gets `86%` rather than a bare `86` — the `useDataGridExport` trap ADR-188's feature documented) and `riskFor` (the sort key, so ordering is numeric rather than lexical).

The descriptor is built from the endpoint's response. **No risk arithmetic exists in TypeScript** — that is §1's whole point and ADR-065's recorded directive.

## Alternatives Considered

**Option A — Compute in the frontend from `cycleTimeData` and `inProgressItems`, which `useMetricsData` already holds.**
Zero backend for slices 01–03; the two counts are a three-line filter over data already fetched for the scatterplot.
- **Rejected.** Slice 04 must compute server-side regardless, so this forks one rule into two languages — and unlike the parity risk ADR-065 weighed, the two outputs here would never appear on one screen to be compared. The disagreement would surface as a Lighthouse figure and a Jira field that differ, across systems, with nothing to show a reader which is wrong. It also contradicts the standing "as little production work in the frontend" directive.

**Option B — A field on `GET /metrics/wip`'s `WorkItemDto`.**
One fewer round trip; the in-flight population is already what that endpoint returns.
- **Rejected.** `wip` takes `asOfDate` and the risk needs a range, so the endpoint would carry two date vocabularies at once. `WorkItemDto` is shared across many call sites, and widening it obliges a client version gate (ADR-062 family) for a field no client asked for.

**Option C — The calculator as a `protected static` on `BaseMetricsService`, beside `ComputeAgeInStatePercentiles` and `BuildPercentiles`.**
Follows the established home for shared pure computation.
- **Rejected on reach, not on taste.** `protected` is unreachable from `WriteBackTriggerService`, which is not a metrics service and should not become one to borrow a function. A pure type owned by neither caller costs one file and serves both.

**Option D — Persist the risk per item, or raise a domain event when it changes.**
Would enable a risk-over-time trend and give write-back a change-trigger.
- **Rejected as unbought scope.** The value is a pure function of inputs that are all already stored; persisting it adds a migration, a staleness question and a second source of truth for something derivable. DISCUSS D16 chose to write on every update rather than on threshold crossings, so no change-detection is owed. A trend over risk is a different feature with a different cost.

## Consequences

**Positive**
- One definition of the rule, reachable from a metrics service and from the write-back trigger without either depending on the other.
- The risk and the cycle-time percentiles on the same page read the same closed population, and the dialog's rows and the chart's dots are the same in-flight items — both by construction rather than by two selections that happen to agree today.
- No migration, no new store, no new dependency, no premium gate, no client version gate. The genuinely new artifacts are one pure type, one service method, one controller action, one DTO and one frontend descriptor module.
- The cache key is complete, because ages are already as-of the range end.

**Negative / accepted**
- A team-only service method means the portfolio twin, if it is ever wanted, is a deliberate addition with its own decision about multi-portfolio membership — which is the point, and is a cost paid on purpose.
- A second round trip on the metrics view, alongside the percentile reads it sits next to. Consistent with every other metrics surface and cached on the same mechanism.
- `SleRiskCalculator` has no consumer but `TeamMetricsService` until slice 04 lands. Sharing is designed for on the strength of a decided slice, not a speculated one; if slice 04 is dropped, the calculator is an unshared pure function and still the right shape.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The risk arithmetic exists once, in `SleRiskCalculator`, and nowhere in TypeScript | No `sleRisk`-shaped arithmetic outside that type; the frontend descriptor is built from the endpoint response. Code review; `sleRisk.ts` contains no counting |
| The evidence set is the expression `GetCycleTimePercentilesForTeam` evaluates | Service test asserting the closed population behind a risk equals the population behind the cycle-time percentiles for the same window |
| The in-flight population is `GetWipSnapshotForTeam`, not a second selection | Service test asserting the returned entries equal the `/metrics/wip` set for the same `endDate` |
| An item that took exactly `R` days is not a breach; an item as old as a finished one counts it among its survivors | `Slice01SleRiskReadScenarios` — one scenario per boundary, each with the arithmetic that would change if it flipped |
| Beyond history is `null`, never `0`, `100` or an omitted entry | `Slice01SleRiskReadScenarios.An_item_older_than_anything_ever_finished_is_given_no_answer` |
| No portfolio route exists | `Slice01SleRiskReadScenarios.Portfolios_are_not_asked_this_question_at_all` asserts 404 |
| No premium gate on the read | The controller action carries no `ILicenseService` call; RBAC scenario asserts a team-scoped read succeeds for a reader with the grant |
| The column's exported value is the label, not the ratio | Vitest asserting the cell carries `86%` and never a bare `86` |

## Cross-feature impact

- **ADR-065** (`work-item-age-percentiles`): its endpoint contract, its in-progress selection and its no-premium-no-RBAC-change stance are the template followed here. Nothing about it changes.
- **ADR-018 / ADR-021 / ADR-024** (no shared per-state aggregation): untouched and unchallenged. This ADR shares a calculator between two consumers with the *same* inclusion rule; ADR-018 refused to share between two with *different* rules behind one signature. Neither decision is evidence about the other.
- **ADR-188** (`story-5884-work-item-age-bands`): the frontend descriptor here is deliberately its twin — optional prop, dialog kept ignorant, value-carrying `labelFor` for the export. The two columns sit side by side in the same dialog and answer different questions, which the headers and their tooltips distinguish.
- **ADR-100** (named cycle-time RAG neutrality and SLE anchoring): upheld. The SLE is anchored to the default started→finished window, so the risk reads the default cycle-time definition and named definitions stay out (DISCUSS D7).
- **`quiet-jira-writeback`** D1: slice 04 will write a value that moves on every refresh, and Jira suppresses watcher email only. Recorded as an accepted cost in DISCUSS D16, not mitigated here.
