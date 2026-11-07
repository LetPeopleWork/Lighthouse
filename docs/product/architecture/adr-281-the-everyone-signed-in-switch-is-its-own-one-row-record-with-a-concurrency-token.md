# ADR-281: The everyone signed in switch is its own one-row record with a concurrency token, and it can only say "view"

**Status**: Accepted (decided 2026-11-07, not yet shipped, Epic 6577)
**Date**: 2026-11-07
**Feature**: `docs/feature/epic-6577-system-wide-access/`

Sits beside ADR-280, which makes a group's system-wide rule a row of the group mappings. This decision is separate because
the switch has no group, and because it is the only part of the feature that needs a schema change.

## Context

A System Admin can let every signed-in person read every Team and Portfolio with one switch. It is off after install and
upgrade, it can never grant Admin, and when it cannot be read nobody gains access. Two System Admins changing it at once
must follow the stale-edit rule of other configuration: the token is read on GET, echoed on save, and a stale save
answers 409 through `ConcurrencyConflictExceptionFilter`.

It cannot ride on `GetVirtualPermissionsAsync`, which returns nothing without group values, and the switch must work with
no group claim configured.

## Decision

1. **Own record.** `EveryoneSignedInSetting`, an `IConcurrencyTokenEntity` with one boolean, `EveryoneCanView`, in
   `Models/Authorization`. One row with a fixed id, owned by Access.
2. **The value says view, and nothing else.** There is no role column, so "everyone is Admin" cannot be stored, and no
   check is needed to refuse it.
3. **Migration and seeding.** One additive table, generated with `Lighthouse.Backend/Create-Migration.ps1`. An idempotent
   `ISeeder` writes the row with `EveryoneCanView` false when it is absent, so it also comes back after a restore from an
   older backup.
4. **Fail closed.** A missing row reads as off. A failed read throws, and the request ends in an error, never a grant.
5. **Read where it cannot depend on groups.** `GetOwnerEffectivePermissionsAsync` reads it after a profile has been
   resolved, outside the group path. On, it adds a Viewer reach on all units to `EffectiveAccess`. A caller with no stable
   subject, and a key whose owner has no profile, stop before that point.
6. **Endpoints.** `GET` and `PUT authorization/everyone-signed-in`, both System Admin only, carrying `everyoneCanView` and
   `concurrencyToken`. `PUT` refuses a missing token, because this endpoint has required it since it first shipped. A
   stale token answers 409 through the existing filter. The Access tab learns the state from the unit's
   `system-wide-rules` read, so nobody else needs this endpoint.
7. **Who changed it is logged.** One Information line per change with the actor's profile id, as for rules.
8. **Removed people do not come back through a session.** `CurrentUserProfileService` lets a key speak only for a person who
   still exists. That guard widens to embed principals, because they also carry a stored subject, not a fresh sign-in.
   Without this, a person removed during an embedded session would get a new empty profile on the next request, harmless
   until now, and with the switch on would read everything until the session ended.

## Alternatives considered

- **An `AppSetting` key with a compare-and-swap on the old value.** Rejected: `AppSetting` has no token, the row must be
  seeded anyway or a conditional update fails forever, `ExecuteUpdateAsync` does not run on EF InMemory so its tests move to
  containers, and it would be a second stale-edit shape beside the one all other configuration uses.
- **Add a token to `AppSetting`.** Rejected: every setting would join a concurrency rule only one of them needs.
- **A row of the group mappings with a new holder column.** Rejected: on would mean a row exists, so there is no token to
  compare. `ScopeId` is null, so the unique index cannot stop two rows, and a double turn-on followed by one turn-off could
  leave the switch on. It would also need a sentinel or a nullable group value, and "everyone as Admin" would become
  storable.
- **An `OptionalFeature`.** Rejected: no token, and it is general product configuration on the System settings list, not
  Access.

## Consequences

Positive: the stale-edit rule comes for free and tests run on EF InMemory; the switch cannot grant more than reading; a
missing row is safe.

Negative, stated honestly:

- One small table for one boolean, and one more query on each permission resolve. The number of queries still does not
  grow with units.
- The line on a unit's Access tab merges two sources, the mappings and this record, in one read model.
- Embed principals stop refreshing what the profile update writes for them, unless the guard is shaped to keep it.
  `LastSeenAt` is the field that matters; delivery has to check who reads it for embed viewers and keep that behaviour.

### Rollback limits

A build one release back ignores the table, so the switch is simply off there. The row survives.

## Enforcement

| # | Rule | Mechanism |
|---|---|---|
| 1 | Off after install and upgrade, and when the row is missing | Tests on the seeder and on a database with no row |
| 2 | A stale save is refused on the real databases, not only on EF InMemory | One test on SQLite and PostgreSQL through the shared test container |
| 3 | The endpoints declare System Admin access | The existing `EveryActionDeclaresItsAccessArchitectureTest`, plus tests that a Team Admin is refused |
| 4 | With no group claim configured, the switch still reaches a signed-in person | A behaviour test with `GroupClaimName` empty |
| 5 | A removed person's key and embedded session read nothing with the switch on, and create no profile | Behaviour tests on both principals |
