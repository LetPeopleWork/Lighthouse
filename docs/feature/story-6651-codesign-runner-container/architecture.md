# Architecture brief: story\-6651\-codesign\-runner\-container

## Paradigm

procedural

## Shared records

### Decisions

#### D1

```text
Paradigm: procedural. The change is a Dockerfile, POSIX/bash scripts, a compose file, a systemd unit and GitHub workflow YAML: imperative transaction scripts with no domain model. No OO or FP smell catalogue applies; the general classes applied are: one owner per fact (one script owns the sign lifecycle, one entrypoint owns registration), no duplicated literals across files (image name, reader id and module paths live once in the Dockerfile ENV or the compose file), fail loud with WHAT/WHY/HOW messages, and no secret on argv or in logs.
```

#### D2

```text
Context map (ADR-270): the feature touches no bounded context. It changes only the release pipeline and the signing machine; it adds no domain term, concept or domain event, so ADR-270 and ARCHITECTURE.md section 4 stay unchanged. UI surface (ADR-271) and the loading/failure looks: not applicable, nothing is shown to a Lighthouse user. Usage-data event: not applicable for the same reason (DEVOPS owns the formal answer).
```

#### D3

```text
Runner image (owner tools/codesign/Dockerfile): a single-stage build FROM ubuntu:26.04 by tag and sha256 digest. It installs from Ubuntu 26.04: osslsigncode (2.13), pkcs11-provider (latchset, 1.2.0), opensc and opensc-pkcs11 (0.27.0~rc1), pcscd (2.4.1), libccid (1.7.1), sudo, nodejs and npm (22; the existing Tauri re-sign steps run npx @tauri-apps/cli@2 on this runner), git, curl and ca-certificates; OpenSSL is 3.5. actions/runner at ARG RUNNER_VERSION (tarball sha256 checked) is unpacked to /opt/runner as the pristine copy each start copies from. User runner keeps uid ARG RUNNER_UID (default 1000, matching the host user that owns the token file): the Ubuntu image already has user ubuntu with uid 1000, so the Dockerfile renames it (usermod -l runner -d /home/runner -m ubuntu and groupmod -n runner ubuntu) instead of adding a second uid-1000 user. The Dockerfile creates /home/runner/state and /home/runner/runtime owned by runner, so a fresh named volume mounted at /home/runner/state takes that ownership and the register copy-back can write. LABEL org.opencontainers.image.source=https://github.com/LetPeopleWork/Lighthouse links the GHCR package to the repository. libengine-pkcs11-openssl (the OpenSSL engine, a separate package) is deliberately NOT installed.
```

#### D4

```text
osslsigncode comes from the Ubuntu 26.04 package (2.13-1, whose sign help lists -provider): no builder stage and no osslsigncode version or sha256 ARG. Reason: Debian trixie ships osslsigncode 2.9-2, which has no -provider option (upstream added it in 2.10), and Debian sid is rolling; Ubuntu 26.04 carries a provider-capable osslsigncode together with pkcs11-provider 1.2.0, so the maintainer target (pkcs11 provider, not engine) needs no source build.
```

#### D5

```text
Provider, not engine, by construction. osslsigncode 2.10+ with -pkcs11module first tries the provider named by -provider and silently falls back to the libp11 engine when the provider does not load (source: osslsigncode.c sign key loading). Because the image carries no pkcs11 engine, that fallback cannot sign: a provider load failure fails the step loudly instead of signing through the engine. The sign command is: osslsigncode sign -provider pkcs11 -pkcs11module /usr/lib/x86_64-linux-gnu/opensc-pkcs11.so -key pkcs11:id=%01;type=private -pkcs11cert pkcs11:id=%01;type=cert -ac tools/codesign/SSL_COM_CODE_SIGNING_INTERMEDIATE_CA_ECC_R2.crt -readpass - -n "Lighthouse VERSION" -i https://letpeople.work/lighthouse -t http://timestamp.digicert.com -in FILE -out FILE.signed. The image sets ENV PKCS11_PROVIDER_MODULE to the opensc module because the latchset provider reads that variable, not the PKCS11_MODULE_PATH that osslsigncode exports. The provider is also configured with pkcs11-module-quirks = no-deinit (tools/codesign/openssl-pkcs11.cnf, installed as /etc/ssl/openssl-pkcs11.cnf): with osslsigncode 2.13 and pkcs11-provider 1.2.0, osslsigncode crashes (exit 139) when the provider shuts the PKCS#11 module down at process exit, even after a good signature; skipping that shutdown costs nothing because signing is a short-lived process and the sign step stops pcscd right after. The file includes the system OpenSSL config and declares the pkcs11 provider section with the quirk.
```

#### D6

```text
Registration carrier (owner tools/codesign/entrypoint.sh, the image ENTRYPOINT). actions/runner keeps its registration in three files (.runner, .credentials, .credentials_rsaparams) in the directory run.sh resolves itself to: run.sh and config.sh follow symlinks (cd -P) and write run-helper.sh and the state files next to the real script, so a symlinked run directory would resolve to /opt/runner. Therefore every start the entrypoint COPIES the runner into the writable run directory: rm -rf /home/runner/runtime/* then cp -a /opt/runner/. /home/runner/runtime/ (a plain copy; no hard links, because /opt/runner is in the image layer and the run directory in the container layer, and hard links would let the runner modify image files through copy-up semantics in ways that are not worth reasoning about). Then it copies the three files from the named volume codesign-runner-state (mounted at /home/runner/state, nothing else) into /home/runner/runtime. The volume never holds binaries, so a new image always runs its own RUNNER_VERSION. Modes: run (default, always-on service): state files present -> exec /home/runner/runtime/run.sh; absent -> exit 1 with WHAT/WHY/HOW naming docker compose --profile register run --rm register. register (one-shot service): after the same copy, reads the PAT from /run/secrets/github_token and passes it to the GitHub API only as an Authorization header that curl reads from a file (curl --config FILE or -H @FILE), never on argv; runs /home/runner/runtime/config.sh --unattended --name codesign-runner-linux --labels linux,codesign --replace --disableupdate; copies the three files back to /home/runner/state; exits. The token is never echoed. --disableupdate because self-update would be lost on every recreate.
```

#### D7

```text
Start on boot and update (owners tools/codesign/compose.yaml and tools/codesign/lighthouse-codesign-runner.service). Service runner (always on): image ghcr.io/letpeoplework/lighthouse-codesign-runner:latest, command run, restart: unless-stopped, init: true (reaps a daemonised pcscd), the state volume, a bind of /dev/bus/usb, device_cgroup_rules c 189:* rmw, no privileged mode, and NO secrets entry, so no job can read the PAT. Service register (profile register, one-shot): same image, command register, the state volume and the github_token secret whose file source is outside the repo (default ~/.config/lighthouse-codesign/github-token). The systemd unit (After=docker.service network-online.target, Type=oneshot, RemainAfterExit=yes) runs ExecStartPre=-docker compose pull and ExecStart=docker compose up -d runner. A failed pull at boot does not stop the start; the maintainer sees it only as a warning in journalctl -u lighthouse-codesign-runner, and the runner keeps the cached image. Between boots the maintainer runs watchtower regularly, which pulls the latest image and recreates the container; the README names watchtower and has no manual restart step. GHCR package visibility: public, so the machine pulls without login (README; otherwise a one-time docker login).
```

#### D8

