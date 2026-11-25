# Architecture brief: story\-6135\-dotnet\-11

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm: object_oriented, unchanged. No domain, application or adapter code changes. The upgrade moves the target framework, the package majors, the SDK pins and the container images from 10 to 11 in one change.
```

#### D2

```text
Push point: commit S1, S2, S3 and S4 locally in order and push once after S4. S1 alone is red in CI because the pipelines install SDK 10.0.404, which cannot build net11.0. After S2 the Docker image build, which no required check runs, still uses sdk:10.0. After S3 only the Renovate hold and the prose are left, so one push after S4 gives one CI run and keeps main green. No slice is merged, split or reordered; the order follows the product dependencies.
```

#### D3

```text
Version targets: TargetFramework net11.0. Every .NET-versioned package moves from 10.0.15 to 11.0.0, and Npgsql.EntityFrameworkCore.PostgreSQL from 10.0.3 to 11.0.0. The dotnet-ef tool in Lighthouse.Backend/.config/dotnet-tools.json moves from 10.0.15 to 11.0.0, because Create-Migration.ps1 uses it with rollForward false. SDK pin 10.0.404 becomes 11.0.100. Docker tags aspnet:10.0 and sdk:10.0 become 11.0. Packages on their own release trains stay: coverlet.collector, Microsoft.Extensions.TimeProvider.Testing, Serilog.AspNetCore, Serilog.Settings.Configuration, SonarAnalyzer.CSharp, NUnit and adapters, Moq, Microsoft.NET.Test.Sdk, SQLitePCLRaw.
```

#### D4

```text
Preservation oracle: the existing backend suite, unmodified, is the behaviour-preservation oracle. NoPendingModelChangesTest is the oracle for "no migration reports a pending model change". If it fails under EF 11, the crafter stops and returns the finding to the architect: a snapshot-only migration is a new decision, not part of this upgrade. Fixtures that say .NET 10.0.0 in SystemInfoControllerTest and in the examine script are values, not pins.
```

#### D5

```text
Formal verification: no domain state machine, protocol, concurrency or temporal law is added or changed, so no Quint, TLA+ or Agda model is warranted and no checker is required. Liveness is not applicable: no progress property exists. Safety, inhabitation and reachability are covered by the real build, the existing test suite and the running app, not by a model. Type strength is unchanged. Property-based testing is not applicable, because no input domain or law changes. Jev is unauthorized in this repository: typesafe: unavailable by configuration.
```

#### D6

```text
No ADR: the change makes no durable decision beyond moving to the current runtime. The existing Renovate rule that keeps .NET major image updates manual stays when the hold is removed. Stryker.NET (dotnet-stryker 5.1.0) names no target framework in its per-feature config files, so no config change is needed; whether it runs on a net11.0 test project is checked at the next backend mutation run.
```

#### D7

```text
No global.json: CI keeps pinning the SDK through setup-dotnet, which Renovate already updates, so a global.json would be a second authority for the same pin that can drift from it. SDK 11 builds net10.0 projects, so other worktrees still on net10.0 keep working with the newest installed SDK as default. Revisit if a local SDK mismatch causes a real incident.
```

### Constraints

#### K1

```text
Input bounds: none are stored or validated by this change. Behaviour-free rule: routes, DTO shapes, serialized responses, UI pixels and topologies stay unchanged. Historical briefs, diagrams and decision records that name .NET 10 stay unchanged. Lighthouse.sln line 5 MinimumVisualStudioVersion is a Visual Studio version and stays. Existing migration Designer files keep their ProductVersion annotations. Adopting new .NET 11 APIs, for example the cookie Partitioned property, is out of scope.
```

#### K2

```text
Local toolchain: SDK 11.0.100 and runtime 11.0.0 are installed next to 10.0.114, so S1 can be built and run locally. Docker is installed and the 11.0 images exist, so S3 can be checked locally. No new analyzer warning may be silenced project-wide: TreatWarningsAsErrors is on in Directory.Build.props, so a warning that .NET 11 adds is fixed in code or suppressed narrowly at the call site.
```

### Reuse

None.

### Impact

None.

### Agreement

Target framework and EF Core major agree across the four backend projects

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Lighthouse\.Migrations\.Sqlite\.csproj\:4

Producer

Migrated

```text
Builds the migrations assembly that Backend loads; must target net11.0 with EF 11.0.0
```

Target framework and EF Core major agree across the four backend projects

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Lighthouse\.Migrations\.Postgres\.csproj\:4

Producer

Migrated

```text
Builds the migrations assembly that Backend loads; must target net11.0 with EF 11.0.0 and Npgsql 11.0.0
```

Target framework and EF Core major agree across the four backend projects

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:90

Consumer

Migrated

```text
Loads both migrations DLLs by HintPath from the gitignored Migrations folder; the first build after the retarget must rebuild them
```

Target framework and EF Core major agree across the four backend projects

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:4

Consumer

Migrated

```text
Test project references Backend and the Testing packages, so it moves to net11.0 and 11.0.0 packages
```

SDK version installed by every pipeline can build net11\.0

\.github\/actions\/build\-backend\/action\.yml\:21

Producer

Migrated

```text
Shared composite action used by ci_backend.yml and package-app.yml, which serves the Linux and Windows standalone workflows
```

SDK version installed by every pipeline can build net11\.0

\.github\/workflows\/ci\_package\-macos\-standalone\.yml\:35

Producer

Migrated

```text
Installs SDK itself before it calls build-backend
```

SDK version installed by every pipeline can build net11\.0

\.github\/workflows\/ci\_sbom\.yml\:26

Producer

Migrated

```text
Restores the solution and runs CycloneDX, which needs the SDK that can read net11.0 projects
```

SDK version installed by every pipeline can build net11\.0

\.github\/workflows\/ci\_sonar\_gates\.yml\:79

Producer

Migrated

```text
Sonar scan builds the backend and needs SDK 11
```

SDK version installed by every pipeline can build net11\.0

Dockerfile\:13

Consumer

Migrated

```text
Build stage restores and builds the net11.0 projects; sdk:11.0 image
```

SDK version installed by every pipeline can build net11\.0

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:1

Consumer

UnchangedCompatible

```text
Reaches the SDK only through package-app and build-backend; no pin of its own
```

SDK version installed by every pipeline can build net11\.0

\.github\/workflows\/ci\_package\-win\-standalone\.yml\:1

Consumer

UnchangedCompatible

```text
Reaches the SDK only through package-app and build-backend; no pin of its own
```

### Not applicable

architectural_tests

```text
No dependency, boundary or owner law is touched: no application code moves and no import direction changes. The existing architecture tests in the backend suite run unmodified as part of the preservation oracle.
```

contract_tests

```text
Producer and consumer of every touched contract change in the same commit and ship in one deployable. The build, NoPendingModelChangesTest and the running app already close the TFM and migrations agreement; persisted data and the wire format are unchanged.
```

### Boundaries

```text
Public HTTP API GET /api/latest/systeminfo served by the app started through Lighthouse.Backend/Start-DevServer.ps1, the Docker image and the standalone packages
```

#### Driven ports

```text
NuGet restore, EF Core providers for SQLite and PostgreSQL, GitHub Actions setup-dotnet, mcr.microsoft.com/dotnet images
```

```text
Build and CI configuration point at the .NET 11 toolchain; application code is untouched and depends on the same ports as before
```

#### Failures

None.

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

None.

### Models

None.

### Closure

```text
Retarget to net11.0 and the .NET 11 toolchain
```

Safety

Pass

DesignedNotBuilt

```text
Excluded state: a project or pipeline still on 10 next to net11.0 projects. The shared TFM and SDK agreements list every producer and consumer, and each slice carries a command that finds a leftover 10 reference.
```

##### Evidence

None.

```text
Retarget to net11.0 and the .NET 11 toolchain
```

Inhabitation

Pass

DesignedNotBuilt

```text
Valid state exists: GA packages 11.0.0 and SDK 11.0.100 and images aspnet:11.0 and sdk:11.0 were observed to exist.
```

##### Evidence

None.

```text
Retarget to net11.0 and the .NET 11 toolchain
```

PublicReachability

Pass

DesignedNotBuilt

```text
Public path: Start-DevServer.ps1, Docker run or standalone start, then GET /api/latest/systeminfo reports a .NET 11 runtime.
```

##### Evidence

None.

```text
Retarget to net11.0 and the .NET 11 toolchain
```

Liveness

Pass

Indeterminate

```text
Not applicable: no progress or fairness property exists in a behaviour-free platform upgrade; no checker is required.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: EF Core 11 and Npgsql 11 model building is unchanged for the current model
```

```text
Migrations describe the current model for SQLite and PostgreSQL
```

```text
Different generated SQL text that keeps the same schema
```

```text
A pending model change or a changed column type or default
```

```text
Provider conventions may differ in ways the in-memory and SQLite tests cannot see; Postgres integration tests with containers are the check
```

crafter S1\; returns to architect if NoPendingModelChangesTest fails

#### Y2

Stressor

```text
Assumption: no .NET 11 analyzer or API obsolescence warning appears
```

```text
Zero-warning build with TreatWarningsAsErrors
```

```text
Narrow fixes in the call site
```

```text
Project-wide warning suppression or removal of TreatWarningsAsErrors
```

```text
New SYSLIB or ASP obsolescence warnings in production or test code
```

crafter S1

#### Y3

Stressor

```text
Assumption: dotnet-stryker 5.1.0 runs against a net11.0 test project
```

```text
Per-feature mutation testing stays possible
```

```text
A Stryker version bump or a roll-forward setting at the next mutation run
```

```text
Skipping mutation testing silently
```

```text
Unverified until the next mutation run; no mutation run belongs to this upgrade
```

host\, next backend feature with mutation testing

#### Y4

SinglePointOfFailure

```text
One push after S4 puts every change into one CI run
```

```text
Main stays green
```

```text
A fix commit before the push
```

```text
Pushing S1 alone or a state that installs SDK 10 with net11.0 projects
```

```text
Docker image and standalone builds are not required checks, so only the local S3 check and the post-push run show them
```

host

#### Y5

Bypass

```text
Second stressor set: a runner image or setup-dotnet cache still offers only SDK 10, the aspnet:11.0 image lacks zoneinfo, the Partitioned cookie comment becomes false
```

```text
Pipelines install SDK 11.0.100 explicitly; IANA time zone ids resolve in the image; comments stay true
```

```text
Comment rewording
```

```text
A comment that states a false .NET 11 fact
```

```text
The zoneinfo claim in LighthouseClockTest and the CookieBuilder claim in Program.cs are re-verified in S1 and S3 and reworded if they changed; the Docker run in S3 resolves Europe/Zurich
```

crafter S1 and S3

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj

Extend

```text
TFM net11.0 and the 11.0.0 package set (Microsoft.AspNetCore.*, EntityFrameworkCore.*, System.Configuration.ConfigurationManager, Npgsql provider); the Serilog packages and SQLitePCLRaw stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj

