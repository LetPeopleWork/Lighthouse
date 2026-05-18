# Spike — Frontend test profiling (top-3 slow files)

**Goal (one sentence)**: Profile `CreateConnectionWizard.test.tsx`, `OverviewDashboard.test.tsx`, and `DeliveryCreateModal.test.tsx` to find the **root cause** of their slowness, so the next FE slice fixes mechanisms (real timers, oversized fixtures, redundant renders, MSW overhead, …) rather than guessing at patterns.

**Owner story**: US-02 (catalog candidate CS-D refined, added during Slice-02 resume after user feedback rejected the CS-I CI-sharding direction).

**Estimated effort**: ½ day. Three files; per-file flame graph + per-`it()` cost attribution; one short report.

**Learning hypothesis**:
- Confirms: The top-3 files share 1–2 recurring causes (most likely candidates from the literature: real timers leaking through, polling `findBy`/`waitFor` with 1 s default timeout where assertions are synchronous, oversized MUI render trees). Once identified, fixes are localised and the saving is predictable.
- Disproves: If each file has a different cause, the FE slice is harder to scope and we may need to accept slower FE for now. If the dominant cost is `import` time / module evaluation, no per-test refactor helps and we'd revisit CS-I anyway despite prior experience.

## IN scope

- For each of the 3 files:
  - Run with `vitest run <path> --reporter=verbose` and capture per-`it()` durations.
  - Run with `node --prof` (or `--cpu-prof`) wrapping Vitest's CLI; produce a flame graph for the file.
  - Walk through the slowest 3 `it()` blocks per file. Document:
    - What the test asserts.
    - What setup / render / interaction it does.
    - Which of the standard FE-slowness causes applies (named list below).
    - The smallest change that would address it AND the expected saving.
- Causes to check against (named upfront so the report is structured):
  1. **Real timers** — `setTimeout` / `setInterval` / `requestAnimationFrame` not faked.
  2. **`waitFor` / `findBy` overuse** — synchronous assertions awaited with polling.
  3. **Oversized fixture trees** — full `Team` / `Project` mocks with 100+ fields per test when 5 are read.
  4. **Redundant renders** — multiple `render()` calls when one + `rerender` suffices.
  5. **`userEvent.type(longString)`** — character-by-character typing when `paste` would be one call.
  6. **MSW handler resolution latency** — unintended fall-through to default 404 handlers.
  7. **Recharts / MUI heavy first-render** — module-evaluation cost paid per test instead of file.
  8. **Excess promise-microtask churn** — `await` chains that needlessly serialise.
- Output: `docs/feature/test-speed-improvements/spike-fe-profile-findings.md` — one section per file, one subsection per slow `it()`, with cause + proposed fix + expected saving.

## OUT scope

- Applying any fix. Fixes go in the follow-up FE slice.
- Profiling all 224 FE files. We're targeting the top-3 (30 % of FE) — diminishing returns past there.
- Vitest config tuning (already attempted, low ceiling).
- CI sharding (rejected per user feedback — prior experience showed effort > gain).
- Switching test framework.

## Acceptance criteria

- AC-SPIKE-FE.1: `docs/feature/test-speed-improvements/spike-fe-profile-findings.md` exists and covers all 3 files.
- AC-SPIKE-FE.2: For each file, the top-3 slow `it()` blocks are diagnosed against one or more named causes from the list above (or a new named cause if the list misses one — back-prop to feature-delta.md catalog if so).
- AC-SPIKE-FE.3: Each diagnosis has a per-fix expected saving in ms with the basis stated (e.g. "remove 2 × `waitFor(..., {timeout: 1000})` around synchronous assertions: 2 × ~50 ms = ~100 ms"); honest about uncertainty.
- AC-SPIKE-FE.4: The report ends with a one-paragraph recommendation: open `slice-fe-root-cause-refactor` with scope X, or defer because no single fix moves the needle.

## Dependencies

- None. The 3 files exist on `main`; profiling is read-only.

## Reference class

Diagnostic spike. Same shape as a `/nw-root-why` investigation, except feature-scoped and focused on test runtime rather than production behaviour.

## Pre-slice SPIKE

This IS the spike. No nested spike.

## Taste tests

- Ship 4+ new components? **No** — one markdown report.
- Depends on a new abstraction? **No** — uses existing Vitest + Node `--prof`.
- Disproves something? **Yes** — that "apply CS-D's generic patterns" is the right move without evidence.
- Synthetic data only? **No** — real profiling output.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.

## Note

The user's feedback explicitly rejected CS-I (Vitest sharding) — prior experience was that the effort outweighed the gain. This spike steers toward local-first wins (because the same `pnpm test` runs locally and in CI; root-cause fixes help both).
