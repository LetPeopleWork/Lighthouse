# RCA — Bug #5973 "Team Creation gives Error" (Jira Data Center, board-selected teams)

Method: Toyota 5 Whys, multi-causal, evidence required at every level.
Date: 2026-09-11 · Investigator: Rex (nw-troubleshooter)
Repo state: `main` @ 13b0c0876

---

## 1. Problem statement (scoped)

Two users, both on **Jira Data Center**, both having **selected a board** in the team
configuration wizard rather than hand-writing JQL, see the team settings form refuse
validation with:

```
Team validation failed due to an unexpected error.
Response status code does not indicate success: 400 (Bad Request).
```

Scope: the `POST /api/latest/teams/validate` path for Jira connections whose
`DataRetrievalValue` was produced by `BoardWizard`. Out of scope: portfolio validation
(same defect class, same code, noted but not the reported symptom), other connectors.

Reporter's stated assumption — "Jira query too complicated/long" — is treated here as one
hypothesis among four and is addressed explicitly in Chain C.

---

## 2. Which 400 is this? (investigation line 1 — settled)

| Candidate | Body shape | Verdict |
|---|---|---|
| (a) `BadRequest(validationResult)` from the controller with a *business* failure (`no_work_items_found`) | `{isValid:false, code:"no_work_items_found", …}` | **Not this.** The screenshot's `code` path is `validation_failed`. |
| (b) ASP.NET model-binding / ModelState 400 | RFC 7807 `ProblemDetails`, `errors:{…}` | **Eliminated.** Never reaches the connector, and would not produce the connector's wording. |
| (c) A 400 **relayed from Jira** through an outbound HttpClient call inside the connector | `{isValid:false, code:"validation_failed", message:"Team validation failed due to an unexpected error.", technicalDetails:"Response status code does not indicate success: 400 (Bad Request)."}` | **This one.** |

Evidence chain, outer to inner:

- `TeamsController.cs:154-157` — `if (!validationResult.IsValid) return BadRequest(validationResult);`
  This is the HTTP 400 the browser sees. It is a *wrapper*, not the cause.
- `JiraWorkTrackingConnector.cs:1002-1009` — the `catch (Exception exception)` that produces
  `ConnectionValidationResult.Failure("validation_failed", "Team validation failed due to an
  unexpected error.", exception.Message)`. `exception.Message` is the second line of the
  screenshot verbatim.
- `"Response status code does not indicate success: 400 (Bad Request)."` is the exact message
  .NET's `HttpResponseMessage.EnsureSuccessStatusCode()` throws. Every `EnsureSuccessStatusCode()`
  in the Jira connector: lines **1175, 1328, 1533, 1842**.
  - 1175 `GetAllChangelogEntriesForIssue` — Cloud-only path, not reached on DC.
  - 1842 `ResolveCloudIdAsync` — only for `JiraScopedToken`/`JiraOAuth`
    (`RoutesViaAtlassianCloudGateway`, line 1788-1790). Not DC.
  - 1328 `GetCustomFieldMappings` → `GET rest/api/latest/field`. Reachable (via
    `SetStoredFieldKeys`, line 1490→1313), but `/field` takes no query parameters and does not
    answer 400; it answers 401/403 on auth. Possible but unmotivated.
  - **1533 `GetIssuesByQueryFromDataCenter`** → `GET rest/api/latest/search?jql=…` — the only call
    on the DC validation path that takes a *user-derived* payload, and the endpoint Atlassian
    documents as answering 400. **This is the throw site.**

Path: `TeamsController.ValidateTeamSettings` (138) → `JiraWorkTrackingConnector.ValidateTeamSettings`
(979) → `PrepareQuery` (985/1735) → `GetIssuesByQuery` (986/1483) → `GetDeploymentType` says
DataCenter (1488/1911) → `GetIssuesByQueryFromDataCenter` (1497/1521) → **`EnsureSuccessStatusCode()`
(1533)**.

**How the frontend distinguishes these three: it cannot.** `BaseApiService.parseApiErrorPayload`
reads `message` / `technicalDetails` / `code` off any JSON body it is handed
(`BaseApiService.ts`, `createApiErrorFromAxios`), and `useCreateWizard.runValidation` renders
`error.message` + `error.technicalDetails` with no branch on `code`
(`useCreateWizard.ts`, `runValidation` / `handleWizardComplete`). A relayed Jira 400,
a business "no work items", and an ASP.NET ProblemDetails all land in the same red `Typography`
(`CreateWizardShell.tsx:246-248`, `ModifyProjectSettings.tsx:357-360`). That is *by design* and is
fine — but it means the only diagnostic signal available to the user is whatever string the
backend put in `technicalDetails`, which is the generic .NET sentence.

---

## 3. Board-selection path, end to end (investigation line 2)

