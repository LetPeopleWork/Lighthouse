# Acceptance SSOT — epic-5306-productization-platform, slice-10 per-tenant-backups (#5208 Part A)
# Driving port: (1) the per-hosted-tenant BACKUP ENABLEMENT (a tenant's generator record/values turn on a
# scheduled backup of its own database to off-cluster storage, keyed by exactly one identifier — its id),
# and (2) the monitoring-stack GitOps config (a standing backup-freshness alert reconciled by ArgoCD).
# Slice-07 gave us a fleet of isolated tenants; slice-09 made their health visible. This slice makes each
# tenant's DATA durable: every tenant the generator produces gets a scheduled backup to off-cluster
# storage, the backup's own freshness is monitored, and a missed/stale backup ALERTS instead of failing
# silent. Tenant Zero (LPW production) is the first thing backed up — we protect our own data first
# (dogfood). ADR-091 (per-tenant backup keyed off the one id; off-cluster storage; standing BackupStale
# alert); RPO commitment ≤24h (O-3 resolved 2026-07-01). The restore that makes these backups real is
# slice-11.
#
# RECONCILIATION (2026-07-01, resolves the DISCUSS↔DESIGN split): the ADO story text (pg_dump CronJob) and
# ADR-091 (CloudNativePG WAL→S3) disagreed, and the DELIVERED reality is the #5199 chart's plain bundled
# Postgres StatefulSet — no CNPG operator installed. USER DECISION: a scheduled logical dump per tenant DB
# against the EXISTING bundled Postgres → off-cluster object storage (Infomaniak Object Storage, S3-
# compatible). No DB-engine migration in this slice; ADR-091's CNPG WAL path is recorded as DEFERRED/
# superseded for backups. These scenarios are written at the OBSERVABLE level (a fresh off-cluster backup
# per tenant within the recovery-point window; a stale backup alerts; new tenants inherit) so they hold for
# the pg_dump mechanism today and would still hold if CNPG is adopted later.
#
# Executable via:
#   @in-memory      — helm-unittest / render assertions over the GitOps config this slice adds: the
#                     per-tenant scheduled-backup template in the `tenant-runtime` chart (keyed by id,
#                     off-cluster destination, backup-off standalone default) PLUS the `BackupStale` alert
#                     rule in the `fleet-monitoring` chart. CI-runnable, no cluster.
#   @requires_external — live ArgoCD + the real cluster + off-cluster storage: the scheduled backup runs
#                     per tenant, a fresh artifact lands off-cluster within the recovery-point window
#                     (Tenant Zero first), and a deliberately missed backup fires the stale-backup alert.
# RED until the PRIVATE platform repo grows the per-tenant scheduled-backup template + its off-cluster
# storage credential (ESO-materialised) + the `BackupStale` alert rule, and hosted tenant records enable
# backups. IaC/GitOps feature → no .cs/.ts scaffolds. The standalone product + chart standalone defaults
# stay byte-unchanged (backup off-by-default, D0 gate): backup is a HOSTED-only overlay, never a chart
# default flip.
#
# CRUX (what slice-10 must get right): (1) the backup destination MUST be off-cluster — a backup that
# shares the tenant's own failure domain is not a backup. (2) every artifact is keyed by exactly one
# identifier (the tenant id) so one tenant can never read or overwrite another's backup. (3) backup
# FRESHNESS is itself monitored — a silently-missed backup is the failure this slice exists to prevent, so
# staleness beyond the recovery-point window must ALERT. (4) new tenants from the generator inherit backups
# with zero per-tenant setup, and the standalone product renders with no backup at all.

