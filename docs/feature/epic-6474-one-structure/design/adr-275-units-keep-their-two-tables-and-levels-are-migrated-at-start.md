# ADR-275: Units keep their two tables behind one `Unit` type, levels get their own tables, and a startup migrator fills them

**Status**: Proposed (Epic #6474 DESIGN, application part, 2026-11-02; maintainer decisions MA1, MA8, MA9 applied). **Revised 2026-11-28** (feature A DESIGN refresh): no switch column, no Feature-level-wins rule, a written-through column for rollback detection, the module already exists, and the CLR base keeps its name.
Promoted to `docs/product/architecture/` with slice 02 (levels) and slice 03 (units).
**Context map**: Organisation set-up (owner). Builds on ADR-273 (unit identity), ADR-274 (item types), ADR-027
(in-process events), ADR-270 Decisions 6–8. Expand-only migrations (maintainer rule).

## Context

ADR-273 fixes a unit's identity as (level, number), with today's Team and Portfolio numbers kept and the five stored
kind discriminators read as the level key. Today `Team` and `Portfolio` are two EF entity types sharing an unmapped
CLR base (`WorkTrackingSystemOptionsOwner`), each with its own table and id sequence. Every foreign key that points
at a unit (work items, feature work, the Portfolio-Feature join, deliveries, the owning Team, refinement, sizing log)
points at one of those two tables. Two database providers are supported (SQLite and PostgreSQL); EF stores a
`List<string>` as JSON text on SQLite and as a native `text[]` on PostgreSQL.

The migration (ADR-274 Decision 7, MD4, MD6, MD7) needs a case-insensitive union, a majority spelling with a
lowest-number tie-break, first-appearance order, a union per level in which a type both levels read sits on both
(2026-11-27), and a flag on every Jira, Azure DevOps or CSV unit left with no types.

## Decision

1. **`Unit` is the concept behind the two existing entity types.** `WorkTrackingSystemOptionsOwner` keeps its name in
   this Epic (a rename no outcome reads; it can be a behaviour-free improvement item); `Team` and `Portfolio` stay as its persistence-shaped subclasses, each on its own table and sequence. No
   row moves, no foreign key changes, no column is renamed. Domain and application code is written against `Unit`
   and `UnitId`; only the persistence adapter, the `/teams` and `/portfolios` controllers and the stored
   discriminators know which subclass a level uses.
2. **Levels are stored in two new tables**: `OrganisationStructures` (one row: id, concurrency token, the upgrade
   notice's dismissal time) and `Levels` (stable id, structure id, ordinal, the stored kind it maps to (`Team` or
   `Portfolio`, the same values the five discriminators hold), four names). There is no on/off flag: level 2 is removed, not hidden (slice 15). Item types live in two further
   new tables (ADR-274, revised 2026-11-06): `ConnectionMappings` (one row per connection: connection id, concurrency
   token) and `ConnectionLevelMappings` (connection id, level id, item types), with the list stored as units store
   theirs. The second table is where parent sources and shared defaults can later be keyed by level.
   Columns (all added by an additive migration):
   - `OrganisationStructures`: `Id` (integer key, always 1), `ConcurrencyToken`.
   - `Levels`: `Id` (text key: the stored level key, `Team` or `Portfolio`), `OrganisationStructureId` (foreign key),
     `Ordinal` (integer, 1 = where the work is done), `UnitSingular`, `UnitPlural`, `ItemSingular`, `ItemPlural` (text).
   - `ConnectionMappings`: `ConnectionId` (key, foreign key to `WorkTrackingSystemConnections`, cascading),
     `ConcurrencyToken`.
   - `ConnectionLevelMappings`: `ConnectionId` and `LevelId` (composite key, foreign keys), `ItemTypes` (list, stored
     as units store theirs).
   - `Teams` and `Portfolios`: `ItemTypeSubset` (list), `WorkItemTypesAsWritten` (list), `NeedsItemTypes` (boolean,
     default false).
   - `UnitTypeLossNotices`: `Id`, `StoredKind`, `UnitNumber`, `LostType`, `RecordedAt`.
   - `LeafCapacities` (slice 07): `TeamId` (key, cascading foreign key to `Teams`), `FeatureWip`,
     `AutomaticallyAdjustFeatureWip`, `UseFixedDatesForThroughput`, `ThroughputHistoryDays`,
     `ThroughputHistoryStartDate`, `ThroughputHistoryEndDate`, `ForecastFilterRuleSetJson`.
   Oracles read these through the declared query ports (`IOrganisationStructureQuery`, `ILeafCapacityStore`), never
   by column name.

