# ADR-105: macOS Standalone is Apple-Silicon-only (arm64), Intel dropped

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: macos-arm64-only-standalone (ADO User Story 5543)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

The macOS Standalone (Tauri) ships a **universal** DMG: the .NET backend is published for `osx-x64` and `osx-arm64`, `lipo`-fused into a fat Mach-O, and Tauri builds `--target universal-apple-darwin`. The Tauri update feed (`tauri-update.json`) maps **both** `darwin-aarch64` and `darwin-x86_64` to the same universal tarball. `minimumSystemVersion` is `10.15` (Catalina — Intel era).

Apple has ended Intel support in macOS. Carrying the `x86_64` slice is now dead weight: it doubles the download, adds a `lipo` fusion step and an extra sidecar/rpath path, and prolongs signing/notarization for an architecture with no future.

## Decision

Ship the macOS Standalone as **`aarch64-apple-darwin` only**.

1. **Build**: drop the `osx-x64` .NET publish; remove the `lipo` universal fusion; drop the `x86_64-apple-darwin` Rust target and the x86_64 sidecar + its rpath patch. Tauri builds `--target aarch64-apple-darwin`.
2. **Update feed**: remove the `darwin-x86_64` platform key from `tauri-update.json`. This is the entire Intel cutoff mechanism — the Tauri updater treats a missing platform key as "no update available", so existing Intel installs silently freeze on their last version. **Hard cutoff, no sunset notice, no frozen Intel download retained.**
3. **Floor**: bump `minimumSystemVersion` `10.15` → `11.0` (Big Sur) — no Apple-Silicon Mac ever ran below 11.0, so the floor is now honest.
4. **Docs**: distribution docs, compliance files, and release notes state Apple Silicon (arm64), not "Universal" / "Intel".

## Alternatives considered

- **One final sunset release** (last universal build + in-app/notes notice, then arm64-only next release): kinder to Intel users, but adds a coordination step and an ephemeral notice for a shrinking cohort. Rejected — maintainer chose the simplest cut.
- **Keep a frozen Intel `.dmg` downloadable** (arm64 updates only, `darwin-x86_64` feed → frozen version): preserves an Intel escape hatch, but keeps a stale, unmaintained, unsupported artifact on the releases page and in the feed indefinitely. Rejected.
- **Stay universal**: rejected — the whole point is that Intel is sunset; universal is pure carrying cost.

## Consequences

- **Positive**: smaller/faster macOS build (no lipo, one RID), shorter notarization, simpler pipeline, honest support floor, honest docs.
- **Negative / accepted**: Intel Mac users silently stop receiving updates (no in-app signal). Mitigation is documentation-only: release notes state Intel Macs remain on their last installed version. Windows/Linux standalone are unaffected (remain x64).
- **Verify unchanged**: `ci_verifymacos.yml` is arch-agnostic (sign/notarize/staple/Gatekeeper/mount) and needs no change. Recommended DISTILL guard: assert `lipo -archs` = `arm64` on the sidecar and `darwin-x86_64` absent from the generated feed.