```text
One independent workflow (owner .github/workflows/codesign-runner-image.yml, name Codesign Runner Image); ci.yml and ci_changes.yml are not changed. Triggers: pull_request to main with no paths filter (so it always reports), push to main for paths tools/codesign/** and the workflow file, a weekly schedule, workflow_dispatch. Workflow permissions: contents read. Job changes (ubuntu-latest, checkout with fetch-depth 0): on pull_request it diffs github.event.pull_request.base.sha...github.event.pull_request.head.sha for tools/codesign/ and .github/workflows/codesign-runner-image.yml and writes output codesign as the string true or false (captured file list matched with a here-string, no pipe into grep -q); on push, schedule and dispatch it writes the string true. Job verify (the required check, context Codesign Runner Image / verify): needs changes; if: !cancelled() and (needs.changes.result is not success, or needs.changes.outputs.codesign equals the quoted string true); its first step exits 1 with a clear message (change detection failed, re-run the workflow) when needs.changes.result is not success, so a failed changes job turns the check red, never skipped; then it builds the image once with buildx for linux/amd64 without push or login and runs the three slice oracles. Job publish: needs verify; if github.ref is refs/heads/main and github.event_name is not pull_request and needs.verify.result is success (a dispatch on another branch must never overwrite latest, which watchtower pulls); permissions packages write; rebuilds from the same commit with the buildx cache, logs in to ghcr.io with GITHUB_TOKEN, pushes tags latest and sha-COMMIT, then runs the shared contract test against latest. Every action pinned to a full commit with its release in a comment (ADR-212).
```

#### D9

```text
Renovate (owner renovate.json): the dockerfile manager finds the FROM digest of ubuntu:26.04 in tools/codesign/Dockerfile. One new custom regex manager reads a line of the form # renovate: datasource=github-releases depName=actions/runner followed by ARG RUNNER_VERSION=VERSION in tools/codesign/Dockerfile; Renovate tracks only the base digest and the runner version. ADR-212 automerge stays on. These PRs are gated by the required check Codesign Runner Image / verify (D14): a runner bump whose sha256 ARG was not updated fails the image build, stays red and does not merge until a human edits the checksum. That is the intended safe failure.
```

#### D10

```text
Sign step (owner tools/codesign/sign-windows.sh; caller .github/workflows/ci_codesign-windows.yml Sign on Linux step, which shrinks to bash tools/codesign/sign-windows.sh VERSION with YUBI_PIN in env). Lifecycle carrier: shell state in the one script process. The script exports PKCS11_PROVIDER_MODULE=/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so itself, so it does not depend on the image ENV. Order: (1) look for a USB device with idVendor 1050 under the sysfs root (default /sys/bus/usb/devices, overridable by CODESIGN_USB_SYSFS for the oracle); none: print plug in the YubiKey and re-run this job, exit 1, no pcscd, no PIN read. (2) set trap stop_pcscd EXIT INT TERM; stop any stale pcscd (sudo -n /usr/bin/pkill -x pcscd, failure ignored) left by a killed earlier run; start pcscd (sudo -n /usr/sbin/pcscd --disable-polkit) with the static reader entry. (3) wait at most CODESIGN_READER_WAIT seconds (default 10) for opensc-tool --list-readers to list a reader; timeout: the plug-in message plus a hint to run tools/codesign/check-host.sh, exit 1, PIN never read. (4) sign the server exe, the NSIS exe and the MSI, PIN on stdin from YUBI_PIN, never argv, never echoed, no set -x, no retry of a failed file. (5) the trap stops pcscd on every exit. Tools resolved through PATH so the oracle can put recording stand-ins first. Sudoers (tools/codesign/sudoers-pcscd, installed 0440 root:root): exactly runner ALL=(root) NOPASSWD: /usr/sbin/pcscd --disable-polkit, /usr/bin/pkill -x pcscd; no wildcard, no ALL command. The script exports OPENSSL_CONF=/etc/ssl/openssl-pkcs11.cnf for its own osslsigncode calls, not the image ENV: every other job on this runner (git, curl, node) then keeps the stock OpenSSL config and never loads the pkcs11 provider. Accepted: YUBI_PIN is in the environment of the script children (osslsigncode, opensc-tool) as for any GitHub secret in a step env.
```

#### D11

```text
Static reader entry (owner tools/codesign/reader.conf, copied to /etc/reader.conf.d/yubikey in the image): FRIENDLYNAME YubiKey, DEVICENAME usb:1050/0407, LIBPATH the libccid driver. Reason: libudev inside a container reports the wrong USB interface, so pcscd hotplug cannot find the reader (observed on the temporary workaround). 0407 is the product id of the maintainer key (YubiKey 5 OTP+FIDO+CCID).
```

#### D12

```text
Host conflicts (owners tools/codesign/README.md and tools/codesign/check-host.sh): scdaemon gets disable-ccid in ~/.gnupg/scdaemon.conf so gpg-agent never claims the CCID interface (LIBUSB_ERROR_BUSY); host pcscd.socket and pcscd.service are disabled and stopped (socket activation would otherwise restart pcscd as soon as anything asks). Consequence the README states: the YubiKey is not usable for GPG or PIV on the host while this setup is active. check-host.sh reports every leftover of the retired setup and every host process that could hold the key, with exit 1 and a HOW per finding.
```

#### D13

```text
Formal verification: the only state/temporal claim is the sign-step lifecycle (S2). Checker: Quint 0.33.1 with the Apalache backend for bounded model checking of safety, plus quint run simulation for mutant kills and reachability witnesses. Liveness under fairness is not claimed: the script is one sequential process whose only loop is bounded by a deadline, so termination is stated as the safety invariant protocolStepsBounded (bounded response). TLC is therefore not needed. All other claims (image contents, compose config, registration, Renovate, host state) are finite configuration facts checked by example oracles, not by a model.
```

#### D14

```text
The required check for PRs is Codesign Runner Image / verify in the independent workflow (D8), not a ci.yml job. The verify job runs if !cancelled() and (needs.changes.result is not success, or needs.changes.outputs.codesign equals the quoted string true), and fails at its first step when changes did not succeed: a changes failure turns the check red, never skipped. a boolean comparison would always be false and skip the job forever, so the S1 oracle checks this string comparison and that the changes job writes the strings true/false. On a pull_request that does not touch tools/codesign/ or the workflow, verify is skipped, which a required status check accepts as passing; otherwise it builds and runs the oracles. The workflow has no paths filter on pull_request, so the check always reports. The root adds Codesign Runner Image / verify to the main ruleset after the first push (outside the repo).
```

#### D15

```text
Rollback (owners tools/codesign/compose.yaml and tools/codesign/README.md): the runner service image is ghcr.io/letpeoplework/lighthouse-codesign-runner:${CODESIGN_RUNNER_TAG:-latest}, so it renders as latest unless the maintainer pins a tag. When a new latest breaks signing: (1) find the previous good tag sha-COMMIT (the GHCR package page, or the publish run of the last green release); (2) write CODESIGN_RUNNER_TAG=sha-COMMIT into the .env file beside compose.yaml; (3) docker compose pull runner and docker compose up -d runner; (4) watchtower then compares only that immutable tag, so it stops moving the runner by itself (no watchtower change needed); (5) re-run the failed sign job. To return to latest: remove the line and repeat step 3. The registration volume is untouched by a rollback, so no re-register. The README (S3) carries these steps.
```

#### D16

```text
Health signal (owner tools/codesign/README.md): the signal is the runner list in GitHub (Settings, Actions, Runners: codesign-runner-linux online/offline) and docker compose ps / docker logs on the machine. If the runner is offline, a release run does not fail: the codesign-windows job for [self-hosted, codesign] queues with Waiting for a runner to pick up this job for up to 24 h; timeout-minutes 15 counts only once the job runs, so it does not bound the queue. No earlier alert is added, on purpose: a signing run needs the maintainer at the machine to plug in the YubiKey and he approves the release gate, so he sees the waiting job at once; the weekly image workflow proves the image and the contract, not that the machine is online, and a monitor for a machine that only matters while he is present would cost more than it saves. The README lists what to check when the job waits.
```

