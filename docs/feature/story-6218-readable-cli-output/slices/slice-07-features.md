# Slice 07 — Features as the Feature list, and their Work Items

**Story** #6218 / US-07 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~4h · **Sketch** `discuss/cli-sketches.md` §8

## Goal

`lh feature get` prints the Feature list's columns and `lh feature workitems` prints the Work Items
dialog's.

## IN scope

- `feature get --ids|--refs`: {Feature} Name (reference id then name) · Progress (`n of m {Work Items}`)
  · Forecasted Start · Forecasted Completion (85%) · State; `Cannot forecast` per `cannotForecast.ts`
  (D13).
- `feature workitems --id`: `{reference} {name} · n {Work Items}` over ID · Name · Type · State · Owned
  by; the Feature name from `getFeaturesByIds` under `--pretty` only; fallback heading `{Feature} [id:
  n] · …` (D4).

## OUT of scope

- Parent, Dependencies, Warnings columns. Any new Lighthouse field.

## Learning hypothesis

**Disproves that a Feature read by id carries per-Feature progress and forecasts as the web list shows
them** — if the progress needs per-Team summing the web does client-side, the CLI must decide whether
summing rows counts as re-deriving (System Constraint 8) and bring it back to the maintainer.

## Acceptance criteria

AC-07.1 … AC-07.4. Risk carriers: **AC-07.3** (no Feature-name read under `--json`/`--toon`),
**AC-07.4** (production data against the Portfolio's Feature list).

## Dependencies

Slice 01.

## Reference class

Slice 05.

## Pre-slice SPIKE

None.
