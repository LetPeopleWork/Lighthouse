# ADR-062: Named Cycle Time Read Contract — `WorkItemDto` Carries an Owned List of Named Cycle Times (Dots One-Shot), `cycleTimePercentiles` Keeps an Optional `definitionId` (Percentiles Per-Definition); No Read-Side License Gate; No Client Version-Gate

**Status**: Accepted (2026-06-08 — Morgan; Fork 2 confirmed by user. **Revised 2026-06-08** per user feedback after Slice-01 backend (`01-01`): list-shaped DTO replaces the per-definition re-fetch + `CycleTime` overload; the read-side premium gate is removed in favour of gating definition create/update only.)
**Date**: 2026-06-08
**Feature**: multiple-cycle-times (Epic 5251)
**Decider**: Morgan (Solution Architect); revision driven by user review of `01-01`
**Relationship to prior ADRs**: pairs with ADR-061 (computation) and ADR-064 (storage + the create/update premium gate). Follows ADR-055's client version-gate pattern. Resolves DISCUSS **D8** (read mechanism) and the cross-cutting Lighthouse-Clients gate; the read-side premium clause of D8 is superseded here (gate-at-write only).

---

## Context

DISCUSS locked (D8) that config writes ride the EXISTING settings endpoint and per-definition scatter/percentile data is served on read; the cross-cutting checklist requires any NEW endpoint wrapped by the CLI/MCP clients to be version-gated.

Code reality (verified `API/TeamMetricsController.cs`):

- `GET …/metrics/cycleTimeData?startDate&endDate` returns `IEnumerable<WorkItemDto>`; the FE scatterplot (`CycleTimeScatterPlotChart.tsx`) plots `item.cycleTime` directly.
- `GET …/metrics/cycleTimePercentiles?startDate&endDate` returns `IEnumerable<PercentileValue>` (50/70/85/95).
- Both are class-level `RbacGuard(TeamRead)` (PortfolioRead twin), share `startDate.Date > endDate.Date ⇒ 400`, and cache per entity by a date-keyed string.

The original ADR-062 (Option c) extended BOTH endpoints with an optional `definitionId` and overloaded `WorkItemDto.CycleTime` to carry the named duration, re-fetching per selector change. User review of the `01-01` implementation rejected the `WorkItemDto` ctor overload and the read-side license gate as needless complexity:

- **Dots**: a work item HAS a default cycle time AND 0..N named cycle times — that is naturally a *list on the item*, returned once, not a value re-fetched per selection.
- **License**: the controller should not consult `ILicenseService`. Named cycle times can only be CREATED behind the premium gate (settings write, ADR-064). No definitions ⇒ nothing to select ⇒ nothing to read; a direct API caller with no definitions gets an empty list. Gating the read too is redundant defence with real cost (a license dependency + a branch + a test on a hot read path).

---

## Decision

### 1. `WorkItemDto` carries an owned list of named cycle times (dots are one-shot)

`WorkItemDto` gains an additive field:

```
CycleTime   : int                       // unchanged — the default StartedDate→ClosedDate duration
NamedCycleTimes : IReadOnlyList<NamedCycleTimeValue>   // additive; 0..N entries
                                          // NamedCycleTimeValue = { int DefinitionId, int Days }
```

- `GET …/metrics/cycleTimeData` returns each `WorkItemDto` carrying its default `CycleTime` AND a `NamedCycleTimes` list with one entry per **valid** definition (ADR-063) the item crossed BOTH boundaries for (ADR-061 `NamedCycleTimeDays`; items that did not cross both boundaries for a given definition simply have NO entry for it — D9 exclusion, per-definition). The endpoint takes **NO `definitionId`** — it returns the default plus all named durations in one shot.
- The FE scatterplot selector switches between Default and each named definition **client-side** off the already-returned list — no re-fetch for the dots, no `WorkItemDto` ctor overload. Default selection plots `item.cycleTime`; a named selection plots `item.namedCycleTimes.find(d => d.definitionId === selected)?.days` (absent ⇒ the item drops out of the named series, D9).
- Additive field on an EXISTING response ⇒ unchanged behaviour for every existing caller (the field is simply ignored by anything not reading it).

### 2. `cycleTimePercentiles` keeps an optional `definitionId` (percentiles per-definition, server-side)

Percentile lines stay a server-side computation (`PercentileCalculator`), NOT derived client-side from the dot list:

```
GET …/metrics/cycleTimePercentiles?startDate&endDate[&definitionId=<int>]   [RbacGuard(TeamRead/PortfolioRead)]
```

- `definitionId` absent/0/null ⇒ today's default-window percentiles, byte-for-byte.
- `definitionId` present ⇒ percentiles over the named series for that definition (the non-null `NamedCycleTimeDays` durations). Invalid definition (ADR-063) ⇒ empty/`IsValid:false` signal, never a 500.
- Cache key gains `_Def_{definitionId}` when present (mirrors `SelectionCacheSuffix`); default key unchanged. (`cycleTimeData` needs no suffix — it has no `definitionId` and returns the full list.)

The selector therefore does ONE percentile re-fetch on change (cheap, server-authoritative) while the dots are already in hand.

### 3. No read-side license gate (gate at create/update only)

