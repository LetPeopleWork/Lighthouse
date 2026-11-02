# Bug 6499: a refused Jira credential shows "no boards"

Root-cause analysis: `rca.md` in this folder.

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
frontend copy table is a presentation detail of the same flow), no new integration points, estimated
2.5 days (slice 01 about 1.5, slice 02 about 1). Not oversized. Linear is out (Q2). Anything beyond the board/field-list/validate reads is
out (Q6, Q8).

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
  board picker and the connection check to say which of those happened and what to fix, so I do not
  re-issue tokens or conclude my boards are gone.

## Wave: DISCUSS / [REF] Locked decisions

Q1 to Q8 above are binding. Derived decisions (D) that follow from them, for DESIGN to honour:

- **D1** (Q2): the Jira and ADO connectors' `GetBoards` and `GetBoardInformation` stop turning failure into
  emptiness. Failure is a `WorkTrackingReadException` (`JiraReadException`, `AzureDevOpsReadException`
  already exist). A genuine `200` with zero boards stays `200 []` (ADR-126 decision 3).
- **D2** (Q3 clarified, Q5, N2, N3): there is **one credential sentence per connector** (Jira: C1, method-neutral
  for Cloud and Data Center; ADO: C6), produced in one place and used by `ValidateConnection`, the board list,
  the board information read and (Jira) the field list. It is reworded: it **replaces today's Test connection
  text** ("Authentication failed for Jira." with detail "Jira returned 401 Unauthorized."), and the picker shows
  the very same sentence. A 401 and a 403 both get it on the board list, board information and field list
  (N3, Q7). Code stays `authentication_failed` (`ConnectionHealthService` keys on it; ADR-184). The Jira
  sentence highlights no field. Scoped-token and OAuth connections never get C1 (D8).
- **D3** (Q4): `BoardWizard` empty-list copy is per connector; ServiceNow keeps today's text, Jira, ADO and
  Linear get the neutral text (Linear gets only this text change; its read path is untouched).
- **D4** (Q7): Jira `FieldListRefused` for 401 or 403 uses the credential sentence; other statuses keep
  today's text.
- **D5** (Q8): no architecture rule. Each connector gets failure-path tests (RCA section 3 lists them; none
  exist today for `GetBoards`).
- **D6**: a failure that is not a credential refusal (5xx, other status, transport, unparseable body) gets
  its own sentence naming what happened, never the credential sentence. This is the maintainer's S4 note
  ("even if Jira is down, the error is odd").
- **D7**: the Information log line "Authentication is not valid" that is written for every non-success
  board status becomes truthful (status-specific). Wording is a DESIGN detail.
- **D8** (N1 = A): Jira connections that use a **scoped API token** or **Jira Cloud OAuth** are routed through
  the Atlassian gateway, and Jira's board endpoints do not accept those credentials even when they are valid
  (`jira.md` lines 73 and 92). For them a 401 or 403 on the board list or board information gets the
  method-specific sentence C1m, never C1. Test connection and the field list, which do work with those
  credentials, use C1o when they are refused, never C1 either. DESIGN picks how the connector knows the method
  (`RoutesViaAtlassianCloudGateway` already does).
- **D9**: Jira `ValidateConnection` today classifies **every** non-2xx from `/myself` as `authentication_failed`
  (`JiraWorkTrackingConnector.cs` lines 438-453), so a 503 reads as an auth failure; that contradicts D6. Only
  401 and 403 keep `authentication_failed` and C1; other statuses get C2v with a non-credential code, and an
  unreachable host keeps `connection_failed`. Health-verdict consequence: `ConnectionHealthService` maps the
  code `authentication_failed` to the stored state `AuthenticationFailed` (line 33, 219). After D9 a Jira 503 no
  longer stores that state; DESIGN confirms which state the non-credential code yields. ADR-184's single
  verdict for real credential refusals is untouched: 401 and 403 still produce `authentication_failed`.
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
- **ADR-184 framing**: the verdict (`authentication_failed`) and its classifier (`ValidateConnection`)
  stay the single source of "credential refused"; this Bug adds callers of that sentence, not a second
  classifier. ADR-184's rule "where the connector cannot say it was authentication, never say it was"
  governs D6: only 401 (and 403 per Q7) get the credential sentence.

## Wave: DISCUSS / [REF] User stories

<!-- markdownlint-disable MD024 -->

### US-6499-01: Jira says why it gave no boards

job_id: `job-config-admin-know-any-credential-is-failing-not-just-oauth`

#### Problem

Tomas Weber administers Lighthouse for about ten Teams on one Jira Cloud connection. He opened the board
picker and read "No boards to offer. Either this account is not a member of any Visual Task Board ..." (a
ServiceNow sentence) because Jira had answered 401. He re-issued the API token, which changed nothing: the
email next to it was the one that was wrong. The only other text he ever saw was a bare "401 Unauthorized".
The same silent emptiness appears when Jira is down.

#### Elevator Pitch

Before: When Jira refuses the connection's credentials or is down, Tomas cannot tell that from "this
account has no boards", so he re-issues tokens that were never the problem.
After: open a Team's General settings, press "Select Jira Board" -> sees, in red under the board field,
"Jira refused the sign-in. Check the user name or email and the API token or personal access token ..." (C1),
and the same sentence on the connection page when he presses Test connection.
Decision enabled: check the user name or email and the token together instead of re-issuing the token or
giving up on the boards.

#### Domain examples

1. **Wrong email (the report)**: Tomas' connection to `https://acme-eng.atlassian.net` has Username
   `tomas.weber@acme-old.example` and a valid token that belongs to `tomas.weber@acme.example`. Jira answers
   401. The picker shows C1.
2. **Jira down**: same connection, Jira answers `503 Service Unavailable` on the board list. The picker
   shows C2 with "503 (Service Unavailable)" and does not mention credentials.
3. **Really no boards**: a Jira Cloud site with Team-managed projects only answers `200` with zero boards.
   The picker shows the neutral C5, not the ServiceNow sentence.
4. **Scoped API token (valid)**: Anika Rossi's connection to `https://acme-eng.atlassian.net` uses a scoped
   token with `read:jira-user` and `read:jira-work`. Jira answers the board list with 401 although the token is
   fine. The picker shows C1m, which says boards cannot be offered to this kind of sign-in and that the Team's
   query can be typed by hand; Test connection on the same connection says Connected.

#### UAT scenarios

1. **A refused Jira credential is named in the picker, on 401 and on 403.** Given Tomas' Jira Cloud
   connection and Jira answers 401 on the board list (example 1) or 403 on it (the account has no Jira
   Software licence), When he opens "Select Jira Board", Then he reads C1 in both cases, the board field stays
   disabled and Confirm stays disabled.
2. **Test connection says the same thing.** Given the same connection and Jira answers 401 on the account
   check, When he presses Test connection, Then he reads C1 with technical detail "Jira returned 401
   Unauthorized." and no input is highlighted.
