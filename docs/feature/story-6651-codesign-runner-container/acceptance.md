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

### S2

##### Covers

P3

```text
Run the sign step in the runner image with no YubiKey plugged in (only a USB root hub present)
```

```text
The step exits 1 within 5 seconds with the message plug in the YubiKey and re-run this job; pcscd is never started and osslsigncode is never called
```

```text
The maintainer sees at once that the key is missing and re-runs the job after plugging it in; no PIN was sent
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:with no YubiKey plugged in the step stops at once with the plug\-in message

```text
A script that starts no pcscd but still reads YUBI_PIN into a variable before the key check; reading without sending is not observable here
```

###### Obligations



##### Covers

P3

```text
Run the sign step in the runner image with the key on USB but no reader ever listed, reader wait 2 seconds
```

```text
The step exits 1 within 7 seconds with the plug-in message and a hint to run check-host.sh; pcscd was started; osslsigncode is never called
```

```text
The maintainer learns the key is not usable and that a host process may hold it, and no PIN was sent
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:with the key on USB but no reader the step gives up within its wait and sends no PIN

```text
A script that gives up at once without polling for a reader; only an upper bound on the wait is observed
```

###### Obligations



##### Covers

P3

```text
Same run as with the key on USB but no reader
```

```text
A pcscd stop follows the last pcscd start and no pcscd runs when the step ends
```

```text
The YubiKey is free for the next run after a refused one
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the key is released when the step ends after no reader appeared

```text
A script that stops pcscd with a direct call instead of a trap, which still stops it on this path
```

###### Obligations


##### Covers

P2

```text
Run the sign step in the runner image with the key plugged in and a reader listed, holding the server exe, the NSIS installer and the MSI
```

```text
The step exits 0, osslsigncode is called exactly three times, once for each of the three files, and each file is replaced in place by its signed copy under its own name
```

```text
The release carries the three signed files under the names the upload and the coverage check expect
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:with the key plugged in the step signs the server exe\, the NSIS installer and the MSI once each in place

```text
A script that signs the files in a different order; order is not promised
```

###### Obligations


##### Covers

P2

```text
Same signing run with the key plugged in
```

```text
Every signing call names the pkcs11 provider, the opensc module, the key and certificate of slot id 01, the SSL.com intermediate, the description Lighthouse VERSION, the product URL and the DigiCert timestamp server, reads the password from stdin, receives exactly the PIN there, has no engine and no password argument, and inherits from the script both the provider module setting and the sign step's own OpenSSL config
```

```text
Every signature goes through the provider with the PIN never on a command line
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:every signing call uses the pkcs11 provider with the PIN on stdin

```text
A script that passes the right flags but a wrong timestamp scheme the server rejects only at release time; the timestamp server is not contacted
```

###### Obligations


##### Covers

P2

```text
Same signing run with the key plugged in
```

```text
A pcscd stop follows the last pcscd start and no pcscd runs when the step ends
```

```text
The YubiKey is free again after a successful signing
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the key is released when the step ends after signing all files

```text
A script whose trap misses INT or TERM; only the normal exit path is driven
```

###### Obligations


##### Covers

P2

```text
Run the sign step with the key and a reader present while the second signing call fails
```

```text
The step exits non-zero after exactly two signing calls for two different files, and its output names the failed file
```

```text
A failed signing stops the release with the file named, and a wrong PIN is never tried twice
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:when signing one file fails the step stops with that file named and retries nothing

```text
A script that names the file only through osslsigncode's own message; the stand-in prints only Failed, so the script must name it
```

###### Obligations


##### Covers

P2

```text
Same run with the second signing call failing
```

```text
A pcscd stop follows the last pcscd start and no pcscd runs when the step ends
```

```text
The YubiKey is free again after a failed signing
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the key is released when the step ends after a failed signing

```text
A script that stops pcscd in its failure branch rather than a trap, which still stops it here
```

###### Obligations


##### Covers

P2

```text
All five sign-step runs above, with a sentinel PIN in the environment
```

```text
The sentinel appears in no output and in no recorded command line of sudo, opensc-tool, pcscd, pkill or osslsigncode, and at least one run handed it to a signing call
```

