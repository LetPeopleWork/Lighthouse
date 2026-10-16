# ADR-240: An inherited setting is the owner's own value plus a source, and the owner answers with the effective value

**Status**: Accepted (2026-10-16 — Morgan, DESIGN wave, interaction mode PROPOSE, pass 2; every *proposed* point of
pass 1 accepted by the maintainer, and the upgrade rule for values naming a deleted field changed by them).
**Feature**: `epic-6303-configure-once-per-connection` (ADO Epic #6303; Stories #6304, #6313)
**Decider**: Morgan (Solution Architect); D9–D11, D16, D18 and G-R1 (b) are the maintainer's (DISCUSS, 2026-10-16)
**Relationship to prior ADRs**: follows ADR-167's "nullable columns behind a paired mutator" over an owned type;
keeps ADR-140's fingerprint on the config aggregate; ADR-241 decides how a changed default reaches the queue.

## Context

Five settings point at a connection's own Additional Fields and are, in practice, the same for every Team and
Portfolio on a connection: Parent Override Field (Team and Portfolio, separate defaults), Dependency Field, Size
Estimate Field and Feature Owner Field (Portfolio), and the Estimation bundle (field, unit, non-numeric flag,
categories; Team and Portfolio). The maintainer decided that each owner stores *Connection default* or *Own* beside
the value, that `null` keeps meaning None, that owners empty at the upgrade follow and owners with a value own it,
that new owners follow, and that a PUT without the mode keeps the stored mode unless it sends a non-empty value that
differs from the effective one.

Facts from the code at `9d7086d77`:

- The five are plain `int?` columns (`WorkTrackingSystemOptionsOwner.cs:68-76`, `Portfolio.cs:21-25`) with no foreign
  key (`LighthouseAppContext.cs` configures one only for write-back mappings). Deleting an Additional Field therefore
  leaves owners pointing at an id that no longer exists, which silently breaks Jira parents and dependencies today.
- About fifteen sites read them directly: both connectors, `ParentSourceSelector`, `DependencySourceSelector`,
  `EstimateNormalizer`, `BaseMetricsService`, `WorkItemService.cs:757`, `WorkItemEstimateDto`, `FetchFingerprint.For`,
  the settings DTOs. Connectors are leaf adapters that may not depend on services or repositories (ArchUnitNET).
- The Team and Portfolio repositories always load the connection with the owner (`TeamRepository.cs:21-23`,
  `PortfolioRepository.cs:74-76`), so the connection's scalar columns arrive with every owner read.
- Enums are int-persisted and append-only in this codebase (`RefreshLog.cs:5-6`, ADR-167).

## Decision

1. **Source per setting, on the owner's own table.** A `SettingSource { ConnectionDefault = 0, Own = 1 }`
   column per inheritable setting: Teams get Parent Override and Estimation; Portfolios get those two plus Dependency,
   Size Estimate and Feature Owner. Each column arrives in the slice that ships its setting.
2. **Defaults as flat nullable columns on the connection row**: one field id per default (Teams' and Portfolios'
   Parent Override apart, D8) and the Estimation bundle as four columns per grain, one bundle for Teams and one for
   Portfolios.
3. **Migration, per slice, expand-only.** Add the source column with default `ConnectionDefault` (0) and the default
   column(s), then two data steps, hand-added to both providers' generated migrations:
   (a) clear the owner's value where it names no Additional Field of the owner's own connection — such a value is
   treated as **empty**; (b) `SET source = Own WHERE value IS NOT NULL`.
   With no default set on release day, a follower's effective value is `null`: exactly what an empty owner read
   before, and for a repaired owner the tracker's own parent / dependency link / nothing again — a deliberate, visible
   fix the release notes call out. **Nothing is queued at the upgrade**: a repaired owner's fingerprint moved, so its
   next scheduled update downloads everything; every other owner's fingerprint is byte-identical. New rows default to
   0, which is "new owners follow". For Estimation, step (a) looks at the bundle's field; a dangling field empties the
   whole bundle (field and unit `NULL`, the non-numeric flag false, the category list empty in each provider's stored
   form).
   **Still expand-only.** The schema change only adds columns. Steps (a) and (b) are data steps: `UPDATE`s of rows that
   drop no column, table or constraint, so an older build still reads the database (it ignores the new columns, and a
   cleared value reads as "no field set" — the id it replaces named nothing that build could find either). On both providers
   each step's predicate goes **owner → its connection → that connection's Additional Field definitions**: a value is
   dangling when no `AdditionalFieldDefinition` row has that id **and** the owner's own `WorkTrackingSystemConnectionId`.
   A field id that exists but belongs to another connection counts as dangling too, since the owner's connector would
   never find it. Written as provider-neutral SQL (`NOT EXISTS` over the definitions table), the same text in both
   generated migrations; the migration test runs it on SQLite and Postgres.
   **No foreign key** (recommended option, taken while the maintainer was AFK). Neither the owners' existing columns nor
   the new default columns get an `OnDelete(Restrict)` FK to `AdditionalFieldDefinition`:
   - adding an FK to an existing column on SQLite makes EF rebuild the whole `Teams` and `Portfolios` tables (copy, drop,
     rename) — two wide, central tables, on every installation, in a migration that should only add columns;
   - a Restrict FK would turn the D17 refusal into a `DbUpdateException` at `SaveChanges`, without the names of the
     default and the owners that the approved refusal copy needs; the application check is what produces them;
   - a default column on the connection row pointing at the connection's own child rows makes a cycle that the
     connection DELETE's cascade would then have to break by hand.
   The guard is therefore the application-level D17 check in the one place a single field is removed (the connection
   PUT; the connection DELETE, which takes its fields with it, is refused while any owner uses the connection,
   `WorkTrackingSystemConnectionController.cs:143-148`), the upgrade's repair for what is already dangling, and the
   tests below.
4. **The owner answers with the effective value.** The property every reader uses today
   (`ParentOverrideAdditionalFieldDefinitionId`, `EstimationUnit`, …) becomes read-only and returns the effective value;
   the stored own value moves to a renamed C# property mapped to the **same column**, so no column changes. The rule
   `(source, own, default) → effective` is one pure function in `Models` — the single resolver. Readers keep their code.
5. **Writes go through two mutators per setting** (locked): *follow* and *own(value)*, the setters private, so the
   compiler names every writer. Following clears the stored own value; for Estimation it clears the whole own bundle.
6. **Applying an incoming setting** (locked, G-R1 (b)): source present → applied as sent (`Own` with `null` is a
   deliberate None); source absent → `own(value)` only when the value is non-null and differs from the effective value
   as it stood before the request; otherwise nothing changes. Creating an owner applies the same rule to a new owner,
   whose stored source starts at `ConnectionDefault`. An owner hand-set to the same value as the default stays *Own*;
   nothing moves it to follow except its own admin.
   **The Estimation bundle under that rule** (resolves the open question; recommended option, taken while the
   maintainer was AFK): the four parts are one value. With no source sent, the bundle is "non-null" when its field is
   set, and it "differs" when any part differs from the effective bundle before the request: the field id, the unit
   (ordinal, case-sensitive string comparison), the non-numeric flag, or the category list (same values in the same
   order, since the order is the ranking of the categories). Non-null and different → `own(whole bundle)`; otherwise
   nothing changes, so an echo of a follower's bundle stays a follower. There is no per-part source: a part can never
   follow while another is owned.
7. **What a GET returns**: the existing id field carries the effective value; beside it the source and the
   followed default's display name, so an owner admin without connection rights reads what is followed (the owner
   page shows, for example, "Connection default (Story Points, Points)"). Source members on request DTOs are nullable.