3. **Jira being down or unreachable is not reported as a credential problem.** Given Jira answers 503 to the
   board list, or does not answer at all (timeout), or answers 503 to Test connection, When Tomas opens the
   picker or presses Test connection, Then he reads C2 (picker, 503), C3 naming the URL (timeout) or C2v
   (Test connection, 503), and no sentence about the user name, email or token; the connection's stored
   health state is not set to authentication failed by the 503.
4. **A genuinely empty board list is not a fault.** Given Jira answers 200 with zero boards, When Tomas
   opens the picker, Then he reads C5 (Jira) and no "Visual Task Board" wording; and for a ServiceNow
   connection the picker still reads today's sentence.
5. **A board that cannot be read is not confirmable.** Given the board list loaded and Jira answers 401 or
   403 for the chosen board's configuration, When Tomas picks the board, Then he reads C1; for another
   failing status or an unreachable Jira he reads C4; Confirm stays disabled in all cases.
6. **The field list tells the truth on a 401.** Given Jira answers 401 or 403 on its field list while
   Tomas validates additional fields, When validation runs, Then he reads C1 and not "needs to be able to
   browse at least one project"; for a 500 the existing field-list text remains.
7. **A valid scoped token or OAuth sign-in is not told to check email and token.** Given Anika Rossi's
   connection uses a scoped API token (or Jira Cloud OAuth) and Jira answers 401 on the board list, When she
   opens "Select Jira Board", Then she reads C1m and no sentence about the email or the API token; and when
   Jira refuses that connection on Test connection or the field list, she reads C1o, also without the email
   sentence.

#### Acceptance criteria

- [ ] Scenarios 1 to 7 hold as automated tests (backend connector tests for the failure paths RCA
      section 3 lists, including a 403 on the board list and a 503 on `/myself`; frontend tests for the
      copy table).
- [ ] No message or technical detail contains the email, the token or any header value.
- [ ] The Information log line for a non-success board status names the real status and no longer claims
      "Authentication is not valid" for a 503.
- [ ] A 401 on the board list and the validate path produce the same message text from one source.
- [ ] `ValidateConnection` gives `authentication_failed` and C1 only for 401 and 403; a 503 or any other status
      gives C2v with a different code, so the stored connection health is not "authentication failed".
- [ ] Scoped-token and OAuth connections never read C1: the board reads give C1m, Test connection and the
      field list give C1o (D8).

#### Outcome KPIs

- **Who**: Lighthouse System Administrators setting up Jira Teams.
- **Does what**: reach the right fix (email or token, or "wait, Jira is down") from the first sentence they read.
- **By how much**: 0 community reports of "no boards" where the cause was a refused credential or a Jira
  outage, in the 90 days after release (baseline: 1 reported, plus the ones nobody reported).
- **Measured by**: community and support thread review at the next two releases; no telemetry (see DEVOPS
  pre-answer below).
- **Baseline**: every such failure reads "No boards to offer".

#### Technical notes

- Jira `GetBoards` and the paging loop `GetBoardsFromJira`, `GetBoardInformation` (its generic `catch` and
  the empty-`BoardInformation` return on a non-success `/configuration`), `FieldListRefused`,
  `ValidateConnection`. Line numbers in `rca.md`.
- `BoardWizard.tsx` already shows `ApiError.message` (`refusalOr`) and disables Confirm on a null
  `boardInformation`; no new frontend error path is expected.
- `ValidateConnection` (`JiraWorkTrackingConnector.cs` lines 438-453) classifies every non-2xx as
  `authentication_failed` today; the status branch (D9) is part of this story. The health verdict follows
  the code (`ConnectionHealthService` line 219), see D9.
- A Team can be configured without a board: the board wizard is an optional button next to the Team's query
  field, which is typed by hand, and `jira.md` lines 73 and 92 already say to "configure teams/portfolios
  manually". That is why C1m may truthfully suggest typing the query.
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
After: open a Team's General settings, press "Select Board" -> sees "Authentication failed for Azure
DevOps. Check your Personal Access Token permissions and make sure it is still valid." (C6).
Decision enabled: renew the PAT instead of searching for a missing board.

#### Domain examples

1. **Expired PAT**: connection `https://dev.azure.com/northwind` answers unauthorized (`VssUnauthorizedException`);
   the picker shows C6, the same sentence Test connection shows today.
2. **Azure DevOps unreachable**: the host does not answer; the picker shows C7 with what ADO said.
3. **Organisation with no boards**: a project with no team boards answers an empty list; the picker shows
   the neutral C5 (Azure DevOps wording).
4. **One project fails, the others answer**: the connection `https://dev.azure.com/northwind` has projects
   Fabrikam and Contoso; Azure DevOps answers Contoso's board list with a service error. The picker does not
   quietly show only Fabrikam's boards as if Contoso had none; it shows C7.

#### UAT scenarios

1. **An expired PAT is named in the picker.** Given Priya's PAT is rejected, When she opens "Select
   Board", Then she reads C6 and Confirm stays disabled.
2. **A failing Azure DevOps is not reported as empty.** Given Azure DevOps answers with a service error or
   cannot be reached, When she opens the picker, Then she reads C7 and not "no boards".
3. **A genuinely empty list is neutral.** Given the project has no boards, When she opens the picker,
   Then she reads C5 (Azure DevOps wording).
4. **A board that cannot be read is not confirmable.** Given the list loaded and the chosen board's
   information read fails, When she picks the board, Then she reads the reason and Confirm stays disabled.
5. **One failing project is not an empty project.** Given the board read of one of Priya's projects fails
   while the others answer, When she opens the picker, Then she reads C7 (or C6 if the PAT is rejected)
   and not a list that silently lacks that project's boards.

#### Acceptance criteria

- [ ] Scenarios 1 to 5 hold as automated tests (the board list failure rows, the per-project failure in
      `GetBoardsForProject`, plus the board information read, in the ADO connector tests).
- [ ] The ADO credential sentence on the picker is character-for-character the one `ValidateConnection`
      returns (Q3).
- [ ] The existing Error-level log for a failed board read is kept for non-refusal failures.

#### Outcome KPIs

- **Who**: Lighthouse System Administrators setting up Azure DevOps Teams.
- **Does what**: reach the right fix from the first sentence.
- **By how much**: 0 reports of "no boards" caused by an expired PAT or an unreachable Azure DevOps in 90 days.
- **Measured by**: same review as US-6499-01. **Baseline**: every such failure reads as empty.

#### Technical notes

- `AzureDevOpsWorkTrackingConnector.GetBoards` (`catch (Exception)` -> `[]`), `GetBoardInformation`
  (same) and the **second swallow** in `GetBoardsForProject` (lines 637-641, per project, called from
  `GetBoardsForProjects`, lines 610-619; D10). All three must stop returning empty, or a rejected PAT still
  reads "no boards". Whether a single failing project fails the whole list or only shows a notice is a
  DESIGN call; it must never read as that project having no boards. `AzureDevOpsReadException` exists; a board-specific factory is likely needed. Which exceptions
  count as "credential" beyond `VssUnauthorizedException` (and 403) is a DESIGN call.
