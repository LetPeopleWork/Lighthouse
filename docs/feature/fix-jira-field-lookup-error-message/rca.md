# RCA — follow-up to Bug #5973: the Jira **custom-field** path still answers with a generic error

Method: Toyota 5 Whys, multi-causal, evidence required at every level.
Date: 2026-09-16 · Investigator: Rex (nw-troubleshooter)
Repo state: worktree `frolicking-fluttering-karp`, branch `worktree-frolicking-fluttering-karp`,
HEAD `73335a089`.
Predecessor: `docs/feature/fix-jira-board-team-validation/rca.md` (Bug #5973, delivered 2026-09-11,
`docs/evolution/2026-09-11-fix-jira-board-team-validation.md`).

Unless stated otherwise, every `:NNN` refers to
`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs`
(2693 lines at HEAD).

---

## 1. Problem statement (scoped)

A paying customer on **Jira Data Center** configures **additional fields** on a Jira connection and
is shown a generic error instead of a sentence naming what Jira refused.

Bug #5973's fix F3 replaced `EnsureSuccessStatusCode()` **on the search endpoints only**
(`:1639`, `:1829`, `:1874` now build a `JiraSearchRejection` and throw
`JiraQueryRejectedException` via `RejectedQuery` `:1709-1714`). The **custom-field** endpoint
`GET rest/api/latest/field` was not touched: `:1427` is still a bare
`response.EnsureSuccessStatusCode();`.