3. **A unit's subset gets a new column** (`ItemTypeSubset`, authoritative) on both tables, plus `WorkItemTypesAsWritten` (the list this release last wrote into `WorkItemTypes`, which the start compares to
   detect a previous release's edit, ADR-274 rule 13) and `NeedsItemTypes`
   (MD7, set by any save that leaves a Jira, Azure DevOps or CSV unit with no types, and at upgrade for every such
   unit, MA8). Every save also writes the effective list to the old `WorkItemTypes` column (dual write, MA9), except
   for a flagged unit, which keeps its last list there; so the previous release, after a rollback, reads what each
   unit reads now. At each start the migrator gives a connection without a mapping one, and re-derives a subset whose
   `WorkItemTypes` was changed by the previous release, by ADR-274 rule 13. Lost types are recorded in `UnitTypeLossNotices` (stored kind, number, lost type), which replaces the
   old column as the recovery record (departure from ADR-274 rule 11, accepted by the maintainer).
4. **Schema by EF migration, data by a startup migrator.** `Create-Migration.ps1` generates the additive schema for
   both providers. The data step is `OrganisationStructureMigrator`, an `ISeeder` registered after
   `TerminologySeeder`. Its first-upgrade step runs only when no structure row exists; later starts only re-derive the subsets the
   previous release edited (ADR-274 rule 13), and a connection without a mapping row gets today's create defaults when
   it is first needed, never a mapping derived from its units. The entry is `ISeeder.Seed`. The first-upgrade step reads every Team, Portfolio and the eight
   Terminology rows, asks a **pure planner** (`OrganisationStructureMigrationPlan.From(...)`, return-only) for the
   levels, subsets, flags and notices, applies the plan in one transaction, and then **checks its own result**
   (every subset within its connection's list for its level; unit counts unchanged; no unit gained or lost a type,
   ignoring case). A failed check
   rolls back and stops the start with a structured fatal message, as the key-ring check does today. A second
   instance racing on PostgreSQL loses on the structure row's fixed key and re-reads.
5. **Case is folded in C#, with `StringComparer.OrdinalIgnoreCase`**, never in SQL: PostgreSQL compares text by case
   and SQLite's `NOCASE` folds ASCII only, so a SQL dedupe would answer differently per provider.
6. **Each extension (`LeafCapacity`, `DefaultSizeGuess`, `ForecastInputFingerprint`) is a table keyed by the
   unit's number on the one level it exists on**, with a cascading foreign key to that level's table, so deleting the
   unit deletes it (slices 07 and 10). The level is implied by the table, as for every other side table (ADR-273).
   Each is filled by its own idempotent startup step on the same pattern; old columns are kept, mapped read-only.
7. **Where the code lives (MA1, as S03i left it).** The structure, levels, mappings, use cases, migrator, ports and
   their adapters are written in the existing `Modules/OrganisationSetup/{Domain, Application, Adapters}`; `LevelId` and
   `TermPair` in its `Domain`; `UnitId`, `UnitCreated`, `UnitDeleted`, the views and the unit directory in its `Contracts`. The EF entities `Team`,
   `Portfolio` and `WorkTrackingSystemOptionsOwner` stay in `Models` (catalogued as Organisation set-up's), mapped by
   `LighthouseAppContext`. `LeafCapacity` and its store are Forecasting's (`Modules/Forecasting`).

## Consequences

- No id, URL, scope, report owner, snapshot row or foreign key changes. #6455 adds a level by adding a stored kind
  value and, if it keeps this shape, one more subclass and table; or it takes the contract step to one table with a
  (level, number) key, which this decision leaves open.
- The planner is tested without a database on every rule of ADR-274; the migrator is tested on SQLite and PostgreSQL
  with slice 01's fixture, a two-tracker install and a Customer-A-shaped install (M12).
- `HistoricalSchemaPatch` and every fixture that builds the schema by hand learn the new tables and columns.
- The `Unit` subclasses remain until a later contract step; an ArchUnit ratchet counts the places outside adapters
  that test for `Team` or `Portfolio` (OUT-4) and must only go down.
- The migrator is one more startup step; a failed self-check stops the start with the database untouched, which is
  louder than serving wrong numbers and recoverable by staying on the previous release.

## Alternatives considered

- **One `Units` table (TPH) with a (level, number) key, rows copied.** Rejected for this Epic: every foreign key
  would need retargeting or dual writes, which is a contract step, not expand-only.
- **EF TPC mapping on `Unit`.** Rejected: TPC needs keys unique across the hierarchy, and Team 3 and Portfolio 3 both
  exist (ADR-273); EF's identity map would confuse them.
- **A database view unioning both tables.** Rejected: view DDL in two dialects to keep in step with every column
  added later, for reads the store adapter can do with two queries.
- **The data step as raw SQL inside the EF migration.** Rejected: JSON text on SQLite and `text[]` on PostgreSQL
  mean two hand-written implementations of a majority-spelling rule, untestable without each provider, and SQL
  case folding differs per provider.
- **Lazy migration on first read.** Rejected: two code paths (migrated and not) live in every reader.
- **Levels as a JSON document in `AppSettings`.** Rejected: no constraints, no concurrency token of its own, and the
  settings table is the platform lane's.
