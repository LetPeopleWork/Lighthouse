# Slice 01 — The forecast reads like the Forecast tab

**Story** #6218 / US-01 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` (`cli`, `client`) · **Estimate** ~6h · **Sketch** `discuss/cli-sketches.md` §1

## Goal

`lh forecast manual` and `lh forecast backtest` answer in the words, order and levels of the web's
Forecast tab and Backtest Results, and the three pieces every later slice reuses land with them.

## IN scope

- **Manual forecast renderer**: heading (Team name · Work Items · target), `When will n {Work Items} be
  done?` and `How Many {Work Items} will you get done till …?` tables (Chance · Level · Date / Work
  Items), highest chance first, levels by the web's 50/70/85 thresholds; the likelihood line with the
  `>95%` cap, `Cannot forecast`, and the insufficient-data sentence (`ManualForecaster.tsx`,
  `ForecastInfoList.tsx`, `ForecastLevel.ts`, `ForecastLikelihood.tsx`, `formatLikelihood.ts`).
- **Backtest renderer**: `Backtest Results`, the `Period:` line, the percentile table in 50/70/85/95
  order, the actual as `── Actual Throughput: n {Work Items} ──` between the rows it falls between.
- **Shared, landed here** (D3): the Terminology resolver widened to every configurable key, keeping
  `refinementWording.ts`'s exports and behaviour; `toTableLines` moved out of `refinementOutput.ts`
  unchanged; date and timestamp formatting (D15).
- `--pretty`-only reads: `getTeam` (name) and `getTerminology` (D4).
- Changeset: minor `lighthouse-cli`, minor `lighthouse-client` (D16).
- The website grep for pasted `lh` output (checklist row): a hit list or "no hit".

## OUT of scope

- The backtest `Average:` (D9). MCP (D17). Any other command.

## Learning hypothesis

**Disproves that the web's forecast display rules can be stated from the facts on the wire** if the
cap, the cannot-forecast case or the thin-history case needs anything the manual forecast answer does
not carry (`likelihood`, `hasSufficientData`, remaining Work Items). If so, the rules belong on the
server, and every later "single answer" slice is in doubt — cheapest to learn here.

Confirms, if it succeeds, that the three shared pieces carry a real renderer, so slices 02–09 add one
wording module and one renderer each.

## Acceptance criteria

AC-01.1 … AC-01.7 (`feature-delta.md`, US-01). The ones carrying the risk:

- **AC-01.2** — the cap, cannot-forecast and insufficient-data rules, each as a scenario.
- **AC-01.6** — `--json`/`--toon` unchanged in bytes and calls.
- **AC-01.7** — production data: a real Team on the dev instance, side by side with the web tab.
- **DoD 4** — `lh refinement get` output unchanged by a byte after the move.

## Dependencies

None. The renderer seam (`PrettyRenderer`) is in `main` (story #6147).

## Reference class

Story #6147 clients slice 09c-02 — the same seam, the same heading style, a Team-name read and a
Terminology fallback, ~1 day including the seam itself.

## Pre-slice SPIKE

None. The one unknown — the manual forecast answer's exact field names (S4) — is a read of the
backend DTO at DESIGN.
