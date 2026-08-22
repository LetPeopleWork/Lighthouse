# ADR-180: A refused Release write is recorded per Portfolio and keyed on HTTP 400, because the permission it reports is per project

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slice 05 / ADO #5832)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Slice 05 reports a refused Version write so an admin is told, rather than watching an opt-in switch do
nothing. Its brief specifies surfacing the refusal *"against the work tracking connection"*, and sets
its own size against slice 00: if the permission bar turns out low, the slice shrinks toward a log line.

Slice 00 measured both halves, and each answer moves the design.

**The bar is low.** The same non-site-admin identity holds `ADMINISTER_PROJECTS` on a default
company-managed project and successfully wrote a Version description there, while lacking it on a
team-managed project with restricted access, where the write was refused. Refusal is the exception.

**The failure shape is not what was assumed.** The refusal is **HTTP 400**, not 403:

```
HTTP 400
{
  "errorMessages": [
    "You must have global or project administrator rights in order to modify versions."
  ],
  "errors": {}
}
```

The write is dropped whole - a read-back confirmed the description byte-identical to its prior value, so
no partial application and no recovery logic is owed.

## Decision

**Per-Portfolio state, keyed on 400, carrying Jira's own words.**

1. **The refusal is recorded on the Portfolio, not on the connection.** This overrides the brief. The
   permission slice 05 reports is `ADMINISTER_PROJECTS`, which slice 00 measured to be **per project**:
   one credential can write project A and be refused project B. A connection-level "Jira refuses writes"
   would therefore be false for most of what the connection touches, and would make a working Portfolio
   look broken because a different one is misconfigured. Two nullable columns beside the publishing
   switch: `LastPublishRefusedOn` and `LastPublishRefusalReason`.

2. **Refusal is detected as HTTP 400 whose body carries `errorMessages`.** Detection keyed on 403 would
   never fire. A 400 with an empty `errorMessages` is not treated as a refusal - it is a malformed
   request, which is a defect, not a permission report.

3. **The reason is Jira's message, carried verbatim.** `errorMessages[0]` already names what to fix in
   the reader's own vocabulary. Paraphrasing it into a Lighthouse sentence would lose the exact words an
   admin can search for.

4. **`404` is not a refusal.** A missing Version raises the broken-source state (ADR-170,
   `SourceUnavailableReason.SourceNotFound`), because a vanished target and a denied one send an admin
   to fix entirely different things.

5. **The state clears on the next successful publish**, and publishing is never disabled, never
   retried in a tight loop, and never fails the Portfolio refresh.

6. **Inbound is unaffected.** Reading Releases and writing them are separate capabilities (D2,
   ADR-178), so a refused write leaves date sync running. This is the criterion that carries the slice.

7. **No pre-flight permission check.** `quiet-jira-writeback` established `mypermissions` as a reporting
   companion rather than a gate in the write path, and the same holds: the write attempt is the check.

## Rejected alternatives

**Recording on `WorkTrackingSystemConnection`,** as the brief specified. Rejected on slice 00's evidence:
the permission is per project, so connection-level state would be wrong in the common mixed case.
`RequiresReconnect` on `WorkTrackingSystemConnectionDto` is the precedent for a connection-level flag,
and it is the right precedent for a *credential* problem - which this is not.

**Keying detection on 403.** The assumed shape. Measured to be 400; code written against 403 would have
shipped a silent no-op.

**Dropping the slice to a log line,** which its own brief allows if the bar is low. The bar is low but
the refusal is real, measured, and reachable through ordinary Jira configuration - a team-managed
project with restricted access. A log line is invisible to the admin who flipped the switch, which is
the person the slice exists for. The slice shrinks in *size*, not out of existence.

**A shared "Jira is broken" state** spanning inbound and outbound. Rejected explicitly by the slice's
carrying criterion: an optional outbound feature must not be able to stop date sync.

## Consequences

- Two nullable columns on `Portfolio`, in the same expand-only migration as the publishing switch.
- The report names the Portfolio and the time, and quotes Jira. An admin can act without reading server
  logs.
- A single credential serving several Portfolios reports accurately per Portfolio, which is the point.
- Detection depends on a message-bearing 400. If Atlassian changes that body, refusals degrade to
  silence - which is why the contract test recommended for the Versions read should cover the refusal
  body too.
