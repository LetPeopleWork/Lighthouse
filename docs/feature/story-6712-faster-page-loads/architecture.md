# Architecture brief: story\-6712\-faster\-page\-loads

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented (settled by CLAUDE.md). Backend: ports-and-adapters, compression is a host (platform lane) concern owned by Program.cs as composition root plus one policy class. Frontend: React hooks over TanStack Query; each shared read has one owning hook module that holds its query key, and one page-move freshness owner.
```

#### D2

```text
Open question 1 resolved (compression of the built frontend): compress at serve time with ASP.NET Core ResponseCompression (shared framework, no new package), Brotli and Gzip providers both at CompressionLevel.Optimal, never SmallestSize (Brotli quality 11 is about 140 times slower: 3510 ms against 25 ms on the 2.43 MB largest JavaScript file, x86_64). No size threshold: a threshold can only act where Content-Length is known, which is the static files, where compression pays most. app.UseResponseCompression placed after UseForwardedHeaders and before UseDefaultFiles/UseStaticFiles so static files, the SPA fallback page and API answers pass through one mechanism. UseStaticFiles(staticFileOptions) and the UseSpa DefaultPageStaticFileOptions stay unchanged, so index.html and sw.js keep no-cache, no-store, must-revalidate on both paths. Measured on the current bundle (3361 KB of JavaScript): Optimal gives 3.22x gzip and 3.23x Brotli; Fastest gives only 2.32x gzip, which misses the about-a-third outcome. Server CPU is about 35 ms per cold load of the bundle in one encoding (measured in DEVOPS, x86_64). Rejected: MapStaticAssets (needs the static web assets manifest built after pnpm build fills wwwroot, replaces the StaticFileOptions path that the no-cache rule lives on, and is empty in dev); a Vite precompression plugin plus a custom precompressed-file provider (new dependency and hand-written content negotiation to save about 35 ms CPU per cold load).
```
Governed slices: S1

#### D3

```text
Open question 2 resolved (BREACH): an answer is compressed only when ResponseCompression:Enabled is true, the request method is GET or HEAD, the response MIME type is in ResponseCompressionDefaults.MimeTypes, the client sent Accept-Encoding with br or gzip, and the path is not an excluded route. Excluded routes, matched on path segments, case-insensitive, by prefix: the SignalR hub api/updateNotificationHub (and everything under it, such as negotiate); the logs controller api/{v1|latest}/logs (tail, download, problems, level), because its answers hold log text an attacker can reach (logged paths, failed sign-ins, tracker queries) next to exception text from third-party libraries that this design cannot show is free of secrets, and an administrator can raise the log level at run time; and the embed handshake api/{v1|latest}/embed/handshake, the one GET whose body carries a secret (the single-use embed session token). The handshake body reflects no attacker text and its nonce is spent on first read, so it is not exploitable today; it is excluded so the rule stays simple: no compressed answer carries a secret, and the answer is a few hundred bytes. Every other GET carries no secret: connection options blank secret values (WorkTrackingSystemConnectionOptionDto sets Value empty when IsSecret), GET api-keys never returns the plaintext (ApiKeyController.GetApiKeys), the OAuth callback answers a redirect with a connection id, session and embed cookies travel in headers, which ResponseCompression never compresses; the plain-text API key of ApiKeyCreationResult answers a POST. The embed cookie is SameSite=None, so cross-site GETs carry it; no GET in embed scope other than the excluded handshake returns a token. EnableForHttps is true because the rule, not the scheme, is the mitigation, so TLS at a proxy does not change the answer. Owner in code: BreachSafeResponseCompressionProvider in Platform/Host, a subclass of ResponseCompressionProvider overriding ShouldCompressResponse; the hub route is one named constant on UpdateNotificationHub read by both MapHub and the policy; the logs and handshake prefixes are named constants in the provider. No PathBase is used, so the segments are matched on Request.Path. Platform facts (DEVOPS): the Docker image serves HTTPS 443 with nothing in front; the Helm chart's plain Ingress (ingress-nginx, Traefik) does not compress by default and passes an already encoded answer through; a proxy that strips Accept-Encoding gets identity answers; the Standalone webview loads its UI from the app bundle and only its loopback API answers are compressed, about 1 ms each; one behaviour everywhere.
```
Governed slices: S1

#### D4

```text
Open question 3 resolved (Features tab reads instead of full Team settings): it reads nothing extra. PortfolioDetail.fetchPortfolio calls teamService.getTeamSettings once per involved Team and stores the result in the involvedTeams state, which no code reads; the Features tab (PortfolioFeatureList) and the Deliveries tab already render from portfolio.involvedTeams, the entity references inside the portfolio answer. So the per-Team settings fetch and the dead state are removed; updateTeamSettingsFromPortfolio keeps its on-demand read before a save. Team pages do not load Team settings for their Features tab. Side effect: the Portfolio load chain loses one step; the rest of that chain stays out of scope.
```
Governed slices: S4

#### D5

```text
S2: useRbac keeps the RbacState shape (isLoading, loadFailed, retry, isRbacEnabled, isSystemAdmin, canCreateTeam, canCreatePortfolio, isTeamAdmin, isPortfolioAdmin, summary) so its 20 consumers do not change. The permissions query key lives only in useRbac.ts; failure keeps today’s permissive fallback with loadFailed true for every component; retry calls refetch once for all. useRefreshPermissions, exported from useRbac.ts, is called after the user’s own grant actions (useScopedAccess, RbacSettings, EveryoneSignedInSection) and own Team or Portfolio creation (EditTeam, EditPortfolio).
```
Governed slices: S2

#### D6

```text
S3: useUnitLists.ts owns the Teams and Portfolios keys (reusing the existing teams and portfolios keys of useLicenseRestrictions) with useTeamsQuery, usePortfoliosQuery and useUnitListsChanged; the licence keeps useLicenseStatusQuery (LICENSE_STATUS_KEY, its hourly refetch) and joins the page-move refresh. useLicenseRestrictions, OverviewDashboard, EditPortfolio, ApiKeysSettings and SurveyNudge read these hooks instead of calling the services. useUnitListsChanged is called after own create (EditTeam, EditPortfolio), rename (TeamDetail and PortfolioDetail settings saves, EditTeam/EditPortfolio updates) and delete (OverviewDashboard). useLicenseRestrictions keeps its 403-as-empty rule.
```
Governed slices: S3

#### D7

```text
S5: new route GET version/newest on VersionController (SignedInOnly, same reason as hasupdate) returns the newest release version string from a new LighthouseReleaseService.GetNewestReleaseVersion (the first of GetNewReleases, so the number equals today’s newReleases[0].version), 404 when there is none, as /version/new. GET version/new stays for the dialog. LighthouseVersion runs current, distribution and hasupdate together with Promise.all; when an update exists it runs newest and, for lighthouse-internal, updateSupported together; any rejection sets the footer error as today. Only data dependencies remain, no order between independent checks. LatestReleaseInformationDialog receives the newest version and reads the release notes with useQuery (key version, newReleases; enabled only while open) and has three states: loading (the dialog with the newest version and a CircularProgress where the notes go), loaded (today’s notes), failed (the newest version, the message Could not load the release notes. and a Retry button that refetches). Sketch at the start of DELIVER.
```
Governed slices: S5

#### D8

```text
Formal verification: claim-to-checker. Shared-read freshness (S2, S3: one request per page, no earlier-page answer for a new component, cancelled earlier-page requests, own change asked at once) has several actors (components, navigation, network, user) on one cache, so it is modelled in Quint (shared_page_reads.qnt), checked with quint run and quint verify (Apalache, bounded). No model for compression and the BREACH rule: a pure decision over one request with no shared state or multi-step protocol; its finite table is checked by unit rows and the wire contract. No model for the Features tab read: removal of an unread fetch, no state window. No model for the parallel version checks: a single Promise.all with one shared error flag, no shared mutable state between actors; finite example rows cover any-failure. Liveness: no liveness claim, because eventual arrival of a page’s answer depends on the HTTP answer arriving, which this design does not control; safety is the claim. No TLA+ or Agda needed.
```

#### D9

```text
Tests: Lighthouse test-writing skill wins over nWave test rules (nine-run timing, PBT density). Frontend rules on hooks in .test.ts/.test.tsx with renderWithQueryClient; backend S1 is a wire contract on WireContractTest because only a running host shows Content-Encoding and headers produced by middleware order; the policy rule itself is pinned by unit rows on BreachSafeResponseCompressionProvider over DefaultHttpContext. No nWave tags in test code.
```

#### D10

```text
Page-move freshness (S2, S3): one owner, usePageFreshQuery in src/hooks/usePageFreshQuery.ts, wraps useQuery with staleTime Infinity, retry false and refetchOnMount false; its queryFn stores the answer together with the page it was asked on (carrier PageFreshAnswer of T: answer and askedOnPage). PageMoveRefresh in src/components/App/PageMoveRefresh/PageMoveRefresh.tsx, mounted inside the Router in App.tsx, holds the page counter (per-process state, a React context), advances it when location.pathname changes and invalidates the shared page reads once (invalidateQueries cancels a request still in flight from the previous page). A component records the page it mounted on; it is loading until an answer asked on or after that page arrives, failed when the shared query failed, ready otherwise. Components mounted later on the same page reuse the cached answer and ask nothing. Model shared_page_reads.qnt checks this protocol.
```
Governed slices: S2, S3

#### D11

```text
S1 switch: ResponseCompressionConfiguration in Configuration/ResponseCompressionConfiguration.cs (SectionName ResponseCompression, Enabled default true), read once at start-up in Program.cs the way RateLimitingConfiguration is (GetSection(SectionName).Get, else a new instance), registered as a singleton and given to BreachSafeResponseCompressionProvider, which answers no for every request when Enabled is false, so the switch is one row of the policy table and the pipeline shape is the same either way. Environment variable ResponseCompression__Enabled=false or --ResponseCompression:Enabled=false; a restart is needed; no Helm chart value. docs/Installation/configuration.md gets one section in the format of the Telemetry block: Lighthouse compresses its own answers, a reverse proxy needs no compression of its own, and how to turn it off.
```
Governed slices: S1

#### D12

```text
S1 accepted behaviours: an identity answer to a request without Accept-Encoding carries no Vary header (never wrong; at worst a shared cache serves identity to a client that could take br); StaticFiles gives one strong ETag for every encoding (harmless with Vary, no CDN in the repository); application/problem+json and image/svg+xml stay uncompressed because they are not on the default MIME list. Usage data: N/A, because nothing here is a user action; the outcome (bytes and calls per page) is measured by the spike's probes, so no UsageDataEventName entry. Observability: the Content-Encoding and Vary response headers show it is on; no metric or alert, because no service-level promise changes.
```
Governed slices: S1

#### D13

```text
S1 test layers: unit rows in the default fast suite (no host) pin the rule as a table over DefaultHttpContext in BreachSafeResponseCompressionProviderTest; one named wire contract (ResponseCompressionWireContractTest on WireContractTest) pins pipeline order and headers through a real host; the Docker smoke step in .github/workflows/ci_docker.yml, after Check the container runs on .NET 11, covers real TLS and the real bundle: curl -k a built asset with Accept-Encoding br and fail unless content-encoding is br, and curl /api/latest/systeminfo without Accept-Encoding and fail if content-encoding is present. No other workflow change: E2E already runs Chromium, which sends Accept-Encoding, against the server. Host verification probe after S1 (the host runs it against a running instance LH): take the first /assets/*.js named in GET /; GET it with Accept-Encoding br (expect content-encoding br and vary Accept-Encoding) and without the header (no content-encoding); GET /api/latest/version/current with gzip, br (br); GET / with br (br, and Cache-Control no-cache, no-store, must-revalidate kept); POST api/updateNotificationHub/negotiate (no content-encoding).
```
Governed slices: S1

### Constraints

#### K1

```text
No visible UI change except the new-version dialog loading and could-not-load looks; pages show the same data.
```

#### K2

```text
All UI permission gating derives from useRbac(); only useRbac.ts calls rbacService.getAuthorizationSummary.
```

#### K3

```text
index.html and sw.js keep Cache-Control no-cache, no-store, must-revalidate on the static and the SPA fallback path (Bug #5732); unmatched requests keep their 404.
```

#### K4

```text
A client that sends no Accept-Encoding receives identical uncompressed bytes as today; Lighthouse-Clients call only /api/v1/version/current, which is unchanged.
```

#### K5

```text
Faster is proven by bytes and calls with measure-bytes.mjs and measure-calls.mjs before and after; slow-4G times are evidence, not promised. No numeric bound on compression ratio is promised beyond about a third; the design does not add a size threshold for compression.
```

#### K6

```text
No EF migration; no API shape change except the added GET version/newest; ADR-288 keeps REST.
```

#### K7

```text
Out of scope: the other useEffect data hooks, the Portfolio load chain beyond removing the unread Team settings step, metrics call count.
```

#### K8

```text
Faster is proven by bytes and calls, measured with the spike's probes (measure-bytes.mjs, measure-calls.mjs) before and after; slow-4G times are reported as evidence, not promised, because emulated timings are noisy. (Maintainer, 2026-11-28)
```

#### K9

```text
Permission freshness stays as today: shared cache, asked again when the user moves to another page, and at once after the user's own create or grant actions. (Maintainer, 2026-11-28)
```

#### K10

```text
Team, Portfolio and licence lists follow the same rule: the user's own change shows at once, others' changes on the next page move. (Maintainer, 2026-11-28)
```

#### K11

```text
Release notes load when the new-version dialog opens; the pop-up and footer need only the newest version number. If the notes cannot be loaded the dialog shows a message and a Retry button. (Maintainer, 2026-11-28)
```

#### K12

```text
The parallel version checks keep today's error rule: any failure shows the footer error. (Maintainer, 2026-11-28)
```

#### K13

```text
In scope beyond permissions: Teams and Portfolios lists, licence, and Team settings on lists. Out: the other useEffect hooks and the Portfolio chain. (Maintainer, 2026-11-28)
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

Admitted

```text
Shared read is loading, ready (asked on or after the component's mount page) or failed (permissive fallback for permissions)
```

usePageFreshQuery

Excluded

```text
A component mounted on the current page reports ready with an answer asked on an earlier page
```

usePageFreshQuery

Unknown

```text
Behaviour of third-party reverse proxies with encoded upstream answers
```

DEVOPS

### Models

Quint

docs\/feature\/story\-6712\-faster\-page\-loads\/design\/shared\_page\_reads\.qnt

##### Properties

pageComponentsSeeOnlyThisPage

Invariant

```text
A component of the current page never treats an answer asked on an earlier page as current
```

usePageFreshQuery

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

oneRequestPerPage

Invariant

```text
Requests on one page are at most one plus one per retry and per own change
```

usePageFreshQuery

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

noEarlierPageRequestInFlight

Invariant

```text
A request asked on an earlier page never lands after a page move
```

PageMoveRefresh

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx

ownChangeIsAsked

Invariant

```text
After the user’s own change an answer missing it is never the latest word without a refetch in flight
```

useRefreshPermissions

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

##### Atoms

State

page

PageMoveRefresh

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx

None.

State

cache

PageFreshAnswer

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

None.

State

mountedOn

usePageFreshQuery

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

None.

State

inflight

QueryClient

Lighthouse\.Frontend\/src\/App\.tsx

```text
model-only: TanStack in-flight fetch
```

Action

movesToAnotherPage

PageMoveRefresh

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx

None.

Action

componentMounts

usePageFreshQuery

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

None.

Action

userRetries

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

None.

Action

ownChangeCompletes

useRefreshPermissions

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

None.

##### Assumptions

```text
TanStack invalidateQueries cancels a request in flight and starts one refetch for active observers
```

```text
Bounded to 3 page moves, 3 observers, 2 retries and 2 own changes per page; network answers or fails per request
```

##### Mutants

###### N1

```text
Readiness ignores the page the answer was asked on
```

###### Kills

pageComponentsSeeOnlyThisPage

###### N2

```text
A mounting component asks again when nothing is in flight (staleTime 0 behaviour)
```

###### Kills

oneRequestPerPage

###### N3

```text
A page move keeps a request still in flight from the previous page
```

###### Kills

noEarlierPageRequestInFlight

###### N4

```text
An own change completes without asking again
```

###### Kills

ownChangeIsAsked

### Closure

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: no GET answer carries a secret (a future GET returns a token next to reflected query text)
```

```text
POST answers stay uncompressed
```

```text
That GET answer is compressed
```

```text
Silent claim that every compressed answer is BREACH-safe
```

```text
Endpoint authors must answer secrets on unsafe methods or extend BreachSafeResponseCompressionProvider; recorded in the ADR consequences
```

BreachSafeResponseCompressionProvider

#### Y2

Stressor

```text
Assumption negated: the client decodes what it asked for (a proxy strips or rewrites Accept-Encoding)
```

```text
Identity answer when Accept-Encoding is absent
```

```text
No compression behind such a proxy
```

```text
An encoded body without Content-Encoding, or double encoding
```

```text
Checked in DEVOPS: Docker serves HTTPS with nothing in front; the Helm Ingress (ingress-nginx, Traefik) does not compress by default and passes encoded answers through; a stripping proxy gets identity; Standalone compresses only loopback API answers. No residue
```

BreachSafeResponseCompressionProvider

#### Y3

SinglePointOfFailure

```text
PageMoveRefresh is the only trigger that asks shared reads again on a page move
```

```text
Own changes still refresh through useRefreshPermissions and useUnitListsChanged
```

```text
Permissions stay as of the last ask if it is not mounted
```

```text
A page component shows an earlier-page answer as ready
```

```text
Readiness is decided per component from askedOnPage, so a missing trigger shows loading, never stale-as-current; App.tsx mounts it once inside the Router
```

PageMoveRefresh

#### Y4

Bypass

```text
A component calls rbacService.getAuthorizationSummary or teamService.getTeams directly
```

```text
useRbac and useUnitLists remain the owners
```

```text
none
```

```text
A second reader with its own freshness
```

```text
Architectural test on the frontend source scans every non-test file for direct callers
```

useRbac

#### Y5

Stressor

```text
Second stressor set (walkthrough, not execution): server CPU spike under many cold loads; an answer that arrives after the user moved twice; a browser tab restored from bfcache; a licence import while on a page
```

```text
Compression CPU about 35 ms per cold bundle at Optimal, assets reused from browser cache; late answer cancelled by invalidation; bfcache restore keeps the last page answers and the next move asks again; licence import sets the cache at once through useStoreImportedLicense
```

```text
Slower first byte under load
```

```text
A wrong or stale permission shown as ready
```

```text
An operator on a small host turns compression off with ResponseCompression__Enabled=false and a restart
```

ResponseCompressionConfiguration

#### Y6

Stressor

```text
Assumption negated: no compressible GET body holds a secret (a log line holds a token from library exception text, or a future GET returns a token)
```

```text
Log routes and the embed handshake are never compressed; POST answers stay uncompressed
```

```text
Log downloads go uncompressed
```

```text
A compressed body that mixes a secret with attacker-reachable text
```

```text
A new GET that returns a secret must answer a POST or be added to the excluded routes of BreachSafeResponseCompressionProvider; stated in ADR-004
```

BreachSafeResponseCompressionProvider

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/BreachSafeResponseCompressionProvider\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Configuration\/ResponseCompressionConfiguration\.cs

Create

```text
Start-up switch ResponseCompression:Enabled, default true
```

docs\/Installation\/configuration\.md

Extend

```text
Prose target, verified by review and EXAMINE, no test pins its wording: Lighthouse compresses its own answers, no proxy compression needed, how to turn it off
```

\.github\/workflows\/ci\_docker\.yml

Extend

```text
Docker smoke step: an asset answers br over real TLS; systeminfo without Accept-Encoding has no Content-Encoding
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ResponseCompressionWireContractTest|FullyQualifiedName~BreachSafeResponseCompressionProviderTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~SpaFallbackRoutingTests
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/BreachSafeResponseCompressionProviderTest\.cs

None.

None.

#### Public oracle

```text
On a test host whose web root holds index.html and assets/app.js (a few KB of repetitive JavaScript): GET /assets/app.js with Accept-Encoding br, then gzip, then none; GET / with br; GET /api/latest/version/current with gzip, br, and again without Accept-Encoding; POST the API-key creation with br; POST api/updateNotificationHub/negotiate with br
```

```text
br and gzip rows carry Content-Encoding br or gzip, Vary contains Accept-Encoding and the body decodes to the file bytes exactly; the no-encoding row has no Content-Encoding and the file bytes; GET / is compressed and still Cache-Control no-cache, no-store, must-revalidate; the API GET with gzip, br carries Content-Encoding br and Vary Accept-Encoding, without Accept-Encoding it has no Content-Encoding; the API-key POST and the hub negotiate POST have no Content-Encoding
```

```text
Today's pipeline: no Content-Encoding on any row, so the br, gzip and API GET rows fail; a compress-everything policy fails the POST rows; a policy that matches the hub by exact path or case fails the negotiate row
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

UseForwardedHeaders

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:166

Reuse

```text
Shared-framework ResponseCompression middleware is inserted right after UseForwardedHeaders; it does negotiation, Vary and skips already-encoded answers; no compression owner exists today
```

staticFileOptions

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:179

Reuse

```text
Keeps the no-cache rule for index.html and sw.js on both paths unchanged
```

BreachSafeResponseCompressionProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/BreachSafeResponseCompressionProvider\.cs\:1

CreateNew

```text
No class decides which answers may be compressed; the default provider decides by MIME only
```

UpdateNotificationHub

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs\:1

Extend

```text
Owns its route as a named constant so MapHub and the policy agree on one literal
```

WireContractTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/WireContractTest\.cs\:17

Reuse

```text
One host per fixture for the wire rows; AdjustTheHost sets a temporary web root as SpaFallbackWebApplicationFactory does
```

RateLimitingConfiguration

Lighthouse\.Backend\/Lighthouse\.Backend\/Configuration\/RateLimitingConfiguration\.cs\:3

Reuse

```text
Pattern reused for the switch: SectionName constant, Enabled default true, read at start-up in Program.cs; its section is rate limiting, so it does not own compression
```

ResponseCompressionConfiguration

Lighthouse\.Backend\/Lighthouse\.Backend\/Configuration\/ResponseCompressionConfiguration\.cs\:1

CreateNew

```text
No configuration section owns compression; one value object holds the switch and is read once
```

LogsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:85

Reuse

```text
Unchanged; its routes api/{v1|latest}/logs are an excluded prefix of the policy
```

Poll

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs\:24

Reuse

```text
Unchanged; the only GET whose body carries a token, so api/{v1|latest}/embed/handshake is an excluded prefix
```

Check the container runs on \.NET 11

\.github\/workflows\/ci\_docker\.yml\:99

Extend

```text
The existing Docker smoke job already runs the container over HTTPS 443; one step after it checks Content-Encoding
```

Telemetry

docs\/Installation\/configuration\.md\:446

Extend

```text
The configuration page is where operators read switches; the new section follows the Telemetry block format
```

##### Impact

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:194

Changed

```text
Reads ResponseCompressionConfiguration at start-up, adds AddResponseCompression with Brotli and Gzip at Optimal and the policy registration, UseResponseCompression before UseDefaultFiles; MapHub reads the hub route constant
```

class

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/BreachSafeResponseCompressionProvider\.cs\:1

Changed

```text
New policy
```

class

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs\:1

Changed

```text
Route constant added, behaviour unchanged
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SpaFallbackRoutingTests\.cs\:1

UnchangedCompatible

```text
Status, media type and no-store assertions hold with compression; test client sends no Accept-Encoding
```

configuration

Lighthouse\.Backend\/Lighthouse\.Backend\/Configuration\/ResponseCompressionConfiguration\.cs\:1

Changed

```text
New switch section; absent section means enabled
```

ci\-workflow

\.github\/workflows\/ci\_docker\.yml\:108

Changed

```text
One smoke step added after the runtime check
```

operator\-docs

docs\/Installation\/configuration\.md\:446

Changed

```text
One section on compression and its switch
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:85

UnchangedCompatible

```text
Its answers stay identity, as today
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs\:24

UnchangedCompatible

```text
Its answer stays identity, as today
```

helm\-chart

chart\/values\.yaml\:21

UnchangedCompatible

```text
No chart value for the switch; the Ingress passes encoded answers through
```

##### Agreement

HTTP content encoding of Lighthouse answers

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:194

Producer

Migrated

```text
Starts compressing safe-method answers when asked
```

HTTP content encoding of Lighthouse answers

Lighthouse\.Frontend \(browsers and Standalone webview\)

Consumer

UnchangedCompatible

```text
Browsers and the WebKit webview send Accept-Encoding and decode transparently
```

HTTP content encoding of Lighthouse answers

Lighthouse\-Clients CLI and MCP \(\/api\/v1\/version\/current\)

Consumer

UnchangedCompatible

```text
Node fetch decodes br and gzip transparently; without Accept-Encoding they get identity
```

HTTP content encoding of Lighthouse answers

Self\-hosted reverse proxies and the Helm Ingress \(chart\/templates\/ingress\.yaml\)

Consumer

UnchangedCompatible

```text
ingress-nginx and Traefik do not compress by default and pass an encoded answer through; a proxy that strips Accept-Encoding gets identity
```

##### Not applicable

prefactoring

```text
Program.cs holds one type (Program); UpdateNotificationHub.cs holds one type; BreachSafeResponseCompressionProvider.cs and ResponseCompressionConfiguration.cs are new; ci_docker.yml and configuration.md hold no types. No file holds several types, nothing moves.
```

##### Boundaries

```text
HTTP GET of a built frontend file or an API route on the Lighthouse host (Kestrel pipeline in Program.cs)
```

###### Driven ports

```text
wwwroot static files
```

```text
Program.cs (composition root) registers BreachSafeResponseCompressionProvider; the provider depends only on ASP.NET Core abstractions and the UpdateNotificationHub route constant
```

###### Failures

####### B1

```text
Client sends no Accept-Encoding or an unsupported one
```

Refusal

```text
Identity answer, same bytes as today
```

##### Observations

```text
Content-Encoding and Vary headers plus decoded body of an answer
```

HTTP headers and bytes

```text
Which compression level was used; CPU cost
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
Decoding a compressed static answer yields the file bytes exactly, for br, gzip and identity
```

RoundTrip

Infrastructure

BreachSafeResponseCompressionProvider

###### Treatment

Finite

###### Cases

```text
br
```

```text
gzip
```

```text
identity
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

None.

###### L2

```text
An answer is compressed iff the switch is on, method is GET or HEAD, MIME is compressible, Accept-Encoding offers br or gzip and no excluded route prefix (hub, logs, embed handshake) matches the path segments, case-insensitive
```

Invariant

Adapter

BreachSafeResponseCompressionProvider

###### Treatment

Finite

###### Cases

```text
GET json br -> compressed
```

```text
HEAD js gzip -> compressed
```

```text
POST json br -> not
```

```text
PUT json br -> not
```

```text
DELETE json br -> not
```

```text
GET image/png br -> not
```

```text
GET application/problem+json br -> not
```

```text
GET api/updateNotificationHub br -> not
```

```text
GET API/UPDATENOTIFICATIONHUB br -> not
```

```text
POST api/updateNotificationHub/negotiate br -> not
```

```text
GET api/latest/logs br -> not
```

```text
GET api/v1/Logs/download br -> not
```

```text
GET api/latest/embed/handshake/n1 br -> not
```

```text
GET api/latest/logsx br -> compressed (segment match, not substring)
```

```text
GET json no Accept-Encoding -> not
```

```text
GET json br with ResponseCompression:Enabled false -> not
```

```text
The policy inputs form a closed finite table of method, MIME class, encoding offer, route class and switch, so native example rows cover every class
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
No answer to an unsafe-method request is compressed
```

```text
BREACH recovery of the API key echoed in a compressed POST answer
```

BreachSafeResponseCompressionProvider

```text
Any HTTP request reaching Kestrel
```

BreachSafeResponseCompressionProvider\.ShouldCompressResponse

ResponseCompressionMiddleware

```text
Content-Encoding absent on POST, PUT, PATCH, DELETE answers
```

```text
A future GET answer carrying a secret; middleware placed before UseResponseCompression writing its own encoding
```

###### C3

```text
No compressed answer carries a secret: log text and the embed handshake token are never compressed
```

```text
BREACH recovery of a token from a compressed body that also holds attacker-reachable text
```

BreachSafeResponseCompressionProvider

```text
Any HTTP request reaching Kestrel
```

BreachSafeResponseCompressionProvider\.ShouldCompressResponse

ResponseCompressionMiddleware

```text
Content-Encoding absent on api/{v1|latest}/logs and api/{v1|latest}/embed/handshake answers
```

```text
A future GET answer carrying a secret on a route not in the excluded prefixes
```

##### States

None.

##### Models

None.

##### Closure

```text
compressed static and GET answers
```

Safety

Pass

DesignedNotBuilt

```text
Policy table of unit rows and the POST wire row
```

###### Evidence

None.

```text
compressed static and GET answers
```

Inhabitation

Pass

DesignedNotBuilt

```text
Shared-framework Brotli and Gzip providers; probe compressed the real bundle 3.2x
```

###### Evidence

None.

```text
compressed static and GET answers
```

PublicReachability

Pass

DesignedNotBuilt

```text
Kestrel pipeline: UseResponseCompression before UseStaticFiles and MapControllers, observed by the wire contract
```

###### Evidence

None.

```text
compressed static and GET answers
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: a single synchronous request-response transformation, no waiting state
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
A test that starts a host is a named skeleton or a wire contract; the new compression contract sits in Tests/Contracts on WireContractTest
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~TestHostUseGuardTest
```

##### Contract tests

###### T2

```text
Browsers, proxies and the Standalone webview change apart from the server; the encoding negotiation is a wire contract
```

HTTP Accept\-Encoding \/ Content\-Encoding \/ Vary on static and GET API answers

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

HttpClient with automatic decompression as browser stand\-in

```text
Headers and decoded bytes per row
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/ResponseCompressionWireContractTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ResponseCompressionWireContractTest
```

### S2

#### Targets

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts

Extend

None.

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts

Create

None.

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx

Create

None.

Lighthouse\.Frontend\/src\/App\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/EveryoneSignedInSection\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/hooks\/useRbac\.test\.ts

Extend

```text
Existing tests gain the QueryClient wrapper
```

Lighthouse\.Frontend\/src\/hooks\/useRbac\.sharedAnswer\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/hooks/useRbac.sharedAnswer.test.tsx src/hooks/useRbac.test.ts src/hooks/useRbacGate.test.ts
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render three components that call useRbac() under one QueryClient, PageMoveRefresh and a MemoryRouter; change the route; change the mocked summary between moves; call useRefreshPermissions; make the summary reject then call retry
```

```text
getAuthorizationSummary called once for the three components on a page and once per route change; after a move the new page components are loading until the answer asked after the move, then show the new grants; after useRefreshPermissions the new summary shows without a route change; on rejection every component has loadFailed true with the permissive fallback, and one retry calls the service once
```

```text
Today’s useRbac: three calls for three components, and no refresh for components that stay mounted
```

##### Promises

P3

P4

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:33

Extend

```text
Owner of permission gating; keeps RbacState, swaps its useEffect fetch for the shared query
```

QueryClient

Lighthouse\.Frontend\/src\/App\.tsx\:70

Reuse

```text
Existing shared cache; no second cache
```

usePageFreshQuery

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts\:1

CreateNew

```text
No owner decides page-move freshness; useLicenseRestrictions uses staleTime 0, which repeats per mount
```

PageMoveRefresh

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx\:1

CreateNew

```text
Nothing observes page moves for shared reads today; remounting did it implicitly
```

useScopedAccess

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts\:1

Extend

```text
Owner of own grant and removal mutations on a unit; calls useRefreshPermissions after success
```

RbacSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Extend

```text
Owner of system-admin grants; calls useRefreshPermissions after success
```

EveryoneSignedInSection

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/EveryoneSignedInSection\.tsx\:1

Extend

```text
Owner of the everyone-signed-in rule; calls useRefreshPermissions after success
```

createTeam

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx\:53

Extend

```text
Own create: the creator becomes Team Admin, so permissions are asked again at once
```

createPortfolio

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:78

Extend

```text
Own create: the creator becomes Portfolio Admin, so permissions are asked again at once
```

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.test\.ts\:1

Extend

```text
Existing hook tests stay, rendered with renderWithQueryClient
```

##### Impact

hook

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:33

Changed

```text
Shared query; RbacState shape unchanged
```

consumers

Lighthouse\.Frontend\/src\/hooks\/useRbacGate\.ts\:14

UnchangedCompatible

```text
20 consumers read the same RbacState fields with the same meaning
```

app\-root

Lighthouse\.Frontend\/src\/App\.tsx\:241

Changed

```text
Mounts PageMoveRefresh inside the Router
```

grant\-flows

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts\:1

Changed

```text
Calls useRefreshPermissions after own grant or removal; RbacSettings.tsx and EveryoneSignedInSection.tsx likewise
```

create\-flows

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx\:53

Changed

```text
Calls useRefreshPermissions after create; EditPortfolio.tsx:78 likewise
```

test

Lighthouse\.Frontend\/src\/hooks\/useRbac\.test\.ts\:1

Changed

```text
Existing hook tests now need a QueryClient wrapper (renderWithQueryClient)
```

test\-support

Lighthouse\.Frontend\/src\/tests\/renderWithQueryClient\.tsx\:1

UnchangedCompatible

```text
Reused as the wrapper
```

hook

Lighthouse\.Frontend\/src\/hooks\/usePageFreshQuery\.ts\:1

Changed

```text
New owner of page-move freshness
```

component

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx\:1

Changed

```text
New page counter and refresh
```

page

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Changed

```text
Calls useRefreshPermissions after own grant
```

component

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/EveryoneSignedInSection\.tsx\:1

Changed

```text
Calls useRefreshPermissions after change
```

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:78

Changed

```text
Calls useRefreshPermissions after create
```

##### Agreement

None.

##### Not applicable

agreement

```text
No wire or persisted contract changes; RbacState is an in-deployable TypeScript interface whose shape stays the same for its consumers in the same commit
```

contract_tests

```text
Producer and consumers ship in one deployable in one commit; no wire change
```

prefactoring

```text
useRbac.ts holds one hook plus its RbacState interface (one concept); App.tsx, EditTeam.tsx, EditPortfolio.tsx, RbacSettings.tsx, EveryoneSignedInSection.tsx each hold one component; useScopedAccess.ts holds one hook; new files hold one unit each. Nothing moves.
```

##### Boundaries

```text
useRbac() hook rendered by the gating components
```

###### Driven ports

```text
rbacService.getAuthorizationSummary (GET /api/latest/authorization/my-summary)
```

```text
Components -> useRbac -> usePageFreshQuery -> TanStack QueryClient -> rbacService; PageMoveRefresh -> QueryClient
```

###### Failures

####### B2

```text
my-summary cannot be loaded
```

Retry

```text
loadFailed true and permissive fallback for every component; one retry asks once
```

##### Observations

```text
RbacState of each mounted component and the call count of getAuthorizationSummary
```

RbacState and number

```text
Network timing of the answer
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
Projection of model invariants on the hook: one call per page plus one per retry and own change; a component mounted after a move is never ready with the answer asked before it
```

Invariant

Application

usePageFreshQuery

###### Treatment

Finite

###### Cases

```text
three components one page -> one call
```

```text
move -> one call, new components loading until answer
```

```text
late answer of previous page after move -> not shown
```

```text
own grant -> one call, new summary shown
```

```text
failure -> all loadFailed, retry -> one call
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

docs\/feature\/story\-6712\-faster\-page\-loads\/design\/shared\_page\_reads\.qnt

##### Refuted

None.

##### Claims

###### C2

```text
A page component never treats a permissions answer asked on an earlier page as current
```

```text
A lost right shows an action as available
```

usePageFreshQuery

```text
Any sequence of mounts, page moves, answers, failures, retries and own changes
```

usePageFreshQuery queryFn wrapping the answer in PageFreshAnswer with askedOnPage

useRbac

```text
isLoading true until the post-move answer
```

```text
A component reading the query cache directly with useQueryClient; backend still refuses the action (403)
```

##### States

None.

##### Models

None.

##### Closure

```text
shared permissions answer
```

Safety

Pass

DesignedNotBuilt

```text
Quint model invariants pageComponentsSeeOnlyThisPage, oneRequestPerPage, noEarlierPageRequestInFlight, ownChangeIsAsked: quint run 20000 samples 15 steps no violation; quint verify Apalache 8 steps NoError; each invariant killed by its mutant
```

###### Evidence

docs\/feature\/story\-6712\-faster\-page\-loads\/design\/shared\_page\_reads\.qnt

```text
shared permissions answer
```

Inhabitation

Pass

DesignedNotBuilt

```text
useQuery with the existing rbacService answer
```

###### Evidence

None.

```text
shared permissions answer
```

PublicReachability

Pass

DesignedNotBuilt

```text
All 20 consumers reach it through useRbac(); PageMoveRefresh mounted in App.tsx
```

###### Evidence

None.

```text
shared permissions answer
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: arrival of the answer depends on the HTTP answer, outside the design; safety is the claim
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T3

```text
Only useRbac.ts calls getAuthorizationSummary and only useUnitLists.ts calls getTeams/getPortfolios in non-test frontend source, including files added later
```

Lighthouse\.Frontend\/src\/hooks\/sharedReadOwners\.test\.ts

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/hooks/sharedReadOwners.test.ts
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.ts

Create

None.

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts

Extend

None.

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx

Extend

```text
Adds the Teams, Portfolios and licence keys to the page-move refresh
```

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Settings\/ApiKeys\/ApiKeysSettings\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/SurveyNudge\/SurveyNudge\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/hooks/useUnitLists.test.tsx src/hooks/useLicenseRestrictions.test.ts
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render several components using useTeamsQuery, usePortfoliosQuery, useLicenseRestrictions and SurveyNudge under one QueryClient with PageMoveRefresh; call useUnitListsChanged after a mocked create, rename and delete; change the route after a mocked change by someone else
```

```text
getTeams, getPortfolios and getLicenseStatus each called once per page however many components read them; after useUnitListsChanged the lists show the change without a route change; the change by someone else shows after the route change
```

```text
Today: useLicenseRestrictions with staleTime 0 plus SurveyNudge and OverviewDashboard each call the services, so counts exceed one
```

##### Promises

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

useLicenseStatusQuery

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:41

Extend

```text
Already the one licence read for gating; joins page-move refresh; SurveyNudge moves onto it
```

useLicenseRestrictions

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:84

Extend

```text
Its teams/portfolios queries with staleTime 0 move to useUnitLists, same keys
```

useUnitLists

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.ts\:1

CreateNew

```text
Teams and Portfolios lists are read by five owners today with no shared hook
```

useQuery

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:46

Reuse

```text
The shared reads move from plain useQuery to usePageFreshQuery, created in S2, the one owner of page-move freshness
```

PageMoveRefresh

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx\:1

CreateNew

```text
Created by S2 as the single page-move refresh owner; S3 adds the Teams, Portfolios and licence keys to its refresh set, and creates it if S2 has not landed
```

getPortfolios

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:151

Extend

```text
Reads lists from useUnitLists; delete calls useUnitListsChanged
```

getTeams

Lighthouse\.Frontend\/src\/pages\/Settings\/ApiKeys\/ApiKeysSettings\.tsx\:162

Extend

```text
Both read sites use useTeamsQuery and usePortfoliosQuery
```

getTeams

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:64

Extend

```text
Teams from useTeamsQuery; create and update call useUnitListsChanged
```

createTeam

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx\:53

Extend

```text
Create and update call useUnitListsChanged
```

saveTeamSettings

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:193

Extend

```text
A rename through settings calls useUnitListsChanged
```

updatePortfolioSettings

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:166

Extend

```text
A rename through settings calls useUnitListsChanged
```

getLicenseStatus

Lighthouse\.Frontend\/src\/components\/SurveyNudge\/SurveyNudge\.tsx\:49

Extend

```text
Reads the licence from useLicenseStatusQuery instead of a second fetch
```

##### Impact

hook

Lighthouse\.Frontend\/src\/hooks\/useLicenseRestrictions\.ts\:84

Changed

```text
Reads shared lists; LicenseRestrictions shape unchanged
```

consumers

Lighthouse\.Frontend\/src\/pages\/Overview\/OverviewDashboard\.tsx\:151

Changed

```text
Lists from the shared hooks; delete calls useUnitListsChanged
```

consumers

Lighthouse\.Frontend\/src\/pages\/Settings\/ApiKeys\/ApiKeysSettings\.tsx\:162

Changed

```text
Both reads use the shared hooks
```

consumers

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Edit\/EditPortfolio\.tsx\:64

Changed

```text
Teams from the shared hook; create/update call useUnitListsChanged
```

consumers

Lighthouse\.Frontend\/src\/components\/SurveyNudge\/SurveyNudge\.tsx\:49

Changed

```text
Licence from useLicenseStatusQuery
```

rename\-flows

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:193

Changed

```text
Settings save calls useUnitListsChanged; PortfolioDetail.tsx:177 and EditTeam.tsx:53 likewise
```

test\-support

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts\:199

UnchangedCompatible

```text
Mocks unchanged
```

hook

Lighthouse\.Frontend\/src\/hooks\/useUnitLists\.ts\:1

Changed

```text
New owner of the Teams and Portfolios reads
```

component

Lighthouse\.Frontend\/src\/components\/App\/PageMoveRefresh\/PageMoveRefresh\.tsx\:1

Changed

```text
Refresh set gains three keys
```

page

Lighthouse\.Frontend\/src\/pages\/Teams\/Edit\/EditTeam\.tsx\:53

Changed

```text
Calls useUnitListsChanged after create and update
```

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:166

Changed

```text
Calls useUnitListsChanged after a settings save
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
The direct-caller law for getTeams/getPortfolios is covered by the S2 architectural test sharedReadOwners.test.ts
```

agreement

```text
No wire change; hooks are in-deployable
```

contract_tests

```text
One deployable, one commit, no wire change
```

prefactoring

```text
useLicenseRestrictions.ts holds functions and hooks of one concept (licence gating), not several types; OverviewDashboard, ApiKeysSettings, EditPortfolio, EditTeam, TeamDetail, PortfolioDetail, SurveyNudge each hold one component; ApiKeysSettings.tsx holds two components (lines 152 and 386) but the change only swaps two reads and leaves its layout move to a later item. Nothing moves.
```

##### Boundaries

```text
useTeamsQuery, usePortfoliosQuery and useLicenseStatusQuery rendered by shell and page components
```

###### Driven ports

```text
teamService.getTeams, portfolioService.getPortfolios, licensingService.getLicenseStatus
```

```text
Components -> useUnitLists / useLicenseRestrictions -> usePageFreshQuery -> QueryClient -> services
```

###### Failures

####### B5

```text
A list or the licence cannot be loaded
```

Retry

```text
Today’s looks per consumer (useLicenseRestrictions failed status, Overview error); shared error for all readers
```

##### Observations

```text
Lists and licence seen by each component, and call counts per service
```

arrays\, ILicenseStatus and numbers

```text
Network timing
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L7

```text
Same projection as S2 over the Teams, Portfolios and licence reads: one call per page plus one per own change; own change visible without a page move
```

Invariant

Application

useUnitLists

###### Treatment

Finite

###### Cases

```text
two readers one page -> one call each
```

```text
own create -> list shows it
```

```text
other user change -> shows after move
```

```text
403 on lists -> empty list for counts, as today
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

docs\/feature\/story\-6712\-faster\-page\-loads\/design\/shared\_page\_reads\.qnt

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
shared unit lists and licence
```

Safety

Pass

DesignedNotBuilt

```text
Same Quint model as S2 (protocol is identical per shared read)
```

###### Evidence

docs\/feature\/story\-6712\-faster\-page\-loads\/design\/shared\_page\_reads\.qnt

```text
shared unit lists and licence
```

Inhabitation

Pass

DesignedNotBuilt

```text
Existing service answers
```

###### Evidence

None.

```text
shared unit lists and licence
```

PublicReachability

Pass

DesignedNotBuilt

```text
Readers listed in impact reach the hooks
```

###### Evidence

None.

```text
shared unit lists and licence
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: answer arrival depends on HTTP
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.teamSettings\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Portfolios/Detail/PortfolioDetail.teamSettings.test.tsx
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render PortfolioDetail on its Features tab for a portfolio with two involved Teams and three features
```

```text
The same feature rows and columns as today, and teamService.getTeamSettings not called
```

```text
Today’s fetchPortfolio calls getTeamSettings once per involved Team
```

##### Promises

P9

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

PortfolioFeatureList

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.tsx\:62

Reuse

```text
Already renders from portfolio.involvedTeams
```

fetchPortfolio

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:124

Extend

```text
Drop the unread per-Team settings fetch and the involvedTeams state
```

##### Impact

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:91

Changed

```text
State and fetch removed; rendered rows unchanged
```

tests

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.test\.tsx\:1

UnchangedCompatible

```text
Tests that mock getTeamSettings keep passing; mocks become unused
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No dependency or ownership law changes
```

agreement

```text
No shared contract touched; GET teams/{id}/settings stays for its other readers
```

contract_tests

```text
No wire change
```

prefactoring

```text
PortfolioDetail.tsx holds one component; nothing moves
```

##### Boundaries

```text
PortfolioDetail page render (Features tab)
```

###### Driven ports

```text
portfolioService.getPortfolio, getPortfolioSettings
```

```text
PortfolioDetail -> services; teamService.getTeamSettings only on a save
```

###### Failures

None.

##### Observations

```text
Rendered feature rows and getTeamSettings call count
```

DOM rows and number

```text
Answer size
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
Rendering a Portfolio tab never calls getTeamSettings; a save from the Portfolio still reads it once
```

Invariant

Application

PortfolioDetail

###### Treatment

Finite

###### Cases

```text
Features tab render -> 0 calls
```

```text
Team settings save from Portfolio -> 1 call
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

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

```text
Features tab without Team settings
```

Safety

Pass

DesignedNotBuilt

```text
Removed state has no reader (grep of involvedTeams in PortfolioDetail.tsx)
```

###### Evidence

None.

```text
Features tab without Team settings
```

Inhabitation

Pass

DesignedNotBuilt

```text
portfolio.involvedTeams already in the portfolio answer
```

###### Evidence

None.

```text
Features tab without Team settings
```

PublicReachability

Pass

DesignedNotBuilt

```text
Route /portfolios/:id/features
```

###### Evidence

None.

```text
Features tab without Team settings
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: removal of a call, no waiting state
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/ILighthouseReleaseService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs

Extend

None.

Lighthouse\.Frontend\/src\/services\/Api\/VersionService\.ts

Extend

None.

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LighthouseVersion\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LatestReleaseInformationDialog\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LighthouseVersion\.updateCheck\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/App/LetPeopleWork/LighthouseVersion.updateCheck.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~LighthouseReleaseServiceNewestVersionTest|FullyQualifiedName~EndpointAuthorizationPolicyTest
```

#### Supports

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceNewestVersionTest\.cs

None.

None.

#### Public oracle

```text
Render LighthouseVersion with a version service whose calls are held: current, distribution and hasupdate pending; then resolve with an update; open the dialog; make getNewReleases pending, then resolve; separately make it reject and press Retry; separately reject one footer check
```

```text
current, distribution and hasupdate all started before any resolves; getNewReleases not called on load; the pop-up shows the newest version from getNewestVersion; the open dialog shows the version and a progress indicator, then the notes; on rejection the version, Could not load the release notes. and a Retry button that calls getNewReleases again; any rejected footer check shows the footer error
```

```text
Today’s footer awaits each check before starting the next and calls getNewReleases on load
```

##### Promises

P10

P11

P12

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

GetNewReleases

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs\:60

Extend

```text
GetNewestReleaseVersion takes its first release so the number equals today’s pop-up
```

VersionController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:14

Extend

```text
Owner of the version routes
```

VersionService

Lighthouse\.Frontend\/src\/services\/Api\/VersionService\.ts\:18

Extend

```text
Adds getNewestVersion
```

LatestReleaseInformationDialog

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LatestReleaseInformationDialog\.tsx\:36

Extend

```text
Loads notes on open; reuses CircularProgress and Alert already imported
```

LighthouseVersion

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LighthouseVersion\.tsx\:26

Extend

```text
Parallel checks, newest version instead of releases
```

ILighthouseReleaseService

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/ILighthouseReleaseService\.cs\:6

Extend

```text
Port of the release application service gains GetNewestReleaseVersion
```

##### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/ILighthouseReleaseService\.cs\:6

Changed

```text
Adds GetNewestReleaseVersion; implemented by LighthouseReleaseService
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:52

Changed

```text
Adds GET newest with SignedInOnly
```

service

Lighthouse\.Frontend\/src\/services\/Api\/VersionService\.ts\:9

Changed

```text
IVersionService gains getNewestVersion
```

test\-support

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts\:1

Changed

```text
Version service mock gains getNewestVersion
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EndpointAuthorizationPolicyTest\.cs\:1

UnchangedCompatible

```text
Covers the new action through its attribute scan
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/VersionControllerTest\.cs\:1

UnchangedCompatible

```text
Existing rows unchanged
```

service

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs\:60

Changed

```text
Adds GetNewestReleaseVersion
```

component

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LighthouseVersion\.tsx\:26

Changed

```text
Parallel checks, newest version only on load
```

component

Lighthouse\.Frontend\/src\/components\/App\/LetPeopleWork\/LatestReleaseInformationDialog\.tsx\:36

Changed

```text
Notes loaded on open with loading and failed states
```

##### Agreement

Version routes under api\/v1 and api\/latest

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:52

Producer

Migrated

```text
Adds GET newest; current, new, hasupdate unchanged
```

Version routes under api\/v1 and api\/latest

Lighthouse\.Frontend\/src\/services\/Api\/VersionService\.ts\:9

Consumer

Migrated

```text
Calls newest on load and new on dialog open
```

Version routes under api\/v1 and api\/latest

Lighthouse\-Clients CLI and MCP \(\/api\/v1\/version\/current\)

Consumer

UnchangedCompatible

```text
They call only version/current, unchanged
```

Version routes under api\/v1 and api\/latest

Jira app and served\-version probe \(version\/current\)

Consumer

UnchangedCompatible

```text
Anonymous current unchanged
```

##### Not applicable

contract_tests

```text
The new route has one consumer, the frontend in the same deployable and commit; external clients use only version/current, unchanged
```

prefactoring

```text
VersionController.cs, LighthouseReleaseService.cs, ILighthouseReleaseService.cs, VersionService.ts (one class plus its interface IVersionService, one concept), LighthouseVersion.tsx and LatestReleaseInformationDialog.tsx each hold one type; nothing moves
```

##### Boundaries

```text
LighthouseVersion footer render and its new-version dialog; backend GET /api/latest/version/newest
```

###### Driven ports

```text
VersionService (frontend) -> VersionController -> ILighthouseReleaseService -> IGitHubService
```

```text
Controller -> application service -> GitHub port; UI -> VersionService
```

###### Failures

####### B3

```text
Release notes cannot be loaded
```

Retry

```text
Dialog shows newest version, Could not load the release notes. and Retry
```

####### B4

```text
Any footer version check fails
```

Refusal

```text
Footer error as today
```

##### Observations

```text
Call order and count on the version service; dialog content in its three states
```

calls and DOM

```text
Bytes on the line (measured by measure-bytes.mjs)
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L5

```text
Footer error iff any started check rejects; independent checks start together
```

Invariant

Application

LighthouseVersion

###### Treatment

Finite

###### Cases

```text
all resolve, no update
```

```text
all resolve, update, internal
```

```text
current rejects
```

```text
hasupdate rejects
```

```text
newest rejects
```

```text
updateSupported rejects
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

None.

###### L6

```text
GetNewestReleaseVersion equals the version of the first of GetNewReleases, none when it is empty
```

Invariant

Application

LighthouseReleaseService

###### Treatment

Finite

###### Cases

```text
two newer releases -> first
```

```text
current is newest -> none
```

```text
DEV -> newest of all
```

```text
The domain is a closed finite set of cases, so native example rows cover every member
```

###### Evidence

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

```text
update check without notes
```

Safety

Pass

DesignedNotBuilt

```text
Finite rows over the any-failure rule
```

###### Evidence

None.

```text
update check without notes
```

Inhabitation

Pass

DesignedNotBuilt

```text
Newest from existing GetNewReleases
```

###### Evidence

None.

```text
update check without notes
```

PublicReachability

Pass

DesignedNotBuilt

```text
Footer in the app shell; route under api/v1 and api/latest
```

###### Evidence

None.

```text
update check without notes
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: each check is one HTTP answer outside the design
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T4

```text
Every API action carries an authorization attribute, including GET version/newest
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EndpointAuthorizationPolicyTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~EndpointAuthorizationPolicyTest
```

##### Contract tests

None.

## ADRs

- 004 Compress safe\-method answers and static files at serve time — Proposed
