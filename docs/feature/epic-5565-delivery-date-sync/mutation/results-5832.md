# Mutation testing — 5832 (Say so when Jira refuses the Release write)

Run 2026-08-25 against `main` at `a1bb47a3c`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **92.50 %** | 214 | 196 | 16 | 2 | 9 m 37 s |
| Frontend (StrykerJS) | **86.67 %** | 15 | 13 | 2 | 0 | 1 m |

Config: `stryker.5832.backend.json`, `stryker.5832.frontend.json` with `mutation-vitest.5832.config.ts`.

## The first backend run was 89.70 %, and seven of its survivors were real

Over the gate already, and the seven were still claims nothing was making — so they were closed rather
than accepted, and the score moved to 92.50 %.

- **Which half of a refusal body wins was incidental.** Reordering the three lookups changed no test.
  A request-level sentence says more than a field-level one, and a gateway's blanket sentence says
  least; that order is now asserted.
- **A message of nothing but spaces read as a reason.** Shown, it tells a reader their source refused
  for a reason it would not name.
- **A description that came back as something other than text** threw from inside a getter, reaching
  the caller as a failed write rather than as a Release with nothing worth keeping.
- **Both ends of the reason clamp were loose**: a sentence exactly as long as the limit was cuttable
  (an ellipsis promising more where there is none), and the truncation could have kept nothing at all.
- **Switching off a broadcast that was already off**, on a Delivery carrying a report, moves the
  version — something a reader can see went away, so a save has to be told.

## Accepted survivors — backend

**Nine are in `Delivery.cs` code this slice never touched** — `TeamsWithoutForecast`'s ordering, the
`ReplaceFeatures` identity guards, the day-zero marker, the feature-breakdown filters. The whole
aggregate is mutated because Stryker.NET takes a file, not a slice; the test filter here is this Epic's
suites plus the aggregate's own, which is narrower than the code being mutated. They belong to earlier
slices and are recorded, not chased.

**Two are equivalent by construction.** `ForgetTheRefusal`'s `&&` becomes `||`: the reason and the day
are only ever set together and cleared together, so no state distinguishes them. `SyncFromSource`'s
`ArgumentNullException.ThrowIfNull(members)` has no caller that can pass null.

**The rest are the log-only branches and the exception message**, already marked `Stryker disable` with
their reasoning, plus two timeouts in the line reader — a loop that never ends is the suite noticing.

## Accepted survivors — frontend

Both are invisible to the test environment rather than untested.

- The `sx` prop on the alert. jsdom renders MUI style objects to nothing a query can see; this repo has
  the same note against a theme colour in `DeliverySection`.
- `toLocaleDateString(undefined, { timeZone: "UTC" })` losing its options object. The component reads
  the day in the **instance's** zone rather than the browser's, which matters — but a test process
  cannot change its own timezone once `Intl` has initialised, so on a UTC host the two branches produce
  identical characters and no assertion could tell them apart. The reasoning is in the component
  instead, where the next person to touch it will read it.

## Scope

Backend mutated `Delivery.cs` and `JiraReleaseVersionReader.cs`. Frontend mutated the **logic lines** of
`DeliveryPublishRefusedNotice.tsx` (`:29-35`, `:56-66`) rather than the whole file — a first attempt at
the whole component scored 65 % on four `sx` survivors, and adding `Delivery.ts` beside it dropped the
score to 24 % on thirty-three no-coverage mutants in fields these three suites never touch. Mutating a
presentational component wholesale measures the renderer, not the tests; the same lesson
`stryker.5831.frontend.json` recorded when it line-scoped `DeliverySection`.
