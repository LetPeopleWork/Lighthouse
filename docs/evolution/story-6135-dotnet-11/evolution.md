# Story 6135 — Lighthouse runs on .NET 11

Delivered 2026-11-25. ADO #6135 (tags `Continuous Improvement`, `Release Notes`). Workspace:
`docs/feature/story-6135-dotnet-11/`. Delivered in AFK mode; no ADR (moving to the current runtime is no lasting
decision).

## What was delivered

The backend moves from .NET 10 to .NET 11 in one change, with no change in behaviour: same routes, responses,
screens and data. .NET 11.0.0 has been generally available since 2026-11-10, and `Npgsql.EntityFrameworkCore.PostgreSQL`
11.0.0 is out, which were the two conditions the Story waited for.

| Slice | What it delivers |
|---|---|
| S1 | The four projects target `net11.0`; ASP.NET Core, EF Core, Npgsql and the `System.*` packages move to 11.0.0, `TimeProvider.Testing` to 11.0.1, the `dotnet-ef` tool to 11.0.0; the dev-server path, the perf readings and the examine script pins follow. EF Core 11 makes `ExecuteSqlInterpolatedAsync` obsolete, so two test set-up helpers call `ExecuteSqlAsync` (same parameterised SQL). The direct SQLitePCLRaw pin goes, because EF Core Sqlite 11 itself requires 3.0.5, above the CVE-2025-6965 fix. |
| S2 | Every CI pipeline installs SDK 11.0.100 (four `setup-dotnet` pins); an unread `dotnet_version: '10'` input is removed. |
| S3 | The Docker image builds on `aspnet:11.0` / `sdk:11.0` (Ubuntu 26.04). The Docker health check now fails unless `/api/latest/systeminfo` reports a .NET 11 runtime. |
| S4 | Renovate may propose .NET updates again (the hold rule is gone; new major .NET images still wait for a maintainer). The current-state docs say .NET 11. |

Oracles: the unchanged backend suite plus reference checks (S1), `toolchain-pins.test.mjs` (S2), the `ci_docker.yml`
runtime step (S3), `renovate-dotnet-updates.test.mjs` (S4).

## Verification outside the oracles

- Backend, filtered suite: 14742 passed, 0 failed on `net11.0` at `ab8ce2e31`; the same totals as the `net10.0`
  baseline. Build: 0 warnings. `NoPendingModelChangesTest` green on SQLite and Postgres.
- Dev server: `GET /api/latest/systeminfo` → `"runtime":".NET 11.0.0"`.
- Docker, local (no BuildKit on this machine, so a copy of the Dockerfile with BuildKit's stage-`ARG` and
  `WORKDIR`-owner behaviour spelled out): builds, starts on SQLite, reports `.NET 11.0.0` on Ubuntu 26.04.1. The
  published .NET 10 image reports `.NET 10.0.15`, which the new step rejects.
- `Scripts/examine/build-examine-commands` builds with the `Mvc.Testing@11.0.0` pins.
- `.github/scripts` tests: 223 pass. E2E project: Biome and `tsc` clean.
- Stryker.NET 5.1.0 on `net11.0`: a smoke run (`docs/feature/story-6135-dotnet-11/mutation/stryker.6135.smoke.json`, `LighthouseClock.cs` against its 24 tests) loads, builds and mutates the `net11.0` solution: 15 mutants tested, 14 killed, 1 survived, score 93.33 %. It is a tool check, not a gate on this feature (no production logic changed).

## Commits

S1 `92338cfce` (refactor `fa7e877e3` `178e1f407`), S2 `196efc77f`, S3 `ab8ce2e31`, S4 `9f479f644`; oracles `664a36258`
`0d8c80f29` `7f7cd477b` `1b25b1cbd` `c5ac4f877`; plus the wave, approval and review records.

## Gates

- Whole-candidate review (Sonnet): accepted, three low findings: the two test-helper edits and the SQLitePCLRaw pin
  removal were recorded as DELIVER decisions in `feature-delta.md` rather than in the design's impact list, and
  `docs/architecture/atdd-infrastructure-policy.md` still mentions `net10.0` in a dated 2026-05-25 note (left as history).
- Slice reviews: S1, S3, S4 accepted; S2 accepted after one design fix (its grep checks matched the oracle's own
  fixtures). Oracle reviews: S4 needed one revision (its .NET matcher missed the `System.*` packages).
- EXAMINE (Sonnet, source-blind): all seven expectations unmet only as observation gaps. Its public probe commands
  cannot start the app, build images or reach CI; nothing it saw contradicts a promise. The nwdes examine record was not
  published (`RoleNotAccepted`), so the verdicts live here. The host checks above close E1–E4 and E7 locally; the CI
  run after the push closes E5 (pipelines, Sonar) and E6 (BuildKit image, standalone packages).
- The maintainer accepted that the standalone builds have no runtime check of their own: they are self-contained
  `net11.0` builds, and the post-push packaging and verify workflows start them.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done in `9f479f644`: `docs/Installation/configuration.md` (the time-zone note is now version-free) and `docs/compliance/cra-technical-file.md` (.NET 11). |
| Screenshots | N/A, because nothing a user sees changed; System Info reads the runtime version at run time. |
| Demo data | N/A, because no product data is involved. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | `ARCHITECTURE.md` names .NET 11 (`9f479f644`). ADR-270 unchanged: no context, glossary term or domain event is touched. |
| Usage-data event | N/A, because the upgrade is behaviour-free and adds nothing a user can do. |
| Mutation | N/A, because no C# or TypeScript production logic changed (package versions, comments, config, YAML, Dockerfile); a Stryker smoke run checked the tool still works on `net11.0` (above). |
| Release notes | N/A; written at /release (the ADO item carries the `Release Notes` tag). |

## Knowingly left open

- Only CI after the push shows the BuildKit Docker build of the real Dockerfile, the macOS rpath/codesign/notarise
  step on .NET 11 output, and the Windows and Linux packages.
- The Dockerfile's publish stage relies on BuildKit passing `VERSION` from the `build` stage and on BuildKit giving
  `/app` to the `app` user. Both predate this Story and work in CI today; a non-BuildKit build fails on SDK 11.
- Self-hoster rollback to a .NET 10 image is expected to work (no migration, unchanged key-ring format) but was not
  observed.
- Oracle gaps noted by reviewers, all low: the S2 input check only knows the key `dotnet_version`; the S3 check would
  also accept `.NET 110`; the S4 check only sees `allowedVersions` caps on its listed names.