- **Depends on US-6499-01** (slice 01) for the `BoardWizard` per-connector copy table, where ADO gets its own
  C5 line. This is the one allowed direction: the later slice builds on the earlier, finished one; slice 01
  does not wait on slice 02.

## Wave: DISCUSS / [REF] Proposed copy and where it shows

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
| Docs prose (for DELIVER finalize) | **Yes, owed**: `docs/concepts/worktrackingsystems/jira.md` line 92 says Lighthouse "will display 'No Boards available'" for scoped tokens; that sentence is wrong after this Bug. Replacement (N1 = A): the picker says Jira does not offer boards to scoped tokens, and the Team's query can be typed by hand or set up with a personal API token first; line 73 (OAuth) gets the same correction and keeps "Configure teams and portfolios manually". ADO page: check for a board-picker note. Per-feature screenshots: **N/A**, because the failure state is one red line of text and `jira_wizard.png` shows the happy path, which does not change. Demo data: N/A, because failure paths need a refused credential, not data. |
| Release notes | Candidate for `/release` (not at finalize): customer pain first ("a refused Jira or Azure DevOps credential no longer reads as 'no boards'"). ADO "Release Notes" tag: maintainer decides. |
| Usage-data event (DEVOPS pre-answer) | Proposed "N/A, because the change reports a failure and adds nothing to be used; a name-only 'board list refused' event would count failures, not use". DEVOPS confirms. |
| Terminology | Copy uses "board", "Team" (Terminology), no tracker-specific heading words. |
| Context map and glossary | Answered above; proposed edits applied in DELIVER. |
| Proposed doc edits, applied in DELIVER | ADR-126 adoption note; ADR-270 glossary row "Read refusal"; `jobs.yaml` REVISED note; `jira.md` lines 73 and 92. |

## Wave: DISCUSS / [REF] Slices

Briefs in `slices/`. Order: Jira first (it carries the shared credential sentence and the per-connector
copy table, and the report), then ADO. **Slice 02 depends on slice 01** (the per-connector copy table); that is
allowed because it runs in one direction, the later slice building on the finished earlier one.

| Slice | Story | Learning hypothesis (disproved if it fails) |
|---|---|---|
| 01 | US-6499-01 | The shared refusal channel carries Jira's 401 to the picker with no new frontend path. Disproved if `BoardWizard` needs more than the copy table. |
| 02 | US-6499-02 | ADO's exception model maps to the same verdicts without a Jira-shaped abstraction. Disproved if a shared helper is needed to make it pass. |

Taste tests: no slice adds 4+ components; no slice depends on a new abstraction; both disprove something;
production-data criterion is met by running one picker against a deliberately wrong credential on a real
Jira and ADO sandbox in DELIVER (not synthetic only).

## Wave: DISCUSS / [REF] Definition of Ready (9 items)

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Problem sections above. |
| 2 | Persona | Tomas Weber and Priya Nair, `config-admin`. |
| 3 | 3+ examples with real data | Three each, with URLs and addresses. |
| 4 | UAT 3-7 | Seven and five. |
| 5 | AC from UAT | Embedded per story. |
| 6 | Right-sized | One slice each: slice 01 about 1.5 days, slice 02 about 1 day (2.5 in total). |
| 7 | Technical notes | Per story. |
| 8 | Dependencies | US-02 depends on US-01 (the per-connector copy table), one direction only. N1 to N3 are answered and folded in. |
| 9 | KPIs | Per story; measured by review, no telemetry. |

DoR passes for both stories. Job traceability: both stories carry `job_id`; neither is `@infrastructure`; both have an
Elevator Pitch. No per-wave reviewer dispatched (optional per the skill; the consolidated review fires at
the end of DISTILL).

## Wave: DISCUSS / [REF] Out of scope

Linear (Q2); a failed refresh's reason in Refresh History or on the Team page (Q6, follow-up below); an
architecture rule against `catch { return []; }` (Q8); widening who may open the picker (ADR-126 decision 4);
guarding the Jira search against an "answered as a stranger" empty result (RCA Direction 2; settled by Q1's
follow-up evidence, not this Bug); ServiceNow.

## Follow-up Story (Q6, to be created after confirmation)

Not created in ADO. Proposed, for the maintainer to approve:

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

The open questions N1 to N3 and the Q3 clarification are answered in the table below and are folded into
the decisions D2, D8 and D9, the copy table (C1, C1m, C1o, C2v, C4), the scenarios, the acceptance criteria,
the sketches, the Definition of Ready and both slice briefs. No question is open.

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
`adr-draft-ado-board-list-fails-whole.md` (decision 3; the file name is kept, but it now records the three
options and the open choice, HELD-1). Nothing outside this folder was edited. Revised after the DESIGN review
against the AFK decisions at the end of this file (AFK-1 held, AFK-2 reversed, AFK-5, AFK-6).

### [REF] Existing-system analysis (reuse first)

| Need | What already exists | Verdict | Contract shape, universe, assertion |
|---|---|---|---|
| A typed "could not ask / was refused" answer | `WorkTrackingReadException` (abstract, carries a `ConnectionValidationResult`), `JiraReadException`, `AzureDevOpsReadException`; `WizardsController.AnsweringARefusalWithItsReason` catches the abstract type and answers `400 {verdict}`; `BoardWizard.refusalOr` shows the verdict text | **Reuse unchanged.** No new exception type, no controller change, no new frontend error path. | Pure data (an exception carrying a value). Nothing to assert beyond the verdict it carries. |
| A place for "status -> sentence" knowledge | ServiceNow already did it: `ServiceNowValidationVerdict` and `ServiceNowBoardVerdict`, pure static classes that turn (status, facts) into a `ConnectionValidationResult`, called by both `ValidateConnection` and the board reads | **Copy the pattern per connector** (decision 1). | **Pure** (return-only). Universe: none; no I/O, no logging. Asserted by table tests; no collaborator can be injected because there is none. |
| Which Jira connections go through the Atlassian gateway | `JiraWorkTrackingConnector.RoutesViaAtlassianCloudGateway(authenticationMethodKey)` | **Reuse** as the one input to C1 / C1m / C1o selection. | Pure predicate. |
| Whether a Jira exception means "could not be asked" | `JiraCouldNotBeAsked` (`JiraWorkTrackingConnector.cs` lines 3760-3761), used by the release and membership reads | **Reuse** for the board reads and for the `ValidateConnection` timeout; no second mapping (Decision 2). | Pure predicate. |
| Health state from a verdict | `ConnectionHealthService.StateFor`: code `authentication_failed` (or `secret_cannot_be_read`) -> `AuthenticationFailed`, **every other code -> `Unreachable`** | **Unchanged** (decision 2). | Pure mapping; only a pin test is added. |
| Jira `GetBoards`, `GetBoardInformation`, field list | The existing read adapters | **Modify**: throw a read exception instead of returning empty. | **Bounded-change with an empty declared mutation set** (they read). Universe: the connection's own Jira over HTTP, GET only; no repository or database write; no health write. Asserted with the existing HTTP stub failing the test on any non-GET request and strict mocks for every collaborator that could write (the single-writer ArchUnitNET test already guards the health side). |
| Jira `ValidateConnection` | The existing adapter | **Modify** (status ladder, timeout). | **Bounded-change.** Declared mutation set: the one in-memory entry `connectionsWhoseCredentialWasAccepted[connection.Id]` (line 456), set only after a 2xx. Universe: that dictionary plus GET requests. Asserted by a test that a 401, 403, 503 and a timeout leave the dictionary entry unset and a 2xx sets it. |
| ADO `GetBoards`, `GetBoardsForProject`, `GetBoardInformation` | The existing read adapters | **Modify**: outcome-or-failure instead of `[]` / empty. | **Bounded-change with an empty declared mutation set.** Universe: the organisation's ADO over the SDK clients, reads only. Asserted with SDK fakes that record calls (only `Get*Async`), strict mocks for the rest. |
| `IBoardInformationProvider` (driving port) | Two read methods | **Unchanged.** | Reads only: it exposes no write method, so nothing to split. |
| Per-wizard facts on the frontend | `IDataRetrievalWizard` registry entry (id, name, system types, contexts); both render sites already pass the entry's facts to the component (`boardType` from the wizard id) | **Extend** the entry with the empty-list sentence (decision 4). | Static data. |

