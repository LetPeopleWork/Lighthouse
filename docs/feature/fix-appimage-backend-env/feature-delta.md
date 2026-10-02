# fix-appimage-backend-env — Linux standalone stuck on the splash screen

Bugfix flow (`/nw-bugfix`). The RCA below was approved by the maintainer on 2026-10-02.

## Wave: DELIVER / [REF] Root Cause

The Linux standalone AppImage v26.10.2.25 never leaves the splash screen. The .NET backend aborts at startup with `No usable version of libssl was found` (SIGABRT), and v26.9.24.6 works on the same host.

- **Trigger.** Renovate commit `92b08e6ba` bumped `@tauri-apps/cli` from 2.11.5 to 2.12.1. Its AppImage bundler now ships extra gio modules (libproxy, gnome-proxy, dconf).
  - The libproxy chain is `libproxy` → `libcurl-gnutls`, which pulls in `libssh.so.4` and `libldap` → `libsasl2.so.2`.
  - Those libraries bring Ubuntu's `libcrypto.so.3` (OpenSSL 3.0.13) into `usr/lib`, but no `libssl`.
- **Mechanism.** The compiled AppImageKit `AppRun.wrapped` prepends `$APPDIR/usr/lib/:…` to `LD_LIBRARY_PATH`.
  - It also prepends to PATH, XDG_DATA_DIRS, GSETTINGS_SCHEMA_DIR, PYTHONPATH, PERLLIB, QT_PLUGIN_PATH and GST_PLUGIN_SYSTEM_PATH(_1_0).
  - The linuxdeploy GTK hook overwrites the GTK_*, GDK_PIXBUF_MODULE_FILE, GIO_MODULE_DIR and GI_TYPELIB_PATH variables.
  - The Tauri shell spawns the backend sidecar (`src-tauri/src/lib.rs`, `shell.sidecar(...)`), and the sidecar inherits that whole environment.
  - On a host with OpenSSL newer than 3.0, the system `libssl.so.3` gets the bundled `libcrypto.so.3`. The linker rejects the pair with `version OPENSSL_3.3.0 not found`, and .NET aborts.
- **Evidence.** With the AppImage's `LD_LIBRARY_PATH`, the extracted backend exits 134. Without it, the same backend logs `Now listening on`. The backend's `RUNPATH=$ORIGIN/../lib` is the same in both images and is not involved.
- **Latent since before the trigger.** The backend always inherited the AppImage environment. Older images simply bundled no OpenSSL library for it to pick up.
- **Why it went unseen.** CI never launches or inspects the AppImage, and `src-tauri` has no tests. The Ubuntu 24.04 runner's own OpenSSL is 3.0.13, so a smoke launch there would have passed.

## Wave: DELIVER / [REF] User Decisions

- D1. The backend gets a clean environment: every entry under `APPDIR` is removed from every variable, and the AppImage identity variables are dropped. The Tauri CLI is **not** pinned.
- D2. CI runs the new Rust unit tests, and fails the build when the AppImage bundles `libssl`. A bundled `libcrypto` alone is harmless once the backend's environment is clean. A bundled `libssl` would be picked up through the backend's RUNPATH and would override the system OpenSSL with a stale pair.
- D3. Out of scope: an error dialog when the backend dies before reporting its URL (declined), and pinning the Tauri CLI (declined).

## Wave: DELIVER / [REF] Scope

Linux AppImage only. The new environment function returns the parent environment unchanged when `APPDIR` is absent or empty, and on any target other than Linux. Windows, macOS, `tauri dev` and non-AppImage Linux runs are unaffected.

## Wave: DELIVER / [REF] Implementation Summary

The Tauri shell now spawns the backend sidecar from a cleared environment built by `backend_environment` in `Lighthouse.Frontend/src-tauri/src/backend_env.rs`. That function is pure, takes the parent environment, and does nothing unless `APPDIR` is set on Linux.
- Inside an AppImage it drops `APPDIR`, `APPIMAGE`, `ARGV0`, `OWD` and `APPIMAGE_UUID`.
- A variable with at least one `:`-entry under the mount has those entries and its empty entries removed, and is left out when nothing remains.
- Every other variable passes through byte for byte.
- Tauri's own process keeps its environment, so the updater still sees `APPIMAGE`.
- Only Linux rebuilds the sidecar's environment. On Windows and macOS the backend inherits Tauri's environment unchanged, as before the fix, because rebuilding it on Windows can drop the hidden per-drive working-directory variables.

CI (`ci_package-linux-standalone.yml`) now runs `cargo test --lib` after the AppImage build, extracts the image, and fails when it bundles `libssl.so*`. A bundled `libcrypto.so*` is only reported as a notice.

## Wave: DELIVER / [REF] Files Modified

- `Lighthouse.Frontend/src-tauri/src/backend_env.rs`: new; the environment rule and 7 unit tests.
- `Lighthouse.Frontend/src-tauri/src/lib.rs`: a Linux-gated `mod backend_env;`, and on Linux `.env_clear().envs(backend_environment(vars_os()))` on the sidecar spawn.
- `.github/workflows/ci_package-linux-standalone.yml`: Rust unit tests, and the bundled-libssl check.

## Wave: DELIVER / [REF] Quality Gates

| Gate | Outcome |
|---|---|
| Unit tests | `cargo test --lib`: 7/7 green. Each test was RED against a passthrough stub or a targeted mutant. |
| Build / lint | `cargo build` has zero warnings; `cargo clippy --all-targets -D warnings` and `cargo fmt --check` are clean. |
| CI check | Run locally: PASS on the real v26.10.2.25 image (libcrypto reported, no libssl); FAIL on a copy with a dummy `libssl.so.3`. actionlint is clean. |
| Live verification | The fixed shell binary was swapped into the broken v26.10.2.25 image and launched through its own `AppRun` on CachyOS (OpenSSL 3.6.5), with HOME pointed at a scratch directory. There was no libssl error and the backend logged `Now listening on`. The unmodified image aborts with SIGABRT. |
| Refactor (L1-L6) | One behaviour-preserving commit: the doc comment now states the rule exactly, the runtime-variable constant was renamed, and a pass-through no longer clones. |
| Adversarial review | Round 1 approved; an orchestrator probe then found that a value containing `::` was corrupted (`ASPNETCORE_URLS=http://[::1]:5000` became `http://[:1]:5000`), fixed in step 01-03. Round 2 (final code) approved. Of its open points, the trailing-slash APPDIR and the resolved-path mismatch were ruled out against a captured real AppRun environment; the env rebuild on Windows/macOS was fixed by making it Linux-only (step 01-05). Running the unit tests only on `main`, after the build, is accepted as the job's existing shape. |
| Mutation (cargo-mutants) | Final code: 19/19 caught (100%). The earlier survivor `&&`→`||` in `is_under` was killed by step 01-04, and the non-Linux passthrough that produced the other two survivors was deleted in step 01-05. |
| DES integrity | `des-verify-integrity`: all 5 steps have complete traces. |

## Wave: DELIVER / [REF] Known Gaps

- The `--appimage-extract` and `realpath` lines of the CI check run for the first time on the next `main` build. Only the scan body was exercised locally.
- Not compiled for Windows or macOS locally (no rustup cross targets). On those targets the whole module and the environment rebuild are compiled out, so the spawn chain there is exactly what it was before the fix.
- The backend's `RUNPATH=$ORIGIN/../lib` still lets a future bundled `libssl` override the host's OpenSSL. The CI check exists to catch exactly that.
