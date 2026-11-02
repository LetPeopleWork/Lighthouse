# ADR-DRAFT: A Team or Portfolio reads its parent from an ordered list of typed sources, stored as one JSON text value, ordered by one selector

**Status**: Draft (2026-11-02, Morgan, DESIGN wave, PROPOSE; revised after the DESIGN review). Promoted to
`docs/product/architecture/` with the next free ADR number in DELIVER. Story 6501. Extends ADR-193 (the selector is the
one tracker-neutral home of the rule) and ADR-241 (own value plus source); relies on ADR-242 for the refetch. One part is
**not decided** and is marked UPGRADE-1 below.

## Context

Parent Override Field holds one Additional Field id per owner and always falls back to the tracker's own parent. A setup
whose parents sit in two or more non-native places, or whose own parent is not a Feature, cannot be expressed. The
maintainer decided: one to five ordered sources, the first that yields a parent wins, the tracker's own parent is an
optional entry (not an implicit last fallback), a link entry with no stored Feature behind it yields nothing and the next
entry is read, a duplicate is refused, and the migration writes today's behaviour explicitly. Epic 6474 will restructure
the owner tables after this ships. The settings DTOs are the public `api/v1` surface.

## Decision

1. **Value.** `ParentSources`: an immutable, validated, ordered list of `ParentSourceEntry`, each of kind `TrackerParent`
   or `AdditionalField(id)`. One to five entries, no duplicate. Violations are returned as values. Every new type is a
   pure value; nothing in them touches a record or the database.
2. **Storage, pinned.** One text column per owner table (`ParentSources`) and one per grain on the connection
   (`ParentSourcesForTeams`, `ParentSourcesForPortfolios`): SQLite `TEXT`, PostgreSQL `text`, not `jsonb`. Mapped with
   `HasConversion` and a `ValueComparer` exactly like `StateMappings` and `CycleTimeDefinitions`: the serialiser is called
   with null options, so member names are PascalCase in declaration order and enums are integers. Stored text, for
   example: `[{"Kind":1,"AdditionalFieldDefinitionId":12},{"Kind":0,"AdditionalFieldDefinitionId":null}]`
   (`TrackerParent` = 0, `AdditionalField` = 1). NULL = no own value: the converter keeps NULL as NULL (never an empty
   list, which is not a legal value), reads blank text as NULL, and refuses a text the validating factory refuses instead of
   repairing it. The serialiser builds an entry only through a private constructor that throws on invalid input (public get-only properties, matching parameter names); the converter does not swallow that exception.
   The stored source column is reused.
3. **Migration.** Expand-only, generated with `Lighthouse.Backend/Create-Migration.ps1`. Additive columns plus a data step
   written as SQL in both providers' migrations (a text concatenation with an explicit `CAST(... AS TEXT)` of the
   integer): own override X becomes `[X, TrackerParent]`, own none becomes `[TrackerParent]`, followers NULL, connection
   defaults likewise. A test runs the real migration on both providers and checks that every migrated text survives
   `Serialize(Deserialize(text)) == text`. `Down` drops the new columns only.
4. **Legacy columns are written through for one release.** Whenever a list is set, the legacy integer column is written
   with the list's projection: `[X, TrackerParent]` writes X, `[TrackerParent]` writes NULL, any other shape writes its
   first field id or NULL. Nothing in the new build reads the legacy columns. A later release drops them.
   **Rollback limit.** A build one release back reads only the legacy column and then the tracker's own parent. It
   behaves exactly like the list for the two legacy shapes; for any other shape it resolves as the first field then the
   own parent, so the order, the later fields and a list without the own parent are not honoured. Edits made on the
   older build are not carried forward. This is a one-release convenience with that stated limit, not a guarantee.