Contract shapes (Earned Trust / effect isolation) in one line: the verdict classes are **pure**; the five read
adapters are **bounded-change with the universes in the table**, whose only new outcome is "throw a read
exception instead of returning empty". The board reads are not a health writer: the stored verdict is still
written only at refresh failure and Test connection (ADR-184 single writer,
`ConnectionHealthSingleWriterArchUnitTest` stays green). Environment lies this design accounts for: Jira answers
401 for a wrong email as well as a wrong token (neutral sentence, Q5), answers 401 on the board endpoints for a
valid scoped token or OAuth sign-in (C1m, D8), and answers 403 for a missing licence as well as for a refused
sign-in (C1 makes no claim about the token's life). No new probe is needed: no new adapter, and the DELIVER live
check against a deliberately wrong credential on a real Jira and ADO sandbox is the empirical probe (DISCUSS
taste test).

### [REF] Decision 1: where the one credential sentence lives

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

### [REF] Decision 2: exception and verdict shape; the controller; the health verdict

**Exceptions.** Reuse `JiraReadException` and `AzureDevOpsReadException` as they are: the read methods throw
`new JiraReadException(JiraConnectionVerdict....)` through their existing public constructors. No new
exception type, no shared "credential refused" exception (it would carry nothing the verdict does not, and the
controller already catches the abstract base). Rejected: a new `CredentialRefusedException` in the Interfaces
namespace (a third place that must agree with the two ladders).

**Catching (board reads only).** The board reads (list and information, both connectors) no longer contain a
catch-all that returns an empty value. The rule per method: `WorkTrackingReadException` passes through; a
non-success status, and everything `JiraCouldNotBeAsked` recognises (`HttpRequestException`, including the one
`ResolveCloudIdAsync` raises, which carries a status; `TaskCanceledException` for a timeout; `UriFormatException`)
map through the verdict class; `JsonException` maps to the "unreadable" verdict; **any other exception on a board
read is not caught** and reaches `UseExceptionHandler` as a 500, which `BoardWizard` renders as its fallback
sentence (Decision 4). A bug in the connector is thus loud, never an empty list. This sentence is about the board
reads only: the catch-alls that remain elsewhere, and why, are listed below. Paging: a failure on page 2 or later
fails the whole list; the pages already read are discarded (a half list is the same lie as an empty one).

**`WizardsController`: unchanged.** It already turns any `WorkTrackingReadException` into `400 {verdict}`.
The wire change is only that Jira and ADO now take that path (`200 []` -> `400`), the envelope ADR-126 already
ships for ServiceNow. `IBoardInformationProvider` is unchanged; a genuine `200` with zero boards stays `200 []`.

**Jira `ValidateConnection` and health.** The status branch moves into `FromStatus(Validation, ...)`:
401 and 403 keep `authentication_failed`; any other status gives `unexpected_answer` (C2v); unreachable keeps
`connection_failed`. `ConnectionHealthService` needs **no change**: `StateFor` maps only `authentication_failed`
(and `secret_cannot_be_read`) to `AuthenticationFailed` and every other code to `Unreachable`, so a Jira 503 now
stores `Unreachable` with the C2v message, which is the true state. Consequences to record in ADR-184 (an
"Adoption" note, DELIVER): (a) the classifier for Jira is now status-aware, a 503 is no longer a credential
verdict; (b) the single-verdict rule is untouched, 401 and 403 still produce `authentication_failed` and the
stored message is C1 / C1o (reworded). `unexpected_answer` is read as `Unreachable` by `StateFor` only because
it is not the rejected-credential code; a test pins that. `ConnectionHealthSingleWriterArchUnitTest` is
unaffected (the picker reads do not write the verdict).

**Timeout on `ValidateConnection` (decided).** Today Jira's `ValidateConnection` catches only
`HttpRequestException` (line 471), so a timeout, which surfaces as a `TaskCanceledException`, falls into the
`catch (Exception)` at lines 491-498 and reads "Connection validation failed due to an unexpected error" with code
`validation_failed`. A timeout is "Jira did not answer", the same fact as an unreachable host, so it joins the
unreachable arm: `connection_failed`, field Url, C3-style text. Implementation: reuse the existing classifier
`JiraCouldNotBeAsked` (`JiraWorkTrackingConnector.cs` lines 3760-3761: `HttpRequestException`,
`TaskCanceledException`, `JsonException`, `UriFormatException`) rather than a second mapping; the board reads use
it to decide "could not be asked" and then pick the sentence (`JsonException` -> C9 unreadable, the rest ->
C3, and for a malformed URL, where no host can be shown, a C3 worded without the host). Health consequence: the
stored state for a timeout is `Unreachable` (code `connection_failed`), which is the true state.

**Every remaining catch-all, listed (none of them is a board read; none returns an empty value).**

| Catch-all | Returns | Decision |
|---|---|---|
| Jira `ValidateConnection` `catch (Exception)`, lines 491-498 | `validation_failed`, "Connection validation failed due to an unexpected error." with `exception.Message` as technical detail | **Stays.** It is a non-empty, truthful verdict that names no credential (D6 holds: it does not say the sign-in was refused). With the timeout moved out, what is left is a genuinely unexpected fault; the unreadable-body case also lands here and keeps `validation_failed`. |
| ADO `ValidateConnection` bare `catch`, lines 304-309 | `validation_failed`, same sentence, no detail | **Stays.** Same reason: non-empty, D6-compliant. DELIVER may add an Information log line with the exception, as Jira's arm has (today it is swallowed unlogged); not required by this Bug. |
| Jira `ValidateTeamSettings` (line 1266) and `ValidatePortfolioSettings` (line 1307) `catch (Exception)`; ADO `ValidateTeamSettings` (line 341) and `ValidatePortfolioSettings` (line 379) | `validation_failed` with `exception.Message` | **Stay, out of scope.** Query validation, not a board read; each returns a non-empty verdict. |
| Jira `TryWriteFields` (line 664), ADO `TryPatch` (line 548) | Failure of a write | **Stay, out of scope.** Write paths of other features. |
| Jira release and membership reads (`JiraCouldNotBeAsked` filters, lines 3572, 3742, 3845) | "could not be asked" answer | **Stay.** Typed, filtered, already the pattern this design reuses. |

ADO has the same timeout gap (a `TaskCanceledException` reaches the bare `catch`); it gets the same fix, a
timeout joins its `HttpRequestException` arm (`connection_failed`, field Url). The only catch-alls removed are the
five that return an empty value: Jira `GetBoards` (line 511) and
`GetBoardInformation` (line 532), ADO `GetBoards` (line 400), `GetBoardInformation` (line 443) and
`GetBoardsForProject` (lines 637-640).

### [REF] Decision 3: Azure DevOps, a failing project in a multi-project list (HELD-1: maintainer to decide)

**Fixed under every option.** D10 stands: the swallow in `GetBoardsForProject` (the `catch (Exception)` that
returns `[]`, `AzureDevOpsWorkTrackingConnector.cs` lines 637-640) goes; a per-project read returns an outcome
(its boards, or its failure), never `[]` for a failure, and the aggregate decides. A project with zero boards is a
genuine empty contribution. **A credential refusal (401, `VssUnauthorizedException`) on any project, or on the
project list, always fails the whole list with C6**, whichever option is chosen: a refused PAT is never skipped
and never partial. Each failing project keeps its Error-level log line. The options differ only in what a
**non-credential** per-project failure (Boards service disabled, 404, a 403 on one project, a timeout) does:

| Option | What the picker does | Consequences |
|---|---|---|
| **A. Fail the whole list** (D10 as written) | Any per-project failure fails the list: C10 for a 403, otherwise C7 naming the first failing project and "and N more". | Holds "a failing project never reads as having no boards" by construction; no wire change, no new UI state. Regression: a picker that works today (a PAT that cannot see one project, a project with Boards disabled) becomes unusable for every project. The Team's query can still be typed by hand. Test pins: one of two projects failing fails the list naming it; a 403 on one project gives C10. |
| **AFK-1. Skip non-credential failures while others answer** | The failing project is left out with a Warning log line naming it, as long as at least one project answered; if none answered the list fails with C7 (or C10 when every failure was a 403) naming the failing project(s). | No regression for pickers that work today; no wire or UI change. The list silently lacks the skipped project, which the admin cannot tell from "that project has no boards", so D10 and US-02 example 4 / UAT 5 are only kept for the case where all projects fail. Test pins: one of two failing, the other answers: list holds the other's boards and the Warning names the project; both failing: C7; zero projects. |
| **B. Partial list plus a notice** ("Contoso's boards could not be read") | The picker shows the boards that answered and a notice naming the projects that did not; if none answered it fails as in A. | Honest and no regression. The list endpoint answers a bare array, so a notice needs a new response envelope (`{boards, notices}`), a `wizardService` change, a new picker state with loading/failure/partial sketches, and a wire break on a UI-only endpoint; DISCUSS would need to cover the new state. Test pins: envelope shape, notice copy, the partial state in `BoardWizard`. |

No option is recommended here. The three trade a regression risk (A), a silent omission (AFK-1) and a larger
change (B). **Maintainer to decide (HELD).** DISTILL leaves the multi-project failing-project scenarios open
until then; the fixed part above is already designed and testable. Rejected outright: "fail only when every
project fails, otherwise partial and silent" (it is AFK-1 without the log line and without the credential rule).

ADO classifiers (revised after the AFK-2 reversal; an ADO 403 is **not** a credential verdict anywhere):

- `IsCredentialRefusal(exception)` = `VssUnauthorizedException`, or an HTTP response exception with status 401.
  This is exactly what `ValidateConnection` classifies as `authentication_failed` today (its
  `catch (VssUnauthorizedException)`, lines 265-274); `GetAllProjects`, `GetBoardsForProject` and
  `GetBoardInformation` use the same classifier so the picker's C6 and Test connection's text cannot drift.
- `IsPermissionRefusal(exception)` = a 403. **Test connection keeps today's mapping for it** (no change to
  `ValidateConnection`; it lands on `connection_failed`, health `Unreachable`, per ADR-184 "where the connector
  cannot say it was authentication, never say it was"). The picker gets its own sentence, **C10**: "Azure DevOps
  refused access to {what was asked}: the account or token lacks permission to read boards{ in project X}. Check the
  token's scopes and the account's permissions in Azure DevOps, then try again." It is never C6. ADR-126's
  ServiceNow precedent (403 is `insufficient_permissions`) agrees.
- DELIVER confirms which SDK exception type an ADO 403 raises (a `VssServiceException` with a status or an HTTP
  exception) with a recorded fake before the classifier is written.

**`GetBoardInformation` malformed board id (slice 02).** The guard at lines 413-417 (`Length != 2`) returns an
empty `BoardInformation`. An empty object is truthy on the frontend, so `BoardWizard` enables Confirm
(`!boardInformation` is false, line 173) and hands an empty board back as if it had been read. That is the same
lie as the empty list, so it becomes a typed `AzureDevOpsReadException` with **C11**: "Lighthouse could not
understand which board was picked. Close the dialog and pick the board again." (code suggested:
`invalid_board_id`). Its `catch (Exception)` at line 443 loses its empty return too (Decision 2).

