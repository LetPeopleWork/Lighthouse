# ADR-205: Each end of a timeline bar says for itself whether it crossed the target date

- **Status**: Proposed (2026-09-21, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-09-21
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, slice 07 / User Story #6067)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

The Delivery Timeline marks the target date as a tinted column on its axis, and has done since slice 04.
Nothing on the chart has ever compared anything to it. A reader who wants to know which Features miss the
date measures each bar against that column by eye, and measures them all again every time they move the
probability selector.

Story #6067 asks for a visual indication of two things: work that is done, and work forecast to be late —
the latter at two levels, one that starts in time and finishes late, and one that is not forecast to start
until after the date at all. It proposes the existing forecast colours, green, orange and red, and it
leaves the form open, offering hatching as an alternative and noting the whole thing should be toggleable,
off by default.

Five facts about the ground shape the decision. Each closes an option that looks obvious from a distance.

**The bar's fill is already spent.** Slice 06 gave a Feature's own bar its Team's colour when it has
exactly one Team, and gave every Team lane its own. A status painted as a fill would take that back, and a
reader turning one switch on would silently lose the other's answer.

**Two of the three requested colours are already on this chart.** `main.tsx` wires
`theme.palette.warning.main` from `appColors.status.warning`, which is `#ff9800` — and
`appColors.forecast.realistic`, the story's "finish late" orange, is the same string. That colour already
paints the target band itself and the warning symbol on a bar. The green sits one family away from
`primary.main`, which is every bar's default fill. Only the red is unclaimed.

**Both statuses are computable with no backend change.** `buildDeliveryTimeline` already reads
`feature.closedDate`, and the tab already holds the target date and the function that reduces it to a
calendar day. Nothing new has to reach the browser.

**A bar's end already means two different things, and says which to nobody.** `buildDeliveryTimeline`
draws the end from `closedDate` when the Feature has closed and from the completion forecast otherwise.
`TimelineBar` carries `startIsObserved` for exactly this distinction at the *other* end, and has no
counterpart for this one.

**The product already ships a per-Feature verdict against the target date, and it is a different
statement.** `IFeatureLikelihood` renders green/amber/red through `ForecastLevel` in the Delivery's
Features grid. That ladder has four levels at 50/70/85 plus an Unknown, and it does not move when the
timeline's probability selector moves.

## Decision

**Each end of a bar carries its own mark, and the mark says whether that end crossed the target date.**

- A bar whose **start** falls after the target is capped at its start end.
- A bar whose **end** falls after the target is capped at its end end.
- A bar that has **finished** is capped at both ends, in the "finished" colour, whatever the target says —
  its ends are facts, so the target comparison is not a question anyone is asking about it.
- A bar on track carries nothing. A mark that lands on every bar tells the reader nothing about any of
  them.

A bar may therefore wear two caps, and a bar that starts late always does: it says at its start that work
has not been reached, and at its end that it does not finish in time. Both are true, and the second is the
consequence of the first rather than a competing verdict.

**There is consequently no precedence rule.** The story implies a ranking — done, then start-late, then
finish-late — and a ranking is only needed while one bar can hold one mark. Moving the mark to the ends
removes the competition rather than resolving it.

**The mark is a cap, never a fill.** It is painted as an inset shadow on the bar's own box: not a border,
which changes the box's size and so the bar's apparent span, and not an outline, which the library's
overflow clips. The fill stays the Team's, and neither switch costs the reader the other answer.

**The comparison is made on calendar days, strictly after.** The target is a stored instant the product
reads as a UTC day; a bar's ends come from forecast dates carrying a time. Both are reduced before
comparing, and a bar ending *on* the target day is on track.

**"Finished" is read off the decision that drew the bar.** `TimelineBar` gains `endIsObserved` beside
`startIsObserved`, set where `buildDeliveryTimeline` already chooses between the close date and the
forecast. Nothing downstream re-reads `closedDate`.

**The colours are the forecast palette, as the story asked**, and the collision with the target band is
accepted on the record rather than designed around. If the amber cap cannot be told from the amber band
when the two are looked at — and the geometry puts them adjacent in the common case, since a late bar ends
just past the column it crossed — then **the band moves to a neutral blue-grey and the palette stays**.
That response is pre-committed here so it is not re-argued later.

**The whole thing is behind a switch, default off, and the switch's presence is a property of the
Delivery** — it has a target date, or something in it has finished — **not of the probability currently
selected.** Gating on "does any bar carry a mark right now" would make the control appear at 95 and vanish
at 70, which is precisely when bars cross the target, and a control that comes and goes under the reader's
hand reads as a fault in the page.

**The status is drawn on a Feature's own bar and never on a Team's lane.** Whether the *Feature* misses
the date is not a fact about one of its Teams.

## Consequences

**A key is not optional.** Three marks in three colours with no words is the mistake slice 04 made with
the target band and fixed. The colours are handed to the bar and to the key by one function, so the two
cannot disagree — the contract `markerColors` was written for and, until now, had only one caller.

**The mark is small.** A cap is a few pixels at one end of a bar a few tens of pixels tall, and it may sit
over any of fourteen Team fills or the default one. This is the slice's live risk and it is settled by
looking at a real chart in both themes, not by a test: this environment mocks the vendor's stylesheet
away, which is the same reason the axis format and the dependency-link routing have always been verified
by a person or not at all. If the cap loses, the story's own alternative — hatching — is what remains.

**The timeline and the Features grid can disagree about one Feature.** The grid's chip is a probability
against the target; a cap is where the bar actually lands at the probability on screen. At P95 a bar can
cross the target while its chip still reads healthy. This is not a defect and it is not hidden: they
answer different questions, and the key says which one the chart is answering.

**Severability is structural rather than promised.** The rule lives in one pure module and reaches the
chart as one optional prop, along the path `barTeams` already travels. Deleting the file and the prop
leaves the chart slice 06 shipped. No dependency is added, no `@svar-ui` import moves, and the adapter's
hand-written vendor vocabulary gains no word.

**Three page-wide preferences now exist where there was one.** `useShowTeams` carries five traps in one
place — comparing the stored value as a string rather than coercing it, reading before first paint,
guarding every access, notifying by hand because the browser's `storage` event does not fire in the
document that wrote, and holding one value per page rather than per component. Copying it twice more would
make three places to get each of the five right, so it becomes one factory taking the key. That refactor
lands in its own commit, ahead of the feature, with no behaviour change.

## Alternatives considered

**A ring around the whole bar, with the story's precedence.** What DISCUSS specified. Louder, and it makes
"starts late" and "finishes late" mutually exclusive when they never are — the first always implies the
second. It also needs the precedence rule, which is one more thing to get right and one more thing a
mutation can survive quietly.

**Status in the fill, Teams taking it back when shown.** Simplest to draw and the loudest possible mark.
Rejected: it makes the two switches mutually exclusive in effect, so turning one on silently costs the
reader the other answer, and neither switch says so.

**Hatching or a pattern instead of colour.** Colour-blind-safe by construction, and the story's own
suggestion. Held as the fallback if the cap cannot be read, rather than taken first: it is harder to read
at these bar heights, and the product has no existing pattern vocabulary to borrow, so it would be a new
visual language for one chart.

**Reusing `ForecastLevel`'s four-level likelihood ladder.** Would make the timeline agree with the Features
grid by construction. Rejected in DISCUSS: that verdict is fixed per Feature and does not move with the
probability selector, so the colour would sit still while the bar under it moved — the chart disagreeing
with itself.

**Recolouring the target band up front to free the amber.** The cleanest palette. Rejected as scope: it
reverses a shipped slice 04 decision and changes a docs screenshot, for a collision that may turn out to
be readable. Retained as the pre-committed response if it is not.

## Related

- **ADR-203** — a drawn dependency line means the forecast acted on it. Untouched: no line changes.
- **ADR-204** — one switch turns a Delivery into Team lanes, and the summary bar never moves. Untouched: a
  Feature's task object is still byte-identical with lanes on and off, because caps are painted by the
  bar's template and never enter the task list.
- **ADR-201** — observed start supersedes the forecast in the domain. `endIsObserved` is the same
  distinction at the other end of the bar, made in the browser because that is where the choice between
  the close date and the forecast is already made.
