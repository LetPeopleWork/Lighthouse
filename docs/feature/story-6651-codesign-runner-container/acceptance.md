# Acceptance brief: story\-6651\-codesign\-runner\-container

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
After I reboot the machine with the YubiKey unplugged, the signing runner appears online in GitHub's runner list without my doing anything.
```

#### E2

##### Promises

P2

```text
When I run a release with the YubiKey plugged in, the NSIS installer, the MSI and the server exe all come out signed, and the 'Verify signing coverage' step passes.
```

#### E3

##### Promises

P3

```text
When I run a release with no YubiKey plugged in, the sign step fails straight away with the message 'plug in the YubiKey and re-run this job', and no PIN has been sent by then.
```

#### E4

##### Promises

P4

```text
The runner image is built by a GitHub Actions workflow from a digest-pinned base and published to GHCR; it is rebuilt when files under tools/codesign change and on a schedule, and Renovate proposes bumps for the base digest and the runner version.
```

#### E5

##### Promises

P5

```text
After the move, the host has only Docker for signing: start-runner-linux.ps1, the host actions-runner, ~/.local/bin/osslsigncode and the local osslsigncode image are gone, and host scdaemon and pcscd no longer hold the YubiKey.
```

#### E6

##### Promises

P6

```text
I can open tools/codesign/README.md and follow it to set up the signing runner on a fresh Linux machine, and it explains why the host conflicts with scdaemon and pcscd are handled the way they are.
```

## Charter

None.

## Examination

None.
