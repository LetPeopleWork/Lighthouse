# ADR-203: A drawn dependency line means the forecast acted on that edge; everything else is said in words

- **Status**: Accepted (maintainer, 2026-09-22 — ratified at the Epic's close-out). Implemented by slice 05 (ADO User Story #6049).
- **Date**: 2026-09-21
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, slice 05 / User Story #6049)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

The Delivery Timeline draws one bar per Feature, positioned by the forecast. Slice 05 adds the
dependency edges to it. Every edge the product knows about is available on the Feature payload
already (ADR-157), and each carries a verdict from the one honour policy (ADR-158): `Honoured`, or
`NotHonoured` with a reason from a closed set.

The edges that were *not* honoured are the interesting problem. `InALoop`, `BlockerCannotBeForecast`,
`IgnoredByPortfolio` — these are real dependencies a reader wants to know about, and the natural
instinct is to draw them too, in a different line style. That instinct is wrong on this surface in a
way it is not wrong on the Feature table, and the difference is worth stating once rather than
re-arguing each time somebody proposes a dashed line.

A bar's **position** is the forecast's output. A line drawn between two bars is read as an explanation
of those positions: *this one starts there because that one ends here*. For a non-honoured edge that
reading is false — the simulation never waited for the blocker, and the bars would sit exactly where
they sit if the edge did not exist. A dashed line does not repair this. It asks the reader to hold two
meanings for one mark and to remember which is which while scanning, and the failure mode when they
forget is that the screen becomes evidence for a causal claim that is not true. ADR-158's own context
names this failure: "a Feature can show a warning that does not match what its date actually did — a
defect that is worse than either behaviour alone".

There is a second class of edge with the same problem from the other direction: the blocker has no bar
on this timeline at all. Three distinct situations produce it, and only the first is named in the
acceptance criteria as drafted — the blocker is not among the Delivery's selected Features; the blocker
is selected but could not be placed (no forecast, so `buildDeliveryTimeline` returns it as
`unplaceable`); or the blocker is withheld from this reader by RBAC and has no name to show. In all
three there is nothing on the axis to point at, so the question of line style does not arise — but the
reader must still be told, because a dependency that is invisible reads as a Feature that has none.

Measured density says this is not a rare corner. On the maintainer's dev instance (one Portfolio,
83 Features, 12 edges), **3 of 12 edges — one in four — point at something outside the Portfolio**,
and a Delivery is a subset of a Portfolio, so the out-of-view fraction at Delivery grain can only be
larger. The undrawable case is ordinary traffic, not an edge case.

## Decision

**A line on the Delivery Timeline is drawn if and only if the edge was honoured by the forecast and
both of its ends have a bar. Every other edge is reported in words, on a single per-bar mark, with its
own reason.**

Five points that are part of the decision:

1. **One meaning per mark.** A line means: the schedule accounted for this wait, and these two bars
   are positioned accordingly. There is no second line style, and there is no tooltip that changes
   what a line means. A reader who has learned the mark once has learned it.

2. **The words come from `dependencySentences`, which already owns them.** `reasonSentence` covers
   every value of `NotHonouredReason` and `positionedBelowSentence` covers the advisory. Two sentences
   are added there — for a blocker that is not in this Delivery, and for one that is but has no
   forecast to place — rather than written locally, so a Feature cannot be explained one way on the
   table and another way on the timeline. This is the same single-source constraint ADR-158's KPI-5
   applies to the verdict itself, applied to the words carrying it.

3. **"Outside this Delivery" is computed, never read.** It is the absence of a bar for the blocker's
   `ReferenceId` among the Delivery's own Features. It is emphatically **not** the
   `OutsideThisPortfolio` verdict, which answers a different question: an edge can be honoured, both
   ends in the same Portfolio, and still have one end outside this Delivery's selection rule.
   Conflating them would report a Delivery's selection as a data problem.

4. **A honoured edge is still drawn when its blocker sits below the dependent in the order.**
   `BlockerRankedBelow` is an advisory that does not change honouring (ADR-158), so the forecast did
   wait, and the line is true. The dependent's bar additionally carries the mark, because this is the
   one case the reader most wants named — a bar late for a reason the board's own order contradicts —
   and because the Feature table already warns about it. The two surfaces call the same predicate,
   `isWorthWarningAbout`, so they cannot disagree.

