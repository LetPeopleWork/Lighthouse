# Pause the Infomaniak platform deployment

Infomaniak's free Public Cloud credit has run out, and the hosted platform has no paying tenant.
Stop running it on Infomaniak and stop paying for it there. Keep the `lighthouse-platform` repo and its
automations exactly as they are, so the platform can be rebuilt later from git plus a saved export.

Repo affected: `LetPeopleWork/lighthouse-platform` (operational change, almost no code). This repo holds
only the wave record.

## Wave: DISCUSS / [REF] Persona ID

`platform-operator` (SaaS-operator flavour): the maintainer who operates LetPeopleWork's hosted
Lighthouse on Kubernetes.

## Wave: DISCUSS / [REF] JTBD one-liner

`job-saas-operator-provision-substrate`: *"…so I can create, reproduce or **tear down** the platform
substrate as reviewable code…"* This feature exercises the tear-down half of an existing job. No new job
is added to `docs/product/jobs.yaml`.

## Wave: DISCUSS / [REF] Locked decisions

- **D1: Decommission, don't freeze.** Destroy the compute. Turning off ArgoCD auto-sync would leave the
  cluster billing, so it does not meet the goal. *(User: "free tier ends, so far we don't need it
  anymore.")*
- **D2: Nothing is left on Infomaniak.** Export first, then delete both object-storage buckets
  (`lighthouse-tfstate` and the tenant DB backups bucket). The export lives outside Infomaniak, in the
  operator's password manager or local storage. *(User choice, 2026-09-26.)*
- **D3: The repo and its automations stay untouched.** `validate-tenants.yml`,
  `dependabot-automerge.yaml` and `renovate.json` keep running. Renovate keeps bumping versions on
  `main`, and nothing pulls those bumps into a cluster any more. When the platform comes back, ArgoCD
  syncs whatever `main` holds at that point. Fleet-promote PRs are never auto-merged, so they will
  collect as open PRs. That is accepted.
- **D4: The export must be proven restorable before anything is destroyed.** The OpenBao secrets live on
  an in-cluster PVC, so they die with the cluster. The Lighthouse encryption key matters most: without
  it, the connector credentials stored in a restored lpw database cannot be read. Destroying the cluster
  is the one step that cannot be undone, so it waits until a restore of the export has worked end to end.
- **D5: Teardown order is set by resources OpenTofu does not own.**
  1. Delete the ingress-nginx `LoadBalancer` Service. The cloud controller created the Octavia load
     balancer, not OpenTofu, so `tofu destroy` would leave it behind and still billing.
  2. Delete tenant and platform PVCs. The Cinder volumes may outlive the cluster.
  3. `tofu destroy` the cluster.
  4. Delete the buckets.
  5. Remove the wildcard DNS record.
- **D6: Remove the wildcard DNS record, don't leave it dangling.** `*.lighthouse.letpeople.work` is a
  hand-set A record pointing at the load balancer's IP. Once the load balancer is gone, that IP goes back
  into Infomaniak's pool. Whoever gets it next would receive all `*.lighthouse.letpeople.work` traffic
  (a subdomain takeover). Remove **only** that wildcard A record. `lighthouse.letpeople.work` is also the
  website's Mailgun sending domain, and its SPF, DKIM, MX and tracking records live in the same zone and
  must stay.
- **D7: The Jira app goes dark, and that is accepted.** Its `manifest.yml` points at
  `lpw.lighthouse.letpeople.work`. *(User: "nobody is interested in this thus far, so not a biggie.")*
  The manifest and the app are not changed.
- **D8: The repo says it is paused.** Add a banner to the `lighthouse-platform` README and a respin
  runbook. The respin runbook links the existing bring-up docs and restore steps rather than copying
  them. Without the banner, a later reader would take the docs as a description of a live cluster.

## Wave: DISCUSS / [REF] User stories

### US-1: A restorable export of everything the platform holds

*job_id: job-saas-operator-provision-substrate*

As the platform operator, I want a complete export of Tenant Zero's data and every platform secret,
saved outside Infomaniak and proven restorable, so that deleting the cluster costs me nothing I would
need to bring it back.

#### Elevator Pitch
Before: lpw's database and every OpenBao secret exist only inside the Infomaniak cluster, so destroying
it would lose them for good.
After: run the lpw DB restore against a local Postgres plus the exported encryption key, then start
Lighthouse → it shows lpw's Portfolios and Teams, and a connector's stored credential decrypts (the
connection test gets past authentication).
Decision enabled: the cluster can be destroyed safely.

#### Acceptance criteria
- AC-1.1: A fresh logical dump of the lpw database is taken after the last write to lpw and saved
  outside Infomaniak.
- AC-1.2: Every OpenBao KV path that an ExternalSecret references is exported: DB, OIDC, encryption,
  backup keypair, Grafana admin, and any others found by listing the `ExternalSecret` objects. The OpenBao
  unseal keys and root token are exported too. All of it goes into the password manager, and none of it
  goes into git.
- AC-1.3: The dump restored into a local Postgres, with a local Lighthouse started on the exported
  encryption key, shows lpw's Portfolios and Teams. At least one stored work-tracking connection gets
  past authentication (no decryption failure).
- AC-1.4: The list of exported items matches the full list of `ExternalSecret` objects in the cluster.
  Nothing referenced goes unexported.

### US-2: Nothing is running or billing on Infomaniak

*job_id: job-saas-operator-provision-substrate*

As the platform operator, I want every Infomaniak resource the platform created to be gone, so that the
end of the free credit costs nothing.

#### Elevator Pitch
Before: the cluster, node pool, load balancer, volumes and buckets keep billing after the credit ends.
After: run `openstack server list`, `openstack loadbalancer list`, `openstack volume list` and
`openstack container list` in the project, and check the Manager's Kubernetes page → all empty.
`dig lpw.lighthouse.letpeople.work` → NXDOMAIN.
Decision enabled: the Infomaniak Public Cloud project (or its payment method) can be closed or left
idle at zero cost.

#### Acceptance criteria
- AC-2.1: US-1 is done before any destroy step begins.
- AC-2.2: The Octavia load balancer and every PVC-backed Cinder volume are deleted before
  `tofu destroy`, and the `openstack` list commands above return nothing that belongs to the platform.
- AC-2.3: `tofu destroy` finishes cleanly for the managed cluster (or reports nothing to destroy), and
  the OpenTofu state is empty afterwards. The state bucket goes last.
- AC-2.4: The wildcard A record `*.lighthouse.letpeople.work` is removed from the `letpeople.work`
  zone, and `dig lpw.lighthouse.letpeople.work` returns NXDOMAIN. The Mailgun records (SPF/DKIM/MX/
  tracking CNAME) are untouched, and a test mail from the website's assessment flow still sends.
- AC-2.5: In the Infomaniak Manager, the Public Cloud project shows no active billable resources. This
  is checked once right after teardown and again a day later, after the first billing tick.

### US-3: The repo says it is paused and how to bring it back

*job_id: job-saas-operator-provision-substrate*

As the platform operator, coming back months from now, I want the repo to say plainly that nothing is
deployed and how to bring it back, so that I don't trust docs describing a cluster that is gone.

#### Elevator Pitch
Before: the README and docs describe a live cluster, a live lpw tenant and live DNS.
After: open `lighthouse-platform/README.md` → the first thing on the page is a "Paused since 2026-09"
banner linking `docs/respin.md`, which lists: substrate bring-up → bootstrap → re-seed OpenBao from the
export → restore lpw → re-add DNS.
Decision enabled: whether a respin fits the time available, and where to start.

#### Acceptance criteria
- AC-3.1: The README banner states the pause date, the reason (free credit ended, no paying tenant) and
  that the automations still run while nothing deploys.
- AC-3.2: `docs/respin.md` puts the respin steps in order and links the existing pages for each step
  (`infomaniak-setup.md`, `operator-bootstrap.md`, `secrets-eso-openbao.md`, `backup-and-recovery.md`,
  `wildcard-dns-routing.md`) rather than copying them. It names where the US-1 export lives: the password
  manager entry name, not its contents.
- AC-3.3: No file under `.github/`, `gitops/`, `infra/` or `renovate.json` changes (D3).
- AC-3.4: `validate-tenants` stays green on `main` after the doc commit.

## Wave: DISCUSS / [REF] Definition of Done

1. US-1 export saved outside Infomaniak, and the restore proven locally (AC-1.3).
2. Every platform resource on Infomaniak deleted (AC-2.2, AC-2.3).
3. Wildcard DNS removed, NXDOMAIN verified (AC-2.4).
4. No active billable resources, re-checked after one day (AC-2.5).
5. README banner and `docs/respin.md` merged to `lighthouse-platform` `main` (US-3).
6. `.github/`, `gitops/`, `infra/` and `renovate.json` byte-unchanged (AC-3.3).
7. `validate-tenants` green on `main` (AC-3.4).
8. No secret material in git (`git grep` has no hits for any exported value).
9. This feature-delta records the outcome, with what was done and when.

## Wave: DISCUSS / [REF] Out of scope

- Changing or unpublishing the Jira app. It keeps pointing at a host that is gone (D7).
- Moving to another provider (Hetzner, Oracle, …). That is the "figure out next steps" decision, to be
  made later.
- Pausing Renovate or any GitHub workflow (D3).
- Closing the Infomaniak account. The operator decides that after AC-2.5.
- Any change to the Lighthouse product, its chart or its CI. `ci_chart.yml` keeps publishing charts.

## Wave: DISCUSS / [REF] WS strategy

None. This is a brownfield teardown of an existing platform, so there is no walking skeleton. Slice 01
(export and prove the restore) is the precursor that makes slice 02 safe.

## Wave: DISCUSS / [REF] Driving ports

The operator's shell: `kubectl` against the Infomaniak kubeconfig, `bao` for the OpenBao export,
`pg_dump`/`pg_restore`, `tofu` in `infra/substrate`, the `openstack` CLI, and the DNS registrar's zone
editor. The Infomaniak Manager is used for the billing check.

## Wave: DISCUSS / [REF] Pre-requisites

- The cluster is reachable and **OpenBao is unsealed**. OpenBao has re-sealed silently before, so
  unseal it first; the secrets cannot be read while it is sealed.
- The operator holds: the kubeconfig, `INFOMANIAK_TOKEN`, the state-bucket S3 keypair, the OpenBao
  root token or unseal keys, and registrar access for `letpeople.work`.
- A local Postgres and a Lighthouse checkout for the AC-1.3 restore.

## Wave: DISCUSS / [REF] Story map and slices

| Slice | Stories | Learning hypothesis |
|---|---|---|
| 01 export and prove the restore | US-1 | Disproves "the lpw dump plus the exported OpenBao secrets are enough to rebuild Tenant Zero" if the local restore cannot decrypt a stored credential, or if an ExternalSecret path turns out to have been missed. |
| 02 decommission and mark paused | US-2, US-3 | Disproves "`tofu destroy` alone releases every billable resource" if an orphaned load balancer, volume or bucket still shows up in the listings or the bill afterwards. |

Order: 01 then 02, with a hard dependency (D4). Each slice fits comfortably within half a day.
Carpaccio check: both slices use the real production data (lpw), and each slice has an outcome the
operator can see. Slice 02 bundles the docs with the teardown, so no slice is docs-only.

## Wave: DISCUSS / [REF] Outcome KPIs

- Infomaniak Public Cloud spend: **0.00 CHF/day** from the day after teardown. Measured with the
  Manager billing view, checked after 1 day and again after 30 days.
- Restorability: **1** successful local restore with credentials decrypting, before teardown
  (AC-1.3).
- Dangling DNS: **0** wildcard answers under `lighthouse.letpeople.work` (`dig lpw.` and
  `dig nonexistent.` both return NXDOMAIN).
- Automation continuity: `validate-tenants` and Renovate still run on `lighthouse-platform` `main`
  **30 days later**, with **0** changes needed to keep them green.

## Wave: DISCUSS / [REF] Project checklist

- RBAC impact: N/A. No Lighthouse product code changes.
- Lighthouse-Clients CLI/MCP versioning: N/A. No API or MCP contract changes. The lpw MCP endpoint goes
  dark, and D7 covers that.
- Website marketing surface: N/A. The website does not advertise hosted Lighthouse or link to
  `lpw.lighthouse.letpeople.work` (checked by grepping the website repo on 2026-09-26). The website
  shares the DNS zone only through Mailgun (D6).
- Terminology: N/A. No user-facing copy in the product.

## Wave: DISCUSS / [REF] DoR validation

| # | Item | Evidence |
|---|---|---|
| 1 | Clear problem statement | Free credit ended, no paying tenant, stop paying (header, D1) |
| 2 | Persona identified | `platform-operator` |
| 3 | Job traced | every story traces to `job-saas-operator-provision-substrate` |
| 4 | ACs testable | every AC names a command or an observable result |
| 5 | Right-sized | 3 stories, 2 slices, each ≤ ½ day |
| 6 | Dependencies known | Pre-requisites section, D4 ordering |
| 7 | Risks named | orphaned load balancer and volumes (D5), dangling DNS (D6), OpenBao data loss (D4) |
| 8 | Out of scope explicit | Out-of-scope section |
| 9 | Outcome measurable | KPIs with numeric targets |

## Wave: DISCUSS / [REF] Scope assessment

PASS: 3 stories, 1 bounded context (the platform), about 1 day of operator time.

---

## Wave: DEVOPS / [REF] Skipped waves

DESIGN and DISTILL were skipped at the user's explicit request (2026-09-26). This is a one-off manual
teardown with no code to design and no automated acceptance suite. The DISCUSS ACs are the checks
DELIVER runs by hand.

## Wave: DEVOPS / [REF] Environment matrix

See `environments.yaml`. There are three surfaces: the Infomaniak dc4 project (end state: empty), the
`letpeople.work` DNS zone (end state: wildcard record gone, Mailgun records untouched), and the operator
workstation (end state: lpw restored locally).

## Wave: DEVOPS / [REF] CI/CD pipeline outline

No pipeline changes. `validate-tenants` (PR and push to `main`) and the Renovate auto-merge
`workflow_run` never talk to the cluster. They only lint, validate and merge, so they stay green with
the cluster gone. ArgoCD, the component that deployed, goes away with the cluster, so a merge to `main`
now updates git and nothing else. The only commit this feature makes to `lighthouse-platform` is the
README banner and `docs/respin.md` (US-3), and it is expected to leave `validate-tenants` green
(AC-3.4).

## Wave: DEVOPS / [REF] Deployment strategy

Ordered teardown with a hard gate, no rollback past the gate. Everything up to and including slice 01 is
read-only. Slice 02 begins with deleting the load balancer, and from that point the rollback is a full
respin (`docs/respin.md`) from the exported secrets and dump. That is why AC-1.3 must pass first.

## Wave: DEVOPS / [REF] Monitoring contracts

| KPI | Instrument | When |
|---|---|---|
| Spend 0.00 CHF/day | Infomaniak Manager, Public Cloud billing view | teardown day +1, +30 |
| Restorability | Operator runs the local restore (AC-1.3) | before slice 02 |
| Dangling DNS 0 | `dig lpw.lighthouse.letpeople.work` and `dig nonexistent.lighthouse.letpeople.work` return NXDOMAIN | after slice 02 |
| Automation continuity | `gh run list -R LetPeopleWork/lighthouse-platform` shows green `validate-tenants` runs | +30 days |

The +30 checks are manual. No cloud reminder is scheduled.

## Wave: DEVOPS / [REF] Observability stack

The in-cluster kube-prometheus-stack, Alertmanager and fleet-monitoring charts are removed along with
the cluster. The repo has no external alert receivers and no uptime monitor, so nothing outside the
cluster starts alerting. Their definitions stay in `gitops/platform/` for the respin.

## Wave: DEVOPS / [REF] Mutation testing strategy

Disabled for this feature, because it changes no code. The project-wide per-feature strategy in
`CLAUDE.md` is unchanged.

## Wave: DEVOPS / [REF] Branching strategy

Trunk. The `lighthouse-platform` doc commit goes straight to `main`, the same way this workspace does in
`Lighthouse`.

## Wave: DEVOPS / [REF] Coexistence matrix

`validate-tenants`, Renovate and its auto-merge, and Lighthouse `ci_chart.yml` (Renovate's chart
source) must all keep working. So must website Mailgun sending on `lighthouse.letpeople.work`. The Jira
app is allowed to break (DISCUSS D7).

## Wave: DEVOPS / [REF] Pre-requisites

The same as the DISCUSS pre-requisites. In addition, the backups container has `force_destroy = false`:
empty it (`openstack object delete --all` / `mc rm --recursive`) before `tofu destroy`, or the destroy
fails partway.
