# Acceptance SSOT — epic-5306-productization-platform, slice-05 wildcard-routing (#5202)
# Driving port: the tenant-lpw GitOps record (gitops/tenants/lpw/) + the platform routing layer
# (wildcard DNS *.lighthouse.letpeople.work → ingress, cert-manager auto-TLS), reconciled by ArgoCD
# onto the shipped chart. Carry-over folded in: the chart gains an auto-reload binding so a
# ConfigMap or ESO-rotated Secret reaches the running pod without a manual `rollout restart`.
# Executable via:
#   @in-memory      — helm template of the tenant-lpw values (render assertions for host-mandatory
#                     routing + the config-checksum + the managed-secret reload binding, CI-runnable
#                     once chart 0.1.4 ships).
#   @requires_external — live wildcard DNS + ingress + cert-manager + reloader against the real cluster.
# RED until chart 0.1.4 adds the host-mandatory guard + config-checksum + managed-secret reload binding
# (DELIVER slice-05, public chart) and the platform repo grows the wildcard DNS record + base-domain-keyed
# ApplicationSet host derivation (DELIVER slice-05, private). ADR-092 (provisioning data-flow: every name
# from the tenant id; chart/route/cert as sync-waved overlays). IaC/chart feature → no .cs/.ts scaffolds;
# the @in-memory band runs under helm-unittest, the @requires_external band against the live cluster.
#
# CRUX of the carry-over (finding #3): a render-time `checksum/config` annotation catches chart-rendered
# ConfigMap changes, but ESO materialises secrets OUT-OF-BAND of Helm — the render never sees the rotated
# value, so a checksum alone cannot restart the pod on rotation. Hence two render assertions (checksum for
# the ConfigMap path; a reload-on-secrets binding naming the DB + login Secrets for the ESO path) and the
# live rotation scenario that proves it end-to-end. This tightens slice-04's rotation scenario, which
# currently passes only after a hand `rollout restart`.

@feature:epic-5306-productization-platform
Feature: Any new subdomain serves the right tenant with a trusted cert and zero manual DNS or cert steps
  As the LPW SaaS operator
  I want tenant routing generalised onto a wildcard DNS record with automatic TLS, and the running
  tenant to pick up configuration and secret changes on its own
  So that every future tenant inherits a working HTTPS URL for free and rotations reach the app
  without a manual restart

  # --- CI-runnable: render-layer (chart 0.1.4) — host-mandatory routing + auto-reload binding ---

  @error @US-05 @in-memory @env:tenant-lpw-values
  Scenario: Routing a tenant without a host fails fast
    Given the tenant-lpw values enable ingress but name no host
    When the tenant release is rendered
    Then rendering refuses with a clear message naming the missing host
    And no host-less route is produced

  @US-05 @in-memory @env:tenant-lpw-values
  Scenario: A tenant's host routes only to that tenant's namespace
    Given the tenant-lpw values set the host "lpw.lighthouse.letpeople.work"
    When the tenant release is rendered
    Then the Ingress binds that host to the tenant-lpw namespace and no other
    And the host carries automatic-TLS issuance for a trusted certificate

  @US-05 @in-memory @env:tenant-lpw-values
  Scenario: A configuration change re-stamps the workload so the running app reloads it
    Given the tenant release rendered, then rendered again with one configuration value changed
    When the two workload pod templates are compared
    Then their configuration fingerprints differ
    And an otherwise-identical render leaves the fingerprint unchanged

  @US-05 @in-memory @env:tenant-lpw-values
  Scenario: The tenant workload is wired to reload on its managed secrets
    Given the tenant-lpw values source the database and login credentials from a managed store
    When the tenant release is rendered
    Then the workload is wired to reload automatically when those managed secrets change
    And it names the database and login secrets as the ones to watch

  @US-05 @in-memory @env:standalone-defaults
  Scenario: Standalone defaults carry no routing or auto-reload wiring
    Given the shipped chart's default values with no ingress host and no managed store
    When the chart is rendered
    Then no auto-reload binding is added to the workload
    And the standalone single-container shape is byte-unchanged

  # --- requires the live cluster + wildcard DNS + cert-manager + reloader ---

  @US-05 @real-io @requires_external @env:demo-subdomain
  Scenario: A never-before-configured subdomain serves over trusted HTTPS
    Given the wildcard record "*.lighthouse.letpeople.work" points at the ingress
    When a throwaway host "demo.lighthouse.letpeople.work" is pointed back at a tenant
    Then it resolves and serves over HTTPS with a trusted certificate
    And no per-host DNS record or certificate request was created by hand

  @US-05 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero's route is migrated onto the wildcard mechanism with no change to its URL
    Given Tenant Zero served at "lpw.lighthouse.letpeople.work" on its slice-03 single-host route
    When its route is moved onto the generalised wildcard mechanism
    Then the same URL still serves Tenant Zero over a trusted certificate
    And no tenant chart value changed to achieve the migration

  @US-05 @real-io @requires_external @env:tenant-zero
  Scenario: A new tenant's host and namespace are derived from its identifier alone
    Given a tenant identifier with no hand-written DNS or certificate
    When the tenant is provisioned through the GitOps record
    Then its host and namespace are both derived from that one identifier
    And it serves over the wildcard mechanism with no per-host routing step

  @US-05 @real-io @requires_external @env:tenant-zero
  Scenario: Rotating a store-sourced credential reaches the running app with no manual restart
    Given Tenant Zero is running on a store-sourced credential
    When the operator rotates that credential in the managed store
    Then the running app picks up the new credential on its own
    And no manual rollout restart and no commit to git were required

# Wildcard DNS + the base-domain-keyed host derivation live in the PRIVATE platform repo (the
# ApplicationSet generator builds host = {id}.lighthouse.letpeople.work and namespace = tenant-{id}
# from the one CC-6 identifier); the chart's public delta for this slice is the host-mandatory guard
# plus the config-checksum + managed-secret reload binding. The tenant RECORD that drives an arbitrary
# host is slice-07 (automated provisioning); cross-tenant NetworkPolicy hardening is out of this slice.
# A wildcard certificate vs per-host issuance is a cert-manager posture choice exercised live, not a
# chart-render concern.
