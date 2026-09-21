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
