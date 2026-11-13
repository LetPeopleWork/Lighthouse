# Architecture brief: epic\-6473\-settings\-split

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Style: existing frontend layering (pages over components/hooks/models/services; fallow zones) on the backend ports-and-adapters. Frontend only: no backend, API, payload or schema change; no new dependency. Paradigm object_oriented per project authority; React components and hooks keep their existing shape, no new classes.
```
Governed slices: S1, S2, S3, S4, S5

#### D2

```text
Addressing: a setting address is a path /settings/:area/:tab? with areas organization, access, system; License is /settings/system/license; connection pages live at /settings/system/connections/new and /settings/system/connections/:id/edit. One data-only definition models/Settings/settingsAreas.ts (areas, tabs with slug, label, test ids, visibility rule, earlier slugs; the 14 legacy ?tab= keys; connection page paths) is the single owner of every settings and connection path; Header, stepper, Task Manager, links and POM expectations take paths from it. It sits in models so components may import it.
```
Governed slices: S1, S2, S3, S4, S5

#### D3

```text
Resolver: one pure function resolveSettingsLocation(request, access) returning Show(area, tab) | Redirect(path) | Wait, where access is (isLoading, isSystemAdmin, isRbacEnabled) read from useRbac(). It never decides while isLoading; bare /settings lands on organization for a System Admin, else on the first visible area; an unknown, hidden or earlier slug falls back or redirects with replace. A failed access read keeps useRbac() permissive answer (isRbacEnabled false, isSystemAdmin true): the person sees what RBAC-off shows today, which excludes System-wide access as the old page excluded System Admins. Carrier of decisions: the Show/Redirect/Wait choice type in models/Settings. Effects (navigate replace, spinner) stay in the shell and settingsRoutes.
```
Governed slices: S1, S2, S3, S4, S5

#### D4

```text
Routes: pages/Settings/settingsRoutes.tsx returns the settings route elements (lazy pages, reportPrintRoutes pattern) and the static redirects (/settings/license, /connections/new, /connections/:id/edit as Navigate replace); App.tsx calls it once inside AppShell and loses its /settings and /connections routes. Connection pages render outside the area resolver, so a reader still gets the connection page no-access alert.
```
Governed slices: S1, S2, S3, S4, S5

#### D5

```text
Shell: SettingsAreaPage extracted from Settings.tsx and parameterised by area (same Container, h1 Settings > {Area}, Paper, TabList, TabPanel, icons); lone spinner while resolver says Wait; tab click navigates with replace. Panels are bound to tab slugs in pages/Settings/settingsPanels.tsx. Header SettingsMenu (text trigger Settings with down arrow; items Organization/Access/System each with a one-line description; mobile drawer caption plus three indented items) reads the definition and useRbac(). Tab Access/System Admins is labelled System-wide access with the old slug kept as an earlier slug.
```
Governed slices: S1, S2, S3, S4, S5

#### D6

```text
Server state: new keyed TanStack query hooks own the lists (useBehaviourSettings for optionalFeatures plus the shared useLicenseStatusQuery; useConfiguredConnections for configuredConnections). Writes update or remove the key so a remount never shows the old value as current. No component adds a direct license-status asker; licenseStatusOneAsker allow-list loses SystemSettingsTab.tsx when the file is deleted. Every new view has a loading look (lone spinner) and a failure look (inline alert, 403 keeps the refused alert), never the empty state.
```
Governed slices: S2, S3, S4

#### D7

```text
Claim-to-checker: Quint covers the resolver safety claims (no decision while access loads; shown or redirected area is visible; admin bare address lands on organization): model design/models/settingsResolver.qnt, typecheck, quint run and quint verify executed by the architect. TLA+/TLC and Agda are not selected: no concurrency or proof obligation beyond the finite resolver. Liveness is not applicable: the resolver is a pure synchronous total function and the Wait outcome is left by the access read, which is the existing useRbac() effect outside this design. Executable projection of the model is the resolver table and seeded property test in the S1 oracle.
```
Governed slices: S1, S2, S3, S4, S5

#### D8

```text
Types: TypeScript strict via tsc -b. Invalid states excluded by construction: tab slugs and area slugs are literals from the definition (a path built by hand fails the source-scan test, not the type checker); the resolver takes three plain booleans so tests need no hook. Not excluded by types: a hand-built path from a variable (accepted, review catches it), and the DOM test ids (kept in step by e2eTestIds.test.ts).
```
Governed slices: S1, S2, S3, S4, S5

#### D9

```text
OO reading: no domain entity changes; the decision owners are the definition and resolver in models (data plus its pure decision), the shell for presentation, hooks for server state. No decision about the definition data lives in a component or in the Header; components only ask the resolver and definition. Smell classes applied from nw-code-design-oo: feature envy on route literals (moved to the definition), shotgun surgery on paths (one owner), long components (extract section from OverviewDashboard).
```
Governed slices: S1, S2, S3, S4, S5

#### D10

```text
Usage data: the feature adds no usage-data event; settings and connection paths raise no usage-data page opening (usageDataRouteKeys unchanged). Context map, ADR-270 and ARCHITECTURE.md unchanged; ADR-272 records addressing, definition, resolver, redirects and the source-scan enforcement and moves to docs/product/architecture at finalize.
```
Governed slices: S1, S2, S3, S4, S5

#### D11

```text
Terminology: every user-renamable word (Work Tracking System(s), Team, Portfolio, Feature) renders through the configured term via the hook the touched component already uses.
```
Governed slices: S1, S2, S3, S4, S5

#### D12

```text
Test substrate: Vitest + React Testing Library in Lighthouse.Frontend (MemoryRouter at the URL a user opens, renderHook with providers, pure modules as functions); Playwright through page objects in Lighthouse.EndToEndTests. Runner argv: pnpm --dir Lighthouse.Frontend exec vitest run <oracle file>. Test rules follow .claude/skills/test-writing; no nWave scenario tags in code. Test time within default budgets; no derogation requested.
```
Governed slices: S1, S2, S3, S4, S5

### Constraints

#### K1

```text
Scope out: any change to what a setting does or stores; any backend change; levels (Epic 6474); connection mapping UI; docs prose and screenshots (finalize). Gating stays exactly as today: a failed access read gets useRbac() permissive answer (RBAC off, System Admin), so nobody is locked out and the System-wide access tab stays hidden as today. No component fetches my-summary directly; all gating derives from useRbac(). A fallback redirect resolves only after access has loaded. Input bounds: legacy ?tab= keys are exactly the 14 the old page accepted, matched case-insensitively; no new bound on stored data.
```
Governed slices: S1, S2, S3, S4, S5

#### K2

```text
Old addresses keep working with replace redirects: the 14 legacy keys, bare /settings, /settings/license, /connections/new, /connections/:id/edit, any earlier tab slug (the old System Admins slug stays). Tab and panel test ids are kept where their tab survives; new ids license-tab, connections-tab, usage-data-tab, terminology-tab, calendar-tab, refresh-ordering-tab, settings-menu plus one id per area item; configuration-tab and configuration-panel retire in S2.
```
Governed slices: S1, S2, S3, S4, S5

#### K3

```text
Confirmed maintainer decisions bound the copy: D1-D4 header and heading, D5 System-wide access, D6 tab order Connections, Database, Encryption, Demo Data, Usage Data, System Info, License (in S1 alone the first tab stays Demo Data), D7, D8 icons Translate/EventBusy/Sync, D9 Could not load the license., D10 no footer sentence on Usage Data, D11 License natural width in one License group, D12 link text View license options, D13 Task Manager link and empty-text link and configured terms, D13a-wording Connect {Work Tracking System} on the tab button and the stepper action, D13b title, D14 Back to {Work Tracking Systems} link above the title of the connection pages, D15 hint line under the disabled Add buttons; D12b nothing extra when the stepper is dismissed.
```
Governed slices: S1, S2, S3, S4, S5

### Reuse

None.

### Impact

None.

### Agreement

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Producer

Migrated

```text
New single owner of every settings and connection path; replaces the tab key map in Settings.tsx and hand-written literals
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/App\/Header\/Header\.tsx\:66

