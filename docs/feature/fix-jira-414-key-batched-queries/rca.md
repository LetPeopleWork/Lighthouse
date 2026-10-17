# RCA — Bug 6315: Jira Cloud rejects key-batched queries with 414 RequestUriTooLong

Investigator: Rex (nw-troubleshooter), 2026-10-16. Diagnosis only, no production code changed.
Paths below are relative to `Lighthouse.Backend/Lighthouse.Backend/` unless they say otherwise.
`JWTC` means `Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs`.

## 1. Problem statement (scoped)

On **Jira Cloud only**, any search whose JQL lists about 200 issue keys of 14 or more characters
(`LONGPROJECTKEY-1251`) fails with `414 RequestUriTooLong`. The connector raises
`JiraQueryRejectedException`, and the Team refresh ends `success=False`. Four Teams fail on every
refresh, on v26.10.10.1 and on v26.10.16.6.

- In scope: Cloud search transport, the key-batched call sites, the refresh orchestration that
  makes the failure stick, and the test blind spot.
- Out of scope: Data Center/Server. It already sends its searches by POST (see below) and cannot
  hit a 414.
- Hypothesis checked: the reporter's hypothesis is **confirmed, with one correction**. The count
  of 200 and the `key = "X" OR …` shape are right. But the URL is not built in the call sites. It is
  built in one shared method, `WalkCloudSearchPages`, which every Cloud search goes through. The
  part about write-backs does **not** hold up: see §2, branch C.

### Evidence: URI length against key length (200 keys, the URL exactly as `WalkCloudSearchPages` builds it)

Computed with `Uri.EscapeDataString`-equivalent encoding. Path plus query only; the host comes on top.

| key length | example | raw JQL | request URI (`OR` form) | request URI (`key in (…)`) |
|---|---|---|---|---|
| 6 | `PROJ-1` | 3,596 | 6,465 | 3,637 |
| 12 | `MOBILE-1251` | 4,796 | 7,665 | 4,837 |
| **14** | `LONGPROJECTKEY-1251` | **5,196** | **8,065** + host ≈ 8.1 KB | 5,237 |
| 16 | | 5,596 | 8,465 | 5,637 |

These figures are **computed, not measured**. The reporter's "~5,000 chars of JQL" matches the
14-character row. The 14-character URI (8,065 bytes, plus "GET /" and " HTTP/1.1", plus a gateway
path prefix such as `ex/jira/{cloudId}/` on OAuth connections) sits right at an 8 KB line. The only
**measured** cliff in the codebase is ServiceNow's: 245 ids answered 200 at 8,182 bytes, and 250
answered 414 at 8,347 (`ServiceNow/ServiceNowHistoryQuery.cs:13-14`). **The Jira Cloud threshold is
unknown.** It is not documented, and the reporter's failing request length has not been measured.
The chain below does not depend on the exact number: it holds for any request line longer than
whatever limit answered.

**Where the 414 came from (Atlassian edge or a customer proxy) is unresolved.** `LogTheRefusal`
(JWTC:2201-2206) logs the whole answer as `Jira answered: {ResponseBody}` at Warning. Ask the
reporter for that line from one failing Team: an Atlassian JSON/HTML error page means the edge, and
a corporate-proxy page means a proxy. The fix is the same either way, because POST takes the query
out of the request line for both.

## 2. Toyota 5 Whys

