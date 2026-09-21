# ADR-205: The Delivery timeline answers one question at a time, and the bar wears the answer

- **Status**: Proposed (2026-09-21, DESIGN; rewritten the same day after the first encoding was seen running) — awaiting maintainer ratification
- **Date**: 2026-09-21
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, slice 07 / User Story #6067)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

The Delivery timeline marks the target date as a tinted column on its axis, and has done since slice 04.
Nothing on the chart has ever compared anything to it. A reader who wants to know which Features miss
the date measures each bar against that column by eye, and measures them all again every time they move
the probability.

Story #6067 asks for a visual indication of two things: work that is done, and work forecast to be late —
the latter at two levels, one that starts in time and finishes late, and one that is not forecast to
start until after the date at all. It proposes the existing forecast colours, green, amber and red.

By the time this slice began, the bar's fill was already spoken for. Slice 06 gave a Feature's own bar
its Team's colour where it has exactly one Team, and gave every Team lane its own. Slice 05 put a warning
symbol on bars with something to say. The chart had acquired three things it could tell a reader about a
bar and one place to say them.

**An earlier draft of this decision resolved that by shrinking the new mark**: the status became a cap at
whichever end of the bar crossed the target, so that a bar could wear a Team's colour and a status at the
same time. It was built, tested and looked at. The maintainer's verdict on seeing it was that the marks
were too small to read and that showing three things at once was the wrong goal — the reader asks one
question at a time.

Five facts about the ground shape what replaced it.

**Two of the three requested colours are already on this chart.** `main.tsx` wires
`theme.palette.warning.main` from `appColors.status.warning`, which is `#ff9800` — and
`appColors.forecast.realistic`, the story's "finish late" amber, is the same string. That colour already
paints the target band. The green sits one family away from `primary.main`, which is every bar's default
fill. Only the red is unclaimed.

**Both statuses are computable with no backend change.** `buildDeliveryTimeline` already reads
`feature.closedDate`, and the tab already holds the target date and the function that reduces it to a
calendar day.

**A bar's end already means two different things, and says which to nobody.** It is drawn from
`closedDate` when the Feature has closed and from the completion forecast otherwise. `TimelineBar`
carries `startIsObserved` for exactly this distinction at the *other* end, and had no counterpart.

**The product already ships a per-Feature verdict against the target date, and it is a different
statement.** `IFeatureLikelihood` renders green/amber/red in the Delivery's Features grid through
`ForecastLevel`. That ladder has four levels at 50/70/85 plus Unknown, and it does not move when the
timeline's probability selector moves.

**Naming a Team that got no lane is a promise the split makes**, not a warning. Slice 06 put that naming
on the Feature's bar so the split could never show fewer Teams than the Feature has.

## Decision

**The reader chooses one thing for the chart to say about its bars, and the bar wears the answer as its
whole fill.**

The choice is one of four — nothing, the Teams, the status, or the warnings — held as a single value, and
the bar's fill belongs to whichever is chosen. Three things wanted that fill and there is one of it; asking
one question at a time is what lets every answer be full width and full colour instead of two of them
competing and a third reduced to a few pixels.

**The cost is real and is accepted**: a reader asking "which Team is making this late" answers it in two
looks rather than one.

**Status is what a reader who has never chosen is shown.** It is the only one of the four whose answer is
not available anywhere else in the product — the Teams and the warnings are both on the Feature table,
and whether a bar crosses the target date is not. This is a deliberate change to what an existing reader
sees: bars gain colour, and the warning symbol that has been unconditional until now is behind a choice.

**One verdict per bar, ranked**, because the bar wears it whole:

- **Finished** outranks everything. Its ends are days work actually stopped, so the target is not a
  question anyone is asking about it — including when it finished after the date.
- **Not started in time** outranks merely finishing late. Every bar that starts after the date also ends
  after it, so without the ranking the sharper case would never be seen; and it is a different
  conversation, about what the Delivery contains rather than about how fast anyone is going.
- **Finishes late** is what is left. **On track wears nothing** — a colour on nearly every bar tells the
  reader nothing about any of them.

