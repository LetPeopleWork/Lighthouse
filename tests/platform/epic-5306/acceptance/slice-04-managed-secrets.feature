# Acceptance SSOT — epic-5306-productization-platform, slice-04 managed-secrets (#5203)
# Driving port: the tenant-lpw GitOps record (gitops/tenants/lpw/) + the ESO ExternalSecret
# reference, reconciled by ArgoCD onto the chart, sourcing secret material from OpenBao.
# Executable via:
#   @in-memory      — helm template of the tenant-lpw values (render assertions for the new
#                     oidc.existingSecret escape hatch, CI-runnable once chart 0.1.3 ships).
#   @requires_external — live ESO + OpenBao against the real cluster (materialise, rotate, log in).
# RED until chart 0.1.3 adds oidc.existingSecret (DELIVER slice-04, public chart) and the platform
# repo grows the ESO/OpenBao components + the tenant-lpw ExternalSecrets (DELIVER slice-04, private).
# ADR-087 / CC-3 (External Secrets Operator + self-hosted OpenBao; only refs in git; rotate = update
# store). Mirrors slice-03's postgresql.auth.existingSecret pattern for the OIDC client secret.

@feature:epic-5306-productization-platform
Feature: Tenant Zero's secrets are sourced from a managed store, never committed to git
  As the LPW SaaS operator
  I want Tenant Zero's database and login credentials materialised from a managed secret store
  So that all platform state lives in git without any plaintext secret, and rotation is structural

  # --- CI-runnable: render-layer, the new OIDC store-sourcing escape hatch (chart 0.1.3) ---

  @error @US-04 @in-memory @env:tenant-lpw-values
  Scenario: Enabling login without a credential source still fails fast
    Given the tenant-lpw values enable login but name no credential and no managed source
    When the tenant release is rendered
    Then rendering refuses with a clear message naming the missing login credential
    And no half-configured tenant manifest is produced

  @US-04 @in-memory @env:tenant-lpw-values
  Scenario: A managed login credential lets the tenant render without any secret in its values
    Given the tenant-lpw values enable login and point at a managed login credential
    And the login credential value is omitted from the values
    When the tenant release is rendered
    Then rendering succeeds
    And the tenant owns no login-credential of its own in the rendered output

  @US-04 @in-memory @env:tenant-lpw-values
  Scenario: The running app reads its login credential from the managed source
    Given the tenant-lpw values point at a managed login credential
    When the tenant release is rendered
    Then the app is wired to read the login credential from that managed source

  @US-04 @in-memory @env:tenant-lpw-values
  Scenario: With database and login both managed, the tenant carries no secret material
    Given the tenant-lpw values point at a managed database credential and a managed login credential
    When the tenant release is rendered
    Then the tenant produces no secret of its own
    And every credential the app consumes resolves to a managed source

  # --- requires the live cluster + External Secrets Operator + OpenBao ---

  @US-04 @real-io @requires_external @env:tenant-zero
  Scenario: The GitOps repositories hold no plaintext secret
    Given the platform and tenant records are committed to git
    When the operator searches both repositories for secret values
    Then no database password, connection string, or login credential is found in git
    And only references to the managed store are present

  @US-04 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero's database secret is materialised from the store
    Given the secret store holds Tenant Zero's database credentials
    When the operator commits the secret reference for Tenant Zero
    Then Tenant Zero's database secret appears in its namespace owned by the store
    And it replaces the hand-made secret with no change to the tenant's chart values

  @US-04 @real-io @requires_external @env:tenant-zero
  Scenario: Rotating a credential in the store reaches the tenant without touching git
    Given Tenant Zero is running on a store-sourced credential
    When the operator rotates that credential in the store
    Then the tenant picks up the new credential
    And no commit to the GitOps repository was required

  @US-04 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero logs in with a store-sourced login credential
    Given login is enabled for Tenant Zero with its credential sourced from the store
    When a user signs in at the trusted public address
    Then the login round-trip completes against the identity provider
    And the login credential never existed as a value in git

# The database secret was hand-made in slice-03; this slice replaces it with a store-sourced one and
# adds the store-sourced login credential. Per-tenant secret templating for arbitrary tenants folds
# into slice-07 (provisioning); OpenBao HA + auto-unseal is a production posture (O-5), out of this
# walking-skeleton slice which runs OpenBao single-node with operator-held unseal keys.
