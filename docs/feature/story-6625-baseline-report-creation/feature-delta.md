# Story 6625 — make creating a Then / Now report intuitive

DISCUSS and DESIGN are in `brief.md` and `architecture.md` (nwdes renders them from `handoff.json`). This file holds
what those do not: the DEVOPS answer and the maintainer decisions taken at the start of DELIVER.

## DEVOPS

No platform risk: no migration, no new external call, no new setting, no job; the Then step fetches nothing new.

**Usage data: N/A.** Report creation is already counted as *A report was created* (`ReportCreated`, by report
type). The feature removes the Now-length question, so nothing is left to count, and the report type is enough
to see Then & Now reports being made.

## Start of DELIVER: maintainer decisions (2026-11-17, sketch review)

| # | Decision |
|---|---|
| 1 | **Sketch approved as sketched.** Create wizard: Type → Then → Metrics & name (the Now step is gone). The Then step reuses `ThenStep` and `WindowLengthControl` and shows both windows by date: `Then 17 Sep – 16 Oct 2026 (30 days)` and `Now 17 Oct – 15 Nov 2026 (30 days, ends today)`, plus the same-days line (P3) or the overlap line (P4). Bulk Create loses "Same as Then / Custom". Edit shows Then read-only with the frozen note, as today, and asks no Now question. |
| 2 | **Throughput Total change** reads like P12: `▲ 13 (+25 %)` beside `Total 65 Work Items`, marked better; the per-day line keeps `▲ 0.43 per day (+25 %)`. Both 0: `No change (both 0)` on both lines; equal: grey dash `No change`; Then 0: `▲ 30 (Then was 0)`. |
| 3 | **Too little kept history (P5)**, an error line with Next disabled, naming the owner, the length and the way out: `{Team name} keeps history from 18 Sep 2026: too little for a 30-day Then and Now. Pick a shorter length or a later end date.` |
| 4 | **Usage data N/A**, as above. |
| 5 | **Rigor:** S1 `thorough` (the window rule and the API refusal: Opus crafter and acceptance designer, Sonnet reviewers); S2 and S3 `standard` (Sonnet; Haiku reviewers). |

Loading and failure looks are unchanged (see DESIGN): the existing templates query's lone spinner with Next disabled, and
its could-not-load alert.
