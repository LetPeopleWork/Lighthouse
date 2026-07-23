# Slice 01 — arm64-only macOS Standalone (Intel sunset)

**Goal:** Ship the macOS Standalone as a native Apple-Silicon build and drop Intel from the pipeline, feed, config floor, and docs.

## IN scope
- `build-backend` composite: remove `osx-x64` publish (keep `osx-arm64`).
- `ci_package-macos-standalone.yml`: drop `x86_64-apple-darwin` Rust target, remove `lipo` universal fusion, remove x86_64 sidecar + its rpath patch, Tauri `--target aarch64-apple-darwin`, fix bundle output paths (`universal-apple-darwin` → `aarch64-apple-darwin`).
- `tauri.conf.json`: `minimumSystemVersion` 10.15 → 11.0.
- `ci_generate-update-feed.yml`: remove `darwin-x86_64` platform key.
- Docs/compliance/release-notes: Universal/Intel → Apple Silicon (arm64).

## OUT scope
- Intel sunset/notice release; frozen Intel download; Windows/Linux; Rosetta detection; in-app arch messaging.

## Learning hypothesis
Removing the lipo/universal path **disproves** the risk that single-arch arm64 breaks Tauri signing, rpath resolution, notarization, Gatekeeper, or the updater — if `ci_verifymacos.yml` goes red, that risk was real. **Confirms** if verify stays green and the DMG runs native.

## Acceptance criteria
- arm64-only DMG builds green on `main`; `lipo -archs` on sidecar/dylib = `arm64` only.
- `ci_verifymacos.yml`: codesign verify + notarization staple + `spctl` Gatekeeper + mount all pass.
- `tauri-update.json` has `darwin-aarch64`, no `darwin-x86_64`.
- `minimumSystemVersion` = 11.0.
- Docs + compliance + release notes state Apple Silicon only.

## Dependencies
Apple signing secrets (existing). None on prior waves.

## Effort estimate
<1 day (≤6h crafter dispatch). Reference class: prior standalone-packaging edits (`ci_package-*-standalone.yml`).

## Reference class / risk
Signing + notarization + updater-feed shape are the fragile bits — CI-only feedback (can't fully reproduce notarization locally). Watch rpath: only the `aarch64` sidecar path survives; verify `@executable_path/../Resources/resources` still correct without the universal copy step.
