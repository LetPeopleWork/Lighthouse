# Slice 01 — Baseline instrumentation (BE + FE)

**Goal (one sentence)**: Publish per-test timing CSVs (backend + frontend) as CI artifacts and provide a local summary helper so US-02's alternatives memo scores candidates against real numbers, not guesses.

**Owner story**: US-01.

**Estimated effort**: ≤ 1 day (workflow YAML edits + small TRX/Vitest-output post-processors + helper script).

**Learning hypothesis** (explicitly named):
- **H1 (the user's working hypothesis)**: backend slowness is dominated by `[Category("Integration")]` tests hitting real Jira + ADO + Linear.
- **H2 (alternative)**: backend slowness is dominated by per-class setup / static caches / parallelism caps, NOT by real-API latency.
- **H3 (alternative)**: frontend slowness is concentrated in a small number of specs (≤ 5) with known anti-patterns.
- **H4 (alternative)**: frontend slowness is uniformly distributed → config-level fix needed.
- **Disprove path**: the timing CSV tells us which hypothesis the data supports.

## IN scope

- `ci_backend.yml` — extract per-test durations from `*.trx` into `test-timings-backend.csv` (columns: `fully_qualified_name,category,duration_ms,outcome`). Distinguish `[Category("Integration")]` tests in the `category` column so H1 vs H2 can be answered from a CSV filter.
- `ci_frontend.yml` — capture Vitest run output and reduce to `test-timings-frontend.csv` (columns: `file,test_name,duration_ms,outcome`).
- `scripts/test-timings/summarise.sh` (Linux dev) + `summarise.ps1` (Windows dev) — read a local `TestResults/` directory and print the top-20 slowest tests with a category breakdown.
- Document the artifacts in `docs/ci-learnings.md` ("where to find per-test timings").

## OUT scope

- E2E / Playwright timings (D6 — out for this feature).
- Acting on the data (that is US-02's memo + candidate slices).
- Storing the timings outside the CI run (no historical trend DB).
- Trend visualisation in the GitHub Actions UI (CSV download is enough for v1).
- Cross-OS dev-laptop benchmark — Linux-runner numbers are canonical.
- Changing any test code.

## Acceptance criteria

- AC-01.1 … AC-01.5 from `feature-delta.md` US-01.

## Dependencies

None. This slice is the foundation for the alternatives memo (Slice 02).

## Reference class

CI workflow tweak + < 100-line post-processor per stack. Comparable scope to a "publish coverage as an artifact" change.

## Pre-slice SPIKE

Not required. The TRX schema and Vitest reporter output are stable, documented formats.

## Taste tests

- Ship 4+ new components? **No** — two post-processors + one helper script.
- Depends on a new abstraction? **No** — pure data extraction.
- Disproves something? **Yes** — the "we know what's slow" assumption AND a choice between H1-H4.
- Synthetic data only? **No** — uses real CI test outputs.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.
