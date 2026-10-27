# Slice 06 — Every report through the clients

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-06 (ADO: to be created) · **Estimate**: ~1d
(rev. 2026-10-27 after review: was ½d — MCP stdio and HTTP, `lh`, skill drift test, changesets and the release) ·
**Tier**: Community · **Ships alone**: yes (on 01) · **Repo**: `lighthouse-clients` ·
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

`lh report list` with no owner, and an MCP tool over stdio and HTTP, list every report the user can read across Teams
and Portfolios in one call, each with report id, owner kind, owner id and name, ready for the existing `get`.

## IN

- **The contract: `lh report list` with no `--team-id` / `--portfolio-id`** lists every readable report (a row reads
  "ID 9 · Then & Now — 90 days to 29 Jul 2026 · Then & Now · Team Gravity · 2026-10-27"); `--json` gives the raw list.
  An MCP tool returns the same (name DESIGN's, e.g. `lighthouse_report_list`), beside the per-owner tools (D6).
- Uses slice 01's cross-owner read with the caller's credential; the server filters.
- Relays as the existing report tools: no access; older Lighthouse → "upgrade Lighthouse".
- The `lighthouse` skill names the new tool and command (drift test); `lighthouse-reports` skill may mention it.
- Changesets (`lighthouse-client`, `lighthouse-mcp-core`, `lighthouse-cli`, minor); manual `pnpm release:version`,
  commit and push before the clients release.
- Usage data (DEVOPS): `AllReportsListed`, source `Cli` / `Mcp`, for the no-owner list only, on success after
  consent; the per-owner list keeps reporting nothing.

## OUT

Bulk create/remove through the clients · combined PDF from the clients.

## Learning hypothesis

**Disproves "assistants want the whole list, not per owner"** if, in the `lighthouse` skill eval for "which Teams
have a Then & Now?", the assistant still lists owner by owner.

## Data and dogfood moment

Dev instance via MCP stdio, MCP HTTP and `lh`; compare with the Reports page.

## Acceptance criteria

AC-6.1 … AC-6.6 in `feature-delta.md` (US-06).

## Dependencies

Slice 01's cross-owner read, released in a Lighthouse version.
