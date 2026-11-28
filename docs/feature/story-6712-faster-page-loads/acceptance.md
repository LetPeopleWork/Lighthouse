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
A server compressing only the version route would pass; the rule rows over many routes cover other GETs
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
Today's pipeline passes both; a rule that refuses POST but matches the hub only by exact path still passes the handshake row, so the hub GET rows in both cases in the rule table catch that
```

###### Obligations




##### Covers

P2

```text
The compression rule is asked about a GET of JSON with br, a HEAD of a script with gzip and a GET of a route that only starts with the letters logs
```

```text
Each answer is compressed
```

```text
Safe answers, including routes that only look like an excluded one, reach the browser compressed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

A\_safe\_answer\_to\_a\_client\_that\_accepts\_compression\_is\_compressed

```text
A rule that compresses everything passes these rows; the refusal rows fail it
```

###### Obligations




##### Covers

P2

```text
The compression rule is asked about POST, PUT and DELETE of JSON, a PNG, a problem answer, the update hub in lower and upper case and its negotiate POST, the logs routes, the embed handshake, all with br, and a GET of JSON without Accept-Encoding
```

```text
None of these answers is compressed
```

```text
Answers that can carry a secret, cannot be compressed, or were not asked for compressed go out as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

An\_answer\_that\_could\_carry\_a\_secret\_or\_is\_not\_asked\_for\_compressed\_is\_sent\_as\_it\_is

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

#### E10

##### Promises

P9

```text
A Features tab loads without the full Team settings (32 KB today) and shows the same rows and columns as today.
```

```text
A byte or call check does not show rows and columns are unchanged for all Team kinds and data.
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