Consumer

Migrated

```text
Plain System Settings link becomes the SettingsMenu reading the definition
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/RecentProblemsSection\.tsx\:14

Consumer

Migrated

```text
full log link /settings?tab=system-info moves to the definition path for System Info
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:354

Consumer

Migrated

```text
demo-data link takes the definition path
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:499

Consumer

Migrated

```text
dead /settings/license link takes the definition path (System in S1, License in S3)
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:878

Consumer

Migrated

```text
dead /settings/license link takes the definition path; the notice may have moved to OAuthPremiumNotice.tsx when it runs
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/Common\/OnboardingStepper\/OnboardingStepper\.tsx\:90

Consumer

Migrated

```text
stepper opens the connect page path from the definition (S5)
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:350

Consumer

Migrated

```text
row edit links and demo-data link move with the extracted list (S4) and take definition paths (S5)
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/ConnectionsSection\.tsx\:98

Consumer

Migrated

```text
row Edit takes the definition path; section gains the manage link (S5)
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx\:137

Consumer

Migrated

```text
Save, wizard cancel and wizard complete (l.137, 180, 181) return to the Connections tab path instead of /
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.Frontend\/src\/App\.tsx\:283

Consumer

Migrated

```text
settings and connection routes come from settingsRoutes()
```

Settings and connection addresses \(paths\, tab and area slugs\, legacy keys\)

Lighthouse\.EndToEndTests\/tests\/models\/app\/LighthousePage\.ts\:93

Consumer

Migrated

```text
POM goToSettings and SettingsPage.goTo* open the header menu, then the tab by its unchanged test id; specs keep their calls
```

Tab and panel test ids

Lighthouse\.Frontend\/src\/tests\/e2eTestIds\.test\.ts\:1

Consumer

UnchangedCompatible

```text
Existing ids kept where a tab survives; new ids added to the app and the POMs together; configuration ids retire in S2
```

Tab and panel test ids

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Consumer

Migrated

```text
POM reads the same ids; new ids for new tabs
```

### Not applicable

contract_tests

```text
Producer and consumers of every contract here (the path definition, hooks, existing services) change in one deployable in one commit; no API, payload or persisted data changes; covered by the slice oracles and the source scan.
```

### Boundaries

```text
In-app links: stepper, Add Team hint, Task Manager, demo-data links, license links, row Edit
```

#### Driven ports

```text
IRbacService.getAuthorizationSummary via useRbac()
```

```text
IWorkTrackingSystemService via ApiServiceContext
```

```text
ILicensingService.getLicenseStatus via useLicenseStatusQuery
```

```text
IOptionalFeatureService via useBehaviourSettings
```

```text
pages -> components/hooks/models/services; components may import models but not pages; the definition in models imports nothing from them; panels moved unchanged keep their own service ports
```

#### Failures

##### B1

```text
Access read fails
```

Indeterminate

```text
permissive answer as today: every area and tab RBAC-off shows, without System-wide access
```

##### B2

```text
Connection list or license or optional-feature call fails
```

Indeterminate

```text
inline could-not-load alert in the view, 403 shows the refused alert, never the empty state or a locked switch
```

##### B3

```text
Person may not see the requested area or tab
```

Refusal

```text
redirect with replace to the first visible area or tab
```

### Observations

```text
Rendered location: area h1, visible tab labels in order, selected tab and panel at a given URL and access
```

DOM under MemoryRouter

```text
which internal route element produced it; whether the URL was reached by redirect or directly except through the final location
```

```text
Resolver outcome for (request, access)
```

Show\(area\,tab\) \| Redirect\(path\) \| Wait

```text
the reason a hidden tab fell back (hidden vs unknown slug)
```

```text
Final location after redirect (pathname and absence of ?tab=)
```

string path

```text
redirect count; replace vs push except through history length
```

### Equality

resolver outcome

```text
Two requests are equal when they resolve to the same Show(area,tab) for the same access; a legacy key in any case equals its table target path
```

rendered location

```text
Equal when final pathname, h1, tab labels and selected panel agree
```

### Constructors

settingsAreas

readonly data\: areas \-\> tabs\(slug\,label\,testIds\,visibleWhen\,earlierSlugs\)\; legacyKeys\: 14 \-\> path\; connectionPaths

resolveSettingsLocation

\(request\: \{area\?\: string\, tab\?\: string\, legacyKey\?\: string\}\, access\: \{isLoading\: boolean\, isSystemAdmin\: boolean\, isRbacEnabled\: boolean\}\) \-\> Show \| Redirect \| Wait

### Laws

#### L1

```text
Resolver safety: while access is loading the outcome is Wait; once loaded a Show or Redirect target is an area and tab the person may see; an admin on a bare address is sent to organization
```

Invariant

Application

resolveSettingsLocation

##### Treatment

Finite

###### Cases

```text
loading x any request -> Wait
```

```text
loaded reader x organization -> redirect to access
```

```text
loaded admin x bare /settings -> organization
```

```text
RBAC off x any request -> permissive, all areas
```

```text
Finite domain: 3 access booleans x 5 request classes; also checked by the Quint model settingsResolver.qnt
```

##### Evidence

design\/models\/settingsResolver\.qnt

#### L2

```text
Legacy table: each of the 14 old ?tab= keys, in lower and upper case, redirects to the area and tab listed in the feature-delta Navigation table for an admin; configuration lands on Terminology; the old System Admins slug opens System-wide access
```

Algebraic

Domain

settingsAreas

##### Treatment

Finite

###### Cases

```text
each of 14 keys x two cases
```

```text
earlier slug of the renamed Access tab
```

```text
Closed 14-key table: a native table test, no generator adds coverage
```

##### Evidence

None.

#### L3

```text
Convergence and round trip: for any request the resolver outcome is Show, Wait or a Redirect whose target, resolved again with the same access, is Show; every tab path built from the definition resolves to Show of that tab
```

RoundTrip

Domain

resolveSettingsLocation

##### Treatment

Broad

###### Generator

```text
requests from definition slugs, earlier slugs, the 14 legacy keys in random case, and junk strings; access over all 8 boolean triples
```

junk strings length at most 12\; 500 cases

fixed seed in the test\, printed on failure

###### Must include

```text
a hidden tab for a reader
```

```text
an earlier slug
```

```text
an unknown area
```

None.

```text
seeded hand-rolled generator (no property library installed); shrink junk strings by truncation and request by dropping the tab part
```

```text
second resolution of the redirect target and the definition tab list, not the resolver own branch logic
```

```text
a redirect whose target is itself redirected, or a hidden tab shown
```

##### Evidence

None.

#### L4

```text
Source scan: no production module other than the definition and settingsRoutes contains a string or template literal starting with /settings or /connections
```

Invariant

Application

settingsAreas

##### Treatment

Finite

###### Cases

```text
whole src tree scanned
```

```text
Whole-tree source scan, not a generated domain
```

##### Evidence

None.

### Refuted

```text
A failed or slow access read can be resolved like a loaded one
```

```text
useRbac() starts with a permissive summary and isLoading true, so a loaded-looking admin summary exists before the answer
```

```text
deep link /settings/access/system-admins at isLoading true bounces to the first tab on the old page; the resolver carries isLoading as its own input and returns Wait
```

### Claims

#### C1

```text
A deep link to any settings tab never redirects before access has loaded and never lands on a tab the person may not see
```

```text
broken bookmarks and wrong-area first impressions
```

resolveSettingsLocation

```text
request path, optional legacy key, three access booleans
```

resolveSettingsLocation

SettingsAreaPage and settingsRoutes

```text
rendered location at a URL in MemoryRouter
```

```text
a link whose path is built from a variable; an outside writer that changes useRbac summary after resolution
```

#### C2

```text
Every in-app link and POM expectation takes its settings or connection path from the definition
```

```text
dead links like the old /settings/license
```

settingsAreas

```text
tab and area slugs, connection id
```

settingsAreas path helpers

Header\, OnboardingStepper\, TaskManager\, DataOverviewTable\, wizard and connection pages\, E2E POMs

```text
source-scan test plus rendered link hrefs
```

```text
path built from a variable; docs and bookmarks outside the repo
```

### States

Admitted

```text
Any of 8 access boolean triples, including the permissive failed-read fallback
```

resolveSettingsLocation

Excluded

```text
Show or Redirect while isLoading; a Redirect target that is hidden for the person; a Redirect loop
```

resolveSettingsLocation

Unknown

```text
Header and shell each read access once, so they may differ for a moment (neither shows a gated item early); a person whose access changes after load
```

useRbac

### Models

Quint

docs\/feature\/epic\-6473\-settings\-split\/design\/models\/settingsResolver\.qnt

##### Properties

noDecisionWhileLoading

Invariant

```text
While loading the outcome is pending or Wait
```

resolveSettingsLocation

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts

targetVisible

Invariant

```text
A shown or redirected area is visible to the person
```

resolveSettingsLocation

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts

adminBareLandsOnOrganization

Invariant

```text
An admin on a bare address is redirected to organization
```

resolveSettingsLocation

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts

##### Atoms

State

loading

isLoading

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

None.

State

admin

isSystemAdmin

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

None.

Action

resolve

resolveSettingsLocation

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts

None.

Action

accessLoaded

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

None.

##### Assumptions

```text
Areas abstracted to integers; access is the pair (loading, admin); organization is admin-only, access and system are visible to everyone
```

##### Mutants

###### N1

```text
Resolver ignores isLoading and decides on the permissive summary
```

###### Kills

None.

### Closure

```text
resolver safety claims
```

Safety

Pass

DesignedNotBuilt

```text
Quint typecheck, quint run and quint verify (8 steps) on settingsResolver.qnt hold; mutant without the loading check is refuted by quint run; runtime resolver not yet built
```

##### Evidence

None.

```text
Show for every valid tab path
```

Inhabitation

Pass

DesignedNotBuilt

```text
definition tab -> path helper -> resolveSettingsLocation -> SettingsAreaPage -> rendered tab; acyclic, admitted input is the URL
```

##### Evidence

None.

```text
settings and connection URLs and menu entries
```

PublicReachability

Pass

DesignedNotBuilt

```text
URL or Header menu item -> settingsRoutes/SettingsMenu -> SettingsAreaPage in MemoryRouter; new files named as slice targets
```

##### Evidence

None.

```text
resolver and redirects
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: pure synchronous total function; Wait ends when the existing useRbac() read completes, outside this design; a failed read yields the permissive fallback, never an endless Wait
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: access loads quickly. Negation: my-summary is slow
```

```text
deep link opens the requested tab once access loads
```

```text
spinner meanwhile
```

```text
redirect before load; first-tab bounce
```

```text
none beyond the spinner
```

resolveSettingsLocation

#### Y2

Stressor

```text
Assumption: my-summary succeeds. Negation: it fails
```

```text
nobody is locked out
```

```text
what RBAC-off shows today (all three areas, every tab except System-wide access); backend refuses what the person may not do
```

```text
a blank or endless spinner page
```

```text
a reader sees admin tabs whose calls the backend refuses
```

useRbac

#### Y3

Stressor

```text
Assumption: old links are only the 14 keys. Negation: bookmarks use odd case, earlier slugs or unknown tabs
```

```text
an old link opens the same setting or the first visible tab
```

```text
fallback to first visible tab for an unknown slug
```

```text
blank page or redirect loop
```

```text
external docs and screenshots naming old URLs (finalize)
```

settingsAreas

#### Y4

SinglePointOfFailure

```text
settingsAreas.ts is the one owner of paths
```

```text
every link resolves
```

```text
a wrong entry is caught by the source scan and POM tests
```

```text
a second hand-written path list
```

```text
a path built from a variable passes the scan; review owns it
```

settingsAreas

#### Y5

Bypass

```text
Direct URL typed or old bookmark skips the menu
```

```text
same gating as via the menu
```

```text
redirect with replace
```

```text
a hidden tab shown
```

```text
connection pages sit outside the resolver and use their own no-access alert
```

EditConnection

#### Y6

Stressor

```text
Second set: list call fails, license call fails, tab opened in a worktree with stale cache, two tabs of the browser changing a connection
```

```text
no view shows an old or empty value as current
```

```text
spinner, inline could-not-load alert, refused alert on 403
```

```text
empty state after a failure; locked switch for an unreadable license
```

```text
a second browser tab may show a list up to the 5-minute stale time until remount; walked through the keyed-query writes, not executed
```

useConfiguredConnections

### Architectural tests

#### T1

```text
No production module other than settingsAreas.ts and settingsRoutes.tsx holds a string literal starting with /settings or /connections; no new direct getLicenseStatus asker; components do not import pages
```

Lighthouse\.Frontend\/src\/models\/Settings\/settingsPaths\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/models/Settings/settingsPaths.enforcement.test.ts src/hooks/licenseStatusOneAsker.enforcement.test.ts src/layerBoundaries.enforcement.test.ts
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts

Create

```text
single definition of areas, tabs, legacy keys, paths
```

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts

Create

```text
pure resolver Show/Redirect/Wait
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx

