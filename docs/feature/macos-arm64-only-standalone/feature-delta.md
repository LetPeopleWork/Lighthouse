# Feature Delta — macOS arm64-only Standalone (Intel sunset)

**ADO:** User Story 5543 — "MacOS Support For Intel Based Apps ending" (State: Active, Tag: Release Notes)
**Feature type:** Infrastructure (CI/build/packaging) with a user-visible distribution outcome
**Density:** lean + ask-intelligent (`~/.nwave/global-config.json`)
**Wave status:** DELIVER complete (code + CI guards + docs) → ready for review / commit

---

## Wave: DISCUSS / [REF] Persona

- **P1 — mac-standalone-user (Apple Silicon):** downloads the macOS Standalone `.dmg`, runs Lighthouse locally, relies on background auto-update. Beneficiary of the native arm64 build.
- **P2 — [[lighthouse-maintainer]]:** owns the release pipeline; wants the macOS build to stay green, signed, notarized, and cheap to maintain once Intel is a dead target.

No dedicated persona YAML created (thin infra feature; reuses existing `lighthouse-maintainer`).

## Wave: DISCUSS / [REF] JTBD one-liner

> When I run Lighthouse Standalone on my Apple-Silicon Mac, I want a natively-built, signed, auto-updating app that isn't carrying a dead Intel slice, so I can stay current on the only Mac architecture Apple still supports — without Rosetta overhead or a doubled download.

Maintainer sub-job: *keep the macOS pipeline building on supported hardware without spending signing/notarization/lipo effort on an architecture Apple has sunset.*

## Wave: DISCUSS / [REF] Locked decisions

| # | Decision | Verdict | Rationale |
|---|----------|---------|-----------|
| D1 | Target architecture | **Apple Silicon only (`aarch64-apple-darwin`)** — drop the `x86_64` slice | Apple has ended Intel support; universal lipo build is now dead weight. New users get a native arm64 DMG. |
| D2 | Existing Intel users | **Hard cutoff now** — remove `darwin-x86_64` from the update feed | Intel installs silently freeze on their current version; no sunset notice, no frozen download kept. (Simplest; user-approved.) |
| D3 | `minimumSystemVersion` | **10.15 → 11.0** | No Apple-Silicon Mac ever ran below macOS 11 (Big Sur); the floor is now honest. |

## Wave: DISCUSS / [REF] User stories

### US-01 — Native Apple-Silicon standalone *(value)*
As a Mac (Apple Silicon) user, I download the macOS Standalone and get a native arm64 app, not a fat universal binary.

`job_id:` mac-standalone-native (see SSOT note below)

**### Elevator Pitch**
Before: the macOS `.dmg` is a fat universal binary carrying a dead Intel slice; arm64 users pay for x86_64 bytes they never run.
After: download `Lighthouse-<ver>.dmg` from the Releases page → app launches; Activity Monitor shows Kind **Apple** (not Intel/Universal) and Gatekeeper passes.
Decision enabled: the user trusts the app is native and current on supported hardware.

**ACs**
- AC1: The DMG bundle is built with Tauri `--target aarch64-apple-darwin` (no `universal-apple-darwin` bundle produced).
- AC2: The bundled sidecar backend (`Lighthouse.Backend-aarch64-apple-darwin`) is a single-arch arm64 Mach-O (`lipo -archs` reports `arm64` only; no `x86_64`).
- AC3: `ci_verifymacos.yml` passes: `codesign --verify --deep --strict`, notarization staple valid, `spctl --assess` Gatekeeper pass, app mounts and `Lighthouse.app` present.
- AC4: `minimumSystemVersion` in `tauri.conf.json` is `11.0`.

### US-02 — Honest distribution surface + Intel hard cutoff *(value)*
As a maintainer, the update feed and distribution docs no longer offer Intel builds or claim Intel support.

`job_id:` maintainer-honest-distribution

**### Elevator Pitch**
Before: `tauri-update.json` maps `darwin-x86_64` → a universal tar, and docs claim "runs natively on both Apple Silicon and Intel" — both now false.
After: an Intel Mac's updater query against the published `tauri-update.json` finds no `darwin-x86_64` platform key → the updater reports "no update available" instead of handing an arm64-only artifact; docs/compliance state Apple Silicon only.
Decision enabled: Intel users aren't offered an update that can't run; readers see accurate architecture support before downloading.

