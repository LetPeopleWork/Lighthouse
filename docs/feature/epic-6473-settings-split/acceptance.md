# Acceptance brief: epic\-6473\-settings\-split

## Slices

### S1

##### Covers

P1

```text
A System Admin opens each of the 14 old settings tab keys, in small and in capital letters
```

```text
Each key redirects to the area and tab that now hold that setting; configuration and system land on Organization, logs on System, Logs, rbac and authorization on System-wide access
```

```text
An old bookmark or runbook link opens the same setting it always opened
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: which settings area and tab an address opens \> the old key \%s\, in any letter case\, redirects to \%s

```text
Only the lower-case keys are mapped, or a key lands on the area's first tab instead of its own tab, or logs still lands on System Info
```

###### Obligations


##### Covers

P1

```text
Any address is resolved by a System Admin, a reader and a person with RBAC off: bare, earlier System Admins address, hidden or unknown area and tab
```

```text
Admin lands on Organization, reader on Access API Keys, hidden or unknown tabs fall back to the first tab the person may open (Connections for an admin in System), the earlier System Admins address opens System-wide access
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
500 generated addresses (visible, hidden, earlier, legacy, unknown, bare) over all 8 access combinations, with Connections among System's tabs
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
h1 reads Settings > {Area}, each area lists its own tabs in order with the first open at its own address; System lists Connections, Database, Encryption, Demo Data, Usage Data, System Info, Logs, Refresh History, License and opens Connections
```

```text
Settings is grouped into Organization, Access and System, each holding its tabs; connections come first in System
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a System Admin opening address sees heading with its own tabs

```text
The old h1 System Settings, or all tabs under every area, or Demo Data still first in System
```

###### Obligations


##### Covers

P1

```text
A System Admin opens /settings?tab=database and /settings?tab=LOGS
```

```text
Lands on System Database and System Logs with the matching panel and no ?tab= left
```

```text
A runbook link opens the same tab
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> an old tab key in \$address opens the area and tab that now hold that setting

```text
The key is read but the first tab is shown, or logs opens System Info, or the address keeps ?tab=
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
Lands on Access with API Keys only, and System with System Info, Logs, Refresh History and License only
```

```text
A reader sees only what is theirs
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> a reader opening Settings \.\.\. \/ a reader asking for the Database tab \.\.\.

```text
Organization or admin tabs visible to a reader, or Logs, Refresh History or License hidden from a reader
```

###### Obligations


##### Covers

P1

```text
A System Admin chooses another tab
```

```text
The chosen tab panel opens at its own address
```

```text
Tab choice is shareable
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> choosing another tab opens its panel at its own address

```text
The tab changes without the address changing
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
The task manager full log link and the empty-list demo data link are followed
```

```text
They lead to System Logs and System Demo Data
```

```text
In-app links open the new place
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: links into settings from elsewhere in the app

```text
The full log link leaves the person on a tab other than Logs, or the demo data link keeps its old address; the license links and the wizard link are held by the source scan and the License oracle, the connections empty-state demo data link by the Connections oracle
```

###### Obligations


##### Covers

P1

```text
Every area and tab address is opened, including System Connections, Logs, Refresh History and License
```

```text
No page event for any area or tab address
```

```text
Opening settings raises no usage-data page event
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: links into settings from elsewhere in the app \> opening \%s is not counted as a page anyone used

```text
A settings address is counted as a page opening
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
The page uses the permissive fallback: no spinner left, Access shows API Keys only (no System-wide access, as with RBAC off), bare Settings opens Organization on Terminology with Terminology, Calendar and Refresh & ordering
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


##### Covers

P1

```text
A reader opens System Info, Logs and Refresh History
```

```text
System Info holds the system details and third party packages, Logs the log level and viewer, Refresh History the history; none shows another tab's content
```

```text
Each of the three has its own tab with only its own content
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx \:\: the three settings area pages \> \$address shows its own content and none of the other two tabs\' content

```text
All three still shown on one System Info tab, or a tab showing another's content
```

###### Obligations


### S2

##### Covers

P2

```text
A System Admin opens Organization
```

```text
Tabs Terminology, Calendar, Refresh & ordering with Translate, EventBusy, Sync icons, opened on Terminology; no blackout, refresh, behaviour or usage-data content on it
```

```text
Organization shows only how the organisation works, in three tabs
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx \:\: Organization holds only how the organisation works \> offers Terminology\, Calendar and Refresh \& ordering \.\.\.

