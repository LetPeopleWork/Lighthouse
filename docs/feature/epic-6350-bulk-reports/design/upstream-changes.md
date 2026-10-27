# Upstream changes — DESIGN → DISCUSS — epic-6350-bulk-reports

Raised by DESIGN on 2026-10-27. **APPLIED 2026-10-27**, after the maintainer answered MQ-1 to MQ-5 with the
recommendation each time (MQ-1 a, MQ-2 N = 50, MQ-3 a, MQ-4 a, MQ-5 a): U-1, U-2 (metric half dropped), U-4 (N = 50
also per run) and U-6 edited into the DISCUSS sections of `../feature-delta.md`, the slice briefs and the journey
yaml; U-3, U-5 and U-8 hold as written under the answers (no edit needed beyond the revision note); U-7 is a DISTILL
note.

| # | Where | Was | Proposed | Why | Depends on |
|---|---|---|---|---|---|
| U-1 | US-02a AC-2.5, US-03 AC-3.4 and example (5) | A result row "Not created: {Team} Zenith does not capture {Work Item Age}" (`metric-not-captured`) | Drop the row from the ACs. The server never answers `metric-not-captured` on creation (it is an edit refusal); a metric outside an owner's kind is refused as `unknown-metric`, worded "Not created: {metric} does not apply to a {Portfolio}" as a safety net, and today no metric is limited to one kind, so no run can produce it | Capture is per owner kind (`ThenAndNowTemplate.Freeze` captures every metric of the kind) | — |
| U-2 | US-04 AC-4.2, the scenario "An owner that does not capture a chosen metric cannot be picked", example (5), D13's metric extension (Q7) | Owners not capturing a chosen metric shown but not pickable | (MQ-3 a) remove; slice 04 ≈ ¾d. (MQ-3 b) restate per owner kind | Same as U-1 | MQ-3 |
| U-3 | US-02b AC-2b.3, scenario "The owner step does not offer Create when it could not load the owners" | Holds as written | (MQ-4 a) unchanged. (MQ-4 b) reword: when the Admin set cannot be told, every readable owner is offered and the server answers "Not created: you are not Admin of …" per owner | `useRbac()` fails open today | MQ-4 |
| U-4 | US-02a/US-03 (owner step), D16 | The ceiling N limits selections for removal and PDF only | N also limits the owners of one bulk-create run, with the same "Pick at most N" wording on the owner step | Keeps one run under about 30 s and under common proxy timeouts | MQ-2 |
| U-5 | US-02a AC-2.7 | "a run from a non-Premium instance is refused as a whole" | Unchanged under MQ-1 (a). Under MQ-1 (b) it cannot hold: on Community, owners holding no Then & Now would be created | — | MQ-1 |
| U-6 | Inventory S7 and "DESIGN must answer" (Admin set) | "No `GetWritable*` exists; the Admin set is new" | `useRbac()` already exposes `isTeamAdmin` / `isPortfolioAdmin`; `GetWritablePortfolioIdsAsync` exists. No story changes; the inventory line is corrected for the record | Code reading | — |
| U-7 | US-02a scenario "Changes during the run are refused owner by owner" | One run with a deleted Team, a lost Admin and a lapsed licence | Unchanged, testable at acceptance level only with stand-ins for the RBAC and licence answers changing between owners (no real clock can interleave them) | DISTILL note | — |
| U-8 | D8 lost answer | "it cannot confirm what was created" | Unchanged; DESIGN adds that the run still finishes on the server (MQ-5 a), so "check Reports" finds everything asked for | — | MQ-5 |
