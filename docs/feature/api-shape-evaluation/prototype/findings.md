# Prototype findings: generate consumers from the OpenAPI document

Date: 2026-11-27. Spike, not production work. Nothing was committed. All backend changes were made in throwaway
git worktrees under the scratchpad and removed. The frontend change (a temporary `src/generated-prototype/`
folder) was removed. The clients repository was not touched: the replay ran on a `git archive` copy.
(Written by the prototype agent; saved by the host after checking the logs it cites.)

Generator: **orval 8.48.0**. One tool gives the TS types, zod 4 schemas, axios functions and MSW mocks with Faker
data, so one configuration answers all the questions. hey-api was not tried: the research (§7) marks it "in
initial development, pin exact version", and orval did not fail. Configuration: `orval.config.mjs` (frontend) and
`orval.clients.config.mjs` (clients).

## Folder map

| Path | What |
|---|---|
| `openapi/current.json` | Document of the running instance (HEAD 5c26f30ee) |
| `openapi/fixed-annotations.json` | Same code + 3 Swashbuckle settings + 1 schema filter (scratch build) |
| `openapi/bug6254-before.json`, `bug6254-after.json` | Built and run at 3271ac3c6 (= 8c0b99756^) and 8c0b99756 |
| `backend-fix/swagger-settings.SCRATCH.md` | The backend fix that was tried, not kept |
| `scripts/` | Every step, re-runnable (`validate-live.mjs`, `generate.sh`, `oasdiff-replay.sh`, `clients-replay.sh`, …) |
| `results/` | Logs and reports quoted below |
| `generated-samples/` | A few generated files, from both documents |

## Q1. Is the OpenAPI document exact?