```text
The old single Configuration tab, or the usage-data row left on a tab of Organization
```

###### Obligations


##### Covers

P2

```text
A person opens the old link /settings?tab=configuration
```

```text
Ends on Organization Terminology with the Terminology section shown, address replaced
```

```text
The old Configuration link opens Terminology
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx \:\: Organization holds only how the organisation works \> the old Configuration link opens Terminology \.\.\.

```text
Landing on a dead or first-visible tab other than Terminology
```

###### Obligations


##### Covers

P2

```text
A System Admin opens Calendar with a Premium licence, then with a slow licence call, then with a failing licence call
```

```text
Blackout section with usable Add buttons and one licence request; a spinner and no Premium required while loading; Could not load the license. instead of Premium required on failure
```

```text
An unreadable licence never looks like no Premium
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx \:\: Calendar tab

```text
Premium required shown while the licence is unknown or failed, or a second licence request by the tab
```

###### Obligations


##### Covers

P2

```text
A System Admin opens Refresh & ordering with ready, locked, slow, failing settings and failing licence
```

```text
Refresh sections and only the ordering row in the configured words, usable with Premium, locked without; spinner while loading; Could not load the behaviour settings. when either call fails, never an empty tab or locked switch; no usage-data row though both rows share id 0
```

```text
Feature ordering keeps its Premium rule and says when it cannot be loaded
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx \:\: Refresh \& ordering tab

```text
An empty section or a locked switch after a failed call, or matching rows by id
```

###### Obligations


##### Covers

P2

```text
A System Admin opens System Usage Data, switches the veto on, leaves for Database and returns; slow and failing settings calls
```

```text
Row and docs link only (no footer sentence, no ordering row), switch usable without Premium; first switch shown on return is on; spinner while loading; Could not load the behaviour settings. on failure
```

```text
The usage-data veto lives with the server's settings and keeps its value
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Organization\/organizationTabs\.test\.tsx \:\: Usage Data tab

```text
A stale switch after return, an empty tab after a failed call, or a footer sentence
```

###### Obligations


##### Covers

P2

```text
The four SystemSettingsTab test files and the licence-asker allow-list entry
```

```text
Moved to the new tabs; entry removed in the commit that deletes the component
```

```text
None directly: the behaviour rows moved with their tests
```

##### Disposition

NotTested

```text
Design marks the four SystemSettingsTab test files and the licence-asker allow-list entry Migrated or Changed, not oracle files; the crafter moves them and removes the entry with the component
```

design

### S3

##### Covers

P3

```text
A System Admin opens System, License with a valid license
```

```text
The tab is the last of System, selected, and shows organization, licensee, expiry, Update License and Clear License in one License section
```

```text
An admin sees what the header popover shows, with import and clear
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> shows a System Admin the license status with import and clear\, in one License section

```text
The tab shows the status but no import or clear action, or the license is not the last tab
```

###### Obligations


##### Covers

P3

```text
A reader opens System, License
```

```text
Status and expiry show, and no add, update or clear action
```

```text
A reader sees the license without changing it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> shows a reader the status and expiry but neither import nor clear

```text
Import and clear are shown to every person
```

###### Obligations


##### Covers

P3

```text
An admin opens System, License while the license call is slow, then it answers
```

```text
Loading... shows, neither No License nor an error shows, then the status replaces it
```

```text
A slow connection shows loading and never a wrong no-license answer
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> shows a loading look while the status is on its way and the status once it has arrived

```text
A blank area or No License while loading
```

###### Obligations


##### Covers

P3

```text
An admin opens System, License while the license call fails
```

```text
Could not load the license. shows, with no Loading... and no No License
```

```text
A failure says so and is not taken for having no license
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> says the license could not be loaded when the call fails\, and never shows an empty or loading area

```text
An empty area, or the free-version text, after a failed call
```

###### Obligations


##### Covers

P3

```text
An admin clears the license on System, License and confirms
```

```text
The clear call is made once and the tab shows No License
```

