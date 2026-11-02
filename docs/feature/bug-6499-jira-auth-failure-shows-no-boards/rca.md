# RCA: Bug 6499, a refused Jira credential reads as "no boards" and as empty Teams

All paths are relative to the repository root. Line numbers are against `c995492a0` (this worktree).
Method: code and existing tests only. No live Jira, Linear, ADO or ServiceNow call was made.

## 1. Problem statement and scope

A community user had about 10 Teams on one Jira Cloud connection. The board picker suddenly said there
were no boards, and every Team showed 0 data. The cause was the API token answering `401 Unauthorized`.
A new token behaved the same. Switching the Username to an old email address fixed it, because the user is
mid email migration and Jira Cloud basic auth is `email:token`. The only signal they ever saw was a bare
"401 unauthorised". The maintainer's note: even if Jira is down, the error is odd and should be fixed.

Two defects, investigated as separate chains:

- **(a)** The Jira board list turns every failure, including a refused credential, into an empty list. The
  wizard then shows an empty-list sentence instead of the reason.
- **(b)** A refused credential did not announce itself on the Teams. They showed 0 data, and the refusal
  reached an operator only through a log line, an admin-only popover, and a validation screen whose
  wording is a bare status code.

**In scope:** the board list read for Jira (connector, controller, wizard); the sibling connectors' board
list reads; the Team and Portfolio refresh on a refused credential and where its failure surfaces;
`ValidateConnection` for the same 401.

**Out of scope:** the Jira Data Center PAT path beyond what is shared; Basecamp (no such connector exists in
this repository: `Services/Implementation/WorkTrackingConnectors/` holds Auth, AzureDevOps, Boards, Csv,
Jira, Linear, OAuth, ServiceNow); the RBAC question of who may open the picker (settled in ADR-126
decision 4).

### What was reproduced, and what was not

Nothing could be run against a live tracker. Everything below is a code read plus a check of the existing
tests. Where a claim needs a live Jira, it is marked **Hypothesis**.

There is no test for any failure path of `JiraWorkTrackingConnector.GetBoards`. The only Jira board-list
test is the happy path `GetJiraBoards_ReturnsCorrectBoards`
(`Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnectorTest.cs:528-543`).
The board-list failure rows in section 3 can each become a committed regression test in DISTILL.

## 2. Symptoms

- **S1.** The Jira board picker says there are no boards, with no mention of authentication.
- **S2.** All Teams on the connection show 0 data.
- **S3.** The only text the user saw about the cause was a bare "401 Unauthorized". That text is the
  technical detail of the connection validation verdict, and it names neither the email nor the
  Username field.