1. `BoardWizard.tsx` → `wizardService.getBoardInformation(connectionId, board.id)`.
2. Backend `JiraWorkTrackingConnector.GetBoardInformation` (433) → `GetBoardInformationFromJira` (629).
3. `GET rest/agile/latest/board/{id}/configuration` (633). **On failure it returns an *empty*
   `BoardInformation` and HTTP 200** (635-638) — no log, no signal.
4. Three fan-out calls (643-645):
   - `ExtractJqlFromBoardConfiguration` (753) → `ExtractFilterQuery` (784) reads
     `configuration.filter.id`, then `GetFilterQueryById` (800) does
     **`GET rest/api/2/filter/{filterId}`** and returns `RemoveOrderByClause(jql)` — or
     **`string.Empty` on any non-2xx, silently** (804-807).
   - `GetItemTypesForBoard` (728) → `GET rest/agile/latest/board/{id}/issue?maxResults=1000`,
     `[]` on failure, silently (732-735).
   - `GetStateMappingForBoard` (658) → `MapStatusToCategory` (678) →
     `GET rest/api/latest/status`, `([],[],[])` on failure, silently (684-687).
5. **The combination step, `ExtractJqlFromBoardConfiguration:757-763`:**

   ```csharp
   var filter = await ExtractFilterQuery(client, root);   // "" when the filter could not be read
   subQuery   = ExtractSubQuery(root, subQuery);          // "AND (<sub-filter>)" or ""
   return $"{filter} {subQuery}";
   ```

   **The board filter's JQL is never wrapped in parentheses**, and the sub-filter is hard-coded to
   arrive with a leading `AND ` (`ExtractSubQuery:778`).
6. Frontend `useCreateWizard.mergedWith` copies `boardInfo.dataRetrievalValue` verbatim into the
   team's `dataRetrievalValue`, then `handleWizardComplete` **immediately** calls
   `validateSettings` — which is exactly the "selected a board, then validation doesn't go through"
   sequence the reporter describes.
7. `PrepareQuery` (1735-1746) assembles the final JQL:

   ```csharp
   var configuredFilter = RemoveOrderByClause(owner.DataRetrievalValue);
   var lighthousesOwnFilters = $"{workItemsQuery} {stateQuery} {cutoffDateFilter}";
   return string.IsNullOrWhiteSpace(configuredFilter)
       ? WithoutTheLeadingConjunction(lighthousesOwnFilters)
       : $"({configuredFilter}) {lighthousesOwnFilters}";
   ```

   The *team's* query **is** parenthesised here (1745) — so the naive "board JQL not bracketed
   before AND-ing Lighthouse's own filters" theory is **false**, and the "ORDER BY left in the
   team query" theory is **also false** (1740 strips it). Both are evidence *against* the simplest
   readings of the reporter's hypothesis. The defect is one level in: the *board* query is
   assembled unsafely **before** it ever reaches `PrepareQuery`.

Verified assembled output (simulation of lines 1735-1786 over the board string from step 5):

```
filter = ""  ·  subQuery = "AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)"

( AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)) AND (issuetype = "Story")  AND (status = "Done")
 ^^^^^ Jira: "Error in the JQL Query: Expecting either a value, list or function but got 'AND'."  -> HTTP 400
```

---

## 4. Root cause chains

### Chain A — the board filter could not be read, and the sub-filter's `AND` was emitted anyway

```
WHY 1A: Jira answers 400 to the validation search.
  [Evidence: JiraWorkTrackingConnector.cs:1533 EnsureSuccessStatusCode(); the thrown message is
   the screenshot's second line verbatim.]

  WHY 2A: The JQL sent begins with "( AND " — a conjunction with no left operand.
    [Evidence: PrepareQuery:1745 emits $"({configuredFilter}) …"; configuredFilter is the board
     string. Simulated output above. Atlassian KB "How to handle HTTP 400 … on the Jira search
     REST endpoint" names "Invalid JQL query — malformed syntax" as a documented 400 cause.]

    WHY 3A: ExtractJqlFromBoardConfiguration concatenates an EMPTY filter with a sub-filter that
            carries a hard-coded leading "AND ".
      [Evidence: ExtractSubQuery:776-779 -> subQuery = $"AND ({subQuery})";
       ExtractJqlFromBoardConfiguration:763 -> return $"{filter} {subQuery}";
       There is no guard for filter == "".]

      WHY 4A: GetFilterQueryById returns string.Empty on ANY non-2xx and tells nobody.
        [Evidence: JiraWorkTrackingConnector.cs:800-807 —
           var response = await client.GetAsync($"rest/api/2/filter/{filterId}");
           if (!response.IsSuccessStatusCode) { return string.Empty; }
         No log line, no thrown exception, no flag on BoardInformation. Contrast
         GetBoardsFromJira:900-910, which DOES log status, reason, URI and body.]

        WHY 5A: The board-reading design treats "could not read part of this board" as
                indistinguishable from "this part of the board is empty", so a half-read board is
                handed to the user as a complete one, and the first thing that notices is Jira.
          [Evidence: four independent silent degradations on one call path —
           GetBoardInformationFromJira:635-638 (returns an empty BoardInformation, HTTP 200),
           GetFilterQueryById:804-807, GetItemTypesForBoard:732-735, MapStatusToCategory:684-687.
           BoardWizard.tsx renders whatever came back and enables Confirm on
           `!boardInformation` alone — a partially-empty object passes.]

-> ROOT CAUSE A: GetBoardInformation has no notion of a partial read. Any sub-request that fails
  degrades to an empty value, and the JQL assembler then produces a syntactically invalid query
  out of the surviving fragments.
```