Extend

```text
TFM net11.0 and Mvc.Testing, SignalR.Client, EFCore.InMemory 11.0.0
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Lighthouse\.Migrations\.Sqlite\.csproj

Extend

```text
TFM net11.0 and EF Design, Relational, Cryptography.Xml, Drawing.Common 11.0.0
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Lighthouse\.Migrations\.Postgres\.csproj

Extend

```text
TFM net11.0 and EF 11.0.0, Npgsql 11.0.0, Cryptography.Xml 11.0.0
```

Lighthouse\.Backend\/\.config\/dotnet\-tools\.json

Extend

```text
dotnet-ef 10.0.15 to 11.0.0 so Create-Migration.ps1 matches the EF runtime; dotnet-stryker stays 5.1.0
```

Lighthouse\.Backend\/Start\-DevServer\.ps1

Extend

```text
bin/Debug/net10.0 becomes net11.0, the path used to find the built migrations DLLs
```

Lighthouse\.EndToEndTests\/perf\/readings\.spec\.ts

Extend

```text
build output folder name net10.0 becomes net11.0
```

Lighthouse\.EndToEndTests\/perf\/README\.md

Extend

```text
documented default build path net10.0 becomes net11.0
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/LighthouseClockTest\.cs

Extend

```text
Comment only: re-verify that .NET 11 accepts IANA ids on every platform and that the aspnet:11.0 image has zoneinfo, then reword the .NET 10 claim to say what is true
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Comment only at the Partitioned cookie workaround: say what is true for net11.0 without adopting a new API; check whether CookieBuilder now has a Partitioned property and, if so, note the follow-up in plain words
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NoPendingModelChangesTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
sh -c '! git grep -nE "net10\.0|10\.0\.15|10\.0\.3[^0-9]" -- Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj Lighthouse.Backend/Lighthouse.Migrations.Sqlite/Lighthouse.Migrations.Sqlite.csproj Lighthouse.Backend/Lighthouse.Migrations.Postgres/Lighthouse.Migrations.Postgres.csproj Lighthouse.Backend/.config/dotnet-tools.json Lighthouse.Backend/Start-DevServer.ps1 Lighthouse.EndToEndTests/perf/readings.spec.ts Lighthouse.EndToEndTests/perf/README.md'
```

#### Supports

None.

#### Public oracle

```text
Build the solution and run the full backend suite with the filter above on SDK 11.0.100, then start the app with Lighthouse.Backend/Start-DevServer.ps1 and send GET /api/latest/systeminfo
```

```text
Build has zero warnings, every test passes (including NoPendingModelChangesTest for SQLite and PostgreSQL), the response has the same shape as before and its runtime field starts with .NET 11
```

```text
A net10.0 or 10.0.15 reference left in the listed files, a failing test, HasPendingModelChanges true, or a runtime field that starts with .NET 10
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

