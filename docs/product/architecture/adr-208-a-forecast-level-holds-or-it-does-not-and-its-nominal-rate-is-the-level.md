# ADR-208: A forecast level holds or it does not, and its nominal rate is the level itself

**Status**: **Accepted** (maintainer, 2026-09-22). Raised as PROPOSED by Morgan during the DESIGN wave
because it corrects two acceptance criteria that DISCUSS had locked (AC-1.6 and AC-2.4 of
`epic-4172-forecast-backtest-sweep`) and one worked example in the journey.

The correction was verified against the source before ratification, not accepted on the proposal's word:
`HowManyForecast` constructs `ForecastBase` with `Comparer<int>.Create((x, y) => y.CompareTo(x))` — a
**descending** key order — and `ForecastBase.GetProbability` walks those keys from the highest item count
down until it has accumulated P% of the trials. `ForecastBase.cs` states the semantics in a comment
against Bug #5586: *"the threshold means 'by day t' ascending and 'at least N items' descending."*
So `value(95) <= value(85) <= value(70) <= value(50)`, a higher level promises a **smaller** number of
Work Items, and a level that holds P% of the time is behaving as advertised. The expected-held count is
therefore `evaluated x P/100`, not `evaluated x (100-P)/100`.

The error was invisible at the 50th level, where both formulas yield the same number — which is why it
survived DISCUSS and its review. At the 95th it was wrong by a factor of nineteen, on the cell the feature
calls its teaching cell.

**Feature**: `epic-4172-forecast-backtest-sweep` — ADO Epic #4172, "Forecast Reality Check"

**Decider**: Morgan (Solution Architect)

---

## Context

The Forecast Reality Check's third honesty requirement is that every confidence level reports how often it
actually came true against how often it was *supposed* to. That requirement is the reason the 95th level
is in the check at all: a level that never comes true is over-forecasting, not excellence, and saying so
out loud is the single most useful thing this feature teaches.

The requirement rests entirely on one word — **"beaten"** — and on the expected count printed beside it.
Both were specified in DISCUSS, and reading them against the forecast engine shows they cannot both be
right.

**What the engine actually computes.** `HowManyForecast` derives from `ForecastBase`, which sorts its
simulation results with a **descending** key comparer — the direction is carried by `KeyOrder` and the
comment at `ForecastBase.GetLikelihood` states it: *"at least N items" descending*.
`GetProbability(percentile)` then walks keys from the highest item count downward, accumulating trials
until it has covered `percentile`% of them, and returns that key. So:

```
value(95) <= value(85) <= value(70) <= value(50)
```

and the meaning of each is *"in `P`% of simulated futures the Team completed **at least** `value(P)`
items."* The 95% number is the small, conservative one.

**What DISCUSS specified.** Three artifacts describe the same measurement in three incompatible ways.

1. The nominal-rate table in the journey and in the feature delta prints, out of 14 evaluable checks:
   `50% beaten in 9 (about 7 expected)`, `70% … (about 4)`, `85% … (about 2)`, `95% … (about 1)`. Those
   expected counts are `14 × (100 − P)/100`. That arithmetic is only correct if **"beaten" means the
   actual fell *short* of the forecast.**
2. The same table then reads the 95% row as *"never beaten — over-forecasting, not excellence."* Under
   reading 1 that is wrong: a 95% forecast is *supposed* to be fallen short of about 5% of the time, so
   0 out of 14 against 1 expected is unremarkable and says nothing about over-forecasting.
3. Decision D2 says *"A mark to the left of the 85th means the 85% forecast held. A mark to the right of
   the 50th means the 50% forecast was beaten."* In the same document's own diagram the levels run
   `95 85 70 50` left to right, so left is the smaller number. A mark to the left of `value(85)` means the
   Team completed *fewer* items than the 85% forecast promised — which is the forecast failing, not
   holding. And here "beaten" is used for the *opposite* event to the one in point 1.

**Why nobody caught it.** The expected count at the 50% level is `14 × 0.5 = 7` under the wrong formula
and `14 × 0.5 = 7` under the right one. `P` and `100 − P` coincide at 50, so the one row a reader would
check by eye is the one row that cannot expose the error. The other three rows are each wrong by a factor
of two or more.

This is not a cosmetic wording problem. The printed expected count is the yardstick the whole
nominal-rate honesty requirement is measured against, and an artifact whose central discipline is *not
claiming more than the data supports* cannot ship a yardstick that is off by a factor of ten at the 95%
level.

## Decision

**There is exactly one observable event per cell per confidence level, it is called *held*, and its
nominal rate is the confidence level itself.**

```
Held(cell, P)          ⟺  cell.actualCompleted >= cell.forecastValue(P)
ExpectedHeldCount(P)   =   evaluatedRuns × P / 100
```

Three consequences, each of which is the thing that changes.

**1. The word "beaten" is retired from this feature's vocabulary — response fields, UI copy, docs, the
exported one-pager and the launch post.** English will not hold it steady: a forecast can be beaten *by*
the Team (they did better than predicted) or the forecast can beat the Team (it promised more than
arrived), and the two readings are exact opposites. A feature whose entire value proposition is precision
about what was and was not shown cannot build its headline sentence on a word that means both. "Held" has
one reading and it is the reading that matters to a forecaster: *did the number I would have published
come true?*

**2. The expected count is `evaluatedRuns × P/100`, not `evaluatedRuns × (100 − P)/100`.** For fourteen
evaluable checks the printed line reads `85% held in 12 (about 12 expected)`, not `about 2`.