```text
The PIN never shows in the job log or the process list
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the PIN never shows in the step\'s output or on any command line it runs

```text
A script that writes the PIN to a temporary file; files are not searched
```

###### Obligations


##### Covers

P2

```text
Ask the image's osslsigncode for its sign help
```

```text
The help lists the -provider option
```

```text
The image can sign through the provider rather than an engine
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image\'s osslsigncode can sign through a provider

```text
An osslsigncode that lists -provider but is older than the provider support the PIV key needs; the real-signature check covers the working path
```

###### Obligations


##### Covers

P2

```text
List OpenSSL providers in the image with the pkcs11 provider and the opensc module
```

```text
The pkcs11 provider is listed with status active
```

```text
The provider the sign step names loads in the image
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the pkcs11 provider loads in the image

```text
A provider that loads but cannot reach the opensc module on a real key; that is a maintainer check
```

###### Obligations


##### Covers

P2

```text
Search the image's library and config folders for an OpenSSL pkcs11 engine
```

```text
No pkcs11 engine file exists
```

```text
Signing can only go through the provider, never silently through the retired engine
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image carries no pkcs11 engine

```text
An engine installed outside /usr, /opt and /etc; the unset-module real signing check covers that path
```

###### Obligations


##### Covers

P2

```text
Ask sudo in the image, as the runner user, which pcscd and other commands it may run, and read the pcscd sudoers file as root
```

```text
Starting pcscd with --disable-polkit and pkill -x pcscd are allowed; pcscd with other or no arguments, pkill of another process or by pattern, a shell and id are refused; the file is 0440 root:root with no wildcard and no ALL command
```

```text
Job code can start and stop the reader daemon and gains no other root right
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:sudo lets the runner start and stop pcscd and nothing else

```text
A second sudoers file granting another narrow command that is not one of the probed ones
```

###### Obligations


##### Covers

P2

```text
Look in the image for opensc-tool, the opensc module, pcscd, pkill and a reader entry for usb:1050/0407
```

```text
All are present
```

```text
The sign script from any checkout finds what it calls in the image the machine pulled
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image carries the reader entry and the tools the sign step calls

```text
A reader entry with a wrong driver path; only the real key at EXAMINE shows the entry works
```

###### Obligations


##### Covers

P2

```text
In a test image derived from the runner image with SoftHSM holding an EC key and self-signed certificate with id 01, sign a tiny Windows executable with the image's osslsigncode through the pkcs11 provider using the image's own sign-step OpenSSL config, only the module pointed at SoftHSM, PIN on stdin, then verify it
```

```text
Signing exits 0, writes the signed file, and verify accepts the signature against the test certificate
```

```text
The image's signing tools and its own OpenSSL config produce a valid Authenticode signature through the provider and exit cleanly
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image\'s osslsigncode makes a signature through the pkcs11 provider that verifies against the signing certificate

```text
A provider that signs with SoftHSM but not with the YubiKey PIV slot 9c, or an opensc module that still crashes at exit; that is the maintainer's check at EXAMINE
```

###### Obligations


##### Covers

P2

```text
Same SoftHSM signing with the image's sign-step OpenSSL config but the provider's module setting unset
```

```text
Signing exits non-zero and writes no signed file, while the same signing with the setting succeeded
```

```text
No engine signs in place of the provider when the provider has no module
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:without the provider\'s module setting signing fails and writes no file

```text
An engine that fails for an unrelated reason on this tiny file; the engine-file search covers presence
```

###### Obligations


##### Covers

P2

```text
Read the Sign on Linux step of the release signing workflow
```

```text
It runs only on Linux runners, calls tools/codesign/sign-windows.sh with an environment variable as its version argument, sets that variable in the step environment to exactly the workflow's inputs.version expression, holds no workflow expression anywhere in its run, keeps YUBI_PIN from the secret in its environment, holds no inline osslsigncode, engine, password argument or PIN reference, and Verify signing coverage still runs after it
```

```text
A release run signs through the script, a crafted version cannot run commands on the signing machine, and the coverage check still judges the signed files
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the release job\'s Linux sign step runs the sign script with the version and the PIN only in its environment

```text
A run that passes the version through the environment variable but then evaluates it as code (eval, or a sign script that runs the version as a command)
```

###### Obligations

None.

##### Covers

P2

```text
Read the jobs of the codesign runner image workflow
```

```text
The verify job runs the sign-step oracle, and the publish job runs it on the pushed commit-tag image before the step that moves latest
```

```text
An image that cannot run the checkout's sign script never becomes the image the machine pulls
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image workflow checks the sign step in the image before it is published and before latest moves

