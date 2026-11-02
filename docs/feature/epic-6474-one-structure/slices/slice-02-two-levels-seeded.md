# Slice 02 — Two levels seeded from today's Terminology and types

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day · depends on: 01

`infrastructure_rationale`: names and item types need one source before any screen shows a level (D6, D12).
Prepares `job-config-admin-describe-our-levels-once`.

## Problem

Priya Raman renamed Team to "Squad" and Portfolio to "Initiative" under Terminology. If levels start with
"Team" and "Portfolio", her words vanish; if both stores keep names, they drift on the next edit.

## Learning hypothesis

Terminology's eight structure keys can be served from the levels without any frontend change. **Disproved if**
`GET /terminology/all` cannot return byte-identical entries (then the frontend reads levels directly in 05).

## IN

- A level store with exactly two levels: level 1 (leaf, generates throughput) and level 2 (the Feature level).
  Each holds unit name (singular, plural), item name (singular, plural) and its item types.
- Migration (expand-only, via the CreateMigration script):
  - names taken from today's Terminology values, custom or seeded (`team/teams`, `workItem/workItems` on level
    1; `portfolio/portfolios`, `feature/features` on level 2);
  - level 1 types = union of all Teams' types; level 2 = union of all Portfolios' types; deduped
    ignoring case (M6); an install with no units gets today's create defaults (User Story, Bug / Epic).
- `GET /terminology/all` returns the same 8 entries, read from the levels; `PUT /terminology` writes them to the
  levels (Premium, System Admin, as today). The seeder stops seeding those 8 keys as Terminology rows.

## OUT

- Units knowing their level (03), any UI (05), types used by the fetch (04).

## Domain examples

1. Priya's install: Teams read {Story, Bug}, {Story, Bug, Task}, {User Story, Bug}; level 1 lists Story, Bug,
   Task, User Story — "Bug" once.
2. Free-tier install with untouched Terminology: levels named Team/Work Item and Portfolio/Feature.
3. Fresh install, no units: level 1 lists User Story and Bug, level 2 lists Epic.

## UAT scenarios

### Scenario: The organisation's own words carry over to the levels

Given Priya renamed Team to "Squad" and Feature to "Initiative"
When the install is upgraded
Then level 1's unit is "Squad" and level 2's item is "Initiative"
And the Terminology list read by the app still says "Squad" and "Initiative"

### Scenario: Each level lists every type its units read, once

Given Teams reading "Bug" from Jira and "Bug" from Azure DevOps
When the install is upgraded
Then level 1 lists "Bug" once

### Scenario: Renaming through Terminology lands on the level

Given an upgraded install
When a System Admin renames Team to "Crew" through Terminology
Then level 1's unit is "Crew"

### Scenario: Nothing anyone reads has changed

Given the pinned numbers of slice 01
When the upgraded install refreshes
Then every unit's numbers equal the pinned numbers

## Acceptance criteria

- [ ] An upgraded or fresh install has a Team level and a Feature level.
- [ ] Every screen shows the same words as before the upgrade, custom ones included.
- [ ] Each level lists every type its units read, once, ignoring case (M6).
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-3 (one source for names).

## Technical notes

- `GET /terminology/all` returns the same 8 entries for existing installs, served from the levels.
- Until slice 06, `PUT /terminology` keeps today's Premium guard for the 8 keys; slice 06 frees them (M7).
- Expand-only: the 8 Terminology rows may stay in the table, unread, until a later release removes them.
- DESIGN adds migration tests for two-tracker installs (Jira + Azure DevOps Teams on one level) and for
  Customer-A-shaped installs (Change as the Feature type), both migrated as they are (D10, M12).
- `HistoricalSchemaPatch` and fixtures need any new table (memory).
