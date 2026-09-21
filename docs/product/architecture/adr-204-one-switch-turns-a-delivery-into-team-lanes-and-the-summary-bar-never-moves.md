# ADR-204: One switch turns a Delivery into team lanes, and the summary bar never moves

- **Status**: Proposed (2026-09-21, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-09-21
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, slice 06 / User Story #6050)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

The Delivery Timeline draws one bar per Feature. A Feature two or three Teams contribute to is one
bar, and which Team drives either end of it is unanswerable from the screen. The per-Team forecasts
for both ends already reach the browser — `teamForecasts` carries one row per contributing Team with
start and completion percentiles — and nothing reads them yet.

Four facts about the ground this sits on shape the decision, and each of them closes off an option
that looks obvious from a distance.

**The chart has no grid pane, so the library's own expander does not exist here.** The toggle that
opens and closes a parent row is rendered by the grid cell renderer and handled inside the grid
component. `DeliveryGanttChart` passes `columns={false}` deliberately — every name the pane would
list is already written along its own bar, and the pane's fixed width pushes the chart off a narrow
window. So "an expander on the row", which is how the story was written, is not a thing this chart
can grow without re-introducing the pane that was removed on purpose.

**Nothing in the timeline knows a Team's name.** `teamForecasts` keys on `teamId`, a number.
`teamsWithoutForecast` is a list of names with no ids attached. The two cannot be joined to each
other. The name does exist client-side, one component up: `DeliverySection` already receives
`teams: IEntityReference[]` and already renders the Timeline tab without passing it.

**A Team with no forecast of its own is not the shape the story assumed.** The story asks for a
named lane carrying its reason for a contributing Team that has no forecast, so that the expansion
cannot silently disagree with `TeamsWithoutForecast`. But a Feature with any Team in
`TeamsWithoutForecast` has **no bar at all** — `buildDeliveryTimeline` sorts it into `unplaceable`,
and `FeatureDto` blanks its `Forecasts` — so there is nothing to expand and that route cannot fire.
What *is* reachable is the other way round: `TeamsWithoutForecast` is empty when the Feature has no
remaining work anywhere, while a per-Team row whose simulation ran no trials still comes back empty.
A **placeable** Feature can therefore carry a `teamForecasts` row with no percentiles in it.

**A Team may have no name to find.** The names available are the *Portfolio's* involved Teams. A
Feature can sit in several Portfolios, so a `teamId` on the Feature can be a Team this Portfolio does
not list. Dropping that lane would recreate exactly the silent disagreement the story exists to
prevent, one level down.

## Decision

**One switch shows every multi-Team Feature's lanes at once, default off. A lane is an ordinary bar
in its own row, coloured for its Team. The Feature's own bar is not touched by any of it — not its
dates, not its object, not its row.**

Six points that are part of the decision.

1. **One global switch, not an expander per row.** The chart has no grid pane and therefore no
   native expander, and building a second one in the bar template would put a control inside a shape
   a few pixels tall whose whole job is to be read at a glance. One switch above the chart, beside
   the probability buttons, is the affordance this surface can carry. Off is the default **on a first
   visit**, because lanes multiply the chart's height by roughly the average Team count and a reader
   who opens the tab wants the Delivery, not every Team in it.

   **The switch remembers, per browser and not per Delivery.** "Show me Teams" is a property of the
   reader rather than of the Delivery, which is the same call column visibility already makes. It is
   kept in `localStorage` under one key, `lighthouse:deliveryTimeline:showTeams`, following the shape
   the product already uses for a boolean view preference: read once through a lazy initialiser, the
   stored string compared rather than coerced, every access wrapped, and any failure — blocked
   storage, private browsing, a corrupt value — falling back to the default with the tab still
   working. The `lighthouse:` prefix is the house convention and is not decoration: this store is
   shared across the whole origin with the auth, theme and usage-data markers.

   The switch is labelled **"Show Teams"**, composed from the plural terminology key so it reads
   "Show Squads" for a reader who renamed the term. It is rendered **whenever showing the Teams would
   change anything on this chart** — a lane, a note about a Team that has none, or a Team's name on a
   bar it has to itself — and is absent otherwise rather than present and inert: a control that cannot
   change anything still invites the click that proves it, and a reader who gets nothing back
   concludes the feature is broken rather than inapplicable. *(This gate was drafted as "two or more
   contributing Teams" and widened during delivery, once point 7 below gave a single-Team Feature
   something to say.)*

2. **Off means absent, not collapsed.** When the switch is off no lane exists — not a hidden row, not
   a closed parent. The task list handed to the library is then byte-identical to the one slice 04
   ships today. That is what makes the claim in point 3 provable rather than argued, and it is most of
   what makes removing the slice cheap — though not all of it, since the axis still has to learn about
   lanes and that edit lands in a shipped model file.

3. **The Feature's bar does not move when lanes are turned on, and does not move *by construction*.**
   Its start, its end and its object are the same with lanes on and lanes off, because nothing in the
   lane path touches them. **Its appearance is another matter and deliberately so**: a single-Team
   Feature's bar takes its Team's colour and name while the Teams are shown (point 7). The guarantee
   is about dates, which is what a reader could be misled by; a colour cannot misdate anything.

   This is the point the story most needed pinned, since a Feature's own
   start is taken inside each simulated run while a lane is that Team's own marginal — so the
   Feature-level figure is **not** required to equal the earliest lane's, and on a dependent pair it
   will not. A reader will see the two disagree. The guarantee that has to hold is that the
   disagreement is the forecast's and not the drawing's, and a single assertion comparing the two
   task lists covers it, with no drawing surface required.

4. **Lanes are flat rows, not children.** A lane is an ordinary task placed immediately after its
   Feature. It carries no `parent`, no `open` and no `type: "summary"`. The library's hierarchy buys
   nothing that is visible here: indentation is drawn by the grid pane, which is off; inclusion is
   already decided by whether a lane is emitted at all; and a summary's span recomputation is a code
   path worth never entering rather than worth relying on not to fire. So this slice adds **no new
   vendor vocabulary at all** — every task it emits uses the one shape already on screen.

   What carries the grouping instead is what a reader actually looks at: the lane sits directly
   under its Feature, it is written with the Team's name rather than the Feature's, and it is filled
   with that Team's colour rather than the one colour every Feature bar wears.

5. **One colour per Team, and the same colour whatever the reader is looking at.** From
   `getColorMapForKeys`, the same helper the Delivery Metrics charts use through `deliveryEpicColors`,
   for the same stated reason: a map built per Feature would paint one Team two different colours on
   one screen. It is keyed by **Team id**, not name — a Team whose name cannot be resolved still needs
   a colour of its own, and keying on the name would collapse every unnamed Team into one bucket.

   **The key set has to be one the probability cannot change, and this is a defect that was
   reproduced rather than a precaution.** The helper sorts its keys and hands out colours by position,
   so a set built from the Teams that happen to have a lane loses a Team when the reader moves the
   control — and every Team after it takes its neighbour's colour. The set is therefore every Team in
   reach: the Portfolio's own, unioned with every Team any Feature names. One consequence is worth
   stating because it is what makes a legend worth drawing at all: **a Team's colour is stable across
   percentile changes and across the Deliveries of one Portfolio.**

   Colour is never the only carrier. The Team's name is written along the lane and in the legend, so
   the chart reads without it.

6. **A Team is never silently dropped, and the two ways it can go missing are answered differently.**

   | | What is missing | What the reader gets |
   |---|---|---|
   | The Team has percentiles but this Portfolio does not list it | the name | a real lane, its own colour, labelled as a Team from outside this Portfolio |
   | The Team has a row but no percentiles at either end | the dates | **no lane** — the Team is named on the Feature's bar mark, with the reason there is no lane |

   The second case gets a note rather than a bar because a lane with no dates is not a lane. The
   adapter's own documentation records what the library does with an undated task: it draws it at a
   position the data does not support instead of leaving it out, which is why the caller filters
   first. A named note on the bar that already carries notes reuses the mark, the tooltip and the
   sentence list wholesale, and it cannot be mistaken for a date.

   **While lanes are shown, that Team is named on the bar and not only in its tooltip.** Every other
   Team on the Feature is written along a row of its own, so the one Team without a row is the only
   one a reader would have to hover to find — and a name reachable only by hovering fails the question
   this whole surface exists to answer, which is seeing which Team it is without opening anything.
   With lanes hidden the bar is back to competing with its own Feature name, and the symbol carries it.

   That note is **not a warning**. On the one shape that is actually reachable the Feature is
   forecasting correctly and the Team has nothing left to do; raising amber there would devalue the
   symbol on exactly the terms that were settled when the dependency marks were designed.

7. **Added during delivery: with the Teams shown, a single-Team Feature's bar says which Team.** It
   takes that Team's colour and name. It is not split and gains no lane — it is one Team's work and
   already occupies a row of its own — so this costs no height and restates nothing.

   The original decision gave it nothing, reasoning that a lane restating a summary bar is noise.
   **The running chart overturned that**: with the switch on, five of the demo Delivery's ten Features
   showed no Team at all, so a reader who had just asked "which Team" was met by half a chart
   declining to answer. A control named *Show Teams* that leaves most bars anonymous is not showing
   Teams. This is why point 1's gate widened and why point 3 guarantees dates rather than pixels.

**What is deliberately not decided here**: lanes carry no dependency lines, and the Feature table is
untouched. Both were settled upstream and neither is reopened.

**Withdrawn during delivery**: the chart also carried a sentence explaining why a Feature's bar can
reach past its Teams' lanes — its start is the earliest across its Teams taken inside each run, so its
P70 is the 70th percentile of a minimum and cannot exceed the smallest Team's P70. That sentence was
removed. The arithmetic is unchanged and is recorded in the feature's own document; the judgement is
that the disagreement does not need explaining on screen, and it was made by looking at the chart
rather than by reasoning about it, which is what the question was always waiting for.

## Alternatives considered

- **An expander on each Feature's row, using the library's native parent/child toggle.** How the
  story is written, and the first thing anybody proposes. **Rejected on evidence**: the toggle is
  rendered by the grid cell renderer and handled in the grid component, and this chart has no grid
  pane. Restoring the pane to get one expander would give back the fixed-width column that was
  removed because it pushes the chart off a narrow window, and would put every Feature's name on
  screen twice.
- **A hand-built expander inside the bar template.** Reachable — the bar's content is ours. **Rejected**:
  it puts a click target inside a shape a few pixels tall that already carries a name, a mark and a
  click of its own, and it makes "which bars are open" a second piece of state the reader has to
  maintain by hand across a chart that can be forty rows long.
- **Keep the hierarchy, with the Feature as `type: "summary"`.** The library recomputes a summary's
  span from its children only when the summary is missing one of its own dates, and this chart is
  read-only so the paths that reset a summary are unreachable — so it would in fact work.
  **Rejected** because it makes the story's central guarantee depend on a conditional inside somebody
  else's store, verified by reading a bundle, with nothing that re-asks after a library upgrade. The
  flat form makes the same guarantee unbreakable instead of merely tested.
- **Keep the hierarchy, with the Feature as `type: "task"` and a `parent` on each lane.** Avoids the
  summary code path entirely and still groups the rows in the store. **Rejected, narrowly**: it buys
  two new hand-written vendor literals — `parent` and `open` — and neither has a visible effect once
  the grid pane is off. A wrong value in either draws nothing and raises nothing, which is the failure
  class this component has already been bitten by twice. Paying that price for a grouping nothing
  reads is the wrong trade. This is the alternative most worth revisiting if the grid pane is ever
  turned back on.
- **Per-Feature expansion state instead of one switch.** More control, and the maintainer considered
  it before settling. **Rejected**: it is the same affordance problem as the first two alternatives,
  and with lanes off by default the reader's actual question — "does this Delivery split by Team at
  all" — is answered by one click rather than by hunting for the Features that do.
- **Add the Team's name to `FeatureTeamForecastDto`.** The obvious fix for the join. **Rejected**:
  the slice forbids a backend change, and correctly — the name is already client-side, one component
  up, in a prop the Timeline tab's own parent already holds. Adding a field to a shipped contract to
  re-deliver something the caller already has is how a DTO grows a copy that drifts.
- **Drop a lane whose Team cannot be named.** Cheapest. **Rejected outright** — it is the silent
  disagreement this story exists to prevent, moved one level down and made harder to notice.

## Consequences

- **Positive**: the Feature's bar is provably untouched. The strongest form of the story's central
  guarantee is available as one assertion over two task lists, and it needs no drawing surface, which
  is the constraint everything else in this module is shaped around.
- **Positive**: no new vendor vocabulary. This slice adds nothing to the set of hand-written library
  literals that a later refactor can silently break, which is the accepted residual the dependency
  lines already carry.
- **Positive**: the switch off is the shipped chart. A reader who never touches it sees exactly what
  they see today, and removing the slice is a deletion of three modules — the lane model, the
  preference hook and the legend — plus the reversal of four small additive edits. **Weaker than the
  equivalent claim one slice ago**, and the difference is worth naming: the dependency overlay could
  point at an untouched `deliveryTimelineModel.ts` as its evidence, and this slice widens
  `timelineWindow` inside that file, so the argument has to be made from the edits themselves rather
  than from a file nobody opened.
- **Positive**: one Team is one colour across the chart, by the same helper and the same argument the
  Delivery Metrics tab already uses. A reader who has learned a colour on one chart has not learned a
  different rule for this one.
- **Negative — accepted residual.** Flat lanes rely on the library rendering tasks in the order it is
  handed them. That is not a new assumption — board order is already relied on for the Features
  themselves and was verified by eye at slice 04's delivery — but it is now load-bearing for the
  grouping as well as the ranking. If the library ever re-sorted, lanes would separate from their
  Feature and the chart would read as nonsense rather than as broken.
- **Negative — accepted residual.** Filling a lane with its Team's colour is done inside the bar
  template, which is ours. The library's own bar element keeps the global border and fill underneath,
  so a rim of the default colour may show at the edges. No unit test can see it: this environment has
  no drawing surface, which is the same reason the axis format and the link routing enum are verified
  by a person or not at all. Verified by looking at the chart, once, at delivery — and after that
  day nothing re-asks.
- **Negative**: with lanes on the chart roughly doubles in height. Counted against the demo Delivery
  it is ten rows becoming twenty-one — four Features carry lanes, contributing eleven between them —
  which is about 850 pixels of chart inside an accordion where 430 sits today. That is the reason the
  switch defaults to off, and whether twenty-one rows still reads is the question the dogfood
  walkthrough exists to answer. (This measurement was briefly wrong: a malformed demo CSV, since
  repaired, had removed one Team from that Portfolio entirely and halved the figure.)
- **Negative**: a lane is always a forecast, even on a Feature whose own bar starts at an observed
  date. There is no per-Team observed start anywhere — not on the contract and not in the domain —
  so a started Feature will show a bar beginning in the past above lanes beginning in the future.
  This is the single most likely thing to be read as a bug, it is carried as an upstream change
  rather than designed around, and the walkthrough checks it on a Feature that has it.
- **Reuse verdict**: `getColorMapForKeys` → **REUSED AS IS**. `buildDeliveryTimeline` → **REUSED AS
  IS**; lanes are computed beside it, never inside it. `cannotBeForecast` → **REUSED AS IS, and
  deliberately not called at Team grain** — it reads `teamsWithoutForecast`, which is a Feature-level
  verdict the backend owns, and re-deriving it per Team client-side would be a second opinion on a
  settled question. `ganttShapes.toGanttTasks` → **EXTEND** (an optional second argument; the
  one-argument call is unchanged). `ganttShapes.chartHeight` → **REUSED AS IS**, handed the visible
  row count instead of the bar count. `deliveryTimelineModel.timelineWindow` → **EXTEND** by widening
  its parameter to anything carrying a start and an end **and by changing its one call site**, since
  the widening alone compiles and changes nothing while a lane is clipped off the axis in silence. It
  lives in `deliveryTimelineModel.ts`, not in `ganttShapes` — which means this slice opens a file
  slice 05 left alone, and that is what the severability consequence above turns on.
  `TimelineBarContent` /
  `TimelineBarMarks` / `BarNote` → **EXTEND**; the un-laned Team's note is one more note on a mark
  that already exists. `DeliveryTimelineTab` → **EXTEND** (one prop, one control, one computation).
  `DeliveryGanttChart` → **EXTEND** (one prop, and a lookup keyed by task id rather than by number).
  The switch's persistence → **PATTERN REUSED, NOT EXTENDED**: the grid's own persistence hook cannot
  carry a boolean, because its state type is a four-field grid shape and every write runs a whitelist
  sanitiser built to prevent storage poisoning. The shape is copied from the product's existing
  boolean view preference instead — lazy read, wrapped access, default on failure.
  `buildDependencyOverlay` → **NO CHANGE**. `FeatureTeamForecastDto`, `FeatureDto`, `Feature` and
  every query behind them → **NO CHANGE**; this slice adds no data, no field and no endpoint.
- Cross-refs [ADR-203](./adr-203-a-drawn-dependency-line-means-the-forecast-acted-on-it.md) (the
  accepted residual for hand-written vendor literals, which this decision minimises rather than
  extends, and the rule that a picture being short is not a warning),
  [ADR-198](./adr-198-shared-dialog-optional-columns-attached-by-the-payload-that-owns-the-population.md)
  (the dialog a lane's click reaches, unchanged),
  [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) and
  [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (why a Feature's own
  figure is not the earliest or latest of its Teams', and why a Team with no throughput takes the
  whole Feature off the chart),
  [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) /
  [ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) (what puts a bar where
  it is, and the grain at which "observed" exists at all).
