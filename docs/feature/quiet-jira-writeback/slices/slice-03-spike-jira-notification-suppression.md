# Slice 03 (SPIKE) - Verify Jira notification suppression + permission failure mode

**Type:** spike (probe, no ship) | **Est:** ~0.5 day, timeboxed | **Stories:** none - gates D4/D5, verifies D10

## Why this exists

D7: Atlassian's own evidence conflicts on what happens when the credential lacks the required permission.

- Cloud docs: the request is **silently ignored** - HTTP 204, watchers emailed anyway.
- [Community report](https://community.atlassian.com/forums/Jira-questions/Using-notifyUsers-parameter-still-fires-notifications-on-api-2/qaq-p/816532):
  a hard error, *"To discard the user notification either admin or project admin permissions are required."*

These demand opposite designs. If Jira **errors**, Lighthouse can surface the failure from the write-back
result and the pre-check is a convenience. If Jira **silently ignores**, Lighthouse would log success while
the email storm continues - and the D5 pre-check becomes the only thing standing between us and a false
promise. The story itself asks to "check the jira api in deep". Not designable on guesswork.

## Learning hypothesis

Disproves **D2/D4/D5** if any of the following turn out false:

- `notifyUsers=false` actually stops watcher email on DC 7.2.0+ and on Cloud.
- Cloud bulk edit with `sendBulkNotification: false` succeeds with **only** "Make bulk changes" - no `Administer Jira`.
- `mypermissions` accurately predicts whether suppression will work.
- `AuthenticationMethodKey` reliably discriminates Cloud from DC.

Confirms the slice plan if all four hold.

## Questions to answer (each with recorded evidence)

1. **DC, permitted:** `PUT /rest/api/2/issue/{key}?notifyUsers=false` with project-admin. Does the watcher
   get email? Does the history entry still appear? (Expect: no email, history present.)
2. **DC, under-permissioned:** same call, credential with only Edit Issues. **403 or silent ignore?**
   Record the exact status code and body.
3. **Cloud, permitted:** same via `/rest/api/3/`. Watcher email?
4. **Cloud, under-permissioned:** same. 403 or silent ignore? Exact status + body.
5. **Cloud bulk:** `POST /rest/api/3/bulk/issues/fields` with `sendBulkNotification: false`, credential
   holding **only** "Make bulk changes" + browse + edit. Succeeds? Watcher email? Record the taskId
   response shape, the progress endpoint, per-item outcome shape, and end-to-end latency for ~50 issues.
6. **Probe accuracy:** `GET /rest/api/3/mypermissions?permissions=BULK_CHANGE` and
   `GET /rest/api/2/mypermissions?permissions=ADMINISTER,ADMINISTER_PROJECTS`. Do verdicts match observed
   behaviour in 1-5? Note that `mypermissions` is project-scoped for project permissions - does it need a
   `projectKey`, and which project when a write-back batch spans several?
7. **Discriminator:** does a Jira **DC** instance ever authenticate via `jira.oauth`? If yes, D4 is unsafe
   and DESIGN needs a different discriminator (e.g. probing `/rest/api/3/` availability or serverInfo).
8. **`latest` vs v3:** Lighthouse calls `rest/api/latest`. What does `latest` resolve to on Cloud? The bulk
   API is v3-only, so slice 06 must pin `/rest/api/3/` explicitly.
9. **Multi-field PUT = one email?** (Verifies D10 / slice 02.) `PUT /rest/api/2/issue/{key}` with **four
   changed fields in one `fields` object** vs four single-field PUTs to the same issue. Does the watcher
   receive **one** email or four? How many issue-history entries result - one grouped changelog entry or
   four? Slice 02 batches on the assumption that one call = one notification; it is near-certain but
   currently unverified, and it is load-bearing for that slice's value story. Also confirm the batched
   PUT's failure shape: if one field in the payload is invalid, does Jira reject the **whole** call, or
   apply the valid fields and report per-field errors? (Drives AC-05.3 / AC-05.4.)

## IN scope

- Manual / scripted probes against a real Cloud site and a real DC instance. `curl` or a throwaway console
  app is fine - no production code, no tests, no commits to the connector.
- A findings note appended to this brief: answers to 1-9, verbatim status codes and bodies.

## OUT of scope

- Any production code change. Slices 04-06 do the real Jira work; slices 01-02 do not depend on this spike.
- Automating the probes in CI.

## Acceptance criteria

- All 9 questions answered with recorded evidence (status code + body, and observed inbox state).
- A written verdict on D4, D5 and D7: **confirmed**, or **disproved with the required design change**.
- If Q5 fails (bulk needs more than "Make bulk changes"), D2's Cloud rationale collapses -> escalate to the
  user before slice 06 is designed, because the whole point of the Cloud path is the lower permission bar.
- If Q9 shows one email per changed field regardless of call count, slice 02's email claim is void ->
  reword its value story to API-call / history / churn reduction only, and do not ship the email claim in
  docs or release notes.

## Dependencies

- Real Jira **Cloud** site + real Jira **DC** instance (7.2.0+).
- **Two credentials per instance** - one with the elevated permission, one without. Without the
  under-permissioned credential, questions 2, 4 and 6 - the entire reason this spike exists - cannot be
  answered.
- An issue on each with a watcher whose inbox is observable.

## Taste tests

- Value-bearing: N/A - explicitly a probe, exempt from the slice value gate (D7).
- Right-sized: 8 questions, two instances, timeboxed half a day. PASS.
- Disproves a pre-commitment: yes - can invalidate D2, D4 and D5. PASS.

---

# FINDINGS - run 2026-08-08, Jira Cloud only

Site `letpeoplework.atlassian.net` (Jira **Free** plan). Credentials: **A** =
`atlassian.pushchair@huser-berta.com` (site admin, the CI integration credential), **B** =
`benjamin@letpeople.work` (licensed user, no site admin).

**DC was not probed** - no Data Center instance is available, and one cannot be obtained before release.
Q1, Q2 and the DC half of Q7 are therefore **unanswered**, deliberately, and are deferred to a
post-release verification. Everything below is Cloud-only evidence.

## Verdicts

| D | Verdict | Evidence |
|---|---|---|
| **D7** (Cloud: error or silent ignore?) | **RESOLVED - Jira ERRORS** | Q4 below |
| **D2** (Cloud bulk = lower permission bar) | **DISPROVED** | Q5 below |
| **D4** (`AuthenticationMethodKey` discriminates Cloud/DC) | **REPLACE** | Q7 below |
| **D5** (`mypermissions` predicts suppression) | **CONFIRMED**, with a caveat | Q6 below |
| **D10** (one call = one notification) | **history CONFIRMED**, email pending | Q9 below |

## Q4 - under-permissioned `notifyUsers=false`: **HTTP 403, and the whole write is rejected**

B (`ADMINISTER_PROJECTS: false` on project `SPIKEPRM`):

```
PUT /rest/api/2/issue/SPIKEPRM-1?notifyUsers=false
403 {"errorMessages":["To discard the user notification either admin or project admin permissions are required."],"errors":{}}
PUT /rest/api/3/issue/SPIKEPRM-1?notifyUsers=false   -> identical 403 (v2/v3 parity)
PUT /rest/api/2/issue/SPIKEPRM-2                      -> 204 (control: same credential, no param)
```

The community report is right and the Cloud documentation is wrong: Jira **errors**, it does not silently
ignore. Confirmed afterwards that `SPIKEPRM-1.duedate` was still `null` - **the field update did not
happen**. The 403 rejects the entire request, not just the suppression.

**Positive control for Q4:** the `SPIKEPRM-2` plain PUT (same credential B, no param, 204) **did** deliver a
watcher email to A. So B can write and notify normally - the 403 is specific to the suppression request,
not a broken credential, and `SPIKEPRM-1` produced no email only because nothing was written.

**This inverts slice 04's premise.** Its Dependencies note assumed the worst case was "silent ignore", in
which the slice would "still ship (it is strictly better than today)". The actual worst case is worse:
shipping D3 (always-on `notifyUsers=false`, no settings) turns a **working-but-noisy** write-back into a
**totally broken** one for every customer whose credential lacks admin or project-admin. That is a
regression, not an improvement, and slice 04 cannot ship as written.

## Q5 - Cloud bulk least-privilege: **DISPROVED, D2's Cloud rationale collapses**

B holds `BULK_CHANGE: true` globally, `EDIT_ISSUES: true`, `ADMINISTER_PROJECTS: false`:

```
POST /rest/api/3/bulk/issues/fields  sendBulkNotification:false
403 {"errors":[{"message":"You do not have the necessary permissions to disable bulk mail notifications for this operation."}]}

POST ... sendBulkNotification:true    -> 201 {"taskId":"35636"}
POST ... flag omitted                 -> 201 {"taskId":"35639"}
```

"Make bulk changes" is enough to **bulk edit**, but **not** to suppress notification - suppression needs
admin/project-admin on the bulk path exactly as on the per-issue path. The Cloud transport therefore
offers **no lower permission bar**, which was the entire stated point of slice 06. Bulk still reduces API
calls (1 request for N issues) - that value survives; the permission argument does not.

**Positive control for Q5:** the Q5c and Q5d bulk edits both landed on `SPIKEPRM-3` and both **delivered a
watcher email** to A ("Be Hu made 2 updates", Due date 6/Oct then 6->7/Oct). So the bulk API is fully
functional for B - the 403 is specific to the suppression request, not to bulk editing.

**Default value:** omitting `sendBulkNotification` behaves as **`true`** (Q5d mailed the watcher). The flag
must be sent explicitly to be quiet; there is no quiet-by-default.

**Escalation raised per this slice's own acceptance criteria** - slice 06 must not be designed until the
user rules on the remaining rationale.

## Q5 mechanics (answered, useful regardless)

- `POST /rest/api/3/bulk/issues/fields` -> **201**, body is only `{"taskId":"35628"}`.
- Progress: `GET /rest/api/3/bulk/queue/{taskId}` -> `status:"COMPLETE"`, `progressPercent:100`,
  `processedAccessibleIssues:[30541,30540]` (numeric **ids**, not keys), `invalidOrInaccessibleIssueCount:0`,
  `failedAccessibleIssues:{}`.
- **`ended` stays `null` even at COMPLETE** - do not gate completion on it; use `status`.
- Submit latency ~784 ms for 2 issues. ~50-issue latency not measured.
- Bulk-editable custom-field types confirmed: `datepicker`, `datetime`, `float`. **A plain text custom
  field does not exist on this site, so the string write-back case is UNVERIFIED** - write-back targets are
  user-configured `customfield_*` of date / number / string type (`WriteBackTriggerService.cs:115`,
  `JiraWorkTrackingConnector.cs:310-312`), so this gap must be closed before slice 06.
- 19 fields are excluded from bulk edit entirely (`Epic Link`, `Parent Link`, `Organizations`, forms, ...).

## Q6 - `mypermissions` accuracy: **CONFIRMED both directions**

| Project | `ADMINISTER_PROJECTS` predicted for B | Actual `notifyUsers=false` |
|---|---|---|
| `SPIKEPRM` | `false` | **403** |
| `DUMMY` | `true` | **204** |

The probe is a reliable pre-check. **Caveat (trap):** called **without** `projectKey`,
`GET /rest/api/3/mypermissions?permissions=ADMINISTER_PROJECTS` returns `havePermission: true` at
**HTTP 200** - it does not 400. A pre-check that omits project context silently over-reports. Since a
write-back batch spans projects, D5's pre-check must be **evaluated per project**, not once per connection.

## Q7 - deployment discriminator: **use `serverInfo`, not `AuthenticationMethodKey`**

`GET /rest/api/2/serverInfo` -> `{"deploymentType":"Cloud","version":"1001.0.0-SNAPSHOT","buildNumber":100293}`.
A positive capability signal, verifiable without a DC instance. D4's auth-key heuristic was never tested
against DC and should be replaced by this rather than carried into DESIGN unverified.

## Q8 - `latest` resolves to **v2**

Fingerprinted via description shape on the same issue: `/rest/api/latest` -> plain string (v2 behaviour),
`/rest/api/2` -> plain string, `/rest/api/3` -> ADF object. Lighthouse calls `rest/api/latest`
(`JiraWorkTrackingConnector.cs:325`) and is therefore on **v2** today. The bulk API is v3-only, so slice 06
must pin `/rest/api/3/` explicitly - confirmed, not assumed.

## Q9 - one call = one notification

**History half - CONFIRMED 4:1.** Same 4 fields, two issues:

| Issue | Calls | Changelog entries |
|---|---|---|
| `DUMMY-4` | **1 PUT, 4 fields** | **1** entry (`summary, labels, duedate, priority`) |
| `DUMMY-5` | **4 PUTs, 1 field each** | **4** entries |

So slice 02's churn-reduction claim holds on issue history regardless of the email outcome.

**Email half - ANSWERED, and it VOIDS slice 02's email claim.** Observed in B's inbox:

| Issue | Calls | Changelog entries | **Emails** | Digest header |
|---|---|---|---|---|
| `DUMMY-4` | **1 PUT, 4 fields** (+1 bulk) | 2 | **1** | "made 2 updates" |
| `DUMMY-5` | **4 PUTs, 1 field each** (+1 bulk) | 5 | **1** | "made 5 updates" |

**Jira Cloud batches notifications per (recipient, issue) over a ~10 minute window**, so one call and four
calls both produced exactly **one** email. Call count does not drive email count. The digest renders the
four separate PUTs as four blocks and the single 4-field PUT as one block, but it is still one message.

Consequence: **slice 02 must not claim "fewer emails".** On a site with notification batching enabled (the
default) batching already collapses per-issue noise, and slice 02 changes nothing about it. Slice 02's
value story is **API-call reduction and issue-history churn reduction (4:1, above)** only. The email claim
may hold for a customer who has *disabled* batching, but that is unverified and must not be asserted in
docs or release notes.

Batching also means the whole "email storm" framing needs re-checking: the storm is per-issue-per-window,
not per-write.

## Q3 - `notifyUsers=false` on a permitted credential: **CONFIRMED (suppression works)**

- `DUMMY-7` control (notifications ON, single PUT): **email delivered** ~3 min later, listing Due date. The
  pipeline and notification scheme are verified good (scheme 10000 routes `Issue Updated` -> `AllWatchers`),
  so a silent inbox elsewhere is real evidence rather than a broken setup.
- `DUMMY-6` (`notifyUsers=false`, A = permitted credential): **no email**.
- Cross-transport: the bulk write with `sendBulkNotification:false` landed `Start date` on `DUMMY-7` at
  02:40 and it is **absent** from that issue's digest, while the same field written to `DUMMY-4` with
  `sendBulkNotification:true` **is** present. Both suppression mechanisms work.

The change still appears in issue history in every case (D1 holds) - the unsuppressible channel.

## Incidental finding - permission-scheme shape matters

All six pre-existing projects grant `ADMINISTER_PROJECTS` to holder type **`applicationRole` (any licensed
user)**, so *every* user is project admin there and `notifyUsers=false` just works. A **newly created
team-managed project** (`SPIKEPRM`) binds a fresh scheme granting only to **project roles** - no
`applicationRole` grant. Two consequences:

1. On a stock Cloud site the suppression failure may be **rarer** than the community report implies - but it
   is entirely a function of one permission-scheme grant, so it cannot be assumed either way per customer.
2. Jira **Free** forbids editing permission schemes at all
   (`403 "Changing permission schemes is not allowed on the Jira free plan."`), so the under-permissioned
   credential had to be obtained by creating a new team-managed project rather than by demoting a user.

## Requirements for a silent write-back (documentation input)

This is the answer end users need, and it is **per project**, not per connection.

**Jira Cloud - VERIFIED.** The write-back credential needs, for **every project** it writes into, either:

- **Administer Jira** (global permission), or
- **Administer Projects** (project permission) on that project.

Without it, that project's write-backs are **not** silent - Lighthouse still writes the value (via the
403 retry) but watchers are emailed. Because the permission is project-scoped, the **same connection can be
silent in one project and noisy in another**. Docs must say this; a single connection-level yes/no would be
wrong.

