# Research & Decision Memo: Should Lighthouse adopt Zod for runtime schema validation at frontend trust boundaries?

**Date**: 2026-06-07 | **Researcher**: nw-researcher (Nova) | **For**: ADO Story 5232 | **Confidence**: Medium-High | **Sources**: 17 cited (6 High, 3 Medium-High, 4 Medium w/ 3-source cross-ref, + primary codebase)

> Decision-grade architectural research memo. Ends with a single verdict (ADOPT / DON'T ADOPT / ADOPT-INCREMENTALLY), a draft ADR if applicable, and required doc changes.

> **Update 2026-06-07 — verdict validated by spike.** A proof-of-concept spike converted the forecast manual-forecast boundary to Zod v4 (4.4.3) and shipped it as a walking skeleton. All gates green on the bleeding-edge TS 6.0 / Vite 8 stack; bundle delta +17.56 kB gzip; 3407/3407 tests pass. The memo's #1 open risk (TS 6.0 compatibility) is **cleared** for the forecast surface. Step-by-step rollout plan: `../adoption-roadmap.md`. Spike evidence: `../spike/findings.md`.

---

## Executive Summary

Lighthouse's frontend has a self-inflicted contradiction: four governing documents (`CLAUDE.md`, `frontend-typescript.instructions.md`, the Copilot rules, `INSTRUCTIONS_README.md`) **mandate Zod-based runtime validation at trust boundaries**, but the package is not installed and is imported nowhere. Every one of the ~30 axios API services instead *asserts* response shapes via unchecked generics (`post<IFeature[]>`) and `as`-casts, and even the "structured" deserialization through `class-transformer`'s `plainToInstance` **maps without validating**. The consequence is a known failure class — a missing/renamed backend field becomes `Invalid Date` or `undefined.map` deep inside a MUI-X chart (cf. the React #185 chart-date incident), caught only by live E2E — made realistic by genuine server/frontend version skew (the reason the clients repo maintains a `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry).

Zod v4 (stable since mid-2025) is the right tool to close this gap: it is the de-facto standard the team's own docs already assume, its maintainer-blessed React Query pattern (`Schema.parse(response.data)` in the `queryFn`) drops cleanly into the existing `BaseApiService`/`ApiError` plumbing, and it *removes* the `any`/`as`/hand-narrowed-`unknown` patterns that the strict-TS and SonarCloud gates dislike. Alternatives were weighed: **Valibot** (~1.4 KB) and `zod/mini` (~1.9 KB) win on bundle but don't justify rewriting all existing Zod guidance; **ArkType** ships a ~40 KB JIT (wrong for a desktop bundle); **Typia** is fastest but **silently emits no validators under stock `tsc`/esbuild** unless a build plugin is wired in — a debugging-hostile risk on Lighthouse's bleeding-edge TS 6.0 / Vite 8 stack; **io-ts** is a poor fp-cultural fit; and the **hand-rolled status quo** doesn't scale and leaves the convention unmet.

**Verdict: ADOPT-INCREMENTALLY (Medium-High confidence).** Install Zod v4, convert the **forecast result** boundary first (highest value + risk, already E2E-exercised), prove a clean `tsc -b`/`vite build`/Biome/Sonar and an acceptable bundle delta on the TS 6.0 / Vite 8 stack as the **gate** before broader rollout, then convert remaining boundaries one service group per slice — deleting each paired interface in favour of `z.infer`, and keeping behaviour-bearing model classes by constructing them *from* the parsed object. Whichever way the go/no-go lands, **the convention docs must change in the same breath** (scope-and-truth the Zod rule for adopt-incrementally; delete it with a rationale for don't-adopt) and the v3-stale instruction examples (`error.errors`, `z.ZodSchema`) must be refreshed to v4 (`error.issues`, `z.ZodType`). A draft ADR is included below. The main open risk is the unverified TS 6.0 / Vite 8 compatibility, deliberately quarantined into the slice-1 spike.

---

## Verdict (up front)

**ADOPT-INCREMENTALLY — Zod v4, highest-risk boundaries first. Confidence: Medium-High.**

Three decisive reasons:
1. **The codebase already chose Zod and pays for the gap.** Four governing documents mandate Zod at trust boundaries; the code does the opposite (unchecked `as`/generic casts). That unmet convention misleads every AI agent and contributor reading it. Adopting incrementally is the only resolution that makes the docs *true* at each step rather than aspirational.
2. **The status quo has a track record of the exact failure Zod prevents.** Runtime shape surprises (e.g. the React #185 chart-date incident; `new Date(undefined)` / `undefined.map` paths in the hand-written deserializers) are an established, E2E-only-caught failure class, made worse by real server/frontend version skew (the clients repo's `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry proves drift is real). `schema.parse` in the `queryFn` converts these into caught, labelled boundary errors.
3. **Cost is bounded and the integration is idiomatic.** React Query's maintainer-blessed pattern is `Schema.parse(response.data)` in the `queryFn`; Zod removes `any`/`as`/hand-narrowed `unknown` (Sonar/strict-TS *friendlier*); v4's type-instantiation cuts even help `tsc`. The one real risk — bleeding-edge **TS 6.0 / Vite 8** vs Zod's tested **TS 5.5+** floor — is exactly why *incremental* (one proving slice before 64 models) beats big-bang.

Why not the other two verdicts: a plain **ADOPT** (big-bang all 64 models) over-commits before the TS-6.0 spike and ignores the behaviour-bearing-model migration cost; **DON'T ADOPT** throws away a safety net the codebase has already been burned without, and still forces a doc rewrite to remove the mandate.

---

## Lighthouse Ground Truth (codebase inspection 2026-06-07)

Verified directly against the repo; cite as "codebase inspection 2026-06-07".

**Toolchain (bleeding-edge majors).** `Lighthouse.Frontend/package.json`: `react ^19.2.6`, `react-dom ^19.2.6`, `typescript ^6.0.3`, `vite ^8.0.14`, `@tanstack/react-query ^5.100.14`, `axios ^1.16.1`, `@biomejs/biome 2.4.16`. Build is `tsc -b && vite build`; `prebuild` runs `biome check --write ./src` (so a clean build implies a clean Biome pass).

**Zod is not installed.** Absent from `package.json`; `grep -r zod src/` returns **0 matches**. The convention is mandated in docs (`CLAUDE.md`, `.github/instructions/frontend-typescript.instructions.md` §"Schema-First Development with Zod", `copilot-instructions.md`, `INSTRUCTIONS_README.md`) but **never implemented**. This is the core contradiction RQ6 must resolve.

**The trust boundary today.** 50 files under `src/services/Api/*.ts` (~30 non-test services). All extend `BaseApiService` (axios). The boundary does **type assertion, not validation**:
- `apiService.post<IManualForecast>(...)` / `.get<IFeature[]>(...)` — the generic is a *compile-time lie*: axios returns `any`/`unknown` cast to the asserted type with **zero runtime checks**.
- Deserialization is hand-written: `ForecastService.deserializeManualForecast` reads `manualForecastData.whenForecasts.map(...)`, `new Date(manualForecastData.targetDate)`, `?? false`, `?? true` — defensive nullish coalescing scattered per-field, no central schema. A missing/renamed field surfaces as `undefined` → `new Date(undefined)` → `Invalid Date` deep in a chart.
- Error-payload parsing (`BaseApiService.parseApiErrorPayload`) narrows `unknown` via a hand-written `data as { message?: unknown; Message?: unknown; errors?: unknown; ... }` cast plus per-field `extractString` guards — exactly the "validate by hand" pattern Zod replaces.

**Models = class + interface pairs via `class-transformer`.** ~64 files under `src/models/`. Pattern (e.g. `Feature`/`IFeature`): an `interface` target type plus a class whose `static fromBackend(data)` calls `plainToInstance(Feature, data)` (`class-transformer` + `reflect-metadata`, with `@Type`/`@Transform` decorators). **Critically, `plainToInstance` maps/transforms but does NOT validate** — it will happily produce a `Feature` with `undefined` where a `number` is declared. So even the "structured" deserialization path provides no runtime shape guarantee.

**Why drift is a real, handled concern here.** Backend is C# .NET, server-authoritative and fairly stable, but DTOs evolve and the project ships frequent calver releases. The separate clients repo maintains a `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry precisely because server/frontend version skew is real (an old backend returns 404 / a stale shape to a new client). A prior incident (chart prop defaulting to `new Date()` → React #185 render loop, MEMORY) confirms runtime-shape surprises are an established failure class in this codebase, caught only by live E2E, not unit tests.

**Quality gates any adoption must survive.** `pnpm build` zero warnings; Biome zero warnings on `./src`; strict TS (`TreatWarningsAsErrors`, no `any`, narrow `unknown` via guards); SonarCloud `new_violations = 0`; bundle size matters (signed standalone Tauri desktop sidecar + web app).

**Migration surface.** ~30 API service files + ~64 models. Models already declare an `interface` as the target type, so `z.infer<typeof FeatureSchema>` could *replace* `IFeature` incrementally with low blast radius — the consuming components import the type name, not the schema.

---

## Findings

### RQ1 — What Zod buys over compile-time types + `as` casts (parse-don't-validate)

**Finding 1.1 — TypeScript types are erased at runtime; `as` casts and `post<T>()` generics are unchecked assertions.** A TypeScript `interface`/`type` and a generic like `apiService.post<IManualForecast>(...)` exist only at compile time. At runtime the value is whatever the network returned. The cast *asserts* a shape the runtime never verifies; if the backend omits or renames a field, the program holds a value that lies about its own type until a downstream access (`new Date(undefined)`, `.map` on `undefined`) throws far from the boundary.
- **Confidence**: High. This is standard TypeScript semantics, documented by Zod's own motivation: "Static types don't tell you anything about runtime behavior... TypeScript types are erased at runtime." Source: [Zod — Intro/Basic usage](https://zod.dev), Accessed 2026-06-07.
- **Verification**: [TanStack Query — Type-safe React Query (TkDodo)](https://tkdodo.eu/blog/type-safe-react-query) — "TypeScript will trust whatever we tell it... `axios.get<Todo>` is an assertion, a lie we tell the compiler" (paraphrased). [MDN/TS handbook on type erasure].

**Finding 1.2 — "Parse, don't validate": validate once at the edge, then trust a precise type everywhere inside.** The principle (Alexis King, 2019) distinguishes a *validator* ("this is fine, continue" — discards what it learned) from a *parser* ("give me a blob; I return a more precise type or an error" — preserves the proof in the type system). Applied to a frontend: parse all incoming data at the very edges (the `queryFn` / API service), fail fast, and pass only trusted, typed structures deeper in. Zod is a parser in exactly this sense — `schema.parse(data)` returns a value whose *static* type is guaranteed to match its *runtime* shape.
- **Evidence**: "A validator says 'this thing is fine, please continue.' A parser says 'give me a blob, and I'll either give you back a more precise type or tell you why I can't.'" / "parse it right away and fail fast... Don't pass incoming data deep into your code."
- **Confidence**: High (canonical principle, 3+ independent expositions).
- **Sources**: [Sylvain Lesage — Parse, don't validate](https://rednegra.net/blog/20250810-parse-dont-validate/), [cekrem.github.io — Parse, Don't Validate in TypeScript](https://cekrem.github.io/posts/parse-dont-validate-typescript/), [Matias Kinnunen — Parsing data is nicer than only validating it](https://mtsknn.fi/blog/parse-dont-just-validate/). All Accessed 2026-06-07. Original concept: Alexis King, "Parse, don't validate" (2019).
- **Analysis (interpretation)**: Lighthouse's `BaseApiService.parseApiErrorPayload` is *hand-rolled parsing* — it already accepts the principle but implements it manually and per-field. Zod would centralize and make that exhaustive.

**Finding 1.3 — What it concretely buys vs the status quo**: (a) the boundary fails *at the boundary* with a structured error (field path + expected vs received) instead of an `Invalid Date`/`undefined.map` crash deep in a chart; (b) `z.infer<typeof S>` becomes the single source of truth so the runtime check and the static type can never drift apart (an `interface` can silently diverge from what `fromBackend` actually produces — `plainToInstance` does not enforce it); (c) coercions/defaults/transforms (snake→camel, string→Date) are declared in one schema instead of scattered `?? false` / `new Date(...)` calls.
- **Confidence**: High (mechanical consequences of 1.1–1.2 plus codebase inspection 2026-06-07).

### RQ2 — Zod v4 status, version, TS 6.x compatibility, bundle/perf

**Finding 2.1 — Zod 4 is stable (GA mid-2025) and is the current major.** zod.dev front page: "💎 Zod 4 is now stable!". InfoQ dated this Aug 2025. The package now ships multiple entrypoints: `zod` (full, method-chaining API), `zod/mini` (functional, tree-shakable), and versioned subpaths (`zod/v3`, `zod/v4`) during the transition.
- **Confidence**: High. Sources: [Zod official docs](https://zod.dev) (Accessed 2026-06-07), [InfoQ — Zod v4 Available](https://www.infoq.com/news/2025/08/zod-v4-available/) (Accessed 2026-06-07).

**Finding 2.2 — TypeScript compatibility: tested against TS 5.5+.** Official docs: "Zod is tested against _TypeScript v5.5_ and later. Older versions may work but are not officially supported." Lighthouse is on **TS 6.0** (`typescript ^6.0.3`). TS 6.0 is *newer* than the tested floor, so it is within the supported "5.5 and later" range — but it is bleeding-edge and **not an explicitly enumerated/CI-tested version on Zod's side**. This is a low-but-nonzero compatibility risk to spike before committing (see Knowledge Gaps).
- **Confidence**: High for the stated floor (official docs, Accessed 2026-06-07); Medium for the "works cleanly on TS 6.0" inference (no source explicitly confirms TS 6.x; needs a local spike).

**Finding 2.3 — Performance vs v3 (large gains).** ~14x faster string parsing, ~7x faster array parsing, ~6.5x faster object parsing. Type-instantiation count drops dramatically (~25,000 → ~175 for the canonical benchmark), which speeds `tsc` in large codebases — relevant given Lighthouse's strict `tsc -b` gate.
- **Confidence**: High (vendor benchmark reported independently by InfoQ). Sources: [InfoQ](https://www.infoq.com/news/2025/08/zod-v4-available/), [Zod release notes](https://zod.dev/v4). Note: parsing speedups are **vendor-reported benchmarks** — treat as directional, not a guarantee for Lighthouse's specific payloads.

**Finding 2.4 — Bundle size: core ~2kb claim vs measured ~11kb; Mini ~1.9kb gzip.** zod.dev front page advertises "Tiny: 2kb core bundle (gzipped)" — this is the *minimal core* tree-shaken path. Independent reporting puts a realistic **standard Zod v4 footprint at ~11–12.8 KB gzipped** for typical usage, ~57% smaller than v3. **`zod/mini` is ~1.9 KB gzipped (~85% / 6.6x smaller than zod@3 core)** using a functional, tree-shakable API (wrapper functions instead of methods, which bundlers can drop).
- **Confidence**: High for Mini ~1.9kb (zod.dev + InfoQ agree); **Medium** for the standard-Zod real-world number — the "2kb" marketing figure and the "~11kb typical" figure are both circulating; the truth is usage-dependent (method-heavy API resists tree-shaking). Sources: [Zod Mini docs](https://zod.dev/packages/mini), [InfoQ](https://www.infoq.com/news/2025/08/zod-v4-available/), [Zod intro](https://zod.dev).
- **Analysis (interpretation)**: For Lighthouse's bundle-sensitive standalone, `zod/mini` is the relevant variant if footprint is the deciding constraint. But note: Lighthouse already ships `class-transformer` + `reflect-metadata` for deserialization; a Zod adoption could eventually *retire* those (`reflect-metadata` ~ a few kb + decorator-metadata transpile cost), making the *net* bundle delta smaller than the gross Zod add — possibly near-neutral if Zod replaces rather than supplements them.

### RQ3 — Concrete pros/cons for Lighthouse given the manual-cast status quo

**Finding 3.1 — The react-query path makes adoption cheap and idiomatic.** Lighthouse uses `@tanstack/react-query 5.100`. The maintainer-endorsed pattern is to call `schema.parse(response.data)` *inside the `queryFn`*, so a shape mismatch puts the query into an `error` state (caught by existing error UI) instead of caching a malformed object that crashes a component later. TkDodo (TanStack Query maintainer): `axios.get<Todo>()` is "a disguised type assertion... you could write `as Todo` instead"; the fix is `todoSchema.parse(response.data)` in the `queryFn`.
- **Confidence**: High. Sources: [TkDodo — Type-safe React Query](https://tkdodo.eu/blog/type-safe-react-query), [Josh Karamuth — Bulletproof Frontend with Zod + React Query](https://joshkaramuth.com/blog/tanstack-zod-dto/), [Noah Falk — Typesafe REST API with React Query and Zod](https://noahflk.com/blog/typesafe-rest-api). Accessed 2026-06-07.
- **Lighthouse fit**: validation belongs in `BaseApiService.withErrorHandling` / the per-service deserializer, which is *already* where the React Query `queryFn`s call into. Replacing `return this.deserializeManualForecast(response.data)` with `return ManualForecastSchema.parse(response.data)` is a near drop-in.

**Finding 3.2 — Real bug classes it prevents here (codebase inspection 2026-06-07).**
- `new Date(manualForecastData.targetDate)` when the field is missing → `Invalid Date` propagating into MUI-X charts. This is the **same failure family** as the documented React #185 incident (a chart fed a bad/unstable date), which unit tests missed and only live E2E caught. A boundary parser turns that into a caught, labelled error.
- `manualForecastData.whenForecasts.map(...)` when `whenForecasts` is absent → `undefined.map` crash. Schema makes the array required (or explicitly optional with a default).
- Server/frontend **version skew** (the very reason the clients repo keeps `FEATURE_REQUIRES_SERVER_NEWER_THAN`): an old backend returning a v(N-1) shape to a new frontend is detected at the edge with a precise field path, not as a mystery render crash.
- The `data as { message?: unknown; Message?: unknown; ... }` error-payload cast becomes a `z.object({...}).safeParse`, removing hand-rolled `extractString`/`extractMessage` guard ladders.

**Finding 3.3 — Cons / ceremony for Lighthouse.**
- **Two-source-of-truth risk during transition**: until a model is migrated, you have both `IFeature` (interface) and a `FeatureSchema`. Mitigation: `z.infer` *replaces* the interface (delete `IFeature`, export `type Feature = z.infer<typeof FeatureSchema>`), so there is one source post-migration. The 64 models already target an interface, so the swap is mechanical.
- **Interaction with `class-transformer`**: models currently use `plainToInstance` + `@Type`/`@Transform` decorators and carry **behaviour** (`Feature.getRemainingWorkForTeam`, etc.). Zod produces plain data, not class instances with methods. So a full migration is not just "schema replaces interface" — the *methods* on model classes (`Feature`, `WhenForecast`, `ManualForecast`) need a home. Options: keep the class but feed its constructor a Zod-parsed plain object (parse → construct), or move methods to free functions. **This is the single biggest real migration cost and is under-appreciated** (the prompt framed models as "interface pairs," but many are behaviour-bearing classes). Confidence: High (codebase inspection 2026-06-07).
- **camelCase boundary**: backend is C# (PascalCase JSON unless configured); repo already depends on `camelcase-keys`. A Zod schema can fold the snake/Pascal→camel transform via `.transform()` or be applied *after* `camelcaseKeys`, but the ordering must be deliberate.

### RQ4 — Cost of adoption (bundle, migration, learning, CI/Biome/Sonar)

**Finding 4.1 — Bundle.** Standard Zod v4 ≈ 11–14 KB gzipped in realistic usage (login-form measurement ~17.7 KB esbuild / ~15 KB rolldown for v3; ~57% smaller for v4 core; method-heavy API limits tree-shaking). `zod/mini` ≈ 1.9 KB gzipped (~6.88 KB for the login-form measurement, esbuild). For Lighthouse's bundle-sensitive standalone, this is **non-trivial but modest**, and **partially offset** because Zod can eventually retire `class-transformer` + `reflect-metadata` (the latter is import-and-decorator-metadata heavy). Net delta is usage-dependent.
- **Confidence**: High for relative ordering; Medium for absolute Lighthouse-specific bytes (no Lighthouse build measured). Sources: [Valibot comparison](https://valibot.dev/guides/comparison/), [InfoQ](https://www.infoq.com/news/2025/08/zod-v4-available/), [zod-vs-valibot bundle repo](https://github.com/anatoo/zod-vs-valibot). Accessed 2026-06-07.

**Finding 4.2 — Migration effort.** ~30 service files + ~64 models. **Not uniform cost**: the cheap part is API services (swap `post<T>` + hand-deserializer for `Schema.parse`). The expensive part is the behaviour-bearing model classes (RQ3.3). A pragmatic incremental path converts **boundaries, not all models at once**: add a schema per endpoint response, parse in the service, keep existing classes constructed *from* the parsed object. Estimated first-boundary slice (one endpoint group, e.g. forecast): small (a single schema file + one service edit + tests). Full sweep: multi-slice, weeks of intermittent work — which argues for incremental, not big-bang.
- **Confidence**: High (codebase inspection 2026-06-07 + standard migration reasoning).

**Finding 4.3 — Learning curve.** Low. Zod's API is the de-facto industry standard for TS validation; the team's own instructions already document the `z.infer` / `z.ZodError` / `z.ZodSchema<T>` patterns, so the *intended* idioms are pre-written. The unfamiliar part is the model-method reconciliation, not Zod itself.
- **Confidence**: Medium-High (instruction files + ecosystem ubiquity).

**Finding 4.4 — CI / Biome / Sonar friction.** Low and favourable. (a) Zod *removes* `any`/`as` casts and hand-narrowed `unknown` — exactly the patterns Sonar and the strict-TS gate dislike — so it should *reduce* new violations, not add them. (b) `z.infer` types are plain TS, Biome-clean. (c) Zod v4's type-instantiation reduction helps the `tsc -b` step rather than hurting it. (d) Risk: adding a dependency on a **bleeding-edge TS 6.0 / Vite 8** stack — Zod tests against TS 5.5+, not explicitly 6.0 (RQ2.2); a build/type spike is required before committing.
- **Confidence**: Medium-High. Sources: codebase CLAUDE.md gates + [Zod docs](https://zod.dev). The TS-6.0 caveat is a genuine open risk (Knowledge Gaps).

### RQ5 — Alternatives with trade-offs (Valibot, ArkType, io-ts, Typia, hand-rolled guards)

**Comparison table** (bundle figures are representative/login-form measurements, not Lighthouse-specific; cite [Valibot comparison](https://valibot.dev/guides/comparison/), [Zod-vs-alternatives 2026 teardown (DEV)](https://dev.to/gabrielanhaia/zod-4-vs-valibot-vs-arktype-a-type-system-teardown-4lha), [InfoQ](https://www.infoq.com/news/2025/08/zod-v4-available/), [Typia docs/setup](https://typia.io/docs/setup/)):

| Option | Bundle (gzip, indicative) | Runtime perf | DX / TS-syntax fit | Ecosystem / maturity | Fit with Vite 8 + react-query + strict TS 6 |
|--------|---------------------------|--------------|--------------------|----------------------|---------------------------------------------|
| **Zod v4** (std) | ~11–14 KB (full); resists tree-shake | Baseline (slowest of the four, still >1M simple validations/s) | Excellent, method-chaining; the de-facto standard the team's docs already assume | Largest by far; most integrations; tested TS 5.5+ | Excellent — `Schema.parse` in `queryFn` is idiomatic; no build plugin |
| **`zod/mini`** | ~1.9 KB (tree-shaken) | Same as Zod v4 | Functional API (wrapper fns) — slightly more verbose | Same package, same ecosystem | Excellent; best Zod choice if bytes dominate |
| **Valibot 1.0** | ~1.4 KB tree-shaken / ~8.7 KB full | ~2x faster than Zod v3; great TTI | Modular functional API; pipe-based; very Zod-adjacent | Growing, smaller than Zod; fewer integrations | Excellent for client bundles; modular design tree-shakes best |
| **ArkType 2.x** | ~40 KB (JIT compiler in bundle) | Fastest (3–4x Zod); server-leaning | 1:1 TS-syntax schemas (define types as strings) — novel, powerful | Smaller ecosystem; "strongest Zod challenger" but gap is real | Large bundle is a poor fit for a bundle-sensitive standalone |
| **io-ts** | small core but needs `fp-ts` | mid | Functional/`Either`-based; steep for a non-fp team | Mature but **declining**; fp-ts ecosystem niche | Poor cultural fit for an OOP/imperative React codebase |
| **Typia** | ~0 runtime lib (compile-time codegen) | Fastest class (generated validators) | Validate from plain TS types, no schema DSL | Capable but **build-coupled** | **Risk**: stock `tsc`/esbuild/tsx **silently emit NO validators**; needs `ts-patch`/`ttsc` or `@ryoppippi/unplugin-typia` wired into Vite. Fragile on bleeding-edge TS 6 / Vite 8 |
| **Hand-rolled guards / thin assert helper** | 0 (zero dep) | n/a | Full control, but every boundary re-implements parsing by hand (status quo's `extractString` ladder) | n/a | Zero risk, zero new dep — but does not scale to 30 services; error messages and exhaustiveness are manual |

**Finding 5.1 — Typia is a poor fit here despite top performance.** Typia's transform runs at compile time and is **silently skipped by stock `tsc`, `tsx`, esbuild** unless `ts-patch`/`ttsc` or an unplugin is correctly installed — "the stock tsc... cannot apply the typia transform, so they will silently produce a build with no validators." On a bleeding-edge **Vite 8 / TS 6.0** stack this is a real, debugging-hostile risk (a green build with no actual validation). Source: [Typia setup docs](https://typia.io/docs/setup/), [unplugin-typia](https://github.com/ryoppippi/unplugin-typia). Confidence: High.

**Finding 5.2 — Valibot is the strongest *bundle*-driven alternative; ArkType the strongest *perf* one.** If the deciding constraint were the standalone's byte budget, Valibot (~1.4 KB) or `zod/mini` (~1.9 KB) beat full Zod. ArkType is fastest but ships a ~40 KB JIT — wrong trade-off for a desktop+web bundle. Confidence: High (3 sources cross-agree on the ordering).

**Finding 5.3 — Status quo (hand-rolled) is defensible only narrowly.** Zero dependency, zero bundle, zero new-TS risk; and the backend *is* server-authoritative and fairly stable. But it does not scale across 30 services, re-implements parsing per boundary (already visible in the error-payload code), and — decisively — **leaves the documented Zod convention unmet**, which is its own liability (RQ6). Confidence: High.

**Why Zod over Valibot for Lighthouse (interpretation).** The team's *own written conventions, instruction files, and Copilot rules already standardize on Zod by name* with worked `z.infer`/`z.ZodError` examples. Choosing Valibot would mean rewriting all that guidance for a marginal byte saving on a desktop app where a ~10 KB delta is not decisive. The convention already picked the tool; the open question is adopt-or-drop, not which library. If bytes later prove critical, `zod/mini` is an in-family escape hatch with no API rewrite of the guidance.

### RQ6 — Resolving the convention contradiction (docs mandate Zod, code never used it)

**The contradiction (codebase inspection 2026-06-07).** Four documents mandate Zod — `CLAUDE.md` ("Schema-first at trust boundaries... using Zod; derive types via `z.infer`"), `.github/instructions/frontend-typescript.instructions.md` (a full "## Schema-First Development with Zod" section with worked `z.object`/`z.infer`/`z.ZodError`/`z.ZodSchema<T>` examples and a `useFetch<T>(url, schema)` helper), `copilot-instructions.md`, and `INSTRUCTIONS_README.md` — yet **zero `zod` imports exist and the package isn't installed**. Every API boundary uses `as`-casts / unchecked generics instead. A stated-but-unfollowed convention is actively harmful: it misleads AI agents (Copilot/Claude are told to "always" use a library that isn't present), misleads new contributors, and erodes trust in the rest of the instruction set.

**Bonus finding — the instructions are already Zod v3-stale.** The instruction file references `error.errors` and `z.ZodSchema<T>`. In **Zod v4** the issue array is `error.issues` (`error.errors` is deprecated/aliased) and `z.ZodType` is the current schema-type (`z.ZodSchema` is a deprecated alias). So *whichever* end-state is chosen, the instruction examples need a v4 refresh or removal. Confidence: Medium-High (Zod v4 changelog; the v3 idioms are visible in the file verbatim).

**Three coherent end-states (leaving it as-is is NOT one):**
- **(a) Adopt Zod, make code match docs.** Install Zod, validate at boundaries, replace interfaces with `z.infer`. Honours the existing convention; highest up-front cost (model-method reconciliation, RQ3.3).
- **(b) Drop/relax the convention to match reality.** Delete the Zod mandate from the four docs, document *why* runtime validation is deemed not worth it here (stable server-authoritative API, bundle sensitivity, existing defensive deserializers), keep hand-rolled guards. Cheapest; but discards a genuinely valuable safety net the codebase has already been bitten by (React #185 shape incident).
- **(c) Adopt incrementally, scope the convention to the highest-risk boundaries first.** Install Zod, convert the highest-value endpoints first, and **rewrite the convention to say "Zod is required at *these* boundary classes; elsewhere is opt-in/roadmap"** so the docs describe the real, growing state rather than an absolute that's unmet. Honours the safety intent, bounds the cost, and ends the contradiction immediately because the doc now matches reality at each step.

**Recommended end-state: (c).** It is the only option that simultaneously (i) closes the doc/code contradiction *now* (the rewritten convention is true after slice 1), (ii) captures the real safety value at the boundaries that have actually hurt (forecast/chart shapes), and (iii) respects the bleeding-edge-stack risk by proving Zod on TS 6.0 / Vite 8 in one slice before a 64-model commitment.

---

## Source Analysis

| Source | Domain | Reputation | Type | Access Date | Cross-verified |
|--------|--------|------------|------|-------------|----------------|
| Zod official docs | zod.dev | High (1.0) | Official | 2026-06-07 | Y |
| Zod v4 release notes / migration | zod.dev | High (1.0) | Official | 2026-06-07 | Y |
| InfoQ — Zod v4 | infoq.com | Medium-High (0.8) | Industry | 2026-06-07 | Y |
| TkDodo — Type-safe React Query | tkdodo.eu | Medium-High (0.8) | Maintainer/practitioner | 2026-06-07 | Y |
| Valibot comparison | valibot.dev | High (1.0, vendor) | Official (competitor) | 2026-06-07 | Y |
| Typia setup docs | typia.io | High (1.0) | Official | 2026-06-07 | Y |
| unplugin-typia | github.com | Medium-High (0.8) | OSS | 2026-06-07 | Y |
| Parse-don't-validate (×3) | rednegra.net / cekrem.github.io / mtsknn.fi | Medium (0.6) | Community | 2026-06-07 | Y (3-source) |
| Zod-vs-alternatives teardown | dev.to | Medium (0.6) | Community | 2026-06-07 | Y |
| Lighthouse codebase | (local) | High (1.0) | Primary | 2026-06-07 | direct inspection |

Reputation: High: 6 | Medium-High: 3 | Medium: 4 (used only with 3-source cross-ref per skill contract) | Avg ≈ 0.83. Excluded-tier domains (blogspot/wordpress/tumblr/quora) were surfaced in results (e.g. a tumblr Zod post) and **not cited**.

## Knowledge Gaps

### Gap 1: Zod on TypeScript 6.0 / Vite 8 — not explicitly verified
**Issue:** Zod officially tests against **TS 5.5+**; no source explicitly confirms clean operation on **TS 6.0** or Vite 8. **Attempted:** zod.dev compatibility statement, v4 changelog. **Recommendation:** make a 30-minute local spike (install `zod`, one schema, `tsc -b`/`vite build`/Biome) the gate for slice 1 before any broader commitment. This is the single highest-leverage unknown.

### Gap 2: Lighthouse-specific bundle delta unmeasured
**Issue:** Bundle figures are indicative login-form measurements, not Lighthouse's actual standalone delta; the offset from retiring `class-transformer`/`reflect-metadata` is also unquantified. **Attempted:** Valibot comparison, InfoQ, bundle-comparison repo. **Recommendation:** measure `vite build` size before/after slice 1; if the delta is unacceptable, switch the import to `zod/mini` (no API/guidance rewrite).

### Gap 3: class-transformer ↔ Zod coexistence specifics
**Issue:** No external source addresses migrating `class-transformer` behaviour-bearing models to Zod specifically; the recommended "parse → construct class" pattern is reasoned from codebase inspection, not a cited precedent. **Recommendation:** validate the pattern empirically in slice 1 (forecast models carry methods).

### Gap 4: standard-Zod-v4 absolute gzip size
**Issue:** Marketing "2kb core" vs reported "~11–14 KB typical" both circulate; the true number is usage-dependent. Rated Medium confidence. **Recommendation:** rely on the slice-1 measurement, not the published figure.

## Recommendation & Decisive Reasons

See **Verdict** above for the one-line call and the three decisive reasons. This section adds the *how*.

### First boundary to convert: the **forecast result** path

Pick **forecast results** (`ForecastService` → `ManualForecast` / `WhenForecast` / `BacktestResult`) as slice 1. Rationale: it is the **highest-value + highest-risk** boundary — it feeds the MUI-X charts that have already produced runtime shape incidents (date handling, `.map` over forecast arrays), it has dense `new Date(...)` / `?? false` / `?? true` defensive code that a schema cleanly subsumes, and it is self-contained enough to prove the TS-6.0/Vite-8 spike on a small surface before committing to 64 models.

(Licensing and auth/RBAC summary are strong *second* candidates — security-adjacent boundaries — but forecast wins slice 1 on proven blast-radius and demo/E2E coverage that already exercises it.)

### The `z.infer`-replaces-interface pattern, concretely against `BaseApiService`

Today (`WhenForecast.ts` + `ForecastService.ts`):
```ts
// interface asserted, plainToInstance maps but does NOT validate
export interface IWhenForecast extends IForecast { expectedDate: Date; /* ... */ }
const whenForecasts = data.whenForecasts.map(f =>
  WhenForecast.new(f.probability, new Date(f.expectedDate)));   // new Date(undefined) risk
```

After (schema is the single source of truth; `z.infer` replaces the interface):
```ts
// models/Forecasts/WhenForecast.ts
import { z } from "zod";
export const WhenForecastSchema = z.object({
  probability: z.number(),
  expectedDate: z.coerce.date(),          // string|Date -> Date, FAILS on garbage
  filterApplied: z.boolean().optional(),
  excludedSummary: z.string().optional(),
});
export type WhenForecast = z.infer<typeof WhenForecastSchema>;   // replaces IWhenForecast
```
```ts
// services/Api/BaseApiService.ts — add one helper
protected static parse<T>(schema: z.ZodType<T>, data: unknown): T {
  const result = schema.safeParse(data);
  if (!result.success) {
    throw new ApiError("SHAPE", "Unexpected response from server",
      z.prettifyError(result.error));   // structured field-path detail in technicalDetails
  }
  return result.data;
}
```
```ts
// services/Api/ForecastService.ts — queryFn-side parse replaces hand deserializer
const response = await this.apiService.post<unknown>(`/forecast/manual/${teamId}`, body);
return BaseApiService.parse(ManualForecastSchema, response.data);   // was deserializeManualForecast
```

Key points: (a) the axios generic becomes `<unknown>` — honest about the boundary; (b) `safeParse` + the existing `ApiError`/`withErrorHandling` plumbing means a shape mismatch flows into the *existing* error UI and React Query error state, no new surface; (c) **behaviour-bearing models** (`Feature`, `ManualForecast`) keep their methods — construct the class *from* the parsed plain object (`new ManualForecast(parsed.remainingItems, parsed.targetDate, …)`), so Zod owns validation and the class owns behaviour; (d) `IWhenForecast`/`IFeature` are deleted in favour of the inferred type, ending the interface-vs-runtime drift.

### Sequenced plan
1. **Slice 1 (spike + first boundary)**: install `zod` (start with full `zod`; keep `zod/mini` as the escape hatch if a bundle measurement demands it), add `BaseApiService.parse`, convert the forecast path, measure the bundle delta and confirm clean `tsc -b`/`vite build`/Biome/Sonar on TS 6.0 / Vite 8. **Gate the rest on this slice being green.**
2. **Slice 2+**: convert remaining high-risk boundaries (licensing, auth/RBAC summary, work-item/feature lists), one service group per slice, deleting the paired interface each time.
3. **Convention update lands with slice 1** (see next section) so the docs never again describe an unmet rule.
4. Defer/avoid touching the ~64 internal/derived models that aren't trust boundaries — per the team's own rule, plain types are fine for internal data.

## What Changes in the Convention Docs (required regardless of verdict)

Because leaving the stated-but-unfollowed convention in place is not acceptable, **one of these doc edits ships no matter what**:

- **If ADOPT-INCREMENTALLY (recommended):** rewrite the Zod sections in `CLAUDE.md` and `.github/instructions/frontend-typescript.instructions.md` (and the two Copilot/README mirrors) to be **scoped and truthful**: "Runtime schema validation with Zod is **required at converted API boundaries** (currently: forecast; expanding per the zod-adoption roadmap). New API boundaries MUST be schema-validated. Internal/derived data uses plain types." Add: validation lives in the `queryFn`/service via `Schema.parse`/`safeParse`; derive types with `z.infer`; **fix the v3-stale examples** — `error.issues` not `error.errors`, `z.ZodType` not `z.ZodSchema`.
- **If DON'T ADOPT:** **delete** the "Schema-First Development with Zod" section and every Zod mandate from all four documents, and add a one-paragraph rationale ("runtime validation intentionally not used; the API is server-authoritative and boundaries use hand-written guards") so future agents/contributors stop being told to use an absent library.

Either way, the `useFetch<T>(url, schema: z.ZodSchema<T>)` example helper in the instruction file should be reconciled with the **React Query** reality (the app fetches via react-query `queryFn`s, not a bespoke `useFetch`), to avoid prescribing a pattern the codebase doesn't use.

## Draft ADR (adopt-incrementally)

> Drop into `docs/product/architecture/` as the next ADR number (e.g. ADR-059; confirm the current max before assigning).

---
### ADR-0NN: Runtime schema validation at frontend trust boundaries with Zod (incremental)

**Status:** Proposed — 2026-06-07 (ADO Story 5232)

**Context.**
The React/TypeScript frontend (`Lighthouse.Frontend/`, React 19.2 / TS 6.0 / Vite 8 / react-query 5.100) crosses a trust boundary at ~30 axios API services. Today that boundary does **type assertion, not validation**: `apiService.post<IFeature[]>(...)` and `data as {...}` casts assert shapes the runtime never checks, and `class-transformer`'s `plainToInstance` maps without validating. Backend DTOs evolve across frequent calver releases and server/frontend version skew is a real, separately-handled concern (`FEATURE_REQUIRES_SERVER_NEWER_THAN` in the clients repo). The result: a missing/renamed field surfaces as `Invalid Date` / `undefined.map` deep inside MUI-X charts (cf. the React #185 chart-date incident), caught only by live E2E. Separately, our own conventions (`CLAUDE.md`, `frontend-typescript.instructions.md`, Copilot rules) **already mandate Zod** at trust boundaries, but Zod is not installed and is used nowhere — a stated-but-unfollowed rule that misleads agents and contributors.

**Decision.**
Adopt **Zod v4** for runtime validation at frontend trust boundaries, **incrementally**, starting with the **forecast result** boundary. Validation runs in the service/`queryFn` via `safeParse`, routed through the existing `ApiError`/`withErrorHandling` plumbing so shape failures become normal React Query error states. Types are derived with `z.infer`, **replacing** the paired hand-written interfaces (`IFeature`, `IWhenForecast`, …). Behaviour-bearing model classes are retained but constructed *from* the parsed plain object. Remaining boundaries convert one service group per slice; the convention docs are rewritten in the same change to describe the real, growing state (not an absolute). A first slice must prove clean `tsc -b`/`vite build`/Biome/Sonar and an acceptable bundle delta on the bleeding-edge TS 6.0 / Vite 8 stack before further slices proceed.

**Consequences.**
- *Positive:* runtime drift fails fast at the boundary with field-path detail; one source of truth (`z.infer`) eliminates interface-vs-runtime drift; removes `any`/`as`/hand-narrowed `unknown` (Sonar/strict-TS friendlier); v4 type-instantiation cuts help `tsc`; the doc/code contradiction is resolved at each step; defensive `?? false`/`new Date(...)` ladders consolidate into schemas.
- *Negative / costs:* a new dependency (~11–14 KB gzip full Zod; `zod/mini` ~1.9 KB available if bytes bind) on a bundle-sensitive standalone; migrating behaviour-bearing model classes is more than an interface swap; transitional coexistence of converted and unconverted boundaries; a real (if low) compatibility risk on TS 6.0 vs Zod's tested TS 5.5+ floor, mitigated by the gating spike.
- *Neutral:* `class-transformer` + `reflect-metadata` may eventually be retired as schemas take over deserialization, partially offsetting the bundle add.

**Alternatives considered.**
- **Valibot** (~1.4 KB tree-shaken, modular): best bundle/TTI, but the team's entire written convention names Zod with worked examples; switching buys ~10 KB on a desktop app while invalidating all existing guidance. `zod/mini` is the in-family bundle escape hatch.
- **ArkType** (1:1 TS-syntax, fastest): ~40 KB JIT in-bundle — wrong trade-off for desktop+web.
- **Typia** (compile-time, fastest validators, ~0 runtime): **silently emits no validators under stock `tsc`/esbuild/tsx**; needs `ts-patch`/`ttsc` or an unplugin wired into Vite — debugging-hostile on bleeding-edge TS 6 / Vite 8.
- **io-ts** (`fp-ts`): functional/`Either` model is a poor cultural fit for this OOP/imperative React codebase; declining ecosystem.
- **Hand-rolled guards (status quo):** zero-dep and defensible for a stable server-authoritative API, but doesn't scale across 30 services, re-implements parsing per boundary, and leaves the documented Zod convention unmet.
---

## Full Citations

[1] Colin McDonnell et al. "Zod — Intro / Basic usage / Packages." zod.dev. (v4 stable). https://zod.dev. Accessed 2026-06-07.
[2] Colin McDonnell et al. "Zod 4 — Release notes." zod.dev. https://zod.dev/v4. Accessed 2026-06-07.
[3] Colin McDonnell et al. "Zod Mini." zod.dev. https://zod.dev/packages/mini. Accessed 2026-06-07.
[4] InfoQ. "Zod v4 Available with Major Performance Improvements and Introduction of Zod Mini." 2025-08. https://www.infoq.com/news/2025/08/zod-v4-available/. Accessed 2026-06-07.
[5] Dominik Dorfmeister (TkDodo, TanStack Query maintainer). "Type-safe React Query." tkdodo.eu. https://tkdodo.eu/blog/type-safe-react-query. Accessed 2026-06-07.
[6] Josh Karamuth. "Stop Trusting Your API: Bulletproof Frontend with Zod and React Query." joshkaramuth.com. https://joshkaramuth.com/blog/tanstack-zod-dto/. Accessed 2026-06-07.
[7] Noah Falk. "Making a REST API typesafe with React Query and Zod." noahflk.com. https://noahflk.com/blog/typesafe-rest-api. Accessed 2026-06-07.
[8] Sylvain Lesage. "Parse, don't validate." rednegra.net. 2025-08-10. https://rednegra.net/blog/20250810-parse-dont-validate/. Accessed 2026-06-07.
[9] cekrem. "Parse, Don't Validate — In a Language That Doesn't Want You To (TypeScript)." cekrem.github.io. https://cekrem.github.io/posts/parse-dont-validate-typescript/. Accessed 2026-06-07.
[10] Matias Kinnunen. "Parsing data is nicer than only validating it." mtsknn.fi. https://mtsknn.fi/blog/parse-dont-just-validate/. Accessed 2026-06-07.
[11] Alexis King. "Parse, don't validate." (2019, original concept). lexi-lambda.github.io. Accessed via secondary expositions 2026-06-07.
[12] Valibot. "Comparison (vs Zod / ArkType)." valibot.dev. https://valibot.dev/guides/comparison/. Accessed 2026-06-07.
[13] Gabriel Anhaia. "Zod 4 vs Valibot vs ArkType: A Type-System Teardown." dev.to. https://dev.to/gabrielanhaia/zod-4-vs-valibot-vs-arktype-a-type-system-teardown-4lha. Accessed 2026-06-07.
[14] anatoo. "zod-vs-valibot: bundle size comparison." github.com. https://github.com/anatoo/zod-vs-valibot. Accessed 2026-06-07.
[15] Samchon. "Typia — Guide Documents > Setup." typia.io. https://typia.io/docs/setup/. Accessed 2026-06-07.
[16] ryoppippi. "unplugin-typia." github.com. https://github.com/ryoppippi/unplugin-typia. Accessed 2026-06-07.
[17] Lighthouse codebase inspection — `Lighthouse.Frontend/package.json`, `src/services/Api/BaseApiService.ts`, `ForecastService.ts`, `src/models/Feature.ts`, `WhenForecast.ts`, `.github/instructions/frontend-typescript.instructions.md`, `CLAUDE.md`. 2026-06-07.
