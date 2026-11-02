# Slice 06 — Rename a level's unit and item

Epic #6474 · **visible** · `job_id: job-config-admin-describe-our-levels-once` · ≤ 1 day · depends on: 05

## Elevator Pitch

- **Before**: to call Teams "Squads", Priya hunts for `team` and `teams` among twenty Terminology fields, and
  only with Premium.
- **After**: she clicks Edit on level 1 and types "Squad" / "Squads", on any licence; the Terminology list below
  no longer repeats them.
- **Decision enabled**: any organisation names its structure where it sees it, once (D6, D16, M7).

## Problem

Priya's organisation says "Squad" and "Initiative". Today the four structure words are scattered in a flat
list next to Cycle Time and WIP, with no hint that two of them name who works and two name what they work on.

## Learning hypothesis

Naming on the level is found first try. **Disproved if** testers look in "Other terms" first (then the box
needs a pointer there).

## IN

- Edit dialog per level: unit singular/plural, item singular/plural. **Free on every licence, System Admin
  only** (M7). The rest of Terminology ("Other terms") stays Premium.
- The Terminology list ("Other terms") no longer shows the 8 structure keys.
- `LevelRenamed` published; every screen reads the new word through `useTerminology()`.

## OUT

- Item types (08). Names of levels above (#6455).

## Domain examples

1. Priya renames level 1 to Squad / Squads; the Overview list header reads "Squads".
2. She renames level 2's item to Initiative / Initiatives; Portfolio pages read "Initiatives".
3. Ana, free tier, renames level 1 to Crew / Crews; Cycle Time under Other terms stays read-only (Premium).

## UAT scenarios

### Scenario: The admin renames the leaf level's unit

Given Priya is a System Admin
When she renames level 1's unit to "Squad" and "Squads"
Then every screen that named Teams says Squads

### Scenario: Structure words are named on the levels only

Given Priya opens Structure & terms
When she looks at Other terms
Then Team, Work Item, Portfolio and Feature are not listed there

### Scenario: Level names are free, the other terms stay Premium

Given Ana is a System Admin on the free tier
When she renames level 1's unit to "Crew"
Then the rename is saved
And the Other terms stay read-only with the Premium explanation

### Scenario: A Team Admin cannot rename levels

Given Tom is Admin of "Payments Crew" only
When he opens Structure & terms
Then he sees no Edit for names

## Acceptance criteria

- [ ] Rename on a level changes every rendered use of that term.
- [ ] Other terms lists no structure keys.
- [ ] Level names: System Admin on any licence. Other terms: Premium, as today. By API and UI.

## Outcome KPI

OUT-3.

## Technical notes

- `TerminologyController`'s `[LicenseGuard(RequirePremium = true)]` on `PUT /terminology` must no longer block
  the 8 moved keys: either they are written only through the levels endpoint (free, System Admin) and
  `PUT /terminology` rejects them, or the guard checks per key. DESIGN chooses; if clients write them through
  `PUT /terminology`, slice 14's breaking-change note covers it.
- Screenshot `terminologyConfiguration.png` changes; regenerate per theme at finalisation.
