# Product brief: story\-6712\-faster\-page\-loads

## Request

```text
ADO User Story 6712 "Faster page loads on slow connections: compression, shared cache, smaller update check", as written on the board:

On a slow connection, Lighthouse pages take 8–20 s to show their data on a first visit, and 5–9 s when you click to a Team or Portfolio page. A measuring spike (2026-11-27, docs/feature/api-shape-evaluation/spike/findings.md) found that most of that time is spent sending uncompressed JavaScript, not on the number of API calls.

Fixes found by the spike
- Compression. The frontend is served with UseStaticFiles, which ignores the .gz files the build already makes, and API answers are not compressed. JavaScript: 3.4 MB raw vs 1.0 MB compressed. Expected gain on slow 4G: about 4–10 s per page.
- One shared cache for data every page uses. useRbac fetches the user's permissions once per component that uses it (72 repeated calls over 16 pages). Teams, portfolios, licence and Team settings repeat for similar reasons.
- A smaller update check. GET /version/new sends 368 KB of release notes on every first visit, only to show whether an update exists.
- Parallel version checks. The version footer runs 5 independent checks one after another.

No visible UI change; pages show the same data, sooner. Measure before and after with the spike's probe (measure-calls.mjs, measure-bytes.mjs).

Separate from the GraphQL / API-shape evaluation, which continues on its own and ends in an ADR.
```

## Work

M

```text
On a slow connection, Lighthouse pages show the same data sooner: less JavaScript and API bytes on the line and fewer repeated calls, with no change to what a page shows.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A browser that accepts compressed answers receives the frontend's JavaScript compressed: on a cold load of the spike's pages, JavaScript bytes on the line drop to about a third of today's (3.4 MB raw against about 1 MB compressed), measured with measure-bytes.mjs before and after. A browser that does not accept compression still gets the same, working files.
```

#### Amendment

None.

### P2

Outcome

```text
API answers that the design judges safe to compress reach a browser that accepts compression compressed, so large answers (metrics, feature lists, settings) take fewer bytes on the line. Which answers are safe is a DESIGN and DEVOPS decision.
```

#### Amendment

None.

### P3

Outcome

```text
Every component that gates on the user's permissions reads them from one shared answer: a page load or a move to another page asks GET my-summary once, not once per component (72 calls over the spike's 16 pages today), measured with measure-calls.mjs before and after. All gating still comes from useRbac().
```

#### Amendment

None.

### P4

Scenario

```text
Rights changed by someone else show on the next page
```

```text
A user has a page open and an administrator grants or removes one of their rights
```

```text
The user goes to another page
```

```text
That page shows the actions the user now has, and hides the ones they lost, without a reload, as today
```

#### Amendment

None.

### P5

Scenario

```text
The user's own change of rights shows at once
```

```text
A user creates a Team or Portfolio, or changes a grant themselves
```

```text
The change completes
```

```text
The actions that follow from it (for example the admin actions on the new Team) show at once, without a reload
```

#### Amendment

None.

### P6

Scenario

```text
Permissions that cannot be loaded behave as today
```

```text
The permissions answer cannot be loaded
```

```text
A page that gates on permissions is shown
```

```text
Every component sees the same could-not-load state and fallback as today, and one retry asks again once for all of them
```

#### Amendment

None.

### P7

Outcome

```text
The lists of Teams and Portfolios and the licence status that the app shell and pages use are fetched once and shared, not once per component, measured with measure-calls.mjs before and after.
```

#### Amendment

None.

### P8

Scenario

```text
The user's own change to a Team or Portfolio shows in the lists at once
```

```text
A user creates, renames or deletes a Team or Portfolio
```

```text
The change completes
```

```text
The menus, overview and lists show it at once; a change made by someone else shows on the user's next move to another page
```

#### Amendment

None.

### P10

Outcome

```text
On page load the update check sends only what the footer and the update pop-up show (whether an update exists and the newest version number), not the release notes (368 KB today). The version checks of the footer run at the same time, not one after another; if any one fails, the footer shows its error as today.
```

#### Amendment

None.

### P11

Scenario

```text
Release notes load when the dialog opens
```

```text
An update is available
```

```text
The user opens the new-version dialog
```

```text
The dialog shows a loading look, then the same release notes as today
```

#### Amendment

None.

### P12

Scenario

```text
Release notes that cannot be loaded
```

```text
An update is available and the release notes cannot be loaded
```

```text
The user opens the new-version dialog
```

```text
The dialog still shows the newest version number and says the release notes could not be loaded, with a Retry button that asks again
```

#### Amendment

None.

## Scope

```text
Compressed delivery of the built frontend files (JavaScript, CSS, HTML) by the Lighthouse server.
```

```text
Compression of API answers that the design judges safe.
```

```text
One shared, keyed cache for the user's permissions behind useRbac(), with the freshness of P4 and P5.
```

```text
One shared cache for the Teams list, the Portfolios list and the licence status, with the freshness of P8.
```

```text
Features tabs that no longer load full Team settings to render a list.
```

```text
An update check without release notes on page load; release notes loaded when the new-version dialog opens, with a loading and a failure look.
```

```text
The version footer's independent checks run in parallel, with today's error rule.
```

```text
Moving the other data hooks that fetch through useEffect (about 37 files) to TanStack Query.
```

