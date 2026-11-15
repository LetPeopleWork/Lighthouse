# Acceptance brief: story\-6601\-linux\-standalone\-glibc\-floor

## Slices

### S1

##### Covers

P2

```text
The packaging check runs over today's Ubuntu 24.04 libraries (bash, WebKitGTK, JavaScriptCore, WebKit web process)
```

```text
It passes and reports the highest version it parsed, 2.38, not only that nothing was over the floor
```

```text
The release keeps uploading an AppImage that starts on Ubuntu 24.04 LTS
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A check that scans nothing and prints a clean summary without a parsed highest version
```

###### Obligations


##### Covers

P2

```text
The check runs over a binary needing glibc 2.9, 2.39, 2.40 and 2.100 against the written floor, and over a 2.42 binary against floors written as 2.9 and 2.100, and over today's libraries against floors 2.37 and 2.38
```

```text
2.9 and 2.39 pass, 2.40 and 2.100 fail; a 2.42 binary fails under floor 2.9 and passes under 2.100; today's libraries fail under 2.37 naming 2.38 and pass under 2.38
```

```text
A version one step above the minimum stops the release and a version below it never does, however many figures it has
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
Versions compared as text, so 2.100 looks older than 2.39 and 2.9 looks newer than 2.39
```

###### Obligations





##### Covers

P2

```text
The check runs over a directory holding a binary built in Ubuntu 26.04 that needs glibc 2.42
```

```text
It fails, naming the binary's path, GLIBC_2.42, the floor 2.39 and the floor file where the minimum is written
```

```text
The maintainer sees which binary raised the glibc requirement and where to change the minimum on purpose
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A failure that exits non-zero without naming the file, the version or the floor
```

###### Obligations


##### Covers

P2

```text
The check runs over a layout with the offender in a nested directory next to passing files, and a symbolic link pointing at the offender
```

```text
It fails once, naming the real path of the offender, and names neither the passing files nor the link
```

```text
One problem looks like one problem, found at any depth
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A check that only scans the top level, blames passing files, or reports the same library once under its real path and once under the link
```

###### Obligations




##### Covers

P2

```text
The check runs over an empty directory, a directory with only a shell script, a missing directory, and a runner without readelf
```

```text
Each ends as indeterminate with its own non-zero outcome, never as a pass; the missing tool is named
```

```text
A broken extraction or missing tool stops the release visibly instead of looking like a clean AppImage
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A check that exits 0 with nothing scanned
```

###### Obligations


##### Covers

P2

```text
The check runs over a file linked to libc whose version-needs section was removed
```

```text
It ends as indeterminate, naming the file and that reading the versions gave no GLIBC versions
```

```text
A changed tool output cannot turn into a silent pass
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A check that treats a file with no parsed versions as needing nothing
```

###### Obligations


##### Covers

P2

```text
The repository is read for the written minimum and the packaging workflow
```

```text
The floor file holds the single value 2.39, the Linux install page and the decision record state the same minimum, and the workflow installs binutils and runs the check after the libssl rejection and before the upload
```

```text
The minimum Linux is written down in one place and a build over it cannot be uploaded
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
A floor check present in the repository but never run by the packaging job, or run after the upload
```

###### Obligations

None.

##### Covers

P2

```text
The packaging run scans a library whose file was cut short so that it starts like a program but cannot be read
```

```text
The run fails as indeterminate and names the cut-off file, instead of reporting that nothing needed a newer glibc
```

```text
The maintainer sees which file could not be read and never gets a false pass
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
the file is skipped silently and the run still exits successfully with highest none
```

###### Obligations


##### Covers

P2

```text
The packaging run scans a folder as an ordinary user who cannot open one of the files in it
```

```text
The run fails as indeterminate and names the file it could not open
```

```text
The maintainer sees which file was not checked and never gets a false pass
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
the unreadable file is skipped and the run counts only the readable files and exits successfully
```

###### Obligations


##### Covers

P2

