# Acceptance brief: story\-6712\-faster\-page\-loads

## Slices

### S1

##### Covers

P1

```text
A browser that accepts br, then one that accepts gzip, asks the server for the built frontend script
```

```text
The script arrives with Content-Encoding br or gzip, Vary contains Accept-Encoding, and the decoded body is the file byte for byte
```

```text
The frontend JavaScript arrives compressed, about a third of the bytes, and runs unchanged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

A\_browser\_that\_accepts\_compression\_gets\_the\_script\_compressed\_and\_the\_same\_file\_once\_decoded

```text
It would still pass a server that compresses the few-KB fixture but not the real bundle under TLS, which the Docker smoke step covers
```

###### Obligations




##### Covers

P1

```text
A client that sends no Accept-Encoding asks for the built frontend script
```

```text
No Content-Encoding and the body is the file byte for byte
```

```text
A browser or tool without compression still gets the same working file
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

A\_browser\_that\_does\_not\_accept\_compression\_gets\_the\_plain\_script

```text
Today's pipeline passes it; it fails a server that compresses whatever the client sends
```

###### Obligations



##### Covers

P1

```text
A browser that accepts br opens the site root
```

```text
The page shell arrives with Content-Encoding br and keeps Cache-Control no-cache, no-store, must-revalidate
```

```text
The first page arrives compressed and the user still always gets the newest version of the app
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

The\_page\_shell\_is\_compressed\_and\_still\_never\_cached

```text
It would pass with a shell whose decoded body is wrong; the script row pins the decoded bytes for static files
```

###### Obligations




##### Covers

P2

```text
A browser that offers gzip and br asks for the current version, and a client that offers nothing asks again
```

```text
The first answer carries Content-Encoding br and Vary Accept-Encoding; the second has no Content-Encoding
```

```text
Safe API answers take fewer bytes for a browser, and command-line clients get them plain as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

A\_safe\_api\_answer\_to\_a\_client\_that\_asks\_for\_no\_compression\_is\_plain

```text
A server compressing only the version route would pass; the rule rows over unmarked endpoints cover other GETs
```

###### Obligations




##### Covers

P2

```text
A browser that accepts br creates an API key, and opens the update hub handshake
```

```text
Both answers arrive without Content-Encoding, the key with 201 Created and the handshake with 200
```

```text
No answer that carries a new key or a connection token can be guessed from its compressed size
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

The\_update\_hub\_handshake\_is\_never\_sent\_compressed

```text
Both answers are POSTs, so a rule that refuses POST passes them even with the hub unmarked; the marked-endpoint rule rows and the marker test on the hub class catch a missing marker
```

###### Obligations



##### Covers

P2

```text
The compression rule is asked about a GET of JSON with br and a HEAD of a script with gzip that no endpoint answers, as for a static file, and a GET of JSON with br from an endpoint without the never-compress marker
```

```text
Each answer is compressed
```

```text
Static files and safe answers of ordinary endpoints reach the browser compressed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

A\_safe\_answer\_to\_a\_client\_that\_accepts\_compression\_is\_compressed

```text
A rule that compresses everything passes these rows; the refusal and marked rows fail it
```

###### Obligations




##### Covers

P2

```text
The compression rule is asked about POST, PUT and DELETE of JSON with br, a PNG and a problem answer with br, and a GET of JSON without Accept-Encoding
```

```text
None of these answers is compressed
```

```text
Answers that can echo a secret, cannot be compressed, or were not asked for compressed go out as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

An\_unsafe\_or\_uncompressible\_or\_not\_asked\_for\_answer\_is\_sent\_as\_it\_is

```text
A rule that compresses nothing passes these rows; the compressed rows fail it. PATCH is not a separate row
```

###### Obligations







##### Covers

P2

```text
With ResponseCompression:Enabled false the rule is asked about a GET of JSON with br
```

```text
The answer is not compressed
```

```text
An operator can turn compression off with one setting and a restart
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

Turning\_compression\_off\_sends\_a\_safe\_answer\_as\_it\_is

```text
A switch that is ignored when the section is absent passes; the safe-answer rows run with the section absent and pin the default on
```

###### Obligations


##### Covers

P2

```text
The compression rule is asked about a GET of plain text and a GET of JSON, both with br, on a route no list names, from an endpoint whose metadata holds the never-compress marker alone or among other metadata
```

```text
Neither answer is compressed
```

