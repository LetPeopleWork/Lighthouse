# Upstream changes owed by DESIGN — Epic 6033, slice 06 (US-06 / ADO #6050)

Raised 2026-09-21 by the DESIGN pass for slice 06. These are changes to **DISCUSS** artifacts, found
by reading the code the slice sits on. Nothing has been edited in place; this file is the record and
the proposed wording, for whoever amends US-06.

The full reasoning is in `feature-delta.md` under
`## Wave: DESIGN / [REF] Slice 06 — Changed Assumptions and Back-Propagation`.

---

## 1. AC-6.1 — rewritten, because there is no expander to have

**Where**: `docs/feature/epic-6033-forecasted-start-dates/feature-delta.md`, DISCUSS / User Stories,
US-06 (line 562 at the time of writing).

> **Original, verbatim (2026-09-20)**
>
> **AC-6.1** — A Feature with one contributing team has no expander on the timeline. Its row is unchanged.

**Why it will not do.** The chart has no per-row expander and cannot grow one. The library's toggle
is rendered by its **grid cell renderer** and handled in its **grid component**, and
`DeliveryGanttChart.tsx` sets `columns={false}` deliberately — there is no grid pane. The maintainer
settled the affordance on 2026-09-21: **one global switch, default off**, no per-row control. An
acceptance criterion phrased against a control that does not exist cannot pass or fail.

The criterion's intent survives and is worth keeping, precisely because a global switch makes it
easier to get wrong: a single-Team Feature must never be split, whatever the switch is set to.

**A second clause is owed, which the original had no equivalent for.** With one control governing the
whole chart, "never split" acquires a Delivery-wide counterpart: a Delivery in which nothing can split
must not offer the control at all. The maintainer confirmed that rule on 2026-09-21; until this
clause it was stated as a design decision and no criterion could fail on it.

> **Proposed**
>
> **AC-6.1** — A Feature with one contributing Team is never split into lanes, whether lanes are
> shown or hidden. Its bar and its row are the same in both. **Where no Feature in the Delivery has
> two or more contributing Teams, there is no control to show them** — not a disabled one, and not one
> that does nothing when used.

---

## 2. AC-6.4 — unreachable as written

**Where**: same file, same section (lines 568–569 at the time of writing).

> **Original, verbatim (2026-09-20)**
>
> **AC-6.4** — A team contributing to the Feature but carrying no forecast gets a named lane with its
> reason, not no lane — otherwise the expansion silently disagrees with `TeamsWithoutForecast`.

**Why it will not do — two independent reasons.**

**The route it names cannot fire.** `buildDeliveryTimeline` calls
`cannotBeForecast({ teamsWithoutForecast })` before anything else and pushes the Feature into
`unplaceable`. `Feature.cs:107` is `CanBeForecast => !TeamsWithoutForecast.Any()`, and
`FeatureDto.cs:34` only fills `Forecasts` when `CanBeForecast`. So a Feature with any Team lacking
throughput **has no bar at all**, and there is nothing to expand. Demo data proves it: OE-001 has
three contributing Teams, one of them Meridian with no throughput, and it is on the "Not on the
timeline" list rather than on the chart.

**Where the shape *is* reachable, a lane is the wrong answer.**
`Feature.TeamsWithoutForecast` returns `[]` outright when `FeatureWork.Sum(RemainingWorkItems) <= 0`
(`Feature.cs:178`), while `SomethingToSay(...)` still returns `null` for a per-Team forecast with
`TotalTrials == 0` (`Feature.cs:167`). So a **placeable** Feature can carry a `teamForecasts` row
with empty percentiles. A lane for it would have no dates — and `DeliveryGanttChart`'s own header
records what the library does with an undated task: it "draws [it] at a position the data does not
support instead of leaving it out, so the caller filters first". The lane would assert a schedule
that does not exist.

What must survive is the criterion's purpose: a contributing Team is never silently dropped.

> **Proposed**
>
> **AC-6.4** — A contributing Team that the Feature carries no forecast for is **named on the
> Feature's bar**, with the reason it has no lane, so the split can never quietly show fewer Teams
> than the Feature has. **While lanes are shown its name is readable without hovering or opening
> anything**, alongside the Teams that do have lanes. Where nothing is wrong with the Feature itself,
> that naming does not read as a warning.

The hovering clause was added 2026-09-21 after the DISCUSS reviewer raised, at the wave gate, that a
name reachable only by tooltip fails the test US-06 is built on — seeing which Team it is without
opening anything. It is phrased as something a reader can observe and names no component.

