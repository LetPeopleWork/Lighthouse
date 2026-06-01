# ADR-031: Adopt Vitest + React Testing Library + jsdom in the website repo

> **Scope: WEBSITE repo (`/storage/repos/website`), NOT the Lighthouse product.** Authored for ADO Epic #5123 (Flow & Forecasting Readiness Assessment). ADR numbering continues the Lighthouse-product sequence by team convention, but this decision applies only to the website application.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

The website repo (`vite_react_shadcn_ts`, a Lovable-generated React 19 + Vite 7 + shadcn/ui app) has **no test framework**. `package.json` scripts are only `dev`, `build`, `build:dev`, `lint`, `preview`. This directly contradicts:

- The feature's own Definition of Done items 1-2 ("All UAT scenarios pass green in the website test stack"; "Scoring is deterministic with boundary tests covering 25/26, 50/51, 75/76, 0, 100").
- Lighthouse CLAUDE.md's non-negotiable TDD mandate (every line of production code exists to satisfy a failing test).

The feature's riskiest correctness surface is the pure scoring module (boundary classification 25/26, 50/51, 75/76, 0, 100) and the degrade-open capture behaviour. Both are cheaply and decisively covered by unit/component tests; neither is well covered by manual QA. Without a test runner the feature cannot be delivered to DoD.

The project's `tsconfig` is deliberately loose (`strict: false`, `noImplicitAny: false`, `strictNullChecks: false`) — the shadcn/Lovable default. Tests therefore carry extra weight: they are the primary correctness net for a codebase whose compiler is permissive.

## Decision

Introduce **Vitest 3 + @testing-library/react + @testing-library/jest-dom + jsdom** to the website repo as a foundational decision shipped inside Slice 01 (the walking skeleton), before any feature production code.

- **Runner**: Vitest (Vite-native; reuses the existing `vite.config.ts` resolve aliases including `@/*`; SWC-compatible; zero extra bundler config). Test config added as a `test` block in a dedicated `vitest.config.ts` (kept separate from `vite.config.ts` build config to avoid coupling build to test deps).
- **Environment**: `jsdom` (component tests for quiz/gate/teaser/breakdown/dashboard; the scoring core needs no DOM but runs in the same runner).
- **DOM assertions**: `@testing-library/jest-dom` matchers via a `vitest.setup.ts` referenced by `setupFiles`.
- **Scripts**: add `"test": "vitest run"` and `"test:watch": "vitest"` to `package.json`.
- **Test layering**:
  - Pure scoring module → cheap, exhaustive **unit tests** on the boundary table (0, 25, 26, 50, 51, 75, 76, 100 plus all-0/all-3).
  - Quiz state machine, email gate, teaser/breakdown, dashboard → **component tests** through public behaviour (RTL `render` + user-event), never internal state.
- **Lint parity**: tests live under `src/**/*.test.ts(x)`; the existing flat-config `eslint.config.js` already globs `src`. Add `vitest`/`jest-dom` to the lint env only if a rule flags globals; otherwise no eslint change. No Biome in this repo (eslint only) — the Lighthouse Biome rules do not apply here.

## Alternatives Considered

- **Jest + ts-jest / babel-jest**: the historical default. Rejected — requires a separate transform pipeline and module-resolution config duplicating what Vite already provides; slower; awkward ESM/`"type": "module"` interop. Vitest is the modern Vite-native peer with the same Jest-compatible API.
- **Playwright component testing / Playwright only**: rejected as the *primary* net — far heavier per-test, no boundary-table ergonomics, and overkill for a pure function. (A thin Playwright/manual E2E walkthrough is still valuable per slice for the live flow, but it is not where boundary correctness is proven.)
- **No framework, rely on manual QA + `pnpm build`**: rejected — `build` only type-checks (and loosely, given `strict:false`); it cannot assert `score(rawSum=9)===50` or that a Supabase write failure still unlocks the breakdown. Violates DoD 1-2 and TDD mandate outright.

## Consequences

- **Positive**: unlocks DoD 1-2 and TDD; the scoring core gets a decisive, fast boundary net; future website features (incl. sibling epic 5124) inherit the runner. Vitest reuses existing Vite config → minimal setup surface.
- **Negative**: adds ~4 dev dependencies and a CI test step the website repo did not have; someone must wire the website CI (currently build-only) to run `pnpm test`. The loose tsconfig means tests — not the compiler — are the type-safety backstop, so test discipline matters more here than in a `strict` project.
- **Follow-up**: website CI pipeline must add a `test` job (handed to platform-architect / website owner). Version pinning in the technology table (DESIGN).
