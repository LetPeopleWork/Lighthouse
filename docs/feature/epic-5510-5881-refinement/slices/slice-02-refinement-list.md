# Slice 02 — The Refinement tab lists the Work Items in refinement, in backlog order

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E1 Refinement tab (new) · **Story**: US-02 ·
**Estimate**: ~1d · **Tier**: Community · **Walking skeleton**: yes (2 of 2)

## Goal

Anyone who can read the Team can open the Refinement tab and see every Work Item in its refinement states. They appear
in the same order the Team's forecasts pull them, and the tab uses the Team's own word for "Refinement".

## IN

- The tab's content: a heading ("14 Work Items in Refinement") and a list. Each row shows the id with a link to the
  work tracking system, the name, the state, the category and the age.
- Ordering comes from the same backlog-order comparer the forecasts use. No second ordering.
- A new Terminology key **Refinement** (singular and plural), seeded with "Refinement" (D26). It is used for the tab,
  the heading and the tooltip.
- An empty state: "No Work Items in Refinement states right now".
- Usage data: the tab opening reuses `TeamTabOpened` with a new route key (DD-16). The route key is added here and
  `docs/settings/usagedata.md` is updated. Confirm the route key with DEVOPS.
- E2E walking skeleton: open demo Team Gravity → Refinement tab → the list shows GR-058 first. Run it against demo data,
  through a Page Object Model.

## OUT

Stages, ready count, cadence, need number, votes.

## Learning hypothesis

**This disproves "a list in backlog order is the right home for refinement"** if, on a real Team, the To Do state
holds the whole backlog (say 300 Work Items). The list would then be noise. In that case stages (03) and the
"enough for" line (06) become prerequisites for anything useful, and 06 moves up.

## Data and dogfood moment

- Demo: Team Gravity's `Backlog` / `Analysing` / `Next` Work Items (GR-051 … GR-079).
- Dogfood, same day: open the tab for the dev instance's busiest Team. Count the rows. Ask whether the first ten are the
  ones the Team would discuss next.

## Acceptance criteria

AC-2.1 … AC-2.6 in `feature-delta.md` (US-02).

## Dependencies

Slice 01.