#### D17

```text
Usage data (Lighthouse DEVOPS rule): N/A. The change is pipeline-only: it alters how Windows release binaries are signed on the maintainer machine; no Lighthouse user runs, sees or configures it, so no UsageDataEventName is added and docs/settings/usagedata.md is unchanged.
```

#### D18

```text
The durable architecture decision of this feature is recorded in docs/product/architecture/adr-283-windows-code-signing-runs-in-a-pinned-container-image-with-the-pkcs11-provider.md (Accepted). That repository document is the authority; this design references it and declares no ADR of its own.
```

### Constraints

#### K1

```text
Scope: the Windows signtool branch of ci_codesign-windows.yml stays as it is, including runs-on [self-hosted, codesign]; the container registers with labels linux,codesign so the existing job selector still picks either runner. A cloud signing service is out of scope.
```

#### K2

```text
Bounds from authority: P3 says the no-key failure is at once and before any PIN is sent. Design bound (measured from the start of the sign step, which runs inside an already started container): with no USB key the step exits within 5 s (no pcscd start); with a USB key but no reader within CODESIGN_READER_WAIT (10 s) plus 5 s. The authority gives no number; these are design choices. Not covered by these bounds: the container start, where the entrypoint copies the runner (about 670 MB) into the run directory before run.sh; that cost is paid once per container start (boot, watchtower recreate), not per job, and the S1 entrypoint cases get their own bound of 60 s. The job timeout stays timeout-minutes: 15.
```

#### K3

```text
Deviation, labelled: pinned are the base image digest (ubuntu:26.04) and actions/runner (version and sha256). Ubuntu packages, osslsigncode included, float within the pinned base at build time on purpose, so the weekly scheduled rebuild picks up Ubuntu security fixes. A package update can therefore change the image between rebuilds, but only through a CI build that runs the oracles before publish, never on the maintainer workstation.
```

#### K4

```text
Secrets: YUBI_PIN only in the step env and on osslsigncode stdin; never in argv, never printed, never in the image (inherited by the script children: accepted). The GitHub PAT only in the host file outside the repo, mounted read-only only into the one-shot register service, passed to curl only from a file, never on argv, never in compose.yaml for the runner service, the image, env or logs. The registration credentials live only in the named state volume and the run directory of the always-on container.
```

#### K5

```text
Platform: the signing machine is linux/amd64; module paths are the Debian multiarch paths /usr/lib/x86_64-linux-gnu/... . The image is built for linux/amd64 only.
```

#### K6

```text
Test substrate: the repository runs bash test scripts (Scripts/test-selection/test-path-classifier.sh) and node --test for .github/scripts; bats and Pester are not used. Each slice oracle is one bash script under tools/codesign/tests/ that needs only Docker and bash, runs on ubuntu-latest and locally, builds the image once (or uses IMAGE=... when given), prints one PASS/FAIL line per check with WHAT/WHY/HOW on failure, and ends with a summary line observed=N assertions_failed=M setup_failed=K; exit 0 only when M and K are 0. After the image exists each oracle finishes well under a minute. No nWave scenario tags in test code or titles. Lighthouse test-writing skill read in full (no stack half applies: no frontend or backend code).
```

#### K7

```text
Shell rules from docs/ci-learnings.md apply to every new script: set -euo pipefail; never pipe a variable or long output into grep -q or head (here-string or capture first); never chain [[ ]] && [[ ]] ||; shellcheck clean at koalaman/shellcheck:v0.9.0. Dockerfile curl calls carry --proto "=https" on the same line (ledger rule docker:S6506). tools/ is already in sonar.exclusions.
```

#### K8

```text
Manual host steps (maintainer, not CI): once per fresh machine: install compose.yaml and the unit, write the token file (owned by the user with uid RUNNER_UID, mode 0600), run docker compose --profile register run --rm register, enable the unit, add disable-ccid, disable host pcscd. Retirement: stop and remove the host actions-runner service and its registration, delete ~/.local/bin/osslsigncode and the .path entry, docker image rm local/osslsigncode-ossl3:trixie. Ongoing: watchtower keeps the image current; nothing manual after a reboot. The README lists them in order; check-host.sh confirms the result.
```

#### K9

```text
Only the maintainer can check these at EXAMINE, no oracle can: pkcs11-provider 1.2.0 with the PIV key in slot 9c (always-authenticate) across three signs in one run; a sign with the real YubiKey exits 0 (no crash at exit, the no-deinit quirk holding for opensc-pkcs11 as it does for SoftHSM); opensc-pkcs11 0.27.0~rc1 (a release candidate in Ubuntu 26.04) talking to the real YubiKey; the static reader entry finding the real key inside the container; /sys/bus/usb showing the plugged key inside the container; growth of /home/runner/work (no volume, so it is reset on every recreate).
```

#### K10

