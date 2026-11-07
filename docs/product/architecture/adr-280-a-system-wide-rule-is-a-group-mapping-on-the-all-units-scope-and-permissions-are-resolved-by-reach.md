# ADR-280: A system-wide rule is a group mapping on the AllUnits scope, Admin on all is one generic UnitAdmin role, and permissions are resolved by reach in one pure value

**Status**: Accepted (decided 2026-11-07, not yet shipped, Epic 6577)
**Date**: 2026-11-07
**Feature**: `docs/feature/epic-6577-system-wide-access/`

Extends ADR-004 (a limited API key is the intersection of its owner's rights and its own rows) and ADR-002 (reads for a
unit have their own endpoint, guarded by the rights to manage that unit). ADR-281 holds the everyone signed in switch.

## Context

A person with no role on a Team sees nothing, so organisations made people System Admin just to read, or mapped a group
per Portfolio. The maintainer decided that a group can hold a role on every Team and Portfolio, including ones created
later: Viewer on all, and Admin on all, which is Team Admin plus Portfolio Admin on every unit and nothing a System Admin
alone may do. Such a rule only adds: where a person also holds a role on a unit, the higher of the two wins, whereas on a
unit today an explicit role replaces a group role, and that stays. A rule must have a distinct "applies to" part whose
first value is all Teams and Portfolios, so another value could come later without changing the read check or the line
on a unit's Access tab.

Three facts about the code shaped the answer.

- `RbacGroupMapping` already holds group, role, scope type and scope id, and `GetVirtualPermissionsAsync` already returns
  every mapping of the caller's groups.
- Effective permissions are a private dictionary keyed by exact scope, and seven places in `RbacAdministrationService`
  read it. No caller outside the class sees it.
- `UserRole` has no value that means admin of both unit types.

## Decision

1. **The rule is a row of `RbacGroupMapping`.** `PermissionScopeType` gains `AllUnits` (value 3) with a null `ScopeId`.
   The scope type is the "applies to" part: the Settings table and the Access tab line read it from the row, never from
   the role. No schema change and no migration. New enum values are appended, never renumbered.
2. **Admin on all is `UserRole.UnitAdmin` (value 4), shown as "Admin".** It is valid only on `AllUnits`. It has the
   priority of a Team Admin or Portfolio Admin, and reads as `TeamAdmin` on a Team and `PortfolioAdmin` on a Portfolio, so
   the write checks keep comparing the roles they compare today. It can never hold a System Admin right:
   `CanManageRbacAsync` asks only for `SystemAdmin` on the System key.
3. **Valid pairs.** `IsValidGroupMappingScope` accepts `Viewer` and `UnitAdmin` on `AllUnits` with no scope id, and keeps
   every pair it accepts today. API key scopes and member roles do not accept the new values: `MapScopeEntryToRequirement`
   has no arm for them, and the member setters take only unit roles. So a key limited to all units, and a system-wide role
   for one named person, cannot be stored.
4. **`EffectiveAccess` replaces the dictionary.** A pure, immutable value in `Models/Authorization`, built once per
   resolve. It holds three things apart: roles on units, the System role, and reach, which is a list of (applies to, role).
   `RoleOn(unit)` answers the higher of the unit role and every reach that covers the unit. The read and write checks, the
   any-scoped-admin check, the summary and the key coverage check all ask it. The explicit-replaces-group merge happens
   before it, on unit keys only, and is unchanged.
5. **"Applies to" is a value with one question.** `AppliesTo.Reaches(unit)` is the only place that decides whether a rule
   covers a unit. Nothing else tests for `AllUnits`, except the code that reads the stored scope into an `AppliesTo`.
6. **Limited keys get no reach.** The intersection with a key's rows yields exact unit roles only. A key is covered when
   the owner's `RoleOn` that unit is at least the key's role, checked on each request. Keys without rows carry the owner's
   access whole.
7. **`my-summary` keeps its contract.** For a caller with an admin reach, `adminTeamIds` and `adminPortfolioIds` are filled
   from the units that exist at that moment, through `Reaches`. `useRbac()` and every gate built on it do not change.
8. **A unit's rules have their own read.** `GET authorization/teams/{teamId}/system-wide-rules` and the Portfolio twin,
   guarded by the rights to manage that unit's members. They answer role plus group name or "everyone signed in", and no
   mapping id. System Admin groups are left out: they are not a role on units, and their name stays away from scoped admins.