```
PROBLEM: Jira Cloud Teams with long project keys never finish a refresh (414 on a key-batched search).

WHY 1A  The by-key download is answered 414.
        [JWTC:274-280 GetWorkItemsForTeam(team, referenceIds) chunks by ReferenceIdsPerQuery (=200, JWTC:66)
         and sends PrepareIssueKeyQuery(chunk) = string.Join(" OR ", ids.Select(id => $"key = \"{id}\""))
         (JWTC:2475-2476) through GetIssuesByQuery -> GetIssuesByQueryFromCloud (JWTC:2081-2084, 2168-2188)]
  WHY 2A  On Cloud, that JQL travels in the request line.
          [WalkCloudSearchPages JWTC:2373-2389 builds "rest/api/3/search/jql?jql=" + Uri.EscapeDataString(jql)
           + &fields + &expand=changelog + &maxResults (+ &nextPageToken), then client.GetAsync(url).
           Every space, '=' and '"' grows from 1 character to 3 when escaped, so 5,196 characters of JQL become 8,065.]
    WHY 3A  Chunking bounds a COUNT of keys. The limit is a LENGTH in bytes. Nothing on the path measures the URI.
            [ReferenceIdsPerQuery is a bare const 200 (JWTC:66), used at JWTC:274, 342, 367, 384.
             The comments at JWTC:270-272 and 340-341 themselves say the URL "has to survive whatever proxy sits
             in front of Jira", then bound it by count, "chunked the way the Azure DevOps connector chunks its
             id list". ADO ids are short integers sent in a POST body, so that analogy does not carry over to
             Jira keys of variable length.]
      WHY 4A  The same defect was fixed for Data Center on 2026-09-17, but the fix was scoped to Data Center
              on an explicit belief that Cloud was not exposed.
              [baa2c0b40 "fix(jira): ask Data Center for issues over POST so the query leaves the request line".
               docs/feature/fix-jira-dc-portfolio-refusal/deliver/roadmap.json:55: "chunking bounds a COUNT,
               the constraint is a LENGTH … The Cloud path is untouched: it walks by page token against
               rest/api/3/search/jql and has no such exposure." Token paging has nothing to do with where the
               JQL rides. The JQL is still in the URL on Cloud.]
        WHY 5A  ROOT CAUSE A — the transport choice "GET query string for Cloud" was never revisited once the
                JQL became caller-sized (key lists). The routing abstraction still writes that choice down as
                a fact: JiraRestRoutes.cs:59 "Cloud's is read with a query string". Jira Cloud offers
                POST /rest/api/3/search/jql with the same response schema (checked 2026-10-16 in
                https://dac-static.atlassian.com/cloud/jira/platform/swagger-v3.v3.json, operation
                searchAndReconsileIssuesUsingJqlPost: GET and POST both return SearchAndReconcileResults;
                the SearchAndReconcileRequestBean body is jql:string, fields:string[], expand:string,
                maxResults:int, nextPageToken:string).
        -> SOLUTION A: send every Cloud search as POST with a JSON body (§4).

WHY 1B  The failure repeats on EVERY refresh instead of once.
  WHY 2B  The refused download propagates out of the refresh. Only a failed SWEEP falls back to the full download.
          [Services/Implementation/WorkItems/WorkItemService.cs:300-332 ScanRemoteIdentities catches and falls back to FetchEverything;
           FetchOnlyWhatMoved WorkItemService.cs:347-360 has no catch, so the JiraQueryRejectedException
           reaches UpdateServiceBase.BuildRefusalReason (UpdateServiceBase.cs:219-226), which produces
           "The work tracking system refused this refresh: …"]
    WHY 3B  Nothing is stored, so the stored stamps never advance and the next sweep reports the same moved set.
            [HasMoved WorkItemService.cs:373-378: stored?.LastChangedRemote != swept.ChangedAt; stamps are
             written only through SyncDownloadedItems, which never runs on a failed download]
      WHY 4B  The delta path assumes "the download can be asked for" once the sweep succeeded. It has no
              notion that a big moved set can be impossible to ask for. The sweep itself uses the Team's
              own short query and succeeds. Only the keyed download is long.
              [SweepWorkItemsForTeam JWTC:116 uses PrepareQuery(team), with no key list]
        WHY 5B  ROOT CAUSE B (contributing, self-sustaining): a deterministic transport failure in phase 2
                becomes a permanent outage, because phase-2 failures are not separated from "the tracker
                is down". Once ≥ ~197 long keys count as moved, the Team is locked out. Why ≥ 200 moved in
                the first place (first delta after an upgrade, unset LastChangedRemote, a stamp-format
                change): *Hypothesis — requires verification* from the reporter's DB/log
                ("Updating {Count} Work Item(s) by reference id").
        -> SOLUTION B: fixing A removes the trigger. Optional hardening: on a WorkTrackingRefusedException
           from the keyed download, fall back to FetchEverything once and log it loudly (separate ticket).

WHY 1C  Write-backs: "0 succeeded, 28 failed".
  WHY 2C  The write-back path does NOT issue any key-batched JQL.
          [JWTC:505-571 WriteFieldsToWorkItems: one GET rest/api/latest/field (GetCustomFieldMappings
           JWTC:1510-1521), then one PUT rest/api/latest/issue/{key} per issue; WriteBackService.cs:147-170
           resolves items from the local repository, not from Jira]
    WHY 3C+ Not determinable from code. JiraQueryRejectedException cannot come out of this path.
            "All failed" points to the catch-all at WriteBackService.cs:48-66 (a field-list refusal or another
            exception, applied to every update) or to 28 per-issue PUT refusals.
            *Hypothesis — requires verification*: ask for the Debug lines
            "Write-back failed for work item {WorkItemId} …" (WriteBackService.cs:71-78) or the Error line
            "Write-back failed … with unhandled exception".
        -> NOT addressed by this fix. Track separately if the per-item messages show it is not 414-related.
```

