# Slice 02: Dependency and Parent Override say what they read, and which way round (ADO #6352)

**Goal:** an info tip beside **Dependency Field** and **Parent Override Field**, wherever they are chosen, says
which end of a link the field reads, with the tracker's own example, and links to the docs.

## IN scope
- Tips on the shared `ConnectionDefaultFieldSelect` uses for Parent Override (`AdvancedInputs.tsx`) and
  Dependency (`DependenciesComponent.tsx`), and on the connection-default entries in
  `ModifyConnectionSettings.tsx`.
- Tracker-aware examples: Jira *is blocked by* / *is caused by*; Azure DevOps the `-Reverse` end (Predecessor).
- A link from each tip to its docs section.

## OUT of scope
- Validation of a field that matches nothing; any change to what is read.
- The other connection-default fields (Estimation, Size Estimate, Feature Owner).

## Learning hypothesis
- **Disproves** "a tooltip prevents the wrong-direction pick" if, on a fresh Jira connection, someone shown the
  tip still picks *blocks* or *Issue Links*.
- **Confirms** it if the reporter's own wrong pick (*Issue Links* as Dependency) is what the tip warns against.

## Acceptance criteria
US-02 AC-2.1 to AC-2.5 in `feature-delta.md`.

## Dependencies
Reuses whatever slice 01 settles for `InfoTooltip`; does not wait on it to ship.

## Effort
About 4 hours plus docs touch-ups. Reference class: slice 01, times five placements and two trackers.

## Dogfood
On the dev instance's Jira and Azure DevOps connections, open each place the fields appear and check the
example matches that tracker.