TargetFramework

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:4

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Target framework and package majors move to 11.
```

TargetFramework

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:4

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Target framework and test package majors move to 11.
```

TargetFramework

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Lighthouse\.Migrations\.Sqlite\.csproj\:4

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Target framework and EF majors move to 11.
```

TargetFramework

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Lighthouse\.Migrations\.Postgres\.csproj\:4

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Target framework, EF and Npgsql majors move to 11.
```

dotnet\-ef

Lighthouse\.Backend\/\.config\/dotnet\-tools\.json\:5

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. dotnet-ef moves to 11.0.0.
```

Start\-DevServer\.ps1

Lighthouse\.Backend\/Start\-DevServer\.ps1\:1

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. bin/Debug/net10.0 becomes net11.0, the path used to find the built migrations DLLs
```

net10\.0

Lighthouse\.EndToEndTests\/perf\/readings\.spec\.ts\:76

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Build folder name moves to net11.0.
```

PERF\_BACKEND\_EXECUTABLE

Lighthouse\.EndToEndTests\/perf\/README\.md\:29

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Documented default build path moves to net11.0.
```

ResolutionOrder\_ConfiguredIanaIdWins

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/LighthouseClockTest\.cs\:110

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Comment only.
```

Partitioned

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:942

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Comment only.
```

