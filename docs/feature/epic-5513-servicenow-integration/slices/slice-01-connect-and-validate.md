# Slice 01 — Connect and validate a ServiceNow instance (walking skeleton)

**Goal**: A config admin creates a ServiceNow connection in Lighthouse, clicks Validate, and gets a
truthful verdict — green, or a failure that names whether it was the host, the credential, or the rights.

**Stories**: US-01 (value).

## IN scope
- `WorkTrackingSystems.ServiceNow` enum member + `AuthenticationMethodKeys` entry.
- One `IWorkTrackingAuthStrategy` implementation — **basic auth** (D3, confirmed working on the target on-prem instance). Likely close in shape to `JiraCloudBasicAuthStrategy`, which is also username+secret over Basic.
- `ServiceNowWorkTrackingConnector` skeleton implementing `IWorkTrackingConnector`: `ValidateConnection` real; the remaining 7 methods returning declared unsupported/empty results (no silent no-ops).
- `AuthenticationMethodSchema` entry so the connection form renders instance URL + credential fields from schema.
- Frontend `DataRetrievalSchemaDefaults` entry for `ServiceNow`.
- Three distinguishable validation failures: unreachable host / bad credential / insufficient rights.

## OUT of scope
- Any read of work items (slice 02). Team or portfolio settings (02/03). Wizards — the schema-driven form is enough for v1; a `DataRetrievalWizardRegistry` entry is a later nicety, not a slice-01 requirement.
- Write-back (D8, permanently out).

## Learning hypothesis
**Disproves** "a ServiceNow customer can connect Lighthouse with rights their platform team will
actually grant" **if** the only credential that validates requires an elevated role, or if the API
cannot distinguish an authentication failure from an authorisation failure — in which case US-01 AC4
collapses and the adoption story is worse than the epic assumes.
**Confirms** the enum→auth-strategy→connector→schema path still works for a fifth system, and that
the FE genuinely needs no bespoke screen.

Note the risk has *moved*, not vanished: D3 settled the protocol question with real evidence, so what
this slice actually tests is **rights**, not auth.

## Acceptance criteria
See US-01 AC1–AC5 in `feature-delta.md`.

## Dependencies
- **SPIKE Q1 (401-vs-403 distinguishability) and Q8 (minimum roles)** — hard blockers. Q2 (ITSM table) is needed only for the rights probe in AC4. The auth *choice* is no longer a dependency: D3 closed it.
- A reachable cloud developer instance.

## Effort / reference class
≤1 day *after the SPIKE*. Reference class: the Linear connector's connection+auth surface —
`LinearApiKeyAuthStrategy` + `LinearWorkTrackingOptionNames` + `ValidateConnection` is a small,
well-bounded addition, and the FE side was configuration only.

## Pre-slice SPIKE
**Mandatory but narrowed.** `spike-questions.md` Q1 (mechanics only — the protocol is settled) and Q8.
Promote the SPIKE's working authenticated call into this slice rather than rewriting it.

## Dogfood moment
Same day: create a ServiceNow connection against the dev instance in a local Lighthouse, validate it
green, then deliberately break each of the three failure modes and confirm three different messages.
