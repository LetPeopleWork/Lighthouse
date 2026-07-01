# Acceptance SSOT — epic-5306-productization-platform, slice-09 fleet-observability (#5206)
# Driving port: (1) the per-hosted-tenant telemetry ENABLEMENT (a tenant's records/values turn on the
# epic-5305 off-by-default signals AND expose the tenant to the fleet scrape with exactly one bounded
# `tenant` attribution), and (2) the monitoring-stack GitOps config (scrape + recording rules +
# per-tenant/fleet/cardinality-budget alert rules + the fleet dashboard) reconciled by ArgoCD. Slice-07
# gave us a fleet of isolated tenants that are each a BLACK BOX; this slice makes the whole fleet's
# health visible in ONE place with per-tenant attribution, flags the one degraded tenant, and fires
# alerts — WITHOUT per-tenant cardinality overwhelming the store and WITHOUT touching the standalone
# product (telemetry stays OFF there — the D0 standalone gate). Tenant Zero (LPW production) is the first
# instance on the dashboard: we watch our own production through the same lens customers get (dogfood).
# ADR-090 (one bounded `tenant` label; drop unbounded labels at scrape; recording rules pre-aggregate
# the fleet dashboard; cardinality-budget alert). Reuses epic-5305 OTel/`/metrics`/Serilog-JSON.
# Executable via:
#   @in-memory      — helm-unittest / render assertions over the monitoring-stack GitOps config (the new
#                     overlay this slice adds: scrape target, recording rules, alert rules, dashboard)
#                     PLUS the per-hosted-tenant telemetry-enablement values. CI-runnable, no cluster.
#   @requires_external — live ArgoCD + the real cluster: deploy the stack, enable telemetry per tenant,
#                     see every tenant on the fleet dashboard, induce a fault on a demo tenant and prove
#                     its tile flags + an alert fires, prove cardinality stays bounded, and prove the
#                     standalone product still emits nothing.
# RED until the PRIVATE platform repo grows the monitoring stack (kube-prometheus-stack app, per-tenant
# scrape with a single `tenant` relabel, recording rules, per-tenant + fleet + cardinality-budget alert
# rules, the fleet dashboard) and the per-hosted-tenant telemetry-enablement values land. IaC/GitOps
# feature → no .cs/.ts scaffolds. The standalone product + chart standalone defaults stay byte-unchanged
# (telemetry off-by-default): the enablement is a HOSTED-only overlay, never a chart default flip.
#
# CRUX (what slice-09 must get right): (1) turning telemetry ON per tenant must attach EXACTLY ONE
# bounded `tenant` attribution and DROP unbounded labels at scrape — otherwise a fleet of tenants blows
# up series cardinality and the store falls over (ADR-090). (2) the fleet dashboard reads PRE-AGGREGATED
# recording-rule series, not raw per-tenant series, so it stays cheap as the fleet grows. (3) the
# standalone product is a DIFFERENT deployment shape and MUST remain telemetry-off — the enablement lives
# in the hosted overlay, so a standalone install renders identically to before this slice.