##### Impact

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:1

Changed

```text
TFM net11.0 and the 11.0.0 package set (Microsoft.AspNetCore.*, EntityFrameworkCore.*, System.Configuration.ConfigurationManager, Npgsql provider); the Serilog packages and SQLitePCLRaw stay
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:1

Changed

```text
TFM net11.0 and Mvc.Testing, SignalR.Client, EFCore.InMemory 11.0.0
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Lighthouse\.Migrations\.Sqlite\.csproj\:1

Changed

```text
TFM net11.0 and EF Design, Relational, Cryptography.Xml, Drawing.Common 11.0.0
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Lighthouse\.Migrations\.Postgres\.csproj\:1

Changed

```text
TFM net11.0 and EF 11.0.0, Npgsql 11.0.0, Cryptography.Xml 11.0.0
```

build\-config

Lighthouse\.Backend\/\.config\/dotnet\-tools\.json\:1

Changed

```text
dotnet-ef 10.0.15 to 11.0.0 so Create-Migration.ps1 matches the EF runtime; dotnet-stryker stays 5.1.0
```

build\-config

Lighthouse\.Backend\/Start\-DevServer\.ps1\:1

Changed

```text
bin/Debug/net10.0 becomes net11.0, the path used to find the built migrations DLLs
```

build\-config

Lighthouse\.EndToEndTests\/perf\/readings\.spec\.ts\:1

Changed

```text
build output folder name net10.0 becomes net11.0
```

build\-config

Lighthouse\.EndToEndTests\/perf\/README\.md\:1

Changed

```text
documented default build path net10.0 becomes net11.0
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/LighthouseClockTest\.cs\:1

