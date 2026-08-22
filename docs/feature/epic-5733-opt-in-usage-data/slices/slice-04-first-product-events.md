# Slice 04 — A handful of named events, and the first KPI that stops saying "deferred"

## Goal

The maintainer can answer "did anyone actually use this" for a shipped feature, and at least three
of the seven KPIs blocked on this Epic move to a live source.

## IN scope

- 2–4 named product events beyond the heartbeat, **chosen with the product owner at slice start**
  from the deferred-KPI list (S5) and from what is in flight at the time. Written down before any is
  instrumented (AC-08.1).
- Every event enumerated in `docs/settings/usagedata.md` and in the consent dialog **before** it is
  emitted.
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
  narrower one. **A legal question, flagged at DoR-9, answered before this slice ships.** If the
  answer is yes, this slice grows a re-consent path and the estimate changes.

## Production-data acceptance

The events are counted at the collector from real consenting instances. The vendor's own instance is
excluded from any adoption ratio.

## Dogfood moment

Same day: exercise each chosen event on the vendor instance and confirm each arrives with the exact
documented field set and nothing more.

## Dependencies

- Slice 03 shipped.
- **AC-08.5's legal answer.** This is the one dependency that can change the slice's shape rather
  than just its start date.
- Slice 02's uptake number, as an input to whether the event set is worth spending on at all.

## Effort

≤ 1 day, *conditional on AC-08.5 answering "no re-consent needed"*. If re-consent is required, split:
the re-consent path becomes its own slice ahead of this one.

## Reference class

Slice 01's heartbeat. Same pipe, same gate, same purity invariant — the marginal event should be
cheap. If it is not, the emitter was built wrong in slice 01 and that is worth knowing.

## Watch

This is where scope creep lives. Every deferred KPI is a temptation and there are seven of them. The
2–4 limit is the whole discipline of the slice; a fifth event is a decision to reopen, not a small
addition.