```text
Clearing works from the tab like from the popover
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> lets an admin clear the license after confirming\, and then shows no license

```text
Clear is offered but does nothing, or the tab keeps the old license
```

###### Obligations


##### Covers

P3

```text
An admin imports a renewed license file on System, License
```

```text
The tab shows the new expiry and the header icon popover shows it too, not the old one
```

```text
The header icon is current after import
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the License tab in System \> shows a new expiry in the License tab and in the header icon after an admin imports a license file

```text
The tab updates but the header icon keeps the old expiry
```

###### Obligations


##### Covers

P3

```text
An admin and a reader open /settings/license
```

```text
Both end on System, License with the address replaced and the status shown
```

```text
The old license address is no longer a dead end
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the ways into the License tab \> the old license address opens System\, License for \$who\, replacing the address

```text
The old address opens System or another tab, or is pushed so Back loops
```

###### Obligations


##### Covers

P3

```text
A person follows View license options in the connection form OAuth notice
```

```text
The link points to System, License and following it opens that tab with the status
```

```text
The link no longer leads to a blank page or System Info
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the ways into the License tab \> the license link in the OAuth premium notice of a connection form opens System\, License

```text
The link opens System but a different tab
```

###### Obligations


##### Covers

P3

```text
A person follows View license options in the connection wizard
```

```text
The link points to System, License and following it opens that tab with the status
```

```text
The wizard link no longer leads to a blank page or System Info
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/System\/LicenseTab\.test\.tsx \:\: the ways into the License tab \> the license link in the connection wizard opens System\, License

```text
The wizard link opens System but a different tab
```

###### Obligations


### S4

##### Covers

P4

```text
A System Admin opens Settings, System, Connections; the instance calls connections Trackers and has Northwind Jira, which needs reconnecting, and Northwind ADO
```

```text
Connections is System's first tab and open; the section titled Trackers lists both with Edit and Delete each, one reconnect banner for Northwind Jira, a Connect Tracker button, no text filter and no empty state
```

```text
The admin manages every connection from System, in the instance's own word
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a System Admin finds Connections as System\'s first tab\, listing every connection with Edit\, Delete and its reconnect banner\, under the configured term

```text
The button keeps the old Add wording only in a tooltip, or the banner shows for a healthy connection as well as the broken one when both are listed
```

###### Obligations

None.

##### Covers

P4

```text
A person hid the Type column on the Overview's connection list, then opens System, Connections
```

```text
The Type column stays hidden on the tab
```

```text
Saved column choices survive the move
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> keeps the columns a person hid on the Overview\'s list hidden on the tab

```text
The tab reads the saved state but ignores column widths or order
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin deletes Northwind ADO from the tab and confirms in the dialog
```

```text
The same Confirm Delete dialog names Northwind ADO; the connection is deleted and leaves the list; Northwind Jira stays
```

```text
Retiring a connection works as it did on the Overview
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> deleting a connection asks the same question and takes it off the list

```text
The deleted row is hidden locally while the remembered list keeps it for the next visit
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin opens the tab on an instance with no connection and follows Load Demo Data
```

```text
The empty-state alert reads No Tracker found. Load Demo Data or Check the documentation, with a Connect Tracker button and the documentation link; Load Demo Data opens System, Demo Data
```

```text
A fresh instance says how to start
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a fresh instance is told no connection exists\, with a link to load demo data that opens System\, Demo Data\, and a link to the documentation

```text
The documentation link opens in the same window
```

###### Obligations


##### Covers

P4

```text
A System Admin opens the tab while the list is still on its way, then the list arrives
```

```text
Only a spinner shows, no empty state and no Connect button; then the list replaces the spinner
```

```text
A slow connection never looks like an empty instance
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> shows a lone spinner while the list is on its way\, never the empty state\, then the list

```text
The spinner sits beside a dimmed previous list from another visit
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin opens the tab and the list cannot be loaded
```

```text
Could not load the Trackers. shows; no empty state and no refused message
```

```text
A failed load is not mistaken for no connections
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a list that cannot be loaded says so in the instance\'s own word\, never \'no connection found\'

```text
The alert offers no way to retry
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin opens the tab and the server refuses the list
```

```text
Today's refused message shows; no empty state and no could-not-load message
```

```text
A refusal says to ask for roles, not that nothing exists
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a refused list shows today\'s refused message\, never the empty state

