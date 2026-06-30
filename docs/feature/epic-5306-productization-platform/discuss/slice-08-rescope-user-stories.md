<!-- markdownlint-disable MD024 -->
# User Stories — slice-08 RESCOPE (ADO #5205 Automated Upgrades)

> 6 stories across 3 thin slices (08a/08b/08c). Every story traces to
> **`job_id: job-saas-operator-upgrade-all-tenants-safely`**. Operator in all examples: **Benjamin**
> (LPW maintainer/operator). Tenant Zero = `lpw` (real LPW production). Demo/throwaway tenants:
> `riverbank`, `canarytest`. Versions: current LIVE image `26.6.21.1` / chart `0.1.4`; new release in
> examples `26.7.0` / chart `0.1.5`. Private gitops repo: `LetPeopleWork/lighthouse-platform`.
> Every story is operator-observable (NOT `@infrastructure`) and carries the 3-line Elevator Pitch.

## System Constraints (cross-cutting, all 6 stories)

- **Standalone gate**: all change is PRIVATE-repo GitOps + CI; the public chart stays byte-unchanged
  (image tag already defaults to `Chart.appVersion`, ADR-083).
- **Expand-only migrations**: a destructive (Drop/Rename) migration is blocked by `ExpandOnlyMigrationGuard`
  in `dotnet test` (the release gate) before any tenant can roll.
- **Extend existing GitHub Actions**, trunk-based; no parallel workflow.
- **Tenant Zero is the permanent canary**: it takes every release first; production is never the broken-image
  target (08c uses a throwaway tenant).
- **Built ON the DELIVERED substrate**: the matrix ApplicationSet + `promotedVersion`/`chartVersion` canary
  override do the actual roll; these stories add the AUTOMATION around it.

---

# Slice 08a — Renovate merge-only release

## US-08a-1 — Renovate raises a version-bump PR

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08a

### Elevator Pitch
- **Before**: Benjamin only learns a new Lighthouse release exists if he goes looking; to roll it he hand-edits `promotedVersion` in the gitops repo — no review surface, no signal he is behind.
- **After**: Benjamin opens `LetPeopleWork/lighthouse-platform` and **sees a Renovate PR** already raised — "chore(deps): update lighthouse 26.6.21.1 → 26.7.0" — bumping the fleet `promotedVersion`, with the changelog linked for review.
- **Decision enabled**: "Is there a new release, and do I want to ship it?" — answered by a reviewable PR he never had to create.

### Problem
Benjamin is the LPW SaaS operator. Today nothing tells him a new Lighthouse image (or a new chart-dependency
version like CNPG/Postgres or cert-manager) has shipped; he has to notice it himself and then hand-edit YAML
to roll it. He finds it painful that releases can silently pile up and that the version bump has no review trail.

### Who
- LPW SaaS operator | maintaining the fleet's version on the gitops repo | wants releases surfaced as reviewable PRs, not self-discovered chores.

### Domain Examples
1. **Happy path (image)** — Lighthouse `26.7.0` is published to GHCR; within one Renovate scan interval a PR appears in `LetPeopleWork/lighthouse-platform` bumping the fleet `promotedVersion` 0.1.4 → 0.1.5, titled with the version delta and linking the release notes.
2. **Chart dependency** — a new CNPG operator version is released; Renovate raises a separate PR bumping that pinned dependency, kept distinct from the Lighthouse app bump so Benjamin can review them independently.
3. **No-op** — no new versions exist this week; Renovate raises no PR and the fleet stays on 0.1.4 (no noise).

### UAT Scenarios (BDD)
```gherkin
Scenario: A new Lighthouse image raises a fleet-bump PR
  Given Renovate watches the published Lighthouse image tag on the gitops repo
  When Lighthouse 26.7.0 is published to GHCR
  Then within one scan interval a PR is opened in LetPeopleWork/lighthouse-platform
  And the PR bumps the fleet promotedVersion to the new release
  And the PR title names the version delta and links the changelog

Scenario: A new chart-dependency version raises its own PR
  Given Renovate watches the tracked chart/platform-component dependency versions
  When a new CNPG operator version is released
  Then Renovate opens a separate PR bumping that dependency
  And it is distinct from the Lighthouse application version PR

Scenario: No new versions produces no PR
  Given the fleet is on the latest released versions
  When Renovate runs its scan
  Then no version-bump PR is opened
```