## Alternatives considered

- **Source storage**: a JSON list of followed settings per owner (one migration, but each slice's back-fill becomes
  JSON surgery on two providers and "who follows X" stops being a plain query); a child table (one more `Include`
  on every owner read — a forgotten one reads as "follows nothing").
- **Defaults storage**: an EF owned type split into the connection row (groups them; ADR-167 rejected the same shape
  for a pair of columns, and an all-null owned group needs a required navigation to materialise); a 1:1 table (a new
  `Include` in three repositories, each one a silent-null risk).
- **Resolver**: keep the stored names and call a static resolver from every reader, with a guard test of allowed
  direct readers (fifteen call sites change; one missed site silently reads the own value); an injected domain service
  (connectors and the fingerprint cannot take one).
- **GET**: return the stored own value (a mode-unaware reader would see "None" for every follower; round trip equally
  safe).
- **Dangling values at the upgrade**: keep them as *Own* (pass 1's proposal: nothing changes on release day, but the
  owner keeps silently reading nothing and a later default never reaches it); queue the repaired owners at the upgrade
  (a burst of full downloads on every installation for a fix their next scheduled update makes anyway).

## Consequences

**Positive**
- Every reader of a setting is correct by construction; the connectors, selectors, normalizer, metrics and
  `FetchFingerprint.For` keep their code.