```text
The logs, the embed handshake and the update hub stay uncompressed even when their routes are renamed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

An\_answer\_of\_an\_endpoint\_marked\_never\_compress\_is\_sent\_as\_it\_is\_whatever\_its\_route

```text
A rule that still matches a hand-typed path list fails these rows, because the route is not on any list; a rule that looks for the marker only as the first metadata item fails the among-other-metadata row
```

###### Obligations



##### Covers

P2

```text
The classes that answer the logs, the embed handshake and the update hub are inspected for the never-compress marker on the class
```

```text
Each of the three classes carries the marker itself
```

```text
Removing the marker from any class whose answers can carry a secret or log text turns a test red before it ships
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretBearingAnswersAreNeverCompressedTest\.cs

Every\_answer\_that\_can\_carry\_a\_secret\_or\_log\_text\_is\_marked\_never\_compress

```text
A marker placed only on one action instead of the class passes for that class while its other actions stay unmarked; the class-level check and the wire handshake row narrow that
```

###### Obligations


### S2

##### Covers

P3

```text
The user opens a Team page where a header, a page part kept across pages and two more page parts gate on the user's permissions
```

```text
The permissions are asked once, and all four parts show the same grants (Team Admin of Team 1)
```

```text
A page load asks for the user's permissions once, not once per component, and gating is unchanged
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

asks for the permissions once when the components on a page need them

```text
A cache that shares the answer but asks again on every later mount of the page
```

###### Obligations


##### Covers

P3

```text
A further component that gates on permissions appears on a page that already has its answer
```

```text
It is ready at once with the page's grants and nothing is asked again
```

```text
Parts that appear later on the same page show their actions without another permissions request
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

gives a component that appears later on the same page the answer already there\, without asking again

```text
A component that reads the cached answer but triggers a background ask on mount, counted only after the check
```

###### Obligations


##### Covers

P4

P3

```text
An administrator removes the user's Team Admin right on Team 1 and grants Portfolio Admin on Portfolio 2, then the user moves from the Team page to a Portfolio page; the header and one page part stay mounted, two page parts mount on the new page
```

```text
Before the new answer lands, the two newly mounted parts are loading while the header and the kept part are not loading and still offer Team Admin on Team 1; once it lands every part offers Portfolio Admin on Portfolio 2 and no longer Team Admin on Team 1; the permissions were asked twice in total
```

```text
Rights changed by someone else show on the next page without a reload, and controls that stay on screen do not vanish while the answer is on its way
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

shows the rights an administrator changed once the user moves to another page\, while the components that stay keep their answer

```text
A refresh on move that also asks once more for each newly mounted component, if that extra ask settles before the count is read
```

###### Obligations



##### Covers

P4

```text
The user moves to a Portfolio page while the first page's permissions answer is still on its way, and that old answer then arrives
```

```text
The header, the kept part and the newly mounted parts all stay loading after the old answer arrives, and only show the grants of the answer asked after the move; two asks in total
```

```text
A page never shows actions from a permissions answer asked on the page the user left
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

never shows the answer of the page the user left on the page they moved to

```text
An implementation that shows the old answer for one render frame and replaces it before the check
```

###### Obligations


##### Covers

P5

```text
On a page whose answer gives no admin rights, the user's own change (a grant or a new Team) completes and the refresh after own changes is called while its answer is still on its way
```

```text
While the refresh runs no part turns loading or could-not-load; once the answer lands every part offers Team Admin on Team 7, with no page move; the permissions were asked once more
```

```text
The user's own change of rights shows at once, without a reload, a page move or controls vanishing meanwhile
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

shows the user\'s own change of rights at once\, without a page move and without loading

```text
A refresh that briefly turns parts loading for a render between the two checks
```

###### Obligations


##### Covers

P6

```text
The permissions answer cannot be loaded when the user opens a page with four gating parts
```

```text
Every part reports could-not-load, is not loading and shows today's permissive fallback (system admin, may create Team and Portfolio, admin of any unit); asked once, with no automatic retries
```

```text
Permissions that cannot be loaded behave as today, the same for every part of the page
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

gives every component the could\-not\-load state and today\'s fallback when the permissions cannot be loaded

```text
A fallback missing only a right that is not listed in the check
```

###### Obligations


##### Covers

P6

```text
After a failed load the user presses retry in one component, and the answer now loads
```

```text
Exactly one more ask, and all parts show the loaded grants with could-not-load cleared
```

```text
One retry asks again once for all parts of the page
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

asks once for every component when one of them retries after a failed load

```text
A retry that refetches twice in sequence, if the second ask is started after the count is read
```

###### Obligations


##### Covers

P3

```text
The product source, every non-test frontend file including files added later, is scanned for direct calls to the permissions service
```

```text
Only the permissions hook calls it
```

```text
Every component gates on the one shared permissions answer behind useRbac()
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/sharedReadOwners\.test\.ts

