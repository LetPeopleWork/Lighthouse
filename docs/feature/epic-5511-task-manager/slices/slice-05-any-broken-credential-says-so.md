# Slice 05 — Any broken credential says so, not just OAuth

**Epic** #5511 Task Manager · **ADO** User Story **#5019** · **Story** US-05 ·
**Job** `job-config-admin-know-any-credential-is-failing-not-just-oauth`

## Goal

Every connection reports a health state regardless of how it authenticates, and the header icon stops
being OAuth-only.

## IN scope

- A per-connection health state — `Healthy` / `AuthenticationFailed` / `Unreachable` / `Unknown` —
  derived from the most recent refresh outcome for that connection, plus `OAuthCredential.Status` where
  one exists.
- Connectors surface an authentication failure as something typed, so a 401 can be told from any other
  failure at the point health is derived.
- A **Test connection** action performing one outbound check for one connection, on demand.
- A Connections section in the popover.
- `OAuthHealthIcon` deleted; its badge folded into the activity icon as a worst-of (D2).

## OUT of scope

- A background probe loop (D9).
- PAT expiry *dates*. #5019's description notes ADO PATs carry a hard expiry the user set when
  generating them; Lighthouse has no awareness of it and gains none here. Warning before expiry is a
  separate, later piece of work.
- Editing a credential from the popover — the row routes to the connection's edit page, which is what
  `OAuthHealthIcon` already does today.

## Learning hypothesis

**Disproves that an authentication failure is distinguishable from any other failure.**

Today `TriggerUpdate` catches bare `Exception`; the only typed failures that survive are
`UnreadableSecretException` (encryption, not authentication) and `OAuthCredentialNotValidException`
(S15). A Jira 401 arrives untyped.

If it succeeds: health for every authentication method is derivable from work already happening.
If it fails — the connectors bury their HTTP status too deep to classify without restructuring their
error handling — then non-OAuth health degrades to "the last refresh failed, cause unknown", which is
still better than today's silence but is a weaker promise, and the UI wording must match it rather than
claim more.

## Acceptance criteria

See US-05 in `feature-delta.md` — AC-05.1 through AC-05.8. The two that carry the risk:

- **AC-05.2** — where a connector cannot say, the state reads `Unreachable`, never
  `Authentication failed`. Guessing wrong sends an administrator to reissue a credential that was never
  the problem, which is precisely the harm `BuildUnreadableSecretReason` exists to prevent.
- **AC-05.3** — never seen, never tested reads `Unknown`, not `Healthy`. Claiming health from an absence
  of evidence is how a status icon becomes decorative.

## Dependencies

Slice 02 — the popover is where this lands. Nothing else.

Not dependent on slice 04.

## Effort

One day, assuming the probe on connector error classification lands inside it. If classification turns
out to need restructuring across five connectors, split: ship the derived-from-last-refresh state first,
the typed classification second.

## Reference class

`work-tracking-oauth-authentication` slice 02 (the icon this replaces), `epic-5775-secret-encryption-key-custody`
(the `UnreadableSecretException` path, which is the one existing example of a typed, user-explained
connector failure).

## DESIGN verdict (2026-08-23) — this slice got smaller

[ADR-184](../../../product/architecture/adr-184-connection-health-is-a-recorded-verdict.md).
DISCUSS assumed the classifier was greenfield. It is not.

`ConnectionValidationResult` already carries a **`Code`**, and **`authentication_failed` already
exists** — emitted by `AzureDevOpsWorkTrackingConnector`, `JiraWorkTrackingConnector` and
`ServiceNowValidationVerdict`; ServiceNow also emits `insufficient_permissions`. Linear and CSV emit
neither. `ValidateConnection` is already reachable through `WorkTrackingSystemConnectionsController`.

So health is a **recorded verdict** — a `ConnectionHealthVerdict` row per connection — produced in two
places and only two: once when a refresh for that connection fails, by calling `ValidateConnection` to
classify why, and on demand when the administrator presses **Test connection**. No new connector
surface, no error-handling changes across five connectors, and no background probe loop. The verdict is
stored because it must outlive the process: a refresh can fail at 02:00 and be read at 09:00.

Linear and CSV read `Unreachable` on failure. That is honest, not degraded — and whether they should
gain an `authentication_failed` code is recorded as an open question, not folded into this slice.

The 2-hour probe below is **closed**; its question is answered above. The typed-passive-exception
alternative was weighed and set aside for this Epic — the verdict record is the same shape either way,
so adding it later is not blocked.

**Do not hand-explore the Linear API while probing** — its key is shared with CI, and a local 429 reds
the next backend CI run under a failure signature that does not name the rate limit.