**ACs**
- AC1: The generated `tauri-update.json` contains `darwin-aarch64` but **no** `darwin-x86_64` platform key.
- AC2: Backend publish no longer emits `osx-x64` (only `osx-arm64`); no `lipo` step remains in `ci_package-macos-standalone.yml`.
- AC3: Distribution docs updated — `docs/Installation/standalone.md`, `docs/compliance/cra-technical-file.md`, `docs/compliance/distribution-and-versioning.md`, `docs/releasenotes/releasenotes.md` say **Apple Silicon (arm64)**, not "Universal" / "Intel".
- AC4: Release-notes entry (Tag: Release Notes) states macOS builds are now Apple-Silicon-only and Intel Macs remain on their last installed version.

## Wave: DISCUSS / [REF] Acceptance strategy note

This is a packaging/pipeline feature — the primary "tests" are the existing CI verify workflows (`ci_verifymacos.yml`) run on `macos-latest`, plus artifact-shape assertions (`lipo -archs`, feed-JSON key absence). No backend/frontend unit tests change. DISTILL should target CI-observable assertions, not new NUnit/Vitest suites.

## Wave: DISCUSS / [REF] Definition of Done

1. arm64-only DMG builds green on `main` (Tauri `aarch64-apple-darwin`).
2. Sidecar + dylib are single-arch arm64 (`lipo -archs` = `arm64`).
3. `ci_verifymacos.yml` green: sign + notarize + staple + Gatekeeper + mount.
4. `tauri-update.json` has no `darwin-x86_64` key.
5. `minimumSystemVersion` = 11.0.
6. `build-backend` composite no longer publishes `osx-x64`.
7. Docs + compliance + release notes reflect Apple-Silicon-only.
8. Release-notes item drafted (ADO tag present).
9. No warnings/CI-learnings regressions; full pipeline green on `main`.

## Wave: DISCUSS / [REF] Out of scope

- Sunset/notice release for Intel users (D2 = hard cutoff, no notice).
- Keeping a frozen Intel `.dmg` downloadable.
- Windows / Linux standalone (unchanged; remain x64).
- In-app "unsupported architecture" messaging.
- Rosetta detection.

## Wave: DISCUSS / [REF] WS strategy

**A (single thin slice).** One end-to-end slice: the pipeline emits an arm64-only signed/notarized DMG that `ci_verifymacos` accepts, the feed drops `darwin-x86_64`, and docs are truthful. Learning hypothesis: *dropping the lipo/universal path does not break signing, rpath, notarization, or the updater.* Not decomposed — merging avoids identical-except-scale slices (carpaccio taste test).

## Wave: DISCUSS / [REF] Driving ports (surfaces touched)

- CI: `.github/actions/build-backend/action.yml`, `.github/workflows/ci_package-macos-standalone.yml`, `.github/workflows/ci_generate-update-feed.yml`.
- App config: `Lighthouse.Frontend/src-tauri/tauri.conf.json`.
- Docs: `docs/Installation/standalone.md`, `docs/compliance/{cra-technical-file,distribution-and-versioning}.md`, `docs/releasenotes/releasenotes.md`.
- User-invocable surface: GitHub Releases page (download) + Tauri background updater (feed JSON).

## Wave: DISCUSS / [REF] Pre-requisites

- Apple signing secrets already wired (`APPLE_*`, `TAURI_SIGNING_PRIVATE_KEY`) — unchanged.
- `macos-latest` GitHub runner still supports `aarch64-apple-darwin` cross-target (it does).
- No new dependency on prior waves.

## Wave: DISCUSS / [REF] nw-discuss checklist

- **RBAC impact:** N/A, because this is a build/distribution change with zero authorization surface — no `IRbacAdministrationService`, `useRbac()`, or endpoint changes.
- **Lighthouse-Clients CLI/MCP versioning:** N/A, because the CLI/MCP clients are not part of the macOS standalone pipeline and ship no macOS-arch-specific artifact.
- **Website marketing surface:** IN scope for DELIVER — the website/download messaging and `docs/releasenotes` currently claim Intel support (`releasenotes.md:764`); must be corrected to Apple-Silicon-only. Flagged, not skipped.

## Wave: DISCUSS / [REF] Scope Assessment

**PASS.** 2 stories, 1 bounded surface (macOS packaging), ~4 file edits + doc sync, <1 day. No oversized signal fires.

---

## Wave: DISCUSS / [REF] Wave decisions summary

- **[D1]** arm64-only target — Apple ended Intel support; drop universal lipo path.
- **[D2]** Hard cutoff — remove `darwin-x86_64` feed key; Intel freezes silently.
- **[D3]** `minimumSystemVersion` 11.0 — honest arm64 floor.
- **Primary need:** native, current, honest macOS Standalone on the only architecture Apple still supports.
- **Upstream changes:** none (no DISCOVER/DIVERGE artifacts for this feature).

