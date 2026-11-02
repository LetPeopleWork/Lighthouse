# Slice 04: An ordinary page request that fails says so in a sentence

(The file name predates the probe; the slice is no longer conditional.)

Story US-6500-01 · job `job-operator-read-the-warnings-without-reading-the-log` · estimate 0.5 to 1 day (backend
handler about half a day, frontend mapping about half a day)

## Goal

A user whose page request meets a failure sees a short plain sentence, never an HTML document and never an empty
error box. Today `UseExceptionHandler("/Error")` (`Program.cs:173`) points at a route that does not exist, so a GET
gets 500 with the SPA `index.html` and a POST/PUT gets 500 with no body; the frontend copies either into the error
message (`BaseApiService.ts:75-77`) because its HTML guard sits on the success path only (`:18`). See
`probe-http-500.md`.

## IN scope

- Backend: an `/api` request that fails with an unhandled exception answers with a ProblemDetails body, and the dead
  `/Error` handler is replaced. The body carries no exception text. A database fault carries one database sentence for every caller ("Lighthouse cannot use its database right now. Please tell your administrator.");
  anything else carries a safe generic sentence.
- Frontend: any 5xx response whose body is HTML or empty becomes a plain sentence in `ApiError.message`, which is
  what `setValidationError` and the error snackbar already show. A 5xx with a ProblemDetails body shows its sentence.
- No role check and no RBAC service in the handler (AFK-D5, replacing A7): every caller, signed in, anonymous or auth
  off, gets the same answer, and exactly two `/api` sentences exist (database, other). The cause for administrators
  stays in Task Manager and the log.

## OUT scope

A new error screen; retry pacing; per-controller catches; non-`/api` requests; startup failures (the process exits
with a `FATAL` log line and serves nothing, which is a different path).

## Domain examples

1. **Read during an outage**: Jonas Brandt opens the Teams page at 09:14 while Postgres is out of disk. The page's
   snackbar says "Lighthouse cannot use its database right now. Please tell your administrator." instead of the page's
   HTML.
2. **Save during an outage**: Priya Nair edits Team "Platform Core" settings and presses Save while her SQLite volume
   is read-only. The validation banner says "Lighthouse cannot use its database right now. Please tell your administrator."
   instead of staying empty.
3. **Not a database fault**: a null reference escapes a controller on Marta Keller's request. She sees "Lighthouse
   could not finish that request: try again, and if it keeps failing ask your administrator to open the log." and no
   stack or message text.
4. **Anonymous caller**: an unauthenticated request to an `/api` route that throws a database fault gets the same
   database sentence as everyone else, which names no product or volume.
5. **Proxy page**: a reverse proxy in front of Lighthouse answers 502 with its own HTML error page; the user sees the
   plain sentence, not the proxy's HTML.

## UAT scenarios (BDD)

```gherkin
Scenario: A page that cannot load during an outage says why
  Given Jonas Brandt is signed in and Postgres has run out of disk space
  When he opens the Teams page
  Then the error says Lighthouse cannot use its database and to tell the administrator
  And the response body holds no exception text and no HTML

Scenario: A save that fails during an outage is not silent
  Given Priya Nair is signed in and her SQLite data folder is read-only
  When she saves the settings of Team "Platform Core"
  Then the validation banner shows the database sentence, not an empty area

Scenario: A failure that is not a database fault gets a safe sentence
  Given an unexpected error escapes while serving Marta Keller's request
  When the response arrives
  Then she sees the generic sentence and no exception text
  And the log holds the exception

Scenario: An anonymous caller gets the same database sentence as everyone else
  Given an unauthenticated /api request hits a database fault
  When the response arrives
  Then the body is the database sentence every caller gets and does not mention PostgreSQL

Scenario: A proxy's HTML error page is not shown as text
  Given a reverse proxy answers 502 with an HTML page
  When the frontend receives it
  Then the user sees the plain sentence, not the HTML
```

## Acceptance criteria

- [ ] An `/api` request failing with an unhandled exception answers with a ProblemDetails body (GET, POST and PUT
      alike): never the SPA shell, never an empty body, and no exception text.
- [ ] For every caller (signed in, anonymous, auth off) a database fault carries "Lighthouse cannot use its database
      right now. Please tell your administrator."; any other failure carries the generic sentence; no other `/api`
      sentence exists and the handler makes no role check.
- [ ] The `/Error` handler that routes to nothing is gone; the exception is still logged once.
- [ ] Any 5xx whose body is HTML or empty is turned by the frontend into a plain sentence; `ApiError.message` is never
      HTML and never empty for a 5xx.
- [ ] 4xx domain messages (validation, conflict) reach the user byte-identical to today.
- [ ] The failure look is the existing snackbar and validation banner; no new control; the loading look is unchanged.

## Technical notes and dependencies

Depends on slice 01 (the classifier). The handler uses no RBAC service. The one `WebApplicationFactory` test injects
the database fault through a test-only faulting service (a dead connection string is the alternative). DELIVER check:
the status of the `/index.html` fallback for a non-`/api` path (500 expected). The frontend 5xx check lives in
`createApiErrorFromAxios` (`BaseApiService.ts:46-65`; the string-body copy that leaks HTML is at `:80-81`). `probe-http-500.md` is the input and its method is repeatable. The Lighthouse-
Clients check is answered in the delta: a 5xx `title` surfaces as the existing optional `problemTitle`, so no
changeset. Frontend copy is sketched (Sketch 4) and walked through at the start of DISTILL.

## Reference class

One middleware or filter plus a test, and one mapping in `BaseApiService` plus a test, about half a day each.
