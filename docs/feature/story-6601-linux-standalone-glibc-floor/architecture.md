# Architecture brief: story\-6601\-linux\-standalone\-glibc\-floor

## Paradigm

procedural

## Shared records

### Decisions

#### D1

```text
Paradigm is procedural: GitHub Actions YAML, bash scripts and a pinned container image. No C# or TypeScript production change; mutation testing does not apply. There is no smell catalogue for procedural; the general classes applied are duplicated knowledge (one floor value, one reader), mixed concerns (the check reads facts, the workflow decides when it runs) and hidden coupling (the floor and the container image release must agree, so both are written next to each other and in one ADR).
```

#### D2

```text
Build environment: the packaging job keeps runs-on: ubuntu-latest, an alias GitHub moves and does not retire, and sets job container ubuntu:24.04 pinned by sha256 digest, so glibc 2.39, the WebKitGTK packages and the toolchain come from an image we chose. The runner host only starts containers. The ADR in docs/product/architecture/adr-284 is the written decision and the authority; no nwdes ADR is declared.
```

#### D3

```text
Floor: one value, glibc 2.39 (Ubuntu 24.04 LTS), in tools/linux-appimage/glibc-floor as a single line FLOOR=2.39. It is read by the floor check and is the only place the number lives besides the ADR and the docs page; changing it is a deliberate edit plus a superseding ADR. Scope out: the floor itself is not changed.
```

#### D4

```text
Floor check carrier: bash script tools/linux-appimage/check-glibc-floor.sh DIR [FLOOR]. It walks DIR for ELF files (file magic 7f454c46), reads the GLIBC_x.y versions each one needs from readelf -V (version-needs section, undefined side only, GLIBC_PRIVATE ignored), and compares dotted versions numerically (sort -V semantics, never lexical). Outcomes: exit 0 pass, exit 1 over the floor with one WHAT/WHY/HOW block per offending file naming the path and the highest GLIBC version it needs, exit 2 indeterminate (directory missing, readelf missing, or zero ELF files scanned). Summary line scanned=N over_floor=M. Binutils is installed in the build container by the workflow.
```

#### D5

```text
Container constraints: the container has no FUSE, so the Tauri build step sets APPIMAGE_EXTRACT_AND_RUN=1 for linuxdeploy; the libssl rejection step already extracts with --appimage-extract and needs no FUSE. The container runs as root, so sudo is dropped. A bare ubuntu image lacks what the GitHub runner image preinstalls: the first step installs ca-certificates, curl, git, jq, file, xz-utils, build-essential, pkg-config, libssl-dev and binutils before checkout (checkout with fetch-depth 0 needs git), and the existing WebKitGTK installs keep their pins. These packages are the crafter list to complete: the oracle installs exactly what the workflow lists in a fresh digest-pinned container.
```

#### D6

```text
Loader check carrier: tools/linux-appimage/check-loads-on.sh IMAGE APPIMAGE extracts the AppImage and, inside IMAGE, installs the distribution WebKitGTK/GTK runtime packages, runs ldd with the AppImage lib directory on LD_LIBRARY_PATH over every ELF in the AppImage, and fails on any not found or version GLIBC_ not found line. Proof: the bundled binaries resolve their libraries and glibc symbols on that distribution. Not proof: the window opens, the backend serves, the app works. A second job in the same reusable workflow runs it for ubuntu:24.04 and debian:13 (both digest-pinned) after the artifact is uploaded.
```

#### D7

```text
Runner and Renovate: the maintainer decided the runner is runs-on: ubuntu-latest and the glibc floor comes from the pinned container base, not the runner. The rule that froze the runner image (matchDepNames ubuntu, this workflow file) is removed, and a separate rule for the container image allows digest updates of the container image and disables major and minor updates of it, so a dependency pull request cannot raise the floor. Unchanged: every other rule.
```

#### D8

```text
WebKitGTK pins are dropped: the six exact =2.44.0-2 pins go, so the build takes the current Ubuntu 24.04 versions (release, updates and security) and keeps getting security fixes. The pins were added in March 2026 with no recorded reason and the Tauri code has no WebKit workaround. The floor still comes from the 24.04 base, whose archive keeps glibc 2.39. The build prints the installed libwebkit2gtk-4.1 version into the job log. User-visible risk: a newer WebKitGTK can change rendering, for example the DMABUF renderer showing a blank window on some GPUs; the floor check cannot see it, so the maintainer starts the AppImage of the first build once on a desktop before the release.
```

