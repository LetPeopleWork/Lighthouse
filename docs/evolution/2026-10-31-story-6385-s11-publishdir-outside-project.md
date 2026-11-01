# Publish output leaves the backend project folder — Story 6385

Delivered 2026-10-31 as item S11, ADO #6385 ("`PublishDir` outside the project folder"), from Epic #6264 (path to
a modular monolith). Pushed to `main` in `9dfb938c6..d71b03f2c` (18 commits, the FE7 catalogue entry among them).
CI run 41228644313 was still running when this was written. The Story is Resolved and is Closed once that run is
green; the Epic's state is left to the maintainer. No production code changed, so there are no release notes unless
the maintainer decides otherwise (see "Found in passing").

Before this Story, CI published the backend with `dotnet publish -o ./Lighthouse.Backend/Lighthouse.Backend/publish/<rid>`,
inside the Web SDK project folder. The SDK's default content globs pick up `*.json` under that folder, so every
publish re-published the previous one: the main checkout held `publish/linux-x64/publish/linux-x64/publish`, 151 MB.

Now every publish goes to `artifacts/publish/<rid>` at the repository root (already gitignored), a guard test keeps
the old path from coming back, and the CI rule that decides whether that guard runs is itself tested on every run.

## What shipped

| Step | What it changed |
| --- | --- |
| 01-01 | The inline `backend` change rule in `ci_changes.yml` moved, unchanged, into `Scripts/test-selection/path-classifier.sh` as `classify_backend()`, with a case pinning today's behaviour (`61a8c3342`). |
| 01-02 | `classify_backend()` also fires for `.github/actions/`, so editing a composite action runs the backend suite (and the guard). `github_changes` is not widened: it would also start every live-connector suite (`92fe12048`). |
| 01-03 | The four consumers (`build-backend` and `package-app` actions, the macOS Standalone workflow, the frontend's `update-sidecar` script, 13 references on 11 lines) read and write `artifacts/publish/<rid>`; the guard is switched on (`3b7afc035`). |
| 01-04 | `.dockerignore` lists `artifacts` and the old in-project folder (`22d50b1d1`); the verification procedure and its evidence (`c51f70128`, `d3e46bbb8`). |
| refactor | Guard constants grouped, classifier header rewrapped (`1ef526d0a`). |
| review | The guard also reads `*.csproj`, `*.props` and `*.targets` and checks the repository root it anchors on (`a817b1d13`); a comment citing internal references rewritten and a test label made unambiguous (`74ea2b5a8`); the evidence corrected to what CI's real publish order changes (`5d62ef3d8`). |
| maintainer | The classifier tests run in the `changes` job right after checkout, so a broken rule stops every CI run (`d71b03f2c`). |

DESIGN, DEVOPS and DISTILL are `bff592581`, `f61715405` and `cb865d27d`, each with its review pass. No ADR: the
decision is a path, and ADR-268's ratchet convention covers the guard as written.

## Key decisions

- **`artifacts/publish/<rid>` at the root**, not `Lighthouse.Backend/publish/<rid>` (still inside the folder the
  Dockerfile copies) and not .NET's `UseArtifactsOutput` (it also moves `bin/` and `obj/`, which tests, Stryker and the
  migration DLL references depend on). Only path strings changed; no `PublishDir` default.
- **The guard is a filesystem scan with an empty list.** It reads workflows, actions, Dockerfiles, every `package.json`,
  shell scripts and MSBuild files, fails naming `file:line`, and proves it saw the known consumers so a wrong root
  cannot make it pass. Still not read: other JSON, `.js`/`.ts`, `.cmd`/`.bat`, and a relative `-o publish` run with
  the project folder as working directory.
- **The backend change rule moved before it widened.** A pure move into the tested classifier, then the widening, so
  each commit says one thing.
- **Local E2E skipped on the maintainer's word**: nothing that runs inside the app changed, and editing `ci_changes.yml`
  makes the push run every suite in CI anyway.

## Numbers

| Measure | Result |
| --- | --- |
| Old-path references | 13 on 11 lines in 4 files → 0, guarded |
| Same tree, one platform at a time, old vs new path | identical names and sizes, linux-x64 219, win-x64 224, osx-arm64 220 files |
| CI's publish order (win, linux, osx into one folder), old vs new | every shared file sha256-identical; publish deterministic run to run |
| Files in the old CI artifacts that the new ones lack | linux-x64 6, osx-arm64 18, win-x64 0 (see below) |
| Backend suite (filtered), after the consumers moved | 11,774 passed, 0 failed |
| Architecture tests after the review fixes | 287 passed |
| Frontend | `pnpm test` 8,888 passed; `pnpm build` 0 warnings |
| Classifier tests | 46 passed |
| Mutation | not applicable: no production code changed |

## Lessons

- **Measure in the order CI runs.** One platform at a time into a clean folder, old and new paths were identical,
  and the first evidence said so. CI publishes three platforms into one folder in sequence, and that is where the
  old path did its damage. The adversarial review caught it; the next "unchanged artifact" claim should reproduce
  the pipeline's own sequence from the start.
- **Compare in one tree.** A before/after across two checkouts showed a 3 % executable gap that came from the
  checkout, not the change. Publishing both paths from the same tree, with sha256, settled it.
- **A rule nobody runs is not a ratchet.** The classifier had tests that no workflow ran; the CI rule this Story
  added would have been as unguarded as the path it protects.

## Found in passing

- **Release bundles shipped development settings.** In CI's order, the old linux-x64 artifact carried a nested copy
  of the win-x64 output's JSON, and osx-arm64 carried both, nested twice, each with `appsettings.Development.json`
  (local settings and the example client secret the pre-push scan acknowledges). The step that strips that file only
  removed the top-level copy. The new path ships none of it. This is the one change to what is published, and it is
  the nesting bug itself; whether it deserves a release-note line is the maintainer's call.
- **A second publish into the same folder drops 13 files** (`Factories/DemoData/*.csv` and `certs/LighthouseCert.pfx`,
  all `PreserveNewest`), on both paths. CI publishes into fresh folders; running `update-sidecar` twice locally does
  not. A Bug candidate, not fixed here.
- **Latent:** every classifier function pipes `printf` into `grep -q`, which would misreport under `pipefail` on a very
  large diff. The step runs without `pipefail` today.
- The maintainer's 151 MB in-project `publish/` folder in the main checkout can be deleted.

## Artifacts

- Workspace (delivery history): `docs/feature/story-6385-s11-publishdir-outside-project/`
- Tests: `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PublishOutputOutsideProjectGuardTest.cs`,
  `Scripts/test-selection/test-path-classifier.sh`
