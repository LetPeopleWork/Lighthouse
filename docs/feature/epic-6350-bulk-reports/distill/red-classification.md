# Epic 6350 — RED classification at DISTILL hand-off (2026-10-27, re-run after the final-gate revision)

Each layer's pending scenarios were temporarily un-skipped, run, classified and re-skipped; the suites then re-ran
green with the scenarios pending. Classes: **MISSING** = fails because the behaviour does not exist yet (the RED
DELIVER turns GREEN); **BROKEN** = fails on setup, wiring or a test mistake; **HOLDS** = passes today (a preservation
or "nothing happens" guard).

## Backend (NUnit, `TestCategory=epic-6350-bulk-reports`)

First pass: every pending scenario un-skipped; 68 failed, 2 passed. Re-check after the revision: the slice-01 file
un-skipped (including the rewritten owner-fields scenario and the Team-filter scenario now that the RBAC double is
scoped); every one failed on `Expected: OK But was: NotFound`.

| Outcome | Count | Signature |
|---|---|---|
| MISSING | all pending | `api/latest/reports…` answers 404 (also where 400 or 403 is expected); the four event names refused / absent from `UsageDataEventName`; the usage page lacks the new rows and sentences; the Postgres FK race answers 404 (Docker present) |
| BROKEN | 0 | One setup failure in the first pass (a Community cap reached while seeding the removal skeleton) was fixed by seeding on Premium and letting the licence lapse |
| HOLDS | 2 | `The_per_owner_list_names_no_owner_as_before`, `Every_event_named_before_them_keeps_its_number` — the two active guards |

## Frontend (Vitest)

All 103 scenarios un-skipped after the revision: **103 failed, all MISSING** — `[data-testid="all-reports"]` absent
(43), no "Create reports" button (39), the empty / could-not-load copy and Retry absent, no progressbar, the
navigation lacking Reports (2), `SCAFFOLD: the combined PDF is not built yet` (bundle rows), and three waits timing
out because the page scaffold throws before the usage-data answer appears. BROKEN 0, HOLDS 0.

## E2E (Playwright)

`BulkReports.spec.ts` is `fixme`; it typechecks, lints and lists one test. MISSING by construction: `goToReports`
clicks a navigation entry that does not exist.

## Clients (vitest, `lighthouse-clients`)

All 18 un-skipped after the revision.

| Outcome | Count | Signature |
|---|---|---|
| MISSING | 14 | `lh report list` with no owner exits 1 with "Missing --team-id or --portfolio-id" (including the renamed-terms scenario); `lighthouse_report_list` is an unknown tool (MCP -32602), so it never reads `GET /reports` (the refused-list scenario now waits for that read and fails on it) |
| HOLDS | 4 | the CLI "reports nothing" guards (said no, refused list, one Team's list) and "a list naming one Team still reads only that Team's reports" — preservation scenarios that must keep holding |
| BROKEN | 0 | — |