### All key-batched / caller-sized JQL call sites

Every Cloud search goes through `WalkCloudSearchPages` (GET). Every Data Center search goes through
POST `rest/api/latest/search`.

| Call site | JQL | Cloud | Data Center |
|---|---|---|---|
| `GetWorkItemsForTeam(team, ids)` JWTC:260-283 (delta download) | key OR-list, chunked by 200 | GET via `GetIssuesByQueryFromCloud`, `fields=*all&expand=changelog` | POST `GetIssuesByQueryFromDataCenter` JWTC:2110-2154 (`:2133`) |
| `GetFeaturesForProject(portfolio, ids)` JWTC:329-349 (Feature delta download) | same | GET | POST |
| `GetParentFeaturesDetails` JWTC:358-375 (parent details) | same | GET | POST |
| `SweepParentFeatures` JWTC:381-386 → `SweepIdentities` → `WalkSweep` JWTC:2347-2356 (identity sweep, keyed) | same, `fields=key,updated` | GET `WalkCloudSearchPages` | POST `WalkDataCenterSearchOffsets` JWTC:2421-2457 (`:2439`) |
| `TryWalkReleaseMembership` JWTC:3205-3241 (Delivery release membership) | `fixVersion in (ids)`, not chunked | GET | POST |
| `SweepWorkItemsForTeam` / `SweepFeaturesForPortfolio` / full downloads / validation (JWTC:116, 356, 248, 316, 1130, 1171) | operator query, no key list | GET | POST |
| Write-back JWTC:505-571 | **no JQL** (PUT per issue) | n/a | n/a |

So **one method**, `WalkCloudSearchPages` JWTC:2362-2410, is the single place where Cloud's JQL
goes into the URL. Changing that method fixes every row at once.

### Is there a POST-capable path already? Pagination and changelog over POST

- POST exists for Data Center only: `DataCenterSearchPath` JWTC:53-60, `AsJson` JWTC:2194.
  `JsonSerializer.Serialize(new {…})` + `client.PostAsync` JWTC:2125-2133 and 2430-2439.
- Jira Cloud `POST /rest/api/3/search/jql` (operationId `searchAndReconsileIssuesUsingJqlPost`) has
  the **same response schema** as the GET (`SearchAndReconcileResults`: `issues`, `nextPageToken`,
  `isLast`, `names`, `schema`, `warnings`). Token paging works the same way: `nextPageToken` goes
  in the body. Changelog comes inline just as it does over GET (`changelog.total`, `histories`), so
  `ShouldFetchFullChangelog` / `CreateIssueWithCompleteChangelog` JWTC:2088-2108, 2316-2337 stay
  unchanged.