**Why the filter read fails on Data Center — HYPOTHESIS, unverified.** `rest/api/2/filter/{id}`
requires the calling account to own the saved filter or to be inside its share permission. Board
filters on DC are routinely owned by an admin and shared with a group/project role the Lighthouse
service account is not in, whereas the *board* itself is visible. Jira answers 400/403/404
depending on version. There is **no log line and no captured response** proving this for the two
affected users — precisely because of Chain D. Alternative triggers for the same empty-filter
state, also unverified: a board whose `configuration.filter` key is absent
(`ExtractFilterQuery:788-792`), or a `filter` payload without a `jql` property
(`GetFilterQueryById:812-815`).

**The "sometimes" in Chain A is explained:** the leading-`AND` string only forms when the filter is
empty **and** the sub-filter is non-empty. `subQuery` is present on **Kanban** boards (the Kanban
sub-filter, e.g. `fixVersion in unreleasedVersions() OR fixVersion is EMPTY` — confirmed by the
existing integration assertion `JiraWorkTrackingConnectorTest.cs:551-553`) and absent on **Scrum**
boards. Scrum board + unreadable filter -> `" "` -> `IsNullOrWhiteSpace` -> `PrepareQuery:1744` takes
the safe branch and no 400 occurs (the user instead gets stuck on Configure with an empty query).
Same instance, same user, same permissions — **one board 400s and the next does not.**

---

### Chain B — `RemoveOrderByClause` cuts by character position and can cut inside a bracket

```
WHY 1B: Jira answers 400 to the validation search.  [same evidence as 1A]

  WHY 2B: The JQL sent has unbalanced parentheses.
    [Evidence: worked example below.]

    WHY 3B: PrepareQuery:1740 runs RemoveOrderByClause over the WHOLE board string — filter and
            sub-filter already joined — and truncates at the first ORDER BY it finds.
      [Evidence: RemoveOrderByClause:820-830 -> `jql[..orderByIndex].TrimEnd()`.]

      WHY 4B: IndexOfOrderByClause tracks quote state but NOT bracket depth, so an ORDER BY that
              sits inside `AND ( … )` is treated as a top-level ordering clause.
        [Evidence: IndexOfOrderByClause:838-859 — the loop handles only `'"' or '\''`
         (EndOfQuotedValue:865-870) and the standalone-word check
         (StartsOrderByClauseAt:872-887). There is no depth counter.
         Worked example:
           board filter   = "project = FOO"
           board subQuery = "resolution = EMPTY ORDER BY Rank"    (kept verbatim — see 5B)
           ExtractSubQuery:778 -> "AND (resolution = EMPTY ORDER BY Rank)"
           line 763            -> "project = FOO AND (resolution = EMPTY ORDER BY Rank)"
           line 1740 cut       -> "project = FOO AND (resolution = EMPTY"     <- bracket never closed
           line 1745           -> "(project = FOO AND (resolution = EMPTY) AND (issuetype = \"Story\")"
                                                                             <- 400 ]

        WHY 5B: ORDER BY is removed with string arithmetic rather than by parsing, and the one
                place that removes it correctly (GetFilterQueryById:817, applied to the filter
                before joining) is not applied to the sub-filter — so the two halves of the same
                board string are handled by different rules.
          [Evidence: GetFilterQueryById:817 calls RemoveOrderByClause; ExtractSubQuery:766-782
           does NOT. The XML doc at 832-837 shows the author reasoned about quoting and stopped
           there; brackets were never considered.]

-> ROOT CAUSE B: RemoveOrderByClause is position-based and bracket-blind, and is applied
  asymmetrically to the two halves of a board-derived query.
```

Chain B is **lower probability than A** — a Kanban sub-filter with an `ORDER BY` in it is unusual,
because the Jira UI does not encourage ordering a sub-filter. It is included because it is a real
defect on the same lines, it produces the identical user-visible symptom, and one fix closes both.

