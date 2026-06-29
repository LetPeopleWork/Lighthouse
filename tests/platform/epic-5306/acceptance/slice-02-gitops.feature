# Acceptance SSOT — epic-5306-productization-platform, slice-02 gitops-control-plane (#5201)
# Driving port: the `argocd` CLI + the platform git repo (gitops/) reconciled by ArgoCD.
# Executable via:
#   @in-memory      — kubeconform/yaml-lint over gitops/ manifests + app-of-apps wiring (CI-runnable now).
#   @requires_external — live ArgoCD reconcile + drift self-heal against a real cluster.
# RED until gitops/bootstrap/ + gitops/platform/ exist (DELIVER slice-02). ADR-086 (ApplicationSet,
# mono-repo bootstrap/+platform/+tenants/), ADR-089 (per-incident break-glass auto-sync disable).

@feature:epic-5306-productization-platform
Feature: The platform changes by merging a PR, with drift self-healed
  As the LPW SaaS operator
  I want ArgoCD to make the cluster become whatever the git repo says
  So that every change goes through review and silent hand-drift can no longer accumulate

  # --- CI-runnable: static GitOps repo structure + manifest validity ---

  @US-02 @in-memory @env:ci
  Scenario: The GitOps repo layout names where tenants live (CC-2 carrier)
    Given the platform git repo with an app-of-apps root
    When the repo layout is inspected
    Then a bootstrap, a platform, and a tenants area are present
    And the tenants area is the declared carrier for a per-tenant record (tenants/<id>/tenant.yaml)

  @US-02 @in-memory @env:ci
  Scenario: Every ArgoCD manifest is schema-valid
    Given the ArgoCD Application and ApplicationSet manifests under the repo
    When the manifests are validated against the Kubernetes/ArgoCD schemas
    Then validation reports no errors
    And the app-of-apps root points at the platform repo, never at a hand path

  # --- requires a real cluster with ArgoCD installed ---

  @US-02 @real-io @requires_external @env:cluster-argocd
  Scenario: A merged PR changes the cluster with no manual apply
    Given ArgoCD reconciles the platform repo via an app-of-apps root
    When the operator merges a PR adding a platform component
    Then ArgoCD syncs the component without a manual kubectl apply
    And "argocd app list" shows the root and child apps Synced and Healthy

  @US-02 @real-io @requires_external @env:cluster-argocd
  Scenario: Manual drift is self-healed
    Given a resource managed by ArgoCD is live
    When the operator deletes that resource by hand
    Then ArgoCD restores it to the state declared in git within the sync interval

  @US-02 @real-io @requires_external @env:cluster-argocd
  Scenario: A break-glass live fix stands until committed back
    Given an incident requires an immediate live change ArgoCD would revert
    When the operator disables auto-sync on the single affected Application (the documented break-glass path)
    Then the live fix stands until it is committed back to git
    And a standing alert flags that auto-sync is disabled until it is re-enabled

# ArgoCD self-bootstrap ordering and Redis/HA posture of the ArgoCD install itself are operator
# runbook concerns, not acceptance scenarios. The tenant ApplicationSet generator is slice-07.
