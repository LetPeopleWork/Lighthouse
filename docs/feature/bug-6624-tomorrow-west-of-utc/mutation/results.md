# Mutation testing — 6624 (Delivery dialog refuses tomorrow as a launch date west of UTC)

Run 2026-11-14 against the worktree @ `4751ecd05` plus the kill-round tests. The gate is an 80 % kill rate. Only the frontend changed.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 10.1.1), first run | 71.43 % | 42 | 30 | 10 | 2 | 19 s |
| Frontend, after the kill round | **83.33 %** | 42 | 35 | 7 | 0 | 18 s |

Backend: N/A, because no backend file changed.

Configs: `stryker.6624.frontend.json` and `vitest.stryker.mutation.ts`. The Vitest config has to be copied into
`Lighthouse.Frontend/` for the run and removed after it. Scope: `deliveryFormRules.ts:64-93`, the `isValidFutureDate`
rule only.

Run it from `Lighthouse.Frontend/` with the clock in New York:

```bash
pnpm exec cross-env TZ=America/New_York stryker run ../docs/feature/bug-6624-tomorrow-west-of-utc/mutation/stryker.6624.frontend.json --incremental
```

Stryker's Vitest runner does not apply the config's `env.TZ`. The zone must therefore be set on the Stryker process
itself. Without it, the west-of-UTC test's offset guard fails the initial run, which is what the guard is for.

## Closed by the kill round

`deliveryFormRules.test.ts` now refuses tomorrow's day string with a time after it and with text before it, and a
string that is not a date. This kills both regex-anchor mutants. It also covers the "no match" branch, which no
test reached before.

## Accepted survivors (all equivalent)

- **`if (!date) return false` (2 mutants: block removed, condition `false`).** An empty string never matches
  the day pattern, so the next guard refuses it anyway.
- **The three calendar comparisons (4 mutants: `&&` to `||`, and each comparison set to `true`).** Any day the
  calendar lacks rolls over into a different month: a day of 0 or past the month's end, or a month of 0 or 13 and
  above. So the month comparison alone refuses every such day. The year and day comparisons never decide, and
  weakening any one of the three changes nothing a caller can see. The test with month 13 still kills the mutant
  that drops the month comparison.
- **`today.setHours(0, 0, 0, 0)` to `setMinutes(0, 0, 0, 0)` (1 mutant).** The picked day is built at local
  midnight. Comparing it with any moment of today gives the same answer as comparing it with today's midnight, so
  the hour that `setMinutes` leaves in place never decides.
