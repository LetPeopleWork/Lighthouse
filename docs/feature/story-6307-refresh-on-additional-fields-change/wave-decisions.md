# Wave Decisions — story-6307-refresh-on-additional-fields-change

ADO User Story #6307 (Active, tagged Release Notes, no parent Epic). Split out of Bug #6306.

# DIVERGE Decisions -- story-6307-refresh-on-additional-fields-change

DISCOVER was explicitly skipped by the maintainer; the problem is grounded in Bug #6306 and in the code on
`main` (`17428144f`). Brownfield, lightweight research. 2026-10-15.

## Key Decisions

- [D1] **The full fetch is already owed; only its timing and visibility are open.** A connection's additional
  field definitions are in the fetch fingerprint, so every Team and Portfolio on it takes one full fetch at its
  next cycle (up to ~4 h away with default settings). Options are judged on *when* and *whether visible*, not on
  *whether to pay*. (see: diverge/job-analysis.md E6, E7)
- [D2] **Cost outcomes are constraints, not targets.** "One full fetch per owner per edit" and "no fetch for a
  credential or name edit" are already served; any option that spends them is a regression.
  (see: diverge/job-analysis.md §7)
- [D3] **Field-only backfill and re-derivation from stored payloads eliminated at DVF.** The first needs a new
  capability in every remote connector and a partial-write mode the architecture rules out; the second needs a raw
  payload stored per item that would not contain the new field anyway. (see: diverge/taste-evaluation.md §1)
- [D3a] **"Update on save", "ask on save" and "next scheduler tick" are one option** (same fetch, same owners, same
  predicate); the moment is a sub-decision for DISCUSS. (see: diverge/options-raw.md, peer review round 1)
- [D4] **Recommend, conditionally: update the connection's Teams and Portfolios on save**, triggered by "this
  owner's fingerprint moved", reusing `TriggerUpdate` and the sequential, coalescing update queue; replace the
  "next refresh" Alert. Condition: the maintainer explicitly sets ADR-030's "expensive ⇒ intentional" aside for
  connection saves (the Team-settings save already auto-updates its owner, so both rulings exist today) and
  records it as an ADR-030 amendment. (see: recommendation.md)
- [D5] **If that condition is refused: the "waiting for an update" marker with one click (Option 2)** — the
  highest-scoring option consistent with both ADR-030 and ADR-140. (see: recommendation.md)
- [D6] **ADR-140's partial-failure question answered with existing machinery:** the save fans out enqueues, not
  writes; each owner fails, records and retries on its own, because its fingerprint is written only after its own
  fetch completes. DISCUSS still has to accept a save-time fan-out explicitly. (see: diverge/taste-evaluation.md §0)

## Job Summary

- Validated job: When I add or change a field on a work tracking connection, I want every Team and Portfolio that
  reads through it to hold that field's values before I build on it, so I can put a rule on the field and trust
  what it matches. (Physical: close, or make visible, the gap between a configuration change and the data it
  governs.)
- ODI outcomes: 6 outcome statements (O1–O6); under-served O2, O1, O3, O4; O5 and O6 over-served (constraints).

## Options Evaluated

- 6 options curated from 6 mechanism families (7 SCAMPER + 4 Crazy 8s), 4 survived the DVF filter.
- Recommended (conditional on the ADR-030 ruling): **Update the connection's Teams and Portfolios on save**
  (4.25) — closes the time gap and the forgotten step with no new concept, using machinery that exists.
- Dissent by score: **The rule that needs the field asks for it** (3.73) — less load up front, but every other
  reader of the field still waits silently, and it carries the same ADR-030 conflict.
- Dissent by principle: **"Waiting for an update" marker with one click** (3.40) — the answer if "the admin
  chooses when an expensive update runs" is a product principle.

## SSOT Updates

- jobs.yaml: updated with job `job-config-admin-see-a-connection-field-change-take-effect` (complements
  `job-config-admin-know-which-settings-cost-a-refetch`); `feature_context` and `updated` bumped.
