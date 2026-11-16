# ADR-285: Connector capabilities are interfaces, and per-connection variance is a probe on the capability

**Status**: Accepted
**Date**: 2026-11-16
**Amends**: [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md)
**Feature**: `docs/feature/story-6376-s06-connector-capability-interfaces/` (ADO Story #6376, Epic #6508)

---

## Context

`IWorkTrackingConnector` is the port of the Tracker integration context (ADR-270): every connector (Jira, Azure
DevOps, Linear, ServiceNow, CSV) implements it, and Work Items and the write-back flow call it. It had grown to 16
members. Three connectors could not serve all of them, so they implemented the member and threw
`NotSupportedException`: CSV from 6, Linear from 6, ServiceNow from 8, 20 in total. The throwing members fell into
three groups: incremental sync (the sweeps and the fetches by id, 15), write-back (3) and reading a Portfolio's
Features (ServiceNow, 2). A caller could only learn what a connector can do by asking a `Supports…` flag, or by
catching the throw.

ADR-139 had looked at this shape and rejected "a separate opt-in interface" for incremental sync, because a type
cannot vary per connection: Jira Cloud and Jira Data Center are one class, and at the time only Cloud was allowed to
sweep. That reason is still right, but its conclusion no longer follows, for two reasons:

- **Most of the variance is per connector type, not per connection.** CSV, Linear and ServiceNow can never sweep,
  never write back, and ServiceNow never reads a Portfolio's Features, whatever the connection. A member that always
  throws for a type says something false about that type.
- **The Jira runtime case is no longer Cloud versus Data Center.** Data Center sweeps since its pagination probe
  passed. Jira's `SupportsIncrementalSync(connection)` now says no only while the connection's deployment is not yet
  known in this process: the Atlassian gateway is always Cloud, and any other site is known only after an earlier
  call has learned its deployment. Until then, a sweep cannot start, because Cloud and Data Center page their search
  results differently.

So a type can say what a connector can *never* do, and a probe is still needed for what one connector can do *for one
connection now*. The two are not alternatives; they answer different questions.

## Decision

**The connector port keeps only what every connector serves. Each thing some connector cannot do is a capability
interface of its own. A capability whose answer depends on the connection carries a probe that takes the connection.**

| Interface | Members | Implemented by |
|---|---|---|
| `IWorkTrackingConnector` (base port) | `SupportsTransitionHistory(connection)`, `GetPredefinedAdditionalFields(connection)`, `GetWorkItemsForTeam(team, ct)`, `ValidateConnection`, `ValidateTeamSettings`, `ValidatePortfolioSettings` | Jira, Azure DevOps, Linear, ServiceNow, CSV |
| `IWriteBackTarget` | `WriteFieldsToWorkItems(connection, updates)` | Jira, Azure DevOps |
| `IFeatureSource` | `GetFeaturesForPortfolio(portfolio, ct)`, `GetParentFeaturesDetails(portfolio, ids, ct)` | Jira, Azure DevOps, Linear, CSV |
| `IIncrementalSource` | probe `SupportsIncrementalSync(connection)`; `SweepWorkItemsForTeam`, `SweepFeaturesForPortfolio`, `SweepParentFeatures`; `GetWorkItemsForTeam(team, ids, ct)`, `GetFeaturesForPortfolio(portfolio, ids, ct)` | Jira, Azure DevOps |

The existing capability interfaces (`IBoardInformationProvider`, `IDeliverySourceProvider`,
`IDeliveryForecastPublisher`) are unchanged. The per-system marker interfaces (`IJira…`, `IAzureDevOps…`,
`ILinear…`, `IServiceNow…WorkTrackingConnector`) list the capabilities their connector offers; CSV declares
`IFeatureSource` on its class.

**The type says "never"; the probe says "not for this connection now".** Incremental sync keeps its per-connection
probe, now declared on `IIncrementalSource`. Azure DevOps answers yes; Jira answers no while its deployment is not
yet known (above). `SupportsTransitionHistory` stays on the base port: it varies per connection for CSV and ServiceNow,
but it gates no member, so it is a fact about the data, not a capability.

**The caller tests the type.** The factory still returns `IWorkTrackingConnector`; no generic lookup is added.

- The refresh sweeps only when `connector is IIncrementalSource source && source.SupportsIncrementalSync(connection)`;
  otherwise it takes the full path, as before. The Team, Portfolio and parent-Feature scans ask the same way.
- The Portfolio refresh asks for `IFeatureSource` where it reads Features. Write-back asks for `IWriteBackTarget`
  inside its existing guard.
- A missing capability fails at the same point the old throw did, with a generic sentence, "… is not supported for
  {system}". It reaches only the log: a refresh that fails this way records no reason for the user, and write-back
  fails each update, as before. The maintainer chose the generic sentence over keeping each connector's own wording.

The connector port also says Portfolio where it means one: `GetFeaturesForProject` is now `GetFeaturesForPortfolio`,
and its parameters are named `portfolio`.

The change is behaviour-free: the same sync path is chosen per connector and connection, the same validation verdicts
are given, and nothing a user sees changed. Only the log sentence for a ServiceNow write-back or Portfolio refresh is
shorter.

### One incremental capability, not one per level

`IIncrementalSource` holds the Team, Portfolio and parent-Feature sweeps together. Splitting it by level would let a
connector sweep its Teams without its Features. Epic #6474 makes Teams and Portfolios units, and the connector will
then fetch the items of a unit rather than Work Items or Features; a split by level would put a two-level shape into
the port that Epic undoes. If a connector ever sweeps only some units, that is a question for the unit-level port.

## Alternatives considered

**Edit ADR-139 in place.** Rejected. It would rewrite an accepted decision's record of an alternative it rejected,
and lose why it was rejected then. ADR-139 gets a status line and a dated note instead.

**Split incremental sync by level** (`IIncrementalWorkItemSource` and `IIncrementalFeatureSource : IFeatureSource`).
Rejected, see "One incremental capability" above: Teams and Portfolios become units, and the port must not fix a
shape that change reverses.

**A generic factory lookup** (`factory.Get<IFeatureSource>(system)`). Rejected. It is a second way of doing what `is`
already does, and three callers in Promises already type-test `IDeliverySourceProvider` and
`IDeliveryForecastPublisher` with the factory as it is.

**A runtime capability object** (`connector.Capabilities.CanSweep(connection)`). Rejected. The throwing members stay
on the port, so the type still claims what it cannot do.

**Split the base port by client** (`IConnectionValidator`, `IWorkItemSource`). Not done. Every connector answers
every base member, so nothing throws there; the split would be interface segregation alone. Left to the packaging of
Tracker integration if its published contract wants it.

## Consequences

**Positive**

- No connector claims a member it cannot serve. A new connector implements the base port and only what it can do, and
  the callers route it by its type.
- Each capability's set of connectors is a fact about types, pinned by reflection tests; the Jira probe's answers are
  pinned for a reached and an unreached connection.
- **A ratchet holds it.** `ConnectorsDeclareNoUnsupportedMembersArchUnitTest` reads the compiled code of every type
  that implements a connector-port interface and fails on any method that creates a `NotSupportedException` (or a type
  derived from it) unless the method is listed with its reason. Code that the compiler moves into a lambda, a local
  function or an async body counts against the method that wrote it. The list may only shrink (ADR-268). Two genuine
  cases remain: Jira's `SweepIdentities` (a sweep asked for before the deployment is known, which the probe prevents)
  and Azure DevOps' `ConvertAuthorizationHeaderToVssCredentials` (an authentication scheme the client cannot turn into
  credentials, a configuration error).