### [REF] Decision 4: frontend copy and how the refusal text arrives

Refusal text: **no change**. `wizardService` already surfaces `ApiError.message` from the `400` body and
`refusalOr` shows it; Jira and ADO simply start arriving there. Board information failures go through the
same line (`setBoardInformation(null)` keeps Confirm disabled).

Empty-list copy per connector: put it where the other per-connector wizard facts already are.

| Option | Trade-offs |
|---|---|
| **A. `emptyListMessage` on the `IDataRetrievalWizard` registry entry, passed as an optional `noBoardsMessage` prop by the two render sites** (**both get the prop edit**: `GeneralSettingsComponent.tsx` line 224 and `CreateWizardShell.tsx` line 148); `BoardWizard` falls back to a tracker-neutral sentence when absent. | One place per connector, next to name and system type; `BoardWizard` stays connector-agnostic; no id string-matching. Two render sites pass one more prop (same precedent as `boardType`). |
| B. A table inside `BoardWizard` keyed by the wizard id or system type. | Needs a new prop anyway (the component is given neither) and repeats `boardType`'s id-matching. |
| C. The backend sends the empty-list sentence. | Rejected: ServiceNow's empty list is a `200 []` by ADR-126 decision 3, and an array carries no text. |

**Recommendation: A.**

