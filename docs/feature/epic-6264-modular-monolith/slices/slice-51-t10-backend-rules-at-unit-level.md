# Slice 51 — T10 Backend rules pinned by unit tests, with integration tests left to prove the wiring

Epic #6506 · ADO #6537 · `@infrastructure` · job_id: infrastructure-only · **Size M** (estimate: pilot plus two suites, a day each) · DRAFT
infrastructure_rationale: a host test that pins a rule a unit test could pin costs about 204 ms where a unit test costs 0.9 ms. Behaviour-free.

## Goal and why

The slowest backend integration tests that only pin a rule move to unit tests; what stays at the host proves wiring. Summed `Integration`
time is measurably lower on CI, with no behaviour lost. ADO #6537, CI run 41691382136: integration tests have a median of 204 ms and take
76 % of backend test time. FE7 cut summed time of touched files by 12 to 63 % but showed no visible CI gain (`evolution.md`), so the
number to watch is the `Integration` time T9 publishes, not wall time.

## Settled elsewhere (not reopened)

T10 comes after T5 and starts from the files T5 leaves; a suite T5 touches is closed to T10 until T5 finishes it. `Integration` is one
category on a shared base class (T6). T5 owns host-to-port moves and the `Contracts` namespace; a test already at the port is not moved again.

## Proposals (each with its why)

1. **Finding candidates by measured time.** Take the timing CSVs of at least 5 plain `main` runs after T6 and T5, keep `Integration` rows
   (the `category` column T6 slice D writes), rank classes by median seconds across the runs. Why: the median damps runner luck (backend
   totals vary 1.13 x, T4), and T5 found that ranking by host builds measures the past. Exclude live connector and Postgres container
   classes (they prove a provider, not a rule), and T5's skeleton list and `Contracts` (meant to stay).
2. **The screen for "a unit test could pin it".** Each assertion is about a business value that a service or domain class computes, and
   none about route, verb, status code, JSON name, header or 401/403. Why: that is T5's line for what stays at the host, so both Stories
   give one answer. A mixed test is split: rule to unit, one wiring check stays.
3. **What it reuses.** From T5: the per-suite HTTP-coverage table, the before/after id table (old id to new id or "stays"), and the
   deliberate break of one production line per moved suite. From FE7: every removed assertion has a row, coverage of the touched area not
   lower, each call site proven by a deliberate break. Changed: FE7's "surviving ids unchanged" cannot hold when tests move, so T5's id
   table replaces it.
4. **Where the unit test builds its subject.** T5's port helper, or direct construction for two or three collaborators. No new helper here;
   if T5 leaves none, T10 asks.
5. **Stopping point.** The top two suites by the ranking, then decide again with the numbers (as T5). A suite is worth its day only if the
   measured seconds a move removes (one timed run before and after) reach 1 % of summed `Integration` time (an estimate, set once the
   baseline exists); below that, stop and list the rest as backlog. Why: FE7's own sweep stopped when payoff fell.
6. **Proof of done.** The median of summed `Integration` seconds over at least 5 `main` runs after the last commit is below the median over
   5 runs before, both from T9's summary; T9's `Integration` mean ceiling is lowered in the last commit, as T13 does the frontend budget.
   If wall time does not fall, the record says so, as FE7's did.

IN: ranking, coverage and id tables, the moves, the ceiling update. OUT: production code (a rule with no extractable unit stays at the host
and feeds Epic #6508, as T5 treats it), live and container classes, frontend, new helpers beyond T5's.
Dependencies: hard T5 #6469 and T6 #6512; soft T9 #6536 (baseline, ceiling). Slice-composition gate waived (D-6264-5).

## Acceptance criteria

- [ ] The ranking and its source runs are in the workspace; each moved suite has a coverage table, an id table and a deliberate-break proof.
- [ ] Median summed `Integration` seconds over at least 5 `main` runs is below the baseline; coverage of the touched area is not lower.
- [ ] No production file changes (mutation N/A, recorded as T5 and T13 record it); T5's guard stays green and its lists do not grow.

## Cross-cutting (no silent N/A)

RBAC: N/A, no authorization logic changes; 401/403 tests stay at the host. Lighthouse-Clients: N/A, no contract or version changes. Website:
N/A, nothing user-visible. Usage-data event: N/A, test structure. UI sketch: N/A, no screen. Terminology: N/A, no user-facing word. Context map: N/A, tests only.

## Open for the maintainer

1. **Stop rule.** (a) Top two suites, then ask; (b) every suite above a seconds threshold set at DESIGN; (c) the 1 % cut. Recommend (a),
   with (c) as the reason to stop early.
2. **Success in numbers.** (a) Any measurable fall in the median sum; (b) a percentage target set after the baseline. Recommend (a) now, (b) at DESIGN.
3. **A suite left on T5's shrinking list.** (a) T10 may take it once T5 finishes; (b) leave it to whoever touches it. Recommend (a) if it ranks high.

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **Stop:** after the top two suites, or earlier if a suite would save under 1 % of integration time; then ask.
- **Success:** any measurable fall in the median summed `Integration` time now, a percentage set at DESIGN.
- **T5's leftovers:** T10 may take a high-ranking suite T5 left, once T5 is finished.
- T10 lowers T9's ceilings in its last commit, the total included.

## Test pyramid vision (maintainer, 2026-11-07)

Direction, not a gate: about 90 % fast, 9.5 % integration, 0.5 % E2E, by test count, as upper limits on the two upper layers.

- **Layers:** fast is the fast tier (`pnpm test:fast`, `Test-Fast.ps1`). Integration is everything in the middle, including frontend `.test.tsx`. E2E is everything in Playwright.
- **Moving down beats the limit:** a test moved from E2E to integration is good, even if the integration share goes over for a while.
- **The hard rule stays time:** the time ceilings gate the build; the shares are only reported, because a share can be met by adding trivial tests.
- **Where it stood on 2026-11-07:** frontend 3,648 fast and 5,857 `.test.tsx` of 9,505; about 56 Playwright tests, 0.25 % of about 22,600. The backend split is known only once T6 has labelled it.

T10 is the rewriting: it moves backend rules from integration tests down to unit tests.

**Change from T5's DEVOPS (2026-11-07).** Read a host-to-port saving by class, not from the tier column.
