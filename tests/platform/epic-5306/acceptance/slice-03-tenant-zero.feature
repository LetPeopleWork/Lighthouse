# Acceptance SSOT — epic-5306-productization-platform, slice-03 tenant-zero-reachable (#5204 + thin #5202)
# Driving port: the tenant-lpw GitOps record (gitops/tenants/lpw/) reconciled by ArgoCD onto the chart.
# Executable via:
#   @in-memory      — helm template of the tenant-lpw values (fail-fast / render assertions, CI-runnable now).
#   @requires_external — live ArgoCD sync + ingress + cert against the real cluster.
# RED until gitops/tenants/lpw/ exists (DELIVER slice-03). The end-to-end happy path is the single
# @walking_skeleton scenario in walking-skeleton.feature; this file carries the variants + error paths.
# ADR-082 (fail-fast on missing required values via values.schema.json + {{ required }}).

@feature:epic-5306-productization-platform
Feature: LetPeopleWork is brought up as the first, permanent tenant (Tenant Zero)
  As the LPW SaaS operator
  I want LPW's real production Lighthouse installed through GitOps as the first tenant
  So that the platform is proven on our own production data before any customer lands

  # --- CI-runnable: render-layer fail-fast (reuses the shipped chart's schema guard) ---

  @error @US-03 @in-memory @env:tenant-lpw-values
  Scenario: First run fails fast on a missing required value
    Given the tenant-lpw values omit the database password
    When the tenant release is rendered
    Then rendering refuses with a clear message naming the missing password key
    And no half-provisioned tenant manifest is produced

  @US-03 @in-memory @env:tenant-lpw-values
  Scenario: The tenant-lpw record routes a single explicit host to the tenant namespace
    Given the tenant-lpw record with ingress host "lpw.lighthouse.letpeople.work"
    When the tenant release is rendered
    Then the Ingress binds that single host to the tenant-lpw namespace
    And the rendered shape is the shipped chart's default embedded single-container shape

  # --- requires the live cluster + ArgoCD + ingress + cert ---

  @US-03 @real-io @requires_external @env:tenant-zero
  Scenario: LPW production runs isolated in its own namespace on real data
    Given the substrate and ArgoCD are up
    When the operator provisions tenant-lpw via the GitOps repo with the shipped chart
    Then LPW's Lighthouse runs in an isolated "tenant-lpw" namespace with all pods Running/Ready
    And it serves LPW's real teams and portfolios, not demo seed data

  @US-03 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is established as the permanent canary
    Given Tenant Zero is live on the platform
    When any later platform capability is delivered
    Then it is proven against Tenant Zero before any customer tenant

# Tenant Zero's secret is hand-made (kubectl-applied) in this slice; the managed External Secret
# replacement is slice-04. Wildcard routing for arbitrary tenants is slice-05. Runtime failure modes
# (cert issuance throttle, DB connection loss, image-pull) exercise the cluster, not the GitOps record.
