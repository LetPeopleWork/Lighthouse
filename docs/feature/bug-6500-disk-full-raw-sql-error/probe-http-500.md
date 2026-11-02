# Probe: what an ordinary API request returns on a database failure (Production mode)

Date: 2026-11-02. Question: in a Production-mode Lighthouse, a frontend API request hits a database
failure. What is in the HTTP response, and what does the frontend show?

## Method

1. `dotnet build` of the three projects (Release). The two migration projects were built first
   because the backend references them by path (`Lighthouse.Backend/Migrations/*.dll`, gitignored).
   Backend built with `--no-incremental -o <scratch>/out/` so the migration DLLs land in `deps.json`
   (the first, incremental build left them out and the app died with `FileNotFoundException`).
   The output is self-contained, so it is started as `./Lighthouse`, not `dotnet Lighthouse.dll`.
2. Production-mode run on a throwaway SQLite file and a throwaway key directory, all under
   `.../scratchpad/probe6500/` (`probe.db`, `keys/`), with:
   `ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://127.0.0.1:5299
   Database__Provider=sqlite "Database__ConnectionString=Data Source=<scratch>/probe.db"
   Encryption__KeyStorePath=<scratch>/keys`.
   Production `appsettings.json` has no Kestrel endpoints (only `appsettings.Development.json` pins
   5169/7246), so `ASPNETCORE_URLS` was honoured here and port 5299 was used. The Development
   profile was never loaded; 5169/7246 and the developer key ring were not touched.
3. `wwwroot` does not exist in the worktree. A one-line stub `wwwroot/index.html`
   (`<!doctype html><html><body>SPA-SHELL-STUB</body></html>`) was placed in the scratch output so the
   SPA fallback behaves as in a published build. The stub stands in for the real `index.html`; the
   mechanism is identical, only the body bytes differ.
4. Failure injection:
   - Attempt A, restart on a read-only DB (`chmod 444 probe.db`, WAL/SHM removed): the app **refuses
     to start**. `FATAL: SQLite Error 8: 'attempt to write a readonly database'`, "Application
     terminated unexpectedly" (migration step). Startup failures are therefore a different, non-HTTP
     path (process exit).
   - Attempt B (used for the results): start on a good DB, confirm `GET /api/latest/teams` = 200, then
     while running `mv probe.db probe.db.orig; rm probe.db-wal probe.db-shm; head -c 20000
     /dev/urandom > probe.db`. New connections now fail with
     `SqliteException: SQLite Error 26: 'file is not a database'`. This stands in for any provider
     exception thrown from a controller or service call; the pipeline path is the same.
5. Requests were made with `curl -si`. Cleanup: the app was stopped by PID, the scratch directory
   emptied.

## Pipeline reading (Program.cs)

`Lighthouse.Backend/Lighthouse.Backend/Program.cs:171-175`: outside Development,
`app.UseExceptionHandler("/Error")`. A search for the `/Error` route finds **no endpoint, controller
or page** with that path (`grep '"/Error"'` hits only Program.cs:173). The handler re-executes the
pipeline with path `/Error`; no endpoint matches, the `/api` 404 guard (Program.cs:247-256,
`ShouldNotFallBackToSpaShell`) does not apply to `/Error`, so `app.UseSpa` (Program.cs:258-263)
answers it.

## Observed responses

All exceptions were the unhandled `SqliteException ... Error 26` (server log, `ExceptionHandlerMiddleware`
at ERROR, so the exception is logged server-side but its text is never put in the response).

| Request | Status | Content-Type | Body |
|---|---|---|---|
| `GET /api/latest/teams` (Accept: application/json) | **500** | `text/html` | The SPA `index.html` shell (56 bytes, the stub). Headers also `Cache-Control: no-cache,no-store`, `Last-Modified`, `Accept-Ranges: bytes`. |
| `POST /api/latest/teams` (JSON body) | **500** | none | **Empty** (`Content-Length: 0`) |
| `PUT /api/latest/teams/1` (JSON body) | **500** | none | **Empty** (`Content-Length: 0`) |

Verbatim, first request:

