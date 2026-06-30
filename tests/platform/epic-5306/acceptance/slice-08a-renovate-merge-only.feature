# Acceptance SSOT — epic-5306-productization-platform, slice-08a renovate-merge-only (#5205 RESCOPE)
# Driving port: a release-watcher-raised PULL REQUEST on the PRIVATE platform repo
# (LetPeopleWork/lighthouse-platform), the AUTO-MERGED Tenant-Zero version-override commit, and the
# operator's ONE git merge of the fleet-version PR (→ ArgoCD reconcile). The DELIVERED slice-08 substrate
# proved the MECHANIC (a per-record version override canaries ahead of the fleet; one fleet default rolls
# the rest); slice-08a adds the AUTOMATION around it: a new release is surfaced as a reviewable PR with no
# self-discovery, Tenant Zero takes that release FIRST hands-off (its override PR auto-merges once tenant
# validation is green), and promoting the whole fleet is exactly ONE reviewed merge. The SaaS payoff and
# the #5205 exit criterion: releasing to an arbitrary number of tenants is one merge, gated on a healthy
# canary on our own production (ADR-094 Renovate auto-canary; ADR-097 watch-scope + automerge policy).
# Executable via:
#   @in-memory      — CI-runnable today, no cluster: validate the committed release-watch configuration is
#                     well-formed and its watch covers the published Lighthouse chart + tracked platform
#                     components; assert the auto-merge policy marks ONLY Tenant Zero's version override
#                     (never the fleet version) for hands-off merge; assert auto-merge is gated on the
#                     required tenant-validation check, so a malformed record cannot auto-canary.
#   @requires_external — live Mend Renovate App + GitHub + ArgoCD + the real cluster: a published release
#                     raises the fleet-bump PR within one scan; a tracked-component bump raises its own
#                     distinct PR; a quiet week raises none; Tenant Zero auto-merges + canaries with zero
#                     operator action and runs ahead of the fleet; a bad canary is never auto-promoted;
#                     one merge converges the fleet; holding the merge leaves it on the prior version.
# RED until the PRIVATE platform repo grows a release-watch configuration (`renovate.json`: two custom
# managers — Tenant Zero's version override with automerge ON, the fleet version with automerge OFF, plus
# tracked platform-component sources no-automerge), re-adds Tenant Zero's per-record version anchor (the
# always-ahead canary the substrate had dropped), and the tenant-validation workflow is wired as the
# REQUIRED status check on `main` so auto-merge has a gate. IaC/GitOps + CI feature → no .cs/.ts/.py
# scaffolds; the RED state is that the release-watch configuration and the canary anchor do not exist yet.
#
# CRUX (what slice-08a must add over the DELIVERED slice-08 substrate): the substrate made canary/promote
# a MANUAL git edit (add an override to canary; edit the fleet default to promote). Slice-08a removes the
# self-discovery and the hand-edit on the canary side: (1) a release no longer has to be NOTICED — it
# arrives as a reviewable PR. (2) Tenant Zero no longer canaries by operator memory — its override PR
# AUTO-MERGES once the tenant-validation check passes, so the "Tenant Zero first" promise becomes a
# property of git + the watcher, not discipline. (3) the fleet version PR is NEVER auto-merged — it stays
# the one-click human promote gate, so nothing auto-promotes the fleet off a failed canary. (4) the
# released-to-fleet operator-action count is driven to exactly ONE merge (the #5205 exit criterion).