Create

```text
route elements and static redirects
```

Lighthouse\.Frontend\/src\/pages\/Settings\/SettingsAreaPage\.tsx

Create

```text
parameterised shell extracted from Settings.tsx
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx

Create

```text
tab slug to panel binding
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Settings\.tsx

Extend

```text
shell content moves to SettingsAreaPage; file reduced or removed
```

Lighthouse\.Frontend\/src\/components\/App\/Header\/SettingsMenu\.tsx

Create

```text
text trigger menu and drawer group
```

Lighthouse\.Frontend\/src\/components\/App\/Header\/Header\.tsx

Extend

```text
nav link replaced by SettingsMenu
```

Lighthouse\.Frontend\/src\/App\.tsx

Extend

```text
settingsRoutes() mounted once; old /settings and /connections routes removed in S5 for connections
```

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/RecentProblemsSection\.tsx

Extend

```text
path from definition
```

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx

Extend

```text
demo-data path from definition
```

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx

Extend

```text
demo-data link path from definition
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx

Extend

```text
license link path from definition
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx

Extend

```text
license link path from definition (or OAuthPremiumNotice.tsx if it has landed)
```

Lighthouse\.EndToEndTests\/tests\/models\/app\/LighthousePage\.ts

Extend

```text
goToSettings opens the header menu
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts

Extend

```text
goTo* pick the area then the tab by test id
```

Lighthouse\.EndToEndTests\/tests\/models\/auth\/rbac\/RbacSettingsPage\.ts

Extend

```text
goToAccessTab through the same path; same-pathname workaround removed
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/settingsAreas.test.tsx src/models/Settings/settingsPaths.enforcement.test.ts
```

#### Supports

Lighthouse\.Frontend\/src\/models\/Settings\/settingsPaths\.enforcement\.test\.ts

#### Public oracle

```text
Render the app routes in a MemoryRouter at /settings, /settings?tab=database, /settings?tab=LOGS, /settings/access/system-admins (RBAC on, access slow) and for a reader at /settings; open the header Settings menu
```

```text
Menu lists Organization, Access, System; area page h1 is Settings > {Area}; each old key opens the area and tab of the Navigation table; deep link shows a spinner then System-wide access; the reader sees Access and System without Organization; license links land on System
```

```text
First-tab bounce of the deep link, a legacy key landing elsewhere, Organization visible to a reader, or old h1 System Settings
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
S1 is the walking skeleton: areas Organization (Configuration whole), Access (System-wide access, API Keys), System (Demo Data first, Database, Encryption, System Info) hold today panels unchanged; header menu, h1 Settings > {Area}, 14 legacy keys, bare, unknown and license redirects, in-app link paths, POM menu navigation. System Admins panel moves as is including the Epic 6577 content. Existing Settings.test.tsx (mocks SystemSettingsTab, asserts old h1) moves with the shell; owner is the acceptance designer.
```

##### Constraints

None.

##### Reuse

Settings

Lighthouse\.Frontend\/src\/pages\/Settings\/Settings\.tsx\:49

Extend

```text
Shell (heading, Paper, TabList, panels, icons) is extracted and parameterised by area; visibility rules and key map move to the definition
```

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:35

Reuse

```text
Only access source; resolver takes its three facts
```

NavigationItem

Lighthouse\.Frontend\/src\/components\/App\/Header\/NavigationItem\.tsx\:1

Reuse

```text
Drawer entries for the three areas
```

reportPrintRoutes

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/reportPrint\.enforcement\.test\.ts\:1

Reuse

```text
Pattern precedent for a routes module with lazy pages; no existing owner of the settings address contract (CreateNew for the definition and resolver)
```

LighthousePage

Lighthouse\.EndToEndTests\/tests\/models\/app\/LighthousePage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

RbacSettingsPage

Lighthouse\.EndToEndTests\/tests\/models\/auth\/rbac\/RbacSettingsPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

SettingsPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

App

Lighthouse\.Frontend\/src\/App\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

Header

Lighthouse\.Frontend\/src\/components\/App\/Header\/Header\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

SettingsMenu

Lighthouse\.Frontend\/src\/components\/App\/Header\/SettingsMenu\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

RecentProblemsSection

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/RecentProblemsSection\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

CreateConnectionWizard

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

ModifyConnectionSettings

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

DataOverviewTable

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

resolveSettingsLocation

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsAreas

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

OverviewDashboard

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

SettingsAreaPage

Lighthouse\.Frontend\/src\/pages\/Settings\/SettingsAreaPage\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsPanels

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsRoutes

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

##### Impact

public route

Lighthouse\.Frontend\/src\/App\.tsx\:283

Changed

```text
/settings exact route replaced by nested area routes and redirects
```

test using old shell

Lighthouse\.Frontend\/src\/pages\/Settings\/Settings\.test\.tsx\:1

Migrated

```text
Mocks SystemSettingsTab and asserts old h1; moves to the area shell
```

e2e page objects

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Changed

```text
Menu then tab navigation; method names and specs unchanged
```

panels moved unchanged

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

UnchangedCompatible

```text
Panel content and test id rbac-tab unchanged; only label and slug in the definition change
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/app\/LighthousePage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/auth\/rbac\/RbacSettingsPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/App\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/App\/Header\/Header\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/App\/Header\/SettingsMenu\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/RecentProblemsSection\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/resolveSettingsLocation\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/SettingsAreaPage\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/Settings\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The shell extraction is the slice behaviour change (areas and routes), not a behaviour-preserving move with its own existing oracle
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

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/TerminologyTab\.tsx