**Method.** `scripts/validate-live.mjs` calls every GET in scope on the running instance (team 1, portfolio 1,
range 2026-08-29 … 2026-11-27) and checks each real answer with the generated zod schema and with a strict walk of
the OpenAPI schema (undeclared properties, null where not nullable, enum values, date format, properties always
present but not "required"). Scope: **38** GET operations under `/teams/{teamId}/metrics/*` plus
`GET /v1/deliveries/portfolio/{id}`. Demo data had no Delivery, so one was created and archived (with the test
premium licence imported into the scratch instance's database) so that both `active` and `archived` hold a row.

**Result with today's document** (`results/validate-current.log`): 39 of 39 answered 200; **36 of 39 pass** the
generated zod.

| Area | Observed | Fixable how |
|---|---|---|
| Missing schemas | None in scope. Whole document: 6 GETs without a body schema (login, oauth callback, embed start, backup and log downloads, `Version/hasupdate`). The files are not JSON; `hasupdate` needs one `ProducesResponseType` | Annotation, where needed |
| Enums | 69 enum schemas, all `type: string`; every enum value in the answers matched. The "numbers in" side does not show in the document, which is fine for consumers | Nothing to fix |
| Dates | Every `date-time` value was ISO with `Z`. `recordedAt` (a day) is a plain string on both sides | Nothing to fix |
| **Nullable references** | **All 3 failures, one cause**: Swashbuckle cannot mark a nullable `$ref` as nullable: `FeatureDto.estimate` (null in `allFeaturesForSizeChart`, `featuresInProgress`) and the enum `DeliveryWithLikelihoodDto.sourceUnavailableReason` | Setting: `UseAllOfToExtendReferenceSchemas()` |
| **Required** | Only 21 of 285 schemas list `required`; 274 of 289 observed properties were always present but optional, so **every generated field is optional** | Schema filter (~45 lines): what System.Text.Json always writes is required |
| Nullable too wide | 630 of 1,180 properties are `nullable` (C# nullable annotations not read) | Setting: `SupportNonNullableReferenceTypes()` |
| Polymorphism | None; no undeclared property in any answer | — |
| A string that is really an enum | `comparison.direction` is a C# `string`; the frontend types it `"up" \| "down" \| "flat" \| "none"` | Small code change: a C# enum (not an annotation) |
| Query parameters | Dates declared `date-time`; the frontend sends `yyyy-MM-dd` | `DateOnly` or a format override; or do not use generated parameter schemas |
| Three media types | Each answer listed as `text/plain`, `application/json`, `text/json`; every type and oasdiff finding tripled | JSON-only output formatter, or `[Produces("application/json")]` |
| `/latest` | `LatestRouteFilter` removes `/api/latest/*`; the document describes `/api/v1/*` only. Same shapes, different URLs | Use generated types + zod, keep the frontend's own axios calls |

**Result with the fixed document** (3 settings + 1 filter, scratch build, same answers,
`results/validate-fixed-annotations.log`): **39 of 39 pass**, strict walk 0 issues, 0 properties always present
but optional; `required` on 196 of 285 schemas; `nullable` from 630 down to 335. 56 properties are nullable but
were never null in demo data; demo data cannot tell whether that is right.

## Q2. Does generated code build clean in the frontend?

A temporary `Lighthouse.Frontend/src/generated-prototype/` with a throwaway service (throughputInfo,
cycleTimePercentiles, blockedCountHistory, written the way `BaseApiService.parse` is used today); `pnpm exec tsc -b`
and Biome; folder removed afterwards.

- **Generation time:** 1.4–3.3 s for the whole document (220 paths).
- **Size, whole document:** zod 49 files / 7,975 lines / 286 KB; types 382 files / 6,178 lines; axios functions
  7,116 lines; MSW mocks 5,510 lines / **1.09 MB**. Team metrics zod: 1,026 lines, 28.5 KB minified, **3.1 KB gzip**.
  Full `vite build` delta not measured (`results/sizes.log`).
- **tsc, today's document** (`results/tsc-frontend-current-doc.log`): generated zod, types and axios functions
  compile under TS 7 strict. MSW files fail only on missing `msw` and `@faker-js/faker`. The throwaway service fails
  3 of 3: all-optional generated types are not assignable to the hand-written ones.
- **tsc, fixed document** (`results/tsc-frontend-fixed-doc.log`): 2 of 3 calls compile against the existing
  hand-written types unchanged. The third shows two real gaps: `direction` is `string` (Q1), and the hand-written
  `IInfoWidgetComparison` says `currentLabel?: string` while the backend sends `null`. There **the generated type is
  right and the hand-written type is wrong**.
- **Dates:** `useDates: true` makes orval emit `zod.coerce.date()`, the frontend's own convention.
- **Biome** (`results/biome-generated.log`): zod and types lint-clean, formatting differs; `prebuild` rewrote all
  435 files. MSW mocks keep 40 errors. So either let `prebuild` reformat (then the regeneration check formats before
  comparing), or exclude the generated folder from Biome and Sonar. Sonar not run; generated code needs a
  `sonar.exclusions` entry either way.

## Q3. Would it have caught Bug #6254?

Backend built and run at 3271ac3c6 (8c0b99756^) and at 8c0b99756 in scratch worktrees; both documents saved, not
reconstructed.

- **(a) Backend CI diff.** `oasdiff breaking before after --fail-on ERR` exits 1 with
  `response-body-type-changed … from 'array' to 'object' for status '200'` on
  `GET /api/v1/Deliveries/portfolio/{portfolioId}` (3 errors, one per media type;
  `results/oasdiff-bug6254-breaking.txt`).
- **(b) Clients.** A `git archive` of lighthouse-clients at 4e76513^ with one change: `listDeliveries` reads the
  answer as the generated type instead of a `readonly unknown[]` cast. Generated from the before-document:
  `tsc -b` exit 0. From the after-document: **exit 2**, errors in `mcp-stdio/src/localServer.ts` and
  `mcp-http/src/bin.ts` (`PortfolioDeliveriesDto` "is missing … length, concat, join, slice";
  `results/clients-tsc-*.log`). The CLI did **not** fail: it passes the answer as `unknown` to renderers that check
  at run time. tsc only catches a reshape where a typed consumer reads the answer.
- **Frontend side:** not replayed.
- **The same gate over the last three months** (`results/oasdiff-aug-to-now.txt`) also flags
  `POST Forecast/update-portfolios-for-team/{teamId}` (`boolean` → `integer`) and the removed `GET /api/oauth/health`,
  and warns on 8 removed answer properties, which become errors once `required` is set.
- **One-off cost:** the annotation change itself reports 1,275 "errors"
  (`results/oasdiff-current-to-fixed.summary.txt`); the commit that introduces it sets the baseline.

## Q4. How does the document reach the clients' repository?

1. **Backend CI** builds, starts the server on an empty SQLite file and saves `/swagger/v1/swagger.json`
   (demonstrated by `scripts/run-and-fetch-doc.sh`, ~30 s); compares it with a committed
   `Lighthouse.Backend/openapi/v1.json` (difference fails: "regenerate and commit"); runs `oasdiff breaking` against
   `main`'s committed copy (demonstrated). A breaking change is then a deliberate, reviewed edit of the committed file.
2. **Release:** `ci_release.yml` attaches `openapi.json` as one more release asset. Not demonstrated.
3. **Clients:** commit the document under `openapi/`; a script runs `gh release download --pattern openapi.json`
   and regenerates; the existing `pnpm ci` typechecks; a weekly job opens a PR. Generation and the failing typecheck
   were demonstrated; the download was not.

Rejected for now: an npm package of types (a second release train); fetching from ghcr `dev-latest` (pulls an
unreleased server).

**Version gates.** Generated types describe one server version; the hand-written gates stay (ADR-055 table).
Generation could type an answer as the union of the oldest supported and the newest released document, and
`oasdiff` between them lists the routes that need a gate. Not demonstrated.

## Q5. What does it cost to adopt?

- **Backend, once:** 3 settings and a ~45-line filter; JSON-only output; string-typed enums to C# enums (1 found in
  scope, count unknown); a CI step (script + ~40 lines YAML) and one baseline commit of the document.
- **Frontend, per service:** `get<unknown>` + `BaseApiService.parse(Generated…Response, data)`, delete the
  hand-written interface, fix consumers the old type lied to (null vs undefined, narrowed unions). Team › Metrics:
  `MetricsService.ts` 996 lines + `models/Metrics` 445 lines; roughly 1–2 days with tests; ~30 services, one at a
  time.
- **Stays hand-written:** `BaseApiService` (axios, `/latest`, error mapping, HTML guard); behaviour-bearing classes
  built from the parsed answer; the lenient `Team`/`Portfolio` schemas until calibrated; widget-only fields; the
  clients' version gates and wording.
- **Tests:** the frontend has no MSW; tests use `vi.mock("axios")` and hand fakes. Generated mocks would add two
  devDependencies, a new test style, 1 MB of code and Biome exclusions. Do not adopt them now.
- **Clients:** 73 `requestJson<…>` calls, 42 cast to `unknown`/`unknown[]`, each switches to a generated type.
- **CI steps:** backend doc extraction, up-to-date check and oasdiff; frontend "generated folder up to date";
  clients scheduled fetch and regenerate.

**Against the zod-adoption evaluation.** Kept: zod 4, `BaseApiService.parse`, parse → construct,
`z.coerce.date()`, "trust boundaries only". Contradicted: that roadmap writes each schema by hand and makes
`z.infer` the source of truth; generation moves the source of truth to the backend. Its open item "MetricsService
feature projections need their own schema" is covered by generation.

## Verdicts

- **(a) Document exact enough, or fixable with small backend annotations: YES for the tested scope.** 36/39 →
  39/39 with three settings and one filter; generated types then as strict as the hand-written ones. Plus one small
  code change per string that is really an enum. Not tested outside the scope.
- **(b) A simple path to the clients exists: YES.** Committed document + release asset + scheduled
  regenerate-and-typecheck. Gates stay hand-written.

## What the ADR can state

- Keep REST. The Swashbuckle document is the contract, with `SupportNonNullableReferenceTypes`,
  `UseAllOfToExtendReferenceSchemas`, an "always written is required" filter, and JSON-only answers.
- The document is committed, checked for being up to date, and diffed with oasdiff in CI; an ERR-level change fails
  unless the committed document is changed on purpose in the same commit.
- Generator: orval (pinned), zod 4 output with `useDates`, and types. No generated mocks and no generated functions
  in the frontend for now.
- Frontend services move one at a time: generated schema at the boundary, behaviour-bearing classes stay.
- The document is a release asset; the clients regenerate from released documents; version gates stay hand-written.

## Follow-up items this would need (none created in ADO)

1. Backend: the three settings and the filter, JSON-only output, the `hasupdate` response type; baseline commit.
2. Backend: string-typed enums to C# enums (start with `InfoWidgetComparison.direction`); find the others.
3. Backend: describe date-only query parameters as dates.
4. CI: document extraction, up-to-date check, oasdiff gate; decide how a deliberate break is accepted.
5. Release: attach `openapi.json`.
6. Frontend: generation script, Biome/Sonar handling of the generated folder, then MetricsService as the first
   service.
7. Clients: fetch + regenerate job; move `listDeliveries` and the other 41 `unknown` casts to generated types;
   decide the retained-document set for gates.

## Limitations

- Demo data, one team, one portfolio; default query values. The scratch instance on :5431 now also holds one
  active and one archived Delivery and the test premium licence.
- Only answers were validated; request bodies and parameters were not.
- The fixed document was validated only on the 39 operations in scope.
- Not measured: the `vite build` bundle delta, Sonar on generated code.
- Not demonstrated: the frontend part of the Bug #6254 replay, the release-asset upload and download, the
  version-union idea.
