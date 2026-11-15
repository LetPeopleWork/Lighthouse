# Story 6601 — Linux standalone builds no longer break when GitHub retires a runner image

Delivered 2026-11-15. ADO #6601 (Continuous Improvement, unparented). Decision record: ADR-284
(`docs/product/architecture/adr-284-linux-appimage-builds-in-a-pinned-ubuntu-24-04-container-and-the-glibc-floor-is-2-39.md`).
Workspace: `docs/feature/story-6601-linux-standalone-glibc-floor/`.

## What was delivered

The Linux AppImage used to be built on whatever Ubuntu the GitHub runner image offered, with WebKitGTK pinned to that
image's package version. When `ubuntu-latest` moved to 26.04 the pins vanished and the release was blocked; the quick
fix pinned `ubuntu-24.04`, which would have broken again on that image's retirement.

| Slice | What it delivers |
|---|---|
| S1 | A written glibc floor (`tools/linux-appimage/glibc-floor`, glibc 2.39 = Ubuntu 24.04 LTS) and `tools/linux-appimage/check-glibc-floor.sh`, run in the Linux packaging job after the libssl rejection and before the upload. It fails the release when any ELF in the AppImage needs a newer glibc, naming the file and version, and never passes by not looking: a missing folder, a missing `readelf`, no ELF, a cut-off or unreadable ELF, or a malformed floor end it as indeterminate. The install page states the minimum Linux. |
| S2 | The packaging job runs on `ubuntu-latest` inside a digest-pinned `ubuntu:24.04` container, so our pin, not the runner, sets the floor. WebKitGTK is no longer pinned to 2.44.0-2 (current 24.04 packages with security fixes; the installed version is printed). `setup-dotnet` is gone, `file` is installed. A new job, `verify-appimage-loads`, resolves every bundled binary with `ldd` on Ubuntu 24.04 and Debian 13 (both by digest). Renovate may bump the container digest, never the Ubuntu release. |

Maintainer decisions: runner `ubuntu-latest`, build environment Ubuntu 24.04 (floor glibc 2.39); WebKitGTK pins
dropped; no pull-request workflow for the oracles; "starts on Ubuntu 24.04" narrowed to library resolution on Ubuntu
24.04 and Debian 13 plus one manual desktop start before a release; the "alarm before retirement" branch of the first
promise is not applicable because its first branch is met.

## Verification

- Oracles (bash + Docker): `tools/linux-appimage/tests/test-glibc-floor.sh` 64/0/0;
  `APPIMAGE=… tools/linux-appimage/tests/test-build-environment.sh` 32/0/0 (loader check on both distributions against
  a real release AppImage).
- The floor check on the AppImage of release run 43714384534: scanned=179 highest=GLIBC_2.39 over_floor=0.
- Build And Deploy Lighthouse run 43740384750 (`77f6d84bc`): the first real Tauri build inside the container, the floor
  step and `verify-appimage-loads` on Ubuntu 24.04 and Debian 13 all green.
- The maintainer started the new AppImage (current WebKitGTK) on a Linux desktop: it works.

## Commits

S1 `024ceb2ab` `d72571105` `fb46db95d` `b7efecfa2`; S2 `f6c998d4a` `07566007b` `3379f0ed7` `3fb2b0313`; final refactor of
the oracles `137a8b71d` `2e8286259` `14276e7f1` `3f63fff59`; ADR-284 wording `4a34197cc`; the examiner's public command
`linux-appimage` `91fea1f52` (plus design, nwdes approval and review records).

## Gates

- Design review: one revision (ICU and setup-dotnet, silent-pass guard, loader job, a floor case), then approved.
- Slice reviews: S1 rejected once (two silent passes, shown by probes), S2 rejected once (the "starts" claim, missing
  `file`); both accepted after one revision.
- Oracle reviews: every oracle change re-approved; no open oracle findings.
- Whole-candidate review: accepted at `39923de69` with no open findings (floor oracle 64/0/0, loader oracle 32/0/0,
  actionlint clean). An earlier attempt was refused because the S2 oracle's approval predated its S1 support's last
  change; S2 was re-approved first. Its one non-blocking finding (ADR-284 claimed an observation not yet made) was
  fixed in `4a34197cc`.
- EXAMINE (source-blind, Haiku): E1, E3 and E4 met; E6's library half met (every bundled binary resolves on Ubuntu
  24.04 and Debian 13, 179 files, 0 unresolved); E6's desktop-start half unmet as an observation gap, because no public
  read or probe can show a maintainer starting the app. The maintainer did start it on 2026-11-15 ("works fine"), so
  the expectation is met in fact. nwdes refused every non-accepted examine close (RoleNotAccepted), so no examine
  record was published; the verdicts are kept here instead. The first examine run could judge nothing, because the
  packet exposed no file and no command for this feature; `linux-appimage` was added for it.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done: `docs/Installation/standalone.md` states the minimum Linux (Ubuntu 24.04 LTS or glibc 2.39, plus WebKitGTK 4.1 and GTK 3). `docs/Installation/installation.md` only links to that page, so it is unchanged. |
| Screenshots | N/A, because nothing in the app's UI changed. |
| Demo data | N/A, because no product data is involved. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A, because this is release tooling outside the application; no bounded context, glossary term or domain event changed. ADR-284 records the decision. |
| Usage-data event | N/A, because no Lighthouse user runs release tooling. |
| Mutation | N/A, because no C# or TypeScript production code changed (bash, YAML and JSON only); the oracles carry deliberate wrong-variant checks instead. |
| Release notes | N/A; written at /release. |

## Knowingly left open

- Renovate's handling of a `container:` image (and whether 24.04 → 26.04 counts as major or minor) is unverified; the
  first Renovate pull request will show it.
- The macOS and Windows packaging jobs still use `macos-latest` / `windows-latest`; their minimum-OS exposure is noted in
  ADR-284 and out of scope here.