Create

```text
composes TerminologyConfiguration
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/CalendarTab\.tsx

Create

```text
composes BlackoutSettings with license looks
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/RefreshAndOrderingTab\.tsx

Create

```text
composes RefreshSettingUpdater and the ordering behaviour row
```

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/UsageDataTab\.tsx

Create

```text
usage-data behaviour row and docs link
```

Lighthouse\.Frontend\/src\/hooks\/useBehaviourSettings\.ts

Create

```text
keyed optionalFeatures query with shared license query
```

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/SystemSettingsTab\.tsx

Extend

```text
emptied then deleted; its four test files move with the tabs
```

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/BlackoutSettings\.tsx

Extend

```text
accepts an unknown license state so Premium-gated controls never read unknown as no Premium
```

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts

Extend

```text
Organization three tabs, Usage Data tab, configuration legacy key to Terminology
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx

Extend

```text
bind new tabs
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SystemSettings\/SystemConfigurationPage\.ts

Extend

```text
split into Terminology, Calendar, Refresh & ordering page objects
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts

Extend

```text
goTo for the new tabs
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/Organization/organizationTabs.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render /settings/organization, /settings/organization/refresh-ordering, /settings/system/usage-data and /settings?tab=configuration in a MemoryRouter with a fake optional-feature service and license service: ready, loading, failing
```

```text
Organization shows Terminology, Calendar, Refresh & ordering with D8 icons; no usage-data row there; Usage Data tab holds the switch and docs link; failing optional-feature call shows Could not load the behaviour settings.; unreadable license shows the could-not-load alert instead of a locked switch or Premium required; configuration key lands on Terminology; toggle then leave and return shows the new value
```

```text
An empty tab or locked switch after a failed call, usage-data row left in Organization, Premium-required shown while the license is unknown, or stale switch after return
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
S2 splits SystemSettingsTab into TerminologyTab, CalendarTab, RefreshAndOrderingTab (Organization) and UsageDataTab (System, row plus docs link only), composing existing InputGroup sections unchanged. useBehaviourSettings owns keyed query optionalFeatures plus useLicenseStatusQuery and returns loading | failed | ready rows; rows are matched by key never by id; toggle writes the cache first and restores on refusal. Calendar shows a spinner then Could not load the license. in place of Premium required while the license is unknown. SystemSettingsTab.tsx and its four test files move; its allow-list entry leaves licenseStatusOneAsker.
```

##### Constraints

None.

##### Reuse

SystemSettingsTab

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/SystemSettingsTab\.tsx\:18

Replace

```text
Four unrelated groups in one component; split into four thin tabs, panels reused
```

BehaviourSettingsTable

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/BehaviourSettingsTable\.tsx\:25

Reuse

```text
Reused with a list filtered by key
```

useLicenseStatusQuery

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:35

Reuse

```text
Single license asker; tabs read through it, no new asker
```

BlackoutSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/BlackoutSettings\.tsx\:69

Extend

```text
Owns the Premium lock; extended with a license-unknown input rather than deciding it in CalendarTab
```

SettingsPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

SystemConfigurationPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SystemSettings\/SystemConfigurationPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

useBehaviourSettings

Lighthouse\.Frontend\/src\/hooks\/useBehaviourSettings\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsAreas

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

CalendarTab

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/CalendarTab\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

RefreshAndOrderingTab

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/RefreshAndOrderingTab\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

TerminologyTab

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/TerminologyTab\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsPanels

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

UsageDataTab

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/UsageDataTab\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

##### Impact

license asker allow\-list

Lighthouse\.Frontend\/src\/hooks\/licenseStatusOneAsker\.enforcement\.test\.ts\:16

Changed

```text
SystemSettingsTab.tsx leaves the list in the commit that deletes it
```

tests of removed component

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/SystemSettingsTab\.test\.tsx\:1

Migrated

```text
Four SystemSettingsTab test files move to the new tabs
```

test ids

Lighthouse\.Frontend\/src\/tests\/e2eTestIds\.test\.ts\:1

Changed

```text
configuration-tab and configuration-panel retire; new tab ids added with the POMs
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SystemSettings\/SystemConfigurationPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/hooks\/useBehaviourSettings\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/CalendarTab\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/RefreshAndOrderingTab\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/TerminologyTab\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/BlackoutSettings\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/SystemSettingsTab\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/UsageDataTab\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The split is the behaviour change (tabs, loading and failure looks); panels stay unchanged inside it
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

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusContent\.tsx

