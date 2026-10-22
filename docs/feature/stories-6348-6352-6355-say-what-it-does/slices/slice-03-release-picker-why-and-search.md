# Slice 03: The Release picker says why a Release cannot be picked and invites searching (ADO #6355)

**Goal:** a Release with no date says why it cannot be picked and what to do, and the picker shows it can be
searched.

## IN scope
- A search placeholder on the picker input in `DeliverySourceTab.tsx`, naming the source.
- The `NoDateSet` entry reads "No date in {tracker}: set one there to follow it", inline on the row, no tooltip; the tracker word comes from the source, else the tenant's term for the work tracking system.
- `docs/portfolios/detail.md#following-a-jira-release` mentions both.

## OUT of scope
- Making a dateless Release pickable, or overriding its date.
- Server-side search or paging of Releases.

## Learning hypothesis
- **Disproves** "the picker only looked broken" if, with the new copy, the reporter still reads the disabled
  entries as a fault.
- **Confirms** it if he finds a Release among hundreds by typing, without being told he can.

## Acceptance criteria
US-03 AC-3.1 to AC-3.4 in `feature-delta.md`.

## Dependencies
None.

## Effort
About 2 hours plus docs. Reference class: the existing `blockedReason` copy map.

## Dogfood
On the dev instance's Jira connection, open Add Delivery → Jira Release, type part of a Release name, and find a
dateless one.
