# Slice 08a — renovate-merge-only-release

- **ADO story**: #5205 (Automated upgrades — RESCOPE part a)
- **job_id**: `job-saas-operator-upgrade-all-tenants-safely`
- **Band**: Fleet operations
- **Supersedes**: the OLD `slice-08-fleet-upgrade.md` (which delivered the staged-rollout SUBSTRATE — matrix
  ApplicationSet, `promotedVersion`/`chartVersion` canary override, expand-only guard, git-revert rollback,
  all LIVE). This slice adds the **merge-only release automation** that drives that substrate.

## Learning hypothesis

> If Renovate watches the published Lighthouse image tag **and** the tracked platform/chart-dependency
> versions on the private gitops repo and opens a version-bump PR, while **Tenant Zero auto-canaries the
> latest release with no human ask**, then a fleet release collapses to **reviewing + merging ONE PR** —
> the operator does nothing else, and everything downstream (canary, roll, converge) is automatic. If true,
> a small team keeps an arbitrary number of tenants current by clicking Merge.

## Elevator Pitch

- **Before**: To roll a new version Benjamin hand-edits `promotedVersion` in the gitops repo, commits and
  pushes — he has to know a new image exists and what its tag is; there is no review surface and no signal
  that he is even behind.
- **After**: Benjamin opens the `LetPeopleWork/lighthouse-platform` repo and **sees a Renovate PR** titled
  "chore(deps): update lighthouse 26.6.21.1 → 26.7.x" already raised; Tenant Zero is **already running the
  new version** (auto-canaried, no action from him); when he is happy with Tenant Zero he clicks **Merge**
  and `argocd app list` shows the whole fleet converge on the new revision.
- **Decision enabled**: "Is shipping a release now a one-click, reviewed action?" — yes: review Tenant Zero,
  merge one PR, the fleet rolls.

## In / Out

- **IN**: Renovate configured on the private gitops repo; watches the published Lighthouse container image
  tag + tracked chart/platform-component dependency versions (e.g. CNPG/Postgres, cert-manager); opens a
  **fleet** version-bump PR (bumps `promotedVersion`); **Tenant Zero auto-canary** (its `chartVersion`
  override tracks the latest release hands-off); **one-click fleet promote** by merging the PR (the shipped
  substrate then rolls every tenant).
- **OUT**: the post-sync smoke-test + alert (→ slice-08b); migration sync-wave ordering (→ slice-08b); the
  broken-image rollback drill (→ slice-08c); per-tenant version-pinning policy; traffic-splitting canary
  inside one tenant (epic-5305 Band D).

## Open design questions handed to DESIGN

- **The Tenant-Zero auto-canary MECHANISM is NOT decided here.** Capture the INTENT only: "Tenant Zero
  takes every new release first, hands-off; the rest of the fleet stays pinned until one merge." Candidate
  mechanisms for DESIGN: (i) Renovate **auto-merge** of a Tenant-Zero-record-only PR that bumps its
  `chartVersion` override; (ii) a literal mutable `latest` tag the TZ record tracks — **known gotcha: a
  mutable tag is registry-side and ArgoCD tracks GIT, not the registry, so a retag does not trigger a sync**
  (do NOT pick this blindly); (iii) a Renovate branch/automerge rule scoped to the TZ record. DESIGN converges.
- Renovate hosting (self-hosted GH-Actions cron vs the Renovate GitHub App) and exactly which
  chart/platform-component versions are in Renovate's watch scope — DESIGN/DEVOPS decide.

## Dogfood moment

Tenant Zero (`lpw`, real LPW production) is the permanent canary: it auto-takes every release first, so
Benjamin feels any upgrade pain on our own production before a customer's tenant ever sees the version.

## Thin end-to-end path

A new image is published → Renovate raises a fleet-bump PR **and** Tenant Zero auto-canaries the new
version → Benjamin watches `lpw.lighthouse.letpeople.work` stay healthy → he merges the one PR →
`promotedVersion` bumps → the substrate rolls every other tenant → `argocd app list` shows all Synced on
the new revision.

## Done = observable

- A new published image tag raises a Renovate PR in `LetPeopleWork/lighthouse-platform` within one scan
  interval (no silently-missed release).
- Tenant Zero runs the new version **without any human merge or edit** (auto-canary), still serving HTTP 200.
- The rest of the fleet stays on the prior version until exactly **one** PR is merged.
- Merging that PR rolls every tenant; `argocd app list` shows all tenants Synced on the new revision.

## Depends on

- DELIVERED slice-08 substrate (matrix appset + `promotedVersion`/`chartVersion`), slice-07 (a generated
  fleet to roll), epic-5305 rolling-update + drain primitives.
