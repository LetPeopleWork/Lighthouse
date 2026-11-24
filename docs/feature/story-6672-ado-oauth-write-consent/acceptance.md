# Acceptance brief: story\-6672\-ado\-oauth\-write\-consent

## Slices

### S1

##### Covers

P1

```text
The admin starts the sign-in of an Azure DevOps OAuth connection that has no write-back mappings
```

```text
Microsoft is asked for read rights on work items and offline access only, never write rights
```

```text
A new read-only connection holds a token that can only read work items
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_connection\_without\_write\_back\_asks\_Microsoft\_only\_to\_read\_work\_items

```text
Read rights are asked for even after a write-back mapping is saved
```

###### Obligations


##### Covers

P1

```text
The admin starts the sign-in of an Azure DevOps OAuth connection that has a write-back mapping
```

```text
Microsoft is asked for read and write rights on work items and offline access
```

```text
Setting up write-back asks the admin for write consent at the next sign-in
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_connection\_with\_write\_back\_asks\_Microsoft\_to\_read\_and\_write\_work\_items

```text
Write rights are asked for on every connection
```

###### Obligations


##### Covers

P2

```text
A connection signed in with read rights only gets a write-back mapping, then a changed value is written back
```

```text
Nothing is sent to Azure DevOps, nothing is reported or stored as written, and one warning names the connection and tells the admin to sign in again
```

```text
A connection without write consent never tries to write, and support can see why
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:Write\_back\_on\_a\_connection\_signed\_in\_only\_to\_read\_sends\_nothing\_and\_logs\_one\_warning

```text
The warning is logged once per update instead of once per run when several updates are skipped
```

###### Obligations




##### Covers

P2

```text
A connection with a write-back mapping is signed in for read only, then signed in again and Microsoft grants write rights; a changed value is written back
```

```text
The change is sent to Azure DevOps once and no warning is logged
```

```text
Write-back works after the admin gives write consent
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:After\_the\_admin\_signs\_in\_again\_with\_write\_consent\_write\_back\_sends\_the\_change

```text
Rights are stored from what was requested rather than what Microsoft granted
```

###### Obligations




##### Covers

P2

```text
A connection with a write-back mapping signs in and Microsoft grants the bare scope vso.work_write
```

```text
The change is sent to Azure DevOps once
```

```text
Write consent is recognised however Microsoft names the write scope
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_grant\_naming\_write\_rights\_without\_the\_resource\_prefix\_lets\_write\_back\_send\_the\_change

```text
Write consent is recognised by a substring match on vso.work_write
```

###### Obligations


##### Covers

P2

```text
A connection with a write-back mapping signs in and Microsoft grants a scope that only starts like write rights, an empty scope, or no scope
```

```text
Nothing is sent to Azure DevOps and nothing is stored as written
```

```text
Lighthouse writes only when Microsoft really granted write rights
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_grant\_that\_does\_not\_name\_write\_rights\_sends\_nothing

```text
A scope entry ending in /vso.work_write but under another resource counts as write consent
```

###### Obligations



##### Covers

P2

```text
A connection without write-back mappings signs in with read rights only and a changed value is written back
```

```text
The change is sent to Azure DevOps once
```

```text
A read-only sign-in blocks nothing while the connection only needs to read
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_read\_only\_sign\_in\_does\_not\_block\_a\_write\_while\_the\_connection\_has\_no\_write\_back\_mapping

```text
The guard asks whether the connection has any mapping instead of whether the sign-in lacks the needed rights
```

###### Obligations


##### Covers

P3

```text
A connection with a write-back mapping whose sign-in was stored before Lighthouse recorded rights writes back a changed value
```

```text
The change is sent to Azure DevOps once with no warning
```

```text
Existing Azure DevOps OAuth connections keep writing back after the update
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_connection\_signed\_in\_before\_rights\_were\_recorded\_keeps\_writing\_back

```text
Unrecorded rights count as write consent only for Azure DevOps and not for other providers
```

###### Obligations



##### Covers

P3

```text
An Azure DevOps connection with a personal access token and a write-back mapping writes back a changed value
```

```text
The change is sent to Azure DevOps once with no warning
```

```text
Personal access token connections are not affected
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_connection\_signed\_in\_with\_a\_personal\_access\_token\_keeps\_writing\_back

```text
The guard skips connections by authentication method key rather than by a missing credential
```

###### Obligations


##### Covers

P2

```text
A read-only sign-in whose token is about to expire is refreshed and Microsoft answers with write rights; then a changed value is written back
```

```text
Nothing is sent to Azure DevOps
```

```text
A token refresh never gives a connection write rights the admin did not consent to at sign-in
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_token\_refresh\_does\_not\_give\_a\_read\_only\_sign\_in\_write\_consent

```text
A refresh overwrites stored rights with Read whatever was stored
```

###### Obligations


##### Covers

P3