---

### Chain C — the reporter's hypothesis: query length (quantified, NOT ruled out, NOT most likely)

```
WHY 1C: Jira answers 400 to the validation search.  [same evidence as 1A]

  WHY 2C: The request line exceeds the servlet container's or reverse proxy's header budget.
    [Evidence FOR: Atlassian KB "How to handle HTTP 400 Bad Request errors on Jira search REST
     endpoint" lists, as its first named cause, "Request header size exceeding Tomcat or proxy
     configuration". Jira DC's bundled Tomcat ships maxHttpHeaderSize=8192; Apache's
     LimitRequestLine defaults to 8190; nginx's large_client_header_buffers defaults to 8k.]

    WHY 3C: The Data Center search is issued as a GET with the JQL URL-encoded into the query
            string, so the JQL competes for the request-line budget.
      [Evidence: GetIssuesByQueryFromDataCenter:1527-1532 —
         var encodedJqlQuery = Uri.EscapeDataString(jqlQuery);
         var url = $"rest/api/latest/search?jql={encodedJqlQuery}&startAt={startAt}&maxResults={maxResults}&expand=changelog";
         var response = await client.GetAsync(url);
       Note this is GET on BOTH deployments — the Cloud walk is also a GET
       (WalkCloudSearchPages:1627-1641). Jira DC also accepts POST /rest/api/2/search with a JSON
       body, which is effectively unbounded.]

      WHY 4C: The JQL PrepareQuery builds grows linearly with the board's mapped state count and
              issue-type count, and nothing caps or measures it.
        [Evidence: PrepareGenericQuery:1781-1786 emits one `status = "X" OR ` clause per mapped
         state and one `issuetype = "Y" OR ` per type, with no cap. The board wizard populates
         those lists automatically from the board's columns (GetStateMappingForBoard:658-676) and
         from up to 1000 of the board's issues (GetItemTypesForBoard:730).]

        WHY 5C: No component on the path knows how long the request it is about to make is, and no
                component would report it if it were rejected for being too long.
          [Evidence: no length check anywhere between 1735 and 1533; the JQL is logged only at
           LogDebug (1485), i.e. invisible at the default log level.]

-> ROOT CAUSE C (conditional): the DC search transport is a length-bounded GET, and the JQL that
  the board wizard generates is unbounded in the number of state and type clauses.
```

**Quantification — this is what the reporter's hypothesis actually requires.** Measured
`Uri.EscapeDataString` expansion factor on realistic Lighthouse JQL: **~1.56x** (letters and digits
pass through; spaces, `=`, `"`, `(`, `)`, `'`, `,` all escape).

| Board shape | raw JQL | encoded | approx. request line |
|---|---:|---:|---:|
| 3 types, 6 states, short filter | 382 | 608 | **690 B** |
| 8 types, 30 states, ~400-char filter | 1 656 | 2 590 | **2 672 B** |
| 10 types, 80 states, 60-project filter | 3 664 | 5 734 | **5 816 B** |

To breach an 8 192-byte request line you need roughly **5 200 characters of raw JQL** — a board
with ~100 mapped statuses, or a genuinely enormous saved filter. Behind a reverse proxy tuned to
4 KB (not the default, but common in locked-down DC estates) the threshold falls to ~2 500 raw
characters, which the "8 types / 30 states" row already clears.

**Verdict on the reporter's hypothesis:** *plausible, unverified, and not the leading explanation.*
It requires an extreme board or a non-default proxy, whereas Chain A requires only a Kanban board
whose saved filter the service account cannot read. It is **not dismissible**, because Atlassian
names it as a documented cause of exactly this 400 and because Lighthouse does use a length-bounded
GET on the DC path. **One log line settles it** — see Chain D. The recommended fix ships that log
line and then, if the evidence points here, the DC search moves to POST.

---

### Chain D — the underlying Jira error is discarded (why A, B and C cannot be told apart)

