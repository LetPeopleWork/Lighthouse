# Acceptance SSOT — epic-5306-productization-platform, slice-11 restore-rehearsal (#5208 Part B)
# Driving port: (1) the per-tenant RESTORE procedure (a parameterised restore, keyed by exactly one
# identifier — the tenant id — that reads that tenant's own off-cluster backup and rebuilds its data into a
# scratch/replacement destination and nothing else), and (2) the rehearsal itself run against Tenant Zero.
# Slice-10 gave every tenant a monitored off-cluster backup; an UNTESTED backup is no backup. This slice
# proves the backup is RESTORABLE: a documented, rehearsed restore brings a single tenant back from its
# slice-10 backup within a stated recovery-time target, verified against Tenant Zero (LPW production) —
# turning the recovery-POINT commitment into an actual recovery guarantee, with an isolation check that a
# restore can never land in the wrong tenant. ADR-091 (namespace-isolated rehearsed restore keyed off the
# one id); RTO commitment ≤30min (O-3 resolved 2026-07-01).
#
# RECONCILIATION (inherits slice-10, 2026-07-01): backups are a scheduled logical dump per tenant of the
# bundled Postgres to off-cluster Infomaniak Object Storage (USER DECISION; ADR-091 CNPG WAL path DEFERRED).
# The restore therefore reloads a tenant's own logical dump into a scratch destination. Scenarios are
# written at the OBSERVABLE level (Tenant Zero restored from backup and serving, verified, timed under the
# recovery-time target, isolated to one tenant) so they hold for the logical-dump mechanism today and would
# still hold for a CNPG restore later.
#
# Executable via:
#   @in-memory      — helm-unittest / render assertions over the GitOps config this slice adds: the
#                     parameterised restore procedure in the `tenant-runtime` chart (keyed by id, reads the
#                     tenant's own off-cluster backup, writes only into that tenant's own destination).
#                     CI-runnable, no cluster.
#   @requires_external — live ArgoCD + the real cluster + off-cluster storage: rehearse a restore of Tenant
#                     Zero from its backup into a scratch destination, verify the data is intact and the
#                     instance serves, time it against the recovery-time target, and prove the restore
#                     touched only the intended tenant.
# RED until the PRIVATE platform repo grows the parameterised restore procedure (keyed by id, reading the
# tenant's own off-cluster backup) and the restore runbook, and a Tenant-Zero rehearsal is run. IaC/GitOps
# feature → no .cs/.ts scaffolds. The standalone product + chart standalone defaults stay byte-unchanged.
#
# CRUX (what slice-11 must get right): (1) the restore must actually SERVE — Tenant Zero brought back from
# backup must answer, not just have rows loaded. (2) the restored data is VERIFIED intact, not assumed. (3)
# the restore is TIMED and lands within the recovery-time target — an unbounded recovery is not a
# guarantee. (4) a restore is keyed to exactly one tenant identifier: restoring tenant A can never write
# into tenant B's namespace or database. (5) a restore with a missing or corrupt source FAILS LOUDLY — it
# never produces a half-restored or empty instance that looks recovered.

