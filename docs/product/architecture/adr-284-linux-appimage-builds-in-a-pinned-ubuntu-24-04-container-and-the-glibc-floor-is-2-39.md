# ADR-284: The Linux AppImage builds in a pinned Ubuntu 24.04 container and its glibc floor is 2.39

**Feature**: `docs/feature/story-6601-linux-standalone-glibc-floor/`

Status: Accepted

## Context

```text
The Linux standalone AppImage was built on a GitHub-hosted Ubuntu runner image, with WebKitGTK pinned to the package version of that image. When ubuntu-latest moved to Ubuntu 26.04 the pinned versions no longer existed and the release stopped. The quick fix named runs-on: ubuntu-24.04, which breaks again when GitHub retires that image. There is a second, silent risk: an AppImage carries the libraries of the system that built it, so a build on a newer image needs a newer glibc and no longer starts on Ubuntu 24.04 LTS (glibc 2.39) or on older distributions. Which Linux we support was decided by whichever runner image GitHub happened to offer.
```

## Decision

```text
The minimum Linux for the AppImage is Ubuntu 24.04 LTS, which ships glibc 2.39. That means glibc 2.39 or newer, plus the WebKitGTK 4.1 and GTK 3 libraries of the distribution, which the AppImage does not bundle. Ubuntu 24.04 LTS and Debian 13 are the tested systems; newer ones are expected to work and are not tested. The floor is one value, written in tools/linux-appimage/glibc-floor, and it changes only by a new ADR that supersedes this one.

The AppImage is built in a job container whose image is ubuntu:24.04 pinned by digest, so the glibc, the WebKitGTK packages and the compiler come from an image we chose and not from the runner. The job runs on ubuntu-latest, a label that is an alias GitHub moves forward and does not retire; the runner host only has to start Docker containers. Inside the container the job installs the Tauri build packages (including git, curl, file and binutils) as root, drops the unused .NET SDK setup step (the job never runs dotnet and the bare image has no ICU library for it), drops the exact version pins on the six WebKitGTK and JavaScriptCore packages so the build takes the current Ubuntu 24.04 versions (release, updates and security), prints the installed WebKitGTK version into the job log, and sets APPIMAGE_EXTRACT_AND_RUN=1 because a container has no FUSE.

After the build, a floor check reads the GLIBC version every ELF file in the extracted AppImage requires and fails the job, naming the file and the version, when any of them is newer than the floor. A check that finds no ELF file also fails, and so does one that finds a file linked to libc but reads no GLIBC version from it, so it cannot pass by looking at nothing. A second job loads the finished AppImage in ubuntu:24.04 and debian:13 containers (both pinned by digest) and fails when the dynamic loader cannot resolve a bundled binary there. That shows the libraries link on those systems; it does not start the window.

The glibc floor is fixed by the pinned container base (Ubuntu 24.04, glibc 2.39), never by the runner: the runner label is ubuntu-latest and may change freely. The Renovate rule that froze the runner image for this workflow is removed, since the runner is no longer a decision. A new, separate rule lets Renovate keep the digest of the container image current but not move its Ubuntu release, because a new release raises the floor. The step that rejects an AppImage bundling libssl stays unchanged.

The macOS and Windows packaging jobs still name macos-latest and windows-latest. They have no glibc exposure, and moving them is out of scope here.
```

## Alternatives

```text
Keep runs-on: ubuntu-24.04: rejected, it only postpones the failure to the day GitHub retires that image, and the floor stays implicit. Build in a prebuilt builder image published to GHCR, like the signing runner image: rejected for now, it adds an image pipeline, a publish step and a registry dependency, while installing the same packages in a job container costs a few minutes per release and no extra moving part; it can replace the install step later without changing the floor. Keep the exact =2.44.0-2 pins: rejected, they were added without a recorded reason and no WebKit workaround exists in the Tauri code, and they keep the bundled WebKitGTK from receiving security fixes. Run the checks of this change in a pull request workflow: rejected, these files change rarely and the main build runs the floor check for real. A scheduled canary build on the newest runner image: rejected, the container removes the dependency on the runner image that the canary would watch, and a vanished ubuntu-latest label already fails every job loudly at job start. Raising the floor to Ubuntu 26.04: out of scope; it would drop Ubuntu 24.04 users.
```

## Consequences

```text
Retiring or moving a GitHub runner image cannot change the glibc of the AppImage or block the Linux release. Supporting an older or newer Linux is a deliberate change of one digest, the floor file and a new ADR. Renovate pull requests that move the Ubuntu release of the container are disabled, so the floor cannot rise by a dependency update. Packages are installed from the Ubuntu 24.04 archive on every run, so the archive can still change which package versions exist; the printed WebKitGTK version and the floor check are what notice that. The first real Tauri build in the bare container is seen only on main; if it fails, the release waits and the fix is to correct the package list or revert the change. Container images come from Docker Hub, whose rate limit can fail a job; the job then fails loudly and a re-run fixes it. A failed loader job is expected to block the release, because the release job needs the Linux packaging call and runs only when none of its needs failed; no failing loader job has been observed yet. The packaging job runs only on main, so a pull request cannot exercise it: the checks in tools/linux-appimage/tests run on a developer machine with Docker, and the container build itself is first seen on the main pipeline. WebKitGTK now moves with the Ubuntu 24.04 archive and keeps receiving security fixes, and its glibc need stays within the floor because that release keeps glibc 2.39. The user-visible risk is that a newer WebKitGTK can change rendering, for example the DMABUF renderer showing a blank window on some GPUs. The floor check cannot see that, so the maintainer starts the AppImage of the first build once on a desktop before the release. The floor check covers glibc symbol versions only; it does not cover libstdc++ or other libraries the host must provide.
```
