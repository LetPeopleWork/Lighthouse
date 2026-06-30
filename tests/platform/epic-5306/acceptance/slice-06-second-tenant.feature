# Acceptance SSOT — epic-5306-productization-platform, slice-06 second-tenant-by-hand (#5207)
# Driving port: the tenant GitOps records under gitops/tenants/<id>/tenant.yaml +
# gitops/tenant-secrets/<id>/secrets.yaml, reconciled by ArgoCD onto the SHIPPED chart 0.1.4. This
# slice de-risks the CC-1 tenancy model BEFORE the slice-07 generator: a second tenant "acme" is stood
# up by COPYING and parameterising Tenant Zero's records by hand — no chart change, no automation. If
# two tenants coexist with no cross-tenant bleed, the model holds and the lpw↔acme record diff is
# exactly the parameter set slice-07 must template.
# Executable via:
#   @in-memory      — a static compare of the two committed tenant records (the lpw↔acme diff is
#                     exactly the id-derived parameters). No cluster, no helm render — the chart is
#                     unchanged this slice, so there is nothing new to render.
#   @requires_external — live ArgoCD + the real cluster (second tenant serving over trusted HTTPS;
#                     namespace + secret + DB-credential isolation against Tenant Zero next door).
# RED until the PRIVATE platform repo grows gitops/tenants/acme/ + gitops/tenant-secrets/acme/ (DELIVER
# slice-06) and OpenBao is seeded with secret/tenants/acme/* + a tenant-acme k8s-auth role, after which
# ArgoCD's `tenants` ApplicationSet fans out a tenant-acme Application and `tenant-secrets` materialises
# acme's ESO Secret. ADR-086 (one generator, no production special-casing — Tenant Zero and acme come
# off the same template) + CC-1 namespace-per-tenant isolation. IaC/GitOps feature → no .cs/.ts
# scaffolds; the @in-memory band is a committed-file diff, the @requires_external band runs against the
# live cluster.
#
# CRUX (the learning de-risked here): isolation is asserted on TWO boundaries the model promises — a
# workload in acme's namespace cannot READ Tenant Zero's Secrets (default-RBAC namespace boundary), and
# acme's database credential is a DISTINCT value sourced from acme's own store path (no shared secret).
# NetworkPolicy hardening (defence-in-depth at the packet layer) is explicitly OUT of this slice; the
# tenancy decision is proven at the credential + RBAC boundary, which is what slice-07 must preserve.

@feature:epic-5306-productization-platform
Feature: A second tenant provisioned by hand coexists with Tenant Zero, fully isolated
  As the LPW SaaS operator
  I want to stand up a second tenant by copying and parameterising Tenant Zero's GitOps records
  So that I prove the tenancy model isolates two tenants before I invest in automating it, and I learn
  exactly which parameters the generator must template

  # --- CI-runnable: structural — the lpw↔acme record diff is the slice-07 parameter set ---

  @US-06 @in-memory @env:tenant-records
  Scenario: A second tenant's record differs from Tenant Zero only by its identifier-derived parameters
    Given the committed tenant records for Tenant Zero and the second tenant
    When the two records are compared field by field
    Then they differ only in the tenant identifier and the values derived from it
    And every other structural field is identical between the two records
    And that difference is exactly the parameter set the provisioning generator must template

  # --- requires the live cluster + ArgoCD + cert-manager + ESO/OpenBao ---

  @US-06 @real-io @requires_external @env:tenant-acme
  Scenario: The hand-provisioned second tenant serves over trusted HTTPS at its own subdomain
    Given the second tenant's records are committed and reconciled by ArgoCD
    When its subdomain is requested over HTTPS
    Then it serves over HTTPS with a trusted certificate
    And it runs in its own namespace with its own database, separate from Tenant Zero

  @US-06 @real-io @requires_external @isolation @env:tenant-acme
  Scenario: The second tenant cannot read Tenant Zero's secrets
    Given the second tenant and Tenant Zero each run in their own namespace
    When a workload identity in the second tenant's namespace attempts to read Tenant Zero's secrets
    Then the read is denied
    And the second tenant can read only the secrets in its own namespace

  @US-06 @real-io @requires_external @isolation @env:tenant-acme
  Scenario: The second tenant runs on its own database credential, not Tenant Zero's
    Given each tenant's database credential is materialised from its own secret-store path
    When the two tenants' database credentials are compared
    Then they are distinct values
    And the second tenant's application is healthy against its own in-namespace database

  @US-06 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is unaffected by the second tenant's provisioning
    Given Tenant Zero is serving before the second tenant is provisioned
    When the second tenant is stood up next to it
    Then Tenant Zero's URL still serves over its trusted certificate with no change
    And no Tenant Zero record or secret was touched to provision the second tenant

# The second tenant's records live in the PRIVATE platform repo (gitops/tenants/acme/tenant.yaml +
# gitops/tenant-secrets/acme/secrets.yaml), copied from Tenant Zero's and parameterised by the one CC-6
# id; OpenBao gains secret/tenants/acme/* + a tenant-acme k8s-auth role out-of-band (never in git, CC-3).
# acme is a demo tenant: OIDC login is OFF (no Auth0 app needed — isolation, not auth, is the learning),
# so its record omits the oidc* fields and its ESO bundle carries only the DB ExternalSecret. The chart
# is UNCHANGED this slice — the same 0.1.4 that serves Tenant Zero serves acme, which is the whole point.
# Automating this copy (one record → whole stack) is slice-07; NetworkPolicy packet-level isolation and
# per-tenant backup scope are later slices.
