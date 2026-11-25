# Story 6135 — Upgrade Lighthouse to .NET 11

ADO: [6135](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6135) (User Story, tag
`Continuous Improvement`, no parent Epic). Product authority: `handoff.json` → `brief.md`.

## Wave: DISCUSS

### Maintainer decisions

- 2026-11-25 — **Skip DISCOVER and DIVERGE**, said in as many words. There is no problem to validate (the
  platform moves on) and only one direction: retarget everything in one change.
- 2026-11-25 — **DISCUSS runs in final-review mode.** The Product Owner works from the ADO scope; the
  maintainer reviews the brief once.
- 2026-11-25 — **The work item gets the `Release Notes` tag.** `/release` writes one short line, such as
  "Lighthouse now runs on .NET 11". Self-hosters see a new base image (`aspnet:11.0`) and a new runtime
  version in System Info, and should be told why.

- 2026-11-25 — **Brief accepted in final review.** The four Proposed product decisions are now Confirmed.
  Go on to DESIGN and hold when it is done.

### Preconditions (checked 2026-11-25)

- .NET 11.0.0 generally available since 2026-11-10, SDK 11.0.100 (`dotnetcli` release metadata).
- NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0, `Microsoft.EntityFrameworkCore` 11.0.0,
  `Microsoft.AspNetCore.*` 11.0.0.
- The Renovate hold is still in `renovate.json` (the `allowedVersions: "<11"` rule).

### Scenario exploration

No value has a sad path (refusal, conflict, race, stale input), so no Quint exploration. Each value is a
settled happy path: the app behaves as before on .NET 11; Renovate offers .NET updates again; the Docker
image and standalone builds run on .NET 11.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | N/A, because the change adds no action, page or data. |
| Lighthouse-Clients CLI / MCP | N/A, because no API route or DTO changes. The clients talk HTTP and do not care which runtime serves it. |
| Website marketing surface | N/A, because this is not a capability. The release note is the only public mention. |
| Context map | Touches no bounded context's model. No new glossary terms, no domain events, no UI surface. Platform and hosting only. ADR-270 does not change. `ARCHITECTURE.md` changes only where it names the runtime (".NET 10" → ".NET 11"). |
| `/continuous-improvement before` | N/A, because that check is for a new feature Epic. 6135 is itself a Continuous Improvement item. |

### Notes for DESIGN

- `.NET 10` references outside history: 4 csproj files, `Lighthouse.sln`, `.github/actions/build-backend/action.yml`
  (+ its README), `ci_package-*-standalone.yml`, `ci_sbom.yml`, `ci_sonar_gates.yml`,
  `.github/scripts/toolchain-pins.test.mjs`, `Dockerfile`, `Start-DevServer.ps1`,
  `Lighthouse.EndToEndTests/perf/readings.spec.ts` (+ README), `Scripts/examine/test-platform-answers-compare.sh`,
  the generated `public/sbom/backend.cdx.json`, `ARCHITECTURE.md`, `CLAUDE.md`, and two user docs:
  `docs/Installation/configuration.md:51` and `docs/compliance/cra-technical-file.md:153`.
- History stays as written: feature briefs in `docs/product/architecture/brief.md`, `c4-diagrams.md`, ADRs.
- System Info reads the runtime version at run time; the ".NET 10.0.x" strings in its tests are fixtures.
- Out of scope, possible follow-up: new .NET 11 APIs, such as a first-class cookie `Partitioned` property
  that `Program.cs` works around today.

## Wave: DESIGN

Closed accepted 2026-11-25 (solution architect, final-review mode). Authority: `handoff.json` -> `architecture.md`.

- Push point: commit S1-S4 locally in order, push once after S4 (S1 alone is red in CI on SDK 10.0.404).
- No `global.json`: CI pins the SDK through setup-dotnet (Renovate keeps it current); a second pin would drift.
- Added target found in DESIGN: `dotnet-ef` 10.0.15 -> 11.0.0 in `Lighthouse.Backend/.config/dotnet-tools.json` (rollForward false).
- No ADR, no formal model (no state machine or temporal law), prefactoring N/A in every slice.
- Residual risks: EF/Npgsql 11 pending model change (crafter stops, back to architect); new analyzer warnings under TreatWarningsAsErrors (fix or narrow suppression); dotnet-stryker 5.1.0 on net11.0 unverified; Docker and standalone builds are not required checks.
- Local toolchain: SDK 11.0.100 and ASP.NET Core 11.0.0 installed 2026-11-25 beside 10.0.114.

## Wave: DEVOPS

Closed 2026-11-25. Platform lens run (risk is material: base image, packaging, CI pins); its facts are recorded in the design authority (`architecture.md`).

- **Usage-data event: N/A**, because the upgrade is behaviour-free and adds nothing a user can do.
- Base image: Ubuntu 24.04 (noble) -> 26.04 (resolute). UID 1654, ports and Kestrel overrides unchanged; compose and Helm values keep working. PGDG `resolute-pgdg` exists (HTTP 200, probed 2026-11-25).
- Added targets: `Scripts/examine` Mvc.Testing pins 10.0.14 -> 11.0.0 (S1); unused `dotnet_version` input removed from `ci_packageapp.yml` (S2). Renovate: delete only the hold rule; the manual-major rule stays (S4).
- Post-push only (not required checks): Docker build + health check, standalone packaging, codesign, verifywindows/verifymacos, SBOM. Highest risk: macOS rpath/codesign/notarise step. The release job needs these, so a failed build publishes nothing; remedy is a forward fix or revert.
- Rollback for self-hosters: no migration planned, key-ring format unchanged across majors — designed, not observed.
- Support: .NET 10 LTS and .NET 11 STS both end 2028-11-14; no support time lost.

## Wave: DELIVER (AFK)

Rigor: S1 thorough, S2-S4 standard. Slice wrap-up: refactor + review. Push: never (hold locally).

### Decisions taken during DELIVER

- S1: `Microsoft.Extensions.TimeProvider.Testing` 10.11.0 -> 11.0.1 (oracle review finding: it follows the .NET 11 train). Accepted extension of the S1 impact list.
- S1 refactor: removed the direct `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5 pin. EF Core Sqlite 11.0.0 depends on SQLitePCLRaw 3.0.5 itself, above the CVE-2025-6965 fix (2.1.12); the resolved set stays 3.0.5. Three comments that still called it a pin now describe the engine fix instead. Accepted change to the design line "SQLitePCLRaw stays".
- S1: EF Core 11 obsoletes `ExecuteSqlInterpolatedAsync`; two test set-up helpers use `ExecuteSqlAsync` (same parameterised SQL, no assertion changed).
- S2: the design's two `git grep` falsifiers also match the oracle's own fixtures. The host runs them with `:!.github/scripts/toolchain-pins.test.mjs` excluded; the node test is the real oracle. Oracle-review findings 2 and 3 (renamed-input and unscanned-folder cases) stay as known, non-blocking gaps.
- S3 oracle review F1 (standalone builds have no runtime-field check): accepted as residual. They are self-contained `net11.0` publishes made with SDK 11.0.100 in `build-backend`, so they bundle the .NET 11 runtime by construction; the post-push package and verify workflows build and start them. Adding a runtime check to each standalone workflow is out of this behaviour-free scope. F2 (failure message cause) and F3 (`.NET 11"*` would also match `.NET 110`) stay non-blocking.
- S3 local Docker check: `buildx` is not installed, so the host builds a copy of the Dockerfile with only the `--platform=$X` flags stripped. At HEAD before S3 the build fails with NETSDK1045 (SDK 10 cannot target net11.0).