Create

```text
content extracted from the popover
```

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusPopover\.tsx

Extend

```text
thin Popover around LicenseStatusContent
```

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.tsx

Create

```text
thin tab with useLicenseStatusQuery
```

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts

Extend

```text
License tab last in System; license redirect target
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx

Extend

```text
bind LicenseTab
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx

Extend

```text
/settings/license redirects to /settings/system/license
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx

Extend

```text
license link target is the License tab
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx

Extend

```text
license link target is the License tab (or OAuthPremiumNotice.tsx if FE5 landed)
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts

Extend

```text
goToLicense
```

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/System/LicenseTab.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render /settings/system/license and /settings/license as System Admin, as reader, with a failing license call; follow the license link from the connection wizard and the connection form; import a license file
```

```text
System License tab shows status, organization, expiry; admin sees import and clear, reader neither; failing call shows Could not load the license.; both links land on System, License; after import the header icon shows the new expiry
```

```text
Blank page or System Info after the link, import and clear shown to a reader, or header icon stale after import
```

##### Promises

P3

#### Prefactoring

```text
L2 extract LicenseStatusContent from LicenseStatusPopover, popover renders it unchanged
```

```text
popover status, import, clear, loading and failure behaviour
```

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusPopover\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/LicenseStatus/LicenseStatusPopover.test.tsx src/components/Common/LicenseStatus/LicenseStatusIcon.test.tsx
```

#### Records

##### Decisions

```text
S3 extracts LicenseStatusContent from LicenseStatusPopover (popover becomes a thin Popover around it) and mounts it in LicenseTab inside one InputGroup License at natural width. Import and clear invalidate the licenseStatus query so the header icon updates from the same asker; no new asker. /settings/license redirects to /settings/system/license; the two License links keep the text View license options and take the definition path. Rights are the popover rights.
```

##### Constraints

None.

##### Reuse

LicenseStatusPopover

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusPopover\.tsx\:721

Extend

```text
Owns import, clear, status and its looks; content extracted so popover and tab share one view, not a second license card
```

useLicenseStatusQuery

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:35

Reuse

```text
licenseStatusOneAsker stays green unchanged
```

SettingsPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

CreateConnectionWizard

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

ModifyConnectionSettings

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

LicenseStatusContent

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusContent\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsAreas

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsPanels

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsRoutes

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

LicenseTab

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

##### Impact

license popover consumers

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusIcon\.tsx\:1

UnchangedCompatible

```text
Icon and popover behaviour unchanged
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/SettingsPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusContent\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/LicenseStatus\/LicenseStatusPopover\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