```text
A sign-in stored before rights were recorded, with an expired token, is refreshed and Microsoft answers with read rights; then a changed value is written back
```

```text
The change is sent to Azure DevOps once
```

```text
A token refresh keeps existing connections writing back
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:\:A\_token\_refresh\_keeps\_a\_connection\_signed\_in\_before\_rights\_were\_recorded\_writing\_back

```text
A refresh replaces stored rights with what Microsoft answered, but only when the answer names write rights
```

###### Obligations


##### Covers

P2

```text
The compiled backend is read for every call of the tracker's write-back
```

```text
Only the write-back service calls it
```

```text
No new code path can write to Azure DevOps past the write-consent check
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs\:\:Only\_the\_write\_back\_service\_sends\_a\_write\_back\_to\_the\_tracker

```text
A caller reaches the write through reflection or a delegate, which the call scan of the compiled code does not see
```

###### Obligations


### S2

##### Covers

P4

P5

```text
An admin opens an Azure DevOps OAuth connection with one write-back mapping whose sign-in may only read, then clicks Sign in again and the sign-in succeeds
```

```text
The Rights line says Rights: Read work items and sits in the Authentication section, below its heading and before the Additional Fields section; a warning with the approved copy and a Sign in again button sits below Authentication and directly above Sync with Source; the click starts the sign-in for that connection and opens the popup without disconnecting; on success the connection is loaded again
```

```text
The admin sees the connection may only read and can give write consent from the connection page while reads keep working
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:the connection page shows the read\-only rights and a warning above the write\-back mappings\, and signing in again reloads the connection

```text
The reload happens but the page keeps showing the old rights because it does not use the reloaded connection
```

###### Obligations

None.

##### Covers

P5

P4

```text
An admin opens an Azure DevOps OAuth connection with write-back whose sign-in may read and write
```

```text
The Rights line says Rights: Read and write work items and sits in the Authentication section, below its heading and before the Additional Fields section; no write-consent warning shows
```

```text
The admin sees the connection holds write rights and is not asked to sign in again
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:the connection page shows read and write rights and no warning once write consent is given

```text
A connection without any recorded rights also shows a Rights line
```

###### Obligations

None.

##### Covers

P4

P5

```text
An admin opens the connection page while the connection is still loading
```

```text
Neither a Rights line nor the write-consent warning shows
```

```text
The admin never sees guessed rights or a stale warning
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:while the connection loads\, neither the rights nor the warning show

```text
The page shows guessed rights after a failed load of the connection
```

###### Obligations


##### Covers

P4

```text
An admin opens the connections list with a connection that needs reconnecting, then the read-only connection that has write-back, then another connection that needs reconnecting, and clicks Sign in again and the sign-in succeeds
```

```text
Exactly one warning with the approved copy shows, after the Reconnect banner of the connection listed before it and before the Reconnect banner of the connection listed after it; the click starts the sign-in for the read-only connection without disconnecting and opens the popup; on success the list is loaded again
```

```text
Right after saving a mapping the admin sees on the list, in the order of the connections, that a connection needs write consent and can give it there
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:the connections list shows the warning only beside the connection that needs write consent\, and signing in again reloads the list

```text
With only one connection needing attention, the warning sits above the whole list and its copy does not name the connection it concerns
```

###### Obligations

None.

##### Covers

P4

```text
An admin on the connections list signs in again on the read-only connection, the sign-in succeeds, and the list then cannot be loaded again
```

```text
While the list loads again the earlier warning is gone and the list's loading look shows; once the load fails, the could-not-load message shows and no write-consent warning
```

```text
A warning from before the sign-in never looks current, and a failed load never looks like a connection that needs more rights
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:when the connections list cannot be loaded again after signing in\, the earlier warning is not shown as current

```text
A failed first load of the list, before any sign-in, shows a warning from data cached by another page
```

###### Obligations

None.

##### Covers

P4

P5

```text
The connection list is sent for an Azure DevOps OAuth connection signed in before rights were recorded with a mapping, one signed in for read with a mapping, one signed in for read without a mapping, one signed in for read and write with a mapping, a Jira OAuth connection with a mapping and a personal access token connection with a mapping
```

```text
grantedRights ReadAndWrite and needsWriteConsent false; Read and true; Read and false; ReadAndWrite and false; no rights and false; no rights and false
```

```text
The settings show rights only for an Azure DevOps sign-in and ask for write consent only when write-back is set up on a read-only sign-in
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkTrackingSystemConnectionDtoTest\.cs\:\:TheConnectionListSaysWhichRightsTheSignInHoldsAndWhetherWriteBackNeedsMore

```text
A Jira credential stored with read rights would be flagged as needing write consent
```

###### Obligations







##### Covers

P4

P5

```text
A body sent to save a connection claims read and write rights and that write consent is needed
```

```text
The connection read back carries no rights and needsWriteConsent false
```