#### D9

```text
Rejected: keep runs-on ubuntu-24.04 (postpones the failure); prebuilt builder image on GHCR like the signing runner image (adds an image pipeline and a registry dependency for a few minutes saved per release; can replace the install step later without touching the floor); keep the exact WebKitGTK pins (no recorded reason, no WebKit workaround, no security updates); scheduled canary on the newest runner (the container removes the dependency the canary would watch, and a vanished ubuntu-latest label fails every job loudly at job start); raising the floor to 26.04 (drops Ubuntu 24.04 users, out of scope); a pull request workflow for the oracles (files change rarely, main runs the floor check).
```

#### D10

```text
Exposure noted only: macOS and Windows packaging still name macos-latest and windows-latest; they have no glibc floor and are out of scope. The verify jobs on ubuntu-latest ship nothing and have no glibc exposure.
```

#### D11

```text
Formal verification: Quint is installed (probed with which quint) but no claim here is a state machine or temporal property. Every claim is a total comparison over a finite ordered set of dotted versions or a static shape of a workflow file. They are checked by the finite bash oracles, run by the host. No Quint, TLA+ or Agda model is delivered; nothing is waived and nothing is claimed proven.
```

#### D12

```text
Test substrate: oracles are bash plus Docker in the style of tools/codesign/tests, summary line observed=N assertions_failed=M setup_failed=K, WHAT/WHY/HOW per failure, no set -e aborts. They need Docker and network. They are not in the default CI suite and no pull request workflow runs them: the files change rarely, and the main build runs the floor check for real. The maintainer runs them locally when these files change. No infrastructure derogation requested.
```

### Constraints

#### K1

```text
Inputs and bounds: floor is a dotted major.minor integer pair; no upper bound beyond integers; the AppImage is any size the Tauri bundler produces and the check reads every ELF in it with no sampling; image digests are sha256 strings. No bound on number of files.
```

#### K2

```text
The libssl rejection step, secrets handling (TAURI_SIGNING_PRIVATE_KEY), artifact names, the if: main condition, the Package and Sign environment and the ci.yml caller stay unchanged.
```

#### K3

```text
The packaging job runs only on main: the real container build and the real AppImage are observable only on the main pipeline or by a maintainer running the container steps by hand. Local oracles prove the floor check, the workflow shape, and that the package install and bundled WebKitGTK library resolve in the pinned image under the floor.
```

#### K4

```text
Proof limit: the floor check and loader check cannot see rendering. The maintainer starts the S2 AppImage once on a desktop before the release; this is a manual step, not a CI gate.
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
bash tools/linux-appimage/check-glibc-floor.sh DIR [FLOOR]; bash tools/linux-appimage/check-loads-on.sh IMAGE APPIMAGE; the reusable workflow .github/workflows/ci_package-linux-standalone.yml called from ci.yml
```

#### Driven ports

```text
readelf and file from binutils; docker; the Ubuntu apt archive; GitHub Actions job container
```

```text
workflow steps call the scripts; scripts read tools/linux-appimage/glibc-floor and never the workflow; oracles read the workflow, renovate.json and the ADR as files; nothing in the backend or frontend depends on any of it
```

#### Failures

None.

### Observations

```text
Floor check result: exit code, one line per offending ELF naming its path and the highest GLIBC_x.y it needs, and the summary scanned=N over_floor=M
```

exit 0\|1\|2 plus text lines

```text
which symbol inside a file needs the version; libstdc++ or other library versions; whether a file is actually loaded at run time
```

```text
Loader check result per image: exit code and the not found or GLIBC_ not found lines from ldd
```

exit 0\|1\|2 plus text lines

```text
GUI start, dlopen at run time, backend behaviour
```

### Equality

floor check outcome

```text
Two runs are equal when exit code and the set of (file path, highest GLIBC version) offenders are equal; line order and the order files are visited are ignored.
```

### Constructors

check\-glibc\-floor

DIR \[FLOOR\] \-\> exit 0 \| 1 \| 2

check\-loads\-on

IMAGE APPIMAGE \-\> exit 0 \| 1 \| 2

### Laws

None.

### Refuted

None.

### Claims

#### C1

```text
No Linux AppImage that needs glibc newer than 2.39 is uploaded by the release
```

