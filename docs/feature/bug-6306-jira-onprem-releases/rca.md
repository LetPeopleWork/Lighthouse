# RCA: Jira on-prem Release picker comes back empty (ADO Bug #6306)

Investigator: Rex (nw-troubleshooter) · 2026-10-15 · read-only, no production code changed.

## Problem statement

On Jira **Data Center / Server**, two independent users get an **empty Release picker** when binding a
Delivery to a Jira Release (delivery source `jira-release`). The maintainer's Jira **Cloud** site works.
Scope: `IDeliverySourceProvider` + `IDeliveryForecastPublisher` on `JiraWorkTrackingConnector` (Epic 5565),
i.e. `GetOptions`, `ResolveMany` (refresh), `PublishAsync`. Work-item sync, search, fields, statuses are
out of scope except where they share the same cause (they do not, see Branch C).

Connector: `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` (below: `Connector.cs`).

## External evidence (Atlassian Jira Server/DC REST docs)

Pages downloaded and parsed for 7.13.0, 8.0.0, 8.13.0, 8.20.10, 9.4.0, 9.12.0 (LTS), 9.17.0 at
`https://docs.atlassian.com/software/jira/docs/api/REST/<version>/`.

| Question | Answer on DC/Server | Evidence |
|---|---|---|
| `rest/api/3/*` | Does not exist. v3 is Cloud-only, so every `rest/api/3` call answers 404. | No `api/3` group in any DC doc version above. |
| Q1 `GET /rest/api/2/project/search` | **Absent in every DC version checked (7.13 to 9.17).** DC parses `search` as a project key and returns 404 "No project could be found with key 'search'". | DC doc anchor list (no `project/search`); [Atlassian Community: get projects, Jira Server API](https://tnckb94959.lithium.com/forums/Jira-questions/Re-Re-get-projects-jira-server-API/qaq-p/2652576/comment-id/935019) |
| Q1 fallback `GET /rest/api/2/project` | Present in all versions. **Not paged**: returns a **bare JSON array** of projects (`key`, `name`, …). Params: `expand`, `recent`, `includeArchived` (and `browseArchive` from 9.x). | [9.12.0 #api/2/project-getAllProjects](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/project-getAllProjects), [7.13.0 same anchor](https://docs.atlassian.com/software/jira/docs/api/REST/7.13.0/#api/2/project-getAllProjects) |
| Q2 `GET /rest/api/2/project/{key}/version` (paged) | Present from 7.13 through 9.17 at least. Params: **`startAt`, `maxResults`, `orderBy`, `expand` only. No `status` parameter.** Response: `{self,nextPage,maxResults,startAt,total,isLast,values:[…]}`, the same envelope as Cloud, **`isLast` included**. Version objects carry `id` (string), `name`, `archived`, `released`, `releaseDate` (absent when unset), `projectId`. | [9.12.0 #api/2/project-getProjectVersionsPaginated](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/project-getProjectVersionsPaginated), [7.13.0 same anchor](https://docs.atlassian.com/software/jira/docs/api/REST/7.13.0/#api/2/project-getProjectVersionsPaginated) |
| Q2 non-paged fallback `/project/{key}/versions` | Present; bare array. Not needed: the paged form exists on every DC version checked. | [9.12.0 #api/2/project-getProjectVersions](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/project-getProjectVersions) |
| Q3 `GET`/`PUT /rest/api/2/version/{id}` | Both present. PUT semantics: fields present override; absent fields are ignored. `description` is a plain string, as on Cloud v3 (version descriptions are not ADF). Same behaviour as Cloud v3 for this use. | [9.12.0 #api/2/version-updateVersion](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/version-updateVersion), `#api/2/version-getVersion` |
| `GET /rest/api/2/version` (global, paged) | 9.x only, marked **experimental**, returns `projectId` not key. Not recommended. | [9.12.0 #api/2/version-getPaginatedVersions](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/version-getPaginatedVersions) |

Jira DC 10.x reference (`developer.atlassian.com/server/jira/platform/rest/v10000/`) is a JS app that
could not be parsed. Treat DC 10 as matching 9.17 (hypothesis: DC never shipped v3). Verify against a
DC 10 instance during DELIVER.

**Confirmed on a live instance (2026-10-15).** An affected user's on-prem Jira, `serverInfo` version
10.3.x, `deploymentType: "Server"`, answers `GET rest/api/3/project/search` with Jira's "dead link"
(404) page. That confirms DC 10 serves no `rest/api/3`, rules out the permission hypothesis (H2), and
shows that `deploymentType` arrives as `"Server"`, not `"DataCenter"`. The connector maps any value
other than `Cloud` to DataCenter. The instance is not named here, by request.

**Oldest supported DC version:** none is documented. `docs/concepts/worktrackingsystems/jira.md:91-92`
says only "on-premise Jira version (*Server* or *Data Center*)". The endpoints proposed below exist
from 7.13 onward, which covers every DC/Server release that is still realistic.

## 5 Whys

PROBLEM: On DC/Server, `GetOptions` for `jira-release` returns `[]`. Refresh (`ResolveMany`) and
publish (`PublishAsync`) fail the same way.

### Branch A: the project/version reads use Cloud-only routes (picker empty, refresh blind)

- **WHY 1A.** The picker is empty because `SweepReleases` gets no projects back.
  [Evidence: `Connector.cs:2928-2955` `ReadVisibleProjects` returns `(projects, false)` with an empty
  list on any non-success answer. `SweepReleases` (`:2901`) then has no projects to read versions for.]
- **WHY 2A.** The project list is always a non-success answer on DC, because the request goes to
  `rest/api/3/project/search`. [Evidence: `Connector.cs:2935`. DC has no `rest/api/3`. DC also has no
  `project/search`, even under v2 (table above).] Completeness check: even if a project were listed,
  the version read at `:2969` is `rest/api/3/project/{key}/version?…&status=` and would also 404.
  Publish at `:3251` is `rest/api/3/version/{id}`, which also 404s, so `PublishVerdictFor` reports a
  "Release not there".
- **WHY 3A.** The release code ignores the Cloud/DC split that the rest of the connector already makes.
  [Evidence: deployment detection exists (`GetDeploymentType`, `:2761-2803`, through
  `rest/api/2/serverInfo`), and the search already branches on it: `WalkCloudSearchPages` uses
  `rest/api/3/search/jql` (`:2437`), while DC uses `DataCenterSearchPath = "rest/api/latest/search"`
  (`:66`, `:2419`, `:3168`). Every other literal in the connector is `rest/api/latest` or `rest/api/2`
  (`:42`, `:44`, `:656`, `:757`, `:895`, `:1402`, `:1554`). The only three `rest/api/3` literals outside
  the gated search are the Epic 5565 ones (`grep -n 'rest/api' Jira/*.cs`).]
- **WHY 4A.** The feature was designed and verified against Cloud only. [Evidence:
  `docs/product/architecture/brief.md:6399`: "**External integration introduced: Jira Cloud REST,
  read-only** (`GET rest/api/3/project/{key}/versions`)". Slice 00 Q2 confirmed the shape "live" on the
  Cloud site (`brief.md:6400`). ADR-171 (`adr-171-…:47`) also names `rest/api/3`. Introducing commits:
  `8cf2e0b31` (2026-08-23, picker) and `33ce637f3` (2026-08-25, publish). Neither DESIGN nor DISTILL
  asked "what does DC answer?"]
- **WHY 5A. ROOT CAUSE A.** **The connector has no single place that owns "which route does this
  deployment serve".** Each call site writes its own `rest/api/N/...` literal, so a Cloud-only route can
  be added without anything (compiler, test or review checklist) asking for the DC equivalent. The
  same gap had to be fixed once before, for search (`cd091cb25` 2025-08-29, "Handle Jira Cloud endpoint
  differently"). That fix was made locally and not structurally, so it did not protect the next feature.

### Branch B: the DC answers differ in shape and parameters, not only in version number

This branch is latent. It only fires once Branch A's URLs are fixed, but a naive fix that just changes
`3` to `2` hits it.

- **WHY 1B.** Changing the URLs to `rest/api/2/project/search` is still a 404 on DC. [Evidence: Q1.]
- **WHY 2B.** The DC project list is `GET rest/api/2/project`, which returns a **bare array**.
  `JiraReleaseVersionReader.ReadPage` (`JiraReleaseVersionReader.cs:127-141`) calls
  `root.TryGetProperty("isLast")` on the root (`:132`). `JsonElement.TryGetProperty` throws
  `InvalidOperationException` when the element is not an Object. `JiraCouldNotBeAsked`
  (`Connector.cs:3096-3097`: `HttpRequestException or TaskCanceledException or JsonException or
  UriFormatException`) does not catch it, so the picker would turn into a **500** instead of an empty list.
- **WHY 3B.** DC's paged `/project/{key}/version` has **no `status` parameter**. JAX-RS ignores unknown
  query params (hypothesis, standard Jersey behaviour, to verify on a live DC). So
  `status=unreleased` (`OfferableVersionStatuses`, `:2824`) would hand the picker archived and released
  Releases too. The test stub honours `status` (`JiraDeliverySourceProviderTest.cs:1055-1080`
  `RespondWithVersionsOf`), so it models Cloud only.
- **WHY 4B.** The reader was specified from Cloud payloads only. [Evidence: `JiraReleaseVersionReader`
  doc comment "the paginated endpoint wraps the same version objects". The fixtures use
  `https://example.atlassian.net/rest/api/3/version/...` (`JiraDeliverySourceProviderTest.cs:89-106`).]
- **WHY 5B. ROOT CAUSE B.** **"Which deployment" was treated as a URL prefix rather than as a different
  contract.** The two deployments differ in endpoint availability (`project/search`), paging (array or
  envelope) and filter support (`status`). A guard that only bans the `3` in `rest/api/3` would not
  catch any of these.

### Branch C: why it was silent and untested

- **WHY 1C.** Nobody saw an error. The users saw an empty list and the maintainer could not reproduce.
- **WHY 2C.** Non-success answers are logged at **Information** and swallowed (`Connector.cs:2939-2943`
  "Jira did not let us list the projects…", `:2974-2978`). `GetOptions` (`:2867-2888`) throws away the
  `sawEveryRelease=false` it receives (`:2877`, only used to skip caching), so the UI cannot tell
  "no Releases" from "could not read Releases". This was a deliberate trade-off for 403s on single
  projects (doc comment `:2921-2926`), but it also hides a **404 on the very first call**, which means
  "this route does not exist" and not "you may not".
- **WHY 3C.** No test runs a release capability against a DC-shaped Jira. [Evidence: all 12 connections
  in `JiraDeliverySourceProviderTest.cs` are `AJiraCloudConnection()` (`:813`). `JiraStub` answers
  `serverInfo` with `{"deploymentType":"Cloud"}` (`:1027-1030`). It matches routes by
  `EndsWith("/project/search")` / `EndsWith("/version")`, ignoring the API version (`:1032-1041`), and
  answers `Ok("{}")` to anything unknown. `JiraDeliveryForecastPublisherTest.cs:106` asserts the
  `rest/api/3/version/…` URL, which pins the bug. `grep DataCenter` returns 0 hits in
  `JiraDeliverySourceProviderTest.cs`, `JiraDeliveryForecastPublisherTest.cs` and
  `DeliverySourcesAcceptanceTest.cs`. `JiraConnectorTestSetup` (`TestHelpers/JiraConnectorTestSetup.cs:119`)
  offers `ATeamOnJiraCloud` and no DC counterpart.]
- **WHY 4C.** A DC fake exists, but only for the search capability
  (`API/Integration/FasterUpdates/Slice04JiraDataCenterDelta*.cs`). It is per-feature, not a shared
  contract that every capability must pass.
- **WHY 5C. ROOT CAUSE C.** **There is no deployment matrix in the connector's test design.** A new
  capability is specified against whichever deployment the author can reach (Cloud). DC was known to be
  unverifiable at release time for earlier features too: `docs/evolution/2026-08-09-epic-5500-quiet-writeback.md:142`
  says "**Data Center is unverified.** No instance was obtainable". That risk was accepted
  feature-by-feature and never closed structurally.

### Cross-validation

- A + B + C are consistent. A explains the symptom (404 leading to empty picker). B explains why a
  shallow fix fails (500 or wrong picker contents). C explains why it shipped and stayed invisible.
- Backward chain: DC user opens the picker → `GetOptions` → `rest/api/3/project/search` 404 → logged at
  Information → `([], false)` → `[]` returned, not cached → empty picker on every retry. This matches
  both reports. On Cloud, v3 exists, which matches the maintainer's working site.
- Search for carried work (`TryWalkReleaseMembership`, `:3157-3187`) **is DC-safe**: it branches on
  `GetDeploymentType`, and DC/Unknown use `rest/api/latest/search`. `CollectCarriedWork` (`:3189`)
  reads `fields.fixVersions[].id` as a string, which DC also sends as a string (`"id":"10000"`). So
  refresh fails only at the sweep (Branch A). The membership search never runs because no Release
  resolves.
- Other connectors: no same-class problem. ADO goes through the `Microsoft.TeamFoundation` SDK clients,
  which negotiate the API version with the server. ServiceNow uses only `/api/now/table`. Linear is
  SaaS-only.

### Alternative hypotheses

| Hypothesis | Fits symptom? | Verdict |
|---|---|---|
| H1: `rest/api/3` / `project/search` answer 404 on DC (Branch A) | Yes | **Primary.** Documented absence across seven DC versions. Fits two independent reports on two instances. Fits Cloud working. |
| H2: credential lacks *Browse projects*, so the project list returns 403 | Yes, the same silent empty list | Unlikely as the sole cause. Two independent DC users with the same gap and no Cloud user with it would be a coincidence, and the same credentials sync work items fine. **Not ruled out by data:** no reporter's status code was captured. Close it with the gate in the Risk section. |
| H3: the DC instance sits under a context path (`/jira`) and the base url is wrong | No | Work-item sync over the same client and base url works for these users. |
| H4: unrecognised `deploymentType` (`Server` vs `DataCenter`) misroutes the call | No | Any non-`Cloud` value maps to DataCenter (`:2789-2797`), and the release code ignores the deployment anyway. |

## Proposed fix (permanent, P1)

1. **Pass the deployment into the release code.** `SweepReleases` / `ReadVisibleProjects` /
   `ReadReleasesOf` take `JiraDeployment` (resolved once through `GetDeploymentType(client, connection)`
   inside `SweepReleasesWithoutThrowing`). `PublishAsync` resolves it the same way.
2. **Routes by deployment** (behind the recurrence guard below):
   | Capability | Cloud (unchanged) | DataCenter and Unknown |
   |---|---|---|
   | list projects | `rest/api/3/project/search?startAt&maxResults` (paged envelope) | `rest/api/2/project` (bare array, one call) |
   | versions of a project | `rest/api/3/project/{key}/version?startAt&maxResults&status=` | `rest/api/2/project/{key}/version?startAt&maxResults` (no `status`) |
   | read/write a version | `rest/api/3/version/{id}` | `rest/api/2/version/{id}` |

   Unknown takes the DC column, because the DC routes are also v2 routes on Cloud. One exception to
   verify: Cloud documents `GET /rest/api/2/project` ("Get all projects") as deprecated in favour of
   `project/search`, so check that it is still served on Cloud before relying on it. Otherwise, for
   Unknown, try `project/search` first and fall back to `project` on 404. Do not cache `Unknown` (see
   Risk).
3. **Reader:** add `JiraReleaseVersionReader.ReadProjectList(string)` for the bare array. Harden
   `ReadPage` so a non-Object root answers `([], true)` instead of throwing. That turns a future
   shape surprise into a visible empty answer, not a 500.
4. **Status filter client-side on DC:** when the route cannot filter, keep a version only if
   `!released && !archived` for the picker. Refresh asks for every status, so it needs no filter. The
   `DeliverySourceOption` already carries `IsRetiredAtSource` and the released flag
   (`JiraReleaseVersionReader.ToOption`).
5. **Stop swallowing a missing route:** on 404 from the *project list*, log at **Warning**, naming the
   route and the deployment. This is the "route does not exist" case, not a permission. Optional
   follow-up (UI, needs a sketch per CLAUDE.md): surface `sawEveryRelease=false` from `GetOptions` as a
   "some Releases could not be read" hint.
6. **Tests:** fix `JiraDeliveryForecastPublisherTest.cs:106` so it expects the deployment-specific URL.
   Run the provider and publisher specs for both deployments (see guard option c).

Immediate mitigation for affected users: none without a release. No configuration selects a different
route.

## Recurrence guard ("so we don't forget")

| Option | Catches | Misses | Cost |
|---|---|---|---|
| (a) `JiraRestRoutes` chosen by `JiraDeployment`: an abstract class with `Cloud` and `DataCenter` subclasses, one **abstract method per capability** (`ProjectList(startAt)`, `ProjectVersions(key, startAt, statuses)`, `Version(id)`, `Search…`, `Field`, `Status`, `ServerInfo`, …) plus a `CanFilterVersionsByStatus` / `ProjectListIsPaged` capability flag | A new capability **will not compile** until both deployments have an answer. Shape differences live next to the route. | Nothing stops a call site from bypassing it with a literal. | Medium: one refactor commit (moves about 15 literals) |
| (b) Convention test: `rest/api/` string literals may appear only in `JiraRestRoutes.cs` | Any bypass of (a). Cheap, fast, no network. | On its own, a plain "ban `rest/api/3`" rule misses Branch B: `rest/api/2/project/search` is Cloud-only too. | Low. ArchUnitNET works on types, not literals, so implement as an NUnit source scan under `Lighthouse.Backend.Tests/Architecture/` beside `LighthouseArchitecture.cs`. |
| (c) DC contract fixture: a `FakeJiraDataCenter` handler that answers `serverInfo` with `deploymentType:"Server"`, returns **404 for every `rest/api/3/*` and for `rest/api/2/project/search`**, ignores `status`, and serves bare arrays where DC does. Every connector capability runs against both fakes (`[TestCase(Cloud)] [TestCase(DataCenter)]`). | Behavioural regressions, including shape and parameter differences. | Only capabilities someone remembered to add to the matrix. A DC fake is only as faithful as its author, and is itself Cloud-biased if written from Cloud docs. | Medium-high |

**Recommendation: (a), enforced by (b), with (c) as the regression net for the release capabilities.**
(a) is the only option that makes forgetting impossible at the moment a capability is written: the
compiler demands the DC answer. (b) costs about 30 lines and closes the bypass. A pure "no
`rest/api/3`" test (b alone) is **not sufficient**, because the DC gap here is endpoint availability
and payload shape, not only the version number. Also add a line to `docs/ci-learnings.md` and to the
DESIGN checklist: "any new Jira endpoint names its DC route and DC response shape". Declare the oldest
supported DC version in `docs/concepts/worktrackingsystems/jira.md` (suggest 8.x; every route above
exists from 7.13).

## Files affected

- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` (`:2867-2988` sweep, `:3073-3086` sweep wrapper, `:3247-3290` publish, plus every route literal if (a) is applied)
- `.../Jira/JiraReleaseVersionReader.cs` (`ReadPage :127`, new `ReadProjectList`)
- new `.../Jira/JiraRestRoutes.cs` (option a)
- `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDeliverySourceProviderTest.cs` (`JiraStub` deployment-aware)
- `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDeliveryForecastPublisherTest.cs` (`:106`)
- `Lighthouse.Backend.Tests/TestHelpers/JiraConnectorTestSetup.cs` (add `ATeamOnJiraDataCenter`)
- new `Lighthouse.Backend.Tests/Architecture/JiraRouteLiteralsTest.cs` (option b)
- `docs/concepts/worktrackingsystems/jira.md`, `docs/ci-learnings.md`

## Risk assessment

- **Cloud regression: low.** Cloud keeps byte-identical routes (v3, `status=` server-side filter). The
  only Cloud-visible change is that the deployment lookup now runs before the sweep. It is cached
  process-wide in `DeploymentCache` and is already paid by search and refresh. The OAuth gateway is
  resolved as Cloud without a call (`TryResolveKnownDeployment`, `:2748`).
- **Unknown deployment** (serverInfo failed): moves from v3 routes to v2 routes, which both deployments
  serve. **But `GetDeploymentType` caches `Unknown` for the life of the process** (`:2779`, `:2799`).
  A Cloud tenant whose first `serverInfo` call fails transiently would stay pinned to the unpaged
  `rest/api/2/project` list and to client-side status filtering until restart. That is functionally
  correct, but it is a silent degradation. The same pin already sends such a tenant to the DC search
  path today, which is a pre-existing issue. Mitigation, part of the fix: do not cache `Unknown`, so
  the next call asks `serverInfo` again. A one-line change in `GetDeploymentType` that also benefits
  search.
- **Publish under Unknown on Cloud:** `GET`/`PUT rest/api/2/version/{id}` are documented v2 Cloud
  endpoints with the same plain-string `description`, so the read-merge-write is unchanged. Cover it
  with a Cloud-stub test that has `serverInfo` failing.
- **DC large instances:** `GET rest/api/2/project` is unpaged. On thousands of projects it is one big
  response, which is still cheaper than the per-project version reads that follow. Per-project
  parallelism stays at 8 (`MaxParallelProjectReads`, `:2836`).
- **DC `status` ignored (hypothesis):** if a DC version 400s on the unknown parameter instead, the
  fix already omits it on DC, so this is safe either way.
- **Refactor (a) touches about 15 route literals**, including search and fields. Do it in a separate
  `refactor(jira):` commit, after the fix is green. Run the `JiraIntegration` category deliberately,
  since that is the live Cloud check.
- **Still unverified:** no DC instance is available to the team. **Gate before DELIVER starts, not before
  release:** get one reporter of #6306 to send the status code and body of
  `GET <base>/rest/api/3/project/search` and of `GET <base>/rest/api/2/project`, plus their
  `rest/api/2/serverInfo` `version`. That confirms the 404 (see the alternatives table) and pins the DC
  version the fix has to work against.
