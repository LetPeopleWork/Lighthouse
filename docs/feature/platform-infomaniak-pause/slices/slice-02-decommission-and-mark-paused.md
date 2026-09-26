# Slice 02: Decommission, and mark the repo paused

**Goal:** Nothing the platform created remains on Infomaniak, no DNS name dangles, and the repo says
it is paused and how to bring it back.

**Stories:** US-2, US-3 (see `../feature-delta.md`).

## IN (in this order, per D5)
1. Delete the `ingress-nginx` LoadBalancer Service, then confirm the Octavia load balancer is gone
   (`openstack loadbalancer list`).
2. Delete every PVC, then confirm the Cinder volumes are gone (`openstack volume list`).
3. `tofu destroy` in `infra/substrate` (managed cluster, node pool, backup container and credential).
   The backup container has `force_destroy = false`, so empty it first.
4. Delete the `lighthouse-tfstate` bucket last.
5. Remove only the `*.lighthouse.letpeople.work` wildcard A record. Leave the Mailgun records alone.
6. `lighthouse-platform`: add the README banner and `docs/respin.md`, then push to `main`.
7. Billing check right away, and again after one day.

## OUT
- Any edit under `.github/`, `gitops/`, `infra/` or `renovate.json`.
- The Jira app manifest.
- Closing the Infomaniak account.

## Learning hypothesis
Disproves "`tofu destroy` alone releases every billable resource" if an orphaned load balancer, volume
or bucket still shows up in the `openstack` listings or on the next day's bill. Confirms that the
platform can be torn down to zero spend from the repo plus this runbook.

## Acceptance criteria
AC-2.1 … AC-2.5 and AC-3.1 … AC-3.4 in the feature-delta.

## Dependencies
Slice 01 done (hard gate, D4).

## Effort
About 2 h, plus the check a day later.
