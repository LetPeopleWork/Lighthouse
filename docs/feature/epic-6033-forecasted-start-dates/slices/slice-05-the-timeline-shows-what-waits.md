# Slice 05 — The timeline shows what waits on what

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-05 · **Estimate**: ~4h
**Severable.** The product owner qualified this with "ideally". Dropping it entirely leaves slice 04
whole, and AC-5.4 says so as an acceptance criterion rather than as a hope.

**Reference class**: the dependency data itself is shipped and rendered elsewhere — Epic 5792 put
`FeatureDependsOnDto` on the Feature API and `FeatureDependsOn` on the Feature table. This slice draws
what is already fetched; it adds no data and no query.

## Goal

A Feature whose bar sits late on the timeline explains itself: it is connected to the one it waits for.

## IN scope

- A connection drawn from a Feature's bar to the bar of a Feature it waits on, within the same Delivery.
- A dependency on a Feature outside this Delivery indicated on the waiting bar, without drawing a bar
  for something the Delivery does not contain.
- A density fallback: above the point where lines stop being readable, degrade to a per-bar indicator
  rather than a thicket. The threshold is chosen from what a real Delivery on the dev instance actually
  reaches, not from a guess.

## OUT of scope

- Editing dependencies from the timeline. It is a read.
- Critical-path computation, slack, or any derived scheduling analysis. Drawing what waits on what is
  not the same as deciding what matters most, and the second is a different feature.
- Any change to how dependencies are fetched or stored.

## Learning hypothesis

**Disproves, if it fails**: that the timeline can carry dependency information without becoming
unreadable — which is a question about real Deliveries, not about drawing lines.

The real risk is density. Five Features with two dependencies is a clear picture; twenty Features with
forty is a ball of wool, and the Deliveries that most need this are the large ones. If the fallback
threshold turns out to be low enough that most real Deliveries hit it, the lines are decoration and the
per-bar indicator is the feature — which is a fine outcome, and much cheaper.

**Confirms, if it succeeds**: the timeline answers "why is this late" as well as "when is this", which
is the difference between a picture and a plan.

## Acceptance criteria

AC-5.1 through AC-5.4 in `feature-delta.md`. AC-5.4 is the severability check and is not optional.

## Dependencies

Slice 04. If slice 04's evaluation recommends buying rather than building, this brief is re-estimated
against whatever that component offers — a commercial Gantt may draw connections for free, or may not
allow them at all.

Independent of slice 06. Either can ship without the other, and dependency lines are drawn at Feature
grain whether or not a Feature is expanded into per-team lanes.

## Pre-slice SPIKE

**No.** But before writing code, count the dependencies on the largest real Delivery on the dev
instance and record the number here. It sets the density threshold, and it decides whether the lines or
the indicator is the primary presentation.

> _(to be filled)_

## Effort

~4h, contingent on slice 04's outcome.

## Dogfood moment

Same day: open the Delivery with the most dependencies and look for a bar that sits later than its
position in the order would suggest. Trace the line. If the answer is not visible in a few seconds, the
density fallback is the feature and the lines are not.