**In scope:** the `GET rest/api/latest/field` call at `:1424-1427`, its three callers, and every
verdict an administrator can be shown as a result — from the connection screen, the team/portfolio
settings screens, and the wizards.
**Out of scope:** the search path (fixed by #5973), the board-reading path (fixed by #5973), the
Releases/delivery-source reads (`:2277`, `:2310`), and any change to the four non-Jira connectors
(their gap is measured in §5 but not fixed here).

### What the user sees today — four distinct generic strings and one silence, all from one endpoint

| # | Entry point | Verdict shown | Produced at |
|---|---|---|---|
| S1 | Connection screen → **Validate** | `"Could not reach Jira with the provided URL."` + `"Response status code does not indicate success: NNN (…)."`, `fieldName = Url` | `:411-415` |
| S2 | Connection screen → **Validate**, Data Center, a field reference that matches nothing | `"Connection validation failed due to an unexpected error."` — **no technicalDetails at all** | `:429-431` |
| S3 | Team settings → **Validate** | `"Team validation failed due to an unexpected error."` + `"Response status code does not indicate success: NNN (…)."` — the original #5973 wording, verbatim, still shipping | `:1078-1081` |
| S4 | Portfolio settings → **Validate** | `"Portfolio validation failed due to an unexpected error."` + same second line | `:1115-1118` |
| S5 | **None** — background team/portfolio refresh | An unhandled `KeyNotFoundException`; no verdict, no field name, nothing on any settings screen. Found during this investigation, **not** in the customer's report | `:1458`, reached from `:266` / `:1139` — see Chain E |

---

## 2. Seed-claim verification (confirm / correct, with evidence)

| # | Seed claim | Verdict | Evidence |
|---|---|---|---|
| 1 | `GetCustomFieldMappings` (:1421-1427) does `GET rest/api/latest/field` then `EnsureSuccessStatusCode()`, body discarded | **CONFIRMED** | `:1424` `const string url = "rest/api/latest/field";` · `:1426-1427` `var response = await jiraClient.GetAsync(url); response.EnsureSuccessStatusCode();` · `:1429` `ReadAsStringAsync()` is *after* the throw, so it is never reached on a non-2xx |
| 2 | Reached from `SetStoredFieldKeys` (:1412), which runs inside `GetIssuesByQuery` (:1589) **before** the search | **CONFIRMED** | `:1589` `await SetStoredFieldKeys(client, …);` sits between `GetDeploymentType` (`:1587`) and both search branches (`:1593`, `:1596`) · `:1412-1413` is the call |
| 3 | So `ValidateTeamSettings` (:1048) / `ValidatePortfolioSettings` (:1085) can fail on the field call, never reach the search, and land in `catch (Exception)` (:1075 / :1112) | **CONFIRMED** | `:1054-1055` `PrepareQuery` → `GetIssuesByQuery` · the only earlier catch is `catch (JiraQueryRejectedException)` at `:1071`/`:1108`, and `GetCustomFieldMappings` throws a plain `HttpRequestException`, not that subtype · `:1078-1081` / `:1115-1118` produce the strings in S3/S4 |
| 4 | Reached from `GetCustomFieldReferences` (:1570) → `GetMissingAdditionalFields` (:1038) → `ValidateConnection` (:395), landing in `catch (HttpRequestException)` (:407) → "Could not reach Jira with the provided URL." with `fieldName = Url` | **CONFIRMED** | `:1575-1577` is the call · `:1040` `GetCustomFieldReferences(connection)` · `:395` `await GetMissingAdditionalFields(connection)` inside the `try` opened at `:373` · `:407-415`, `fieldName = JiraWorkTrackingOptionNames.Url` |
| 5 | Also called from `UpdateItems` (:487) on the write-back path | **CONFIRMED, and worse than "check what happens"** | `:487-489` calls it *before* the per-issue loop at `:491`. The per-attempt `catch (Exception ex)` lives in `TryWriteFields` (`:595-599`), **inside** the loop, so it cannot see this throw. `WriteFieldsToWorkItems` (`:470-481`) has no try/catch. `WriteBackService.WriteUpdates` (`WriteBackService.cs:158`) and `WriteBackCollector` (`WriteBackCollector.cs:53`) have none either. The exception escapes the whole write-back flush. |
| 6 | The good case already exists: "Some additional fields could not be found: X" (:398) | **CONFIRMED — and it is unreachable on Jira Data Center.** See Chain C. | `:396-403` |
| 7 | Frontend: validate → POST `/validate` → controller returns `BadRequest(validationResult)` on `!IsValid`, so the `catch` branch renders `message` + `technicalDetails` | **CONFIRMED** | `WorkTrackingSystemConnectionsController.cs:145-151` — `BadRequest(validationResult)` at `:148`, `Ok(validationResult)` at `:151` · `BaseApiService.ts:36-37`, `:42-60`, `:91-102` lift `message`/`technicalDetails`/`fieldName`/`code` off the 400 body into an `ApiError` · `ModifyConnectionSettings.tsx:444-451` → `resolveValidationErrorMessage` (`:77-98`) → `setValidationErrorMessage` + `setValidationTechnicalDetails` |
| 8 | The `if (!isValid)` branch at `ModifyConnectionSettings.tsx:437` is genuinely unreachable for real failures, and would drop the backend message | **CONFIRMED with a correction** | It substitutes a hardcoded string (`:439-441`) and drops `message`/`technicalDetails` — confirmed. It is unreachable **for any backend-produced validation failure**, because the controller only ever returns a `!IsValid` body with HTTP 400 (`:148`), which axios throws on. It is **not dead code**: `readConnectionValidation` (`ConnectionValidationResult.ts:9-21`) returns `{isValid:false}` for a `null`/`undefined`/malformed **200** body, so the branch is a live defensive guard for "the endpoint answered 200 and said nothing". Correction to the seed: it is unreachable for *real failures*, not unreachable outright. **Leave it in place; do not use it to carry a message.** |

### One claim the seed did not make, and it is the decisive one

`GetIdForCustomFieldByProperty` (`:1454-1461`) reads each property with
`f.GetProperty(propertyIdentifier)` — the **throwing** accessor:

```csharp
var elements = allFields.RootElement.EnumerateArray()
    .Where(f => string.Equals(
        f.GetProperty(propertyIdentifier).GetString(),   // <- :1458
        customField,
        StringComparison.OrdinalIgnoreCase))
    .ToList();
```

`JsonElement.GetProperty(String)` throws **`KeyNotFoundException` — "No property was found with the
requested name."** ([Microsoft Learn][ms-getproperty]). And `GetCustomFieldReferences` (`:1575-1577`)
and `UpdateItems` (`:487-489`) both pass the identifier list
`[NamePropertyName, IdPropertyName, KeyPropertyName]` = `["name", "id", "key"]`
(`JiraFieldNames.cs:49,51,53`).

**Jira Data Center's `GET /rest/api/2/field` does not return a `key` property.** The documented
response object carries `id`, `name`, `custom`, `orderable`, `navigable`, `searchable`,
`clauseNames`, `schema` — and nothing else ([Jira DC 9.12 REST reference, `field-getFields`][dc-field]).
`key` is a **Jira Cloud** addition.

That makes the following true on Data Center, by construction:

> Every additional-field reference that matches no field falls through `name`, falls through `id`,
> reaches `key`, and throws `KeyNotFoundException` on the **first** element of the array.
> Therefore `GetIdForCustomFieldByProperty` can never return `string.Empty`, therefore
> `GetMissingAdditionalFields` (`:1042-1045`) can never produce a non-empty list, therefore
> the `"additional_fields_invalid"` verdict at `:398-402` — *"Some additional fields could not be
> found: X"* — **is unreachable on Jira Data Center.**

`SetStoredFieldKeys` (`:1412-1413`) passes `[NamePropertyName]` only, so the search path is immune
to this and can only fail on an HTTP status. That is why the two branches must be told apart.

---

## 3. Which status codes can `GET rest/api/latest/field` actually return?

**Documented, for Jira Data Center:** exactly two.

| Status | Atlassian's words | Source |
|---|---|---|
| 200 | "Contains a full representation of all visible fields in JSON." | [dc-field] |
| 401 | "Returned if user is not logged-in and don't have access to any project" | [dc-field] |

No `400` is documented, and the call site sends **no query string at all** (`:1424` — the URL is the
bare constant `"rest/api/latest/field"`, with no interpolation anywhere). There is no input for Jira
to reject.

### So: is the customer's report a 400?

**Almost certainly not.** The customer's earlier 400 (Bug #5973) came from
`rest/api/latest/search?jql=…`, where the JQL *is* the input and Atlassian documents malformed-JQL
and oversized-request-header as named 400 causes. `/field` has no equivalent. A 400 there would have
to come from something in front of Jira, not from Jira.

**The differentiator, stated up front.** The candidates below split cleanly on one question: *did
the customer's error carry an HTTP status number?* Candidates (b), (c), (e) and (f) all produce a
message ending in a status sentence; candidates (a) and (d) produce a message with **no technical
detail whatsoever**. The report as relayed quotes no status, which favours (a)/(d). **If the
customer can produce a status, this ranking inverts and (c) leads.** Ask for the exact text before
treating (a) as settled.

Ranked candidates for what the customer actually hit, most likely first:

**(a) Not an HTTP status at all — `KeyNotFoundException` on the missing `key` property. [most likely]**
Evidence: the report is specifically *"while configuring additional fields"*; the reporter is on
**Data Center**; `key` is Cloud-only ([dc-field]); the throw is unconditional for any unmatched
reference (§2, last row). It surfaces as **S2** — `"Connection validation failed due to an
unexpected error."` with **no technical details whatsoever**, which is the most "generic error"
string the connector can produce. A user who mistyped a field name, or pasted a Cloud-style key such
as `com.atlassian.atlas.jira__project-status`, hits this every time.
Note this also means the customer may have no HTTP status to report at all — consistent with a
vaguely-worded "same generic error again".

**(b) 401 — the only documented failure. [possible, but narrow]**
On the *connection* screen this is nearly excluded: `rest/api/2/myself` runs first at `:376` inside
the same `try`, and a credential Jira rejects is answered at `:377-392` with
`"Authentication failed for Jira."` — never reaching `/field`. On Data Center, Seraph also answers
401/403 when CAPTCHA has been triggered by failed logins, signalled by the `X-Seraph-LoginReason`
header ([Atlassian][seraph]) — but again `/myself` would trip first. 401 on `/field` alone requires
a credential that authenticates but is scoped away from field metadata, which Data Center PATs are
not. **Ruled improbable for the connection screen; still possible on the team/portfolio screens**
(S3/S4), which have no `/myself` pre-flight.

**(c) A proxy answering 502/504 on the size of the `/field` payload. [the best differential 5xx]**
`/field` on a mature Data Center instance returns every field definition on the instance — hundreds
of KB. `/myself` returns a few hundred bytes. A reverse proxy with a small `proxy_buffer_size` or a
tight response timeout can refuse the large one and pass the small one, which is exactly the
asymmetry needed to reach `/field` after `/myself` succeeded. Atlassian's own proxy guidance
documents buffer/timeout tuning as required configuration ([Atlassian][proxy]). **Not diagnosable
today, because `:1427` discards the body** — and a proxy error page is HTML, which is precisely what
distinguishes it from a Jira refusal (JSON `errorMessages`).

**(d) A 200 carrying an HTML SSO interstitial.** `EnsureSuccessStatusCode()` passes, then
`JsonDocument.Parse` (`:1430`) throws `JsonException` → `catch` at `:427` → **S2** again.
Indistinguishable from (a) today.

**(e) A context-path-stripped base URL (`https://jira.corp/jira` → `https://jira.corp/rest/…`).**
`GetOrCreateClient` sets `BaseAddress = new Uri(baseUrl)` (`:2063`/`:2070`) over a URL that
`ResolveBaseUrlAndCacheKeyAsync` has `TrimEnd('/')`-ed (`:2025`), and every call site uses a
*relative* URI, so RFC 3986 relative resolution replaces the last path segment and drops the context
path. **Ruled out for this defect, re-derived here rather than taken on trust:** the stripping is a
property of the *base address*, not of the path, so it applies identically to every relative call
the connector makes. `rest/api/2/myself` (`:376`) is one of those, and it runs first inside the same
`try`; a context-path-stripped base URL therefore produces a 404 there and is answered at `:377-392`
with `"Authentication failed for Jira."` — the `/field` call at `:1426` is never reached. The
predecessor RCA reached the same conclusion by a different route (CF-10: these users had
successfully listed boards, which is also a relative call).

**(f) A 403 from a WAF or from Jira's own websudo/CAPTCHA state.** Undocumented for this endpoint,
and it would have to be path-specific to slip past `/myself`. Recorded so it is not re-derived;
no evidence for it either way.

**On auth scope specifically** (the seed raised it): Lighthouse's two *scoped* Jira auth methods,
`JiraScopedToken` and `JiraOAuth`, are exactly the ones `RoutesViaAtlassianCloudGateway` (`:2016`)
sends through `api.atlassian.com`, i.e. **Cloud only**. A Data Center connection authenticates with
a PAT or basic credentials, which carry no scopes. So "the token is scoped away from field
metadata" is not available as an explanation for this customer.

**Answer to "which one, then":** the code is probably not seeing a 400 here at all. The most
defensible reading is **(a)** — a `KeyNotFoundException` that never involved a status code — with
**(c)** as the leading alternative if the customer can produce a status. The fix in §7 closes both,
and, crucially, makes the two distinguishable from the verdict text for the first time: after F2
and F3, (a) yields *"Some additional fields could not be found: X"* and (c) yields
*"Lighthouse could not read the list of fields… Jira answered 502"*. **Ask the customer for the
verdict text once the fix ships; it is now diagnostic on its own.**

---

## 4. Root-cause chains

```
PROBLEM: An administrator on Jira Data Center configuring additional fields is shown a generic
         error instead of a sentence naming what Jira refused or what could not be found.
```

### Chain A — the field-list refusal is blamed on the URL

```
WHY 1A: The connection screen says "Could not reach Jira with the provided URL." and sends the
        administrator to edit a URL that was never wrong.
  [Evidence: :411-415 — ConnectionValidationResult.Failure("connection_failed",
   "Could not reach Jira with the provided URL.", ex.Message, JiraWorkTrackingOptionNames.Url).
   The frontend renders that message and pins it to the Url field:
   BaseApiService.ts:91-102 lifts fieldName off the 400 body;
   ModifyConnectionSettings.tsx:444-451 renders message + technicalDetails.]

  WHY 2A: A non-2xx from GET rest/api/latest/field throws a bare HttpRequestException.
    [Evidence: :1426-1427 — GetAsync then EnsureSuccessStatusCode(). The .NET message is built
     from the status line only; :1429 (ReadAsStringAsync) is after the throw and never runs.]

    WHY 3A: That exception is raised inside the same try block whose HttpRequestException handler
            exists to describe an unreachable URL.
      [Evidence: the try opens at :373; the /field call is reached at :395 via
       GetMissingAdditionalFields :1038-1046 -> GetCustomFieldReferences :1570-1580;
       the handler is :407-416. There is no catch between them.]

      WHY 4A: The connector has no exception type that means "Jira answered, and refused a read",
              reachable from this path. The type that does exist -- JiraReadException :13,
              carrying a ConnectionValidationResult verdict -- is thrown only from the board
              filter read (:857, :869), and the type thrown here (HttpRequestException) is the
              connector's agreed signal for "Jira could not be asked at all".
        [Evidence: JiraReadException.cs:6-12 says so in its own XML doc; its only factories are
         BoardFilterRefused (:25) and BoardFilterCarriedNoQuery (:32). JiraCouldNotBeAsked
         (:2445-2446) defines "could not be asked" as
         HttpRequestException or TaskCanceledException or JsonException or UriFormatException.]

        WHY 5A: Bug #5973's F3 introduced the "Jira answered and refused" concept but scoped it to
                the three search call sites, because the search is where the reported 400 came
                from. The field endpoint was never a suspect, so it kept the old convention --
                and the old convention is the one that gets misread as a reachability failure.
          [Evidence: after #5973, exactly three EnsureSuccessStatusCode() calls remain in the
           file -- :1274, :1427, :2041 -- and all three search sites were converted
           (:1639, :1829, :1874 build a JiraSearchRejection). The predecessor RCA
           (fix-jira-board-team-validation/rca.md, §2) explicitly examined /field and set it
           aside: "/field takes no query parameters and does not" produce the reported 400.
           That reasoning was right about the 400 and wrong about the conclusion drawn from it.]

-> ROOT CAUSE A: A refusal of the field-metadata read is indistinguishable, by exception type,
   from a failure to reach Jira at all -- so the one handler that exists claims the URL is wrong.
```

### Chain B — the field-list refusal reaches team/portfolio validation as the original #5973 wording

```
WHY 1B: Team settings validation says "Team validation failed due to an unexpected error." and
        "Response status code does not indicate success: NNN (…)." -- the exact two lines the
        customer reported in Bug #5973, still shipping after the #5973 fix.
  [Evidence: :1078-1081 -- Failure("validation_failed",
   "Team validation failed due to an unexpected error.", exception.Message).
   exception.Message for an EnsureSuccessStatusCode() throw is the .NET status sentence.
   Portfolio twin: :1115-1118.]

  WHY 2B: The field read happens BEFORE the search, so the query never runs and the new
          "query_rejected" verdict never applies.
    [Evidence: GetIssuesByQuery :1582-1597 -- SetStoredFieldKeys at :1589 sits between
     GetDeploymentType (:1587) and both search branches (:1593, :1596).
     SetStoredFieldKeys :1412-1413 calls GetCustomFieldMappings.]

    WHY 3B: The only typed catch on these two methods is for JiraQueryRejectedException, which
            the field path cannot throw.
      [Evidence: :1071 / :1108 catch JiraQueryRejectedException and answer via QueryWasRejected
       :1128-1133. :1427 throws a plain HttpRequestException. JiraQueryRejectedException is
       constructed in exactly one place -- RejectedQuery :1709-1714 -- which is called only from
       the three search sites (:1641, :1688, and the DC walk's caller).]

      WHY 4B: SetStoredFieldKeys short-circuits on a WARM cache, so the failure is intermittent
              per process and looks unrelated to the field configuration.
        [Evidence: :1406-1410 -- if Rank and Flagged are already non-empty for this connection
         id, it returns without calling Jira. FieldNames is a process-wide static
         ConcurrentDictionary keyed by connection id (:1396). So the first validation after a
         restart calls /field and can fail; the next one on the same connection does not.]

        WHY 5B: The connector's error model for validation answers "did the call work", and the
                two validation methods each collapse every non-query failure into one
                catch-all sentence -- so a field-metadata problem and a genuine unexpected
                defect are reported with the same words.
          [Evidence: :1075-1082 and :1112-1119 are bare catch (Exception) with a fixed message.
           The same shape exists in three of the four other connectors:
           AzureDevOpsWorkTrackingConnector.cs:303-310 / :337-344,
           LinearWorkTrackingConnector.cs:332-339 / :277-284,
           CsvWorkTrackingConnector.cs:192-199. ServiceNow is the exception -- it has no
           catch-all (ServiceNowWorkTrackingConnector.cs:800-807) because its reads throw a
           verdict-carrying ServiceNowReadException instead.]

-> ROOT CAUSE B: The fix for #5973 gave the search path a typed refusal and a verdict, and left
   the field path -- which runs first on the very same call -- throwing into the catch-all that
   produces the string #5973 was raised about.
```

### Chain C — on Data Center, "field not found" cannot be reported at all

```
WHY 1C: On Jira Data Center, a mistyped or unmatched additional-field reference produces
        "Connection validation failed due to an unexpected error." with NO technical details,
        instead of "Some additional fields could not be found: X".
  [Evidence: :429-431 -- the bare catch at :427 returns Failure("validation_failed",
   "Connection validation failed due to an unexpected error.") with no third or fourth argument,
   so technicalDetails and fieldName are both null. Contrast the intended verdict at :398-402.]

  WHY 2C: Resolving a reference reads a "key" property, and Jira Data Center's field list has no
          "key" property on any field.
    [Evidence: :1458 -- f.GetProperty(propertyIdentifier).GetString() inside a Where predicate.
     :1576 and :488 pass [NamePropertyName, IdPropertyName, KeyPropertyName] =
     ["name","id","key"] (JiraFieldNames.cs:49,51,53).
     Atlassian's Jira DC 9.12 REST reference documents the getFields response object as
     id / name / custom / orderable / navigable / searchable / clauseNames / schema --
     no "key". [dc-field]]

    WHY 3C: GetProperty is the throwing accessor, so a missing property is an exception rather
            than a miss.
      [Evidence: Microsoft Learn, JsonElement.GetProperty(String):
       "KeyNotFoundException -- No property was found with the requested name." [ms-getproperty].
       The surrounding code knows this pattern and uses TryGetProperty everywhere it expects an
       optional property -- :1478 (schema), :1499 (id), :1603 (changelog), :1736 (errorMessages),
       :2540 (key). :1458 is the one place in this method that does not.]

      WHY 4C: The "key" identifier was added for Jira Cloud, and the only test that exercises the
              three-identifier lookup runs against Jira Cloud and cannot run offline.
        [Evidence: JiraWorkTrackingConnectorTest.cs:13-14 -- the fixture carries
         [Category("Integration")] and [Category("JiraIntegration")] at class level.
         ValidateConnection_GivenAdditionalFields_ReturnsTrueOnlyIfFieldsExist (:666) hard-codes
         organizationUrl = "https://letpeoplework.atlassian.net" (:672) and throws
         NotSupportedException without a live token (:674). Its case list includes
         "com.atlassian.atlas.jira__project-status" (:663) -- a Cloud field KEY, which is what
         the third identifier exists to match. Its "MamboJambo" case (:660, expected false)
         proves that on Cloud every field DOES carry "key": the lookup walks the whole array on
         the third identifier and returns a miss rather than throwing.
         No offline fixture covers this. `grep -L "Category(" *.cs` over
         Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/ returns
         exactly six uncategorised fixtures -- JiraBoardQueryAssemblyTest,
         JiraCancellationGranularityTest, JiraDependencyLinkTest, JiraIncrementalSyncTest,
         JiraIssuesPerRequestTest, JiraWorkTrackingConnectorAuthDelegationTest -- and
         `grep -n "ValidateConnection"` over the same directory returns hits in only three files,
         of which one is uncategorised: JiraWorkTrackingConnectorAuthDelegationTest.cs:17,
         ValidateConnection_DelegatesAuthorizationToResolvedStrategy. **That test asserts nothing
         about the verdict** -- it discards the result (`await subject.ValidateConnection(connection);`
         at :32) and verifies only that the auth factory and strategy were called
         (factoryMock.Verify / strategyMock.Verify, :35-39). It also points the connection at
         `http://127.0.0.1:1/` (:28), a port chosen so the socket is refused, so it never reaches
         rest/api/2/myself let alone rest/api/latest/field. **Corrected from an earlier draft of
         this RCA, which said the six fixtures contain no ValidateConnection test at all: one of
         them does, and it is simply not a test of this behaviour.**]

        WHY 5C: The connector reads two different JSON shapes -- Cloud's and Data Center's -- from
                one endpoint through one parser, with no deployment switch and no offline
                coverage of the Data Center shape, even though the connector already models the
                deployment split explicitly for search.
          [Evidence: GetDeploymentType :2110-2140 exists and is consulted at :1587 to choose
           between GetIssuesByQueryFromCloud (:1593) and GetIssuesByQueryFromDataCenter (:1596).
           GetCustomFieldMappings :1421 takes no deployment and is static. The changelog reader at
           :1279-1295 does handle both shapes -- "Try v3 format first (Cloud), then fall back to
           v2 format (Data Center)" -- using TryGetProperty for exactly this reason, which shows
           the shape divergence was known to the author in one place and not carried to this one.]

