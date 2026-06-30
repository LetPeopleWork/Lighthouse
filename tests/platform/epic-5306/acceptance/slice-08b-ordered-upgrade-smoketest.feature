# Acceptance SSOT — epic-5306-productization-platform, slice-08b ordered-upgrade-smoketest (#5205 RESCOPE)
# Driving port: (1) the per-tenant rolling UPGRADE (a tenant's served behaviour through the roll) and
# (2) a per-tenant post-upgrade HEALTH CHECK that runs after a tenant syncs a new version and, on failure,
# the operator's OPS ALERT channel naming the tenant + version. Slice-08a made releasing one merge;
# slice-08b makes each tenant's upgrade SAFE-BY-CONSTRUCTION and OBSERVED: the schema change is applied
# before the new version serves (so a new API never runs against an un-migrated schema), a destructive
# schema change is blocked in the release build before any tenant rolls, and after a tenant syncs a new
# version a smoke-test confirms the EXPECTED version actually serves healthy — staying silent on success
# and raising a per-tenant-attributed alert (tenant + version + failing code) within minutes on failure.
# The SaaS payoff: a bad upgrade is named and surfaced fast instead of festering until a customer reports
# it (ADR-095 migration-before-API is emergent, no pre-upgrade Job; ADR-096 version-stamped post-sync
# smoke-test → GitHub-issue alert).
# Executable via:
#   @in-memory      — CI-runnable today, no cluster: a helm render of the version-stamped post-upgrade
#                     health check proving it carries the EXACT version it must see served and targets the
#                     tenant's served health endpoint; plus the epic-5305 expand-only guard reused as the
#                     release pre-flight (a destructive schema change fails the build; an additive-only one
#                     is cleared), and the additive-only invariant that makes migration-before-API safe.
#   @requires_external — live ArgoCD + the real cluster + GitHub: poll a tenant through its roll for
#                     continuous successful responses; prove the health check waits for the new version
#                     before judging (race-robust); prove a healthy tenant raises nothing; prove an
#                     unhealthy tenant raises a named alert within the detection target; prove only the
#                     sick tenant is named when several upgrade at once.
# RED until the PRIVATE platform repo's `tenant-runtime` overlay grows a version-stamped post-sync
# smoke-test Job (asserts the served version + health on the tenant's public health endpoint; on failure
# opens/updates a GitHub issue in LetPeopleWork/lighthouse-platform naming tenant + version + code) and an
# ESO-materialised issues-write token, and the runtime ApplicationSet is folded onto the fleet-version
# matrix so the Job re-fires on every roll. The migration-before-API ordering needs NO new component
# (ADR-095): it is emergent from the readiness-gated rolling update + on-boot advisory-lock-coordinated
# Database.Migrate() + the expand-only guard — documented and PROBED, not built. IaC/GitOps + CI feature
# → no .cs/.ts/.py scaffolds; the RED state is that the smoke-test Job + token + version-stamp do not
# exist yet (the expand-only guard already ships from epic-5305 and runs in the release build today).
#
# CRUX (what slice-08b must add over the substrate): the substrate ROLLS a version; it does not GUARANTEE
# the migration runs before the API serves and it does not CONFIRM the rolled tenant is actually healthy
# on the new version. Slice-08b adds (1) the explicit assertion that ordering holds by construction — the
# new pod migrates on boot before it passes readiness, and the old pod tolerates the additive schema, so
# there is never a new-API-on-old-schema window (proven by continuous-200-through-the-roll, not faith).
# (2) a destructive schema change is caught in CI before any tenant can be offered it (the only case where
# strict pre-ordering would matter is made structurally impossible). (3) a version-STAMPED post-sync
# smoke-test that defeats the two-app race by waiting for the EXPECTED version before judging, so the
# signal has true version attribution and only the genuinely-sick tenant is named.