---

## 3. AC-6.6 — new, for a case the original set has no criterion for

**Why it is owed.** `teamForecasts` keys on `teamId`; the only Team names available to the timeline
are the **Portfolio's** `involvedTeams`. A Feature can sit in several Portfolios, so a `teamId` need
not resolve to a name. Silently dropping that lane would recreate exactly the disagreement AC-6.4
exists to prevent, one level further down and harder to notice. The shape already exists in the
repository: `DeliverySourceTabFixture.tsx:55` sets `involvedTeams: []`.

> **Proposed**
>
> **AC-6.6** *(new)* — A contributing Team whose name this Portfolio does not hold still gets its own
> lane, identified as a Team from outside this Portfolio rather than omitted.

---

## 4. The slice brief's observed-start bullet — withdrawn

**Where**: `docs/feature/epic-6033-forecasted-start-dates/slices/slice-06-per-team-sub-lanes-on-the-timeline.md`,
**IN scope**, final bullet.

> **Original, verbatim**
>
> A started team's sub-lane begins at that team's observed start where one exists, consistent with D5.

**Why it will not do.** There is no per-Team observed start anywhere in the product.
`FeatureTeamForecastDto` (`API/DTO/FeatureStartDto.cs:43`) carries a Team id and two percentile
lists; `IFeatureTeamForecast` mirrors it exactly. And this is **not** a plumbing gap slice 01 left
behind — the **domain** has never recorded it either. `Feature.WhenWorkBegins` is Feature-level and
resolves to `StartedDate ?? CreatedDate` **of the Feature**; a Team-grain equivalent would be the
earliest started child work item of that Feature for that Team, which nothing computes. So the change
would be a domain addition, not a DTO field, and it is out of scope under the slice's own
no-backend-change rule.

**Consequence, carried rather than hidden**: a lane is always a forecast, even under a bar that
starts at an observed date. OE-002 is exactly that Feature — state `Next`, started eight days ago,
four contributing Teams (Equinox, Gravity, Lightspeed, Zenith) — so the demo shows a bar beginning in
the past above four lanes beginning in the future. It is the single most likely thing in this slice
to be read as a defect, and step 5 of the DESIGN walkthrough is where it is judged.

> **Proposed**: delete the bullet from IN scope, and add to OUT of scope:
>
> - **A per-Team observed start.** Neither the contract nor the domain records one. A lane is always
>   a forecast, including on a Feature whose own bar starts at an observed date; whether that reads
>   as correct is what the dogfood moment decides.

---

## Not changed

**AC-6.2, AC-6.3 and AC-6.5 stand exactly as written.** AC-6.3 is strengthened rather than qualified
by the design: with lanes emitted as flat tasks and the switch off meaning *absent* rather than
*collapsed*, the Feature's task object is byte-identical whether lanes are shown or not. The
criterion holds by construction and is assertable with no drawing surface.

---

# Upstream changes owed by DESIGN — Epic 6033, slice 07 (US-07 / ADO #6067)

Raised 2026-09-21 by the DESIGN pass for slice 07. Same contract as the slice 06 record above: changes
to **DISCUSS** artifacts found by reading the code and the geometry, written here rather than edited in
place, for whoever amends US-07.

Full reasoning: `feature-delta.md`, `## Wave: DESIGN / [REF] Slice 07 — Changed Assumptions and
Back-Propagation`.

---

## 5. AC-7.2 — the precedence has nothing left to rank

**Where**: `feature-delta.md`, DISCUSS / US-07 acceptance criteria.

> **Original, verbatim (2026-09-21)**
>
> **AC-7.2** — A Feature whose bar *starts* after the target date carries the "starts late" colour
> instead. One status per bar, and the start test wins: a Feature that has not begun by the target cannot
> finish by it, so saying only "finishes late" about it would be true and useless (D7-4).

**Why it will not do.** The precedence existed only because the mark was one thing wearing one colour.
The maintainer chose, on 2026-09-21, to put the mark on **the end that crossed the target** — a cap at
the start end, a cap at the end end, either or both. There is then nothing for a ranking to rank: each
end answers for itself, and a bar that starts after the target says so at its start *and* says it does
not finish in time at its end. Both statements are true and neither suppresses the other.

The criterion's purpose survives — the "has not even been reached" case must be visible as its own
thing, not folded into ordinary lateness — and is better served, because it is now visible *together*
with what it implies rather than instead of it.