```text
Ubuntu 24.04 LTS users cannot start the app after an update
```

tools\/linux\-appimage\/check\-glibc\-floor\.sh

```text
the extracted AppImage directory and the floor file
```

check\-glibc\-floor step between the libssl rejection step and the upload step

upload step\, release job

```text
job fails naming the file and GLIBC version; exit 1
```

```text
a build that skips the step by editing the workflow; an ELF the file(1) magic test misses (scripts are not ELF); libraries loaded at run time by dlopen
```

#### C2

```text
The Linux build does not depend on a GitHub runner image that GitHub retires
```

```text
release blocked when the image is retired
```

\.github\/workflows\/ci\_package\-linux\-standalone\.yml

```text
runs-on ubuntu-latest and container ubuntu:24.04 by digest
```

job container block

ci\.yml release job

```text
workflow file names no versioned ubuntu runner label
```

```text
GitHub removes the ubuntu-latest label or the Docker Hub image is withdrawn: every job then fails loudly at job start
```

### States

Admitted

```text
AppImage built in the pinned ubuntu:24.04 container: every ELF needs at most GLIBC 2.39
```

tools\/linux\-appimage\/check\-glibc\-floor\.sh

Excluded

```text
A release whose AppImage needs a glibc newer than the floor reaches the upload step
```

tools\/linux\-appimage\/check\-glibc\-floor\.sh

Excluded

```text
The floor is a value that differs between the check, the docs page and the ADR without a deliberate change
```

docs\/product\/architecture\/adr\-284

Unknown

```text
Whether the first real Tauri build in the bare container succeeds (missing packages, linuxdeploy without FUSE)
```

maintainer at the first main pipeline

Unknown

```text
libstdc++ and other non-glibc library floors of the host
```

maintainer

### Models

None.

### Closure

None.

### Risks

None.

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

tools\/linux\-appimage\/check\-glibc\-floor\.sh

Create

```text
The floor check: reads GLIBC versions from every ELF and fails above the floor.
```

tools\/linux\-appimage\/glibc\-floor

Create

```text
The single written floor value FLOOR=2.39.
```

\.github\/workflows\/ci\_package\-linux\-standalone\.yml

Extend

```text
Install binutils, extract the AppImage once, run the floor check after the libssl rejection and before the upload.
```

docs\/Installation\/standalone\.md

Extend

```text
Linux section states the minimum: Ubuntu 24.04 LTS (glibc 2.39) and newer.
```

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

#### Verification

```text
bash tools/linux-appimage/tests/test-glibc-floor.sh
```

#### Supports

None.

#### Public oracle

```text
Run the oracle: it runs check-glibc-floor.sh inside digest-pinned ubuntu:24.04 over (a) the real bash binary and the real libwebkit2gtk-4.1 library installed from the Ubuntu 24.04 archive, (b) a small C program compiled in ubuntu:26.04 against a symbol first versioned GLIBC_2.42, (c) an empty directory, (d) a copy of (a) with the floor argument lowered to 2.38.
```

```text
(a) exit 0 with scanned>0 over_floor=0; (b) exit 1 and output names the offending file path and GLIBC_2.42; (c) exit 2; (d) exit 1 naming the highest version found; the workflow runs the check after the libssl rejection and before the upload.
```