Changed

```text
Comment only: re-verify that .NET 11 accepts IANA ids on every platform and that the aspnet:11.0 image has zoneinfo, then reword the .NET 10 claim to say what is true
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Comment only at the Partitioned cookie workaround: say what is true for net11.0 without adopting a new API; check whether CookieBuilder now has a Partitioned property and, if so, note the follow-up in plain words
```

solution

Lighthouse\.Backend\/Lighthouse\.sln\:5

UnchangedCompatible

```text
MinimumVisualStudioVersion is a Visual Studio version, not .NET; the solution names no target framework
```

test\-fixture

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:83

UnchangedCompatible

```text
The string .NET 10.0.0 is a fixture value passed through the controller, not a pin
```

build\-props

Lighthouse\.Backend\/Directory\.Build\.props\:21

UnchangedCompatible

```text
TreatWarningsAsErrors stays; SonarAnalyzer.CSharp is on its own release train; new .NET 11 warnings are fixed in code
```

generated\-migrations

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\:1

UnchangedCompatible

```text
Existing migrations and snapshots keep their ProductVersion annotations; no new migration is generated unless the oracle shows a pending change, which returns to the architect
```

generated\-output

Lighthouse\.Backend\/Migrations\:1

Migrated

```text
Gitignored folder with the copied migrations DLLs; the first build after the retarget overwrites them
```

consumer\-public\-contract

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:46

UnchangedCompatible

```text
Response shape is unchanged; Runtime reads RuntimeInformation.FrameworkDescription at run time and shows .NET 11 without code change
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No structure move makes the retarget easier; the edits are version values and two comments.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

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

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

\.github\/actions\/build\-backend\/action\.yml

Extend

```text
setup-dotnet dotnet-version 10.0.404 becomes 11.0.100; this composite action also serves ci_backend and the Linux and Windows standalone workflows through package-app
```

\.github\/workflows\/ci\_package\-macos\-standalone\.yml

Extend

```text
dotnet-version 10.0.404 becomes 11.0.100
```

\.github\/workflows\/ci\_sbom\.yml

Extend

```text
dotnet-version 10.0.404 becomes 11.0.100; the committed backend.cdx.json stays a pipeline-generated file and is not edited by hand
```

\.github\/workflows\/ci\_sonar\_gates\.yml

Extend

```text
dotnet-version 10.0.404 becomes 11.0.100
```

\.github\/scripts\/toolchain\-pins\.test\.mjs

#### Verification

```text
node --test .github/scripts/toolchain-pins.test.mjs
sh -c '! git grep -nE "dotnet-version: .?10[.]" -- .github'
```

#### Supports

None.

#### Public oracle

```text
Run the pin checker test and search .github for an SDK 10 pin; after the push, the SQLite and Postgres verify builds, E2E and Sonar run
```

```text
The pin checker test passes, no workflow or action installs SDK 10, and the pipelines are green with no new Sonar issue
```

```text
Any setup-dotnet step with a 10.x version, or a pipeline that cannot build net11.0
```

##### Promises

P2

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

dotnet\-version

\.github\/actions\/build\-backend\/action\.yml\:21

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. SDK pin moves to 11.0.100.
```

dotnet\-version

