# Slice 02 — The dashboard reads Lighthouse's answer

**Goal:** Every Stale signal in the UI comes from the server's fields; `deriveStaleness` is deleted.

**Stories:** US-02

## IN
- Aging chart stale dots, Time-in-State badge + tooltip reasons, Work Items dialog column, Flow Overview Stale count/RAG and aging-pace data read `isStale` / reasons.
- Team and Portfolio views.
- Delete `utils/staleness/deriveStaleness.ts` and its tests; threshold props no longer threaded for staleness.

## OUT
- Any new visual look — the badge, tooltip and widget look as today.
- The thresholds settings form.

## Learning hypothesis
Disproves "the UI needs nothing but the WIP read" if a surface showing the badge gets its Work Items from a read that does not carry the flag.

## Acceptance criteria
US-02 AC 1–6 (`feature-delta.md`).

## Dependencies
Slice 01.

## Effort
~1 day.

## Dogfood
Demo data Team, current and a past range: Stale count equals the WIP read's stale count.