**The comparison is on calendar days, strictly after.** The target is a stored instant the product reads
as a UTC day; a bar's ends come from forecast dates carrying a time. Both are reduced before comparing,
and a bar ending *on* the target day is on track.

**"Finished" is read off the decision that drew the bar.** `TimelineBar` gains `endIsObserved` beside
`startIsObserved`, set where `buildDeliveryTimeline` already chooses between the close date and the
forecast. Nothing downstream re-reads `closedDate`.

**The colours are the forecast palette**, and two collisions are accepted on the record rather than
designed around. The late amber is the target band's own colour: a whole bar against a tinted column
reads as a bar, and the key names it. The finished green is a second green on a chart whose default bars
are green. If either fails to read at bar height, the thing that moves is the one that belongs to a case
rather than to every bar on every Delivery — the band, and the default fill respectively.

**A view this Delivery cannot answer is not offered**, and the group of choices hides entirely when only
"nothing" is left. A control that does nothing when used is still worth the click that proves it, and a
reader who gets nothing back concludes the chart is broken rather than that the question does not apply.
Each view's availability is decided from what the Delivery *is* — never from what is drawn at the
probability currently selected, since that is exactly what the probability buttons change.

**A reader's choice is one value for the whole page.** A Delivery that cannot honour it shows nothing
rather than something the reader did not ask for, and leaves the stored choice untouched so the Delivery
that can honour it still does.

## Consequences

**The key is not optional, and the second green is why.** A finished bar and an on-track bar are both
green, and the key is the only thing that distinguishes them. It is shown only while the status is being
shown. If the two greens turn out to read apart cleanly, the key becomes droppable.

**Naming a Team without a lane follows the Teams, not the warnings.** An earlier arrangement carried that
naming inside the bar's warning marks, which meant a reader looking at the Teams was told about one Team
and left to guess at another. The view owns everything about the view.

**The timeline and the Features grid can disagree about one Feature.** The grid's chip is a probability
against the target; a bar's colour is where it actually lands at the probability on screen. They answer
different questions and the key says which one the chart is answering.

**Severability is structural.** The rule lives in one pure module and reaches the chart as one optional
prop, along the path the Team colours already travel. No dependency is added, no `@svar-ui` import moves,
and the adapter's hand-written vendor vocabulary gains no word.

**A stored choice can outlive the code that wrote it.** The value is a word rather than a flag, so a build
that renames or withdraws a view leaves readers holding something this version does not know. The store
validates against the words on offer and falls back, because handing the word back would leave the
control with nothing selected on a chart that is showing something.

## Alternatives considered

**A cap at the end that crossed the target, so status and Team could coexist.** Built, tested and
rejected on sight: a few pixels at the end of a bar is too small to carry a verdict, and the coexistence
it bought was not worth the second visual language it introduced. Its one merit — that a bar could say
"starts late" and "finishes late" at once, with no precedence — is the thing this decision gives back up.

**Status in the fill with the Teams taking it back when shown.** Two switches, either of which silently
costs the reader the other answer without saying so. The exclusive choice is the same trade made
honestly.

**Hatching or a pattern instead of colour.** Colour-blind-safe by construction, and the story's own
suggestion. Still the fallback if a colour fails to read, rather than the first choice: it is harder to
read at these bar heights and the product has no existing pattern vocabulary to borrow.

**Reusing `ForecastLevel`'s four-level likelihood ladder.** Would make the timeline agree with the
Features grid by construction. Rejected: that verdict is fixed per Feature and does not move with the
probability selector, so the colour would sit still while the bar under it moved.

**Recolouring the target band up front to free the amber.** The cleanest palette, and still available.
Rejected as scope: it reverses a shipped slice 04 decision and changes a docs screenshot, for a collision
that may well be readable now that the mark is a whole bar.

## Related

- **ADR-203** — a drawn dependency line means the forecast acted on it. Untouched.
- **ADR-204** — one switch turns a Delivery into Team lanes, and the summary bar never moves. The switch
  becomes one option in a group; the summary bar is unchanged, and a Feature's task object is still
  byte-identical with lanes on and off.
- **ADR-201** — observed start supersedes the forecast in the domain. `endIsObserved` is the same
  distinction at the other end of the bar, made in the browser because that is where the choice between
  the close date and the forecast is already made.