-> ROOT CAUSE C: The field lookup reads a property that exists only on Jira Cloud with the
   throwing accessor, so on Data Center "this field does not exist" becomes an unhandled
   exception -- making the connector's one good, specific verdict unreachable for the entire
   Data Center population.
```

### Chain D — the write-back path throws into nothing

```
WHY 1D: A field-list refusal during write-back aborts the whole flush for that connection, and no
        per-item result records it.
  [Evidence: UpdateItems :483-497 calls GetCustomFieldMappings at :487, BEFORE the per-issue loop
   at :491. The per-attempt catch (Exception ex) is in TryWriteFields :595-599, inside the loop,
   so it is not on this code path. WriteFieldsToWorkItems :470-481 has no try/catch.
   WriteBackService.WriteUpdates (WriteBackService.cs:145-163) has none. WriteBackCollector
   (WriteBackCollector.cs:51-54) has none.]

  WHY 2D: The isolation the write-back path was designed around is per-issue and per-field, and
          this call is per-connection.
    [Evidence: the XML doc at :499-504 describes the batch-then-isolate design; :516-519 and
     :523-529 implement it. All of it is downstream of :487.]

    WHY 3D: The same helper serves a user-facing validation and a background write, with one
            failure mode for both.
      [Evidence: GetCustomFieldMappings is called from :1412 (sync read), :1575 (validation and
       feature creation) and :487 (write-back). It is one static method with one throw at :1427.]

      WHY 4D: Nothing distinguishes "the write-back could not start" from "the write-back wrote
              nothing", because the result type has no representation for the former.
        [Evidence: WriteBackResult carries ItemResults only (:474, :480); the per-item outcomes
         are Written / Refused (:512, :518, :526-528). There is no connection-level outcome.]

        WHY 5D: Same root as A and B -- a read that Jira refused has no type of its own, so every
                caller has to guess from an HttpRequestException whether Jira was unreachable,
                unwilling, or simply asked for the wrong thing.