- No stored fingerprint moves at the upgrade except a repaired owner's: an `Own` owner renders its own id, an empty
  follower with no default renders `null`, both as before.
- An owner that pointed at a deleted field reads the tracker's own value again instead of nothing, and can follow a
  default set later.
- A follower that moves to another connection follows that connection's default without any extra rule.

**Negative, stated honestly**
- The effective value depends on the connection being loaded with the owner. It always is through the repositories;
  an owner built without one reads a follower's value as `null`.
- A renamed property mapped to an old column name is a trap for the next reader of `LighthouseAppContext`; the
  mapping says why.
- Under G-R1 (b) a mode-unaware client can no longer clear an owned value by sending `null`; it must send `Own` with
  `null`. No Lighthouse-Clients code sends these fields today.
- **Under G-R1 (b) a stale echo silently turns a follower into an owner** (accepted; the rule is the maintainer's).
  Example: a client loads a Team that follows the Teams' Parent Override default "Caused by"; a System Admin then
  changes that default to "Parent Link"; the client PUTs the Team back with "Caused by" and no source. "Caused by" is
  non-null and differs from the effective value before the request ("Parent Link"), so the Team becomes
  *Own ("Caused by")* and no longer follows. A client that knows about sources avoids this by sending the source it
  read; the browser always sends it. Lighthouse-Clients sends none of these fields today; the client changes that add
  them must send the source too. The Team's own concurrency token does not catch this case: the default changed on
  the connection row, not on the Team.
- A repaired owner changes on release day without an admin doing anything; the release notes must say so.
- No database constraint stops a dangling id; only the application check and the tests do.

## Architectural enforcement

| Rule | Mechanism |
|---|---|
| Follower and owner read alike | NUnit table: source × default set/unset × each setting, through the public property |
| Upgrade changes only the repaired | Pinned `FetchFingerprint.For` digests for an `Own` owner and an empty owner, committed before the first migration; a migration test on both providers seeds an own, an empty and a dangling owner, applies the data steps and checks: own stays `Own` with its digest, empty follows with its digest, dangling follows with `null` and nothing is queued |
| Only mutators write | Private setters; an ArchUnitNET rule that `API` reaches the own values only through the mutators |
| Round trip is harmless | AT: GET-then-PUT of every owner in an echoing and a property-dropping shape changes no source and no effective value |
| Stale echo behaves as documented | AT pinning the accepted consequence: a follower of "Caused by" is loaded; the default moves to "Parent Link"; the stale body is PUT back without a source → the Team is *Own ("Caused by")*. The same PUT **with** `source: ConnectionDefault` → still follows, effective "Parent Link" |
| Estimation bundle compares as a whole | Unit table over the four parts: each part alone differing (owns), all equal (unchanged), field null (unchanged), categories reordered (owns) |
| Dangling repair is owner-scoped | Migration test also seeds an owner whose id names a field of **another** connection: it is repaired |

## Cross-reference

- [ADR-167](./adr-167-source-binding-as-nullable-columns-behind-a-paired-mutator.md): the columns-behind-mutators precedent.
- [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md): the fingerprint this keeps stable.
- [ADR-241](./adr-241-a-connection-save-queues-the-owners-whose-own-fetch-fingerprint-moved-and-says-who-follows.md):
  the fan-out, the save-time log entry and the pre-save dialog.
- Full analysis: `docs/feature/epic-6303-configure-once-per-connection/feature-delta.md` (Wave: DESIGN).