@feature:epic-5306-productization-platform
Feature: Every tenant the generator produces gets a monitored off-cluster backup within a stated recovery point, and a stale backup alerts instead of failing silent
  As the LPW SaaS operator
  I want a scheduled off-cluster backup per tenant with the backup's own freshness monitored
  So that every tenant has a known recovery point and a missed backup can never pass unnoticed

  # --- CI-runnable: render-layer (the per-tenant backup config + the backup-freshness alert) ---

  @US-10 @in-memory @env:tenant-runtime
  Scenario: A hosted tenant renders a scheduled backup of its own data to off-cluster storage
    Given a hosted tenant produced by the generator
    When its runtime is rendered
    Then it has a scheduled backup of its own database
    And the backup's destination is off-cluster storage keyed by the tenant's own identifier

  @US-10 @in-memory @env:tenant-runtime
  Scenario: A newly added tenant inherits backups with no per-tenant setup
    Given a second hosted tenant added as one generator record
    When its runtime is rendered
    Then it has the same scheduled backup as every other tenant
    And no backup configuration was written by hand for this tenant

  @US-10 @in-memory @env:monitoring-stack
  Scenario: A standing alert is defined for a backup that goes stale beyond the recovery-point window
    Given the monitoring stack configuration
    When its alert rules are inspected
    Then there is a standing alert that fires when a tenant's most recent backup is older than the recovery-point window
    And the alert names the tenant whose backup went stale

  @US-10 @in-memory @env:standalone
  Scenario: The standalone product renders with no scheduled backup
    Given the single-container standalone product with default values
    When it is rendered
    Then it has no scheduled backup and no off-cluster backup destination
    And its rendering is unchanged by this slice

  @error @US-10 @in-memory @env:tenant-runtime
  Scenario: A backup destination that is not off-cluster is rejected
    Given a proposed tenant backup whose destination shares the tenant's own failure domain
    When the runtime configuration is validated
    Then validation fails naming the on-cluster destination
    And the configuration is never applied to the tenant

  @error @US-10 @in-memory @env:tenant-runtime
  Scenario: A backup that would write under another tenant's storage key is rejected
    Given a proposed tenant backup whose storage key is not the tenant's own identifier
    When the runtime configuration is validated
    Then validation fails naming the mismatched identifier
    And no tenant's backups can be written under another tenant's key

  @error @US-10 @in-memory @env:monitoring-stack
  Scenario: A backup older than the recovery-point window is flagged, never silently tolerated
    Given the backup-freshness alert rule
    When a tenant's most recent backup age crosses the recovery-point window
    Then the alert is in a firing state for that tenant
    And there is no configuration under which a stale backup stays unreported

  # --- requires the live cluster + ArgoCD + off-cluster storage ---

  @US-10 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero has a fresh backup off-cluster within the recovery-point window
    Given the production tenant Tenant Zero with backups enabled
    When the operator checks Tenant Zero's most recent backup
    Then there is a backup artifact stored off-cluster
    And it is timestamped within the recovery-point window

  @US-10 @real-io @requires_external @env:fleet
  Scenario: Every hosted tenant has a recent off-cluster backup with no gaps
    Given a fleet of hosted tenants with backups enabled
    When the operator surveys the fleet's backups
    Then every tenant has a backup artifact stored off-cluster within the recovery-point window
    And each tenant's artifacts are stored only under its own identifier

  @error @US-10 @real-io @requires_external @env:fleet
  Scenario: A missed backup raises the stale-backup alert rather than passing unnoticed
    Given a fleet of hosted tenants each with a recent backup
    When one tenant's scheduled backup is prevented from completing
    Then that tenant's backup ages past the recovery-point window
    And the stale-backup alert fires naming that tenant
    And the other tenants' backups stay fresh and unflagged

# The per-tenant scheduled-backup template (a logical dump of the bundled Postgres to off-cluster
# Infomaniak Object Storage, keyed by the tenant id, with an ESO-materialised storage credential) and the
# `BackupStale` alert rule live in the PRIVATE platform repo (LetPeopleWork/lighthouse-platform),
# reconciled by ArgoCD like every other platform component. Backup enablement is a HOSTED-only overlay over
# the generator (slice-07) and the monitoring stack (slice-09); it does NOT change the shipped #5199
# chart's standalone defaults (D0 gate). ADR-091's CloudNativePG WAL→S3 path is DEFERRED/superseded for
# backups (reconciliation 2026-07-01) — tenants run the bundled Postgres StatefulSet, so backups are a
# scheduled logical dump. Cross-region replication and point-in-time recovery are OUT. The restore that
# turns these backups into an actual recoverability guarantee is slice-11.
