# Acceptance brief: epic\-6473\-settings\-split

## Slices

### S1

##### Covers

P1

```text
A System Admin opens each of the 14 old settings tab keys, in small and in capital letters
```

```text
Each key redirects to the area and tab that now hold that setting; configuration and system land on Organization, logs on System Info, rbac and authorization on System-wide access
```

```text
An old bookmark or runbook link opens the same setting it always opened
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: which settings area and tab an address opens \> the old key \%s\, in any letter case\, redirects to \%s

```text
Only the lower-case keys are mapped, or a key lands on the area's first tab instead of its own tab, or a key is dropped from the 14
```

###### Obligations


##### Covers

P1

```text
Any address is resolved by a System Admin, a reader and a person with RBAC off: bare, earlier System Admins address, hidden or unknown area and tab
```

```text
Admin lands on Organization, reader on Access API Keys, hidden or unknown tabs fall back to the first tab the person may open, the earlier System Admins address opens System-wide access
```

```text
Nobody lands on a tab they may not open, and old links keep working
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: which settings area and tab an address opens \(bare\, fallback\, unknown\, earlier address tests\)

```text
A reader is shown Organization, or an unknown tab shows an empty page, or the earlier slug is dropped
```

###### Obligations


##### Covers

P1

```text
The address is resolved while the person's access is still loading, for every access combination
```

```text
Every request waits and none is decided early
```

```text
A System Admin deep link is not bounced to the first tab before access is known
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: which settings area and tab an address opens \> never decides while the person\'s access is still loading

```text
The resolver decides with the permissive default while loading
```

###### Obligations


##### Covers

P1

```text
500 generated addresses (visible, hidden, earlier, legacy, unknown, bare) over all 8 access combinations
```

```text
Loading waits; a redirect target shows at once; every shown area and tab is one the person may open; a visible tab's own address shows it
```

```text
Every old or odd address ends on something the person may open
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: every settings address ends on a tab the person may open

```text
A redirect chain that loops, or a hidden tab shown to a reader for one rare combination
```

###### Obligations


##### Covers

P1

```text
A System Admin opens /settings, /settings/access and /settings/system
```

```text
h1 reads Settings > {Area}, each area lists its own tabs in order with the first open at its own address
```

```text
Settings is grouped into Organization, Access and System, each holding today's tabs
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a System Admin opening address sees heading with its own tabs

```text
The old h1 System Settings, or all seven tabs under every area
```

###### Obligations


##### Covers

P1

```text
A System Admin opens /settings?tab=database and /settings?tab=LOGS
```

```text
Lands on System Database and System System Info with the matching panel and no ?tab= left
```

```text
A runbook link opens the same tab
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> an old tab key \.\.\.

```text
The key is read but the first tab is shown, or the address keeps ?tab=
```

###### Obligations


##### Covers

P1

```text
A System Admin deep links to the earlier System Admins address while access loads slowly
```

```text
A spinner and no tab row until access has loaded, then the System-wide access tab is open at its own address
```

```text
The deep link opens System-wide access, not the first tab
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a deep link to System\-wide access shows a spinner \.\.\.

```text
The page bounces to the first tab, or shows the tab row while loading, or keeps the System Admins label
```

###### Obligations


##### Covers

P1

```text
A reader with RBAC on and no System Admin opens Settings and the Database tab
```

```text
Lands on Access with API Keys only, and System with System Info only
```

```text
A reader sees only what is theirs
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a reader opening Settings \.\.\. \/ a reader asking for the Database tab \.\.\.

```text
Organization or admin tabs visible to a reader
```

###### Obligations


##### Covers

P1

```text
A System Admin chooses another tab, and opens the old license address
```

```text
The chosen tab panel opens at its own address; the license address opens System
```

```text
Tab choice is shareable, and the license link no longer leads to a blank page
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> choosing another tab \.\.\. \/ the old license address \.\.\.

```text
The tab changes without the address changing; license address stays unrouted
```

###### Obligations


##### Covers

P1

```text
A person opens the header Settings menu on desktop (admin, reader, access loading) and the mobile drawer
```

```text
Menu lists Organization, Access, System with one-line descriptions and opens the chosen area; reader has no Organization; none while loading; drawer has a non-link Settings caption over three area links
```

```text
Settings is found by who decides
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the header\'s Settings menu

