# Acceptance brief: story\-6135\-dotnet\-11

## Slices

### S1

##### Covers

P4

```text
Look for the old .NET 10 target, the 10.0.15 package versions and the 10.0.3 database provider version in the four backend projects, the migrations tool pin, the dev server script and the performance readings files
```

```text
None is found: every project targets .NET 11, the .NET packages and the database provider are on 11.0.0, the migrations tool matches the database library, and the dev server and performance readings look in the .NET 11 build folder
```

```text
The maintainer builds and runs the backend, its migrations tool and its dev server on .NET 11 only
```

##### Disposition

Tested

sh \-c \'\! git grep \-nE \"net10\\\.0\|10\\\.0\\\.15\|10\\\.0\\\.3\[\^0\-9\]\" \-\- Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Lighthouse\.Migrations\.Sqlite\.csproj Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Lighthouse\.Migrations\.Postgres\.csproj Lighthouse\.Backend\/\.config\/dotnet\-tools\.json Lighthouse\.Backend\/Start\-DevServer\.ps1 Lighthouse\.EndToEndTests\/perf\/readings\.spec\.ts Lighthouse\.EndToEndTests\/perf\/README\.md\'

```text
a package moved to another 10.x patch such as 10.0.16, or to an 11 preview instead of 11.0.0; a mismatched major then shows in the build and suite rows, a stale patch does not
```

###### Obligations

None.

##### Covers

P4

```text
Look for the .NET 10 web testing package version 10.0.14 in the examine scripts
```

```text
None is found: the examine scripts load the web testing package at 11.0.0, matching the backend they host
```

```text
The examine tooling runs against the .NET 11 backend
```

##### Disposition

Tested

sh \-c \'\! git grep \-n \"10\[\.\]0\[\.\]14\" \-\- Scripts\/examine\'

```text
the pin moved to a different .NET 10 version such as 10.0.15 instead of 11.0.0; the examine script would then fail to host the .NET 11 backend only when run
```

###### Obligations

None.

##### Covers

P4

```text
Compare the current data model with the latest SQLite migration snapshot after the move to the .NET 11 database libraries
```

```text
No pending model change is reported for SQLite
```

```text
A self-hoster on SQLite starts the upgraded app without a new migration and keeps the same data
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NoPendingModelChangesTest\.cs\:\:TheSqliteMigrationsDescribeTheCurrentModel

```text
the projects still run on the .NET 10 database libraries, so the comparison never sees the .NET 11 model builder; the first row rules that out. It also accepts a difference EF only shows against a live database, such as a changed default at run time
```

###### Obligations

None.

##### Covers

P4

```text
Compare the current data model with the latest PostgreSQL migration snapshot after the move to the .NET 11 database libraries and the 11.0.0 PostgreSQL provider
```

```text
No pending model change is reported for PostgreSQL
```

```text
A self-hoster on PostgreSQL starts the upgraded app without a new migration and keeps the same data
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NoPendingModelChangesTest\.cs\:\:ThePostgresMigrationsDescribeTheCurrentModel

```text
the PostgreSQL provider stayed on 10.0.3 while EF moved to 11, so the provider's own model conventions are not exercised; the first row rules that out. It also accepts a difference only a live PostgreSQL database shows
```

###### Obligations

None.

##### Covers

P1

P4

```text
Build the backend solution and run the full backend suite, Integration included and the live tracker categories left out, on the .NET 11 SDK
```

```text
The build has zero warnings and every existing test passes unchanged
```

```text
A self-hoster sees the same routes, responses and data as before, because every behaviour the suite pins still holds on .NET 11
```

##### Disposition

Tested

dotnet build Lighthouse\.Backend\/Lighthouse\.sln \&\& dotnet test Lighthouse\.Backend\/Lighthouse\.Backend\.Tests \-\-filter \"TestCategory\!\=JiraIntegration\&TestCategory\!\=LinearIntegration\&TestCategory\!\=AdoIntegration\&TestCategory\!\=ServiceNowIntegration\&TestCategory\!\=GithubIntegration\"

```text
a behaviour change in an area no existing test covers, or a green run after an existing test was weakened, skipped or deleted to make the upgrade pass; the candidate review must see no test removed or loosened
```

###### Obligations

None.

##### Covers

P1

P4

```text
Start the app with the dev server script and open Settings, System Info
```

```text
The page and the system information answer have the same fields as before, and the runtime reads .NET 11
```

```text
A self-hoster sees in Settings, System Info that Lighthouse runs on .NET 11
```

##### Disposition

NotTested

```text
Live observation of the served app. The suite hosts the app inside the test runner, so a test would read the runner's runtime, not the dev server's, and would pin a version every later upgrade must edit; the design names this as a check on the running app and declares no test locator for it
```

host\: start Lighthouse\.Backend\/Start\-DevServer\.ps1 on the candidate and GET \/api\/latest\/systeminfo

##### Covers

P4

```text
Read the time zone comment in the clock test and the partitioned cookie comment in the start-up code
```

```text
Each states what is true on .NET 11 in plain words, and no new .NET 11 cookie API is adopted
```

```text
A maintainer reading the code is not told a .NET 10 fact that no longer holds
```

##### Disposition

NotTested

```text
Comment-only prose; a test would pin wording, which the test rules forbid
```

reviewer\: the whole\-candidate review reads both comments

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