-> ROOT CAUSE D: shares root cause A. Recorded separately because the remedy differs: this path
   needs no new verdict, only for the exception to stay an exception with a better message.
```

### Chain E — Chain C also fires with **no** additional fields configured, and it kills the refresh, not the screen

```
WHY 1E: On a Jira Data Center instance that has no field literally named "Flagged", every team
        refresh throws KeyNotFoundException -- with the administrator having configured no
        additional fields at all.
  [Evidence: GetWorkItemsForTeam :255 -> CreateWorkItemsFromIssues :258 ->
   EnsurePredefinedAdditionalFieldsRegistered :264 -> GetCustomFieldReferences :266, which is the
   three-identifier lookup (:1575-1577). The portfolio twin is CreateFeaturesFromIssues :1139,
   reached from GetFeaturesForProject :290 and the two-phase fetch at :321 and :347.]

  WHY 2E: The connector registers a predefined "Flagged" additional field on every refresh, and
          falls back to a HARD-CODED Jira Cloud custom-field id when it could not resolve one.
    [Evidence: GetPredefinedAdditionalFields :161-172 sets
     Reference = ResolveFlaggedFieldReference(connection.Id);
     ResolveFlaggedFieldReference :174-184 returns the cached key if non-empty and otherwise
     DefaultFlaggedFieldReference, which is `private const string DefaultFlaggedFieldReference =
     "customfield_10001";` at :37. customfield_10001 is the Flagged field id on Jira CLOUD;
     Data Center assigns custom-field ids per instance, so it names a different field there, or
     none. The repository says this itself: the test pinning the constant asserts it with the
     message "an unsynced connection resolves the flagged Reference to the stable Jira Cloud
     default." -- JiraWorkTrackingConnectorTest.cs:37-38. The Cloud-specificity was known; what
     was not considered is what the three-identifier lookup does with it on Data Center.]

    WHY 3E: The cached key is empty exactly when the instance has no field NAMED "Flagged".
      [Evidence: SetStoredFieldKeys :1412-1418 resolves Rank / Flagged / Epic Link / Parent Link
       with the single identifier [NamePropertyName] (:1412) and stores whatever comes back,
       including string.Empty (:1415, via GetIdForCustomFieldByProperty :1469).
       JiraFieldNames.cs:63 -- FlaggedName => "Flagged". A Jira Core or Jira Service Management
       instance, or one where the field was renamed, has no such field.]

      WHY 4E: The unresolvable reference is then looked up through the THREE-identifier path,
              which on Data Center reaches "key" and throws -- so a fallback meant to keep the
              feature working is what breaks the refresh.
        [Evidence: same mechanism as Chain C -- :1576 passes ["name","id","key"], :1438-1446
         breaks only on a non-empty match, :1458 throws on the missing "key". Nothing on the
         refresh path catches it: CreateWorkItemsFromIssues :258-282 and CreateFeaturesFromIssues
         :1135-1161 have no try/catch.]

        WHY 5E: Validation and refresh do not resolve additional fields the same way, so the
                screen that exists to tell an administrator the configuration is sound never
                executes the code path that fails.
          [Evidence: ValidateTeamSettings :1055 calls GetIssuesByQuery and then only COUNTS the
           result (:1056) -- it never reaches CreateWorkItemsFromIssues, so it never calls
           EnsurePredefinedAdditionalFieldsRegistered or the three-identifier lookup on the
           predefined field. Compounding this, nothing gates saving on validation at all:
           WorkTrackingSystemConnectionsController.CreateNewWorkTrackingSystemConnectionAsync
           (:82-110) persists the connection at :105-106 without ever calling ValidateConnection,
           which lives on a separate endpoint (:112-152).]

-> ROOT CAUSE E: A Cloud-specific custom-field id is hard-coded as the fallback for a predefined
   field, and on Data Center that unresolvable reference is pushed through the same "key" lookup
   as Chain C -- so the refresh fails on instances where the administrator configured nothing.