##### Agreement

None.

##### Not applicable

None.

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

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx

Create

```text
section extracted from the Overview
```

Lighthouse\.Frontend\/src\/hooks\/useConfiguredConnections\.ts

Create

```text
keyed connections query and the one writer-side key removal
```

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx

Extend

```text
section and Add button out; banners, existence check, stepper stay
```

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx

Extend

```text
save and wizard completion remove the configuredConnections key
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx

Extend

```text
draft deletes remove the key
```

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts

Extend

```text
Connections first System tab, admin only
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx

Extend

```text
bind ConnectionsSettings
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts

Create

```text
addConnection, editConnection, deleteConnection on the connections grid
```

Lighthouse\.EndToEndTests\/tests\/models\/overview\/OverviewPage\.ts

Extend

```text
loses the three connection methods
```

Lighthouse\.EndToEndTests\/tests\/specs\/ado\/ado\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/csv\/csv\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/jira\/jira\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/linear\/linear\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/servicenow\/servicenow\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/oauth\/OAuthConnection\.spec\.ts

Extend

```text
switch to SystemConnectionsPage
```

Lighthouse\.EndToEndTests\/tests\/specs\/settings\/ConnectionDefaults\.spec\.ts

Extend

```text
switch to SystemConnectionsPage (2 calls)
```

Lighthouse\.EndToEndTests\/tests\/specs\/screenshots\/Screenshots\.spec\.ts

Extend

```text
switch to SystemConnectionsPage (3 calls)
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/Connections/ConnectionsSettings.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render /settings/system/connections and the Overview in a MemoryRouter with a fake work-tracking-system service: two connections, none, failing, 403; as admin and reader; delete one; create one then return to the tab
```

```text
Connections is the first System tab listing both connections with Edit and Delete and reconnect banners; Overview shows Portfolios and Teams and its reconnect banners but no connections section or button; none shows the empty state with Load Demo Data link to System Demo Data and docs link; failure shows the could-not-load alert, never the empty state; reader sees no tab; a created connection is listed after return
```

```text
Empty state while loading or after a failure, Overview still listing connections, stale list after a write, or Connections visible to a reader
```

##### Promises

P4

#### Prefactoring

```text
L3 extract the Overview connections section into ConnectionsSettings, still rendered on the Overview; makes mounting it as System Connections a move
```

```text
Overview connection list, banners, empty state, delete dialog and handlers behave as before
```

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Overview/OverviewDashboard.test.tsx src/pages/Overview/OverviewDashboard.licenseCheck.test.tsx
```