- personas/config-admin.yaml: new job id added to `primary_jobs`, beside its sibling.

## Peer review

- See `diverge/review.yaml`.

# DISCUSS Decisions -- story-6307-refresh-on-additional-fields-change

Luna (nw-product-owner), 2026-10-15. Brownfield, lightweight, lean density. JTBD not re-run (job validated by
DIVERGE). Full detail: `feature-delta.md` (`## Wave: DISCUSS / [REF] …`).

## Scope Assessment: PASS — 3 stories (1 conditional), 2 contexts, estimated ~2 days

One Story, no Epic split: three slices of a day or less (6 h, 5 h, 3 h), each releasable alone.

## Key Decisions

- [D1] **Connection save follows save-then-update** — maintainer, 2026-10-15. ADR-030 amended or overruled for
  this case; ADR-140's passive fan-out consequence amended. DESIGN writes the ADR text.
- [D2] **Immediately on save**, through the existing queue; no confirmation by default — maintainer, 2026-10-15.
- [D3] **Label-only rename still costs a full fetch**; fingerprint unchanged — maintainer, 2026-10-15.
- [D4] **Every licence**, not premium-gated — maintainer, 2026-10-15.
- [D5] **No large-instance threshold** — maintainer, 2026-10-15.
- [D6] **Admin sees what the save started**; a dialog may offer "save without updating" (next scheduled update
  picks it up, copy says so). Placement/copy sketched at the start of DISTILL — maintainer, 2026-10-15.
- [D7] **Rules read empty until each update finishes**; "rules tell the truth" out of scope, not a follow-up —
  maintainer, 2026-10-15.
- [D8] Queue only owners whose fingerprint **this save** moved (DISCUSS; computation → DESIGN Q1).
- [D9] One update per owner per save; existing coalescing (DISCUSS).
- [D10] Save stands alone; per-owner failure; refused save queues nothing (DISCUSS).
- [D11] Predefined-field registration on connection GET does not fan out — proposed, DESIGN confirms (Q2).

## Requirements Summary

- Primary job: `job-config-admin-see-a-connection-field-change-take-effect`.
- Stories: US-01 field change updates its owners (slice 01), US-02 save says what it started (slice 02), US-03
  save now, update later (slice 03, conditional on the DISTILL sketch picking the dialog).
- Walking skeleton: strategy B (extend existing), none needed.
- Feature type: user-facing (backend fan-out + save message on the connection page).

## Constraints Established

- One full fetch per owner per edit (O5); no fetch for a non-shaping save (O6); no new permission; no premium gate.

## Upstream Changes

- DIVERGE flagged "rules tell the truth" as a possible follow-up; the maintainer closed it as out of scope (D7).
- DIVERGE's timing variants resolve to "now" by default with an optional "later" (D2, D6).

## Hand-offs

- DESIGN: Q1–Q5 in `feature-delta.md`; ADR-030/ADR-140 amendments.
- DEVOPS: usage-data candidate `ConnectionFieldChangeSaved` (`updates: now | later`, or name-only if US-03 drops).
- DISTILL (start): sketch items UI-1…UI-5.

# DESIGN Decisions -- story-6307-refresh-on-additional-fields-change

Morgan (nw-solution-architect), 2026-10-15. Scope application, PROPOSE, OOP, lean. **Every decision is proposed,
pending maintainer approval.** Full detail: `feature-delta.md` (`## Wave: DESIGN / [REF] …`, DDD-1…DDD-15).

## Key Decisions

- [DDD-1] "Moved by this save" = the connection's share of the fetch fingerprint, rendered before the merge and after
  `Save()`; moved ⇒ every owner on the connection, else none (Q1).
- [DDD-2] That share is one pure `FetchFingerprint.ForConnection`, used by `For` itself; `For`'s digest stays
  byte-identical (pinned-digest test first); the property guard gains a connection-scoped subset.
