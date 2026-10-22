# Slice 01: Closed Items Cutoff says what it limits (ADO #6348)

**Goal:** an info tip beside **Closed Items Cutoff (days)** says what the number limits, what 0 means and that
raising it brings older items back with the next update.

## IN scope
- Info icon + tooltip on the field in `AdvancedInputs.tsx`, so Team settings, Portfolio settings and both create
  wizards get it.
- Copy in configured terms (Work Items on a Team, Features on a Portfolio).
- Docs: a new "Closed Items Cutoff" section in `docs/teams/edit.md` and `docs/portfolios/edit.md`.

## OUT of scope
- Any change to retention, the fetch fingerprint or the Then & Now window (#6347).
- A docs link inside the tip, unless DESIGN gives `InfoTooltip` one for slice 02 anyway.

## Learning hypothesis
- **Disproves** "one sentence where the number is set is enough" if the reporter, shown the tip, still cannot
  say why his Then & Now window was refused.
- **Confirms** it if he can, and picks a value for his Portfolios from the tip alone.

## Acceptance criteria
US-01 AC-1.1 to AC-1.5 in `feature-delta.md`.

## Dependencies
None.

## Effort
About 2 hours plus the docs section. Reference class: an existing `InfoTooltip` placement.

## Dogfood
On the dev instance (`:5169`), raise a Team's cutoff, run its update, and see the older finished items return,
so the tip's last sentence is true.