## SSOT note

`jobs.yaml` job ids `mac-standalone-native` / `maintainer-honest-distribution` are new and thin (infra-adjacent). Recommend adding them to `docs/product/jobs.yaml` in DESIGN only if a reviewer requires strict traceability; not created here to keep the SSOT free of pipeline-maintenance micro-jobs.

---

## Wave: DESIGN / [REF] DDD list

- **DDD-1** — Scope = **System/infrastructure** (CI packaging). No domain model, no aggregates, no application components. `@nw-ddd-architect` / `@nw-solution-architect` not dispatched: zero new classes, zero persistence, zero endpoints. Verdict: pipeline-only design.
- **DDD-2** — Distribution contract = **single-arch arm64** replaces universal. macOS artifact identity changes shape (`universal-apple-darwin` → `aarch64-apple-darwin`); update-feed platform map shrinks. Verdict: ADR-105.
- **DDD-3** — Intel cutoff = **feed-key deletion**, not a code branch. Removing the `darwin-x86_64` key is the whole cutoff mechanism (Tauri updater treats a missing platform key as "no update"). Verdict: no runtime detection code.
- **DDD-4** — `minimumSystemVersion` floor = **config data**, not logic. 10.15 → 11.0 in `tauri.conf.json`. Verdict: one-line config edit.

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change | Type |
|-----------|------|--------|------|
| Backend publish (osx-x64) | `.github/actions/build-backend/action.yml` | remove `osx-x64` publish step + its dev-settings rm line | DELETE |
| macOS package pipeline | `.github/workflows/ci_package-macos-standalone.yml` | drop `x86_64-apple-darwin` Rust target; remove `lipo` fusion (2×); remove x86_64 sidecar copy + rpath loop entry; Tauri `--target aarch64-apple-darwin`; bundle paths `universal-apple-darwin`→`aarch64-apple-darwin` | MODIFY |
| Update feed | `.github/workflows/ci_generate-update-feed.yml` | remove `darwin-x86_64` platform key from `jq` feed template | MODIFY |
| Tauri config | `Lighthouse.Frontend/src-tauri/tauri.conf.json` | `minimumSystemVersion` 10.15 → 11.0 | MODIFY |
| Verify workflow | `.github/workflows/ci_verifymacos.yml` | unchanged — already arch-agnostic (DMG sign/notarize/mount) | NONE |
| Docs / compliance / release notes | `docs/Installation/standalone.md`, `docs/compliance/{cra-technical-file,distribution-and-versioning}.md`, `docs/releasenotes/releasenotes.md` | Universal/Intel → Apple Silicon (arm64) | MODIFY (DELIVER) |

## Wave: DESIGN / [REF] Driving ports

- **GitHub Releases page** — user downloads `Lighthouse-<ver>.dmg` (now arm64 native). Unchanged surface, changed artifact.
- **Tauri background updater** — reads `tauri-update.json`, queries by `{target}-{arch}`. arm64 Macs hit `darwin-aarch64`; Intel Macs hit the now-absent `darwin-x86_64` → no update.

## Wave: DESIGN / [REF] Driven ports + adapters

- **Apple notarization** (`xcrun notarytool`) — unchanged; single-arch DMG notarizes identically.
- **Apple codesign / Gatekeeper** (`codesign`, `spctl`) — unchanged; signs arm64 Mach-O.
- **GitHub Release asset store** (`gh release upload`) — unchanged; same asset names.
- **Tauri signing** (`TAURI_SIGNING_PRIVATE_KEY`) — unchanged; signs the arm64 updater tar.

## Wave: DESIGN / [REF] Technology choices

- Tauri action `tauri-apps/tauri-action@v1.0.0` — pinned, unchanged.
- Rust toolchain: `aarch64-apple-darwin` only (was `aarch64,x86_64`).
- .NET 10 publish RID: `osx-arm64` only (was `osx-x64` + `osx-arm64`).
- Runner: `macos-latest` (Apple Silicon) — native arm64 build, no lipo.

## Wave: DESIGN / [REF] Decisions table