- **S4 (maintainer's note).** The same silent-empty treatment applies when Jira is down.

## 3. Toyota 5 Whys

### Branch A: the board picker says "no boards" (S1, S4)

- **WHY 1A.** The picker shows the "No boards to offer" sentence.
  [Evidence: `BoardWizard.tsx:45-62`. When `getBoards` resolves with an empty array,
  `setError(NO_BOARDS)` runs (`:55-57`). `NO_BOARDS` (`:22-23`) reads: "No boards to offer. Either this
  account is not a member of any Visual Task Board, or none of the boards it can see carries both a table
  and a filter." The Autocomplete is also disabled (`:153`).]
- **WHY 2A.** The backend answered `200 OK` with `[]` instead of a refusal.
  [Evidence: `JiraWorkTrackingConnector.GetBoards` wraps the whole read in a bare `catch { return []; }`
  (`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs:502-515`).
  Below it, `GetBoardsFromJira` returns `[]` on any non-success status (`:1114-1124`) after logging at
  Information level. That log line says "Authentication is not valid for {Connection}" for every status
  code, including a 500 or a 404, and it carries the status code only inside the message
  (`:1116-1122`). So a 401, a 403, a 5xx, a timeout, an `HttpRequestException` from `ResolveCloudIdAsync`
  (`:3183` `EnsureSuccessStatusCode`) and a JSON parse failure all collapse into `[]`.]
- **WHY 3A.** The controller only converts a `WorkTrackingReadException` into a 400 that carries the
  verdict, and Jira's board **list** never throws one. The refusal channel exists, and Jira's
  `GetBoardInformation` partly uses it, but `GetBoards` was never connected to it.
  [Evidence: `API/WizardsController.cs:43-58` catches only `WorkTrackingReadException`. Jira's
  `GetBoardInformation` rethrows it (`:528-531`) but still swallows every other exception into an empty
  `BoardInformation` (`:532-536`), and a non-success `/configuration` read returns an empty
  `BoardInformation` too (`:724-731`). ServiceNow's `GetBoards` is wired end to end: it asks
  `ServiceNowBoardVerdict.FromBoardList` and throws `ServiceNowReadException` for a 401
  (`ServiceNow/ServiceNowWorkTrackingConnector.cs:124-141`; ADR-126 decision 1 table, `401` row).]
- **WHY 4A.** ADR-126 shipped the refusal channel for ServiceNow and left the other three connectors "to
  adopt later". Nothing tracked the adoption, and nothing forces it: the port's return type
  (`IEnumerable<Board>`) lets a connector report failure as emptiness, and the `catch`-all predates the
  channel.
  [Evidence: ADR-126 decision 1: "Jira, ADO and Linear can adopt later without another controller change";
  its Consequences section lists only a possible ArchUnit probe that no adapter's board method leaks a
  connector-specific exception past the port. The inverse rule, that no board method may turn a refusal
  into emptiness, appears nowhere. The ADR also states its own blast radius covers Jira, ADO and Linear
  for the wizard repair, and the repair shipped for the wizard side only.]
- **WHY 5A. Root cause A.** The board-list contract cannot tell "nothing to offer" from "could not ask".
  Both are an empty `IEnumerable<Board>`. The empty-list copy that ADR-126 decision 3 wrote for a ServiceNow
  fact ("not a member of any Visual Task Board") is then rendered for any connector, so a swallowed
  failure displays a confident, wrong, ServiceNow-worded explanation. It is a design gap rather than a
  one-line bug: once failure is encoded as emptiness, every per-connector `catch` that returns `[]` is
  correct by the type system.
  [Evidence: `BoardWizard.tsx:20-23` has no per-system copy, and `DataRetrievalWizardRegistry.ts:20-47`
  maps all four connectors to the same `BoardWizard`. `BoardWizard.test.tsx:161,341` pin the "Visual Task
  Board" wording.]

### Branch B: the Teams show 0 data and the cause is hard to find (S2, S3)

- **WHY 1B.** The Teams hold no data, and nobody was told why.
  **Hypothesis, requires verification:** the report does not say whether the Teams had data before the
  token broke. Two readings fit it, and the code supports both (WHY 2B).
- **WHY 2B.** What a refused credential does to a Team refresh depends on **which Jira endpoint answers
  with what**, and the code guards only some of them.
  1. **Refused loudly (401 on the first call).** The refresh throws before anything is written, so stored
     data stays. This does **not** produce 0 data on a Team that had data.
     [Evidence: for Jira Cloud the first reads are the field list (`GetCustomFieldMappings`,
     `:1773-1784`, throws `JiraReadException.FieldListRefused`, whose text blames project browse
     permission and a proxy and never mentions the credential, `JiraReadException.cs:76-86`), or the
     status list (`ReadList`, `:2583-2603`, throws `StatusListRefused`: "Jira answered 401 (Unauthorized).
     Nothing was changed; the Team keeps the work items from its last refresh"). `WorkItemService`
     fetches before it removes or saves (`WorkItems/WorkItemService.cs:130-145`), so the exception leaves
     the stored items alone. On Data Center the search itself raises `JiraQueryRejectedException`
     (`Jira/JiraWorkTrackingConnector.cs:2713-2733`).]
  2. **Answered as a stranger (200 with nothing).** The refresh succeeds with an empty result and deletes
     every stored item: removal is a set difference against whatever the tracker returned
     (`WorkItemService.cs:277-288`), and no guard stops an empty result from emptying the Team. In
     delta mode, an empty successful sweep does the same thing.
     [Evidence: the repository already records that Jira does this for a stopped credential. The doc on
     `RefuseUnlessTheCredentialIsStillAccepted` says Jira "does not say so by refusing - it answers as it
     would answer a stranger" (`:2087-2094`), and the guard it implements is called only from the
     issue-link-type read (`:2133-2146`), not from the search. The status and issue type lists carry a
     non-empty guard (`ParseAsNonEmptyArray`, `:2616-2630`), but the search results do not.
     **Hypothesis:** whether Jira Cloud answers the *search* of a rejected `email:token` with 200 and an
     empty issue list, or with 401, needs a live probe and was not run.]
  3. **Never populated.** If the Teams were new, or created after the token broke, "0 data" is simply "never
     refreshed successfully", and the 401 path in (1) explains why they stayed that way.
- **WHY 3B.** In every reading the refusal is recorded in places an ordinary user never looks.
  [Evidence: `TeamUpdater.cs:83-93` and `PortfolioUpdater.cs:148-158` put the connector's sentence into
  `outcome.Reason`, and that goes only into the refresh round's summary line (`ReportUpdateSummary`,
  `UpdateServiceBase.cs:80-85`), a log line. `RefreshLog` has no reason or error column
  (`Models/RefreshLog.cs`: Success, Cancelled, Skipped, counts, no text), so the Refresh History in
  Settings shows a red row with no cause. The recorded connection verdict is better: a failed refresh calls
  `ValidateConnection` and stores `AuthenticationFailed` plus its message
  (`UpdateServiceBase.cs:207-240`, `ConnectionHealthService.cs:61-66,186-198`), and the header popover shows
  it in red (`ConnectionsSection.tsx:106-116`). But that endpoint is `SystemAdmin` only
  (`API/ConnectionHealthController.cs:13-20`), and the Team page itself shows nothing.]
- **WHY 4B.** Each surface was designed to answer a different reader, and none of them is the Team page or
  the picker. ADR-184 chose to classify failures by re-asking `ValidateConnection` and to show the result
  in an admin popover, and the board picker was never part of that. So the same 401 produces four
  different texts, three of which have no actionable sentence.
  [Evidence: ADR-184 "The verdict is produced in two places and only those two". See the table in
  section 4 for the four texts.]
- **WHY 5B. Root cause B.** There is no single, typed "this credential was refused" fact that every Jira
  call site raises. Each site decides alone whether a 401 is an exception, an empty list, an empty result,
  or a canned sentence about something else, and the one sentence that does name the credential
  (`credential_no_longer_accepted`, `:2112-2125`) is reachable from one call site only. The result is that
  a 401 is reported correctly on the validate path and in a stopped-refresh path, and incorrectly (or not
  at all) on the others.

### Branch C: the 401 text does not help (S3)

- **WHY 1C.** The user's only signal was "401 unauthorised".
  [Evidence: `ValidateConnection` answers code `authentication_failed`, message "Authentication failed for
  Jira.", technical detail "Jira returned 401 Unauthorized.", field `ApiToken`
  (`Jira/JiraWorkTrackingConnector.cs:436-451`).]
- **WHY 2C.** The verdict names the **API token** as the field at fault, while the actual fault was the
  Username (email). The code cannot tell which of the two Jira rejected, because Jira answers a 401 for both.
  [Evidence: the highlighted field is hard-coded to `JiraWorkTrackingOptionNames.ApiToken` (`:450`). The
  auth strategy builds `username:token` for the Jira Cloud and scoped-token methods
  (`Auth/JiraCloudBasicAuthStrategy.cs:26-32`). The field is already labelled "Username (Email)"
  (`AuthenticationMethodSchema.cs:77,98`).]
- **WHY 3C.** The message is a classification, not a diagnosis, and it was written for the case where the
  token itself expired. A new token that "behaved the same" (the report) sent the user around the loop of
  reissuing credentials, which ADR-184 names as exactly the harm to avoid.
  [Evidence: ADR-184 "Guessing wrong sends an administrator to reissue a credential that was never the
  problem".]
- **WHY 4C.** For Jira Cloud basic auth a 401 has two usual causes that Lighthouse can name without
  guessing: the email does not own the token, or the token is revoked or expired. Neither verdict sentence
  says that Jira Cloud signs in with the email that owns the token.
- **WHY 5C. Root cause C.** The `authentication_failed` verdict was designed to separate "credential
  refused" from "unreachable". It was not designed to be actionable for the specific shape of the Jira
  Cloud credential (a pair, not a token).

### Completeness check

- **The other connectors.** Scope is **cross-connector**, not Jira-only. See the table below.
- **Reading of the board-list "Failed to load boards. Please try again."** The wizard keeps a canned
  string for failures that never reached the backend, and uses the backend's own words otherwise
  (`refusalOr`, `BoardWizard.tsx:25-27`). So a connector that throws a read exception gets its reason
  shown. That is why fixing the connector fixes the picker, without a new frontend path.
- **Access.** The picker is `SystemAdmin` only (`WizardsController.cs:14`), so the person who saw "no
  boards" is the same person who can see the health popover.

### Other connectors, bounded

| Connector | Board list failure path | Evidence | Verdict |
|---|---|---|---|
| Jira | any exception or non-success status becomes `[]` | `Jira/JiraWorkTrackingConnector.cs:502-515,1114-1124` | **Defect** |
| Azure DevOps | any exception becomes `[]`, logged at Error level | `AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:389-402`; the board **information** read also returns an empty `BoardInformation` on any exception (`:405-448`) | **Same pattern** |
| Linear | no `catch`; a transport or GraphQL failure propagates out of the controller (no handler for it, so a 500 via `UseExceptionHandler`, `Program.cs:173`). A GraphQL-level error with HTTP 200 is not inspected, so `response.Data` null yields `[]` (`:907-922`, `:650-672` where `errors` are only debug-logged) | **Different**: loud when the HTTP layer throws, silent-empty when only GraphQL `errors` come back. **Hypothesis:** which of the two a bad Linear key produces needs a live probe (the Linear key is shared with CI) |
| ServiceNow | 401 maps to `authentication_failed`, thrown as `ServiceNowReadException`, shown by the wizard | `ServiceNow/ServiceNowWorkTrackingConnector.cs:124-141`, ADR-126 table | **Correct** (the reference implementation) |
| CSV, Basecamp | no board picker (CSV uses an upload wizard); no Basecamp connector in the tree | `DataRetrievalWizardRegistry.ts:11-17` | Not applicable |

The Team **refresh** on a refused credential is separate and per connector. This RCA traced Jira's. Azure DevOps
already classifies `VssUnauthorizedException` as `authentication_failed` on validation (`:265-270`); Linear
validation reports a generic `validation_failed` (`LinearWorkTrackingConnector.cs:230-252`), which ADR-184
already lists as a known gap and which health reads as `Unreachable`.

### Alternative hypotheses considered

| Hypothesis | Assessment |
|---|---|
| The picker showed "No boards" because the account has no boards | Rejected for this report: the same credential worked after the Username changed, with no board change |
| Jira was down | Cannot be told apart from a refusal by the current code: both give `[]`. That is the maintainer's S4 point, and the fix must cover it |
| Team data was deleted by the 401 itself | A 401 on any guarded read stops the refresh before it writes. Only the "answered as a stranger" reading (WHY 2B.2) deletes. **Needs a live probe or the user's refresh log** |
| The frontend hid a backend error | Rejected: the backend answered 200 with `[]`, so there was nothing to show (WHY 2A) |

## 4. What the same 401 reports on each path

| Path | Code location | What the user reads |
|---|---|---|
| Board picker (list) | `Jira/JiraWorkTrackingConnector.cs:511-514` | The ServiceNow-worded "No boards to offer" sentence. Log line at Information, "Authentication is not valid" |
| Board picker (board info) | `:724-731` | An empty board that the Confirm button refuses; Warning log with the status code |
| Connection validate / Test connection | `:436-451` | "Authentication failed for Jira." plus "Jira returned 401 Unauthorized.", field ApiToken. Correct classification, thin wording |
| Team refresh (Cloud) | `:1779-1784`, `:2583-2603` | Log summary line only: "could not read the list of statuses ... Jira answered 401" (or the field-list text that blames project permissions). Data kept |
| Team refresh (Data Center) | `:2713-2733` | Log summary line: "The work tracking system refused this refresh: Jira answered 401 Unauthorized without saying why. Lighthouse asked: ..." |
| Connection health popover (admin only) | `ConnectionHealthService.cs:61-66` | Red "Authentication failed" plus the validate message |
| Refresh history (Settings) | `Models/RefreshLog.cs` | A failed row, no reason |

## 5. Backwards-chain validation

- **Root A to S1.** If failure and emptiness are the same value, a 401 on the board list reaches the wizard
  as `[]`, which renders the empty-list sentence. Yes. It also explains why a Jira outage, a timeout and a
  bad token all look the same (S4).
- **Root B to S2.** If a refresh that cannot ask throws before writing, a Team that had data keeps it, so
  the 401 alone does not explain 0 data on an existing Team. Yes for "never populated"; for "emptied" it
  needs Jira to answer the search as a stranger, which this repository already documents Jira doing for
  another endpoint. **Not closed.** See open question 1.
- **Root B to the missing explanation.** If the reason lives in a log line and an admin popover, a user who
  looks at the Team page sees only the data. Yes.
- **Root C to S3.** If the verdict highlights `ApiToken` and says "401 Unauthorized", a user whose token is
  fine is sent to reissue it. Yes, and it matches "a newly created token behaved the same".
- **Cross-validation.** A, B and C do not contradict. All three trace to one missing fact: a typed,
  worded "credential refused" outcome that every call site uses. S1 and S3 are fully explained. S2 is
  explained for the never-populated reading and conditionally for the emptied reading.

## 6. Candidate fix directions

### Direction 1: finish ADR-126 for Jira (and ADO); stop returning `[]` on failure

`GetBoards` and `GetBoardInformation` throw a `JiraReadException` for a refusal, carrying a verdict. A 401
reuses the `authentication_failed` code and sentence from `ValidateConnection`. Other statuses and
transport errors get their own words ("Jira answered 503", "Could not reach Jira"). Genuine `200` with zero
boards stays an empty list. ADO gets the same treatment. The empty-list copy in `BoardWizard` becomes
per-connector, or at least stops naming a ServiceNow concept for Jira.

- **For:** it is the smallest change that fixes S1 and S4 for the connectors that share the pattern; it uses
  the channel ADR-126 already built; the frontend needs almost nothing (`refusalOr` already shows the
  message).
- **Against:** it does not touch the refresh path, so S2 and the invisible-failure part stay.
- **Effort:** small to medium per connector; tests are the bulk, because none exist for the failure paths.

### Direction 2: one "credential refused" fact for Jira, used by every call site

Introduce a single place in the Jira connector that turns a 401 or 403 from any Jira read into one typed
refusal with the worded sentence, and call it from the board list, board info, field list, status lists,
search and sweep. The search gets the same "is the credential still accepted" check that the link-type
read already has (`RefuseUnlessTheCredentialIsStillAccepted`), run once per refresh, so an empty answer can
never be read as "the Team has no work". Direction 1 falls out of it for Jira.

- **For:** it addresses root B directly, closes the "answered as a stranger" gap that can empty a Team, and
  gives the field-list text the credential sentence it lacks.
- **Against:** a larger change to a 3,800-line connector that is already dense with guards; one extra
  `/myself` call per refresh (the existing guard caches it per refresh once accepted, `:2097-2102`); it
  needs the live probe in open question 1 to be sure what to guard.
- **Effort:** medium to large.

### Direction 3: make the failure visible where the data is

Keep the connectors as they are and add the last refresh failure's reason to `RefreshLog` and show it as a
banner on the Team and Portfolio pages (reusing `WidgetShell`'s could-not-load look), visible to the same
readers as the data.

- **For:** it makes S2 visible for all connectors at once, whatever the connector does.
- **Against:** it adds a column and an EF migration (expand-only), a new UI surface that needs a sketch and
  DESIGN, and it leaves the board picker lying.
- **Effort:** medium to large, and a feature rather than a bug fix.

### Recommendation

**Direction 1 as the fix for this Bug**, because it is the defect the report and the maintainer's note name
(an odd error that should be fixed, even when Jira is down) and the one the existing ADR already promised.
Add to it the narrow, cheap part of Direction 2 that the evidence supports: reword the validate verdict for
Jira Cloud so a 401 names the email and token pair. Carry the rest of Direction 2 and all of Direction 3
as separate items, and decide on them after the live probe in question 1.

## 7. Open questions for the maintainer

1. **Do the Teams show 0 data because they never refreshed, or because a refresh emptied them?** The code
   keeps stored data on a loud 401 and deletes it on an empty 200. I cannot tell which happened without
   either the user's refresh log (the `Refresh History` rows, or the log lines "Jira answered 401" /
   "Removed Work Item") or a live probe of a Jira Cloud search with a rejected `email:token`.
   - **Option A:** ask the community user for the Settings Refresh History rows and the lines around the
     failure. **Recommended**: it costs nothing and settles it.
   - **Option B:** run one live probe against a throwaway Jira Cloud site with a wrong email.
   - **Option C:** do not settle it; guard the empty-result case regardless (Direction 2).
2. **Scope of this Bug: Jira only, or Jira plus Azure DevOps?** ADO has the identical `catch { return []; }`.
   - **Option A:** Jira and ADO together, since the pattern and the fix are the same. **Recommended.**
   - **Option B:** Jira only, with a follow-up Bug for ADO.
   - **Option C:** all of Jira, ADO and Linear, after a Linear probe.
3. **How should a refused credential read on the board picker?**
   - **Option A:** the same sentence the validate path uses, plus an added hint that Jira Cloud signs in
     with the email that owns the token. **Recommended.**
   - **Option B:** reuse the validate sentence unchanged.
   - **Option C:** a new picker-specific sentence.
4. **The empty-list copy ("Visual Task Board") is ServiceNow wording shown for Jira, ADO and Linear. Fix it here?**
   - **Option A:** make the copy depend on the connector, ServiceNow keeping today's text, and give the
     others a neutral "No boards are available to this connection". **Recommended**, because the fix
     above makes a true empty list reachable for Jira and it would otherwise still carry the wrong words.
   - **Option B:** leave the copy and track it separately.
5. **Should the validate verdict highlight the Username as well as the API Token for Jira Cloud?**
   Jira answers 401 for both, so the field cannot be known.
   - **Option A:** highlight neither specifically and name both in the sentence. **Recommended.**
   - **Option B:** keep `ApiToken` highlighted and add the email hint to the text.
   - **Option C:** leave the field as it is.
6. **Should a failed refresh's reason become visible outside the log and the admin popover (Direction 3)?**
   - **Option A:** not in this Bug; raise a Story, because it needs a sketch, a migration and a DESIGN
     wave. **Recommended.**
   - **Option B:** include a minimal reason column and show it in Refresh History only.
   - **Option C:** a banner on the Team page in this Bug.
7. **The field-list refusal text blames project-browse permission even for a 401.** Reword it for a 401 as
   part of this Bug?
   - **Option A:** yes, a 401 or 403 from the field list reuses the credential sentence. **Recommended**, as
     it is the first read a Cloud refresh makes and the text is wrong for the case in this report.
   - **Option B:** leave it for the broader Direction 2 work.
8. **Should the Bug add a rule against `catch { return []; }` in board methods, in the style of the ArchUnit
   probe ADR-126 suggested?**
   - **Option A:** yes, a test that no `GetBoards`/`GetBoardInformation` implementation may catch and
     return empty. **Recommended**, so the fourth connector cannot repeat this.
   - **Option B:** rely on the per-connector failure-path tests only.