5. **A Portfolio that has set its dependencies aside gets one note, not a mark on every bar.** Every
   edge then returns `IgnoredByPortfolio`, so a per-bar mark would appear on every dependent bar and
   say the same thing each time. The Feature table reached the same conclusion for the same reason and
   says it on the entry instead of the row. On the timeline the equivalent placement is one note above
   the chart.

**`NotLicensed` is unreachable on this surface** and is not designed for. The Timeline tab is premium-
gated in its entirety (AC-4.7), so a reader who could meet that verdict cannot reach the chart. The
sentence still exists in `dependencySentences` for the table, which is not gated.

### What the mark is, refined 2026-09-21

Point 1 above says "a single per-bar mark". The maintainer settled what it is, and the refinement is
part of this decision because it turns on the same single-source constraint.

**The mark is a warning symbol — the same `WarningAmberIcon` the Feature table uses — raised by
`isWorthWarningAbout` and by nothing else.** That predicate already lives in `models/FeatureDependency`
and is already called by the table's warnings column and by `featureWarningSentences`. A third copy of
the warning rule would be a third chance for the surfaces to disagree about one Feature, which is the
failure point 4 above already refuses for `BlockerRankedBelow`.

**A dependency whose blocker merely has no bar here is not a warning.** `isWorthWarningAbout` is
`!isSetAside && !hasNothingWrongWithIt`, so an honoured, entirely sound dependency whose blocker sits
outside the Delivery does not raise it — and that is correct rather than an oversight. The forecast
accounted for the wait properly; only the picture is short. Because roughly one edge in four points out
of view, raising a warning there would put an amber icon on a Delivery that is forecasting correctly and
the symbol would stop meaning anything. Those get a neutral mark. **A bar with nothing to say carries no
mark at all** — which is where this departs from `WarningsIndicator`, whose green "no warnings" check is
right in a column a reader scans down and wrong inside a bar a few pixels tall.

**Clicking the bar opens the dialog it already opens, now carrying a Warnings column.** `WorkItemsDialog`
already takes optional columns attached by the caller that owns the payload — `highlightColumn`,
`timeInStateColumn`, `ageBandColumn`, `sleRiskColumn` — and `DeliveryTimelineTab` already renders it on
a bar click. So this is **a fifth instance of the idiom ADR-198 records, attached at one existing call
site**, not a new pattern and not a new flow. The column is `featureWarningSentences` in full and
carries no timeline vocabulary: the neutral notes stay on the bar's tooltip. That keeps the column
honest under its own header, and keeps it useful to the other fifteen render sites, which is what would
make widening it cheap later. **Widening is not part of this decision**; ADR-198's own limitation
applies in mirror, since a fifth descriptor attached at one site is precisely the case its partition
test does not cover and which it says remains a review-time concern.

## Alternatives considered

- **Draw every edge; distinguish non-honoured ones by line style (dashed, muted).** The most
  information on the chart, and the first thing anybody proposes. **Rejected** on two grounds. It asks
  a line to mean two things, which is the false-causal-reading this ADR exists to prevent; and it
  makes the honesty of the picture depend on a vendor capability nobody has probed — per-link styling
  in `@svar-ui/react-gantt`'s free edition is unverified, and a styling prop that is silently ignored
  degrades to alternative three below without anybody noticing.
- **Draw every edge identically, and put the reason in a tooltip.** Cheapest. **Rejected outright** —
  a tooltip nobody knows to hover over says nothing at all, and the default reading of the chart is
  then a causal claim that is false.
- **Draw only honoured edges and say nothing about the rest.** Clean, and the ACs as drafted almost
  permit it. **Rejected** — at one edge in four pointing out of view, a Feature with dependencies
  would routinely render identically to a Feature with none, and the reader has no way to tell that
  what they are looking at is short.
- **Three distinct visual indicators, one per undrawable situation.** Precise. **Rejected** — the
  chart already carries a target band, a today line and an observed-versus-forecast start distinction.
  A fourth, fifth and sixth mark spend the reader's attention on a distinction the tooltip can make in
  a sentence, and the three situations share the one consequence that matters: there is no line.
