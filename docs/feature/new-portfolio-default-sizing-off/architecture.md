# Architecture brief: new\-portfolio\-default\-sizing\-off

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Owner: Portfolio (Models/Portfolio.cs) owns the starting value of Default Feature sizing. The property InfersDefaultFeatureSize starts false (off). A new method Portfolio.StartOnConnection(connection) assigns the connection and sets the switch on exactly when the connection names a Portfolio Size Estimate Field default (SizeEstimateFieldForPortfolios has a value), else leaves it off. It is a named method, not a property setter, so EF materialisation of a stored row never runs it. Only the create path calls it, once, before SyncWithPortfolioSettings. Existing rows are never rewritten; a connection default set after a Portfolio exists does not switch it on.
```

#### D2

```text
EF mapping: no migration. Mapping stays HasDefaultValue(true).HasSentinel(true). Table, entity value then stored result: CLR false (absent field, no connection default, or explicit false) differs from the sentinel, so EF inserts false (off). CLR true (rule with a connection default, or explicit true) equals the sentinel, EF omits the column, the DB default true applies (on). Both starting values store correctly. Existing rows and restored old backups hold true and are not touched. Changing the DB column default is rejected: it needs an expand-only migration for every provider and changes no observable outcome.
```

#### D3

```text
Create path: PortfoliosController.CreatePortfolio calls newPortfolio.StartOnConnection(connection) where it now assigns the connection, before the setting-refusal check and SyncWithPortfolioSettings. PortfolioExtensions keeps the entity value when the DTO bool? is null, so an absent field gives the rule value and a named true or false wins. The create wizard omits infersDefaultFeatureSize from its settings, so the backend is the single owner of the starting value (the type already allows an absent field). Clone sends the source settings, so it keeps the source value. The frontend read fallback stays true: it only meets stored values or the missing field of an older server, and both mean on. Usage event 29 does not fire on a wizard create, because an omitted field reads as on and nothing switches on to off.
```

#### D4

```text
Demo data: DemoDataFactory builds demo Portfolios off (entity default). DemoDataService assigns the demo connection afterwards, and that connection names no Portfolio Size Estimate Field default, so the rule would also give off: the demo path needs no call to StartOnConnection today. If a demo connection ever names such a default, DemoDataService must call StartOnConnection instead of assigning the connection. A demo Portfolio gets InfersDefaultFeatureSize = true in DemoDataFactory only where a measured screenshot result needs inferred sizes; the crafter runs each affected @screenshot test alone and decides per Portfolio.
```

#### D5

```text
Checker choice: the 6504 Quint model (docs/feature/story-6504-childless-feature-size/model/default-sizing.qnt) is unchanged and not re-run. This feature adds no state, transition or interaction, only a finite starting-value table (absent, true, false, by create, clone, existing). The falsifier is a native NUnit TestCase table over real SQLite EF, because the in-memory EF provider ignores column defaults and sentinels and cannot tell the wrong mapping from the right one. Liveness: not applicable, no temporal behaviour. Agda: no proof obligation. Quint is not used for the table by host direction to stay proportionate; the table claim is DESIGN-checked by the executable table only.
```

#### D6

```text
Test port: create has no application service; the controller builds the Portfolio. The lowest layer that proves the starting value is PortfoliosController.CreatePortfolio over real SQLite EF, as NewPortfolioStartingValueTest already does through ServicePortHarness; its table gains rows for a connection naming a size field default by absent, explicit false and explicit true. The wizard test pins that the settings sent omit the field. The five Slice10 PortfoliosSizeEstimateFieldDefault scenarios run unchanged and must go green. The older-client wire concern stays in PortfolioSettingDtoCompatibilityTest (connection without a default). No web host start is warranted for the new rows.
```

#### D7

```text
Usage event (recommendation, DEVOPS to confirm): keep DefaultFeatureSizingSwitchedOff = 29 unchanged, no rename, no renumber; it still counts people who leave sizing on for an existing Portfolio. If DEVOPS wants to measure opting in on new Portfolios, append a name-only DefaultFeatureSizingSwitchedOn with id 30. No usage-data code in this slice.
```

#### D8

```text
Context map: no new context, term, edge or domain event. ADR-270 glossary row Default Feature sizing changes from On by default to On for Portfolios created before this change, off for new ones. docs/portfolios/edit.md section Switching default Feature sizing off gets the same fact. ARCHITECTURE.md: the crafter greps for a stated default of the switch and edits only if one exists.
```

#### D9

```text
Views: no new or changed view. Create wizard, settings tab and Features list keep their existing loading and failure looks; only a starting value and demo content change.
```

#### D10

```text
Patterns: chosen, a domain-owned named method on the aggregate (Portfolio.StartOnConnection) over a constructor-level false default alone: one owner, called by the one create path, testable without a host. Rejected: an if in the controller (rule outside the entity); a setter on the connection property that sets the switch (EF materialisation would flip stored rows); wizard derives the value from the connection (second owner, races a later connection change); set false in the controller and demo factory (two owners); change the DB column default (migration, no gain); heuristic migration for already using it (maintainer rejected, moves forecasts); nullable stored value as a third state (needs migration and a resolver).
```

#### D11

```text
Paradigm object_oriented applied (nw-code-design-oo): the starting-value rule is data of the Portfolio entity, no service decides it. No anemic or script path is added.
```

#### D12

```text
Routing of the three other failures after the first build, all tests only, acceptance designer owns them. Principle: seed the switch on where the test guards behaviour as before; re-approve only a test about what a new Portfolio holds. (1) Slice02DependencyDetailScenarios A_refresh_that_finds_features_no_one_can_forecast_says_how_many_in_one_line: it guards dependency forecasting for Features that carry work; seed its Portfolio with InfersDefaultFeatureSize true in GivenAPortfolio, or give the Feature child Work Items. (2) Slice02OverviewReadsNamesAndCountsScenarios one Portfolios settings and (3) CatalogueBodiesOnPostgresTests settings: both read the shared Northwind world built in FastWithManyFeaturesAcceptanceTest.cs; they guard that the reads are exactly as before, so seed Northwind Platform with the switch on there and keep the approved body slice02-portfolio-northwind-platform-settings unchanged. No re-approval. If a body is ever re-approved, the mechanism is ApprovedBodies.Verify: it writes the .received.json beside the approved file and the person renames it to .approved.json after reading it.
```

#### D13

```text
Crafter hand-back: before handing back, run the whole filtered backend suite, host and container tests included: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration" (about 8 minutes), as a background job. The earlier probe skipped these tests.
```

### Constraints

#### K1

```text
Existing Portfolios keep their stored value (on); no data migration, no heuristic. Only the starting value for new Portfolios changes; what on and off does stays as Story 6504 built it.
```

#### K2

```text
An API or Lighthouse-Clients create that names the switch (true or false) keeps the named value; only an absent field gets off. PortfolioSettingDto.InfersDefaultFeatureSize stays bool?, no bound on its value.
```

#### K3

```text
No EF migration and no change to HasDefaultValue(true).HasSentinel(true). Migrations stay expand-only; none is needed.
```

#### K4

```text
Existing tests that rely on a new Portfolio being on (the Untouched cases in DefaultFeatureSizingTest, fixtures built with new Portfolio, the Northwind world, the dependency log test) must name the switch explicitly where they test inferred sizing or behaviour as before. Owner: acceptance designer, with the crafter running the affected loop. The crafter edits no test.
```

#### K5

```text
Test budgets: defaults. The new SQLite table test builds one in-memory database per fixture and stays well inside the per-test budget; no derogation requested.
```

#### K6

```text
Starting rule bounds: the rule reads only whether the connection names a Portfolio Size Estimate Field default at create time. A connection that is missing or names none gives off. A create that names its own Size Estimate Field follows the same rule until the maintainer decides (open). An explicit true or false on a create wins over the rule. A clone and an update never run the rule.
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Portfolio\.cs

