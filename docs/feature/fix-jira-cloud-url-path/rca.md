# RCA — Bug #6401: Jira Cloud URL with `/jira/` path breaks connection setup

Investigator: Rex (nw-troubleshooter), 2026-10-23. Code at `ead31f93f`.
Paths below are relative to `/storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth`.
`JWC` = `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs`.

## Problem statement (scope)

A Jira Cloud site URL entered as `https://<site>.atlassian.net/jira/` (the form the Jira Cloud address bar now shows):

- S1 (OAuth): setup reports an "invalid" URL.
- S2 (API token): `/validate` returns `isValid: true`, but after Create the user is back at onboarding step 0 with no connection listed.

Also in scope: Gap 1 (the wizard swallows a failed save) and Gap 2 (Overview shows a refused 403 connection list as "no connections").
Out of scope: Jira Scoped Token, ADO, Linear, ServiceNow connectors.

## Evidence collected

| # | Evidence | Source |
|---|---|---|
| E1 | `GET https://letpeoplework.atlassian.net/_edge/tenant_info` returns `200 application/json {"cloudId":…}` | live curl, unauthenticated |
| E2 | `GET https://letpeoplework.atlassian.net/jira/_edge/tenant_info` returns **`200 text/html`** (the Jira SPA shell, not JSON) | live curl |
| E3 | `GET …/jira/rest/api/2/serverInfo` returns `200 text/html`; `GET …/rest/api/2/serverInfo` returns JSON with `deploymentType: Cloud` | live curl |
| E4 | `new Uri(new Uri("https://x.atlassian.net/jira"), "rest/api/2/myself")` gives `https://x.atlassian.net/rest/api/2/myself`. The same holds for `https://host/jira` (gives `https://host/rest/...`). Only `https://host/jira/` keeps the path. | `dotnet run` scratch program (RFC 3986 merge, the same thing HttpClient does with `BaseAddress`) |
| E5 | `…/jira/browse/KEY` returns 200 (the browse links keep working) | live curl |

## Branch A: API token, `isValid: true` but no connection after Create (S2)

```
WHY 1A: /validate says valid although the URL carries /jira.
  [JWC:2817 base = Url.TrimEnd('/'); JWC:2862 BaseAddress = new Uri(base); JWC:422 GetAsync("rest/api/2/myself")
   (JiraRestRoutes.cs:23 is a relative route). By E4 the last path segment "jira" is replaced,
   so requests hit the site root and succeed.]
 WHY 2A: Validation succeeds by accident. The connector never inspects the path, and the
   Uri merge silently discards it. [JWC:409-417 checks only Uri.TryCreate(Absolute).]
  WHY 3A: Create stores the URL verbatim and does not check it.
    [WorkTrackingSystemConnectionsController.cs:89-122: no URL check; CreateConnectionFromDto :232-267 copies options as-is.]
    So the URL path alone does NOT make the API-token Create fail. The stored connection
    still works: API calls hit the root (E4), and browse links /jira/browse/KEY resolve (E5, JWC:1774-1775).
   WHY 4A: The "no connection" symptom therefore comes from the UI's error handling, not from the URL.
     Two code paths can produce "back at onboarding step 0, no connection":
     A-i (Gap 1): the save fails. handleCreate (CreateConnectionWizard.tsx:269-279) is try/finally with
       no catch. ActionButton (ActionButton.tsx handleClick) is also try/finally, so the rejection is unhandled.
       SnackbarErrorHandler (SnackbarErrorHandler.tsx:140-152) turns it into a 6 s raw snackbar, e.g.
       "Request failed with status code 403". onComplete is never called, so the user stays on step 3 and
       nothing inline explains the failure. Leaving with Cancel/Overview lands on onboarding step 0.
     A-ii (Gap 2): the save succeeds, navigate("/") runs (EditConnection.tsx:152-159), and
       getConfiguredWorkTrackingSystems is refused with 403. OverviewDashboard.tsx:176-185 maps the 403 to [],
       so hasConnections=false (:154). OnboardingStepper (OnboardingStepper.tsx activeStep: !hasConnections → 0)
       is shown because rbac.isSystemAdmin (:444). useRbac falls back to PERMISSIVE_SUMMARY
       (isSystemAdmin: true) both while loading and when my-summary fails (useRbac.ts:5-12, 32-33, 45-48).
    WHY 5A (root cause): Both screens turn a failure into a normal-looking state. The wizard has no failure state
      for Create. The Overview has no "refused" state for the connection list. useRbac's failure default claims
      "System Admin". So a server refusal looks the same as "nothing created yet".
```

