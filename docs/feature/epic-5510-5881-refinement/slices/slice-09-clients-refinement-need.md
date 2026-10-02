# Slice 09 — Ask the refinement need from the CLI or an assistant

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-09 ·
**Estimate**: ~1d · **Tier**: Community · **Repo**: `lighthouse-clients` (+ Lighthouse endpoint already built)

## Goal

The CLI and the MCP server state the same verdict the web tab states: the next Refinement date, the ready count, the
range and below / in / above. They also return the list with the "enough for" line.

## IN

- A CLI command and an MCP tool. Names are for DESIGN; the working names are `teams refinement <id>` and
  `get_team_refinement`.
- The client builds the sentence from facts on the wire, using the instance's Terminology (the epic-4172 precedent).
- Read-only in this slice. Voting from the clients comes in 17b (DD-19).
- A changeset and a minor version bump. Run `pnpm release:version` before the release run.

## OUT

Votes — reading them is slice 17a, casting them 17b (DD-19). Writing any setting.

## Learning hypothesis

**This disproves "facts on the wire let a client state the verdict honestly"** if the client has to re-derive the band
or the line itself. That would mean the API shape is wrong, and it should be fixed before slices 17a/17b copy it.

## Data and dogfood moment

- Demo instance plus the dev instance.
- Dogfood: ask the MCP assistant "how many Work Items should Team Gravity refine before Thursday?" and compare its
  answer with the tab.

## Acceptance criteria

AC-9.1 … AC-9.3 (US-09).

## Dependencies

Slices 05, 06 (07 optional).