**Jira Data Center - NOT VERIFIED.** Atlassian documents the same admin / project-admin requirement, and
the behaviour is assumed identical pending the post-release check below. Do not claim DC suppression works
until it is verified.

**Azure DevOps.** Already suppresses via `suppressNotifications: true`
(`AzureDevOpsWorkTrackingConnector.cs:356`); no additional permission is known to be required. Not
re-verified by this spike.

### What "silent" does NOT mean

Suppression covers **email notification only**. Regardless of permission, every write-back still:

- appears in the **issue history / changelog** (D1 - verified in every case here),
- bumps the issue's `Updated` timestamp,
- fires **webhooks**, listeners and automation rules.

Docs must set this expectation explicitly, so an unsuppressible channel is never reported as a bug later.

### Nuance worth documenting: batching already hides per-field noise

Even with **no** suppression, Jira Cloud batches watcher mail per (recipient, issue) over a ~10 min window.
So a user does not receive one email per changed field - they receive one digest per issue per window. The
noise customers actually feel is **one email per issue per cycle**, which scales with portfolio size and is
only addressed by suppression, not by batching fields into fewer calls.

## Post-release DC checklist (Q1/Q2, plus one raised on 2026-08-08)

1. **Q1** DC, permitted credential: does `?notifyUsers=false` stop watcher email? Does history still record?
2. **Q2** DC, under-permissioned: 403 or silent ignore? Exact status + body.
3. **Q10 (new)** The DC **UI** bulk-change wizard exposes a "Send mail for this update" checkbox that the
   maintainer believes worked **without** admin rights. Determine whether that is because (a) the account
   was in fact project admin - the most likely explanation, since every licensed user is project admin on
   the Cloud test site via an `applicationRole` grant and that shape is common on DC, (b) the UI
   bulk-change path enforces a different permission than the REST `notifyUsers` param, or (c) DC genuinely
   differs from Cloud. If (b), a UI-equivalent REST path may exist that suppresses under a lower bar - and
   the deferred slice 06 would deserve a second look on DC rather than Cloud.

None of these change the shipped design: slice 04's retry-on-403 is safe under every outcome.

## Reproduction

Probe scripts are throwaway (`curl` + `jq`), no production code touched, nothing committed to the
connector. Scratch issues created: `DUMMY-4..7`, `SPIKEPRM-1..3`, plus project `SPIKEPRM` - all disposable.
