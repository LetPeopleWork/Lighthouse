# ADR-214: Refinement settings are one JSON-valued property on the Team, saved through the Team settings write, in a module of their own

- **Status**: Proposed (DESIGN, 2026-10-02)
- **Date**: 2026-10-02
- **Feature**: epic-5510-5881-refinement (ADO Epics #6136, #5881, #5510; slices 01, 03, 04, 07, 08, 13)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

A Team gets a group of new settings, delivered over six slices in an order the maintainer fixed (DD-22):
refinement states (01), a stage per state (03), a cadence (04), band percentiles (07), stage rules (08) and the
readiness rule with an optional veto (13). They are Team-level only (D2), edited by Team admins in a new section of
the Team's Settings tab (DD-14), and read on every open of a new Refinement tab.

Constraints that shape the storage:

- **Migrations are expand-only** and generated per provider (SQLite + Postgres) through `CreateMigration`. Each one
  is a release-coupled, two-provider artefact with a known build-order trap: the provider migration projects
  reference the backend through a HintPath DLL, so the backend must be built first or `dotnet ef` reports pending
  model changes.
- The Team settings write (`PUT /teams/{teamId}`, `TeamController.UpdateTeam`, `TeamController.cs:120-202`) already
  provides TeamWrite, autosave (ADR-029) and the optimistic-concurrency token with 409 (ADR-027) — the guarantees
  these settings need.
- That write calls `WorkItemRelatedSettingsChanged` and, if it returns true, **deletes the Team's Work Items**
  (`TeamController.cs:181-185`).
- Structured per-Team settings already persist as JSON text through a `ValueConverter` + `ValueComparer`:
  `StateMappings` and `CycleTimeDefinitions` (`LighthouseAppContext.cs:583-629`, ADR-064).
- The Refinement feature introduces its own vocabulary (stage, readiness, cadence, need, sizing log) and one read
  composition; no existing module owns those words.

## Decision

1. **One value object `RefinementSettings` on `Team`** (not on `WorkTrackingSystemOptionsOwner`, because Portfolios
   never have it): `States[] { State, Stage }`, `Cadence? { Weekdays, IntervalWeeks, AnchorWeek? }`,
   `Band { LowPercentile = 50, HighPercentile = 85 }`, `Readiness { MinYes = 3, MinVoters = 3, Veto? }`,
   `StageRules { Waiting?, BeingRefined?, Ready? }`. Persisted as **one nullable JSON text column** on `Teams`
   through the same converter/comparer pattern as `StateMappings`. Null means "not configured".
2. **One migration, in slice 01.** Every later settings slice adds JSON members **with a default in their
   initialiser**, so a document written by an older release deserialises into the newer shape without a migration.
   The evolution rule is part of this decision: members are only added; renaming or removing one is a contract
   change that needs its own ADR.
3. **Saved through the existing Team settings write.** `TeamSettingDto` gains a nullable `Refinement` member;
   **null on the wire leaves the stored settings unchanged** (it never clears them). Validation is one pure
   `RefinementSettingsValidator` called from `UpdateTeam`; value-type members of the DTO are nullable (S6964).
4. **`WorkItemRelatedSettingsChanged` never considers refinement members.** Changing what counts as refinement must
   not discard the Team's Work Items; refinement is a lens over stored items, not a query change.
5. **Refinement states are entries picked from `ToDoStates ∪ DoingStates`** (mapped names or raw states, as
   `WaitStates` does under ADR-056) and resolve through the existing `GetRawStatesForCategory`. An entry that later
   leaves To Do ∪ Doing is **kept and flagged** on read, never dropped silently.
6. **A new module `Refinement`** (`Services.*.Refinement` + `API/Refinement*Controller`) holds the feature. It depends
   down on Forecasting, Metrics, WorkItems/Rules, RBAC/Identity and Platform; **nothing outside `API` and the
   composition root depends on it**.

## Alternatives considered

- **A column per setting.** Six additive migrations across six slices, each a two-provider artefact, for values that
  are never queried by column. **Rejected** — cost without a query that needs it.
- **A 1:1 `TeamRefinementSettings` table.** Clean separation, but a join on every Team load and a second concurrency
  root, so a Team admin editing two sections of one form could conflict with themselves. **Rejected.**
- **A separate `PUT …/refinement/settings` endpoint.** Re-implements the token, the 409 path and the autosave save
  path for one section of a form that already has them. **Rejected.**
- **Spread the code over Forecasting, WorkItems and Metrics.** Puts a vote log in a forecasting namespace and gives
  no single place to enforce "nothing depends on refinement". **Rejected.**

## Consequences

- **Positive**: two migrations for the whole feature (this one and the sizing log's, ADR-216) instead of seven; the
  settings inherit TeamWrite, autosave and 409 for free; slices 03/04/07/08/13 ship without schema work.
- **Positive**: one place (`RefinementSettings`) holds every default the DISCUSS wave fixed (50/85, 3/3, veto off).
- **Negative**: the column cannot be queried in SQL ("which Teams use a cadence?"). Nothing asks that today; usage
  data answers it from the browser (ADR-190).
- **Negative**: JSON evolution is a discipline, not a schema check. Enforced by a round-trip test that deserialises
  a slice-01-shaped document into the current type and asserts every later member takes its default.
- **Risk named**: a client that PUTs a `TeamSettingDto` without `refinement` must not wipe it — hence "null = leave
  unchanged", pinned by a test.

## Enforcement

- ArchUnitNET: nothing outside `API`/composition root references `Services.*.Refinement`.
- NUnit: `WorkItemRelatedSettingsChanged` returns false when only refinement members change (every member).
- NUnit: null `Refinement` on the DTO leaves stored settings byte-identical; slice-01 JSON round-trips into the
  current shape with defaults.
- Migration via `CreateMigration` only (DELIVER).

Cross-refs: [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md),
[ADR-029](./adr-029-autosave-on-valid-mechanism-placement-and-save-state-machine.md),
[ADR-056](./adr-056-wait-states-config-placement-and-mapping-aware-resolution.md),
[ADR-064](./adr-064-cycle-time-definitions-storage-as-owned-collection-on-settings-aggregate.md).
