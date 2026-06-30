# Slice 08c — broken-image-rollback-drill

- **ADO story**: #5205 (Automated upgrades — RESCOPE part c)
- **job_id**: `job-saas-operator-upgrade-all-tenants-safely`
- **Band**: Fleet operations

## Learning hypothesis

> If we deliberately push a broken Lighthouse image to a throwaway canary tenant and rehearse the recovery,
> then we prove the post-sync smoke-test **catches** the bad release and that `git revert` (+ ArgoCD) rolls
> the tenant back to the prior healthy revision within the recovery-time target — so the "safe" claim of the
> whole fleet-upgrade story is **tested, not assumed**. If true, the operator can state a rollback runbook
> with evidence.

## Elevator Pitch

- **Before**: The rollback path (git revert + helm rollback, additive-only migrations) is believed to work
  but has never been exercised against a genuinely broken release; "safe upgrades" is an unproven promise.
- **After**: Benjamin pushes a deliberately broken image to a throwaway canary tenant, **watches the
  smoke-test alert fire**, runs the documented rollback (`git revert` the bump), and sees the tenant return
  to HTTP 200 on the prior version — the drill is recorded as a rehearsed runbook.
- **Decision enabled**: "If a release is bad, will we catch it and recover fast?" — proven, with a timed,
  repeatable drill.

## In / Out

- **IN**: a deliberate broken-image push to a **throwaway** canary tenant; observe the slice-08b smoke-test
  detect it + alert; execute and time the rollback (`git revert` of the version bump; additive-only
  migrations need no schema rollback); a **rehearsed runbook** capturing the steps and the measured
  detect→recover time; Tenant Zero (production) is NOT used as the broken-image target.
- **OUT**: building Renovate/merge-only (08a) or the smoke-test/alert itself (08b) — this slice CONSUMES
  them; automated auto-rollback (this drill is operator-initiated recovery, not an auto-revert controller);
  DR/backup-restore (slice-10/11, separate job).

## Open design questions handed to DESIGN

- Whether rollback stays **operator-initiated** (revert the PR/commit) or whether a future auto-rollback on
  smoke-test failure is desired — this slice only rehearses the manual path; auto-revert is a possible
  later capability, flagged not built.

## Dogfood moment

The drill runs on a **throwaway canary tenant alongside** Tenant Zero (mirroring the DELIVERED slice-08
proof, which used `canarytest` so LPW production never moved version) — we prove recovery without risking
real production.

## Thin end-to-end path

Push a broken image to a throwaway canary tenant → the post-sync smoke-test fails → an alert fires naming
the tenant → Benjamin runs the rollback runbook (`git revert` the bump) → ArgoCD restores the prior
revision → the tenant serves HTTP 200 again → the detect→recover time is recorded.

## Done = observable

- A deliberately broken release on a throwaway tenant is **detected by the smoke-test** (alert fires),
  not silently served.
- The documented rollback restores the prior healthy revision; the tenant returns to HTTP 200.
- The detect→recover time is measured and within the rollback target; the runbook is recorded as rehearsed.
- Tenant Zero (production) is untouched throughout the drill.

## Depends on

- slice-08b (smoke-test + alert is what the drill validates), DELIVERED slice-08 substrate (the roll +
  git-revert rollback path), slice-07 (throwaway-tenant provisioning/teardown).