```

**Why E matters more than its position suggests.** C is what the customer reported: a screen says
the wrong thing. E is the same defect reached with an empty configuration, and it does not produce
a message at all — it throws inside a background refresh. F3 closes both. Everything else in §7
closes C but not E, which is the argument for treating F3 as the mandatory part of this change
rather than the tidy-up.

**Scope note on E.** F3 is sufficient and is all this change should do: once `key` is a miss rather
than a crash, an unresolvable `"customfield_10001"` resolves to `string.Empty` and
`PopulateAdditionalFieldValues :1559-1567` reads an empty field name, i.e. the flag is simply
absent — which is the correct answer for an instance that has no Flagged field. Replacing the
hard-coded Cloud id at `:37` with something deployment-aware is a separate item, and is **not**
required to stop the throw.

### Cross-validation

- **A, B, C, D and E are consistent, not competing.** A, B and D are three callers of the *same*
  throw at `:1427`, differing only in which handler catches it. C is a second, independent failure
  mode of the *same* method (`GetCustomFieldMappings`), reached only through the two callers that
  pass three identifiers (`:1576`, `:488`) and only on Data Center. A and C can be told apart by
  what the user sees: A yields a URL-blaming message *with* a status sentence; C yields the
  message with **no** technical details at all.
- **Backwards validation A.** If `GET rest/api/latest/field` answers non-2xx while
  `rest/api/2/myself` answered 2xx, then `:1427` throws `HttpRequestException` → `:407` catches →
  `:411-415` returns `connection_failed` / `fieldName = Url` → controller `:148` returns HTTP 400 →
  `BaseApiService.ts:53-59` builds an `ApiError` → `ModifyConnectionSettings.tsx:444-451` renders
  "Could not reach Jira with the provided URL." **Every link has a file:line. Validates.**
- **Backwards validation B.** If the same happens during team validation, `:1589` throws before
  `:1593`/`:1596`; `:1071` does not match (wrong subtype); `:1075` catches; `:1078-1081` returns the
  #5973 wording. **Validates.** Caveat: only on a cold `FieldNames` cache (`:1406-1410`) — which is
  why this is intermittent, and consistent with a report that says "again".
- **Backwards validation C.** If a Data Center instance returns a field array whose objects have no
  `key`, and the configured reference matches no `name` and no `id`, then the third iteration of
  `:1438-1446` calls `GetIdForCustomFieldByProperty(…, "key", …)`, whose `.ToList()` at `:1461`
  enumerates and `:1458` throws `KeyNotFoundException` on the first element. That is neither
  `HttpRequestException` nor `UriFormatException`, so `:427` catches → `:429-431`. **Validates.**
  Reverse check: on **Cloud**, every field carries `key`, so the same walk returns `string.Empty`
  and `:398` reports the field by name — which is exactly what the live Cloud test at
  `JiraWorkTrackingConnectorTest.cs:660` asserts. The two halves agree.
- **Backwards validation D.** `:487` is provably outside every catch on its path (four files
  checked above). **Validates trivially.**
- **Backwards validation E.** If a Data Center instance has no field named `"Flagged"`, then
  `SetStoredFieldKeys :1412-1415` stores `string.Empty` for it; `ResolveFlaggedFieldReference
  :176-183` therefore returns `"customfield_10001"` (`:37`); `EnsurePredefinedAdditionalFieldsRegistered
  :191-211` writes that onto the connection's `AdditionalFieldDefinitions`;
  `GetCustomFieldReferences :266` looks it up with `["name","id","key"]`; no field is named
  `customfield_10001`; if no field carries that **id** either, the walk reaches `"key"` at `:1458`
  and throws. **Validates.** Note the near-miss variant: if some *unrelated* field on that instance
  happens to hold id `customfield_10001`, there is no throw and the wrong field is read into the
  flag, silently — a second, quieter defect the same fallback produces.
- **A and E do not contradict.** A is about a status code the field endpoint returned; E is about
  the shape of a 200 it returned. They are reached through the same method and are told apart by
  whether `technicalDetails` is populated.
- **Completeness check.** Are we missing a contributing factor? Two, both recorded in §5 rather
  than as chains: the changelog `EnsureSuccessStatusCode()` at `:1274` and the tenant-info one at
  `:2041`. Neither is on an additional-fields path, and neither is reachable from the connection or
  team/portfolio settings screens for a Data Center customer.
- **Do the five root causes explain all four observed strings?** S1 ← A. S2 ← C (and D-adjacent
  `JsonException`). S3, S4 ← B. E produces **no** string — it is an unhandled throw on a background
  refresh, which is why it was not in the customer's report and would not have been found from the
  report alone. **All four strings explained, plus one silent failure the strings do not cover.**

---

## 5. Sweep: every remaining `EnsureSuccessStatusCode()` and silent degradation

### 5a. `EnsureSuccessStatusCode()` — all three, all in Jira

| Site | Method | Endpoint | Caller chain | UI action that reaches it | What the user sees today | Verdict |
|---|---|---|---|---|---|---|
| `:1427` | `GetCustomFieldMappings` (`:1421`) | `GET rest/api/latest/field` | (1) `SetStoredFieldKeys :1412` ← `GetIssuesByQuery :1589` ← `ValidateTeamSettings :1055` / `ValidatePortfolioSettings :1092` / every sync read; (2) `GetCustomFieldReferences :1575` ← `GetMissingAdditionalFields :1040` ← `ValidateConnection :395`, **and** ← `CreateWorkItemsFromIssues :266` ← `GetWorkItemsForTeam :255`, **and** ← `CreateFeaturesFromIssues :1139` ← `GetFeaturesForProject :290` / `:321` / `:347`; (3) `UpdateItems :487` ← `WriteFieldsToWorkItems :478` | **Connection → Validate**; **Team settings → Validate**; **Portfolio settings → Validate**; team/portfolio create wizards; **every background refresh**; write-back flush | S1 / S2 / S3 / S4 / **S5** (§1) | **MUST FIX NOW** |
| `:1274` | `GetAllChangelogEntriesForIssue` (`:1259`) | `GET rest/api/latest/issue/{id}/changelog` | ← `CreateIssueWithCompleteChangelog :1756` ← `GetIssuesByQueryFromCloud :1684` (Cloud only — the DC walk at `:1620` reads the inline changelog) | Only reached when an issue has >30 changelog entries (`ShouldFetchFullChangelog :1607`). From **Team settings → Validate** it is reachable in principle (`maxResultsOverride = 10`, `:1055`), but only on **Cloud** | "Team validation failed due to an unexpected error." + the .NET status sentence, i.e. S3 | **Same shape, not on this path.** Cloud-only; not additional-fields. Fix opportunistically, not in this change. |
| `:2041` | `ResolveCloudIdAsync` (`:2029`) | `GET {jiraUrl}/_edge/tenant_info` | ← `ResolveBaseUrlAndCacheKeyAsync :2019`, guarded by `RoutesViaAtlassianCloudGateway(connection.AuthenticationMethodKey)` (`:2016`) | Every Jira call, but **only** for `JiraScopedToken` / `JiraOAuth` auth methods — i.e. Jira **Cloud** | S1 (`:407` catches it; the URL blame is arguably *correct* here, since the URL really is what `_edge/tenant_info` is derived from) | **Same shape, not on this path.** Structurally excluded for a Data Center customer. |

**No other connector contains `EnsureSuccessStatusCode()` at all** — Azure DevOps 0, Linear 0,
ServiceNow 0, CSV 0. (Azure DevOps and Linear route through SDK clients that raise their own
`VssServiceException` / GraphQL exceptions, so "no `EnsureSuccessStatusCode`" is not "no raw
exception".)

### 5b. Silent `return default` on a path an admin can trigger from a settings screen

| Site | Method | Degrades to | Reachable from | Verdict |
|---|---|---|---|---|
| `:969-979` | `GetBoardsFromJira` | `[]` — an empty board list, presented as "this connection has no boards" | **Team/Portfolio create wizard → board picker** (`WizardsController`) | **Same shape, not this bug.** It does log status, reason, URI and body at `:972-978`, so it is diagnosable from the log. Candidate for a `JiraReadException` in a follow-up, since `WizardsController.cs:55` already unwraps one. |
| `:2125-2130` | `GetDeploymentType` | `JiraDeployment.Unknown`, cached | Every read; decides Cloud vs DC search | **Not this bug.** Unknown falls through to the DC walk (`:1596`), which is the correct default for a DC customer. Logged at `LogDebug` only (`:2127`) — invisible at default level. Recorded. |
| `:2286-2292`, `:2321-2327` | `ReadVisibleProjects`, `ReadReleasesOf` | partial list + `SawEverything: false` | Delivery/Release source picker | **Not this bug.** This is a deliberate, documented partial-read design (`:2275`, `:2305-2308`) that carries a completeness flag. Correct as written. |
| `:657-663`, `:713-719`, `:765-771` | `GetBoardInformationFromJira`, `MapStatusToCategory`, `GetItemTypesForBoard` | empty board / no states / no types | Board wizard | **Already addressed by #5973** — all three now log status and reason before degrading, and the filter read (`:851-857`) was promoted to a thrown `JiraReadException`. No further action. |
| `:1494-1504` | `GetIdForCustomFieldByProperty` | `string.Empty` for "no match" | the additional-fields path | **This is the intended behaviour** — `:1042-1045` turns it into the good verdict at `:398`. Chain C is that it never gets the chance on Data Center. |
| `:1415-1418` | `SetStoredFieldKeys` | `string.Empty` cached for Rank / Flagged / Epic Link / Parent Link, indistinguishable from "not looked up yet" | every read | **Contributing factor to Chain E.** An empty Flagged key is what makes `ResolveFlaggedFieldReference :176-183` fall back to the hard-coded Cloud id at `:37`. Not fixed here (§7, "Deliberately NOT"). |

---

## 6. Blast radius: the other connectors

Measured, not fixed here.

| Connector | Validates additional fields in `ValidateConnection`? | Same misattribution to the URL? | Evidence |
|---|---|---|---|
| **Azure DevOps** | **Yes** — `AzureDevOpsWorkTrackingConnector.cs:219` `GetMissingAdditionalFields(witClient, connection.AdditionalFieldDefinitions)`, with the same `"additional_fields_invalid"` / `"Some additional fields could not be found: {…}"` / `fieldName = "Additional Fields"` verdict at `:222-226` | **YES — identical shape.** The call at `:219` sits inside the `try` opened at `:213`, whose `catch (HttpRequestException)` at `:250-258` returns `"connection_failed"` / **"Could not reach Azure DevOps with the provided URL."** / `fieldName = AzureDevOpsWorkTrackingOptionNames.Url`. A `GetWorkItemFieldsAsync` failure (`:684`) is blamed on the ADO URL, or lands in `catch (VssServiceException)` `:241-248`, or the bare catch-all at `:270-274` | `:213`, `:219`, `:222-226`, `:241-248`, `:250-258`, `:270-274`, `:684` |
| **Azure DevOps (sync)** | n/a | An unresolvable additional field is **silently dropped** during refresh — `:690-692` `SingleOrDefault(…)?.ReferenceName ?? string.Empty`, reached from `ThePayloadsAndFieldsFor :743`. No warning, no result | `:690-692`, `:735-747` |
| **Linear** | **No** — no additional-field validation exists; `GetPredefinedAdditionalFields` returns `[]` (`LinearWorkTrackingConnector.cs:45`) | N/A — every failure collapses to `"validation_failed"` / `"Could not validate the Linear connection with the provided settings."` at `:249-252` with **no fieldName**, so nothing is attributed anywhere | `:45`, `:230-254` |
| **ServiceNow** | **No** — `GetPredefinedAdditionalFields` returns `[]` (`ServiceNowWorkTrackingConnector.cs:216-219`) | **No, and deliberately not.** All connection-scope verdicts omit `fieldName` (`ServiceNowValidationVerdict.cs:19,33,66,74,85,96,110,121`), and team-scope verdicts are re-pointed at the field the user typed (`ServiceNowTeamQueryVerdict.cs:206-213`, with a comment stating exactly this anti-misattribution rule). Reads throw a verdict-carrying `ServiceNowReadException`, not a raw transport exception | as cited |
| **CSV** | **No** — `GetPredefinedAdditionalFields` returns `[]` (`CsvWorkTrackingConnector.cs:33`); `ValidateConnection` (`:125-137`) does no IO | No — its one failure (`:131-133`) carries no `fieldName` | `:33`, `:125-137` |

**Conclusion on blast radius.** The defect family is **Jira + Azure DevOps**. Linear, ServiceNow
and CSV do not validate additional fields at all, so they cannot misreport a failure to do so —
which is its own gap, but a different one. **ServiceNow is the reference implementation**: it is the
only connector whose remote refusals become a typed, verdict-carrying exception, and the only one
whose verdicts are deliberately pointed at the field the user typed. The Jira fix below moves Jira
toward that shape; Azure DevOps should follow in a separate change.

Also recorded, out of scope: `LinearWorkTrackingConnector.cs:662-667` discards the GraphQL `errors`
array (logged at `LogDebug` only, `:682-685`), so a Linear response carrying GraphQL errors under
HTTP 200 returns `ConnectionValidationResult.Success()` at `:244`. Different defect, same family.

---

## 7. Proposed fix (minimal, regression-test-first, in the existing idiom)

### Which existing type, and why

| Candidate | Fits? |
|---|---|
| `JiraQueryRejectedException` | **No.** It extends `HttpRequestException` *on purpose* — its own XML doc (`JiraReadException.cs:8-11`) records that the sync path must read it as "Jira could not be asked". Reusing it here would leave it caught by `:407` and the URL blame would survive. It also carries a `RejectedQuery`, and there is no query. |
| `WorkTrackingReadException` | **Yes, as the base.** It is abstract (`WorkTrackingReadException.cs:11-15`) and carries a `ConnectionValidationResult Verdict` — the exact channel the UI already renders. `WizardsController.cs:55` already unwraps it. |
| **`JiraReadException`** | **Yes — this is the one.** It is the Jira subclass (`JiraReadException.cs:13`), already used for "a read Jira would not answer, carrying the verdict the administrator is shown" (`:6-7`). Add one factory. Critically, it is **not** an `HttpRequestException`, so it structurally cannot be swallowed by `:407` — the fix is a type change, not a reordered catch. |
| `ConnectionValidationResult` | **Yes, as the payload** — `Failure(code, message, technicalDetails, fieldName)` (`ConnectionValidationResult.cs:32`). No new type needed anywhere. |

**No new exception type, no new result type.** One factory on an existing exception, one new
`private const`, four call-site edits.

### Fix dependency order — F1+F2 must NOT ship without F4 and F5

F2 changes what `GetCustomFieldMappings` throws. Until F4 and F5 add the matching catches, the new
`JiraReadException` falls through to `ValidateConnection`'s bare `catch` at `:427` and to the
`catch (Exception)` at `:1075`/`:1112` — which means **shipping F1+F2 alone makes the message
strictly worse**: the URL blame at `:411-415` is replaced by `"Connection validation failed due to
an unexpected error."` with no details at all, because `JiraReadException` is not an
`HttpRequestException` and `:407` no longer matches it.