Extend

```text
InfersDefaultFeatureSize starts false; new method StartOnConnection assigns the connection and sets the switch on when the connection names a Portfolio Size Estimate Field default.
```

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx

Extend

```text
Wizard base settings omit infersDefaultFeatureSize so the backend decides.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
Only if a measured screenshot needs a demo Portfolio on: set InfersDefaultFeatureSize = true there.
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Glossary row states the starting value: off, on when the connection names a Portfolio Size Estimate Field default.
```

docs\/portfolios\/edit\.md

Extend

```text
User docs state the starting value, including the connection default case.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
CreatePortfolio calls StartOnConnection where it assigned the connection.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DefaultFeatureSizingTest|FullyQualifiedName~NewPortfolioStartingValueTest|FullyQualifiedName~PortfolioSettingDtoCompatibilityTest|FullyQualifiedName~Slice10PortfoliosSizeEstimateFieldDefaultScenarios
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/PortfolioSettingDtoCompatibilityTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DefaultFeatureSizing\/NewPortfolioStartingValueTest\.cs

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice10PortfoliosSizeEstimateFieldDefaultScenarios\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/Slice02DependencyDetailScenarios\.cs

#### Public oracle

```text
Create a Portfolio through the create route on a connection with no size field default and on one that names it, with the sizing switch absent (and, as table rows, true and false), clone one with the switch on and one off, then refresh with a Feature that has no child Work Items; the wizard creates without touching the switch and sends no value for it.
```

```text
Absent gives off (0 of 0, No child Work Items yet) without a connection default and on with one (the Feature takes the size the field holds); named values are kept; clones copy the source; an existing stored true stays true; switching on later gives the Default Feature Size.
```

```text
A Portfolio on a size-field-default connection stored off after an absent field (the field ignored), one without it stored on, explicit true stored off, an existing row flipped, or a wizard create that sends a value.
```

##### Promises

P1

P2

P3

P4

P5

P6

P7

P8

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

InfersDefaultFeatureSize

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Portfolio\.cs\:17

Extend

```text
Portfolio owns the switch, its two guarded selectors and the connection-default size field readers; the starting rule is its data. No other candidate decides a new Portfolio starting state.
```

HasSentinel

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:355

Reuse

```text
HasDefaultValue(true).HasSentinel(true) already gives absent-false, explicit-true and explicit-false the right stored result once the entity starts false. Unchanged, so no migration.
```

InfersDefaultFeatureSize

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/PortfolioExtensions\.cs\:31

Reuse

```text
SyncWithPortfolioSettings keeps the entity value when the DTO bool? is null and takes a named value otherwise: exactly decision D4 and the named-value rule. No change.
```

CreatePortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:104

Extend

```text
The one create path for API, Clients and wizard; its connection assignment becomes the single call of the domain rule.
```

CreateDemoProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs\:32

Extend

```text
Inherits off from the entity. Extended only to set InfersDefaultFeatureSize = true on a demo Portfolio whose measured screenshot needs inferred sizes; the factory is the one place demo Portfolios are built.
```

infersDefaultFeatureSize

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx\:47

Extend

```text
The wizard names the switch today; it must stop naming it so the backend rule applies. The field is already optional in the settings type.
```

getPortfolioSettings

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:50

Reuse

```text
Clone spreads the source settings, which carry the stored switch value: decision D3 already holds. No change.
```

infersDefaultFeatureSize

Lighthouse\.Frontend\/src\/models\/Portfolio\/PortfolioSettings\.ts\:29

Reuse

```text
Reads stored values, or the missing field of an older server (an existing Portfolio, so on). Fallback true stays correct.
```

Default Feature sizing

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:747

Extend

```text
The glossary row is the one owner of the term; edit its default wording instead of adding a row.
```

Switching default Feature sizing off

docs\/portfolios\/edit\.md\:141

Extend

```text
The docs section on the switch is the one user-facing owner; edit it instead of adding a page.
```

SizeEstimateFieldForPortfolios

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemConnection\.cs\:77

Reuse

```text
The connection default the rule reads. Same fact Portfolio.SizeEstimateField already follows; no second copy.
```

##### Impact

domain\-entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Portfolio\.cs\:17

Changed

```text
Initial value true to false; new StartOnConnection method.
```

persistence\-mapping

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:353

UnchangedCompatible

```text
Sentinel true and DB default true still give the right stored result for every create; existing rows hold true.
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/20261125090443\_AddPortfolioInfersDefaultFeatureSize\.cs\:1

