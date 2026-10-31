# Feature: story-6385-s11-publishdir-outside-project

Story #6385 · S11 `PublishDir` outside the project folder · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-29-s11-publishdir-outside-project.md`](../epic-6264-modular-monolith/slices/slice-29-s11-publishdir-outside-project.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → S11.
- [REF] Cross-Cutting Impact Checklist (answered for this Story below).
- [REF] Outcome KPIs + guardrails: "Published binary size within noise" and "Single binary and the deployment
  topologies unchanged" are the guardrails this Story is measured against. No KPI row is touched.
- [REF] Wave Decisions and [REF] Risks / Open questions: nothing specific to S11.
- [REF] Improvement log: nothing since 2026-10-29 that changes S11.
- ADR-268 (ratchet convention): applies if DESIGN adds a guard against the old path.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Publish output lands in `Lighthouse.Backend/Lighthouse.Backend/publish/` and nests on the next publish: the main checkout holds `publish/linux-x64/publish/linux-x64/publish`, 151 MB. |
| 2 Persona | PASS | `lighthouse-maintainer` and the CI build. |
| 3 Domain examples | PASS | The four consumers in the re-count below; the nested folder in the main checkout; `.dockerignore` sending that folder into a Docker build context. |
| 4 UAT G/W/T | PASS | The brief's three AC, already Given/When/Then; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS | 1:1 with the brief's AC. |
| 6 Right-sized | PASS | Size S, likely a single slice (brief). |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None (brief and graph agree). |
| 9 Outcome measurable | PASS | Published file set and size, before and after, per RID. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-10-31)

| Brief says | Today |
|---|---|
| Output nests: `…/publish/linux-x64/publish/...` | Confirmed, three levels deep in the main checkout: `publish/linux-x64/publish/linux-x64/publish`. |
| ~151 MB locally | **151 MB** (`du -sh` on the main checkout). |
| Consumers: CI workflows, release workflow, Dockerfiles, Standalone packaging scripts | **4 files, 14 references**: `.github/actions/build-backend/action.yml` (6: three `-o`, three `rm` of `appsettings.Development.json`), `.github/actions/package-app/action.yml` (3: `chmod`, two artifact paths), `.github/workflows/ci_package-macos-standalone.yml` (1: `ARM64_DIR`), `Lighthouse.Frontend/package.json` `update-sidecar` script (3 in one line). |
| Dockerfile | Publishes to `/app/publish` inside the image already, so not a consumer. **But** `.dockerignore` excludes `**/bin` and `**/obj`, not `publish/`, so a local publish folder enters the Docker build context through `COPY ["Lighthouse.Backend/", "."]`. For DESIGN to weigh. |
| Website / helm repos | `website`, `lighthouse-platform`, `lighthouse-clients`: **no reference** to the old path. |
| `ci_verifymacos.yml` / `ci_verifywindows.yml` mention `publish/` | Not consumers: a `./publish/` folder of the downloaded DMG and the Standalone's runtime pid/log, unrelated to the project folder. |
| `.gitignore` | `publish/` already ignored (line 180). |

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no code path or authorization rule changes; only build output locations move.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes, and the clients repo has no
  reference to the path.
- Website: N/A, because nothing user-visible changes and the website repo has no reference to the path.
- Usage-data event: provisionally N/A, because nothing is added that a user could use; DEVOPS confirms.
- UI sketch: N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: published file set identical per RID, size within noise; routes, DTOs, pixels, topologies unchanged.
- [ ] Existing API, E2E and response tests green unmodified.
- [ ] Ratchet in the same change: whatever guard DESIGN chooses against the old path coming back (or "none, because …" stated).
- [ ] Moves separate from edits (here: the path change is one edit commit per consumer group, no code moves).
- [ ] Re-count before designing (done above).
- [ ] Mutation last, on frozen code: N/A expected, because no production C# or TS changes; DESIGN confirms.
- [ ] `ARCHITECTURE.md` and any ADR whose decision changed, updated in the same change.
- [ ] Payoff proven: local nesting gone after two publishes in a row; CI and release pipelines green on the new path.
- [ ] No user-facing release notes.
- [ ] One context at a time: N/A, because no context is packaged.
- [ ] Terminology: N/A, no new names in user-facing text.
- [ ] Cross-cutting re-check answered (above).
- [ ] `pnpm build` and `dotnet build` clean before push.

