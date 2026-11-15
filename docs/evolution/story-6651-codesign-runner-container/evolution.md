# Story 6651 — Windows code signing runs in a pinned container runner

Delivered 2026-11-15. ADO #6651. Decision record: ADR-283
(`docs/product/architecture/adr-283-windows-code-signing-runs-in-a-pinned-container-image-with-the-pkcs11-provider.md`).
Workspace: `docs/feature/story-6651-codesign-runner-container/`.

## What was delivered

The self-hosted `codesign` runner that signs the Windows server exe, the NSIS installer and the MSI no longer runs on
the signing machine's own toolchain. A host OpenSSL upgrade broke signing once; the runner now runs from a container
image whose whole toolchain is pinned, and the YubiKey only needs to be plugged in on signing day.

| Slice | What it delivers |
|---|---|
| S1 | The runner image (`ubuntu:26.04` by digest, actions/runner by version and sha256), built and checked by its own pipeline `Codesign Runner Image` (required check `verify`, no change to `ci.yml`), published to GHCR as `sha-<commit>` and `latest`, started on boot by a systemd unit through `compose.yaml`, registered once by a `register` profile. Renovate bumps the runner version. |
| S2 | The sign step in the container: `osslsigncode` with the OpenSSL 3 pkcs11 provider, the PIN on stdin only, an own `pcscd` started and always stopped, no retry of a failed file (protects the PIN retries), and a clear message when the key is not plugged in. `ci_codesign-windows.yml` calls `tools/codesign/sign-windows.sh`. |
| S3 | `tools/codesign/check-host.sh`, which reports what is left of the old host setup and changes nothing; the old `start-runner-linux.ps1` retired; the README rewritten for a fresh machine, the switch-over, signing day and rollback. |

Final revision pass (after the whole-candidate review and the switch-over on the real machine): the release version
reaches the sign step through `env`, not inside `run:`; `YUBI_PIN` leaves the environment before any child process;
the publish job is serialised; and the host check reads systemd 262's "no match" (empty output, exit 1) as no unit.
That last defect was found on the real host and first pinned by a new oracle case (RED, then GREEN).

Oracles (bash + Docker, in the `verify` job): runner image 25/0/0, sign step 21/0/0, host check 18/0/0.

## Verification outside the oracles

- Maintainer, locally: the published image signed three copies of the last release's files with the real YubiKey;
  `osslsigncode verify` ok on all three. This also showed the pkcs11 provider's no-deinit setting works with the real
  token (no crash at exit).
- The maintainer switched the machine over (compose in `/storage/docker-tools/lighthouse-codesign`, the unit's
  `WorkingDirectory` changed to match); `check-host.sh` reports `host is clean`.
- Build And Deploy Lighthouse run 43714384534 (`a1b75c6a8`): `Code Sign Windows App` and `Verify Windows App` green.
  The downloaded server exe, setup exe and MSI each verify with signer CN=LetPeopleWork GmbH and a DigiCert timestamp.

## Commits

`31ab3c4d8` design + devops, `992775170` ADR-283 base image, S1 `c04226de7` `3ef0f8bff` `73932088b` `6c20a53f7`
`081c816b6`, S2 `a321abf78` `dc3622afc` `6d057ab8b`, S3 `eb855156d` `a33b0dde5` `e90669c1f` `74f952618`, final
revision `342c9ab38` `719a6c281` `bf0aed0ee` `a1b75c6a8` (plus the nwdes approval and review records).

## Gates

- Whole-candidate review (Sonnet): accepted, 4 low findings, all fixed in the final revision pass.
- EXAMINE (Sonnet, source-blind), on a build of the image with stand-ins: E3 (no key → plug-in message, no PIN
  spent), E4 (pinned image, own pipeline, Renovate) and E6 (README) met. E1, E2 and E5 were unmet only as observation
  gaps the examiner cannot close from a sandbox, and the maintainer's live checks above close them: the runner came
  online in GitHub after registration (E1), the real YubiKey signed in run 43714384534 with Verify Windows App green
  (E2), and `check-host.sh` says `host is clean` on the switched-over machine (E5). The nwdes examine record was not
  published (`RoleNotAccepted`), so these verdicts live here. A reboot of the signing machine has not been observed
  yet.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because no page under `docs/` describes how signing is done; `docs/compliance/distribution-and-versioning.md` and `cra-technical-file.md` state only that Windows binaries are signed, which stays true. The operator documentation is `tools/codesign/README.md`, rewritten in S3. |
| Screenshots | N/A, because nothing in the app changed. |
| Demo data | N/A, because no product data is involved. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A, because the change is release tooling outside the application; no bounded context, glossary term or domain event is added or changed. ADR-283 records the decision. |
| Usage-data event | N/A, because the feature is internal release tooling that no user of Lighthouse runs. |
| Mutation | N/A, because no C# or TypeScript production code changed (bash, YAML and a Dockerfile only); the bash oracles carry deliberate wrong-behaviour checks instead. |
| Release notes | N/A; written at /release. |

## Knowingly left open

- The registration token reaches `config.sh` on its argv inside the container for the few seconds of registration
  (the runner's own CLI takes it no other way).
- watchtower is not part of the setup; between reboots a new image arrives only when the maintainer runs it.
