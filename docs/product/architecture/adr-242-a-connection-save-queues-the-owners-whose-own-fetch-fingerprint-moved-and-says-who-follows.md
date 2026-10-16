# ADR-242: A connection save queues the owners whose own fetch fingerprint moved, and says who follows

**Status**: Accepted (2026-10-16 — Morgan, DESIGN wave, PROPOSE, pass 2). Pass 1 proposed a save notice carried in
the PUT response; the maintainer withdrew it. Who follows is now said by the pre-save dialog (counts) and one log
entry (names).
**Feature**: `epic-6303-configure-once-per-connection` (Stories #6304 slices 05a, 06; #6313)
**Narrows**: [ADR-239](./adr-239-a-connection-save-that-moves-the-fetch-fingerprint-starts-the-affected-updates.md)
§1 (which owners are queued) and §8 (what the 6307 dialog shows and when). ADR-239 §3 stands: the PUT response is
unchanged and the page still navigates to `/` after a save.

## Context

ADR-239 compares the connection's share of the fetch fingerprint, `FetchFingerprint.ForConnection`, before and after
a connection save, and queues **every** owner on the connection when it moved. It rejected comparing each owner's full
`For()` as "same answer, at the cost of loading every owner before the merge".

Connection defaults (ADR-241) change that. A default is connection data, but it must not enter `ForConnection`:

- `For` splices `ForConnection` into every owner's digest (`FetchFingerprint.cs:80-96`), so rendering a default there
  changes every stored fingerprint at the upgrade — a full download for every owner on release day.
- An owner that keeps its own value is not affected by a default and must not be queued (D13).

So a default reaches the fingerprint only through each follower's effective value, and only a per-owner comparison
can see which owners a save moved.

The admin who changes a default also needs to know who it reaches. The maintainer ruled out an on-screen notice after
the save (the page leaves for `/`, as 6307 decided) and asked instead for: follower counts in the dialog before the
save completes, one log entry naming the followers, and the followers' updates showing in Task Manager → Activity as
any update does. Settings → System Info → Logs is readable by `SystemAdmin` only (`LogsController.cs:23`), the same
role that can save a connection.

## Decision

1. **Defaults are never rendered by `ForConnection`.** `FetchShapingPropertyGuardTest` gains a third category, "reaches
   the fingerprint through the owner's effective value", for the default and source properties.
2. **One application service owns the save; the controller maps and calls it.** A new
   `IConnectionSaveService` (`Services/Interfaces`, implemented in `Services/Implementation/Connections`) holds the
   connection, Team and Portfolio repositories, the domain-event dispatcher and its own logger. It **replaces
   `IDomainEventDispatcher`** on `WorkTrackingSystemConnectionController`, whose only use of the dispatcher is the
   fan-out that moves into the service, so the controller stays at **7** constructor parameters (S107 fires above 7,
   `docs/ci-learnings.md`). The controller keeps its Team and Portfolio repositories for the DELETE's "in use" check.
   The save is two calls inside one request, so one `DbContext`, one unit of work. `GetEntityByIdAnExecuteAction`
   (`API/APIHelpers.cs`) reads the entity once and runs its lambda once, with no retry, so `Open` simply replaces that
   helper's read in the PUT; nothing has to move inside a retry. (`SaveWithRetry` in `LighthouseAppContext.cs:734-757`
   retries only conflicts on entities **without** a concurrency token, and the connection has one, so a conflict on it
   is never retried and always reaches the 409 filter.)
   - `Open(connectionId)` reads the tracked connection and its Teams and Portfolios, and renders each owner's `For`
     **from that same read**, before anything is changed. It returns a save handle, or nothing when the id is unknown
     (the controller answers 404 as today).
   - The controller maps the DTO onto the handle's connection exactly as it does today (name, options, Additional
     Fields, write-back mappings, and now the defaults), and keeps today's write-back validation (400) and licence
     check (403).
   - **The defaults travel as one nested `defaults` member of the connection request**, the connection-side
     counterpart of G-R1 (b) (an owner PUT without the source keeps the stored source). When `defaults` is absent,
     every stored default stays as it is, so a browser tab opened before the upgrade, or a script that drops
     properties it does not know, can never clear one. When `defaults` is present it is authoritative: each member
     sets its default, and a null inside it clears that default, which the save then logs like any other changed
     default (decision 4). With flat nullable members, *absent* and *cleared* would bind alike; nesting is what
     keeps them apart.
   - `Commit(handle, concurrencyToken, startUpdates)`, in order:
     1. **In-use check from a fresh read, not from the `Open` snapshot.** One set-based query in the same unit of
        work: the Teams and Portfolios on this connection whose own value of any of the five settings is one of the
        removed field ids, plus the connection's own default ids as they now stand. Its rows are handed to the pure
        refusal rule. A Team saved with field X after `Open` is therefore seen, and removing X is refused, naming it.
     2. Apply the client's concurrency token and `Save()`.
     3. **"After" from a fresh read**: the connection's Teams and Portfolios read again, untracked, with their
        connection, as the 6307 fan-out already reads its owner ids after the save; each `For` rendered from those.
        The pure movers rule compares them with the `Open` fingerprints.
     4. Publish the unchanged `ConnectionFetchShapeChanged` for the movers, and write the log entry from the fresh
        read's followers.
     It returns *saved* or *refused, field in use* with the referrers; the controller turns the second into the 409
     `additional-field-in-use`. A save that was stored still answers 200 if publishing or logging fails afterwards
     (Story 6307's rule, moved with the fan-out).
   **The rules are pure, take data only, and live in `Models`.** "Which owners moved" takes two plain maps (owner id →
   fingerprint string, before and after) and returns owner ids; it never sees `FetchFingerprint`, which stays in
   `Services`, so `Models` gains no dependency on `Services`. "Which removed fields are still in use" takes the removed
   field ids, the connection's default ids and the owner rows the query returned, and returns the referring defaults
   and owners. Neither reads the database or holds a repository: **`IConnectionSaveService` does every database read**
   (the `Open` read, the in-use query, the after read) and only acts on the rules' answers. A field-definition edit
   still moves every owner (ADR-239's behaviour, now as a special case); an Estimation default moves nobody (it is read
   at page load, not fetched); `startUpdates=false` still publishes nothing. (Recommended option, taken while the
   maintainer was AFK.)
   **What remains of the race, accepted.** The in-use query and the save are two statements: a Team save that commits
   between them can still name a field this save removes. That window is milliseconds and needs two admins acting at
   once; closing it would need a serialisable transaction on Postgres for a rare case. Its effect is today's (the
   owner reads nothing for that setting), and it is narrowed from the other side: the Team / Portfolio PUT refuses
   (400) an own value that names no Additional Field of the owner's connection, read in its own unit of work.
