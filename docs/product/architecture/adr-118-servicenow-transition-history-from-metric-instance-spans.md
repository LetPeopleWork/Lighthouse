# ADR-118: ServiceNow transition history is derived from `metric_instance` span starts, filtered by metric definition, and its absence is disclosed at connection validation

- **Status**: **Proposed** (2026-07-30, Epic 5513 slice 04 DESIGN) — pending maintainer ratification.
- **Date**: 2026-07-30
- **Feature**: epic-5513-servicenow-integration (ADO Epic 5513, Story 5577)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Supersedes nothing. Resolves**: ADR-117's open question. **Amends**: ADR-117 decision 2 —
  `StartedDate` is the first Doing span's `start` where history is readable, and ADR-117's
  `opened_at` only where it is not. **Cancels**: the
  `ServiceNowChoiceLabelResolver` seam named in slice 01's DESIGN.

## Context

ADR-117 established that without `itil`-grade rights ServiceNow work is measured
request-to-resolution from `opened_at`, and left true time-in-progress to this slice. It closed with
one open question: **how Lighthouse carries the honesty obligation** once the same team can report
either metric depending on the rights its integration account was granted.

The slice-04 brief carried a hard gate of its own — build only if the history source is affordable —
with an explicit re-slice branch if it turned out to cost one call per work item.

### Measured on the live PDI, 2026-07-30

**The source is batchable.** `metric_instance.id` accepts an `IN` list: 96 incident sys_ids returned
157 spans in a single 0.81 s call. The re-slice branch does not fire. The binding constraint is the
**8192-byte URL limit**, not a row count — 245 ids pass at 8182 bytes, 250 fail with `414`.

**Three beliefs carried out of the SPIKE were wrong**, and correcting them removed most of the
expected complexity:

| Believed | Measured |
|---|---|
| `value` holds the raw choice value, so Q10 forces a label resolver | `value` holds the **label** (`"New"`); `field_value` holds the number (`"1"`) |
| History may not fit the team's hand-typed state mapping | Label sets are **identical** across `state` and `incident_state` |
| Filter on rows with an empty `field` / empty `value` | Those are script-calculation rows; the discriminator is the **metric definition** |

**What got harder.** `metric_instance` mixes definitions. On `field=incident_state` the PDI returns
rows from "Incident State Duration" (*Field value duration* — the state spans) and from "Create to
Resolve Duration" and "First Call Resolution" (*Script calculation* — not spans). Others cover
`active`, `assigned_to`, `assignment_group`. Reading them all fabricates transitions out of
assignment changes.

## Decision

**1. Transitions are derived from each span's `start`.** Sort a record's spans by `start`, pair
consecutive entries, emit `previous.value → current.value` at `current.start`.

Deriving at the start rather than the end is what makes the rest cheap. 128 of 189 rows on the PDI
carry an empty `end`, so open spans need no special case — the newest span simply contributes no
outgoing transition yet. The ~30 s asynchronous metric lag stops mattering, because a span whose
`calculation_complete` is `false` still has a valid `start`. And the Glide-duration trap disappears
entirely: `duration` renders as an epoch offset (`1970-01-01 21:09:13` = 21 h 9 min), and nothing
reads it, so nothing has to parse it.

**2. Spans are filtered by metric definition, resolved once per sync.** Query `metric_definition`
for the configured table with `type = Field value duration`, keep the definitions whose `field` is
the state field, and accept only spans whose `definition` is in that set.

**3. `sysparm_display_value=all`, and `value` is read as the state label.** No choice-label
resolution is needed anywhere. The seam slice 01's DESIGN named for
`ServiceNowChoiceLabelResolver` is cancelled rather than deferred.

**4. Chunk at 200 sys_ids** — ~18 % headroom under the 8192-byte cliff, which covers
`sysparm_fields`, `sysparm_limit`, and customer instances on longer hostnames or a reverse-proxy
subpath. That a `414` is loud matters: an over-long batch **cannot silently return partial history**.

**5. The capability disclosure is a connection-validation notice, not a chart annotation.** This is
ADR-117's open question, answered.

Two distinct causes produce the downgraded metric, and — unlike the rights-vs-empty case that forced
the C-1 amendment in slice 01 — **they are distinguishable**:

| Cause | Signal | Remedy named to the user |
|---|---|---|
| The account lacks the rights | `403` on `metric_definition` / `metric_instance` | Grant the integration account `itil` |
| Rights granted, no state-span metric set up | `200` with **zero** matching definitions | Activate a *Field value duration* definition on the state field |

The notice is raised in `ValidateConnection`, at connection setup, as something the user
acknowledges. Re-validating re-evaluates it, so a customer who grants `itil` afterwards sees it
clear. **It does not appear in the metrics UI.** A caveat pinned permanently to every chart is noise;
a capability limit belongs where the capability is configured.