```text
A check that compares versions as strings (2.9 above 2.39), accepts a directory with no ELF, names no file, ignores a library below the top directory, or passes (b) fails the oracle.
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

pass

tools\/codesign\/tests\/test\-runner\-image\.sh\:40

Reuse

```text
The oracle repeats the same summary-line and WHAT/WHY/HOW helper shape (pass, fail, setup_failure, summary_and_exit); a convention copy, since bash has no shared library here and the tools are unrelated.
```

Reject an AppImage that bundles libssl

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:150

Extend

```text
It already extracts the AppImage to squashfs-root; the floor check follows it and reuses that extraction.
```

floor check

tools\/linux\-appimage\/check\-glibc\-floor\.sh\:1

CreateNew

```text
No file in the repo reads GLIBC versions; code search for GLIBC found only ldd/glibc text in docs.
```

FLOOR

tools\/linux\-appimage\/glibc\-floor\:1

CreateNew

```text
No file in the repo holds a glibc floor; it needs one single-line source read by the script.
```

Linux

docs\/Installation\/standalone\.md\:60

Extend

```text
The Linux section is the page that lists the platform requirement; the minimum is added there, not in a new page.
```

##### Impact

workflow

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:150

Changed

```text
Adds binutils and the floor check step.
```

docs

docs\/Installation\/standalone\.md\:60

Changed

```text
States the Linux minimum.
```

caller

\.github\/workflows\/ci\.yml\:164

UnchangedCompatible

```text
Inputs and permissions of the reusable workflow do not change.
```

script

tools\/linux\-appimage\/check\-glibc\-floor\.sh\:1

Changed

```text
New script; its only caller is the workflow step.
```

config

tools\/linux\-appimage\/glibc\-floor\:1

Changed

```text
New floor file; read by the script and checked by the oracle against the docs page and the ADR.
```

##### Agreement

glibc floor value

tools\/linux\-appimage\/glibc\-floor\:1

Producer

Migrated

```text
New single source of the number.
```

glibc floor value

tools\/linux\-appimage\/check\-glibc\-floor\.sh\:1

Consumer

Migrated

```text
Reads the number from the floor file.
```

glibc floor value

docs\/Installation\/standalone\.md\:60

Consumer

Migrated

```text
Prose states the same number; the ADR is the authority and the oracle checks the number appears in the docs page.
```

##### Not applicable

architectural_tests

```text
No dependency or ownership law between modules is touched: scripts and workflow only; the oracle already pins the one boundary that matters (workflow calls script, script reads the floor file).
```

contract_tests

```text
Producer and consumer of the floor file change in the same commit and deployable; the primary oracle covers them.
```

prefactoring

```text
No existing behaviour moves; the floor check is a new step and script.
```

##### Boundaries

```text
bash tools/linux-appimage/check-glibc-floor.sh DIR [FLOOR]; the package-linux-standalone job step that calls it
```

###### Driven ports

```text
readelf and file from binutils
```

```text
workflow step calls the script; the script reads tools/linux-appimage/glibc-floor only
```

###### Failures

####### B1

```text
directory missing, readelf absent, or no ELF file found
```

Indeterminate

```text
exit 2 with a message naming what could not be read; never exit 0
```

####### B2

```text
an ELF needs a version above the floor
```

Refusal

```text
exit 1; one block per file with WHAT (path, GLIBC version), WHY (floor and where it is written), HOW (build in the pinned container or raise the floor by ADR)
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
Dotted version comparison is numeric and total: 2.9 < 2.39, 2.39 = 2.39, 2.40 > 2.39, 2.100 > 2.39, and the floor check fails iff some file needs a version greater than the floor
```

Algebraic

Domain

tools\/linux\-appimage\/check\-glibc\-floor\.sh

###### Treatment

Finite

###### Cases

```text
2.9 vs 2.39
```

```text
2.39 vs 2.39
```

```text
2.40 vs 2.39
```

```text
2.100 vs 2.39
```

```text
Closed finite table of version pairs; a generator adds nothing.
```

###### Evidence

None.

###### L2

```text
Every ELF file under the directory is scanned, at any depth, and an offender is reported by its path
```

Invariant

Application

tools\/linux\-appimage\/check\-glibc\-floor\.sh

###### Treatment

Finite

###### Cases

```text
offender in a nested lib directory
```

```text
offender next to passing files
```

```text
directory with no ELF
```

```text
Three concrete layouts, built as real ELF files.
```

###### Evidence

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
floor check on today build
```

Safety

Pass

DesignedNotBuilt

```text
Total numeric comparison over finite version pairs, three outcomes reach the exit code; checked by the finite oracle, not a model.
```

###### Evidence

None.

```text
floor check on today build
```

Inhabitation

Pass

DesignedNotBuilt

```text
Input: Ubuntu 24.04 archive libraries -> check-glibc-floor.sh -> exit 0 on the shell; constructed in the oracle from real binaries.
```

###### Evidence

None.

```text
floor check on today build
```

PublicReachability

Pass

DesignedNotBuilt

```text
tools/linux-appimage/check-glibc-floor.sh is invoked by a workflow step in ci_package-linux-standalone.yml after extraction (CreateNew step) and by the oracle argv.
```

###### Evidence

None.

```text
floor check on today build
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: a bounded synchronous scan over a finite directory; no temporal or fairness claim.
```

###### Evidence

None.

##### Risks

###### Y1

Stressor

