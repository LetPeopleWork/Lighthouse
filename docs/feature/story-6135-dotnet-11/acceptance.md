# Acceptance brief: story\-6135\-dotnet\-11

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A self-hoster who upgrades to the .NET 11 build sees the same routes, responses, screens and data as before, and Settings, System Info shows a .NET 11 runtime version.
```

#### E2

##### Promises

P2

```text
The maintainer can build, test and package Lighthouse with the .NET 11 SDK in every pipeline, and Renovate proposes .NET 11 updates again.
```

#### E3

##### Promises

P3

```text
The self-hosted Docker image and the Linux, macOS and Windows standalone builds start on .NET 11 and serve the app.
```

#### E4

##### Promises

P4

```text
The backend, all four projects on net11.0 with .NET 11 packages, passes the full suite including Integration, reports no pending model change for any migration, starts from the dev server script, and shows a .NET 11 runtime in Settings, System Info; any net10.0 reference or failing test breaks this.
```

#### E5

##### Promises

P5

```text
Pipelines verify on .NET 11: SDK pins and toolchain pin checks name .NET 11, and SQLite and Postgres verify builds, E2E and Sonar run green with no new Sonar issue; a pipeline still installing SDK 10 breaks this.
```

#### E6

##### Promises

P6

```text
The Docker image built from the 11.0 images and a standalone package built with the 11 SDK each start, serve the app and report a .NET 11 runtime; a build or start failure breaks this.
```

#### E7

##### Promises

P7

```text
Renovate reads its configuration with no rule holding back .NET packages or images, and current docs and pins naming .NET 10 as current now name .NET 11, while historical briefs and decision records stay unchanged.
```

## Charter

None.

## Examination

None.