```text
ADR-212 is not edited: its consequence that path-filtered workflows are not required checks stays true in general. The ADR of this feature records that for the runner image this gap is closed by the required check Codesign Runner Image / verify, a pull_request workflow without a paths filter.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

```text
GitHub Actions job codesign-windows (ci_codesign-windows.yml) dispatched to a runner labelled self-hosted,codesign; locally, docker and systemd on the signing machine
```

#### Driven ports

```text
GitHub REST API (runner registration token) and the Actions service
```

```text
GHCR (image pull and push)
```

```text
YubiKey over USB through pcscd, libccid, opensc-pkcs11 and the pkcs11 provider
```

```text
DigiCert timestamp server
```

```text
The workflow in the repo depends on the image tool set (osslsigncode with -provider, the pkcs11 provider, pcscd, node); the image depends on nothing in a given repo revision except the files under tools/codesign baked at build. The host depends only on Docker, the compose file, the unit and the token file.
```

#### Failures

##### B9

```text
GHCR unavailable at boot or at a watchtower run
```

Indeterminate

```text
The cached image keeps running and the runner stays online; journalctl -u lighthouse-codesign-runner shows the failed pull as a warning; watchtower retries on its next run
```

##### B10

```text
Runner registration deleted, revoked or expired in GitHub
```

Refusal

```text
run.sh exits with an authentication or registration error and the container restarts repeatedly (unless-stopped); GitHub shows no codesign-runner-linux. Maintainer: docker compose stop runner, docker compose --profile register run --rm register (config.sh --replace overwrites the state files), docker compose up -d runner. README carries it.
```

##### B11

```text
YubiKey PIV PIN blocked after wrong PINs
```

Refusal

```text
osslsigncode fails on the first file with a PIN-locked or login error (CKR_PIN_LOCKED); the step never retries a file, so it never spends another PIN attempt. Unblocking with the PUK (ykman piv access unblock-pin) is a manual, out-of-software step; the README says how to recognise it and to never re-run the job before the PIN is fixed and YUBI_PIN checked.
```

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

None.

### Models

None.

### Closure

None.

### Risks

#### Y1

Stressor

```text
Negates: the pkcs11 provider loads and finds the key through opensc
```

```text
No file is signed through the engine; the failure names the provider and the step exits non-zero
```

```text
The release sign step fails and the release waits
```

```text
Silent fallback to the libp11 engine or an unsigned file passing Verify signing coverage
```

```text
The engine is absent from the image, so the fallback cannot sign; the S2 oracle loads the provider in the built image before publish
```

tools\/codesign\/Dockerfile

#### Y2

Stressor

```text
Negates: GHCR is reachable when the machine boots
```

```text
The runner starts and shows online with the cached image
```

```text
The image can be older than main until the next watchtower run or boot
```

```text
The runner does not start because a pull failed
```

```text
ExecStartPre has a leading minus, so the failure shows only as a warning in journalctl; watchtower pulls again on its next run; skew between script and image is accepted by the maintainer because watchtower runs regularly, and the contract test against latest runs after every publish
```

tools\/codesign\/lighthouse\-codesign\-runner\.service

#### Y3

Stressor

```text
Negates: the runner version stays inside the window GitHub accepts
```

```text
A refused runner is visible as offline in GitHub and in the container log
```

```text
Signing blocked until the image is rebuilt with a current runner
```

```text
A Renovate bump that cannot build merging and leaving the machine on an old image unnoticed
```

```text
Renovate bump PRs must pass the required check Codesign Runner Image / verify, which builds the image; a bump without its sha256 stays red and waits for a human; the weekly rebuild and watchtower carry a merged bump to the machine
```

maintainer

#### Y4

Bypass

```text
Any workflow job with runs-on codesign runs inside the always-on container with /dev/bus/usb and the cgroup rule (public repository, self-hosted runner)
```

```text
Signing needs the YUBI_PIN secret, which fork pull requests do not receive; the key is plugged in only while signing; the always-on container holds no PAT
```

```text
A job can see USB devices and the runner registration while it runs
```

```text
A PAT, a secret in the image, privileged mode or the host home directory reachable from a job
```

```text
The PAT is only in the one-shot register service; a job could read the runner registration credentials (they let it impersonate this runner, not mint new ones); the repository keeps approval required for workflows from outside collaborators (maintainer setting, unchanged)
```

maintainer

#### Y5

SinglePointOfFailure

```text
One signing machine and one YubiKey
```

```text
Unchanged from today
```

```text
Windows releases wait while the machine or key is away
```

```text
Signing with any other key
```

```text
Accepted; the cloud signing alternative is out of scope and a separate decision
```

maintainer

#### Y6

Stressor

```text
Second stressor set, walked over the finished design (walkthrough, not execution): a different YubiKey model (other USB product id), a Docker daemon upgrade restarting containers, a USB hub renumbering the device, two release runs queued, a host gpg update re-enabling CCID in scdaemon
```

```text
Each ends in either a signed release or a non-zero step with a message
```

```text
A different PID gives the reader-timeout message until reader.conf is updated; queued runs sign one after another on the single runner
```

```text
A hang beyond the job timeout or a PIN sent with no reader
```

```text
cgroup rule 189:* covers renumbering; unless-stopped covers daemon restarts; the reader-timeout message tells the maintainer to run check-host.sh, which reports scdaemon or host pcscd holding the key; reader.conf is one line to change for a new key model
```

tools\/codesign\/sign\-windows\.sh

### Architectural tests

#### T1

```text
Every uses: in .github/workflows and .github/actions, including the new codesign-runner-image.yml, is pinned to a full commit with its release named after it, so Renovate can keep it current (ADR-212 decision 6). Reused existing guard; it scans the real repository.
```

\.github\/scripts\/action\-pins\.test\.mjs

```text
node --test .github/scripts/action-pins.test.mjs
```

#### T2

```text
No workflow if: decides on the actor, and every Renovate hold carries a reason (ADR-212 consequences). Covers the codesign runner workflow and the new renovate.json custom manager: a Renovate PR runs the same Codesign Runner Image / verify check as a person.
```

\.github\/scripts\/dependency\-bot\.test\.mjs

```text
node --test .github/scripts/dependency-bot.test.mjs
```

### Contract tests

#### T3

```text
The sign script from the checkout and the image the machine runs are released apart (watchtower pulls the image, the script comes with every commit). The checkout sign-windows.sh must run in the published image. Runs only on main after publish and in the weekly run, never on pull_request, because a PR changing both cannot have its image published yet.
```

sign\-windows\.sh tool contract\: osslsigncode with \-provider\, pkcs11 provider loadable\, sudo \-n for the two pcscd commands\, opensc\-tool\, static reader entry\, no pkcs11 engine

tools\/codesign\/Dockerfile via ghcr\.io\/letpeoplework\/lighthouse\-codesign\-runner\:latest

tools\/codesign\/sign\-windows\.sh called by \.github\/workflows\/ci\_codesign\-windows\.yml

```text
The S2 oracle run with IMAGE set to the published latest tag passes every check.
```

tools\/codesign\/tests\/test\-sign\-step\.sh

```text
env IMAGE=ghcr.io/letpeoplework/lighthouse-codesign-runner:latest bash tools/codesign/tests/test-sign-step.sh
```

## Slices

### S1

#### Targets

tools\/codesign\/Dockerfile

Create

```text
Runner image: digest-pinned ubuntu:26.04 base, actions/runner at pinned version and sha256, user runner with uid 1000 (renamed from ubuntu), entrypoint
```

tools\/codesign\/entrypoint\.sh

Create

```text
Register once from the token file, then run; refuse loudly without registration and token
```

tools\/codesign\/compose\.yaml

Create

```text
Restart policy, named state volume, token secret, /dev/bus/usb mount, cgroup rule c 189:* rmw
```

tools\/codesign\/lighthouse\-codesign\-runner\.service

Create

```text
Pull on boot, then compose up
```

\.github\/workflows\/codesign\-runner\-image\.yml

Create

```text
Independent pipeline, ci.yml and ci_changes.yml untouched: changes (own diff), verify (required check Codesign Runner Image / verify: build without push, run the slice oracles), publish (main, schedule, dispatch: push latest and sha tags, then the contract test against latest)
```

renovate\.json

Extend

```text
Custom regex manager for the annotated ARG versions in tools/codesign/Dockerfile
```

tools\/codesign\/tests\/test\-runner\-image\.sh

#### Verification

```text
bash tools/codesign/tests/test-runner-image.sh
actionlint .github/workflows/codesign-runner-image.yml
node --test .github/scripts/action-pins.test.mjs .github/scripts/dependency-bot.test.mjs
```

#### Supports

None.

#### Public oracle

```text
Build the image from tools/codesign/Dockerfile (or take IMAGE); run it through its real entrypoint with docker run against a state volume, with recording stand-ins for config.sh and curl first on PATH where a case needs them; run the image real run.sh once with no stand-in; render tools/codesign/compose.yaml with docker compose config (default and --profile register, and once with the token source file missing); read the unit file, the image labels, every FROM line and the jobs of .github/workflows/codesign-runner-image.yml.
```

```text
(1) Every FROM names ubuntu:26.04 with an @sha256 digest; the image has label org.opencontainers.image.source=https://github.com/LetPeopleWork/Lighthouse; /opt/runner holds config.sh and run.sh at the RUNNER_VERSION in the Dockerfile; /home/runner/state and /home/runner/runtime exist owned by runner; the default user is runner with uid 1000 and no user ubuntu remains. (2) Run mode with an empty volume exits 1 within 60 s, names the register command and never starts run.sh or config.sh. (3) Run mode with a volume holding .runner, .credentials and .credentials_rsaparams starts run.sh and never config.sh. (4) A volume left by an older image plus the new image: run.sh executes from /home/runner/runtime (writable, a copy, not a symlink to /opt/runner), the three files sit beside it, and the runner there reports the image RUNNER_VERSION. (5) Real-binary case, no stand-in: with the three fixture files in the volume, the image real run.sh started by the entrypoint under a 60 s timeout prints neither Must configure nor Permission denied (a network or authentication failure is acceptable). (6) Register mode with a token file: curl receives the token only through a file or stdin and never on argv; config.sh runs from /home/runner/runtime with labels linux,codesign and --disableupdate; the three files land in a fresh volume; the token is in no output. Register mode without a token file exits 1 naming /run/secrets/github_token. (7) Rendered compose: service runner has restart unless-stopped, init true, a bind of /dev/bus/usb, device_cgroup_rules c 189:* rmw, no privileged true, no fixed devices entry, the state volume, image ghcr.io/letpeoplework/lighthouse-codesign-runner:latest and NO secrets; service register has profile register and the github_token secret with a source outside the repo; config with a missing token file still renders the runner service. (8) The unit has ExecStartPre=-docker compose pull, ExecStart=docker compose up -d runner, After=docker.service, WantedBy=multi-user.target. (9) codesign-runner-image.yml has a pull_request trigger with no paths filter; its changes job writes codesign as the string true or false; the verify job if contains !cancelled(), a needs.changes.result clause and a comparison of needs.changes.outputs.codesign with the quoted string true, and its first step exits 1 when needs.changes.result is not success; publish needs verify and its if requires github.ref refs/heads/main, an event other than pull_request and needs.verify.result success; ci.yml and ci_changes.yml are not referenced. So the check Codesign Runner Image / verify runs when codesign is true.
```

```text
A tag-only FROM, a missing source label, binaries in the volume, a run directory of symlinks into /opt/runner (run.sh then fails with Permission denied or Must configure), a root-owned state directory, a run mode that runs config.sh, a PAT on curl argv or in output, a secrets entry on the runner service, privileged: true, a fixed devices line, missing restart or init, a pull without the leading minus, or a boolean comparison in the job if each turns one named check red.
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