#### Records

##### Decisions

```text
S4 starts with a behaviour-unchanged extraction commit of the Overview connections section (title, Connect button, empty-state alert, ReconnectBanners, DataGridBase with storageKey overview-connection-table, delete dialog) into ConnectionsSettings, then mounts it as the first System tab and removes it and its button from the Overview. The Overview keeps its existence fetch (stepper, disabled Add Team), the refused alert, its ReconnectBanners and the filter box (no longer narrows connections); the tab has no text filter. useConfiguredConnections owns keyed query configuredConnections; every write (list delete, EditConnection save and wizard completion, wizard draft deletes, ReconnectBanner) removes the key. Looks: lone spinner, 403 refused alert, Could not load the {Work Tracking Systems}. alert, empty state only after success. Button text Connect {Work Tracking System}, section title {Work Tracking Systems}; System Admin only.
```

##### Constraints

None.

##### Reuse

OverviewDashboard

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:586

Extend

```text
Inline section is the existing owner of the list, banners, empty state and delete; extracted then moved, not rewritten
```

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:1

Reuse

```text
Same columns and storageKey so saved column state survives
```

ReconnectBanner

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/ReconnectBanner\.tsx\:1

Reuse

```text
Unchanged; shown on Overview and on the tab
```

OverviewPage

Lighthouse\.EndToEndTests\/tests\/models\/overview\/OverviewPage\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

SystemConnectionsPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

addConnection

Lighthouse\.EndToEndTests\/tests\/specs\/ado\/ado\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

csv

Lighthouse\.EndToEndTests\/tests\/specs\/csv\/csv\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

addConnection

Lighthouse\.EndToEndTests\/tests\/specs\/jira\/jira\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

addConnection

Lighthouse\.EndToEndTests\/tests\/specs\/linear\/linear\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

OAuthConnection

Lighthouse\.EndToEndTests\/tests\/specs\/oauth\/OAuthConnection\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

addConnection

Lighthouse\.EndToEndTests\/tests\/specs\/screenshots\/Screenshots\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

addConnection

Lighthouse\.EndToEndTests\/tests\/specs\/servicenow\/servicenow\.spec\.ts\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

editConnection

Lighthouse\.EndToEndTests\/tests\/specs\/settings\/ConnectionDefaults\.spec\.ts\:25

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

CreateConnectionWizard

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

useConfiguredConnections

Lighthouse\.Frontend\/src\/hooks\/useConfiguredConnections\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsAreas

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

EditConnectionPage

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx\:38

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

ConnectionsSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

settingsPanels

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

##### Impact

overview page

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.test\.tsx\:1

