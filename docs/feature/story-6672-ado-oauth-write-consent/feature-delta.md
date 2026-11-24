# Story 6672 — Azure DevOps OAuth asks for write rights only when write-back is set up

## Problem

The Azure DevOps OAuth connection always asks for `vso.work_write` (`AdoOAuthProvider.cs`), also when the
connection is only used to read. A security review sees a read-only connection that holds a token with write
rights. Lighthouse does not store which rights a token was granted: the token response carries `scope`, but it
is only logged.

Jira OAuth already asks for read scopes only, so this Story concerns Azure DevOps only. (Write-back on a Jira
OAuth connection cannot write with those scopes; that is Bug 6686, not part of this Story.)

## Acceptance criteria (from the Story)

- A new ADO OAuth connection without write-back asks only for read rights on work items.
- Setting up write-back on a connection asks the admin to give write consent, and write-back works after that.
- A connection without write consent never tries to write.
- Existing connections keep working.

## Maintainer decisions (DISCUSS, 2026-11-24)

- **Existing connections count as write-consented.** Their tokens were granted `vso.work_write`; nothing
  changes for them and write-back keeps working. The next time an admin signs in again, Lighthouse asks only for
  the rights the connection needs (read, or read and write when it has write-back mappings), and from then on
  stores the rights that were granted.
- **Adding the first write-back mapping to a read-only connection:** the mapping saves. The connection then
  shows a warning with a **Sign in again** button. Until the admin signs in with write consent, Lighthouse
  tries no write; each skipped write-back is logged.
- **Removing the last write-back mapping:** nothing happens at once. The token keeps its write rights until
  the next sign-in, which then asks for read rights only. Lighthouse never writes without a mapping anyway.
- **Rigor profile:** `thorough` for every slice (crafter, acceptance designer, architect on `opus`; reviewers
  and EXAMINE on `sonnet`).

### Approved UI sketch

Reuses the `ReconnectBanner` sign-in popup flow and the write-back editor's `Alert`.

```
Settings → Connections → <ADO OAuth connection>

┌ Authentication ──────────────────────────────────────────┐
│ Azure DevOps OAuth · Connected                            │
│ Rights: Read work items                  ← new, one line  │
│   (or: Read and write work items)                         │
└───────────────────────────────────────────────────────────┘

┌ Write-back mappings ─────────────────────────────────────┐
│ ⚠ Write-back needs more rights. Lighthouse can only read  │  ← only when ≥1 mapping
│   from Azure DevOps with this sign-in. Sign in again to   │     and no write consent
│   allow Lighthouse to write the mapped fields.            │
│                                   [ Sign in again ]       │  ← same popup flow as Reconnect
│                                                           │
│ Target Field   Applies To   Sync Value   …   (unchanged)  │
└───────────────────────────────────────────────────────────┘

Existing connections (consent from before the update): "Rights: Read and write work items".
Sign-in from the wizard for a new connection without mappings: asks for read only.
```

Loading and failure looks: the Rights line and the warning read the connection the settings page already loads;
they add no fetch of their own, so they share that page's loading and failure states. DESIGN confirms this.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | N/A, because it adds no action, page or data: connection settings and write-back mappings are already gated to who may administer connections; the new Rights line and warning sit inside them. |
| Lighthouse-Clients CLI / MCP | To confirm at DESIGN: the connection DTO gains the granted rights. If a client command or MCP tool shows connection authentication, it follows; otherwise N/A. |
| Website marketing surface | N/A, because this is least-privilege hygiene on an existing connection type, not a new capability. |
| Context map | Tracker Integration context only (OAuth credentials, write-back). New term: **write consent** (the connection's token was granted the right to write work items). No new context or edge. Domain events: none required; DESIGN weighs whether a change in a connection's write-back mappings should raise one. UI surface: the connection's settings under System, per ADR-271; no move. |
| Usage-data event | DEVOPS question; recommendation N/A, because a sign-in for write consent is an admin set-up step, not feature use. |
| Improvement backlog | N/A, because this is a Story without a feature Epic; `/continuous-improvement before` applies to new feature Epics. |

## Waves

- DISCUSS: done (this record, 2026-11-24, with the maintainer).
- DESIGN, DEVOPS, DELIVER: next, in AFK mode after Bug 6656.

## DESIGN (2026-11-24, architect, with the maintainer present)

- Design recorded in nwdes (`handoff.json`, `design/write-consent.qnt`). Summary posted in chat.
- **Maintainer decision (09:2x):** the write-consent warning also shows on the **connections list**, next to the
  existing Reconnect banner, with the same component and copy; it still shows above the write-back mappings
  on the connection page.
- Lighthouse-Clients CLI/MCP: N/A, because the clients read only the connection's name, type, sign-in method
  labels and options, and cannot write the new fields.
- Open for the hold: one manual Azure DevOps sign-in to confirm the exact `scope` text Microsoft returns.

## DEVOPS (2026-11-24, AFK)

- **Platform risk:** small. One additive, nullable column on the OAuth credential (expand-only, generated with
  `Create-Migration.ps1` for SQLite and Postgres, no backfill). No new external call, no new configuration,
  no new deployment step. The same Microsoft authorize and token endpoints are used with a smaller scope.
- **Usage-data event:** N/A, because a sign-in for write consent is an admin set-up step, not use of a feature
  whose adoption we want to count; a name-only event would only ever count admins re-signing in.
- **Operational signal instead:** each skipped write-back logs one warning per run naming the connection and
  the fix ("sign in again"), so a support case can see why nothing was written.
- **Manual check owed at the hold:** one real Azure DevOps sign-in to confirm the `scope` text Microsoft
  returns for `vso.work` and `vso.work_write`.

## DELIVER (AFK, 2026-11-24)

- **Slices:** S1 backend, S2 UI. Rigor `thorough` (agreed at DISCUSS).
- **S1** (`1ca8a89e8` oracle, `d94520028` feature): RED 6 failed / 8 passed, GREEN 14/14; slice review accepted.
- **Decision (AFK, revisit if wanted):** write-back is blocked when the sign-in lacks the rights the connection
  *needs* (its mappings), as designed, not on "no write consent" alone. A write-back always comes from the
  connection's own mappings, so a connection without mappings has nothing to write. The stricter rule would also
  hold if mappings were ever not loaded; it costs one design change and one oracle row.
- **Deferred to the final refactor:** the oracle's summary comments cite design-law ids; reword them in plain words.
- **S2 oracle** (`b40573fe9`): RED frontend 3 failed / 13 passed, backend DTO 7 failed / 7 passed; review accepted.
  The declared command for `connectionFormRules.test.ts` was dropped (no such test file; the Rights wording is
  pinned on the connection page).
- **Decision (AFK, revisit if wanted):** "Sign in again" reuses the Reconnect flow's in-progress, popup-blocked,
  cancelled and error handling and messages through the shared sign-in hook; no new copy was sketched for them.
- **Deferred to after S2 GREEN (oracle owner):** tighten three S2 tests (list-failure guard that cannot fail,
  the warning's row on the list, the Rights line's place in the Authentication section).
- **S2** (`e7e920823` prefactor, `850ae5be2` feature, `0ee48fb44` tightened tests): GREEN FE 16/16, BE 14/14; slice
  review accepted.
- **Fix in the final gates (known cause):** on the connection page, a failed reload after Sign in again leaves the
  old rights and warning on screen as if current. A page-level failed-reload test first, then the fix.
- **Open for the maintainer:** a connection needing both a reconnect and write consent shows both banners
  stacked (no test pins the order); the list warning does not name its connection (default: keep as approved,
  like the Reconnect banner).