```text
A publish step that runs the oracle without IMAGE set, checking a fresh local build instead of the pushed image
```

###### Obligations


##### Covers

P2

```text
Run the existing action-pin guard over every workflow
```

```text
Every uses: is pinned to a full commit with its release named
```

```text
Renovate can keep the actions of the signing workflows current
```

##### Disposition

Tested

\.github\/scripts\/action\-pins\.test\.mjs

```text
A pinned commit that is not the named release; the guard checks the shape, not the registry
```

###### Obligations


##### Covers

P2

```text
Run the existing dependency-bot guard over the workflows and Renovate config
```

```text
No workflow if: decides on the actor and every Renovate hold carries a reason
```

```text
A Renovate pull request runs the same checks as a person
```

##### Disposition

Tested

\.github\/scripts\/dependency\-bot\.test\.mjs

```text
An actor check written as an environment variable instead of if:
```

###### Obligations


##### Covers

P2

```text
Run a release with the real YubiKey plugged into the signing machine
```

```text
The NSIS installer, the MSI and the server exe are signed through the pkcs11 provider and Verify signing coverage passes
```

```text
Customers download Windows binaries signed with the Lighthouse certificate
```

##### Disposition

NotTested

```text
Needs the physical YubiKey, its PIN and the signing machine; the design leaves this to the maintainer at EXAMINE
```

maintainer

##### Covers

P2

```text
Same signing run with the key plugged in
```

```text
A reader was listed before the first signing call
```

```text
No PIN reaches a token before the key is seen
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:no PIN is sent before a reader is listed

```text
A script that lists readers once and ignores the answer, then signs anyway; the separate no-reader run catches that
```

###### Obligations


##### Covers

P2

```text
Run the sign step with the key and a reader present while a pcscd from an earlier killed run is still running
```

```text
pkill -x pcscd runs before the new pcscd start, and the step goes on to sign all three files
```

```text
A release after a killed run signs without manual cleanup on the machine
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:a pcscd left from an earlier run is stopped before the step starts its own

```text
A script that always kills pcscd twice; extra stops are not refused
```

###### Obligations


##### Covers

P2

```text
Read the image's sign-step OpenSSL config and the environment the image sets
```

```text
The config file /etc/ssl/openssl-pkcs11.cnf exists and declares pkcs11-module-quirks with no-deinit, and the image environment does not set OPENSSL_CONF
```

```text
Signing does not crash at exit, and other jobs on the runner keep OpenSSL's stock config
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-sign\-step\.sh\:\:the image ships the sign step\'s own OpenSSL config and leaves the stock one to other jobs

```text
A config that sets the quirk but does not include the system config; only the real-signature check shows the default provider still loads
```

###### Obligations


### S3

##### Covers

P5

```text
The maintainer runs the host check on the real signing machine after the switch-over steps.
```

```text
host is clean and exit 0; the YubiKey is free for the container runner.
```

```text
The maintainer sees the real host retired.
```

##### Disposition

NotTested

```text
Real host state (systemd, gpg, pcscd, the real key) is never driven by an oracle; the design reserves this for the maintainer.
```

maintainer at EXAMINE

##### Covers

P6

```text
The maintainer sets up a fresh Linux signing machine from tools/codesign/README.md.
```

```text
The README covers the container runner setup with the one-time register run and watchtower, the retire steps, why disable-ccid and no host pcscd, rollback by pinning CODESIGN_RUNNER_TAG, what to check when the sign job waits for a runner, re-register after a revoked registration, and recognising a locked PIN; Windows sections stay.
```

```text
The maintainer can set up and recover the signing machine from the README alone.
```

##### Disposition

NotTested

```text
Prose: the design makes the README review and EXAMINE only; no test pins its wording.
```

whole\-candidate review and EXAMINE

##### Covers

P5

```text
The maintainer looks at the code signing tools after the switch to the container runner.
```

```text
The old Linux runner start script is gone and nothing in the tools or workflows still points at it.
```

```text
The maintainer cannot start the retired host runner by following an old step.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: the old Linux runner script is gone\; nothing in tools or the workflows still points at the old Linux runner script