Migrated

```text
Connection-list assertions move to the tab oracle; stepper, banners, existence check stay
```

e2e page objects and specs

Lighthouse\.EndToEndTests\/tests\/models\/overview\/OverviewPage\.ts\:1

Changed

```text
11 call sites in 8 specs switch to SystemConnectionsPage
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/overview\/OverviewPage\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/ado\/ado\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/csv\/csv\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/jira\/jira\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/linear\/linear\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/oauth\/OAuthConnection\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/screenshots\/Screenshots\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/servicenow\/servicenow\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/settings\/ConnectionDefaults\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/CreateConnectionWizard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/hooks\/useConfiguredConnections\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsPanels\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

##### Agreement

None.

##### Not applicable

None.

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

### S5

#### Targets

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx

Extend

```text
return paths and Back link
```

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx

Extend

```text
connection page routes and old /connections redirects
```

Lighthouse\.Frontend\/src\/App\.tsx

Extend

```text
old /connections routes removed so the source scan passes
```

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts

Extend

```text
connection page path helpers
```

Lighthouse\.Frontend\/src\/components\/Common\/OnboardingStepper\/OnboardingStepper\.tsx

Extend

```text
connect path and action text
```

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx

Extend

```text
D15 line under the disabled Add buttons
```

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/ConnectionsSection\.tsx

Extend

```text
Manage link, empty-text link, row Edit path, configured terms
```

Lighthouse\.EndToEndTests\/tests\/specs\/auth\/RoleBasedAccessControl\.spec\.ts

Extend

```text
goto path updated; still expects the no-access alert
```

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts

Extend

```text
Connect button text
```

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Connections/Edit/connectionEntryPoints.test.tsx
```

#### Supports

None.

#### Public oracle

```text
In a MemoryRouter open /connections/4/edit and /connections/new; click the stepper Connect action with no connection and complete or cancel the wizard; render the Overview with no connection and the Task Manager connections section with a failing and with no connection
```

```text
Old addresses redirect (replace) to /settings/system/connections/...; Save, wizard cancel and wizard complete end on System Connections; both pages show Back to {Work Tracking Systems} linking there; Overview shows the connect-first line linking to the tab; Task Manager shows Manage {Work Tracking Systems} and the empty-text link with the configured term; a reader still sees the no-access alert
```

```text
A save or cancel that lands on the Overview, an old URL that 404s or loops, hard-coded Connection wording in the Task Manager, or a reader redirected instead of shown the alert
```

##### Promises

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
S5 mounts EditConnection at /settings/system/connections/new and /:id/edit (outside the area resolver); /connections/new and /connections/:id/edit become Navigate replace routes keeping the id. Return paths are EditConnection l.137 (save), l.180 (wizard cancel), l.181 (wizard complete): all go to the Connections tab; the no-access alert keeps Back to Overview; the edit page of an existing connection has no Cancel. Both pages show Back to {Work Tracking Systems} above the title. Stepper step opens the new connect path and its action reads Connect {Work Tracking System}; Overview shows the D15 text line under the disabled Add buttons only while no connection exists and the stepper is not the way; Task Manager ConnectionsSection gains Manage {Work Tracking Systems}, a link in its empty text and the configured terms. Bug 6498 is owned by S1 and S3, not here.
```

##### Constraints

None.

##### Reuse

EditConnectionPage

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx\:38

Extend

```text
Existing owner of save, wizard and no-access alert; only route, return path and Back link change
```

OnboardingStepper

Lighthouse\.Frontend\/src\/components\/Common\/OnboardingStepper\/OnboardingStepper\.tsx\:90

Extend

```text
Step action path and label from the definition and configured term
```

ConnectionsSection

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/ConnectionsSection\.tsx\:56

Extend

```text
Existing Task Manager section; gains links and configured terms
```

SystemConnectionsPage

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

goto

Lighthouse\.EndToEndTests\/tests\/specs\/auth\/RoleBasedAccessControl\.spec\.ts\:866

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

App

Lighthouse\.Frontend\/src\/App\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

settingsAreas

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

OverviewDashboard

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:1

Extend

```text
Existing file extended by this slice; owner of the responsibility named in the slice decision
```

settingsRoutes

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

CreateNew

```text
New file; no existing symbol owns this responsibility (see slice decision and shared reuse analysis)
```

##### Impact

connection page routes

Lighthouse\.Frontend\/src\/App\.tsx\:259

Changed

```text
/connections/new and /connections/:id/edit become redirects; pages move under /settings/system/connections
```

e2e spec

Lighthouse\.EndToEndTests\/tests\/specs\/auth\/RoleBasedAccessControl\.spec\.ts\:866

Changed

```text
goto path updated; redirect then the same no-access alert
```

wizard and form components

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:1

UnchangedCompatible

```text
Holds no navigate or Cancel; wizard steps unchanged
```

target file

Lighthouse\.EndToEndTests\/tests\/models\/settings\/WorkTrackingSystems\/SystemConnectionsPage\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.EndToEndTests\/tests\/specs\/auth\/RoleBasedAccessControl\.spec\.ts\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/App\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/App\/Header\/TaskManager\/ConnectionsSection\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/components\/Common\/OnboardingStepper\/OnboardingStepper\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/models\/Settings\/settingsAreas\.ts\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/EditConnection\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:1

Changed

```text
Edited by this slice; consumers and tests named in the slice records
```

target file

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsRoutes\.tsx\:1

Changed

```text
New file created by this slice; consumers named in the slice records
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No behaviour-preserving move precedes this slice; the redirects and return paths are the behaviour change
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
