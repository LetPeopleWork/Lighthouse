# Slice 28 — S10 Data ownership per context (logical only)

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L, spike first** · Pick-up group G5

## Goal

Every `DbSet` / table has one owning context, only the owner writes it, and code in one context never navigates into another context's entities. **Table ownership follows aggregate ownership from S08.** The context that owns an aggregate owns its tables, and the ID references between contexts come from S08's aggregate boundaries.

## IN (logical ownership only)

- **(a) Document**: an ownership table in ARCHITECTURE.md §8 mapping each `DbSet` → owning context → aggregate. There are 33 `DbSet`s today.
- **(b) Enforce owner-only writes**, by an ArchUnit rule or at repository level. A non-owner writer fails naming the file.
- **(c) References by ID in code**: a type in one context holds the other context's ID and never follows a navigation property into its entities. Enforced by a rule naming the offender.
- **Spike first**: does (c) need EF navigation properties removed or hidden, and what does that do to the two migration assemblies (`Lighthouse.Migrations.Sqlite` / `.Postgres`) and the `HintPath` build order? The goal is no schema change.

## OUT

- **Removing or replacing cross-context foreign keys or joins in the database.** **Database FKs and cascading deletes stay.** A single store makes FKs free integrity checks, and contexts rely on cascades. Reports, for example, go when their Team or Portfolio goes. Removing FKs only pays off with physically separate stores, which are a non-goal.
- **Physical separation of any kind**: no database, schema, table prefix or file per context (decided 2026-10-14). A database per module is rejected (D-6264-13) for these reasons:
  - Cross-context transactions: sync writes Work Items and snapshots together.
  - The single-file Standalone store.
  - Backup and restore of one store.
  - No independent scaling need inside a single binary.

## Hypothesis

S08's aggregates give an unambiguous owner for nearly every table, and ID-only references are achievable in code without a schema change. **Disproved if** many tables are written by two contexts for good reasons (then the S01 context cut needs revisiting), or if (c) forces a schema change (then stop at (a) + (b) and record why).

## Acceptance criteria

- [ ] Spike result recorded before any change.
- [ ] Ownership documented for all `DbSet`s and derived from S08.
- [ ] Given a non-owner context writes a table, when the architecture tests run, then they fail naming the writer.
- [ ] Given code navigates into another context's entity, then a rule fails naming it.
- [ ] Database FKs and cascades unchanged (a Report still goes when its owner goes). Single store, Standalone with zero external dependencies, backup/restore unchanged. Behaviour-free.

## Dependencies

- Hard: all S03*, S08 (aggregate catalogue).
- Soft: S13 (aggregate boundaries in persistence), before or alongside. S13 removes the cross-context navigation, such as Feature→Forecasts, that this Story's rule (c) then forbids.

## Notes for DESIGN

- Migration DLLs are `HintPath` references: build first, or `dotnet ef` reports pending model changes.
- Any unavoidable mapping change is expand-only via `CreateMigration`, and `HistoricalSchemaPatch` needs any new column.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (spike, then one context's tables per slice).
