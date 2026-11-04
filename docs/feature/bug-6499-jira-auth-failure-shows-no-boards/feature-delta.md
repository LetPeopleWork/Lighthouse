# Bug 6499: a refused Jira credential shows "no boards"

Root-cause analysis: `rca.md` in this folder.

**Read "Maintainer decisions: simplified copy" (2026-11-04) first.** It supersedes the copy and much of the
classification below, and it holds the answers to the DISTILL review. Passages it changed are restated in place.
The Q, N and AFK tables, the first copy table and DESIGN Decision 1 are kept as history only.

## Maintainer decisions (2026-11-02, after the RCA)

| # | Question | Decision |
|---|---|---|
| Q1 | Did the Teams show 0 data because they never refreshed, or because a refresh emptied them? | Not a blocker. The point is that no proper error message reached the user. The reporter is a premium customer, so their Refresh History can be asked for as a follow-up; it does not gate DISCUSS. |
| Q2 | Scope | **Jira and Azure DevOps.** Both turn every failed board read into an empty list. ServiceNow already does it right; Linear is out of scope. |
| Q3 | What the board picker says on a refused credential | **The connection check's sentence, unchanged.** No picker-specific text. Clarified later (Q3 clarified, end of file): one shared, reworded sentence replaces today's Test connection text and is what the picker shows. *Annotation (DESIGN, not a decision change):* for Azure DevOps the shared sentence is today's C6 reused as is, and only for a 401; an ADO 403 gets a separate permission sentence (C10) on the picker and keeps today's Test connection text. For Jira the shared sentence is C1 as reworded in DESIGN Decision 1. |
| Q4 | The ServiceNow "Visual Task Board" wording shown for every connector's empty list | **Fix it here, per connector.** ServiceNow keeps today's text; the others get a neutral sentence. |
| Q5 | Which field the connection check highlights on a Jira 401 | **Neither specifically.** The sentence names the sign-in details without saying which one is wrong, because Jira can answer 401 for a wrong email as well as for a wrong token (the reporter's case; the general claim rests on that case, not on Jira documentation). Restated by N2: one method-neutral sentence for Cloud and Data Center, see C1. |
| Q6 | Show a failed refresh's reason outside the log and the admin popover | **Separate Story**, not this Bug. Showing it in Refresh History or on the Team page needs its own DISCUSS. |
| Q7 | The field-list refusal text blames project permission on a 401 | **Reword it here.** A 401 or 403 from the field list uses the credential sentence. |
| Q8 | An architecture rule against `catch { return []; }` in board methods | **Skip.** Per-connector failure-path tests only. |

## Wave: DISCUSS / [REF] Scope, persona, job

Mode: PROPOSE (maintainer reviews; nothing below is final until confirmed). Feature type: user-facing bug fix
(backend verdicts plus one frontend copy table). Walking skeleton: N/A, because the whole path
(picker -> `WizardsController` -> connector -> `BoardWizard`) already exists end to end for ServiceNow; this
Bug connects two more connectors to it. Journey artifacts (visual/YAML): N/A, because it is one step of an
existing flow with a known emotional arc (confused -> told what to fix); the sketches below carry it.

### Scope Assessment: PASS

2 user stories (one per connector, each a slice), 1 bounded context touched (Tracker integration; the
frontend sentence is a presentation detail of the same flow), no new integration points, estimated
about 2 days (slice 01 about 1.5, slice 02 about 0.5). Not oversized. Linear is out (Q2). Anything beyond the
board reads, the one Test connection string and the disabled button is out (Q6, Q8, the 2026-11-04 block).

### Persona and job

- **Persona**: Tomas Weber, `config-admin` (a System Administrator) of a Lighthouse instance with about ten
  Teams on one Jira Cloud connection. He opens "Select Jira Board" while setting up a Team and expects
  either a list or a reason.
- **job_id**: `job-config-admin-know-any-credential-is-failing-not-just-oauth` (existing, persona
  `config-admin`, `docs/product/jobs.yaml` line 6433). Fit: its job story is "when a connection's credential
  stops working ... I want Lighthouse to say so ... so I fix it", and its anxiety force is "a mis-classified
  failure sends someone to reissue a credential that was never the problem". This Bug extends the same
  promise from refresh health to the board picker, the Test connection screen and the field list. **No new
  job proposed**: nothing in the file is about the board picker (the ServiceNow job is connector-specific).
  Proposed `jobs.yaml` edit, applied in DELIVER: a dated `REVISED` line in that job's `note` saying the
  signal now also reaches the picker and the validate sentence. No score change.
- **JTBD one-liner**: When Jira or Azure DevOps refuses the credential or cannot be reached, I want the
  board picker to say what the tracker answered and what to check, so I do not conclude my boards are gone.

## Wave: DISCUSS / [REF] Locked decisions

Q1 to Q8 above are binding where the 2026-11-04 block left them standing. Derived decisions (D) that follow from
them, for DESIGN to honour; D2, D3, D4, D6, D8 and D9 are restated as that block left them:

- **D1** (Q2): the Jira and ADO connectors' `GetBoards` and `GetBoardInformation` stop turning failure into
  emptiness. Failure is a `WorkTrackingReadException` (`JiraReadException`, `AzureDevOpsReadException`
  already exist). A genuine `200` with zero boards stays `200 []` (ADR-126 decision 3).
- **D2** (restated 2026-11-04): the picker has no credential sentence. A failed board list shows F1 and a failed
  board read shows F2, with the tracker's answer in it, whatever the cause. Jira Test connection shows T1, one
  neutral string for every Jira sign-in; its code (`authentication_failed`), technical detail and highlighted field
  stay as they are.
- **D3** (restated 2026-11-04): the empty list reads E1 for every connector, ServiceNow included. No per-connector
  copy. Linear gets only this text change; its read path is untouched.
- **D4** (dropped 2026-11-04): the field-list text is not reworded. A Jira field-list 401 still says it needs to
  browse a project.
- **D5** (Q8): no architecture rule. Each connector gets failure-path tests (RCA section 3 lists them; none
  exist today for `GetBoards`).
- **D6** (restated 2026-11-04): every failure, credential or not (5xx, other status, transport, unparseable body),
  gets F1 or F2 naming what happened. Both keep "Check the connection's URL and sign-in details", also for a 503
  or a timeout: the maintainer accepted this because the status sits in the sentence. That is the answer to the
  S4 note ("even if Jira is down, the error is odd"): the reader sees `503 Service Unavailable`, not "no boards".
- **D7**: the Information log line "Authentication is not valid" that is written for every non-success
  board status becomes truthful (status-specific). Wording is a DESIGN detail.
- **D8** (restated 2026-11-04): Jira connections that use a **scoped API token** or **Jira Cloud OAuth** are routed
  through the Atlassian gateway, and Jira's board endpoints do not accept those credentials even when they are
  valid (`jira.md` lines 73 and 92). For them "Select Jira Board" is disabled with the reason in tooltip D1, and no
  board call is made. Test connection works for them and shows T1 when refused.
- **D9** (dropped 2026-11-04): `ValidateConnection`'s status logic is untouched. Jira classifies **every** non-2xx
  from `/myself` as `authentication_failed`, so a 503 on Test connection still reads as an authentication failure.
  That is a known defect left out of this Bug, with no Bug number. `ConnectionHealthService` is untouched.
- **D10**: Azure DevOps has a second swallow: `GetBoardsForProject` (`AzureDevOpsWorkTrackingConnector.cs`
  lines 637-641, `catch (Exception) { return []; }`), called per project from `GetBoardsForProjects`
  (lines 610-619). Fixing only `GetBoards` would leave a rejected PAT as `200 []`. A failure of any project's
  board read must surface as the refusal, not as that project having no boards.

## Wave: DISCUSS / [REF] Context map, glossary, events, UI surface

- **Contexts touched (ADR-270)**: **Tracker integration** only (connectors, connection health, verdicts).
  Frontend `BoardWizard` is the driving adapter of the same flow. **Work Items** (refresh pipeline) is not
  touched; Organisation set-up is not touched.
- **Edges**: no new edge, none changed. The wizard reaches the connector through the existing port
  (`IBoardInformationProvider`); the controller keeps catching only the abstract `WorkTrackingReadException`.
- **Glossary**: no new Lighthouse term. One proposed glossary row for Tracker integration, applied in
  DELIVER: **Read refusal** = "a connector's typed answer 'I could not ask or was refused', distinct from
  'there is nothing to offer'; code name `WorkTrackingReadException`". It sharpens the existing row
  "Connection stopped working" (still target). ADR-270 map is unchanged.
- **Domain events**: none published, none consumed. "Connection stopped working" stays target; this Bug
  does not make a failed board read record a health verdict (that stays at refresh failure and Test
  connection, ADR-184).
- **ARCHITECTURE.md section 4**: checked, it does not mention the refusal channel, so no change is needed.
  If DESIGN adds a shared "credential refused" factory, add one line in the Tracker integration entry.
- **UI surface (ADR-271)**: the picker is a dialog opened from a unit's own settings (Team and Portfolio
  create/edit, "General settings"); the Test connection result is on the connection page, which ADR-271
  decision 4 keeps under **System**. No new surface; nothing is moved. Step towards the target: none
  cheap here, so none taken (said explicitly).
- **ADR-126 framing**: ADR-126 decision 1 built the refusal channel and said Jira, ADO and Linear "can
  adopt later without another controller change". Nothing tracked the adoption; Jira's
  `GetBoardInformation` adopted it only partly. This Bug **finishes ADR-126 for Jira and ADO**. Proposed
  ADR-126 edit, applied in DELIVER: a dated "Adoption" note (Jira and ADO adopted for list and information;
  Linear not adopted, out of scope) so the next reader does not find a promise with no owner.
- **ADR-184 framing**: unchanged. The verdict (`authentication_failed`) and its classifier (`ValidateConnection`)
  stay as they are, and this Bug changes only the message string of that verdict (T1). The picker makes no claim
  about authentication: it repeats what the tracker answered.

## Wave: DISCUSS / [REF] User stories

<!-- markdownlint-disable MD024 -->

### US-6499-01: Jira says why it gave no boards

job_id: `job-config-admin-know-any-credential-is-failing-not-just-oauth`

#### Problem

Tomas Weber administers Lighthouse for about ten Teams on one Jira Cloud connection. He opened the board
picker and read "No boards to offer. Either this account is not a member of any Visual Task Board ..." (a
ServiceNow sentence) because Jira had answered 401. He re-issued the API token, which changed nothing: the
email next to it was the one that was wrong. The only other text he ever saw was a bare "401 Unauthorized".
The same silent emptiness appears when Jira is down. In his case Jira answered 401 to a valid token paired with
the wrong email, so nothing in the answer pointed at the email, and only going through all the sign-in details would
have found it.

#### Elevator Pitch

Before: When Jira refuses the connection's credentials or is down, Tomas cannot tell that from "this
account has no boards", so he re-issues tokens that were never the problem.
After: open a Team's General settings, press "Select Jira Board" -> sees, in red under the board field,
"Lighthouse could not read the boards from Jira: 401 Unauthorized. Check the connection's URL and sign-in
details, then try again." (F1). Test connection says "Authentication failed for Jira. Check all of this
connection's sign-in details." (T1).
Decision enabled: go through every sign-in detail of the connection instead of re-issuing the token or giving up
on the boards.

#### Domain examples

1. **Wrong email (the report)**: Tomas' connection to `https://acme-eng.atlassian.net` has Username
   `tomas.weber@acme-old.example` and a valid token that belongs to `tomas.weber@acme.example`. Jira answers
   401. The picker shows F1 with `401 Unauthorized`; Test connection shows T1. Neither names the email: both send
   him to the sign-in details.
2. **Jira down**: same connection, Jira answers `503 Service Unavailable` on the board list. The picker
   shows F1 with `503 Service Unavailable`. The sentence still mentions the sign-in details; the status beside it
   tells him what happened.
3. **Really no boards**: a Jira Cloud site with Team-managed projects only answers `200` with zero boards.
   The picker shows E1, not the ServiceNow sentence.
4. **Scoped API token (valid)**: Anika Rossi's connection to `https://acme-eng.atlassian.net` uses a scoped
   token with `read:jira-user` and `read:jira-work`. "Select Jira Board" is disabled, and hovering it shows D1,
   which says Jira does not offer boards to this sign-in and to enter the query by hand. No board call is made.
   Test connection on the same connection says Connected.

#### UAT scenarios

1. **A refused Jira credential is told in the picker, on 401 and on 403.** Given Tomas' Jira Cloud
   connection and Jira answers 401 on the board list (example 1) or 403 on it (the account has no Jira
   Software licence), When he opens "Select Jira Board", Then he reads F1 with that answer in it, the board field
   stays disabled and Confirm stays disabled.
