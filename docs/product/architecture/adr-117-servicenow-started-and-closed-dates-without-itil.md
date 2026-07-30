# ADR-117: ServiceNow started and closed dates come from the record, so slice-02 metrics measure request-to-resolution rather than time-in-progress

- **Status**: **Accepted** (ratified 2026-07-30 by the maintainer). The open question below is
  deferred to slice 04's DESIGN, which must answer it for *both* cases — with and without `itil`.
- **Date**: 2026-07-29
- **Feature**: epic-5513-servicenow-integration (ADO Epic 5513, Story 5575)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Supersedes nothing. Constrains**: slice 02 scope, slice 04 scope.

## Context

Slice 02's brief carried an explicit stop condition:

> Pre-slice SPIKE **Mandatory** — Q2/Q3/Q4/Q7. If Q4 reveals no started date, **stop and re-plan**:
> cycle time then depends on slice 04, and this brief's scope is wrong.

**Q4 revealed exactly that.** `work_start` stays empty after a real API-driven transition with business
rules firing (`spike/findings.md`). There is no trustworthy started-timestamp on the record itself.

Q6 found the real source: `metric_instance` yields one row per state span with `start` / `end` /
`duration`, so the started time is the `start` of the first span mapping to a Doing state. But the Q8
role matrix measured `metric_instance` and `metric_definition` at **403 for every read-only role**,
opening only at `itil` / `itil_admin` / `metric_admin` — all fulfiller-grade.

That collides with what slice 01 established and the live integration tests now pin: a **read-only**
account (`sn_incident_read` and its per-table siblings) is sufficient to connect and read ITSM tables.
That least-privilege story is the epic's best adoption argument. Requiring `itil` for Cycle Time means
asking a customer's platform team to escalate the integration account from read-only to fulfiller.

### What the record actually carries (measured 2026-07-29, live PDI, closed incident)

| Field | Value | Readable read-only? |
|---|---|---|
| `opened_at` | `2026-07-09 02:46:49` | yes |
| `sys_created_on` | `2026-07-29 06:46:49` | yes |
| `resolved_at` | `2026-07-29 07:25:29` | yes |
| `closed_at` | *empty* (state 6 = Resolved) | yes |
| `work_start` / `work_end` | *empty* | yes |
| `business_duration` | `1970-01-05 16:00:00` (Glide duration = 4d 16h) | yes |
| `calendar_duration` | `1970-01-21 04:38:40` | yes |

Two things fall out. `opened_at` is a real, settable timestamp distinct from `sys_created_on` — the
seeder backdates it, and so does a customer importing history. And **`closed_at` is empty on Resolved**:
only state 7 (Closed) populates it.

## Decision

**1. `ClosedDate` = `resolved_at`, falling back to `closed_at`.**

Keying on `closed_at` alone would silently drop every resolved-but-not-closed incident from Throughput
— the same denial-in-a-success-costume shape as the epic's headline bug, and just as invisible. Many
ITSM shops never move records past Resolved.

**2. `StartedDate` = `opened_at`, falling back to `sys_created_on`.**

**3. The resulting metric is request-to-resolution, and Lighthouse says so.** For ServiceNow without
`itil`, the span Lighthouse reports is when the request arrived until it was resolved — MTTR-shaped,
which is the measure ITSM organisations already run their service desks on. It is **not** time-in-progress
and must not be presented as though it were.

**4. True time-in-Doing is slice 04's**, derived from `metric_instance`, and carries a documented
role-escalation cost.

## Consequences

**Good.** Throughput and the "how many by date" forecast work on a read-only account, which is the
epic's minimum shippable value. The metric produced is the one ITSM audiences already understand.
Slice 02 stays roughly its estimated size instead of absorbing slice 04.

**Bad, and this is the real cost.** The span includes queue time. A shop that maps `New → ToDo` and
`In Progress → Doing` will see a number larger than the time their people spent working, and Lighthouse
cannot tell them by how much until slice 04. If that number is displayed under a label that reads
"Cycle Time" with no qualification, Lighthouse is overstating — quietly, plausibly, in precisely the way
this epic exists to prevent. **The honesty obligation is therefore load-bearing, not cosmetic**: the
per-connector wording is part of this decision, not a docs afterthought.

**Also.** Two ServiceNow deployments can produce different numbers for the same work depending on
whether their records are opened at request time or at triage time. That is inherent to the field and
worth stating in the docs.

## Alternatives considered

**A. Require `itil` and read `metric_instance` in slice 02** (pull slice 04 forward). Gives a true
cycle time immediately. Rejected: it makes fulfiller-grade access a day-one requirement for every
ServiceNow customer, discarding the adoption story slice 01 just proved, and it inflates slice 02 with
Glide-duration parsing and partial-history handling. Deferred rather than dismissed — this is slice 04.

**B. Leave `StartedDate` null when no transition history is available.** The most conservative option:
Throughput works, Cycle Time simply has no data. Rejected because it discards a real signal the record
does carry, and because an empty chart is its own kind of unexplained failure. Reconsider if the
labelling in decision 3 proves impossible to land clearly in the UI.

**C. Follow the Azure DevOps precedent — `startedDate = closedDate` when undeterminable**
(`AzureDevOpsWorkTrackingConnector.GetStartedAndClosedDateForWorkItem`). Biases the other way: cycle
time collapses toward zero, obviously wrong rather than plausibly wrong. Rejected because for ServiceNow
this is not an edge case fallback — it would be the *normal* path for every read-only customer, and
systematically reporting near-zero cycle times is not more honest than reporting inflated ones.

## Open question for ratification

Whether decision 3's honesty obligation is met by per-connector terminology, a UI annotation, a docs
statement, or all three. The DISCUSS wave owns Lighthouse's terminology surface; this ADR asserts only
that *something* user-visible must carry it, and that shipping the number unqualified is not acceptable.