### Acceptance Criteria
- [ ] Renovate is configured on `LetPeopleWork/lighthouse-platform` and watches the Lighthouse image tag (driving port: a Renovate-raised PR).
- [ ] A newly published image tag raises a PR bumping the fleet `promotedVersion` within one scan interval.
- [ ] Tracked chart/platform-component dependency versions each raise their own PR.
- [ ] When nothing new exists, no PR is raised (no false churn).

### Technical Notes
Renovate hosting + exact watch scope are open (O-08-4). The PR bumps `promotedVersion` (the DELIVERED
substrate's fleet default); merging it is US-08a-3.

---

## US-08a-2 — Tenant Zero auto-canaries the latest release, hands-off

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08a

### Elevator Pitch
- **Before**: To canary a release on Tenant Zero, Benjamin manually adds a `chartVersion` override to the `lpw` record and pushes — production only gets the new version if he remembers to do it.
- **After**: A new release lands and **Tenant Zero (`lpw`) is already running it** — `argocd app list` shows `tenant-lpw lighthouse@0.1.5 Synced/Healthy` while the rest of the fleet is still on 0.1.4 — with **no merge or edit from Benjamin**.
- **Decision enabled**: "Is the new version safe on our own production?" — answered automatically, on real data, before any customer tenant moves.

### Problem
Benjamin wants Tenant Zero to always take a new release first so he feels any upgrade pain on LPW's own
production before a customer does. Today that canary step is manual, so it gets skipped under load and the
"Tenant Zero first" promise is only as reliable as his memory.

### Who
- LPW SaaS operator | wants Tenant Zero to be a hands-off permanent canary | values dogfooding on production before customers.

### Domain Examples
1. **Happy path** — `26.7.0` publishes; the auto-canary mechanism moves `tenant-lpw` to chart 0.1.5 with no human action; `lpw.lighthouse.letpeople.work` serves HTTP 200 on the new version while `riverbank` stays on 0.1.4.
2. **Canary stays ahead** — until the fleet PR is merged, `lpw` is on 0.1.5 and every other tenant is on 0.1.4 — the canary is genuinely ahead of the fleet, not in lockstep.
3. **Unhealthy canary** — `26.7.0` is bad; `lpw` shows the regression first (e.g. health 503); because nothing auto-promotes, no customer tenant is moved — the human gate (US-08a-3) holds.

### UAT Scenarios (BDD)
```gherkin
Scenario: Tenant Zero runs the new version with no human action
  Given a new Lighthouse version 26.7.0 has been published
  When the auto-canary mechanism applies it to Tenant Zero
  Then tenant-lpw serves the new version
  And no merge or manual edit was performed by the operator

Scenario: The canary is ahead of the fleet until promotion
  Given Tenant Zero has auto-canaried 26.7.0
  And the fleet promotion PR is not yet merged
  When Benjamin runs "argocd app list"
  Then tenant-lpw shows the new revision
  And every other tenant shows the prior revision

Scenario: A bad canary is not auto-promoted
  Given Tenant Zero has auto-canaried 26.7.0
  And Tenant Zero is unhealthy on that version
  Then no other tenant is moved to 26.7.0 automatically
  And the regression is visible on Tenant Zero first
```

### Acceptance Criteria
- [ ] On a new release, Tenant Zero takes the new version with zero operator actions (driving port: the resolved `tenant-lpw` `targetRevision`).
- [ ] Tenant Zero's version is ahead of the fleet until the fleet PR is merged.
- [ ] Tenant Zero serves HTTP 200 on the new version when the release is healthy.
- [ ] No mechanism auto-promotes the fleet off the canary; promotion is the human merge (US-08a-3).

### Technical Notes
**Auto-canary mechanism is the headline open question O-08-1** — capture intent ("TZ takes every release
first, hands-off"); DESIGN picks the mechanism (Renovate auto-merge of a TZ-only PR / mutable `latest` tag
[known gotcha] / scoped automerge rule). Do not lock here.

---

## US-08a-3 — Promote the whole fleet by merging one PR

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08a

### Elevator Pitch
- **Before**: Rolling the fleet means Benjamin hand-edits `promotedVersion`, commits and pushes — a multi-step chore with no review gate.
- **After**: Benjamin reviews a healthy Tenant Zero, clicks **Merge** on the one Renovate PR, and `argocd app list` shows every tenant converge on `lighthouse@0.1.5` — that single merge is the only action.
- **Decision enabled**: "Ship this release to all tenants now?" — one reviewed click, and the fleet rolls itself.

### Problem
Benjamin wants releasing to the whole fleet to be a single reviewed action gated on a healthy canary, not a
hand-edit. Today there is no one-click promote and no review surface tying the roll to the canary's health.

### Who
- LPW SaaS operator | promoting a reviewed release to every tenant | wants a one-click, canary-gated fleet roll.

### Domain Examples
1. **Happy path** — Tenant Zero is healthy on 0.1.5; Benjamin merges PR #142; `promotedVersion` bumps 0.1.4 → 0.1.5; `riverbank` and every other non-canary tenant roll to 0.1.5; `argocd app list` shows all Synced on the new revision.
2. **Hold the merge** — Tenant Zero looks suspect; Benjamin does NOT merge; the fleet stays on 0.1.4; nothing rolled.
3. **One action only** — after the merge, Benjamin performs no further step; the substrate (matrix appset) does the rest end-to-end (exit criterion met).

### UAT Scenarios (BDD)
```gherkin
Scenario: Merging one PR rolls the whole fleet
  Given Tenant Zero is healthy on the new version
  And a Renovate PR bumping the fleet promotedVersion is open
  When Benjamin merges that one PR
  Then every non-canary tenant rolls to the new version
  And "argocd app list" shows all tenants Synced on the new revision
  And no further manual step is required

Scenario: Not merging leaves the fleet on the prior version
  Given the fleet promotion PR is open
  When Benjamin chooses not to merge it
  Then every non-canary tenant remains on the prior version

Scenario: Releasing requires exactly one operator action
  Given a healthy canary and an open fleet PR
  When Benjamin releases to the fleet
  Then the only action he performs is merging one PR
```

### Acceptance Criteria
- [ ] Merging the one Renovate PR bumps `promotedVersion` and rolls every non-canary tenant (driving port: the git merge → ArgoCD).
- [ ] After the merge, `argocd app list` shows all tenants Synced on the new revision (convergence).
- [ ] Not merging leaves the fleet unchanged.
- [ ] Releasing to the fleet requires exactly one operator action (the merge) — the #5205 exit criterion.

### Technical Notes
Consumes the DELIVERED substrate (`promotedVersion` default). Convergence equality is the
`chart-version` shared-artifact check. Depends on US-08a-1 (the PR) and US-08a-2 (the canary gate).

---

# Slice 08b — Ordered upgrade + post-sync smoke-test + alert

## US-08b-1 — Migrations run before the API upgrade

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08b

### Elevator Pitch
- **Before**: When a tenant rolls, there is no explicit ordering guarantee that its schema migration completes before the new API serves — a migration/API race could surface errors.
- **After**: During a tenant's upgrade the schema migration is applied first; the new API only begins serving once the schema is ready, and `curl` against the tenant returns continuous HTTP 200 through the roll.
- **Decision enabled**: "Can the new API ever serve against an un-migrated schema?" — no, by construction.

### Problem
Benjamin needs each tenant's upgrade to apply schema changes before the new API version serves, so a rolling
update never exposes a new API against an old schema. Today ordering is implicit in on-boot
`Database.Migrate()` and not asserted as a property of the upgrade.

### Who
- LPW SaaS operator | rolling versions across tenants | wants migration-before-API ordering guaranteed, not incidental.

### Domain Examples
1. **Happy path** — `riverbank` upgrades 0.1.4 → 0.1.5 with an additive migration; the migration applies, then the new API serves; polling `riverbank.lighthouse.letpeople.work` returns 200 throughout.
2. **Additive migration cleared** — the release's migration is expand-only; the pre-flight passes and the ordered upgrade proceeds.
3. **Destructive migration blocked** — a release carries a Drop/Rename; `ExpandOnlyMigrationGuard` fails the release build (`dotnet test`) and the version never reaches a tenant — ordering never has to cope with a destructive change.

### UAT Scenarios (BDD)
```gherkin
Scenario: Schema migration is applied before the new API serves
  Given a tenant is upgrading to a new version with an additive migration
  When the upgrade rolls
  Then the schema migration is applied before the new API version serves traffic
  And the tenant returns continuous successful responses through the roll

Scenario: A destructive migration is blocked before any tenant rolls
  Given a candidate release carries a Drop or Rename migration after the baseline
  When the release build runs
  Then ExpandOnlyMigrationGuard fails the build
  And no tenant is offered the destructive version

Scenario: An additive-only migration is cleared to roll
  Given a candidate release carries only additive migrations
  When the expand-only pre-flight runs
  Then it passes and the ordered upgrade proceeds
```

### Acceptance Criteria
- [ ] During a tenant upgrade, the schema migration is applied before the new API version serves (driving port: the upgrade ordering / sync-wave).
- [ ] A tenant under upgrade returns continuous successful responses (no API-before-migration window).
- [ ] A destructive migration is blocked by `ExpandOnlyMigrationGuard` in the release build before any tenant rolls.
- [ ] An additive-only migration passes the pre-flight and is cleared.

### Technical Notes
**Ordering mechanism is open (O-08-2)** — DESIGN decides whether on-boot `Database.Migrate()` + expand-only +
rolling update already satisfies the ordering or whether a dedicated pre-upgrade migration Job (earlier
ArgoCD sync-wave) is warranted. AC stays solution-neutral. Reuses the epic-5305 guard (no new guard).

---

## US-08b-2 — A post-sync smoke-test alerts when a tenant is unhealthy after upgrade

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08b

### Elevator Pitch
- **Before**: After a roll, Benjamin has no automatic confirmation each tenant is healthy on the new version; a sick tenant could serve errors unnoticed until a customer reports it.
- **After**: When a tenant syncs the new version a post-sync smoke-test hits its health endpoint; if it fails, Benjamin **sees an alert in the ops channel** — "tenant riverbank unhealthy after upgrade to 26.7.0 (health 503)" — within minutes, naming the tenant and version.
- **Decision enabled**: "Did this release land healthy on every tenant?" — answered automatically, with the failing tenant named.

### Problem
Benjamin needs to know fast when an upgraded tenant comes back unhealthy, without polling each subdomain by
hand. Today there is no post-upgrade health gate and no alert, so a bad upgrade on one tenant among many can
fester silently.

### Who
- LPW SaaS operator | operating many tenants | wants to be told, fast and by tenant name, when an upgrade leaves a tenant unhealthy.

### Domain Examples
1. **Healthy → silence** — `riverbank` upgrades to 0.1.5; the post-sync smoke-test gets HTTP 200 on its health endpoint; no alert fires (no noise on success).
2. **Unhealthy → alert** — `riverbank` comes back returning 503; within minutes an alert lands in the ops channel naming `tenant=riverbank version=26.7.0 health=503`.
3. **Per-tenant attribution** — `acme` is fine but `riverbank` is sick; the alert names only `riverbank`, so Benjamin acts on the right tenant.

### UAT Scenarios (BDD)
```gherkin
Scenario: A healthy upgraded tenant produces no alert
  Given a tenant has synced a new version
  And its health endpoint returns healthy
  When the post-sync smoke-test runs
  Then no alert is raised

Scenario: An unhealthy upgraded tenant raises a named alert
  Given a tenant has synced a new version
  And its health endpoint returns unhealthy
  When the post-sync smoke-test runs
  Then an alert reaches the ops channel within the detection target
  And the alert names the tenant and the version

Scenario: Only the sick tenant is named
  Given two tenants upgrade and one returns unhealthy
  When the post-sync smoke-tests run
  Then an alert names only the unhealthy tenant
```

### Acceptance Criteria
- [ ] After a tenant syncs a new version, a post-sync smoke-test checks its served health endpoint (driving port: the ArgoCD PostSync hook / smoke-test job).
- [ ] An unhealthy tenant produces an alert naming the tenant + version within the detection target (driving port: the ops alert channel).
- [ ] A healthy tenant produces no alert.
- [ ] Alerts are per-tenant attributed (only the sick tenant is named).

### Technical Notes
**Smoke-test surface + alert channel are open (O-08-3)**; slice-09's Prometheus/Alertmanager stack does NOT
exist yet, so a thin standalone alert path may be needed first. Health endpoint = the epic-5305 chart probe.

---

# Slice 08c — Broken-image rollback drill

## US-08c-1 — Rehearse rollback by catching and recovering a broken release

- **job_id**: `job-saas-operator-upgrade-all-tenants-safely` · **slice**: 08c

### Elevator Pitch
- **Before**: The rollback path (git revert, additive-only migrations) is believed to work but has never been exercised against a genuinely broken release — "safe upgrades" is an unproven promise.
- **After**: Benjamin pushes a deliberately broken image to a throwaway tenant, **watches the smoke-test alert fire**, runs the documented rollback (`git revert` the bump), and sees the tenant return to HTTP 200 on the prior version — the detect→recover time recorded as a rehearsed runbook.
- **Decision enabled**: "If a release is bad, will we catch it and recover within our target?" — proven by a timed, repeatable drill, not assumed.

### Problem
Benjamin needs the rollback path proven against a real broken release so he can state a recovery runbook with
evidence. Today the path is theoretical; an untested rollback is no rollback.

### Who
- LPW SaaS operator | rehearsing recovery | wants the broken-image detect→rollback path tested on a throwaway tenant, never on production.

### Domain Examples
1. **Happy path (drill)** — Benjamin provisions throwaway tenant `canarytest`, pushes a deliberately broken image; the 08b smoke-test fails and alerts; he `git revert`s the bump; ArgoCD restores the prior revision; `canarytest` serves HTTP 200 again; detect→recover measured at ~8 min, within target.
2. **Detection works** — the broken release is caught by the smoke-test (alert fires), not silently served.
3. **Production untouched** — throughout the drill, Tenant Zero (`lpw`) stays Healthy on its current version and serves 200 (mirrors the DELIVERED slice-08 throwaway-tenant proof).

### UAT Scenarios (BDD)
```gherkin
Scenario: A deliberately broken release is detected
  Given a deliberately broken image is pushed to a throwaway canary tenant
  When the post-sync smoke-test runs
  Then the smoke-test fails and an alert fires naming the tenant

Scenario: Rollback restores the prior healthy revision
  Given a throwaway tenant is unhealthy on a broken release
  When Benjamin reverts the version bump in git
  Then ArgoCD restores the prior revision
  And the tenant returns to HTTP 200
  And the detect-to-recover time is within the rollback target

Scenario: Production is untouched by the drill
  Given the broken-image drill runs on a throwaway tenant
  Then Tenant Zero remains healthy on its current version throughout
```

### Acceptance Criteria
- [ ] A deliberately broken release on a throwaway tenant is detected by the smoke-test (alert fires) — driving port: the smoke-test + alert channel.
- [ ] `git revert` of the bump restores the prior healthy revision; the tenant returns to HTTP 200 (driving port: the git revert → ArgoCD).
- [ ] The detect→recover time is measured and within the rollback target; a rehearsed runbook is recorded.
- [ ] Tenant Zero (production) is untouched throughout.

### Technical Notes
Operator-initiated rollback only; auto-rollback on smoke-test failure is flagged (O-08-5), not built.
Consumes 08b (smoke-test/alert) + the DELIVERED substrate (git-revert path) + slice-07 throwaway provisioning.

---

## Definition of Ready — validation

| Story | Problem (domain) | Persona specific | 3+ real examples | UAT 3–7 GWT | AC from UAT | Right-sized | Tech notes | Deps tracked | KPIs | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| US-08a-1 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-08a-2 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS (O-08-1) | PASS | PASS | READY |
| US-08a-3 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-08b-1 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS (O-08-2) | PASS | PASS | READY |
| US-08b-2 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS (O-08-3) | PASS | PASS | READY |
| US-08c-1 | PASS | PASS | PASS (3) | PASS (3) | PASS | PASS | PASS (O-08-5) | PASS | PASS | READY |

> Open questions O-08-1..O-08-5 are **mechanism** decisions owned by DESIGN; they do NOT block DoR because
> the requirements stay solution-neutral (intent + observable outcome captured). They are tracked dependencies,
> not unresolved requirements. KPIs in `slice-08-rescope-outcome-kpis.md`.