```
F1 (define the verdict)  ->  F2 (throw it)  ->  F4 + F5 (catch it)     one commit, not three
F3 (TryGetProperty)                                                     independent; ship first if splitting
```

**F3 is the only piece that stands alone**, and it is the one that fixes the customer's most likely
symptom (Chain C) and the silent refresh failure (Chain E). If this work has to be split, split it
as *F3 first, F1+F2+F4+F5 together* — never F1+F2 on their own.

---

### F1 — `JiraReadException`: a factory for a field list Jira would not hand over

`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraReadException.cs`,
added beside the two existing factories (after `:32`, before the private `Unreadable` at `:35`):

```csharp
/// <summary>
/// Jira would not hand over the list of fields on the instance, so no additional field can be
/// checked against it. This is not a broken URL: the same connection had just answered on
/// rest/api/2/myself, so the address and the credential are both good enough to be talked to.
/// </summary>
public static JiraReadException FieldListRefused(HttpStatusCode status, string whatJiraSaid)
    => new(ConnectionValidationResult.Failure(
        "field_list_unreadable",
        $"Lighthouse could not read the list of fields on this Jira instance, so it cannot check "
        + $"the additional fields against it. Jira answered {(int)status} ({status}). The account "
        + "this connection signs in with needs to be able to browse at least one project for Jira "
        + "to return the field list; a proxy in front of Jira can also refuse this response, which "
        + "is much larger than the others Lighthouse asks for.",
        $"GET rest/api/latest/field answered {(int)status} {status}. {whatJiraSaid}",
        AdditionalFieldsFieldName));
```

with, at the top of the class:

```csharp
private const string AdditionalFieldsFieldName = "Additional Fields";
```

> **S1192 note.** `"Additional Fields"` currently appears once in the Jira connector (`:402`) and
> once in the ADO connector (`:226`). Adding a second Jira occurrence stays under the threshold —
> but `docs/ci-learnings.md:156` records that `JiraWorkTrackingConnector` has already tripped S1192
> once, and that "adding a single line to an existing file is enough to trip it". Hoist the const
> now and reference it from `:402` too.

### F2 — `GetCustomFieldMappings`: read the field list, or say what Jira answered

`:1421-1431`, replacing `EnsureSuccessStatusCode()`. The method must stop being `static` so it can
log — the other `JiraReadException` throw sites log first (`:853-857`, `:865-869`) and this one
should match.

```csharp
private async Task<Dictionary<string, string>> GetCustomFieldMappings(HttpClient jiraClient, string[] propertyIdentifiers,
    IEnumerable<string> customFields)
{
    const string url = "rest/api/latest/field";

    var response = await jiraClient.GetAsync(url);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        logger.LogWarning(
            "Jira would not return the field list: {StatusCode} ({ReasonPhrase}). Jira answered: {ResponseBody}",
            (int)response.StatusCode, response.ReasonPhrase, responseBody);

        throw JiraReadException.FieldListRefused(response.StatusCode, ExplanationIn(new JiraSearchRejection(response.StatusCode, responseBody)));
    }

    var jsonResponse = JsonDocument.Parse(responseBody);
    // … unchanged from :1432
```

Notes:
- `ExplanationIn` (`:1721-1728`) and `ErrorMessagesIn` (`:1730-1754`) are reused verbatim: they
  already return Jira's own `errorMessages` sentences when the body is JSON, and fall back to
  `"Jira answered NNN Status."` when it is not — which is exactly the HTML-proxy-page case. If the
  `JiraSearchRejection` record name reads wrong for a non-search call, rename it to
  `JiraRefusal` in the same change; it is a `private sealed record` (`:1695`) with three call sites.
- Reading the body **before** the status check (rather than only in the failure branch) keeps one
  read for both paths and removes the `ReadAsStringAsync` at `:1429`.
- Dropping `static` requires one ripple, and exactly one: of the three callers, `UpdateItems`
  (`:483`) and `GetCustomFieldReferences` (`:1570`) are already instance methods, but
  **`SetStoredFieldKeys` (`:1389`) is `static`** and must drop `static` too. It has a single call
  site — `GetIssuesByQuery :1589`, itself an instance method — so the ripple stops there.
- CA1822 ("mark members as static") does not fire on either: `GetCustomFieldMappings` now uses
  `logger`, and `SetStoredFieldKeys` now calls an instance member at `:1412`. Verify this in the
  pre-push `dotnet format analyzers … --severity info` sweep rather than assuming it.

### F3 — `GetIdForCustomFieldByProperty`: a property that is not there is a miss, not a crash

`:1456-1461` — this is the Data Center fix (Chain C). One expression:

```csharp
var elements = allFields.RootElement.EnumerateArray()
    .Where(f => f.TryGetProperty(propertyIdentifier, out var propertyValue)
                && string.Equals(propertyValue.GetString(), customField, StringComparison.OrdinalIgnoreCase))
    .ToList();
```

This restores `"Some additional fields could not be found: X"` (`:398`) for the entire Jira Data
Center population, **and stops the unhandled throw on every Data Center team/portfolio refresh**
(Chain E). It matches the file's own convention everywhere else (`:1478`, `:1499`, `:1603`,
`:1736`, `:2540`).

**F3 closes Chains C and E and is the mandatory part of this change.** F1/F2/F4/F5 close Chains A,
B and D — they improve what the user is told; F3 is what stops Data Center instances failing.

### F4 — `ValidateConnection` must stop blaming the URL

`:373-432`, adding one catch **before** `catch (HttpRequestException ex)` at `:407`:

```csharp
catch (WorkTrackingReadException refusal)
{
    return refusal.Verdict;
}
```

Catch the **abstract base**, mirroring `WizardsController.cs:55` and `:459`. Ordering is not
required by the compiler — `WorkTrackingReadException` derives from `Exception`, not from
`HttpRequestException` — but put it first so the reading order matches the specificity order.

After F2, the only `HttpRequestException` the `/field` call can still raise is a genuine transport
failure (DNS, refused connection, TLS). For that, `"Could not reach Jira with the provided URL."`
at `:411-415` is **correct and should stay**. That is the argument that F2 + F4 together are
sufficient: they do not weaken the URL verdict, they stop it being used for something else.

### F5 — team and portfolio validation must carry the verdict too

`:1071` and `:1108`, adding the same catch before the existing `catch (JiraQueryRejectedException)`:

```csharp
catch (WorkTrackingReadException refusal)
{
    return refusal.Verdict;
}
```