\.github\/workflows\/ci\_package\-macos\-standalone\.yml\:35

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. SDK pin moves to 11.0.100.
```

dotnet\-version

\.github\/workflows\/ci\_sbom\.yml\:26

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. SDK pin moves to 11.0.100.
```

dotnet\-version

\.github\/workflows\/ci\_sonar\_gates\.yml\:79

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. SDK pin moves to 11.0.100.
```

##### Impact

ci\-config

\.github\/actions\/build\-backend\/action\.yml\:1

Changed

```text
setup-dotnet dotnet-version 10.0.404 becomes 11.0.100; this composite action also serves ci_backend and the Linux and Windows standalone workflows through package-app
```

ci\-config

\.github\/workflows\/ci\_package\-macos\-standalone\.yml\:1

Changed

```text
dotnet-version 10.0.404 becomes 11.0.100
```

ci\-config

\.github\/workflows\/ci\_sbom\.yml\:1

Changed

```text
dotnet-version 10.0.404 becomes 11.0.100; the committed backend.cdx.json stays a pipeline-generated file and is not edited by hand
```

ci\-config

\.github\/workflows\/ci\_sonar\_gates\.yml\:1

Changed

```text
dotnet-version 10.0.404 becomes 11.0.100
```

test\-fixture

\.github\/scripts\/toolchain\-pins\.test\.mjs\:73

Changed

```text
The embedded Dockerfile fixture names sdk:10.0; the oracle owner aligns it to sdk:11.0 so the fixture does not name .NET 10 as current. The pin checker itself pins Node and pnpm, not .NET, so no rule changes.
```

ci\-config

\.github\/workflows\/ci\_package\-linux\-standalone\.yml\:1

UnchangedCompatible

```text
No SDK pin of its own; uses package-app and build-backend
```

ci\-config

\.github\/workflows\/ci\_package\-win\-standalone\.yml\:1

UnchangedCompatible

```text
No SDK pin of its own; uses package-app and build-backend
```

generated\-output

Lighthouse\.Frontend\/public\/sbom\/backend\.cdx\.json\:1

UnchangedCompatible

```text
A 3.8 KB placeholder that every pipeline overwrites from the CycloneDX run; it names no .NET version
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Only pinned version strings change.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

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

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

Dockerfile

Extend

```text
FROM aspnet:10.0 becomes aspnet:11.0 and FROM sdk:10.0 becomes sdk:11.0
```

\.github\/workflows\/ci\_docker\.yml

#### Verification

```text
sh -c 'docker build --build-arg NODE_VERSION=$(cat .nvmrc) -t lighthouse-s6135 .'
sh -c 'docker run -d --rm --name lighthouse-s6135 -p 8443:443 -e Database__Provider=sqlite -e UsageData__CollectorBaseUrl=http://127.0.0.1:9 -e "Database__ConnectionString=Data Source=lighthouse.db" lighthouse-s6135 && timeout 60 bash -c "until curl --silent --fail --insecure https://localhost:8443/api/latest/systeminfo; do sleep 1; done"; docker stop lighthouse-s6135'
```

#### Supports

None.

#### Public oracle

```text
Build the Docker image, run it with SQLite and send GET /api/latest/systeminfo; start a standalone package built with SDK 11 the same way
```

```text
Both serve the app and the runtime field starts with .NET 11
```

```text
A build or start failure, or a runtime field that starts with .NET 10
```

##### Promises

P3

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

aspnet

Dockerfile\:5

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Image tags move to 11.0.
```

##### Impact

container\-image

Dockerfile\:5

Changed

```text
Runtime image tag 11.0; confirm /usr/share/zoneinfo is present so IANA time zone ids still resolve
```

container\-image

Dockerfile\:13

Changed

```text
Build image tag 11.0, built from the net11.0 projects
```

ci\-config

\.github\/workflows\/ci\_docker\.yml\:64

UnchangedCompatible

```text
Builds and starts the image from the Dockerfile; no .NET version in the workflow
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Only two image tags change.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

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

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S4

#### Targets

renovate\.json

Extend

```text
Remove the rule that holds .NET images and packages below 11; keep the rule that leaves .NET major image updates to a maintainer
```

