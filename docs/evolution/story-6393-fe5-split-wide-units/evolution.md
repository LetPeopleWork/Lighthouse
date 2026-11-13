# Story 6393 — Split the wide frontend units

**Finished:** 2026-11-13 · ADO User Story #6393 (FE5) · Epic #6509

## The problem

Six production files under `Lighthouse.Frontend/src` were at or past 1000 lines: `BaseMetricsView.tsx` (2074),
`ragRules.ts` (1063), `ModifyConnectionSettings.tsx` (1062), and `DeliverySection.tsx`, `DeliveryCreateModal.tsx` and
`useMetricsData.ts` just under the line. Each mixed several responsibilities, so a change to one risked the others.
The Team and Portfolio pages also carried two copies of the same member and group-mapping handlers.

## What changed

Behaviour-free, in two nwdes features (`story-6393-wide-units-a`, S1–S5, pushed 2026-11-12;
`story-6393-wide-units-b`, S6–S9, pushed 2026-11-13). Commits 9574ced15 to 2ec0c2d5e, moves apart from edits.

- **The size rule.** `src/fileSize.enforcement.test.ts` fails naming any production file over 1000 lines that is not
  listed in `src/fileSize.offenders.ts`, and any listed file that came under the limit. The list started with two
  names and is empty now.
- **Connection settings form** (S1) split by responsibility: defaults section, form rules, premium notice, auth section
  and a save hook. Landed before #6473 slice 05, as agreed.
- **Status rules** (S2): `ragRules.ts` became three rule-family files plus types.
- **Metrics view** (S3, S4, S6): builders, widget nodes and frame helpers, then the named-percentiles scope and the
  state drill-down as hooks. 2074 → 683 lines.
- **Shared access hook** (S5): `useScopedAccess` serves both Team and Portfolio pages; `rbacAccess.enforcement.test.ts`
  keeps the copies from coming back. Its failure messages now use the configured Team and Portfolio terms (six texts),
  and renaming a term no longer reloads the members.
- **Metrics data** (S7): applicability rules, owner key, query helper and fetch keys left `useMetricsData.ts`
  (970 → 769).
- **Delivery grid** (S8, S9): `DeliverySection.tsx` 992 → 501 and `DeliveryCreateModal.tsx` 989 → 506; the form rules are
  a pure module tested as functions.
- **`ARCHITECTURE.md` §12** names the size rule and the shared access hook.

## What it achieved

- KPI 11 (production frontend files over 1000 lines): 3 → 0, and a test keeps it there.
- Full frontend suite 9858/9858 at the last push; CI green on every pushed slice.
- Mutation (StrykerJS, touched files): part A 97.06 %, part B 91.00 %, after one kill round each.

## Key decisions

- A split goes next to the file it came from, with no barrel; pure rules into function modules, effects into hooks.
- Part B moved `MetricsFetchKey` and `QueryProgress` into the hooks layer rather than adding two layer-ratchet leaks.
- Checks that read a file by its path move with the code (`formatLikelihood`, `deliveryGridServerState`,
  `deliveryJointLikelihoodDocs` enforcement tests).
- EXAMINE observation gaps on developer constraints (unmodified tests, file sizes) were accepted in both parts; the
  maintainer took the part A gap, part B followed it in AFK mode.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because behaviour-free: no page under `docs/` describes these files or anything a user sees changed. |
| Screenshots | N/A, because no pixel changed; the screen-level tests passed unmodified. |
| Demo data | N/A, because no data shape changed. |
| Website assets | N/A, because no file under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | ADR-270 and §4 unchanged: no new context, concept, edge or domain event. §12 gained the size rule and the shared access hook, in this finalize commit. |
| Usage-data event | N/A, because behaviour-free (DEVOPS answer). |
| Mutation | Done: part A 97.06 % (`docs/feature/story-6393-fe5-split-wide-units/mutation/results.md`), part B 91.00 % (`mutation/6393b/results.md`). Backend N/A, no backend file changed. |
| Release notes | N/A; written at /release. |

## Follow-ups

- **Bug #6624** (New): `isValidFutureDate` in `deliveryFormRules.ts` reads a date-only string as UTC, so a browser west
  of UTC refuses tomorrow as a launch date. Found during part B; the test runner pins Europe/Zurich, which hides it.
- `deliveryFeatureRow.tsx` keeps style-literal survivors (78.57 %), recorded in `mutation/6393b/survivors-run2.txt`.