2. **Test connection sends him to all the sign-in details.** Given the same connection and Jira answers 401 on the
   account check, When he presses Test connection, Then he reads T1 with technical detail "Jira returned 401
   Unauthorized." and the API Token field highlighted as today.
3. **Jira being down or unreachable reads as what it is.** Given Jira answers 503 to the board list, or does not
   answer at all (timeout), When Tomas opens the picker, Then he reads F1 with `503 Service Unavailable` or
   `no answer`. The sentence keeps "Check the connection's URL and sign-in details", which the maintainer accepted
   because the status sits in the sentence. Test connection's 503 stays out of this Bug.
4. **A genuinely empty board list is not a fault.** Given Jira answers 200 with zero boards, When Tomas
   opens the picker, Then he reads E1 and no "Visual Task Board" wording; a ServiceNow connection reads E1 too.
5. **A board that cannot be read whole is not confirmable.** Given the board list loaded and Jira refuses, fails or
   answers unusably on the chosen board's configuration, status list, work items or work item type list, When Tomas
   picks the board, Then he reads F2 with that answer in it and Confirm stays disabled. A board without its states or
   types is never offered. The one exception: a board whose saved filter is refused keeps its existing sentence
   (`board_filter_unreadable`), which names the filter to share.
6. **A valid scoped token or OAuth sign-in is not sent to a picker that cannot work.** Given Anika Rossi's
   connection uses a scoped API token (or Jira Cloud OAuth), When she reaches the Team or Portfolio settings or the
   create flow, Then "Select Jira Board" is disabled, hovering it shows D1, and no board call is made. In the create
   flow the load-data step is skipped and Configure shows the same disabled button.

#### Acceptance criteria

- [ ] Scenarios 1 to 6 hold as automated tests (backend connector tests for the failure paths RCA
      section 3 lists, including a 403 on the board list; frontend tests for E1, D1 and the button).
- [ ] No message or technical detail contains the email, the token or any header value.
- [ ] The log line for a non-success board status names the real status and no longer claims
      "Authentication is not valid" for a 503.
- [ ] `{answer}` covers well-formed JSON Lighthouse cannot use (a board entry with no id, a `/configuration` with no
      columns, a status list or work item list without what it should hold): it reads
      `an answer Lighthouse could not read`, the same as unparseable JSON.
- [ ] A refused status list, `board/{id}/issue` or work item type list inside `GetBoardInformation` gives F2, and
      never a board with no states or no types. A board that really holds no work items yet is still read.
- [ ] `ValidateConnection` changes only its message: code, technical detail, highlighted field and status logic are
      as today.
- [ ] Scoped-token and OAuth connections show the button disabled with D1 on every screen that offers it, and make no
      board call. Other Jira connections are unaffected.

#### Outcome KPIs

- **Who**: Lighthouse System Administrators setting up Jira Teams.
- **Does what**: reach the right fix (go through the sign-in details, or "wait, Jira is down") from the first
  sentence they read.
- **By how much**: 0 community reports of "no boards" where the cause was a refused credential or a Jira
  outage, in the 90 days after release (baseline: 1 reported, plus the ones nobody reported).
- **Measured by**: community and support thread review at the next two releases; no telemetry (see DEVOPS
  pre-answer below).
- **Baseline**: every such failure reads "No boards to offer".

#### Technical notes

- Jira `GetBoards` and the paging loop `GetBoardsFromJira`, `GetBoardInformation` (its generic `catch`),
  `GetBoardInformationFromJira` (the empty return on a non-success `/configuration`), `MapStatusToCategory` and
  `GetItemTypesForBoard` (the quiet returns), and the one T1 string in `ValidateConnection`. DESIGN Decision 2
  lists each site.
- `BoardWizard.tsx` already shows `ApiError.message` (`refusalOr`) and disables Confirm on a null
  `boardInformation`; no new frontend error path is expected.
- A Team can be configured without a board: the board wizard is an optional button next to the Team's query
  field, which is typed by hand, and `jira.md` lines 73 and 92 already say to "configure teams/portfolios
  manually". That is why D1 may truthfully say to enter the query by hand.
- Depends on nothing.

### US-6499-02: Azure DevOps says why it gave no boards

job_id: `job-config-admin-know-any-credential-is-failing-not-just-oauth`

#### Problem

Priya Nair administers an Azure DevOps connection with a Personal Access Token that expired over the
weekend. The "Select Board" dialog on a new Team says there are no boards, and the board information read
returns an all-empty result on any exception. Her only way to learn the PAT expired is to leave the picker
and press Test connection on the connection page.

#### Elevator Pitch

Before: When the PAT is expired or Azure DevOps cannot be reached, the Azure DevOps picker says "No boards
to offer" and Priya assumes the project has none.
After: open a Team's General settings, press "Select Board" -> sees "Lighthouse could not read the boards from
Azure DevOps: Contoso: 401 Unauthorized. Check the connection's URL and sign-in details, then try again." (F1).
Decision enabled: renew the PAT instead of searching for a missing board.

#### Domain examples

1. **Expired PAT**: connection `https://dev.azure.com/northwind` answers unauthorized (`VssUnauthorizedException`);
   the picker shows F1 with what Azure DevOps said, naming the project that failed.
2. **Azure DevOps unreachable**: the host does not answer; the picker shows F1 with `no answer`.
3. **Organisation with no boards**: a project with no team boards answers an empty list; the picker shows E1.
4. **One project fails, the others answer**: the connection `https://dev.azure.com/northwind` has projects
   Fabrikam and Contoso; Azure DevOps answers Contoso's board list with a service error. The picker does not
   quietly show only Fabrikam's boards as if Contoso had none; it shows F1 naming Contoso.

#### UAT scenarios

1. **An expired PAT is told in the picker.** Given Priya's PAT is rejected, When she opens "Select
   Board", Then she reads F1 with what Azure DevOps said and Confirm stays disabled.
2. **A failing Azure DevOps is not reported as empty.** Given Azure DevOps answers with a service error or
   cannot be reached, When she opens the picker, Then she reads F1 and not "no boards".
3. **A genuinely empty list is neutral.** Given the project has no boards, When she opens the picker,
   Then she reads E1.
4. **A board that cannot be read is not confirmable.** Given the list loaded and the chosen board's
   information read fails, When she picks the board, Then she reads F2 and Confirm stays disabled.
5. **One failing project is not an empty project.** Given the board read of one of Priya's projects fails
   while the others answer, When she opens the picker, Then she reads F1 naming that project and not a list that
   silently lacks its boards.

#### Acceptance criteria

- [ ] Scenarios 1 to 5 hold as automated tests (the board list failure rows, the per-project failure in
      `GetBoardsForProject`, plus the board information read, in the ADO connector tests).
- [ ] F1 names the failing project and Azure DevOps' answer, with a trailing full stop of that answer trimmed. Azure
      DevOps' Test connection is unchanged.
- [ ] The existing Error-level log for a failed board read is kept, for every failure.

#### Outcome KPIs

- **Who**: Lighthouse System Administrators setting up Azure DevOps Teams.
- **Does what**: reach the right fix from the first sentence.
- **By how much**: 0 reports of "no boards" caused by an expired PAT or an unreachable Azure DevOps in 90 days.
- **Measured by**: same review as US-6499-01. **Baseline**: every such failure reads as empty.

#### Technical notes

- `AzureDevOpsWorkTrackingConnector.GetBoards` (`catch (Exception)` -> `[]`), `GetBoardInformation`
  (same) and the **second swallow** in `GetBoardsForProject` (lines 637-641, per project, called from
  `GetBoardsForProjects`, lines 610-619; D10). All three must stop returning empty, or a rejected PAT still
  reads "no boards". One failing project fails the whole list (HELD-1). `AzureDevOpsReadException` exists. There
  is no classification: 401, 403 and everything else reach the same sentence with what Azure DevOps said.
- **Depends on US-6499-01** (slice 01) for E1 in `BoardWizard`. This is the one allowed direction: the later slice
  builds on the earlier, finished one; slice 01 does not wait on slice 02.

## Wave: DISCUSS / [REF] Proposed copy and where it shows

**Superseded in full by the 2026-11-04 block**, which replaced C1 to C11 with F1, F2, E1, T1 and D1. This section is
kept as the record of the first proposal, and no test or slice follows it.

All text is **proposed**, for the sketch walk-through at the start of DISTILL. "Board" is the tracker's own
word (a literal tracker concept, not a configurable Terminology term); "Team" in the dialog title comes from
Terminology as today. Labels "Username (Email)" and "API Token" are the existing field labels.

