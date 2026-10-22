# Slice 02: Dependency and Parent Override say what they read, and which way round (ADO #6352)

**Goal:** an info icon beside **Dependency Field** and **Parent Override Field**, wherever they are chosen, opens a
popover saying which end of a link the field reads, with the tracker's own example, and a "Learn More" link to the
docs.

## IN scope
- Extract the dashboard widgets' info button and popover (`WidgetInfoButton` in `WidgetShell.tsx`) into a shared
  `InfoPopoverButton`, with an accessible name; the widgets look and behave as today.
- An optional `info` on `FormSelectField` (and passed through `ConnectionDefaultFieldSelect`) renders that button
  beside the select.
- The tips on Parent Override (`AdvancedInputs.tsx`, Team and Portfolio settings), Dependency
  (`DependenciesComponent.tsx`, Portfolio settings) and the connection-default rows for both in
  `ModifyConnectionSettings.tsx`.
- Tracker-aware examples from one copy module: Jira *is blocked by* / *is caused by*; Azure DevOps the `-Reverse`
  end (Predecessor); a neutral sentence for any other tracker.
- A "Learn More" link from each popover to its docs section, in a new tab.

## OUT of scope
- Validation of a field that matches nothing; any change to what is read.
- The other connection-default fields (Estimation, Size Estimate, Feature Owner).
- `InfoTooltip`, which stays as it is.

## Learning hypothesis
- **Disproves** "a tip prevents the wrong-direction pick" if, on a fresh Jira connection, someone shown the tip
  still picks *blocks* or *Issue Links*.
- **Confirms** it if the reporter's own wrong pick (*Issue Links* as Dependency) is what the tip warns against.

## Acceptance criteria
US-02 AC-2.1 to AC-2.6 in `feature-delta.md`.

## Dependencies
None on the other slices.

## Effort
About 5 hours plus docs touch-ups: the extraction, the prop, the copy module, four placements, two trackers.

## Dogfood
On the dev instance's Jira and Azure DevOps connections, open each place the fields appear, open the popover with
the keyboard, and check the example matches that tracker.