5. **One rule, one place; the record keeps what the second pass needs.** Connectors read each entry into a reading (no
   value, field value, link candidates, the tracker's own parent, cannot be read) and attach the ordered readings to the
   record as a transient, unpersisted value for the duration of one refresh. `ParentSourceSelector` alone orders them:
   first entry that yields wins; a link entry yields only a Feature Lighthouse holds (Teams narrow; a Portfolio does not),
   a single candidate included, except for a legacy-mode owner if UPGRADE-1 (B) is chosen; the tracker's own parent only
   when listed. The selector only **returns** a choice; one helper (`HangUnder`) is the single place that assigns the stored
   parent and the shown link values, and an empty choice clears them. The connector runs a provisional pass without Feature
   knowledge and stops at the first link reading that needs a Feature test; the work item refresh then runs the full pass,
   with the stored Features, on exactly those records, and may retract a provisional key. To make that possible a single
   link candidate is now judged by the same narrowing as several (the placement function no longer treats one candidate as
   "not asked"), and the record's copy constructor carries the readings across the wrap that happens between the two passes.
   On Jira the tracker's own parent is the parent key, then, on Data Center, the instance's own parent field.
6. **Refetch.** The fetch fingerprint renders the ordered effective list in a form that can never equal a legacy
   rendering; `[TrackerParent]` renders as the old `-`. What the upgrade queues depends on UPGRADE-1 below: under (A)
   every Jira owner and every Azure DevOps override owner is re-read once (the Jira reading version moves every Jira
   owner's fingerprint, because SQL cannot tell which owners hold a link entry); under (B) nothing is queued.
7. **API.** Additive `parentSources` and `parentSourcesDefault` on the owner settings; `parentSourcesForTeams/Portfolios`
   on the connection defaults. The old integer members stay on the wire for one release as deprecated projections; an
   old client's echo cannot overwrite a richer list (act only if different, as in ADR-241).

### UPGRADE-1 (not decided): what the upgrade does to parents that are read today

No static migration reproduces today's parent results exactly, because whether the tracker's own parent is today's
fallback is decided at read time: on Azure DevOps by the grain and the dependency setup, on Jira Data Center by a field
read that an override owner does not make today. Writing `[X]` does not preserve them either: it drops the `ParentKey`
fallback on Jira, and renders a new fingerprint (a re-read). A single link candidate that is no Feature is held today
and falls through under the new rule. One decision for the maintainer; neither branch is the default.

- **(A) Accept the change.** Every override owner becomes `[X, TrackerParent]`, every other owner `[TrackerParent]`, and
  the fall-through rule applies everywhere, with a one-time re-read (every Jira owner through the Jira reading version,
  Azure DevOps override owners through the new fingerprint form). Some items whose override names nothing, or only a
  non-Feature, newly hang under their own Parent (on Data Center, the instance's parent field) and may start counting
  towards a Feature; a release note is required. No flag and no per-option branch.
- **(B) Preserve until edited.** A legacy-mode flag per owner reproduces today's read-time behaviour until the owner saves
  the list once: the override, else the tracker's own parent only where today's code would fall back; a single link
  candidate at face value; no Data Center field read for an override owner. Nothing is queued at the upgrade. Cost: the
  stored flag, read-time branches in the selector and both connectors (the Azure DevOps one depends on a computed
  condition), a second behaviour to document and test, and a later cleanup migration.

Recommendation: (A); the items that move are those whose configured sources already named nothing usable, and one rule
beats a hidden per-owner mode that must match a conditional Azure DevOps read path exactly. Full text: the DESIGN's DD-4.

## Alternatives considered

- **Child table** (owner type, owner id, position, kind, field id). Rejected: polymorphic owner key over two tables,
  an `Include` on every read that, forgotten, reads as "follows nothing" (the ADR-241 warning), and a re-key in Epic 6474.
- **Five flat columns.** Rejected: the cap becomes schema and reordering rewrites five columns.
- **`jsonb` columns.** Rejected: key order and spacing are normalised, so the migrated text could not match the
  serialiser byte for byte.
- **Sentinel field id for the own parent.** Rejected: a magic number reaches every consumer of field ids.
- **A `LinkType` entry kind.** Rejected: ADR-193 §5, a stored kind lies after a link type is renamed in Jira.
- **Each connector walks the list.** Rejected: the rule written per tracker (the ADR-193 `DependencySourceSelector`
  lesson) and fall-through needs stored Features a connector may not reach.
- **Persist the readings on the work item.** Rejected: a column, a migration and staleness rules for data only the same
  refresh uses.
- **A domain event for a list change.** Rejected: the fingerprint already drives the refetch (ADR-242), an event would be
  a second path to the same refetch. A deliberate exception to preferring events.
- **Freeze the legacy columns.** Rejected: every owner edited after the upgrade would resolve differently on rollback.
- **Dropping the legacy columns now.** Rejected: expand-only.

## Consequences

Positive: one ordering rule, tested as a table; Epic 6474 carries three plain nullable text columns; no new tracker
request, no new event, no new edge on the context map; the class of "own parent silently wins" disappears because it is an
entry.

Negative, stated honestly: the "field in use" guard loads one connection's owners and scans their lists in memory (the
legacy column holds only a list's first field, so it cannot serve as a filter); the data step builds JSON text in SQL on
two providers (probed by a round-trip test on both); the rollback is exact only for the two legacy shapes; two
fingerprint renderings coexist for one release; and an install with a link entry reads the stored Features once per
fetch where it read them only for ambiguous links before (under UPGRADE-1 (B) not for a legacy-mode owner with a single
candidate). What the upgrade does to already-configured owners is UPGRADE-1 (above).

## Enforcement

Rows 1 to 5 are architecture rules to add; none exists today. Rows 2, 3 and 5 are source-scan rules modelled on the
source-scan helpers and the "every tracker names the selector" rule of the existing dependency-source architecture test;
rows 1 and 4 are type-dependency rules (the nearest existing shape is its "reaches nothing that loads or stores" rule),
with edges read from the IL where a Release build would drop async ones. Each is watched to fail once before it is
trusted. Rows 6 to 9 are tests.

| # | Rule | Mechanism |
|---|---|---|
| 1 | The selector and its value types import no connector | New type-dependency rule |
| 2 | Every connector that reads the parent-source list (Jira and Azure DevOps) names the selector | New source-scan rule, copy of the dependency one |
| 3 | Nothing but the one helper assigns `ParentReferenceId` in production source, except a short allow-list with a reason each (Linear, CSV, a copy in the portfolio metrics service, the work item's own merge and copy); the shown link values are covered by behaviour tests (the selector returns, the helper writes) | New source-scan rule |
| 4 | Connectors never use stored Features or the feature repository | New type-dependency rule |
| 5 | Nothing reads the legacy columns except the write-through, the EF mapping and the migrations | New source-scan allow-list; the compiler alone does not find these readers because the legacy members are concrete properties |
| 6 | Fingerprint stable for legacy shapes, new forms never collide with them | Golden-value test plus a property test; `FetchShapingPropertyGuardTest` records the registered property |
| 7 | The migration keeps every parent and its text round-trips | Migration test on SQLite and Postgres, including `Down` |
| 8 | Write-through and rollback limit | Golden table list -> legacy value, and a rollback test against a stand-in for the older rule |
| 9 | List rules | Property tests on `ParentSources`; the frontend reducer mirrors them (mutation-tested) |
