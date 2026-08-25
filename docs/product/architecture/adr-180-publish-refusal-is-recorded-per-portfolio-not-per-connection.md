# ADR-180: A refused Release write is recorded per Portfolio and keyed on HTTP 400, because the permission it reports is per project

- **Status**: **Accepted with point 1 superseded** (DELIVER, 2026-08-25). The state is recorded **per
  Delivery**, not per Portfolio — see the supersession note at the end. The file name is left as it was
  so existing links keep working.
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

1. **SUPERSEDED (2026-08-25) — the refusal is recorded on the Delivery.** What follows is the reasoning
   as written; the supersession note at the end says why one more step down was the coherent answer.
   ~~The refusal is recorded on the Portfolio, not on the connection.~~ This overrides the brief. The
   permission slice 05 reports is `ADMINISTER_PROJECTS`, which slice 00 measured to be **per project**:
   one credential can write project A and be refused project B. A connection-level "Jira refuses writes"
   would therefore be false for most of what the connection touches, and would make a working Portfolio
   look broken because a different one is misconfigured. Two nullable columns beside the publishing
   switch: `LastPublishRefusedOn` and `LastPublishRefusalReason`.

2. **Refusal is detected as a rejection whose body carries a message.** Amended in DELIVER: keying on 400
   alone would have missed the 403 the API documents and the 401 a rejected credential produces, so every
   4xx other than 404 is read as a refusal *provided it says something*. A rejection carrying no message
   in either half of the body is not treated as a refusal - it is a malformed request, which is a defect,
   not a permission report, and it is thrown rather than recorded.

3. **The reason is Jira's message, carried verbatim.** It already names what to fix in the reader's own
   vocabulary, and paraphrasing it into a Lighthouse sentence would lose the exact words an admin can
   search for. Amended in DELIVER: **both halves of the error envelope are read**. A refusal about the
   request arrives in `errorMessages`; one about a field arrives in `errors` with `errorMessages` empty -
   and that is the shape of the refusal a *permitted* credential can actually provoke, a description over
   the size ceiling. Reading only the first half left the one refusal an administrator can act on
   reported as a bare status line.

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

- Two nullable columns on `Delivery` (see the supersession note), in an expand-only migration of their own.
- The report names the Delivery and the day, and quotes Jira. An admin can act without reading server logs.
  **It does not name the project** - Lighthouse does not persist which project a bound Release belongs to,
  and adding that is a schema change nobody has asked for yet.
- A single credential serving several Deliveries reports accurately per Delivery, which is the point.
- Detection depends on a message-bearing 400. If Atlassian changes that body, refusals degrade to
  silence - which is why the contract test recommended for the Versions read should cover the refusal
  body too.

## Supersession — point 1 moved one level down (2026-08-25, DELIVER)

Written while the publishing switch still lived on the Portfolio (DES-22). **D8a moved that switch to the
Delivery**, and this ADR's own argument moved with it.

The argument here was that connection-level state would be *"false for most of what a connection touches"*
because `ADMINISTER_PROJECTS` is per project. A Portfolio is not one project either: it routinely holds
Deliveries bound to Releases across several, so Portfolio-level state is false for most of what a
*Portfolio* touches, for exactly the reason given above. One Delivery is one Release in one project, which
is the granularity the permission actually has.

**The decisive part is point 5.** *"The state clears on the next successful publish"* cannot be made
correct at Portfolio scope: if Delivery A is refused and Delivery B succeeds, B's success clears A's
refusal and the administrator is told the problem went away while it is still there. That is not a
reporting inaccuracy, it is the report actively lying, and no amount of care at the call site fixes it
while the state is shared.

Everything else in this ADR stands: keyed on a message-bearing rejection, Jira's words verbatim, 404 is
not a refusal, cleared on success, never disabled or retried tightly, inbound unaffected, no pre-flight
check.

**Decided by the maintainer**, presented with both options and this reasoning.