Hypothesis, needs the reporter's logs: which of A-i and A-ii happened. A-ii conflicts with the code.
`GET /worktrackingsystemconnections` (:57-58), `POST` (:89-90) and `POST /validate` (:124-125) all carry
`[RbacGuard(SystemAdmin)]`, and `CanSatisfyRequirementAsync` is deterministic per principal
(RbacAdministrationService `CanSatisfyRequirementAsync`, SystemAdmin arm). If `/validate` passed, the list should pass too,
unless the principal or claims changed between requests (for example Entra group overage or token refresh)
or a proxy/WAF returned the 403. A-i fits the "isValid:true then nothing" report best.
**Verify:** the backend request log for `POST /api/latest/worktrackingsystemconnections` (status) and
`GET /api/latest/worktrackingsystemconnections` (status) at the reporter's timestamp, plus a DB row count in
`WorkTrackingSystemConnections`. Either way both Gaps are real defects, and the fix covers both.

### Branch A: what is unverified (peer review, iteration 1)

The S2 trigger is **unverified**. The wizard reaches "/" through only two routes: `onComplete` after a
successful save, or `onCancel` (EditConnection.tsx `onCancel`/`onComplete` both `navigate("/")`; the wizard's
`handleCancel` and `handleCreate`).

- "Back at step 0" without the user leaving the wizard therefore implies the save **succeeded**, and the list was then empty or refused (A-ii).
  That is inconsistent with the shared SystemAdmin guard, unless something outside the code (a token or claim change, or a proxy) intervened.
- A-i matches only if the user left the wizard after a failed save.

Neither is proven. The Gap 1 and Gap 2 fixes are justified by the AC on their own: they turn a silent failure into a named one,
and the next report will carry the real status code. They do NOT by themselves stop whatever failed.
Before closing S2, get the reporter's backend request log or a browser HAR for the Create and the following list GET.

## Branch B: OAuth reports an invalid URL (S1)

Status: the mechanism is reproduced at the HTTP level (E2), but the exact user-visible copy is **unverified**.
A JsonException is caught only by the generic catch (JWC:476-484). That catch returns
`"Connection validation failed due to an unexpected error."` with technicalDetails `"'<' is an invalid start of a value…"`, not the
`invalid_url` copy at JWC:414 and 472. The reporter's "invalid jira url" is therefore either a paraphrase of that technical detail
or comes from a surface not identified here. Ask the reporter for a screenshot of the message.


```
WHY 1B: OAuth setup shows a validation failure.
  The exact copy "invalid jira url" does not exist in the code (grep across backend and frontend/src).
  The closest strings are "The Jira URL appears to be invalid." (JWC:414, 472) and the JsonException text
  "'<' is an invalid start of a value." (surfaced as technicalDetails). The copy is likely paraphrased.
 WHY 2B: The OAuth route resolves a cloudId by string concatenation, not Uri merge, so the path is kept:
   JWC:2808-2812 → ResolveCloudIdAsync(jiraUrl) → GET $"{jiraUrl}/_edge/tenant_info" (JWC:2832).
   With /jira that URL returns 200 text/html (E2). EnsureSuccessStatusCode passes (JWC:2833), and
   JsonDocument.Parse(html) throws JsonException (JWC:2836). ValidateConnection's generic catch
   (JWC:476-484) returns validation_failed with that message. The same failure hits GetBoards,
   ConnectionHealthService.cs:181-183, and every gateway API call.
  WHY 3B: The OAuth wizard never validates before saving. It saves a draft connection first
    (CreateConnectionWizard.tsx ensureDraftConnectionId → POST), runs the handshake (OAuthService.cs:81-120,
    which never reads the URL), and then Create PUTs it. The first use of the URL is post-setup
    (health check, Edit validate, board wizard), which is where the failure appears.
   WHY 4B: The two auth routes treat the same option differently. The API-token route builds on a Uri
     BaseAddress (path dropped by accident, E4). The gateway route concatenates strings (path kept). Nothing
     reduces a Cloud URL to its site root. JWC:2810, 2817, 2897, 2914 and 1774 each read the raw option
     and apply only TrimEnd('/').
    WHY 5B (root cause): There is no single "Jira site address" concept. The URL option is read raw in
      6 places with ad-hoc trimming. Cloud versus Data Center is decided only by auth method
      (JWC:2779-2781) or by a /serverInfo call (JWC:2906-2945), never by host. So there is no place
      where "*.atlassian.net, so use scheme+host" could live.
```