- [DDD-3] Fan-out on the domain-event bus: the PUT publishes `ConnectionFetchShapeChanged(ConnectionId, TeamIds,
  PortfolioIds)`; a handler triggers each owner through the existing updaters (Q1).
- [DDD-4] The connection controller publishes (sixth ctor parameter, under S107).
- [DDD-5] D11 confirmed: GET and Jira sync-path registration never publish (Q2).
- [DDD-6] SUPERSEDED by S3/S4 (2026-10-15): the PUT response is unchanged, no `queuedUpdates`.
- [DDD-7] SUPERSEDED by S3/S4 (2026-10-15): no after-save notice; counts are asked before Save, no new fetch.
- [DDD-8] Opt-out = `?startUpdates=false` (default true). Confirmed by S1: ships.
- [DDD-9] The question shows when the page's field definitions differ from those loaded and `usedBy` > 0; counts
  from `usedBy` on the connection **list** read (amended 2026-10-15).
- [DDD-10] Queue coalescing reused unchanged ⇒ AC-1.5 reworded (upstream change UC-1).
- [DDD-11] Teams enqueued before Portfolios.
- [DDD-12] No licence gate or RBAC change; the "Update All" premium gate is an endpoint attribute the fan-out never hits.
- [DDD-13] SUPERSEDED by S3/S4 (2026-10-15): navigate to `/` as today, no notice.
- [DDD-14] New ADR-239; ADR-030 and ADR-140 carry dated amendment notes (Q3).
- [DDD-15] One Information log line per fan-out (ids and counts, no field names).
- [DDD-16] A stored save answers 200 even if the owner lookup or a handler fails after `Save()` (added 2026-10-15).
- [DDD-17] `usedBy { teams, portfolios }` on the connection list read, through one read-only usage port (S107)
  (added 2026-10-15).

## Architecture Summary

- Pattern: modular monolith, ports-and-adapters, in-process domain events (ADR-027) — unchanged.
- Paradigm: OOP.
- Key components: `FetchFingerprint` (EXTEND), connection PUT (EXTEND), `ConnectionFetchShapeChanged` + handler
  (CREATE), connection-usage read port (CREATE), list GET + connection DTO `usedBy` (EXTEND), frontend connection
  service / `EditConnection` / `ModifyConnectionSettings` + update question dialog (EXTEND).

## Reuse Analysis

See `feature-delta.md` → "Wave: DESIGN / [REF] Reuse Analysis". 3 CREATE NEW (event record, handler, connection-usage
read port; the response DTO was dropped after S3), each justified; everything else EXTEND or REUSE AS-IS.

## Technology Stack

- No new technology, package, migration, setting or permission.

## Constraints Established

- Publish only after a successful `Save()`; only from the PUT.
- `FetchFingerprint.For` output must not change (stored fingerprints survive the upgrade).
- The question before Save reads only `usedBy` from the list read the page already makes.

## Upstream Changes

- UC-1: AC-1.5 and its scenario reworded to "no owner downloads everything twice because of one save; one Task
  Manager row each" (`design/upstream-changes.md`).

## Hand-offs

- DEVOPS: usage-data event (settled by S7); no Lighthouse-Clients change (PUT shapes unchanged); `Program.cs` edit ⇒
  full backend Integration suite in CI.
- DISTILL: UI-1…UI-5 settled by S1–S7; UC-1 accepted.

# DEVOPS Decisions — story-6307-refresh-on-additional-fields-change

## Key Decisions
- [DO-1] No infrastructure, CI, migration or rollout change; `Program.cs` edit triggers the full backend Integration suite in CI (see: feature-delta.md, DEVOPS / Platform and delivery).
- [DO-2] Usage-data event `WorkTrackingSystemFieldsChanged = 19` with closed enum `Updates: Now | Later`, browser-emitted when the admin answers the update question after a successful save; emitted in slice 03 (see: feature-delta.md, DEVOPS / Usage-data event; revised after the final review gate).
- [DO-3] No Lighthouse-Clients release: PUT response is typed `unknown` there.
