# Zod Adoption — Step-by-Step Roadmap

**Status**: In progress — slice 1 (forecast manual-forecast) shipped as a spike walking skeleton on 2026-06-07.
**Decision**: ADOPT-INCREMENTALLY (see `research/zod-adoption-evaluation.md` for the full evaluation + draft ADR, and `spike/findings.md` for the proven pattern).

Zod is adopted **one trust boundary at a time**, not in a big-bang sweep. Each step is a thin, independently shippable slice that (a) converts one API response boundary to a Zod schema, (b) deletes the paired hand-written interface in favour of `z.infer`, (c) keeps existing behaviour-bearing model classes by constructing them *from* the parsed object, and (d) leaves the build/tests/biome green. The reusable machinery — `BaseApiService.parse(schema, data)` + the `z.coerce.date()` convention — already exists from slice 1.

## The reusable pattern (established in slice 1)

```ts
// 1. schema next to the model, z.infer is the source of truth
export const XSchema = z.object({ /* ... */ expectedDate: z.coerce.date() });
export type XResponse = z.infer<typeof XSchema>;

// 2. parse at the boundary via the shared helper (already on BaseApiService)
const parsed = BaseApiService.parse(XSchema, response.data);   // throws ApiError on drift

// 3. construct the behaviour-bearing class from the parsed plain object
return new X(parsed.a, parsed.b /* ... */);
```
`z.coerce.date()` lets one schema serve both unit tests (Date objects) and production (ISO strings) with no test churn.

## Steps

| # | Boundary | Scope | Why this order | Status |
|---|----------|-------|----------------|--------|
| 0 | **Convention docs** | Rewrite the Zod sections in `CLAUDE.md` + `.github/instructions/frontend-typescript.instructions.md` (+ Copilot/README mirrors) to "required at *converted* boundaries (currently: forecast manual-forecast), expanding per this roadmap; new boundaries MUST be schema-validated." Fix the v3-stale examples (`error.errors`→`error.issues`, `z.ZodSchema`→`z.ZodType`). | Ends the stated-but-unfollowed contradiction *now*; makes the docs true at each step. **Awaiting user confirmation before editing.** | ⏳ pending |
| 1 | **Forecast — manual forecast** (`ForecastService.runManualForecast` / `runItemPrediction`) | `ManualForecastSchema` + `BaseApiService.parse`; construct `ManualForecast`/`WhenForecast`/`HowManyForecast`. | Highest value + risk; feeds MUI-X charts with a history of runtime shape incidents (React #185). Proves the TS 6.0 / Vite 8 spike. | ✅ done (2026-06-07) |
| 2 | **Forecast — backtest** (`ForecastService.runBacktest`) | `BacktestResultSchema`; replace `deserializeBacktestResult`. | Same boundary, finishes the forecast surface; smallest possible next slice. | ⏳ next |
| 3 | **Work-item / feature lists** (`FeatureService`, `WorkItemService`) | Schemas for `IFeature` / work-item DTOs; replace `deserializeFeatures`. Reconcile behaviour-bearing `Feature` methods (`getRemainingWorkForTeam`, …) via parse→construct. | Largest blast radius + the behaviour-bearing-class cost the memo flagged; do it once the pattern is battle-tested. | ⏳ later |
| 4 | **Licensing** (`LicensingService`) | `LicenseStatusSchema`. | Security/correctness-adjacent; a malformed license response should fail loudly. | ⏳ later |
| 5 | **Auth / RBAC summary** (`AuthService`, RBAC summary endpoint) | Schema for the authorization summary the `useRbac()` hook derives from. | Security-adjacent; gates UI permissions — drift here is high-impact. | ⏳ later |
| 6+ | Remaining services, one group per slice | Convert opportunistically when a service is touched for other work (boy-scout). | Long tail; no need to convert internal/derived models that aren't trust boundaries. | ⏳ ongoing |

## Guardrails

- **Only trust boundaries.** Per the team's own rule, internal/derived data keeps plain types — don't schema-ify the ~64 models wholesale; only the ones that cross the network boundary.
- **Each slice stays green.** `tsc -b` + `vite build` + `biome check ./src` (zero warnings) + the boundary's unit tests + a fail-fast drift test, before push.
- **Bundle watch.** Slice 1 added +17.56 kB gzip (full `zod`). If the cumulative budget binds, switch the import to `zod/mini` (no schema/API rewrite).
- **Delete the interface each slice.** The end state is `z.infer` as the single source of truth — an `interface` left alongside its schema can silently drift.