## Branch C: where the URL option is read and how Cloud vs DC is told apart

Read sites (all in JWC): 409 (validate guard), 1774-1775 (browse link), 2810 (gateway → cloudId),
2817-2818 (direct BaseAddress and cache key), 2897 and 2914 (DeploymentCache key). Elsewhere it is only declared:
`Factories/WorkTrackingSystemFactory.cs:178` (default option) and `AuthenticationMethodSchema.cs:75,86,96,108`
(display). There are no Forge app or CLI/MCP read sites in this repo (grep `JiraWorkTrackingOptionNames.Url`).
Cloud vs DC: by auth method (`RoutesViaAtlassianCloudGateway`, JWC:2779) or from `/serverInfo.deploymentType`
(JWC:2930). Host suffix is never consulted.

**Latent defect found (DC context path):** by E4, a DC URL `https://host/jira` is trimmed (JWC:2817) and
then merged, so every relative route hits `https://host/rest/...`. DC instances with a context path are
already broken today, which contradicts the AC "left alone". A correct fix gives the DC BaseAddress a trailing `/`.

**Untestable seam:** `ResolveCloudIdAsync` uses `new HttpClient()` (JWC:2831), not `_httpMessageHandlerForTesting`.
So no unit test could ever exercise the tenant_info lookup, which is why branch B went unseen.

## Branch D: frontend

- Gap 1: `CreateConnectionWizard.tsx:269-279` (`handleCreate`, try/finally). The only error alert is
  rendered in `renderStep2`; step 3 (`renderStep3`) has none.
- Gap 2: `OverviewDashboard.tsx:176-185` (403 → `[]`) plus `:444-456` (stepper gated on `rbac.isSystemAdmin`).
  This component fetches imperatively (`useState`/`fetchData`, `:157-205`), not through TanStack. It learns about RBAC only
  via `useRbac()` (compliant; it does not fetch my-summary directly). `useRbac` exposes no error state; its failure
  default is permissive (`useRbac.ts:45-48`).

## Branch E: where to normalise

**Backend, at read time, authoritative.** One helper covers OAuth, API token, CLI/MCP and the API, plus every
already-saved row, with no migration. **Optionally also at save time** (Create/Update/Validate) so the stored and
displayed value is the canonical one. **Frontend:** no normalisation, because the rule would duplicate backend knowledge. At most
a helper text under the field ("Use your site address, e.g. https://yourcompany.atlassian.net").

## Cross-validation

- A and B do not contradict each other: the same raw URL gives opposite outcomes because of Uri merge versus string concat (E2, E4).
- The root causes explain all symptoms: S1 is explained by 5B. S2 is explained by 5A, with the URL a bystander (3A). Gaps 1 and 2 are 5A.
- Backwards check: if the URL were reduced to `https://site.atlassian.net`, then tenant_info returns JSON (E1) and S1 disappears.
  If handleCreate had a catch and the Overview a refused state, S2 would show an error instead of step 0.

## Root causes

- RC1: There is no canonical Jira site-address abstraction. The raw option is read in 6 places (5B).
- RC2: The OAuth gateway route concatenates strings, and the direct route merges Uris, so the same input behaves inconsistently.
  The DC context path is lost too (E4).
- RC3: The cloud-id lookup bypasses the injectable handler, so it cannot be tested.
- RC4: The create wizard has no failure state for Create (Gap 1).
- RC5: The Overview maps a refused list to empty, under a permissive RBAC fallback (Gap 2).

## Proposed fix (minimal)

Backend (RC1-RC3):
1. New `JiraSiteAddress` (static, in `…/WorkTrackingConnectors/Jira/`): `string Of(string raw)`.
   - Trim, then `Uri.TryCreate`. If the host ends with `.atlassian.net` (case-insensitive), return `scheme://host`.
   - Otherwise return the URL with a single trailing `/` trimmed (DC: keep the context path).
   - Return a malformed URL unchanged, so the existing `invalid_url` verdict (JWC:410) still fires.
2. In JWC add `private static string SiteUrlOf(WorkTrackingSystemConnection c) => JiraSiteAddress.Of(c.GetWorkTrackingSystemConnectionOptionByKey(Url))`.
   Replace the reads at 1774, 2810, 2817, 2897 and 2914.
3. `GetOrCreateClient`: `BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/")`. This makes the DC
   context path survive. The gateway base already ends in `/` (JWC:2812).
