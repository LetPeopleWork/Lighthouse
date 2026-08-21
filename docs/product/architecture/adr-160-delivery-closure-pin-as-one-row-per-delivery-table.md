# ADR-160: The closure pin is a one-row-per-Delivery table keyed on `DeliveryId`, not a flag on the daily series and not an FK into it

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 04)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

DISCUSS D1 fixed the *shape* of what an archived Delivery reads from — the `DeliveryMetricSnapshot`
column set — because `FeatureBreakdownJson` is already the serialised Feature grid, and a second
encoding of the same rows would be two things to keep in step. D1 did not fix where the pinned row
lives.

`DeliveryMetricSnapshot` carries a unique index on `(DeliveryId, RecordedDay)`
(`Data/LighthouseAppContext.cs`). Any mechanism that pins by pointing *into* that series inherits
that key, and two ordinary user actions collide with it:

- **Archiving on a day the forward recorder has already run.** The `(DeliveryId, today)` row exists.
- **Archive → un-archive → re-archive inside one day.** AC-05.7 requires the second archive to
  re-pin for the new closure moment, and requires the un-archive not to have destroyed the record.

AC-05.7 additionally requires that a Delivery is never left with two competing pins.

AC-04.4 requires the pinned record to be complete even for a Delivery the recorder never ran for, so
the pin is **computed at archive time**, not copied from an existing row. A pin that is an FK into
the series therefore has to *write a series row*, on a day key that may already be taken.

## Decision

**A separate table, `DeliveryClosureRecord`, whose primary key IS `DeliveryId`.**

Five points that are part of the decision:

1. **One row per Delivery is the primary key, not a constraint.** Two competing pins are
   unrepresentable rather than merely rejected, which is what AC-05.7 asks for. No partial index, no
   application-level rule, nothing to enforce in review.

2. **The `(DeliveryId, RecordedDay)` key is not in the picture at all.** Every collision path above
   collapses to a plain upsert of one row keyed by `DeliveryId`. Archiving on an already-recorded day
   is a non-event; re-archiving after an un-archive overwrites the single row. **The Slice 04 SPIKE
   was scoped to resolve this collision — the collision does not exist under this decision, so the
   SPIKE reduces to verifying the additive migration on a real provider, which the ledger requires
   anyway because InMemory skips migrations.**

3. **Same shape, one encoding.** The value columns mirror the snapshot's
   (`TotalWork`, `DoneWork`, `RemainingWork`, `EstimatedItemCount`, `ForecastHowMany`,
   `LikelihoodPercentage`, `WhenDistributionJson`, `FeatureBreakdownJson`, `TargetDateAtSnapshot`),
   and both tables are written by **one** projection and read by **one** parser
   (`DeliveryMetricsHistoryDto.ParseFeatureBreakdown`). D1's objection was to a second *encoding*,
   not to a second table; this keeps the encoding singular.

   **The closure record additionally carries what the daily series does not need but the archived row
   does** (added 2026-08-21): `HasSufficientData`, `TeamsWithoutForecastJson`, `SelectionMode`,
   `RuleDefinitionJson` and `RuleSchemaVersion`. Without these, a Delivery archived while
   un-forecastable would render as CANNOT_FORECAST naming no teams, where on its closure day it read
   INSUFFICIENT_FORECAST_DATA naming them — the record rewriting itself, which is the single failure
   this epic exists to remove. `HasSufficientData` defaults to `true` on the DTO, so an absent value
   does not fail safe; it fails *confident*, which is worse. This is the last cheap moment to add
   them: same new table, same additive migration.

4. **Archiving state is `Delivery.ArchivedOn` (`DateOnly?`), separate from the pin.** `null` means
   active. Un-archive clears `ArchivedOn` and **leaves the closure row in place**, which is exactly
   AC-05.7. One nullable column carries both the state and the "Archived on {date}" marker, so
   "archived but with no archived-on date" cannot be represented. It is `DateOnly` because the ledger
   rule for persisted day keys is `DateOnly`, never `DateTime` — a `DateTime` column is in reach of
   the global `Properties<DateTime>()` UTC converter, which shifts a local-kind midnight onto the
   previous day on write. The value comes from `ILighthouseClock`, never `DateTime.UtcNow.Date`.

5. **Cascade matches the snapshot lifecycle.** `DeliveryClosureRecord.DeliveryId` → `Delivery` is
   `ON DELETE CASCADE`, as ADR-048 already established for `DeliveryMetricSnapshot`. AC-04.9's hard
   delete still removes everything, and no `DeliveryDeleted` event is introduced.