```
HTTP/1.1 500 Internal Server Error
Content-Length: 56
Content-Type: text/html
...
Cache-Control: no-cache,no-store

<!doctype html><html><body>SPA-SHELL-STUB</body></html>
```

Verbatim, write requests:

```
HTTP/1.1 500 Internal Server Error
Content-Length: 0
Server: Kestrel
```

Why the two differ: after the exception handler re-executes `/Error`, `UseSpa` serves `index.html` only
for GET/HEAD. For POST/PUT it cannot, throws `InvalidOperationException: The SPA default page
middleware could not return the default page '/index.html'...`, which is logged and ends as a bare
500. In a published build the GET body is the real `index.html` (a few KB of HTML), not the stub.

Not exercised: `DELETE` and `GET /teams/{id}` returned `400 Bad Request` (empty, `Connection: close`)
from my curl shape before reaching the database, so they say nothing; the three rows above are the
evidence. Disk-full itself was not reproduced.

**Answer: no raw exception text on this path.** The response is the SPA HTML shell with status 500
(GET) or an empty 500 (writes). The raw SQL text reported in the bug must come from somewhere other
than the global exception handler (a controller or service that catches and returns `ex.Message`, or
a validation/connector path), which the RCA should keep as a separate branch.

## What the frontend does with it

- `Lighthouse.Frontend/src/services/Api/BaseApiService.ts:18`: `interceptors.response.use(assertNotHtmlResponse)`
  registers only the success handler. Axios rejects a 500, so the HTML guard
  (`htmlResponseGuard.ts:8-23`, which would raise the "outdated version, reload with Ctrl+Shift+R"
  `STALE_CLIENT` message) is **not** reached for a 500 HTML body.
- `BaseApiService.ts:45-62` (`createApiErrorFromAxios`) and `:64-76` (`parseApiErrorPayload`): when
  `response.data` is a string, `{ message: data }` is used verbatim (`BaseApiService.ts:75-77`).
  - GET 500: `ApiError.message` = the **entire index.html text** (code 500).
  - POST/PUT 500: axios gives `""` for an empty body, which is a string, so `ApiError.message` =
    **empty string** (the axios fallback `Request failed with status code 500` at line 73 is not used).
- Where that message goes:
  - Save flow `Lighthouse.Frontend/src/hooks/useModifySettings.ts:477-481` and `:215-222`:
    `setValidationError(error.message)` puts it in the form's validation banner, so the user sees either
    an HTML document dumped as text or an empty error area.
  - Wizards: `useCreateWizard.ts:204-205`, `:241-242` (`setValidationError(error.message)`);
    `CreateConnectionWizard.tsx:282,402`; `WizardStepperFrame.tsx:102` render `error.message`.
  - Anything unhandled: `SnackbarErrorHandler.tsx:140-151` turns an unhandled rejection into
    `reason.message` and `showError` shows it in the top snackbar (`:189-200`, auto-hides at 6 s).
  - Pages that use a fallback only when the message is falsy (e.g. `BoardWizard.tsx:27`,
    `BlackoutSettings.tsx:63`, `ModifyConnectionSettings.tsx:112`) get their fallback text for the
    empty-body (write) case but the HTML text for a GET.

So the UI today shows no database text, but it also shows nothing useful: an HTML page dump for reads,
an empty or fallback message for writes.

## Conclusion for DESIGN

On an ordinary request in Production, the global handler does not leak exception text: reads return a
500 with the SPA shell (`text/html`) and writes return a bodiless 500, so the raw SQL wording in the
bug is produced by some other path and this one is not the leak. It does, however, need its own
user-facing sentence: the global handler's target `/Error` does not exist, so the frontend receives
HTML or nothing and renders it as the error message (the HTML guard misses 500s because it sits on the
success interceptor). DESIGN should give the unhandled-database-failure case a deliberate JSON
ProblemDetails (or the project's existing error payload with a `code`) from the exception handler for
`/api` requests, and have the frontend map a 5xx with that code (and any 5xx with an HTML or empty body)
to one plain sentence such as "Lighthouse could not reach its database. Check the disk space and try
again." instead of passing `response.data` through as `ApiError.message`. A startup failure (read-only
or full disk at migration) is a separate path: the process exits with a `FATAL` log line and serves
nothing.
