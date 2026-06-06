# Slice 02 — "Every 4th Friday" interval cadence with a bounded end date

**Story:** US-02 · **Surface:** recurring-rules create (same endpoint) · **Direction:** interval + bounded recurrence → concrete blackout days

## Goal
One sentence: let a config-admin create a rule that repeats on an interval ("every 4 weeks, Friday, start 2026-06-12, end 2026-12-31") so exactly the off-site Fridays — and no others — become non-working days within the bounded window.

## IN scope
- Honour the **interval (every X weeks)** field in recurrence expansion: only weeks that are an integer multiple of X from the start week match (anchor = the rule's start date's week).
- Honour the **concrete end date**: no days beyond `end` match; days before `start` never match.
- The second epic use case proven end-to-end: "team off-site on Friday every 4 weeks this year".

## OUT scope
- Open-ended weekly path (covered by Slice 01).
- Edit / delete / validation error paths → Slice 04.
- Feature/delivery/write-back/chart fan-out → Slice 03.

## Learning hypothesis
**Disproves** "the every-X-weeks interval anchors correctly on the rule's start week so exactly the intended Fridays match (not every Friday, not off-by-one weeks)" **if** the expansion marks the wrong Fridays, marks days outside [start, end], or the interval is ignored.
**Confirms** both epic use cases (weekends-forever AND bounded interval off-site) are expressible with the same entity.

## Acceptance criteria
- US-02 AC1: a rule (Fri, every 4 weeks, start 2026-06-12, end 2026-12-31) marks 2026-06-12, 2026-07-10, 2026-08-07, … as blackout days and marks NO other Fridays (e.g. 2026-06-19 is a working day).
- US-02 AC2: a date before `start` (2026-06-05) and a date after `end` (2027-01-08) are NOT blackout days for this rule.
- US-02 AC3: a "When" forecast whose window spans one off-site Friday steps that single Friday over (+1 working day) and lands on a working day.
- US-02 AC4: interval defaults are honoured — "every 1 week" with weekdays = the Slice-01 weekly behaviour (no regression).
- Production-data: a real team with an off-site every 4th Friday for the rest of the year.

## Dependencies
Slice 01 (entity + endpoint + expansion seam). No new endpoint.

## Effort / reference class
~0.5–1 day. Reference class: adding interval/bound logic to an existing expansion function + a focused integration test on the existing endpoint.

## Slice value
Observable: admin enters an interval + end date → only the intended off-site Fridays show as non-working days and a forecast steps over exactly one of them. Completes the epic's second named use case.
