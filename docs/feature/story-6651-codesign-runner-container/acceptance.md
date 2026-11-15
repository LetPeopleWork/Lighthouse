# Acceptance brief: story\-6651\-codesign\-runner\-container

## Slices

### S1

##### Covers

P4

```text
Build the runner image and read every base line of its Dockerfile
```

```text
Every build stage starts from ubuntu:26.04 pinned by a sha256 digest
```

```text
A rebuild of the same commit cannot pick up a different base, and Renovate can propose base digest bumps
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:every build stage starts from ubuntu\:26\.04 pinned by digest

```text
A digest that is well formed but not a real ubuntu:26.04 digest; the digest is not looked up in a registry
```

###### Obligations

None.

##### Covers

P4

```text
Read the labels of the built image
```

```text
The image carries the source label https://github.com/LetPeopleWork/Lighthouse
```

```text
The published package appears under the Lighthouse repository in GHCR
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the image links its GHCR package to the Lighthouse repository

```text
A correct label on an image that is never pushed; publishing is observed only in the workflow definition
```

###### Obligations

None.

##### Covers

P1

P4

```text
Look inside the built image at the pristine runner, the state and run directories and the users
```

```text
The pristine runner has config.sh and run.sh and reports the runner version the Dockerfile pins; the state and run directories belong to runner; the image runs as runner with uid 1000 and no user ubuntu remains
```

```text
The runner GitHub sees is the pinned version, jobs do not run as root, uid 1000 has one name, and a fresh state volume can receive the registration
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the image carries the GitHub runner at the version the Dockerfile pins

```text
A runner whose tarball checksum is not verified at build; the checksum check is only exercised by a version bump
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in run mode against an empty state volume
```

```text
It exits with failure within a minute, names docker compose --profile register run --rm register, and starts neither run.sh nor config.sh
```

```text
An unregistered machine tells the maintainer the one command to run instead of looping offline
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:a runner that was never registered refuses to start and says how to register

```text
A message naming the command inside otherwise misleading text; only the command text and exit status are checked
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in run mode against a state volume holding the three registration files
```

```text
run.sh starts, config.sh does not, and the state volume holds no directory or executable afterwards
```

```text
After a reboot the runner comes back from its saved registration without registering again
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:a registered runner starts the runner and does not register again

```text
An entrypoint that writes extra plain data files into the volume; only directories and executables count as binaries
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in run mode against a state volume an older image used, which also holds an old run.sh
```

```text
run.sh runs from /home/runner/runtime as a real, writable copy with the three registration files beside it, reports the version the Dockerfile pins, and the volume's old run.sh never runs
```

```text
An image update always runs the new runner version, and the runner can write its state next to itself
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the runner runs from a writable copy of the image runner\, not from the volume

```text
An entrypoint that copies only part of /opt/runner; the recorded run.sh and the runner binary are checked, not every file of the copy
```

###### Obligations

None.

##### Covers

P1

```text
Start the image with its real run.sh, no stand-ins and no network, against a volume holding the three registration files
```

```text
Within a minute the runner prints neither Must configure nor Permission denied; a network or authentication failure is accepted
```

```text
The real runner accepts the saved registration, so on the machine it can connect and show online
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the real runner starts with the registration in place

```text
A runner that fails for another reason (for example unreadable fixture credentials) before it would print either phrase
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in register mode with a token file mounted where the design puts the secret
```

```text
curl receives the token only through a file or stdin and never on its command line; the token appears in no output and not on config.sh's command line
```

```text
Registering the runner does not leak the GitHub token to logs or other processes
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:registration keeps the GitHub token off the command line and out of the output

```text
A token written to a file inside the container that a later job could read; only argv, output and what curl received are observed
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in register mode with a token file and a fresh state volume
```

```text
config.sh runs from /home/runner/runtime with labels linux,codesign and --disableupdate, and the three registration files land in the fresh volume
```

```text
One registration command makes the runner selectable by the release job and survives every reboot
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:registration configures the runner with the codesign labels and keeps the result in the volume

```text
Wrong --url or --name passed to config.sh; only labels, the update switch, location and the saved files are checked
```

###### Obligations

None.

##### Covers

P1

```text
Start the image in register mode without a token file
```

```text
It exits with failure, names /run/secrets/github_token, and config.sh does not run
```

```text
The maintainer learns which file to create instead of getting a half-registered runner
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:registration without a token file refuses and names where the token goes

```text
A message that names the container path but not the host file to create; only the container path is checked
```

###### Obligations

None.

##### Covers

P1

```text
Render the compose file with a token file present and no profile
```

```text
Service runner restarts unless stopped, uses init, binds /dev/bus/usb, has the cgroup rule c 189:* rmw, is not privileged, has no fixed devices, mounts the state volume, uses the latest GHCR image and has no secrets
```

```text
The always-on runner restarts by itself, reaches the key when it is plugged in, and no job can read the GitHub token
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the always\-on runner service restarts\, reaches the USB key and holds no token

```text
A runner service with extra capabilities or host mounts other than the ones checked
```

###### Obligations

None.

##### Covers

P1

```text
Render the compose file with the register profile
```

```text
Service register has profile register and the github_token secret, whose file is an absolute path outside the repository
```

```text
Only the one-shot registration ever sees the token, and the token never sits in the checkout
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the one\-shot register service alone gets the token\, from a file outside the repository

```text
A token file path outside the repository but in a world-readable place; the location's permissions are not checked
```

###### Obligations

None.

##### Covers

P1

```text
Render the compose file when the token file does not exist
```

```text
The runner service still renders
```

