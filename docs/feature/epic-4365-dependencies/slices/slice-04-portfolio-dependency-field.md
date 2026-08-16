# Slice 04 — Read dependencies from the field this Portfolio actually uses (free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-04 · **Estimate**: ~5h
**Reference class**: `ParentOverrideAdditionalFieldDefinitionId` end to end — the setting on
`IWorkItemQueryOwner:27`, its selector in the Portfolio settings form, its carry-through in
`PortfolioExtensions.cs:31` and `FetchFingerprint.cs:38`, and its read path at
`AzureDevOpsWorkTrackingConnector.cs:1012-1018` and `:1095-1106`. This slice is that mechanism a second
time, with one difference (D15).

## Goal

A Portfolio whose teams record dependencies in a custom field rather than the tracker's built-in link
gets the whole feature — column, dialog, warnings, and whatever the forecast does with them once
Epic #5792 ships — by naming that field once.

## IN scope

- `DependencyOverrideAdditionalFieldDefinitionId`, nullable, on `IWorkItemQueryOwner`, beside the
  parent override. Additive migration via `CreateMigration`, expand-only.
- The read path: when set, **skip the relation fetch entirely** and read the value from
  `AdditionalFieldValues`. This copies the early return at `:1014-1018` deliberately — the parent
  override's comment ("no need to load stuff if we have an override anyway") is the behaviour, not an
  optimisation to reconsider.
- **List parsing (D15)** — the one genuine difference from the parent override, which returns 0..1.
  Split on comma or semicolon, trim, resolve each entry, skip entries that resolve to nothing while
  keeping the rest. Entries are references in the connector's own form — Jira keys on Jira, work item
  ids on ADO, identifiers on Linear — which is `ReferenceId` space, so no normalisation layer is owed
  beyond Linear's lower-casing. Unit-testable in isolation, and where most of this slice's tests live.
- **Replace, not union**: while the override is set the native link is not read at all for that
  Portfolio.
- The selector in Portfolio settings, offered only for additional fields defined on that Portfolio's
  connection, with the same permission the parent override requires.
- `FetchFingerprint` gains the new setting, so changing it triggers a refetch exactly as changing the
  parent override does (`FetchFingerprint.cs:38`, `:81`). Missing this means the setting appears to do
  nothing until an unrelated change forces a refresh.
- Free on every instance (D9) — it feeds detection, and detection is free.

## OUT of scope

- Any per-Feature authoring of a dependency inside Lighthouse. Rejected under D4 and not in this epic
  in any form.
- A configurable separator (D15 fixes comma and semicolon).
- The same override on a Team. Features are fetched per Portfolio, so the Team owner has no consumer.
- Jira and Linear override support beyond what falls out of the shared port — their standard links
  land in slice 03, and the override is connector-agnostic by construction.

## Learning hypothesis

**Disproves** "a hand-maintained field yields resolvable references" **if** what people actually put in
such a column is not the tracker's reference form. The intended contract is settled (D15: Jira keys,
ADO ids, Linear identifiers), but `ParentReferenceId` holds one canonical id because a *connector*
wrote it, whereas this column is typed by a person — so full URLs, prose, and titles instead of keys
are all plausible in the same column across one Portfolio.

If it fails, the slice needs a normalisation step — parse a URL down to its id, strip a prefix — which
is a second slice, not a bigger version of this one.

**Confirms**, if it holds, that a third override of this shape is a copy of a known pattern.

## Why this slice exists (and what it is NOT for)

It serves ADO, Jira and Linear instances that record dependencies in a custom field rather than the
tracker's native link type. **It does not bring ServiceNow or CSV into the feature**, however tempting
that argument is: every connector supports additional fields, so the mechanism looks available to
them, but `ServiceNowWorkTrackingConnector.GetFeaturesForProject` throws `NotSupportedException`
(`:751-757`) — ServiceNow has no Features, so there is nothing for a dependency to be between. This
was checked during DISCUSS and is written down so the argument is not re-made mid-slice.

## Verify the premise first (30 min, before the migration)

Ask for one real example of such a field from an instance that uses one, and read what is actually in
it. This is the only slice in the epic serving a population the dogfood instance does not contain, so
the premise cannot be checked against `:5169` — it has to be checked against a real user's data or it
is not checked at all. If no example is available, say so in the verdict below rather than proceeding
as though it were confirmed.

## Acceptance criteria

AC-4.1 … AC-4.7 verbatim from `feature-delta.md`. The three that carry the slice:

- A field reading `1234;5678` yields two edges that appear in the count and the dialog (AC-4.1).
- With the override set, the connector performs **no** relation fetch (AC-4.2).
- With the override unset, behaviour is byte-identical to slices 01-03 (AC-4.5).

## Dependencies

Slices 01-03 — this slice changes where edges come from and nothing about what happens to them.
One additional field defined on the dogfood ADO connection carrying a dependency list, created by hand
for the manual confirmation.

## Dogfood moment

Same day: on `:5169`, define an additional field on the ADO connection, put a dependency list in it for
two Features, point one Portfolio at it, refresh, and confirm the column matches what the standard
links produced for the same pair. That comparison is the strongest evidence available without a real
user's instance.

## Commit gate

Normal — the approval gate is Epic #5792's only (maintainer, 2026-08-16).

## Learning hypothesis verdict

_Not yet run._