```text
The header still says System Settings, or Organization appears for a reader or while loading
```

###### Obligations


##### Covers

P1

```text
The license link in a connection form, the task manager full log link and the empty-list demo data link are followed
```

```text
They lead to System, System Info and System Demo Data
```

```text
In-app links open the new place
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: links into settings from elsewhere in the app

```text
A link keeps its old address; wizard and Overview links are held by the source scan only
```

###### Obligations


##### Covers

P1

```text
Settings addresses are checked by the usage-data page-opening rule
```

```text
No page event for any area or tab address
```

```text
Opening Settings adds no usage-data event
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: links into settings \.\.\. \> opening address is not counted as a page anyone used

```text
Preserved obligation, passes today; a settings route that matched a team or portfolio tab rule
```

###### Obligations


##### Covers

P1

```text
The whole production source is scanned for hand-written /settings and /connections addresses
```

```text
Only the definition and routes hold a /settings address, no new module holds a /connections address, and the definition exists and holds them
```

```text
One owner for settings addresses so links cannot lead nowhere again
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/models\/Settings\/settingsPaths\.enforcement\.test\.ts \:\: one place knows where the settings pages live

```text
Links deleted instead of moved, or a connection address added in a new module
```

###### Obligations


##### Covers

P1

```text
A reader opens System Info
```

```text
System Info without Log Level
```

```text
Reader sees System Info without Log Level
```

##### Disposition

NotTested

```text
No production target gates Log Level and the panel moves unchanged; the frontend no longer renders that section, so it cannot be observed
```

design

##### Covers

P1

```text
The access read fails and a person opens /settings/access/system-wide and bare /settings
```

```text
The page uses the permissive fallback: no spinner left, Access shows API Keys only (no System-wide access, as with RBAC off), bare Settings opens Organization Configuration
```

```text
A failed access read never locks anyone out
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a failed access read \.\.\.

```text
The page keeps the spinner forever, or shows System-wide access with RBAC unknown, or redirects to an empty area
```

###### Obligations


##### Covers

P1

```text
The old Settings page tests are retired with the old shell (file deleted in the working tree)
```

```text
Each old case is observed by a settingsAreas row: initial tab and heading by the area pages row; demodata/configuration/rbac/database keys by the legacy key table; tab clicks by the choosing another tab row; system admin all tabs, RBAC off hides System Admins, non-admin variants (team admin, portfolio admin, viewer share one access) by the area pages, reader and failed-read rows
```

```text
No second test pins the old System Settings heading
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages

```text
The old Settings.test.tsx is left in place asserting the System Settings heading
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Settings shows three areas named Organization, Access and System, and each area holds every tab it held before, unchanged as a whole.
```

#### E2

##### Promises

P1

```text
Every settings address the user had before (old settings link or old tab link) opens the same tab it opened before, now inside its new area.
```

#### E3

##### Promises

P2

```text
Organization shows exactly the Terminology, Calendar and Refresh & ordering tabs.
```

#### E4

##### Promises

P2

```text
System shows a Usage Data tab.
```

#### E5

##### Promises

P2

```text
Each of the four tabs shows a loading look while its data is on its way, and a failure look when its data cannot load, never an empty area that reads as no data.
```

#### E6

##### Promises

P3

```text
System shows a License tab that displays the current license status and offers import and clear actions.
```

#### E7

##### Promises

P3

```text
Each license link in a connection form opens the System License tab.
```

#### E8

##### Promises

P4

```text
System shows a Connections tab as its first tab.
```

#### E9

##### Promises

P4

```text
The Connections tab lists connections, shows reconnect banners where a connection needs it, shows an empty state when there are none, and offers a Connect button.
```

#### E10

##### Promises

P4

```text
The Overview page no longer shows a connections section.
```

#### E11

##### Promises

P5

```text
Each way to a connection (setup stepper, Task Manager, the no-connection hint and old connection URLs) leads to System, Connections.
```

#### E12

##### Promises

P5

```text
Each connection page offers a way back to System, Connections.
```

## Charter

None.

## Examination

None.