This closes Chain B: the field-metadata failure that happens *before* the search now produces
`"Lighthouse could not read the list of fields on this Jira instance…"` pinned to
`"Additional Fields"`, instead of `"Team validation failed due to an unexpected error."`.

> **S2139 note** (`docs/ci-learnings.md:157`): do not log inside these new catches. The throw site
> (F2) already logs, and a log-and-return here would be the duplicate S2139 exists to catch.

### F6 — write-back: a consequence of F2, not a separate change

**No implementation.** Listed with a number only so Chain D has something to map to: `UpdateItems :487` will now throw a `JiraReadException` whose
message is the verdict's message (`WorkTrackingReadException.cs:12` passes `verdict.Message` to
`Exception`), which is strictly more informative than the .NET status sentence it replaces.

Verified safe: nothing on the write-back path filters on exception type —
`WriteFieldsToWorkItems :470-481`, `WriteBackService.cs:145-163` and `WriteBackCollector.cs:51-54`
have no catch at all. `JiraCouldNotBeAsked` (`:2445-2446`), the one type filter in the connector, is
used only at `:2431` and `:2530` on the Releases path, which never calls `GetCustomFieldMappings`.

**Deliberately not in this fix:** giving `WriteBackResult` a connection-level outcome so a flush
that could not start is distinguishable from a flush that wrote nothing (Chain D, WHY 4D). That is
a contract change across five connectors and belongs in its own item.

### Deliberately NOT in this fix

- **`:1274` (changelog) and `:2041` (tenant_info).** Same shape, both Cloud-only, neither on the
  additional-fields path (§5a). Raise a follow-up; do not enlarge this change.
- **Azure DevOps `:219`/`:250-258`.** Identical defect, separate connector, separate live test
  category. Measured in §6, fixed separately.
- **A deployment switch on `GetCustomFieldMappings`.** F3 makes the Cloud and Data Center shapes
  both work through one parser, which is cheaper and has no branch to get wrong.
- **Replacing the hard-coded `DefaultFlaggedFieldReference = "customfield_10001"` (`:37`).** It is a
  Jira **Cloud** field id used as the fallback on every deployment (Chain E, WHY 2E). After F3 it no
  longer throws — it resolves to "no flag", which is correct for an instance that has no Flagged
  field. But on a Data Center instance where some *unrelated* field happens to hold that id, it
  silently reads the wrong field into the flag. That is a second defect with its own reproduction,
  its own test and its own migration question for connections that already stored it. **Raise it as
  a separate item; do not fold it in here.**

---

## 8. Files affected

| File | Change |
|---|---|
| `…/WorkTrackingConnectors/Jira/JiraReadException.cs` | F1 — one factory + one `private const` |
| `…/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` | F2 (`1421-1431`, drop `static`; and drop `static` from `SetStoredFieldKeys` `1389`), F3 (`1456-1461`), F4 (`before 407`), F5 (`before 1071`, `before 1108`), and `402` to use the new const |
| `…/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraFieldLookupTest.cs` | **new** — offline fixture, no `Category` attribute (§9) |
| `…/Lighthouse.Backend.Tests/TestHelpers/JiraConnectorTestSetup.cs` | possibly a `AConnectionToJiraDataCenter` sibling; see §9 |

---

## 9. Regression tests to write first (offline, no live Jira)

Build on `Lighthouse.Backend.Tests/TestHelpers/JiraConnectorTestSetup.cs` —
`AConnectorOver(handler, logger)` injects a stub `HttpMessageHandler` through the connector's
`httpMessageHandlerForTesting` constructor parameter, and `ATeamOnJiraCloud()` issues a connection
with a unique id and url so the process-wide `FieldNames` (`:1396`) and `DeploymentCache` caches
stay disjoint between tests. New fixture `JiraFieldLookupTest`, plain `[TestFixture]`, **no**
`Category` attribute so it runs in the default filter. Copy the `AHandlerAnswering` /
`Respond` / `AbsolutePath`-switch pattern from `JiraBoardQueryAssemblyTest.cs:747-760`.

Note: the connection's deployment is decided entirely by what the stub answers to
`rest/api/2/serverInfo` (`:2124`), since `AuthenticationMethodKeys.JiraCloud` is a **basic-auth**
method and does not route via the Atlassian gateway (`RoutesViaAtlassianCloudGateway`, `:2016`).
Answer `{"deploymentType":"Server"}` for a Data Center instance — `JiraBoardQueryAssemblyTest.cs:37`
records this and names the constant `OnDataCenter = "Server"`.

1. **`ValidateConnection_FieldListRefused_NamesTheFieldListAndNotTheUrl`** — stub answers 2xx on
   `rest/api/2/myself` and **403** on `rest/api/latest/field`. Assert `Code == "field_list_unreadable"`,
   `FieldName == "Additional Fields"`, and `Message` does **not** contain `"provided URL"`.
   **This is Chain A's regression test.**
2. **`ValidateConnection_FieldListRefused_CarriesJirasOwnSentence`** — same, with a body of
   `{"errorMessages":["You do not have permission to view fields."]}`. Assert `TechnicalDetails`
   contains that sentence. Chain A / the discarded body.
3. **`ValidateConnection_OnDataCenter_UnmatchedField_SaysWhichFieldIsMissing`** — stub answers a
   **Data Center-shaped** field array (objects with `id`/`name`/`custom`/`schema` and **no `key`**)
   and the connection carries an `AdditionalFieldDefinition` with `Reference = "MamboJambo"`.
   Assert `Code == "additional_fields_invalid"` and `Message` contains `"MamboJambo"`.
   **This is Chain C's regression test, and the customer's exact scenario.** It fails today with
   `Code == "validation_failed"`.
4. **`ValidateConnection_OnCloud_UnmatchedField_StillSaysWhichFieldIsMissing`** — same with a
   Cloud-shaped array (objects that *do* carry `key`). Pins that F3 did not change Cloud, which is
   what the live test at `JiraWorkTrackingConnectorTest.cs:666` covers and cannot be run here.
5. **`ValidateTeamSettings_FieldListRefused_DoesNotSayUnexpectedError`** — stub answers 2xx on
   `serverInfo`, **502** on `rest/api/latest/field`, and would answer a page of issues on `/search`.
   Assert the search was **never requested** (capture the URLs, as
   `JiraBoardQueryAssemblyTest.cs:661-670` does) and that `Code == "field_list_unreadable"`.
   **This is Chain B's regression test.** It fails today with
   `"Team validation failed due to an unexpected error."`.
6. **`ValidatePortfolioSettings_FieldListRefused_DoesNotSayUnexpectedError`** — the `:1108` twin.
7. **`ValidateConnection_MyselfRefused_StillNamesTheCredential`** — stub answers **401** on
   `rest/api/2/myself`. Assert `Code == "authentication_failed"`. Guards that F4's new catch did not
   shadow `:377-392`.
8. **`ValidateConnection_JiraUnreachable_StillNamesTheUrl`** — stub `HttpMessageHandler` throws
   `HttpRequestException`. Assert `Code == "connection_failed"` and `FieldName == Url`. Guards that
   F4 did not weaken the genuine reachability verdict.
9. **`GetWorkItemsForTeam_OnDataCenterWithoutAFlaggedField_ReadsTheTeamAnyway`** — stub answers
   `{"deploymentType":"Server"}` on `serverInfo`, a **Data Center-shaped** field array containing
   no field named `"Flagged"` and no field with id `"customfield_10001"`, and one page of issues on
   `/search`. The connection carries **no** user-configured additional fields. Assert the call
   returns the work items rather than throwing. **This is Chain E's regression test**, and it fails
   today with `KeyNotFoundException`. Portfolio twin over `GetFeaturesForProject` (`:284`) optional.

`JiraWorkTrackingConnectorTest.cs` is `[Category("Integration")]` + `[Category("JiraIntegration")]`
(`:13-14`) and was **read for evidence only**. No live-connector category was executed at any point
in this investigation.

---

## 10. Risk assessment

