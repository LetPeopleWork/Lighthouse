# Slice 03 — Say which refinement states mean Ready

> **Superseded in part on 2026-10-04 (maintainer).** Refinement states no longer carry a stage. A Work Item's stage
> comes only from two optional rules, "Ready when" and "Being refined when"; anything unmatched is Waiting. Stage and
> votes are two separate signals: without rules the ready count is the votes', with rules the stages', never a sum,
> and a row is marked only where votes have been cast and disagree with the stage. Slice 08 is folded in here; #6141
> is retitled "Optional stage rules: see what is refined, being refined or waiting". See `../feature-delta.md` →
> "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)" and `../distill/upstream-issues.md`.

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-03 ·
**Estimate**: ~½d · **Tier**: Community

## Goal

Each refinement state is tagged with a stage: Waiting, Being refined or Ready. The tab shows the stage of every Work
Item and how many are ready, which is the input every count in E2 needs.

## IN

- A stage per refinement state, defaulting to Waiting, in Settings → Refinement. Expand-only storage that extends
  slice 01's shape.
- A Stage column in the list, and "N ready" in the heading.
- A hint when no state is marked Ready and no rule exists (AC-3 scenario 3).
- Ready resolution follows steps (1) and (2) of DD-5, added to the vote-ready Work Items that slice 13 (shipped
  earlier, DD-22) already counts. Rules join in slice 08.

## OUT

Rules (08), votes (11/13), the need number (05).

## Learning hypothesis

**This disproves "the Team's states already make the split" (D15)** if the dogfood Team has no state that means
Ready. In that case rules (08) become a prerequisite of the need number and move ahead of 05.

## Data and dogfood moment

- Demo: Team Gravity — `Next` = Ready (GR-058, GR-059), `Analysing` = Being refined, `Backlog` = Waiting.
- Dogfood: tag the dev instance Team's states. Does any one of them honestly mean "ready to pull"?

## Acceptance criteria

AC-3.1 … AC-3.4 (US-03).

## Dependencies

Slices 01, 02. Slice 13 normally ships first (DD-22) but is not required.
