# Slice 04 — Say the true thing when there is still nothing to show

**Story**: US-04 | **ADO**: 6053 | **Estimate**: ~0.5 day | **Reference class**: ADR-108 slice-03b's range-end empty-state predicate

## Goal

A coach opening an over-time widget that is still empty reads copy naming the actual reason, and can tell
"still filling in" from "your history does not reach back that far" from "nothing was ever recorded".

## Why this is last, not first

The set of reachable empty states is not knowable until reconstruction exists. Writing the copy before slices
01–03 would be guessing at which states survive — and the current copy is a live example of exactly that
failure mode, having been written when forward-only was the only possibility.

## IN scope

- Revised empty-state copy for both over-time widgets, covering the states that actually remain after slices
  01–03.
- Revisiting ADR-108 slice-03b's range-end predicate against the new state set.
- `docs/metrics/predictability.md`: the forward-only note and the "demo ships backdated Throughput only" note
  both change meaning under this story.
- Regenerating any `@screenshot` shot in which the empty-state copy is visible.

## OUT of scope

- Marking reconstructed points (D6).
- The ADR-108 / ADR-109 amendments (written at finalization).
- Closing ADR-108's known defect — the owner whose snapshots all predate a window that still ends today, which
  reads the forward-only copy falsely. Reconstruction makes it **rarer**, not gone (an owner past its
  `UpdateTime` floor still hits it). It stays the tracked follow-up ADR-108 already names.

## The copy problem, concretely

`"builds forward from today — no snapshots recorded yet"` (Epic 5427 D6) was true when the series could only
grow forwards. After this story it is false in two new ways:

| State | Today's copy says | Truth after this story |
|---|---|---|
| Fresh owner, reconstruction enqueued, not finished | "no snapshots recorded yet" | data is on its way; come back |
| Range entirely before the data floor | "builds forward from today" | this period is not reconstructible — no items that far back |
| Owner past its `UpdateTime` floor, empty range | "no snapshots recorded yet" | this owner stopped refreshing; nothing will fill |

Three distinguishable states, one message. The widget already knows the range it asked for (the slice-03b
discriminator); what it does not yet know is the data floor or whether a reconstruction is pending — so whether
those become response fields is a DESIGN call, constrained by ADR-108's standing rejection of a discriminated
envelope.

## Learning hypothesis

**Disproves if it fails**: "the widget can distinguish these states from what it already holds." If it cannot,
the honest options are a response field (reopening ADR-108's envelope rejection) or accepting one vaguer
message that is true of all three — and the second is preferable to a precise message that is sometimes false.

**Confirms if it succeeds**: empty-state honesty stays a client-side property, as D10 and ADR-108 both intended.

## Acceptance criteria

1. An owner with no stored items and no snapshots reads copy that does not promise a superseded forward-only
   fill.
2. A range entirely before the data floor reads copy naming that, distinct from "nothing recorded yet".
3. A range whose reconstruction is enqueued but unfinished is distinguishable from one with nothing to
   reconstruct — or, if it cannot be, the single message used is true of both and a comment says why.
4. The slice-03b range-end predicate is re-evaluated against the new state set, and either kept with a stated
   reason or replaced.
5. `docs/metrics/predictability.md` updated; the two **Affected by Filtering** rows and the forward-only note
   re-checked against actual behaviour.
6. Frontend `pnpm test` green; `pnpm build` zero errors and zero warnings.
7. Any screenshot showing the old copy is regenerated (`rm` the PNG first — the regen keeps the old file when
   the pixel diff is under threshold).

## Dependencies

Slices 01–03, which determine which empty states remain reachable.

## Dogfood moment

Same day: on the restored dev DB, create a fresh team with no items and read its widget; then select a range
before the data floor on team 1 and read that.