@feature:epic-5306-productization-platform
Feature: One fleet dashboard shows every tenant's health with per-tenant attribution, flags the sick one, and stays telemetry-off for the standalone product
  As the LPW SaaS operator
  I want per-tenant and fleet health in one stack with the one degraded tenant flagged and alerting
  So that a small team can operate many tenants and honour SLAs without logging into each black box

  # --- CI-runnable: render-layer (the monitoring-stack config + per-tenant telemetry enablement) ---

  @US-09 @in-memory @env:tenant-telemetry
  Scenario: Enabling telemetry for a hosted tenant attributes it by exactly one bounded identifier
    Given a hosted tenant with telemetry enabled
    When its telemetry enablement is rendered
    Then every signal it emits carries exactly one attribution: its own tenant identifier
    And no unbounded per-request attribution is attached

  @US-09 @in-memory @env:monitoring-stack
  Scenario: The fleet dashboard reads pre-aggregated fleet health, not raw per-tenant series
    Given the monitoring stack configuration
    When the fleet dashboard's data sources are inspected
    Then it reads from pre-aggregated fleet-health signals
    And it does not fan out over every tenant's raw per-request series

  @US-09 @in-memory @env:monitoring-stack
  Scenario: The scrape keeps only the bounded tenant attribution and drops the rest
    Given the fleet scrape configuration
    When an incoming signal carries both a tenant identifier and unbounded labels
    Then only the tenant identifier is kept
    And the unbounded labels are dropped before the signal is stored

  @US-09 @in-memory @env:monitoring-stack
  Scenario: A degraded tenant and an unhealthy fleet each have a standing alert defined
    Given the monitoring stack configuration
    When its alert rules are inspected
    Then there is a per-tenant alert that names the degraded tenant
    And there is a fleet-level alert for overall health crossing its threshold

  @US-09 @in-memory @env:standalone
  Scenario: The standalone product renders with telemetry off by default
    Given the single-container standalone product with default values
    When it is rendered
    Then no telemetry is emitted and no scrape target is exposed
    And its rendering is unchanged by this slice

  @error @US-09 @in-memory @env:monitoring-stack
  Scenario: A telemetry attribution that is not bounded is rejected before it can merge
    Given a proposed telemetry enablement that would attach an unbounded attribution
    When the monitoring configuration is validated
    Then validation fails naming the unbounded attribution
    And the configuration is never applied to the fleet

  @error @US-09 @in-memory @env:monitoring-stack
  Scenario: A cardinality-budget alert is defined to catch a series explosion
    Given the monitoring stack configuration
    When its alert rules are inspected
    Then there is a standing alert that fires when the fleet's series count exceeds its budget
    And it fires before the store is overwhelmed

  # --- requires the live cluster + ArgoCD + the monitoring stack ---

  @US-09 @real-io @requires_external @env:fleet
  Scenario: The fleet's health is visible at a glance with per-tenant attribution
    Given telemetry is enabled on every hosted tenant and scraped into one stack
    When the operator opens the fleet dashboard
    Then it shows request, error and latency health for every tenant, attributed per tenant
    And the fleet's overall health is shown as one summary

  @US-09 @real-io @requires_external @env:tenant-zero
  Scenario: Tenant Zero is watched on the same dashboard customers get
    Given the production tenant Tenant Zero
    When the fleet dashboard is opened
    Then Tenant Zero appears as an instance on it, attributed like every other tenant
    And its health is watched through the same lens the fleet uses

  @error @US-09 @real-io @requires_external @env:fleet
  Scenario: A degraded tenant is flagged and a per-tenant alert fires
    Given a live fleet dashboard with every tenant healthy
    When a fault is induced on one demo tenant
    Then that tenant's health is visibly flagged on the dashboard
    And a per-tenant alert fires naming the degraded tenant
    And the other tenants stay healthy and unflagged

  @error @US-09 @real-io @requires_external @env:fleet
  Scenario: Fleet metric cardinality stays bounded as the fleet grows
    Given telemetry enabled across the whole fleet
    When the stored fleet series are inspected
    Then the tenant is the only per-tenant attribution present
    And the series count stays within its budget without the cardinality alert firing

  @US-09 @real-io @requires_external @env:standalone
  Scenario: The standalone product emits no telemetry when run with default values
    Given the single-container standalone product running with default values
    When it is observed over time
    Then it emits no telemetry and exposes no scrape target
    And it is never scraped into the fleet stack

# The monitoring stack (kube-prometheus-stack: scrape + recording rules + per-tenant/fleet/cardinality-
# budget alert rules + the fleet dashboard) and the per-hosted-tenant telemetry-enablement values live in
# the PRIVATE platform repo (LetPeopleWork/lighthouse-platform), reconciled by ArgoCD like every other
# platform component. The enablement REUSES epic-5305's off-by-default OTel/`/metrics`/Serilog-JSON — this
# slice turns them ON per hosted tenant and scrapes them with one bounded `tenant` relabel (ADR-090); it
# does NOT change the shipped #5199 chart's standalone defaults (D0 gate). A customer-facing status page,
# long-term metrics warehousing, and the standalone self-hoster are OUT. Per-tenant backups are slice-10.