```text
The packaging run is given a floor that is not a dotted number, such as abc or a number ending in a dot
```

```text
The run fails as indeterminate with what is wrong, why and how to fix it, naming the refused floor
```

```text
A mistyped floor stops the build with a clear message and never lets every file pass
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-glibc\-floor\.sh

```text
the bad floor is treated as a number, or as zero, and the run exits successfully or refuses real files against a wrong floor
```

###### Obligations


### S2

##### Covers

P1

```text
The maintainer reads the Linux packaging workflow, its runner label, its build container and its steps
```

```text
The job runs on ubuntu-latest, no job names a versioned Ubuntu runner, the build container is an Ubuntu image by digest whose release matches the written floor (a tag only, or a digest of another release, is refused), there is no .NET setup, no exact WebKitGTK pin, the install step prints the installed WebKitGTK version after the install, the bundler runs without FUSE, packages install as root first, and the libssl rejection and the glibc floor check both run after the build and before the upload
```

```text
A GitHub runner image retirement cannot change the glibc of the Linux AppImage or block the release, and an AppImage over the floor is never uploaded
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-build\-environment\.sh

```text
a container image given by tag only that happens to say 24.04, a runner label ubuntu-latest with the old sudo install that fails in a bare container, or a workflow with the floor step deleted
```

###### Obligations





##### Covers

P1

```text
The maintainer reads the Renovate configuration for the Linux packaging workflow
```

```text
No rule disables every Ubuntu update of that workflow, and one rule disables major and minor Ubuntu updates while digest updates stay on
```

```text
Renovate keeps the container digest current and can never raise the Ubuntu release and with it the glibc floor
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-build\-environment\.sh

```text
a rule that disables all Ubuntu updates of any file, which would also freeze the digest
```

###### Obligations


##### Covers

P1

```text
The package list of the workflow is installed in a fresh container of the digest the workflow names, and the floor check runs over the installed WebKitGTK libraries
```

```text
The install succeeds without version pins, prints the installed WebKitGTK version, and the installed libwebkit2gtk and libjavascriptcoregtk need no glibc newer than the floor
```

```text
The release build can install its tools from the current Ubuntu 24.04 archive and still stay within the floor
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-build\-environment\.sh

```text
an install step that only echoes a version string without the package being installed, or a list installed in a different image than the digest named
```

###### Obligations


##### Covers

P3

```text
The loader job of the workflow is read, and the loader check runs on stand-in AppImages and, when APPIMAGE is set, on the finished AppImage for Ubuntu 24.04 and Debian 13
```

```text
The loader check script exists; the loader job waits for the packaging job and runs both digests; a missing AppImage or one without ELF files exits 2; a missing library or a glibc version the system lacks exits 1 and names it; a fully resolving AppImage exits 0, and the real AppImage resolves on both distributions
```

```text
The AppImage still resolves all its libraries on Ubuntu 24.04 LTS and Debian 13 before it is released
```

##### Disposition

Tested

tools\/linux\-appimage\/tests\/test\-build\-environment\.sh

```text
a loader check that exits 0 without reading the AppImage, or one that refuses everything; rendering and window start are not covered
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
The Linux standalone release build runs on a CI runner image that is not retired, so a release is never blocked by a retired image.
```

#### E2

##### Promises

P1

```text
Before a runner image is retired, a visible alarm reports it, early enough that a release is not blocked by the retirement unannounced.
```

#### E3

##### Promises

P2

```text
A written decision states the minimum glibc version and Linux distribution that the AppImage requires.
```

#### E4

##### Promises

P2

```text
A build whose AppImage needs a newer glibc than the written floor fails in CI, so such an AppImage is never released.
```

#### E6

##### Promises

P3

```text
Every binary bundled in the AppImage resolves its libraries and glibc versions on Ubuntu 24.04 LTS and on Debian 13, and the maintainer has started the AppImage once on a desktop before the release.
```

## Charter

None.

## Examination

None.