| Id | Where | Proposed text |
|---|---|---|
| C1 | Jira credential refused (401 or 403, N3) for the API token (Jira Cloud) and the personal access token (Data Center): picker, board information, Test connection, field list. One method-neutral sentence (N2 = B); it **replaces today's Test connection text** (Q3 clarified). Not shown to scoped-token or OAuth connections (C1m, C1o). | Jira refused the sign-in. Check the user name or email and the API token or personal access token, whichever this connection uses, and that the token has not been revoked or expired, then try again. |
| C1m | Jira Cloud scoped API token or OAuth connection: 401 or 403 on the board list or board information (N1 = A). Jira's board endpoints do not accept these credentials even when valid. | Jira does not offer boards to this kind of sign-in (a scoped API token or Jira Cloud OAuth), even when the sign-in is valid. Type the query by hand, or use a personal API token while you set it up and switch back afterwards. Press Test connection to check the credential itself. |
| C1o | Jira Cloud scoped API token or OAuth connection: 401 or 403 on Test connection or the field list (these endpoints accept those credentials, so a refusal is real). | Jira refused the sign-in. For a scoped API token, check that it is still valid and has the scopes read:jira-user and read:jira-work. For Jira Cloud (OAuth), use Reconnect on the connection, then try again. |
| C2 | Jira answered a status other than 401 or 403 for the list (N3) | Jira answered 503 (Service Unavailable) when Lighthouse asked for the list of boards. This does not look like a problem with the credentials. Try again in a moment, and check that Jira is up if it keeps happening. (Status and reason come from Jira's answer.) |
| C2v | Jira answered a status other than 401 or 403 to Test connection (D9); code is not `authentication_failed` | Jira answered 503 (Service Unavailable) when Lighthouse checked the connection. This does not look like a problem with the credentials. Try again in a moment, and check that Jira is up if it keeps happening. |
| C3 | Jira did not answer | Lighthouse could not reach Jira at https://acme-eng.atlassian.net to read the list of boards. Check the URL and that Jira can be reached from the machine Lighthouse runs on, then try again. |
| C4 | Board configuration read failed (Jira) | 401 or 403: C1 (C1m for scoped-token and OAuth). Other status: "Jira answered 503 (Service Unavailable) when Lighthouse asked for the configuration of this board. This does not look like a problem with the credentials. Try again in a moment, and check that Jira is up if it keeps happening." Unreachable: "Lighthouse could not reach Jira at https://acme-eng.atlassian.net to read the configuration of this board. Check the URL and that Jira can be reached from the machine Lighthouse runs on, then try again." |
| C5 | Empty list, per connector (Q4); ServiceNow keeps "No boards to offer. Either this account is not a member of any Visual Task Board ..." | Jira: "No boards are available to this connection. The account it signs in with may not have access to any board, or none of the boards it can see can be used here. Check the account's access in Jira, then try again." Azure DevOps: the same with "in Azure DevOps". Linear: the same with "in Linear". |
| C6 | ADO credential refused | Authentication failed for Azure DevOps. Check your Personal Access Token permissions and make sure it is still valid. (today's ADO sentence is kept as the one shared sentence; the picker shows it unchanged) |
| C7 | ADO failing or unreachable | Azure DevOps could not be asked for the list of boards: {what Azure DevOps said}. Check the organisation URL and that Azure DevOps can be reached, then try again. |
| C8 | Jira field list 401 or 403 | C1 (C1o for scoped-token and OAuth); technical detail "GET /rest/api/{version}/field answered 401 Unauthorized."; no input highlighted. Other statuses keep today's text. |

Sketch 1, the picker (loading look is today's spinner; the failure look is the red line; both exist, no new
control; reuses `BoardWizard`, `Typography color="error"`):

```
+------------------------------------------------------------+
| Select Jira Board                                          |
|                                                            |
|  [ Board                                         v ] (off) |
|                                                            |
|  Jira refused the sign-in. Check the user name or email    |   <- red, C1
|  and the API token or personal access token, whichever     |
|  this connection uses, and that the token has not been     |
|  revoked or expired, then try again.                       |
|                                                            |
|                                  [ Cancel ]  [ Confirm ]off|
+------------------------------------------------------------+
```

Sketch 2, Test connection on the connection page (System surface, existing result block):

```
  Test connection   [ Test ]
  (x) Jira refused the sign-in. Check the user name or email and the API token
      or personal access token, whichever this connection uses, and that the
      token has not been revoked or expired, then try again.   <- C1
      Jira returned 401 Unauthorized.            <- technical detail, as today
  Username (Email) [ tomas.weber@acme-old.example ]   API Token [ ******** ]
  (neither input highlighted, Q5)
```

Sketch 3, the empty list (C5) is the same dialog as Sketch 1 with the C5 text in the red line. The scoped-token
and OAuth picker (C1m) is also the same dialog with the C1m text in the red line.

Test connection and the field list for scoped-token and OAuth connections: same result block as Sketch 2 with
C1o in place of C1. A Team can be set up without a board (the wizard is an optional button, the query field is
typed by hand), which is why C1m may suggest it; the email sentence never reaches those connections.

Slow-connection rule: no new screen fetches data; the picker keeps its spinner while loading and its red
line on failure, and a late answer for a closed dialog is today's behaviour. DESIGN confirms nothing more is
needed. Visualization skill: N/A, because nothing here charts data.

## Wave: DISCUSS / [REF] Lighthouse checklist

| Item | Answer |
|---|---|
| RBAC impact | None. The picker stays `SystemAdmin` (ADR-126 decision 4, unchanged); the refusal reaches only System Administrators, who can already read connection health. No new permission, no API-key scope change, and no component fetches `my-summary`; `useRbac()` is untouched. The verdict never contains credential values (AC). |
| Lighthouse-Clients (CLI/MCP) versioning | N/A, because `/api/latest/wizards/...` is a UI-only System Administrator endpoint; a search of the clients repo for "wizards" found no match. The only wire change is `200 []` -> `400 {message, technicalDetails, fieldName}` for a failed read, the envelope ADR-126 already ships for ServiceNow. No changeset. |
| Website marketing surface | N/A, because this is a bug fix with no new capability or claim. The website hot-links docs images from `main`; `jira_wizard.png` shows the happy path and does not change. |
| Docs prose (for DELIVER finalize) | **Yes, owed**: `docs/concepts/worktrackingsystems/jira.md` line 92 says Lighthouse "will display 'No Boards available'" for scoped tokens; that sentence is wrong after this Bug. Replacement: "Select Jira Board" is disabled for scoped tokens, its tooltip says why, and the Team's query is entered by hand; line 73 (OAuth) gets the same correction and keeps "Configure teams and portfolios manually". ADO page: check for a board-picker note. Per-feature screenshots: **N/A**, because the failure state is one red line of text and `jira_wizard.png` shows the happy path, which does not change. Demo data: N/A, because failure paths need a refused credential, not data. |
| Release notes | Candidate for `/release` (not at finalize): customer pain first ("a refused Jira or Azure DevOps credential no longer reads as 'no boards'"). ADO "Release Notes" tag: maintainer decides. |
| Usage-data event (DEVOPS pre-answer) | Proposed "N/A, because the change reports a failure and adds nothing to be used; a name-only 'board list refused' event would count failures, not use". DEVOPS confirms. |
| Terminology | Copy uses "board", "Team" (Terminology), no tracker-specific heading words. |
| Context map and glossary | Answered above; proposed edits applied in DELIVER. |
| Proposed doc edits, applied in DELIVER | ADR-126 adoption note; ADR-270 glossary row "Read refusal"; `jobs.yaml` REVISED note; `jira.md` lines 73 and 92. |

## Wave: DISCUSS / [REF] Slices

Briefs in `slices/`. Order: Jira first (it carries E1 and the report), then ADO. **Slice 02 depends on slice 01**
(E1 in `BoardWizard`); that is allowed because it runs in one direction, the later slice building on the finished
earlier one.

| Slice | Story | Learning hypothesis (disproved if it fails) |
|---|---|---|
| 01 | US-6499-01 | The shared refusal channel carries Jira's 401 to the picker with no new frontend path. Disproved if `BoardWizard` needs a new error path. |
| 02 | US-6499-02 | ADO's exception model fits the same template without a Jira-shaped abstraction. Disproved if a shared helper is needed to make it pass. |

Taste tests: no slice adds 4+ components; no slice depends on a new abstraction; both disprove something;
production-data criterion is met by running one picker against a deliberately wrong credential on a real
Jira and ADO sandbox in DELIVER (not synthetic only).

## Wave: DISCUSS / [REF] Definition of Ready (9 items)

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Problem sections above. |
| 2 | Persona | Tomas Weber and Priya Nair, `config-admin`. |
| 3 | 3+ examples with real data | Three each, with URLs and addresses. |
| 4 | UAT 3-7 | Six and five. |
| 5 | AC from UAT | Embedded per story. |
| 6 | Right-sized | One slice each: slice 01 about 1.5 days, slice 02 about 0.5 (about 2 in total). |
| 7 | Technical notes | Per story. |
| 8 | Dependencies | US-02 depends on US-01 (E1 in `BoardWizard`), one direction only. N1 to N3 are answered and folded in. |
| 9 | KPIs | Per story; measured by review, no telemetry. |

DoR passes for both stories. Job traceability: both stories carry `job_id`; neither is `@infrastructure`; both have an
Elevator Pitch. No per-wave reviewer dispatched (optional per the skill; the consolidated review fires at
the end of DISTILL).

## Wave: DISCUSS / [REF] Out of scope

Linear (Q2); a failed refresh's reason in Refresh History or on the Team page (Q6, not tracked; draft below); an
architecture rule against `catch { return []; }` (Q8); widening who may open the picker (ADR-126 decision 4);
guarding the Jira search against an "answered as a stranger" empty result (RCA Direction 2; settled by Q1's
follow-up evidence, not this Bug); ServiceNow.

## Follow-up Story draft (Q6, not created)

Not created in ADO, by the maintainer's decision; kept here for reference only:

- **Title**: Show why a refresh failed, where the person looking at the data can see it
- **Description 1**: A Team or Portfolio whose refresh failed (refused credential, Jira down, a rejected
  query) shows no reason to anyone but a System Administrator who opens the connection health popover.
- **Description 2**: Today the reason is only in a log line; the Refresh History row in Settings is red with
  no text (`RefreshLog` has no reason column), and the Team page shows only stale data.
- **Description 3**: Needs its own DISCUSS: store the last failure reason per refresh (expand-only
  migration), show it in Refresh History and decide whether the Team and Portfolio pages carry a banner
  (reusing `WidgetShell`'s could-not-load look), visible to the readers of the data.
- **Description 4**: Related: Bug 6499 (board picker and Test connection wording), ADR-184 (health verdict),
  RCA Direction 3. Its Q1 evidence (the reporter's Refresh History) may tell whether a refresh emptied their Teams.
- **Description 5**: Needs a sketch and the slow-connection loading and failure looks at DESIGN; surface
  per ADR-271 is the unit's page (Team, Portfolio) plus Settings -> Refresh History.

## Wave: DISCUSS / Resolved

The open questions N1 to N3 and the Q3 clarification were answered in the table below, and the 2026-11-04 block
then replaced what they fed (D2, D8, D9 and the copy table). No question is open.

## Maintainer decisions on the DISCUSS questions (2026-11-02)

| # | Question | Decision |
|---|---|---|
| N1 | Scoped API tokens and OAuth get a 401 from Jira's board endpoint even with a valid credential | **Method-specific sentence (A).** For those methods the picker says Jira does not offer boards to this sign-in method, and suggests configuring the Team with JQL or using a personal API token while setting up. Never the "check your email and token" sentence. |
| N2 | Data Center personal access tokens have no email | **Same sentence for all methods (B)** that use the credential sentence, Cloud and Data Center alike. |
| N3 | A 403 on the board list (e.g. no Jira Software licence) | **401 and 403 alike (B)**, as the connection check does: both get the credential sentence. |
| Q3 (clarified) | "Unchanged" meant one shared sentence | **One shared credential sentence, reworded** (naming the user name or email and the token, method-neutral per N2, per Q5). It replaces today's Test connection text ("Authentication failed for Jira. Jira returned 401 Unauthorized.") and is what the picker shows. |

## Wave: DESIGN

Mode: PROPOSE. Scope: application (one bounded context, Tracker integration; no new container, no new
external system, so the C4 L1 and L2 in `docs/product/architecture/brief.md` are unchanged and no new C4 is
drawn). Paradigm: object-oriented, ports and adapters. ADR drafts in this folder, applied in DELIVER:
`adr-draft-credential-verdict-per-connector.md` (decisions 1 and 2) and
`adr-draft-ado-board-list-fails-whole.md` (decision 3: fail the whole list, decided by the maintainer as HELD-1).
Nothing outside this folder was edited. Revised after the DESIGN review
against the AFK decisions at the end of this file (AFK-2 reversed, AFK-5 re-confirmed, AFK-6) and the maintainer's
decisions on the held items. **Restated after the 2026-11-04 block:** Decisions 2 to 5, the diagram and the handoff
now describe the simplified design; Decision 1 is history; `adr-draft-credential-verdict-per-connector.md` is
superseded.

### [REF] Existing-system analysis (reuse first)

| Need | What already exists | Verdict | Contract shape, universe, assertion |
|---|---|---|---|
| A typed "could not ask / was refused" answer | `WorkTrackingReadException` (abstract, carries a `ConnectionValidationResult`), `JiraReadException`, `AzureDevOpsReadException`; `WizardsController.AnsweringARefusalWithItsReason` catches the abstract type and answers `400 {verdict}`; `BoardWizard.refusalOr` shows the verdict text | **Reuse unchanged.** No new exception type, no controller change, no new frontend error path. | Pure data (an exception carrying a value). Nothing to assert beyond the verdict it carries. |
| A place for "status -> sentence" knowledge | ServiceNow did it with `ServiceNowValidationVerdict` and `ServiceNowBoardVerdict` | **Not needed** (2026-11-04): F1 and F2 carry the tracker's answer, so nothing maps a status to a sentence. | None. |
| Which Jira connections cannot use the picker | `JiraWorkTrackingConnector.RoutesViaAtlassianCloudGateway(authenticationMethodKey)` on the backend; `authenticationMethodKey` on the frontend's connection | **The frontend decides** (decision 4): the picker never opens for them, so the backend needs no branch. | Pure predicate. |
| Whether a Jira exception means "could not be asked" | `JiraCouldNotBeAsked` (`JiraWorkTrackingConnector.cs` lines 3760-3761), used by the release and membership reads | **Reuse** for the board reads; no second mapping (Decision 2). | Pure predicate. |
| Health state from a verdict | `ConnectionHealthService.StateFor` | **Unchanged**, and no test added. | Not touched. |
| Jira `GetBoards`, `GetBoardInformation` | The existing read adapters | **Modify**: throw a read exception instead of returning empty, or a board with no states or types. | **Bounded-change with an empty declared mutation set** (they read). Universe: the connection's own Jira over HTTP, GET only; no repository or database write; no health write. |
| Jira `ValidateConnection` | The existing adapter | **Modify one string** (T1). | No change of shape: the same result with a different message. |
| ADO `GetBoards`, `GetBoardsForProject`, `GetBoardInformation` | The existing read adapters | **Modify**: outcome-or-failure instead of `[]` / empty. | **Bounded-change with an empty declared mutation set.** Universe: the organisation's ADO over the SDK clients, reads only. Asserted with SDK fakes that record calls (only `Get*Async`), strict mocks for the rest. |
| `IBoardInformationProvider` (driving port) | Two read methods | **Unchanged.** | Reads only: it exposes no write method, so nothing to split. |
| Per-wizard facts on the frontend | `IDataRetrievalWizard` registry entry (id, name, system types, contexts) | **Extend** the entry with the sign-ins it cannot serve (decision 4). | Static data. |

Contract shapes (Earned Trust / effect isolation) in one line: the read adapters are **bounded-change with the
universes in the table**, whose only new outcome is "throw a read exception instead of returning empty". The board
reads are not a health writer: the stored verdict is still written only at refresh failure and Test connection
(ADR-184 single writer, `ConnectionHealthSingleWriterArchUnitTest` stays green). Environment lies this design
accounts for: Jira answers 401 for a wrong email as well as a wrong token (T1 and F1 name no single detail), and
answers 401 on the board endpoints for a valid scoped token or OAuth sign-in (the disabled button, D8). No new probe
is needed: no new adapter, and the DELIVER live check against a deliberately wrong credential on a real Jira and ADO
sandbox is the empirical probe (DISCUSS taste test).

### [REF] Decision 1: where the one credential sentence lives

**Superseded in full by the 2026-11-04 block.** There is no verdict class and no status ladder: F1 and F2 are built
where the connector throws, and T1 is one string. This section is kept as the record of what was weighed; nothing
below is to be built, including the log split at its end (see DEVOPS answer 3).

| Option | Trade-offs |
|---|---|
| **A. One pure verdict class per connector** (`JiraConnectionVerdict`, `AzureDevOpsConnectionVerdict`), next to the connector and its read exception. `ValidateConnection`, the board list, board information and (Jira) the field list all call it. | Matches ServiceNow's existing `...Verdict` classes, so a fourth reader finds one idiom. Wording stays in the connector. One place to pin the sentence in a table test. Cost: two classes. |
| B. A shared cross-connector factory taking the tracker name and the sign-in hint. | Rejected. Jira's C1 / C1m / C1o and ADO's C6 are different knowledge (different tokens, different gateway rule); one parameterised factory would be a switch on connector wearing a shared name. DRY is knowledge, not code. Also contradicts slice 02's hypothesis ("no shared helper"). |
| C. Static factories on the read exceptions (`JiraReadException.CredentialRefused(...)`), as `FieldListRefused` does. | Rejected for the sentence. `ValidateConnection` needs the verdict, not an exception; it would build an exception to unwrap it. The exception factories stay for read-specific verdicts; they call the verdict class where a credential refusal is one of their cases. |

**Recommendation: A.** Shape (interface contract only):

```
JiraConnectionVerdict       (pure, static)   FromStatus(read, status, signIn, host) -> ConnectionValidationResult
                                             FromUnreachable(read, host)            -> ConnectionValidationResult
                                             FromUnreadableAnswer(read)             -> ConnectionValidationResult
   read   = Validation | FieldList | BoardList | BoardRead
   signIn = Direct (API token, PAT) | ViaAtlassianGateway (scoped token, OAuth)
AzureDevOpsConnectionVerdict (pure, static)  CredentialRefused()                    -> C6
                                             PermissionRefused(read, project?)       -> C10 (picker only, 403)
                                             Unavailable(read, whatAdoSaid, project?) -> C7
                                             InvalidBoardId()                        -> C11
                                             IsCredentialRefusal(exception)         -> bool  (401 only; the one classifier)
                                             IsPermissionRefusal(exception)         -> bool  (403)
    (suggested names; DELIVER may rename them)
```

Jira ladder (all text from the DISCUSS copy table; the class owns it):

| read | 401 / 403, Direct | 401 / 403, ViaAtlassianGateway | other status | no answer | unparseable body |
|---|---|---|---|---|---|
| Validation | C1, `authentication_failed`, no field | C1o, `authentication_failed` | C2v, `unexpected_answer` | `connection_failed`, field Url (as today); **a timeout joins this column** (today it reaches the catch-all and reads `validation_failed`) | `validation_failed` (the catch-all stays, Decision 2) |
| FieldList | C1 / C8 detail, `authentication_failed` | C1o | today's `FieldListRefused` text | C3-style, `connection_failed` | today |
| BoardList | C1, `authentication_failed` | C1m, `boards_not_offered_to_sign_in_method` | C2, `unexpected_answer` | C3, `connection_failed` | new C9 (below), `unreadable_answer` |
| BoardRead | C1 (C4) | C1m (C4) | C4 other-status text | C4 unreachable text | C9 for board |

Rules that go with it:

- The class receives the host as the URL's authority only (scheme and host), never the raw option value, so no
  userinfo, query string or token can reach a sentence (AC "no message contains the email or token").
- Technical detail stays `"{Verb} {route} answered {code} {reason}."` as today; no header, no body.
- **The 401/403 -> credential sentence rule exists once**, in `FromStatus`. Connector code never decides
  "is this a credential refusal" by itself; it passes the status.
- Gateway routing is decided by the connector (`RoutesViaAtlassianCloudGateway`) and passed in as `signIn`; the
  verdict class does not know authentication-method keys.
- **One new sentence the DISCUSS table lacks, C9** (unparseable board-list or board answer, D6 "unparseable body"):
  "Jira answered the list of boards with something Lighthouse could not read. Try again in a moment; if it keeps
  happening, check that nothing between Lighthouse and Jira (a proxy) rewrites the answer." Proposed for the
  DISTILL sketch walk; the board-read variant swaps "the list of boards" for "the configuration of this board".
- **C1 as designed (AFK-5).** N3 gives a 403 the credential sentence, and a Jira 403 can mean "no licence for
  Jira Software", not a revoked token. DISCUSS C1 says "and that the token has not been revoked or expired", which
  would be false for that 403. C1 is therefore reworded, still as **one** sentence for 401 and 403 (Q3 clarified,
  N2): "Jira refused the sign-in. Check the user name or email and the API token or personal access token,
  whichever this connection uses, and that this account is allowed to use Jira, then try again." It claims
  nothing about the token's life. Proposed for the DISTILL sketch walk (AFK-6); C1o and C1m are unaffected.
  Sketches 1 and 2 above show the DISCUSS wording and are superseded by this one.
- ADO: C6 keeps today's `ValidateConnection` text and its highlighted field (the PAT is the only credential, so
  there is no email ambiguity); the picker shows it unchanged, for a 401 only. A 403 is C10, never C6 (Decision 3).
  C7 gains the project name when one project is the cause (Decision 3).
- Method names, enum members, error codes (`boards_not_offered_to_sign_in_method`, `unreadable_answer`,
  `invalid_board_id`) and the log wording in this design are **suggested; DELIVER may rename them**. The
  sentences are what DISTILL pins.
- The Jira Information log line "Authentication is not valid" (D7) becomes two status-specific lines: a
  credential refusal at Information ("Jira refused the credential of {Connection} on {Read}: {Status}"), anything
  else at Warning naming the status. Both keep today's `RequestUri` and body fields.

### [REF] Decision 2: exception shape, the controller, what is removed

**Exceptions.** Reuse `JiraReadException` and `AzureDevOpsReadException` as they are: the read methods throw them
through their existing public constructors, carrying a failed `ConnectionValidationResult` whose message is F1 or F2.
No new exception type: the controller already catches the abstract base.

**Catching (board reads only).** The board reads (list and information, both connectors) no longer turn any failure
into an empty value. **Every failure gives F1 or F2, and there is no uncaught case**:

| What happened | `{answer}` |
|---|---|
| A non-success status | that status and its reason, e.g. `503 Service Unavailable` |
| What `JiraCouldNotBeAsked` recognises as no answer (`HttpRequestException`, `TaskCanceledException`) | `no answer` |
| Anything else: unparseable JSON, well-formed JSON Lighthouse cannot use, an unexpected exception | `an answer Lighthouse could not read` |

An unexpected exception on a board read is logged with its stack trace, so a bug in the connector is visible in the
log and never an empty list. `board_filter_unreadable` passes through as it is: it is the one exception to F2, and the
only `WorkTrackingReadException` that keeps its own sentence on the picker. Paging: a failure on page 2 or later fails
the whole list; the pages already read are discarded (a half list is the same lie as an empty one).

**Partial Jira board reads (maintainer decision, 2026-11-04).** `GetBoardInformation` reads more than the
configuration: the status list, `board/{id}/issue` and the work item type list. A failure of any of them now fails the
board with F2, so Confirm stays disabled, instead of returning a board with no states or no types. Three things to
watch:

- The status list and work item type list already raise `JiraReadException`, with a sentence written for other
  screens. These two sites must turn it into F2, not pass it through, and `HowJiraAnswered` reads
  `503 (Service Unavailable)` where F2 wants `503 Service Unavailable`. A `200` with an unusable body arrives there as
  `200 (OK)` and reads `an answer Lighthouse could not read`.
- `GetBoardInformationFromJira` awaits the query last, with a comment saying it is the only one of the three reads
  that can throw. That stops being true: await all three together, so no failure is left unobserved.
- A board that really holds no work items answers `{"issues":[]}`. That is a real answer, not a failure. So is a
  status or type id the list lacks: that stays a warning and the id is left out.

**`WizardsController`: unchanged.** It already turns any `WorkTrackingReadException` into `400 {verdict}`.
The wire change is only that Jira and ADO now take that path (`200 []` -> `400`), the envelope ADR-126 already
ships for ServiceNow. `IBoardInformationProvider` is unchanged; a genuine `200` with zero boards stays `200 []`.

**`ValidateConnection` and health: unchanged, for both connectors.** T1 replaces one string, at
`JiraWorkTrackingConnector.cs` line 451. Status logic, timeout handling, `ConnectionHealthService` and ADR-184 are not
touched, so a 503 on Test connection still reads as an authentication failure: a known defect left out of this Bug.

**What is removed, listed.**

| Site | Today | Pinned by |
|---|---|---|
| Jira `GetBoards` bare `catch`, line 511 | returns `[]` | `A_board_list_from_a_Jira_that_cannot_be_reached_reads_as_no_answer`, `A_board_list_that_times_out_reads_as_no_answer`, `A_board_list_Lighthouse_cannot_read_says_so` |
| Jira `GetBoardsFromJira`, line 1124 | returns `[]` on a non-success status, on any page | `A_board_list_Jira_answers_with_a_failure_carries_that_answer`, `A_failure_on_the_second_page_fails_the_whole_board_list` |
| Jira `GetBoardInformation` `catch (Exception)`, line 532 | returns an empty `BoardInformation` | `A_board_from_a_Jira_that_cannot_be_reached_reads_as_no_answer`, `A_board_read_that_times_out_reads_as_no_answer`, `A_board_Lighthouse_cannot_read_says_so` |
| Jira `GetBoardInformationFromJira`, lines 724-730 | returns an empty `BoardInformation` on a non-success `/configuration` | `A_board_Jira_answers_with_a_failure_carries_that_answer` |
| Jira `MapStatusToCategory`, lines 785-790 | a refused status list gives no states | `A_board_Jira_hands_over_only_in_part_carries_the_answer_of_the_part_it_refused`, `A_part_of_a_board_Lighthouse_cannot_read_says_so`, `A_part_of_a_board_that_cannot_be_reached_reads_as_no_answer` |
| Jira `GetItemTypesForBoard`, lines 866-872 and 889-896 | a refused `board/{id}/issue` or work item type list gives no types | the same three tests |
| ADO `GetBoards`, line 400 | returns `[]` | `A_project_list_nobody_answered_reads_as_no_answer` |
| ADO `GetBoardsForProject`, lines 637-640 | returns `[]` | `One_project_failing_among_others_fails_the_whole_list_and_names_only_that_project` and its neighbours |
| ADO `GetBoardInformation`, line 443, and its `Length != 2` guard, lines 413-417 | returns an empty `BoardInformation` | `A_board_nobody_answered_for_reads_as_no_answer`, `A_malformed_board_id_is_a_failed_read_and_never_an_empty_board` |

**Each Azure DevOps project's failure is logged inside its own task, before aggregation.** `GetBoardsForProject` writes
its Error line where it catches the failure, then returns the failure as its outcome. The aggregate only composes F1,
so two failing projects leave two lines, and neither depends on the other
(`Each_failing_project_keeps_its_line_in_the_error_log`).

**Catch-alls that stay (none is a board read; none returns an empty value).** `ValidateConnection` on both connectors,
`ValidateTeamSettings` and `ValidatePortfolioSettings` on both, the write paths `TryWriteFields` and `TryPatch`, and the
Jira release and membership reads. All out of scope and unchanged.

### [REF] Decision 3: Azure DevOps, a failing project in a multi-project list (decided: fail the whole list)

**Decided by the maintainer (2026-11-02): Option A, fail the whole list** (D10 as written). The swallow in
`GetBoardsForProject` (the `catch (Exception)` that returns `[]`, `AzureDevOpsWorkTrackingConnector.cs` lines
637-640) goes; a per-project read returns an outcome (its boards, or its failure), never `[]` for a failure, and the
aggregate decides. A project with zero boards is a genuine empty contribution. **Any failure of any project, or of
the project list, fails the whole list with F1**, whose `{answer}` names each failing project and what Azure DevOps said
about it. There is no credential or permission branch (2026-11-04): a 401, a 403, a disabled Boards service and a
timeout take the same path. A trailing full stop in Azure DevOps' message is trimmed, because F1 adds its own. Each
failing project keeps its Error-level log line.
Holds "a failing project never reads as having no boards" by construction; no new UI state.
Accepted consequence: a picker that works today (a PAT that cannot see one project, a project with Boards disabled)
becomes unusable for every project; the Team's query can still be typed by hand. Test pins: one of two projects
failing fails the list naming only it; two failing are both named; a project nobody answered for reads `no answer`.

Rejected, one line each:

- **AFK-1 (skip non-credential failures while others answer):** the list silently lacks the skipped project, which
  the admin cannot tell from "that project has no boards", contradicting D10 and US-02 example 4 / UAT 5.
- **B (partial list plus a notice):** needs a new response envelope (`{boards, notices}`), a `wizardService` change
  and a new picker state, a wire break on a UI-only endpoint, for a regression the hand-typed query already covers.

Also rejected outright: "fail only when every project fails, otherwise partial and silent" (it is AFK-1 without the
log line).

**`GetBoardInformation` malformed board id (slice 02).** The guard at lines 413-417 (`Length != 2`) returns an
empty `BoardInformation`. An empty object is truthy on the frontend, so `BoardWizard` enables Confirm
(`!boardInformation` is false, line 173) and hands an empty board back as if it had been read. That is the same
lie as the empty list, so it becomes a typed `AzureDevOpsReadException` carrying F2, with an answer saying the board id
is not one Azure DevOps gave. Its `catch (Exception)` at line 443 loses its empty return too (Decision 2).

### [REF] Decision 4: frontend copy, how the refusal text arrives, and the disabled button

Refusal text: **no change**. `wizardService` already surfaces `ApiError.message` from the `400` body and
`refusalOr` shows it; Jira and ADO simply start arriving there. Board information failures go through the
same line (`setBoardInformation(null)` keeps Confirm disabled).

Empty-list copy: E1 replaces `NO_BOARDS` in `BoardWizard`, one constant for every connector, so no prop and no
per-connector table. ServiceNow's picker loses the table-and-filter hint, which the maintainer accepted; the detail
stays in `servicenow.md` line 187.

**The one frontend place that decides a wizard is unusable.** Suggested: `isWizardUsable(wizard, authenticationMethodKey)`
in `models/DataRetrievalWizard/DataRetrievalWizard.ts`, next to `IDataRetrievalWizard`. Its input is the registry entry
and the connection's `authenticationMethodKey`; the `jira.board` entry lists the sign-ins it cannot serve, in an
optional field, along with the reason D1. `GeneralSettingsComponent` asks it to disable the button, and
`useCreateWizard` asks it to skip the load-data step; nothing else compares sign-in keys. The name may change, the
single place may not. It sits in the models module because five test files mock `components/DataRetrievalWizards`
whole, so a new export there reads as undefined in about 45 of their tests.

**DELIVER item:** add `JiraOAuth: "jira.oauth"` to `AuthenticationMethodKeys` in
`models/WorkTracking/WorkTrackingSystemConnection.ts`, where it is missing today, so the entry can name both sign-ins.
`WorkTrackingSystemConnection.authenticationMethodKeys.test.ts` pins the whole list.

**Loading look and failure look (slow-connection rule), for the picker and the board read:**

```
LOADING (list)                   LOADED                         FAILED (list)                  EMPTY (200 [])
+----------------------------+   +-------------------------+    +-------------------------+    +-------------------------+
| Select Jira Board          |   | Select Jira Board       |    | Select Jira Board       |    | Select Jira Board       |
|                            |   | [ Board          v ]    |    | [ Board         v ](off)|    | [ Board         v ](off)|
|        ( spinner )         |   |                         |    | red: F1                 |    | red: E1                 |
|  (no field, no text)       |   |                         |    |                         |    |                         |
|        [Cancel][Confirm off]   |      [Cancel][Confirm]  |    |    [Cancel][Confirm off]|    |    [Cancel][Confirm off]|
+----------------------------+   +-------------------------+    +-------------------------+    +-------------------------+

BOARD READ (after picking)  loading: BoardInformationDisplay spinner, Confirm off
                            failed : red line F2, board info cleared, Confirm off
```

What exists and is reused (no new control): the list's loading look is today's `CircularProgress` that
*replaces* the field and the red line (so no empty field and no stale text can read as "no boards" while it is
on its way); the failure look is the red `Typography color="error"` with the disabled field; the board read's
loading look is `BoardInformationDisplay loading`. **Gaps found and closed here, both inside slice 01:**
(1) on a failed list `BoardWizard` leaves the previous `boards` state in place; set it to `[]` in the `catch`
so a failure can never sit beside an old list; (2) the empty-list branch (`fetchedBoards.length === 0`) is the
only one that may use E1. DISTILL pins both states; E2E needs no new wait (`data-widget-status` is the
dashboard-widget contract, the dialog uses its spinner). Check by hand at a throttled connection in DELIVER.
Visualization skill: N/A, nothing charts data. No keyed TanStack query is introduced: the dialog is mounted per
opening (`open={true}` on mount), so a late answer for a closed dialog lands on an unmounted component; migrating the
picker to a query is out of scope for a Bug.

**Gap 3, a superseded board-information answer (slice 01).** `handleBoardChange` (`BoardWizard.tsx` lines
98-107) starts a read per selection and nothing ties an answer to the selection that asked: with a slow board A
and a fast board B, A's late answer overwrites B's `boardInformation` and error, and Confirm then hands the wrong
board's information back. Smallest guard first: a `latestSelection` ref holding the id of the most recent
selection (null when cleared); `fetchBoardInformation` applies its result, its error and its `setFetchingBoardInfo(false)`
only while `latestSelection` still equals the board it asked for. Rejected: an `AbortController` (the
`wizardService` call would have to take a signal and `ApiService` pass it through, three files for the same
effect; stays the next step if the list read ever needs cancelling) and a request counter (the same guard with a
number instead of an id, no gain). The same ref also stops a late answer after the selection was cleared.

**Fallback sentences.** Today's two canned lines ("Failed to load boards. Please try again.", "Failed to read the
board. Please try again.") stay, reached only when the failure never came from the backend.

### [REF] Decision 5: test strategy (high level; scenarios belong to DISTILL)

- **Connector tests through the existing ports with the existing HTTP stubs** (Jira) **and fake SDK clients** (ADO),
  in untagged classes. Jira board list: 401, 403, 503, no answer, timeout, an unusable body, **a failure on page 2
  after page 1 succeeded**, `200 []`. Jira board read: 401, 403, 404, 503, no answer, timeout, an unusable body, and
  each part of a partial read. T1 with its code, detail and field. ADO: a failing project, two failing, an unusable
  project list, zero projects, projects without boards, a board read failing, **a malformed board id gives F2, never
  an empty `BoardInformation`**.
- **Driving port**: `WizardsController` over the real connector, for each connector and each read, asserting the
  `400` and the exact sentence.
- **Frontend (Vitest + RTL)**: `BoardWizard` shows a refusal message verbatim, never E1, with the field and
  Confirm disabled; E1 on an empty list; a board-read failure keeps Confirm disabled; **a superseded answer is
  ignored**: pick board A, pick board B, resolve A's answer last (both a success and a refusal): B's state is
  untouched, Confirm reflects B only, and the spinner of B is not cleared by A; a late answer after the selection was
  cleared changes nothing. The disabled button with D1 on the settings and create screens, with no board call. The
  `BoardWizard.test.tsx` assertions that pin "Visual Task Board" change to E1. No new Playwright spec (E2E is a thin
  sanity check; the picker's happy path is covered).
- **No architecture rule** against `catch { return []; }` (Q8, binding). Per-connector failure-path tests are the guard.
- **DELIVER live check** (not CI): a deliberately wrong credential on a real Jira and ADO sandbox.

### [REF] Component diagram (hexagonal view; N = new, M = modified, - = unchanged)

```
 DRIVING ADAPTERS                APPLICATION / PORTS                 DRIVEN ADAPTERS (trackers)
 ----------------                -------------------                 --------------------------
 BoardWizard.tsx        (M: E1, latest-selection guard)
 Settings, create flow  (M: disabled button, D1)
   isWizardUsable       (N, pure)
   registry entry       (M)      IBoardInformationProvider (-)       JiraWorkTrackingConnector     (M)
                             --> GetBoards / GetBoardInformation     --> |- GetBoards, GetBoardInformation and its
 WizardsController      (-)           ^                                  |  parts (M: throw F1 / F2; nothing quiet)
   catch abstract only                |  throws                          |- ValidateConnection (M: one string, T1)
                                      | WorkTrackingReadException (-)  JiraReadException          (-)
 Connection page (Test) (-) --> IWorkTrackingConnector (-)           AzureDevOpsWorkTrackingConnector (M)
   shows verdict text           ValidateConnection (shape -)           |- GetBoards, GetBoardsForProject,
                                                                       |  GetBoardInformation (M: throw F1 / F2)
 ConnectionHealthService (-)                                         AzureDevOpsReadException      (-)
                                                                    ServiceNow, Linear connectors   (-)
 DOMAIN (Models): ConnectionValidationResult (-), Board (-), BoardInformation (-)  -- no domain change
 Events: none.  Persistence: none.  New external call: none (same Jira/ADO endpoints as today).
```

### [REF] Context map, glossary, ARCHITECTURE.md (design answer)

Unchanged from DISCUSS: Tracker integration only, no new edge, no event. The DISCUSS-proposed glossary row
"Read refusal" still applies. No `ARCHITECTURE.md` section 4 change: there is no new class to describe. ADR-126 gets
its dated "Adoption" note. ADR-184 gets none: its classifier is untouched.

### [REF] Handoff to DISTILL / DEVOPS

- **Slices**: 01 = Jira reads, partial reads included, the T1 string, E1, `boards = []` on failure, the
  superseded-answer guard, the disabled button with D1, and `JiraOAuth` in the frontend key list. 02 = the ADO read
  paths, including the malformed-board-id failure. Order and one-direction dependency as in DISCUSS. Method and
  string names in this design are suggested; DELIVER may rename them.
- **Gate (AFK-6)**: the DISTILL sketch walk of all new copy gated DELIVER; it is done, and the 2026-11-04 block
  holds its outcome.
- **External integrations** (annotation for platform-architect): Jira REST (`/rest/agile/1.0/board`, `.../configuration`,
  `/myself`, `/field`) and Azure DevOps Work/Project APIs, both already integrated. Contract tests recommended
  for Jira and Azure DevOps (consumer-driven contracts, for .NET e.g. PactNet) to catch a changed status or error
  body for the refused-credential case; this Bug adds none and relies on the live sandbox check.
- **Usage-data event**: DEVOPS to confirm the DISCUSS pre-answer, N/A (a failure is reported, nothing is used).
- **Enforcement tooling**: none new (Q8); the existing ArchUnitNET suite keeps guarding the health single writer.
- **Sketch walk at the start of DISTILL**: done, see the 2026-11-04 block.

### [REF] HELD for the maintainer

- **HELD-1, ADO multi-project list (Decision 3): decided, fail the whole list (Option A).** Any project failing gives
  F1 naming the failing project(s). Every ADO scenario can be written now, the multi-project one included.
- N3 (Jira 401 = 403 on the board list), AFK-5: re-confirmed by the maintainer. It stands, and diverges from
  ADR-126's ServiceNow 403 rung (`insufficient_permissions`) as a deliberate, recorded divergence.

Closed since the first draft: ADO 403 on Test connection (AFK-2 reversed: today's mapping stays, no
change). The Jira timeout on Test connection was decided in the first draft and dropped by the 2026-11-04 block.

## AFK decisions (2026-11-02, orchestrator; the maintainer reviews them on return)

History. The maintainer's decisions below and the 2026-11-04 block replaced what these say about C-numbered sentences.

| # | Decision | Why |
|---|---|---|
| AFK-1 (DESIGN Q1) | **ADO multi-project list, a middle way, not A or B:** a **credential refusal** (401/403) on any project fails the whole list with C6. **Any other per-project failure** (Boards disabled, 404, one project the PAT cannot see) **skips that project** with a Warning log line naming it, **as long as at least one project answered**. If **no** project answered, the list fails with C7 naming the failing project(s). No wire or UI change. | Option A would turn a picker that works today (a PAT that cannot see one project, a project with Boards disabled) into a hard failure for every project, which is a regression with a wide blast radius. The defect being fixed is "a failure reads as an empty list"; skipping one project while others answer never produces an empty list. A refused credential is never skipped. Option B (partial list + notice) stays the next step if reports come in. **Flagged for the maintainer.** |
| AFK-2 (DESIGN Q2) | ~~Option A: an ADO 403 joins 401 as `authentication_failed`~~ **Revised after the DESIGN review: an ADO 403 is NOT a credential verdict anywhere.** Test connection keeps today's mapping (health `Unreachable`); the ADO picker shows a permission sentence for a 403 ("Azure DevOps refused access to ...: the account or token lacks permission"), never C6. | ADR-184: where the connector cannot say it was authentication, the state is `Unreachable`, never "Authentication failed"; ADR-126's ServiceNow precedent maps 403 to `insufficient_permissions`. No maintainer decision covers ADO 403, so the precedent wins. |
| AFK-1 status | **AFK-1 is now HELD for the maintainer** (review blocker B1): it contradicts D10, US-02 example 4 and UAT 5 ("a failing project must never read as that project having no boards"). Options for the maintainer: **(A)** fail the whole list on any project failure (D10 as written; regresses pickers that work today when one project has Boards disabled or is invisible to the token); **(AFK-1)** skip non-credential project failures while others answer (no regression, but the list silently lacks that project); **(B)** partial list + a notice naming the skipped projects (honest and no regression, but a new response envelope and picker state). DESIGN documents all three; DISTILL leaves the multi-project scenario open until decided. | Blast radius in both directions; the maintainer's call. |
| AFK-5 (review M1) | **The maintainer's N3 (Jira 401 = 403 on the board list) stands**, and is recorded as a deliberate divergence from ADR-126's ServiceNow 403 rung in an adoption note. C1's wording for a 403 must not claim the token was revoked or expired (the licence-403 case). **Flagged for the maintainer** so they can re-confirm N3 with the precedent in view. | N3 is a maintainer decision; the orchestrator does not reverse it, only makes its consequence visible. |
| AFK-6 (review m9) | **The DISTILL sketch walk gates DELIVER** for all new copy (C1..C9), C9 included. | CLAUDE.md: no UI ships unseen. |
| AFK-3 | **C9 (an unparseable board answer) is accepted as a new sentence**; its wording and C7 naming the failing project go into the DISTILL sketch walk. | Small, additive; copy is reviewed at DISTILL anyway. |

## Wave: DEVOPS

Mode: PROPOSE, AFK, proportionate to a bug fix: no new infrastructure, pipeline stage, config, migration or
deployment unit. Answers below are explicit; nothing is a silent N/A.

### [REF] Answers

1. **Usage-data event: N/A, because** this is a bug fix that makes an existing screen tell the truth; the
   feature is not a new thing whose adoption we would count, and a failed board read is not an action a user
   chooses. An event for "picker showed a refusal" would need the connector kind or the verdict code as a
   property, which is not worth a catalogue entry. `UsageDataEventName` stays at 24 (`ReportCombinedPdfExportStarted`),
   `docs/settings/usagedata.md` is not touched. (If the maintainer later wants a signal, the only acceptable
   shape is name-only, value 25.)
2. **CI and pipeline: no change.** No new test category, no new workflow, no new job. All new tests are plain
   NUnit unit tests (connector failure paths with a faked HTTP handler or SDK client) and Vitest tests; they run in
   the existing `ci_backend.yml` default filter (`Category!=Integration`) and `ci_frontend.yml`. The live-connector
   categories (`JiraIntegration`, `AdoIntegration`) are opt-in inputs of `ci_backend.yml` and are **not needed**
   and must not be added to the fix. The DESIGN's live check against a deliberately wrong credential on a real
   Jira and ADO sandbox is a **manual DELIVER step**, run by hand against the maintainer's sandboxes, never in
   CI, recorded in the DELIVER log (wrong email, wrong token, ADO bad PAT). For a scoped token and for OAuth the live
   check covers the disabled button and Test connection, not a board read, because the picker never opens for them.
   Linear: not touched, so the key shared with CI is not exercised. `ci_e2e.yml` and the verify workflows: no new
   Playwright spec (E2E stays a thin sanity check; the existing wizard walking skeleton is unchanged).
   **Placement rule (review F1):** CI runs `Category!=Integration` (`ci_backend.yml:114`), and
   `JiraWorkTrackingConnectorTest.cs` (lines 17-18) and `AzureDevOpsWorkTrackingConnectorTest.cs` (lines 16-17)
   carry `[Category("Integration")]`, so a test added there is silently skipped in CI. New failure-path tests go
   in **untagged** classes (`JiraFieldLookupTest` style, or a new untagged file per connector), never in those two
   classes. Side finding: the existing board happy-path tests in those two classes are CI-excluded today; this
   fix does not change that, and the new untagged tests are what put the board reads under CI for the first time.
   Static gates apply unchanged (review F6): `dotnet build` with `TreatWarningsAsErrors`, `pnpm build` (Biome +
   `tsc`), and no new SonarCloud issues.
3. **Observability: one operator-visible change, an improvement.**
   - Jira (decided by the maintainer, 2026-11-04): one Error line per failed Jira board read, carrying `{answer}`,
     with no status split, the same as Azure DevOps. It replaces the "Authentication is not valid" Information line at
     `JiraWorkTrackingConnector.cs:1118` and the Warning lines of the failed reads: `:727` (the configuration), `:787`
     (a refused status list), `:868` (the board issue read) and `:892` (the work item type list), since the user now
     sees each of these failures. The missing-id warnings at `:794` (status) and `:900` (work item type) stay Warnings:
     the read succeeded and one id is left out. The picker no longer classifies by status, so neither does its log
     line. The test pins the truth of the line (`503 Service Unavailable`, no "Authentication is not valid"), not its
     level.
   - Azure DevOps (review F2): today the ADO board reads log at **Error** (`AzureDevOpsWorkTrackingConnector.cs`
     lines 402, 445, 639). Rule: a line where the read fails and the user sees the failure stays **Error**
     (unchanged). :402, :445 and :639 all stay Error, because the user sees every one of these failures in the picker.
     No ADO log level changes. Nothing may log the email, token or a header value.
   - The old text "Authentication is not valid" appears only at `JiraWorkTrackingConnector.cs:442` and `:1118`
     (repo-wide grep done in this wave, hits elsewhere are this folder's own documents). No alert, dashboard or
     test keys on it. `:1118` is replaced with no compatibility shim. `:442` keeps its phrase for a Test connection
     503 until the known defect is fixed: it sits in `ValidateConnection`, which this Bug leaves alone.
4. **Release and rollback: trivial.** No EF migration, no setting, no feature flag, no data change. Rollback is
   reverting the commit(s) (or the previous release), which restores `200 []` for failures. The wire change
   (`200 []` becomes `400 {verdict}` on a failure of Jira and ADO board list/information) is consumed by the
   frontend `BoardWizard` only, shipped in the same build as the backend, so no version skew. Checked:
   `grep -ril "wizards"` and `/boards` over `/storage/repos/lighthouse-clients` (ts, md, json, no node_modules)
   returns nothing, so the CLI/MCP does not call the wizard endpoints and needs no version bump or release.
   Self-hosted users running an old frontend against a new backend does not occur (one bundle).
5. **Mutation testing (per-feature, 80% minimum), targets:**
   - Stryker.NET: the failure and empty-list rules of the board reads in `JiraWorkTrackingConnector` (`GetBoards`,
     `GetBoardsFromJira`, `GetBoardInformation`: what fills `{answer}`, and that an empty answer stays empty); the
     three partial reads, each with the rule that a refused, unusable or unanswered read fails the board with F2: the
     status list (`MapStatusToCategory`; an id the list lacks stays a warning), the work item type list and the board
     issue read (both `GetItemTypesForBoard`; a board with no work items stays a real answer), with
     `board_filter_unreadable` as the one exception, which keeps its own sentence; and the whole-list failure in
     `AzureDevOpsWorkTrackingConnector` (`GetBoards`, `GetBoardsForProject(s)`, `GetBoardInformation`). **Enabling the
     partial-read tests is a DELIVER step before the Stryker run:** skipped tests kill nothing, so those mutants would
     all survive. Per the Stryker traps: mutate by file, skip the acceptance suite, run last on
     frozen code, and kill survivors with targeted tests rather than hours of reruns. **Config (review F4):**
     start from `docs/feature/fix-jira-dc-portfolio-refusal/mutation/stryker.6019.connector.json`, which has
     `thresholds.break` 0; set **`break: 80`** for these targets. Its name-based test-case filter also runs the
     Integration-tagged connector classes, which helps the kill rate (the new tests in untagged classes still carry
     the kills CI sees). DELIVER outputs, named explicitly: the `mutate` globs (the two connector files) and the
     config path, `docs/feature/bug-6499-jira-auth-failure-shows-no-boards/mutation/stryker.6499.connector.json`,
     plus the StrykerJS config.
   - StrykerJS: the disabled-button guard (`isWizardUsable` or its final name, and both places that ask it), E1 on the
     empty branch only, and the guard that keeps Confirm disabled on a failure.
   - Not targets: `WizardsController` and `ConnectionHealthService` (both unchanged).
6. **Docs at finalize (DELIVER, not now):** `docs/concepts/worktrackingsystems/jira.md` line 73 (the "board
   wizard is empty" row for OAuth) and line 92 (the scoped-token note says Lighthouse displays "No Boards
   available"; after the fix the button is disabled and its tooltip D1 says why): reword to the shipped behaviour, and
   add a short troubleshooting line that a failed board read now says what Jira answered in the picker.
   `azuredevops.md` line ~194 (what the picker shows): add that a refused PAT or a failing project is named in the
   picker instead of an empty list. Per-feature screenshots: only if the DISTILL sketch walk adds
   a visible new state worth a shot (decide at finalize, say so explicitly). Also owed from DISCUSS: the ADR-126
   adoption note, the glossary row "Read refusal" in ADR-270, `jobs.yaml` REVISED note. No release
   notes at finalize (written at /release). Terminology: copy and docs use the configurable terms.

### AFK decisions (DEVOPS)

| # | Decision | Why |
|---|---|---|
| DEVOPS-1 | No usage-data event (N/A, bug fix). | Nothing to count; avoids a catalogue entry whose only useful property would be a verdict code. |
| DEVOPS-2 | No new test category, workflow or CI job; new failure-path tests live in untagged classes, never in the two Integration-tagged connector test classes; live Jira/ADO check is a manual DELIVER step against sandboxes. | Live categories are opt-in, flaky and cost API quota; unit tests cover the failure paths deterministically. |
| DEVOPS-3 | Decided by the maintainer: Jira writes one Error line per failed board read, with no status split, the same as Azure DevOps, carrying `{answer}`. It replaces `:727`, `:787` (a refused status list), `:868`, `:892` and the old phrase at `:1118` (grep done, no consumer). The missing-id cases at `:794` and `:900` stay Warnings. `:442` keeps its phrase for a Test connection 503 until the known defect is fixed. ADO: lines :402/:445/:639 keep Error. | The user sees every one of these failures, and the picker no longer classifies by status, so one rule fits both connectors. The old text was false for a 503. |
| DEVOPS-4 | Struck on 2026-11-04: Test connection's health handling is unchanged, so a Jira 503 there still stores `AuthenticationFailed`. | A known defect left out of this Bug. |
| DEVOPS-5 | No Lighthouse-Clients change or release. | Clients do not call the wizard endpoints (grep empty). |
| DEVOPS-6 | Rollback = revert; no feature flag. | No data or schema change; flag would add a branch for a pure correctness fix. |
| DEVOPS-7 | Mutation targets as listed in answer 5: the failure and empty-list rules, the disabled-button guard and the ADO whole-list failure. | Per-feature strategy in CLAUDE.md. |

HELD for the maintainer: none left. HELD-1 (ADO multi-project list: fail the whole list) and the N3 re-confirmation
(AFK-5) are decided in the section below; the DISTILL sketch walk (AFK-6) gates DELIVER.

## Maintainer decisions on the held items (2026-11-02, evening)

Binding; they close every HELD item above.

| # | Decision |
|---|---|
| HELD-1 (ADO multi-project list) | **Fail the whole list.** Any project whose board read fails makes the picker show the failure sentence naming the failing project(s) (C7); a refused credential on any project shows C6. This is Option A of DESIGN Decision 3 and DISCUSS D10 as originally written; AFK-1 (skip) and Option B (partial + notice) are rejected. Accepted consequence: a project with Boards disabled, or one the token cannot see, blocks the picker for every project; the Team's query can still be typed by hand. ADO board-read log lines keep Error level. |
| AFK-5 / N3 re-confirmed | **Keep 401 = 403** on the Jira board list: one credential sentence for both, worded without "revoked or expired". Recorded as a deliberate divergence from ADR-126's ServiceNow 403 rung. |
| Follow-up Story (Q6, "show why a refresh failed") | **Not created.** The draft stays here for reference only; it is not tracked in ADO. |

## Maintainer decisions: simplified copy (2026-11-04, start of DISTILL)

Binding, and they **supersede** everything above that disagrees, including Q3, Q4, Q7, N1, N2, N3, D2, D4, D8,
D9, the DESIGN decisions on per-status classification, and the copy table C1 to C11. The fix itself stands:
Jira and Azure DevOps board reads stop turning failure into emptiness (D1, D10), and an Azure DevOps list fails
as a whole, naming the failing project (HELD-1). Sketch walk done. Every question is answered, including those the
DISTILL review raised, which are at the end of this block.

Why: about 15 sentences said the same thing in different words. A template carrying the tracker's own answer
tells the reader as much, and the picker no longer has to tell a 401 from a 403 or a 503.

### The copy

| Id | Where | Text |
|---|---|---|
| F1 | Board list failed, Jira and Azure DevOps, every cause | Lighthouse could not read the boards from {tracker}: {answer}. Check the connection's URL and sign-in details, then try again. |
| F2 | Board information failed, Jira and Azure DevOps, every cause except the one named under the DISTILL review answers | Lighthouse could not read this board from {tracker}: {answer}. Check the connection's URL and sign-in details, then try again. |
| E1 | Empty list, every connector including ServiceNow; replaces `NO_BOARDS`, no per-connector table | No boards are available to this connection. The account may not have access to any board, or none of the boards it can see can be used by Lighthouse. |
| T1 | Jira Test connection, every Jira sign-in, message of the existing `authentication_failed` result | Authentication failed for Jira. Check all of this connection's sign-in details. |
| D1 | Tooltip on the disabled "Select Jira Board" button | Jira does not offer boards to scoped API tokens or Jira Cloud (OAuth). Enter the query by hand. |

`{answer}` is what the tracker said: `401 Unauthorized`, `503 Service Unavailable`, `no answer` for an
unreachable host or a timeout, `an answer Lighthouse could not read` for unparseable JSON and for well-formed JSON
Lighthouse cannot use (a board entry with no id, a `/configuration` with no columns), or Azure DevOps' own message
with the failing project named. A trailing full stop in Azure DevOps' message is trimmed, because F1 and F2 add their
own. A malformed Azure DevOps board id is F2 with an answer saying so, never an empty `BoardInformation`.

### Decisions

- **Failure sentence**: F1 and F2, the shorter wording without the email hint. No credential classification on
  the picker for either connector: no 401/403/5xx branches, no separate Azure DevOps 403 sentence.
- **Empty list**: E1 for all four connectors. ServiceNow's failure verdicts are unchanged; only the frontend
  empty line changes. ServiceNow's picker loses the table-and-filter hint, which is accepted: that detail stays in
  `servicenow.md` line 187.
- **Scoped API token and Jira Cloud OAuth**: "Select Jira Board" is **disabled, with the reason in a tooltip**
  (D1), on the Team and Portfolio settings and create screens, decided from the connection's
  `authenticationMethodKey`. The picker never opens, so no board call is made. The create flow skips the
  load-data step when no wizard is usable, and shows the same disabled button on Configure. A disabled MUI button
  fires no pointer events, so the tooltip needs a wrapping element.
- **Test connection**: T1 replaces the one string. Code, technical detail and the highlighted API Token field
  stay as they are. `ValidateConnection`'s status logic and `ConnectionHealthService` are untouched.

### Dropped from this Bug

C1 to C11, the two new frontend fallback sentences (today's canned lines stay), the field-list rewording (a Jira
field-list 401 still says it needs to browse a project), and the `ValidateConnection` status and timeout changes.

**Known defect left out, with no Bug number:** a 503 on Test connection still reads as an authentication failure. No
follow-up Bugs go on the board for now.

`adr-draft-credential-verdict-per-connector.md` is superseded by this block. `adr-draft-ado-board-list-fails-whole.md`
is rewritten in terms of F1 and F2 and reads alone.

### Still in scope

The `latestSelection` guard, `boards = []` on a failed list, a failure on page 2 failing the whole Jira list, partial
Jira board reads (below), `JiraOAuth` in the frontend key list, the truthful log line, the failure-path tests, the live
sandbox check, `jira.md` lines 73 and 92 (now describing the disabled button), ADR-126, ADR-270 and `jobs.yaml` edits
at finalize.

### Maintainer decisions on the DISTILL review (2026-11-04)

Binding; they replace everything that was held or open in the first DISTILL pass.

| # | Decision |
|---|---|
| T1 for every sign-in | T1 is one neutral string for every Jira sign-in method, as in the table above. The token mention was dropped so the text fits every sign-in: an OAuth connection has no token to check. Code, technical detail and field stay. |
| F1 and F2 for every cause | They stay one sentence each, including for a 503 or a timeout, and keep "Check the connection's URL and sign-in details". Accepted because the status sits in the sentence. |
| Partial Jira board reads | **In slice 01.** A refused status list or `board/{id}/issue` inside `GetBoardInformation` now fails with F2, so Confirm stays disabled, instead of returning a board with no states or types. |
| `board_filter_unreadable` | Stays as it is, and is **the one exception to F2**: a board whose saved filter is refused keeps its own sentence naming the filter to share. |
| Log level | One Error line per failed Jira board read, with no status split, the same as Azure DevOps, carrying `{answer}`. DEVOPS answer 3 names the sites. |
| DELIVER rigor profile | `standard` for both slices. |
| Follow-up Bugs | None on the board for now. The 503 on Test connection is recorded above as a known defect left out, with no Bug number. |

### Estimate

About 2 days: slice 01 about 1.5, slice 02 about 0.5.

## Wave: DISTILL / [REF] Prior Wave Consultation

2026-11-04. The sketch walk was done before this wave started, and its outcome is "Maintainer decisions: simplified
copy" above. Copy ids in these sections (F1, F2, E1, T1, D1) are that block's.

| Artifact | Read |
|---|---|
| This `feature-delta.md`: the 2026-11-04 decision block first, then DESIGN Decision 5, DEVOPS answers and AFK-DEVOPS | yes |
| `slices/slice-01-jira-picker-says-why.md`, `slices/slice-02-ado-picker-says-why.md` (scope) | yes |
| `docs/architecture/atdd-infrastructure-policy.md` (inherited; one row appended) | yes |
| `docs/ci-learnings.md`, Vitest/RTL and NUnit sections | yes, applied |
| `docs/feature/bug-6458-typed-category-entries/feature-delta.md`, `docs/feature/epic-5074-blocked-items/distill/red-classification.md` (precedent) | yes |
| Separate `discuss/`, `design/`, `devops/` folders | none: those waves live in this file |
| `docs/product/kpi-contracts.yaml` | not applicable: DEVOPS records no usage-data event and no outcome KPI for this bug |

`[lang-mode]` C# and TypeScript, per the policy. `[policy-mode]` inherit. `[port-mode]` not applicable: the policy
excludes the Python state-delta port, `__SCAFFOLD__` stubs and property-based harnesses here.

## Wave: DISTILL / [REF] Reconciliation result

Reconciliation passed: no contradiction that blocks a scenario. The 2026-11-04 block settles the DISCUSS, DESIGN
and DEVOPS differences, and its "Maintainer decisions on the DISTILL review" settle what the first pass held. All
scenarios are written against F1, F2, E1, T1 and D1. How each point was closed:

1. **F2 "every cause" against the existing filter verdict.** Decided: `board_filter_unreadable` stays, the one
   exception to F2. `JiraBoardQueryAssemblyTest` keeps pinning it.
2. **Partial Jira board reads.** Decided: in slice 01, with F2. The new tests are in the inventory, and the tests that
   pinned the quiet degrade are under "Tests DELIVER changes".
3. **`{answer}` for Azure DevOps has no fixed format beyond naming the project.** Where no project is involved the
   tests pin the exact sentence. Where one is, they pin the sentence's two halves, the project and Azure DevOps'
   own message, so DELIVER may choose the separator.
4. **A double full stop.** The block now says a trailing full stop in Azure DevOps' message is trimmed. The tests use
   a message with no final full stop; the trim cases (project list, project, board) are DISTILL rows in
   `AzureDevOpsBoardReadFailureTest`, skipped until DELIVER.
5. **What counts as "could not read".** The block now defines `{answer}` to cover well-formed JSON Lighthouse cannot
   use, and the tests pin it.
6. **Log level.** Decided: one Error line per failed Jira board read, with no status split (DEVOPS-3). The test pins
   the truth of the line (no "Authentication is not valid" on a 503, status and reason named), not its level.
7. **`:442`.** The same untruthful phrase sits in `ValidateConnection`, which the block leaves untouched. No test
   asks for it to change.
8. **T1 on sign-ins without an email.** Decided: one neutral string for every Jira sign-in. See "T1 finding" below.

## Wave: DISTILL / [REF] Scenario inventory

No scenario tags appear in the code (project rule); this table is the only classification. Every row is skipped:
`[Ignore(...)]` with a plain reason, `it.skip` or `it.skip.each`. Kind: **D** driving port end to end, **E** error
path, **B** boundary or edge, **G** guard on behaviour the fix must keep.

**100 cases over 56 test methods: backend 74 over 43, frontend 26 over 13.** Error and edge methods are 47 of 56
(84%), against the 40% target. Guards are the other 9.

### Slice 01: `JiraBoardReadFailureTest`

| Scenario | Cases | Kind |
|---|---|---|
| An administrator asking for the boards of a Jira that refuses them is told what Jira said (F1, 401) | 1 | D, E |
| An administrator asking for a board of a Jira that refuses it is told what Jira said (F2, 403) | 1 | D, E |
| A board list Jira answers with a failure carries that answer (401, 403, 503) | 3 | E |
| A board list from a Jira that cannot be reached reads as `no answer` | 1 | E |
| A board list that times out reads as `no answer` | 1 | E |
| A board list Lighthouse cannot read says so | 2 | E |
| A board list that breaks unexpectedly is a failure and keeps its stack trace | 1 | E |
| A failure on the second page fails the whole board list | 1 | E |
| A board list over two pages still offers every board | 1 | G |
| A Jira that really has no boards for this account stays an empty list | 2 | G |
| A failed board list is logged with what Jira answered and not as a bad credential | 1 | E |
| A board Jira answers with a failure carries that answer (401, 403, 404, 503) | 4 | E |
| A board from a Jira that cannot be reached reads as `no answer` | 1 | E |
| A board read that times out reads as `no answer` | 1 | E |
| A board Lighthouse cannot read says so | 2 | E |
| A board read that breaks unexpectedly is a failure and keeps its stack trace | 1 | E |
| A board Jira hands over only in part carries the answer of the part it refused (status list, work items, work item type list; 403, 503) | 6 | E |
| A part of a board Lighthouse cannot read says so (the same three parts; page of HTML, `{}`) | 6 | E |
| A part of a board that cannot be reached reads as `no answer` (the same three parts) | 3 | E |
| A part of a board that times out reads as `no answer` (the same three parts) | 3 | E |
| A board Jira hands over whole still gives its states and types | 1 | G |
| A board that holds no work items yet is a real answer and not a failure | 1 | G |
| Test connection on a refused credential says to check all the sign-in details (T1, with code, technical detail and field) | 1 | E |

### Slice 01: frontend

| File | Scenario | Cases | Kind |
|---|---|---|---|
| `BoardWizard.test.tsx` | Says the same thing about an empty list on a `Board` and a `Team` picker (E1) | 2 | B |
| `BoardWizard.test.tsx` | Shows what the tracker said about a board and cannot be confirmed (F2) | 1 | E |
| `BoardWizard.test.tsx` | Takes away the boards of the last answer when the list is refused | 1 | E |
| `BoardWizard.test.tsx` | Keeps the newer board when the older one answers last | 1 | E |
| `BoardWizard.test.tsx` | Says nothing about the older board being refused after the newer one answered | 1 | E |
| `BoardWizard.test.tsx` | Keeps waiting for the newer board when the older one arrives first or is refused first | 2 | E |
| `BoardWizard.test.tsx` | Ignores an answer that arrives after the selection was cleared | 1 | E |
| `GeneralSettingsComponent.test.tsx` | Shows the picker disabled and says why (D1; scoped token and OAuth, Team and Portfolio) | 4 | B |
| `GeneralSettingsComponent.test.tsx` | Leaves the picker usable for `jira.cloud` and `jira.datacenter` | 2 | G |
| `CreateWizard.boardPicker.test.tsx` | Goes straight to Configure, with the picker disabled and the reason on it | 4 | B |
| `CreateWizard.boardPicker.test.tsx` | Going back from Configure returns to choosing the connection | 2 | B |
| `CreateWizard.boardPicker.test.tsx` | Still offers the picker on the Load Data step for `jira.cloud` and `jira.datacenter` | 4 | G |
| `WorkTrackingSystemConnection.authenticationMethodKeys.test.ts` | Includes every Jira sign-in, so the picker rule can name them | 1 | B |

### Slice 02: `AzureDevOpsBoardReadFailureTest`

| Scenario | Cases | Kind |
|---|---|---|
| An administrator asking for the boards of an Azure DevOps that refuses them is told what it said (F1) | 1 | D, E |
| An administrator asking for a board of an Azure DevOps that refuses it is told what it said (F2) | 1 | D, E |
| A rejected token on the only project fails the list and names the project | 1 | E |
| One project failing among others fails the whole list and names only that project | 1 | E |
| Two projects failing are both named | 1 | E |
| A status Azure DevOps sent without a sentence of its own still reaches the picker | 1 | E |
| Each failing project keeps its line in the error log | 1 | E |
| A project list nobody answered reads as `no answer` (unreachable, two kinds of timeout) | 3 | E |
| A project nobody answered for reads as `no answer` and is named | 3 | E |
| A board nobody answered for reads as `no answer` | 3 | E |
| A project list that breaks unexpectedly is a failure and keeps its stack trace | 1 | E |
| A project that breaks unexpectedly fails the list and keeps its stack trace | 1 | E |
| A board read that breaks unexpectedly is a failure and keeps its stack trace | 1 | E |
| A full stop ending what Azure DevOps said about the project list is not doubled (F1) | 1 | E |
| A full stop ending what Azure DevOps said about a project is not doubled (F1) | 1 | E |
| A full stop ending what Azure DevOps said about a board is not doubled (F2) | 1 | E |
| A malformed board id is a failed read and never an empty board | 4 | E |
| An organisation with no projects stays an empty list | 1 | G |
| Projects without boards stay an empty list | 1 | G |
| Boards of every project that answers are offered | 1 | G |

Tier B (state-machine property tests): not applicable; the policy excludes them here and the picker's journey is
short. The two copy-carrying `An_administrator_...` scenarios of each connector pin the exact final sentence once.

### Self-completeness audit

13 of 15, **COMPLETE**. C2a (no state diagram of the picker's selection in a test docstring) and C6c (no test
throws an exception type outside the listed failures) are open, both low severity and within delivery scope. C4a,
C4b and C5b count as passing because nothing here mutates and Team and Portfolio behave alike. No specification
ambiguity blocks a scenario.

## Wave: DISTILL / [REF] Walking skeleton

None required: this is a bug fix. The nearest thing is the four `An_administrator_...` scenarios, which enter at
`WizardsController` with the real connector over a fake transport and assert the 400 and the sentence.

E2E: no new spec. A failure path needs a refused credential, which demo data cannot give, and the scoped-token
button is pinned on every screen by the component tests. `jira.spec.ts` uses an ordinary Jira connection, so its
picker walk is unchanged.

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | Real-I/O scenario | Covered by |
|---|---|---|
| `JiraWorkTrackingConnector` board reads | fake transport under the real connector (policy row), no network | `JiraBoardReadFailureTest` |
| `JiraWorkTrackingConnector.ValidateConnection` (one string) | fake transport under the real connector | `JiraBoardReadFailureTest`, T1 scenario |
| `AzureDevOpsWorkTrackingConnector` board reads | fake project and board clients under the real connector (row appended to the policy) | `AzureDevOpsBoardReadFailureTest` |
| Real Jira and Azure DevOps sandboxes | **no CI scenario, by decision**: manual DELIVER live check (wrong email, wrong token, bad PAT; for a scoped token and OAuth, the disabled button and Test connection, not a board read) | DEVOPS answer 2. Also run the `JiraIntegration` and `AdoIntegration` categories once, because narrowing a connector `catch` may break something pinned only there |

What the fakes cannot show: the exact exception the Azure DevOps SDK raises for a real 401, 403 or timeout. Three
kinds of "no answer" are covered, but the live check is what confirms the mapping.

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving port | Scenario |
|---|---|
| `GET wizards/{id}/boards`, Jira and Azure DevOps | `An_administrator_asking_for_the_boards_...`, through the real `WizardsController` and connector |
| `GET wizards/{id}/boards/{boardId}`, Jira and Azure DevOps | `An_administrator_asking_for_a_board_...` |
| Same endpoints over HTTP | not repeated: `ServiceNowConnectionAcceptanceTest` already pins the 400 and the verdict through the host, and the controller catches the abstract type, so the wire shape is shared |
| The Test connection endpoint, Jira | T1 at the connector; the controller's handling of a failed verdict is unchanged |
| Board picker dialog | `BoardWizard.test.tsx` |
| Team and Portfolio settings | `GeneralSettingsComponent.test.tsx`, both contexts |
| Team and Portfolio create flows | `CreateWizard.boardPicker.test.tsx`, through the real registry, hook and shell |

## Wave: DISTILL / [REF] Scaffolds

One production change, and a test could not compile without it. `AzureDevOpsWorkTrackingConnector.GetClientAsync`
went from `private` to `internal virtual`, with a comment saying why. Behaviour is unchanged. `WorkHttpClient` and
`ProjectHttpClient` had no seam, and a real local server would have meant faking the SDK's location discovery. No
other scaffold: `JiraReadException`, `AzureDevOpsReadException`, `BoardWizard` and the registry already exist. The
disabled button is asserted through the rendered screens, so the tests do not fix where the one deciding place sits
(DESIGN Decision 4 suggests it).

## Wave: DISTILL / [REF] Test placement

| File | New or extended |
|---|---|
| `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraBoardReadFailureTest.cs` | new, untagged (DEVOPS-2) |
| `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsBoardReadFailureTest.cs` | new, untagged (DEVOPS-2) |
| `Lighthouse.Frontend/src/components/DataRetrievalWizards/BoardWizard.test.tsx` | extended |
| `Lighthouse.Frontend/src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx` | extended |
| `Lighthouse.Frontend/src/components/Common/CreateWizards/CreateWizard.boardPicker.test.tsx` | new: the existing create-flow tests mock the wizard registry, and these need the real one |
| `Lighthouse.Frontend/src/models/WorkTracking/WorkTrackingSystemConnection.authenticationMethodKeys.test.ts` | new: pins the whole frontend key list |

**Skipped tests and Biome.** `noSkippedTests` became an error on 2026-11-03, so `it.skip` now fails `pnpm build`.
The 6 plain `it.skip` calls (5 in `BoardWizard.test.tsx`, 1 in the key test) carry a narrow `biome-ignore` saying why.
The 7 `it.skip.each` declarations (20 cases) carry none, because the rule does not see them. So Biome will **not**
report them if one is forgotten: DELIVER must enable all 26 cases and delete the 6 suppressions, and
`grep -rn "it\.skip" Lighthouse.Frontend/src` must come back empty.

## Wave: DISTILL / [REF] Tests DELIVER changes

Found by running the existing suites against a throwaway fix, partial reads included (Jira, Azure DevOps, wizard and
connection health tests: 861 passed, 22 failed), plus reading the live-only classes, which that run excludes. The 22
are 8 that pinned the quiet degrade and 14 that fail only through one fixture line; with that line changed, the same
two classes fail on exactly the 8. Changed, not deleted.

| Test | Why | Change |
|---|---|---|
| `BoardWizard.test.tsx`, "names both reasons a connection may have no board to offer" | pins the old `NO_BOARDS` text; fails with E1 | replaced by the E1 scenario |
| `BoardWizard.test.tsx`, "displays fetched boards in autocomplete", the `/not a member of any Visual Task Board/i` absence check | stays green but no longer proves anything | point it at E1 |
| `JiraBoardQueryAssemblyTest`, `GetBoardInformation_PartOfTheBoardCannotBeRead_LogsWhatJiraAnsweredBeforeCarryingOn` (all 3 cases: `configuration`, `issue`, `status`) | pinned the quiet degrade: each now throws | rewrite to expect the refusal, or delete in favour of `A_board_Jira_hands_over_only_in_part_...`, whose log half the new log line then needs |
| `JiraBoardOriginalNamesTest`, `A_work_item_type_list_that_cannot_be_read_gives_no_types_and_a_warning_naming_the_answer` (4 cases) | pinned the quiet degrade for the work item type list | expect F2 with the answer; the `200` cases read `an answer Lighthouse could not read` |
| `JiraBoardOriginalNamesTest`, `A_status_list_that_cannot_be_read_gives_no_states_but_the_types_are_still_original` | pinned the quiet degrade for the status list | expect F2 |
| `JiraBoardQueryAssemblyTest`, the `BuildResponse` fixture | answers the status list with `[]`, which Lighthouse reads as unusable, so 14 tests of the board's query, the filter refusal among them, fail for a reason that is not theirs | one line: `path.EndsWith("status")` answers `JiraWireFormat.TheStatusListOf(JiraWireFormat.AnEnglishStatuses)`. Proved with a throwaway run |
| `AzureDevOpsWorkTrackingConnectorTest`, `GetBoardInformation_BoardIdIsNotContainingTwoGuids_ReturnsEmpty` (live category) | pins the empty board | expect the failure, or delete in favour of the new untagged test |
| `JiraScopedTokenIntegrationTest`, `GetBoards_ReturnsCorrectBoards` (live category) | asserts `Is.Empty`, which held only because the failure was swallowed | expect `JiraReadException` |

Not changed, but mention-worthy: `Slice05AnyBrokenCredentialSaysSoSpecifications`,
`Slice08HealthThatChecksItselfSpecifications` and `TaskManagerIcon.test.tsx` use "Authentication failed for Jira."
as their own fixture text, so T1 does not touch them.

**A placement trap.** Five test files `vi.mock` `components/DataRetrievalWizards` whole, with only
`getWizardsForSystem` on the mock: `useCreateWizard.test.ts`, `CreateTeamWizard.test.tsx`,
`CreatePortfolioWizard.test.tsx` and both `*.licenseCheck.test.tsx`. A new export from that module which the hook or
shell imports turns about 45 of their tests red with no change in behaviour. Put the rule on the wizard entry or in
a separate module, or extend those mocks first.

## Wave: DISTILL / [REF] T1 finding (decided)

Decided by the maintainer on 2026-11-04: T1 is one neutral string for every Jira sign-in. This is the finding that led
there, kept because it says which sign-ins reach T1.

Asked: do Jira OAuth connections reach T1, given they have no email or token? **Yes, and so does a second sign-in.**

`ValidateConnection` has no branch on the sign-in method. `jira.oauth` routes through the Atlassian gateway
(`RoutesViaAtlassianCloudGateway`), takes `OAuthBearerAuthStrategy`, and calls `rest/api/2/myself` like any other
connection. Any non-success answer returns the `authentication_failed` result, whose message becomes T1.

| Sign-in | Options it has | Reaches T1 when |
|---|---|---|
| `jira.cloud`, `jira.scopedtoken` | Url, Username, token | Jira refuses |
| `jira.datacenter` | Url, Personal Access Token, **no user name** | Jira refuses |
| `jira.oauth` | Url, Client ID, Client Secret, **no email, no token** | a token Lighthouse still holds as fresh is refused (revoked at Atlassian, scope removed) |

- Not every OAuth failure gets there. When the refresh itself fails, or no credential exists,
  `EnsureFreshTokenAsync` throws a plain exception, and `ValidateConnection` answers `validation_failed`, "Connection
  validation failed due to an unexpected error."
- The result also names `ApiToken` as the field to highlight, and `jira.oauth` has no such option. That predates
  this Bug, and the maintainer left the field as it is. How the screen handles it was not checked.
- The stored health verdict carries the same message, so T1 also appears in the connection popover.

The T1 test uses `jira.cloud`: `ValidateConnection` has no branch on the sign-in, so one case covers the one string.

## Wave: DISTILL / [REF] Pre-requisites

- Local runs: `dotnet test --filter` on the class names with the live categories excluded, never the unfiltered
  suite. Frontend: `pnpm install` once per worktree, then `pnpm vitest run <files>`.
- No new package, environment variable, key or migration.
- The maintainer's Jira and Azure DevOps sandboxes for the manual live check.

## Wave: DISTILL / [REF] Proposed slice order for DELIVER

Slice 01, then slice 02, the one allowed direction. Suggested enabling order:

| # | Slice | Enable | Turns on |
|---|---|---|---|
| 1 | 01 | `JiraBoardReadFailureTest`: the two `An_administrator_...` first, then list, page 2, log line, board | Jira reads stop answering failure with emptiness |
| 2 | 01 | `JiraBoardReadFailureTest`: the partial-read group, with the fixture line and the 8 tests under "Tests DELIVER changes" in the same step | a board is never offered without its states or types |
| 3 | 01 | T1 test | Test connection message |
| 4 | 01 | `BoardWizard.test.tsx`: E1, F2, refused list, the superseded-answer group | picker copy and the `latestSelection` guard |
| 5 | 01 | key test, `GeneralSettingsComponent.test.tsx`, then `CreateWizard.boardPicker.test.tsx` | `JiraOAuth`, the disabled button and the skipped step |
| 6 | 02 | `AzureDevOpsBoardReadFailureTest`: the two `An_administrator_...` first, then projects, `no answer`, malformed id | Azure DevOps reads stop answering failure with emptiness |

Step 4 is what slice 02's E1 relies on. Each step ships alone, green. Run the live categories once per
connector before narrowing its `catch`.
