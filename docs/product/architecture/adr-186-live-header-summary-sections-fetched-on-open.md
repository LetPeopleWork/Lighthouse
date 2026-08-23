# ADR-186: The header summary is live and small; each popover section fetches its own data on open

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 02 / ADO #5840; extended by slices 05 and 06)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

The Task Manager is a header icon that opens a popover. The icon must answer two questions without
being clicked — is anything running, and is anything wrong — because that is what makes it worth
glancing at. The popover behind it grows over three slices: activity in slice 02, connection health in
slice 05, recent problems in slice 06.

The icon renders on **every page** for **every** System Administrator, for the whole session. The
sections are read by one human, occasionally, when they choose to look. Those are very different
traffic profiles for data that arrives from three different backend subsystems — an in-memory status
store, a database table, and a ring buffer.

The live channel already exists: `UpdateNotificationHub` has a `GlobalUpdates` group every client
joins, and `UpdateQueueService.NotifyListeners` fires into it on every status change.

## Decision

**A small always-live summary drives the badge. Each section fetches its own payload when the popover
opens.**

- The summary carries what the icon needs and nothing else: the active count, and the worst severity
  across failed runs and unhealthy connections. It rides the existing `GlobalUpdates` group — no new
  transport, no polling.
- Activity, Connections and Recent problems are three routes and three frontend section components,
  each owning its own loading and empty state.
- Slices 05 and 06 therefore each add one endpoint and one section without reshaping slice 02's
  contract.

**Empty is stated in words, never rendered as an empty box** — in every section, and in the popover as
a whole.

**Every label renders the tenant's configured Terminology.** `team`, `teams`, `portfolio`,
`portfolios`, `feature` and `features` are all renameable, and the two update types the frontend has
never had to name — `PortfolioDelete` and `TeamDelete` — need labels chosen deliberately rather than
falling through as `undefined`.

**The icon returns `null` for anyone who is not a System Administrator**, following exactly what
`OAuthHealthIcon` does today, and it mounts in **both** the mobile and desktop branches of
`Header.tsx`.

## Consequences

**Positive.** The header stays cheap: one small live value, not three payloads, on every page for the
whole session. Sections are independently shippable, which is what lets the slicing hold. A section
that fails to load degrades to that section rather than blanking the popover.

**Negative.** Three contracts instead of one, and three loading states instead of one. Accepted: the
alternative couples three subsystems into a payload that every later slice reshapes.

The summary and the sections can momentarily disagree — the badge says two running, the list arrives a
beat later showing three. Bounded by the fetch, corrected by the next live push, and not worth a
consistency mechanism for a surface a human reads.

**Enforced by**: an import rule that no component fetches connection health directly, mirroring the
existing constraint that all RBAC gating derives from `useRbac`. The popover reads through
`SystemActivityService` like every other surface reads through its service.

## Alternatives considered

**One aggregated endpoint returning updates, health and warnings together.** One round trip, one
loading state, one contract. Rejected: the header would pay for all three on every page even when the
popover is never opened, and each later slice would reshape a contract two other sections depend on.

**Three endpoints and no summary — derive the badge from the activity list.** Simplest per-section
contract. Rejected: the badge could then show activity but not connection health or a recent failure,
which is precisely what the one-icon decision promised it would show. It would either force a fetch of
all three on page load — the aggregated option's cost without its simplicity — or break the promise.

**Poll the summary on an interval.** Simpler than SignalR. Rejected: the live channel already exists,
already carries exactly this signal, and every client is already in the group.
