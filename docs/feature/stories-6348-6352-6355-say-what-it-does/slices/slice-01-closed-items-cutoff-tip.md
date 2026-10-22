# Slice 01: Closed Items Cutoff says what it limits (ADO #6348)

**Goal:** the always-visible helper line under **Closed Items Cutoff (days)** says what the number limits, what 0
means and that raising it brings older items back with the next update. No info icon.

## IN scope
- Rewrite the helper text in `AdvancedInputs.tsx` (line 68) to the maintainer's wording: "Finished {Work Items}
  older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0
  keeps them all. Raising it brings older ones back with the next update."
- The term follows the owner: the Work Items term on a Team, the Features term on a Portfolio, read through the
  terminology hook. `parentOverrideDefault` becomes required so the owner is never guessed.
- Docs: retitle the "Cut Off Days" sections in `docs/teams/edit.md` and `docs/portfolios/edit.md` to "Closed Items
  Cutoff", say the same, and correct the Team default to 365.

## OUT of scope
- Any change to retention, the fetch fingerprint or the Then & Now window (#6347).
- An info icon or docs link on this field.

## Learning hypothesis
- **Disproves** "one line where the number is set is enough" if the reporter, shown the line, still cannot say why
  his Then & Now window was refused.
- **Confirms** it if he can, and picks a value for his Portfolios from the line alone.

## Acceptance criteria
US-01 AC-1.1 to AC-1.5 in `feature-delta.md`.

## Dependencies
None.

## Effort
About 1 hour plus the docs section. Reference class: a helper-text change with a terminology lookup.

## Dogfood
On the dev instance (`:5169`), raise a Team's cutoff, run its update, and see the older finished items return,
so the line's last sentence is true.
