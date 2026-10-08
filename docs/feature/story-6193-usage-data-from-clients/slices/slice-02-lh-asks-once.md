# Slice 02 — `lh` asks once, and the answer can be checked and changed

**Story** #6193 / US-02, US-03 · **Job** `job-user-decide-once-whether-lighthouse-may-learn-from-me` ·
**Repo** `lighthouse-clients` (`client`, `cli`) + Lighthouse docs · **Estimate** ~7h

## Goal

The first time a person uses `lh` against a Lighthouse in a terminal, they are asked once in the approved
words; their answer is kept for that Lighthouse; `lh config usage-data [on|off]` shows and changes it; with a
yes, `lh forecast manual` reports `TeamManualForecastRun` with `source: Cli`.

## IN scope

- The per-Lighthouse usage-data store beside the voter keys (D4), shared later by MCP stdio.
- The question (M1 copy verbatim) under D5's conditions; `DO_NOT_TRACK` (D14); the D3 check.
- Grant on yes; nothing posted on No; revoke on `off`; re-grant after a lapse (D6).
- `lh config usage-data`, `on`, `off` (M2; Journey copy, pinned in DISTILL).
- The reporter in `client` and the first event, `TeamManualForecastRun`, after success.
- Docs: `docs/settings/usagedata.md` rewritten where it says "browser" (D13); `docs/aiintegration.md`
  paragraph; `packages/cli/README.md`; `skill/SKILL.md` rule (D15); clients `ARCHITECTURE.md` §5.
- Website grep (checklist row).

## OUT of scope

- The other nine `lh` events (slice 03). MCP (slices 04, 05).

## Learning hypothesis

**Disproves that a one-time `[y/N]` after the output is acceptable** — if dogfooding shows the question
landing in the middle of piped output, or a second question for the same Lighthouse, D5's placement is wrong.

## Acceptance criteria

AC-02.1 … AC-02.7, AC-03.1 … AC-03.5. Risk carrier: **AC-02.7**, a real forecast on the dev instance
arriving with `source: Cli`.

## Dependencies

Slice 01 (the D3 signal).

## Reference class

Story #6156 — the voter-key store (`voterKeyStore.ts`) and `lh config voter`.

## Pre-slice SPIKE

None.
