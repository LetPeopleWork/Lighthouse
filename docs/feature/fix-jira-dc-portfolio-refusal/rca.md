# Bug #6019 — Jira Data Center Portfolio refresh fails with a bare 400

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6019 (Bug, Active, tagged Release Notes)
Reported by Steve (community). RCA performed 2026-09-17 at commit `292bf033a`.

## Reported symptom

```
20:17:19 - ERROR - PortfolioUpdater: An exception occurred while updating Portfolio with ID 4:
Response status code does not indicate success: 400.
System.Net.Http.HttpRequestException: Response status code does not indicate success: 400.
```

Nothing names the query Jira refused or what Jira answered. Reporter's words: "The last thing I want
to do when something fails without any info is click all over then scroll for a couple mins."

Reporter is on v26.9.9.9 or older. Two commits post-date that tag and were recorded on the work item
as a likely fix: `36817a1b5` (report what Jira said) and `690a208f9` (chunk the parent Feature fetch).

## What the RCA established

The literal sentence is gone — `GetIssuesByQueryFromDataCenter` no longer calls
`EnsureSuccessStatusCode()`. No such call remains on the Data Center refresh path; the three
surviving call sites are Cloud-only or telemetry.

The reasoning recorded on the work item was wrong even though its conclusion held.
`ValidatePortfolioSettings` is reachable only from `PortfoliosController` — it is the connection
**validation** path and never runs during a background refresh. `f6d0c161c` is likewise
validation-only. The refresh path reaches `LogTheRefusal` because that call sits at the shared throw
site (`RejectedQuery:1734`), not in the validation catch.

Four root causes remain live. Two are in scope here.

### A — the refusal never reaches any surface an operator reads (IN SCOPE)

`PortfolioUpdater` has no logging catch: its only two catches (`:127` `OperationCanceledException`,
`:132` `UnreadableSecretException`) rethrow without logging, which `UpdateServiceBase.cs:24-33`
documents as deliberate. The exception escapes to `UpdateQueueService.cs:447`, which logs
`"Error processing update task for {UpdateType} with ID {Id}"` — a *type of work*, not the portfolio.

So the query and Jira's answer go out on a Warning from the connector that names no portfolio, and
the failure goes out on an Error that names no query. Nothing correlates them; on an instance
refreshing several portfolios concurrently they cannot be paired by eye.

The channel that would join them already exists and is wired for exactly one exception type:
`SyncOutcome.Reason` is set only inside `catch (UnreadableSecretException)`
(`PortfolioUpdater.cs:132-143`), rendered by `UpdateServiceBase.cs:159-163` as `| reason=`. For a
`JiraQueryRejectedException` the outcome is still `SyncOutcome.None`, so the summary line reads
`success=False` with no reason — while the exception in hand carries *both* Jira's sentence (its
`Message`) and the rejected JQL (`JiraQueryRejectedException.cs:36 RejectedQuery`) as properties.

**Root cause:** the refresh pipeline models failure as "it threw", and the reason-carrying channel it
already owns was wired for one bespoke exception rather than for "the tracker refused us" as a class.

### B — the 400 itself: a length-bounded GET nothing measures (IN SCOPE)

Data Center search puts the whole JQL in the request line, against a servlet-container budget this
repo has already measured: `docs/feature/epic-5513-servicenow-integration/spike/findings.md:412-426`
bisected the same 8192-byte cliff on a comparable API (245 ids = 8182 B → 200 OK; 250 ids = 8347 B →
414). `docs/feature/fix-jira-board-team-validation/rca.md:242-245` records the Jira side: Atlassian's
KB names request-header size against Tomcat/proxy configuration as the *first* cause of 400 on the
search endpoint, and DC's bundled Tomcat ships `maxHttpHeaderSize=8192`.

`ReferenceIdsPerQuery = 200` (`:50`) was copied from `AzureDevOpsWorkTrackingConnector.cs:36`, whose
identifiers are short numerics — the comments at `:244-246`, `:313-314` and `:338-339` say so in as
many words. At ~1.56x encoding expansion, 200 Jira keys of 12 characters is ~7.6 KB of request line
before headers; at 16 characters it is over the cliff. Chunking bounds a *count*; the constraint is a
*length*, and no component between `PrepareIssueKeyQuery:1949` and the `GetAsync` knows how long the
request it is about to make is. The existing tests (`JiraIncrementalSyncTest.cs:417/597/667`) assert
chunk *count* only.