UnchangedCompatible

```text
Column default true stays; no new migration for Postgres or Sqlite.
```

test\-helper

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/HistoricalSchemaPatch\.cs\:41

UnchangedCompatible

```text
Mirrors the column default TRUE for restored old backups; the default does not change.
```

wire\-contract

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/PortfolioSettingDto\.cs\:98

UnchangedCompatible

```text
bool? with absent meaning keep the entity value; the entity value is now off for a create.
```

api\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:137

UnchangedCompatible

```text
Validate-style Portfolio is never saved; its switch value is not read.
```

background\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:301

UnchangedCompatible

```text
Short-lived Portfolio built to resolve forecast write-back, never saved; write-back resolution does not use the sizing switch. Crafter re-confirms by reading ResolveForecastWriteBackForPortfolio.
```

demo\-data

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs\:32

Changed

```text
Demo Portfolios now start off; a demo that needs inferred sizing is set on explicitly.
```

frontend\-component

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx\:47

Changed

```text
Wizard omits the field.
```

frontend\-component

Lighthouse\.Frontend\/src\/components\/Common\/ProjectSettings\/useDefaultFeatureSizingSavedReporter\.ts\:21

UnchangedCompatible

```text
Compares last saved with new value; a wizard create of a new Portfolio holds false before and after, so event 29 does not fire.
```