\.github\/actions\/README\.md

Extend

```text
build-backend description says .NET 11
```

Scripts\/examine\/test\-platform\-answers\-compare\.sh

Extend

```text
Fixture runtime value .NET 10.0.0 becomes .NET 11.0.0 so it reads as current
```

ARCHITECTURE\.md

Extend

```text
Lines naming .NET 10 as current (process box, paradigm, test stack) name .NET 11
```

CLAUDE\.md

Extend

```text
Backend line says C# .NET 11
```

docs\/Installation\/configuration\.md

Extend

```text
Time zone sentence names .NET 11, after the IANA claim is re-verified
```

docs\/compliance\/cra\-technical\-file\.md

Extend

```text
Technology stack table says .NET 11
```

renovate\.json

#### Verification

```text
python3 -m json.tool renovate.json
sh -c '! git grep -n "<11" -- renovate.json'
```

#### Supports

None.

#### Public oracle

```text
Parse renovate.json and search it for the below-11 cap; read the listed current-state docs
```

```text
The file parses, no rule caps .NET packages or images, and the listed docs name .NET 11 while historical records are unchanged
```

```text
A rule with allowedVersions below 11 for .NET, or a listed current-state doc that still names .NET 10 as current
```

##### Promises

P2

P7

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

allowedVersions

renovate\.json\:50

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. The .NET hold rule is removed.
```

build\-backend

\.github\/actions\/README\.md\:6

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Description names .NET 11.
```

test\-platform\-answers\-compare\.sh

Scripts\/examine\/test\-platform\-answers\-compare\.sh\:1

Extend

```text
Existing owner of this statement; edited in place. Fixture runtime value .NET 10.0.0 becomes .NET 11.0.0 so it reads as current
```

Paradigm

ARCHITECTURE\.md\:52

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Statements name .NET 11.
```

CLAUDE\.md

CLAUDE\.md\:1

Extend

```text
Existing owner of this statement; edited in place. Backend line says C# .NET 11
```

IANA

docs\/Installation\/configuration\.md\:51

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Sentence names .NET 11.
```

Technology Stack

docs\/compliance\/cra\-technical\-file\.md\:148

Extend

```text
Existing owner of this fact; edited in place, nothing new is created. Table names .NET 11.
```

##### Impact

docs\-or\-config

renovate\.json\:1

Changed

```text
Remove the rule that holds .NET images and packages below 11; keep the rule that leaves .NET major image updates to a maintainer
```

docs\-or\-config

\.github\/actions\/README\.md\:1

Changed

```text
build-backend description says .NET 11
```

docs\-or\-config

Scripts\/examine\/test\-platform\-answers\-compare\.sh\:1

Changed

```text
Fixture runtime value .NET 10.0.0 becomes .NET 11.0.0 so it reads as current
```

docs\-or\-config

ARCHITECTURE\.md\:1

Changed

```text
Lines naming .NET 10 as current (process box, paradigm, test stack) name .NET 11
```

docs\-or\-config

CLAUDE\.md\:1

Changed

```text
Backend line says C# .NET 11
```

docs\-or\-config

docs\/Installation\/configuration\.md\:1

Changed

```text
Time zone sentence names .NET 11, after the IANA claim is re-verified
```

docs\-or\-config

docs\/compliance\/cra\-technical\-file\.md\:1

Changed

```text
Technology stack table says .NET 11
```

history

docs\/product\/architecture\:1

UnchangedCompatible

```text
Historical briefs, diagrams and decision records that name .NET 10 as history stay unchanged; release notes are written at release time
```

##### Agreement

Renovate configuration after the hold is gone

renovate\.json\:39

Producer

Migrated

```text
Rule that capped .NET at below 11 is removed; the manual-merge rule for major .NET images stays
```

Renovate configuration after the hold is gone

\.github\/scripts\/dependency\-bot\.mjs\:1

Consumer

UnchangedCompatible

```text
Reads PR metadata, not the removed rule; a read of the file is the check
```

##### Not applicable

prefactoring

```text
Prose and one config rule; no structure moves.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

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

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