## Wave: DESIGN / [REF] Options

Mechanism, confirmed with high confidence: the Web SDK's default items pick up `**/*.json` (and similar) under the
project folder as `Content`, and the project folder includes `publish/`. In the main checkout the nested
`publish/linux-x64/publish/linux-x64/publish` holds exactly 30 files, all `.json` (440 KB), which is the Content glob
and nothing else. So the nesting is small; the 151 MB is the real publish output, not the junk. Only `-o` inside the
project folder nests: a bare `dotnet publish -r <rid>` lands in `bin/<cfg>/net10.0/<rid>/publish`, which the SDK already excludes.

| Option | Output path | Pros | Cons |
|---|---|---|---|
| A | repo-root `artifacts/publish/<rid>` | Outside every project folder, the Docker `COPY Lighthouse.Backend/`, `ProductionSource` and `sonar.sources`. Already gitignored (`artifacts/`, line 63). Conventional .NET name. | `ci_codesign-windows.yml` already downloads into `./artifacts/server` and `./artifacts/standalone`; disjoint sub-folders in a different job, so no clash, but the folder name is shared. |
| B | `Lighthouse.Backend/publish/<rid>` | Beside the project, covered by the existing `publish/` ignore. | Still inside the folder the Docker build copies and the production scans walk; needs a `.dockerignore` line to stay out. Leaves `publish/` ambiguous with the old location. |
| C | `UseArtifactsOutput=true` | The SDK's own answer; no `-o` needed. | Moves `bin/` and `obj/` of every project to `artifacts/`, which reaches the test projects, Stryker config and the EF migration DLL `HintPath` references. Not behaviour-free, far beyond S. Rejected. |

Recommendation: **A**, changing only the `-o`/path strings in the four consumers. No MSBuild change: setting a default
`PublishDir` would add a second source of truth for a case that already works (bare publish goes to `bin/`), and the
Dockerfile's `-o /app/publish` would override it anyway.

## Wave: DESIGN / [REF] Decisions

