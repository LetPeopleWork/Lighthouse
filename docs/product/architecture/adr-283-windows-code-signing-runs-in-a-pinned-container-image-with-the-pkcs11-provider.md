# ADR-283: Windows code signing runs in a pinned container image with the pkcs11 provider

**Feature**: `docs/feature/story-6651-codesign-runner-container/`

Status: Accepted

## Context

```text
Release v26.11.15.4 failed at Code Sign Windows App: the host-installed runner on the maintainer Arch workstation lost its toolchain to package updates (OpenSSL 4.1, libp11 dropping the OpenSSL engine, osslsigncode needing newer symbols) and gpg scdaemon held the YubiKey. Debian trixie ships osslsigncode 2.9, which has no -provider option (added upstream in 2.10), and Debian sid is rolling; Ubuntu 26.04 packages osslsigncode 2.13 with -provider, pkcs11-provider 1.2.0 and OpenSSL 3.5.
```

## Decision

```text
The Linux signing runner is the image ghcr.io/letpeoplework/lighthouse-codesign-runner, built from a digest-pinned ubuntu:26.04 with actions/runner pinned by version and sha256 and osslsigncode from the Ubuntu package (2.13, which has -provider). One independent workflow, codesign-runner-image.yml, runs on every pull_request without a paths filter: its required check Codesign Runner Image / verify builds the image and runs its oracles when the PR touches tools/codesign or the workflow; on main, weekly and on dispatch it verifies and then publishes. ci.yml and ci_changes.yml are not changed. Renovate keeps base digest and versions current. The image has the latchset pkcs11 provider and no pkcs11 engine, so signing goes through the provider or fails. It runs on the signing machine from a compose file started by a systemd unit; watchtower keeps it current. The runner binaries stay in the image; only the three registration files live in a named volume. The PAT is used only by a one-shot register service, never by the container jobs run in. pcscd runs only inside the sign step, which checks for the key before any PIN is read and stops pcscd on every exit. The host keeps only Docker; scdaemon uses disable-ccid and host pcscd is disabled.
```

## Alternatives

```text
Keep the host runner and pin host packages: rejected, a rolling distribution keeps moving underneath and it is what failed. Use Debian osslsigncode 2.9 with the engine (the temporary wrapper): rejected, the maintainer chose the provider and the engine is being dropped upstream. Debian trixie-slim with osslsigncode built from source: rejected, Ubuntu 26.04 packages a provider-capable osslsigncode, so no builder stage or second pinned checksum is needed. libp11 pkcs11prov provider: rejected, the latchset pkcs11-provider is packaged and osslsigncode selects it with -provider. Long-running pcscd: rejected, it would hold the key whenever plugged in. Mounting the volume over the runner directory: rejected, it would hide every newer runner version. Mounting the PAT into the always-on runner: rejected, every job could read it. Cloud signing service: out of scope.
```

## Consequences

```text
A package update on the workstation can no longer stop a release; image changes are built and tested before merge and before publish. ADR-212 says path-filtered workflows are not required checks and a dependency update can merge green and break them; for this image that gap is closed by the required check Codesign Runner Image / verify, so a runner bump without its sha256 stays red and waits for a human (the safe failure). The YubiKey is unusable for GPG/PIV on the host while the setup is active. Ubuntu packages float within the pinned base, refreshed weekly; opensc-pkcs11 is a release candidate (0.27.0~rc1) in 26.04 and is watched at EXAMINE with the real YubiKey. A new machine needs one register run with the PAT; reboots need nothing. The sign step runs osslsigncode with an OpenSSL config that sets pkcs11-module-quirks = no-deinit, because osslsigncode 2.13 with pkcs11-provider 1.2.0 crashes when the provider shuts the PKCS#11 module down at exit; whether the real YubiKey needs it too is checked at EXAMINE.
```
