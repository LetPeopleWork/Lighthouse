# Acceptance scenarios: the over-time history fill is on by default (Story 6083)

Backend story. Written against the test-writing skill (SKILL.md and references/backend.md). Source: the stories and locked
decisions D1-D3 in `docs/feature/story-6083-over-time-history-fill-on-by-default/feature-delta.md`. Plain words, no tags.

## Placement (checklist 1, 12-17)

| Layer | Where | Why |
|-------|-------|-----|
| Port scenarios 1-7 | `Tests/Acceptance/OverTimeHistoryFill/`, namespace `Tests.Acceptance.OverTimeHistoryFill`, built with `ServicePortHarness` (own SQLite file per scenario, deleted afterwards) | The outcome is a stored switch value after the start-up seeders run. No route is needed to prove it. |
| Rule row, scenario 8 | unit test next to `OptionalFeatureSeeder` | The once-only decision is a rule, so it is pinned below the port. |
| Wire contract, scenario 9 | one row on `WireContractTest` in `Tests/Contracts/` | Only the route, JSON names and the 403 for a non-admin need a host. One host for all rows. |
| Skeleton | none | The fill itself is covered by Story 6053's suite. Its set-up drops its switch-on, so it runs on the default. |

Mutation config for the feature: the test-case filter names `Tests.Acceptance.OverTimeHistoryFill` as its added last term, since that folder is outside the integration filter.

Fixtures: each scenario builds its own harness. A row is one `OptionalFeatures` row plus at most one `AppSettings` row. No shared state, no clock (nothing here reads time, so no `FakeLighthouseClock`).

## Scenarios

### 1. A new instance has the history fill switched on
Given an empty database
When the instance starts
Then "Fill in past days on over-time charts" is on
And it is still a Preview and still free
And the record that the upgrade switch-on happened exists

### 2. An upgraded instance has the fill on, whichever way the earlier release left it
Table, one `[TestCase]` per row:

| Earlier release left the row | After the first start-up of this release |
|------------------------------|------------------------------------------|
| switched off (seeded off)    | on |
| switched off by an admin     | on |
| switched on by an admin      | on |
| not there yet                | on |

Given an instance with the fill row as in the table and no record of the switch-on
When the instance starts
Then the fill is on
And the record of the switch-on exists

### 3. The upgrade changes nothing else in behaviour settings
Given an upgraded instance with the fill off and three other optional features set as an admin left them (one on, one off, one premium and unlicensed)
When the instance starts
Then the fill is on
And the three other features have the values they had
And name, description, Preview flag and Free flag of the fill are unchanged

### 4. An admin's off after the upgrade survives every restart
Given scenario 2's start (earlier release left the fill off, then the upgrade)
And a System Admin switches the fill off
When the instance restarts, three times
Then after each restart the fill is off
And the record of the switch-on is still exactly one row

### 5. A new instance whose admin switches the fill off keeps it off
Given scenario 1's start
And a System Admin switches the fill off
When the instance restarts, twice
Then after each restart the fill is off

### 6. A fill setting removed by hand comes back on at the next restart (edge)
Given scenario 1's start
And the fill row has been deleted from the database
When the instance restarts
Then the fill is on, as for a new instance
And only one fill row exists

### 7. The record of the switch-on survives the clean-up of obsolete settings (edge)
Given scenario 1's start
When every start-up seeder runs again
Then the record of the switch-on is still there
And the fill still has the value an admin gave it

### 8. The once-only rule (unit level, no harness)
Rows, each a `[TestCase]` on the seeder against a plain in-memory context:

| Fill row | Record | Result |
|----------|--------|--------|
| off | absent | on, record written in the same save |
| off | present | off |
| on | present | on |
| absent | absent | on, record written |
| absent | present | on (seeded on, D2) |

### 9. Someone who is not a System Admin cannot read or switch the fill (wire contract, one host)
Given a signed-in user who is not a System Admin
When they ask for, or try to switch, "Fill in past days on over-time charts"
Then the answer is 403
And, for a System Admin, the JSON names of the answer (`key`, `enabled`, `isPreview`) are as the client expects

## Coverage

Error and edge scenarios: 3, 4, 5, 6, 7, 9 and the off rows of 8 (6 of 9 cases above are error, edge or preservation).
Not re-authored: the admin-only write right, already held by Story 6053's suite; the docs and release-note copy, which is a finalize checklist item.

## Checks owed at hand-over (checklist 9, 11)

- Break check: for each of scenarios 1, 4 and 8, invert the seeded `Enabled` value, delete the marker write, see the named test red, put the line back, and confirm `git diff` shows the file as it was.
- Run `Lighthouse.Backend/Test-Fast.ps1` and the named scenarios; run the class alone, in file order and shuffled.
