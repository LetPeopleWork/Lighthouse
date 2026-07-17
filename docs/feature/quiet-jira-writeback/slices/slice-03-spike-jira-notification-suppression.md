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
