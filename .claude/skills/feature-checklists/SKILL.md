---
name: feature-checklists
description: Lighthouse's own checklists for the end of DISCUSS and for finalizing a delivered feature — RBAC, Lighthouse-Clients, website, docs prose, screenshots, demo data, context map, usage data. Load it when DISCUSS closes, and after a feature's last slice is GREEN and its whole-candidate review and EXAMINE have passed, before the push. nWave 4 has no finalize step of its own; this is where it lives.
---

# Feature checklists

nWave answers "is the code right". These checklists answer "is everything around the code done". Every item
gets an explicit answer: **Done** (with the commit or file), **Deferred by the maintainer** (with when), or
**N/A, because …**. Never a silent skip.

## At the end of DISCUSS

Record the answers in `docs/feature/<feature>/feature-delta.md` under `### DISCUSS checklist`.

| Item | The question |
|---|---|
| RBAC impact | Does the feature add an action, a page or data that needs a permission? Which scope (team, portfolio, system) and which role? All gating goes through `IRbacAdministrationService` and `useRbac()`. |
| Lighthouse-Clients CLI / MCP | Does an API route or DTO change? If so, which client command or MCP tool must follow, and does it need a client version bump? |
| Website marketing surface | Is this a capability worth a line or a page on the website? Which asset does it hot-link from `docs/assets/`? |
| Context map | Which bounded contexts it touches, new glossary terms, domain events, and the UI surface (ADR-270, ADR-271). |

## Finalize, after the candidate is accepted

Record the answers in `docs/evolution/<feature>/evolution.md` under `## Finalize checklist`, with what was
delivered, the commits, and anything knowingly left open.

| Item | The question |
|---|---|
| Docs prose | Which pages under `docs/` describe what changed? Updated in the configurable terms (`Feature`, `Work Item`, …), never "Epic" or "Story". |
| Screenshots | Which `@screenshot` tests must be added or re-run? One per theme; run them with `--grep` on the one test, never the whole suite. |
| Demo data | Does the demo data show the feature? Guarded by `DemoDataFactoryTest`. |
| Website assets | Does any asset the website hot-links from `docs/assets/` change? |
| Clients (CLI, MCP) | Did DISCUSS's client answer ship, and is the client release noted? |
| Context map, `ARCHITECTURE.md` | Do ADR-270 and `ARCHITECTURE.md` §4 match what shipped? Update them in this commit. |
| Usage-data event | Is the event emitted and listed in `docs/settings/usagedata.md`? |
| Mutation | Stryker on each stack the feature changed, ≥ 80 % (`/mutation-testing`), or N/A because no production code changed. |
| Release notes | Always "N/A; written at /release". |

Then commit the evolution record and the workspace as `docs(<scope>): finalize <feature>`, move the Stories
the ADO way (`/ado-sync`), and push only through the normal gates.