```text
The machine starts the runner on boot even after the token file was deleted
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the runner service still renders when the token file is missing

```text
A compose file that renders but whose up would fail for another reason; only rendering is observed
```

###### Obligations

None.

##### Covers

P1

```text
Read the systemd unit
```

```text
It pulls with a leading minus before start, then runs docker compose up -d runner, after docker.service, wanted by multi-user.target
```

```text
After a reboot the runner starts by itself, even when GHCR cannot be reached
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the boot unit pulls the latest image without blocking on a failed pull\, then starts the runner

```text
A unit that runs compose without pointing at the installed compose file; the working directory or -f path is not checked
```

###### Obligations

None.

##### Covers

P4

```text
Read the triggers and the verify job of the image workflow
```

```text
The workflow is named Codesign Runner Image, triggers on pull_request with no paths filter, and has a job verify
```

```text
The required check Codesign Runner Image / verify reports on every pull request
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the image workflow reports on every pull request as Codesign Runner Image \/ verify

```text
A pull_request trigger restricted to other branches; branch filters are not checked
```

###### Obligations

None.

##### Covers

P4

```text
Read the push and schedule triggers of the image workflow
```

```text
It runs on push to main for tools/codesign/** and on a schedule
```

```text
Changes to the image files and Debian fixes are rebuilt without anyone starting a run
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the image is rebuilt on changes under tools\/codesign on main and on a schedule

```text
A schedule that almost never fires; its frequency is not checked
```

###### Obligations

None.

##### Covers

P4

```text
Read the change detection job of the image workflow
```

```text
Job changes has an output codesign and its steps write codesign= with the strings true and false
```

```text
Verify can tell reliably whether the pull request touched the image
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the change detection hands verify the string true or false

```text
A diff that inspects the wrong paths; the detection logic is not executed
```

###### Obligations

None.

##### Covers

P4

```text
Read the condition and first step of the verify job
```

```text
verify needs changes; its condition holds !cancelled(), a needs.changes.result clause and a comparison of needs.changes.outputs.codesign with the quoted string true, never the boolean; its first step exits 1 when change detection did not succeed
```

```text
The required check runs when the image changed and turns red rather than skipped when change detection broke
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:verify runs when codesign changed and turns red when change detection failed

```text
A condition that holds all three parts but joins them with the wrong logic; the expression is matched, not evaluated
```

###### Obligations

None.

##### Covers

P4

```text
Read the verify job's steps
```

```text
verify runs tools/codesign/tests/test-runner-image.sh and neither pushes nor logs in to a registry
```

```text
An image that fails its checks never reaches GHCR
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:verify builds and checks the image without publishing it

```text
A verify job that runs the oracle against a different image than the one it built
```

###### Obligations

None.

##### Covers

P4

```text
Read the publish job of the image workflow
```

```text
publish needs verify, runs only for refs/heads/main on an event other than pull_request when verify succeeded, and pushes latest and sha- tags of ghcr.io/letpeoplework/lighthouse-codesign-runner
```

```text
Only a verified main build replaces the image the machine pulls
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:only a non\-pull\-request run on main whose verify passed publishes latest and the commit tag

```text
A publish that pushes before running the contract test; step order inside publish is not checked
```

###### Obligations

None.

##### Covers

P4

```text
Read the whole image workflow
```

```text
It names neither ci.yml nor ci_changes.yml
```

```text
The image pipeline runs on its own and the main CI is unaffected
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:the image workflow stands apart from the main CI workflows

```text
Edits to ci.yml or ci_changes.yml made in the same change; only references from this workflow are checked
```

###### Obligations

None.

##### Covers

P4

```text
Apply the Renovate custom managers that match tools/codesign/Dockerfile to the Dockerfile
```

```text
One of them extracts datasource github-releases, depName actions/runner and the RUNNER_VERSION the Dockerfile pins
```

```text
Renovate proposes runner version bumps instead of the maintainer tracking them
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-runner\-image\.sh\:\:Renovate reads the pinned runner version from the Dockerfile

```text
No manager for the osslsigncode version; only the runner version is checked here
```

###### Obligations

None.

##### Covers

P4

```text
Scan every action reference in the workflows, including the new image workflow
```

```text
Each is pinned to a full commit with its release named after it
```

```text
Renovate keeps the image workflow's actions current and no tag can move under it
```

##### Disposition

Tested

\.github\/scripts\/action\-pins\.test\.mjs

```text
A pin to a commit that is not the release it names
```

###### Obligations


##### Covers

P4

```text
Scan workflow conditions and Renovate rules, including the new workflow and custom manager
```

```text
No condition decides on the actor and every Renovate hold carries a reason
```

```text
A Renovate bump of the image runs the same required check as a person's change
```

##### Disposition

Tested

\.github\/scripts\/dependency\-bot\.test\.mjs

```text
A condition that singles out the bot by another field than the actor
```

###### Obligations


##### Covers

P1

```text
Reboot the signing machine with the YubiKey unplugged and look at the runner list in GitHub
```

```text
codesign-runner-linux shows online without any manual step
```

```text
The maintainer can release without starting anything by hand after a reboot
```

##### Disposition

NotTested

```text
Needs the maintainer's machine, its systemd boot and a real registration against GitHub; no oracle can observe it (design closure: public reachability is maintainer-observed)
```

maintainer

##### Covers

P4

```text
Merge a change under tools/codesign to main and look at the GHCR package
```

```text
The workflow publishes latest and a sha- tag of ghcr.io/letpeoplework/lighthouse-codesign-runner
```

```text
The machine can pull the new image
```

##### Disposition

NotTested

```text
Publishing needs a run on main with packages write; only the workflow definition is observable before merge
```

maintainer

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
