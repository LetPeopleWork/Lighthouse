# Feature Delta — story-6247-server-marks-stale

> ADO User Story #6247 — *Lighthouse marks stale Work Items itself, so skills and clients read one rule.*
> Follow-up from the skills DISCUSS (#6246 / `story-6217-lighthouse-skills`, gap G4, maintainer answer Q2 → (a)).

Today the staleness rule lives in the browser (`Lighthouse.Frontend/src/utils/staleness/deriveStaleness.ts`), so
neither `lh` nor the MCP tools nor the Daily Flow Review skill can tell which Work Items are stale. A second,
different rule sits in the backend (`WorkItemService.IsStale`) behind the `WorkItemBecameStale` event. This
feature makes Lighthouse answer "is it stale, and why" on its own reads, one rule for every surface.

---

## Wave: DISCUSS / [REF] Persona

- **flow-coach** — runs the Team's daily / flow review; wants Lighthouse's Stale signal in the same place as
  Blocked and SLE Risk, whether they read the dashboard or ask the Daily Flow Review skill.
- **client user** (secondary) — reads Lighthouse through `lh` or an MCP-connected assistant; today sees
  `isBlocked`/`blockedSince` per Work Item but nothing about staleness.

## Wave: DISCUSS / [REF] JTBD one-liner

Extends `job-flow-coach-spot-stuck-items` (`docs/product/jobs.yaml`): *When I run the Team's daily, I want the
Work Items past our staleness threshold named for me — by the dashboard and by the skill alike — so I can start
the "what is holding this?" conversation before Cycle Time suffers.* The job's **anxiety** force (does
Lighthouse's number agree with reality?) is exactly what two disagreeing rules realise; one rule removes it.

## Wave: DISCUSS / [REF] Evidence — the two rules today

| | Frontend `deriveStaleness` | Backend `WorkItemService.IsStale` (`WorkItemService.cs:486`) |
|---|---|---|
| Blocked takes precedence over time in state | yes | no |
| Blocked-duration trigger (`blockedStalenessThresholdDays`, `≥`) | yes | no |
| Threshold `0` means off | yes | no — every Doing item older than 0 days counts |
| Day counting | whole days, UTC dates | fractional `TotalDays` at sync time |
| Measured against | `now`, or the range end on the Flow Overview count — but **today** for the aging-pace data (`BaseMetricsView.tsx:1878`) | sync time |

`WorkItemBecameStale` is published through `domainEventDispatcher` but **no handler subscribes to it** (only tests
reference it), so changing its rule changes no user-visible behaviour today.

UI consumers of `deriveStaleness`: `WorkItemAgingChart` (stale dots), `TimeInStateBadge` (red badge + reasons
tooltip, also in `WorkItemsDialog` columns), `BaseMetricsView` (Flow Overview Stale count/RAG, aging-pace data).

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **Team and Portfolio current-WIP reads carry the flag** (`GET …/teams/{id}/metrics/wip`, `GET …/portfolios/{id}/metrics/wip`). Both owners have both thresholds and the UI shows Stale on both. Any other Work Item read the UI shows a Stale badge on carries it too, so the frontend never needs the rule (D3); which reads that is, is DESIGN's inventory. | Locked (maintainer, 2026-10-16) |
| D2 | **Each Work Item carries `isStale` plus its reasons**: a list of `{ kind, days }` (+ state name where it applies), `kind` a closed set — time in state, blocked duration, and the context reason (a Blocked item also past the time-in-state threshold, shown but not making it stale). Names and wire shape are DESIGN's. | Locked (maintainer) |
| D3 | **The frontend reads the server's answer and `deriveStaleness` is deleted.** Aging chart, Time-in-State badge and its tooltip, Flow Overview Stale count/RAG and aging-pace data all read the server fields. | Locked (maintainer) |
| D4 | **One rule, the current frontend semantics**: Blocked takes precedence; time in state stale when days `>` threshold and not Blocked; blocked duration stale when Blocked days `≥` blocked threshold; a threshold of `0` turns its trigger off; only Work Items in progress (Doing). | Locked |
| D5 | **Measured as of the read's date**: today for a plain read, the `asOfDate` when one is passed — the snapshot the read already returns. This also fixes the aging-pace data, which today measures past ranges against today. | Locked |
| D6 | **`WorkItemBecameStale` uses the same rule.** It has no subscriber, so this is safe, and keeping a second rule behind it would contradict the story. Whether the event (and `WasStaleAtLastSync`) should instead be removed is a DESIGN call. | Locked (rule); removal → DESIGN |
| D7 | **Lighthouse-Clients pass-through and the Daily Flow Review "Stale" item are in this story**, as its last slice. The clients pass the server's fields through; they never compute staleness. Against an older Lighthouse without the fields, the skill says Lighthouse does not say which Work Items are stale — it never guesses. | Locked (maintainer) |
| D8 | **Days are counted in the instance's time zone** (the clock `WorkItemAge` uses), not the browser's UTC dates. Near midnight a badge may read one day different from today; that is the cost of one rule. | Locked — DESIGN confirms the clock seam |

## Wave: DISCUSS / [REF] User stories

### US-01 — Lighthouse says which Work Items in progress are stale, and why

As a **client user or flow-coach**, I want Lighthouse's WIP reads to say which Work Items are stale and why, so
every surface — dashboard, `lh`, MCP — answers from one rule.

`job_id: job-flow-coach-spot-stuck-items`

#### Elevator Pitch
Before: `GET /api/latest/teams/{id}/metrics/wip` returns `isBlocked`/`blockedSince` per Work Item but no staleness; only the browser can work it out.
After: run `lh metrics team --metrics wip --id <id>` (or call the endpoint) → each Work Item carries `isStale` and its reasons, e.g. stale "12 days in Review" or "Blocked 6 days".
Decision enabled: which stuck Work Items to raise in today's daily, without opening the dashboard.

#### Acceptance Criteria
1. With staleness threshold 5 and blocked threshold 3, a Work Item in progress, not Blocked, 6 days in its state reads `isStale` true with one time-in-state reason (6 days, state name); at exactly 5 days it reads false.
2. A Blocked Work Item Blocked for 3 days reads `isStale` true with a blocked-duration reason (3 days); Blocked for 2 days and 9 days in state reads false, carrying only the context reason.
3. Threshold `0` turns its trigger off: with both thresholds `0` no Work Item reads stale and none carries a reason.
4. Passing `asOfDate` measures days against that date, not today.
5. The same holds for `GET /api/latest/portfolios/{id}/metrics/wip` using the Portfolio's thresholds.
6. `WorkItemBecameStale` fires on the transition into stale under this rule (Blocked items past time in state but under the blocked threshold do not fire it; threshold `0` never fires it).

### US-02 — The dashboard reads Lighthouse's answer

As a **flow-coach**, I want the dashboard's Stale signals to come from the same answer the clients get, so what I
see and what the skill says never disagree.

`job_id: job-flow-coach-spot-stuck-items`

#### Elevator Pitch
Before: the browser works staleness out itself, and the aging-pace data measures a past range against today while the Stale count measures it against the range end.
After: open a Team's **Flow Overview** (current or past range) → the Stale count, the red Time-in-State badges with their tooltip reasons, and the aging chart's stale dots all match the WIP read's `isStale` for the same date.
Decision enabled: the coach trusts one Stale number, on any range, as the list to discuss.

#### Acceptance Criteria
1. For the same Team and date, the Flow Overview Stale count equals the number of WIP Work Items the server marks stale.
2. The Time-in-State badge is red exactly when the server says `isStale`; its tooltip lists the server's reasons (`Blocked 6d`; `12d in Review`).
3. On a past range, the aging-pace data's stale items match the Stale count's (both as of the range end).
4. `deriveStaleness` no longer exists in the frontend; nothing in `src/` computes staleness from thresholds.
5. Same behaviour on the Portfolio Flow Overview.
6. No visible change for a current range other than D8's time-zone day counting.

### US-03 — The Daily Flow Review names stale Work Items

As a **flow-coach using the Daily Flow Review skill**, I want the skill to bring stale Work Items into what to
discuss, so the daily covers every stuck Work Item, not only the Blocked ones.

`job_id: job-flow-coach-spot-stuck-items`

#### Elevator Pitch
Before: the skill names Blocked and at-risk Work Items, and says nothing about stale ones.
After: ask the assistant for the Daily Flow Review → it lists stale Work Items that are not already listed as Blocked, with Lighthouse's reason ("12 days in Review"), using the instance's term for Stale.
Decision enabled: which stale Work Items the Team discusses today.

#### Acceptance Criteria
1. `lighthouse_team_metrics_wip` (and `lh metrics team --metrics wip`) pass through `isStale` and its reasons unchanged; the `summary` text mentions stale Work Items when there are any.
2. The Daily Flow Review lists a stale Work Item with the reason Lighthouse gave; a Work Item already listed as Blocked is not listed twice.
3. Against a Lighthouse without the fields, the skill says Lighthouse does not say which Work Items are stale, and never works it out itself.
4. A calm day ("Nothing to decide today…") still requires no stale Work Items as well.
5. Client packages carry a version bump and the version gate names the first Lighthouse release with the fields.

## Wave: DISCUSS / [REF] Out-of-scope

- Changing the rule's semantics beyond D4/D8 (new triggers, per-state thresholds, Feature-level staleness rules).
- Staleness on Work Items in To Do or Done.
- Any new setting or UI control — the thresholds and their settings form are unchanged.
- A usage-data event (DEVOPS skipped by the maintainer; a read-model field is not a feature to count).
- The general `lighthouse` skill's wording beyond what it already says about Stale.

## Wave: DISCUSS / [REF] Cross-cutting impact (DoR Item 7)

- **RBAC** — **N/A, because** the fields ride on existing reads already gated by Team/Portfolio read
  permissions; no new endpoint, no new write.
- **Lighthouse-Clients (CLI + MCP)** — **In scope (US-03).** Additive fields on existing reads → minor version
  bump on `client`, `cli`, `mcp-core` (and the `mcp-http`/`mcp-stdio` hosts as they pick it up); version-gated on
  the Lighthouse release that ships US-01. Release needs the manual `pnpm release:version` bump.
- **Website** — **N/A, because** no new capability is marketed; Stale is already on the site as part of the Flow
  Overview. The Daily Flow Review skill's listing changes only in what it covers.
- **Terminology** — "Stale"/"Blocked"/"Work Item" render as the instance's terms in the summary and the skill.
- **Docs** — `docs/metrics/flow-overview.md` (Stale is now Lighthouse's answer, days in the instance's zone),
  `docs/aiintegration.md` (WIP read carries staleness). At finalization.

## Wave: DISCUSS / [REF] WS strategy

**B — extend existing.** No skeleton: each slice extends a shipped read end to end.

## Wave: DISCUSS / [REF] Driving ports

- HTTP `GET /api/latest/teams/{id}/metrics/wip`, `GET /api/latest/portfolios/{id}/metrics/wip` (+ `asOfDate`).
- Team / Portfolio Flow Overview UI (Stale widget, aging chart, Time-in-State badge, Work Items dialog).
- `lh metrics team|portfolio --metrics wip`, MCP `lighthouse_team_metrics_wip`.
- Daily Flow Review skill (`lighthouse-clients/skills/lighthouse-daily-flow-review/SKILL.md`).

## Wave: DISCUSS / [REF] Scope Assessment: PASS

3 stories, 3 slices, 2 repositories, ~2–3 days in total. One user outcome per slice; no oversized signal beyond
the second repository, which DFR work always needs.

## Wave: DISCUSS / [REF] Story map and slices

| # | Slice | Stories | Learning hypothesis |
|---|---|---|---|
| 01 | Lighthouse marks stale on its WIP reads | US-01 | Disproves "the browser rule can move to the server unchanged" if the as-of snapshot or the zone day count cannot reproduce the frontend's results on demo data. |
| 02 | The dashboard reads Lighthouse's answer | US-02 | Disproves "the UI needs nothing but the WIP read" if a surface showing the badge gets its Work Items from a read that does not carry the flag. |
| 03 | Clients and the Daily Flow Review read it | US-03 | Disproves "pass-through is enough" if the skill still needs to compute anything to place a stale Work Item. |

Order 01 → 02 → 03: 02 and 03 each depend only on 01. Each slice ships on its own (01 already gives `lh` users the
flag through the raw read).

## Wave: DISCUSS / [REF] Outcome KPI

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-one-stale-rule` | 0 staleness rules outside the backend domain after slice 02 | `grep` for threshold-based staleness logic in `Lighthouse.Frontend/src` and `lighthouse-clients/packages` → none |
| `OUT-stale-agrees` | 100% agreement between Flow Overview Stale count and the WIP read's `isStale` count, same Team and date | Acceptance tests on demo data, current and past range |

## Wave: DISCUSS / [REF] Definition of Done

1. US-01..03 acceptance criteria green.
2. `deriveStaleness` and its tests deleted; equivalent rule coverage lives in backend tests.
3. `pnpm test`, `pnpm build` (Biome clean) green.
4. `dotnet build` (warnings as errors) and the filtered `dotnet test` green.
5. Clients: tests green, version bumped, gate set.
6. Mutation ≥ 80% on the new backend rule.
7. Docs (`flow-overview.md`, `aiintegration.md`) and the skill updated at finalization.
8. SonarCloud: no new issues.
9. ADO #6247 → Resolved on push.

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | User value clear | ✅ | Elevator pitches US-01..03 |
| 2 | Job traceability | ✅ | `job-flow-coach-spot-stuck-items` |
| 3 | ACs testable | ✅ | Boundaries (5/6 days, 2/3 days, 0), as-of, count equality |
| 4 | Dependencies known | ✅ | Skills feature shipped `lighthouse_team_metrics_wip`; WIP endpoints exist |
| 5 | Scope bounded | ✅ | Out-of-scope list; D4 fixes semantics |
| 6 | KPI defined | ✅ | Two KPIs above |
| 7 | Cross-cutting addressed | ✅ | RBAC / Clients / Website / Terminology / Docs answered |
| 8 | Sized | ✅ | 3 slices, each ≤ 1 day |
| 9 | No blocking unknowns | ✅ | Reads inventory (D1), wire names (D2), event removal (D6) left to DESIGN by name |

## Wave: DISCUSS / [REF] Wave Decisions Summary

**Key decisions:** D1 Team + Portfolio WIP reads carry the flag; D2 `isStale` + reasons; D3 frontend switches,
`deriveStaleness` deleted; D4 current frontend semantics are the one rule; D5 measured as of the read's date;
D6 the event uses the same rule; D7 clients + DFR in scope, last slice; D8 days in the instance's zone.

**Feature type:** cross-cutting (backend read model, frontend, clients, skill).

**Waves:** DEVOPS skipped by the maintainer (2026-10-16). DESIGN → DISTILL → DELIVER follow.

**Open for DESIGN:** where the rule lives (domain service vs. Work Item method), which reads carry it, wire names,
the clock seam for the zone, keep or remove `WorkItemBecameStale` / `WasStaleAtLastSync`.

**Upstream changes:** none against DISCOVER (none exists). Resolves `story-6217-lighthouse-skills` G4.

---

## Wave: DESIGN / [REF] Scope and mode

Application scope, PROPOSE mode (maintainer, 2026-10-16). Architect: Morgan (`nw-solution-architect`). Every
recommended option taken in AFK mode; none changes what a user sees. ADR-240 (Proposed).

## Wave: DESIGN / [REF] Decisions

| ID | Decision |
|---|---|
| DDD-1 | **One pure domain policy** `Models/Staleness/StalenessPolicy` (`For(owner)`, `Evaluate(candidate, zone, asOfDay) → StalenessVerdict(IsStale, Reasons)`). No clock dependency; zone and day passed as values. Rejected: a `WorkItemBase` method (cannot judge a Feature's past state on the Portfolio read), an `IStalenessService` port (ceremony over pure arithmetic). |
| DDD-2 | **Semantics = DISCUSS D4**, plus the context reason `TimeInStateWhileBlocked` (Blocked and past time in state; does not make the item stale). |
| DDD-3 | **Days** = `asOfDay − InstanceCalendar.DayOf(enteredAt, zone) + 1`, minimum 1 — the Work Item Age arithmetic (`WorkItemBase.cs:160`, browser `age.ts:24-27`), so D4's boundaries keep their meaning. `asOfDay = DateOnly.FromDateTime(asOfDate)`. Blocked days use the same arithmetic on the `blockedSince` the read already resolves; null `blockedSince` → no blocked-duration reason. |
| DDD-4 | **Reads that carry it**: only `GET teams/{id}/metrics/wip` (Team thresholds) and `GET portfolios/{id}/metrics/wip` (Portfolio thresholds, `FeatureDto : WorkItemDto`). Every Stale surface — Stale count/RAG, Stale and WIP dialogs, aging dots and its dialog, aging-pace data — reads `inProgressItems` from these. Evaluated on the DTO's resolved fields after the as-of projection. |
| DDD-5 | **Not carried** by `blockedItemsAtDate` (spans To Do, no time-in-state column), Team `featuresInProgress` and Feature child items (owner ambiguous, no Stale UI), cycle-time / run-chart / size / SLE-risk reads. |
| DDD-6 | **Wire**: `isStale: bool?`, `staleReasons: StaleReasonDto[]?` with `{ kind, days, stateName? }`, all `[JsonIgnore(WhenWritingNull)]` — absent means "Lighthouse does not say". `StaleReasonKind { TimeInState, BlockedDuration, TimeInStateWhileBlocked }` serialises PascalCase via the global `JsonStringEnumConverter`; TS mirrors it as a string union, never a numeric enum. |
| DDD-7 | **DTO wiring without a new ctor parameter** (the ctor already carries an S107 suppression): the two WIP projections set the verdict after construction through one narrow method on `WorkItemDto`. |
| DDD-8 | **Sync-time event on the same policy**: inject `ILighthouseClock` into `WorkItemService`, evaluate with `clock.Today` and `blockedItemService.IsBlocked`, blockedSince = `CurrentStateEnteredAt` when blocked (as the live read does). `ThresholdDays` = the threshold of the reason that fired. Keep `WorkItemBecameStale` and `WasStaleAtLastSync` (removal leaves a dead column; dropping it is not expand-only). |
| DDD-9 | **Frontend**: `IWorkItem` gains `isStale?` / `staleReasons?`; aging chart, `TimeInStateBadge` (tooltip formats the server reasons as today: `Blocked 6d`, `12d in Review`), dialog columns and `BaseMetricsView` read them; threshold props for staleness dropped; `utils/staleness/` deleted. `hasStaleConfig` and the Blocked RAG keep reading thresholds from settings. |
| DDD-10 | **Clients**: `client/metricsWording.ts` `InProgressItem` gains the fields; unknown reason kinds are dropped, never fail the item; `describeStaleNow` beside `describeBlockedNow`; `describeWhatWipLeavesUnsaid` says when Lighthouse does not say which are Stale. mcp-core `summariseCurrentWip`, cli `headlineLines`, both WIP tool descriptions. `FEATURE_REQUIRES_SERVER_NEWER_THAN.staleness` documents the gate; runtime falls back on field presence (as `isBlocked`). DFR SKILL.md: step 3 lists the fields, a Stale decision after Blocked (no duplicates), a "Stale unknown" bullet, calm-day rule, evals. Contract fixture test pins the wire shape. |
| DDD-11 | **Guards**: ArchUnitNET — `Models.Staleness` depends on nothing in `Services`/`API`. Vitest — nothing in `src/` compares days against a staleness threshold (allow-list `hasStaleConfig` and the Blocked RAG), making `OUT-one-stale-rule` executable. |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `deriveStaleness` | `Lighthouse.Frontend/src/utils/staleness/deriveStaleness.ts` | the whole rule | DELETE (semantics ported) | D3 |
| `WorkItemService.IsStale` | `Services/Implementation/WorkItems/WorkItemService.cs:486` | second rule | REPLACE with the policy | D6 |
| `InstanceCalendar`, `WorkItemBase.AgeOnDay` | `Models/InstanceCalendar.cs`, `Models/WorkItemBase.cs` | day reduction, +1 | REUSE | D8 parity with Work Item Age |
| `WorkItemDto` | `API/DTO/WorkItemDto.cs` | resolved snapshot fields | EXTEND, additive | single wire home |
| `IBlockedItemService` | `IBlockedItemService.cs` | is-blocked decision | REUSE | the one blocked authority |
| `StalenessPolicy` | — | none | CREATE NEW | no owner-level pure policy exists; pure-function contract |
| Blocked pass-through | `lighthouse-clients/packages/client/src/metricsWording.ts` | absence semantics | EXTEND | same compatibility model |

## Wave: DESIGN / [REF] Component decomposition

| Path | Change |
|---|---|
| `Lighthouse.Backend/Lighthouse.Backend/Models/Staleness/` (`StalenessPolicy`, `StalenessVerdict`, `StaleReasonKind`) | NEW |
| `API/DTO/WorkItemDto.cs`, `API/DTO/StaleReasonDto.cs` | EXTEND / NEW |
| `API/TeamMetricsController.cs` (`wip`), `API/PortfolioMetricsController.cs` (`wip`) | EXTEND |
| `Services/Implementation/WorkItems/WorkItemService.cs` | MODIFY (clock, policy) |
| `Models/Events/WorkItemBecameStale.cs` | KEEP (semantics change) |
| Frontend `models/WorkItem.ts`, `TimeInStateBadge.tsx`, `WorkItemAgingChart.tsx`, `WorkItemsDialog.tsx`, `workItemColumns.tsx`, `BaseMetricsView.tsx` | MODIFY |
| Frontend `utils/staleness/*` | DELETE |
| Clients `client/src/metricsWording.ts`, `client/src/index.ts`, `mcp-core/src/index.ts`, `cli/src/metricsOutput.ts`, `skills/lighthouse-daily-flow-review/` | MODIFY |
| `docs/product/architecture/adr-240-…md`, `brief.md` section | NEW / EXTEND |

## Wave: DESIGN / [REF] Driving and driven ports

- Driving: the two WIP HTTP reads (+ `asOfDate`); the sync pipeline (`WorkItemService` update cycle).
- Driven: none new. The in-process domain event dispatcher (existing) for `WorkItemBecameStale`.

## Wave: DESIGN / [REF] Loading and failure looks

**N/A, because** no view is new and none fetches anything new: the Stale signals arrive on the WIP read the widgets
already wait for, under their existing `WidgetShell` status. A slow or failed WIP read looks exactly as today.

## Wave: DESIGN / [REF] Technology

No change: .NET 10 / EF Core, React 18 + TypeScript, clients TypeScript. OOP backend per `CLAUDE.md`.

## Wave: DESIGN / [REF] Open questions for DISTILL / DELIVER

- Pin: an omitted `asOfDate` binds `DateTime.MinValue` and returns an empty snapshot (`TeamMetricsController.cs:140`); a future `asOfDate` is not clamped. Pin current behaviour; do not change it here.
- Verify the WIP query is refetched after a thresholds save (else a threshold edit shows late, weakening ADR-026's DDD-8).
- Which released Lighthouse version the clients' `staleness` gate names — set when slice 01 ships.

## Wave: DESIGN / [REF] Changed Assumptions

- DISCUSS US-03 says the skill uses "the instance's term for Stale". **There is no Terminology key for Stale**
  (`TerminologySeeder.cs` has `blocked`, not `stale`; clients `terminology.ts` likewise). New assumption: the literal
  "Stale", as the dashboard shows it (`BaseMetricsView.tsx:706`). No user-visible change from today.
- DISCUSS D1 left "any other read the UI shows a Stale badge on" to DESIGN: the inventory finds none beyond the two
  WIP reads (DDD-4/5).

## Wave: DESIGN / [REF] Handoff

DEVOPS skipped by the maintainer (2026-10-16) → DISTILL.

---

## Wave: DISTILL / [REF] Run log

- `[lang-mode]` C# (.NET 10, NUnit 4) backend + TypeScript (Vitest + RTL) frontend; clients (TypeScript,
  Vitest) as a spec only. `[policy-mode]` inherit — `docs/architecture/atdd-infrastructure-policy.md`
  already covers every port in scope (WebApplicationFactory, real EF, licence mock; the Faster Updates
  harness for the refresh: connector mock, capturing handler on the real dispatcher; RTL with held
  services). No row added. `[port-mode]` n/a — the policy records that the Python state-delta port does
  not apply to this C#/TS project.
- **DEVOPS artifacts missing** (skipped by the maintainer, 2026-10-16) — default environment matrix
  (clean / with-pre-commit / with-stale-config) applies; none of the three changes a scenario here, since
  no install, hook or config file is involved. Warned, not blocked.
- **Reconciliation passed — 0 contradictions** across DISCUSS and DESIGN (DEVOPS absent). DESIGN's
  "Changed Assumptions" (no Terminology key for Stale; no third read carries the flag) refine DISCUSS
  rather than contradict it.

## Wave: DISTILL / [REF] Maintainer decision — UI sketches

**UI sketches: none needed — no new look; maintainer confirmed 2026-10-16.** The badge, its tooltip
format (`Blocked 6d`, `12d in Review`), the aging chart's red dot and the Stale count keep exactly the look
they have; only where the answer comes from changes. DELIVER re-sketches nothing.

## Wave: DISTILL / [REF] Scenario list with tags

Slice 01 — backend, `Lighthouse.Backend.Tests/API/Integration/Staleness/Slice01ServerMarksStaleScenarios.cs`
(driving port: the Team and Portfolio WIP reads):

| Scenario | Tags |
|---|---|
| A flow coach reads which Work Item has sat too long in Review and why | `@walking_skeleton @driving_port @real-io @us-01 @contract-shape:bounded-change` |
| A Work Item goes stale once it has sat in its state longer than the threshold (5 → no, 6 → yes) | `@driving_port @us-01 @AC-1 @boundary` |
| A Blocked Work Item goes stale on the day it reaches the blocked threshold (2 → no, 3 → yes) | `@driving_port @us-01 @AC-2 @boundary` |
| A Blocked Work Item under the blocked threshold is not stale however long it has sat in its state (Blocked 2 days, 9 in state: context reason only) | `@us-01 @AC-2 @error` |
| A Blocked Work Item is stale for its blocked time and still shows how long it has sat in its state | `@us-01 @AC-2` |
| A Work Item still Blocked in its state is not stale before the blocked threshold, even past the time-in-state one | `@us-01 @AC-2 @error` |
| With both thresholds off no Work Item is stale and none carries a reason | `@us-01 @AC-3 @error` |
| Turning the time-in-state trigger off leaves the blocked trigger working | `@us-01 @AC-3 @error` |
| Turning the blocked trigger off leaves the time-in-state trigger working | `@us-01 @AC-3 @error` |
| A Work Item is judged as of the day the read asks about, not today (chained: day 5, day 6) | `@us-01 @AC-4` |
| A Work Item whose state entry Lighthouse never saw is not stale | `@us-01 @edge` |
| A Feature goes stale once it has sat longer than the Portfolio's threshold (5 → no, 6 → yes) | `@us-01 @AC-5 @boundary` |
| A Blocked Feature is stale for its blocked time against the Portfolio's blocked threshold | `@us-01 @AC-5` |
| The Blocked items read says nothing about staleness — **active, green** | `@us-01 @error @contract-shape:unbounded-preservation` |
| A WIP read without a day answers with no Work Items — **active, green; pins today's behaviour** | `@us-01 @edge` |

Slice 01 — the refresh signal, `…/Staleness/Slice01StaleSignalScenarios.cs` (driving port: the scheduled
Team refresh through the production update queue; observable: `WorkItemBecameStale` on the real dispatcher):

| Scenario | Tags |
|---|---|
| A refresh signals a Work Item going stale once, with the threshold it crossed (chained: second refresh is silent) | `@driving_port @us-01 @AC-6` |
| A refresh does not signal a Work Item that has sat exactly as long as the threshold | `@AC-6 @boundary @error` |
| A refresh does not signal a Blocked Work Item still under the blocked threshold | `@AC-6 @error` |
| A refresh signals a Blocked Work Item with the blocked threshold it crossed | `@AC-6` |
| A refresh never signals staleness while both thresholds are off | `@AC-6 @error` |

Slice 01 — the rule at its boundaries, `Lighthouse.Backend.Tests/Models/Staleness/StalenessPolicyTest.cs`
(unit, layer 1; 12 tests, 22 cases): time in state `>` (5/6, 1/2); blocked `≥` (2/3/4); Blocked precedence
with both reasons; context reason only; threshold 0 off (3 cases) and blocked-off context reason; only
Doing (To Do, Done, Unknown); unknown entered day; unknown blocked start; days on the instance calendar
(22:30 UTC is the next day in Zurich); a block starting on or after the day asked about counts 1 day;
thresholds read from the owner. This is the coverage `deriveStaleness.test.ts` carries today, so that file
can go at DELIVER.

Slice 01 — architecture, `Lighthouse.Backend.Tests/Architecture/StalenessPolicyPurityArchUnitTest.cs`
(**active, green**): the policy exists under its own name; `Models.Staleness` depends on nothing in
`Services`, `API` or `Data`.

Slice 02 — frontend (Vitest + RTL, `it.skip` / `test.skip`):

| File | Scenario |
|---|---|
| `components/Common/TimeInStateBadge/TimeInStateBadge.test.tsx` | is red when Lighthouse marks the Work Item stale, even with no threshold given |
| ″ | is not red when Lighthouse says the Work Item is not stale, however long it has sat (`@error`) |
| ″ | is not red when Lighthouse does not say, as an older Lighthouse does not (`@error`) |
| ″ | a Blocked Work Item not yet stale stays un-red even though Lighthouse names its time in state (`@error`) |
| ″ | its tooltip gives Lighthouse's reason — `Blocked 6d`; `12d in Review`; `Blocked 6d; 12d in Review` (3 cases) |
| `components/Common/Charts/WorkItemAgingChart.test.tsx` | reds the dot of a Work Item Lighthouse marks stale, however recently it moved |
| ″ | does not red the dot of a Work Item Lighthouse says is not stale, however long it has sat (`@error`) |
| ″ | does not red the dot of a Work Item an older Lighthouse said nothing about (`@error`) |
| `pages/Common/MetricsView/BaseMetricsView.test.tsx` | the Team's Stale count is the number of Work Items Lighthouse marks stale (`@kpi OUT-stale-agrees`) |
| ″ | the Portfolio's Stale count is the number of Features Lighthouse marks stale (`@kpi OUT-stale-agrees`) |
| ″ | the aging chart's health counts a Work Item Lighthouse marks stale as stale |
| ″ | the aging chart's health does not call stale a Work Item Lighthouse says is not stale (the past-range case, `@error`) |
| `utils/oneStalenessRule.enforcement.test.ts` | the browser's copy of the rule is gone (`@kpi OUT-one-stale-rule`) |
| ″ | nothing in the app compares days with a staleness threshold (`@kpi OUT-one-stale-rule`) |
| ″ | nine cases on the guard's own pattern — **active, green** |

Slice 03 — clients: 18 scenarios in `distill/slice-03-clients-scenarios.md`, with the wire-shape fixture
`distill/wip-read-with-staleness.json`, for DELIVER to write into `lighthouse-clients`.

Counts: slice 01 — 20 acceptance scenarios (23 runnable cases; 21 pending, 2 active) + 12 policy unit
tests (22 cases, pending) + 2 architecture rules (active); slice 02 — 14 scenarios (16 pending cases) +
9 active guard cases; slice 03 — 18 specified. Error/edge share: slice 01 acceptance 11 of 20 (55%),
slice 02 6 of 14 (43%), slice 03 8 of 18 (44%).

## Wave: DISTILL / [REF] WS strategy

One walking skeleton for the feature: *A flow coach reads which Work Item has sat too long in Review and
why*, through `GET /api/latest/teams/{id}/metrics/wip?asOfDate=…` on the real host. The Architecture of
Reference treatments apply (real host and EF, licence faked).

**Deviation, recorded on purpose:** the nWave rule is that the walking skeleton is green at DISTILL
hand-off. Here it cannot be: the skeleton *is* the feature (no read says `isStale` until the policy is
written), and this project's practice is that every not-yet-passing acceptance test is skipped and the
hand-off is green. The skeleton is therefore `[Ignore]`d with the rest and **becomes green at DELIVER
step 1** — un-skipping it is that step's RED, and the first slice-01 commit makes it pass.

Tier B (state-machine PBT): not declared. The journey is one read and one refresh with a closed input
space (two thresholds, a day count, blocked or not) that the boundary tables cover; FsCheck is not
referenced by the test project.

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | Real I/O scenario | Covered by |
|---|---|---|
| EF `LighthouseAppContext` + `IWorkItemRepository`, `IRepository<Feature>`, `IWorkItemBlockedTransitionRepository`, `IFeatureBlockedTransitionRepository` | YES | every slice-01 WIP-read scenario (seeded through the real repositories, read back through the real controllers) |
| `IWorkItemStateTransitionRepository` (where a refresh derives the day a Work Item entered its state) | YES | the five refresh-signal scenarios |
| `IDomainEventDispatcher` (real; capturing handler added beside the production ones) | YES | the five refresh-signal scenarios |
| `IWorkTrackingConnector` | faked by policy (external) | refresh-signal scenarios, through `FasterUpdatesAcceptanceTest` |
| `ILighthouseClock` | real | the WIP reads take the day from `asOfDate`; the refresh scenarios count days from the real clock's today |

No new driven adapter: zero "MISSING" rows.

## Wave: DISTILL / [REF] Scaffolds (RED-ready, `// SCAFFOLD: true`)

| File | What it is |
|---|---|
| `Lighthouse.Backend/Models/Staleness/StalenessPolicy.cs` | `StalenessCandidate` record; `StalenessPolicy.For(owner)` (reads the two thresholds); `Evaluate(candidate, zone, asOfDay)` **throws `InvalidOperationException("Not yet implemented -- RED scaffold …")`** |
| `Lighthouse.Backend/Models/Staleness/StalenessVerdict.cs` | `StalenessVerdict(IsStale, Reasons)`, `StaleReason(Kind, Days, StateName?)` |
| `Lighthouse.Backend/Models/Staleness/StaleReasonKind.cs` | `TimeInState`, `BlockedDuration`, `TimeInStateWhileBlocked` |
| `Lighthouse.Backend/API/DTO/StaleReasonDto.cs` | `{ kind, days, stateName? }` — `stateName` omitted when null |
| `Lighthouse.Backend/API/DTO/WorkItemDto.cs` (extended, not marked) | `IsStale: bool?`, `StaleReasons: StaleReasonDto[]?`, both `[JsonIgnore(WhenWritingNull)]`; `CarryStaleness(verdict)` — the narrow setter. Nothing calls it yet, so every read is byte-for-byte unchanged |
| `Lighthouse.Frontend/src/models/WorkItem.ts` (extended) | `isStale?`, `staleReasons?`, `StaleReasonKind` string union, `IStaleReason` |
| `Lighthouse.Frontend/src/components/Common/TimeInStateBadge/TimeInStateBadge.tsx` (marked on the props) | optional `isStale` / `staleReasons` props, accepted and not read yet — skipped tests are type-checked, so the props had to exist |

Scaffold exception choice: C# has no `AssertionError`, and NUnit's `AssertionException` is a test-framework
type that production code may not reference, so the throwing body uses `InvalidOperationException` with
one `const` message (S1192) that interpolates the instance's thresholds (S2325 / S4487 — a scaffold that
does not touch instance state does not build here). Only `Evaluate` is logic; `For`, the records and the
DTO mapping are plain data and are written in full. None of it changes runtime behaviour today.

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Backend.Tests/API/Integration/Staleness/` — new folder beside `BlockedItems/` and
  `FasterUpdates/`, same partial-class `*Scenarios.cs` + `*Specifications.cs` split. `StaleWorkItemsAcceptanceTest`
  is the harness (real host, seeding, read helpers); the refresh scenarios inherit
  `FasterUpdatesAcceptanceTest`, which already carries the update queue and the event capture.
- `Lighthouse.Backend.Tests/Models/Staleness/` mirrors `Models/Staleness/` (precedent: `Models/InstanceCalendarTest.cs`).
- `Lighthouse.Backend.Tests/Architecture/` for the ArchUnitNET rule (precedent: the `*PurityArchUnitTest` files).
- Frontend tests sit in the existing test file of each component they drive, inside the describe that
  already holds its fixtures and mocks; the guard sits in `src/utils/` (not `utils/staleness/`, which
  DELIVER deletes). Precedent: `*.enforcement.test.ts` files.
- No Playwright spec: every surface's wiring is proven at a cheaper level — the WIP read through the real
  host, each widget through RTL against a held service — and the one thing E2E would add, that the
  dashboard calls the WIP read, is unchanged by this story.

## Wave: DISTILL / [REF] Driving adapter coverage

| Entry point (DESIGN) | Scenario through its protocol |
|---|---|
| `GET /api/latest/teams/{id}/metrics/wip?asOfDate=` | walking skeleton + 11 Team scenarios (HTTP, real host) |
| `GET /api/latest/portfolios/{id}/metrics/wip?asOfDate=` | 2 Portfolio scenarios (HTTP, real host) |
| `GET /api/latest/teams/{id}/metrics/blockedItemsAtDate` (must not carry it) | 1 active scenario (HTTP) |
| The Team refresh (`ITeamUpdater.TriggerUpdate` → update queue → `WorkItemService`) | 5 refresh-signal scenarios |
| Flow Overview / aging chart / Time-in-State badge | 12 RTL scenarios (slice 02), plus the 2 source guards |
| `lh metrics team --metrics wip`, MCP `lighthouse_team_metrics_wip`, DFR skill | slice-03 spec (`runCliSession`, `createLocalLighthouseMcpServer`, eval cases) |

## Wave: DISTILL / [REF] Pre-requisites and notes for DELIVER

- Driving ports and wire names: DESIGN DDD-4, DDD-6. `stateName` is left off the wire when null
  (Blocked time belongs to no state); DESIGN wrote `stateName?`, this pins "omitted" rather than `null`.
- Reason order on the wire: the reason that makes the item stale first, the context reason after it
  (`BlockedDuration`, then `TimeInStateWhileBlocked`) — the order `deriveStaleness` uses today, which the
  tooltip joins with `; `.
- Live reads take blocked-since from the day the item entered its current state, as today, so on a
  current read a Blocked item's blocked days equal its days in state. The scenarios that need the two
  apart ("Blocked 2 days, 9 in state") seed a recorded blocked spell and read a past day.
- Pinned, unchanged: an omitted `asOfDate` binds `DateTime.MinValue` and answers with no Work Items.
- `BaseMetricsView` keeps its two threshold props: `hasStaleConfig` and the Blocked RAG still read them.
  The slice-02 tests pass them for that reason; the badge and chart tests pass none, because DELIVER drops
  those props there.
- The refresh-signal pair *fires once* / *not at exactly the threshold* pass on today's rule at some
  times of day and not others (fractional days from `UtcNow`); they stay pending until the new rule makes
  them deterministic. See `distill/red-classification.md`.
- Not done here, owed to DELIVER: delete `utils/staleness/*` (its tests included) once slice 02 is green;
  check the WIP query is refetched after a thresholds save (DESIGN open question); set the clients'
  `staleness` gate when slice 01 is released.
- Pre-existing, not from this change: `dotnet build` reports 2 `NU1903` package-audit warnings
  (`Microsoft.AspNetCore.SignalR.Common` 10.0.12) — NuGet restore advisories, not compiler warnings.

## Wave: DISTILL / [REF] Self-completeness and mandate notes

- AT-completeness (15-item check): 13/15 — COMPLETE. Gaps, both documented: no concurrency matrix (one
  refresh at a time is the product's own guarantee; nothing here races) and no environment matrix
  (DEVOPS skipped; default matrix does not touch these scenarios). No `SPECIFICATION_AMBIGUITY` found.
- Mandate 8 (state-delta): per the project policy the Python port does not apply; the "nothing else
  changed" half is asserted explicitly where it matters (the Blocked read says nothing; the refresh
  raises nothing when it should not, with a positive control that the Work Items were still there).
- Mandate 12: domain types `StalenessThresholds`, `ReasonSeen`, `StalenessSeen`, `StaleReasonKind`;
  step bodies delegate to the harness in one or two statements. Step-reuse ratio (informational):
  97 step invocations / 24 step methods ≈ 4.0× on the backend scenarios.
