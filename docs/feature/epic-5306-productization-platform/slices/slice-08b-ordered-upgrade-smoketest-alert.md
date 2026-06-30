# Slice 08b — ordered-upgrade-smoketest-alert

- **ADO story**: #5205 (Automated upgrades — RESCOPE part b)
- **job_id**: `job-saas-operator-upgrade-all-tenants-safely`
- **Band**: Fleet operations

## Learning hypothesis

> If each tenant's upgrade applies schema migrations **before** the new API version begins serving, and a
> **post-sync smoke-test** checks every upgraded tenant's health and **alerts the operator's ops channel on
> failure**, then an unhealthy upgrade is detected and named within minutes — instead of festering silently
> until a customer reports it. If true, the operator can merge a release and trust they will be told fast
> if any tenant came back unhealthy.

## Elevator Pitch

- **Before**: After a roll, Benjamin has no automatic confirmation each tenant is actually healthy on the
  new version; he would have to `curl` each subdomain by hand, and a tenant whose migration and API raced
  could serve errors unnoticed.
- **After**: When a tenant syncs the new version, an ordered upgrade runs the migration first, then the API;
  a post-sync smoke-test hits the tenant's health endpoint and, if it fails, Benjamin **sees an alert in the
  ops channel** ("tenant riverbank unhealthy after upgrade to 26.7.x") within minutes — he never has to poll.
- **Decision enabled**: "Did this release land healthy on every tenant?" — answered automatically, with a
  named failing tenant if not.

## In / Out

- **IN**: deterministic **ordering** so a tenant's schema migration is applied before the new API serves
  (sync-wave / pre-upgrade ordering); a **post-sync smoke-test** per tenant (health/readiness probe of the
  served instance after the roll); an **alert to the operator's ops channel** when the smoke-test fails;
  the alert names the specific tenant and version.
- **OUT**: Renovate / the merge-only flow (slice-08a); the deliberate broken-image **rollback drill**
  (slice-08c); the full fleet observability stack + dashboards (slice-09, separate job
  `job-saas-operator-observe-fleet`) — this slice's alert is upgrade-health-specific, not the general
  fleet dashboard.

## Open design questions handed to DESIGN

- **Migration ordering mechanism.** The app currently runs `Database.Migrate()` on boot, so "migrations
  before API" may already be implicit. DESIGN decides whether the expand-only guarantee + rolling update
  already satisfies the ordering, or whether a dedicated **pre-upgrade migration Job as an earlier ArgoCD
  sync-wave** is warranted. Keep the AC solution-neutral ("migrations applied before the new API serves").
- **Smoke-test surface + alert channel.** Operator hint: an **ArgoCD PostSync hook Job**. The exact alert
  channel (Discord/Slack webhook, email, Alertmanager) is a red card — and note that slice-09's
  Prometheus/Alertmanager stack does not exist yet, so this slice may need a thin standalone alert path
  before the full stack lands. DESIGN/DEVOPS converge.

## Dogfood moment

The ordered-upgrade + smoke-test is proven on Tenant Zero (`lpw`) first: LPW production's own upgrade is
the one whose health gate we rely on before any customer tenant.

## Thin end-to-end path

A tenant syncs a new version → migration runs and completes before the new API pod serves → the new API
becomes Ready → a post-sync smoke-test hits the tenant health endpoint → on success, nothing; on failure,
an alert naming the tenant + version reaches the ops channel within minutes.

## Done = observable

- During a tenant upgrade, the schema migration is applied before the new API version serves traffic
  (no API-before-migration window).
- After a tenant syncs, a post-sync smoke-test runs automatically and records pass/fail per tenant.
- A tenant that comes back unhealthy produces an **alert naming that tenant + version** in the ops channel
  within the detection target; a healthy tenant produces no alert.

## Depends on

- DELIVERED slice-08 substrate (the roll), slice-08a (the trigger to roll) — though 08b can be built
  against a manual `promotedVersion` bump independently. epic-5305 probes/drain; expand-only guard (CI).
