# Story 6564 — Notes on reports

**Finished:** 2026-11-27 · ADO User Story #6564 "Add notes to reports" · no parent Epic

## The problem

A report freezes numbers, but not the reasons they moved. Whoever opens a Then & Now report months later sees that
Cycle Time dropped and cannot tell that the Team grew from 6 to 9 people or spent two weeks in training. That context
lived in people's heads, in slide notes or nowhere.

## What changed

- **Notes on every report.** A short, dated text on one report, at most 2000 characters, built like the Delivery notes.
  Each note shows the day it was written, its author's name when sign-in is on, and "edited <day>" once changed. The
  newest note comes first.
- **Who may do what.** Everyone who can read a report reads its notes. Adding takes Write on the Team or Portfolio. A
  signed note is changed or deleted only by its author; one written with sign-in off, by anyone with Write. With sign-in
  on and no profile, a write is refused. `NoteSignature` holds that rule for Delivery notes and report notes alike.
- **Where the notes sit.** Between the report's name and its numbers, in a row that starts closed and reads
  "Notes (<count>)", "Notes" while the notes load or cannot be loaded. The maintainer first chose an open area above the
  numbers (Option A), then asked on 2026-11-27 for it to start closed, so the numbers come first.
- **Notes go with their report**, also when a Team or Portfolio is deleted, and are never printed or put in a PDF.
- **Usage data:** the name-only event `ReportNoteAdded`, listed in `docs/settings/usagedata.md`.
- One additive table `ReportNotes` (cascade to `Reports`, set-null to the author's profile), migrated on SQLite and
  Postgres.

Commits: S1 `ec928023f` (add and read), S2 `8ad562553` (readers, print), S3 `29798fa97` and `31a8b7c21` (change and
delete), the collapsed notes `0c9ca5b5f`, and the refactor, oracle, review and mutation commits around them.

## Key decisions

- One kind of note, dated by the day it was written; no separate event date (maintainer, DISCUSS).
- Same change and delete rule as Delivery notes (maintainer, DISCUSS).
- Notes not in the CLI or MCP report reads for now (maintainer, DISCUSS).
- Report notes live in `Modules/Reports`, and the store is a port that `ReportRepository` implements.
- Last save wins on concurrent edits; a change to a note deleted meanwhile answers not found (`31a8b7c21`).
- Collapsed by default, at the same place, "Notes (0)" when empty (maintainer, 2026-11-27).

## Quality

- nwdes: three slices, each with an approved oracle and an observed RED, then a slice review; the whole-candidate review
  was accepted (`f3dfeda76`). EXAMINE closed indeterminate by maintainer decision (`afe603838`).
- The collapsed notes went the direct route with one Haiku review, whose test gaps were closed before the commit.
- The maintainer checked the Story by hand in the running app on 2026-11-27.
- Mutation: backend 86.4 % and frontend 91.1 %, both over the 80 % gate, after kill rounds that added wire, repository,
  service and component tests. Survivors are triaged in `docs/feature/story-6564-report-notes/mutation/results.md`.
- CI: the first push failed twice on the connection-defaults Slice06 scenario, which read the Task Manager after the
  updates had finished. Fixed in the test helper (`66604d44f`), and run 45954183788 went green.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done: `docs/reports/reports.md` gains "Notes" (reading, adding, changing and deleting), the PDF line and three rows in "Who Can Do What". |
| Screenshots | Done: `features/report_thenandnow.png` re-taken, it now shows the closed "Notes (0)" row. The other report shots show no open report, so they are unchanged. |
| Demo data | N/A, because notes are context people write themselves; the demo data adds no notes. |
| Website assets | Only `features/report_thenandnow.png` changed. Not checked whether the website hot-links it; if it does, it now shows the closed notes row, which is correct. |
| Clients (CLI, MCP) | N/A, because the maintainer chose "not now"; the report routes and DTOs the clients read are unchanged. |
| Context map, `ARCHITECTURE.md` | Done: ADR-270 gains the glossary term "Report note" and "their notes" in the Reports row; `ARCHITECTURE.md` §4's Reports row describes the notes. |
| Usage-data event | Done in S1: `ReportNoteAdded`, name only, listed in `docs/settings/usagedata.md`. |
| Mutation | Done: backend 86.4 %, frontend 91.1 %. |
| Release notes | N/A; written at /release. |

## Left open

- Delivery notes have the same gap as report notes once had for sign-in on with no profile: a write there is not refused.
  A possible Bug, not created.
- `ReportRepository.TrySave`'s concurrency branch has no test in the mutation filter (see the mutation results).
- A freer arrangement of a report's parts was mentioned by the maintainer as a possible later item.