start\-runner\-linux\.ps1

tools\/codesign\/start\-runner\-linux\.ps1\:1

Replace

```text
Its registration (token request, config.sh labels linux,codesign, run.sh) moves into entrypoint.sh; its host pcscd/pkcs11 preflight moves into the sign step (S2); the file itself retires in S3
```

customManagers

renovate\.json\:87

Extend

```text
Existing regex custom managers already pin docker images in files Renovate does not parse; one more covers the annotated ARG versions
```

findActionPinViolations

\.github\/scripts\/action\-pins\.mjs\:34

Reuse

```text
Guards the new workflow pins without change
```

packages\: write

\.github\/workflows\/ci\_docker\.yml\:17

Reuse

```text
GHCR publishing pattern reused: login-action pin, GITHUB_TOKEN, packages: write, build then verify then push
```

Dockerfile

tools\/codesign\/Dockerfile\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

entrypoint\.sh

tools\/codesign\/entrypoint\.sh\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

compose\.yaml

tools\/codesign\/compose\.yaml\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

lighthouse\-codesign\-runner\.service

tools\/codesign\/lighthouse\-codesign\-runner\.service\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

codesign\-runner\-image\.yml

\.github\/workflows\/codesign\-runner\-image\.yml\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

##### Impact

container\-image

tools\/codesign\/Dockerfile\:1

Changed

```text
New file
```

runtime\-script

tools\/codesign\/entrypoint\.sh\:1

Changed

```text
New file
```

deployment\-config

tools\/codesign\/compose\.yaml\:1

Changed

```text
New file
```

deployment\-config

tools\/codesign\/lighthouse\-codesign\-runner\.service\:1

Changed

```text
New file
```

ci\-workflow

\.github\/workflows\/codesign\-runner\-image\.yml\:1

Changed

```text
New workflow; scanned by action-pins and dependency-bot guards
```

build\-metadata

renovate\.json\:87

Changed

```text
One custom manager added; existing rules unchanged
```

ignore\-rules

\.gitignore\:428

UnchangedCompatible

```text
tools/codesign/actions-runner ignore stays harmless after the host runner retires
```

test\-scan\-exclusion

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:140

UnchangedCompatible

```text
Excludes a directory that will no longer exist; no behaviour change
```

##### Agreement

runner labels self\-hosted\,codesign

tools\/codesign\/entrypoint\.sh\:1

Producer

Migrated

```text
Container registers with labels linux,codesign as the host runner did
```

runner labels self\-hosted\,codesign

\.github\/workflows\/ci\_codesign\-windows\.yml\:12

Consumer

UnchangedCompatible

```text
runs-on stays [self-hosted, codesign]; the Windows runner keeps its label
```

image ghcr\.io\/letpeoplework\/lighthouse\-codesign\-runner\:latest

\.github\/workflows\/codesign\-runner\-image\.yml\:1

Producer

Migrated

```text
New producer
```

image ghcr\.io\/letpeoplework\/lighthouse\-codesign\-runner\:latest

tools\/codesign\/compose\.yaml\:1

Consumer

Migrated

```text
New consumer, pulls latest on boot
```

##### Not applicable

prefactoring

```text
No existing code moves: the slice adds new files or replaces one workflow step and one script; there is no GREEN oracle over the old Linux sign step to preserve, and preserving the engine path is the opposite of the goal
```

architectural_tests

```text
Covered by the shared action-pins and dependency-bot guards; the provider-only law is a slice oracle check, not a dependency law
```

contract_tests

```text
The image tag contract between the image workflow and compose.yaml changes in one commit and one repo; the shared contract test covers the script/image skew
```

##### Boundaries

```text
S1: docker run of the image entrypoint and docker compose config on the signing machine; S2: the Sign on Linux step running tools/codesign/sign-windows.sh in the container; S3: tools/codesign/check-host.sh run by the maintainer
```

###### Driven ports

```text
GitHub API, GHCR, USB/pcscd/pkcs11 provider, timestamp server (see shared boundaries)
```

```text
Repo scripts depend on the image tool set; the host depends only on Docker, compose file, unit and token file
```

###### Failures

####### B1

```text
No registration in the state volume (run mode), or no token file (register mode)
```

Refusal

```text
Run mode exits 1 naming docker compose --profile register run --rm register; register mode exits 1 naming /run/secrets/github_token and the host file to create
```

####### B2

```text
GHCR pull fails at boot
```

Indeterminate

```text
Unit continues with the cached image; image may be older than main
```

####### B3

```text
Image oracle fails in the image workflow
```

Refusal

```text
No push to GHCR; the previous latest stays deployed; workflow red
```

##### Observations

```text
Rendered compose config and unit file fields
```

docker compose config YAML and unit text

```text
Whether the machine actually boots the unit and the runner shows online in GitHub (maintainer at EXAMINE, P1)
```

```text
Entrypoint exit status and output without registration and token
```

exit code and stdout\/stderr

```text
Successful registration against the real GitHub API (maintainer at EXAMINE)
```

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
runner image and start-on-boot config
```

Safety

Pass

DesignedNotBuilt

```text
Finite configuration facts, each with a named check in the S1 oracle
```

###### Evidence

None.

```text
runner shows online after reboot
```

PublicReachability

Pass

Indeterminate

```text
Path boot -> systemd unit -> compose pull/up -> entrypoint -> run.sh -> GitHub runner list; only the maintainer can observe it on the machine (P1)
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

tools\/codesign\/Dockerfile

Extend

```text
Ubuntu packages osslsigncode, pkcs11-provider, opensc, pcscd, libccid, node/npm, reader entry, sudoers line, PKCS11_PROVIDER_MODULE, the provider OpenSSL config copied to /etc/ssl/openssl-pkcs11.cnf; no pkcs11 engine
```

tools\/codesign\/sign\-windows\.sh

Create

```text
Owns the sign lifecycle: key check, pcscd start, bounded reader wait, provider signing with PIN on stdin, pcscd stop on every exit
```