lets only the permissions hook ask for the user\'s permissions

```text
A component that reads the service through destructuring or a renamed alias, which the scan does not see
```

###### Obligations


##### Covers

P6

P4

```text
The user moves from a Team page to a Portfolio page and the permissions ask after the move fails; then one newly mounted part retries and the answer loads
```

```text
The header and the kept part keep offering Team Admin on Team 1, not loading and not could-not-load; the two newly mounted parts report could-not-load with today's permissive fallback; two asks, no automatic retries; the retry asks exactly once and every part then offers Portfolio Admin on Portfolio 2
```

```text
A failed permissions load after a page move leaves the controls already shown in place, gives new parts today's could-not-load behaviour, and one retry fixes all of them
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

keeps the shown answer for the components that stay when the ask after a page move fails\, and one retry asks once

```text
Kept parts that switch to the fallback for one render and back before the check
```

###### Obligations


##### Covers

P3

```text
The app runs in React strict mode, which renders twice; the user moves from a Team page to a Portfolio page
```

```text
The move asks the permissions exactly once and every part on the new page then offers Portfolio Admin on Portfolio 2
```

```text
One page move asks for the user's permissions once, even when the app renders twice
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

asks once and moves one page when the app runs in strict mode

```text
A page counter that advances twice but whose second invalidation is deduplicated into the first ask; the extra advance is not visible through the permissions
```

###### Obligations


### S3

##### Covers

P7

```text
Open a page where the menus, the page body and the survey nudge all read the Teams, the Portfolios and the license
```

```text
Each is asked once and every component shows the same lists
```

```text
The page shows its data after one round of requests, not one per component
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

asks for each of them once when the menus\, the page and the nudge all need them

```text
the survey nudge or the create gates keep asking the license or the lists themselves while showing the same data
```

###### Obligations


##### Covers

P8

```text
The user creates, renames or deletes a Team without leaving the page
```

```text
Menus, overview and the create gate show the new list at once, after one extra ask for all components
```

```text
Their own change shows in the lists immediately
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

shows the lists at once\, without a page move\, when the user \$change

```text
each reader asks the service again on its own after the change
```

###### Obligations


##### Covers

P8

```text
Someone else changes the Teams, then the user moves to another page
```

```text
The old list stays until the move, then the new list shows once after one ask for all
```

```text
A change by someone else shows on the next page move
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

shows a change made by someone else on the user\'s next move to another page\, not before

```text
the lists refresh on a timer or on every render instead of on the page move
```

###### Obligations


##### Covers

P7

```text
Read the lists on one page, move to a page with no list reader, then to a page where readers mount
```

```text
Nothing is asked on the page without readers; the readers on the third page ask once and show the list
```

```text
A page after a quiet page shows its lists, never a lasting loading look
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

gives the reader on the third page its list after a page that had no reader\, asking once

```text
readers that mount after a quiet page reuse the old answer or wait for ever
```

###### Obligations


##### Covers

P7

```text
The Teams cannot be loaded; one reader retries
```

```text
Every reader reports the failure after one ask; one retry asks once for all and every reader shows the list
```

```text
A failed load is shown to all readers and one retry fixes all
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

tells every reader the Teams could not be loaded\, and one retry asks once for all of them

```text
each reader retries by itself or only the retrying reader recovers
```

###### Obligations


##### Covers

P7

```text
The user may not read the Teams list and has no Premium license
```

```text
The Teams count as empty and creating a Team stays allowed, as today
```

```text
A user without the right to list Teams is not blocked by an unread list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

counts no Teams and still lets the user create one when the user may not read the Teams list

```text
the gate stays loading or failed on a refused list
```

###### Obligations


##### Covers

P7

```text
A settings editor asks for the Teams as a promise while a reader already loaded them
```

```text
It receives the shared list and the service is not asked again
```

```text
The Portfolio settings editor offers the Teams without another request
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

hands a caller that needs the Teams as a promise the list a reader already loaded\, without asking again

```text
the loader returns the right list by calling the service itself
```

###### Obligations


##### Covers

P7

```text
Scan all non-test frontend source for calls to the Teams, Portfolios and license services
```

```text
Only the shared lists hook asks for Teams and Portfolios, and only the license hook asks for the license, aliases included
```

```text
No page can quietly keep its own copy of the shared lists
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/sharedListOwners\.test\.ts

lets only the shared lists hook ask for the Teams

```text
a file added later calls a service through a destructured alias
```

