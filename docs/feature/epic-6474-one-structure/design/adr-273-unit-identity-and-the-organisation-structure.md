# ADR-273: Unit identity is (level, number), and the organisation structure is one aggregate

**Status**: Proposed (Epic #6474 DESIGN, 2026-11-02; maintainer decisions MD1, MD5 applied). Promoted to
`docs/product/architecture/` with slice 03.
**Context map**: Organisation set-up (ADR-270). Related: ADR-027 (in-process events), ADR-241 (follow the
connection default), ADR-270 Decisions 3, 7 and 8, ADR-274 (item types).

## Context

Epic #6474 turns Teams and Portfolios into one `Unit` type on two installation-wide levels (6455 DISCUSS D7, D8,
D11), without changing any id the API, the CLI and MCP clients, RBAC scopes, report owners, snapshots, the refresh
log or the update queue use (D9).

Today Teams and Portfolios live in two tables with two id sequences: Team 3 and Portfolio 3 can both exist. Every
table that points at one of them already stores a kind next to the number: `OwnerType` (three snapshot tables),
`PermissionScopeType` (user and API-key scopes), `ReportOwnerKind` (reports), `RefreshType` (refresh history) and
`UpdateType` (queue keys). Migrations are expand-only.

Some rules span levels: there are exactly two levels for now, one of them the leaf; only the level above the leaf
(the Feature level) can be switched off, and only while exactly two levels exist (M13). Item types are not one of
these rules any more: each connection's mapping guards them (ADR-274).

## Decision

1. **A unit is identified by its level and its number on that level.** `UnitId` is a value object (level, number).
   Numbers are allocated per level: the leaf level continues today's Team numbers, the Feature level today's
   Portfolio numbers. `/teams/3` is (leaf, 3) and `/portfolios/3` is (Feature level, 3).
2. **The stored kind discriminators are read as the level key and never rewritten.** `Team` means the leaf level,
   `Portfolio` the Feature level, in all five enums. A level above the Feature level (#6455) gets its own key there.
3. **Levels have a stable `LevelId`, distinct from their ordinal.** Adding a level above changes no existing id.
   Code says "leaf" and "Feature level" (the leaf's direct parent), never "level 2".
4. **`OrganisationStructure` is one aggregate per installation**, holding the `Level` entities (names, on/off). It guards the cross-level rules above. Item types are not here: each
   connection maps them onto the levels (ADR-274). `Unit` is its own aggregate and refers to its level by
   `LevelId`; a unit's level never changes.
5. **What only one level needs lives in an extension keyed by `UnitId`, owned by the context whose data it is**:
   `LeafCapacity` (Forecasting: Feature WIP, auto-adjust, throughput window, forecast filter rules),
   `DefaultSizeGuess` (Work Items: the default-size guess settings) and `ForecastInputFingerprint` (Forecasting,
   written only by the forecast run). The Feature level's mapping (Size Estimate, Feature Owner and Dependency
   fields, `IgnoreDependencies`, the owning leaf unit) is Organisation set-up's and stays inside the `Unit` as a value
   object present only on the Feature level.
6. **Domain events** published by Organisation set-up: `UnitCreated(UnitId, LevelId)`,
   `UnitDeleted(UnitId, LevelId, UnitsThatReferencedIt)` (replaces `TeamDeleted` and Portfolio deletion's inline
   clean-up), `LevelRenamed`, `ConnectionItemTypesChanged`, `LevelSwitchedOff`, `LevelSwitchedOn`. The refresh events
   (`TeamDataRefreshed`, `PortfolioFeaturesRefreshed`, `PortfolioForecastsUpdated`) keep their names in this Epic.
7. `UnitId` and `LevelId` join the shared kernel's cross-context identifiers (ADR-270 Decision 7).

## Consequences

- No id, URL, bookmark, client script, scope row, report owner or snapshot row changes. No column is rewritten.
- A bare integer is a unit id only inside an adapter that already knows the level (a `/teams` route, a stored
  `Team` discriminator). Domain and application code passes `UnitId`.
- Storage is the solution architect's choice within this decision: two tables behind one `Unit` model is the
  cheapest expand-only shape; one table with a (level, number) key is a later contract step if ever wanted.
- A Team or Portfolio settings save writes the `Unit` and one extension owned by another context. The `Unit`'s
  concurrency token guards the whole save; each owner validates and writes its own part, both in one commit.
- Units on a switched-off level are hidden, not deleted: they keep their data and memberships, stop refreshing,
  still count towards the free-tier limits, and refresh once when the level is switched on again.
- Two System Admins editing different levels at once share one token: the later save gets a 409. Rare, accepted.

## Alternatives considered

- **A new global unit id with (kind, old number) kept as an alias.** Rejected for now: every table that points at
  a unit needs a new column and dual keys until a contract step, for no gain while two levels exist. #6455 may
  revisit it if a level-3 key in five enums proves awkward.
- **One number space by offsetting Portfolio ids.** Rejected: it rewrites ids (not expand-only) and breaks every
  URL, client script, scope and owner row.
- **Each level its own aggregate.** Rejected: nothing would guard "exactly two levels, one leaf, only the Feature
  level can be off".
- **The structure aggregate also holding the units.** Rejected: every unit settings save would lock the whole
  structure, and units have their own lifecycle and admins.
- **One `Unit` with nullable leaf and Feature-level parts.** Rejected: level-3 units would carry dead settings and
  code would keep branching on "is this a Portfolio".
- **Event Sourcing for the structure or units.** Not warranted: low-churn configuration, no temporal queries and no
  audit need beyond today's.