tools\/codesign\/reader\.conf

Create

```text
Static reader entry usb:1050/0407
```

tools\/codesign\/sudoers\-pcscd

Create

```text
Lets user runner start and stop pcscd only
```

\.github\/workflows\/ci\_codesign\-windows\.yml

Extend

```text
Sign on Linux step calls the script with YUBI_PIN in env; Windows step unchanged
```

\.github\/workflows\/codesign\-runner\-image\.yml

Extend

```text
Runs the S2 oracle against the built image before publish
```

tools\/codesign\/openssl\-pkcs11\.cnf

Create

```text
OpenSSL config for the pkcs11 provider: includes the system config and sets pkcs11-module-quirks = no-deinit, so osslsigncode does not crash at exit
```

tools\/codesign\/tests\/test\-sign\-step\.sh

#### Verification

```text
bash tools/codesign/tests/test-sign-step.sh
actionlint .github/workflows/ci_codesign-windows.yml .github/workflows/codesign-runner-image.yml
```

#### Supports

None.

#### Public oracle

```text
Run tools/codesign/sign-windows.sh VERSION inside the built image (docker run, workdir holding artifacts/server/Lighthouse.exe, artifacts/standalone/Lighthouse-setup.exe and Lighthouse.msi), YUBI_PIN set to a sentinel, with recording stand-ins for sudo/pcscd/pkill, opensc-tool and osslsigncode first on PATH: (a) CODESIGN_USB_SYSFS empty; (b) a fake device with idVendor 1050 and no reader ever listed, CODESIGN_READER_WAIT=2; (c) fake device and a listed reader; (d) as (c) with osslsigncode failing on the second file; (e) as (c) with a stale pcscd reported running before start. In the image without stand-ins: osslsigncode sign --help, openssl list -providers -provider pkcs11, a search for any pkcs11 engine file, and the sudoers file. Real-binary case, test only: a test image derived from the built image adds softhsm2, creates an EC key and self-signed cert with id 01 in a token, sets PKCS11_PROVIDER_MODULE to the SoftHSM module (the provider reads that variable, not -pkcs11module) and runs the image real osslsigncode sign -provider pkcs11 -pkcs11module SOFTHSM -key pkcs11:id=%01;type=private -pkcs11cert pkcs11:id=%01;type=cert -readpass - on a tiny PE fixture, then osslsigncode verify; then the same sign with PKCS11_PROVIDER_MODULE unset.
```

```text
(a) exit 1 within 5 s, output contains plug in the YubiKey and re-run this job, pcscd never started, osslsigncode never invoked; (b) exit 1 within 7 s, same message plus the check-host.sh hint, pcscd started then stopped, osslsigncode never invoked; (c) exit 0, osslsigncode invoked exactly three times, once per file, each argv with -provider pkcs11 and -readpass - and without -pkcs11engine, the sentinel on stdin and never on argv, pcscd stopped; (d) non-zero exit, two invocations, no third, pcscd stopped; (e) pkill -x pcscd runs before the pcscd start. The sentinel never appears in stdout or stderr. Tools: help lists -provider, the pkcs11 provider is active, no pkcs11 engine file exists, sudoers is 0440 root:root and allows exactly /usr/sbin/pcscd --disable-polkit and /usr/bin/pkill -x pcscd with no * and no ALL command. Real-binary: with the variable set to the SoftHSM module and the image own OpenSSL provider config (/etc/ssl/openssl-pkcs11.cnf, which sets pkcs11-module-quirks = no-deinit) in OPENSSL_CONF, the real osslsigncode signs the PE against the software token and exits 0 (no crash at exit), and verify accepts the signature against the test cert; with it unset, sign exits non-zero and writes no signed file. Signing with the real YubiKey and Verify signing coverage: maintainer at EXAMINE (P2).
```

```text
Signing before the reader check, a missing trap, no stale-pcscd stop, an unbounded wait, -pass on argv, an -pkcs11engine argument, a retry of a failed file, a sudoers wildcard, the Debian osslsigncode 2.9, an installed engine that signs without the provider, or a provider that cannot sign each turns a named check red.
```

##### Promises

P2

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

Verify signing coverage

\.github\/workflows\/ci\_codesign\-windows\.yml\:131

Reuse

```text
Unchanged consumer of the signed files
```

Sign on Linux

\.github\/workflows\/ci\_codesign\-windows\.yml\:59

Extend

```text
Inline osslsigncode loop moves into sign-windows.sh; globs, intermediate cert, timestamp URL and description stay; engine flag and -pass on argv go
```

sign\-windows\.sh

tools\/codesign\/sign\-windows\.sh\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

reader\.conf

tools\/codesign\/reader\.conf\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

sudoers\-pcscd

tools\/codesign\/sudoers\-pcscd\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

Dockerfile

tools\/codesign\/Dockerfile\:1

CreateNew

```text
Created by this feature in S1 (no owner existed before); this slice adds its part to that new file
```

codesign\-runner\-image\.yml

\.github\/workflows\/codesign\-runner\-image\.yml\:1

CreateNew

```text
Created by this feature in S1 (no owner existed before); this slice adds its part to that new file
```

openssl\-pkcs11\.cnf

tools\/codesign\/openssl\-pkcs11\.cnf\:1

CreateNew

```text
No OpenSSL provider config exists in the repository; the system openssl.cnf is included, not copied
```

##### Impact

container\-image

tools\/codesign\/Dockerfile\:1

Changed

```text
Signing toolchain added
```

runtime\-script

tools\/codesign\/sign\-windows\.sh\:1

Changed

```text
New file
```

runtime\-config

tools\/codesign\/reader\.conf\:1

Changed

```text
New file
```

runtime\-config

tools\/codesign\/sudoers\-pcscd\:1

Changed

```text
New file
```

ci\-workflow

\.github\/workflows\/ci\_codesign\-windows\.yml\:59

Changed

```text
Linux step only; Windows step, Tauri re-sign, coverage and artifact steps unchanged
```

ci\-workflow

\.github\/workflows\/codesign\-runner\-image\.yml\:1

Changed

```text
Adds the S2 oracle step
```

ci\-workflow

\.github\/workflows\/ci\_verifywindows\.yml\:1

UnchangedCompatible

```text
Consumes the same artifact names
```

runtime\-config

tools\/codesign\/openssl\-pkcs11\.cnf\:1

Changed

```text
New file, read only by the sign step through OPENSSL_CONF
```

##### Agreement

YUBI\_PIN secret delivery

\.github\/workflows\/ci\_codesign\-windows\.yml\:95

Producer

Migrated

```text
Still the step env; the script now passes it on stdin
```

YUBI\_PIN secret delivery

tools\/codesign\/sign\-windows\.sh\:1

Consumer

Migrated

```text
Reads env, writes stdin of osslsigncode, never argv
```

signed artifact names and paths

\.github\/workflows\/ci\_codesign\-windows\.yml\:153

Consumer

UnchangedCompatible

```text
Signed files replace the unsigned ones in place as before; upload names unchanged
```

##### Not applicable

prefactoring

```text
No existing code moves: the slice adds new files or replaces one workflow step and one script; there is no GREEN oracle over the old Linux sign step to preserve, and preserving the engine path is the opposite of the goal
```

architectural_tests

```text
Covered by the shared action-pins and dependency-bot guards; the provider-only law is a slice oracle check, not a dependency law
```

##### Boundaries

```text
S1: docker run of the image entrypoint and docker compose config on the signing machine; S2: the Sign on Linux step running tools/codesign/sign-windows.sh in the container; S3: tools/codesign/check-host.sh run by the maintainer
```