**6. AC5's opt-in team setting is not built.** Measured cost is 3 chunks ≈ 2.4 s per 500 items, which
is not material against existing refresh expectations. The feature ships on by default.

**7. `StartedDate` switches to the first Doing span's `start` when history is available, and falls
back to ADR-117's `opened_at` when it is not.** Ratified by the maintainer 2026-07-30: *"this is how
it is meant to be"*.

This is the point of the slice. ADR-117 decision 4 deferred true time-in-progress to slice 04, and
the `itil` escalation is paid for precisely to get it. Without this, a team that granted the role
would see Cumulative State Time report ~20 h in Doing while Cycle Time reported ~600 h for the same
work, with nothing on the page explaining the difference — two numbers contradicting each other is
not a more conservative outcome than one number changing.

The fallback is the existing per-instance capability branch, not new machinery: the same verdict that
drives `SupportsTransitionHistory` and the runtime downgrade selects the `StartedDate` source.

**`ClosedDate` is deliberately NOT switched** and stays `resolved_at ?? closed_at` from ADR-117
decision 1. The asymmetry is justified rather than an oversight: `resolved_at` is a genuine recorded
resolution instant, measured present on the record, whereas `work_start` is empty (SPIKE Q4) — which
is the entire reason `StartedDate` needed a substitute in the first place. Deriving a close instant
from history would add a dependency without adding accuracy.

**Upgrade consequence, which must reach the release notes and not only the docs.** Existing
ServiceNow teams that grant `itil` will see Cycle Time and Work Item Age *drop*, and historical
charts move with them. The number was inflated before and is correct after, but it changes without
the user editing anything.

## Consequences

**Good.** ServiceNow teams reach flow-diagnosis parity — Cumulative State Time, per-state percentiles
and staleness — through the same `WorkItemStateTransitionMapper` every other connector uses, with no
ServiceNow-specific mapping surface and no migration of existing team state mappings. Three of the
expected complications (label resolution, duration parsing, open-span handling) are designed out
rather than handled.

**Bad, and named.** `metric_definition` is `403` for every read-only role, so the capability question
cannot be answered at all without escalating. Lighthouse cannot distinguish "not granted" from
"granted but I have not looked yet" before the first read — which is why the verdict is produced by
`ValidateConnection`, where a read is actually attempted, rather than inferred from configuration.

**Also.** A customer who disables the out-of-box "Incident State Duration" definition silently loses
history. The `200`-with-zero-definitions rung names exactly that, but only at validation time — a
definition disabled *after* a successful validation degrades at the next sync and is caught by the
runtime downgrade, not by a fresh notice.

**Contract change.** `ConnectionValidationResult` gains an advisory channel that survives
`IsValid = true`. Today the frontend surfaces `message` / `technicalDetails` only through the error
path. This is a shared contract: grep every usage and extend the test factory before touching it.

## Alternatives considered

**A. Filter spans client-side on the `field` name.** Avoids the `metric_definition` call. Rejected:
it hardcodes which field counts as "state" per table (`incident_state` on incident, `state`
elsewhere, `problem_state` on problem) and is blind to customer-defined definitions — brittle in
exactly the place customers differ. The saved call is one per sync, not one per item.

**B. Annotate the metric in the UI.** The original recommendation. Rejected by the maintainer: a
permanent qualifier on every chart is noise, and it puts a configuration concern in a reading
surface. The information is not less important — it is moved to where it is actionable.

**C. Generalise a cross-connector capability surface now.** Linear already downgrades at runtime and
ServiceNow now will too, which looks like a pattern. Rejected as a rule-of-three violation on
dissimilar triggers — Linear's is a rejected GraphQL field, ServiceNow's is a `403` or an empty
definition set, and only ServiceNow's needs to carry a *reason* to the user. Parked as ADO 5612 for
evaluation at the end of the MVP.

**D. Persist the acknowledgement.** Rejected for v1: validation always reports the instance's current
capability, so "reset by re-validating" falls out with no new schema and no migration. A durable
dismissed-flag is additive later if wanted.

## Open questions for DISTILL

- Does a reopened record produce a second span carrying an earlier label? Pairing would then yield a
  `Resolved → In Progress` transition, which is correct — but it is unverified.
- Spans begin when the definition became active, so records predating it carry partial history and
  the first span's `value` is not guaranteed to be the record's first state. Whether a leading
  synthetic transition from creation is honest or invented is a DISTILL call.
- ~~Whether `StartedDate` should switch to the first Doing span's `start`.~~ **Decided 2026-07-30 —
  it switches. See decision 7.**