**Negative / accepted**

- **A wrapper hides capabilities.** A future decorator or proxy around connectors (caching, logging, metrics) must
  forward the capability interfaces, or the type tests see only the wrapper: the capabilities vanish silently and
  refreshes fall back to the full path. That is slower, never wrong, because the full path is always safe. Today the
  factory hands out the connector itself; whoever adds a wrapper owns this. Test doubles are not affected: a mock that
  sets up a capability member must go through `Mock.As<Capability>()`, or the test does not compile.
- The ratchet reads construction sites, so a `NotSupportedException` built in another type and thrown from a
  connector is not caught.
- The caller has to remember to test the type; the compiler only proves that a caller which calls a capability member
  holds the capability.

**Adding a capability that varies per connection.** Implement the capability interface on each connector that can
ever serve it, and add a `Supports…(WorkTrackingSystemConnection connection)` probe to that interface, as
`IIncrementalSource` does. Callers ask the type and then the probe. Never throw from a member instead: the ratchet
refuses it.

## Cross-reference

- [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md): incremental sync as a per-connection
  probe; amended here (the probe now lives on the capability, and the runtime case is "deployment not yet known").
- [ADR-138](./adr-138-two-phase-incremental-work-tracking-sync.md): the two-phase sync the incremental capability
  serves, and why a sweep that may lose an id must never drive removal.
- [ADR-268](./adr-268-a-ratchet-guard-lists-its-offenders-by-name-in-both-directions-and-only-ever-shrinks.md): the ratchet convention.
- [ADR-270](./adr-270-bounded-contexts-context-map-and-module-layout.md): Tracker integration, its glossary
  (Capability), and the connector port as the contract on the Tracker integration → Work Items edge.
