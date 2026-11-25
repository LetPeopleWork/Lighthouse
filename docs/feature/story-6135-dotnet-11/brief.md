# Product brief: story\-6135\-dotnet\-11

## Request

```text
ADO Story 6135: Upgrade Lighthouse from .NET 10 to .NET 11 in one change, now that .NET 11.0.0 is GA (2026-11-10, SDK 11.0.100) and Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 is released. Retarget all four projects, SDK pins, Docker images and the .NET-versioned packages together, and remove the Renovate .NET 10 hold.
```

## Work

M

```text
Lighthouse runs on .NET 11 with unchanged behaviour, so self-hosters get the current runtime and Renovate can keep the .NET stack current again.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A self-hoster runs Lighthouse on .NET 11 and sees no change in behaviour: the same routes, responses, screens and data. Settings, System Info shows the .NET 11 runtime version.
```

### P2

Outcome

```text
The maintainer can build, test and package Lighthouse with the .NET 11 SDK in every pipeline, and Renovate proposes .NET 11 updates again.
```

### P3

Outcome

```text
The self-hosted Docker image and the Linux, macOS and Windows standalone builds run on .NET 11 and serve the app.
```

### P4

Scenario

```text
Backend runs on .NET 11 with the full suite green
```

```text
all four projects target net11.0 with .NET 11 package versions
```

```text
the full backend suite runs, Integration included, and the app starts from the dev server script
```

```text
all tests pass, no migration reports a pending model change, and Settings, System Info shows a .NET 11 runtime; a net10.0 reference or a failing test falsifies this
```

### P5

Scenario

```text
Build pipelines verify the app on .NET 11
```

```text
the SDK pins and toolchain pin checks name .NET 11
```

```text
the SQLite and Postgres verify builds, E2E and Sonar run
```

```text
all are green with no new Sonar issue; a pipeline that still installs SDK 10 falsifies this
```

### P6

Scenario

```text
Docker image and standalone builds serve the app
```

```text
the Dockerfile uses the 11.0 images and the standalone workflows use the 11 SDK
```

```text
the image is built and started, and a standalone package is built and started
```

```text
each serves the app and reports a .NET 11 runtime; a build or start failure falsifies this
```

### P7

Scenario

```text
Renovate no longer holds .NET
```

```text
the .NET 10 hold rule is removed from renovate.json
```

```text
Renovate reads its configuration
```

```text
no rule holds back .NET packages or images, and the remaining docs and pins that name .NET 10 as current now name .NET 11; historical briefs and decision records stay unchanged
```

## Scope

```text
Retarget the four projects to net11.0; move the .NET-versioned packages to 11.x; change SDK pins in the build action and the standalone packaging and SBOM workflows; update the Dockerfile images, Start-DevServer.ps1 path, the Sonar gates workflow, toolchain pin test, solution file, perf readings files, examine script, regenerated SBOM, ARCHITECTURE.md, CLAUDE.md and the two user docs; remove the Renovate .NET 10 hold; check migrations via Create-Migration.ps1, Stryker.NET config and standalone packaging.
```

```text
Adopting new .NET 11 APIs, for example the first-class cookie Partitioned property that Program.cs works around today. Possible follow-up only.
```

```text
Any change to routes, DTO shapes, serialized responses, UI pixels or topologies; this is a behaviour-free upgrade.
```

```text
Historical architecture briefs, diagrams and decision records that name .NET 10 as history stay unchanged. Release notes are written at release time, not here.
```

## Decisions

```text
DISCOVER and DIVERGE are skipped for this Feature.
```

#### Status

Confirmed


```text
The work item gets the Release Notes tag with one short line at release time: Lighthouse now runs on .NET 11.
```

#### Status

Confirmed


```text
The Quint scenario exploration is not relevant for any of the three values: each has a settled happy path with no refusal, race or stale-input case.
```

#### Status

Confirmed


```text
The retarget and the package majors form one slice, because splitting them leaves the build broken. The other slices only check or clean up what follows.
```

#### Status

Confirmed


```text
No UI sketch is needed: no screen changes. Settings, System Info reads the runtime version at run time and shows .NET 11 with no code change; test fixtures that say .NET 10.0.0 there are values, not pins.
```

#### Status

Confirmed


```text
Platform and hosting only: no bounded context model, glossary term, domain event or UI surface is touched; RBAC, Lighthouse-Clients and the website need no change. The usage-data event question belongs to DEVOPS.
```

#### Status

Confirmed


## Journey

```text
The maintainer retargets the projects and packages, then runs the backend suite and dev server
```

```text
Not supplied
```

```text
The maintainer pushes and the verify builds, E2E and Sonar run on SDK 11
```

```text
Not supplied
```

```text
The self-hoster pulls the new Docker image or standalone package and the app runs as before
```

```text
Not supplied
```

```text
Renovate proposes .NET 11 updates again
```

```text
Not supplied
```

## Slices

### S1

```text
A maintainer runs the full backend suite and the dev server on net11.0: every test passes, no migration reports a pending model change, and Settings, System Info shows a .NET 11 runtime.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

M

```text
Backend, tests and migrations run on .NET 11 with unchanged behaviour
```

##### Rework

None.

### S2

```text
A maintainer pushes the change: the SQLite and Postgres verify builds, E2E and Sonar run on the .NET 11 SDK and are green with no new Sonar issue.
```

#### Promises

P2

P5

#### Depends on

S1

#### Slice work

S

```text
Every pipeline builds and verifies on .NET 11
```

##### Rework

None.

### S3

```text
A self-hoster builds and starts the Docker image, and a standalone package for Linux, macOS or Windows: each serves the app on a .NET 11 runtime.
```

#### Promises

P3

P6

#### Depends on

S2

#### Slice work

S

```text
Self-hosted image and standalone builds run on .NET 11
```

##### Rework

None.

### S4

```text
A maintainer reads renovate.json and the docs: no rule holds back .NET, and current-state docs and pins name .NET 11 while history stays unchanged.
```

#### Promises

P2

P7

#### Depends on

S3

#### Slice work

S

```text
Renovate keeps the .NET stack current and the docs match the runtime
```

##### Rework

None.
