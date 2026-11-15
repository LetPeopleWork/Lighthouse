# Lighthouse Code Signing Setup

Lighthouse signs its Windows binaries (the server exe, the NSIS setup exe and the MSI) on a self-hosted
GitHub Actions runner. The job `codesign-windows` in `.github/workflows/ci_codesign-windows.yml` asks for
a runner with the labels `self-hosted` and `codesign`. Two kinds of runner can take it:

- **Linux, in a container (the normal case):** the runner runs in a Docker container on the maintainer's
  Linux machine and signs with `osslsigncode` and the YubiKey. Most of this file is about it.
- **Windows:** a runner registered on a Windows machine signs with `signtool.exe` and a certificate in the
  Windows certificate store. See [Windows runner](#windows-runner) at the end.

## What the Linux runner is, and why it is a container

The runner is the image `ghcr.io/letpeoplework/lighthouse-codesign-runner`. It holds the GitHub Actions
runner, `osslsigncode` with the OpenSSL pkcs11 provider, OpenSC and its own `pcscd`. The image is built and
checked in GitHub Actions and published to GHCR, so the machine runs a tested, pinned tool set and nothing
has to be installed or kept up to date on the host. The host only needs Docker, `compose.yaml`, a systemd
unit and a token file. The container starts on boot by itself, and the YubiKey has to be plugged in only
when you sign.

| File | What it does |
|---|---|
| `Dockerfile` | The runner image |
| `entrypoint.sh` | Starts the runner (`run`), or registers it once with GitHub (`register`) |
| `compose.yaml` | The `runner` service (always on) and the one-time `register` service |
| `lighthouse-codesign-runner.service` | The systemd unit that pulls and starts the runner on boot |
| `sign-windows.sh` | The signing step the release job runs inside the container |
| `openssl-pkcs11.cnf`, `reader.conf`, `sudoers-pcscd` | Image configuration: the pkcs11 provider, the YubiKey reader, and the one `pcscd` right the runner user has |
| `SSL_COM_CODE_SIGNING_INTERMEDIATE_CA_ECC_R2.crt` | The intermediate certificate added to every signature |
| `check-host.sh` | Looks for anything on the host that is left from the old setup or that would take the key away from the container |
| `tests/` | The checks the image workflow runs |
| `start-runner-windows.ps1` | Registers a Windows runner |

## Prerequisites

- A Linux machine with systemd, Docker and the Docker Compose plugin (`docker compose`).
- A free USB port for the YubiKey. The key holds the code signing certificate and its private key.
- Network access to `ghcr.io`. The image is public, so `docker pull` needs no login.
- Optional: [watchtower](https://containrrr.dev/watchtower/). Nothing in this setup installs or runs it.
  The unit pulls the image on every boot; between reboots a newly published image reaches the machine
  only if you run watchtower yourself, for example from a regular update script. Without it, a new image
  arrives at the next boot. One run, limited to the runner container, is:

  ```bash
  docker run --rm -v /var/run/docker.sock:/var/run/docker.sock containrrr/watchtower --run-once lighthouse-codesign-runner-1
  ```

  Run it from a systemd timer or cron job, or keep watchtower running as its own container without
  `--run-once`. `lighthouse-codesign-runner-1` is the name Docker Compose gives the runner container
  (`docker compose ps` in `/opt/lighthouse-codesign` shows it).
- Optional: the GitHub CLI `gh` (to remove the old host runner's registration, see the switch-over below)
  and `ykman` from YubiKey Manager (to read or unblock the PIN, see signing day).
- A GitHub token that may register runners on `LetPeopleWork/Lighthouse` (a fine-grained token with the
  repository permission "Administration: read and write", or a classic token with the `repo` scope).

## One-time setup on a fresh machine

Do these steps as the user with uid 1000. The `runner` user in the image has that uid, so only it can read
your token file inside the register container.

1. Install the compose file and the unit:

   ```bash
   sudo mkdir -p /opt/lighthouse-codesign
   sudo cp tools/codesign/compose.yaml /opt/lighthouse-codesign/compose.yaml
   sudo cp tools/codesign/lighthouse-codesign-runner.service /etc/systemd/system/
   sudo systemctl daemon-reload
   ```

   Another folder works too (for example `/storage/docker-tools/lighthouse-codesign`), if you change
   `WorkingDirectory=` in the unit to match; use that folder wherever these steps say `/opt/lighthouse-codesign`.

2. Write the GitHub token to a file only you can read. It stays outside the repository and outside the
   always-on runner: only the one-time register service mounts it.

   ```bash
   mkdir -p ~/.config/lighthouse-codesign
   (umask 077 && printf '%s\n' 'YOUR_TOKEN' > ~/.config/lighthouse-codesign/github-token)
   ls -ln ~/.config/lighthouse-codesign/github-token   # owner 1000, mode -rw-------
   ```

3. Register the runner once. This writes the registration into the Docker volume `codesign-runner-state`,
   which every later start reads.

   ```bash
   cd /opt/lighthouse-codesign
   docker compose --profile register run --rm register
   ```

   The runner registers as `codesign-runner-linux` with the labels `linux` and `codesign`. Registering
   replaces a runner of the same name, so on a second machine give it another name:
   `docker compose --profile register run --rm -e CODESIGN_RUNNER_NAME=codesign-runner-2 register`.
   On one machine there is only one registration: the volume `codesign-runner-state` holds it, so a test
   registration under another name replaces this machine's real one. After such a test, run the register
   command above again without `CODESIGN_RUNNER_NAME`, and remove the test runner in GitHub.
   You can delete the token file after this step; you need a token again only to register again.

4. Resolve the host conflicts in the next section.

5. Start the runner now and on every boot:

   ```bash
   sudo systemctl enable --now lighthouse-codesign-runner
   ```

6. Check that GitHub shows `codesign-runner-linux` as **Idle** under Settings, Actions, Runners, and run
   `bash tools/codesign/check-host.sh` until it says `host is clean`.

After a reboot nothing is manual: the unit pulls the image and starts the runner. When GHCR cannot be
reached, it starts the cached image and `journalctl -u lighthouse-codesign-runner` shows the failed pull as
a warning. Between reboots, a newer image arrives only when you run watchtower (see Prerequisites).

## Host conflicts, and why

Only one program at a time can talk to the YubiKey over USB. The container brings its own `pcscd`, which
the signing step starts and stops. Anything on the host that holds the key makes the signing step fail
with "no reader appeared".

- **GnuPG's scdaemon:** add a line `disable-ccid` to `~/.gnupg/scdaemon.conf`, then run
  `gpgconf --kill scdaemon`. Without it, scdaemon claims the key's USB interface whenever gpg looks for a
  card.
- **The host's pcscd:** disable both the socket and the service with
  `sudo systemctl disable --now pcscd.socket pcscd.service`. Disabling only the service is not enough:
  the socket starts pcscd again as soon as anything asks for a card.

The consequence: while this setup is active, the YubiKey cannot be used for GPG or PIV on the host itself.
To use it there for a while, stop the runner (`sudo systemctl stop lighthouse-codesign-runner`), start the
host's `pcscd.socket`, and undo both before you sign again.

## Switching over from the old host runner

Before the container runner, the runner ran directly on the host, installed by a PowerShell script, with a
`~/.local/bin/osslsigncode` wrapper that ran a locally built image. Retire it in this order:

1. Stop and remove the old runner service and its registration. In its folder (it was
   `tools/codesign/actions-runner` in the Lighthouse checkout), run:

   ```bash
   sudo ./svc.sh stop
   sudo ./svc.sh uninstall
   ./config.sh remove --token "$(gh api -X POST repos/LetPeopleWork/Lighthouse/actions/runners/remove-token --jq .token)"
   ```

   Then delete the folder. If GitHub no longer lists the old runner, skip `config.sh remove`.
2. Remove the wrapper with `rm ~/.local/bin/osslsigncode`. If you added `~/.local/bin` to your PATH only for
   the wrapper, remove that line too.
3. Remove the old image. `docker images local/osslsigncode-ossl3` (or `check-host.sh`) shows its tag, which
   is usually `trixie`; remove each one it shows, for example
   `docker image rm local/osslsigncode-ossl3:trixie`.
4. Resolve the host conflicts above.
5. Run `bash tools/codesign/check-host.sh`. It prints one line per leftover, each with what to do, and
   exits 1; repeat until it says `host is clean` and exits 0. It only looks and never changes anything. If
   it says Docker or systemd could not be asked, it did not finish looking: fix that first.

## Signing day

1. Plug in the YubiKey.
2. Approve the release. The job `codesign-windows` runs `sign-windows.sh` in the container and signs the
   three files.

What a failure means:

- **"no YubiKey is on USB. Plug in the YubiKey and re-run this job."** The key is not plugged in. Plug it in
  and re-run the job. No PIN was sent.
- **"the YubiKey is on USB but no reader appeared"**: the key is plugged in, but a host process holds it.
  Run `bash tools/codesign/check-host.sh`, do what it says, then re-run the job. No PIN was sent.
- **A PIN or login error on the first file**, such as `CKR_PIN_INCORRECT` or `CKR_PIN_LOCKED`: the PIN in
  the `YUBI_PIN` secret is wrong, or the key's PIN is locked after too many wrong PINs. **Do not re-run the
  job**: every run spends another PIN attempt. The step never retries a file, so a failed run spends at most
  one. To see how many tries are left, stop the runner, start the host's `pcscd.socket` and run
  `ykman piv info`. A locked PIN shows 0 tries left; unblock it with the PUK (`ykman piv access
  unblock-pin`). Make sure the `YUBI_PIN` secret holds the right PIN, undo the host changes, and only then
  re-run.

## Rolling back to an earlier image

The runner uses the tag `latest` unless you pin another. When a new image breaks signing:

1. Find the last good tag `sha-<commit>`: on the GHCR package page, or in the publish job of the image
   workflow run for that commit.
2. Write it into the `.env` file next to `compose.yaml`:

   ```bash
   echo 'CODESIGN_RUNNER_TAG=sha-<commit>' | sudo tee -a /opt/lighthouse-codesign/.env
   ```

3. `cd /opt/lighthouse-codesign && docker compose pull runner && docker compose up -d runner`
4. Re-run the failed sign job.

watchtower now watches only that fixed tag, so it no longer moves the runner by itself. The registration is
kept, so you do not register again. To go back to `latest`, remove the line from `.env` and repeat step 3.

## When the sign job waits for a runner

If no runner is online, the release does not fail: the job `codesign-windows` waits with "Waiting for a
runner to pick up this job" for up to 24 hours. Check on the signing machine:

1. GitHub, Settings, Actions, Runners: is `codesign-runner-linux` listed, and is it Idle or Offline?
2. `systemctl status lighthouse-codesign-runner` and `journalctl -u lighthouse-codesign-runner`: did the
   unit start? A failed pull is only a warning.
3. `cd /opt/lighthouse-codesign && docker compose ps` and `docker compose logs runner`: is the container
   running, or restarting again and again?
4. If the log says the runner is not registered, or shows an authentication or registration error while
   the container keeps restarting, the registration was deleted, revoked or has expired. Write a fresh
   token to the token file and register again:

   ```bash
   cd /opt/lighthouse-codesign
   docker compose stop runner
   docker compose --profile register run --rm register
   docker compose up -d runner
   ```

## How the image is built and published

The workflow `.github/workflows/codesign-runner-image.yml` ("Codesign Runner Image") builds the image apart
from the main CI. Its job `verify` builds the image and runs the checks in `tests/`; `Codesign Runner Image
/ verify` is a required check on pull requests. On `main` (after a change under `tools/codesign/`, every
week, and on demand) the job `publish` pushes the tag `sha-<commit>`, checks the pushed image, and only then
moves `latest` to it. The weekly run brings security fixes of the base packages to the machine without a
code change.

Renovate opens pull requests for the pinned base image and the GitHub Actions runner version. A runner
version bump also needs its checksum: edit `RUNNER_SHA256` in the `Dockerfile` by hand to the sha256 of the
new `actions-runner-linux-x64-<version>.tar.gz`. Until then the image build, and with it the required
check, fails on purpose.

## Windows runner

`start-runner-windows.ps1` downloads and registers a runner on Windows (`.\start-runner-windows.ps1 -Help`
lists its options; it needs `GITHUB_PAT` set). The machine needs `signtool.exe` from the Windows SDK on the
PATH and the code signing certificate in the "Personal" store of the user that runs the runner. The job
reads the certificate's SHA1 thumbprint from the `CERT_THUMBPRINT` secret and signs with:

```powershell
signtool.exe sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 $thumbprint /d "Lighthouse $version" /du "https://letpeople.work/lighthouse" $file
```

- **Thumbprint mismatch**: compare the thumbprint in certmgr.msc; paste it into the secret without spaces.
- **signtool not found**: add the Windows SDK bin folder to the system PATH.

## Security

- The private key never leaves the YubiKey: `osslsigncode` signs through the pkcs11 provider and OpenSC.
- The PIN reaches `osslsigncode` on stdin, never as a command-line argument, and is never printed.
- The GitHub token is mounted only into the one-time register container, never into the always-on runner,
  so no job can read it.
- The `runner` user may run exactly two commands as root (start `pcscd`, stop `pcscd`), because jobs run
  repository code as that user.
- Keep the signing machine physically secure, and unplug the YubiKey when you are done signing.