###### Driven ports

```text
GitHub API, GHCR, USB/pcscd/pkcs11 provider, timestamp server (see shared boundaries)
```

```text
Repo scripts depend on the image tool set; the host depends only on Docker, compose file, unit and token file
```

###### Failures

####### B4

```text
No USB device with vendor 1050
```

Refusal

```text
plug in the YubiKey and re-run this job; exit 1; no pcscd; no PIN read
```

####### B5

```text
Key on USB but no reader within CODESIGN_READER_WAIT
```

Refusal

```text
plug-in message plus: if it is plugged in, a host process may hold it, run tools/codesign/check-host.sh; pcscd stopped
```

####### B6

```text
osslsigncode fails (provider not loaded, wrong PIN, key removed, timestamp server down)
```

Refusal

```text
Non-zero exit naming the file; no retry of that file (a wrong PIN retried would count down the PIV retry counter); pcscd stopped
```

####### B8

```text
A pcscd from an earlier killed run is still running
```

Retry

```text
The step stops it (pkill -x pcscd) before starting its own and continues
```

##### Observations

```text
Stand-in call log: order and argv of pcscd start/stop and osslsigncode calls, stdin received by osslsigncode
```

line\-per\-call log file written by the stand\-ins

```text
Whether the real YubiKey accepts the PIN and produces a valid Authenticode signature (maintainer at EXAMINE, P2)
```

```text
Exit status, elapsed seconds and output of the sign script
```

exit code\, seconds\, stdout\/stderr

```text
What GitHub masks in the job log; the sentinel check covers the script own output only
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
The PIN is never read or sent before a reader is listed; with no key the step exits with the plug-in message and osslsigncode is never invoked
```

Invariant

Infrastructure

tools\/codesign\/sign\-windows\.sh

###### Treatment

Finite

###### Cases

```text
(a) no USB key
```

```text
(b) key, no reader
```

```text
Closed finite set of lifecycle paths and argv shapes; one example case per path exercises the script; the Quint model covers the lifecycle design, not the script
```

###### Evidence

\/sign\_lifecycle\-\-verify\-\-safety\.txt

\/sign\_lifecycle\_M1\-\-run\-\-noPinWithoutReader\.txt

\/sign\_lifecycle\_M4\-\-run\-\-plugInMessageBeforeAnyPin\.txt

###### L2

```text
pcscd is stopped on every exit of the step
```

Invariant

Infrastructure

tools\/codesign\/sign\-windows\.sh

###### Treatment

Finite

###### Cases

```text
(b) reader timeout
```

```text
(c) success
```

```text
(d) signing failure
```

```text
Closed finite set of lifecycle paths and argv shapes; one example case per path exercises the script; the Quint model covers the lifecycle design, not the script
```

###### Evidence

\/sign\_lifecycle\-\-verify\-\-safety\.txt

\/sign\_lifecycle\_M2\-\-run\-\-daemonStoppedWhenDone\.txt

###### L3

```text
The step ends within a bound: 5 s without a key, CODESIGN_READER_WAIT plus 5 s without a reader
```

Invariant

Infrastructure

tools\/codesign\/sign\-windows\.sh

###### Treatment

Finite

###### Cases

```text
(a) elapsed <= 5 s
```

```text
(b) elapsed <= 7 s with wait 2
```

```text
Closed finite set of lifecycle paths and argv shapes; one example case per path exercises the script; the Quint model covers the lifecycle design, not the script
```

###### Evidence

\/sign\_lifecycle\_M3\-\-run\-\-waitIsBounded\.txt

\/sign\_lifecycle\_M3\-\-run\-\-protocolStepsBounded\.txt

###### L4

```text
Success means each of the server exe, the NSIS exe and the MSI was signed exactly once; a failed file is not retried
```

Invariant

Infrastructure

tools\/codesign\/sign\-windows\.sh

###### Treatment

Finite

###### Cases

```text
(c) three calls, one per file
```

```text
(d) two calls, non-zero
```

```text
Closed finite set of lifecycle paths and argv shapes; one example case per path exercises the script; the Quint model covers the lifecycle design, not the script
```

###### Evidence

\/sign\_lifecycle\_M5\-\-run\-\-signedMeansAllFiles\.txt

\/sign\_lifecycle\-\-run\-\-witnessNeverSigned\.txt

###### L5

```text
Every osslsigncode call goes through the pkcs11 provider: -provider pkcs11 present, -pkcs11engine absent, PIN on stdin not argv, and the image carries no pkcs11 engine
```

Invariant

Adapter

tools\/codesign\/Dockerfile

###### Treatment

Finite

###### Cases

```text
(c) argv of each call
```

```text
image: help lists -provider, provider active, no engine file
```

```text
Closed finite set of lifecycle paths and argv shapes; one example case per path exercises the script; the Quint model covers the lifecycle design, not the script
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
No PIN is sent to a token unless a reader was listed, and the YubiKey is released (pcscd stopped) when the step ends
```

```text
A PIN sent blind or a pcscd left holding the key blocks the next release or burns PIV retries
```

tools\/codesign\/sign\-windows\.sh

```text
YUBI_PIN env, artifacts on disk, USB sysfs, reader list
```

sign\-windows\.sh main\: check\_key\, start\_pcscd\, wait\_for\_reader\, sign\_files under trap stop\_pcscd

ci\_codesign\-windows\.yml Sign on Linux step\, then Verify signing coverage

```text
Step exit, message, stand-in call log in the S2 oracle; the signed release at EXAMINE
```

```text
Another workflow step calling osslsigncode directly; SIGKILL of the container skipping the trap (next start stops a stale pcscd)
```

##### States

Admitted

```text
Signed: three files signed, pcscd stopped
```

None.

Admitted

```text
Refused: no key or no reader, plug-in message, no PIN sent, pcscd stopped
```

None.

Admitted

```text
Failed during signing: PIN sent, non-zero exit, pcscd stopped, artifacts not uploaded
```

None.

Excluded

```text
PIN sent with no reader; pcscd running after the step; success with fewer than three files; signing through the engine
```

None.

Unknown

```text
Behaviour of pcscd or opensc on a key model other than product id 0407
```

maintainer

##### Models

Quint

docs\/feature\/story\-6651\-codesign\-runner\-container\/models\/sign\_lifecycle\.qnt

###### Properties

noPinWithoutReader

Invariant

```text
The PIN is sent only after a reader was found
```

sign\_files

tools\/codesign\/sign\-windows\.sh

plugInMessageBeforeAnyPin

Invariant

```text
The plug-in refusal is never printed after a PIN was sent
```

wait\_for\_reader

tools\/codesign\/sign\-windows\.sh

daemonStoppedWhenDone

Invariant

```text
Every terminal state has pcscd stopped
```

stop\_pcscd

tools\/codesign\/sign\-windows\.sh

waitIsBounded

Invariant

```text
The reader wait never exceeds its deadline
```

wait\_for\_reader

tools\/codesign\/sign\-windows\.sh

signedMeansAllFiles

Invariant

```text
Success only after every file is signed
```

sign\_files

tools\/codesign\/sign\-windows\.sh

protocolStepsBounded

Invariant

```text
Every run takes at most MAX_WAIT+FILES+3 protocol steps (bounded response)
```

main

tools\/codesign\/sign\-windows\.sh

###### Atoms

Action

startDaemon

start\_pcscd

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

readerAppears

wait\_for\_reader

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

waitTick

wait\_for\_reader

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

giveUp

wait\_for\_reader

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

signOne

sign\_file

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

finish

main

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

cancel

stop\_pcscd

tools\/codesign\/sign\-windows\.sh