- **Compute the undrawable set on the server and ship it on the DTO.** It would make the classification
  testable in the backend suite. **Rejected** — the server does not know which Features a client has
  placed on a timeline, and the answer changes with the selected percentile, which is client state.
  The scope is frontend-only by construction.

## Consequences

- **Positive**: the set of drawn lines is exactly the set of edges the forecast acted on. That is an
  invariant a test can state, and it is the same invariant whether the reader is licensed, whether the
  Portfolio is ignoring dependencies, and whichever percentile is selected.
- **Positive**: no dependency on vendor link-styling capability. The only vendor surface used is the
  `links` prop, confirmed present in the free MIT edition.
- **Positive**: the warning symbol, the Feature table's warnings column and the export all read one
  predicate and one set of sentences, so a Feature cannot be clean on one surface and marked on another.
  The click-through adds a third reader on the same terms rather than a second opinion.
- **Positive**: the dialog stays ignorant of the domain. It receives finished sentences and never learns
  what an `IFeatureDependency` is, so ADR-198's and ADR-188's shape is preserved rather than eroded by
  the fifth column.
- **Negative — accepted residual, not a mitigated risk.** The library's link shape carries a routing
  enum (`e2s` and friends) written by hand in `ganttShapes`, exactly as `type: "task"` already is. **A
  wrong value there draws nothing and raises nothing** — the same silent failure the axis `format`
  callback already produced once in this component. No unit test can catch it, and the reason is
  structural rather than a matter of effort: the test environment has no drawing surface, which is why
  `ganttShapes` exists as a library-free file at all.

  DESIGN proposed a rendered probe — a screenshot or Playwright assertion that a line is present between
  two known bars. **The maintainer overruled it on 2026-09-21: no screenshot test and no Playwright step
  for this slice; the live visual check held before the push is the whole verification.** That check is
  real and this ADR does not pretend otherwise — the enum *is* verified once, by a person, at delivery.
  **What is given up is durability.** After that day nothing re-asks. A rename, a type change, a library
  upgrade or a well-meant tidy of the literal can silently return the links to drawing nothing, and the
  first observer will be a user who concludes the Delivery has no dependencies. The failure presents as
  absence, which is the hardest kind to notice and the most expensive kind to report.

  Recorded here as **accepted**, so that a future reader meets a decision rather than an oversight. The
  cost of adding the rendered assertion later is the same as the cost of adding it now, which is part of
  why accepting it is reasonable.
- **Negative**: a reader who wants to see a non-honoured edge as a line cannot. That is the intended
  trade: the Feature table and the dependency dialog remain the place to read the full edge set, and
  the timeline is the place to read what the dates were built from.
- **Reuse verdict**: `dependencySentences` → **EXTEND** (two sentences). `isWorthWarningAbout` /
  `isSetAside` → **REUSED AS IS**. `featureWarningSentences` → **REUSED AS IS**. `buildDeliveryTimeline`
  → **REUSED AS IS**. `ganttShapes` → **EXTEND** (`toGanttLinks` beside `toGanttTasks`).
  `TimelineBarContent` → **EXTEND**. `WorkItemsDialog` → **EXTEND** (a fifth optional descriptor,
  absent by default). `createWarningsColumn` → **PATTERN REUSED, NOT EXTENDED** (its column is over
  `IFeature`; the dialog's grid is over `IWorkItem`). `WarningsIndicator` → **NOT REUSED on the bar,
  deliberately** — its "no warnings" green check is noise inside a bar. `IDependencyHonourPolicy` and
  the `FeatureDependsOnDto` contract → **NO CHANGE**; this slice adds no data, no query and no field.
- Cross-refs [ADR-198](./adr-198-shared-dialog-optional-columns-attached-by-the-payload-that-owns-the-population.md)
  (the optional-column idiom this decision uses for the fifth time, and whose stated limitation — a new
  descriptor attached at one site is a review-time concern — applies here in mirror),
  [ADR-157](./adr-157-dependency-references-stored-on-the-feature.md) (where the edges come
  from), [ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md) (the one honour
  decision this surface renders and must not re-derive),
  [ADR-159](./adr-159-un-forecastable-blocker-drops-and-the-date-reads-as-a-floor.md)
  (`BlockerCannotBeForecast`, the verdict that most often coincides with an unplaceable blocker),
  [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) /
  [ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) (what puts a bar where
  it is).
