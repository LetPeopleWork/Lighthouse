# Slice 04 — Lock in the gain: justified residue + regression guard

**Goal (one sentence)**: Make the surviving `[NonParallelizable]` set an explicit, justified allowlist and add a build guard that fails when a new opt-out appears off-allowlist, so the parallel-debt cannot silently re-accumulate the way it did after #5020's CS-P.

**Owner story**: US-04.

**Estimated effort**: ½ day.

**Learning hypothesis**:
- Confirms: A small, justified allowlist + a guard keeps the suite parallel-by-default; planting an un-listed `[NonParallelizable]` turns the build red.
- Disproves: "This won't regress again on its own." It did exactly that once (54 accumulated with no guard) — the guard is the structural fix, not discipline-by-hope.

## IN scope

1. **Allowlist** — the inherently-serial fixtures (expected: `API/Security/**` rate-limiting/CORS-env/API-key-scopes/group-snapshot + `LighthouseAppContextConcurrencyTest`), each with a one-line justification. Lives where the guard reads it (a typed list in the guard test, or a checked-in data file the guard loads).
2. **Guard** — an ArchUnitNET test (per `feedback-ci-and-e2e-minimalism`: backend test, NOT Playwright) that scans the test assembly for `[NonParallelizable]` and fails if any fixture carrying it is not on the allowlist. The failure message points to the allowlist + the `docs/ci-learnings.md` entry.
3. **CI-learnings entry** — document the per-fixture isolation pattern, why the allowlist exists, the `IntegrationTestBase` precedent, and "adding `[NonParallelizable]` requires an allowlist entry + reason."
4. **ADO comment** — #5258 before/after wall-clock + final allowlist size.

## OUT scope

- Any further fixture isolation (that's Slices 02–03; this slice only ratifies the residue).
- A custom Roslyn analyzer (an ArchUnit test is sufficient and cheaper; revisit only if the test proves flaky).

## Acceptance criteria

- AC-04.1..04.4 from `feature-delta.md` US-04.

## Dependencies

- Slices 02 + 03 merged (the allowlist must be final — it can only list what genuinely remains).

## Reference class

Guard/ratchet test — same shape as the existing ArchUnitNET seam tests (e.g. the cumulative-state-time / domain-events arch tests) that fail the build on a structural violation.

## Taste tests

- Ship 4+ new components? No — one guard test + one doc entry. Pass.
- Depends on a new abstraction? No (reuses ArchUnitNET already in the suite). Pass.
- Disproves something? Yes (the "won't regress" assumption — the guard is the proof). Pass.
- Synthetic data only? The guard plants a violation to prove it goes red — that's a guard self-test, not value-faking. Pass.
- Duplicate-except-scale? No. Pass.
