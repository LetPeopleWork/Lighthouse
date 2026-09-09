# Story #5914 — Named ranges and window stepping on the metrics date range

**Shipped** 2026-09-09 · ADO User Story #5914 · workspace `docs/feature/story-5914-metrics-time-horizon-presets/`

## What users get

Changing the metrics date range meant setting two dates by hand, one picker at a time. Two of the
commonest intentions — "show me the last two weeks" and "now show me the two weeks before that" —
each cost two deliberate edits, and the second one had no support at all: walking a window backwards
through time meant computing both ends yourself, every time.

Two controls now cover both. A row of **named ranges** in the date panel jumps to a range ending
today — 7, 14, 30 or 90 days for a team, 30, 90 or 180 for a portfolio — with the chip matching the
range on show highlighted. And a pair of **arrows** either side of the header's date label walks the
range you already have backwards and forwards, a week at a time for a team and four weeks for a
portfolio, keeping its length fixed. That last one is what makes period-over-period comparison a
click instead of an arithmetic exercise.

## What the story asked for that was not built

The story was titled for *defaults*, and both halves of that turned out to be wrong about the code.

Portfolios **already** opened on 90 days. And teams do not open on a flat 30: the range is derived
from the team's own configured throughput window, falling back to 30 only under fixed throughput
dates. The user chose to keep the derived default, so the "default timings" half of the story is one
no-op plus one declined change. What shipped is the two controls above.

The public documentation had been asserting the flat-30 default, incorrectly, since before this
story. That was corrected as part of it.

## What was built

Frontend only. **Zero backend change** — no endpoint, DTO, schema, migration or new dependency.

| Commit | Change |
| --- | --- |
| `90196e755`, `165e621a7`, `a18ebdd6a` | DISCUSS through DISTILL, and the four-reviewer gate |
| `94a0cf75b`, `031c1c8d4`, `f0ff65573` | DELIVER roadmap and its reviewed verdict |
| `cbad43d80` | `dateWindow.ts` — local-calendar window arithmetic, dependency-free |
| `b92e2a35f` | `useDateRange.ts` — the single two-ended write path |
| `91739713a`, `e314b162a` | The chip row, and wiring it into the date panel |
| `cb64a8a7e`, `37ef48c0b` | The midnight-anchored window regression, recorded and fixed |
| `7f9745146` | `useDebouncedRevisionRun` promoted out of `TeamForecastView` |
| `44faecd27`, `8327b41d8`, `fd1347084` | Debounced commit, the stepper, and the header wiring |
| `8b9b8ac1c` | Playwright walking skeleton |
| `a1facbc89`, `2d71e5321` | Refactor pass — name the repeated question; fold the repeated round-wait |
| `84d898831`, `ac32fb1d0` | Review fix: end a window landing on today at *now*, and pin it |
| `49dae1f93`, `a7529d400` | One place decides usability; the second-move reads |
| `02899d14e`, `18a7dc1df` | Mutation write-up and its config |
| `2573fdc20` | Documentation and the date-range screenshot |

## Decisions worth keeping

**The two date setters could not be composed, and that is why the hook exists**
([ADR-189](../product/architecture/adr-189-metrics-window-hook-and-revision-keyed-debounce.md)).
The old `handleStartDateChange` and `handleEndDateChange` each rebuilt the *other* end from the value
current at their own render, over a stale `searchParams`. Calling both to move a two-ended window made
the second silently undo the first, then fetch the result. Every control this story adds moves both
ends, so a single `applyDateRange(start, end)` built from its arguments alone had to land before
anything else could.

**A window that ends today ends *now*, not at the start of today.** Widgets compare the end as a point
in time — the blocked trend's latest-at-or-before, staleness, time-in-state — so an end normalised to
midnight silently drops every reading taken so far today. This was got wrong twice: once when the
preset window was first written, and again in the clamp and rehydrate paths, where `addDays` carried
the old time of day along. The rule now lives in one predicate and one re-anchor, applied wherever a
window lands on today.

