# ADR-184: Connection health is a recorded verdict per connection, classified by `ValidateConnection`'s existing code

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 05 / ADO #5019)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Lighthouse warns about a broken credential only when that credential is OAuth.
`OAuthHealthAggregator` reads `IRepository<OAuthCredential>`, groups by connection, and counts anything
whose `Status` is not `Valid`. A connection authenticating with a PAT or a scoped API token has no
`OAuthCredential` row at all, so it is invisible to that aggregator — and
`WorkTrackingSystemConnectionDto.RequiresReconnect` is computed the same OAuth-only way. An Azure
DevOps PAT that expired, a Jira API token that was revoked, a Linear key whose owner was deprovisioned:
all fail silently, Lighthouse keeps making 401-returning calls, and the first signal anyone gets is a
throughput chart that stopped moving on a date nobody can name. This is ADO #5019.

The DISCUSS wave assumed classifying a non-OAuth failure was greenfield, on the grounds that
`UpdateServiceBase.TriggerUpdate` catches bare `Exception` and a Jira 401 arrives untyped. Reading the
connectors during DESIGN corrects that. `ConnectionValidationResult` already carries a **`Code`**
discriminator, and the value **`authentication_failed`** is already emitted by
`AzureDevOpsWorkTrackingConnector`, `JiraWorkTrackingConnector` and `ServiceNowValidationVerdict`;
ServiceNow additionally emits `insufficient_permissions`. `ValidateConnection` is already reachable
through `WorkTrackingSystemConnectionsController`. Linear and CSV emit no auth code.

So the classifier exists. What is missing is only that nothing ever *asks* it outside a manual
validate — a refresh that 401s never calls `ValidateConnection`.

## Decision

**Health is a verdict, recorded per connection, produced by asking `ValidateConnection` why — and the
OAuth-only aggregator is absorbed, not kept alongside.**

- A `ConnectionHealthVerdict` row per connection holds the state, the code, the message and the moment
  it was observed. It is a stored record because a verdict must outlive the process that observed it:
  a refresh can fail at 02:00 and the administrator opens the popover at 09:00.
- The verdict is produced in two places and **only** those two: once when a refresh for that connection
  fails, by calling `ValidateConnection` to classify the failure; and on demand, when an administrator
  presses **Test connection**.
- `OAuthCredential.Status` folds in for connections that have one, so OAuth connections keep exactly
  their present behaviour and wording.
- `OAuthHealthAggregator`, `IOAuthHealthAggregator` and `OAuthHealthController` are **deleted**, and
  `GET /api/oauth/health` is removed rather than deprecated in place — it has one caller, and that
  caller is deleted in the same slice.

**A connection that has never failed and has never been tested reads `Unknown`, not `Healthy`.**
Claiming health from an absence of evidence is how a status icon becomes decorative.

**Where the connector cannot say it was authentication, the state is `Unreachable`, never
`Authentication failed`.** Guessing wrong sends an administrator to reissue a credential that was never
the problem — the exact harm `BuildUnreadableSecretReason` was written to prevent. Linear and CSV
therefore read `Unreachable` on failure until someone needs better, which is honest rather than
degraded.

## Consequences

**Positive.** No new connector surface, and no new error-handling in five connectors. Coverage for
three of five systems arrives from a classifier that already ships and is already tested. The outbound
cost is one extra call **per failed refresh** — only when something has already gone wrong, never on a
schedule. One source answers "is this connection healthy" instead of two.

**Negative.** A connection whose refresh fails pays one extra outbound call to the system that just
failed. Accepted: it is bounded by the failure rate, not by the connection count, and the alternative
is an administrator who cannot tell a dead credential from a dead network.

Coverage is uneven by connector on day one. Recorded as an open question rather than papered over:
whether Linear and CSV should gain an `authentication_failed` code is a separate, small piece of work.

`WorkTrackingSystemConnectionDto.RequiresReconnect` is left as it is, so the connection edit page is
unaffected by this Epic. Two sources for one question is a real smell and it is recorded as an open
question — merging them touches a page this Epic does not otherwise open.

**Enforced by**: an ArchUnit rule that `ConnectionHealthService` is the only writer of
`ConnectionHealthVerdict`.

## Alternatives considered

**A typed authentication exception thrown by connectors on 401/403, classified passively at the
updater.** No extra outbound call at all. Rejected as the primary mechanism: it needs error-handling
changes in five connectors to reach the same coverage that three already have through
`ValidateConnection`, and Linear and CSV would still be uncovered. Worth revisiting if the extra call
per failed refresh ever proves to matter.

**Both — typed passive plus `ValidateConnection` on demand.** Best coverage and clearest behaviour.
Rejected for this Epic on size: it is two mechanisms in one slice and would need splitting into 05a and
05b. The chosen design does not preclude adding the passive path later; the verdict record is the same
either way.

**A background probe loop calling every connection periodically.** Freshest data. Rejected: it adds
recurring outbound calls to systems whose rate limits Lighthouse already shares and already trips, and
it would be a second scheduler in a product whose first one this Epic exists to explain.

**Generalise `OAuthHealthAggregator` in place.** Smaller diff. Rejected: its entire shape is "count
OAuth credential rows", and the connections that need covering have none — leaving a class named for
OAuth as the authority on connections that do not use it.