4. `ResolveCloudIdAsync`: use `_httpMessageHandlerForTesting` when set (same pattern as JWC:2851-2857).
   Optionally return `invalid_url` with "Use your site address, e.g. https://yourcompany.atlassian.net" when the body
   is not JSON. That verdict should stay reachable for non-atlassian.net hosts, such as custom domains.
5. Optional, save-time canonicalisation: in `WorkTrackingSystemConnectionsController.CreateConnectionFromDto`
   (shared by Create and Validate) and the Update path in `WorkTrackingSystemConnectionController.cs:84-147`, rewrite
   the Jira Url option via `JiraSiteAddress.Of` when `WorkTrackingSystem == Jira`. Not required for correctness.

Frontend (RC4, RC5):
6. `CreateConnectionWizard.handleCreate`: add a `catch` that mirrors `handleNext` (`ApiError` gives `error.message` and
   `technicalDetails`; otherwise a fallback copy). Render the existing error `Alert` block in step 3 too.
7. `OverviewDashboard.fetchData`: in the connections 403 branch, set `connectionsRefused=true` (do not just return `[]`).
   When `rbac.isSystemAdmin && connectionsRefused`, render an `Alert` ("Access to connections was refused…") in place of the
   OnboardingStepper. Keep the existing 403→[] behaviour for genuine non-admins.

## Files affected (absolute)

- /storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth/Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs
- /storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth/Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraSiteAddress.cs (new)
- (optional) /storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth/Lighthouse.Backend/Lighthouse.Backend/API/WorkTrackingSystemConnectionsController.cs, …/API/WorkTrackingSystemConnectionController.cs
- /storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth/Lighthouse.Frontend/src/components/Common/Connection/CreateConnectionWizard.tsx
- /storage/repos/Lighthouse/.claude/worktrees/spicy-chasing-knuth/Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.tsx

## Regression test hosts

- Backend (unit, no network):
  - New `…Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraSiteAddressTest.cs` (parametrised cases):
    - `https://x.atlassian.net/jira/` gives `https://x.atlassian.net`.
    - `https://X.Atlassian.net/jira/software/projects` gives `https://X.Atlassian.net` (host casing preserved).
    - `https://host/jira` and `https://host/jira/` give `https://host/jira`.
    - Malformed input is returned unchanged.
  - Request-URI assertions with the handler: `JiraWorkTrackingConnectorAuthDelegationTest.cs` (already builds a connector with a
    handler and the Url option, line 58) and `JiraFieldLookupTest.cs` (ValidateConnection with a handler). Cases:
    - API token + `/jira/` sends a request to `https://x.atlassian.net/rest/api/2/myself`.
    - DC `https://host/jira` sends `https://host/jira/rest/api/2/myself`.
    - OAuth + `/jira/` sends tenant_info to `https://x.atlassian.net/_edge/tenant_info` (needs step 4).
  - Do NOT use `JiraWorkTrackingConnectorTest.cs`, which is `[Category("Integration")]`/`JiraIntegration`, a live Jira.
  - (If step 5) `…Tests/API/WorkTrackingSystemConnectionsControllerTest.cs` and `WorkTrackingSystemConnectionControllerTest.cs`.
- Frontend (Vitest):
  - `src/components/Common/Connection/CreateConnectionWizard.test.tsx`: saveConnection rejects → an error alert is shown on step 3 and onComplete is not called.
  - `src/pages/Overview/OverviewDashboard.test.tsx`: a system admin with connections 403 → the refused alert is shown and the stepper is absent;
    a non-admin with 403 → behaviour unchanged.

## Risk assessment

- **Data Center context path:** this is currently broken (E4). Step 3 changes DC request URLs from `host/rest` to `host/jira/rest`.
  Behaviour therefore changes for any DC user who entered a context-path URL; they have been getting 404s or wrong hosts. Low risk, but call it out in release notes.
- **Custom Cloud domains** (not `*.atlassian.net`) are left as entered, as they are today. Accepted.
- **Already-saved connections with a path:** read-time normalisation covers them, so no migration is needed (expand-only rule respected).
  The stored value still shows `/jira/` in the Edit form until saved again. Step 5 fixes that on the next save only.
- **Caches:** `CloudIdCache`, `DeploymentCache` and `ClientCache` keys change to the normalised URL. They are in-memory, so a restart is harmless and
  the change is a benefit, because two spellings of one site now share a cache entry.
- **Browse links** become `https://site/browse/KEY` instead of `/jira/browse/KEY`. Both resolve (E5).
- **Gap 2 alert:** it must not show for genuine non-admins. Gate it on `rbac.isSystemAdmin` and keep the existing `showRbacNoAccessAlert`.