```text
one shell function call in the single script process
```

Action

toggleKey

environment\: YubiKey plugged or unplugged

host USB

None.

###### Assumptions

```text
One sequential script process; each protocol step is atomic at script level
```

```text
SIGKILL of the container is outside the model: the trap does not run; next run stops any stale pcscd before starting
```

```text
The no-USB-key pre-check path (Idle to Failed, no pcscd, no PIN) is not modelled; it satisfies every invariant because it changes only phase and the message
```

```text
Bounded check: quint verify --max-steps=12 with MAX_WAIT=3 and FILES=3
```

###### Mutants

######## N1

```text
M1: sign while still waiting for the reader
```

###### Kills

noPinWithoutReader

######## N2

```text
M2: give up without stopping pcscd
```

###### Kills

daemonStoppedWhenDone

######## N3

```text
M3: reader wait without a deadline
```

###### Kills

waitIsBounded

protocolStepsBounded

######## N4

```text
M4: print the plug-in message when signing fails after the PIN
```

###### Kills

plugInMessageBeforeAnyPin

######## N5

```text
M5: finish after the first signed file
```

###### Kills

signedMeansAllFiles

##### Closure

```text
sign-step lifecycle
```

Safety

Pass

DesignedNotBuilt

```text
quint verify (Apalache) of safety to 12 steps passed; five behavioural mutants each killed by quint run counterexamples
```

###### Evidence

docs\/feature\/story\-6651\-codesign\-runner\-container\/models\/receipts\/sign\_lifecycle\-\-verify\-\-safety\.txt

docs\/feature\/story\-6651\-codesign\-runner\-container\/models\/receipts\/sign\_lifecycle\-\-run\-\-safety\.txt

```text
sign-step lifecycle
```

Inhabitation

Pass

DesignedNotBuilt

```text
Witness runs reach Signed and the plug-in refusal
```

###### Evidence

docs\/feature\/story\-6651\-codesign\-runner\-container\/models\/receipts\/sign\_lifecycle\-\-run\-\-witnessNeverSigned\.txt

docs\/feature\/story\-6651\-codesign\-runner\-container\/models\/receipts\/sign\_lifecycle\-\-run\-\-witnessNeverPlugInMessage\.txt

```text
signed release through the pkcs11 provider
```

PublicReachability

Pass

Indeterminate

```text
release run -> codesign-windows job -> sign-windows.sh in the container -> Verify signing coverage -> verifywindows; the real key path is observable only by the maintainer (P2)
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

tools\/codesign\/check\-host\.sh

Create

```text
Reports every leftover of the retired host setup and every host holder of the key, exit 1 with a HOW per finding
```

tools\/codesign\/README\.md

Extend

```text
Rewritten: fresh-machine setup of the container runner incl. the one-time register run and watchtower, the manual retire steps, why disable-ccid and no host pcscd, rollback by pinning CODESIGN_RUNNER_TAG, what to check when the sign job waits for a runner, re-register after a revoked registration, recognising a locked PIN; prose-only, verified by review and EXAMINE
```

tools\/codesign\/start\-runner\-linux\.ps1

Extend

```text
Deleted: replaced by entrypoint.sh, compose.yaml and the unit
```

\.github\/workflows\/codesign\-runner\-image\.yml

Extend

```text
Runs the S3 oracle in the image workflow
```

tools\/codesign\/tests\/test\-host\-check\.sh

#### Verification

```text
bash tools/codesign/tests/test-host-check.sh
```

#### Supports

None.

#### Public oracle

```text
Run tools/codesign/check-host.sh with HOME set to a fixture home and recording stand-ins for systemctl, docker, pgrep and gpgconf first on PATH, once with the old-setup fixture and once with the clean fixture; also check that tools/codesign/start-runner-linux.ps1 no longer exists.
```

```text
Old setup: exit 1 and one finding each, with its HOW, for the host actions-runner service, ~/.local/bin/osslsigncode, the local/osslsigncode-ossl3 image, an enabled or active pcscd.socket or pcscd.service, and scdaemon.conf without disable-ccid. Clean fixture: exit 0 and the line host is clean. start-runner-linux.ps1 is absent. On the real machine the maintainer runs check-host.sh at EXAMINE (P5); the README is reviewed as prose (P6).
```

```text
A check-host.sh that reports clean while one leftover is present, or that exits 0 with a finding, turns that fixture red; a remaining start-runner-linux.ps1 turns the absence check red.
```

##### Promises

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

start\-runner\-windows\.ps1

tools\/codesign\/start\-runner\-windows\.ps1\:1

Reuse

```text
Windows runner untouched
```

Lighthouse Code Signing Setup

tools\/codesign\/README\.md\:1

Extend

```text
Windows sections stay; Linux sections replaced by the container setup and the retire steps
```

check\-host\.sh

tools\/codesign\/check\-host\.sh\:1

CreateNew

```text
No existing owner: the repository has no container runner image, entrypoint, compose/unit, image workflow, sign script, reader entry, sudoers line or host check; start-runner-linux.ps1 and the inline workflow step are replaced, not extended
```

start\-runner\-linux\.ps1

tools\/codesign\/start\-runner\-linux\.ps1\:4

Replace

```text
Registration moved to entrypoint.sh, compose.yaml and the unit (S1), preflight to sign-windows.sh (S2); the file is deleted
```

codesign\-runner\-image\.yml

\.github\/workflows\/codesign\-runner\-image\.yml\:1

CreateNew

```text
Created by this feature in S1 (no owner existed before); this slice adds its part to that new file
```

##### Impact

runtime\-script

tools\/codesign\/check\-host\.sh\:1

Changed

```text
New file
```

documentation

tools\/codesign\/README\.md\:1

Changed

```text
Prose-only change
```

runtime\-script

tools\/codesign\/start\-runner\-linux\.ps1\:1

Changed

```text
Removed; only reference was README.md
```

ci\-workflow

\.github\/workflows\/codesign\-runner\-image\.yml\:1

Changed

```text
Adds the S3 oracle step
```

##### Agreement

None.

##### Not applicable

agreement

```text
Touches no shared contract: check-host.sh and the README have no machine consumer
```

prefactoring

```text
No existing code moves: the slice adds new files or replaces one workflow step and one script; there is no GREEN oracle over the old Linux sign step to preserve, and preserving the engine path is the opposite of the goal
```

architectural_tests

```text
Covered by the shared action-pins and dependency-bot guards; the provider-only law is a slice oracle check, not a dependency law
```

contract_tests

```text
No independently changing producer and consumer
```

##### Boundaries

```text
S1: docker run of the image entrypoint and docker compose config on the signing machine; S2: the Sign on Linux step running tools/codesign/sign-windows.sh in the container; S3: tools/codesign/check-host.sh run by the maintainer
```

###### Driven ports

```text
GitHub API, GHCR, USB/pcscd/pkcs11 provider, timestamp server (see shared boundaries)
```

```text
Repo scripts depend on the image tool set; the host depends only on Docker, compose file, unit and token file
```

###### Failures

####### B7

```text
Any leftover or host key holder found
```

Refusal

```text
exit 1, finding with HOW
```

##### Observations

```text
check-host.sh findings and exit status
```

exit code and one line per finding

```text
Holders it does not know about; whether the README reads well (review, EXAMINE)
```

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
host retirement
```

Safety

Pass

DesignedNotBuilt

```text
Finite list of five leftovers, each a fixture case in the S3 oracle
```

###### Evidence

None.

```text
clean host after retirement
```

PublicReachability

Pass

Indeterminate

```text
Only the maintainer runs check-host.sh on the real machine (P5)
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
