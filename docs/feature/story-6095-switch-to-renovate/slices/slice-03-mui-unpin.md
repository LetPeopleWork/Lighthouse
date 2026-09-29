# Slice 03 — mui-unpin

**Goal:** bring `@mui/x-charts`, `@mui/x-date-pickers` and `@mui/icons-material` up to the latest 9.x and
remove their Renovate ignores. Any ignore that must stay carries its reason next to it.

**Stories:** US-04 (see `../feature-delta.md`). Runs after slice 01, since it edits the `renovate.json`
that slice 01 creates.

## Why they were held (from git)
- `cbf153ded` 2025-11-02: x-charts and x-date-pickers were downgraded 8.16 → 8.11.3 for an x-axis label
  issue (touched `BarRunChart.tsx`). Majors later went through to 9.0.x on 2026-04-14. Minor/patch has
  been ignored ever since, and the latest is 9.14.0.
- `aec637797` 2026-04-17: icons-material v9 → v7, with `@mui/lab` at `7.0.0`. See also `60b8e759f`, the
  ConfidentIcon/RiskyIcon import fix. The latest is 9.4.0.

## IN
- A hand upgrade of the three packages in `Lighthouse.Frontend`, with the lockfile under pnpm 10.33.2.
- The code changes the upgrade needs: renamed icons, chart API drift, picker API drift.
- A visual check on the dev instance: bar, run and stacked-area chart axes; the date-range picker; the
  forecast-level icons.
- Removing the lifted ignores from `renovate.json`, and regenerating the affected `@screenshot` images.

## OUT
`@mui/lab` (no stable v9). Any other MUI package.

## Learning hypothesis
- Disproves "the regressions were fixed upstream" if labels, the picker or icons break on latest 9.x.
  In that case the ignore stays with a reproduction and an ADO Bug.
- Confirms it when all gates are green and the visual check matches today.

## Acceptance
AC-04.1 … AC-04.6.

## Dependencies
Slice 01 (`renovate.json` exists).

## Effort / reference class
Half a day to a day. It's like the 2026-06-18 MUI 9.1 bump (see ci-learnings, `server.deps.inline`).
Watch for `ERR_UNSUPPORTED_DIR_IMPORT` in Vitest again.
