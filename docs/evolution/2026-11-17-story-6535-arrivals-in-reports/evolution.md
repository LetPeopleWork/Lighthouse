# Story 6535 — Arrivals in Then & Now reports

Delivered 2026-11-17, nwdes feature `story-6535-arrivals-in-reports`, one slice, rigor `standard` (Sonnet crafter and
acceptance designer, Haiku reviewers; EXAMINE re-run on Sonnet by the maintainer's choice).

## The problem

A Then & Now report showed how fast work went out ({Throughput}), not how fast it came in. A Team that finishes more
than before can still be drowning if even more work starts. The Metrics tab already shows Arrivals; the report could
not freeze it, so "did we stop starting more than we finish?" had no before-and-after answer.

## What changed

- **A fifth report metric, Arrivals.** For each window it freezes the Work Items started: the total, per day, the
  {Throughput} per day beside it, and the gap between the two. Team and Portfolio. The gap is computed once, in the
  backend metric; Then is frozen at creation like every other metric and never rebuilt.
- **The Arrivals panel** reads the gap in words: "0.30 more started than finished per day", "0.10 fewer started than
  finished per day", or "as many started as finished per day" when it rounds to zero. The change on the gap carries no
  percentage (the gap can cross zero) and lower is better. An ⓘ ("About Arrivals") explains the gap in one sentence.
  Nothing finished is a real 0.00; a dash appears only with "Not captured for this report". The print document shows
  the same words.
- **The wizard** offers Arrivals last, under its fixed name. A report made before Arrivals existed shows it disabled and
  unticked with "Not captured for this report", following the existing rule for metrics added later.
- Small shared changes: the panel frame takes an optional info control (other panels render exactly as before), the
  change line can leave out its percentage, and Throughput and Arrivals take their per-day average from one helper.

## Key commits

| Commit | What |
|---|---|
| `45e83357e` | Examiner expectations E1–E6 |
| `0e43e0b70`, `e41be3fb2` | Oracle, then its review fixes (real started and finished counts, info text, old-report checkbox) |
| `4ffa83f08`, `420023b90` | GREEN: metric, panel, wizard; the report scenarios that pinned four metrics now expect a fifth |
| `9e212d749`, `e961c52dd`, `40ed685a5` | Whole-candidate review fixes: print checks, untouched titles of other panels, shared decimals, one rounding, one per-day helper |
| `1a3219b3d` | Test titles quote the text a reader sees, so EXAMINE can observe it |
| `1239446a0` | Mutation survivors killed |

## Gates

- Whole-candidate review (Haiku): accepted after one revision round, no open findings. Its published record carries the
  last observation only; the earlier ones are in `runs/review.candidate.json` as of `9e212d749`.
- EXAMINE: accepted, E1–E6 met, on the public oracle rows (screen and print) and the backend scenarios (Team and
  Portfolio). Stated limit: no live browser and no real PDF file. The first examiner (Haiku) would not count test output
  as an observation; the maintainer chose a Sonnet re-run, as for Story 6655.
- Mutation: backend 72.7 % → **100 %** (22 mutants), frontend 78.9 % → **96.7 %** (90 mutants), see
  `docs/feature/story-6535-arrivals-in-reports/mutation/results.md`. The port scenarios live in `API.Integration` and are
  named in the backend filter ahead of its exclusion.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done** — `docs/reports/thenandnow.md`: the five metrics in the wizard, an Arrivals row in the panels table, how to read the gap, and why its change has no percentage. |
| Screenshots | **Done** — `report_thenandnow.png` and `reports.png` regenerated with `--grep` on their one test ("Take @screenshot of the Reports tab and a Then & Now report"). The summary now reads "Improved: Throughput, Arrivals"; the Arrivals panel itself lies below the captured viewport, and the docs describe it in the panels table. |
| Demo data | **N/A**, because the demo Teams and Portfolios already start and finish Work Items every day, so a new report shows a real gap. |
| Website assets | **Done** — the two regenerated images under `docs/assets/features/`; the website hot-links assets from `main`, so any page showing them updates once this is pushed. No website copy lists the report metrics by name. |
| Clients (CLI, MCP) | **No code or release**, because the clients pass a report's raw values through and hold no list of metric keys (ADR-247). **Deferred to the maintainer:** the `lighthouse-reports` skill in the clients repository should learn the gap wording. |
| Context map, `ARCHITECTURE.md` | **N/A**, because Arrivals is one more entry in the Reports metric catalog; no context, edge, event or glossary term changes. |
| Usage-data event | **N/A by maintainer decision** (2026-11-17): the report type already tells enough. |
| Mutation | **Done** — see Gates. |
| Release notes | N/A; written at /release. |

## Left open, knowingly

- The `lighthouse-reports` skill prose in the clients repository (owner: maintainer).
- The port scenarios for reports still live under `API.Integration/Reports` rather than `Acceptance/`, like the report
  scenarios before them; moving them belongs to the test-layering improvement work.