| # | Decision |
|---|---|
| D1 | Publish output moves to repo-root `artifacts/publish/<rid>` (Option A). Option C rejected: moves bin/obj, out of scope. |
| D2 | No `PublishDir` default and no csproj or `Directory.Build.props` change: a bare publish already lands in `bin/`, excluded by the SDK. |
| D3 | No `<Content Remove="publish\**">`: nothing in the repo publishes there any more, and the ratchet (D5) stops it coming back. |
| D4 | `.dockerignore` gains one line, `artifacts`, so a local publish folder stays out of the build context. Changes nothing published. A stray old `Lighthouse.Backend/Lighthouse.Backend/publish/` is deleted by the maintainer locally (it is untracked), not excluded. |
| D5 | Ratchet: one small architecture test with an empty offender list (a plain rule, per ADR-268's last bullet), see Ratchet. |
| D6 | One slice, three commits: consumers, `.dockerignore`, guard. No code move, no production C# or TS change, so mutation testing is N/A. |
| D7 | No ADR: a path change with one obvious home and no trade-off that outlives the Story. |
| D8 | No C4 diagrams: housekeeping, no container, component or boundary changes. |
| D9 | `Lighthouse.Frontend/build-tauri-local.sh` publishes to `./temp_publish` (frontend folder, not the Web SDK project), so it is not a consumer and does not change. |
| D10 | Usage-data event: N/A, because nothing a user can use is added. Loading and failure looks: N/A, because no screen, chart or widget changes. |

## Wave: DESIGN / [REF] Change list

Old = `./Lighthouse.Backend/Lighthouse.Backend/publish/<rid>` (Frontend script: `../Lighthouse.Backend/Lighthouse.Backend/publish/<rid>`). New = `./artifacts/publish/<rid>` (Frontend: `../artifacts/publish/<rid>`). `<rid>` is `win-x64`, `linux-x64`, `osx-arm64`.

| File | Change | Refs |
|---|---|---|
| `.github/actions/build-backend/action.yml` | Three `-o` values and three `rm -f …/appsettings.Development.json` paths: old → new. The trailing project argument `./Lighthouse.Backend/Lighthouse.Backend` stays. | 6 |
| `.github/actions/package-app/action.yml` | `chmod -R +x` path and the two `upload-artifact` `path:` values: old → new. Artifact names unchanged; uploaded contents are the same files. | 3 |
| `.github/workflows/ci_package-macos-standalone.yml` | `ARM64_DIR` (line ~82): old → new. | 1 |
| `Lighthouse.Frontend/package.json` `update-sidecar` | `-o`, the `cp …/Lighthouse` source and the `cp -r …/.` source: `../Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64` → `../artifacts/publish/linux-x64`. The project argument stays. | 3 |
| `.dockerignore` | Add `artifacts`. | +1 |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PublishOutputOutsideProjectGuardTest.cs` | New (see Ratchet). | new |

Verified not to change: `ci_package-linux-standalone.yml` and `ci_package-win-standalone.yml` (no reference to the path; they consume the composite action's output or artifacts by name), `ci_codesign-windows.yml` (downloads by artifact name into its own `./artifacts/...`), `ci_verifymacos.yml` / `ci_verifywindows.yml` (their `publish/` is unrelated), `Dockerfile` (`-o /app/publish` inside the image), `.gitignore` (`artifacts/` and `publish/` already ignored), `Lighthouse.Backend.csproj`, `Directory.Build.props`, `.github/actions/README.md`, `ARCHITECTURE.md`, `docs/` and the product docs: none mention the path (searched; the only `publish` hits are publish/subscribe prose and the unrelated verify folders).

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| Composite publish step | `.github/actions/build-backend/action.yml` | The one place that runs the three publishes | EXTEND | Only its path strings change. |
| Composite artifact upload | `.github/actions/package-app/action.yml` | Reads the same dirs | EXTEND | Path strings only. |
| `artifacts/` ignore | `.gitignore` line 63 | Already ignores the new location | REUSE | No ignore change. |
| `publish/` ignore | `.gitignore` line 180 | Covers the old location | REUSE | Left in place; harmless and still covers a stray local folder. |
| `bin/…/publish` default | SDK | A bare `dotnet publish` already stays outside the Content globs | REUSE | Why no `PublishDir` default (D2). |
| Source-scanning guard style | `PostgresContainerSharingGuardTest.cs`, `ProductionSource.cs` | Read repo text files, assert a non-empty scan | CREATE NEW (one test file, same shape) | `ProductionSource` only walks `*.cs` under the backend, so it cannot see workflows or `package.json`; the new test copies its shape (find the repo root, assert the scan was not empty) rather than widening that helper. |
| `.dockerignore` | root | Already excludes `**/bin`, `**/obj` | EXTEND | One line. |

## Wave: DESIGN / [REF] Ratchet

Decision: a guard, kept small. `PublishOutputOutsideProjectGuardTest` reads the tracked build files (`.github/**/*.yml`, `Dockerfile`, `Lighthouse.Frontend/package.json`, `*.sh` and `*.ps1` under `Lighthouse.Frontend`, `Lighthouse.Backend` and `Scripts`) and fails, naming file and line, if any contains `Lighthouse.Backend/Lighthouse.Backend/publish`. The needle is split in the source so the test does not match itself. It also asserts the scan found the workflow and `package.json` files, so it cannot pass by looking at nothing.

Offender list: **empty**. There is no offender left after the change, so under ADR-268 it is a plain rule with no list and no "only shrinks" bookkeeping. It guards only the one path that caused the nesting; a general "no `-o` inside a project folder" rule is rejected as heavier than the risk. Cost: about 30 lines, runs with the normal architecture tests, no new package.

Alternative considered: "none, because `artifacts/` is gitignored and the nesting is 440 KB of JSON". Rejected as the default because the Epic asks for a ratchet and a copy-pasted `-o` is the exact way this returns. If the maintainer prefers none, drop D5 and the third commit; nothing else depends on it.

## Wave: DESIGN / [REF] Verification plan

Only DELIVER runs publishes. Proof of behaviour-free, per RID (`linux-x64` locally; all three in CI):

1. Before (unmodified HEAD, clean tree: remove any old `publish/` first so junk is not counted): publish with the exact CI command to the old path, then `find <dir> -type f -printf '%P %s\n' | LC_ALL=C sort > before-<rid>.txt` and `du -sb <dir>`.
2. After (HEAD plus the change): publish to `artifacts/publish/<rid>` and produce `after-<rid>.txt` the same way.
3. `diff before-<rid>.txt after-<rid>.txt` is empty: same file set, sizes within build noise (compare names exactly; name any file whose size differs). `appsettings.Development.json` is removed in both.
4. Run the publish twice in a row to the new path: no `publish/` directory appears inside `artifacts/publish/<rid>` and the second listing equals the first.
5. After the maintainer deletes the old local folder and both publishes ran, `find Lighthouse.Backend/Lighthouse.Backend -path '*/publish/*'` finds nothing.
6. `pnpm run update-sidecar` (linux) leaves `src-tauri/binaries` and `src-tauri/resources` with the same contents as before.
7. The new guard test fails when one old-path string is put back (red check) and is green on the change.
8. CI green on the push: `ci_backend`, the Windows, Linux and macOS standalone packaging, the codesign and verify workflows, and the Docker build. The macOS job is the only runner for the `ARM64_DIR` edit, so it is the proof for that line.

Existing API, E2E and response tests run unmodified. `pnpm build` and `dotnet build` clean before push. Mutation: N/A, because the only C# added is a test.

## Wave: DESIGN / [REF] Slices and commit plan

One slice (S, under a day). Three focused commits, each green on its own:

1. `build(publish): publish backend output to artifacts/publish instead of the project folder` — the four consumers (producer and readers move together).
2. `build(docker): keep local artifacts out of the build context` — `.dockerignore`.
3. `test(publish): guard against publishing inside the backend project folder` — the new test, committed after commit 1 so the tree is green (DELIVER sees it red first by running it against the old strings).

No code moves; every commit is an edit.

## Wave: DESIGN / [REF] Before → after

```
BEFORE                                                AFTER
Lighthouse.Backend/Lighthouse.Backend/                Lighthouse.Backend/Lighthouse.Backend/
  Lighthouse.Backend.csproj                             Lighthouse.Backend.csproj   (no publish/)
  publish/<rid>/            <- dotnet publish -o      artifacts/                     (gitignored)
    publish/<rid>/...       <- *.json re-published      publish/<rid>/               <- dotnet publish -o
                                                          (win-x64, linux-x64, osx-arm64)
Readers of publish/<rid>:                              Readers of artifacts/publish/<rid>:
  build-backend  (rm appsettings.Development.json)      build-backend, package-app (chmod, upload)
  package-app    (chmod, upload-artifact)               ci_package-macos-standalone (ARM64_DIR)
  macos-standalone (ARM64_DIR)                          Frontend `update-sidecar` (cp to src-tauri)
  Frontend `update-sidecar`
Docker context: Lighthouse.Backend/ incl. publish/    Docker context: `artifacts` ignored
Unchanged: Dockerfile -o /app/publish, bin/ + obj/, ci_codesign-windows (its own ./artifacts/server, /standalone)
```

## Wave: DESIGN / [REF] Open questions

1. Ratchet: accept the small guard (recommended), or "none, because …"?
2. Folder name: `artifacts/publish/<rid>` shares the `artifacts/` root with `ci_codesign-windows.yml`'s downloads (different job, disjoint sub-folders). Acceptable, or prefer a distinct root such as `out/publish/<rid>`? That needs a `.gitignore` line.
3. Does the test-selection path classifier run the backend architecture tests when only `.github/**` and `package.json` change? If not, the guard runs only on the next backend change. DISTILL checks this; a decision is needed only if the classifier needs a rule.
4. The maintainer deletes the stray local `Lighthouse.Backend/Lighthouse.Backend/publish/` in the main checkout (151 MB, untracked) after DELIVER; not touched here.
