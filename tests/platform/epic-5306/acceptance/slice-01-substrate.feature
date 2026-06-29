# Acceptance SSOT — epic-5306-productization-platform, slice-01 substrate-up (#5320)
# Driving port: the `tofu` CLI against the OpenTofu substrate module (infra/substrate/).
# Executable via:
#   @in-memory      — tofu fmt/validate + tflint on the module, no cloud (CI-runnable now).
#   @requires_external — tofu apply/destroy against the primary provider, real cluster.
# RED until infra/substrate/ exists (DELIVER slice-01). ADR-088 (revised, O-1 resolved 2026-06-29):
# primary adapter = Infomaniak managed Kubernetes; k3s-on-compute (OpenStack/Hetzner) = fallback —
# both satisfy the same CC-4 conformant-cluster contract.

@feature:epic-5306-productization-platform
Feature: A conformant Kubernetes substrate stands up from code
  As the LPW SaaS operator
  I want the hosting cluster declared as OpenTofu, not hand-clicked in a console
  So that the substrate is reproducible, portable code rather than an unrebuildable snowflake

  # --- CI-runnable: static module validation, no cloud ---

  @US-01 @in-memory @env:ci
  Scenario: The substrate module is well-formed and valid
    Given the OpenTofu substrate module configured for the primary provider
    When the module is formatted and validated
    Then formatting is canonical and validation reports no errors
    And provider-specific resources live behind the conformant-cluster boundary (CC-4)

  # --- requires a real provider account + cluster ---

  @US-01 @real-io @requires_external @env:provider-infomaniak
  Scenario: Substrate stands up from code on the chosen provider
    Given the substrate module configured for Infomaniak managed Kubernetes (a node pool of 3)
    When the operator runs "tofu apply"
    Then a conformant cluster is created with an ingress controller and a default storage class
    And "kubectl get nodes" lists every declared node as Ready

  @US-01 @real-io @requires_external @env:provider-infomaniak
  Scenario: The shipped chart installs onto the substrate unchanged
    Given a freshly applied substrate cluster
    When the operator installs the shipped #5199 chart with default values into a scratch namespace
    Then all chart workloads reach Running/Ready
    And no chart template was modified to fit the substrate (standalone gate intact)

  @US-01 @real-io @requires_external @env:provider-infomaniak
  Scenario: Teardown leaves no orphans and re-apply reproduces the cluster
    Given a running substrate cluster
    When the operator runs "tofu destroy"
    Then all cloud resources created by the module are removed
    And a subsequent "tofu apply" reproduces an equivalent conformant cluster

# Multi-provider parity (AWS EKS et al.) is deferred to slice-12 and intentionally NOT tested here.
# Provider rate limits / quota-exhaustion / partial-apply recovery exercise the cloud API, not the
# module contract; they are operator-runbook concerns, not chart/IaC acceptance.