9. **Removing a rule removes every row with the same group, role and scope.** `ScopeId` is null, and SQLite and PostgreSQL
   treat nulls as distinct in a unique index, so a race can store two rows. Removing one must not leave the rule alive.
10. **Who changed a rule is logged.** One Information line per add and remove, naming the actor's profile id and the group
    value, in the existing log. Mappings logged nothing before; a rule reaches every unit, so it should say who.
11. **No domain event.** Permissions are computed on each request, and a rule names no unit, so nothing has to react to a
    rule change or to a Team created later.

### Rollback limits

A build one release back ignores the new rows: they match no exact key, the role has priority 0, and the old Settings page
filters them out. People reached only through a rule lose that reach until the newer build returns. The rows survive.

## Alternatives considered

- **Reuse `ScopeType = System` with a null scope id.** Rejected: "System" would mean both the instance and every unit, so
  the "applies to" text would have to be guessed from the role, and the rule would share its key with System Admin and rely
  on highest-role-wins to stay apart.
- **A new `SystemWideRule` table with holder, role and applies-to.** Rejected: a migration, a second place that says "this
  group gets this role", and a Settings table merged from two stores, for no behaviour the mapping row cannot give.
- **Store `TeamAdmin` on the rule and read it as admin of everything.** Rejected: the stored word is false on every
  Portfolio, and labels would need a special case.
- **Two rows, one per unit type.** Rejected: two "applies to" values and two table rows for what the maintainer decided is
  one generic scope.
- **Expand rules into per-unit keys when resolving.** Rejected: every permission check, including the System Admin check,
  would read the unit tables, and its work would grow with the units. It would have left the seven readers untouched, but
  they are all in one class, and the higher-role rule belongs in one value either way.
- **A flag such as `adminOnAll` on `my-summary`.** Rejected: a second gating path in the browser that a later "applies to"
  value would have to replace, and a limited key would need its own exception.
- **Add the rule rows to the unit's existing `group-mappings` read.** Rejected: they would land in a table with a Remove
  button, and Remove on a unit must not end a system-wide rule.
- **A domain event on a rule change.** Rejected: nothing reacts to one, and a change must apply on the next request anyway.
  A deliberate exception to preferring events.

## Consequences

Positive: no migration for the rule; callers outside the service do not change; the higher-role rule exists once and is
tested as a pure value without a database; a later "applies to" value changes `AppliesTo` and its stored form, not the read
checks or the Access tab line.

Negative, stated honestly:

- `PermissionScopeType` is shared with `UserPermission` and `ApiKeyPermission`, so those tables could hold `AllUnits` if a
  future write path forgot to validate. Only the validation in decision 3 prevents it, and tests pin it.
- A value that needs a unit's name, such as a pattern, needs more than the id `Reaches` receives today. The unit reference
  grows then, or the value resolves when it is loaded. The read checks stay as they are.
- `my-summary` carries one id per unit for a person with Admin on all, and reads unit ids to do so, on each call.
- A unit created while a page is open gains its admin controls for that person on the next page load, not sooner.
- Creating or removing reports for many owners still checks permissions per owner, so its cost grows with the owners
  selected. That is existing behaviour, also for System Admins; a person with Admin on all makes large selections likelier.
- The website's "four roles" wording has to be checked when this ships.

## Enforcement

| # | Rule | Mechanism |
|---|---|---|
| 1 | Only the Access service reads `RbacGroupMappings` and `UserPermissions` | A new architecture test, reading compiled IL because a Release build drops async edges from ArchUnitNET's view |
| 2 | Every `UserRole` and `PermissionScopeType` value has an answer in `EffectiveAccess` and in the valid-pairs table | A table test that enumerates the enums, so a new value fails until it is placed |
| 3 | Every new endpoint declares its access | The existing `EveryActionDeclaresItsAccessArchitectureTest` |
| 4 | An explicit role on a unit still replaces a group role there | The existing `CanWriteTeamAsync_ExplicitViewerOverridesVirtualTeamAdmin_ReturnsFalse` |
| 5 | A rule adds and never narrows; a limited key stays at its exact scope | Table tests on `EffectiveAccess`, and property tests that a key never exceeds the lower of its limit and its owner's role |
| 6 | Admin on all reaches nothing that stays with a System Admin | One test per row of the capability table |
| 7 | Permission cost does not grow with units | A test that counts queries for a list with 200 Teams |
