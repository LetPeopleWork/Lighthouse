# Azure DevOps OAuth asks only for the rights write-back needs (ADO Story #6672)

Delivered 2026-11-24. Workspace: `docs/feature/story-6672-ado-oauth-write-consent/` (`brief.md`,
`architecture.md`, `acceptance.md`, `feature-delta.md`, `design/`, `runs/`, `mutation/`).

## What was delivered

An Azure DevOps OAuth connection used to ask Microsoft for the right to read **and write** work items, even when
nothing was ever written back. Now a sign-in asks only for what the connection needs: reading, unless the
connection has write-back mappings, in which case reading and writing. Lighthouse stores the rights Microsoft
actually granted (read from the token answer's `scope`).

- The connection's **Authentication** section shows *Rights: Read work items* or *Rights: Read and write work
  items*.
- A connection with write-back mappings whose sign-in may only read shows a warning, *Write-back needs more
  rights*, with **Sign in again**: above the write-back mappings and on its card in the connections list. Signing
  in again does not disconnect first. If the reload after the sign-in fails, the old rights and warning are not
  shown as current.
- Write-back never writes through a sign-in without the needed rights: it skips the write and logs one warning
  per run.
- Connections signed in before rights were recorded count as allowed to read and write, so nothing changes for
  them. Jira and PAT connections are untouched (Jira follows in Bug #6686).

API: the connection payload gains `grantedRights` (absent when there is no OAuth sign-in) and `needsWriteConsent`,
both computed by the server; a saved body cannot set them. Migration: one nullable column `GrantedRights` on the
OAuth credential, SQLite and Postgres, expand-only, no backfill.

## Slices and key commits

| Slice | What it delivers | Key commits |
|---|---|---|
| S1 | Backend: the sign-in asks for the needed rights, stores what was granted, write-back skips without consent | `1ca8a89e8`, `d94520028` |
| S2 | Connection settings and the connections list show the rights and the "Sign in again" warning | `b40573fe9`, `e7e920823`, `850ae5be2`, `0ee48fb44` |
| Final gates | A failed reload after signing in no longer leaves old rights on the page; refactors; payload guard; mutation kill-round tests | `eda155da9`, `45e34c3a8`, `3753c1537`, `172a84a95`, `affac2f41`, `00760922f`, `66e954003` |

## Gates

- Slice reviews: S1 and S2 accepted. The S2 review found the stale page after a failed reload; fixed in place
  (test RED first, then the fix).
- Whole-candidate review: accepted at `affac2f41`, no blocker. EXAMINE: accepted, E1–E5 met (from narrow test
  probes; no live Microsoft sign-in).
- Full gates: re-run after the mutation tests; see the push commit's run.
- Mutation: backend 93.55 % (29/31), frontend 87.61 % (99/113) on the changed code (see `mutation/results.md`).

## Decisions taken in AFK mode, worth a second look

- The guard blocks a write when the sign-in lacks the rights the connection **needs** (it has mappings), not on
  "may only read" alone. A read-only sign-in with no mappings cannot produce a write, so the two agree in practice.
- "Sign in again" reuses the Reconnect flow's failure messages, with its own wording.
- The list warning does not name its connection, like the Reconnect banner beside it. A connection that needs
  both shows both, Reconnect first. Both were the defaults while the maintainer was away.

## Knowingly left open

- Checked by the maintainer on 2026-11-24: a read-only sign-in against an app registration with only `vso.work`
  works and shows *Rights: Read work items*. Not yet checked live: signing in again for write consent with
  `vso.work_write` registered. The parser fails closed: anything it does not recognise counts as reading only.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. `docs/concepts/worktrackingsystems/azuredevops.md`: the API permissions (`vso.work`, plus `vso.work_write` for write-back), what a sign-in asks for, the Rights line, a "Write-back rights" section, and a troubleshooting row for the warning. "work items" there is Azure DevOps's own word for its records and the literal UI copy. |
| Screenshots | N/A, because no `@screenshot` test shows an OAuth connection: the connections tab and the creation screenshots use demo connections without an OAuth sign-in, so neither the Rights line nor the warning appears. |
| Demo data | N/A, because an OAuth sign-in needs a real Entra app; demo data cannot hold one. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because the clients read only a connection's name, type, sign-in method labels and options; the two new fields are additive (checked by the whole-candidate reviewer). |
| Context map, `ARCHITECTURE.md` | Done in S1 (`d94520028`): ADR-270's Tracker glossary has "Write consent"; `ARCHITECTURE.md` §4 row 1 has the clause. They match what shipped. |
| Usage-data event | N/A, because DEVOPS decided so: signing in for write consent is an admin set-up step, not feature use. |
| Mutation | Done. Backend 93.55 % and frontend 87.61 % on the changed code; survivors triaged in `mutation/results.md`. |
| Release notes | N/A; written at /release. |