**3. The never-held case is what carries the over-forecasting reading, and it now does so correctly.** A
95% level that held in 0 of 14 checks, against about 13 expected, is a Team that never once reached even
the most conservative number it was publishing. That is over-forecasting, stated with a yardstick behind
it. Under the superseded arithmetic the same situation printed as "0 against about 1 expected" and read as
unremarkable — the honesty requirement's most important case was the one it got most wrong.

The symmetric case gains a reading it did not have: a 50% level holding in 14 of 14 against about 7
expected is **under-forecasting** — the Team routinely delivers more than its median forecast, and the
number it publishes is costing it credit it has earned.

### The cell-level verdict, settled in the same terms

D7's three-way verdict is about the cell, not the level, and it is named from the **forecast's** point of
view, which is the thing the user is judging:

| Cell outcome | Condition | Reading |
|---|---|---|
| `OverForecast` | `actual < value(95)` | Below the whole band. Every level promised more than the Team delivered. |
| `WithinBand` | `value(95) <= actual <= value(50)` | Inside the band. The forecast bracketed what happened. |
| `UnderForecast` | `actual > value(50)` | Above the whole band. Even the optimistic number was short. |

This matches the worked example DISCUSS already wrote — *"the 14-day window over-forecast in three of its
four checks"* — where over-forecasting is the actual landing below the band. D2's left/right sentence is
superseded: **a mark to the right of a level's tick is that level holding**, because right is the larger
count.

### The degenerate forecast is unevaluable, not a forecast of minus one

`ForecastBase.GetProbability` returns `-1` when no key reaches the threshold. A cell can pass the
data-sufficiency bar and still produce a degenerate simulation. **A cell whose forecast contains any
sentinel value is reported unevaluable and excluded from every count**, exactly as a cell that failed the
sufficiency bar is — with its own reason string, never blank, per
[ADR-194](./adr-194-sle-risk-is-a-number-per-item-never-a-background-ladder.md). A `-1` must never reach
a band, a count or an exported table.

## Alternatives considered

### A. Keep "beaten" meaning *fell short*, and keep `(100 − P)` as the expected count

Internally consistent, and it requires no change to the printed numbers.

**Rejected** because it destroys the reason the 95th level is in the feature. Under this reading a 95%
forecast is expected to fall short about 5% of the time, so never falling short is exactly what should
happen and carries no lesson. D1 justified including the 95th on the grounds that it is *"the teaching
cell"* for the nominal-rate lesson; this alternative makes it the one cell that teaches nothing. It also
leaves the verdict copy ("never beaten — over-forecasting, not excellence") factually wrong, so the copy
would have to be deleted rather than corrected — removing the single sentence the honesty requirement
exists to produce.

### B. Report both events — `heldCount` and `fellShortCount` — and let the reader pick

**Rejected.** They are complements over the evaluable runs (`held + fellShort = evaluated`), so the second
adds no information and doubles the surface on which the two-readings confusion can recur. The feature's
problem here is an ambiguity, and the fix for an ambiguity is never to ship both branches of it.

### C. Leave the definition to the implementer, and assert only the response fields

**Rejected.** The definition is the product. Three artifacts already disagree about it, which is direct
evidence that it will not converge on its own, and the crafter would have to make the same call with less
context and no place to record it.

## Consequences

**Positive**

- One event, one name, one formula, decided once and testable directly: for a synthetic distribution with
  known percentiles, `ExpectedHeldCount(P)` and `Held(cell, P)` are assertions over values, with no
  Monte Carlo variance in the way.
- The over-forecasting and under-forecasting readings both become true statements with a yardstick behind
  them, rather than one true statement and one that happened to be printed next to the wrong number.
- The computation lives in one pure static policy on the backend. The client never re-derives which levels
  held, so the band's rendering and the nominal-rate line cannot disagree with each other.

**Negative**

- **Two locked acceptance criteria change.** AC-1.6 and AC-2.4 say "beaten"; they must say "held", and
  their expected-count formula changes. The journey's worked example and its TUI mockup change with them.
  DISCUSS is reopened on this one point, which is why this ADR is PROPOSED rather than Accepted.
- The corrected numbers read less dramatically at 70% and 85%, where "held in 12 of 14, about 12 expected"
  is a calm result. That is the honest reading and the calm case is the modal one throughout this feature,
  but it is a change in the artifact's tone at two of its four levels.

**Neutral**

- Nick Brown's published figures are unaffected. His study reported correct-rates per percentile, which is
  the same quantity as `held / evaluated` under this decision. The three-way cell verdict remains this
  product's own departure from his one-sided scoring and is still never attributed to him.

## Relationship to existing ADRs

| ADR | Relationship |
|---|---|
| [ADR-194](./adr-194-sle-risk-is-a-number-per-item-never-a-background-ladder.md) | Governs the rendering of the unevaluable cases this ADR adds one to (the degenerate forecast). Not amended. |
| [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md) | The sufficiency bar, composed with and unchanged. This ADR adds a second, independent reason a cell can be unevaluable; it does not add a second sufficiency threshold. |
| [ADR-207](./adr-207-a-report-is-a-response-not-a-record.md) | The response this measurement is carried in. Nothing here is persisted. |
| [ADR-192](./adr-192-sle-risk-as-a-pure-conditional-over-the-cycle-time-population.md) | Precedent for settling a measurement's definition in an ADR rather than in an implementation, for the same reason: a metric whose definition drifts is a metric nobody can argue with. |
