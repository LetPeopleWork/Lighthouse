# Slice 09 — Housekeeping commands read as sentences and tables

**Story** #6218 / US-09 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~3h · **Sketch** `discuss/cli-sketches.md` §9

## Goal

The last commands on the generic view — `blackout list`, `worktracking list|get`, `version get`,
`health check` — read as the web's settings pages, footer, or a plain sentence; KPI-1 reaches 0.

## IN scope

- `blackout list`: `Recurring blackout rules`, Schedule (server `summary`, with `[id: n]`) ·
  Description; `No recurring blackout rules.` when none.
- `worktracking list`: `{Work Tracking Systems}`, Name `[id: n]` · Type.
- `worktracking get`: name and id, `Type:`, Option · Value table with editor labels; every option
  marked secret reads `(secret, not shown)` whatever the server sent.
- `version get`: `Lighthouse v{version}`.
- `health check`: `Lighthouse at {url} is reachable.` / `The standalone Lighthouse is reachable.`;
  failure unchanged.
- The KPI-1 contract test: every group's subcommands enumerated, none on the generic view.

## OUT of scope

- `connection`, `config`, `help` (unchanged on purpose, D2).

## Learning hypothesis

**Disproves that `--pretty` can never print a secret** — the AC drives a server answer that wrongly
carries a secret's value and asserts it is still not shown. Confirms, with the contract test, that no
command was missed.

## Acceptance criteria

AC-09.1 … AC-09.5. Risk carrier: **AC-09.2**.

## Dependencies

Slice 01; the KPI-1 test needs slices 02–08 shipped to pass in full (it lists the expected renderer per
command, so it can land earlier with the later rows pending).

## Reference class

Slice 08 (one-line answers).

## Pre-slice SPIKE

None.
