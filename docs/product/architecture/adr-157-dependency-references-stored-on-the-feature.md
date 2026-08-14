# ADR-157: A Feature stores the references it waits on, and the relations fetch it rides on already exists

- **Status**: Proposed (2026-08-14, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-08-14
- **Feature**: epic-4365-dependencies (ADO Epic #4365, slices 01, 05, 06)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

A dependency is a directed "cannot start until" edge between two Features, and it always comes from the
work tracking system — Lighthouse never authors one. Two shapes were open at the end of DISCUSS: what
is persisted, and what the read costs.

The persistence question is settled by sync order. If Feature B references A and A has not been
imported yet, a resolved Feature-to-Feature foreign key cannot be written at all, and the edge silently
does not exist until some later sync happens to fix it — a defect that presents as "the dependency is
just not there".

The cost question is the one this epic can lose on. Epic #5687 took a full portfolio refresh from
468 856 ms to 2 087 ms. An N+1 relation read hands that back.

Reading the connectors settles it favourably, and differently from what DISCUSS assumed.
`AzureDevOpsWorkTrackingConnector.GetParentReferencesFromRelationFields` (`:1032-1052`) already fetches
every Feature's relations in **chunked batches** with `WorkItemExpand.Relations` — the dependency
relations are in that response already, unread. Jira sends an explicit `fields=` list per request
(`:1613`); adding `issuelinks` widens a payload without adding a request. Linear's GraphQL query
(`:660-726`) already selects `parent { … }`; `dependencies` is a sibling selection in the same
document. **On all three connectors the dependency read costs zero additional requests.**

## Decision

**A Feature owns a persisted collection of the references it waits on, in `ReferenceId` space, resolved
to Features at read time. Ingestion rides the fetch that already happens.**

Five points that are part of the decision:

1. **The stored form is a string, in the connector's own reference space** — an ADO work item id, a
   Jira key, a Linear identifier — exactly as `WorkItemBase.ParentReferenceId` already works. A stored
   reference heals on its own: the next read resolves it, whatever order the sync imported things in.
   No per-tracker normalisation layer is owed; the single transformation is Linear's lower-casing,
   because that connector stores `Identifier?.ToLowerInvariant()` (`:343`) and its GraphQL connection
   returns the identifier upper case.

2. **Its own table, `FeatureDependencyReference (Id, FeatureId, ReferenceId, Source)`**, never a field
   on `WorkItemBase` and never inside `Feature.Update`. `WorkItemBase.Update` overwrites every synced
   field on every refresh, which is exactly why `Feature.ManualRank` sits outside it. `Source` is
   `TrackerLink` or `PortfolioField` and exists because the dialog must say which part of the work
   tracking system an edge was read from (AC-2.2). Additive, expand-only, generated with the
   `CreateMigration` script.

3. **Exactly one writer: the portfolio sync's reconcile**, replacing a Feature's references wholesale
   with what the current source now says. Wholesale replacement is what makes a link removed in the
   tracker disappear from Lighthouse rather than accumulate. Duplicates within one Feature collapse to
   one edge. A self-reference is stored, not silently dropped — it is a one-member loop, and the loop
   warning is the honest rendering of it.

4. **The ADO relations fetch is skipped only when *both* overrides are set.** The existing early return
   (`:1014-1018`) skips the fetch when the parent override is set, on the reasoning that there is
   nothing left to read. That reasoning stops being true the moment a second consumer reads the same
   response. Copying the early return verbatim would silently yield zero dependencies for every
   Portfolio that has a parent override — a failure indistinguishable from having none. The condition
   becomes: fetch relations unless the parent override *and* the dependency override are both set.

5. **`IWorkTrackingConnector` gains no method.** `GetFeaturesForProject` already returns
   `List<Feature>`, and a Feature now carries its own references — the same way `ParentReferenceId`
   arrives. Adding a port method would mean a second round trip per connector to carry data the first
   one already returns.

**The Portfolio override.** `DependencyOverrideAdditionalFieldDefinitionId` is declared on `Portfolio`,
alongside `FeatureOwnerAdditionalFieldDefinitionId` and `SizeEstimateAdditionalFieldDefinitionId` —
**not** on `IWorkItemQueryOwner` beside the parent override. Dependencies are between Features and
Features are fetched per Portfolio, so a Team-level setting would have no consumer, and
`FetchFingerprint`'s own note records why the portfolio-only references arrive by pattern match rather
than by widening the interface: a Team would carry them as dead surface. The field joins
`FetchFingerprint.RegisteredProperties` under *how the answer is read*, so changing it forces the next
cycle to re-download.

When the override is set it **replaces** the tracker's native links for that Portfolio rather than
unioning with them. Its value is split on comma or semicolon and each entry trimmed; an entry that
resolves to no Feature is skipped while its neighbours are kept, because a hand-maintained field will
contain typos and one typo must not discard the three good references beside it.

## Alternatives considered

- **A resolved `Feature → Feature` foreign key.** The obvious relational modelling, and it makes the
  graph a join rather than a lookup. **Rejected** — it cannot be written when the referenced Feature
  has not been imported yet, and the resulting missing edge is invisible. The stored reference is the
  shape the product already uses for the one inter-item link it has.
- **A delimited string column on `Feature`.** No migration join, no second table. **Rejected** — it
  introduces a second grammar that every reader must parse, it cannot carry `Source`, and it cannot be
  queried, so the reverse direction ("what waits on me") would need a table scan with a `LIKE`.
- **Fetch the forward direction too**, so both "waits on" and "blocks" come from the tracker.
  **Rejected** — it doubles the payload to learn something the stored reference set already contains by
  inversion.
- **Union the override field with the native links.** An ADO team could plausibly have some
  Predecessors native and some in a custom field. **Rejected** — harder to explain than it is worth,
  and inconsistent with the parent override whose shape it copies.
- **A new connector port method returning references.** **Rejected** — a second round trip per
  connector for data the existing call already returns, and five implementations to keep in step.

## Consequences

- **Positive**: KPI-3 is defended by construction rather than by optimisation. Zero additional requests
  on all three connectors; the growth is payload, which is what the 110 % budget is there to measure.
- **Positive**: the graph the simulation and the cycle detector consume is derived at read time from a
  dumb stored form, so there is one stored thing and one derived thing rather than two stored things
  that can disagree.
- **Negative — a substrate that is allowed to lie.** Jira link type names are editable per instance, so
  matching `type.inward == "is blocked by"` is trusting a string an administrator can rename. The read
  therefore emits a structured `dependency.jira.unknown_link_type` event listing the inward names it
  actually saw when it found none it recognised, so "this Jira instance calls it something else" is
  diagnosable rather than presenting as "this instance has no dependencies". The Linear lower-casing
  has the same failure signature and is covered by its own acceptance criterion (AC-9.2) against an
  upper-case fixture.
- **Negative**: ServiceNow and CSV remain out, and the override does not rescue them.
  `ServiceNowWorkTrackingConnector.GetFeaturesForProject` throws `NotSupportedException` (`:751-757`) —
  the override changes where a reference is read from, it does not create the objects a reference
  points at.
- **Reuse verdict**: `WorkItemBase.ParentReferenceId` → **PATTERN REUSED, NOT EXTENDED** (a parent is
  0..1 and can be a scalar; a dependency is 0..n). `Feature` → **EXTEND** (one collection, deliberately
  outside `Update`). `Portfolio` → **EXTEND** (one nullable field, third of its kind).
  `FetchFingerprint` → **EXTEND** (one registered property). `AzureDevOpsWorkTrackingConnector`,
  `WorkItemExtensions`, `JiraWorkTrackingConnector`, `LinearWorkTrackingConnector` → **EXTEND** (the
  existing fetch, read a second time). `IWorkTrackingConnector` → **NO CHANGE**.
  `FeatureDependencyReference` and the reconcile → **CREATE NEW**; the product has never stored a
  Feature-to-Feature relation, and a search for `dependenc`, `predecessor`, `blockedBy` and `issuelink`
  across the backend returns nothing outside epic #5074's unrelated blocked-item concept.
- Cross-refs [ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md) (what is done
  with the stored edges), [ADR-138](./adr-138-two-phase-incremental-work-tracking-sync.md) and
  [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md) (the sync whose speed this must not
  cost, and the fingerprint the new setting joins),
  [ADR-102](./adr-102-feature-blocked-transition-standalone-entity.md) /
  [ADR-103](./adr-103-feature-blocked-semantics-per-portfolio.md) (epic #5074's *blocked*, a different
  concept whose renameable term this feature must not borrow).
