# Acceptance SSOT — epic-5306-productization-platform, slice-07 automated-provisioning (#5376)
# Driving port: ONE declarative tenant record `gitops/tenants/<id>/tenant.yaml`. Slice-06 proved the
# tenancy model by hand (a record PLUS a hand-written ESO secrets file PLUS out-of-band OpenBao); this
# slice makes a SINGLE record fan — via ArgoCD, no bespoke controller — into a complete isolated tenant
# (namespace + ResourceQuota + default-deny NetworkPolicy + store-sourced secrets + #5199 chart release +
# wildcard-routed TLS subdomain), and removing the record tears it ALL down with no orphans. The SaaS
# payoff: onboarding a customer is a minutes-long, reviewable, one-action commit (ADR-092 provisioning
# data-flow; ADR-086 one generator, no production special-casing).
# Executable via:
#   @in-memory      — helm-unittest of the in-repo `tenant-runtime` chart (the new overlay this slice
#                     adds: ns/quota/netpol/SecretStore/ExternalSecret rendered from the record) + a
#                     committed-records uniqueness lint. CI-runnable, no cluster.
#   @requires_external — live ArgoCD + the real cluster: provision a throwaway tenant from one record,
#                     prove it serves isolated, then remove the record and prove zero orphans; plus the
#                     dogfood check that Tenant Zero is produced by the same generator.
# RED until the PRIVATE platform repo grows the `tenant-runtime` chart + a `tenants-runtime`
# ApplicationSet (folds slice-06's hand-written secrets file into the record), the `tenants`
# ApplicationSet defaults subdomain→id (missingkey=error-safe `hasKey`) and lets the runtime own the
# namespace (so prune deletes it), and a PR-time uniqueness check lands. Full ADR-092 scope: per-tenant
# NetworkPolicy + ResourceQuota are IN. IaC/GitOps feature → no .cs/.ts scaffolds.
#
# CRUX (what slice-07 must fix over slice-06): (1) slice-06 needed TWO files + out-of-band seeding per
# tenant — here it is ONE record (secrets folded into the generated runtime). (2) slice-06's namespace
# was created by the `CreateNamespace=true` syncOption, which ArgoCD does NOT prune on de-provision —
# here the namespace is a TRACKED manifest in the runtime chart, so record removal prunes it. (3) the
# subdomain→id default was unprovable under helm in slice-05 (`.Values` ≠ ArgoCD param map) — here it is
# a `hasKey`-guarded goTemplate validated against the LIVE ArgoCD generator.

@feature:epic-5306-productization-platform
Feature: One declarative record provisions a complete isolated tenant, and removing it tears it all down
  As the LPW SaaS operator
  I want to onboard or off-board a customer by adding or removing a single reviewable tenant record
  So that provisioning is a minutes-long, isolated, one-action commit with no hand-wiring and no orphans

  # --- CI-runnable: render-layer (the new in-repo tenant-runtime chart) ---

  @US-07 @in-memory @env:tenant-record
  Scenario: One record fans into a complete isolated tenant runtime
    Given a tenant record naming only its identifier and plan
    When the tenant runtime is rendered
    Then it produces a namespace, a resource quota, a default-deny network policy, a secret store, and a database secret
    And every one of them is named and scoped from that single identifier

  @US-07 @in-memory @env:tenant-record
  Scenario: A record that names no subdomain defaults its host to the tenant identifier
    Given a tenant record that omits the subdomain
    When the tenant is rendered
    Then its host and namespace both derive from the identifier
    And rendering does not fail on the missing field

  @US-07 @in-memory @env:tenant-record
  Scenario: The tenant namespace is a tracked resource so off-boarding can prune it
    Given a tenant record is rendered
    When the produced resources are inspected
    Then the namespace is a managed manifest owned by the tenant, not an untracked side effect
    And removing the record would therefore prune the namespace along with everything in it

  @US-07 @in-memory @env:tenant-record
  Scenario: The tenant is isolated by a default-deny network posture
    Given a tenant record is rendered
    When its network policy is inspected
    Then cross-namespace ingress is denied except from the shared ingress controller
    And egress is denied except name resolution, traffic within the tenant, and outbound HTTPS

  @US-07 @in-memory @env:tenant-record
  Scenario: A tenant with login off renders only its database secret
    Given a tenant record with login disabled
    When the tenant runtime is rendered
    Then only the database secret is produced
    And no login secret is rendered for that tenant

  @error @US-07 @in-memory @env:tenant-records
  Scenario: A duplicate identifier or subdomain is rejected before it can merge
    Given the committed tenant records plus a new record reusing an existing identifier or subdomain
    When the records are validated
    Then validation fails naming the collision
    And the duplicate tenant is never provisioned

  # --- requires the live cluster + ArgoCD ---

  @US-07 @real-io @requires_external @env:tenant-riverbank
  Scenario: One committed record yields a reachable, isolated tenant in minutes
    Given a single new tenant record is committed, with no hand-written secret file
    When ArgoCD reconciles it
    Then the tenant serves over trusted HTTPS at its own subdomain within minutes
    And it runs in its own namespace with its own database, quota, and network policy

  @US-07 @real-io @requires_external @env:tenant-riverbank
  Scenario: Removing the record off-boards the tenant with no orphaned resources
    Given a provisioned tenant
    When its record is removed and ArgoCD reconciles
    Then its namespace, database, secret, route, and certificate are all pruned
    And no resource belonging to that tenant is left behind

  @US-07 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is produced by the same generator, not a bespoke special case
    Given the production tenant Tenant Zero
    When its provisioning is inspected
    Then it is produced by the same one-record generator every customer uses
    And it keeps serving over trusted HTTPS throughout, with no hand-written secret file remaining

# The generator, runtime overlay, and uniqueness check live in the PRIVATE platform repo
# (LetPeopleWork/lighthouse-platform): the `tenant-runtime` chart (ns/quota/netpol/SecretStore/
# ExternalSecret), the `tenants-runtime` ApplicationSet, the `tenants` ApplicationSet's subdomain→id
# default + runtime-owned namespace, and the PR-time uniqueness workflow. OpenBao still holds the secret
# VALUES out-of-band (CC-3, never in git) — what slice-07 folds into the record is the ESO wiring, not
# the secret material. A self-service signup UI and billing/plans beyond a sizing parameter are OUT;
# multi-provider substrate parity is slice-12.
