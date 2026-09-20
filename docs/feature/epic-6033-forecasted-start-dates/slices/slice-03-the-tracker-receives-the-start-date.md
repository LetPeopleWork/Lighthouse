# Slice 03 — The work tracking system receives the forecasted start date

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-03 · **Estimate**: ~4h
**Reference class**: `WriteBackTriggerService.ResolveForecastValue` (`:254-292`) — percentile, working-day
projection over effective blackout days, `yyyy-MM-dd` or a custom `DateFormat`, `null` when Done. The
start resolver is that method with a different distribution and one extra branch.

## Goal

An administrator maps a forecasted start percentile onto a field, and the next refresh writes it — so a
Jira Plan bar gets both of its ends from measured flow instead of from somebody typing them.

This is the customer's literal request. Slices 01 and 02 exist so that what gets written is right.

## IN scope

- Four `WriteBackValueSource` members for the start percentiles, **appended after `SleRisk`**. The enum
  persists ordinals and says so in an in-place comment; inserting anywhere above silently re-points every
  existing mapping on every database.
- A regression test asserting the ordinal of every pre-existing member, so the next person to add a
  source is stopped by a red test rather than by having read the comment.
- The start resolver: observed start date when the Feature has started (D5), forecast percentile when it
  has not, nothing when it is Done or has no forecast.
- The value source appearing in the mapping screen's list, gated by the premium check that screen
  already applies. No new gate (D8).
- `WriteBackTriggerService`'s trigger list extended so a start mapping actually fires on refresh — the
  existing four are enumerated there as well as in the validator, and both lists are easy to miss.

## OUT of scope

- Reading a start date back from the tracker. One-directional, as completion is.
- Any connector-specific work. The resolver is connector-agnostic and both Jira and Azure DevOps get
  this by existing (D15).
- Any new field type. Date and formatted text, exactly as completion supports.
- Jira Plans configuration. What a customer does with a populated field is theirs.

## Learning hypothesis

**Disproves, if it fails**: that write-back is the cheap half — the claim on which this Epic was scoped
as four slices rather than two Epics.

The specific risks:

1. **The enum's ordinal contract is load-bearing in more places than the enum.** `WriteBackMappingValidator`
   and `WriteBackTriggerService` each carry their own hardcoded list of which sources are forecast
   sources. A third list somewhere unfound means a start mapping validates and never fires, or fires and
   never validates. The regression test in scope covers the ordinals; it does not cover a missed list.
2. **A Plan bar needs both ends to agree.** If a customer maps start P85 and completion P50, the bar is
   backwards. Nothing in the mapping screen prevents that pairing and this slice does not add a
   constraint — but it is worth knowing whether it is reachable by accident before deciding it is fine.

**Confirms, if it succeeds**: the Epic's title is satisfied and Chris has what he asked for, with three
slices' worth of evidence that the number being written is the right one.

## Acceptance criteria

AC-3.1 through AC-3.6 in `feature-delta.md`.

## Dependencies

Slice 01 for the distribution. Slice 02 is not a hard dependency but is deliberately sequenced first
(see Prioritisation): a wrong date discovered in our own column costs a re-run, and the same date
discovered in a customer's Jira instance costs a published Plan built on it.

## Pre-slice SPIKE

**No.** But **before writing the first test**, grep for every place `ForecastPercentile50` is enumerated
and list them in this brief. Two are known (`WriteBackMappingValidator:9-12`,
`WriteBackTriggerService:21-24`, plus the `GetPercentileFromSource` switch at `:282-292`). Risk 1 is
entirely about whether that list is complete, and it costs one search to close.

Record the result here:

> **Run across `Lighthouse.Backend/`, `Lighthouse.Frontend/src/` and `Lighthouse.EndToEndTests/`. The list
> was complete — there is no third list.** Backend: `WriteBackMappingValidator`'s set,
> `WriteBackTriggerService`'s set, and the `GetPercentileFromSource` switch, all three as the brief
> predicted. Frontend: three more that the brief did not predict, because it only looked at the backend —
> `FORECAST_SOURCES`, `PORTFOLIO_ONLY_SOURCES` and `VALUE_SOURCE_DISPLAY_NAMES` in
> `models/WorkTracking/WriteBackMappingDefinition.ts`. Nothing in the E2E suite enumerates them.
>
> Risk 1 is therefore closed, and it was closed twice: the two backend sets are now one shared membership
> beside the enum, so the "third list" failure mode cannot recur there. The three frontend lists remain
> separate by necessity — a different codebase — and the cross-stack contract they depend on is not the
> ordinals but the member *names*, which is what the wire carries. That contract had no test on either
> side; saving now refuses rather than silently rewriting a mapping it cannot name.
>
> Recorded after implementation rather than before, which is the discipline this section exists to
> enforce and did not get. The answer is the same either way, but the next reader could not have told
> "checked, clean" from "skipped" — which is the whole reason the line is here.

**Risk 2 — a backwards bar — is reachable, and deliberately not prevented.** Start and completion come
from two independent distributions, both projected from today, with no cross-mapping validation anywhere:
mapping start P95 against completion P50 produces a bar that ends before it begins, and even the same
percentile on both ends carries no guarantee that start precedes completion, because they are separate
reads of separate histograms. Both values validate and both write. Left as is for now — constraining the
pairing is a product decision, not a defect — but it is now written down rather than unexamined.

## Effort

~4h. Enum and ordinals ~30m, resolver ~1h, the two-or-three lists ~30m, mapping screen ~30m, tests ~1.5h.

## Dogfood moment

Same day: map Forecasted Start P85 to a date field on the Lighthouse ADO connection, refresh, and open
the work item. The date in the field is the whole demo. A second look at the same field a day later —
after throughput has moved — is what shows it is a forecast and not a stamp.