@feature:epic-5306-productization-platform
Feature: A rehearsed restore brings Tenant Zero back from backup — verified, timed within the recovery-time target, and isolated to one tenant
  As the LPW SaaS operator
  I want a documented restore rehearsed against our own production tenant
  So that recoverability is proven before any customer relies on it, within a known recovery time

  # --- CI-runnable: render-layer (the parameterised, id-keyed restore procedure) ---

  @US-11 @in-memory @env:tenant-runtime
  Scenario: A tenant's restore reads its own backup and writes only into its own destination
    Given a hosted tenant's restore procedure
    When it is rendered for that tenant
    Then it reads from that tenant's own off-cluster backup, keyed by the tenant's identifier
    And it writes only into that same tenant's own destination

  @US-11 @in-memory @env:tenant-runtime
  Scenario: The restore procedure is the same for every tenant, parameterised by identifier only
    Given two hosted tenants' restore procedures
    When each is rendered
    Then they differ only by the tenant's identifier
    And neither names another tenant's backup or destination

  @error @US-11 @in-memory @env:tenant-runtime
  Scenario: A restore aimed at another tenant's destination is rejected before it runs
    Given a proposed restore whose destination is not the tenant's own
    When the restore configuration is validated
    Then validation fails naming the mismatched tenant
    And no restore can write into a tenant it was not scoped to

  @error @US-11 @in-memory @env:tenant-runtime
  Scenario: A restore with no source backup is rejected rather than producing an empty instance
    Given a proposed restore whose source backup does not exist
    When the restore configuration is validated
    Then validation fails naming the missing source
    And no restore proceeds to build an empty instance that looks recovered

  # --- requires the live cluster + ArgoCD + off-cluster storage (the rehearsal) ---

  @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is restored from backup into a scratch destination and serves
    Given a recent off-cluster backup of Tenant Zero
    When the restore runbook is followed into a scratch destination
    Then the restored Tenant Zero instance serves requests
    And it was rebuilt only from the backup, with no hand-repair

  @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: The restored data is verified intact, not assumed
    Given Tenant Zero restored from backup into a scratch destination
    When its restored data is checked against what was backed up
    Then the data is verified intact
    And any missing or altered data would fail the check

  @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: The rehearsed restore completes within the recovery-time target
    Given a recent off-cluster backup of Tenant Zero
    When the restore rehearsal is run end to end and timed
    Then Tenant Zero is serving from the restore within the recovery-time target
    And the measured recovery time is recorded

  @error @US-11 @real-io @requires_external @env:fleet
  Scenario: Restoring one tenant leaves every other tenant untouched
    Given a fleet of hosted tenants each serving its own data
    When one tenant is restored from its backup
    Then only that tenant's data is rebuilt
    And every other tenant keeps serving its own unchanged data

  @error @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: A restore from a corrupt or absent backup fails loudly, not into a half-restored instance
    Given a restore pointed at a corrupt or absent backup
    When the restore is attempted
    Then it fails loudly and names the unusable backup
    And it leaves no half-restored instance that could be mistaken for recovered

  # --- AUTOMATED restore rehearsal (scope pulled in 2026-07-02: an untested backup is not a backup, so
  #     every hosted tenant's backup is proven restorable on a schedule, not just once by hand) ---

  @US-11 @in-memory @env:tenant-runtime
  Scenario: A hosted tenant renders a scheduled rehearsal of its own backup on a recurring cadence
    Given a hosted tenant's restore-rehearsal procedure
    When it is rendered for that tenant
    Then it runs on a recurring schedule against that tenant's own off-cluster backup, keyed by the tenant's identifier
    And it restores into a throwaway destination that is torn down afterwards, never the live database

  @error @US-11 @in-memory @env:tenant-runtime
  Scenario: A rehearsal that cannot prove the backup restorable is wired to alert, not to pass silently
    Given a hosted tenant's restore-rehearsal procedure
    When it is rendered for that tenant
    Then a failure to restore, an empty result, or a run over the recovery-time target raises an alert naming the tenant
    And the run reports failure rather than being recorded as a successful rehearsal

  @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: The scheduled rehearsal proves Tenant Zero's latest backup restorable and intact within the recovery-time target
    Given the scheduled restore rehearsal for Tenant Zero
    When it runs on its cadence
    Then it restores Tenant Zero's most recent backup into a throwaway destination, verifies it is intact, and completes within the recovery-time target
    And the throwaway destination is removed, leaving the live tenant untouched

  @error @US-11 @real-io @requires_external @env:tenant-zero
  Scenario: A failed rehearsal informs the operator rather than failing silently
    Given a scheduled rehearsal whose backup cannot be restored
    When the rehearsal runs
    Then the operator is actively informed, with the tenant named
    And the rehearsal is not recorded as successful, so the unproven backup also surfaces on the fleet health view

# The parameterised restore procedure (reload a tenant's own logical dump from off-cluster Infomaniak
# Object Storage into a scratch/replacement destination, keyed by the tenant id), the AUTOMATED weekly
# rehearsal of it, and the restore runbook live in the PRIVATE platform repo
# (LetPeopleWork/lighthouse-platform). The rehearsal dogfoods Tenant Zero (LPW production) so the runbook
# is proven before any customer relies on it. ADR-091's CloudNativePG restore path is DEFERRED/superseded
# (reconciliation 2026-07-01) — the restore reloads a logical dump. Full-cluster DR failover automation is
# documented in the runbook but its automation is OUT. Automated periodic restore-TESTING was a noted
# follow-up and has now been PULLED INTO this slice (reconciliation 2026-07-02, user decision): a weekly
# CronJob rehearses every hosted tenant's backup into a throwaway database, times it against the RTO, and
# opens a GitHub issue (the slice-08b alert channel) on any failure — with the passive RestoreRehearsalStale
# alert as the fleet-health backstop. This slice closes story #5208 (Backup & disaster recovery): slice-10
# gives the recovery POINT, slice-11 proves the recovery — once by hand and continuously thereafter.