```
WHY 1D: Neither the user nor the maintainer can tell which of A, B or C happened.
  [Evidence: the entire technical detail available is
   "Response status code does not indicate success: 400 (Bad Request)." — the screenshot.]

  WHY 2D: Jira's 400 response body — which carries `errorMessages` naming the exact fault, e.g.
          "Error in the JQL Query: Expecting either a value, list or function but got 'AND'." —
          is never read.
    [Evidence: GetIssuesByQueryFromDataCenter:1532-1534 —
       var response = await client.GetAsync(url);
       response.EnsureSuccessStatusCode();                             <- throws here
       var responseBody = await response.Content.ReadAsStringAsync();  <- never reached on 400
     EnsureSuccessStatusCode() builds its message only from the status line. Same shape at
     WalkCloudSearchPages:1644 and WalkDataCenterSearchOffsets:1686.]

    WHY 3D: The JQL that failed is not recorded either. It is logged only at Debug.
      [Evidence: GetIssuesByQuery:1485 — logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
       and ValidateTeamSettings:983 logs team.DataRetrievalValue (the board string) at Information
       but NOT the assembled query that PrepareQuery produced at 985. The catch at 1004 logs the
       exception at LogInformation without the query.]

      WHY 4D: The connector's convention is `EnsureSuccessStatusCode()` for "should not happen"
              paths and `IsSuccessStatusCode` + degrade for "might not work" paths, and neither
              convention preserves the remote system's own explanation.
        [Evidence: 17 status checks in this file; the ONLY one that logs the response body is
         GetBoardsFromJira:902-909. Every other one either throws the generic .NET message
         (1175, 1328, 1533, 1842) or returns a default silently (360, 635, 684, 732, 804, 1644,
         1686, 1926, 2087, 2122).]

        WHY 5D: Connector failures are modelled as "did it work", not as "what did the other
                system say" — so a remote system that explains itself precisely is flattened into
                a boolean plus a status code before the explanation reaches anyone.
          [Evidence: ConnectionValidationResult (Models/Validation/ConnectionValidationResult.cs)
           has the right shape for this — `code`, `message`, `technicalDetails`, `fieldName` — and
           the frontend already renders technicalDetails (useCreateWizard.runValidation ->
           CreateWizardShell.tsx:246-248). The channel exists and is fed a .NET sentence.]

-> ROOT CAUSE D: the Jira response body is discarded at the throw site, so the one artefact that
  would name the fault never enters a log, a support bundle, or the UI.
```

Chain D is a **separately shippable defect** independent of A/B/C. It is also what makes A, B and C
undecidable from the field report alone, and why this RCA carries an explicitly labelled hypothesis
rather than a proven trigger.

---

## 5. Cross-validation

- **A + B + C are consistent, not competing.** All three terminate at the same throw site
  (1533) and produce the same user-visible string. They differ only in what makes Jira say 400.
- **Backwards validation A:** if `rest/api/2/filter/{id}` returns non-2xx for a Kanban board, then
  `GetFilterQueryById:806` returns `""` -> `763` yields `" AND (…)"` -> `1745` yields `"( AND (…)) …"`
  -> Jira 400 -> `1533` throws -> `1004` catches -> `1007-1008` produces the screenshot's two lines
  -> `TeamsController:156` returns HTTP 400. **Every link has a file:line. Chain validates forward.**
- **Backwards validation B:** identical from `1740` onward. Validates.
- **Backwards validation C:** validates *given* a sufficiently large board or a tightened proxy;
  the magnitude is quantified in §4 and is the reason it is ranked third.
- **Backwards validation D:** validates trivially — the body is provably unread at 1533.
- **All symptoms explained?** Yes: the 400, the exact wording, "both on Data Center" (Chains A and
  C are both DC-shaped: DC saved-filter sharing, and DC's customer-operated Tomcat/proxy — Cloud
  has neither), "both selected a board" (the board path is the only producer of an
  un-parenthesised, machine-assembled `DataRetrievalValue`), and "sometimes" (Kanban vs Scrum
  sub-filter presence; board size).
- **Contradictions:** none found. One theory was **disproven and should not be re-derived**: the
  team's own query *is* correctly parenthesised and *is* correctly ORDER-BY-stripped at
  `PrepareQuery:1740-1745`. The defect is upstream of that, in the board assembler.

---

## 6. Contributing factors

| # | Factor | Evidence | Severity |
|---|---|---|---|
| CF-1 | Jira's 400 body (`errorMessages`) discarded at the throw site | `1533` | **High** — this is Chain D; a separate fixable defect |
| CF-2 | The assembled JQL is logged only at `LogDebug` | `1485`; the catch at `1004` omits it | **High** — no support bundle contains the failing query |
| CF-3 | Four silent degradations on the board path present a half-read board as complete | `635-638`, `684-687`, `732-735`, `804-807` | **High** — Chain A's WHY-5 |
| CF-4 | Board filter never parenthesised before `AND`-ing the sub-filter -> **operator-precedence corruption** | `763` + `778`. `project = A OR project = B` + sub-filter yields `project = A OR project = B AND (…)`; AND binds tighter, so half the board silently drops out | **High, latent** — this is *valid* JQL, so it returns HTTP 200 and silently wrong data. Not the reported 400, but the same line fixes it |
| CF-5 | `ORDER BY` stripped from the filter but not the sub-filter | `817` vs `766-782` | Medium — Chain B |
| CF-6 | State and issue-type names are interpolated into JQL without escaping `"` or `\` | `1783`: `$"{fieldName} {queryComparison} \"{o}\""` | Medium — a status named `5" display` produces invalid JQL |
| CF-7 | A state or type name that does not exist on the instance produces a Jira 400, not an empty result | Atlassian KB: *"The value 'KANBAN3' does not exist for the field 'Project'"* | Medium — only reachable when the user hand-edits the wizard-filled lists |
| CF-8 | DC search is a length-bounded GET where DC also accepts POST | `1527-1532` | Medium — Chain C |
| CF-9 | `handleWizardComplete` sets no error at all when the throw is not an `ApiError` | `useCreateWizard.ts`, `handleWizardComplete` catch — only the `instanceof ApiError` branch calls `setValidationError` | Low — the wizard silently bounces back to Configure |
| CF-10 | `new Uri(baseUrl)` with a `TrimEnd('/')`-ed URL drops a Jira DC **context path** (`https://jira.corp/jira` -> `https://jira.corp/rest/...`) | `1826`/`1871`; RFC 3986 relative resolution replaces the last segment | **Not this bug** — it would break board listing too, and these users listed boards. Recorded so it is not re-derived. |

