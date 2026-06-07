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
| 0 | **Convention docs** | Rewrote the Zod sections in `CLAUDE.md` + `frontend-typescript.instructions.md` + `copilot-instructions.md` + `INSTRUCTIONS_README.md` to "required at *converted* + new boundaries (rolling adoption), validate via `BaseApiService.parse`"; added the behaviour-bearing-model rule; fixed v3-stale examples (`error.errors`→`error.issues`, `z.ZodSchema`→`z.ZodType`). | Ends the stated-but-unfollowed contradiction; makes the docs true at each step. | ✅ done (2026-06-07) |
| 1 | **Forecast — manual forecast** (`ForecastService.runManualForecast` / `runItemPrediction`) | `ManualForecastSchema` + `BaseApiService.parse`; construct `ManualForecast`/`WhenForecast`/`HowManyForecast`. | Highest value + risk; feeds MUI-X charts with a history of runtime shape incidents (React #185). Proves the TS 6.0 / Vite 8 spike. | ✅ done (2026-06-07) |
| 2 | **Forecast — backtest** (`ForecastService.runBacktest`) | `BacktestResultSchema`; replace `deserializeBacktestResult`. | Same boundary, finishes the forecast surface; smallest possible next slice. | ✅ done (2026-06-07) |
| 3 | **Feature list** (`FeatureService` → `BaseApiService.deserializeFeatures`) | `FeatureSchema` (records via `z.record`, `StateCategory` via `z.enum`, nested `forecasts` reusing `WhenForecastSchema`, coerced dates); `Feature.fromParsed` constructs the behaviour-bearing class; **class-transformer + reflect-metadata retired from `Feature.ts`**. | Largest blast radius + the behaviour-bearing-class cost the memo flagged. Proved the hardest parts. | ✅ done (2026-06-07) |
| 4 | **Licensing** (`LicensingService`) | `LicenseStatusSchema`; `getLicenseStatus` + `importLicense` parse via `BaseApiService.parse` (replaced the hand-rolled `expiryDate` date conversion). `canUsePremiumFeatures` defaults `false` (fail-closed on omission). | Security/correctness-adjacent; a malformed license response should fail loudly. | ✅ done (2026-06-07) |
| 5 | **RBAC summary** (`RbacService.getAuthorizationSummary` → `/authorization/my-summary`) | `UserAuthorizationSummarySchema`; the summary `useRbac()` derives from. Required permission booleans have NO defaults → drift fails closed (deny). | Security-adjacent; gates UI permissions — drift here is high-impact. | ✅ done (2026-06-07) |
| 6+ | Remaining services, one group per slice | Convert opportunistically when a service is touched for other work (boy-scout). | Long tail; no need to convert internal/derived models that aren't trust boundaries. | ⏳ ongoing |

## Finding from slice 3 (Feature): interfaces are NOT always deletable

The memo's ideal end-state is "delete the interface in favour of `z.infer`." Slice 3 surfaced a real exception: **`IFeature` is not just a DTO — it's the class's behavioural contract** (it declares `getRemainingWorkForTeam()` etc. on top of `IWorkItem`), and it has ~60 importers. A data-only `z.infer<typeof FeatureSchema>` cannot express methods, so the right design for **behaviour-bearing models** is:

- **`FeatureSchema` owns runtime validation** of the incoming data (the boundary).
- **`Feature.fromParsed(data)` constructs the class** from the validated plain object (parse → construct).
- **`IFeature` stays** as the behavioural contract the class implements and that ~60 consumers import.

So for behaviour-bearing classes, Zod replaces the **deserializer**, not the interface. The "delete the interface → `z.infer`" rule applies cleanly only to **pure-data DTOs** (no methods). Worth encoding in the convention rewrite (step 0).

A bonus payoff: retiring `class-transformer`/`reflect-metadata` from `Feature.ts` removed decorator-metadata runtime cost from the single most-imported model (Team/Portfolio/WhenForecast still use class-transformer — candidates for later slices to fully drop the dependency).

## Guardrails

- **Only trust boundaries.** Per the team's own rule, internal/derived data keeps plain types — don't schema-ify the ~64 models wholesale; only the ones that cross the network boundary.
- **Each slice stays green.** `tsc -b` + `vite build` + `biome check ./src` (zero warnings) + the boundary's unit tests + a fail-fast drift test, before push.
- **Bundle watch.** Slice 1 added +17.56 kB gzip (full `zod`). If the cumulative budget binds, switch the import to `zod/mini` (no schema/API rewrite).
- **Delete the interface each slice.** The end state is `z.infer` as the single source of truth — an `interface` left alongside its schema can silently drift.
