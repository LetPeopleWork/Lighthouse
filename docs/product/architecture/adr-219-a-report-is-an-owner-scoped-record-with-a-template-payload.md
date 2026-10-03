# ADR-219: A Report is an owner-scoped record with a template-specific payload; delivery and schedule live outside it

- **Status**: Proposed (DESIGN, 2026-10-03)
- **Date**: 2026-10-03
- **Feature**: epic-5878-baseline (ADO Epic #5878 "Then & Now"; slices 01–10, Stories #6159–#6168)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)
- **Supersedes**: [ADR-209](./adr-209-a-report-is-a-response-not-a-record.md) in its *deferral* only. ADR-209's own
  decision for the Forecast Reality Check (a response, nothing stored) stands unchanged.

## Context

ADR-209 deferred the Report abstraction "to the arrival of the second Report kind" and wrote down six questions that
ADR would have to answer. Epic 5878 is that arrival, and it arrives with two more Report kinds already decided:

- **Then & Now (5878)**: freezes a past window at creation, reads a rolling Now on every view. Persisted,
  owner-scoped (Team or Portfolio), many per owner, deleted with its owner (D9, D10, D42).
- **Signals (5935)**: a *live* template on the same view and the same metric catalog, optionally snapshotted on a
  cadence; adds rules (D36).
- **Email Reports (5882)**: PDF / email / schedule / delivery as a capability of **every** report whatever its
  template (D36), rendering the real frontend components server-side (Spike 6052).

Constraints: expand-only migrations through `CreateMigration` on SQLite and PostgreSQL; a Community cap of 2 reports per
owner counted across all templates (D29); RBAC through the owner's Read/Write; a later cross-owner listing must not be
blocked (D36); creation ≤ 10 s and all-or-nothing (D26).

Precedents in the codebase: `DeliveryClosureRecord` (ADR-160) pins a frozen state as scalar columns plus JSON text
columns; `Team.RefinementSettings` (ADR-214) and `StateMappings` (ADR-064) persist structured values as JSON text
through a converter; `DeliveryNote` and `Delivery` are owner-scoped rows with a cascading foreign key.

## Decision

### The six questions, answered with three Report kinds on the table

| # | Question (ADR-209) | Answer |
|---|---|---|
| 1 | Entity or projection? | **An entity** with identity and a lifecycle (create, edit, delete). The Reality Check remains a projection; it is a *check*, not a Report kind. |
| 2 | Payload format? | **Typed per template in code, stored as one JSON text column** (`TemplatePayloadJson` + `PayloadSchemaVersion`). The compiler sees typed records; the schema sees one column. |
| 3 | Runner? | **In-request** for Then & Now: creation computes and stores in one request; the read computes Now in the request. No update-queue work (ADR-209 §3 still holds for that queue). A cadence runner, if 5935/5882 need one, is theirs to decide. |
| 4 | Completion event? | **None.** Nothing completes asynchronously. No `ReportCreated` domain event is published until a subscriber exists. |
| 5 | Retention, who deletes? | **The owner's editors, explicitly; and the owner's deletion, by database cascade.** No time-based retention. Licence lapse never deletes (D29). The stored data is aggregate numbers — no Work Item ids, titles or people. |
| 6 | Scope? | **Owned by exactly one Team or one Portfolio.** Every read model carries `ownerKind` + `ownerId`, so a later cross-owner listing is a new query returning the same summary item. |

### The shape

1. **One `Reports` table** for every template: `Id`, `TeamId?` / `PortfolioId?` (exactly one set; each an FK with
   `ON DELETE CASCADE`), `TemplateKey` (stable string, e.g. `then-and-now`), `Name`, `CreatedAt` (UTC instant),
   `ShownMetricKeysJson`, `TemplatePayloadJson`, `PayloadSchemaVersion`, `ConcurrencyToken`.
2. **Common vs template-specific.** Common: owner, template key, name, creation instant, shown metrics (the
   catalog is shared by Then & Now and Signals). Template-specific, inside the payload: for Then & Now the Then
   window, the Now length (ADR-222) and the frozen captures (ADR-221); nothing about the owner's settings (maintainer, D46). A frozen template stores
   captures; a live template (Signals) would store only its definition. The table does not care which.
3. **Templates are strategies** (`IReportTemplate`: key, create-from-request, edit, read). Adding a template adds
   one class and one payload record — no column, no migration.
4. **Delivery and schedule live outside the template and outside this row.** When 5882 lands it adds its own
   table referencing `Reports.Id` (cascade), and renders whatever the report's read returns. Nothing for it is
   built now.
5. **Payload evolution is additive.** Members are only added, as nullable; an absent member means "not captured"
   (D27). Renaming or removing a member needs its own ADR. `PayloadSchemaVersion` exists for the day a reshape is
   unavoidable; v1 writes `1`.

## Alternatives considered

- **EF table-per-hierarchy with typed columns per template.** Each template adds nullable columns and a two-provider
  migration; the column list becomes the union of every template — exactly the "four nullable columns nobody can
  delete" ADR-209 warned about. **Rejected.**
- **Common `Reports` table + a child table per template (+ a per-metric capture table).** Relational and queryable,
  but every template is a migration, and per-metric captures are heterogeneous (one percentile and limits for
  {Cycle Time}; two totals for {Work Item Age}) so they end up as JSON or as wide nullable columns anyway. Nothing
  queries captures by column. **Rejected.**
- **Polymorphic `OwnerId` + `OwnerType` without a foreign key** (the snapshot-table precedent) with a
  deletion handler. There is no `PortfolioDeleted` event to hang it on, the snapshot tables keep orphans today, and
  D42 makes deletion with the owner a promise. Two real foreign keys make the database keep it. **Rejected.**
- **Keep deferring (one more feature without a Report entity).** Then & Now must be stored (D10), so the only
  question is whether three Report kinds share a shape. **Rejected.**

## Consequences

- **Positive**: one migration for the whole Epic (slice 01); every later slice and template stores inside the
  shape; cascade with the owner is enforced by both providers; the cap counts one table; 5882 and 5935 attach
  without reshaping it.
- **Positive**: the Reality Check is untouched, and ADR-209's six questions are closed on the record.
- **Negative**: captures cannot be queried in SQL ("which reports froze a {Cycle Time} above 20 days?"). Nothing
  asks that.
- **Negative**: JSON evolution is a discipline, not a schema check — enforced by a round-trip test (a slice-01-shaped
  payload deserialises into the current type with later members null).
- **Risk named**: two concurrent creations can both pass the Community count and leave three reports. Accepted: the
  lapse rule (D29) already tolerates more than two, and creation stays blocked until the count drops.

## Enforcement

- ArchUnitNET: nothing outside `API` and the composition root depends on the Reports module.
- NUnit (both providers): deleting a Team or Portfolio removes its reports; a report row with both or neither owner
  cannot be saved.
- NUnit: payload round-trip from a slice-01-shaped document; `JsonSerializerOptions` is one cached instance (CA1869).
- A read of a report through another owner's route returns 404, never the report.
- Migration via `CreateMigration` only (DELIVER).

Cross-refs: [ADR-209](./adr-209-a-report-is-a-response-not-a-record.md) (superseded deferral),
[ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md) (pin precedent),
[ADR-214](./adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md) and
[ADR-064](./adr-064-cycle-time-definitions-storage-as-owned-collection-on-settings-aggregate.md) (JSON-text
precedent), [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) (modules,
concurrency tokens), ADR-220, ADR-221, ADR-222.