3. **The save uses the connection's existing concurrency token.** `WorkTrackingSystemConnection` already carries a
   `ConcurrencyToken` configured with `IsConcurrencyToken()` (`LighthouseAppContext.cs:316-318`); the PUT already
   applies the client's token (`WorkTrackingSystemConnectionController.cs:114-117`), a stale one raises
   `DbUpdateConcurrencyException`, `ConcurrencyConflictExceptionFilter` answers 409 `concurrency-conflict`, and the
   page already says "This connection was changed by someone else since you opened it"
   (`ModifyConnectionSettings.tsx:55-57`). `Commit` keeps exactly that. Because "before" is rendered from the row
   read in the same unit of work, and the save fails if that row changed since the client loaded it, the diff, the
   queue and the log entry are computed against the state the save replaced. A refused save publishes nothing and logs
   no entry, since both happen only after a successful `Save()`.
   **A save without a token, as the code behaves today** (`LighthouseAppContext.cs:759-766`, `:780-786`): the token is
   still an EF concurrency token, so the `UPDATE` is guarded by the token value **this request read**. A save that
   commits between this request's read and its `Save()` therefore still yields 409; anything older than that is
   overwritten. And because `ApplyConcurrencyTokenForEdit` is the only place a modified connection gets a new token
   (tokens are regenerated only for *added* rows), a token-less save **leaves the token unchanged**: a browser form
   opened before it can still save over it without a 409. Both are today's behaviour, kept; the CLI sends no token.
   **Owners changed during the save** (accepted limit). "Before" comes from the `Open` read; "after" and the log's
   follower names come from the fresh read in `Commit`. An owner whose own setting another admin saved in between can
   therefore show up as a mover (one extra full download, harmless) or be missed as one; its own save queues its own
   update, and a missed one is picked up by its next scheduled update, which compares its stored fingerprint. The
   log names the followers as they were at commit.
