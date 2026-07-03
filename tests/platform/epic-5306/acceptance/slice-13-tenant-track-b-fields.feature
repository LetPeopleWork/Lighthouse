# Acceptance SSOT — epic-5306-productization-platform, slice-13 tenant-record Track B fields (#5387)
# Driving port: the declarative tenant record `gitops/tenants/<id>/tenant.yaml` (same one-record port as
# slice-07) PLUS a commented onboarding template. Track B (US-11 / re-discuss 2026-07-02) formalises the
# per-customer DECISIONS on the record: opt a tenant into the optional MCP server (`mcpEnabled`), record a
# forward-looking `placement`/`provider` (for multi-provider once #5320 lands), keep `backupEnabled` a
# field, and make authentication + a valid licence MANDATORY for every tenant (no auth-off path — RD-2).
# The #5199 chart already ships the MCP workload (ADR-085) and its OAuth inbound-auth model (ADR-079);
# this slice WIRES the record's decision to `mcp.enabled` and the OAuth issuer/audience, adds a copy-me
# template, and extends the PR-time validator so an unauthenticated or malformed record cannot merge.
# Executable via:
#   @in-memory      — helm render of the shipped chart from a record's generated values (MCP workload on/
#                     off, OAuth wiring) + the dependency-free `validate-tenants.sh` lint (auth-mandatory,
#                     mcpEnabled shape, template-is-not-a-tenant). CI-runnable, no cluster.
#   @requires_external — live ArgoCD + the real cluster: a committed `mcpEnabled: true` record serves the
#                     MCP endpoint over the tenant's trusted-HTTPS subdomain; flipping it off removes the
#                     MCP surface with the app still serving.
# RED until the PRIVATE platform repo (LetPeopleWork/lighthouse-platform) threads `mcpEnabled` →
# `mcp.enabled` + `mcp.auth.mode=oauth` + `oidc.audience` through the `tenants` ApplicationSet (hasKey-
# guarded, missingkey=error-safe, like `oidcEnabled`), records a no-effect `placement`/`provider`, adds a
# commented copy-me onboarding template that the generator glob + the validator BOTH ignore, and
# `validate-tenants.sh` grows the auth-mandatory + mcpEnabled-shape guards. IaC/GitOps feature → no
# .cs/.ts scaffolds.
#
# CRUX (what Track B adds over slice-07): (1) slice-07 fanned id/subdomain/plan/oidc*/runtime — here the
# record also carries the MCP + placement DECISIONS a human makes when onboarding a customer. (2) the
# chart's MCP workload existed but no record could turn it on — here `mcpEnabled` is the single switch,
# and enabling it on an authenticated tenant selects OAuth and supplies BOTH values the mcp-http server
# demands (issuer + audience) or the render fails loud. (3) slice-07 let ANY record merge — here the
# validator makes auth non-optional, so "auth configured but not really" (Tenant Zero's AuthMode.Blocked
# gap) cannot recur silently.

@feature:epic-5306-productization-platform
Feature: The tenant record carries every onboarding decision, and only a complete, authenticated record provisions
  As the LPW SaaS operator
  I want each customer choice — MCP server, placement, authentication, licence — expressed on one reviewable record
  So that onboarding is a template-driven, one-commit decision with no tribal knowledge and no half-configured tenants

  # --- CI-runnable: render-layer (the shipped chart from a record's generated values) ---

  @US-11 @in-memory @env:tenant-record
  Scenario: A record that opts into the MCP server provisions the MCP workload alongside the app
    Given a tenant record that enables the MCP server
    When the tenant is rendered
    Then the optional MCP server workload is provisioned and routed alongside the application
    And its metadata endpoint is reachable at the tenant's own host

  @US-11 @in-memory @env:tenant-record
  Scenario: A record that does not opt in gets no MCP workload, and the missing choice is not an error
    Given a tenant record that omits the MCP server decision
    When the tenant is rendered
    Then no MCP server workload is provisioned for that tenant
    And rendering does not fail on the absent decision

  @US-11 @in-memory @env:tenant-record
  Scenario: Enabling the MCP server on an authenticated tenant selects OAuth and supplies both values it needs
    Given an authenticated tenant record that enables the MCP server
    When the tenant is rendered
    Then the MCP server is configured for OAuth inbound authentication
    And it is given both the identity issuer and the audience it requires to start

  @US-11 @in-memory @env:tenant-record
  Scenario: Placement is recorded for the future but does not change where a tenant deploys today
    Given a tenant record that names a placement or provider
    When the tenant is rendered
    Then the placement is carried on the record as forward-looking metadata
    And it does not change the cluster the tenant deploys onto while a single provider exists

  @US-11 @in-memory @env:onboarding-template
  Scenario: The onboarding template is a complete, valid record once its decisions are filled in
    Given a copy of the onboarding template with its decisions filled in under a new identifier
    When the record is validated and rendered
    Then it passes validation and renders a complete tenant
    And the template documents every mandatory step, including authentication and licence seeding, as required rather than optional

  # --- CI-runnable: the PR-time validator (auth-mandatory + shape guards) ---

  @error @US-11 @in-memory @env:tenant-records
  Scenario: A record with authentication turned off is rejected before it can merge
    Given a tenant record that does not configure authentication
    When the records are validated
    Then validation fails stating that every tenant must be authenticated
    And the unauthenticated tenant is never provisioned

  @error @US-11 @in-memory @env:tenant-record
  Scenario: Enabling the MCP server with no audience configured is rejected loudly
    Given an authenticated tenant record that enables the MCP server but configures no audience
    When the tenant is validated and rendered
    Then it fails naming the missing audience the OAuth MCP server requires to start
    And the tenant is not provisioned in a state where the MCP server cannot start

  @error @US-11 @in-memory @env:onboarding-template
  Scenario: The onboarding template never provisions a tenant of its own
    Given the onboarding template sitting unfilled in the repository
    When the generator and the validator scan the tenant records
    Then both ignore the template
    And no placeholder tenant is ever fanned out from it

  @error @US-11 @in-memory @env:tenant-records
  Scenario: A record whose MCP decision is not a yes-or-no value is rejected before merge
    Given a tenant record whose MCP-server decision is neither yes nor no
    When the records are validated
    Then validation fails naming the malformed decision
    And the malformed record is never provisioned

  # --- requires the live cluster + ArgoCD ---

  @US-11 @real-io @requires_external @env:tenant-mcp
  Scenario: A committed record with the MCP server enabled serves it, and disabling it removes the surface
    Given a committed authenticated tenant record with the MCP server enabled
    When ArgoCD reconciles it
    Then the MCP endpoint answers over the tenant's trusted-HTTPS subdomain and advertises its OAuth protected-resource metadata
    And flipping the decision off removes the MCP surface while the application keeps serving

# The record schema, generator wiring (`mcpEnabled` → `mcp.enabled` + `mcp.auth.mode=oauth` + the OAuth
# issuer/audience, hasKey-guarded), the no-effect `placement`/`provider` field, the commented copy-me
# onboarding template (placed where the `tenants/*/tenant.yaml` generator glob cannot catch it), and the
# extended `validate-tenants.sh` auth-mandatory + mcpEnabled-shape guards all live in the PRIVATE platform
# repo (LetPeopleWork/lighthouse-platform). The licence seed stays out-of-band (a required onboarding
# STEP per RD-2, not a record field). DELIVER also flips `docs/onboarding-a-customer.md` +
# `docs/tenant-management.md` from "Planned (Track B)" to live. A self-service signup UI, billing/plans
# beyond `plan`, and the actual second-provider stand-up (#5320) are OUT of this slice.
