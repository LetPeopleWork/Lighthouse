# Architecture brief: bug\-6665\-former\-parents\-stranded

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Keep rule has one owner: OrphanedFeatureCleanupService.CleanupAsync, a domain-service policy over the whole Feature set (it reads other Features, so it is cross-aggregate). A Feature is kept iff it is in a Portfolio or its ReferenceId is the ParentReferenceId of a Feature that is in a Portfolio; every other Feature is deleted set-based with ExecuteDeleteAsync. The two clauses are named private static Expression<Func<Feature,bool>> members (IsInAPortfolio, ParentsAPortfolioFeature) so the rule is written once. Rejected: a query method on IFeatureRepository (a second place deciding the same fact); a method on Feature (the rule reads other rows, not one object's data).
```

#### D2

```text
IsParentFeature stops deciding the clean-up. The column and its write in WorkItemService.RefreshParentFeatures stay unchanged (migrations are expand-only: no drop, no rename). Its other reader, WorkItemService.RefreshFeatures (keeps flagged items out of blocked-edge detection), is unchanged and out of scope. The WorkItemService comment above TheSweepDidNotVouchForThisParent that names !f.IsParentFeature as the clean-up rule is reworded in S3. No API DTO or frontend code reads the flag; a child's Parent column resolves its parent by ReferenceId and needs only the stored row. Rejected: dropping the column (expand-only); maintaining the flag on update (no reader needs it any more).
```

#### D3

```text
Remaining work of a Feature in no Portfolio is removed by the same clean-up: after the Feature delete, CleanupAsync deletes every FeatureWork row whose Feature is in no Portfolio (ExecuteDeleteAsync). Rows are deleted, not zeroed, because FeatureRepository.FeaturesOfTeam and FeaturesWorkedOnBy select by FeatureWork row existence; Feature.ClearFeatureWork only zeroes counts and would leave the item on the Team tab. WhenForecast and StartForecast rows of a kept parent are left alone (P5); with no FeatureWork it is in no forecast set and on no Team page. Rejected: a 'Feature left its last Portfolio' domain event (covers only future departures, so existing instances would still need the sweep: two owners of one fact); clearing inline in WorkItemService.RefreshRemainingWork (it loops only the Portfolio's current Features and misses Portfolio delete and start).
```

#### D4

```text
Portfolio delete runs the clean-up at once through a new domain event PortfolioDeleted(int PortfolioId) in Models/Events, published by PortfolioController.DeletePortfolio's queued lambda after the save, as TeamController publishes TeamDeleted. A new handler PortfolioDeletedOrphanedFeatureCleanupHandler in Modules/WorkItems/Application/Policies calls IOrphanedFeatureCleanupService.CleanupAsync and is registered in Program.cs beside the TeamDeleted handlers. PortfolioRepository.Remove stops deleting Features (RemoveOrphanedFeatures goes): today it is a second owner of the orphan rule and deletes a Feature that still parents a Feature in another Portfolio. Rejected: calling the clean-up directly in the controller lambda (the decision would sit in a driving adapter, and ADR-270 lists Portfolio deleted as a fact with no event); keeping RemoveOrphanedFeatures (duplicate rule that contradicts maintainer decision A1).
```

#### D5

```text
Existing instances need no migration: the startup clean-up (Program.RunStartupOrphanedFeatureCleanupAsync) and the clean-up in the finally of every PortfolioUpdater run call the same CleanupAsync, so the new rule applies at the next start or refresh. The refreshed Portfolio's forecast is enqueued by forecastUpdater.TriggerUpdate and runs after that clean-up; its input fingerprint changes because the gathered Feature set changed, so it recomputes and the stranded Feature no longer takes Team throughput.
```

#### D6

```text
Churn ends by the keep rule alone: a parent stored unflagged (downloaded by the Feature half) is kept while a Portfolio Feature names it, and RefreshParentFeatures finds the row by ReferenceId and updates it (TheStoredFeatureFor), never re-creating it. Once PortfolioRepository.Remove stops deleting Features, the clean-up is the only production path that deletes Features.
```

#### D7

```text
Claim-to-checker: the keep rule, the removal of remaining work outside every Portfolio and no-churn are finite state-safety claims over Features, Portfolio memberships and parent links, checked in Quint (model docs/feature/bug-6665-former-parents-stranded/models/keepRule.qnt): simulation with quint run and a bounded check with quint verify on the Apalache backend. No Agda proof is needed (no totality or arithmetic claim). Liveness does not apply: the clean-up is a synchronous step at the end of every refresh, Portfolio delete and start; nothing promises eventual progress beyond that step running.
```

#### D8

```text
Context map: Work Items context only (ADR-270). New domain event PortfolioDeleted, published where the Portfolio is deleted (a controller today, as for TeamDeleted) and handled in Work Items; it closes ADR-270's gap 'facts with no event: Portfolio deleted'. New Work Items glossary term: Orphaned Feature, a Feature in no Portfolio that parents no Feature in a Portfolio; the clean-up deletes it. ADR-270 (gap list, event-to-context list, glossary) and ARCHITECTURE.md section 4 are updated at finalize. No UI surface change.
```

#### D9

```text
Type strength and PBT: C# with nullable analysis in the build. The keep rule depends on other rows, so it cannot be a constructor invariant; it stays extrinsic and set-based at its single writer. Property-based testing is not used: the relevant shapes form a finite set (in a Portfolio, parents a Portfolio Feature, flagged, has FeatureWork) that native NUnit examples cover, and the broad state space is covered by the Quint model.
```

### Constraints

#### K1

```text
No data migration; EF migrations are additive-only, so the IsParentFeature column stays.
```

#### K2

```text
Feature to Portfolio stays many-to-many; no UI copy or layout change; no usage-data event.
```

#### K3

```text
Backend acceptance scenarios drive the application-service port (IOrphanedFeatureCleanupService, the PortfolioDeleted handler) and never start a new web host (TestHostUseGuardTest).
```

#### K4

```text
Authority states no bound on Feature, Portfolio or parent-link counts; the clean-up stays set-based (ExecuteDeleteAsync) and never loads Features into memory.
```

### Reuse

OrphanedFeatureCleanupService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

Extend

```text
already the one clean-up called after every refresh and at start; it becomes the single owner of the keep rule and of removing remaining work outside every Portfolio
```

RemoveOrphanedFeatures

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/PortfolioRepository\.cs\:130

Replace

```text
semantic duplicate of the orphan rule that contradicts decision A1; replaced by the clean-up run on PortfolioDeleted
```

TeamDeleted

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Events\/TeamDeleted\.cs\:3

Reuse

```text
pattern for a controller-published deletion event; PortfolioDeleted is created new because no Portfolio deletion event exists (ADR-270 gap)
```

TeamDeletedRefreshLogCleanupHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:8

Reuse

```text
shape and home of the new Work Items policy handler; no existing handler reacts to a Portfolio deletion, so PortfolioDeletedOrphanedFeatureCleanupHandler is created
```

ClearFeatureWork

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Feature\.cs\:318

Reuse

```text
considered and not used for removal: it zeroes counts and keeps rows, which FeaturesOfTeam still selects
```

FeaturesOfTeam

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:143

Reuse

```text
unchanged reader; correct once FeatureWork rows of non-members are gone
```

FeaturesWorkedOnBy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:86

Reuse

```text
unchanged forecast input; correct once FeatureWork rows of non-members are gone
```

DeletePortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:72

Extend

```text
S1: existing delete endpoint; its queued lambda publishes PortfolioDeleted after the save, as TeamController does
```

TeamDeletedRefreshLogCleanupHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1647

Extend

```text
S1: existing composition root; the new handler is registered beside the TeamDeleted handlers
```

### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IOrphanedFeatureCleanupService\.cs\:5

UnchangedCompatible

```text
signature unchanged; only the rule behind it changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:209

UnchangedCompatible

```text
still calls CleanupAsync in its finally
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:742

UnchangedCompatible

```text
startup clean-up applies the new rule to existing instances
```

schema

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Feature\.cs\:77

UnchangedCompatible

```text
IsParentFeature column kept and still written; no migration
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:937

UnchangedCompatible

```text
blocked-edge exclusion by flag unchanged and out of scope
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs\:128

UnchangedCompatible

```text
reads FeaturesWorkedOnBy; drops non-members once their FeatureWork rows are gone
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs\:118

UnchangedCompatible

```text
still expects the deleted Portfolio's sole Features gone; now delivered by the PortfolioDeleted handler, so it witnesses the event wiring
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1001

UnchangedCompatible

```text
pins the flag written by the parent half, which stays
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice03JiraCloudPortfolioDeltaSpecifications\.cs\:356

UnchangedCompatible

```text
asserts the parent is stored and flagged after a delta cycle; both still hold
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Story5913AlwaysFasterUpdatesSpecifications\.cs\:289

UnchangedCompatible

```text
same as above
```

public\-interface

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:72

Changed

```text
S1: DELETE keeps its route, rights and 204/404; it now also publishes PortfolioDeleted
```

build

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1647

Changed

```text
S1: new handler registration
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/PortfolioRepository\.cs\:114

Changed

```text
S1: Remove only removes the Portfolio; Feature deletion moves to the clean-up
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:17

Changed

```text
S1 to S3: the keep rule and the FeatureWork removal live here
```

### Agreement

None.

### Not applicable

agreement

```text
no shared contract changes: IOrphanedFeatureCleanupService keeps its signature, PortfolioDeleted is a new in-process event with one producer and one consumer in one deployable, no DTO, wire or persisted shape changes
```

contract_tests

```text
producer and consumer of PortfolioDeleted live in one deployable and change in the same commit; covered by the slice oracle and the existing delete host test
```

### Boundaries

```text
IOrphanedFeatureCleanupService.CleanupAsync, called by PortfolioUpdater, the startup clean-up and the PortfolioDeleted handler
```

#### Driven ports

```text
LighthouseAppContext Features and FeatureWork sets
```

```text
IDomainEventDispatcher publishing PortfolioDeleted
```

```text
PortfolioController publishes PortfolioDeleted -> Work Items policy handler -> clean-up service -> EF context; readers FeatureRepository.FeaturesOfTeam and FeaturesWorkedOnBy unchanged
```

#### Failures

None.

### Observations

```text
Stored Features after CleanupAsync: which rows exist, with their Id
```

set of Feature Id and ReferenceId

```text
a row deleted and re-created with the same Id is not possible on SQLite autoincrement, but the observation alone cannot show history loss; transitions and forecasts are observed separately
```

```text
A Team's Features tab: FeatureRepository.FeaturesOfTeam(teamId)
```

IReadOnlyList\<Feature\>

```text
why a Feature is absent (deleted or only its FeatureWork removed)
```

```text
Forecast set: FeatureRepository.FeaturesWorkedOnBy(teamIds), the input of ForecastService.UpdateForecastsForPortfolio
```

IReadOnlyList\<Feature\>

```text
the simulated dates themselves; the order effect follows from set membership
```

```text
A kept parent's history: its FeatureStateTransition and WhenForecast rows by FeatureId
```

row counts per FeatureId

```text
whether the rows are current; only that they survived
```

### Equality

stored Features

```text
Two stores are equal when they hold the same Feature Ids with the same Portfolio memberships and the same FeatureWork rows per Feature; Order, names and forecast values are not part of it
```

### Constructors

None.

### Laws

#### L1

```text
After CleanupAsync every stored Feature is in a Portfolio or is the parent of a Feature in a Portfolio
```

Invariant

Application

OrphanedFeatureCleanupService

##### Treatment

Finite

###### Cases

```text
flagged former Feature, no Portfolio, no child: deleted
```

```text
unflagged Feature in no Portfolio, no child: deleted
```

```text
Feature in a Portfolio: kept
```

```text
parent of a Feature in another Portfolio, itself in none: kept
```

```text
parent whose only child is in no Portfolio: deleted
```

```text
finite shape set; the Quint model covers interleavings
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

#### L2

```text
After CleanupAsync no FeatureWork row belongs to a Feature in no Portfolio
```

Invariant

Application

OrphanedFeatureCleanupService

##### Treatment

Finite

###### Cases

```text
kept parent outside every Portfolio with FeatureWork: rows gone, Feature kept
```

```text
Feature in a Portfolio with FeatureWork: rows unchanged
```

```text
finite shape set
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

#### L3

```text
CleanupAsync never deletes a Feature that is the parent of a Feature in a Portfolio, whatever its IsParentFeature flag; repeated runs keep its Id, transitions and forecasts
```

Invariant

Application

OrphanedFeatureCleanupService

##### Treatment

Finite

###### Cases

```text
unflagged parent of a Portfolio Feature with transitions and a forecast, CleanupAsync run three times: same Id, rows kept
```

```text
finite shape set
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

#### L4

```text
CleanupAsync is idempotent: a second run on a cleaned store deletes nothing and changes nothing
```

Algebraic

Application

OrphanedFeatureCleanupService

##### Treatment

Finite

###### Cases

```text
run twice, second returns 0
```

```text
single example suffices; follows from the invariant
```

##### Evidence

None.

#### L5

```text
Deleting a Portfolio leaves the store as if its memberships were removed and CleanupAsync ran
```

Refinement

Application

PortfolioDeletedOrphanedFeatureCleanupHandler

##### Treatment

Finite

###### Cases

```text
parent whose only Portfolio children were in the deleted Portfolio: deleted at once
```

```text
Feature only in the deleted Portfolio that parents a Feature in another Portfolio: kept
```

```text
finite shape set
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

### Refuted

```text
A Feature with IsParentFeature set is still a parent
```

```text
the flag is written once when the parent half creates the row and never cleared; it is a designation, not the parent relation
```

```text
item stored as a parent, later its type added to the Portfolio query and removed again: flagged, in no Portfolio, no child, FeatureWork kept: stays on the Team tab forever
```

```text
Zeroing FeatureWork removes a Feature from a Team's Features tab
```

```text
FeaturesOfTeam selects by row existence, not by counts
```

```text
Feature.ClearFeatureWork leaves rows with 0/0; FeaturesOfTeam still returns the Feature
```

### Claims

#### C1

```text
No Feature outside every Portfolio is on a Team page or in a forecast after the next clean-up
```

```text
stranded items take Team throughput and push real Features later; stale progress shown
```

OrphanedFeatureCleanupService

```text
any stored Features, memberships and parent links, including legacy flagged rows
```

OrphanedFeatureCleanupService\.CleanupAsync

FeatureRepository\.FeaturesOfTeam and FeaturesWorkedOnBy

```text
Team Features tab and the forecast set exclude the Feature
```

```text
any writer that adds FeatureWork to a Feature in no Portfolio between clean-ups; today only RefreshRemainingWork writes FeatureWork and it loops Portfolio members only
```

### States

Admitted

```text
Feature in at least one Portfolio, with or without FeatureWork
```

WorkItemService

Admitted

```text
Feature in no Portfolio that parents a Feature in a Portfolio, without FeatureWork (name only)
```

OrphanedFeatureCleanupService

Excluded

```text
Feature in no Portfolio that parents no Feature in a Portfolio, after a clean-up
```

OrphanedFeatureCleanupService

Excluded

```text
FeatureWork on a Feature in no Portfolio, after a clean-up
```

OrphanedFeatureCleanupService

Unknown

```text
Window between a Portfolio save and the following clean-up in the same queued update; readers may briefly see the old state
```

PortfolioUpdater

### Models

Quint

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

##### Properties

cleanKeepsOnlyMembersAndParents

Invariant

```text
after a clean-up every stored Feature is in a Portfolio or parents a Portfolio Feature
```

CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

noWorkOutsidePortfolios

Invariant

```text
after a clean-up no Feature outside every Portfolio has FeatureWork
```

CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

neverDeletesANeededParent

Invariant

```text
no clean-up deletes the parent of a Portfolio Feature
```

CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

##### Atoms

Action

cleanup

OrphanedFeatureCleanupService\.CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

```text
two ExecuteDeleteAsync statements, Features then FeatureWork, inside one queued update
```

Action

deletePortfolio

PortfolioController\.DeletePortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:72

```text
repository remove and save, then PortfolioDeleted dispatched in the same queued lambda
```

Action

parentHalf

WorkItemService\.RefreshParentFeatures

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1354

None.

Action

leavePortfolio

Portfolio\.UpdateFeatures

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Portfolio\.cs\:186

None.

State

work

Feature\.FeatureWork

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Feature\.cs\:68

None.

State

flagged

Feature\.IsParentFeature

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Feature\.cs\:77

None.

##### Assumptions

```text
clean-up runs atomically after each refresh, delete and start
```

```text
3 Features, 2 Portfolios, fixed parent chain f1->f2->f3
```

##### Mutants

###### N1

```text
today's rule: keep when flagged (mutants/mutant_flag_decides.qnt), violates cleanKeepsOnlyMembersAndParents
```

###### Kills

None.

###### N2

```text
keep only Portfolio members (mutants/mutant_deletes_parents.qnt), violates neverDeletesANeededParent
```

###### Kills

None.

###### N3

```text
kept parents keep their FeatureWork (mutants/mutant_keeps_parent_work.qnt), violates noWorkOutsidePortfolios
```

###### Kills

None.

### Closure

```text
keep rule
```

Safety

Pass

DesignedNotBuilt

```text
Quint simulation 20000 traces and Apalache bounded check to 5 steps pass; three behavioural mutants killed
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

```text
name-only parent
```

Inhabitation

Pass

DesignedNotBuilt

```text
a Portfolio Feature with ParentReferenceId and its stored parent outside every Portfolio is reachable via parentHalf then leavePortfolio; kept without work
```

##### Evidence

docs\/feature\/bug\-6665\-former\-parents\-stranded\/models\/keepRule\.qnt

```text
Team tab and forecast exclusion
```

PublicReachability

Pass

DesignedNotBuilt

```text
CleanupAsync -> FeatureWork rows gone -> FeaturesOfTeam / FeaturesWorkedOnBy, read by FeatureListService and ForecastService
```

##### Evidence

None.

```text
clean-up
```

Liveness

Pass

DesignedNotBuilt

```text
not applicable: synchronous step at the end of every refresh, delete and start; no eventual-progress claim
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
assumption: the clean-up runs after every refresh (negated: it throws)
```

```text
refresh result saved
```

```text
stranded rows survive until the next successful clean-up or start
```

```text
refresh failing because of the clean-up
```

```text
PortfolioUpdater swallows and logs clean-up failures (non-fatal)
```

PortfolioUpdater

#### Y2

Stressor

```text
assumption: only RefreshRemainingWork writes FeatureWork (negated: a new writer adds work to a non-member)
```

```text
next clean-up removes it
```

```text
one refresh interval of stale work
```

```text
permanent stranded work
```

```text
a future writer must target Portfolio members only
```

WorkItemService

#### Y3

Bypass

```text
foreign writer: a direct SQL edit or restored backup with stranded rows
```

```text
next start or refresh cleans it
```

```text
stale rows until then
```

```text
rows kept forever
```

```text
none beyond the interval
```

OrphanedFeatureCleanupService

#### Y4

Stressor

```text
second set: Portfolio delete raced by a refresh of another Portfolio
```

```text
update queue serialises both; each ends with a clean-up
```

```text
the later clean-up does the work
```

```text
a needed parent deleted
```

```text
walkthrough only, not executed
```

UpdateQueueService

#### Y5

Stressor

```text
second set: child whose ParentReferenceId points at an item stored twice (duplicate ReferenceId rows)
```

```text
both rows kept while the child is in a Portfolio
```

```text
duplicates remain as before
```

```text
deleting the parent the child names
```

```text
duplicate rows are an existing condition, out of scope; walkthrough only
```

WorkItemService

#### Y6

SinglePointOfFailure

```text
CleanupAsync is the one owner of deletion and work removal
```

```text
one rule, one place
```

```text
a failed run is retried at the next refresh or start
```

```text
a second deleter (PortfolioRepository.RemoveOrphanedFeatures) diverging
```

```text
no architecture test can name 'who deletes Features'; review keeps it single
```

OrphanedFeatureCleanupService

### Architectural tests

#### T1

```text
The new PortfolioDeletedOrphanedFeatureCleanupHandler sits in Modules/WorkItems/Application/Policies and the Work Items membership, layout, boundary, test-placement and handler-order lists name it; existing suites fail on an unlisted member
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.WorkItems|FullyQualifiedName~DomainEventHandlerOrderPinTest
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs

Extend

```text
keep rule: in a Portfolio or flagged parent of a Portfolio Feature
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Events\/PortfolioDeleted\.cs

Create

```text
domain event record PortfolioDeleted(int PortfolioId)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandler\.cs

Create

```text
Work Items reaction: run the clean-up
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
publish PortfolioDeleted after the save in the queued delete
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
register the handler beside the TeamDeleted handlers
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/PortfolioRepository\.cs

Extend

```text
Remove stops deleting Features
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrphanedFeatureCleanupServiceTests|FullyQualifiedName~PortfolioDeletedOrphanedFeatureCleanupHandlerTest|FullyQualifiedName~PortfolioControllerTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.WorkItems|FullyQualifiedName~DomainEventHandlerOrderPinTest|FullyQualifiedName~PortfolioDeleteSerialisationTests
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

#### Public oracle

```text
In OrphanedFeatureCleanupServiceTests (in-memory SQLite): seed a Team, a Portfolio holding Feature C, and a former Feature X flagged IsParentFeature, in no Portfolio, parent of no Portfolio Feature, with FeatureWork for the Team and a WhenForecast; call IOrphanedFeatureCleanupService.CleanupAsync. In PortfolioDeletedOrphanedFeatureCleanupHandlerTest: seed Portfolio P with Feature C whose parent is flagged item X (in no Portfolio) and a Feature only in P; PortfolioRepository.Remove(P) and Save; handle PortfolioDeleted(P).
```

```text
X is deleted with its FeatureWork and forecast; FeaturesOfTeam(team) no longer returns X; C is kept. After the delete, X and P's sole Feature are gone at once without another refresh.
```

```text
Today's predicate (!IsParentFeature && no Portfolio) keeps X; without the handler X survives the Portfolio delete
```

##### Promises

P2

P3

P6

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
S1 changes the clean-up predicate to: keep iff in a Portfolio, or (IsParentFeature and ParentsAPortfolioFeature). The flag conjunct is a deliberate intermediate that preserves today's behaviour for unflagged parents; S3 removes it. S1 also adds PortfolioDeleted, its Work Items handler and registration, publishes it from PortfolioController.DeletePortfolio, and removes PortfolioRepository.RemoveOrphanedFeatures.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
the changes are a predicate edit, one removed method and two new types; no existing structure needs moving first
```

reuse

```text
The two created files, Models/Events/PortfolioDeleted.cs and Modules/WorkItems/Application/Policies/PortfolioDeletedOrphanedFeatureCleanupHandler.cs, do not exist yet, so no locator resolves; their CreateNew rationale is recorded against TeamDeleted.cs:3 and TeamDeletedRefreshLogCleanupHandler.cs:8, whose shape they follow, because no Portfolio-deletion event or handler exists.
```

impact

```text
The two created files have no consumers before this slice; their only consumers are the PortfolioController publish and the Program.cs registration, both recorded as Changed.
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs

Extend

```text
remove FeatureWork of Features in no Portfolio
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrphanedFeatureCleanupServiceTests
```

#### Supports

None.

#### Public oracle

```text
Seed Team T, Portfolio P2 holding Feature C with ParentReferenceId = X, Feature R in P2 with FeatureWork for T, and X flagged, in no Portfolio, with FeatureWork for T; call CleanupAsync; read FeatureRepository.FeaturesOfTeam(T) and FeaturesWorkedOnBy([T]) over the same database
```

```text
X still exists (C's parent name resolves); X has no FeatureWork rows; neither reader returns X; R and C keep their FeatureWork and R is returned
```

```text
After S1 alone X keeps its FeatureWork and both readers return it, ahead of R in the order
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

```text
S2 adds the second statement to CleanupAsync: delete every FeatureWork row whose Feature is in no Portfolio, after the Feature delete. Forecast rows are left alone.
```

##### Constraints

None.

##### Reuse

CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

Extend

```text
same owner removes remaining work outside every Portfolio
```

##### Impact

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:17

Changed

```text
adds removal of FeatureWork rows of Features in no Portfolio
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
one added set-based statement in a method S1 already reshaped
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs

Extend

```text
drop the flag from the keep rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
comment no longer names the flag as the clean-up rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrphanedFeatureCleanupServiceTests
```

#### Supports

None.

#### Public oracle

```text
Seed Portfolio P with Feature C whose ParentReferenceId = Y; Y stored unflagged, in no Portfolio, with two FeatureStateTransitions and a WhenForecast; call CleanupAsync three times
```

```text
Y keeps the same Id after every run and its transitions and forecast rows remain; each run after the first returns 0
```

```text
With the flag conjunct, the first run deletes Y and cascades its transitions and forecast
```

##### Promises

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
S3 removes the IsParentFeature conjunct, so the rule is exactly: in a Portfolio or parent of a Portfolio Feature. It rewords the WorkItemService comment above TheSweepDidNotVouchForThisParent, which still names the flag as the clean-up rule.
```

##### Constraints

None.

##### Reuse

CleanupAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:12

Extend

```text
same owner; the flag conjunct goes
```

TheSweepDidNotVouchForThisParent

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1484

Extend

```text
only the comment above it changes, so it no longer names the flag as the clean-up rule
```

##### Impact

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OrphanedFeatureCleanupService\.cs\:17

Changed

```text
keep rule no longer reads IsParentFeature
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1481

Changed

```text
comment reworded; no behaviour change
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
removes one conjunct and rewords one comment
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
