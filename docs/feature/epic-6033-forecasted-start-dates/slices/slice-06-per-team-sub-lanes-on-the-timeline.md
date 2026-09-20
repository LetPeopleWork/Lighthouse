# Slice 06 — A multi-team Feature splits into sub-lanes on the timeline

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-06 · **Estimate**: ~4h
**Severable.** Dropping it leaves slice 04 whole; AC-6.5 says so as a criterion rather than a hope.
Independent of slice 05 — either, both or neither can go.

**Timeline only.** The Feature table carries one forecast at four percentiles, exactly as the completion
column does, and gains no expander (D16). Splitting a Feature by team is something a Gantt does well and
a table does badly, and the product owner drew that line explicitly on 2026-09-20.

**Reference class**: sub-lanes under a summary bar are the one Gantt idiom slice 04 does not build. The
data is already there — slice 01 serves per-team starts and per-team completions (AC-1.9, AC-1.10).

## Goal

A Feature two or three teams contribute to can be opened on the timeline, and shows one lane per team
under its summary bar.

## IN scope

- An expander on timeline rows for Features with two or more contributing teams. One sub-lane per team,
  each spanning that team's start to that team's completion at the selected percentile.
- The selected percentile drives the sub-lanes exactly as it drives the summary bar (D10). One control,
  every bar, every lane.
- The summary bar unchanged by expanding: it stays the stored Feature-level value (D4), not the earliest
  of the sub-lanes. These can differ, and pinning that is the point of AC-6.3.
- A contributing team with no forecast gets a named lane with its reason rather than no lane, so the
  expansion cannot silently disagree with `TeamsWithoutForecast`.
- A started team's sub-lane begins at that team's observed start where one exists, consistent with D5.

## OUT of scope

- **Anything in the Feature table.** No expander, no per-team column, no popover. One forecast, four
  percentiles, like completion.
- Any backend change. Slice 01 already serves both grains. If this slice needs a backend change, slice
  01 was incomplete and the change belongs there.
- An expander on single-team Features. Most Features have one team, and a lane restating the summary bar
  is noise.
- Dependency lines between sub-lanes. Slice 05 draws dependencies at Feature grain; drawing them at team
  grain is neither asked for nor obviously readable.

## Learning hypothesis

**Disproves, if it fails**: that splitting a Feature by team is worth seeing.

Two ways it disappoints:

1. **Multi-team Features may be rare enough not to matter.** Count them on the dev instance before
   building. If nearly every Feature has one team, this is machinery for a case nobody hits — and the
   honest outcome is to drop the slice and keep slice 01's API half, which costs nothing and leaves the
   question answerable by anyone who asks the API.
2. **The summary and the sub-lanes can disagree, legitimately, and that will read as a bug.** The
   Feature-level start is taken inside the trial (D4); each sub-lane is that team's own distribution. So
   the Feature-level P85 is *not* required to equal the earliest sub-lane's P85, and on a dependent pair
   it will not. Two bars that do not line up is exactly the kind of thing that gets screenshotted into a
   support thread. If the presentation cannot make that read as correct, this slice makes the product
   look wrong and should not ship.

Risk 2 is why this is severable rather than folded into slice 04.

**Confirms, if it succeeds**: "which team is the late one" becomes visible, for start and completion
both, which it has never been — the per-team completion forecasts have existed in the domain and died at
the DTO boundary since before this Epic (S22).

## Acceptance criteria

AC-6.1 through AC-6.5 in `feature-delta.md`.

## Dependencies

Slice 04, entirely — there is no timeline to expand without it. If slice 04's evaluation recommends
buying a component rather than building one, this brief is re-estimated against what that component
offers; sub-lane support varies and some render it only as a paid tier above the one being considered.

Slice 01 for the data, which is already in hand by then.

## Pre-slice SPIKE

**No.** But before writing code, count how many Features on the dev instance have more than one
contributing team, and record it here. It decides whether risk 1 kills the slice, and it costs one query.

> _(to be filled)_

## Effort

~4h. Expander and lane layout ~2h, per-team bar rendering ~1h, tests ~1h.

## Dogfood moment

Same day: open a multi-team Feature on the timeline and expand it, then move the percentile from 70 to
95. The demo is not the lanes appearing — it is whether the lanes explain the summary bar. If the
summary starts in October and no lane starts in October, the presentation has not earned the
disagreement and risk 2 has landed.
