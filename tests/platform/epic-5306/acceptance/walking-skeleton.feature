# Acceptance SSOT — epic-5306-productization-platform, Walking Skeleton (slice-03)
# The single end-to-end line the whole epic hangs on: substrate (S01) → ArgoCD (S02) →
# shipped #5199 chart → DNS route → hand-made secret → LPW production reachable as Tenant Zero.
# Executable via: tofu apply + argocd sync + helm-through-ArgoCD + curl against the live host.
# @requires_external — needs the real substrate (Infomaniak/OpenStack) + ArgoCD + ingress + cert.
# RED until infra/substrate/, gitops/, and the tenant-lpw GitOps record exist (DELIVER S01-S03).

@feature:epic-5306-productization-platform
Feature: LetPeopleWork runs as Tenant Zero on the productization platform
  As the LPW SaaS operator (Benjamin)
  I want LPW's own production Lighthouse brought up end-to-end through the platform
  So that the whole thread — substrate, GitOps, chart, routing, secret — is proven on real
  production data before any customer tenant or automation is built

  @walking_skeleton @driving_port @US-03 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is reachable over HTTPS through the full platform path
    Given a conformant substrate cluster stood up from code
    And ArgoCD reconciling the platform repo via an app-of-apps root
    When the operator commits the tenant-lpw record (shipped chart, single-host ingress, hand-made secret)
    Then LPW's Lighthouse runs in an isolated "tenant-lpw" namespace with all pods Ready
    And "https://lpw.lighthouse.letpeople.work" serves LPW over a trusted TLS certificate
