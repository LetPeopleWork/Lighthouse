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

**Added 2026-09-21, DESIGN.** The indicator above is a **warning symbol**, raised by the same rule the
Feature table uses and by nothing else; a blocker that merely has no bar here gets a neutral mark
instead, and a bar with nothing to say gets none. Clicking a bar opens the work-items dialog it already
opens, now carrying a Warnings column — a fifth optional column descriptor on `WorkItemsDialog`,
attached **at this call site only**. Effort below is understated by roughly an hour as a result.

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

**Counted 2026-09-21**, read-only against the dev instance's SQLite database
(`Lighthouse.Backend/Lighthouse.Backend/LighthouseAppContext.db`), which is pointed at the real
Lighthouse Azure DevOps board — real Features and real tracker links, not demo data.

| | |
|---|---|
| Portfolios | 1 ("New Portfolio") |
| Features | 83 |
| Dependency edges | 12 |
| Features carrying any dependency | 8 of 83 — **9.6%** |
| Maximum out-degree | **2** — no Feature waits on three or more things |
| Mean out-degree among those that have any | 1.5 |
| Maximum fan-in | **2** — the busiest blockers (ADO #6051, #5698) have two waiters each |
| Edges resolving inside the same Portfolio | 9 of 12 |
| Edges pointing outside it | **3 of 12 — one in four** |

**The caveat is part of the number.** This is one instance with one Portfolio, and its only Delivery
record is a five-Feature scratch row called "Test", so density could not be measured at Delivery grain
at all. **The counts above are at Portfolio grain.** That is a sound *upper bound* for a Delivery — a
Delivery's Features are a subset of a Portfolio's, so it can carry no more edges than the Portfolio
does — but a bound is not an observation, and nobody should quote these as "what a Delivery looks
like". A second instance, or this one once it carries a real Delivery, would be worth re-counting.

**What it decides.** The learning hypothesis asked whether the lines are decoration and the per-bar
indicator is the real feature. On this sample the answer is **no**: at a maximum out-degree of 2 and
one Feature in ten carrying any edge at all, the lines are drawable essentially always, and the
thicket does not materialise. So the density fallback is designed as cheap insurance against a
pathological Delivery, not as the expected presentation — see D5-9 in `feature-delta.md`.

**What it changes in the other direction.** One edge in four points out of view. The indicator for a
dependency that cannot be drawn is therefore ordinary traffic rather than an edge case, which is a far
stronger argument for giving it a proper presentation than AC-5.2 makes on its own. That is what
ADR-203 and the AC-5.2 amendment act on.

## Effort

~4h, contingent on slice 04's outcome.

## Dogfood moment

Same day: open the Delivery with the most dependencies and look for a bar that sits later than its
position in the order would suggest. Trace the line. If the answer is not visible in a few seconds, the
density fallback is the feature and the lines are not.
