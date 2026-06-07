# SPIKE Findings — zod-adoption-evaluation

**Date**: 2026-06-07 | **Agent**: nw-software-crafter (driven by orchestrator) | **For**: ADO Story 5232

## Assumption tested

Zod v4 works cleanly on Lighthouse's bleeding-edge **TS 6.0 / Vite 8 / react-query 5.100** stack, AND the **"parse → construct behaviour-bearing class"** pattern works for the forecast models (`ManualForecast` / `WhenForecast` carry methods + `class-transformer` decorators) — at acceptable bundle cost and with the existing test suite staying green.

## Verdict: **WORKS**

Zod **4.4.3** installed. The forecast manual-forecast boundary was converted from a hand-written deserializer to a Zod schema (`ManualForecastSchema`) parsed through a new shared `BaseApiService.parse(schema, data)` helper, then used to construct the existing behaviour-bearing `ManualForecast` class.

### Evidence

| Gate | Result |
|------|--------|
| Probe (standalone, Node 26): ISO-string coercion, Date-object input, defaults, drift detection | ✓ all four — drift produced a structured `z.prettifyError` message pointing at `whenForecasts` |
| `tsc -b` (full TS 6.0 typecheck) | ✓ green |
| `vite build` (Vite 8 production) | ✓ green |
| `biome check ./src` (CI parity, no `--write`) | ✓ 613 files, no fixes |
| `ForecastService` unit tests | ✓ 17/17 (16 existing unchanged + 1 new fail-fast acceptance test) |
| Full frontend suite | ✓ 3407/3407 (260 files) |

### Bundle-size delta (measured, `vite build` main chunk gzip)

| | raw | gzip |
|---|---|---|
| Before (main) | 2,560.72 kB | 750.01 kB |
| After (main) | 2,629.03 kB | 767.57 kB |
| **Delta** | **+68.31 kB** | **+17.56 kB (+2.3%)** |

This is the **full `zod`** import (method-chaining API resists tree-shaking, and we use `z.coerce` + `z.prettifyError`). Consistent with the research memo's ~11–14 KB estimate, slightly higher for the full API. `zod/mini` (~1.9 KB) remains an in-family escape hatch if the byte budget ever binds — no guidance rewrite needed to switch.

## Design implications (for the step-by-step rollout)

1. **`z.coerce.date()` is the key to test-compatibility.** Existing `ForecastService` tests pass `ManualForecast` instances (real `Date` objects) as mock responses; the real backend sends ISO strings. `z.coerce.date()` accepts both, so the schema works identically against tests and production with zero test changes.
2. **Parse → construct keeps behaviour-bearing classes intact.** `ManualForecast` / `WhenForecast` keep their methods; the schema owns *validation*, the class owns *behaviour*. The deserializer maps the parsed plain object into the class exactly as before — output objects are byte-identical, so `toEqual` assertions stay green untouched.
3. **The shared helper is the reusable unit.** `BaseApiService.parse(schema, data)` routes `safeParse` failures through the existing `ApiError` plumbing (`code: "INVALID_RESPONSE"`, `technicalDetails` = `z.prettifyError`). Every future boundary reuses it — one helper, N schemas.
4. **The axios generic became `<unknown>`.** `post<IManualForecast>` was a compile-time lie; `post<unknown>` is honest — the schema is now the only thing that asserts shape, and it does so at runtime.
5. **Interfaces NOT yet deleted.** This slice *added* schemas alongside `IManualForecast`/`IWhenForecast` to keep blast radius minimal. Replacing the interfaces with `z.infer` is deliberately deferred to per-boundary rollout slices (see `../adoption-roadmap.md`).

## Constraints discovered

- **`backtest` left on the old path on purpose.** `runBacktest` still uses its hand-written deserializer — the spike intentionally converted only the manual-forecast path to keep the slice thin. It's the natural next conversion within the same boundary.
- **No TS 6.0 incompatibility surfaced.** Zod officially tests TS 5.5+; the memo flagged TS 6.0 as the top risk. This spike clears it for the forecast surface (`z.object`, `z.coerce.date`, `z.array`, `z.infer`, `z.prettifyError`, `safeParse`, `.optional().default()` all typecheck and run). Risk downgraded from "open" to "cleared for the APIs used here."

## Promoted

Promoted on 2026-06-07 — committed to `src/`, not throwaway. See `wave-decisions.md`.
