# Journey (visual) — Ship a release = merge one PR

> Lightweight operator journey for the slice-08 RESCOPE. Persona: platform-operator (LPW SaaS-operator
> flavour). Operator: Benjamin. Builds ON the DELIVERED staged-rollout substrate. Emotional arc:
> **Problem Relief** — from "a fleet upgrade is a scary batch job I do by hand" → "I merge a PR and watch
> it roll, and I'm told fast if anything is wrong."

## Flow

```
  PUBLISH            AUTO-CANARY            REVIEW                MERGE ONE PR          FLEET ROLLS         VERIFY / RECOVER
  (CI ships image)   (Tenant Zero,          (watch TZ)            (one click)           (substrate)         (smoke-test + alert)
                      hands-off)
  --------------     ----------------       ---------------       ----------------      -------------       -------------------
  new image tag  →   TZ takes latest    →   Benjamin sees     →   Benjamin clicks   →   promotedVersion →   each tenant: migrate
  on GHCR            with NO ask;           a Renovate PR         "Merge"               bumps; every        before API; post-sync
                     Renovate raises        already open +        on the ONE            tenant rolls        smoke-test → 200,
                     a FLEET PR             TZ already green      fleet PR              (epic-5305 drain)   else ALERT names tenant

  Feels: (calm,      Reassured -           Confident -           Decisive -            Calm -              Assured -
   passive)          "prod already         "I'm not guessing;    "one click, not       "no window, the     "if a tenant came back
                     took it, hands-off"    TZ proves it"         a hand-edit"          fleet rolls itself" sick I'd be told now"

  EMOTIONAL ARC: scary-batch-job (start) -> reassured-by-canary (middle) -> one-click-calm + told-fast-if-bad (end)
```

## What the operator actually sees (material honesty — ops surface, not a GUI)

```
# 1. Renovate has already opened the fleet PR (private LetPeopleWork/lighthouse-platform)
PR #142  chore(deps): update lighthouse 26.6.21.1 -> 26.7.0   [renovate]  open
         bumps tenants/applicationset promotedVersion: 0.1.4 -> 0.1.5

# 2. Tenant Zero ALREADY runs the new version, hands-off (auto-canary) — nobody merged anything yet
$ argocd app list | grep -E 'tenant-lpw|tenant-riverbank'
  tenant-lpw         Synced   Healthy   lighthouse@0.1.5   <-- canary, auto, ahead of fleet
  tenant-riverbank   Synced   Healthy   lighthouse@0.1.4   <-- fleet, still pinned

$ curl -sI https://lpw.lighthouse.letpeople.work | head -1
  HTTP/2 200                                              <-- TZ healthy on 26.7.0

# 3. Benjamin merges PR #142 (the one click) -> promotedVersion bumps -> fleet rolls
$ argocd app list | grep tenant-
  tenant-lpw         Synced   Healthy   lighthouse@0.1.5
  tenant-riverbank   Synced   Healthy   lighthouse@0.1.5   <-- converged on the merge

# 4. Post-sync smoke-test result (08b) — healthy: silence; unhealthy: an alert lands
[ops-channel] ALERT  tenant=riverbank upgrade=26.7.0 smoke-test=FAILED (health 503) -> rollback runbook
```

## Error paths (designed for, not discovered in production)

| Trigger | What the operator sees / does |
|---|---|
| Canary unhealthy on Tenant Zero | The fleet PR is NOT merged (the human gate); `lpw` shows the regression first, on production data, before any customer tenant moves. Decision: hold the merge, investigate. |
| A release carries a destructive migration | `ExpandOnlyMigrationGuard` fails the release build (`dotnet test`) — the version never reaches a tenant; no Renovate PR can roll a destructive schema. |
| A tenant returns unhealthy after upgrade (08b) | The post-sync smoke-test fails → an alert names the tenant + version in the ops channel within minutes; the operator is told, not left to poll each subdomain. |
| A broken image is shipped (08c drill) | On a throwaway tenant: smoke-test catches it → alert → `git revert` the bump → ArgoCD restores the prior revision → HTTP 200 again, within the rollback target. Tenant Zero untouched. |
| Migration races the new API | Ordering (08b) applies the schema migration before the new API serves — no API-before-migration window. |

## Notes for DESIGN

- The Tenant-Zero auto-canary MECHANISM is an open question (O-08-1) — the journey captures the INTENT
  ("TZ hands-off, fleet one-click"), not the implementation.
- The alert channel + the smoke-test surface (PostSync hook?) are open (O-08-3), and the observability
  stack (slice-09) does not exist yet — 08b may need a thin standalone alert path first.