This was already written down in this repo on 2026-09-11 and left open —
`fix-jira-board-team-validation/rca.md:264-267` (root cause C, conditional) and its CF-8 at `:380`,
"DC search is a length-bounded GET where DC also accepts POST". That RCA's plan was: ship the log
line first, then move DC search to POST if the evidence points there. The log line shipped; the POST
move did not. **Bug #6019 is that deferral coming back.**

**Two GETs carry the JQL in the URL, not one** — both on the refresh path:
- `GetIssuesByQueryFromDataCenter:1660` — the full download
- `WalkDataCenterSearchOffsets:1911` — the identity sweep

### C — the explanation is empty exactly when the cause is length (partly addressed here)

`ExplanationIn:1763-1770` falls back to `"Jira answered 400 BadRequest."` whenever the body has no
`errorMessages` array, and `ErrorMessagesIn:1772-1795` returns empty for any non-Jira-JSON body. A
request-line-too-long 400 is produced by Tomcat or the proxy *before* Jira's application code runs,
so there is no such array — the refusal root cause B predicts is precisely the one that renders as
uselessly as what Steve complained about. `JiraBoardQueryAssemblyTest.cs:516` pins that string.

### D — no reason is persisted or shown (OUT OF SCOPE, see below)

`Models/RefreshLog.cs` has no reason column and `RefreshLog.ts` mirrors it.
`RecentProblemsSink.cs:83-92` captures the exception *type name*, never its message. And
`LoggingConfigurator.cs:42/45` puts `LogTheRefusal` behind the operator-settable global level
(`SerilogLogConfiguration.cs:104-108`, `LogSettings.tsx:48-51`) — an operator who sets it to Error
loses the refusal from both the file and the Recent Problems panel.

## Scope decision (user, 2026-09-17)

**In scope:** root causes A and B, plus the part of C that makes B reportable, plus the missing
refresh-path regression tests.

**Out of scope, deliberately:** root cause D (a `RefreshLog.Reason` column across SQLite and Postgres
plus the frontend row, and carrying the exception message into Recent Problems). That is a slice, not
a bugfix. It is what would actually stop the reporter needing to open a log file at all, so it should
be raised as its own work item rather than dropped.

**Superseded by the POST decision:** the RCA proposed reducing `ReferenceIdsPerQuery` from 200 to 100
and adding a Tomcat-specific sentence about request length to `ExplanationIn`. Moving DC search to
POST removes the URL-length bound entirely rather than reducing it, so the chunk size **stays at
200** — halving it would double round trips for a constraint that no longer exists — and the
explanation fallback names the query Lighthouse sent rather than lecturing about Tomcat.

## What only the reporter can close

Whether *his* 400 recurs, and what Jira actually refused, cannot be settled here. Per the standing
rule, this bug closes on the reporter's own trace, not on our reading of our own code.

One tell worth sending him: his line was written by `PortfolioUpdater`; on current main the
equivalent line is written by `UpdateQueueService`. **If his new log still says `PortfolioUpdater:`,
he is on the old build and nothing has been tested.** Ask for the **Warning**-level lines around the
failure, not the ERROR lines, and have him confirm the log level is Information or lower — at Error,
`LogTheRefusal` does not exist.

The cleanest discriminator: a JQL *content* refusal (an unsupported construct on his DC version, a
state name that does not exist on his instance) comes back with a populated `errorMessages` array. A
*length* refusal comes back with an empty or HTML body. One Warning line tells the two apart.

## Contributing factors not addressed by this fix

| # | Factor | Evidence | Disposition |
|---|---|---|---|
| CF-4 | `RefreshLog` has no reason column | `Models/RefreshLog.cs`, `RefreshLog.ts` | Root cause D — own work item |
| CF-5 | `RecentProblemsSink` keeps exception type, not message | `RecentProblemsSink.cs:83-92` | Root cause D — own work item |
| CF-7 | Operator log level can suppress `LogTheRefusal` from file *and* panel | `LoggingConfigurator.cs:42/45` | Root cause D — own work item |
| CF-8 | `AddTheWorkThatCarriesTheRelease:2538` builds `fixVersion in (...)` unchunked | reached via `PortfolioUpdater.cs:104` | Closed incidentally by POST; degrades softly already (`TryWalkReleaseMembership:2583-2596`) |
| CF-9 | `SweepIdentities:118-122` rethrows the typed refusal as `InvalidOperationException` | logs first, caller falls back loudly | Low — leave |
| CF-11 | `SetStoredFieldKeys:1614` → `GetCustomFieldMappings:1438-1449` is a second DC hop | fails closed with `JiraReadException.FieldListRefused` | **Ruled out** as a source of the reported symptom; recorded so it is not re-derived |
