# Slice 01 — Rule-based blocked definition (FOUNDATION / Walking Skeleton)

**Job**: `job-config-admin-define-blocked-rules` | **Persona**: config-admin (Carlos Mendez)
**MoSCoW**: Must | **Est**: ~1 day | **Premium**: No

## Goal (one line)
Replace the hardcoded `BlockedStates` + `BlockedTags` with a flexible `BlockedRuleSet` (the existing
`WorkItemRuleSet`/`RuleEvaluator<WorkItem>`, ADR-013, Include semantics), reusing the
`DeliveryRuleBuilder` UI, and auto-migrate existing config into equivalent rule conditions.

## Why this is the walking skeleton
It is the thinnest end-to-end slice that proves the whole Epic's central hypothesis (the rule engine
that powers deliveries + throughput can also define "blocked"). It touches config write → persistence
→ `IsBlocked` computation → existing per-item badge + overview widget, with zero new read surfaces.
Every later slice derives its blocked signal from the single definition this slice establishes.

## Learning hypothesis
**Disproves "the rule model is expressive enough to replace the hardcoded blocked mechanism" if**: any
real customer's existing `BlockedStates`/`BlockedTags` config cannot be represented as equivalent rule
conditions, OR a team's real blocked definition (e.g. a Jira flag custom field) cannot be expressed
with the existing fields + operators.

## In scope
- `BlockedRuleSet` (WorkItemRuleSet) on the Team/Portfolio settings aggregate, replacing
  `BlockedStates` + `BlockedTags`.
- `IsBlocked` computed via `RuleEvaluator<WorkItem>` over `BlockedRuleSet` (Include semantics: matched
  = blocked). Removes the old `IsItemInList`-based `IsBlocked`.
- One-time auto-migration: each `BlockedStates` value → `State equals X`; each `BlockedTags` value →
  `Tags contains Y`; OR-combined (Mode "or").
- Reuse `DeliveryRuleBuilder.tsx` + `WorkItemRules.ts` in `FlowMetricsConfigurationComponent`.
- Settings DTO carries `blockedRuleSet` (changed contract — version-gate clients, see feature-delta).

## Out of scope (later slices)
- Blocked-time history capture and per-item duration (slice 02).
- Over-time chart (slice 03). Blocked→stale (slice 04). Flagged-field cleanup (slice 05).
- The synthetic "Flagged" label in `IssueFactory` stays UNTOUCHED this slice (cleaned in slice 05) —
  migrated `BlockedTags` containing "Flagged" keep working via the `Tags contains Flagged` condition.

## Production-data AC (drive via demo data + real connector)
- AC1: A team configured today with `BlockedStates=["Blocked"]`, `BlockedTags=["impediment"]` shows,
  after migration, a pre-populated OR rule set `State equals Blocked` / `Tags contains impediment`, and
  the SAME items read as blocked as before the change (no item changes blocked status).
- AC2: Carlos adds `additionalField.<flagId> isnotempty`, ORs it in, saves; an item with that flag set
  (and no blocked state/tag) now reads as blocked on the per-item badge and counts in the overview
  Blocked widget.
- AC3: On reload the rule conditions persist (read-your-writes); no code path still reads
  `BlockedStates`/`BlockedTags`.
- AC4 (@property): there is exactly ONE evaluation of blocked — `RuleEvaluator<WorkItem>` over
  `BlockedRuleSet`; the badge, the overview widget, and `IsBlocked` agree for every item.

## Dogfood moment
Migrate the Lighthouse team's own demo/real team blocked config to a rule set and confirm the blocked
overview widget count is unchanged post-migration.

## Cross-cutting
- **RBAC**: existing settings write gate (team-admin/portfolio-admin via `IRbacAdministrationService`,
  UI via `useRbac()`). No new surface.
- **Clients**: changed settings contract (`blockedRuleSet` replaces `blockedStates`/`blockedTags`) —
  version-gate; see feature-delta.
- **Website**: N/A (non-premium).

## Dependencies
None upstream. Foundation for slices 02–05.