```text
The rights shown are only ever what the sign-in was granted
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkTrackingSystemConnectionDtoTest\.cs\:\:ASavedConnectionCannotClaimRightsItsSignInWasNotGranted

```text
The fields are ignored on save but another endpoint echoes a claimed value
```

###### Obligations


##### Covers

P4

P5

```text
The admin opens the settings of an Azure DevOps OAuth connection with write-back whose sign-in may only read, clicks Sign in again, the sign-in succeeds, and loading the connection again fails
```

```text
The connection is requested again; afterwards the write-consent warning and the Rights line from before the sign-in are gone, and the failed load does not surface as an unhandled failure
```

```text
The admin never sees the old read-only rights or the old warning as if they were still true after the connection could not be loaded again
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx\:\:when the connection page cannot load the connection again after signing in\, the earlier rights and warning are not shown as current

```text
A reload that returns no connection, instead of failing, still leaves the old rights and warning; this scenario does not stimulate that case
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
When an admin connects a new Azure DevOps account via OAuth without turning on write-back, the Microsoft consent screen asks only for read access to work items, not write access.
```

#### E2

##### Promises

P2

```text
With a read-only connection, changes in Lighthouse never reach Azure DevOps, and each skipped write-back is visible in the log as skipped.
```

#### E3

##### Promises

P3

```text
Azure DevOps OAuth connections made before this change keep syncing and writing back without re-signing in, and are shown as having write consent.
```

#### E4

##### Promises

P4

```text
When an admin enables write-back on a read-only connection, Lighthouse tells them more rights are needed and offers a sign-in again; after they consent, write-back works.
```

#### E5

##### Promises

P5

```text
A connection's settings page states whether its sign-in holds read rights or read and write rights, matching what was consented.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe backend-tests WriteConsent: 13/13 passed. Scenario names: connection without write-back asks Microsoft only to read work items; with write-back asks read and write; write-back on a read-only sign-in sends nothing and logs one warning; sign-in before rights recorded and PAT connections keep writing back; token refresh neither grants write consent nor removes it; after sign-in again with write consent write-back sends the change; scope lookalike/empty/missing sends nothing. Names only; URL and log text not printed.
```

### E2

met

#### Observations

```text
Probe backend-tests WriteConsent: 13/13 passed. Scenario names: connection without write-back asks Microsoft only to read work items; with write-back asks read and write; write-back on a read-only sign-in sends nothing and logs one warning; sign-in before rights recorded and PAT connections keep writing back; token refresh neither grants write consent nor removes it; after sign-in again with write consent write-back sends the change; scope lookalike/empty/missing sends nothing. Names only; URL and log text not printed.
```

### E3

met

#### Observations

```text
Probe backend-tests Rights: connection DTO scenarios passed: ADO sign-in from before rights were recorded reads and writes and needs no more consent; read-only sign-in needs write consent once write-back is set up; read-only needs nothing without write-back; write-granted needs nothing; Jira and PAT show no rights; a saved connection cannot claim rights its sign-in was not granted. Scenario names only, DTO values not printed.
```

```text
Probe backend-tests WriteConsent: 13/13 passed. Scenario names: connection without write-back asks Microsoft only to read work items; with write-back asks read and write; write-back on a read-only sign-in sends nothing and logs one warning; sign-in before rights recorded and PAT connections keep writing back; token refresh neither grants write consent nor removes it; after sign-in again with write consent write-back sends the change; scope lookalike/empty/missing sends nothing. Names only; URL and log text not printed.
```

### E4

met

#### Observations

```text
Probe frontend-tests WriteConsentAlert (name fragment sign): connection page test name states rights, warning above write-back mappings, 'and signin...' (name truncated by cut); connections list test 'warning only beside the connection that needs write consent, and signin...'. 7 passed. Click behaviour and copy not printed, inferred from test names only.
```

```text
Probe backend-tests WriteConsent: 13/13 passed. Scenario names: connection without write-back asks Microsoft only to read work items; with write-back asks read and write; write-back on a read-only sign-in sends nothing and logs one warning; sign-in before rights recorded and PAT connections keep writing back; token refresh neither grants write consent nor removes it; after sign-in again with write consent write-back sends the change; scope lookalike/empty/missing sends nothing. Names only; URL and log text not printed.
```

### E5

met

#### Observations

```text
Probe frontend-tests WriteConsentAlert (name fragment sign): connection page test name states rights, warning above write-back mappings, 'and signin...' (name truncated by cut); connections list test 'warning only beside the connection that needs write consent, and signin...'. 7 passed. Click behaviour and copy not printed, inferred from test names only.
```

```text
Probe backend-tests Rights: connection DTO scenarios passed: ADO sign-in from before rights were recorded reads and writes and needs no more consent; read-only sign-in needs write consent once write-back is set up; read-only needs nothing without write-back; write-granted needs nothing; Jira and PAT show no rights; a saved connection cannot claim rights its sign-in was not granted. Scenario names only, DTO values not printed.
```
