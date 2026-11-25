# Architecture brief: new\-portfolio\-default\-sizing\-off

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Owner: Portfolio (Models/Portfolio.cs) owns the starting value of Default Feature sizing. Its property InfersDefaultFeatureSize starts false, so every new Portfolio starts off wherever it is built: API create, demo data, tests. Existing rows keep their stored value, because the column default and the EF sentinel stay true.
```

#### D2

```text
EF mapping: no migration. Mapping stays HasDefaultValue(true).HasSentinel(true). Table, entity value then stored result: absent field gives CLR false, which differs from the sentinel, so EF inserts false (off). Explicit true equals the sentinel, EF omits the column, the DB default true applies (on). Explicit false is inserted false (off). Existing rows and restored old backups hold true and are not touched. Changing the DB column default is rejected: it needs an expand-only migration for every provider and changes no observable outcome.
```

#### D3

```text
Create path: the create route stays as it is. PortfolioExtensions keeps the entity value when the DTO bool? is null, so an absent field gives off and a named true or false is kept. The create wizard sends false explicitly (its base settings were true). Clone spreads the source settings, so it keeps the source value. The frontend read fallback stays true: it only meets stored values, or the missing field of an older server, and both mean on.
```

#### D4

```text
Demo data: demo Portfolios inherit off from the entity with no factory change. A demo Portfolio gets InfersDefaultFeatureSize = true in DemoDataFactory only where a measured screenshot result needs inferred sizes. The crafter runs each affected @screenshot test alone (--grep one test) and decides per Portfolio. Candidates, by what they show: portfolio detail page, Features page, portfolio metrics widgets, delivery creation, retire-delivery record, delivery timeline, team forecasts. No E2E spec sets or reads the sizing fields, so none relies on them. Changed screenshot content is visible to the maintainer: open finding.
```

#### D5

```text
Checker choice: the 6504 Quint model (docs/feature/story-6504-childless-feature-size/model/default-sizing.qnt) is unchanged and not re-run. This feature adds no state, transition or interaction, only a finite starting-value table (absent, true, false, by create, clone, existing). The falsifier is a native NUnit TestCase table over real SQLite EF, because the in-memory EF provider ignores column defaults and sentinels and cannot tell the wrong mapping from the right one. Liveness: not applicable, no temporal behaviour. Agda: no proof obligation. Quint is not used for the table by host direction to stay proportionate; the table claim is DESIGN-checked by the executable table only.
```

#### D6

```text
Test port: create has no application service; the controller builds the Portfolio. The lowest layer that proves absent field gives off is Portfolio plus SyncWithPortfolioSettings plus repository Save on real SQLite. The older-client wire concern is covered by extending PortfolioSettingDtoCompatibilityTest (JSON without the field, read as the route reads it, then synced). No web host start is warranted.
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
Patterns: chosen, a constructor-level default on the aggregate (one owner; covers API, demo data and tests). Rejected: set false in the controller and demo factory (two owners, new Portfolio() stays on elsewhere); change the DB column default (migration, no gain); heuristic migration for already using it (maintainer rejected, moves forecasts); nullable stored value as a third state (needs migration and a resolver).
```

#### D11

```text
Paradigm object_oriented applied (nw-code-design-oo): the starting-value rule is data of the Portfolio entity, no service decides it. No anemic or script path is added.
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
Existing tests that rely on a new Portfolio being on (for example the Untouched cases in DefaultFeatureSizingTest, and fixtures built with new Portfolio) must name the switch explicitly where they test inferred sizing. Owner: acceptance designer, with the crafter running the affected fast loop. The crafter edits no test.
```

#### K5

```text
Test budgets: defaults. The new SQLite table test builds one in-memory database per fixture and stays well inside the per-test budget; no derogation requested.
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
InfersDefaultFeatureSize starts false.
```

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx

Extend

```text
Wizard base settings name infersDefaultFeatureSize false.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
Only if a measured screenshot needs a demo Portfolio on: set InfersDefaultFeatureSize = true there.
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Glossary row for Default Feature sizing states the new starting value.
```

docs\/portfolios\/edit\.md

Extend

```text
User docs state the new starting value.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DefaultFeatureSizingTest|FullyQualifiedName~NewPortfolioStartingValueTest|FullyQualifiedName~PortfolioSettingDtoCompatibilityTest
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/PortfolioSettingDtoCompatibilityTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NewPortfolioStartingValueTest\.cs

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.test\.tsx