```text
Assumption: every library that matters is an ELF file with a GLIBC version-needs section. Negated: a library loads another through dlopen, or a file is a script.
```

```text
No release uploads an AppImage with a statically visible glibc need above the floor
```

```text
dlopen-only needs are not seen
```

```text
pass on a directory with no ELF; string comparison of versions
```

```text
dlopen and non-glibc library needs are covered only by the loader check on two distributions
```

maintainer

###### Y2

Stressor

```text
Assumption: readelf prints the needs section in the format the parser reads. Negated: binutils changes the format.
```

```text
Floor check never passes silently
```

```text
exit 2 indeterminate
```

```text
exit 0 with scanned=0
```

```text
format drift shows as exit 2 on main, not as a false pass
```

crafter

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

\.github\/workflows\/ci\_package\-linux\-standalone\.yml

Extend

```text
Job container ubuntu:24.04 by digest, runs-on ubuntu-latest, first step installs build packages as root, sudo removed, exact WebKitGTK version pins dropped and the installed version printed to the log, APPIMAGE_EXTRACT_AND_RUN on the build step, loader-check job after upload.
```

tools\/linux\-appimage\/check\-loads\-on\.sh

Create

```text
Loader check of the finished AppImage on a distribution image.
```

renovate\.json

Extend

```text
Remove the runner freeze rule and add a rule for the container image that allows digest updates and disables major and minor updates of the container image.
```

tools\/linux\-appimage\/tests\/test\-build\-environment\.sh

#### Verification

```text
bash tools/linux-appimage/tests/test-build-environment.sh
```

#### Supports

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

#### Public oracle

```text
Run the oracle: it reads the workflow, renovate.json and the ADR as files; installs the workflow package list in a fresh ubuntu:24.04 container by the digest the workflow names, with no exact version pin, and runs check-glibc-floor.sh over the libwebkit2gtk-4.1 library installed from the current archive; with APPIMAGE set it runs check-loads-on.sh for that AppImage in ubuntu:24.04 and debian:13.
```

```text
The Linux packaging job names runs-on ubuntu-latest and a container image ubuntu:24.04@sha256 digest and no versioned ubuntu runner label; no webkit or javascriptcore package in the workflow carries an exact = version pin; the workflow prints the installed WebKitGTK version; the package install succeeds in the digest container and the installed libwebkit2gtk library passes the floor; renovate.json no longer freezes a runner and keeps the container image on digest updates only; the libssl rejection and the floor check steps stay between build and upload; the ADR and the floor file exist and agree; with APPIMAGE set both distributions resolve every bundled binary.
```

```text
A workflow still naming a versioned ubuntu runner, a container by tag only, an exact = pin on a webkit or javascriptcore package, no version print, a Renovate rule that lets the Ubuntu release move or still freezes the runner, a package list that fails to install in the digest container, a libwebkit2gtk that exceeds the floor, or a loader line not found on either distribution fails the oracle. Not covered: rendering; the maintainer starts the AppImage once on a desktop before the release.
```

##### Promises

P1

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ubuntu\:26\.04\@sha256

tools\/codesign\/Dockerfile\:4

Reuse

```text
Same technique as the signing runner (base pinned by digest, Renovate keeps the digest); no image of our own is built because the build needs no pre-baked state.
```

package\-linux\-standalone

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:14

Extend

```text
One job already owns build, signing and upload; adding a container block keeps one owner.
```

ci\_package\-linux\-standalone\.yml

renovate\.json\:78

Replace

```text
The rule froze a runner label that is no longer a decision; it is removed and a separate rule keeps the container image digest current without moving its Ubuntu release.
```

check\-loads\-on

tools\/linux\-appimage\/check\-loads\-on\.sh\:1

CreateNew

```text
Nothing in the repo runs an AppImage against a distribution image.
```

##### Impact

workflow

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:15

Changed

```text
Runner label, container, package step, extra job.
```

config

renovate\.json\:77

Changed

```text
Rule replaced.
```

caller

\.github\/workflows\/ci\.yml\:240

UnchangedCompatible

```text
The release job needs the same package-linux-standalone job id; the added job belongs to the called workflow and its failure still fails the call.
```

docs

docs\/ci\-learnings\.md\:409

UnchangedCompatible

```text
No learning added; the ADR holds the rule.
```

script

tools\/linux\-appimage\/check\-loads\-on\.sh\:1

Changed

```text
New script; called by the loader-check job and the oracle.
```