client\-library

lighthouse\-clients\/packages\/client\/src\/index\.ts\:2539

UnchangedCompatible

```text
createPortfolio passes an untyped payload and never names the switch, so it gets off from the backend.
```

documentation

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:747

Changed

```text
Glossary row says On by default; must say on for existing Portfolios, off for new ones unless the connection names a Portfolio Size Estimate Field default.
```

documentation

docs\/portfolios\/edit\.md\:141

Changed

```text
User docs section on the switch states the new starting value and the connection default exception.
```

documentation

ARCHITECTURE\.md\:99

UnchangedCompatible

```text
No statement about the switch default found in the file.
```

e2e\-and\-screenshots

Lighthouse\.EndToEndTests\/tests\/specs\/screenshots\/Screenshots\.spec\.ts\:365

Changed

```text
Screenshot tests over demo data that shows Features without child Work Items (portfolio detail, Features page, portfolio metrics, delivery shots, team forecasts) may change content; crafter measures each alone and sets a demo Portfolio on only where needed.
```

api\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:104

Changed

```text
Create calls StartOnConnection instead of assigning the connection; update, clone and validate paths do not run the rule.
```

test\-seed

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:237

Changed

```text
The Northwind world and the dependency scenarios seed the switch on where they guard behaviour as before; tests only, acceptance designer.
```

##### Agreement

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/PortfolioSettingDto\.cs\:98

Producer

UnchangedCompatible

```text
Wire shape unchanged; GET maps the stored value.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/PortfolioExtensions\.cs\:31

Consumer

UnchangedCompatible

```text
Absent keeps the entity value, which a create has already set by the connection rule; a named value is kept.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx\:47

Producer

Migrated

```text
Wizard omits the field instead of sending true.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:50

Producer

UnchangedCompatible

```text
Clone sends the source value.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

lighthouse\-clients\/packages\/client\/src\/index\.ts\:2539

Producer

UnchangedCompatible