#### Public oracle

```text
Create a Portfolio through the create route with the sizing switch absent (and, as table rows, true and false), clone one with the switch on and one off, then refresh it with a Feature that has no child Work Items; the wizard creates without touching the switch.
```

```text
Absent gives off and the Feature counts as 0 (0 of 0, No child Work Items yet); named values are kept; clones copy the source; an existing stored true stays true; switching on later gives the Default Feature Size.
```

```text
The Portfolio is saved on after an absent field (entity default still true), explicit true is stored off, or an existing row flips; also a wizard create that sends true.
```

##### Promises

P1

P2

P3

P4

P5

P6

P7

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
Portfolio already owns the switch and its two guarded selectors; only the initial value changes. No other candidate decides a new Portfolio starting state.
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

Reuse

```text
new Portfolio() then sync is the single create path for API, Clients and wizard; it inherits the new entity default.
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
The wizard names the switch with true today; it must name false so a wizard create starts off.
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

##### Impact

domain\-entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Portfolio\.cs\:17

Changed

```text
Initial value true to false.
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
Wizard names false.
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
Glossary row says On by default; must say on for existing Portfolios, off for new ones.
```

documentation

docs\/portfolios\/edit\.md\:141

Changed

```text
User docs section on the switch states the new starting value.
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
Absent keeps the entity value, now off for a create; named value kept.
```

PortfolioSettingDto\.InfersDefaultFeatureSize \(bool\?\, absent keeps the entity value\)

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.tsx\:47

Producer

Migrated

```text
Wizard sends false instead of true.
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
One property default and one literal change; no structure blocks the change, so no preserving move is needed.
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

##### Laws

###### L1

```text
Starting-value table: create without the field gives off; create naming true gives on; create naming false gives off; clone gives the source value; an existing stored row is read unchanged; after save and reload through real SQLite EF.
```

Invariant

Adapter

Portfolio

###### Treatment

Finite

###### Cases

```text
create absent -> off
```

```text
create true -> on
```

```text
create false -> off
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
Closed finite domain of three inputs by three paths; a native TestCase table is exhaustive and PBT adds nothing.
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
A Portfolio built without naming the switch is off; one naming true or false keeps that value; stored rows are not rewritten.
```

```text
Wrong default: new Portfolios infer sizes (the confusion) or existing Portfolios silently flip and forecasts move.
```

Portfolio

```text
absent, true or false switch in a create payload; stored true
```

Portfolio\(\) default false\, with SyncWithPortfolioSettings for a named value

WorkItemService sizing selectors and PortfolioSettingDto GET

```text
GET settings shows the value; Feature rows show 0 of 0 and No child Work Items yet when off
```

```text
Raw SQL insert or an old backup restore omit the column and get the DB default true (on); no writer in the product does so. A direct EF save of a Portfolio set true by code is explicit.
```

##### States

Admitted

```text
switch off (new Portfolio) and on (existing, cloned from on, or set on by the person)
```

Portfolio

Excluded

```text
new Portfolio created without the field starting on
```

Portfolio

Unknown

```text
which demo Portfolio screenshots need inferred sizing until measured
```

crafter

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
Entity default false plus unchanged sentinel gives the table by EF semantics; falsified by the SQLite table test.
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
Input: create payload; constructor: new Portfolio() then SyncWithPortfolioSettings; consumer: PortfolioSettingDto GET and sizing selectors; port: PortfoliosController.CreatePortfolio.
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

##### Architectural tests

None.

##### Contract tests

###### T1

```text
An older client payload without the switch, read as the create route reads it and synced into a new Portfolio, starts off; a payload naming true or false keeps it.
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
