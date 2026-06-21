# Acceptance SSOT — epic-5306-k8s-productization, Walking Skeleton (slice-01)
# Executable via: helm template (render assertions) + helm install into ephemeral kind (smoke).
# RED until chart/ templates exist (DELIVER slice-01).

@feature:epic-5306-k8s-productization
Feature: One-command install brings the whole stack up
  As a self-hoster (platform-operator)
  I want to install the whole Lighthouse stack with a single helm command
  So that I adopt it as a unit without hand-writing manifests

  @walking_skeleton @driving_port @US-01 @real-io @env:ci-kind-clean
  Scenario: Self-hoster brings the stack up with one command
    Given a clean kind cluster with an ingress controller
    And a values file with the Postgres password and ingress host set
    When the operator runs "helm install l8e ./chart"
    Then all chart workloads reach Ready and Helm exits 0
    And the API workload and a Postgres workload are both Running
    And NOTES.txt prints the resolved access URL and the next step

  @walking_skeleton @US-01 @in-memory @standalone_gate @env:default-values
  Scenario: Default values preserve the single-container (embedded) shape
    Given the chart is rendered with default values only
    When the manifests are produced
    Then frontend.mode is embedded and exactly one API workload is defined
    And the database provider rendered is Postgres (never SQLite)
