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