---

## 7. Proposed fix (minimal, regression-test-first)

### F1 — `ExtractJqlFromBoardConfiguration`: assemble a well-formed board query (closes A, CF-4, and half of B)

`.../Jira/JiraWorkTrackingConnector.cs:753-782`

```csharp
private static async Task<string> ExtractJqlFromBoardConfiguration(JsonDocument boardsJson, HttpClient client)
{
    var root = boardsJson.RootElement;

    var filter   = await ExtractFilterQuery(client, root);          // already ORDER BY-stripped at 817
    var subQuery = RemoveOrderByClause(ExtractSubQuery(root));      // now stripped too

    if (string.IsNullOrWhiteSpace(filter))
    {
        return string.IsNullOrWhiteSpace(subQuery) ? string.Empty : $"({subQuery})";
    }

    return string.IsNullOrWhiteSpace(subQuery) ? $"({filter})" : $"({filter}) AND ({subQuery})";
}

// returns the RAW sub-filter; the caller decides whether a conjunction is warranted
private static string ExtractSubQuery(JsonElement root)
    => root.TryGetProperty("subQuery", out var subQueryElement)
       && subQueryElement.TryGetProperty("query", out var query)
        ? query.GetString() ?? string.Empty
        : string.Empty;
```

Three behaviour changes, all of them the point: no query can begin with `AND`; each half is
bracketed so `OR` inside either half cannot be captured by the other's `AND`; and the sub-filter's
ordering clause comes off before the two are joined.

### F2 — `IndexOfOrderByClause`: never cut inside a bracket (closes the rest of B)

`.../Jira/JiraWorkTrackingConnector.cs:838-859` — add a depth counter to the existing walk:

```csharp
var depth = 0;
while (index < jql.Length)
{
    if (jql[index] is '"' or '\'') { index = EndOfQuotedValue(jql, index); continue; }
    if (jql[index] == '(') { depth++; index++; continue; }
    if (jql[index] == ')') { depth--; index++; continue; }
    if (depth == 0 && StartsOrderByClauseAt(jql, index)) { return index; }
    index++;
}
```

A comment here should say *why* — a cut inside a bracket leaves the bracket unclosed and Jira
rejects the whole query — not restate the code.

### F3 — surface Jira's own words (closes D / CF-1 / CF-2)

`.../Jira/JiraWorkTrackingConnector.cs:1531-1534`, replacing `EnsureSuccessStatusCode()`:

```csharp
var response = await client.GetAsync(url);
if (!response.IsSuccessStatusCode)
{
    var failBody = await response.Content.ReadAsStringAsync();
    logger.LogWarning(
        "Jira rejected a search for {Owner}. {StatusCode} {Reason}. JQL: '{Query}'. Body: {Body}",
        owner.Name, (int)response.StatusCode, response.ReasonPhrase, jqlQuery, failBody);
    throw new HttpRequestException(
        $"Jira rejected the query ({(int)response.StatusCode} {response.ReasonPhrase}): {failBody}");
}
```

The pattern is already in this file at `900-910`; this makes the search path match it. The thrown
message flows unchanged through `1008` -> `technicalDetails` -> `CreateWizardShell.tsx:246-248`, so
the user reads *"Error in the JQL Query: …"* instead of *"400 (Bad Request)"* with no frontend
change at all. Mirror at `1644` and `1686` if cheap; not required to close this bug.

### F4 — stop presenting a half-read board as whole (closes CF-3)

`GetFilterQueryById:804-807` and `GetBoardInformationFromJira:635-638`: log status, reason, URI and
body before degrading, mirroring `900-910`. Minimal version is log-only. (Carrying a
"filter unreadable" flag onto `BoardInformation` so `BoardWizard` can say so is the right answer
but is a feature, not this bugfix — record it as follow-up.)

