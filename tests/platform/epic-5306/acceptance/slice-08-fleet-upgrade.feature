# Acceptance SSOT — epic-5306-productization-platform, slice-08 fleet-upgrade (#5205)
# Driving port: ONE version bump in git (the chart's shipped `appVersion`/image tag) reconciled by
# ArgoCD across the fleet, STAGED — Tenant Zero takes the new version first as a canary
# (`canaryVersion`), and only after it is proven healthy is it promoted to every tenant
# (`promotedVersion`). Slice-07 made the fleet (one record → one isolated tenant); this slice makes the
# fleet UPGRADE safely: a single bump rolls the new version to every tenant as a zero-downtime rolling
# update, Tenant Zero feels any pain first, a destructive migration is blocked in CI before it can reach
# anyone, and rollback is `git revert` + `helm rollback`. The SaaS payoff: a small team can ship a
# release to an arbitrary number of tenants during the working day with no maintenance window and no
# tenant left behind (ADR-093 automated upgrade; ADR-086 one generator, no production special-casing).
# Executable via:
#   @in-memory      — CI-runnable today, no cluster: the epic-5305 `ExpandOnlyMigrationGuard` rejects a
#                     non-expand-only migration (the fleet-wide pre-flight), and a helm render proving a
#                     single shipped-version bump fans to every tenant (image tag defaults to
#                     Chart.appVersion, ADR-083 — one source, whole fleet).
#   @requires_external — live ArgoCD + the real cluster: bump the version, watch Tenant Zero canary it
#                     with zero dropped requests, promote, watch the fleet converge, prove a failed
#                     canary is NOT promoted, and rehearse `git revert` rollback. Proven on Tenant Zero
#                     during DELIVER.
# RED until the PRIVATE platform repo (LetPeopleWork/lighthouse-platform) grows the staged-rollout in
# its `tenants` ApplicationSet — a `canaryVersion` override pinned to Tenant Zero and a `promotedVersion`
# default the rest of the fleet inherits — and the existing GitHub Actions release workflow wires the
# expand-only pre-flight as a tenant-rollout gate. The #5199 chart is UNCHANGED: its image tag already
# defaults to Chart.appVersion (ADR-083), so a version bump is already a single-source change; epic-5305
# supplies the rolling-update + connection-drain + `ExpandOnlyMigrationGuard` primitives this slice
# composes. IaC/GitOps + CI feature → no .cs/.ts scaffolds; the RED state is that the staged-rollout
# params and the workflow gate do not exist yet.
#
# CRUX (what slice-08 must add over slice-07): slice-07 PROVISIONS tenants — one record fans into one
# isolated tenant. Slice-08 UPGRADES the fleet that slice-07 produced, and the learning is STAGING: (1)
# the new version does NOT hit every tenant at once — Tenant Zero takes it first (`canaryVersion`) and
# the fleet (`promotedVersion`) only follows a healthy canary, so a bad release stops at our own
# production. (2) the upgrade is a zero-downtime rolling update (epic-5305 probes + drain), not a
# maintenance window. (3) a destructive (non-expand-only) migration is caught by the CI pre-flight
# BEFORE any tenant rolls, because rolling tenants share a live Postgres and additive-only is the only
# safe shape (expand-only mandate). (4) rollback is `git revert` (+ `helm rollback`): additive
# migrations need no schema rollback, so reverting the version is sufficient.