> **Proposed**
>
> **AC-7.2** — A Feature whose bar *starts* after the target date is marked at its start end, and — since
> such a bar also ends after the target — at its end end as well. The two marks are different, and a
> reader can tell the "not reached in time" case from ordinary lateness without counting anything.

---

## 6. AC-7.5 — narrowed from an edge to a cap

> **Original, verbatim (2026-09-21)**
>
> **AC-7.5** — The status is carried by an **edge on the bar, not its fill**. With the Teams shown, a bar
> keeps its Team colour *and* its status; neither hides the other, at any combination of the two switches
> (D7-1).

**Why it is narrowed.** "An edge" admitted a full outline round the bar, which is what DISCUSS pictured.
The encoding chosen is narrower and says more: only the end or ends concerned are capped. The guarantee
the criterion was written for is untouched, and it is the half that matters.

> **Proposed**
>
> **AC-7.5** — The status is carried by a cap at the end or ends concerned, **never by the bar's fill**.
> With the Teams shown, a bar keeps its Team colour *and* its status; neither hides the other, at any
> combination of the two switches (D7-1, D7-13).

---

## 7. AC-7.8 — the gate must not move with the probability

> **Original, verbatim (2026-09-21)**
>
> **AC-7.8** — A **Show status** switch, default off. It is **absent, never present-and-inert**, when no
> bar on this chart would carry a status — the rule `canShowTeams` already established.

**Why it will not do.** Read literally, "no bar would carry a status" is evaluated against the bars as
currently drawn — and which bars cross the target is exactly what the probability selector changes. The
switch would therefore be absent at P70 and present at P95 on the same Delivery, appearing and vanishing
under the reader's hand as they work the three buttons. That reads as a fault in the page, and it is the
opposite of what `canShowTeams` achieves, whose condition is a property of the data and does not move.

> **Proposed**
>
> **AC-7.8** — A **Show status** switch, default off. It is **absent, never present-and-inert**, on a
> Delivery that could carry no status at all — one with no target date and nothing finished in it. **Its
> presence does not change when the probability changes**: a control that came and went as the reader
> moved between 70, 85 and 95 would read as a fault rather than as an answer.

---

## 8. D7-4 — withdrawn

> **Original, verbatim (2026-09-21)**
>
> **D7-4** — **Start-late wins over finish-late.** Every start-late Feature is also finish-late, so
> without a precedence the red case never appears.

**Withdrawn**, and replaced by **D7-13**. The observation is still true and is now the reason such a bar
wears two caps rather than the reason one colour is suppressed.

---

## Not changed

**AC-7.1, AC-7.3, AC-7.4, AC-7.6, AC-7.7 and AC-7.9 to AC-7.12 stand exactly as written.** AC-7.3 in
particular is strengthened rather than qualified: with the mark at the ends, "done wins" stops being a
precedence and becomes a statement about which question is being asked — a finished bar's ends are facts,
so the target comparison is not asked of them at all, and both ends are simply marked as finished.

---

## 9. AC-7.9 — the warnings switch had no visibility gate

**Raised by DISTILL, 2026-09-21**, while writing the scenarios for slice 07, and settled in the same pass
by applying a rule the maintainer had already set rather than by inventing one.

**Where**: `feature-delta.md`, DISCUSS / US-07 acceptance criteria.

> **Original, verbatim (2026-09-21)**
>
> **AC-7.9** — A **Show warnings** switch, default off, hides every bar mark: the symbol *and* the
> sentences it contributes to the bar's hover text. Half-hidden is a switch that reads as broken (D7-9).

**Why it was incomplete.** D7-19 settles the visibility gate for `Show status` — offered when the
Delivery could carry a mark, and not recomputed per probability — and nothing settled one for
`Show warnings`. As specified, that switch is always present, including on a Delivery where no Feature
has a warning and none carries a dependency. It would then be a control that does nothing when used,
which is the shape slice 06 ruled out in as many words and made AC-6.1's second clause about: *"not a
disabled one, and not one that does nothing when used."*

**Why it is settled here rather than asked.** The rule is not new. The maintainer set it for
`Show Teams` on 2026-09-21 — absent, never present-and-inert — and D7-19 applied it unchanged to
`Show status`. Applying the same rule to the third switch on the same row is consistency, not a
decision; leaving the third switch to behave differently from the other two would be the thing that
needed arguing for. The inputs are already to hand and are both independent of the selected probability,
so the gate is stable for the same reason D7-19's is: `featureWarningSentences` reads a Feature's own
state and its dependency list, and `feature.dependsOn` is a property of the Feature rather than of the
forecast.

