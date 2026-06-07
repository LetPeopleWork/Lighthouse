# SPIKE Decisions — zod-adoption-evaluation

## Assumption Tested
- Zod v4 works on TS 6.0 / Vite 8 / react-query 5.100, and the "parse → construct behaviour-bearing class" pattern works for the forecast models at acceptable bundle cost with the test suite green.

## Probe Verdict
- **WORKS**: zod 4.4.3 parses ISO-string + Date-object + drift inputs correctly; `tsc -b`/`vite build`/biome/full test suite all green; +17.56 kB gzip bundle delta.

## Promotion Decision
- **PROMOTE**: user pre-approved ("proof the concept with a spike"). The converted forecast boundary is real, idiomatic, and tested — worth keeping and building on rather than discarding. The concept is proven *in the actual codebase*, which is more valuable than a throwaway.

## Walking Skeleton
- Driving boundary: `ForecastService.runManualForecast` / `runItemPrediction` → `BaseApiService.parse(ManualForecastSchema, …)` → `ManualForecast` class → forecast UI.
- Acceptance test: new case in `src/services/Api/ForecastService.test.ts` — "should reject a response missing required fields with a structured ApiError" (asserts fail-fast `ApiError` with `code: INVALID_RESPONSE` and `technicalDetails` naming the missing field). This is the *new behaviour* the slice delivers.
- Commit: see `feat(forecast): walking skeleton` commit on `main`.
- Demo: feed `runManualForecast` a payload missing `whenForecasts` → caught `ApiError` with field-path detail, instead of an `undefined.map` crash deep in a chart.

## Design Implications
- `z.coerce.date()` makes one schema serve both tests (Date objects) and production (ISO strings) — zero test changes.
- `BaseApiService.parse(schema, data)` is the reusable unit; every future boundary = one schema + one call.
- `post<unknown>` is the honest boundary type; the schema is the sole runtime shape authority.
- Interface→`z.infer` replacement is deferred to per-boundary rollout slices (kept blast radius minimal here).

## Constraints Discovered
- `runBacktest` deliberately left on its hand-written deserializer — thinnest slice; it's the natural next conversion.
- TS 6.0 compatibility risk (memo's #1 open risk) is **cleared** for the Zod APIs used at the forecast surface.

## Convention contradiction — still open (NOT resolved by this spike)
- The four docs (`CLAUDE.md`, `frontend-typescript.instructions.md`, Copilot rules, `INSTRUCTIONS_README.md`) still mandate Zod absolutely while only one boundary is converted. Per the memo, the convention must be rewritten to "required at converted boundaries (currently: forecast manual-forecast); expanding per the adoption roadmap." **Deferred to user confirmation** (see `../adoption-roadmap.md` step 0). Also: instruction examples are Zod-v3-stale (`error.errors`→`error.issues`, `z.ZodSchema`→`z.ZodType`).