@feature:epic-5306-productization-platform
Feature: One version bump upgrades the whole fleet safely — canary on Tenant Zero, then promote, with a rollback path
  As the LPW SaaS operator
  I want to bump one version in git and have it roll to every tenant as a staged, zero-downtime upgrade
  So that I can ship a release to an arbitrary number of tenants during the working day, feel any pain
  on my own production first, never let a destructive migration reach a customer, and revert if needed

  # --- CI-runnable: render-layer + the epic-5305 expand-only pre-flight (no cluster) ---

  @US-08 @in-memory @env:release-candidate
  Scenario: One shipped-version bump is the single source every tenant inherits
    Given a release that bumps only the chart's shipped version
    When a tenant's workload is rendered without an explicit version override
    Then its running image resolves to the chart's shipped version
    And any tenant without an explicit override resolves to that same shipped version

  @error @US-08 @in-memory @env:release-candidate
  Scenario: A release carrying a destructive migration is blocked before any tenant upgrades
    Given a release candidate whose database migration drops or renames an existing column or table
    When the expand-only pre-flight inspects the migration before rollout
    Then the release is rejected naming the destructive operation
    And no tenant is rolled to that version

  @US-08 @in-memory @env:release-candidate
  Scenario: An additive-only migration passes the pre-flight and is cleared to roll
    Given a release candidate whose migration only adds new columns or tables
    When the expand-only pre-flight inspects the migration before rollout
    Then the migration is accepted as expand-only
    And the release is cleared to begin the staged rollout

  # --- requires the live cluster + ArgoCD (proven on Tenant Zero during DELIVER) ---

  @US-08 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero canaries the new version first, with zero dropped requests
    Given the fleet is serving the current version and a new version is committed
    When ArgoCD rolls the new version to Tenant Zero ahead of the fleet
    Then Tenant Zero keeps returning successful responses throughout the rolling update
    And its old workload is drained only after the new one passes readiness, so no in-flight request is lost
    And the rest of the fleet is still on the current version until the canary is promoted

  @error @US-08 @real-io @requires_external @env:tenant-zero
  Scenario: A canary that fails health on Tenant Zero is not promoted to the fleet
    Given a new version is rolled to Tenant Zero as the canary
    When Tenant Zero fails its health check on the new version
    Then the new version is not promoted to any other tenant
    And the rest of the fleet keeps serving the current version unaffected

  @US-08 @real-io @requires_external @env:fleet
  Scenario: After a healthy canary is promoted, every tenant converges on the new revision
    Given Tenant Zero has canaried the new version and is healthy
    When the new version is promoted to the fleet and ArgoCD reconciles
    Then every tenant reports as synced on the new revision
    And each tenant served continuously throughout its own rolling update

  @error @US-08 @real-io @requires_external @env:fleet
  Scenario: A tenant that fails to roll during promotion is surfaced, not silently skipped
    Given the new version is being promoted across the fleet
    When one tenant fails to roll to the new version
    Then that tenant is reported as out of sync on the prior version
    And the operator can see the fleet is on mixed versions until it is resolved

  @error @US-08 @real-io @requires_external @env:fleet
  Scenario: A transient failure mid-rollout is retried, never left half-applied
    Given a tenant is rolling to the new version
    When its rollout is interrupted by a transient failure
    Then the rollout is retried until the tenant is healthy on the new version
    And the tenant is never left serving a half-applied upgrade

  @US-08 @real-io @requires_external @env:fleet
  Scenario: Reverting the version in git rolls the whole fleet back to the prior revision
    Given the fleet has been promoted to a new version
    When the version bump is reverted in git and ArgoCD reconciles
    Then every tenant returns to the prior revision
    And no schema rollback is needed because the migration was additive only

  @US-08 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is the permanent canary, taking every release before any customer tenant
    Given Tenant Zero is registered as the fleet's standing canary
    When a new release is rolled out across the fleet
    Then the new version reaches Tenant Zero before any customer tenant
    And no customer tenant is promoted ahead of a healthy Tenant Zero canary

# The staged-rollout lives in the PRIVATE platform repo (LetPeopleWork/lighthouse-platform): the
# `tenants` ApplicationSet gains a `canaryVersion` override pinned to Tenant Zero and a `promotedVersion`
# default the fleet inherits, and the existing GitHub Actions release workflow wires the epic-5305
# `ExpandOnlyMigrationGuard` as a tenant-rollout gate. The #5199 chart is UNCHANGED — its image tag
# already defaults to Chart.appVersion (ADR-083), so a version bump is already a single-source change,
# and epic-5305 already supplies the rolling-update + connection-drain + expand-only primitives this
# slice composes. Per-tenant version PINNING policy, and blue-green / in-tenant canary TRAFFIC splitting
# (epic-5305 Band D), are OUT of this slice; fleet observability of the rollout is slice-09.