###### Obligations


##### Covers

P7

```text
A settings editor asks for the shared Teams as a promise while the Teams cannot be loaded
```

```text
The caller is rejected with the reason the service gave, not a wrapper, and the Teams service was asked exactly once
```

```text
A settings editor that cannot get its Teams shows the real reason after one ask, not after three
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/unitListsOwnChanges\.test\.tsx

fails with the reason the Teams could not be loaded\, after asking once

```text
A loader that rejects with the wrapper but asks once, or that gives the reason after retrying twice
```

###### Obligations


##### Covers

P8

```text
On each page where the user changes a Team or Portfolio (create wizard, create as a copy, edit page, Settings tab of a Team page and of a Portfolio page, delete on the overview), the user completes the change while the server now holds the changed lists
```

```text
Every component showing the lists shows the change without a page move, and Teams and Portfolios were each asked exactly once on opening and once after the change
```

```text
After creating, renaming, copying or deleting a Team or Portfolio, the menus, overview and lists show it at once
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/unitListsOwnChanges\.test\.tsx

shows the lists with the change at once\, asking each list once more\, when the user \$change

```text
A page that skips the refresh after its own change but whose navigation or another page move later refreshes the lists; the oracle opens each page without a page move so this is refused
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
With a browser that accepts compression, a cold load of the spike's pages transfers the frontend JavaScript at about a third of today's bytes (about 1 MB instead of 3.4 MB).
```

```text
A byte measure on one page set does not show every JS file is compressed or that the figure holds on other pages.
```

None.

None.

None.

#### E2

##### Promises

P1

```text
A browser that does not accept compression still loads the same files, and the pages work.
```

```text
A bytes-on-the-line oracle does not see that the uncompressed path still serves complete, working files.
```

None.

None.

None.

#### E3

##### Promises

P2

```text
A browser that accepts compression receives the large API answers judged safe (metrics, feature lists, settings) compressed, in fewer bytes than today.
```

```text
The oracle does not see which answers were judged safe or whether any large safe answer was left uncompressed.
```

None.

None.

None.

#### E4

##### Promises

P3

```text
A page load or move to another page asks GET my-summary once in total, not once per gating component, and every gate still shows the right actions.
```

```text
A call count does not show that gating results are unchanged or that nothing reads my-summary outside useRbac().
```

None.

None.

None.

#### E5

##### Promises

P4

```text
When an administrator grants or removes the user's right while a page is open, the next page the user moves to shows the new actions and hides lost ones, without a reload.
```

```text
A single-user simulation does not see a real second session or the timing of when the change is picked up.
```

None.

None.

None.

#### E6

##### Promises

P5

```text
After the user creates a Team or Portfolio or changes a grant, the follow-on actions (such as admin actions on the new Team) show at once, without a reload.
```

```text
The oracle does not see every kind of own change or a stale gate shown briefly before the refresh.
```

None.

None.

None.

#### E7

##### Promises

P6

```text
When the permissions answer cannot be loaded, every gating component shows the same could-not-load state and fallback as today, and one Retry asks again once for all of them.
```

```text
Failure observed on one page does not show all components agree, or that Retry asks exactly once and recovers every component.
```

None.

None.

None.

#### E8

##### Promises

P7

```text
The Team list, Portfolio list and licence status used by the app shell and pages are each fetched once per load or move, not once per component.
```

```text
A call count does not show the shared lists and licence status still render the same content everywhere.
```

None.

None.

None.

#### E9

##### Promises

P8

```text
After the user creates, renames or deletes a Team or Portfolio, the menus, overview and lists show it at once, and a change by someone else shows on the next move to another page.
```

```text
The oracle does not see all surfaces showing the change or the other-user case in a real second session.
```

None.

None.

None.

#### E11

##### Promises

P10

```text
On page load the update check carries only whether an update exists and the newest version, not the release notes (368 KB today); the footer's version checks run at the same time, and if any fails the footer shows its error as today.
```

```text
The oracle does not see real concurrency timing or each individual failure case in the footer.
```

None.

None.

None.

#### E12

##### Promises

P11

```text
When the user opens the new-version dialog with an update available, it shows a loading look and then the same release notes as today.
```

```text
The oracle does not see the loading look on a slow connection or that the notes are identical to today's.
```

None.

None.

None.

#### E13

##### Promises

P12

```text
When release notes cannot load, the dialog still shows the newest version number, says the notes could not be loaded, and its Retry button asks again and shows the notes on success.
```

```text
The oracle does not see repeated failures or Retry after a recovered connection.
```

None.

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