@feature:epic-5306-productization-platform
Feature: Each tenant upgrades in the right order and a post-upgrade smoke-test alerts when one comes back unhealthy
  As the LPW SaaS operator
  I want every tenant's schema change applied before its new version serves, destructive changes blocked
  before any tenant rolls, and a post-upgrade smoke-test that names a tenant the moment it is unhealthy
  So that a rolling upgrade never exposes a new API on an old schema and a bad upgrade on one tenant among
  many is surfaced fast and by name, not left to fester until a customer reports it

  # --- CI-runnable: render-layer (version-stamped check) + the epic-5305 expand-only pre-flight ---

  @US-08b-2 @in-memory @env:release-candidate
  Scenario: The post-upgrade health check is stamped with the version it must confirm
    Given a tenant runtime is rendered for a given fleet version
    When its post-upgrade health check is inspected
    Then the check carries the exact version it must see served
    And it targets the tenant's served health endpoint

  @error @US-08b-1 @in-memory @env:release-candidate
  Scenario: A destructive schema change is blocked before any tenant rolls
    Given a candidate release whose schema change drops or renames an existing column or table
    When the release build runs its expand-only pre-flight
    Then the build fails naming the destructive change
    And no tenant is ever offered the destructive version

  @US-08b-1 @in-memory @env:release-candidate
  Scenario: An additive-only schema change is cleared to roll
    Given a candidate release whose schema change only adds new columns or tables
    When the release build runs its expand-only pre-flight
    Then the change is accepted as additive-only
    And the ordered upgrade is cleared to proceed

  # --- requires the live cluster + ArgoCD + GitHub ---

  @US-08b-1 @real-io @requires_external @env:tenant-riverbank
  Scenario: A tenant under upgrade serves continuously and never exposes an un-migrated schema
    Given a tenant is upgrading to a new version that adds to its schema
    When the upgrade rolls
    Then the schema change is applied before the new version serves any traffic
    And the tenant returns continuous successful responses throughout the roll

  @US-08b-2 @real-io @requires_external @env:tenant-riverbank
  Scenario: The health check waits for the new version before judging health
    Given a tenant is mid-roll and still serving the prior version on some instances
    When the post-upgrade health check runs
    Then it waits until the expected version is served before judging health
    And it does not alert on the transitional prior version

  @US-08b-2 @real-io @requires_external @env:tenant-riverbank
  Scenario: A healthy upgraded tenant raises no alert
    Given a tenant has synced a new version
    And its health endpoint serves the expected version healthily
    When the post-upgrade health check runs
    Then no alert is raised
    And the operator's ops channel stays quiet

  @error @US-08b-2 @real-io @requires_external @env:tenant-riverbank
  Scenario: An unhealthy upgraded tenant raises a named alert within the detection target
    Given a tenant has synced a new version
    And its health endpoint never serves the expected version healthily
    When the post-upgrade health check runs
    Then an alert reaches the operator's ops channel within the detection target
    And the alert names the tenant, the version, and the failing health code

  @error @US-08b-2 @real-io @requires_external @env:fleet
  Scenario: Only the unhealthy tenant is named when one of several upgrades fails
    Given two tenants upgrade and only one comes back unhealthy
    When the post-upgrade health checks run
    Then an alert names only the unhealthy tenant
    And the healthy tenant raises nothing

  @error @US-08b-2 @real-io @requires_external @env:tenant-riverbank
  Scenario: An undeliverable alert fails loudly instead of passing silently
    Given a tenant has come back unhealthy after upgrade
    And the operator's ops alert channel cannot be reached
    When the post-upgrade health check tries to raise the alert
    Then the health check itself reports failure where the operator can see it
    And the unhealthy upgrade is never recorded as a success

# The version-stamped smoke-test lives in the PRIVATE platform repo (LetPeopleWork/lighthouse-platform):
# the `tenant-runtime` overlay grows an ArgoCD PostSync-hook Job (minimal curl image, hook-delete-policy
# HookSucceeded) that polls the tenant's served-version + health endpoint with bounded retry/backoff,
# waits for the EXPECTED version, and on failure POSTs to the GitHub Issues API titled
# "tenant <id> unhealthy after upgrade to <version> (health <code>)"; the issues-write token is an
# ESO-materialised fine-grained PAT (recommended: a shared `platform` namespace so the token lives once),
# and `applicationset-runtime.yaml` is folded onto the `promotedVersion` matrix so the Job is version-
# stamped and re-fires on every roll. Migration-before-API ordering adds NO component (ADR-095) — it reuses
# the epic-5305 on-boot Database.Migrate() + Postgres advisory lock + readiness gating + the expand-only
# guard, asserted via the continuous-serve observable. Auto-rollback on smoke-fail is OUT (slice-08c
# rehearses the manual revert); paging/severity routing and the Prometheus/Alertmanager channel are
# slice-09 (which can supersede the GitHub-issue channel without changing this smoke-test contract).