| Wizard entry | Empty-list sentence (DISCUSS C5) |
|---|---|
| `servicenow.board` | today's `NO_BOARDS` text, unchanged |
| `jira.board` | "No boards are available to this connection. The account it signs in with may not have access to any board, or none of the boards it can see can be used here. Check the account's access in Jira, then try again." |
| `ado.board` | the same, "in Azure DevOps" |
| `linear.team` | the same, "in Linear". Kept although Linear is out of scope (Q2): Q4 asks that every connector but ServiceNow stop showing the "Visual Task Board" sentence, so this is the only Linear change and its read path is untouched. |
| `BoardWizard` default (prop absent) | the same without a tracker name ("in the tracker") |

**Loading look and failure look (slow-connection rule), for the picker and the board read:**

```
LOADING (list)                   LOADED                         FAILED (list)                  EMPTY (200 [])
+----------------------------+   +-------------------------+    +-------------------------+    +-------------------------+
| Select Jira Board          |   | Select Jira Board       |    | Select Jira Board       |    | Select Jira Board       |
|                            |   | [ Board          v ]    |    | [ Board         v ](off)|    | [ Board         v ](off)|
|        ( spinner )         |   |                         |    | red: C1 / C2 / C3 / C1m |    | red: C5 (per connector) |
|  (no field, no text)       |   |                         |    |                         |    |                         |
|        [Cancel][Confirm off]   |      [Cancel][Confirm]  |    |    [Cancel][Confirm off]|    |    [Cancel][Confirm off]|
+----------------------------+   +-------------------------+    +-------------------------+    +-------------------------+

BOARD READ (after picking)  loading: BoardInformationDisplay spinner, Confirm off
                            failed : red line C1 / C1m / C4, board info cleared, Confirm off
```

What exists and is reused (no new control): the list's loading look is today's `CircularProgress` that
*replaces* the field and the red line (so no empty field and no stale text can read as "no boards" while it is
on its way); the failure look is the red `Typography color="error"` with the disabled field; the board read's
loading look is `BoardInformationDisplay loading`. **Gaps found and closed here, both inside slice 01:**
(1) on a failed list `BoardWizard` leaves the previous `boards` state in place; set it to `[]` in the `catch`
so a failure can never sit beside an old list; (2) the empty-list branch (`fetchedBoards.length === 0`) is the
only one that may use the C5 copy, which a test pins by asserting a refused credential never renders C5 or the
ServiceNow sentence. DISTILL pins both states; E2E needs no new wait (`data-widget-status` is the dashboard-widget
contract, the dialog uses its spinner). Check by hand at a throttled connection in DELIVER. Visualization
skill: N/A, nothing charts data. No keyed TanStack query is introduced: the dialog is mounted per opening
(`open={true}` on mount), so a late answer for a closed dialog lands on an unmounted component; migrating the
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