```text
Untyped payload without the field; now gets off, as decision D4 wants.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Frontend\/src\/models\/Portfolio\/PortfolioSettings\.ts\:29

Consumer

UnchangedCompatible

```text
Read fallback true applies to an older server without the field.
```

##### Not applicable

architectural_tests

```text
No dependency, boundary or owner law is reachable by this change: one entity default and one frontend literal; the existing architecture guards are unaffected.
```

prefactoring

```text
One property default, one new method on the entity and one controller line; no structure blocks the change, so no preserving move is needed.
```

##### Boundaries

```text
PortfoliosController.CreatePortfolio (HTTP create, shared by wizard, API and Clients) and EditPortfolio clone form; the Portfolio refresh through WorkItemService for the observed sizing
```

###### Driven ports

```text
IPortfolioRepository.Save on EF (SQLite and Postgres)
```

```text
API adapter to Portfolio entity and PortfolioExtensions; EF mapping depends on the entity; the entity depends on nothing outward
```

###### Failures

None.

##### Observations

```text
Stored and read InfersDefaultFeatureSize of a Portfolio after create, clone or upgrade, and the size a Feature without child Work Items gets after the next refresh
```

bool plus Feature remaining work \(0 of 0\, No child Work Items yet\, or the Default Feature Size\)

```text
An in-memory EF store cannot tell a DB-default insert from an explicit insert; the sizing outcome alone cannot tell off from an on Portfolio whose default size is 0.
```

##### Equality

starting value

```text
Two creates are equal when their stored InfersDefaultFeatureSize is equal after save and reload through the real EF mapping.
```

##### Constructors

new Portfolio\(\)

Portfolio\(\) \-\> Portfolio with InfersDefaultFeatureSize \= false

Portfolio\.StartOnConnection

StartOnConnection\(WorkTrackingSystemConnection\) \-\> void\; assigns the connection\; InfersDefaultFeatureSize \= connection\.SizeEstimateFieldForPortfolios\.HasValue

##### Laws

###### L1

```text
Starting-value table, after save and reload through real SQLite EF: create without the field gives off, or on when the connection names a size field default; create naming true gives on; create naming false gives off whatever the connection; clone gives the source value; an existing stored row is read unchanged; a connection default set after create does not change a stored Portfolio.
```

Invariant

Adapter

Portfolio

###### Treatment

Finite

###### Cases

```text
create absent, no connection default -> off
```

```text
create absent, connection default -> on
```

```text
create true, no connection default -> on
```

```text
create true, connection default -> on
```

```text
create false, no connection default -> off
```

```text
create false, connection default -> off
```

```text
clone of on -> on
```

```text
clone of off -> off
```

```text
existing row true -> true
```

```text
connection default set after create, stored off -> off
```

```text
Closed finite domain of three inputs by two connection states by the create, clone and existing paths; a native TestCase table is exhaustive and PBT adds nothing.
```

###### Evidence

None.

###### L2

```text
Switching on later: a new Portfolio saved with the switch on gives Features without child Work Items the Default Feature Size, as before this change.
```

Invariant

Domain

Portfolio

###### Treatment

Finite

###### Cases

```text
new Portfolio, switched on, refreshed -> Default Feature Size
```

```text
Behaviour of on is unchanged 6504 behaviour, covered by existing DefaultFeatureSizingTest cases; one example.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
A Portfolio built by the create path without naming the switch is on exactly when its connection names a Portfolio Size Estimate Field default, else off; one naming true or false keeps that value; stored rows are not rewritten.
```

```text
Wrong default: new Portfolios infer sizes (the confusion), a connection size field is ignored by a Portfolio that follows it, or existing Portfolios silently flip and forecasts move.
```

Portfolio

```text
absent, true or false switch in a create payload; connection with or without a size field default; stored true
```

Portfolio\(\) default false\, then StartOnConnection\(connection\)\, then SyncWithPortfolioSettings for a named value

WorkItemService sizing selectors\, Portfolio\.SizeEstimateAdditionalFieldDefinitionId readers and PortfolioSettingDto GET

```text
GET settings shows the value; Feature rows show 0 of 0 and No child Work Items yet when off, or the size the Size Estimate Field holds when on through the connection default
```

```text
Raw SQL insert or an old backup restore omit the column and get the DB default true; a direct EF save of a Portfolio set by code is explicit; a caller that assigns the connection without StartOnConnection gets off.
```

##### States

Admitted

```text
switch off (new Portfolio) and on (existing, cloned from on, or set on by the person)
```

Portfolio

Excluded

```text
new Portfolio created without the field starting on without a connection size field default, or starting off with one
```

Portfolio

Unknown

```text
which demo Portfolio screenshots need inferred sizing until measured
```

crafter

Unknown

```text
own-field create: a create naming its own Size Estimate Field follows the plain rule (off unless the connection names a default) until decided
```

maintainer

##### Models

None.

##### Closure

```text
starting-value table
```

Safety

Pass

DesignedNotBuilt

```text
Entity default false, StartOnConnection as the only raiser at create, and the unchanged sentinel give the table by EF semantics; falsified by the SQLite table test and the five Slice10 scenarios.
```

###### Evidence

None.

```text
starting-value table
```

Inhabitation

Pass

DesignedNotBuilt

```text
Input: create payload and connection; constructor: new Portfolio(), StartOnConnection, SyncWithPortfolioSettings; consumer: PortfolioSettingDto GET and sizing selectors; port: PortfoliosController.CreatePortfolio. Both starting values are reachable.
```

###### Evidence

None.

```text
starting-value table
```

PublicReachability

Pass

DesignedNotBuilt

```text
Create route to stored value to GET settings and the Features list, all existing public ports.
```

###### Evidence

None.

```text
starting-value table
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no temporal or progress behaviour, a stored value is set once at create.
```

###### Evidence

None.

##### Risks

###### Y1

Stressor

```text
Assumption: EF omits the column when the CLR value equals the sentinel. Negated: EF writes the column always.
```

```text
Explicit true stays on
```

```text
none
```

```text
Explicit true stored off
```

```text
Explicit true is written either way, so the outcome holds; the SQLite table test observes it.
```

crafter

###### Y2

Bypass

```text
Assumption: every writer goes through Portfolio and the EF mapping. Negated: backup restore or raw SQL inserts without the column.
```

```text
Existing rows stay on
```

```text
a restored old backup reads on
```

```text
a restore flipping a Portfolio to off
```

```text
Restored old rows get the DB default true, the same as stored today; accepted.
```

maintainer

###### Y3

Stressor

```text
Assumption: existing tests rely on default on only where they test sizing. Negated: many fixtures built with new Portfolio rely on it implicitly.
```

```text
Test suite meaning
```

```text
tests naming the switch explicitly
```

```text
silently weakened tests
```

```text
Broad blast over 13 files that set a default size; the fast loop shows which. Owner: acceptance designer.
```

acceptance\-designer

###### Y4

Stressor

```text
Second walk: an older Lighthouse UI tab saves an existing Portfolio without the field; a script creates with infersDefaultFeatureSize null.
```

```text
Update keeps the stored value; null on create means off
```

```text
none
```

```text
update flips the stored value
```

```text
Update goes through sync on a loaded entity, which keeps the stored value on null; create null gives off.
```

crafter

###### Y5

Stressor

```text
Assumption: the connection default is present when the Portfolio is created. Negated: an admin sets it after the Portfolio exists, or the connection is missing.
```

```text
Existing Portfolio keeps its stored value
```

```text
a Portfolio created before the admin set the default stays off
```

```text
a later default switching a stored Portfolio on, or a missing connection failing the create
```

```text
The size field is ignored by an off Portfolio until the person switches sizing on; accepted by the maintainer.
```

maintainer

###### Y6

Bypass

```text
Assumption: only the create path starts a Portfolio. Negated: EF materialisation or a second caller runs the rule on a stored row, or a new create path skips it.
```

```text
Stored rows are read unchanged
```

```text
a new create path calls StartOnConnection itself
```

```text
a stored row rewritten on load
```

```text
StartOnConnection is a named method, never a setter, so loading cannot run it; a future create path that skips it gets off. Owner crafter: the controller is the only saved create path today (validate and forecast write-back build unsaved Portfolios).
```

crafter

##### Architectural tests

None.

##### Contract tests

###### T1

```text
An older client payload without the switch, read as the create route reads it and synced into a new Portfolio on a connection without a size field default, starts off; a payload naming true or false keeps it.
```

PortfolioSettingDto\.InfersDefaultFeatureSize wire shape

Older client JSON payload \(WhatAnOlderClientSends\)

PortfolioExtensions\.SyncWithPortfolioSettings on a new Portfolio

```text
Portfolio.InfersDefaultFeatureSize after sync: false when absent, the named value when named
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/PortfolioSettingDtoCompatibilityTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~PortfolioSettingDtoCompatibilityTest
```

## ADRs

None.
