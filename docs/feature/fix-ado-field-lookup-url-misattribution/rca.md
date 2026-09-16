# RCA — Bug #6013: the Azure DevOps additional-field lookup, and what it is actually blamed on

Method: Toyota 5 Whys, multi-causal, evidence required at every level.
Date: 2026-09-16 · Investigator: Rex (nw-troubleshooter)
Repo state: worktree `frolicking-fluttering-karp`, branch `worktree-frolicking-fluttering-karp`, HEAD `b47274c86`.
Twin: `docs/feature/fix-jira-field-lookup-error-message/rca.md` (Bug #6012, Jira, delivered today).
Predecessor of the twin: Bug #5973.

Unqualified `:NNN` line references are
`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs`
(1378 lines at HEAD).

---

## 0. Headline — the bug as filed is not reproducible, and there is still a defect

**#6013 as written says: "reports a failure of the additional-field lookup as a broken URL." That
cannot happen.** Measured, not inferred (§3): the Azure DevOps .NET SDK never surfaces an
`HttpRequestException` for an HTTP status. Every status the fields endpoint can answer with
(401/403/404/429/4xx/5xx, and a 200 carrying an HTML proxy page) arrives at `ValidateConnection` as a
`Vss*` exception, which is caught at `:231` or `:241` — **above** the URL branch at `:250`. The URL
branch is reachable only from a genuine transport failure (DNS, refused connection, TLS), and in that
case the message it prints is the right one.

**Do not close #6013 as not-a-defect. Re-title it.** The misattribution is real; it points somewhere
else:

| What happens to the fields call | What the administrator is told | Where |
|---|---|---|
| 403 / 404 / 429 / 5xx / 400, any body shape | **"Azure DevOps rejected the connection settings."** — about a connection that had just answered a WIQL query two lines earlier. Technical detail is often the single word `Forbidden` | `:241-248` |
| 401 carrying a `WWW-Authenticate` challenge | **"Authentication failed for Azure DevOps." pinned to the `PersonalAccessToken` input** — the credential that had just authenticated successfully | `:231-239` |
| The same failure, reached from **Portfolio settings → Validate** | "Portfolio validation failed due to an unexpected error." + the same one word | `:337-344` |
| A malformed/cut-off response body | "Connection validation failed due to an unexpected error." with **no detail at all** | `:270-274` |

Nothing anywhere says the field list could not be read, and nothing points at the Additional Fields
input. That is the same defect class as #6012 and #5973 with a different exception type in the middle.

---

## 1. Problem statement (scoped)

An administrator configures **additional fields** on an Azure DevOps connection and presses
**Validate**. The connection itself is fine — `ValidateConnection` proves that at `:217` before it
ever looks at fields. If the field-metadata read then fails, the verdict names the connection
settings or the PAT, never the field list, and the technical detail is frequently one word.

**In scope:** `witClient.GetWorkItemFieldsAsync` at `:684`, its two callers
(`GetMissingAdditionalFields :667`, `ThePayloadsAndFieldsFor :735`), the three verdict-producing
entry points that reach it, and the `SingleOrDefault` at `:690` that consumes its result.

**Out of scope:** the WIQL/query paths (Bug #5756 already made those refuse rather than answer
empty), write-back (`:408-518`), boards discovery (`:347-406`, recorded in §5 as a separate item),
and the four non-ADO connectors.

---

## 2. Seed-claim verification

Every seed claim was re-read against the file. All confirmed; two need a correction of the
*conclusion* drawn from them.

| Seed claim | Verdict |
|---|---|
| `:219` `GetMissingAdditionalFields(witClient, connection.AdditionalFieldDefinitions)` inside the `try`, right after `VerifyConnection(witClient)` at `:217` | **Confirmed.** `try` opens at `:213`; `:215` gets the client, `:217` runs a WIQL, `:219` reads fields. |
| `:667` → `:681` → `:684` reaches `GetWorkItemFieldsAsync`, not wrapped in `ExecuteWithRetry` | **Confirmed.** `GetMissingAdditionalFields :667` → `GetCustomFieldReferences :681` → `:684`, a bare `await`. Every other remote call in the file goes through `ExecuteWithThrottle :771` (`:169`, `:184`, `:493`, `:584`, `:610`, `:835`, `:886`, `:947`, `:1022`). The fields call and `VerifyConnection`'s WIQL (`:662`) are the only two that do not. |
| `:250-258` `catch (HttpRequestException ex)` → "Could not reach Azure DevOps with the provided URL.", `fieldName = Url` | **Confirmed** — and **unreachable from an HTTP status** (§3). |
| `:231` / `:241` sit above it | **Confirmed**, and both are needed: `VssUnauthorizedException` derives from `VssException`, **not** from `VssServiceException`, so `:241` would not catch it. Measured by reflection against the pinned package (§3). |
| `:808` `catch (HttpRequestException ex) when (ex.StatusCode is TooManyRequests or ServiceUnavailable)` | **Confirmed present — and dead.** See Chain F. This line is what made the seed's second inference look right. |
| `:270` bare catch, "Connection validation failed due to an unexpected error.", no details | **Confirmed.** |
| `:690` `SingleOrDefault(...)` throws on two matches | **Confirmed** — and effectively unreachable on Azure DevOps Services through supported paths. See §4 Chain E and §6. |

**One claim the seed did not make, and it matters:** `fieldName` never reaches a rendered input.
`ConnectionValidationResult.FieldName` (`ConnectionValidationResult.cs:20`) is serialised, parsed
into `ApiError.fieldName` (`BaseApiService.ts:97-98`, `ApiError.ts:23`) — and **no component reads
it**. A grep for `fieldName` across `Lighthouse.Frontend/src/**/*.tsx` returns nothing. The
connection screen renders `message` and `technicalDetails` only
(`ModifyConnectionSettings.tsx:715-730`). So the administrator is misdirected **entirely by the
sentence**, not by a highlighted box — which raises the value of the message text and lowers the
value of the `fieldName` argument. (Set it anyway, for consistency with `JiraReadException`
and for whoever wires the highlight up.)

---

## 3. The central question, settled by measurement

> Which exception type does a failing `GetWorkItemFieldsAsync` actually raise, for which HTTP status?

**Method.** Not inference and not decompilation. An offline probe: a local TLS listener plays Azure
DevOps, a real `WorkItemTrackingHttpClient` from the pinned package
(`Microsoft.TeamFoundationServer.Client` **20.256.2**, `Lighthouse.Backend.csproj:45`) is pointed at
it, the API-negotiation `OPTIONS /_apis/` is answered **successfully** so that only the
`GET /_apis/wit/fields` fails — which is exactly the connector's shape, because `VerifyConnection`
has already succeeded by then. Two client shapes were run: the production one
(`VssCredentials`, so `VssHttpMessageHandler` is in the pipeline) and a raw `HttpMessageHandler`
pipeline, to separate what the auth handler does from what `VssHttpClientBase` does. No real Azure
DevOps was contacted; no live-connector test category was run.

Probe source, kept out of the repo:
`%LOCALAPPDATA%\Temp\claude\C--Users-benja-repos-Lighthouse\<session>\scratchpad\adoprobe\`.

### 3a. Measured results

| Response to `GET /_apis/wit/fields` | Exception that reaches the caller | `ValidateConnection` catch | Verdict shown |
|---|---|---|---|
| 401 + ADO JSON error body | `VssServiceException`, message = **ADO's own sentence** | `:241` | "Azure DevOps rejected the connection settings." |
| 401 + HTML, no challenge header | `VssServiceResponseException`, message = `"Unauthorized"` | `:241` | same |
| **401 + `WWW-Authenticate: Basic`** | **`VssUnauthorizedException`** — "VS30063: You are not authorized to access …" (after 3 silent retries) | **`:231`** | **"Authentication failed for Azure DevOps." pinned to `PersonalAccessToken`** |
| 401 + `WWW-Authenticate: Negotiate`/`NTLM` | `VssUnauthorizedException` (after 8 retries) | `:231` | same |
| 401 + `WWW-Authenticate: Bearer` only | `VssServiceResponseException` `"Unauthorized"` | `:241` | "Azure DevOps rejected the connection settings." |
| 403 + ADO JSON | `VssServiceException`, ADO's sentence | `:241` | "…rejected the connection settings." |
| **403 + HTML** (proxy/WAF page) | `VssServiceResponseException`, message = **`"Forbidden"`** | `:241` | same, technical detail = one word |
| 404 + JSON / + HTML | `VssServiceException` / `VssServiceResponseException` `"NotFound"` | `:241` | same |
| **429** + plain text + `Retry-After` | `VssServiceResponseException` `"TooManyRequests"` | `:241` | same. **No retry happened** — see Chain F |
| 500 + JSON / + HTML | `VssServiceException` / `VssServiceResponseException` `"InternalServerError"` | `:241` | same |
| 503 + HTML | `VssServiceResponseException` `"ServiceUnavailable"` | `:241` | same |
| 400 + ADO JSON | `VssServiceException`, ADO's sentence | `:241` | same |
| 200/203 + `text/html` (sign-in or proxy page) | `VssServiceResponseException` — "Invalid response content type: text/html Response Content: …" | `:241` | same |
| Body cut off mid-response | `Newtonsoft.Json.JsonSerializationException` | **`:270`** | "Connection validation failed due to an unexpected error." — **no details** |
| **DNS failure** | `HttpRequestException` **`StatusCode = null`** ← `SocketException` | `:250` | "Could not reach Azure DevOps with the provided URL." |
| **Connection refused** | `HttpRequestException` `StatusCode = null` ← `SocketException` | `:250` | same |
| **TLS failure** (https onto a plaintext port) | `HttpRequestException` `StatusCode = null` ← `AuthenticationException` | `:250` | same |

### 3b. The type hierarchy, measured by reflection against the same package

```
VssException                : ApplicationException : Exception        is HttpRequestException? False
VssServiceException         : VssException                            is HttpRequestException? False
VssServiceResponseException : VssServiceException                     is HttpRequestException? False
      own properties: HttpStatusCode HttpStatusCode, String Content
VssUnauthorizedException    : VssException                            is HttpRequestException? False
VssResourceNotFoundException: VssServiceException                     is HttpRequestException? False
```

Three consequences:

1. **No `Vss*` exception is an `HttpRequestException`.** The SDK never calls
   `EnsureSuccessStatusCode()`, which is the only thing in .NET that populates
   `HttpRequestException.StatusCode` on a response. So an `HttpRequestException` arriving at `:250`
   **always** has `StatusCode == null` and always means a transport failure.
2. **`catch (VssServiceException)` at `:241` catches `VssServiceResponseException` too**, which is
   why nearly every status funnels into one sentence.
3. **`VssUnauthorizedException` is not a `VssServiceException`**, so `:231` is load-bearing and its
   position above `:241` is not what makes it work.

### 3c. Does it depend on anything?

Yes, on two things, and neither changes the answer:

- **The response body shape**, not the status. A body ADO's own JSON error contract recognises yields
  `VssServiceException` carrying **ADO's sentence**; anything else (HTML from a proxy, plain text)
  yields `VssServiceResponseException` whose `Message` is just the status **name**. Both are caught
  at `:241`. The practical effect is only on how useful `technicalDetails` is.
- **The presence of a `WWW-Authenticate` challenge on a 401**, which moves the verdict from `:241` to
  `:231` and re-points it at the PAT input. `dev.azure.com` does send a challenge on 401.
- Not on the endpoint: the same `VssHttpClientBase` response handling serves every `*HttpClient` in
  the package. Not on the SDK version within reason — this behaviour is `VssHttpClientBase`'s
  long-standing contract — but the measurement above is specific to **20.256.2** and should be
  re-run if the package is bumped.

### 3d. Alternatives considered and ruled out

Named here rather than left implicit in the chains, so the negatives are auditable:

| Alternative | Ruled out by |
|---|---|
| The connector has a raw `HttpClient` somewhere that *would* produce a status-bearing `HttpRequestException` | Grep for `HttpClient` / `EnsureSuccessStatusCode` / `HttpRequestException` across all 1378 lines: the only `HttpClient` is `ConcurrentDictionary<string, IVssHttpClient> ClientCache` at `:76`, and there is no `EnsureSuccessStatusCode` at all. |
| A different SDK version behaves differently | Measured against the pinned **20.256.2** (`Lighthouse.Backend.csproj:45`). The finding is version-specific by construction and the probe is re-runnable in minutes. |
| The `VssConnection`-built client behaves differently from a directly-constructed one | Both client shapes were probed (credentialed pipeline with `VssHttpMessageHandler`, and a raw `HttpMessageHandler`). They differ **only** on the challenged-401 row (Chain B); every other row is identical. |
| An OAuth token refresh inside the `try` produces the `HttpRequestException` | `OAuthService.PerformRefreshAsync` converts every provider failure to `OAuthRefreshFailedException` (`OAuthService.cs:256-259`). It reaches `:270`, not `:250`. |
| A proxy/WAF blocking at the TCP or TLS layer | It would produce an `HttpRequestException` and reach `:250` — but it would do so at `VerifyConnection :217` first, so the administrator would never be looking at a field-lookup failure. The URL verdict is correct in that case. |
| The 191-field response exceeds a client buffer limit | `VssHttpRequestSettings.MaxContentBufferSize` defaults to **536,870,912** bytes (measured). Not a factor. |

### 3e. So how does the fields call fail while the WIQL just succeeded?

Both are org-scope calls on the same cached client (`ClientCache`, `:1304-1310`), so a
credential/permission answer that differs between them is unusual. The realistic causes, in order:

1. **Rate limiting between the two calls** — `:217` and `:219` are back-to-back, the fields call is
   the one not wrapped in `ExecuteWithThrottle`/`ExecuteWithRetry`, and Chain F shows that even the
   retry it does not use would not have caught a plain 429.
2. **A transient 5xx** on one of two sequential calls.
3. **A proxy or WAF in front of an on-prem Azure DevOps Server** refusing or rewriting the *fields*
   response specifically. It is by far the largest response of the three the validation makes —
   191 field objects on the reporter's own organisation — and a content-type-rewriting proxy lands
   on the "Invalid response content type: text/html" row above.
4. **A conditional-access / sign-in redirect** that returns HTML under 200.

None of these is the URL being wrong. All of them currently say the settings or the PAT are.

---

## 4. Root-cause chains

```
PROBLEM: An administrator configuring additional fields on Azure DevOps is told the connection
         settings, or the PAT, are wrong — for a failure to read the instance's field list, on a
         connection that had answered a query moments earlier.
```

### Chain A — the field-list refusal is blamed on the connection settings

```
WHY 1A: The connection screen says "Azure DevOps rejected the connection settings." with a technical
        detail that is frequently the single word "Forbidden".
  [Evidence: :241-248 — Failure("connection_failed", "Azure DevOps rejected the connection
   settings.", ex.Message), no fieldName. §3a measures ex.Message == "Forbidden" for a 403 whose
   body is not ADO's JSON error contract. Rendered at ModifyConnectionSettings.tsx:715-730 as
   message + caption.]

  WHY 2A: Every HTTP status the fields endpoint can answer with arrives as a VssServiceException or
          one of its subclasses, and that one catch handles all of them identically.
    [Evidence: §3a — 400/401/403/404/429/500/503 and a 200 with an HTML body all produce
     VssServiceException or VssServiceResponseException; §3b — VssServiceResponseException derives
     from VssServiceException, so :241 matches every one.]

    WHY 3A: The fields read happens inside the same try block whose handlers exist to describe a
            connection that could not be established at all.
      [Evidence: try opens :213; :215 builds the client, :217 VerifyConnection, :219 the fields
       read. The five catches at :231/:241/:250/:260/:270 are the connection-scope vocabulary:
       auth, settings, URL, malformed URL, unknown. There is no catch between :219 and them.]

      WHY 4A: The connector has no exception type meaning "Azure DevOps answered, and refused a
              read" — the abstract carrier exists and Azure DevOps is the one connector with no
              subclass of it.
        [Evidence: WorkTrackingReadException.cs:11-15 is abstract and carries a
         ConnectionValidationResult Verdict; WizardsController.cs:55 already unwraps it. Jira has
         JiraReadException (JiraReadException.cs:13) and ServiceNow has ServiceNowReadException
         (ServiceNowReadException.cs:13). A grep for ": WorkTrackingReadException" returns exactly
         those two. The ADO connector references neither the base nor any subclass.]

        WHY 5A: The ADO connector's error model answers "could Azure DevOps be reached, and did it
                accept the settings" — a model written for the connection handshake and then reused,
                unchanged, for a read that happens after the handshake has already passed.
          [Evidence: the five catches at :231-274 are all phrased about the connection, and the only
           verdict in the method that is about a *read* — "Some additional fields could not be
           found" at :222-226, the one verdict that names "Additional Fields" — is reachable only on
           the success path. A read that fails has no vocabulary at all.]

-> ROOT CAUSE A: A refused field-metadata read is indistinguishable, by exception type, from Azure
   DevOps rejecting the connection settings — so the handler that exists for the second claims the
   first, and does so about a connection that had demonstrably just worked.
```

### Chain B — a 401 on the fields call is blamed on the PAT

```
WHY 1B: For a 401 carrying a WWW-Authenticate challenge, the screen says "Authentication failed for
        Azure DevOps." and names the PersonalAccessToken input — about a token that authenticated
        the WIQL at :217 seconds earlier.
  [Evidence: :231-239 — Failure("authentication_failed", "Authentication failed for Azure DevOps.",
   "Check your Personal Access Token permissions and make sure it is still valid.",
   AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken). §3a: 401 + "WWW-Authenticate: Basic"
   produces VssUnauthorizedException("VS30063: You are not authorized to access …").]

  WHY 2B: VssHttpMessageHandler turns a challenged 401 into VssUnauthorizedException after silently
          re-issuing the request; without a challenge it passes the 401 through to
          VssHttpClientBase, which raises VssServiceResponseException instead.
    [Evidence: §3a — the same 401 body produces VssUnauthorizedException in the credentialed client
     and VssServiceResponseException in the raw-pipeline client; the request log shows 4 and 9
     attempts respectively for the Basic and Negotiate challenges, versus 1 without a challenge.]

    WHY 3B: The catch cannot tell which of the two calls in the try raised it. For :217 the verdict
            is right; for :219 it is wrong; the same handler serves both.
      [Evidence: :217 and :219 are consecutive statements in one try opened at :213.]

      WHY 4B: The advice attached to the verdict is actionable and wrong. "Check your Personal
              Access Token permissions and make sure it is still valid" sends the administrator to
              reissue a credential that has just been proven valid.
        [Evidence: :238 is that string; :217 ran a WIQL successfully on the same client before :219
         was reached. A PAT that can run QueryByWiql is not expired.]

        WHY 5B: Catch-by-exception-type is being used as a proxy for catch-by-operation, and for a
                try block containing two operations with different meanings that proxy is simply not
                available — the SDK reports "who refused" and never "what was being read".
          [Evidence: none of VssUnauthorizedException, VssServiceException or
           VssServiceResponseException carries the request URI; §3b lists their whole public
           surface, which is HttpStatusCode and Content on the response subclass and nothing else.]

-> ROOT CAUSE B: The validation groups two operations with different failure meanings into one try,
   and the only discriminator available at the catch is the exception type — which describes the
   remote system, not the request. The 401 case turns that into an instruction to replace a working
   credential.
```

### Chain C — portfolio validation reaches the same read and says even less; team validation does not reach it at all

```
WHY 1C: Portfolio settings -> Validate reports a field-list refusal as "Portfolio validation failed
        due to an unexpected error." plus the same bare status word.
  [Evidence: :337-344 — catch (Exception) -> Failure("validation_failed", "Portfolio validation
   failed due to an unexpected error.", exception.Message).]

  WHY 2C: ValidatePortfolioSettings reaches the field lookup; ValidateTeamSettings does not. The
          symmetry the Jira twin had does NOT hold here.
    [Evidence: ValidatePortfolioSettings :313 -> FetchAdoWorkItemsByQuery :321 -> :705 ->
     ThePayloadsAndFieldsFor :735 -> GetCustomFieldReferences :743 -> :684.
     ValidateTeamSettings :278 -> GetWorkItemReferencesByQuery :286 -> :832, which only issues a
     WIQL. There is no field read on the team path.]

    WHY 3C: On the portfolio path the read is conditional, so the failure is intermittent in a way
            that looks unrelated to the field configuration.
      [Evidence: :738 — ThePayloadsAndFieldsFor returns early when workItemIds.Count == 0. A
       portfolio whose query matches nothing returns "no_features_found" at :328-333 and never
       touches the field list; one that matches something goes to :743 and can fail there.]

      WHY 4C: The catch-all was written for "something unexpected", and a remote refusal is not
              unexpected — it is the single most likely thing to go wrong on a validation that makes
              three network calls.
        [Evidence: :337 is a bare catch (Exception) with a fixed sentence. The same shape is at
         :303-310 for teams. ServiceNow is the counter-example: it has no catch-all on the
         equivalent method because its reads throw a verdict-carrying ServiceNowReadException.]

        WHY 5C: The ADO connector has one failure vocabulary per *screen*, not one per *cause*, so
                every cause on a screen collapses to that screen's single sentence.
          [Evidence: three screens, three sentences — "Connection validation failed due to an
           unexpected error." :274, "Team validation failed due to an unexpected error." :305,
           "Portfolio validation failed due to an unexpected error." :339. None of the three names
           a cause; two of them append exception.Message, which §3a shows is often one word.]

-> ROOT CAUSE C: The portfolio path reaches the same unprotected read through a different caller and
   falls into a per-screen catch-all, so the same defect wears a third message. And the team path,
   which the Jira twin had to fix, does not reach the read at all on Azure DevOps — the symmetry
   must not be assumed.
```

### Chain D — why the bug was filed as a URL misattribution, and why that inference was reasonable

*This chain explains the **report**, not the defect. It is kept in the tree rather than folded into
prevention because its WHY 3D is a live measurement about `ExecuteWithRetry` that Chain F depends on,
and because its root cause is the one with a prevention action attached (§9.1).*

```
WHY 1D: The seed reasoned both ways and landed on "a 403/404 reaches :250", which measurement
        refutes.
  [Evidence: §3a — no status produces an HttpRequestException.]

  WHY 2D: The evidence for that reading is in the file: :808 catches HttpRequestException filtered on
          ex.StatusCode being TooManyRequests or ServiceUnavailable. A reader is entitled to conclude
          that the SDK produces such exceptions, because otherwise why write the line.
    [Evidence: :808, inside ExecuteWithRetry :792.]

    WHY 3D: That line is dead. Every action ExecuteWithRetry is ever handed is an SDK client call,
            and no SDK client call produces an HttpRequestException with a StatusCode.
      [Evidence: ExecuteWithRetry has exactly one caller, ExecuteWithThrottle :777; its call sites
       are :169, :184, :493, :584, :610, :835, :886, :947, :1022 — all witClient.* / workClient.* /
       projectClient.* calls. The connector contains no HttpClient of its own and no
       EnsureSuccessStatusCode: a grep for both over the 1378-line file returns only
       ConcurrentDictionary<string, IVssHttpClient> ClientCache at :76.]

      WHY 4D: Nothing distinguishes a dead catch from a live one at read time, and no test covers it.
        [Evidence: the `when` clause cannot be exercised by AzureDevOpsOrganisation, whose refusal
         switches all throw VssServiceException (TestHelpers/AzureDevOpsOrganisation.cs:64, :86,
         :104). Nothing else in the suite constructs an HttpRequestException for this connector.]

        WHY 5D: A defensive catch written against a plausible-sounding exception type, never
                exercised, becomes documentation of a behaviour the dependency does not have — and
                is then read as evidence by the next investigator.
          [Evidence: this bug report. The chain is: line written -> never fails -> read as a
           specification -> a second defect filed against the behaviour it implies.]

-> ROOT CAUSE D: An unexercised defensive catch is indistinguishable from a verified one, and this
   one misdescribed the SDK's contract for long enough to be cited as evidence in a bug report.
```

### Chain E — `SingleOrDefault` at `:690`, and the silent `?? string.Empty` beside it

```
WHY 1E: :690-692 selects the matching field with SingleOrDefault over a predicate that matches on
        EITHER Name or ReferenceName, then falls back to string.Empty.
  [Evidence: :690-692 — availableFields.SingleOrDefault(f =>
   string.Equals(f.Name, reference, OrdinalIgnoreCase) ||
   string.Equals(f.ReferenceName, reference, OrdinalIgnoreCase))?.ReferenceName ?? string.Empty]

  WHY 2E: Two matches throw InvalidOperationException. Name-vs-Name and RefName-vs-RefName
          collisions are impossible; the only route is one field's Name equalling another field's
          ReferenceName.
    [Evidence: Microsoft Learn, "Naming restrictions and conventions" — field names "Must be unique
     within the organization or project collection"; reference names are "globally unique".]

    WHY 3E: That cross-collision is blocked by a character rule, because every reference name
            contains a period and a field name may not.
      [Evidence: same page, Field names / Special characters: "Must not contain any of the following
       characters: . , ; ' : ~ \ / * ? " & % $ ! + = ( ) [ ] { } < > - |". Reference names "Must
       contain at least one period".]

      WHY 4E: The rule is a UI rule, and the documentation says so explicitly.
        [Evidence: same page, General considerations: "When you use the Azure DevOps APIs rather
         than the user interface (UI), you can directly specify a name that might include characters
         restricted in the UI." So a field created through the REST API, or through an on-prem XML
         process definition, can carry a period in its display name.]

        WHY 5E: The expression encodes a uniqueness guarantee the platform only enforces at one of
                its two front doors, and pays for a violation with an unhandled exception rather
                than a verdict — while the sibling `?? string.Empty` pays for the opposite case, a
                field that matches nothing, with silence.
          [Evidence: the throw reaches :270 on the validation path ("Connection validation failed
           due to an unexpected error.", no details) and is unhandled on the refresh path, where
           GetCustomFieldReferences is called from ThePayloadsAndFieldsFor :743. The ?? string.Empty
           makes an unresolvable additional field read as "no value" on every refresh, with no
           warning logged and nothing on any screen — the connection screen's
           "Some additional fields could not be found" verdict (:222-226) never runs during a
           refresh.]

-> ROOT CAUSE E: The field-matching expression is strict where the platform is lenient (SingleOrDefault
   against a uniqueness rule enforced only in the UI) and lenient where the user needs to be told
   (?? string.Empty for a field that resolved to nothing). Both halves are latent today on Azure
   DevOps Services; the second is reachable on any deployment and is already silently in effect.
```

### Chain F — the retry that cannot fire, for the two statuses it names

```
WHY 1F: A 429 or 503 from any throttled call is retried only if the response body happens to be
        Azure DevOps's own JSON error contract AND its sentence contains particular English.
  [Evidence: ExecuteWithRetry :792-810 has exactly two catches. :803 catches VssServiceException
   filtered by IsRateLimited; :808 catches HttpRequestException filtered on StatusCode.]

  WHY 2F: :808 never matches, because the SDK never raises HttpRequestException with a StatusCode.
    [Evidence: §3a/§3b, and Chain D WHY 3D for the call-site sweep.]

    WHY 3F: So everything depends on :785-790, which is a substring match on the exception message.
      [Evidence: IsRateLimited :785-790 — msg.Contains("Rate limits") || msg.Contains("exceeding
       usage of resource 'Concurrency'").]

      WHY 4F: A 429 that does not carry ADO's JSON error body arrives as VssServiceResponseException
              with Message == "TooManyRequests", which matches neither substring — so it is not
              retried, and is reported as a failure.
        [Evidence: §3a, the 429 row: VssServiceResponseException "TooManyRequests", one request
         issued, no retry. A front-door or proxy 429 has no ADO JSON body.]

        WHY 5F: Retry eligibility is decided on the prose of a message rather than on the status code
                the response subclass carries, and the one branch that does read a status code reads
                it off a type that never arrives.
          [Evidence: VssServiceResponseException.HttpStatusCode exists (§3b) and is never consulted
           anywhere in the file — grep for HttpStatusCode returns only :808's `when` clause.]

-> ROOT CAUSE F: The throttling retry is keyed on message text plus a dead status check, so the
   subset of 429/503 responses that do not carry Azure DevOps's JSON error body is never retried at
   all. Separate bug; same root shape as A (type-based reasoning about a remote refusal).
```

### Cross-validation

- **A, B and C are the same root defect at three call sites**, discriminated by the 401 challenge
  header (B) and by which entry point reached the read (C). They do not contradict: A and B are the
  same `try` with different exception types; C is a different method reaching the same `:684`.
- **D explains the bug report, not the bug.** It is consistent with A: both say the connector reasons
  about remote refusals via exception type and that the type does not carry what the reasoning needs.
- **E is independent of A–D** — it fires on a response that succeeded. It shares the consequence
  (a detail-free verdict at `:270`, a silent refresh) but not the cause.
- **F is independent** and would be unaffected by the fix for A–C. It is listed because it is a
  plausible *producer* of the 429 that A then mislabels, and because it is measured dead here.
- **Every symptom in §0 is explained**: "rejected the connection settings" by A; the PAT verdict by
  B; the portfolio wording by C; "no details at all" by E's throw and by a malformed body (§3a).
  No symptom is left without a chain, and no chain predicts a symptom that was not observed.
- **Completeness check.** Three things were looked for and not found: (i) a raw `HttpClient` path in
  the connector that could legitimately reach `:250` — none exists (Chain D WHY 3D); (ii) a
  `fieldName`-driven input highlight that would make the wrong `fieldName` visible — none exists
  (§2); (iii) an OAuth token-refresh failure inside the `try` that could reach `:250` — it cannot,
  because `OAuthService.PerformRefreshAsync` converts every provider failure to
  `OAuthRefreshFailedException` (`OAuthService.cs:256-259`, `OAuthRefreshFailedException.cs:3`),
  which lands at `:270` with no details. That is a **fourth** detail-free path and is recorded in §5.

---

## 5. Sweep: the other silent degradations an administrator can trigger

| Site | What it does | Reached from | Verdict |
|---|---|---|---|
| `:690-692` `?? string.Empty` | An additional field that resolves to nothing becomes "no value" on **every refresh**, silently | every team and portfolio refresh via `ThePayloadsAndFieldsFor :743` | **Same change** — it is the other half of the expression being edited. At minimum log it. |
| `:690` `SingleOrDefault` | Throws on a cross-kind name collision; detail-free verdict or a dead refresh | as above | **Same change, one word** (Chain E) — the line is already being touched. |
| `:347-362` `GetBoards` → `return []` | Any failure listing boards yields an empty picker in the team/portfolio wizard, with no message | team & portfolio create/settings wizards | **Separate item.** Same shape as the Jira twin's §5b. |
| `:365-406` `GetBoardInformation` → `return emptyInfo` | A board whose settings cannot be read yields blank query/types/states, which the wizard then saves | same | **Separate item**, and the more dangerous of the two — it persists a blank configuration. |
| `:270-274` reached by an OAuth refresh failure | `OAuthRefreshFailedException` / `OAuthCredentialNotValidException` / `OAuthRefreshTimeoutException` all produce "Connection validation failed due to an unexpected error." with **no detail** | Connection → Validate on an OAuth-keyed ADO connection | **Separate item.** `OAuthService` already marks the credential `RefreshFailed` and logs precisely; none of that reaches the screen. |
| `:792-810` `ExecuteWithRetry` | 429/503 without an ADO JSON body are never retried (Chain F) | every refresh | **Separate item.** Fix by reading `VssServiceResponseException.HttpStatusCode` instead of the message text. |
| `:303-310` / `:337-344` catch-alls | Collapse every non-configuration cause into one sentence | Team / Portfolio → Validate | **Partially in this change** — adding the typed catch above them (F3) is enough for the field-list cause; the rest is the same debt Jira still carries. |

Not found in this connector, and worth recording as negatives: no `EnsureSuccessStatusCode()`, no
raw `HttpClient`, no `return default` on a read path other than the two board methods above.

**Why the "separate item" rows really are separable** (they are not being deferred for convenience):
each touches a different method with a different caller set, and none shares a line with the fix.
`GetBoards :347` and `GetBoardInformation :365` are reached only from the board wizard and never from
`ValidateConnection`; the OAuth path enters at `:215`, **before** the fields read, so its verdict is
decided by a catch this change does not add or reorder; `ExecuteWithRetry :792` is never on the
fields call's stack at all (that is Chain D's finding). The fix can land first, and each separate
item can land later, in either order, with no merge interaction.

---

## 6. Answers to the four explicit questions

**1. `ValidateTeamSettings` / `ValidatePortfolioSettings`.** Asymmetric, unlike Jira.
`ValidatePortfolioSettings` **does** reach the field lookup (`:321` → `:705` → `:735` → `:743` →
`:684`), but only when the query matched at least one work item (`:738`). `ValidateTeamSettings`
**does not** — it calls `GetWorkItemReferencesByQuery` (`:286` → `:832`), which issues a WIQL and
nothing else. So the portfolio method needs the same treatment; the team method does not need it for
correctness. Add the typed catch to both anyway — it is one line, it costs nothing, and `:286`'s
call chain is one refactor away from acquiring a field read.

**2. `SingleOrDefault` at `:690`.** Latent, and your live measurement is consistent with the
documented rules rather than a coincidence: field names are unique per organisation/collection and
reference names are globally unique, so the only way to get two matches is a field whose *display
name* equals another field's *reference name* — which the UI forbids, because a field name may not
contain a period and every reference name must. It is reachable only on a collection whose fields
were created outside the UI (REST API, or an on-prem XML process definition), which Microsoft's own
docs explicitly say bypasses the character restrictions. **Recommendation: change it in this
change**, because the fix is one word on a line the change is already editing, and the failure mode
is an unhandled `InvalidOperationException` that produces a detail-free verdict on the connection
screen and kills a refresh outright. Not worth a separate item; not worth a separate test either
beyond the one asserting a deterministic pick.

**3. Blast radius within ADO.** See §5. **Must fix now:** `:684` (Chains A/B/C), and the `:690`
expression while it is open. **Same shape, separate item:** `GetBoards`/`GetBoardInformation`
silent empties, the OAuth-failure path into `:270`, and `ExecuteWithRetry`'s dead status check.
None of the separate items is a prerequisite for this fix, and none is made worse by it.

**4. Testability — confirmed, offline, and the seam already exists.** `WorkItemTrackingHttpClient`
has a public `(Uri, HttpMessageHandler, bool)` constructor (verified by reflection), so a
`DelegatingHandler` route is available — **but it is not needed.** The repository already has the
seam: `RecordedAzureDevOpsConnector` (`Lighthouse.Backend.Tests/TestHelpers/AzureDevOpsOrganisation.cs:21-27`)
subclasses the real connector and overrides `GetWorkItemTrackingHttpClientAsync` (`:1299`, `internal
virtual`) to hand back a Moq'd client, and `GetWorkItemFieldsAsync` is already stubbed at
`AzureDevOpsOrganisation.cs:94-108` with a `RejectTheFieldLookup` switch that throws
`VssServiceException`. The one extension needed is to let that switch throw a **chosen** exception —
`new VssServiceResponseException(HttpStatusCode.Forbidden, …)` for the status-specific cases and
`new VssUnauthorizedException(…)` for Chain B — so the test can pick which of `:231`/`:241` it is
exercising. `ValidateConnection` runs fine through that seam; the only other requirement is that the
connection option `Url` parses as an absolute URI so the pre-flight at `:204` does not short-circuit.
`AzureDevOpsFetchRefusalTest.cs` is the file to extend; it is **not** in a live category.

---

## 7. Proposed fix — minimal, in the existing idiom

### Which existing type, and why

| Candidate | Fits? |
|---|---|
| `HttpRequestException` | **No.** It is the connector's signal for "Azure DevOps could not be reached", handled at `:250` with the URL verdict. Reusing it would recreate the bug the report describes — and would this time make it real. |
| `VssServiceException` / a subclass | **No.** Throwing an SDK type to be caught by `:241` is what already happens; that is the defect. |
| `WorkTrackingReadException` | **Yes, as the base.** Abstract, carries `ConnectionValidationResult Verdict` (`WorkTrackingReadException.cs:11-15`), already unwrapped by `WizardsController.cs:55`. Azure DevOps is the only connector with a `ValidateConnection` that reads metadata and no subclass of it. |
| **A new `AzureDevOpsReadException : WorkTrackingReadException`** | **Yes — this is the one.** Exactly mirrors `JiraReadException` (`JiraReadException.cs:13`) and `ServiceNowReadException`. Critically it is **not** an `HttpRequestException` and not a `Vss*` type, so no existing catch can swallow it. One new file, one factory. |
| `ConnectionValidationResult` | **Yes, as the payload.** `Failure(code, message, technicalDetails, fieldName)`, `ConnectionValidationResult.cs:32`. No new result type. |

**Why `JiraReadException` deliberately does not extend `HttpRequestException`** (asked in the brief,
and it is the same reason here): `JiraReadException.cs:6-11` records it. Its sibling
`JiraQueryRejectedException` *does* extend `HttpRequestException` on purpose, because the Jira sync
path reads that type as "Jira could not be asked". If `JiraReadException` shared the base, the URL
catch would swallow it and the URL blame would survive the fix — the fix is a **type** change, not a
reordered catch. The ADO equivalent is exact: if `AzureDevOpsReadException` derived from
`VssServiceException`, `:241` would keep it and nothing would change.

### Dependency order — F2 must not ship without F3

`AzureDevOpsReadException` is not a `Vss*` type and not an `HttpRequestException`, so until F3's
catches exist it falls straight through `:231`–`:260` into the bare catch at `:270` and the message
gets **strictly worse** (no technical details at all). Ship F1+F2+F3+F4 as one commit. F5 is
independent.

```
F1 (the type) -> F2 (throw it) -> F3 (catch it) -> F4 (the two test expectations)   one commit
F5 (SingleOrDefault / ?? string.Empty)                                              independent
```

### F1 — `AzureDevOpsReadException`

New file beside the connector,
`…/WorkTrackingConnectors/AzureDevOps/AzureDevOpsReadException.cs`:

```csharp
using System.Net;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.VisualStudio.Services.WebApi;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// A read Azure DevOps would not answer, carrying the verdict the administrator is shown. It is
    /// deliberately neither a Vss* exception nor an HttpRequestException: those are the connector's
    /// signals for "Azure DevOps rejected the connection settings" and "Azure DevOps could not be
    /// reached", and a refused read is neither of those - the same connection answered a query
    /// moments earlier.
    /// </summary>
    public class AzureDevOpsReadException : WorkTrackingReadException
    {
        /// <summary>
        /// The input on the connection screen that additional fields are typed into. Every verdict
        /// about those fields has to name it identically.
        /// </summary>
        internal const string AdditionalFieldsFieldName = "Additional Fields";

        public AzureDevOpsReadException(ConnectionValidationResult verdict)
            : base(verdict)
        {
        }

        /// <summary>
        /// Azure DevOps would not hand over the list of fields in the organisation, so no additional
        /// field can be checked against it.
        /// </summary>
        public static AzureDevOpsReadException FieldListRefused(Exception refusal)
            => new(ConnectionValidationResult.Failure(
                "field_list_unreadable",
                "Lighthouse could not read the list of fields in this Azure DevOps organisation, so "
                + "it cannot check the additional fields against it. "
                + $"Azure DevOps answered: {WhatAdoSaid(refusal)} "
                + "The connection itself is good - a work item query on it succeeded moments before "
                + "this. The account this connection signs in with needs to be able to read work "
                + "items in at least one project for Azure DevOps to return the field list; a proxy "
                + "in front of an on-premises server can also refuse this response, which is much "
                + "larger than the others Lighthouse asks for.",
                $"GET _apis/wit/fields answered {WhatAdoSaid(refusal)}",
                AdditionalFieldsFieldName));

        private static string WhatAdoSaid(Exception refusal)
            => refusal is VssServiceResponseException response
                ? $"{(int)response.HttpStatusCode} ({response.HttpStatusCode}). {response.Message}"
                : refusal.Message;
    }
}
```

`WhatAdoSaid` is the one piece with no Jira counterpart and it earns its place: §3a shows that for a
non-JSON body `Message` is the bare status **name**, and `VssServiceResponseException.HttpStatusCode`
is the only place the **number** survives. Without it the technical detail is the word `Forbidden`.

### F2 — `GetCustomFieldReferences :681`: name what failed

`:684` becomes:

```csharp
List<WorkItemField2> availableFields;
try
{
    availableFields = await witClient.GetWorkItemFieldsAsync(cancellationToken: cancellationToken);
}
catch (VssException refusal)
{
    logger.LogWarning(refusal, "Azure DevOps would not return the field list for this organisation");

    throw AzureDevOpsReadException.FieldListRefused(refusal);
}
```

Notes:

- `catch (VssException)` — not `VssServiceException` — is what covers both `:231`'s and `:241`'s
  cases, because §3b shows `VssUnauthorizedException` does not derive from `VssServiceException`.
- **`HttpRequestException` is deliberately not caught here.** After this change it is the only thing
  the fields call can still raise, it means a genuine transport failure, and `:250`'s message is
  then correct. This matches the decision the Jira twin recorded.
- `GetCustomFieldReferences` (`:681`) and `GetMissingAdditionalFields` (`:667`) must both drop
  `static` so the log line can run. The ripple stops there: `GetMissingAdditionalFields`'s only
  caller is `ValidateConnection :219`, and `ThePayloadsAndFieldsFor :735` is already an instance
  method. CA1822 will not fire on either — one uses `logger`, the other calls an instance member.
- Choosing `:684` rather than the `:219` call site is what makes the **portfolio** path (Chain C)
  and the refresh path benefit from the same edit.

### F3 — three catches

As the **first** catch in each of `ValidateConnection` (before `:231`), `ValidateTeamSettings`
(before `:303`) and `ValidatePortfolioSettings` (before `:337`):

```csharp
catch (WorkTrackingReadException refusal)
{
    return refusal.Verdict;
}
```

Verbatim the shipped Jira idiom (`JiraWorkTrackingConnector.cs:407-410`, `:1075-1078`, `:1116-1119`).
Catching the **base** rather than `AzureDevOpsReadException` matches Jira and keeps
`WizardsController.cs:55` the single place that knows about the hierarchy.

### F4 — the two test expectations that this changes

`AzureDevOpsFetchRefusalTest.cs:46` and `:82` currently assert
`Throws.TypeOf<VssServiceException>()` for `GetWorkItemsForTeam_RefusesWhenTheFieldLookupFails` and
its by-reference-id twin. Both still refuse — which is the property those tests exist to protect —
but the type becomes `AzureDevOpsReadException`. **Update the two expectations; do not weaken them
to `Throws.Exception`.** The class comment at `:19-20` ("the tracker's own failure is what has to
arrive, unwrapped") stays honest because `WhatAdoSaid` puts Azure DevOps's own sentence in the
verdict and `logger.LogWarning(refusal, …)` puts the whole exception in the log.

This is the one place the change can surprise: the field-lookup refusal is on the **refresh** path as
well as the validation path, so it is load-bearing for Bug #5756's guarantee. It keeps working —
`AzureDevOpsReadException` still propagates out of `GetWorkItemsForTeam` — but the type on the way
out is new.

**The new test this change owes, and its shape.** One offline test per verdict branch, through
`RecordedAzureDevOpsConnector`, asserting **positively**:

```csharp
Assert.That(result.Code, Is.EqualTo("field_list_unreadable"));
Assert.That(result.Message, Does.Contain("could not read the list of fields"));
Assert.That(result.FieldName, Is.EqualTo("Additional Fields"));
```

Never `Does.Not.Contain("provided URL")` on its own. That assertion passes against `""`, and it is
how #5973 and #6012 both lost their messages to a mutant
(`docs/evolution/2026-09-16-fix-jira-field-lookup-error-message.md`, "Lessons"). At least one
assertion must fail when the message is blanked.

**Status of §7: proposal, not executed.** Nothing in this section has been compiled or run — this is
an investigation and it did not modify production code. The delivery step owes a build, the offline
tests above, the two F4 updates, and a Stryker pass scoped to the introduced lines.

### F5 — `:690-692`, independent

```csharp
var fieldReference = (availableFields.Find(f =>
        string.Equals(f.ReferenceName, additionalFieldDefinition.Reference, StringComparison.OrdinalIgnoreCase))
    ?? availableFields.Find(f =>
        string.Equals(f.Name, additionalFieldDefinition.Reference, StringComparison.OrdinalIgnoreCase)))
    ?.ReferenceName ?? string.Empty;
```

Reference name wins, then display name, then nothing — deterministic where `SingleOrDefault` threw
and `FirstOrDefault` would have been arbitrary. `List<T>.Find` rather than `FirstOrDefault` because
`GetWorkItemFieldsAsync` returns a `List<WorkItemField2>` and `Find` on a `List<T>` is the house
convention (`ServiceNowReadScope.cs:52`, `ServiceNowWorkTrackingConnector.cs:1269`) as well as what
Sonar's LINQ-on-`List` rule asks for.

### What is NOT proposed

- No change to `:250-258`. It is correct for what can still reach it.
- No new `ConnectionValidationResult` shape, no frontend change. Wiring `fieldName` to an input
  highlight is a real improvement (§2) and a separate item for all five connectors.
- No change to `ExecuteWithRetry`, `GetBoards`, `GetBoardInformation` or the OAuth path (§5).

---

## 8. Risk assessment

**Tests pinning the current wording — searched, and the news is good.** A repo-wide grep for the
five ADO verdict strings returns: `"Could not reach Azure DevOps with the provided URL."` — the
connector only, **no test**. `"Azure DevOps rejected the connection settings."` — the connector only,
**no test**. `"Connection validation failed due to an unexpected error."` — the ADO and Jira
connectors, no test. `"Authentication failed for Azure DevOps."` — pinned in **four** places
(`WorkTrackingSystemConnectionsControllerTest.cs:188,214`,
`CreateConnectionWizard.test.tsx:597,619`, `ModifyConnectionSettings.test.tsx:542,561`), all of which
construct the verdict themselves rather than driving the connector, so none is affected. The only
test changes required are the two in F4.

| Risk | Likelihood | Mitigation |
|---|---|---|
| F2 without F3 ships and the message gets worse | Low, if the order above is followed | One commit. The Jira twin hit exactly this and wrote the order down first. |
| A live `AdoIntegration` test pins the swallow | **Low, but checked by hand, because `docs/ci-learnings.md:429-432` records precisely this happening on this connector for Bug #5756**: a catch removal went green locally across 4734 tests and broke a live test the local filter excludes | Read, not run. `AzureDevOpsWorkTrackingConnectorTest.cs` (`[Category("Integration")]` + `[Category("AdoIntegration")]`, `:15-16`) has three validation tests. `ValidateConnection_GivenValidSettings_ReturnsTrue :685` asserts the **success** verdict. `ValidateConnection_GivenInvalidSettings_ReturnsFalse :709-732` asserts only `IsValid == false` and `Message` non-empty — and its cases fail at `VerifyConnection :217` (bad PAT → 401; `https://not.valid` → DNS), not at the fields call. `ValidateConnection_GivenAdditionalFields_ReturnsTrueOnlyIfFieldsExist :734-774` reads the field list successfully against a real organisation, so it keeps returning `additional_fields_invalid`. **None pins a failure sentence.** CI will run all three because the diff touches connector paths; that is the desired live verification, not a risk. |
| Stryker reports the new catch as `NoCoverage` and it reads as unpinned | Likely | Same ledger entry: `NoCoverage` means "no test in this run's filter", and the mutation config excludes `*Integration`. Judge it against the F4 tests, not the label. |
| Mutation blanks the new message and the suite stays green | **High — this is the twin's headline lesson.** #5973 and #6012 both lost their messages to `Does.Not.Contain(...)`-shaped assertions, which pass against `""` | Pin at least one **positive** assertion against the literal: assert the verdict `Code` is `field_list_unreadable` **and** that `Message` contains a distinctive clause. Never assert only the absence of the old wording. |
| `TreatWarningsAsErrors` breaks the build | Medium | `logger` use in a previously-`static` method resolves CA1822; `S2139` is satisfied because this logs **and transforms** rather than logs-and-rethrows; `S6667` is satisfied because the exception is the **first** argument to `LogWarning`; `S4136` does not apply (no overloads added). |
| SonarCloud `new_violations = 0` | Medium | **S1192** is the live one: `"Additional Fields"` appears at `:226` today and the new const would be a second occurrence in the same file — `ci-learnings.md` records `JiraWorkTrackingConnector` tripping S1192 and warns that "adding a single line to an existing file is enough to trip it". Reference `AzureDevOpsReadException.AdditionalFieldsFieldName` from `:226` too, exactly as the Jira fix did. Also run the analyzer sweep for **CA1859** on any new non-public helper and **NUnit2045 / CA1861** on the touched test file — all three are INFO locally and gate-fatal. |
| The measurement in §3 goes stale | Low | It is pinned to `Microsoft.TeamFoundationServer.Client` **20.256.2**. Re-run the probe if that version changes; the probe is self-contained and needs no credentials. |

**Known-environmental failures in this worktree, not regressions:** 2 Licensing tests on the
gitignored `valid_not_expired_license.json`, and
`ServiceContainer_BuildsWithoutScopeViolations_WhenValidateScopesIsEnforced` failing in `Dispose` on
a temp SQLite file.

---

## 9. Prevention

1. **A catch filtered on a property of a dependency's exception needs a test that produces that
   exception, or it is a claim about the dependency with nothing behind it.** `:808` is the evidence:
   an unexercised `when (ex.StatusCode is …)` misdescribed the SDK for long enough to be quoted as
   proof in a bug report. Cheapest prevention: when adding such a filter, add the offline probe that
   shows the dependency really raises it.
2. **When a defect is ported to a sibling connector, verify the call graph rather than the shape.**
   Jira's `ValidateTeamSettings` reached the field list; Azure DevOps's does not, and its
   `ValidatePortfolioSettings` does — but only when the query matched something. Symmetry of
   *appearance* here was not symmetry of *reachability*.
3. **A verdict that names an input should name one the user can act on, and the code should be able
   to say why.** ServiceNow already encodes this (`ServiceNowTeamQueryVerdict.cs:206-213` re-points a
   connection-ladder rung at the field the user typed, with a comment stating the rule). Azure DevOps
   `:238` does the opposite: it points at the PAT because of the exception type, for an operation
   that had already authenticated.
4. **Mutation-test the message, positively.** Two message losses in two bugs on the same defect
   family (`docs/evolution/2026-09-16-fix-jira-field-lookup-error-message.md`, "Lessons"). A negative
   assertion cannot see an empty string.
5. **After this lands, three of the five connectors have a `WorkTrackingReadException` subclass.**
   Linear and CSV do not validate additional fields at all, so they have nothing to refuse — but the
   asymmetry is worth a line in the architecture brief, because the next person porting this fix will
   look for a fourth.