| Risk | Likelihood | Impact | Mitigation / evidence |
|---|---|---|---|
| **An existing test asserts the current (bad) wording** | **None found** | — | Repo-wide grep over `Lighthouse.Backend.Tests`: **zero** hits for `"Could not reach Jira with the provided URL"`, `"additional_fields_invalid"`, `"Some additional fields could not be found"`, or `GetCustomFieldMappings`. The 11 `"connection_failed"` assertions are all ServiceNow. The only Jira additional-field test is `JiraWorkTrackingConnectorTest.cs:666`, which asserts `IsValid` only (`:699`), not the code or message. |
| **A live `JiraIntegration` test pins the current swallow** | **Low, but check it** | High if hit | `docs/ci-learnings.md:419-423` records this exact failure mode for a `catch` removal in the ADO connector, where the only thing pinning the behaviour was a live test the default filter excludes, and a Stryker `NoCoverage` reading that was therefore meaningless. `JiraWorkTrackingConnectorTest.cs:666` asserts `IsValid == expectedValidationResult` for eleven cases against **Cloud**; F3 changes the Cloud path from `GetProperty` to `TryGetProperty`, which is a no-op there because every Cloud field carries `key`. **Rule: run `dotnet test --filter "TestCategory=JiraIntegration"` once before merge** — and be aware the Linear key is shared with CI, so do not run it in a loop. |
| **F3 changes which field a reference resolves to** | Very low | Medium | Only where a field object *lacks* the identifier being read. On Cloud that set is empty (proved by the `"MamboJambo"` case returning a miss rather than throwing). On Data Center it is the whole array for `key`, where the current behaviour is an exception, not a different answer. |
| **F4's new catch shadows an existing verdict** | Very low | Medium | `WorkTrackingReadException` is thrown from exactly two other sites (`:857`, `:869`), both inside board reads, and `ValidateConnection` does not read boards. Test 7 and test 8 pin the two verdicts that share the `try`. |
| **F2's throw escapes somewhere that used to catch `HttpRequestException`** | Very low | Medium | Repo-wide: `catch (HttpRequestException` appears at ADO `:250`/`:808`, Jira `:407`, ServiceNow `:243`/`:800`. Only Jira `:407` is on any `GetCustomFieldMappings` path, and F4 handles it. The sync read path (`CreateFeaturesFromIssues :1139`) and the write-back path have no type filter (§7 F6). |
| **`JiraReadException` message text leaks Jira internals to a user** | Medium | Low | `errorMessages` is the text Jira shows its own users; it lands in `TechnicalDetails`, which the UI already treats as diagnostic rather than the headline (`ModifyConnectionSettings.tsx:449-450`). Same precedent as #5973's `QueryWasRejected` (`:1128-1133`). |
| **Frontend change needed** | **None** | — | The 400 path already carries `message`, `technicalDetails`, `fieldName` and `code` end to end (§2 row 7). Leave `ModifyConnectionSettings.tsx:438-443` alone (§2 row 8). |
| **The Chain-E fallback constant is pinned by a test** | **Certain, and unaffected** | — | `JiraWorkTrackingConnectorTest.cs:37` asserts `flagged.Reference == "customfield_10001"` verbatim. Note this test needs no network even though its class carries the live categories (`:13-14`), so it *would* run if the category filter were dropped. None of F1–F6 touches `:37`, so it stays green — and its existence is a further reason the constant's replacement belongs in its own item (§7, "Deliberately NOT"). |
| **F4's new catch changes an existing offline test** | **No** | — | `JiraWorkTrackingConnectorAuthDelegationTest.cs:17` calls `ValidateConnection` and is uncategorised, so it runs in the default filter. It points the connection at `http://127.0.0.1:1/` (`:28`), where the socket is refused and the `HttpClient` raises `HttpRequestException` — still matched by `:407`, not by F4's new catch. It also asserts nothing about the verdict (`:35-39` verify the auth mocks only). Unaffected. |
| **Shipping F1+F2 without F4/F5** | Only if the change is split | **High** | It would make the message *worse*, not better — see "Fix dependency order" in §7. The mitigation is procedural: one commit. |
| **The Chain-E path is covered only by live Cloud tests** | Certain | Medium | `GetWorkItemsForTeam_ItemIsFlagged_*` (`JiraWorkTrackingConnectorTest.cs:182`, `:204`, `:223`) exercise the predefined-flag path against **Cloud**, where `key` is present and Chain E cannot fire. Test 9 in §9 is what puts the Data Center shape under an offline assertion for the first time. |

### Gates the change must respect

Local build (`TreatWarningsAsErrors` — any warning is a failure):

- **S6667** (*error* severity here) — a `catch` that logs must pass the exception as the **first**
  logger argument. F2's `LogWarning` is *not* in a catch, so this does not apply — but F4/F5's new
  catches **must not log at all** (see S2139 below), which sidesteps it.
- **S4136** (*error* severity) — all overloads of a method must be adjacent. F1 adds a third static
  factory to `JiraReadException`; place `FieldListRefused` **immediately beside**
  `BoardFilterRefused` (`:25`) and `BoardFilterCarriedNoQuery` (`:32`), not at the end of the class.
  These are not overloads of each other, so the rule is not strictly triggered — but the private
  `Unreadable` helper (`:35`) is the shape S4136 punishes if a sibling lands after it.
- **CA1822** — F2 drops `static` from `GetCustomFieldMappings` (body now uses `logger`) and from
  `SetStoredFieldKeys` (body now calls an instance member); the rule stays silent on both. Do
  **not** drop `static` from `GetIdForCustomFieldByProperty` (F3) — it uses no instance state and
  CA1822 would fire.

Sonar Cloud `new_violations = 0` (silent in the local build — `docs/ci-learnings.md:149-232`):

- **S1192** — the same literal three or more times in one file. `docs/ci-learnings.md:156` records
  a prior S1192 recurrence **in `JiraWorkTrackingConnector`** specifically. F1's `private const
  AdditionalFieldsFieldName` is the pre-applied fix; also update `:402` to use it.
- **S2139** — do not log-and-rethrow, and do not log in a catch whose caller already logged.
  F4/F5's catches must be bare `return refusal.Verdict;`.
- **CA1859** — any **new** non-public method, parameter or local must be typed as the concrete type
  it holds. F2 keeps the existing `Dictionary<string, string>` return; no new signature is
  introduced. Test helpers in §9 that end in `.ToList()` must declare `List<T>`, never
  `IReadOnlyList<T>` (7 recorded recurrences).
- **CA1861** — never pass an inline `new[] { … }` to an NUnit assertion; hoist to
  `private static readonly`. Relevant when writing tests 3 and 4, which assert on field-name lists.
- **NUnit2045** — two or more adjacent `Assert.That` must be wrapped in `Assert.Multiple` /
  `Assert.EnterMultipleScope`. Every test in §9 asserts on at least two properties of the verdict.
- **NUnit4002** — assert a default with `Is.Default`, never `Is.EqualTo(default(T))`.
- **S3776** — keep `GetCustomFieldMappings` under cognitive complexity 15. F2 adds one `if`; the
  method currently has two nested loops and one `if`. Measure, do not assume.
- **Run `dotnet format analyzers Lighthouse.sln --severity info` before pushing** —
  `docs/ci-learnings.md:7` marks the pre-push sweep as non-optional, and it is the only thing that
  surfaces the INFO-severity rules above before CI does.

---

## Sources

- [dc-field] [Jira Data Center 9.12 REST API reference — `GET /rest/api/2/field` (`field-getFields`)](https://docs.atlassian.com/software/jira/docs/api/REST/9.12.0/#api/2/field-getFields) — documented responses: **200** "Contains a full representation of all visible fields in JSON."; **401** "Returned if user is not logged-in and don't have access to any project". Example field object: `id`, `name`, `custom`, `orderable`, `navigable`, `searchable`, `clauseNames`, `schema` — **no `key`**.
- [ms-getproperty] [`JsonElement.GetProperty(String)` — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement.getproperty?view=net-9.0) — "**KeyNotFoundException** — No property was found with the requested name."
- [Jira Cloud platform REST API — Issue fields (`GET /rest/api/2/field`)](https://developer.atlassian.com/cloud/jira/platform/rest/v2/api-group-issue-fields/) — the Cloud response carries `key` alongside `id`; permission required is "Permission to access Jira".
- [seraph] [REST API calls returning 401 Unauthorized — `X-Seraph-LoginReason` / CAPTCHA (Atlassian)](https://jira.atlassian.com/browse/ID-6351) — `AUTHENTICATION_DENIED` / `AUTHENTICATED_FAILED` mean the login was rejected without checking the password, most commonly because Jira's CAPTCHA feature triggered.
- [proxy] [Proxying Atlassian server applications with Apache HTTP Server (mod_proxy_http) — Atlassian](https://confluence.atlassian.com/kb/proxying-atlassian-server-applications-with-apache-http-server-mod_proxy_http-806032611.html) and [Configure Jira to run behind an NGINX reverse proxy — Atlassian](https://support.atlassian.com/jira/kb/configure-jira-to-run-behind-a-nginx-reverse-proxy/) — buffer and timeout tuning required in front of Jira; the basis for the large-`/field`-payload 502/504 candidate.
- [How to handle HTTP 400 Bad Request errors on Jira search REST API endpoint — Atlassian](https://support.atlassian.com/jira/kb/how-to-handle-http-400-bad-request-errors-on-jira-search-rest-api-endpoint/) — cited by the predecessor RCA for the **search** 400; listed here to mark that it does **not** apply to `/field`.

**Constraint honoured throughout:** no live-connector test category (`Integration`,
`JiraIntegration`, `LinearIntegration`, `AdoIntegration`, `ServiceNowIntegration`) was executed, and
no live Jira instance was contacted. `JiraWorkTrackingConnectorTest.cs` and the other categorised
fixtures were read for evidence only.
