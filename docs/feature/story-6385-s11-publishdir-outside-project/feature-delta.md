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
| Consumers: CI workflows, release workflow, Dockerfiles, Standalone packaging scripts | **4 files, 13 references on 11 lines**: `.github/actions/build-backend/action.yml` (6: three `-o`, three `rm` of `appsettings.Development.json`), `.github/actions/package-app/action.yml` (3: `chmod`, two artifact paths), `.github/workflows/ci_package-macos-standalone.yml` (1: `ARM64_DIR`), `Lighthouse.Frontend/package.json` `update-sidecar` script (3 in one line). Correction: this row first said 14; the parts always summed to 13 (6+3+1+3), on 11 distinct lines because the three `update-sidecar` references share one line. |
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

- [ ] Behaviour-free: published file set identical per RID (CI sequence: minus the nested JSON copies, see Demo Evidence), size within noise; routes, DTOs, pixels, topologies unchanged.
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
| D4 | `.dockerignore` gains two lines: `artifacts` (a local publish folder at the new root) and `Lighthouse.Backend/Lighthouse.Backend/publish` (a stale in-project folder from before). Both only trim the Docker build context; the image's published files are unchanged. The guard does not scan `.dockerignore` (its file name does not start with `Dockerfile`, and it is not a consumer), so the second line does not trip it. The maintainer still deletes the stray old folder locally (it is untracked). |
| D5 | Ratchet: one small architecture test with an empty offender list (a plain rule, per ADR-268's last bullet), see Ratchet. |
| D6 | One slice, four commits (see Slices and commit plan). No code move beyond the pure move of the `backend` change rule, no production C# or TS change, so mutation testing is N/A. |
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
| `.dockerignore` | Add `artifacts` and `Lighthouse.Backend/Lighthouse.Backend/publish`. | +2 |
| `.github/workflows/ci_changes.yml`, `Scripts/test-selection/path-classifier.sh`, `Scripts/test-selection/test-path-classifier.sh` | The `backend` change rule moves out of inline YAML into `BACKEND_REGEX` + `classify_backend()` in `path-classifier.sh` (ci_changes.yml already sources that file); then the regex widens to include `^\.github/actions/`, with a test case in `test-path-classifier.sh`. See the commit plan. | new |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PublishOutputOutsideProjectGuardTest.cs` | New (see Ratchet). | new |

Verified not to change: `ci_package-linux-standalone.yml` and `ci_package-win-standalone.yml` (no reference to the path; they consume the composite action's output or artifacts by name), `ci_codesign-windows.yml` (downloads by artifact name into its own `./artifacts/...`), `ci_verifymacos.yml` / `ci_verifywindows.yml` (their `publish/` is unrelated), `Dockerfile` (`-o /app/publish` inside the image), `.gitignore` (`artifacts/` and `publish/` already ignored), `Lighthouse.Backend.csproj`, `Directory.Build.props`, `.github/actions/README.md`, `ARCHITECTURE.md`, `docs/` and the product docs: no build or doc consumer. Problem-statement prose in the Epic names the old path and needs no change (the only other `publish` hits are publish/subscribe prose and the unrelated verify folders).

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| Composite publish step | `.github/actions/build-backend/action.yml` | The one place that runs the three publishes | EXTEND | Only its path strings change. |
| Composite artifact upload | `.github/actions/package-app/action.yml` | Reads the same dirs | EXTEND | Path strings only. |
| `artifacts/` ignore | `.gitignore` line 63 | Already ignores the new location | REUSE | No ignore change. |
| `publish/` ignore | `.gitignore` line 180 | Covers the old location | REUSE | Left in place; harmless and still covers a stray local folder. |
| `bin/…/publish` default | SDK | A bare `dotnet publish` already stays outside the Content globs | REUSE | Why no `PublishDir` default (D2). |
| Source-scanning guard style | `PostgresContainerSharingGuardTest.cs`, `ProductionSource.cs` | Read repo text files | CREATE NEW (one test file, same shape) | `ProductionSource` only walks `*.cs` under the backend, so it cannot see workflows or `package.json`; the new test copies its shape (find the repo root by `Lighthouse.sln`) rather than widening that helper. The sibling asserts only that it found the solution file, not that its scan was non-empty; the new test adds that assertion itself. |
| `.dockerignore` | root | Already excludes `**/bin`, `**/obj` | EXTEND | Two lines. |
| `backend` change rule | `.github/workflows/ci_changes.yml` (inline), `Scripts/test-selection/path-classifier.sh` | The file ci_changes.yml already sources holds the per-connector classifiers | EXTEND | The rule moves there as `classify_backend()`, covered by `test-path-classifier.sh`. |

## Wave: DESIGN / [REF] Ratchet

Decision: a guard, kept small. `PublishOutputOutsideProjectGuardTest` does a filesystem walk of the repository from its root (not the git-tracked set) and reads `.github/**/*.yml|yaml`, `Dockerfile*`, every `package.json` and every `*.sh` and `*.ps1` (so `Scripts/test-selection/`, `Lighthouse.Frontend` and `Lighthouse.Backend` are all covered), minus pruned folders (`node_modules`, `bin`, `obj`, `.git`, `artifacts`, `.claude`, `target`, and generated output: `StrykerOutput`, `.stryker-tmp`, `dist`, `coverage`, `playwright-report`, `test-results`); symlinked folders are not followed. It fails, naming file and line, if a line contains `Lighthouse.Backend/Lighthouse.Backend/publish`, matched case-insensitively with runs of `\` and `/` collapsed to one `/` (so Windows paths, lower-case paths and JSON-escaped double backslashes are caught). It also asserts the scan found the known consumer files, so it cannot pass by looking at nothing.

The guard also reads `*.csproj`, `*.props` and `*.targets`, where a `PublishDir` default would bring the in-project path back. Known gaps, accepted and not tested: a relative `-o publish` run with the project folder as the working directory never spells out the project folder, so the guard cannot see it; and file types it does not read: other JSON (such as `src-tauri/tauri.conf.json`), `.js`/`.ts`/`.mjs`, and `.cmd`/`.bat`/`.psm1`.

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
7. The new guard test fails when one old-path string is put back (red check, with `[Ignore]` removed locally and restored; commit 3 removes it for good) and is green on the change.
8. CI green on the push: `ci_backend`, the Windows, Linux and macOS standalone packaging, the codesign and verify workflows, and the Docker build. The `ARM64_DIR` read runs only on `main` (see DEVOPS), so a PR proves the publish, not that line; DELIVER checks the osx-arm64 folder exists at the new path locally.

Existing API, E2E and response tests run unmodified. `pnpm build` and `dotnet build` clean before push. Mutation: N/A, because the only C# added is a test.

## Wave: DESIGN / [REF] Slices and commit plan

One slice (S, under a day). Four focused commits, each green on its own (revised after the DISTILL review: the classifier work comes first so the consumer commit runs the backend suite):

1. `refactor(ci): move the backend change rule into path-classifier.sh` — a pure move: the same regex becomes `BACKEND_REGEX` + `classify_backend()`, `ci_changes.yml` calls it, and `github_changes` still ORs in as before.
2. `ci: run the backend suite when a composite action changes` — the regex widens to `^\.github/actions/`, with a test case in `test-path-classifier.sh` proving an actions-only diff gives `backend=true` while the `ci*.yml`-only `github_changes` stays false.
3. `build(publish): publish backend output to artifacts/publish instead of the project folder` — the four consumers (producer and readers move together; split by consumer group if preferred) plus the guard test. The commit that moves the last consumer also removes `[Ignore(...)]` and the pending-reason constant from the guard, so the tree is green. DELIVER sees the guard red first by running it, `[Ignore]` removed locally, against the old strings.
4. `build(docker): keep local publish output out of the build context` — the two `.dockerignore` lines.

Only commit 1 moves code; the rest are edits.

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
Docker context: Lighthouse.Backend/ incl. publish/    Docker context: `artifacts` and the old publish/ ignored
Unchanged: Dockerfile -o /app/publish, bin/ + obj/, ci_codesign-windows (its own ./artifacts/server, /standalone)
```

## Wave: DESIGN / [REF] Open questions

1. Ratchet: accept the small guard (recommended), or "none, because …"?
2. Folder name: `artifacts/publish/<rid>` shares the `artifacts/` root with `ci_codesign-windows.yml`'s downloads (different job, disjoint sub-folders). Acceptable, or prefer a distinct root such as `out/publish/<rid>`? That needs a `.gitignore` line.
3. Does the test-selection path classifier run the backend architecture tests when only `.github/**` and `package.json` change? If not, the guard runs only on the next backend change. DISTILL checks this; a decision is needed only if the classifier needs a rule.
4. The maintainer deletes the stray local `Lighthouse.Backend/Lighthouse.Backend/publish/` in the main checkout (151 MB, untracked) after DELIVER; not touched here.

## Wave: DESIGN / [REF] Maintainer decisions (2026-10-31)

- Publish root: `artifacts/publish/<rid>` at the repo root, as recommended.
- Ratchet: add the guard test (empty offender list, fails on an empty scan).
- DESIGN approved; go to DEVOPS.

## Wave: DEVOPS / [REF] Platform decisions (2026-10-31)

Every platform decision is the project's existing one. S11 changes where the backend's publish output is written
and read; nothing is deployed, built or served differently.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | Unchanged. The release artifacts (win-x64, linux-x64, osx-arm64 single files, the Docker image, the Standalone bundles) change only by losing nested copies of other platforms' JSON files (see the CI publish sequence measurement under Demo Evidence); the runner-local folder they are staged in moves to `artifacts/publish/<rid>`. |
| CI/CD platform | GitHub Actions, existing workflows. No new job or workflow. One edit to `ci_changes.yml` (below). |
| Branching | Trunk-based on `main`. |
| Observability | N/A, because no runtime behaviour changes. |
| Continuous learning / flags | N/A, because there is nothing a user can see to roll out. |
| Mutation testing | **N/A, because S11 adds no production code.** The only C# is the guard test; the rest is YAML, JSON and an ignore file. Recorded at the final gate rather than run on an empty mutate set. |
| Usage-data event | **N/A, because S11 is behaviour-free build housekeeping**: nothing a user does changes, so there is no use to count. |
| Security | No secret, permission or token scope changes. `package-app`'s upload paths change, not what is uploaded. |

## Wave: DEVOPS / [REF] CI pipeline outline

- **Publish and package:** `build-backend` (`publish: 'true'`) writes `artifacts/publish/<rid>` and strips
  `appsettings.Development.json` there; `package-app` chmods and uploads from there; `ci_package-macos-standalone`
  reads `ARM64_DIR` from there. Upload artifact names stay the same, so every downstream `download-artifact` is
  unaffected.
- **Where the guard runs:** the backend unit suite (`ci_backend` via `ci_changes.yml` → `backend=true`). Today
  `backend` is true for `Lighthouse.Backend/`, `Scripts/test-timings/` and any `.github/workflows/ci*.yml` change.
  **Maintainer decision (2026-10-31): widen this so `.github/actions/**` changes also run the backend suite**,
  because the composite actions hold 9 of the 13 old references. The widening goes into the `backend` regex only,
  not into `github_changes`: `github_changes` also fans out to every live-connector output, and an action edit
  should not make real Jira, Linear, ADO and ServiceNow calls (the Linear key is shared with CI). The rule moves
  into `Scripts/test-selection/path-classifier.sh` first (see the commit plan) so the widening is a committed shell test.
- **No live calls from the widening, checked 2026-10-31:** every test file carrying a `*Integration` connector
  category also carries `Category("Integration")` (grep returned no exception), so the filtered backend run never
  reaches a live connector.
- **Action-edit fan-out:** the `packageapp` and `verify*` jobs are not gated by change outputs and run on any push;
  the E2E job need not run. The Standalone, Docker and release jobs are `main`-only. An edit to the
  `build-frontend` action skips the frontend job; that is outside S11's scope.
- **macOS packaging:** the job reads `ARM64_DIR` only on `main` runs (job `if: github.ref == 'refs/heads/main'`), so a
  PR proves the publish but not the read. DELIVER therefore runs the osx-arm64 publish locally and checks the folder
  exists at the new path.
- **Accepted lag:** a change to `Lighthouse.Frontend/package.json`, the `Dockerfile`, a `*.sh` or `*.ps1` outside
  `Lighthouse.Backend/`, or a non-`ci*` `.github/workflows/*.yml` alone does not run the backend suite; a
  reintroduced path there is caught on the next backend run. Accepted because the first two change often
  (Renovate) and running the backend on each would cost far more than the lag.
- **Commit 3 outline:** the guard loses its `[Ignore]` and pending-reason constant in the commit that moves the last consumer.
- **Sonar:** the guard is test code; `dotnet build` with `TreatWarningsAsErrors` must stay clean.

## Wave: DEVOPS / [REF] Environment matrix

See `environments.yaml`: a clean CI checkout on all three runners, and a developer checkout that still holds the
old in-project `publish/`. Nothing installs or runs differently between them.

## Wave: DEVOPS / [REF] Risks for DISTILL and DELIVER

- **The verify workflows.** `ci_verifywindows` / `ci_verifymacos` use their own `./publish/` for downloaded
  bundles and the Standalone's pid and log. They are not consumers and must not be edited by a careless replace.
- **Behaviour-free proof needs a real publish.** DELIVER publishes each RID before and after (old `-o` vs new) and
  compares file lists and sizes by the mechanical rule in the verification procedure, excluding the nested
  `publish/` junk from the before side.

## Wave: DISTILL / [REF] Scenarios

Classification stays here, not in the code (project rule: no nWave tags in tests). Reconciliation: DESIGN and DEVOPS agree (path, guard, `.github/actions/**` in `backend` only); no DISCUSS-level decision exists for this Story, so 0 contradictions.

| # | Scenario | Layer | What it observes | Test or verification step | State |
|---|---|---|---|---|---|
| 1 | No build file writes or reads the backend publish output inside the backend project folder | Architecture test (reads repo text) | `file:line` of every `.github/**/*.yml\|yaml`, `Dockerfile*`, `package.json`, `*.sh`, `*.ps1` mentioning the old path (both `./` and `../` forms; case-insensitive; runs of `\` and `/` collapsed) | `PublishOutputOutsideProjectGuardTest.NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder` | pending (`[Ignore]`) until the consumers move |
| 2 | The scan sees the files that consume the publish output (not vacuous) | Architecture test | Scan includes `build-backend/action.yml`, `package-app/action.yml`, `ci_package-macos-standalone.yml`, `Lighthouse.Frontend/package.json`, `Dockerfile` | `TheScanSeesTheFilesThatConsumeThePublishOutput` | green today (stays green) |
| 3 | Only the in-project path is recognised (error paths: must not flag `./publish/`, `publish/lighthouse.pid`, `/app/publish`, `./temp_publish`, the new `./artifacts/publish/…`) | Architecture test, table of sample lines (11 cases: 6 positive incl. `../`, backslash, lower-case Windows path and JSON-escaped double backslash; 5 negative) | the pattern is specific | `OnlyThePathInsideTheBackendProjectFolderIsRecognised` | green today (stays green) |
| 4 | Consumers point at the new root (build-backend `-o` + `rm`, package-app, macOS `ARM64_DIR`, `update-sidecar`) | none | n/a | **No new test.** Scenario 1 proves the old path is gone; CI green on the push proves the new one is right (publish, package; the macOS job reads `ARM64_DIR` only on `main`, so the local osx-arm64 cross-publish proves the folder at the new path; `update-sidecar` by verification step 6). A test asserting the new strings would restate the YAML. | verification-only |
| 5 | `.dockerignore` lists `artifacts` and the old in-project publish folder | none | n/a | **No test; DELIVER checklist.** Two lines, no behaviour; the Docker build in CI plus `grep -n '^artifacts$' .dockerignore` and `grep -n '^Lighthouse.Backend/Lighthouse.Backend/publish$' .dockerignore` at commit 4. A test would restate the file. | verification-only |
| 6 | A change only under `.github/actions/` sets `backend=true` but not `github_changes` | Committed shell test, written in DELIVER with the extraction | `backend` / `github_changes` for sample diffs | A case in `Scripts/test-selection/test-path-classifier.sh` against `classify_backend()` in `path-classifier.sh` (the `backend` rule moves there from inline YAML first, as a pure move; then the regex widens to `^\.github/actions/`). Proves an actions-only diff gives `backend=true` and the `ci*.yml`-only `github_changes` stays false. | pending, red today (`.github/actions/…` gives `backend=false`); written in DELIVER |
| 7 | Behaviour-free proof: same file set per RID (CI sequence: see Demo Evidence), two publishes do not nest | Real publish | file list and sizes | Verification step 1 and 3 | verification-only |

Counts: 13 NUnit cases in the new class, 1 pending (scenario 1) and 12 green (scenarios 2 and 3); 1 committed shell test to write in DELIVER (scenario 6); 3 verification-only items (scenarios 4, 5 and 7). Non-happy paths: scenario 2 and the 5 negative cases of scenario 3; the Story has one happy path.

## Wave: DISTILL / [REF] Behaviour-free guarantee

Nothing in production C# or TS changes, so these stay green unmodified; DELIVER runs them and edits none:

- Backend: `dotnet build` (zero warnings) and `dotnet test --filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"` (includes the architecture tests, the API/response tests and `PostgresContainerSharingGuardTest`).
- Frontend: `pnpm test` and `pnpm build` (Biome + `tsc`; `package.json` is touched by the `update-sidecar` edit).
- E2E: the Playwright walking skeletons through their usual command, unmodified (they never read the publish folder; run to prove the guarantee).
- Script test: `bash Scripts/test-selection/test-path-classifier.sh`, run after the extraction and the widening; it gains the scenario 6 case and every existing case stays green, which shows the widening did not alter the connector classifiers.
- CI on the push: `ci_backend`, Windows, Linux and macOS standalone packaging, codesign and verify workflows, Docker build.
- Environmental noise per CLAUDE.md (2 Licensing tests in a worktree, 10 `ReleaseServiceTest` under load) is not a regression.

## Wave: DISTILL / [REF] Verification procedure

Behaviour-free proof, run by DELIVER before the push. `WORK` is a scratch folder outside the repo.

1. Published file set per RID, all three compared locally on Linux: `linux-x64`, `win-x64` (a win-x64 publish cross-builds on Linux) and `osx-arm64` (cross-publish). The macOS runner is not needed for this proof.
   - Before: on unmodified HEAD, with `Lighthouse.Backend/Lighthouse.Backend/publish/` deleted first, run the exact CI command, e.g. `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64 ./Lighthouse.Backend/Lighthouse.Backend`, then `rm -f <dir>/appsettings.Development.json`.
   - After: on HEAD plus the change, the same command with `-o ./artifacts/publish/linux-x64`, same `rm`.
   - List each side with name and byte size (portable on GNU and BSD), the before side excluding the nested folder:
     ```
     list() { (cd "$1" && find . -type f $2 | LC_ALL=C sort | while IFS= read -r f; do printf '%s\t%s\n' "$f" "$(wc -c < "$f" | tr -d ' ')"; done); }
     list ./Lighthouse.Backend/Lighthouse.Backend/publish/<rid> "-not -path './publish/*'" > $WORK/before-<rid>.tsv
     list ./artifacts/publish/<rid> "" > $WORK/after-<rid>.tsv
     ```
   - Pass rule: every file present on both sides; every size delta listed; a delta above 0.1% of the file, or any delta on a non-binary file (anything but executables and libraries: no extension, `.dll`, `.exe`, `.so`, `.dylib`, `.pdb`), fails. Computed by:
     ```
     LC_ALL=C join -t "$(printf '\t')" -a1 -a2 -e MISSING -o 0,1.2,2.2 $WORK/before-<rid>.tsv $WORK/after-<rid>.tsv |
       awk -F'\t' 'BEGIN{bad=0} $2=="MISSING"||$3=="MISSING"{print "FAIL not on both sides:",$1; bad=1; next}
         $2!=$3{d=($3>$2?$3-$2:$2-$3); m=($3>$2?$3:$2); bin=($1 ~ /\.(dll|exe|so|dylib|pdb)$/ || $1 !~ /\.[^\/]+$/);
                print "size delta:",$1,$2,$3,d; if(!bin||d*1000>m){print "FAIL"; bad=1}}
         END{exit bad}'
     ```
     Exit status 0 and an empty or fully-listed delta output is the pass. `appsettings.Development.json` is absent in both.
2. Classifier widening (scenario 6) is a committed shell test, not a procedure step: DELIVER writes it in `test-path-classifier.sh` with the extraction (see the commit plan) and runs `bash Scripts/test-selection/test-path-classifier.sh`. Expected after the widening: `.github/actions/build-backend/action.yml` gives `backend=true` and `github_changes=false`; `.github/workflows/ci_backend.yml` gives both true; `Lighthouse.Frontend/package.json` and `docs/readme.md` give `backend=false`. After the pure-move commit the existing cases stay green and the `backend` regex is unchanged; `frontend` and `e2e` lines are untouched throughout.
3. Two publishes in a row to the new path: no `publish` directory appears inside `artifacts/publish/linux-x64`, and the second `find` listing equals the first.
4. After the maintainer deletes the old local folder and both publishes ran, `find Lighthouse.Backend/Lighthouse.Backend -path '*/publish/*'` prints nothing.
5. `grep -n '^artifacts$' .dockerignore` and `grep -n '^Lighthouse.Backend/Lighthouse.Backend/publish$' .dockerignore` each find their line (scenario 5).
6. `pnpm run update-sidecar` (Linux): `src-tauri/binaries` and `src-tauri/resources` hold the same names and sizes as before (same `find` comparison).
7. Red check of the guard: remove `[Ignore]` locally for the check and restore it afterwards (commit 3 removes it for good, in the commit that moves the last consumer); put one old-path string back and the guard names that `file:line`; remove it and it is green.
8. CI green on the push. The macOS job reads `ARM64_DIR` only on `main` runs, so a PR proves the publish, not that read: DELIVER checks the osx-arm64 folder exists at the new path from the local cross-publish (step 1). The `changes` job log shows `backend=true` for a change under `.github/actions/` without the connector outputs.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PublishOutputOutsideProjectGuardTest.cs`, beside `PostgresContainerSharingGuardTest.cs`, which it mirrors (finds `Lighthouse.sln`, scans text, asserts the scan was not empty). `ProductionSource` is not reused: it walks only `*.cs` under the backend project. The scan starts at the repository root (parent of the folder holding `Lighthouse.sln`) and prunes `node_modules`, `bin`, `obj`, `.git`, `artifacts`, `.claude` (nested worktrees in the main checkout), `target` and generated output (`StrykerOutput`, `.stryker-tmp`, `dist`, `coverage`, `playwright-report`, `test-results`) while walking; symlinked folders are skipped. Matching is case-insensitive with runs of `\` and `/` collapsed. `.dockerignore` is not scanned (not a `Dockerfile*` name, not a consumer).

## Wave: DISTILL / [REF] Right-reason RED

Run with the `[Ignore]` removed locally (restored afterwards; the delivered file keeps it): 1 failed, 12 passed. The guard failed on its own assertion, 11 offenders in exactly the 4 consumer files:

```
.github/actions/build-backend/action.yml:48, :49, :52, :59, :60, :61
.github/actions/package-app/action.yml:38, :44, :50
.github/workflows/ci_package-macos-standalone.yml:82
Lighthouse.Frontend/package.json:20
```

The baseline counts 13 references on 11 lines (the earlier 14 was a miscount); the three `update-sidecar` references share line 20. The two guard-support tests were green unchanged, so the scan reaches the right files.

## Wave: DISTILL / Open points

- Scenario 6 is now a committed shell test (maintainer decision below): DELIVER extracts the `backend` rule into `path-classifier.sh` and writes the case.
- DELIVER removes the `[Ignore]` and the pending-reason constant in commit 3, the commit that moves the last consumer.

## Wave: DISTILL / [REF] Maintainer decisions (2026-10-31, after the review gate)

- The `backend` change rule moves out of inline YAML into `Scripts/test-selection/path-classifier.sh` as `BACKEND_REGEX` + `classify_backend()` (`ci_changes.yml` already sources that file; its tests are in `test-path-classifier.sh`). Two DELIVER commits: a pure move (same regex; `ci_changes.yml` calls `classify_backend`; `github_changes` still ORs in), then the widening to `^\.github/actions/` with a test case proving an actions-only diff gives `backend=true` and the `ci*.yml`-only `github_changes` stays false.
- `.dockerignore` also gets `Lighthouse.Backend/Lighthouse.Backend/publish`, next to `artifacts`.
- Commit 3 removes `[Ignore(...)]` and the pending-reason constant in the commit that moves the last consumer.

## Wave: DELIVER / [REF] Rigor profile (maintainer, 2026-10-31)

- One slice, profile **standard**: crafter `sonnet`, reviewer `haiku`, refactor pass and review on, mutation N/A (no production code). Run in AFK mode: slice wrap-up refactor + review, nothing pushed.
- Roadmap: 4 steps, one per approved commit; integrity check exit 0; review needs_revision (0 blockers), one revision pass, every finding applied and re-checked by the orchestrator (2026-10-31). E2E Playwright runs once at the final gates.

## Wave: DELIVER / [REF] Demo Evidence

Run 2026-10-31 on Linux, step 01-04. `.dockerignore` change is commit `f172f8c4b`. Version `2026.10.31.1` passed to both sides so the version metadata is equal. Full filtered backend suite not re-run in this step: no test reads `.dockerignore`, and the full run passed on the 01-03 commit (11774 passed, 0 failed).

### Checks that passed

| Check | Result |
|---|---|
| `grep -n '^artifacts$' .dockerignore` (empty before, one line after) | `31:artifacts` |
| `grep -n '^Lighthouse.Backend/Lighthouse.Backend/publish$' .dockerignore` (empty before) | `32:Lighthouse.Backend/Lighthouse.Backend/publish` |
| Guard class `PublishOutputOutsideProjectGuardTest` after the edit | `Passed! Failed: 0, Passed: 13, Total: 13` |
| `bash Scripts/test-selection/test-path-classifier.sh` | `passed: 46`, `failed: 0` |
| Guard red check: one old-path line appended to `.github/actions/package-app/action.yml` | `Failed! Failed: 1, Passed: 12`; offender `.github/actions/package-app/action.yml:51` |
| Guard after `git checkout -- .github/actions/package-app/action.yml` | `Passed! Failed: 0, Passed: 13, Total: 13`; `git status` clean |
| Nesting after a clean publish to `artifacts/publish/linux-x64` (twice) | `find artifacts/publish/linux-x64 -type d -name publish` prints 0 |
| `find Lighthouse.Backend/Lighthouse.Backend -path '*/publish/*'` (excluding `obj/` and `bin/`) | prints nothing (0 lines) |
| `artifacts/publish/osx-arm64` exists after the local cross-publish | yes (also `win-x64`, `linux-x64`) |
| `pnpm run update-sidecar` before/after, `src-tauri/resources` and `src-tauri/binaries` file names | identical (219 and 1 files); sizes: see deltas below |

The literal step-4 command also lists SDK-internal `Lighthouse.Backend/Lighthouse.Backend/obj/Release/net10.0/<rid>/compressed/publish/*.br` files; those are build intermediates, not a publish output, and `obj/` is gitignored.

### Published file set per RID (before = 4ffc410e3, old `-o` path; after = this worktree, `-o ./artifacts/publish/<rid>`)

`appsettings.Development.json` is absent on both sides for every RID. The file sets are identical (same names on both sides) for all three RIDs.

| RID | Before files / bytes | After files / bytes | Pass-rule verdict |
|---|---|---|---|
| linux-x64 | 219 / 152029127 | 219 / 156423015 | exit 1 (two size deltas) |
| win-x64 | 224 / 156641189 | 224 / 161040745 | exit 1 (two size deltas) |
| osx-arm64 | 220 / 162691420 | 220 / 167125340 | exit 1 (two size deltas) |

Size deltas, verbatim from the comparison (name, before, after, absolute):

```
linux-x64:  ./Lighthouse      144720266 149115238 4394972   ./Lighthouse.pdb 759852 758768 1084
win-x64:    ./Lighthouse.exe  146303381 150704025 4400644   ./Lighthouse.pdb 759852 758764 1088
osx-arm64:  ./Lighthouse      153685931 158120939 4435008   ./Lighthouse.pdb 759856 758768 1088
```

The `.pdb` deltas (about 0.14%) and the executable deltas (about 3%) exceed the 0.1% limit, so the pass rule reports FAIL. No file is missing on either side. The same ~4.4 MB executable delta appears in the `update-sidecar` outputs (`Lighthouse.Backend-x86_64-unknown-linux-gnu` 144720252 before, 149115224 after; `Lighthouse.pdb` 759848 vs 758760).

Control: the post-change commit (727c8dcb8) published with the new `-o` path inside the scratch "before" checkout gives `Lighthouse` = 144720266 bytes and `Lighthouse.pdb` = 759852 bytes, equal to the old-path result there. So the output path does not change the executable; the delta tracks the checkout the publish ran in (this worktree versus a scratch worktree), not the change. The cause inside the executable was not identified; this stays an open finding for the maintainer.

Setup notes: the scratch checkout lacks gitignored `wwwroot/` (built frontend); a first run without it showed 195 missing files plus a 53-byte versus 472246-byte `Lighthouse.staticwebassets.endpoints.json`, so `wwwroot/` was copied from this worktree and the "before" publish was repeated from a clean state. The step-1 `list()` helper passed `"-not -path './publish/*'"` unquoted, which keeps the literal quote characters and excludes nothing; the comparison used the pattern without inner quotes and with globbing off.

### Second publish into an existing output folder

From a clean `artifacts/publish/linux-x64`, publish #1 gives 219 files / 156423015 bytes; publish #2 into the same folder gives 206 files / 156359972 bytes. No `publish` directory appears inside. The 13 files missing after #2 (`Factories/DemoData/*.csv`, 12 files, and `certs/LighthouseCert.pfx`) are `None` items with `CopyToOutputDirectory=PreserveNewest` in `Lighthouse.Backend.csproj`; the csproj is unchanged by this feature (only the `-o` path moved), so this is a property of an incremental publish into a dirty folder, not of the path. CI publishes once into a clean runner. Recorded as a finding: the DISTILL step-3 expectation "the second listing equals the first" does not hold for the dirty-folder case.

Stale-folder evidence for the change itself: in the "before" checkout, a leftover `publish/` folder was picked up as content and nested (`publish/win-x64/...`, 3 levels deep after three publishes), and `pnpm run update-sidecar` then failed with `MSB3030: Could not copy ... publish/linux-x64/publish/win-x64/appsettings.Development.json because it was not found` until the folder was deleted. The same run from the new path is clean.

### Docker build-context probe

Stale files placed at `Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64/stale.json` and `artifacts/publish/probe/stale.txt`; throwaway image from the repo root copying `Lighthouse.Backend/` to `/src/` and `.` to `/all/`, then:

```
stale files:
artifacts dirs:
backend publish dirs:
PROBE_END
```

Neither stale file nor an `artifacts` or in-project `publish` folder is in the context. The stale folders and the probe image (`s11-probe:tmp`) were removed afterwards; the real Dockerfile was not built. (The first probe form, `find ... -path '*publish*' | head -50`, was drowned by `docs/` file names containing "publish", so it was replaced by the name-specific form above. The legacy builder here rejects `--progress`, so it was dropped.)

### Pending the push (not run locally)

- CI green on the push (`ci_backend`, standalone packaging, codesign and verify workflows, Docker build).
- The macOS job reads `ARM64_DIR` only on `main` runs; locally `artifacts/publish/osx-arm64` exists, which is the part checkable here.
- The `changes` job log showing `backend=true` for an `.github/actions/`-only change.

### Same-tree comparison (orchestrator, 2026-10-31) — supersedes the size deltas above

The before/after comparison above published "before" in a separate checkout, and its executable came out about
3 % smaller on every platform. Publishing commit `727c8dcb8` (new path) in that same separate checkout gave the
"before" sizes too, so the gap followed the checkout, not the output path. To isolate the one variable this Story
changes, the old in-project `-o` path and the new `artifacts/publish/<rid>` path were published one after the
other from **this** worktree, with the CI flags, and listed by name and byte size
(`scratchpad/same-tree-compare.sh`):

| RID | Old path vs new path | Files | Bytes |
|---|---|---|---|
| linux-x64 | identical names and sizes | 219 | 156,362,705 |
| win-x64 | identical names and sizes | 224 | 160,980,439 |
| osx-arm64 | identical names and sizes | 220 | 167,065,048 |

Per RID published on its own, the move changes nothing. The CI sequence publishes three RIDs into one base folder, though, and that does change the artifacts; see "CI publish sequence" below, which supersedes this table for CI.

Second publish into the same folder (linux-x64):

| Path | First publish | Second publish | Nested `publish/` inside |
|---|---|---|---|
| old, in the project folder | 219 files | 206 files | **1** (the nesting this Story removes) |
| new, `artifacts/publish/` | 219 files | 206 files | 0 |

The 13 files missing after a second publish into the same folder (`Factories/DemoData/*.csv` and
`certs/LighthouseCert.pfx`, all `CopyToOutputDirectory=PreserveNewest`) go missing on **both** paths, so this predates
the Story and is not caused by it. CI always publishes into a fresh folder, so releases are unaffected; a developer
who runs `update-sidecar` twice gets a sidecar without demo data and certificate. Recorded as a finding for the
maintainer, not fixed here (it would change what a repeat publish produces).

### CI publish sequence (orchestrator, 2026-10-31)

Reproduced build-backend's exact sequence (win-x64, then linux-x64, then osx-arm64 into one base folder, then `rm -f`
of the top-level `appsettings.Development.json`) on the old in-project base and on the new artifacts base, from the
same tree, listing name, size and sha256. A control ran the new sequence twice from clean.

| Check | Result |
|---|---|
| Control (new sequence twice) | 0 files differ run to run: the publish is deterministic |
| win-x64 | 224 vs 224 files, 0 differ (published first, nothing to pick up) |
| linux-x64 | old 225, new 219. The 6 old-only files are a nested copy of the win-x64 output's JSON under `publish/win-x64/`: `Lighthouse.staticwebassets.endpoints.json`, `appsettings.Development.json`, `appsettings.json`, `wwwroot/manifest.json`, `wwwroot/sbom/backend.cdx.json`, `wwwroot/sbom/frontend.cdx.json` |
| osx-arm64 | old 238, new 220. The 18 old-only files are the same six nested under `publish/win-x64/`, under `publish/linux-x64/`, and under `publish/linux-x64/publish/win-x64/` |
| Files on both sides | sha256-identical, for all three RIDs |

Consequence: the old CI path shipped nested copies of other platforms' JSON inside the linux-x64 and osx-arm64
artifacts, including `appsettings.Development.json` (the step that strips it only removes the top-level copy; the file
holds local development settings and the example client secret the pre-push scan already acknowledges). The new path
ships none of them. So the move does change the release artifacts, by removing only that nested junk; nothing the app
loads changes, because the nested files sit in folders the app never reads.

The earlier cross-checkout executable size gap (about 3 %) is a property of that separate checkout, not of the path:
from one tree, old and new are sha256-identical. Its exact cause in that checkout was not investigated further.

## Wave: DELIVER / [REF] Review findings

- **F1** (CI artifacts differ): confirmed and handled as above. Maintainer to confirm at the hold that removing the
  nested junk is in scope for a behaviour-free item.
- **F3, F4, F8**: fixed in the guard commit `6015a5e7d` (MSBuild files scanned, repository root pinned, known-gap list
  corrected). **F2 label**: fixed in `163115971` (the connector classifier label no longer reads as `github_changes`),
  together with the plain-words comment in `ci_changes.yml`.
- **F2** (classifier tests run by no workflow): fixed on the maintainer's word — the `changes` job now runs `test-path-classifier.sh` right after checkout, so a broken rule stops every CI run. Red check: with the `.github/actions/` part reverted, the script exits 1 naming two cases.
- **F5** (guard runs only when the backend suite runs): accepted, documented lag.
- **F6** (`printf | grep -q` under pipefail with very large diffs): deferred; latent, a pattern shared by every
  classifier function, and the step runs without pipefail today.
- **F7** (unreadable directories error loudly; pruning is by folder name): accepted.
- **F9**: resolved by the sha256 comparison above.