```text
The refused message also shows a Connect button that will be refused too
```

###### Obligations

None.

##### Covers

P4

```text
A reader with RBAC on opens the Connections address
```

```text
No Connections tab; the address moves to System Info; the connection list is never asked for
```

```text
Readers see no connections, as before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a reader sees no Connections tab\, and its address leads to a tab they may open

```text
The tab is hidden from readers while the list is still fetched in the background
```

###### Obligations


##### Covers

P4

```text
A System Admin sees the list, creates a connection in the wizard, then returns to the tab within the app's cache time
```

```text
The new connection is listed
```

```text
The tab never shows the list from before a change as current
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the connections live in System\, Connections \> a connection created in the wizard is listed on return to the tab\, not the list from before

```text
A saved edit of an existing connection or a discarded wizard draft still leaves the old list remembered
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin opens the Overview with Northwind Jira needing reconnecting and Northwind ADO, then types a filter matching nothing
```

```text
Portfolios and Teams show with one reconnect banner; no connections section, no connection names, no Edit, no Add or Connect Tracker button; Add Team enabled; the banner stays after filtering
```

```text
The Overview is about Portfolios and Teams but still warns of a broken connection
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the Overview without the connections section \> shows a System Admin the Portfolios\, Teams and reconnect banners\, but no connections section and no button to add a connection

```text
The banner shows only for connections whose name matches the filter while the filter is empty
```

###### Obligations

None.

##### Covers

P4

```text
A System Admin opens the Overview on an instance with no connection
```

```text
Add Team is disabled and the Overview shows no connections empty-state alert
```

```text
Onboarding still waits for a connection before Teams, and the empty state lives only on System, Connections
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx \:\: the Overview without the connections section \> still asks whether a connection exists\: with none\, Add Team stays disabled for a System Admin

```text
Add Team is disabled while the existence check is still loading and stays disabled after it fails
```

###### Obligations

None.

### S5

##### Covers

P5

```text
Tomasz opens the old edit link for an existing connection
```

```text
The address is replaced by the one under Settings, System, Connections; the edit page of that connection shows, with a Back to {Work Tracking Systems} link to System, Connections and no Cancel
```

```text
A runbook link from before the move still opens the right connection, and he can get back to the connections list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:an old edit link opens the same connection under System\, with a way back and no Cancel

```text
The old link redirects with a pushed history entry, or the Back link carries a fixed Connection word or points to the Overview
```

###### Obligations

None.

##### Covers

P5

```text
Priya opens the old new-connection link
```

```text
The address is replaced by the one under Settings, System, Connections; the wizard shows with the title in the configured term and a Back to {Work Tracking Systems} link to System, Connections
```

```text
An old bookmark to add a connection opens the wizard where it now lives
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:an old new\-connection link opens the wizard under System\, with a way back

```text
The old link redirects but the wizard is still mounted only at the old address, or the Back link is missing on the create page
```

###### Obligations

None.

##### Covers

P5

```text
A reader without system-admin access opens the old edit link
```

```text
They end on the connection page address under System and see the no-access alert; they are not moved into a settings area
```

```text
A reader is told they have no permission instead of being bounced elsewhere
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:a reader sent along an old link is still told they have no permission\, not moved elsewhere

```text
The old link redirects a reader to the settings area resolver, which silently sends them to another tab
```

###### Obligations

None.

##### Covers

P5

```text
Priya cancels the connection wizard on its first step
```

```text
She is on Settings, System, Connections and no connection was created
```

```text
Backing out of adding a connection returns her to the connections list, not the Overview
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:cancelling the wizard returns to System\, Connections and creates nothing

```text
Cancel still lands on the Overview, or creates the connection before leaving
```

###### Obligations

None.

##### Covers

P5

```text
Priya picks a system, fills it in, names it and creates the connection
```

```text
She ends on Settings, System, Connections, the remembered connections list is dropped so the list asks the server again, and exactly one connection was added
```