```text
the script is renamed or copied to another folder outside tools and the workflows
```

###### Obligations

None.

##### Covers

P5

```text
The maintainer runs the host check on a machine that still has the whole old setup: the host runner service, the osslsigncode wrapper, the local osslsigncode image, host pcscd on, and scdaemon without disable-ccid.
```

```text
Exit 1 and a finding for each of the five leftovers, each saying what to do; no claim that the host is clean.
```

```text
The maintainer gets one list of everything still to retire, with the step for each.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: a host with the whole old setup gets a finding for each of the five leftovers\, each with what to do\, and exit 1

```text
one line names two leftovers and only one of them carries a usable step; caught only by the one-leftover cases
```

###### Obligations

None.

##### Covers

P5

```text
The maintainer runs the host check on a machine where exactly one leftover remains, once for each leftover, including a host runner unit that is installed but disabled and inactive, pcscd only enabled or only active, and scdaemon.conf missing or with disable-ccid commented out.
```

```text
Exit 1, one finding naming that leftover with what to do, and no finding for anything that is not there.
```

```text
The maintainer sees exactly what is left, also a host runner that is installed but not running, and is not sent after leftovers that are already gone.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: a host whose only leftover is \.\.\. gets one finding for it\, with what to do\, and exit 1 \(nine cases\)

```text
the step it prints names the right action but a wrong command or path, for example docker image rm of the wrong tag
```

###### Obligations

None.

##### Covers

P5

```text
The maintainer runs the host check on a retired machine where the container runner and watchtower run and the container's runner process is visible on the host.
```

```text
Exit 0 and the line host is clean, after the check asked systemd and Docker; no finding.
```

```text
The maintainer knows the retirement is done and the new setup is not mistaken for a leftover.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: a retired host\, with the container runner running\, says host is clean after asking systemd and Docker\, and exits 0

```text
the check asks systemd and Docker about something unrelated and never looks for the leftovers; caught by the old-setup and one-leftover cases
```

###### Obligations

None.

##### Covers

P5

```text
The maintainer runs the host check while Docker cannot be reached and the old local osslsigncode image is still there.
```

```text
Exit 1, a line saying Docker could not be reached, and no claim that the host is clean.
```

```text
The maintainer is not told the host is clean when the check could not look at the images.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: when Docker cannot be asked\, the check says so\, does not call the host clean\, and exits 1

```text
the line says Docker could not be reached but gives no step to make it reachable
```

###### Obligations

None.

##### Covers

P5

```text
The maintainer runs the host check on a machine with the whole old setup.
```

```text
The check only asks: no systemctl stop, disable or kill, no docker image removal, no gpgconf kill, no sudo, and the home directory is unchanged.
```

```text
The maintainer can run the check at any time, also during a release, without it stopping or deleting anything.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: the check changes nothing on the host it reports on

```text
the check changes the host through a tool with no stand-in, for example a call by absolute path such as /usr/bin/systemctl or writing outside the home directory
```

###### Obligations

None.

##### Covers

P5

```text
A change to the host check is pushed.
```

```text
The verify job of the codesign runner image workflow runs the host check oracle.
```

```text
A broken host check cannot reach main unseen through the required codesign check.
```

##### Disposition

Tested

tools\/codesign\/tests\/test\-host\-check\.sh \:\: the image workflow runs the host check oracle in verify

```text
the step is present but skipped by a step-level condition
```

###### Obligations

None.

##### Covers

P5

```text
The image workflow gains the step that runs the host check oracle.
```

```text
Every action the workflows use stays pinned to a full commit with its release named.
```

```text
Renovate keeps the codesign workflow current and no unpinned action runs.
```

##### Disposition

Tested

node \-\-test \.github\/scripts\/action\-pins\.test\.mjs

```text
the new step runs a script that downloads an unpinned tool at run time
```

###### Obligations


##### Covers

P5

```text
The image workflow gains the step that runs the host check oracle.
```

```text
No workflow condition decides on the actor, so a Renovate PR runs the same verify check as a person.
```

```text
A dependency update cannot skip the host check oracle.
```

##### Disposition

Tested

node \-\-test \.github\/scripts\/dependency\-bot\.test\.mjs

```text
the new step is skipped by a condition on the event rather than the actor
```

###### Obligations


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