> **Proposed**
>
> **AC-7.9** — A **Show warnings** switch, default off, hides every bar mark: the symbol *and* the
> sentences it contributes to the bar's hover text. Half-hidden is a switch that reads as broken (D7-9).
> **It is absent, never present-and-inert, on a Delivery where no Feature has anything to be marked for**
> — none carries a warning and none carries a dependency — and, like the status switch, **its presence
> does not change when the probability changes.**

Scenario 31 covers what the switch does; **scenario 32 covers when it is offered.**

---

# Upstream changes owed by DELIVER — Epic 6033, slice 07 (US-07 / ADO #6067)

Raised 2026-09-21, after the first encoding was built and looked at. The maintainer's verdict on seeing
it: the marks were too small to read, and showing three things at once was the wrong goal — the reader
asks one question at a time. What follows is what that costs the criteria, written here rather than
edited in place.

The encoding is now: **one choice of what the chart says about its bars — nothing, the Teams, the status
or the warnings — and the bar wears the answer as its whole fill.** ADR-205 is rewritten accordingly
rather than amended; it argued for caps throughout.

---

## 10. AC-7.2 and D7-4 — the precedence is reinstated

**Item 5 above withdrew D7-4** on the grounds that, with a mark at each end, there was nothing for a
ranking to rank. A whole-bar fill takes that back: one bar carries one colour, so the three cases are
ranked again.

> **Live form**
>
> **AC-7.2** — A Feature whose bar *starts* after the target date carries the "not started in time"
> colour, and not the "finishes late" one. Every bar that starts after the date also ends after it, so
> without the ranking the sharper case would never be seen — and it is a different conversation, about
> what the Delivery contains rather than about how fast anyone is going.
>
> **D7-4 stands as originally written.** D7-13's withdrawal of it applied only to the cap encoding.

---

## 11. AC-7.5 — the whole bar, not an edge and not a cap

> **Original, verbatim (2026-09-21)**
>
> **AC-7.5** — The status is carried by an **edge on the bar, not its fill**. With the Teams shown, a bar
> keeps its Team colour *and* its status; neither hides the other, at any combination of the two switches.

**Both halves are gone.** The status *is* the fill, and a bar never carries the Teams and the status at
once, because the reader is asked one question at a time. What the criterion was protecting — that
choosing one thing does not silently cost the reader another — is now served by saying so outright
rather than by finding room for both.

> **Proposed**
>
> **AC-7.5** — While the status is being shown, a Feature's bar wears its status as its whole fill. The
> Teams and the status are never on the chart together: the reader chooses one, the control says which,
> and neither is ever hidden while something claims it is showing.

---

## 12. AC-7.8 and AC-7.9 — one control, and the default is not "off"

> **Original, verbatim (2026-09-21, as already amended)**
>
> **AC-7.8** — A **Show status** switch, default off. It is absent, never present-and-inert, on a
> Delivery that could carry no status at all …
>
> **AC-7.9** — A **Show warnings** switch, default off, hides every bar mark … It is absent, never
> present-and-inert, on a Delivery where no Feature has anything to be marked for …

**Two switches become one group of four, and the default becomes the status rather than nothing.** The
visibility rule survives unchanged and now governs every option; the default is a deliberate change to
what an existing reader sees, and a larger one than either switch was on its own.

> **Proposed**
>
> **AC-7.8** — The chart offers one choice of what it says about its bars: nothing, the Teams, the
> status, or the warnings. **The status is what a reader who has never chosen is shown** — it is the only
> one of the four whose answer is not available elsewhere in the product. An option this Delivery cannot
> answer is absent, never present-and-inert, and the group is absent entirely when only "nothing" is
> left. **No option's presence changes when the probability changes.**
>
> **AC-7.9** — Choosing anything other than the warnings removes every warning mark: the symbol *and* the
> sentences it contributes to the bar's hover text. The warnings column in the list opened from a bar is
> untouched.

---

## 13. AC-7.6 — unchanged in force, changed in mechanism

AC-7.6 said a Team's sub-lane carries no status. It still holds, and now holds for a second reason: the
lanes and the status are never on the chart at the same time. The criterion needs no rewording; this note
exists so the next reader knows it is guarded twice rather than once.

## Not changed

**AC-7.1, AC-7.3, AC-7.4, AC-7.7 and AC-7.10 to AC-7.12 stand as written.** AC-7.7 in particular is
strengthened: the key was arguably droppable while each mark sat at a known end of the bar, and is not
droppable now, because a finished bar and an on-track bar are both green and nothing else tells them
apart.