@feature:epic-5306-productization-platform
Feature: A new release is surfaced as a pull request, Tenant Zero auto-canaries it hands-off, and one merge rolls the fleet
  As the LPW SaaS operator
  I want new releases raised as reviewable pull requests, my own production to take each release first
  hands-off, and promoting the whole fleet to cost exactly one merge
  So that releases stop being self-discovered chores, I feel any upgrade pain on Tenant Zero before any
  customer, and shipping to an arbitrary number of tenants is one reviewed click gated on a healthy canary

  # --- CI-runnable: the committed release-watch configuration + automerge policy (no cluster) ---

  @US-08a-1 @in-memory @env:release-candidate
  Scenario: The release-watch configuration is well-formed and covers the right versions
    Given the repository's release-watch configuration is committed
    When the configuration is validated
    Then it is accepted as well-formed
    And its watch covers the published Lighthouse chart and the tracked platform components

  @US-08a-2 @in-memory @env:tenant-zero
  Scenario: Only Tenant Zero's version is allowed to merge itself
    Given the release-watch configuration is committed
    When the auto-merge policy is inspected
    Then only Tenant Zero's version override is marked for hands-off auto-merge
    And the fleet-wide version is never marked for auto-merge
    And each tracked platform component is left for the operator to review

  @US-08a-3 @in-memory @env:release-candidate
  Scenario: A version bump cannot merge itself until tenant validation passes
    Given the auto-merge policy gates on a required tenant-validation check
    When the gate that auto-merge waits on is inspected
    Then tenant validation is required before any version bump can merge
    And auto-merge has nothing to act on until that check is green

  @error @US-08a-2 @in-memory @env:tenant-zero
  Scenario: A malformed Tenant Zero record cannot auto-canary
    Given Tenant Zero's record fails tenant validation
    When the auto-merge gate evaluates the version override
    Then the override is not merged
    And Tenant Zero is not moved to the new version

  # --- requires the live release-watcher + GitHub + ArgoCD + the real cluster ---

  @US-08a-1 @real-io @requires_external @env:release-candidate
  Scenario: A new Lighthouse release raises a fleet-bump pull request
    Given the release-watcher is watching the published Lighthouse chart
    When a new Lighthouse version is published
    Then within one scan interval a pull request is opened on the platform repository
    And it bumps the fleet version to the new release
    And its title names the version delta and links the changelog for review

  @US-08a-1 @real-io @requires_external @env:release-candidate
  Scenario: A new platform-component version raises its own separate pull request
    Given the release-watcher is watching the tracked platform-component versions
    When a new version of a tracked component is published
    Then a separate pull request is opened for that component
    And it stays distinct from the Lighthouse application bump so each is reviewed on its own

  @US-08a-1 @real-io @requires_external @env:release-candidate
  Scenario: A week with no new versions raises no pull request
    Given the fleet is already on the latest released versions
    When the release-watcher completes its scan
    Then no version-bump pull request is opened
    And the fleet stays on its current version with no churn

  @US-08a-2 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero takes the new release first with no operator action
    Given a new Lighthouse version has been published
    When the release-watcher auto-merges Tenant Zero's version override
    Then Tenant Zero rolls to the new version with no merge or edit by the operator
    And Tenant Zero serves successful responses on the new version

  @US-08a-2 @real-io @requires_external @env:tenant-zero
  Scenario: The canary runs ahead of the fleet until the fleet is promoted
    Given Tenant Zero has auto-canaried the new version
    And the fleet promotion pull request is not yet merged
    When the operator lists the fleet's running versions
    Then Tenant Zero shows the new revision
    And every other tenant shows the prior revision

  @error @US-08a-2 @real-io @requires_external @env:tenant-zero
  Scenario: A bad canary on Tenant Zero is never auto-promoted to the fleet
    Given Tenant Zero has auto-canaried the new version
    And Tenant Zero is unhealthy on that version
    When the fleet promotion is left to the operator
    Then no other tenant is moved to the new version automatically
    And the regression is visible on Tenant Zero first

  @US-08a-3 @real-io @requires_external @env:fleet
  Scenario: Merging one pull request rolls the whole fleet
    Given Tenant Zero is healthy on the new version
    And a pull request bumping the fleet version is open
    When the operator merges that one pull request
    Then every non-canary tenant rolls to the new version
    And listing the fleet shows all tenants synced on the new revision

  @US-08a-3 @real-io @requires_external @env:fleet
  Scenario: Releasing to the whole fleet costs exactly one operator action
    Given a healthy canary and an open fleet-bump pull request
    When the operator releases the new version to the fleet
    Then the only action performed is merging the one pull request
    And the fleet converges with no further manual step

  @error @US-08a-3 @real-io @requires_external @env:fleet
  Scenario: Holding the merge leaves the fleet on the prior version
    Given a fleet-bump pull request is open
    And Tenant Zero looks suspect on the new version
    When the operator chooses not to merge
    Then every non-canary tenant stays on the prior version
    And nothing is rolled

# The release-watch configuration, the Tenant-Zero canary anchor, and the automerge split live in the
# PRIVATE platform repo (LetPeopleWork/lighthouse-platform): `renovate.json` (Mend Renovate App; two
# custom regex managers over the published `lighthouse` helm datasource — `chartVersion` on the lpw record
# with automerge ON, `promotedVersion` on the fleet appset with automerge OFF — plus tracked
# platform-component sources, no automerge), the re-added `chartVersion` anchor on `tenants/lpw/tenant.yaml`,
# and `validate-tenants.yml` wired as the REQUIRED status check that automerge gates on (operator one-time
# setup: install the Mend App, grant repo access, enable branch protection on `main`). The substrate's
# matrix ApplicationSet + `hasKey` targetRevision is UNCHANGED — the watcher only writes the knobs it
# already exposes. Auto-rollback, self-service signup, and per-tenant version PINNING policy are OUT;
# the ordered upgrade + smoke-test alert is slice-08b; the rehearsed broken-image drill is slice-08c.