| DDD-N | Decision |
|-------|----------|
| DDD-1 | Infra-only design; no architect subagent, no domain/app model |
| DDD-2 | arm64 single-arch replaces universal (ADR-105) |
| DDD-3 | Intel cutoff = feed-key deletion, no runtime detection |
| DDD-4 | minSystemVersion floor bumped to 11.0 (config only) |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|--------------------|------|---------|----------|---------------|
| macOS package workflow | `ci_package-macos-standalone.yml` | Full packaging | **EXTEND** | Modify existing steps (drop lipo, retarget Tauri); ~30 LOC deleted, ~5 changed. No new workflow. |
| build-backend composite | `build-backend/action.yml` | Publishes all RIDs incl. osx | **EXTEND** | Delete one publish block; win/linux/osx-arm64 untouched. |
| Update-feed workflow | `ci_generate-update-feed.yml` | Emits platform map | **EXTEND** | Remove one `jq` map line. No new feed generator. |
| Verify workflow | `ci_verifymacos.yml` | Verifies DMG | **REUSE AS-IS** | Already arch-agnostic — sign/notarize/mount don't assert arch. Zero change. |

**Zero CREATE NEW.** All deletion/edit inside existing pipeline files.

## Wave: DESIGN / [REF] Open questions (deferred)

- **DISTILL**: add CI guards asserting artifact shape — `lipo -archs` on the sidecar proves `arm64`-only; grep asserts `darwin-x86_64` absent from generated `tauri-update.json`. Recommended (cheap, CI-observable). Deferred to acceptance design.
- **DELIVER**: exact prose for the "Intel Macs frozen on last build" release-note line (Tag: Release Notes).
- **DELIVER**: does the public website download page (separate repo) also claim Intel? Verify + sync alongside docs.

## Wave: DESIGN / [REF] Wave decisions summary (DESIGN)

- **[D1]** Infra-only, no architect subagent — no domain/app surface (DDD-1).
- **[D2]** arm64 single-arch, ADR-105 — universal path is dead weight post-Intel-sunset.
- **[D3]** Cutoff via feed-key deletion — simplest honest mechanism (DDD-3).
- **Pattern:** none new — edits to existing GitHub Actions pipeline; app ports-and-adapters untouched.
- **Paradigm:** unchanged (OOP backend / functional-leaning React) — no code paradigm surface.
- **Upstream changes:** none. DISCUSS change-surface confirmed exact.

---

## Wave: DISTILL / [REF] Acceptance strategy

Port-to-port acceptance for a **packaging** feature = **CI-observable artifact-shape assertions**, not NUnit/Vitest. The "port" is the produced artifact (DMG / sidecar / feed JSON / config); the "test" runs inside the existing `macos-latest` workflows. No new test project. No step-reuse target (shell steps, not Given-When-Then automation code). Self-completeness audit: 4 ACs across 2 stories, each maps to exactly one executable guard below; AC3 (sign/notarize/Gatekeeper) already covered by existing `ci_verifymacos.yml` — no new guard.

## Wave: DISTILL / [REF] Acceptance criteria → executable guards

| ID | Story/AC | Given | When | Then (executable) | Where |
|----|----------|-------|------|-------------------|-------|
| **A1** | US-01 AC2 (+AC1) | the macOS package job has placed the arm64 sidecar | `lipo -archs` runs on `binaries/Lighthouse.Backend-aarch64-apple-darwin` | output is exactly `arm64` and contains no `x86_64` | new step in `ci_package-macos-standalone.yml` |
| **A2** | US-01 AC4 | the Tauri config is version-patched | `jq -r .bundle.macOS.minimumSystemVersion tauri.conf.json` | equals `11.0` | new guard step in `ci_package-macos-standalone.yml` |
| **A3** | US-01 AC1 | the Tauri build finished | the bundle dir is located | `target/aarch64-apple-darwin/release/bundle` exists; no `universal-apple-darwin` bundle produced | existing "Find and rename artifacts" step path |
| **A4** | US-02 AC1 | the update-feed JSON is generated | `jq -e '.platforms\|has("darwin-aarch64") and (has("darwin-x86_64")\|not)'` on `/tmp/tauri-update.json` | exit 0 (arm64 present, x86_64 absent) | new step in `ci_generate-update-feed.yml` after "Build update feed JSON" |
| **A5** | US-01 AC3 | signed DMG artifact exists | existing verify runs `codesign`/`stapler`/`spctl`/`hdiutil` | all pass | `ci_verifymacos.yml` — **existing, unchanged** |
| **A6** | US-02 AC2 | the package workflow source | grep the workflow file | no `lipo` invocation and no `osx-x64` path remain | optional static drift-guard (CI or review) |
| **A7** | US-02 AC3/AC4 | docs/compliance/release-notes edited | reviewer/`update-docs` diff check | "Universal"/"Intel" replaced by Apple Silicon (arm64) in the 4 doc files | DELIVER doc-sync (manual/`/update-docs`) |