## Alternatives considered

- **A nullable `ClosureSnapshotId` FK on `Delivery` pointing at a `DeliveryMetricSnapshot` row.**
  Attractive because the FK's cardinality alone gives "at most one pin". **Rejected** — it makes the
  pin and that day's historical record *the same row*, so re-archiving on an already-recorded day
  either overwrites a day of metrics history (silently changing a chart the user already read) or
  violates the unique key. It also introduces a second FK path `Delivery` → `DeliveryMetricSnapshot`
  → `Delivery`; on a provider that rejects multiple cascade paths the new FK has to be `NoAction`,
  so "deleting a Delivery cleans up after itself" stops being uniform across the two tables.

- **An `IsClosure` boolean on `DeliveryMetricSnapshot`.** The smallest schema change. **Rejected** —
  "at most one closure row per Delivery" degrades from a key to a rule that application code must
  keep. It *is* expressible as a partial unique index, but EF's `HasIndex().HasFilter()` takes
  provider-specific raw SQL into a model shared by the Sqlite and Postgres migration assemblies, so
  the invariant would have to be written twice in two dialects and could drift between them. It also
  still collides on `(DeliveryId, RecordedDay)`, so it solves nothing that made this decision hard.

- **A denormalised archive table with its own encoding of the Feature grid.** **Rejected by D1** —
  two encodings of the same rows, and the second one would have to be kept in step with a payload
  (`FeatureBreakdownJson`) that ADR-120 has already extended once.

## Consequences

- **Positive**: every collision path named in DISCUSS becomes a single-row upsert, and AC-05.7 holds
  because of the key rather than because of a check.
- **Positive**: archiving never writes into the metrics history, so an archive cannot retroactively
  change a chart someone has already read. K2's "0 snapshot rows lost per archived Delivery" holds
  trivially — archiving does not touch that table.
- **Negative**: two tables carry the same value-column shape. Mitigated by one shared projection and
  one shared parser, pinned by a test — **not** by a shared EF base entity, which would couple two
  genuinely different lifecycles (a daily append-only series vs. a mutable single row) for the sake
  of column reuse.
- **Negative**: "is it archived" and "what was pinned" are now two facts and can in principle
  disagree. Both writes happen inside one `SaveChanges` in the archive application service, and an
  integration test asserts the pair — an archived Delivery always has a closure record, and archiving
  a Delivery that already has one replaces rather than duplicates it.
- **Deleting an archived Delivery destroys its pin, and that is intended.** AC-04.9 keeps hard delete
  available on an archived Delivery, and the cascade takes the closure record and the snapshots with
  it. Archive is the alternative to deletion, not a protection against it — a user who chooses delete
  after archiving is choosing to lose the record. Worth stating because "archived" and "safe from
  deletion" are easy to conflate, and the UI wording should not imply the second.
- **No backfill.** Existing Deliveries take `ArchivedOn = null` from the nullable column and have no
  closure record, which is correct: none of them is archived. The archived read path only runs when
  `ArchivedOn is not null`, so an absent closure record is never dereferenced. The release-time
  assumption is written down rather than assumed — **no production Delivery is archived at release**,
  because the capability does not exist until this epic ships.
- **Migration**: expand-only and additive — one new table, one new nullable column on `Delivery`. One
  migration per provider (Sqlite, Postgres) generated via the `CreateMigration` script, never
  `dotnet ef migrations add`. Verify on a real provider; InMemory skips migrations.
- **Reuse verdict**: `DeliveryMetricSnapshot` → **REUSED AS SHAPE** (neither extended nor subclassed).
  `DeliveryMetricsHistoryDto.ParseFeatureBreakdown` → **REUSED AS IS**.
  `DeliveryClosureRecord` → **CREATE NEW** (no existing table can hold a one-per-Delivery pin without
  inheriting the day key that causes the collisions).
- Cross-refs [ADR-048](./adr-048-delivery-metric-snapshot-store.md) (the store whose shape this
  reuses and whose cascade convention this follows),
  [ADR-050](./adr-050-metrics-history-endpoint-and-snapshot-schema.md) (the parser reused for reads),
  [ADR-051](./adr-051-per-snapshot-target-date-capture.md) (`TargetDateAtSnapshot`, carried into the
  pin so an archived Delivery still knows the target it was scored against),
  [ADR-161](./adr-161-archived-delivery-read-path-cannot-see-live-features.md) (what reads this row).
