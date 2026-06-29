# Slice 03 — tenant-zero-reachable  ⭐ WALKING SKELETON

- **ADO stories**: #5204 (Tenant Zero) + thinnest slice of #5202 (single-host route)
- **job_id**: `job-saas-operator-dogfood-tenant-zero`
- **Band**: Walking skeleton (completes substrate → chart → DNS → secret → reachable)

## Learning hypothesis

> The thinnest viable path — substrate (slice-01) → ArgoCD (slice-02) → the shipped chart → a DNS
> route → a hand-made secret — brings **LPW's own production Lighthouse up as Tenant Zero, reachable
> at its own subdomain over HTTPS**. If true, the end-to-end thread the whole epic hangs on is
> proven on real production data before any automation is built.

## Elevator Pitch

- **Before**: LPW's Lighthouse is hosted ad-hoc; the productization platform is paper.
- **After**: Benjamin opens `https://lpw.lighthouse.letpeople.work` → sees LPW's real Lighthouse serving over a valid cert, installed by ArgoCD from git into an isolated `tenant-lpw` namespace.
- **Decision enabled**: "Does the end-to-end platform path actually work on our own production?" — yes, tenant zero is live.

## In / Out

- **IN**: One tenant (zero) namespace; shipped chart installed via ArgoCD at default embedded shape; **single explicit** ingress host `lpw.lighthouse.letpeople.work` + a cert; a **hand-made** Kubernetes Secret for DB/OIDC.
- **OUT**: Managed secret store (slice-04), wildcard DNS for arbitrary tenants (slice-05), any second tenant, automated provisioning.

## Dogfood moment

This IS the dogfood: LPW production becomes Tenant Zero on the platform, first.

## Thin end-to-end path

Commit a `tenant-lpw` app (chart + values + hand-made secret + single-host ingress) to the GitOps repo → ArgoCD syncs → DNS A/CNAME for the one host → cert issued → browser hits the URL → LPW serves.

## Done = observable

- `kubectl get pods -n tenant-lpw` all Running/Ready.
- `https://lpw.lighthouse.letpeople.work` serves LPW with a trusted cert.
- The repo holds a secret **reference/placeholder**, with the real secret applied out-of-band (slice-04 will replace the hand-made one).

## Depends on

- slice-01 (substrate), slice-02 (ArgoCD).
