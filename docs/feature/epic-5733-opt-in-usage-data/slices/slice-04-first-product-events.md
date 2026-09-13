# Slice 04 — A handful of named events, and the first KPI that stops saying "deferred"

> **SCOPE NARROWED, POSITION UNCHANGED — 2026-09-12.**
>
> **Position.** DESIGN proposed moving this slice up, ahead of slice 02, on the grounds that with the
> heartbeat abandoned the named events are what the Epic delivers. **The maintainer kept the original
> order**: 01c → 02 → 03 → 04. The importance framing stands — #5837 is the point of the Epic — but the
> sequencing does not change, and the original reasoning is the reason: the event vocabulary is worth
> choosing once there is a consenting population to spend it on, and slice 02 is what creates one.
>
> **Scope.** Slice 01c now ships **one real product event** (a Team or Portfolio detail tab was
> opened), not a bare page view and not nothing. So this slice is **the remaining named events**, not
> the first ones. Concretely: the 2–4 target becomes **1–3 more**, and the first slot is spent.
>
> **What that leaves this slice doing, which is still the harder half.** 01c proved the pipe with an
> event chosen for what it *exercises* — it is a navigation event, and it tells you a view was opened,
> not that anything in it was used. This slice is where that limitation is fixed and the vocabulary
> becomes about capability use rather than page traffic. The marginal event is now cheap — an enum
> member, a detector call site and a docs line — so the whole cost of this slice is the *choosing*,
> which is where it always was.
>
> **One discipline is now structural rather than a habit.** The closed `UsageDataEventName` enum means
> AC-08.2 is enforced by the compiler for our own client and by a `400` for anyone else's: an
> undocumented event cannot be emitted, rather than merely should not be.

## Goal

The maintainer can answer "did anyone actually use this" for a shipped feature.

> **The second half of this goal was dropped, 2026-09-13, after the events were chosen.** It read
> "and at least three of the seven KPIs blocked on this Epic move to a live source" — and none of
> them does. The brief allowed the set to be chosen either from the deferred-KPI list or from what
> is in flight; the maintainer chose the second, and the two do not overlap. Of the five deferred
> outcomes (not seven — that figure was corrected on 2026-09-12), two want an event nobody has
> built, two want customer feedback no event can ever supply and should stop being counted as
> telemetry-blocked at all, and the OAuth adoption ratio is a near miss: the connection event now
> says which *kind* of system was set up, and what it still lacks is which *authentication method*.
>
> Three outcomes were added instead for what this slice does answer —
> `OUT-usagedata-capability-use`, `OUT-usagedata-connector-mix`, `OUT-usagedata-manual-refresh-rate`.
> Each deferred outcome now names the event it is waiting for rather than waiting on the feature as
> a whole, which is the more useful state to leave them in: the next person to add an event can see
> at a glance which question it would close.

## IN scope

- **1–3 further named product events**, on top of the one slice 01c already ships, for a total of 2–4
  across the Epic. **Chosen with the product owner at slice start** from the deferred-KPI list (S5 —
  five, not seven; the row was corrected on 2026-09-12) and from what is in flight at the time, and
  now also informed by **slice 02's uptake number**, which exists by this point. Written down before
  any is instrumented (AC-08.1), which here means added to the `UsageDataEventName` enum and to
  `docs/settings/usagedata.md` in the same commit.
- **At least one of them must be a capability-use event rather than a navigation one.** 01c's event
  says a view was opened; it does not say anything in it was used. Fixing that is the substance of
  this slice, not a nice-to-have.
- Every event enumerated in `docs/settings/usagedata.md` **before** it is emitted. **Not in the
  dialog** — the shipped `UsageDataDialog` deliberately carries no field list, because a list in a
  dialog goes stale silently while still looking authoritative; it links the page instead. That makes
  the page the only place the list exists, and the CI docs comparison the only thing enforcing it.
- A payload-purity invariant asserted in CI over the whole event set (AC-08.3).
- KPI contracts updated: the affected outcomes move off
  `status: deferred-pending-telemetry-feature` and name the events that now source them.
- The `kpi-contracts.yaml` preamble amended — it currently states as fact that no phone-home exists.

## OUT of scope

- Instrumenting everything. The Epic's own words: "Resist the urge to instrument everything"
  (inherited from #5015). Two to four events, chosen deliberately.
- The OAuth adoption events specifically. The product owner has said these are not obviously the
  most important thing now; the set is chosen at slice start against what is actually in flight, not
  against a list written in 2026-05.
- Any event carrying customer content. Not a scope boundary — an invariant (AC-08.3).

## Learning hypothesis

**Disproves "the event vocabulary is the right one" if** the first real product question asked after
this ships cannot be answered from the events chosen. That is the test, and it is deliberately not
answerable in advance — which is why this slice is last, after slice 02 has shown how large the
consenting population actually is.

**Disproves "a small event set is enough" if** answering the first question requires a fifth event
within a fortnight. That would mean the selection method, not the events, needs rethinking.

## Acceptance criteria

Per US-08 (AC-08.1…8.6) in `feature-delta.md`. The two that carry it:

- **AC-08.2** — an event that ships ahead of its documentation is a defect. The Epic's transparency
  non-negotiable is "every event enumerated in user-facing docs; no weasel-words", and this is where
  that is either honoured or quietly abandoned.
- **AC-08.5** — whether a widened payload requires re-consent from browsers that consented to the
  narrower one. ~~A legal question, flagged at DoR-9, answered before this slice ships.~~
  **Answered 2026-09-12: no, within a stated boundary.** Re-consent is required only if the payload
  gains something person-scoped or free-text, the purpose widens, the set of parties holding the data
  widens, or retention lengthens. Adding an event inside those bounds updates the dialog's linked page
  and the existing consent stands. **The redesign narrows two of those four further**: the payload
  *cannot* gain free text, because the ingest DTO has no free `string` property; and a paid-plan
  upgrade is the one that would lengthen retention, which is why the free-tier recheck below is a
  privacy dependency rather than a cost one.

## Production-data acceptance

The events are counted at the collector from real consenting instances. The vendor's own instance is
excluded from any adoption ratio.

## Dogfood moment

Same day: exercise each chosen event on the vendor instance and confirm each arrives with the exact
documented field set and nothing more.

## Dependencies

- **Slice 03 shipped**, and with it slices 01c and 02. The original dependency, unchanged — DESIGN
  briefly proposed moving this slice ahead of 02 and the maintainer kept the original order.
- ~~AC-08.5's legal answer.~~ **Closed 2026-09-12** — see above. It can no longer change the slice's
  shape.
- **Slice 02's uptake number**, as an input to whether the event set is worth spending on at all.
  Restored: it was briefly dropped when this slice was proposed for reordering, and it is the main
  thing the original order exists to provide.
- **The PostHog free-tier recheck from slice 01c must have been done**, because this is the slice that
  multiplies the volume.

## Effort

≤ 1 day, *conditional on AC-08.5 answering "no re-consent needed"*. If re-consent is required, split:
the re-consent path becomes its own slice ahead of this one.

## Reference class

Slice 01c's tab-open event. Same pipe, same gate, same purity invariant — the marginal event should be
an enum member, a detector call site and a docs line. If it is not, the pipe was built wrong in 01c
and that is worth knowing.

## Watch

This is where scope creep lives. Every deferred KPI is a temptation and there are **five** of them —
the "seven" this slice used to cite was wrong, corrected on 2026-09-12. The Epic-wide 2–4 limit is the
whole discipline of the slice, and **01c has already spent one of those slots**, so the budget here is
1–3. A fifth event across the Epic is a decision to reopen, not a small addition.
