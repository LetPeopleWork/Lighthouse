# Slice 02 — a team reads its own table

**Story**: A (feature-delta.md) · **ADO**: #5611 · **Effort**: ~1 day + an EF migration
**Order**: second (D7). Independent of every open call, so it can start the moment slice 01's model holds — or on its own if slice 01 stalls on OC-1.

## Goal

An administrator points two Lighthouse teams on one ServiceNow connection at two different tables.

## Learning hypothesis

**Confirms** that team-scope configuration is a viable place to put ServiceNow read parameters.
**Disproves** "the per-team override is the cheap half" — the phrase the ADO body uses. `Team` has no
option bag, so this needs a new persisted column and a migration across every supported provider,
where slice 01 needed neither. If it also turns out to need its own validation path, the gap between
the two halves is wider still.

## IN scope

- A nullable per-team work-item table, persisted on `Team`.
- `GetWorkItemsForTeam` and `ValidateTeamSettings` resolve *team table ?? connection table*.
- `ValidateConnection` keeps the connection table, untouched (D5).
- Team settings + create-team wizard expose the field for ServiceNow only, driven by the schema —
  both twins (D6 discipline applies to any schema-shaped change).
- Expand-only EF migration via the `CreateMigration` PowerShell script.

## OUT of scope

- Any change to what a *single* team can read — that is slice 01.
- Portfolio scope (unsupported, permanently).
- A table picker or any instance introspection.

## Acceptance criteria

AC-A1..AC-A4 in `../feature-delta.md`.

## Dependencies

- Story 5577 landed and pushed — shares `ServiceNowWorkTrackingConnector`.
- Migration must be expand-only (standing project rule).

## Reference class

`ResolveWorkItemTable` (`ServiceNowWorkTrackingConnector.cs:632`) already centralises the resolution;
three call sites at `:87`, `:128`, `:340`. Only two of the three change — `:87` is `ValidateConnection`
and stays on the connection.

## Pre-slice SPIKE

None. No open call blocks this slice.