## Wave: DISTILL / [REF] Guard snippets (for DELIVER)

**A1 — arm64-only sidecar** (add after "Place sidecar binary and resources for Tauri"):
```bash
SIDE="./Lighthouse.Frontend/src-tauri/binaries/Lighthouse.Backend-aarch64-apple-darwin"
ARCHS=$(lipo -archs "$SIDE")
echo "sidecar archs: $ARCHS"
[ "$ARCHS" = "arm64" ] || { echo "FAIL: expected arm64-only, got '$ARCHS'"; exit 1; }
```

**A2 — minimumSystemVersion floor** (add after "Update Tauri Version"):
```bash
MIN=$(jq -r '.bundle.macOS.minimumSystemVersion' Lighthouse.Frontend/src-tauri/tauri.conf.json)
[ "$MIN" = "11.0" ] || { echo "FAIL: minimumSystemVersion=$MIN, expected 11.0"; exit 1; }
```

**A4 — Intel key absent from feed** (add after "Build update feed JSON" in `ci_generate-update-feed.yml`):
```bash
jq -e '.platforms | has("darwin-aarch64") and (has("darwin-x86_64") | not)' /tmp/tauri-update.json \
  || { echo "FAIL: feed platform map wrong (darwin-x86_64 must be absent, darwin-aarch64 present)"; exit 1; }
```

## Wave: DISTILL / [REF] Out of test scope

- No unit/integration tests (no code branch, no endpoint, no persistence).
- Rosetta/arch runtime detection — feature explicitly excludes it (D2 hard cutoff).
- Notarization content parity across arch — single arch, N/A.

## Wave: DISTILL / [REF] Wave decisions summary (DISTILL)

- **[D1]** Acceptance = 3 new inline CI guards (A1/A2/A4) + reuse existing `ci_verifymacos.yml` (A5). Zero new test framework surface.
- **[D2]** A6 (source drift-guard) + A7 (doc-sync) are DELIVER-time verifications, not automated gates — flagged, not skipped.
- **Upstream changes:** none. DESIGN open-question "add CI guards" resolved here (A1/A2/A4).

---

## Wave: DELIVER / [REF] Shipped changes

| File | Change |
|------|--------|
| `.github/actions/build-backend/action.yml` | removed `osx-x64` publish + its dev-settings rm line (osx-arm64 remains) |
| `.github/workflows/ci_package-macos-standalone.yml` | Rust target `aarch64-apple-darwin` only; removed universal-binary/lipo step + osx-universal dev-settings step; Place-sidecar sources `osx-arm64` (single sidecar); **Guard A1** (`lipo -archs`=arm64); **Guard A2** (minSystemVersion=11.0); rpath step arm64-only; Tauri `--target aarch64-apple-darwin`; notarize + find-artifacts bundle path `aarch64-apple-darwin` |
| `.github/workflows/ci_generate-update-feed.yml` | removed `darwin-x86_64` platform key; **Guard A4** (jq asserts aarch64 present, x86_64 absent) |
| `Lighthouse.Frontend/src-tauri/tauri.conf.json` | `minimumSystemVersion` 10.15 → 11.0 |
| `docs/Installation/standalone.md` | added "Apple Silicon only" requirement note (arm64, macOS 11+, Intel unsupported) |
| `docs/compliance/cra-technical-file.md` | macOS row "Universal" → "Apple Silicon / arm64, macOS 11+" |
| `docs/compliance/distribution-and-versioning.md` | 2× "macOS (Universal)" → "macOS (Apple Silicon / arm64)" |
| `docs/releasenotes/releasenotes.md` | macOS line "natively on both Apple Silicon and Intel" → "Apple Silicon (arm64, macOS 11+)…Intel Macs no longer supported" |

**Verified locally**: all 3 workflow/action YAMLs parse (`yaml.safe_load` OK); no residual `osx-x64` / `universal-apple` / `10.15` / real `darwin-x86_64` feed key. **Not verifiable locally** (CI-only, `macos-latest`): signing / notarization / Gatekeeper / actual arm64 DMG build — the A1/A2/A3/A4 guards fail-closed in CI.

**Not committed / not pushed** — awaiting user review. ADO 5543 still Active.

**DELIVER checklist**: docs synced (this wave, not batched ✓); no RBAC surface (N/A); no clients CLI/MCP (N/A); website marketing surface → `docs/releasenotes` corrected here, **separate public website repo not verified** (deferred — A7).
