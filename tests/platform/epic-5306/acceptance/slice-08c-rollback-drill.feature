# Acceptance SSOT — epic-5306-productization-platform, slice-08c rollback-drill (#5205 RESCOPE)
# Driving port: the post-upgrade smoke-test + ops ALERT channel (the DETECT half, from slice-08b) and the
# operator's `git revert` of a version bump → ArgoCD reconcile (the RECOVER half, from the DELIVERED
# slice-08 substrate). Slices 08a/08b built the merge-only release and the smoke-test alert; slice-08c
# PROVES the recovery promise by rehearsing it end-to-end against a GENUINELY broken release on a THROWAWAY
# tenant — push a deliberately broken image, watch the smoke-test catch it and alert, run the documented
# `git revert`, and watch the tenant return to successful responses on the prior version — with the
# detect→recover time recorded as a rehearsed runbook and Tenant Zero (real LPW production) untouched
# throughout. The SaaS payoff: "if a release is bad, will we catch it and recover within target?" is
# answered by a timed, repeatable drill, not assumed (ADR-096 smoke-test is detect+alert only; ADR-093
# rollback = operator `git revert`; reuses slice-07 throwaway provisioning).
# Executable via:
#   @in-memory      — CI-runnable today, no cluster: the additive-only invariant that makes the recovery a
#                     pure version revert — reverting the bump alone restores the prior revision, no schema
#                     rollback step, because the upgrade carried only additive schema changes (the same
#                     expand-only guarantee slice-08b clears in the release build).
#   @requires_external — a LIVE drill: provision a throwaway tenant, push a deliberately broken image,
#                     prove the smoke-test detects it and alerts (naming the throwaway tenant + broken
#                     version), `git revert` the bump, prove ArgoCD restores the prior healthy revision and
#                     the tenant serves successfully again within the rollback target, and prove Tenant
#                     Zero stays healthy on its current version the whole time.
# RED until the slice-08b smoke-test + alert exist (the DETECT dependency) and the drill is run live on a
# throwaway tenant (`canarytest`) provisioned by the slice-07 generator: the broken-image push, the timed
# detect→revert→recover sequence, and the recorded runbook are the deliverable. Operator-initiated rollback
# only — auto-rollback on smoke-test failure is flagged future (O-08-5), NOT built here. IaC/GitOps + CI
# feature → no .cs/.ts/.py scaffolds; the RED state is that the rehearsed broken-image runbook does not
# exist yet (the recovery MECHANIC — `git revert` + additive-only migrations — already shipped in slice-08).
#
# CRUX (what slice-08c must add over the substrate + 08b): slice-08 proved a CLEAN `git revert` rolls a
# throwaway tenant back; slice-08b proved a smoke-test can detect an unhealthy tenant. Slice-08c joins them
# against a DELIBERATELY broken release and TIMES the whole loop: (1) the broken release must be CAUGHT by
# the smoke-test (alert fires), not silently served — detection is the unproven link. (2) the documented
# `git revert` must actually RECOVER the throwaway tenant to the prior healthy revision within the rollback
# target, turning a theoretical path into an evidenced runbook. (3) Tenant Zero (production) must stay
# untouched throughout — the drill is rehearsed on a throwaway, mirroring the DELIVERED slice-08 proof that
# kept prod's version fixed while the canary→promote→rollback mechanic was exercised alongside it.

@feature:epic-5306-productization-platform
Feature: Rehearse recovery by catching a deliberately broken release on a throwaway tenant and reverting to health
  As the LPW SaaS operator
  I want to push a deliberately broken release to a throwaway tenant, watch the smoke-test catch it, revert
  the bump, and see the tenant recover within target — all with my own production untouched
  So that "we catch a bad release and recover within target" is a proven, timed, repeatable runbook rather
  than an untested promise, and I never rehearse recovery on a customer or on Tenant Zero

  # --- CI-runnable: the additive-only invariant that makes recovery a pure version revert ---

  @US-08c-1 @in-memory @env:release-candidate
  Scenario: Rolling back needs no schema rollback because migrations are additive-only
    Given the upgrade being rehearsed carried only additive schema changes
    When the rollback path is examined
    Then reverting the version bump alone restores the prior revision
    And no schema rollback step is required

  # --- requires the live cluster + ArgoCD + GitHub (a timed drill on a throwaway tenant) ---

  @error @US-08c-1 @real-io @requires_external @env:tenant-canarytest
  Scenario: A deliberately broken release on a throwaway tenant is detected, not silently served
    Given a deliberately broken image is pushed to a throwaway canary tenant
    When the post-upgrade smoke-test runs
    Then the smoke-test fails and an alert fires naming the throwaway tenant and the broken version
    And the broken release is caught rather than served unnoticed

  @US-08c-1 @real-io @requires_external @env:tenant-canarytest
  Scenario: Reverting the bump restores the prior healthy revision within the rollback target
    Given a throwaway tenant is unhealthy on a broken release
    When the operator reverts the version bump in git
    Then ArgoCD restores the prior revision
    And the throwaway tenant returns to successful responses
    And the detect-to-recover time is recorded and within the rollback target

  @US-08c-1 @real-io @requires_external @env:tenant-zero
  Scenario: Production is untouched throughout the rollback drill
    Given the broken-image drill runs on a throwaway tenant
    When the drill runs from the broken push through revert and recovery
    Then Tenant Zero stays healthy on its current version throughout
    And no customer tenant is affected

# The drill is run live in the PRIVATE platform repo (LetPeopleWork/lighthouse-platform): a throwaway
# tenant (`canarytest`) is provisioned by the slice-07 one-record generator, given a deliberately broken
# image, and driven through detect (the slice-08b version-stamped PostSync smoke-test → GitHub-issue alert)
# → recover (`git revert` of the bump → ArgoCD restores the prior revision; additive-only migrations need
# no schema rollback, ADR-093) → teardown (remove the record, slice-07 no-orphan prune). The detect→recover
# time is captured as a runbook against the rollback target (KPI-5 ≤15 min). Tenant Zero is NEVER the
# broken-image target — production stays on its current version throughout (mirrors the DELIVERED slice-08
# throwaway-convergence proof). Auto-rollback on smoke-test failure (a future controller, O-08-5) and any
# rehearsal on a real customer tenant are OUT.