```text
Making the Portfolio page's four-step load chain parallel (about 0.5 s on slow 4G); a possible later item.
```

```text
Fewer metrics calls (one composite read per page) and any API-shape change; ADR-288 keeps REST.
```

```text
Any change to what a page shows, apart from the loading and failure looks of the new-version dialog.
```

```text
Changes to the Lighthouse-Clients CLI or MCP; they call only /version/current.
```

```text
Removing the per-Team settings fetch on the Portfolio page. The Feature WIP quick setting reads each Team's feature WIP from it, so the fetch stays as today.
```

## Decisions

```text
Faster is proven by bytes and calls, measured with the spike's probes (measure-bytes.mjs, measure-calls.mjs) before and after; slow-4G times are reported as evidence, not promised, because emulated timings are noisy. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
Permission freshness stays as today: shared cache, asked again when the user moves to another page, and at once after the user's own create or grant actions. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
Team, Portfolio and licence lists follow the same rule: the user's own change shows at once, others' changes on the next page move. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
Release notes load when the new-version dialog opens; the pop-up and footer need only the newest version number. If the notes cannot be loaded the dialog shows a message and a Retry button. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
The parallel version checks keep today's error rule: any failure shows the footer error. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
In scope beyond permissions: Teams and Portfolios lists, licence, and Team settings on lists. Out: the other useEffect hooks and the Portfolio chain. (Maintainer, 2026-11-28)
```

#### Status

Confirmed


```text
The Story says the build already makes .gz files; no compression step was found in vite.config.ts or the pipeline. DESIGN decides how the files get compressed (at build or at serve time) and how they are served (MapStaticAssets, or a provider beside UseStaticFiles), keeping index.html uncacheable (Bug #5732).
```

#### Status

Open

Design

```text
Which API answers are safe to compress over HTTPS (BREACH: an answer that mixes a secret with text an attacker controls), and whether reverse proxies, the Docker image and Standalone change the answer (DESIGN with DEVOPS).
```

#### Status

Open

Design

```text
What a Features tab reads instead of full Team settings.
```

#### Status

Open

Design

```text
Suggestion, not in scope: a later item for the Portfolio page's four-step load chain, and for moving the remaining useEffect data hooks to the shared query cache.
```

#### Status

Proposed


```text
S4 dropped (maintainer, 2026-11-28): the Portfolio page keeps its per-Team settings fetch, because the Feature WIP quick setting reads featureWIP from it. Loading it on dialog open or adding featureWIP to the portfolio answer would change the UI or a shared contract.
```

#### Status

Confirmed


## Journey

```text
On a slow connection, the user opens Lighthouse for the first time (cold load): the compressed frontend arrives, the shell asks for permissions, Teams, Portfolios and licence once, and the update check sends no release notes.
```

```text
emotion: not supplied
```

```text
The user clicks to a Team or Portfolio page (in-app): the page's compressed JavaScript and answers arrive; the shared permissions and lists are reused or asked again once.
```

```text
emotion: not supplied
```

```text
The user creates a Team: its admin actions and its place in the lists show at once.
```

```text
emotion: not supplied
```

```text
An update is available: the pop-up shows the newest version; the user opens the dialog, sees a loading look, then the release notes.
```

```text
emotion: not supplied
```

```text
Recovery: the release notes cannot be loaded; the dialog says so, the user presses Retry and the notes load.
```

```text
emotion: not supplied
```

```text
Recovery: the permissions cannot be loaded; every component shows today's fallback, one retry asks again for all of them.
```

```text
emotion: not supplied
```

## Slices

### S1

```text
Walking skeleton: on a cold load from the real Lighthouse server, a browser that accepts compression receives the frontend's JavaScript compressed, and safe API answers compressed; a browser that does not still gets working files. Before/after bytes from measure-bytes.mjs.
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

M

```text
The biggest gain: 4–10 s per slow page.
```

##### Rework

```text
The answers that are never compressed are chosen by an endpoint marker, NeverCompressResponseAttribute on LogsController, EmbedHandshakeController and UpdateNotificationHub, instead of a list of path literals in BreachSafeResponseCompressionProvider.
```

```text
The maintainer asked, 2026-11-28: could we NOT use literal paths and have something smarter instead? A renamed controller route silently drops its exclusion from a hand-typed path list.
```

### S2

```text
Every component that gates on permissions reads one shared answer: one my-summary call per page load or page move, rights changed by others show on the next page, the user's own changes at once, and a failed load behaves as today with one retry for all.
```

#### Promises

P3

P4

P5

P6

#### Depends on

None.

#### Slice work

M

```text
72 repeated calls gone over 16 pages; RBAC gating unchanged.
```

##### Rework

None.

### S3

```text
The Teams and Portfolios lists and the licence status are fetched once and shared; the user's own create, rename or delete shows in the lists at once.
```

#### Promises

P7

P8

#### Depends on

None.

#### Slice work

M

```text
Fewer repeated shell calls on every load.
```

##### Rework

None.

### S5

```text
On page load the update check sends no release notes and the footer's checks run at the same time; the new-version dialog loads the notes when opened, with a loading look and a could-not-load message with Retry.
```

#### Promises

P10

P11

P12

#### Depends on

None.

#### Slice work

S

```text
368 KB less on every first visit when an update exists; a shorter footer chain.
```

##### Rework

None.
