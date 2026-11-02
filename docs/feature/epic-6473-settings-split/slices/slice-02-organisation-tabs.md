# Slice 02: Organisation holds only how the organisation works

Story US-02. job_id `job-config-admin-find-how-we-work-in-one-place`. Effort about 0.5 to 1 day. Depends on slice 01.

## Outcome a user sees

Organisation's single Configuration tab becomes three: Terminology, Calendar, Refresh & ordering (names decided, M2).
"Never send usage data" leaves the Behaviour Settings table and gets its own System → Usage Data tab. Both behaviour
rows show a spinner while loading and an alert when they cannot be loaded, instead of disappearing.

## Learning hypothesis

If Organisation shows only the organisation's settings, nobody looks for the usage-data veto there and nobody asks
where Terminology went (K1, K2). Disproved if a tester opens Organisation to find usage data.

## IN

- Split `SystemSettingsTab` into three Organisation tabs, panels unchanged.
- Behaviour Settings: the ordering row in Refresh & ordering; the usage-data row in System → Usage Data, with a link
  to the usage-data docs page.
- Loading and failure looks for both rows (existing inline alert style).
- Legacy key `configuration` now lands on Terminology. `SystemConfigurationPage` POM split accordingly.
- The Terminology tab is where #6474 adds levels; nothing about levels is built here.

## OUT

Levels, level names, item types (#6474); organisation defaults with override (#6349); per-person usage-data consent
(stays in the footer); docs and screenshots (finalize).

## Problem

Priya opens Configuration to rename "Feature" to "Initiative" and finds "Never send usage data" between Feature
ordering and Terminology. Tomasz, who decides about usage data, has to open the organisation's settings to do it.

## Who

- System Admin as organisation set-up | renames terms, adds blackout periods | wants nothing else in the way
- Platform operator | decides the instance's usage data | wants it with the server's settings

## Domain examples

1. Priya opens Organisation → Terminology and renames Feature to Initiative (her organisation's word, an example
   rename; the seeded default stays Feature); the Refresh & ordering tab now reads
   "Initiative Refresh".
2. Priya adds 24–31 December as a blackout period under Organisation → Calendar.
3. Tomasz turns on "Never send usage data" under System → Usage Data.
4. On a slow connection the optional-features call fails; Refresh & ordering shows "Could not load the behaviour
   settings" instead of an empty space.

## UAT scenarios

### Scenario: Organisation shows three tabs of the organisation's settings

Given Priya Raman is a System Admin
When she opens Organisation
Then she sees Terminology, Calendar, and Refresh & ordering
And she sees no usage-data setting there

### Scenario: The usage-data veto is with the server's settings

Given Tomasz Nowak is a System Admin
When he opens System, Usage Data, and turns on "Never send usage data"
Then the switch stays on after a reload, as it did on the old page

### Scenario: Feature ordering keeps its Premium rule

Given Northwind Logistics has no Premium licence
When Priya opens Refresh & ordering
Then "Let Lighthouse own the order of your Initiatives" is shown and cannot be turned on, as before

### Scenario: A behaviour setting that cannot be loaded says so

Given the behaviour settings cannot be loaded
When Priya opens Refresh & ordering
Then she sees that the behaviour settings could not be loaded, not an empty tab

### Scenario: An unreadable licence does not look like "no Premium"

Given Northwind Logistics holds a Premium licence and the licence status cannot be loaded
When Priya opens Refresh & ordering
Then the ordering row says it could not be loaded, rather than showing a locked switch

### Scenario: The old Configuration link opens Terminology

Given a docs link "/settings?tab=configuration"
When Priya opens it
Then she lands on Organisation, Terminology

## Acceptance criteria

- [ ] Organisation has exactly Terminology, Calendar, Refresh & ordering; each panel behaves as before.
- [ ] System → Usage Data holds the usage-data veto only; toggling it reports usage as today.
- [ ] Labels use the configurable terms.
- [ ] Both behaviour rows have a loading and a failure look, for the optional-features call and the licence call.
- [ ] `?tab=configuration` lands on Terminology.

## Outcome KPIs

K1 and K2; see feature-delta.

## Technical notes

- `BehaviourSettingsTable` is reused with a filtered list; the key is matched by `key`, never by id (every row has
  id 0).
- Dependencies: slice 01.