```text
She sees the new connection where connections live, not a stale list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:completing the wizard returns to System\, Connections and the list asks again

```text
Completion lands on the Overview, or lands on the tab but keeps the old list
```

###### Obligations

None.

##### Covers

P5

```text
Tomasz saves changes to an existing connection
```

```text
He ends on Settings, System, Connections, the remembered list is dropped (and was kept until then), and the connection was updated once
```

```text
After fixing a connection he sees the connections list showing the change
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:saving an existing connection returns to System\, Connections and the list asks again

```text
Save lands on the Overview, or the list key is dropped before the save so a stale refetch sneaks in
```

###### Obligations

None.

##### Covers

P5

```text
Priya starts an OAuth connection, the wizard saves a draft, and she cancels at the naming step
```

```text
The draft is deleted on the server, the remembered list is dropped, and she is on Settings, System, Connections
```

```text
A cancelled draft does not linger in the connections list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:cancelling after the wizard saved a draft deletes it\, returns to System\, Connections and the list asks again

```text
The draft is deleted but the list still shows it from the remembered answer, or Cancel lands on the Overview
```

###### Obligations

None.

##### Covers

P5

```text
The Overview shows the onboarding stepper with no connection and Priya clicks its action
```

```text
The action reads Connect {Work Tracking System} in the configured term and opens the new-connection page under Settings, System, Connections
```

```text
The first-time path starts on System, Connections with words the instance uses
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:the onboarding stepper offers to connect a Data Source and opens the new connection page under System

```text
The action says Add {term}, or opens the old connections address which then lands nowhere
```

###### Obligations

None.

##### Covers

P5

```text
The Overview with no connection, both with the onboarding stepper showing and with it put away; and the same Overview with a connection
```

```text
With none, a line under the disabled Add buttons reads Connect a {Work Tracking System} first: followed by a link Settings, System, Connections to the tab; with one, no such link
```

```text
Priya always learns where to connect a first source, whether or not the stepper is showing
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:the Overview

```text
The line is a tooltip only, hides while the stepper shows, uses the fixed word Connection, or shows even when a connection exists
```

###### Obligations

None.

##### Covers

P5

```text
The Task Manager lists a failing connection and Tomasz presses Edit; and the Task Manager with nothing configured
```

```text
A Manage {Work Tracking Systems} link leads to the tab, Edit opens that connection under System, the empty text links to the tab, and the section has no hard-coded connection word
```

```text
A warning in the Task Manager leads to the connection under System, in the instance's own words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Connections\/Edit\/connectionEntryPoints\.test\.tsx\:\:the Task Manager\'s connections section

```text
Edit still opens the old address, or the section keeps the words Test connection, Edit connection or No connections
```

###### Obligations

None.

##### Covers

P5

```text
Access is still loading, a reader opens Organization, an admin opens bare Settings, or RBAC is off or its read failed
```

```text
Wait while loading; reader sent to Access; admin lands on Organization; RBAC off or failed read shows every area RBAC-off shows
```

```text
Settings never decides before access is known and never locks anyone out
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx\:\:which settings area and tab an address opens

```text
Not changed by this slice: a resolver that decides while loading is caught only by these first-slice tests
```

###### Obligations





##### Covers

P5

```text
Each of the 14 old tab keys in both cases, and the old System Admins slug
```

```text
Each opens the tab listed in the navigation table; the old slug opens System-wide access
```

```text
Old Settings links keep working
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx\:\:which settings area and tab an address opens

```text
Not changed by this slice; first-slice table test
```

###### Obligations



##### Covers

P5

```text
Generated addresses including a hidden tab, an earlier slug and an unknown area
```

```text
Every redirect target shows, and every visible tab's own address shows that tab
```

```text
No settings address ends on a page the person may not open
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/settingsAreas\.test\.tsx\:\:every settings address ends on a tab the person may open

```text
Not changed by this slice; first-slice round-trip test
```

###### Obligations





##### Covers

P5

```text
The whole production source scanned for hand-written settings and connection addresses
```

```text
Only the definition and the routes hold such an address; needs the earlier allow-list of connection addresses emptied
```

```text
A missed link is a failing build
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/models\/Settings\/settingsPaths\.enforcement\.test\.ts\:\:no module that did not already hold a connection address starts writing one

```text
The allow-list stays non-empty, so old connection addresses survive in the stepper, Task Manager or App
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
