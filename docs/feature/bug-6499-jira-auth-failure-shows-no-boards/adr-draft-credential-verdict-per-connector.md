# ADR draft: each connector owns one pure verdict class; Jira and Azure DevOps board reads refuse instead of returning empty

Status: Proposed (Bug 6499, DESIGN). Applied in DELIVER as the next free ADR number under
`docs/product/architecture/`, together with the dated "Adoption" notes in ADR-126 and ADR-184.

## Context

ADR-126 decision 1 built a refusal channel (`WorkTrackingReadException`, caught as the abstract type by
`WizardsController`) and left Jira, Azure DevOps and Linear "to adopt later". Nothing tracked the adoption.
Jira and Azure DevOps still turn every failed board read into `200 []`, so a refused credential or an outage
reads as "no boards". The sentence that does name a refused credential exists only in `ValidateConnection`,
and for Jira it classifies every non-2xx as `authentication_failed`, so a 503 reads as a credential fault.
ServiceNow already solved both with pure `ServiceNow...Verdict` classes that `ValidateConnection` and the
board reads share.

## Decision

1. Each of Jira and Azure DevOps gets one **pure static verdict class** next to its connector
   (`JiraConnectionVerdict`, `AzureDevOpsConnectionVerdict`; names suggested, DELIVER may rename them). It maps
   (read kind, HTTP status or exception, sign-in kind) to a `ConnectionValidationResult`. `ValidateConnection`,
   the board list, the board information read and the Jira field list all call it, so the credential sentence
   exists once per connector and the credential classification is decided in one place. Wording stays in the
   connector; nothing is shared across connectors. For Jira a 401 and a 403 are both a credential refusal (N3);
   for Azure DevOps only a 401 is, and a 403 has its own permission sentence on the picker (C10) while Test
   connection keeps today's mapping for it (health `Unreachable`).
2. The **board reads** throw the existing `JiraReadException` / `AzureDevOpsReadException` (public constructor,
   verdict from the class). They contain no catch-all that returns an empty value. On a board read, an exception
   that is neither a non-success status nor "could not be asked" (Jira's existing `JiraCouldNotBeAsked`
   classifier is reused, not duplicated) nor an unreadable body is not caught and surfaces as a 500. This is
   about board reads only: the catch-alls that remain elsewhere stay, because each returns a non-empty,
   non-credential verdict (`validation_failed`) or belongs to another feature: Jira `ValidateConnection`
   (`catch (Exception)`), ADO `ValidateConnection` (bare `catch`), the Team and Portfolio settings validators of
   both connectors, and the write paths `TryWriteFields` / `TryPatch`. A genuine `200` with zero boards stays
   `200 []`. The ADO malformed-board-id guard becomes a typed failure instead of an empty `BoardInformation`.
3. Jira `ValidateConnection` keeps `authentication_failed` for 401 and 403 only; other statuses give
   `unexpected_answer`; an unreachable host **and a timeout** give `connection_failed` (today a timeout falls into
   the catch-all and reads `validation_failed`). A timeout is "no answer", the same fact as an unreachable host.
4. `WizardsController`, `IBoardInformationProvider` and `ConnectionHealthService` do not change.
   `ConnectionHealthService.StateFor` already maps only `authentication_failed` to `AuthenticationFailed`; a Jira
   503 therefore stores `Unreachable`.

## Alternatives considered

- **One shared cross-connector credential factory.** Rejected: the Jira sentence depends on Cloud versus Data
  Center and on the Atlassian gateway rule, the Azure DevOps one on a single PAT; a shared factory would switch
  on connector under a shared name (DRY is knowledge, not code).
- **Static factories on the read exceptions only.** Rejected: `ValidateConnection` wants a verdict, not an
  exception to unwrap.
- **A new shared `CredentialRefusedException`.** Rejected: it carries nothing the verdict does not, and the
  controller already catches the abstract base.
- **An architecture rule banning `catch { return []; }` in board methods.** Rejected by the maintainer
  (Q8); the pure ladders and per-connector failure-path tests are the guard.

## Consequences

- Positive: a refused credential, an outage and an empty account are three different things on the picker and the
  connection page; the sentence is pinned in a table test and is the mutation-testing target; ADR-184's single
  verdict is untouched.
- Positive: ADR-126's open promise gets an owner for Jira and Azure DevOps (Linear stays out of scope and is
  noted as not adopted).
- Negative: `200 []` becomes `400 {verdict}` for a failed read on two more connectors, a wire change on a UI-only
  System Administrator endpoint (the envelope already ships for ServiceNow).
- Negative: the stored Jira health message changes wording (C1 replaces "Authentication failed for Jira.").
- Enforcement: no new architecture rule; `ConnectionHealthSingleWriterArchUnitTest` keeps guarding that board
  reads never write the verdict.
- Negative: the Jira stored health message loses the claim it never could prove. DISCUSS C1 said the token "has
  not been revoked or expired", false for a 403 that means no licence; C1 is reworded to claim nothing about the
  token's life (still one sentence for 401 and 403).

## Adoption notes (recorded here, applied to ADR-126 and ADR-184 in DELIVER)

- **Divergence from ADR-126's ServiceNow 403 rung, by maintainer decision N3 (AFK-5).** ADR-126 maps a
  ServiceNow 403 to `insufficient_permissions`, not to the credential sentence. For Jira the maintainer decided
  that a 401 and a 403 alike get the credential sentence on the board list, board information and field list
  (as Jira's connection check already did). The two connectors therefore treat 403 differently on purpose.
  Flagged for the maintainer to re-confirm N3 with this precedent in view; until then N3 stands.
- **Azure DevOps follows the ServiceNow precedent, not N3.** An ADO 403 is not a credential verdict anywhere
  (AFK-2 reversed): Test connection keeps today's mapping (health `Unreachable`), the picker shows a permission
  sentence (C10), and C6 is for a 401 only. ADR-184's rule "where the connector cannot say it was
  authentication, never say it was" governs.
