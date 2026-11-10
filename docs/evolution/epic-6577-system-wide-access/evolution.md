# Epic 6577 — Viewer and Admin for all Teams and Portfolios, and "Everyone signed in"

Delivered 2026-11-09, finalized 2026-11-10. Feature workspace: `docs/feature/epic-6577-system-wide-access/`.

## The problem

A customer's leaders had to hold System Admin just to read every Team and Portfolio, because a role could only be
given on one named unit. Adding each new Team to each leader did not scale, and System Admin gave them far more than
reading: connections, system settings, the licence and access itself.

## What changed

- **Viewer on all.** A System Admin maps an SSO group to Viewer on all Teams and Portfolios. Its members read every
  unit, including ones created later, without being added to any.
- **Admin on all.** The same, with the admin role: edit settings and members of every Team and Portfolio. It stays
  refused everything that is System Admin's alone (connections, system settings, creating or deleting units, the
  licence, managing access).
- **Limited API keys follow their owner.** A key limited to a unit works there if its owner reaches the unit through
  a rule, never wider than its limit, and stops when the rule goes.
- **Everyone signed in.** An opt-in switch, off by default and confirmed before it turns on, that lets every
  signed-in person with a profile read every Team and Portfolio.
- **The Access tab says so.** A unit admin sees, read-only, which system-wide rules also reach the unit.
- **"No direct role"** replaces "Unassigned" for a person with no role of their own on a unit.
- One usage-data event, `SystemWideAccessGranted` (name only), when a rule is added or the switch is turned on.

Design: ADR-280 (a system-wide rule is a group mapping on the all-units scope; permissions resolve by reach) and
ADR-281 (the switch is its own one-row record with a concurrency token).

## Key commits

| Commit | What |
|---|---|
| `1514cf8e0` | Prefactoring: permissions resolve through an `EffectiveAccess` value; every reader moved |
| `0d2e5ab00` | One SSO group can view every Team and Portfolio, and limited keys follow their owner (S1, with the former key slice folded in) |
| `d5bcc1530` | The Access tab says who else reaches a unit; No direct role (S3) |
| `b9aef114f` | Everyone signed in can view, if a System Admin says so (S4) |
| `7bc283a86` | A group can be Admin on all Teams and Portfolios (S5) |
| `81fcc459a`, `4f39cde19`, `e99ff369a`, `2b85585ec` | Refactor passes after the slices |
| `be8a44b5f` | RBAC guide, context map, UI surfaces, `ARCHITECTURE.md`, ADR statuses |
| `614e113f6`, `69f06b9c5` | Tests from the mutation kill rounds |

## Quality

- Four slices (S1, S3, S4, S5), each with an approved oracle and its RED observed before craft, accepted craft and an
  accepted slice review. The S1 and S3 oracles were re-approved at the end for test additions made after their
  approval.
- Whole-candidate review: accepted (backend build 0 warnings; 449 selected backend and 1886 frontend tests green).
- EXAMINE (source-blind): met on all nine expectations, run in WSL because nwdes cannot examine on Windows.
- Mutation, on the lines this feature changed: backend 95.78 % (from 71.69 %), frontend 89.81 % (from 66.24 %). Details in
  `docs/feature/epic-6577-system-wide-access/mutation/results.md`.

## Decisions taken without the maintainer, open to revisit

- The S2 key slice was folded into S1, so S1 could ship alone without leaving limited keys reading nothing.
- Creating or updating a mapping onto an equal existing row is refused ("This group already has this role here.");
  this is new behaviour, not an existing message.
- The examiner's probe surface was widened: `frontend-tests`, and named passing tests from `backend-tests` when
  narrowed (`604e6836e`).
- Mutation is gated on the feature's changed lines, because Stryker.NET mutates whole files and two of them are
  mostly older code.

## Knowingly left open

- **Possible latent defect, older than this feature:** `EffectiveAccess.Covers` compares the owner's and the key's
  roles by their general priority instead of the priority on that unit. A key limited to Viewer on a Team could pass
  for an owner whose only row there is a role that does not belong to Teams. Every write path refuses such a row, so it
  takes a hand-written one. Worth a Bug on the maintainer's word.
- **Website wording:** the "Authentication & RBAC" tooltip (`website`, `src/pages/Lighthouse.tsx:862`) says "four
  roles and SSO group mappings". Whether "Admin on all" and "Viewer on all" deserve a word there is the maintainer's call.
- **Flake watch:** the examiner once saw four `SystemWideRulesRefusalWireContractTest` cases fail in a combined run;
  six sequential runs on Windows and Linux were green. Likely two probe runs on one build output.
- **nwdes on Windows:** three backslash-path bugs (draft paths, the dirty-evidence prefix, draft clean-up) and no
  EXAMINE at all. Worth reporting upstream.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done in `be8a44b5f`: `docs/settings/rbac.md` (roles, matrix, system-wide rules, Everyone signed in, the Access tab list, No direct role, API keys, when a change takes effect, steps matched to today's screens), `docs/product/jobs.yaml`. |
| Screenshots | Done: `settings/rbac.png` and `settings/rbac_groupmapping.png` regenerated against Keycloak with only the two `@screenshot @auth` tests; the group-mapping test now waits for the page to load before deciding whether to become the first System Admin (it raced, and the SSO Groups section now needs a System Admin). |
| Demo data | N/A, because the demo data sets up no RBAC at all: rules need Premium, RBAC on, a first System Admin and identity-provider groups, none of which the demo seeds. |
| Website assets | `docs/assets/settings/rbac.png` and `rbac_groupmapping.png` are hot-linked from the release notes (raw GitHub URLs on `main`), so they follow the regenerated images. The website itself links neither. Its tooltip wording is under "Knowingly left open". |
| Clients (CLI, MCP) | N/A, because no client lists, creates or follows group mappings, keys or the new routes (`lighthouse-clients` searched; only a passthrough test matches "apiKeys"). No client release. |
| Context map, `ARCHITECTURE.md` | Done in `be8a44b5f`: ADR-270 Access row and glossary, ADR-271 Access surface, `ARCHITECTURE.md` §4 and the authorization bullet, ADR index rows 280 and 281, both ADRs marked shipped. |
| Usage-data event | Done: `SystemWideAccessGranted = 26`, emitted for a Viewer-on-all or Admin-on-all rule and for turning Everyone signed in on, listed in `docs/settings/usagedata.md`. |
| Mutation | Done: backend 95.78 %, frontend 89.81 % (from 66.24 %) on the changed lines; `docs/feature/epic-6577-system-wide-access/mutation/results.md`. |
| Release notes | N/A; written at /release. |
