# Story 6648 — Clean as we go: the slowest frontend page and dialog tests move down

Delivered 2026-11-15. ADO #6648 (Continuous Improvement, unparented; its Epic #6506 is Closed). Follow-up: #6652.
Workspace: `docs/feature/story-6648-frontend-clean-as-we-go/`.

## What was delivered

Three test files proved rules through a whole page render. Each now keeps one page test per wiring, and the rules are
proven on the dialog, hook or function that owns them. Behaviour-free: no pixel or user-visible behaviour changed.

| Slice | File | Tests | Local per-test time ratio (file alone / with its new direct tests) | First CI runs (s, median before ≈) |
|---|---|---|---|---|
| S1 | `src/pages/Reports/ReportsPage.removal.test.tsx` | 21 → 6 | 0.375 / 0.456 | 8.0, 6.7 (≈ 14.5) |
| S3 | `src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx` | 24 → 7 | 0.362 / 0.570 | 9.4, 5.8 (≈ 13.3) |
| S2 | `src/pages/Reports/ReportsPage.combinedPdf.test.tsx` | 18 → 13 | 0.790 / 0.816, **target 0.75 / 0.80 not met** | 12.9 (one run) |

New or extended direct tests: `RemoveSelectedDialog`, `useBulkReportRemoval`, `useSelectionActions`, `NoPdfDialog`,
`printFailures`, `ConditionPrompt`, `VotesAndCommentsDialog`, `useSizingLog`, `useCommentAdding`, `useVoteCasting`,
`sizingLogWording`.

One production change, agreed by the maintainer against the architect's recommendation: the failure rules of a combined
PDF (`isTheSameReport`, `failedAmong`, `leaveOutTheFailed`) moved out of `useReportsPdfExport.ts` into the pure module
`src/pages/Reports/printFailures.ts`, in its own `refactor(reports)` commit with the same 51 tests green before and after.

## Commits

`439d282ef` (pick-up) `..` the finalize commit on `main`. Code: `3f30ff73a` (S1), `8226e39c4` and `6cbc9492c` (S3),
`33f14d0a0`, `465b5b75c`, `fba8fb2fe`, `dea10c9ea` (S2). CI green on the code pushes (runs 43701347778, 43706351132).

## Gates

- Behaviour-free checks (`measurement/check-slice.sh`): pass for S1, S3, S2. Every removed test has a mapping row naming
  the test that now proves its rule (`measurement/slice-S*-moved-assertions.md`).
- Deliberate breaks (`measurement/deliberate-breaks.sh`): 17, 44 and 31 production breaks, each turns its named test red.
- Whole-candidate review (Haiku): accepted, 14 files / 137 tests, `tsc`, Biome.
- Mutation: `printFailures.ts` 100 % (28/28), `mutation/results.md`.
- EXAMINE (Haiku): E1 to E5 observation gaps, no violation observed; nwdes records the close as rejected because of the
  unmet verdicts. No public examine command reaches this story's mapping notes and breaks, and P1 needs main runs.

## Knowingly left open

- **P1 is judged later:** the per-file medians over at least 4 `main` runs after the push. Two runs so far for S1 and
  S3, one for S2. The 54 s frontend budget was not lowered; the largest file was 16.6 s before the story.
- S2 time target not met (0.790 / 0.816). No limit was loosened and no assertion weakened.
- `leaveOutTheFailed` compares reports by reference while `failedAmong` compares by id; the code did so before the move.
  Aligning them would change behaviour; not filed.
- One combinedPdf test title names one of the two holds its test checks.
- The export hook's guards are reached by no test, before and after.
- The full `pnpm test` / `pnpm build` were not run locally (parallel work on the machine); CI ran them green.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because no user-visible behaviour changed; only tests and one internal module. |
| Screenshots | N/A, because no pixel changed. |
| Demo data | N/A, because no feature or data shape changed. |
| Website assets | N/A, because nothing under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A, because no context, edge or concept changed; `printFailures.ts` sits beside the hook it came from. |
| Usage-data event | N/A, because the item is behaviour-free and adds no user action. |
| Mutation | Done: `printFailures.ts` 100 %, `docs/feature/story-6648-frontend-clean-as-we-go/mutation/results.md`. |
| Release notes | N/A; written at /release (behaviour-free, nothing a user sees). |