4. **One Information log entry per save that changed a default**, written by the service after `Save()`: the
   connection's name, each changed default (setting, grain, old and new display name), its followers by name (none is
   said as such), and how many Teams and Portfolios were queued. Written for every default-changing save, followers
   or not. Names only: no credentials, no tracker field values. **The PUT response is unchanged**, so the CLI and MCP
   clients see nothing new. **Where it is read:** `ILogger` goes to Serilog, whose file sink is what Settings → System
   Info → Logs shows (`LogsController.GetLogs` reads `SerilogLogConfiguration`'s log folder). The shipped minimum level
   is Information (`appsettings.json`, `Serilog:MinimumLevel:Default`), and the page's controller is `SystemAdmin`-only
   (`LogsController.cs:23`). An admin who raised the level to Warning on that page will not get the entry; accepted,
   since that admin chose to see less, and Warning would misstate a normal save.
5. **The browser asks before, with counts.** The reused 6307 `UpdateQuestion` reads follower counts per default from
   the connection list read the page already makes (`usedBy`, through the existing `IWorkTrackingSystemConnectionUsage`
   port). Per save:
   - nobody follows any changed default and no Additional Field changed → **no dialog**; the save works as today;
   - only defaults read at page load (Estimation) with followers changed → **plain**: Cancel / Save (built in slice
     12, on top of slice 06's dialog, since it needs the Estimation default);
   - a fetch-time default with followers changed, or an Additional Field changed → the **update question**: Cancel /
     Save only / Save and update.
   The dialog opens for a changed default **from slice 06**. In slice 05a a changed default saves exactly as today,
   with no dialog (only an Additional Field change opens one, as today), and the log entry is still written. So no
   dialog copy the maintainer has not approved is ever built, and #6304 releases only after slice 06.
   The title carries counts per grain ("Update 3 Teams and 0 Portfolios?", "Change the Estimation default?"); names
   are never shown in the dialog. Counts are as of page load; the log entry, computed at save, is the record.
   **The choice the dialog returns, and the usage-data guard.** `ISaveConnectionChoice`
   (`services/Api/WorkTrackingSystemService.ts:23-25`) gains `additionalFieldsChanged: boolean`, set from
   `fieldsChangedSince(loadedFields, additionalFields)` (`ModifyConnectionSettings.tsx:575`), which already decides
   whether 6307's question is asked. `EditConnection.saveAndNavigate` (`EditConnection.tsx:90`) changes its guard from
   `if (choice)` to `if (choice?.additionalFieldsChanged)`, so `WorkTrackingSystemFieldsChanged` keeps meaning "Additional
   Fields changed" and a dialog opened for a default alone sends nothing. Shapes per variant:
   - update question → `{ startUpdates: <the answer>, additionalFieldsChanged: <fieldsChangedSince> }`;
   - plain (Cancel / Save) → Save returns `{ startUpdates: true, additionalFieldsChanged: false }`: the same request a
     save without a dialog sends (the server's default), since the plain variant appears only when no Additional Field
     changed and nothing the save changes is to be updated now;
   - no dialog → no choice, as today.
   The condition at `ModifyConnectionSettings.tsx:572-576` widens from "fields changed and the connection is used" to
   "fields changed and the connection is used, or a changed default has followers".

## Alternatives considered

- **Render defaults in `ForConnection` and keep ADR-239 unchanged.** Every stored fingerprint moves at the upgrade and
  every owner is queued on every default change. Rejected: AC-4.4 and D13.
- **Compare each owner's stored fingerprint with its current one after the save.** Also queues owners that owed a full
  fetch for an unrelated reason (ADR-239's own rejection still applies).
- **A save notice in the PUT response, shown on the connection page** (pass 1). Reverses ADR-239 §3 and keeps the admin
  on the page; withdrawn by the maintainer in favour of the dialog and the log.
- **A dialog on every save that changed a default, even with no followers.** Asks a question whose answer changes
  nothing; the maintainer chose no dialog when nobody follows. The log entry still records the change.
- **Names in the dialog.** Long lists in a confirmation dialog; the maintainer kept names to the log.
- **A second event for default changes.** Two events reaching the same handler for the same reason; the existing one
  already carries the owner ids.
- **Keep the comparison, the refusal and the log in the controller** (pass 1). Business rules in a driving adapter that
  is already at the S107 limit; untestable without HTTP.
- **A service that also takes the DTO and does the mapping.** `Services` would depend on `API.DTO`, the wrong
  direction for ports and adapters.
- **A new row version or a CAS update** (`ExecuteUpdateAsync` and the affected-row count). Not needed: the connection
  already has an EF concurrency token that the PUT and the page use.
- **Make the concurrency token required.** Breaks clients that save without one today; out of scope here.

## Consequences

**Positive**
- Exactly the owners whose fetched data a save changes are updated (AC-5.1), with one code path for field and default
  edits.
- The PUT response, the clients and the page's navigation stay as they are; the request gains one optional nested
  `defaults` member, and a request without it keeps every stored default.
- A record of who followed each change exists for every default-changing save, from the browser or a client.

**Negative / accepted**
- Every connection save loads its owners before the merge. It already loaded them after the save; the extra cost is
  rendering a short string and a SHA-256 per owner. Slice 06 measures it on the dev instance's largest connection.
- Follower names are read in the log, not on the page; an admin who wants them goes to Settings → System Info → Logs.
- The dialog's counts can be stale if an owner changed its source after the page loaded; the log entry is correct.
- A token-less save is checked only against the row its own request read, and does not change the token (today's
  behaviour, described in decision 3).
- An owner saved concurrently with the connection can be queued once more than needed or missed until its next
  scheduled update; a Team save racing the in-use query by milliseconds can still leave a dangling id (decision 2).
- One more service and interface to register in DI.

## Enforcement

- Property-based test on the pure movers rule: a default change never moves `For` of an `Own` owner and always moves
  `For` of a follower of a fetch-time setting; a field-definition edit moves every owner. Unit table on the pure
  refusal rule: a removed field named by a default, by an `Own` owner, by both, by neither.
- Acceptance (WebApplicationFactory): queued set equals the movers, for a default change, a field edit, an Estimation
  default change and `startUpdates=false`; the response body is byte-identical to today's shape.
- Acceptance, stale save: two admins load the connection; the first saves a new default; the second saves from its
  stale form with its old token → 409 `concurrency-conflict`, the first save's default stands, nothing is queued for
  the second and no log entry is written for it.
- Acceptance, in-use race: the connection save is opened; a Team PUT then sets its own Parent Override to field X and
  commits; the connection save, removing X, commits → 409 `additional-field-in-use` naming that Team, X still exists.
  (Driven deterministically by a test seam between `Open` and `Commit`, not by timing.)
- Acceptance, owner PUT: an own value naming no Additional Field of the owner's connection → 400.
- Acceptance, connection PUT: a request without `defaults` keeps every stored default; one with
  `defaults.parentOverrideForTeams: null` clears that default and writes the log entry for it.
- Logger test (a capturing `ILogger` on the service): exactly one Information entry per default-changing save, naming
  exactly the owners in `ConnectionDefault` at commit for each changed default, saying "none" when there are none, and
  none for a save that changed no default or was refused.
- ArchUnitNET: `WorkTrackingSystemConnectionController` has no dependency on the **types**
  `Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventDispatcher` and
  `Lighthouse.Backend.Services.Implementation.WorkItems.FetchFingerprint` (matched by type, not by file); the movers and
  refusal rules (in `Models`) depend on no type outside `Models` and the BCL.
- Vitest + RTL on `ModifyConnectionSettings`: no dialog when nobody follows; plain for Estimation-only; the update
  question for a fetch-time default with followers or an Additional Field change; the title's per-grain counts; no
  owner name rendered; the choice's `additionalFieldsChanged` matches `fieldsChangedSince`; plain Save returns
  `{ startUpdates: true, additionalFieldsChanged: false }`. On `EditConnection`: `WorkTrackingSystemFieldsChanged` is
  sent when `additionalFieldsChanged` is true and not otherwise.

## Cross-reference

- [ADR-239](./adr-239-a-connection-save-that-moves-the-fetch-fingerprint-starts-the-affected-updates.md) (§1 and §8
  narrowed; §3 stands).
- [ADR-241](./adr-241-an-inherited-setting-is-its-own-value-plus-a-source-and-the-owner-answers-with-the-effective-value.md).
- [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md).