**The half-second quiet period is deliberate, and is not the 300 ms used elsewhere.** Every committed
window change resets the visited-category set and refetches the whole category, which is the fix for
the category-scoped fetching bug and must not be weakened. A burst of stepper clicks would otherwise
be a burst of refetches. 500 ms rather than the 300 ms used for typed input, because the gap between
two intended clicks is wider than between two keystrokes.

**The chips carry `aria-pressed`** so the chosen range is not conveyed by colour alone.

## Corrected during delivery

**The midnight anchor, twice.** The first fix (`37ef48c0b`) covered the preset path; an adversarial
review found the clamp and rehydrate paths still carried it, and that walking the window forward across
midnight put the end in the *future*. Fixed in `84d898831`.

Worth recording as a coverage lesson: **all 29 `dateWindow` tests built their fixtures at local
midnight and asserted only on Y/M/D**, so a midnight end and a now end were the same window to them.
The first regression was caught by two tests in an unrelated shipped suite, not by the suite that owned
the code. The suites now set their clock off midnight and assert the instant.

**Three copies of one decision.** Mutation testing showed both date handlers screening their date
before handing it to the write path, which screens it again — so no guard could ever be the one that
refused, and a version where they disagreed behaved identically. Collapsed to one.

**The reload-to-midnight path was pre-existing.** The review framed it as part of this regression, but
the code before this story did the identical thing. It is where the UTC day-shift in age calculation
actually bites for viewers east of Greenwich. Fixed here under the same rule, but it did not ship
with this story.

## Mutation testing

**94.96 %** on the frontend, over three runs (78.49 → 91.37 → 94.96). Full write-up, including why each
of the seven remaining survivors stays, in
[`docs/feature/story-5914-metrics-time-horizon-presets/mutation/results.md`](../feature/story-5914-metrics-time-horizon-presets/mutation/results.md).

One survivor is a **false survivor** — applied by hand it fails nine tests. Two of the first run's
survivors were false the same way. On this repo a "Survived" verdict on a load-bearing branch is worth
a minute of checking before writing a test to chase it.

## Delivery-log reconciliation

All nine steps carry a passing COMMIT. Two entries need reading in context:

- **01-04 logged a failing GREEN** before a passing retry. That is the midnight-anchor regression being
  caught by the shipped suite and fixed, not a broken step.
- **02-01 logged a SKIPPED RED**, justified: it was a behaviour-preserving refactor already guarded by
  the existing `TeamForecastView.autorun` suite.

The four review-and-mutation commits after the roadmap closed carry `Step-Id` trailers of their own and
are not roadmap steps.

## Verified before release

Frontend suite 4897 green; build and Biome clean on both stacks' lint targets; end-to-end TypeScript
clean. CI run `34325567134` green on every gate including both SonarCloud projects, with the Playwright
spec exercised from source by the SQLite and Postgres verify jobs. Behaviour confirmed by the
maintainer in their own environment.

## Still open

Five findings raised by the adversarial review were deliberately left, each outside this story's scope:

- The date panel is fed the **committed** window while the header label reads the **pending** one, so
  opening the panel inside the quiet period shows pre-step dates, and typing there discards the queued
  step.
- No "already this window" guard: re-clicking the selected chip, or stepping back then forward, refetches
  an unchanged window.
- A hand-edited address with the ends **inverted** is accepted, and the stepper propagates it.
- The native `disabled` on the clamped forward arrow drops keyboard focus to the document body.
- The date panel renders with `disablePortal`, so the modal manager marks it `aria-hidden` and screen
  readers cannot reach inside it — which defeats the reason the chips carry `aria-pressed`. Pre-existing,
  introduced by `73a0b7e1e`.

The last one is the only accessibility defect among them and is the strongest candidate for its own
work item.
