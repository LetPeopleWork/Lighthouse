# Bug 6686 — Jira OAuth: write-back cannot write with read-only scopes

Route: `/nw-bugfix`, nwdes, no lane, in the worktree checkout. Size S.

## Cause

`JiraOAuthProvider.ScopesFor` ignores what the connection needs and always asks Atlassian for the same read
scopes, so no Jira OAuth sign-in ever holds `write:jira-work`. Story 6672 built the rights machinery
(`ScopesFor(WorkItemRights)`, `OAuthCredential.GrantedRights`, `NeedsWriteConsent`, the write-back guard,
the Rights line and the Sign in again warning) but wired it for Azure DevOps only.

## Maintainer decisions (2026-11-24)

- **Fix shape: mirror Azure DevOps.** Ask for `write:jira-work` only when write-back mappings are saved on
  the connection. Record what Atlassian granted (its token answer carries `scope`). Show the Rights line and
  the Sign in again warning for Jira OAuth too.
- **Old Jira sign-ins are read-only.** A Jira OAuth credential stored before rights were recorded
  (`GrantedRights` null) counts as Read: it never asked for write. Azure DevOps keeps null = ReadAndWrite.
- **UI copy: the warning names the tracker.** Sketch agreed:

  ```
  ┌ ⚠ Write-back needs more rights ───────────────┐
  │ Lighthouse can only read from Jira with this  │
  │ sign-in. Sign in again to allow Lighthouse to │
  │ write the mapped fields.     [ Sign in again ]│
  └───────────────────────────────────────────────┘
  (Azure DevOps connection: "...read from Azure DevOps...")
  Rights: Read          <- existing line, now also for Jira
  ```

  Reuses `WriteConsentAlert` / `SignInAlert` and the existing Rights line in `ConnectionAuthSection`.
  No new loading or failure look: the warning derives from the connection list already loaded.
- **Rigor profile: `standard`** for both slices (Sonnet for authors and crafter, Haiku for reviewers and
  the examiner).
- **AFK:** wrap-up refactor + review per slice, mutation once at the end; push at the end, rebase if
  `origin/main` moved.

## Slices

| Slice | Delivers |
|---|---|
| S1 | Backend: Jira sign-in asks for write rights only with write-back, records the granted rights, old Jira sign-ins count as read-only, write-back and the connection list follow them |
| S2 | Frontend: the write-consent warning names the connection's tracker |

## Context map

Tracker Integration context only (OAuth adapter, credential, write-back guard). No new term, concept,
context, edge or domain event; ADR-270 and `ARCHITECTURE.md` §4 unchanged.

## DEVOPS

No platform risk. Usage data: N/A, because this restores an existing capability (write-back) on one
sign-in method; the existing write-back usage covers it and no new user action is added.

## Operator impact (for the docs at finalize)

An operator who sets up write-back on a Jira OAuth connection must add `write:jira-work` to the Atlassian
OAuth app; otherwise Atlassian refuses the sign-in for an unconfigured scope. Read-only connections need no
change. `docs/concepts/worktrackingsystems/jira.md` gets this at finalize.
