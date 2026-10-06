# Slice 06 — Deliveries read like the Delivery cards

**Story** #6218 / US-06 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~5h · **Sketch** `discuss/cli-sketches.md` §7

## Goal

`lh delivery list` prints one row per Delivery with the web header chip's answer, and `lh delivery
metrics` prints one row per recorded day.

## IN scope

- `{Portfolio name} · {Deliveries}` over Name · {Delivery} Date · {Features} · Done (`n of m {Work
  Items}`) · Likelihood · Forecast 85%.
- Likelihood: the web's exclusive answers in order — `Cannot forecast`, `Not enough data`, the number
  (`>95%` cap) — and `Overdue` only when the server says `isOverdue` (D5).
- `delivery metrics`: heading `{Delivery} [id: n] · {Delivery} Date … · recorded since …`, Date · Done
  · Remaining · Total · {Features} · Likelihood.
- `--detail epics`: the latest day's per-{Feature} table (Done · Likelihood · Size, default size marked)
  and its four chances (D14).
- `--pretty`-only Portfolio read for the list heading's name.

## OUT of scope

- Every day's per-Feature breakdown under `--pretty` (D14). Renaming the `epics` flag value.

## Learning hypothesis

**Disproves that the Delivery list answer carries everything the web's header chip decides on**
(`likelihoodPercentage`, `hasSufficientData`, `teamsWithoutForecast`, `isOverdue`, `completionDates`).
If the chip's answer needs a field the list does not send, the CLI must say less than the web — and the
sketch changes before DELIVER finishes.

## Acceptance criteria

AC-06.1 … AC-06.5. Risk carriers: **AC-06.1** (the exclusive-answer order), **AC-06.5** (production
data against the web cards).

## Dependencies

Slice 01.

## Reference class

Slice 01 (the cap and the exclusive answers are the same rules).

## Pre-slice SPIKE

None.
