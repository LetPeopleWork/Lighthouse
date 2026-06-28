# RESUME — Epic 5306 productization platform (combined DISCUSS/DESIGN)

> **Step 2 of 2.** Do AFTER the MCP fix (`docs/feature/mcp-oauth-discovery-fix/`).
> **User decision (2026-06-28):** run a **single combined DISCUSS + DESIGN wave covering ALL remaining
> Epic 5306 stories as a whole** first, then slice into the individual stories for DELIVER.

## Start here tomorrow (after MCP fix)
`/nw-discuss` for the whole set below, treated as one platform-design effort (not per-story discuss).

## Scope — all remaining Epic 5306 children (New)
| Story | Title |
|---|---|
| 5201 | GitOps with ArgoCD |
| 5202 | Wildcard DNS + subdomain routing |
| 5203 | Secrets management |
| 5204 | LetPeopleWork as Tenant Zero (dogfood) |
| 5205 | Automated upgrades |
| 5206 | Observability |
| 5207 | Tenant provisioning automation |
| 5208 | Backup & disaster recovery |
| 5320 | Multi-provider cluster substrate (OpenTofu) |

(Already shipped in this epic: #5199 Helm chart, #5200 docs — see
`docs/feature/epic-5306-k8s-productization/`.)

## Why combined
These stories are one coherent productization platform (multi-tenant SaaS-ish hosting of Lighthouse on
k8s): substrate → GitOps → DNS/routing → secrets → provisioning → upgrades → observability → backup/DR,
with LPW as tenant zero. The user wants the shape designed as a whole before slicing, so the boundaries
and shared decisions (tenancy model, secret strategy, GitOps repo layout, substrate) are coherent.

## Not started
No DISCUSS/DESIGN artifacts yet. This workspace is a placeholder so `/nw-continue` surfaces it as the
next feature after the MCP fix.