- Shape differences from the GET **and** from the Data Center POST:
  - `fields` is a **string array** (`["*all"]`, `["key","updated"]`).
  - `expand` is a **comma-delimited string** (`"changelog"`), *not* an array. Data Center takes
    `["changelog"]` (`TheChangelogAsWell`, JWTC:62), so that constant must not be reused for Cloud.
  - Leave `nextPageToken` out of the first page. Atlassian JRACLOUD-94632
    (https://jira.atlassian.com/browse/JRACLOUD-94632) reports that a null token on the first page
    causes trouble on GET. Leaving it out works on every variant.
- Auth: the classic scope `read:jira-work` covers both GET and POST, and that is the scope Lighthouse
  documents (`docs/concepts/worktrackingsystems/jira.md:38,83`, `JiraOAuthProvider.cs:23`). The beta
  *granular* scopes listed for the two operations differ, so a custom granular-only scoped token is a
  small residual risk (§5).

### Branch status

| Branch | Status |
|---|---|
| A, transport (GET query string on Cloud) | **Closed.** Root cause found, and the fix is in §4 |
| B, permanent lockout | **Open**: why ≥ ~200 keys counted as moved. Candidates and the checks that tell them apart: (1) first delta after the upgrade that brought in incremental sync, where stored items have `LastChangedRemote` unset (check the DB for NULL/default on the Team's items); (2) a stamp-format change between stored and swept values, for example the 2026-10-16 instant-offset fix (compare one stored `LastChangedRemote` with the issue's `updated`); (3) a real bulk edit in Jira (check the Jira audit log). The Debug line `Updating {Count} Work Item(s) by reference id` gives the size |
| C, write-backs 0/28 | **Open, not this defect.** Pull the Error line `Write-back failed for connection … with unhandled exception` (WriteBackService.cs:53-55): if present, every item failed for one reason, most likely the field-list GET. Otherwise pull the Debug lines `Write-back failed for work item …` (`:75-77`): those are per-issue PUT refusals, and the line names the reason |

**Feature / parent-Feature path.** `GetFeaturesForProject(portfolio, ids)` (JWTC:342-345),
`GetParentFeaturesDetails` (JWTC:367-371) and `SweepParentFeatures` (JWTC:381-386) build the same
key list on the same Cloud GET. A Portfolio with ≥ ~200 moved long-key Features, or parents, fails
the same way. Whether it locks out permanently like branch B depends on the Portfolio fetch
orchestration (WorkItemService.cs:1228-1325) and on PortfolioUpdater's catch (`PortfolioUpdater.cs:145`).
That has not been traced here. The fix in §4 covers it either way.

**Immediate mitigation (before the release).** Any refresh that takes the full download avoids the
keyed query. The full download uses the Team's own short query, so it succeeds and stores fresh
stamps. That shrinks the moved set and unlocks the delta path. *Requires verification*: whether a
user-triggered full sync is available for a Jira Team. The code only falls back to it when the sweep
fails (WorkItemService.cs:300-332). **Permanent fix:** §4.

## 3. Why the tests did not catch it

1. **The stub transport has no URI limit.** `JiraIncrementalSyncTest.JiraStub`
   (`Lighthouse.Backend.Tests/…/Jira/JiraIncrementalSyncTest.cs:1217-1300`) is a Moq
   `HttpMessageHandler` that answers any URI of any length with 200. A 414 can only show up if a test
   asserts on the request itself.
2. **The one length assertion is Data Center-only.**
   `GetWorkItemsForTeamByReferenceId_OnDataCenter_AsksForAQueryNoRequestLineCouldCarry`
   (`:974-1001`) was added with baa2c0b40 for 200 keys like `LIGHTHOUSEPROJ-000000001`. There is no
   `[TestCase(Cloud)]` twin, in line with the roadmap's "Cloud has no such exposure".
3. **The Cloud tests use short keys.** `TheTwoKeysAskedFor = ["PROJ-1","PROJ-3"]` (`:62`). The
   `…SplitsALargeSetAcrossSeveralRequests` tests (`:452, 632, 702, 715`) assert chunk count, not
   bytes. At 6-character keys a full chunk is 6.5 KB, under any limit.
4. **Live Jira tests do not exercise it.** `JiraWorkTrackingConnectorTest` / the Integration
   categories run against a demo instance with short keys (`LGHTHSDMO-12`, `IssueFactoryTest.cs:30`)
   and small moved sets.

## 4. Proposed fix (one direction): send every Cloud search as POST with a JSON body

Change only `WalkCloudSearchPages`. It already funnels the full download, the delta download,
parent details, both sweeps and release membership on Cloud. Keep `ReferenceIdsPerQuery = 200`
(it still bounds the response size and round-trip cost, and that is all it should bound) and keep
`PrepareIssueKeyQuery` as is (the test stubs parse its `" OR "` form).

```diff
@@ JWTC: usings (:14-19)
+using System.Text.Json.Serialization;

@@ JWTC: near the other search constants (~:60)
+        private static readonly string CloudSearchPath = JiraRestRoutes.For(JiraDeployment.Cloud).Search;
+
+        // A first page has no token and a sweep wants no expansion; leaving them out of the body
+        // rather than sending null keeps the request the same as the query string it replaces.
+        private static readonly JsonSerializerOptions CloudSearchBodyOptions =
+            new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
+
+        // Cloud reads expand as one comma-joined string, where Data Center reads a list.
+        private const string ChangelogExpansion = "changelog";

@@ JWTC:2373-2389 WalkCloudSearchPages
-                var url = new StringBuilder(JiraRestRoutes.For(JiraDeployment.Cloud).Search).Append('?');
-                url.Append("jql=").Append(Uri.EscapeDataString(request.Jql));
-                url.Append("&fields=").Append(Uri.EscapeDataString(request.Fields));
-
-                if (request.ExpandChangelog)
-                {
-                    url.Append("&expand=changelog");
-                }
-
-                url.Append("&maxResults=").Append(request.PageLimit);
-
-                if (!string.IsNullOrEmpty(nextPageToken))
-                {
-                    url.Append("&nextPageToken=").Append(Uri.EscapeDataString(nextPageToken));
-                }
-
-                var response = await client.GetAsync(url.ToString(), cancellationToken);
+                var payload = JsonSerializer.Serialize(new
+                {
+                    jql = request.Jql,
+                    fields = request.Fields.Split(','),
+                    expand = request.ExpandChangelog ? ChangelogExpansion : null,
+                    maxResults = request.PageLimit,
+                    nextPageToken = string.IsNullOrEmpty(nextPageToken) ? null : nextPageToken,
+                }, CloudSearchBodyOptions);
+
+                var response = await client.PostAsync(CloudSearchPath, AsJson(payload), cancellationToken);
```

Also fix the comments that are now wrong (this follows the CLAUDE.md rule to fix comments as you
touch them):
- `JiraRestRoutes.cs:58-60`: "Cloud's is read with a query string" becomes "Both take their
  parameters in a JSON body; Cloud is paged by token, Data Center by offset."
- `JWTC:53-59` (the `DataCenterSearchPath` summary) and the "OR clause in a URL" comments at
  JWTC:270-272, 340-341, 365-366: say the chunk bounds round-trip size, and that the query rides in
  the body on both deployments.
- Check whether `StringBuilder` / `System.Text` usings are still needed after the edit.

Alternatives considered (not recommended):
- **`key in ("A","B",…)`**: cuts the URI by ~35% (8,065 → 5,237 at 14 characters) but is still
  count-bound. Keys of about 40 characters, or a longer hostname or proxy path, hit the same cliff.
  It also breaks the test stubs that split on `" OR "` (`JiraIncrementalSyncTest.cs:1358-1368`,
  `ParentPlacementAcceptanceTest.cs:87`). It works as a second step, not as the fix.
- **Size chunks by encoded length**: correct, but it duplicates a budget (an undocumented Cloud-edge
  limit, plus host and proxy) that POST makes unnecessary. It also turns one walk into more round trips.

### ci-learnings rules that apply

- **CA1869**: cache the `JsonSerializerOptions` in a `static readonly` field (4× recurrence,
  `docs/ci-learnings.md:163`).
- **S1192**: `"changelog"` already appears as a literal (JWTC:62). Use a named const rather than a
  third literal.
- **JiraRestRoutes rule** (`docs/ci-learnings.md:211-212`, `JiraRouteLiteralsTest`): reuse
  `JiraRestRoutes.For(Cloud).Search`, and write no `rest/api/3/…` literal in the connector.
- **NUnit2056 / NUnit2045**: the new test uses `using (Assert.EnterMultipleScope())`.
- **No live connector runs** to verify. Unit fixtures only. The Linear key is shared with CI.
- Run the analyzer / Sonar-rule build **after the last edit** (`docs/ci-learnings.md:14-60, 309`).

## 5. Risk assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| `expand` sent as an array (copying Data Center) is ignored or refused by Cloud, so the changelog is lost and every transition is silently gone | Medium if copied blindly | Separate `ChangelogExpansion` string const. Test asserts `"expand":"changelog"` is a JSON string |
| An explicit `"nextPageToken":null` on page 1 gets a 400 | Low | `WhenWritingNull`. Existing token-paging tests (`:347-360`) still cover page 2 |
| Cloud POST defaults `fields` to `id` only if `fields` is left out | Low | `fields` is always sent (`*all` / `key,updated` / `ReleaseMembershipFields`) |
| Test fixtures that read Cloud JQL from the query string break | Certain, test-only | `JiraIncrementalSyncTest.SearchAsk` already falls back to the body. Needs updating: `StubTransport.FieldsAskedForInTheLastSearch` (`TestHelpers/StubTransport.cs:74-75`, used by `JiraDependencyLinkTest`), `ParentPlacementAcceptanceTest.TheQueryIn` (`:561-566`, falls back to the raw JSON body, where `key = \"` is escaped and `KeyClause` no longer matches), `JiraDeliverySourceProviderTest.QueryValue` (`:1168-1174`, read at `:860` and `:1424`), `JiraBoardQueryAssemblyTest` (`:1101`, records `PathAndQuery`). Grep `"jql="` and `.Query` across `Lighthouse.Backend.Tests` |
| Custom granular-scoped API token lacking the POST's beta scopes | Low | Documented scopes are classic `read:jira-work`, which covers both. Mention it in release notes if a scoped-token customer reports it |
| Search caching / proxies treating POST differently | Negligible | Jira search responses are not cached by Lighthouse per URL |
| Mutation-testing debt | — | `WalkCloudSearchPages` already had survivors (`docs/feature/fix-jira-dc-portfolio-refusal/mutation/results.md:48`). Run Stryker on the connector after the code is frozen |

Blast radius: Cloud only, but **every** Cloud search (refresh, sweeps, validation preview with
`maxResults=10`, Delivery release membership). That is intended. It is also why the change is one
method: there is no second Cloud transport left to drift.

## 6. Proposed regression test

**Where:** `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraIncrementalSyncTest.cs`,
next to `GetWorkItemsForTeamByReferenceId_OnDataCenter_AsksForAQueryNoRequestLineCouldCarry` (`:974`).
Turn that test into a `[TestCase(Cloud)] [TestCase(DataCenter)]` pair (rename it
`GetWorkItemsForTeamByReferenceId_AsksForAQueryNoRequestLineCouldCarry`), using the existing
`AJiraThatHasAlreadyBeenTalkedTo(deploymentType)` and `JiraStub`.

**Given** 200 keys like `LONGPROJECTKEY-{n:D9}` (19 characters, so the escaped JQL alone is well over
8,192 bytes, and the positive control below holds regardless of the unknown Cloud threshold).
**When** `GetWorkItemsForTeam(team, keys, …)`.
**Then**, in `Assert.EnterMultipleScope()`:
1. positive control: `Uri.EscapeDataString(download.Value("jql")).Length > 8192`;
2. `download.Uri.OriginalString.Length < 8192` (the JQL left the request line);
3. `download.Value("jql")` contains the last key (no truncation);
4. `workItems.Count() == 200` (the answer was read back).

Add a Cloud-only companion that pins the wire shape, since the transport is what regressed:
`GetWorkItemsForTeam_OnCloud_PostsTheSearchWithTheChangelogExpansionAsOneString`. Record
`request.Method` in `JiraStub.Respond` (extend `SearchAsk` with `HttpMethod Method`). Assert
`Method == HttpMethod.Post`, that the path ends `rest/api/3/search/jql`, that the body `expand` is a
JSON **string** `"changelog"`, that `fields` is an array `["*all"]`, and that page 1 has no
`nextPageToken`. Extend the existing token-paging Cloud sweep test (`:345-360`) to assert that the
second request carries `nextPageToken` **in the body**.

**Why these fail on current code:** on Cloud, `WalkCloudSearchPages` puts the escaped JQL in the URI
(JWTC:2374), so assertion 2 sees ~8.1 KB and fails, and the Method assertion sees `GET`. The
Data Center case passes today, which shows the test detects exactly the Cloud defect.

Optional (separate ticket, ROOT CAUSE B): a `WorkItemService` test where the keyed download throws
`JiraQueryRejectedException`. Assert today's behaviour (refresh refused, nothing stored), and later
the fallback to `FetchEverything` if that hardening is accepted.

## 7. Contributing factors (summary)

1. A count-based chunk standing in for a byte budget (JWTC:66), with the reasoning copied from ADO,
   whose ids are integers in POST bodies.
2. A previous RCA/fix (baa2c0b40, roadmap.json:55) that named the real constraint ("COUNT vs
   LENGTH") and then ruled Cloud out on a non-sequitur (token paging).
3. The route abstraction's documentation recording GET as a fact of Cloud (`JiraRestRoutes.cs:59`).
4. A test stub that cannot produce a 414, and Cloud tests that use only short keys.
5. Phase-2 (keyed download) refusals have no fallback, so one deterministic failure becomes a
   permanent outage (WorkItemService.cs:347-360 vs 300-332).
6. Unexplained: the write-back failures (branch C). They are not on a JQL path.

## 8. Other connectors (note only, no scope expansion)

- **Azure DevOps**: WIQL goes through `QueryByWiqlAsync` (POST body). `TheQueryParentFeaturesAreFetchedBy`
  (`AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1272-1276`) is an **unchunked** `[System.Id] = n OR …`
  list. Integer ids make it about 25 characters per id against WIQL's 32K-character limit, so it is
  only exposed at more than ~1,200 parents. Low risk, worth a note.
- **ServiceNow**: `ServiceNowHistoryQuery.RecordsPerBatch = 200` (`ServiceNow/ServiceNowHistoryQuery.cs:9-18`)
  is count-based **but measured** (245 ids → 200, 250 → 414 at 8,347 bytes). sys_ids are a fixed 32
  characters, so count is a faithful proxy for length. The residual risk is long hostnames or proxy
  subpaths, which its own remarks already name.
- **Linear**: GraphQL over POST. Not exposed.
- **Jira `TryWalkReleaseMembership`** (`fixVersion in (ids)`, unchunked): fixed by the same
  `WalkCloudSearchPages` change.