### F5 — escape quote characters in interpolated names (closes CF-6)

`PrepareGenericQuery:1783` — escape `\` then `"` in `o` before interpolation. One expression.

### Deliberately NOT in this fix

- **Moving the DC search to `POST /rest/api/2/search`** (Chain C). It is the correct answer *if*
  length is the trigger, but it changes the transport for every DC customer and the evidence does
  not yet point there. **F3 produces that evidence**: the log line carries the JQL, so its length
  is measurable from the next occurrence, and a Tomcat/proxy rejection is distinguishable from a
  Jira JQL rejection by the body (an HTML Tomcat error page vs. JSON `errorMessages`). Raise a
  follow-up item, gated on one real log.
- Any refactor of the 17 status checks into a shared helper.

---

## 8. Files affected

| File | Change |
|---|---|
| `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` | F1 (753-782), F2 (838-859), F3 (1531-1534), F4 (635-638, 804-807), F5 (1783) |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/` | **new** `JiraBoardQueryAssemblyTest.cs` (see §10) |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnectorTest.cs:553` | assertion must be updated — F1 changes the expected string to `(project = LIGHTHOUSE AND type IN (Bug, Story)) AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)`. **`[Category("JiraIntegration")]` — edit it, do not run it.** |

No frontend change. No DTO change. No migration.

---

## 9. Risk assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| F1 changes the `DataRetrievalValue` string stored for **existing** teams? | **None** | — | F1 runs only when a board is (re)selected. Stored values are untouched; `PrepareQuery` already brackets them at 1745. |
| F1's extra brackets change which issues a query matches | Low | Medium | Only where the filter or sub-filter contains a top-level `OR` — and there the *current* behaviour is wrong (CF-4). Re-selecting the board is what applies the correction; existing teams keep their stored string until they do. Worth a release-note line. |
| F2's depth counter mis-handles a bracket inside a quoted literal | Low | Medium | The quote skip at `844-846` runs *before* the bracket check, so quoted brackets are never counted. Cover with a case: `summary ~ "a (b" ORDER BY key`. |
| F2 changes behaviour for unbalanced input | Low | Low | `depth` can go negative on a malformed query; `depth == 0` then never holds and the ordering clause is left in. That query was already broken. Acceptable; do not add recovery. |
| F3 leaks Jira internals into a user-facing string | Medium | Low | Jira's `errorMessages` is the text Jira shows its own users. It lands in `technicalDetails`, which the UI already treats as diagnostic, not as the headline `message`. |
| F3 logs a JQL containing a project or label name | Medium | Low | `LogWarning`, only on failure. `GetBoardsFromJira:903-909` already logs a body on failure at `LogInformation`; this is no wider. |
| F5 breaks a state name that legitimately contains a backslash | Very low | Low | Escaping `\` before `"` is the standard order; a regression case pins it. |
| Existing JiraIntegration assertion at `:553` reds in CI | **Certain** if not updated | Medium | Update it in the same commit. It is in `JiraIntegration`, so it will not be caught by the local filtered run — this is the one thing most likely to burn a CI cycle. |
| Sonar: new `if` nesting in F1/F2 | Low | Low | F1 is three early returns; F2 adds three `continue`s. Both flat. |

---

## 10. Reproduction recipe (writable as a regression test, no live Jira)

### 10a. The exact shape that triggers it

**Chain A.** A Jira **Data Center** connection; a **Kanban** board (a board whose
`/rest/agile/latest/board/{id}/configuration` payload contains a `subQuery.query`) whose
`/rest/api/2/filter/{filterId}` answers anything non-2xx (403/404/400 — a saved filter the
service account does not own and is not in the share permission of).

```
board configuration payload:
  { "filter": { "id": "10500" },
    "subQuery": { "query": "fixVersion in unreleasedVersions() OR fixVersion is EMPTY" },
    "columnConfig": { "columns": [ { "statuses": [ { "id": "10001" } ] } ] } }

GET rest/api/2/filter/10500            -> 403     (this is the trigger)
GET rest/api/latest/status             -> [ { "id":"10001","name":"Done",
                                              "statusCategory":{"name":"Done"} } ]
GET rest/agile/latest/board/8/issue?…  -> { "issues":[ {"fields":{"issuetype":{"name":"Story"}}} ] }

-> BoardInformation.DataRetrievalValue == " AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)"
-> PrepareQuery                        == "( AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)) AND (issuetype = \"Story\")  AND (status = \"Done\")  "
-> GET rest/api/latest/search?jql=…    -> 400
```

**Chain B.** Same, but `filter/10500` **succeeds** with `"jql": "project = FOO ORDER BY Rank ASC"`
and `subQuery.query` is `"resolution = EMPTY ORDER BY Rank"`. Assembled:
`project = FOO AND (resolution = EMPTY ORDER BY Rank)` -> `RemoveOrderByClause` at `1740` truncates
to `project = FOO AND (resolution = EMPTY` -> bracket never closed -> 400.

