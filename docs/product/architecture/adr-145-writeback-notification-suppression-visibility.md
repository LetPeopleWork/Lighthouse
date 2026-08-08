# ADR-145: Write-back suppression capability is reported per Jira project, probed on demand, and never stored

**Status**: **Accepted** (2026-08-08 — discovery shape ratified by the user: option S2, probe-on-demand)
**Date**: 2026-08-08
**Feature**: `quiet-jira-writeback` (ADO Epic #5500 "Quiet write-back", slice 05 / Story #5506)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Evidence**: SPIKE-03 Q6 and "Requirements for a silent write-back",
`docs/feature/quiet-jira-writeback/slices/slice-03-spike-jira-notification-suppression.md`

---

## Context

`job-config-admin-know-writeback-is-quiet` — *"See upfront whether Lighthouse can actually be quiet with
the credential I gave it"* — importance 3, satisfaction 0.

SPIKE-03 changed what the answer to that question has to look like. Two findings:

**The permission is per Jira project; a Lighthouse connection is not.** The credential needs, for **every
project it writes into**, either Administer Jira (global) or Administer Projects (that project). A
connection writing into five projects where two lack it is silent in three of them and noisy in two. A
connection-level boolean would therefore be wrong in both directions — it would either claim quiet while
two projects mail their watchers, or claim noisy and send the administrator to grant a permission in the
wrong place. The spike states this explicitly: *"a single connection-level yes/no would be wrong."*

**The probe over-reports if it is asked carelessly.** `GET /rest/api/3/mypermissions?permissions=ADMINISTER_PROJECTS`
called **without** `projectKey` returns `havePermission: true` at **HTTP 200** — it does not 400. With
`projectKey` it is accurate in both directions (`SPIKEPRM` → `false` → observed 403; `DUMMY` → `true` →
observed 204). The probe is trustworthy exactly when it cannot be asked without project context.

A third finding bounds how often this bites. Every pre-existing project on the test site granted
`ADMINISTER_PROJECTS` to holder type `applicationRole` — any licensed user — so suppression just works
there; only a freshly created team-managed project bound a scheme without that grant. So the failure is
probably rarer than the community report suggests, and entirely a function of one permission-scheme grant
per customer. It cannot be assumed either way.

Two pieces of the surface DISCUSS assumed exist do **not**. `ConnectionValidationResult` has no `Advisory`
field and no `SuccessWith` factory — the ADR-127 advisory channel was removed by Story #5612's merge, so
there is no shipped mechanism for "valid, but with a caveat". And the ADO-parity argument does not apply:
ADO suppresses unconditionally and needs no such surface (D8).

## Decision

### 1. The unit of the verdict is the **Jira project**, not the connection

The stored/reported shape is a set of `(projectKey, verdict)` where verdict is `Suppressed`,
`NotSuppressed` or `Unknown`. The connection-level rollup is **derived**, never authored:

| Rollup | Condition |
|---|---|
| `Quiet` | every known project is `Suppressed`, none `Unknown` |
| `PartiallyNoisy` | at least one `NotSuppressed` and at least one `Suppressed` |
| `Noisy` | every known project is `NotSuppressed` |
| `Unknown` | nothing known, or the discovery failed |

The surface names the affected projects. An administrator must be able to read "grant Administer Projects
on `SPIKEPRM` and `OPS`" off the screen, not "something somewhere is noisy".

### 2. Project keys are derived from the reference id, with an honest fallback

Jira work items carry `ReferenceId = issue.Key` (`JiraWorkTrackingConnector.cs:1030`), and a Jira issue key
is `<PROJECTKEY>-<number>` where the project key contains no hyphen. The project key is the substring
before the last `-`. No extra API call, no new stored field.

**A reference id that does not match that shape is grouped as `Unknown` and reported as unknown** — never
silently dropped and never folded into a neighbouring project's verdict. This is the only place the design
takes a convention on trust, so it is the place that must degrade visibly.

### 3. No request is ever issued without project context

The port method takes the project keys as a required argument and returns one verdict per key. A required
parameter does not, on its own, prevent an *empty* collection being passed — so the rule that closes the
Q6 trap is the behavioural one beside it: **an empty collection returns an empty result and issues no
request at all.** Between them, the two mean there is no code path on which a `mypermissions` call goes out
without a `projectKey`, which is precisely the condition under which the endpoint answers
`havePermission: true` at HTTP 200 and over-reports.

The claim is exactly that, and no more. It is not a type-level impossibility — a wrapper type asserting
non-emptiness was considered and rejected as ceremony for one call site. It is a property of two rules
holding together, and it is enforced where properties are enforced here: a unit assertion that every
issued request URI carries `projectKey=`, and a test that an empty project set produces zero outbound
calls. If either test is deleted, the property is gone; that is the honest strength of the guarantee.

### 3a. Probe latency budget — a human is waiting

This runs on a page load, so it needs a stated budget rather than an implicit one:

| Bound | Value | On expiry |
|---|---|---|
| Per request | **3 s** | that project's verdict is `Unknown` |
| Total for the whole fan-out | **10 s** | every project not yet answered is `Unknown`; answers already received are kept and shown |
| Concurrency | at most **4** in flight | bounds load on the customer's Jira, not on Lighthouse |

Partial results are shown rather than discarded: a connection where three projects answered and two timed
out reports three verdicts and two `Unknown`, not a blanket failure. Expiry copy says **"could not
check"** and states that the permission is unverifiable until the first write-back cycle observes it —
never "not quiet", which would be a claim the probe did not earn.

**When N is large.** The "typically 1-5 projects" figure is an expectation, not a bound; a connection
whose query spans a whole Jira site can reach dozens. Three things keep that case bounded: the total
budget above caps wall-clock regardless of N; the fan-out is over **distinct project keys**, not work
items, so it grows with the customer's project count and not with their backlog size; and projects beyond
the budget degrade to `Unknown` rather than hanging the page. The panel states how many projects were
checked out of how many found, so a partially-checked connection is legible as partial rather than
appearing complete. If a real customer is routinely truncated, the answer is on-demand re-check of the
named projects — not a longer budget and not a cache (see the rejected S3).

### 4. The probe is a capability interface, not a widening of `IWorkTrackingConnector`

```csharp
public interface IWriteBackNotificationProbe
{
    Task<IReadOnlyList<ProjectSuppressionVerdict>> ProbeSuppressionAsync(
        WorkTrackingSystemConnection connection,
        IReadOnlyCollection<string> projectKeys,
        CancellationToken cancellationToken = default);
}
```

Implemented by `JiraWorkTrackingConnector` alone; the call site type-tests. **This deliberately diverges
from [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md)'s idiom, on ADR-139's own
stated criterion.** ADR-139 rejected the type test because incremental-sync capability varies *per
connection* — Jira Cloud and Jira Data Center are one class and must answer differently — and a type test
cannot express that. Here the variance is *per connector class*: Jira can always probe, and Azure DevOps,
ServiceNow, Linear and CSV can never. That is precisely the condition under which ADR-139 says the type
test is adequate, so widening a five-implementation shared port to add a member four of them would answer
`[]` to would be ceremony.

The interface exposes **read methods only**. A capability that answers "may I be quiet?" must not be able
to change anything, and that is enforced by the shape of the port rather than by review.

### 5. The surface is a separate read-only endpoint, not a wider connection DTO

`GET /api/v1/worktrackingsystemconnections/{id}/writeback-notification-status`, guarded
`[RbacGuard(RbacGuardRequirement.SystemAdmin)]`, returning `{ rollup, projects[], checkedAt }`. Three
reasons, following [ADR-006](./adr-006-connection-list-payload-shape.md)'s precedent of one route / one
stable shape:

- `WorkTrackingSystemConnectionDto` is consumed by Lighthouse-Clients; widening it is a client contract
  change for a field only one connector ever populates.
- The status is slow and fallible; the connection payload is neither. Binding them means one degrades the
  other.
- It renders for Jira connections with write-back mappings and for nothing else (AC-02.5), which is a
  routing decision, not a nullable field.

Rendered as a read-only panel beside `WriteBackMappingsEditor`. **No remedy action, no toggle** — granting
the permission is a Jira-side administrative act and is deliberately not automated (D3).

### 6. Discovery is a probe on demand, and nothing is stored

**The verdict is computed when the connection settings page asks for it, and never persisted.** On
request, Lighthouse derives the distinct Jira project keys from the write-back targets bound to that
connection (decision 2), calls the probe once per project (decision 3), and returns the per-project
verdicts plus the derived rollup. There is no table, no EF migration, no cache and no invalidation
policy, because there is no stored state to invalidate.

**The probe and the observed 403 are complementary, not alternatives.** They answer different questions
at different moments and both ship:

| | Probe (this ADR) | Observed 403 ([ADR-142](./adr-142-writeback-suppression-optimistic-retry.md)) |
|---|---|---|
| Question | *Will* write-back be quiet in this project? | *Was* write-back quiet in this project? |
| Moment | before the first cycle, on demand | after each write-back flush |
| Reaches the user via | the connection settings panel | one Warning per connection per flush |
| Needs someone to look? | yes | no |

Where they disagree, one of them has a defect — see the Earned Trust table. Neither is derived from the
other.

Degradation: a probe failure or timeout yields `Unknown`, the page renders, saving is never blocked, and
Lighthouse never claims quiet it has not established (AC-02.4).

## Alternatives Considered

**S1 — observe only, and persist the verdict on the connection aggregate.** Derive the per-project
verdict from the `NotificationSuppression` values ADR-142 already puts on every write-back result, upsert
them into a new owned table, and read that table on page load. Zero extra Jira calls, and the state
stays readable while Jira is unreachable. **Rejected on two counts.** It cannot answer before the first
noisy cycle — which is the job as stated, *"before my team does"* — and it never answers at all for a
project whose mapped values did not change in a cycle, so the panel would show `Unknown` for exactly the
quiet-in-practice projects an administrator is least worried about and most often looking at. It also
buys an EF migration and an invalidation policy for a verdict that is one cheap request away.

**S3 — hybrid: probe primary, last observation cached in memory as a fallback.** Would keep the panel
populated across a Jira outage without a migration. **Rejected**: it reintroduces exactly the problem S2
deletes. Two independently-derived answers to one question need a precedence rule, a staleness marker and
a story for the case where they disagree — and the fallback is shown precisely when the authoritative
source could not be reached, which is when a stale verdict is most likely to be wrong and least likely to
be questioned. The observed fact already has a delivery channel that needs no cache: ADR-142's Warning.

**A connection-level boolean.** The DISCUSS default, and what AC-02.1/AC-02.2 literally describe.
**Rejected on measured evidence**: the permission is per project, so a single flag is unable to represent
the common mixed case and would send an administrator to grant a permission in a place where it changes
nothing. This is the finding that reframed the slice.

**Report at the Team / Portfolio level instead.** Closer to where the write-back mappings are felt.
**Rejected**: the credential and the permission both belong to the connection, and a Team can span
projects just as a connection can, so it relocates the problem without solving it.

**Gate the write on the probe.** Rejected in [ADR-142](./adr-142-writeback-suppression-optimistic-retry.md)
— it makes a permission read a precondition for a write, and the retry makes it unnecessary.

**Revive `ConnectionValidationResult.Advisory` / `SuccessWith` and ride the validation path.** It is the
idiom ADR-127 designed for exactly this class of message. **Rejected for this slice**: the mechanism was
deleted from the codebase by #5612's merge, so "reuse" would mean rebuilding a removed frontend and
backend contract, and the advisory fires on Save while this question is asked on page load. Restoring that
channel is #5627's business, not this epic's. If it returns, this endpoint's rollup is a natural producer
for it.

**Say nothing in the product; document the permission requirement instead.** Zero code, makes no false
statement, and the spike already wrote the documentation input. **Rejected as the primary answer** — it
leaves the administrator to discover from their team that the credential was under-permissioned, which is
the exact failure this epic exists to remove — but it is the honest fallback if slice 05 is deferred, and
ADR-142's Warning log means the condition is still not silent.

## Consequences

**Positive**

- The administrator is told *which projects* to fix, which is the only form of the answer that is
  actionable.
- Slice 05 adds no persisted state, no migration and no invalidation logic — the three things that make
  capability-reporting features rot.
- The probe's shape makes the one verified way of getting a wrong answer unrepresentable.
- The capability interface keeps four connectors untouched.

**Negative / accepted**

- Project keys are derived from a naming convention. Bounded by decision 2's visible `Unknown` fallback,
  but it is an assumption about Jira's key format and it is stated as one.
- N requests on a page load, N = distinct projects, on a path where a human is waiting. Bounded by the
  budget in §3a (3 s per request, 10 s total, 4 concurrent) rather than by the hope that N stays small;
  a connection spanning dozens of projects reports partial verdicts plus `Unknown`, and says so.
- A Jira outage makes the panel say `Unknown`. That is correct behaviour and it will still read as a
  regression to someone who saw a green panel yesterday. The copy must say "could not check", never
  "not quiet".
- The verdict is true of the moment it was asked. A permission scheme changed a minute later invalidates
  it, and nothing tells the user. ADR-142's per-cycle Warning is the compensating control.
- AC-02.3 as written routes the required permission by deployment (D4). **D4 is dropped** — there is one
  code path, and the permission is Administer Jira or Administer Projects on both deployments. AC-02.3
  needs restating in DISTILL.

## Earned Trust — the substrate lies, and the probe exercises the lie

| Substrate lie | Probe |
|---|---|
| `mypermissions` answers `true` at HTTP 200 when `projectKey` is omitted | Unit: every issued URI contains `projectKey=`; empty project set → zero requests |
| A licensed user is "obviously" project admin (true on six of seven spike projects, false on the seventh) | Verification against both a scheme granting `applicationRole` and one granting only project roles |
| The probe agrees with the write | Verification that a project the probe calls `NotSuppressed` is the same project whose write-back result reports `NotSuppressed` — the two independently-derived answers must agree, and disagreement is a defect in one of them. **A write-back item reporting `Unknown` (a 403 that survived the retry, ADR-142 §3) is excluded from this comparison** — it is not a suppression verdict and must not be treated as one |
| Jira is reachable when the page loads | Test: timeout → `Unknown` rendered, page loads, save unaffected |
| Jira is reachable but slow | Test: a project exceeding the 3 s per-request budget yields `Unknown` for that project while the others still report; the total budget caps wall-clock regardless of N |
| N is small | Test: a connection spanning more projects than the total budget allows reports partial verdicts plus `Unknown`, states how many of how many were checked, and never renders as complete |

## Cross-reference

- [ADR-142](./adr-142-writeback-suppression-optimistic-retry.md) — the observed `NotificationSuppression`
  fact and the per-connection Warning that fires without a page load.
- [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md) — the port-widening idiom
  this deliberately diverges from, and the criterion that licenses the divergence.
- [ADR-006](./adr-006-connection-list-payload-shape.md) — one route, one stable shape; the precedent for a
  separate endpoint rather than a conditional payload.
- [ADR-127](./adr-127-team-settings-advisory-channel.md) — the advisory channel this would have used, and
  its current status in the codebase.
