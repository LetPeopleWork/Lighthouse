# ADR-089: Break-Glass GitOps Path — Per-Incident Auto-Sync Disable on the *Single Affected* ArgoCD Application (Not a Sync-Window, Not a Cluster-Wide Freeze); a Standing "Auto-Sync-Disabled" Alert Makes the Break-Glass State Self-Expiring

**Status**: **ACCEPTED** (2026-06-29, Benjamin)
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5201 GitOps) — resolves the **break-glass red card**
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: Composes with ADR-086 (ArgoCD app-of-apps + ApplicationSet) and ADR-090 (the alert rides the kube-prometheus-stack). Honours the GitOps invariant (git is the source of truth) while admitting that incidents sometimes need a live change *before* it can be committed.

---

## Context

US-02 red card: an incident may require an immediate live change that ArgoCD's self-heal would revert within the sync interval. GitOps must not become a straitjacket that prevents emergency response, but the escape hatch must not silently leave the cluster diverged from git (the whole point of GitOps). Three mechanisms were weighed.

## Decision

**The break-glass path is: disable automated sync on the *single affected* ArgoCD `Application`, make the live fix, then commit the fix to git and re-enable automated sync.**

```
# 1. Break glass — scope to ONE app, leave the rest of the fleet self-healing
argocd app set <tenant-or-platform-app> --sync-policy none
# 2. Apply the live emergency fix (kubectl / helm) — it now survives (no self-heal revert)
# 3. Commit the same change to git; re-enable
argocd app set <app> --sync-policy automated --auto-prune --self-heal
```

- **Scoped blast radius**: only the affected Application stops self-healing; every other tenant keeps drift-protection. A sync-window or a cluster-wide freeze would suspend self-heal for the *whole fleet* during an incident affecting one tenant — unacceptable.
- **Self-expiring via observability** (Earned-Trust honesty): a standing Prometheus alert `ArgoCDAutoSyncDisabled` fires whenever any Application has `syncPolicy.automated == null` for longer than a short grace period (e.g. 30 min). Break-glass cannot be silently *forgotten* into a permanent drift — the alert is the pressure to re-commit and re-enable. The break-glass state is thus observable, not a hidden mode.
- **Runbook, not a tool**: documented in the operator runbook (the recover/operate surface), with the re-commit step mandatory before the incident is closed.

| Mechanism | Blast radius | Forgettable? | Fit for incident hotfix |
|---|---|---|---|
| (A) Per-app auto-sync disable ✅ | **One app** | No — `ArgoCDAutoSyncDisabled` alert | **Best** — surgical, observable, reversible |
| (B) ArgoCD sync-window | Whole project/fleet for a *time range* | Window auto-closes (good) but freezes everything (bad) | For *planned* maintenance freezes, not surprise incidents |
| (C) `argocd.argoproj.io/sync-options: Disabled` annotation per-resource | One resource | Yes — annotation easily left in git/cluster, no alert | Too fine-grained + drift-prone for incident use |

## Consequences

- **Positive**: emergency response is unblocked without disarming the whole fleet's drift protection; the escape hatch is observable (alert) and reversible (re-enable); planned freezes still have sync-windows available as a separate, complementary tool.
- **Negative / cost**: relies on operator discipline to re-commit (mitigated by the standing alert); a forgotten re-enable degrades to "this one app no longer self-heals" — surfaced loudly, not silently.
- **Standalone gate**: untouched — applies only to the hosted ArgoCD-managed platform.

## Alternatives considered

1. **ArgoCD sync-windows** — kept for *planned* maintenance freezes (e.g. a known risky window), rejected as the *incident* break-glass because it freezes a whole project rather than the one affected app.
2. **Cluster-wide self-heal off** — rejected: removes drift protection from every tenant during a single-tenant incident.
3. **Per-resource `sync-options: Disabled` annotations** — rejected as the primary path: too granular and easy to leave behind with no alerting; usable ad hoc but not the documented runbook.