Neither `cycleTimeData` nor `cycleTimePercentiles` consults `ILicenseService`. The premium gate lives exclusively on definition **create/update** in the settings write (ADR-064 §3). Rationale: a non-premium owner cannot create a definition, so it has none to select or read; a direct API caller with no definitions receives an empty `NamedCycleTimes` list / default percentiles. The `TeamMetricsController`/`PortfolioMetricsController` stay free of the license dependency, the premium branch, and the non-premium-refusal test on the read path.

**Accepted edge (user-confirmed)**: a team that was premium, created definitions, then downgraded keeps computing those definitions on read. Named cycle times are a non-sensitive flow metric; data persisting through a downgrade is preferable to it vanishing, and blocking it is not worth the effort.

### 4. Lighthouse-Clients version-gate consequence — NO new gate

- `NamedCycleTimes` is an **additive field on the existing `cycleTimeData` response**; `definitionId` is an **additive optional query param on the existing `cycleTimePercentiles`** ⇒ **NO version gate** (ADR-055 rule: additive field/param on an existing contract needs none). Old servers omit the field / ignore the param and degrade gracefully — no opaque 404.
- The settings **write** (`CycleTimeDefinitions`) rides the existing settings endpoint as an additive field ⇒ **NO version gate** (D8, same as ADR-056 `waitStates`).
- **Net**: zero new version-gate touch-points. Clients read named durations straight off `WorkItemDto.namedCycleTimes` and may pass `definitionId` to the existing percentile method; no `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry.

---

## Alternatives Considered

**Chosen: list-shaped `WorkItemDto.NamedCycleTimes` (dots one-shot) + per-definition `cycleTimePercentiles` + gate-at-write.**

- Pros: a work item's 0..N named durations are modelled as what they are — a list on the item; one fetch for all dots, client-side selector switching, no ctor overload; percentiles stay server-authoritative via the existing calculator; the read controllers carry no license concern (simpler, no premium branch on a hot path); zero new client gate. Single premium enforcement point (create/update).
- Cons: `cycleTimeData` computes all valid definitions' durations for every item on each call (more compute than a single default), and the response is larger. Accepted: definitions are few, the computation reuses the existing ordering walk (ADR-061), and it removes N re-fetches.

**Rejected (original ADR-062 Option c): overload `WorkItemDto.CycleTime` + re-fetch `cycleTimeData?definitionId` per selection.**

- Cons: a ctor overload to smuggle the named value into a scalar field; a re-fetch round-trip per selector change for data the server could return once; a `definitionId` branch + premium gate on the read controller. Superseded by user review of `01-01`.

**Rejected: NEW definition-by-id endpoint (Option a) / inline-boundaries endpoint (Option b).** New route ⇒ new client version-gate + opaque 404 + duplicated scaffolding (a); boundaries on the wire bypass the saved-definition validity check and can't carry the definition id for telemetry (b).

---

## Consequences

**Positive**:
- Dots for Default + all named definitions arrive in one `cycleTimeData` response; the selector is a pure client-side projection — no re-fetch, no ctor overload.
- Read controllers have no license dependency; one premium enforcement point at create/update; simpler hot path.
- Additive DTO field + additive percentile param ⇒ zero new client version-gate touch-points; old servers degrade gracefully.

**Negative**:
- `cycleTimeData` computes every valid definition per item per call (larger payload, more compute). Contained — few definitions, reused ordering walk.

**Neutral**:
- Downgraded-premium owners keep computing existing definitions on read (accepted; non-sensitive metric).
- Percentiles remain a per-definition server call so the percentile math stays in `PercentileCalculator`, not the browser.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `cycleTimeData` returns each item's default `CycleTime` + a `NamedCycleTimes` list (one entry per valid crossed definition) | `TeamMetricsControllerTests`/integration: PHX-204 ⇒ `namedCycleTimes` contains `{definitionId:1, days:47}`; an item crossing one boundary has no entry for that definition |
| `cycleTimeData` takes NO `definitionId`; dots switch client-side | Integration: no `definitionId` param; FE test: selector projects off `item.namedCycleTimes` without a network call |
| `cycleTimePercentiles?definitionId` computes named percentiles server-side; absent ⇒ default byte-identical | Integration: golden default equality; named P85 over the named series |
| Read controllers do NOT reference `ILicenseService` | Grep/ArchUnit: no `ILicenseService` usage in `Team/PortfolioMetricsController` read paths; premium gate asserted only on the settings write (ADR-064) |
| Invalid definition ⇒ no `NamedCycleTimes` entry / empty percentiles, never 500 | Integration: removed boundary state ⇒ no exception (ADR-063) |
| Additive field + additive param ⇒ NO new client version-gate | Clients-repo handoff note: read `namedCycleTimes` off the existing DTO; pass `definitionId` to the existing percentile method; no registry entry |

---

## Cross-feature impact

- Lighthouse-Clients (CLI + MCP): **no new gate** — additive DTO field + additive optional param.
- Default scatter/percentile callers (FE Default selection, existing clients): UNCHANGED (new field ignored; no `definitionId` ⇒ default).
- Premium gating now lives solely on definition create/update (ADR-064 §3); DISCUSS D8's read-side premium clause is superseded by this gate-at-write decision.
- `state-time-cumulative-view`: the US-04 scoped cumulative read (ADR-063 §4) is unaffected by this revision — it keeps its own optional `definitionId`.
