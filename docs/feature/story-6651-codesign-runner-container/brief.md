# Product brief: story\-6651\-codesign\-runner\-container

## Request

```text
Story 6651: Windows code signing runs in a pinned container, using the pkcs11 provider. The signing runner is a container image built in GitHub Actions and published to GHCR, started locally on boot with nothing done by hand; the YubiKey is plugged in only when needed; osslsigncode uses the pkcs11 provider, not the engine; the host-installed runner, start-runner-linux.ps1 and the temporary osslsigncode wrapper retire.
```

## Work

L

```text
Windows releases are signed by a runner whose software we pin and rebuild ourselves, so a package update on the maintainer's workstation can no longer stop a release.
```

### Rework

None.

## Promises

### P1

Outcome

```text
After a reboot, with the YubiKey unplugged, the signing runner shows online in GitHub without any manual step.
```

### P2

Outcome

```text
A release run with the YubiKey plugged in signs the NSIS installer, the MSI and the server exe through the pkcs11 provider, and 'Verify signing coverage' passes.
```

### P3

Outcome

```text
With no YubiKey plugged in, the sign step fails at once with 'plug in the YubiKey and re-run this job', before any PIN is sent.
```

### P4

Outcome

```text
The runner image is built in GitHub Actions from a digest-pinned base, published to GHCR, and rebuilt on changes under tools/codesign/** and on a schedule; Renovate bumps the base digest and the runner version.
```

### P5

Outcome

```text
Nothing signing-related is installed on the host besides Docker: start-runner-linux.ps1, the host actions-runner, ~/.local/bin/osslsigncode and the local osslsigncode image are retired, and host scdaemon/pcscd no longer hold the YubiKey.
```

### P6

Outcome

```text
tools/codesign/README.md describes setting up the signing runner on a fresh Linux machine, and why the host conflicts (scdaemon, pcscd) are handled as they are.
```

## Scope

None.

```text
A cloud signing service (SSL.com eSigner, Azure Trusted Signing) that would remove the YubiKey and the self-hosted runner; a separate decision.
```

```text
The Windows signtool path in ci_codesign-windows.yml (Windows self-hosted runner) stays as it is.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The runner image is built and published to GHCR by a workflow, and the locally started container registers and shows online in GitHub after a reboot with no key plugged in.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

None.

### S2

```text
Inside the container, the sign step starts pcscd, finds the key, signs NSIS, MSI and server exe with the pkcs11 provider and stops pcscd; with no key it fails fast with the plug-in message before any PIN is sent.
```

#### Promises

P2

P3

#### Depends on

S1

#### Slice work

None.

### S3

```text
The old host setup is retired and the README describes a fresh-machine setup.
```

#### Promises

P5

P6

#### Depends on

S2

#### Slice work

None.
