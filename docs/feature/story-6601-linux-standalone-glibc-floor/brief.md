# Product brief: story\-6601\-linux\-standalone\-glibc\-floor

## Request

```text
Story 6601: Linux standalone builds no longer break when GitHub retires a runner image. The AppImage is built on a GitHub-hosted Ubuntu image with WebKitGTK pinned to that image's package version; when ubuntu-latest moved to 26.04 the pins vanished and the release was blocked; the quick fix pins runs-on: ubuntu-24.04, which breaks again when GitHub retires 24.04. Building on a newer image raises the glibc the AppImage needs and stops it starting on Ubuntu 24.04 LTS and Debian 13. Goal: the Linux build no longer depends on which runner images GitHub offers; the minimum glibc and distribution is a written decision changed on purpose. Acceptance: (1) the Linux standalone builds without naming a GitHub runner image that will be retired, or there is a documented, alarmed path for when it is; (2) the minimum glibc and distribution is written down and CI fails if a build exceeds it; (3) the AppImage still starts on Ubuntu 24.04 LTS.
```

## Work

M

```text
Releases are never blocked by GitHub retiring a runner image, and Linux users on the supported distributions keep a working app
```

### Rework

None.

## Promises

### P1

Outcome

```text
The Linux standalone release builds without naming a GitHub runner image that GitHub will retire, or a retirement is announced by an alarm before it blocks a release
```

### P2

Outcome

```text
The minimum glibc and distribution for the Linux AppImage is a written decision, and CI fails a build whose AppImage needs a newer glibc than that floor
```

### P3

Outcome

```text
Every binary bundled in the AppImage resolves its libraries and glibc versions on Ubuntu 24.04 LTS and Debian 13; the maintainer starts the AppImage once on a desktop before the release
```

## Scope

None.

```text
Raising or lowering the supported Linux floor itself: today's floor (the glibc of Ubuntu 24.04) is written down as it is, not changed
```

```text
Moving the macOS and Windows packaging off macos-latest and windows-latest; DESIGN only notes their exposure
```

## Decisions

```text
The maintainer (2026-11-15) narrowed 'the AppImage still starts on Ubuntu 24.04 LTS' to library and glibc resolution of every bundled binary on Ubuntu 24.04 and Debian 13, checked in CI; a real window start is the maintainer's one manual desktop start before the release, because a headless start probe of a WebKit app with a backend sidecar is slow and flaky in CI
```

#### Status

Confirmed


```text
The maintainer (2026-11-15) recorded the alarm branch of the first promise as not applicable: the promise reads 'builds without naming a retired runner image, or a retirement is announced by an alarm', and the first branch is met, because the job runs on ubuntu-latest (an alias GitHub moves forward and does not retire) inside a digest-pinned Ubuntu 24.04 container; if that label ever disappeared, every job would fail loudly at job start rather than break silently
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A Linux packaging run fails, naming the binary and the GLIBC version, when the AppImage needs a newer glibc than the written floor, and passes on today's build
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.

### S2

```text
The Linux AppImage is built on a build environment whose glibc we choose, on a runner GitHub will not retire, still passes the floor check, and every bundled binary resolves its libraries on Ubuntu 24.04 LTS and Debian 13
```

#### Promises

P1

P3

#### Depends on

S1

#### Slice work

None.
