# Slice 02 — One-time refresh of all website Lighthouse screenshots

**Goal:** Every Lighthouse still-screenshot on the marketing website is current and GitHub-hosted, matching the released UI.

## IN scope
- Map each of the 10 website screenshots to a canonical `docs/assets` PNG (current-UI):
  `ALM_Connection`, `Forecasts_Project`, `Forecasts_Team_Epics`, `Forecasts_Team_Manual`, `GitHub`,
  `Metrics_Project_1`, `Metrics_Project_2`, `Metrics_Team_1`, `Metrics_Team_2`, `Query_Configuration`.
- Where no acceptable canonical equivalent exists, add a focused `@screenshot` test in
  `Lighthouse.EndToEndTests` (driven from `testWithDemoData`, per-theme) that produces a
  marketing-suitable canonical asset — and **run it live** against a clean backend before commit.
- Re-point each website reference from the bundled `import` to the GitHub-hosted URL (reusing the
  slice-01 helper).
- Remove the now-dead bundled copies under `website/src/assets/screenshots/` for migrated images.
- `bun run build` clean; all migrated images render remotely on the running site.

## OUT scope
- Video assets and OG/SEO image (D4, D5).
- The finalization gate (slice-03).
- Any new marketed surface or layout change.

## Learning hypothesis
Disproves *"the docs/assets canonical set covers the website's marketing needs"* if more than a
couple of the 10 images need bespoke marketing framing the docs shots can't provide. The gap count
is the signal: 0–2 gaps → canonical set is sufficient; >2 → marketing needs a dedicated shot set.

## Acceptance criteria
- All 10 website Lighthouse screenshots render from GitHub-hosted canonical assets matching current UI.
- Every gap (no canonical equivalent) is listed with the `@screenshot` test added to close it — no silent omission.
- New `@screenshot` tests run live before commit; `bun run build` clean; no dead imports remain.

## Dependencies
slice-01 (URL helper + proven hotlink mechanism).

## Effort / reference class
~4–6h depending on gap count. Reference class: existing `@screenshot` test authoring (cheap with demo data) + mechanical website edits. If gap count is high, split the bespoke-shot work into a follow-up slice.

## Pre-slice SPIKE
None — slice-01 retired the mechanism risk. First task is the 10→canonical mapping audit (produces the gap count that sizes the rest).
</content>