**CF-4 (silent, not a 400).** `filter/10500` returns `"jql": "project = A OR project = B"`,
sub-filter non-empty. Assembled today: `project = A OR project = B AND (…)`. Jira accepts it and
`project = A` matches unconditionally while `project = B` is narrowed — half the board behaves
differently from the other half, with no error anywhere.

### 10b. Test to write (NUnit 4.6 + Moq — **not** xUnit/NSubstitute)

Build on `Lighthouse.Backend.Tests/TestHelpers/JiraConnectorTestSetup.cs`:

- `JiraConnectorTestSetup.AConnectorOver(handler)` — injects the stub `HttpMessageHandler` through
  the connector's `httpMessageHandlerForTesting` constructor parameter
  (`JiraWorkTrackingConnector.cs:28`).
- `JiraConnectorTestSetup.ATeamOnJiraCloud()` — despite the name, it uses
  `AuthenticationMethodKeys.JiraCloud`, which is **not** a gateway method
  (`RoutesViaAtlassianCloudGateway:1788-1790` covers only `JiraScopedToken`/`JiraOAuth`). So the
  deployment is decided entirely by what the stub answers to `rest/api/2/serverInfo`, and returning
  `{"deploymentType":"Server"}` puts the test on the **Data Center** path. It also hands out a
  unique connection id and a unique URL per call, which the static `DeploymentCache` /
  `FieldNames` on the connector require.

Copy the stub-handler pattern verbatim from
`Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraIssuesPerRequestTest.cs`
(`CreateRecordingHandler` / `BuildResponse`, `Mock<HttpMessageHandler>` + `.Protected()` +
`ItExpr`, path-switch on `request.RequestUri.AbsolutePath`). Extend its switch with
`rest/agile/latest/board/{id}/configuration`, `rest/api/2/filter/{id}`, `rest/api/latest/status`
and `rest/agile/latest/board/{id}/issue`, and let the filter response's status code be a parameter.

New fixture `JiraBoardQueryAssemblyTest` (plain `[TestFixture]`, **no** `Category` attribute — it
must run in the default filtered suite):

1. `GetBoardInformation_FilterUnreadable_DoesNotProduceAQueryStartingWithAnd`
   — filter stub -> `403`; assert `boardInformation.DataRetrievalValue` does not start with `AND`
   after trimming, and is either empty or a balanced bracketed expression. **RED today.**
2. `GetBoardInformation_FilterAndSubFilterBothPresent_BracketsEachHalf`
   — assert `"(project = FOO) AND (fixVersion is EMPTY)"`. Pins CF-4.
3. `ValidateTeamSettings_BoardWithUnreadableFilter_SendsAJqlJiraCanParse`
   — full path: capture the `rest/api/latest/search?jql=` URL the connector issues (the
   `CaptureSearchUrl` helper already does exactly this), `Uri.UnescapeDataString` it, and assert it
   contains no `( AND ` and has balanced parentheses. **This is the bug's regression test.**
4. `PrepareQuery_SubFilterCarriesAnOrdering_LeavesBracketsBalanced`
   — Chain B, asserted on the same captured URL.
5. `ValidateTeamSettings_JiraRejectsTheQuery_ReportsWhatJiraSaid`
   — search stub -> `400` with body
   `{"errorMessages":["Error in the JQL Query: Expecting either a value, list or function but got 'AND'."]}`;
   assert `result.TechnicalDetails` **contains** `Error in the JQL Query` and does **not** equal
   the bare `Response status code does not indicate success…`. Pins F3 / Chain D.
6. `PrepareQuery_StateNameContainsAQuote_EscapesIt` — CF-6, via the same captured URL.

Mutation testing: these all assert on a captured request string rather than on a boolean, which is
what keeps the Stryker kill rate up on `ExtractJqlFromBoardConfiguration` and
`IndexOfOrderByClause`.

**Constraint honoured throughout:** no live-connector category was executed and the live Jira API
was not hand-explored. `JiraWorkTrackingConnectorTest.cs` was read for evidence only.

---

## Sources

- [How to handle HTTP 400 Bad Request errors on Jira search REST API endpoint — Atlassian](https://support.atlassian.com/jira/kb/how-to-handle-http-400-bad-request-errors-on-jira-search-rest-api-endpoint/)
- [The Jira Data Center REST API — filter](https://developer.atlassian.com/server/jira/platform/rest/v11002/api-group-filter/)
- [Jira REST API Filter getting 400 status — Atlassian Community](https://community.atlassian.com/forums/Jira-questions/Jira-REST-API-Filter-getting-400-status-and-connection-close/qaq-p/1940060)