##### Agreement

build container Ubuntu release

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:14

Producer

Migrated

```text
Names the ubuntu:24.04 digest.
```

build container Ubuntu release

renovate\.json\:77

Consumer

Migrated

```text
Must not move the release.
```

build container Ubuntu release

tools\/linux\-appimage\/glibc\-floor\:1

Consumer

UnchangedCompatible

```text
Floor 2.39 is the glibc of the same release.
```

##### Not applicable

architectural_tests

```text
No code module dependency law is touched; the shape of the workflow is asserted by the primary oracle.
```

contract_tests

```text
Parties change in the same commit and deployable; no persisted or wire data outlives the producer.
```

prefactoring

```text
No behaviour-preserving move precedes the change; the workflow edit is the change itself.
```

##### Boundaries

```text
the reusable workflow .github/workflows/ci_package-linux-standalone.yml called from ci.yml; bash tools/linux-appimage/check-loads-on.sh IMAGE APPIMAGE
```

###### Driven ports

```text
docker; the Ubuntu apt archive; the GitHub Actions job container
```

```text
oracle reads workflow, renovate.json and ADR as files; workflow calls scripts; nothing in backend or frontend depends on any of it
```

###### Failures

####### B3

```text
the container build fails on a missing package or without FUSE
```

Refusal

```text
the main pipeline job fails before upload; the release waits for a fix; seen only on main
```

####### B4

```text
the ubuntu-latest label or the container image becomes unavailable
```

Refusal

```text
every job fails at job start with a visible error; the ADR names this as the documented path
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
The build container image is named by a sha256 digest and its Ubuntu release equals the release the floor file documents; changing one without the other fails the oracle
```

Invariant

Infrastructure

\.github\/workflows\/ci\_package\-linux\-standalone\.yml

###### Treatment

Finite

###### Cases

```text
tag only without digest
```

```text
digest with release 26.04
```

```text
digest with release 24.04
```

```text
Finite shapes of one string; a table of cases.
```

###### Evidence

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
container build on a non-retiring runner
```

Safety

Pass

DesignedNotBuilt

```text
Container digest fixes glibc; the floor check from the first slice excludes an over-floor upload; checked by the static workflow oracle and the install-and-floor probe.
```

###### Evidence

None.

```text
container build on a non-retiring runner
```

Inhabitation

Pass

DesignedNotBuilt

```text
Workflow package list -> digest ubuntu:24.04 container -> installed WebKitGTK library -> floor check exit 0, built in the oracle; the full Tauri build is first seen on main.
```

###### Evidence

None.

```text
container build on a non-retiring runner
```

PublicReachability

Pass

DesignedNotBuilt

```text
ci.yml package-linux-standalone calls the reusable workflow; the loader-check job and the oracle argv call check-loads-on.sh (CreateNew).
```

###### Evidence

None.

```text
container build on a non-retiring runner
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no state machine; the job is a finite sequence of steps that ends or fails.
```

###### Evidence

None.

##### Risks

###### Y3

SinglePointOfFailure

```text
Assumption: the first real Tauri build in a bare container works with the listed packages and APPIMAGE_EXTRACT_AND_RUN. Negated: a package or tool the runner image preinstalled is missing.
```

```text
The previous release artifacts stay available; no AppImage over the floor is uploaded
```

```text
the Linux job fails on main until fixed
```

```text
an AppImage built outside the container is released silently
```

```text
the real build is first seen on main; the maintainer may run the container steps by hand before merging if a release is imminent
```

maintainer

###### Y4

Bypass

```text
Assumption: nobody edits the workflow back to a runner build. Negated: a person re-adds runs-on ubuntu-NN.04.
```

```text
floor and container are one written decision
```

```text
a deliberate edit with a new ADR
```

```text
an unreviewed change of the Ubuntu release
```

```text
Renovate is disabled for the release; the oracle fails on a versioned runner label; humans can still edit
```

maintainer

###### Y5

Stressor

```text
Second set: Docker Hub rate limit or outage; the image digest is withdrawn; ubuntu-latest drops Docker; apt archive removes the pinned WebKitGTK version.
```

```text
no wrong AppImage is released
```

```text
the job fails loudly at pull or install
```

```text
silent fallback to another base image or version
```

```text
walked through the design, not executed: all four stop the job before upload; recovery is a manual digest or pin change by ADR
```

maintainer

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
