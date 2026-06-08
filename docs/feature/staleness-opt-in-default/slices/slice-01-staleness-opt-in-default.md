# Slice 01 — Staleness off by default for new teams & portfolios

**Goal:** A team or portfolio created via the wizard ships with `stalenessThresholdDays = 0` (no red staleness badges) until an admin opts in.

## IN scope
- `CreateTeamWizard.tsx:55` `7 → 0`
- `CreatePortfolioWizard.tsx:56` `14 → 0`
- `EditTeam.tsx:102` (getDefaultTeamSettings) `7 → 0`
- Align FE tests asserting the wizard/default values.
- Clients-repo parity check (same hardcoded default in client create helpers) — fix or record N/A.

## OUT scope
- Existing teams/portfolios (no data migration — D3).
- EF migration DB column default 7/14 (D4).
- Seed-on-enable values team `5` / portfolio `14` (D5).
- Any staleness evaluation/visualisation logic.

## Learning hypothesis
Confirms: new entities created post-ship persist `stalenessThresholdDays = 0` (KPI `OUT-new-entity-staleness-off` = 100%).
Disproves the slicing if: a *fourth* creation path still injects a non-zero default → the literal sweep was incomplete.

## Acceptance criteria
US-01 ACs 1–5 (see `feature-delta.md`). Production-data check: create a real team + portfolio via the running app, confirm board shows no staleness-red and settings show staleness disabled.

## Dependencies
`time-in-state-and-staleness` shipped (revised D8). No backend change required.

## Effort
≪ 1 day. 3 literal edits + test alignment + live verification. No ADR (mechanical completion of a locked decision).

## Dogfood moment
Create a fresh team + portfolio in the local app the same day; confirm a clean (non-red) first board.