**Fallback sentences.** The two canned lines ("Failed to load boards. Please try again.", "Failed to read the
board. Please try again.") say nothing about what happened and are reached only when the failure never came
from the backend (the request did not reach Lighthouse, or Lighthouse itself failed with a 500). Suggested
replacements, for the DISTILL sketch walk: "Lighthouse could not get an answer to its request for the list of
boards (the request did not complete, or Lighthouse failed unexpectedly). Reload the page and try again." and the
same with "for this board".

### [REF] Decision 5: test strategy (high level; scenarios belong to DISTILL)

- **Pure verdict tables** (`JiraConnectionVerdict`, `AzureDevOpsConnectionVerdict`): one table test per ladder,
  read x status x sign-in kind, pinning the sentence, the code and the field. This is the mutation-testing
  target (80% kill rate, per-feature); a sentence not pinned is a surviving mutant.
- **Connector tests through the existing `IBoardInformationProvider` / `IWorkTrackingConnector` ports with the
  existing HTTP stubs** (Jira) **and SDK fakes** (ADO). Failure paths per connector, which RCA section 3 notes
  have none today: board list 401, 403, 503, timeout/no answer, malformed body, **failure on page 2 after page 1
  succeeded**, `200 []`; board information 401, 403, 503, non-success `/configuration`, transport; scoped-token
  and OAuth sign-in (C1m, C1o); `ValidateConnection` 401, 403, 503, unreachable, **a timeout (a
  `TaskCanceledException` gives `connection_failed` and C3-style text, not `validation_failed`; Decision 2)**, an
  unreadable body (`validation_failed`, kept); field list 401/403 (C1) versus 500 (today's text).
  `JiraFieldLookupTest.cs` line 157 (`browse at least one project` asserted on a 403 field list) **is expected to
  change** with D4; it is rewritten to the C1 sentence, not deleted. ADO: project list unauthorized, a
  credential refusal on any project (C6 for the whole list, under every Decision 3 option), a 403 on the board
  list (C10, never C6; Test connection for the same 403 keeps today's text and a health state of `Unreachable`,
  pinned), zero projects, board information failures, **a malformed board id gives a typed failure (C11),
  never an empty `BoardInformation`**. The multi-project failing-project scenarios follow Decision 3 (HELD-1) and
  are written only after it is decided. DELIVER confirms the exact SDK exception type an ADO 403 raises (a
  `VssServiceException` with a status, or an HTTP exception) with a recorded fake before writing the classifier.
- **Driving port**: `WizardsController` test that a `JiraReadException` and an `AzureDevOpsReadException` from the
  provider answer `400` with the verdict body, and that `200 []` stays `200`. One end-to-end API test through the
  host (as ServiceNow's) is enough for the wire shape.
- **Health**: `ConnectionHealthService` test that `unexpected_answer` stores `Unreachable` and 401 still stores
  `AuthenticationFailed`. No change to production health code, only a pin.
- **Frontend (Vitest + RTL)**: `BoardWizard` shows a refusal message verbatim, never C5, with the field and
  Confirm disabled; shows the spinner and no sentence while loading; per-connector C5 from the registry entry;
  board-read failure clears the board information; **a superseded answer is ignored**: pick board A, pick board
  B, resolve A's answer last (both a success and a refusal): B's state is untouched, Confirm reflects B only, and
  the spinner of B is not cleared by A; a late answer after the selection was cleared changes nothing; the two
  fallback sentences render when the failure never came from the backend. The two `BoardWizard.test.tsx` assertions that pin "Visual
  Task Board" (lines 161 and 341) become the ServiceNow-only case. No new Playwright spec (E2E is a thin sanity
  check; the picker's happy path is covered).
- **No architecture rule** against `catch { return []; }` (Q8, binding). The pure ladders plus per-connector
  failure-path tests are the guard.
- **DELIVER live check** (not CI): a deliberately wrong credential on a real Jira and ADO sandbox.

### [REF] Component diagram (hexagonal view; N = new, M = modified, - = unchanged)

```
 DRIVING ADAPTERS                APPLICATION / PORTS                 DRIVEN ADAPTERS (trackers)
 ----------------                -------------------                 --------------------------
 BoardWizard.tsx        (M: latest-selection guard, fallbacks)
                                 IBoardInformationProvider (-)       JiraWorkTrackingConnector     (M)
   registry entry       (M)  --> GetBoards / GetBoardInformation     --> |- GetBoards, GetBoardInformation (M: throws; no catch-all)
   emptyListMessage     (N)           ^                                  |- ValidateConnection       (M: status branch)
 WizardsController      (-)           |  throws                          |- field-list refusal       (M)
   catch abstract only                | WorkTrackingReadException (-)  JiraConnectionVerdict        (N, pure)
                                      |   |- JiraReadException        (-)  JiraReadException          (-)
 Connection page (Test) (-) --> IWorkTrackingConnector (-)           AzureDevOpsWorkTrackingConnector (M)
   shows verdict text           ValidateConnection (shape -)           |- GetBoards, GetBoardsForProject,
                                                                       |  GetBoardInformation (M: malformed board id throws C11), Validate (M)
 ConnectionHealthService (-)  <-- StateFor: only "authentication_    AzureDevOpsConnectionVerdict  (N, pure)
   (unchanged; 503 now lands     failed" is AuthenticationFailed      AzureDevOpsReadException      (-)
   in Unreachable)                                                  ServiceNow, Linear connectors   (-)
 DOMAIN (Models): ConnectionValidationResult (-), Board (-), BoardInformation (-)  -- no domain change
 Events: none.  Persistence: none.  New external call: none (same Jira/ADO endpoints as today).
```

### [REF] Context map, glossary, ARCHITECTURE.md (design answer)

Unchanged from DISCUSS: Tracker integration only, no new edge, no event. The DISCUSS-proposed glossary row
"Read refusal" still applies. Add one line to the Tracker integration entry of `ARCHITECTURE.md` section 4 in
DELIVER: each connector keeps one pure verdict class that turns a tracker's answer into the sentence both
Test connection and the board reads show (the shared "credential refused" factory DISCUSS asked DESIGN to
decide: it is per connector, not shared). ADR-126 gets its dated "Adoption" note; ADR-184 gets the status-aware
Jira classifier note; both edits are listed in the two ADR drafts.

### [REF] Handoff to DISTILL / DEVOPS

- **Slices**: 01 = `JiraConnectionVerdict` + Jira reads/validate/field list + `BoardWizard` registry copy (all
  connectors' C5, `boards = []` on failure, the superseded-answer guard, the two fallback sentences). 02 =
  `AzureDevOpsConnectionVerdict` + the ADO read paths, including the malformed-board-id typed failure and the
  403 permission sentence. Order and one-direction dependency as in DISCUSS. Method and string names in this
  design are suggested; DELIVER may rename them.
- **Gate (AFK-6)**: the DISTILL sketch walk of all new copy (C1 as reworded, C1m, C1o, C2, C2v, C3, C4, C5,
  C6, C7, C9, C10, C11, and the two frontend fallbacks) gates DELIVER.
- **External integrations** (annotation for platform-architect): Jira REST (`/rest/agile/1.0/board`, `.../configuration`,
  `/myself`, `/field`) and Azure DevOps Work/Project APIs, both already integrated. Contract tests recommended
  for Jira and Azure DevOps (consumer-driven contracts, for .NET e.g. PactNet) to catch a changed status or error
  body for the refused-credential case; this Bug adds none and relies on the live sandbox check.
- **Usage-data event**: DEVOPS to confirm the DISCUSS pre-answer, N/A (a failure is reported, nothing is used).
- **Enforcement tooling**: none new (Q8); the existing ArchUnitNET suite keeps guarding the health single writer.
- **Sketch walk at the start of DISTILL**: C1 to C8 as in DISCUSS (C1 as reworded in Decision 1), plus the new
  C9, C10, C11, the project-naming C7, the two fallbacks, and the four picker states above.

### [REF] HELD for the maintainer

- **HELD-1, ADO multi-project list (Decision 3): maintainer to decide.** Three options, none recommended here;
  a credential refusal on any project fails the whole list with C6 under all three. DISTILL leaves the
  multi-project scenario open until this is decided; every other ADO scenario can be written now.
- Not blocking, flagged (AFK-5): N3 (Jira 401 = 403 on the board list) stands, and diverges from ADR-126's
  ServiceNow 403 rung (`insufficient_permissions`). The maintainer may re-confirm it with that precedent in view.

Closed since the first draft: ADO 403 on Test connection (AFK-2 reversed: today's mapping stays, no
change) and the Jira timeout (decided in Decision 2).

## AFK decisions (2026-11-02, orchestrator; the maintainer reviews them on return)

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
   NUnit unit tests (connector failure paths with a faked HTTP handler or SDK client, the pure verdict classes,
   a `ConnectionHealthService.StateFor` pin) and Vitest tests (`BoardWizard` copy table); they run in the
   existing `ci_backend.yml` default filter (`Category!=Integration`) and `ci_frontend.yml`. The live-connector
   categories (`JiraIntegration`, `AdoIntegration`) are opt-in inputs of `ci_backend.yml` and are **not needed**
   and must not be added to the fix. The DESIGN's live check against a deliberately wrong credential on a real
   Jira and ADO sandbox is a **manual DELIVER step**, run by hand against the maintainer's sandboxes, never in
   CI, recorded in the DELIVER log (wrong email, wrong token, scoped token, ADO bad PAT). Linear: not touched,
   so the key shared with CI is not exercised. `ci_e2e.yml` and the verify workflows: no new Playwright spec
   (E2E stays a thin sanity check; the existing wizard walking skeleton is unchanged).
   **Placement rule (review F1):** CI runs `Category!=Integration` (`ci_backend.yml:114`), and
   `JiraWorkTrackingConnectorTest.cs` (lines 17-18) and `AzureDevOpsWorkTrackingConnectorTest.cs` (lines 16-17)
   carry `[Category("Integration")]`, so a test added there is silently skipped in CI. New failure-path tests go
   in **untagged** classes (`JiraFieldLookupTest` style, or a new untagged file per connector), never in those two
   classes. Side finding: the existing board happy-path tests in those two classes are CI-excluded today; this
   fix does not change that, and the new untagged tests are what put the board reads under CI for the first time.
   Static gates apply unchanged (review F6): `dotnet build` with `TreatWarningsAsErrors`, `pnpm build` (Biome +
   `tsc`), and no new SonarCloud issues.
3. **Observability: one operator-visible change, an improvement.**
   - Jira "Authentication is not valid" Information line (written for every non-success board status) becomes
     two truthful lines: a credential refusal (401/403) stays Information, "Jira refused the credential of
     {Connection} on {Read}: {Status}"; anything else is Warning naming the status. `RequestUri` and body fields
     are kept. The same phrase is also written by `ValidateConnection` (`JiraWorkTrackingConnector.cs:442`, besides
     the board line at `:1118`); **both** get the same split (review F3). Jira gains no Error line.
   - Azure DevOps (review F2): today the ADO board reads log at **Error** (`AzureDevOpsWorkTrackingConnector.cs`
     lines 402, 445, 639). Rule: a line where the read fails and the user sees the failure stays **Error**
     (unchanged); a line where the failure is survived (a project skipped while others answer) is **Warning**.
     Per HELD-1 option: **A** (fail whole list): :402, :445, :639 all stay Error, no change. **B** (partial list +
     notice): the same three stay Error for the failing project, since the notice shows the user the failure.
     **AFK-1** (skip while others answer): :639 for a skipped project drops to Warning (DESIGN Decision 3 says
     "keeps its Error-level log line"; that holds for A and B, and for AFK-1 only when no project answers and the
     list fails), :402 and :445 stay Error. So the ADO level change exists only under AFK-1 and counts as an
     operator-visible change (a skipped project no longer shows in an Error-level filter); DESIGN Decision 3's
     sentence is to be read with this. Nothing may log the email, token or a header value.
   - The old text "Authentication is not valid" appears only at `JiraWorkTrackingConnector.cs:442` and `:1118`
     (repo-wide grep done in this wave, hits elsewhere are this folder's own documents). No alert, dashboard or
     test keys on it, so the "grep before removal" deferral is **closed**: the phrase is replaced with no
     compatibility shim.
   - Connection health store: Jira Test connection with a 503 or other non-401/403 status now stores
     `Unreachable` (code `unexpected_answer`) instead of `AuthenticationFailed`. `ConnectionHealthService`
     is unchanged; 401/403 still give `AuthenticationFailed`. Operators will see fewer false "Authentication
     failed" states; the admin popover shows the C2v text. Board reads still write no health verdict (single
     writer, ADR-184).
4. **Release and rollback: trivial.** No EF migration, no setting, no feature flag, no data change. Rollback is
   reverting the commit(s) (or the previous release), which restores `200 []` for failures. The wire change
   (`200 []` becomes `400 {verdict}` on a failure of Jira and ADO board list/information) is consumed by the
   frontend `BoardWizard` only, shipped in the same build as the backend, so no version skew. Checked:
   `grep -ril "wizards"` and `/boards` over `/storage/repos/lighthouse-clients` (ts, md, json, no node_modules)
   returns nothing, so the CLI/MCP does not call the wizard endpoints and needs no version bump or release.
   Self-hosted users running an old frontend against a new backend does not occur (one bundle). Health-verdict
   rows written by the new code (a Jira 503 stored as `Unreachable`) stay after a revert (review F5); they are
   harmless and stale only until the next Test connection or refresh rewrites them.
5. **Mutation testing (per-feature, 80% minimum), targets:**
   - Stryker.NET: `JiraConnectionVerdict` and `AzureDevOpsConnectionVerdict` (status ladders: 401/403 vs
     other, C1/C1m/C1o, host-only, no secrets in text), the board-read catch ladders in
     `JiraWorkTrackingConnector` (`GetBoards`, `GetBoardInformation`, field list, `ValidateConnection` status
     branch) and `AzureDevOpsWorkTrackingConnector` (`GetBoards`, `GetBoardsForProject(s)`, the 401/403 versus
     other per-project rule). Per the Stryker traps: mutate by file, skip the acceptance suite, run last on
     frozen code, and kill survivors with targeted tests rather than hours of reruns. **Config (review F4):**
   start from `docs/feature/fix-jira-dc-portfolio-refusal/mutation/stryker.6019.connector.json`, which has
   `thresholds.break` 0; set **`break: 80`** for these targets. Its name-based test-case filter also runs the
   Integration-tagged connector classes, which helps the kill rate (the new tests in untagged classes still carry
   the kills CI sees). DELIVER outputs, named explicitly: the `mutate` globs (the two verdict class files and
   the two connector files above) and the config path, e.g. `docs/feature/bug-6499-jira-auth-failure-shows-no-boards/mutation/stryker.6499.connector.json`,
   plus the StrykerJS config for `BoardWizard`.
   - StrykerJS: `BoardWizard` empty-list/failure copy table (per connector, ServiceNow keeps today's text) and
     its guard that keeps Confirm disabled on a failure.
   - Not targets: `WizardsController` (unchanged), `ConnectionHealthService` (pin test only).
6. **Docs at finalize (DELIVER, not now):** `docs/concepts/worktrackingsystems/jira.md` line 73 (the "board
   wizard is empty" row for OAuth) and line 92 (the scoped-token note says Lighthouse displays "No Boards
   available"; after the fix it shows the C1m sentence saying boards cannot be offered to this sign-in): reword
   to the shipped sentence, and add a short troubleshooting line that a refused credential now shows in the
   picker. `azuredevops.md` line ~194 (what the picker shows): add that a refused PAT or a permission problem is
   named in the picker instead of an empty list. Per-feature screenshots: only if the DISTILL sketch walk adds
   a visible new state worth a shot (decide at finalize, say so explicitly). Also owed from DISCUSS: ADR-126
   and ADR-184 adoption notes, the glossary row "Read refusal" in ADR-270, `jobs.yaml` REVISED note. No release
   notes at finalize (written at /release). Terminology: copy and docs use the configurable terms.

### AFK decisions (DEVOPS)

| # | Decision | Why |
|---|---|---|
| DEVOPS-1 | No usage-data event (N/A, bug fix). | Nothing to count; avoids a catalogue entry whose only useful property would be a verdict code. |
| DEVOPS-2 | No new test category, workflow or CI job; new failure-path tests live in untagged classes, never in the two Integration-tagged connector test classes; live Jira/ADO check is a manual DELIVER step against sandboxes. | Live categories are opt-in, flaky and cost API quota; unit tests cover the failure paths deterministically. |
| DEVOPS-3 | Jira: refusal Information, everything else Warning, split applied at both `:442` and `:1118`; the old phrase is removed (grep done, no consumer). ADO: lines :402/:445/:639 keep Error, except a project skipped under AFK-1 which is Warning (an operator-visible change under that option only). | A user-fixable Jira state is not an application error and the old text was false for a 503; for ADO the Error stays wherever the user still sees the failure. |
| DEVOPS-4 | Jira 503 on Test connection stores `Unreachable` (health service unchanged). | Matches ADR-184 and DESIGN Decision 2; true state. |
| DEVOPS-5 | No Lighthouse-Clients change or release. | Clients do not call the wizard endpoints (grep empty). |
| DEVOPS-6 | Rollback = revert; no feature flag. | No data or schema change; flag would add a branch for a pure correctness fix. |
| DEVOPS-7 | Mutation targets as listed in answer 5. | Per-feature strategy in CLAUDE.md. |

HELD for the maintainer (big blast radius only): none new from DEVOPS. Still open from DESIGN: AFK-1 (ADO
multi-project list: fail all, skip non-credential failures, or partial list with a notice) and re-confirming N3
(AFK-5); DISTILL sketch walk (AFK-6) gates DELIVER.
