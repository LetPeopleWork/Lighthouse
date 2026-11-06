# Slice 14 — The CLI and MCP read the levels, and handle the refused unknown type

> **Amended 2026-11-06.** Item types moved to the connection's mapping. The last section of `../design/upstream-changes.md` wins over this file; apply it at DISTILL.

Epic #6474 · **visible** (API clients) · `job_id: job-config-admin-describe-our-levels-once` · ≤ 1 day ·
depends on: 04 (the 400), 05 (`GET /levels`), 06 (where names are written) · repo: Lighthouse-Clients
(DESIGN/DELIVER; not reachable from this workspace)

## Elevator Pitch

- **Before**: Priya's coding agent, asked "which tracker types count as Work Items here?", lists every Team's
  types and guesses.
- **After**: `lighthouse levels list` and the MCP `levels` tool return both levels with names and types; a Team
  update naming an unknown type fails with the server's message.
- **Decision enabled**: scripts and agents use the organisation's structure as it is, and learn of the breaking
  change from a major version and a note, not from a failed run.

## Problem

Priya Raman scripts Team set-up through the CLI. From this Epic, a write naming a type the level does not list
is refused for everyone (M1), and she has no client command to see what the levels list.

## Learning hypothesis

A read-only `levels` command plus a clear breaking-change note is enough for scripted users. **Disproved if**
users ask for level writes in the clients within a release (then that is its own Story; administering versus
using Lighthouse as separate AI skills is a later idea, out of scope).

## IN

- CLI `levels list` and MCP `levels` read tool over `GET /api/latest/levels`, names in the instance's words.
- Unit updates surface the server's 400 ("Sub-task is not on the Team level") unchanged.
- Released with a **major version bump** and a breaking-change note (to confirm at DESIGN), together with the
  server release that ships slice 04.

## OUT

- Level writes from the clients. Separate admin and user AI skills (later idea).

## Domain examples

1. `lighthouse levels list` on Priya's install: 1 Squad/Work Item (Story, Bug, Task, User Story), 2 Portfolio/
   Initiative (Epic, Change).
2. A script updating Team "Search" with Story and Spike gets "Spike is not on the Team level" and exit code ≠ 0.
3. Ana's free-tier install: Team/Work Item (User Story, Bug), Portfolio/Feature (Epic).

## UAT scenarios

### Scenario: A script reads the organisation's levels

Given Priya's install has two levels named Squad and Portfolio
When her script lists the levels
Then it gets both levels with their unit names, item names and item types

### Scenario: An agent learns why a Team update was refused

Given level 1 does not list "Spike"
When an agent updates Team "Search" with Story and Spike
Then it reports that Spike is not on the Team level

### Scenario: Users are told about the breaking change before they upgrade

Given the clients' release notes
When Priya reads the new major version's notes
Then they say that an unknown type is now refused and how to see the level's types

## Acceptance criteria

- [ ] `levels` read in CLI and MCP, in the instance's words.
- [ ] The server's 400 message reaches the user unchanged.
- [ ] Major version bump and breaking-change note, released with slice 04's server release.

## Outcome KPI

OUT-2 (scripted set-ups pick from the level).

## Technical notes

- Memory: clients release needs a manual version bump (`pnpm release:version` + commit).
- If clients write the 8 structure keys through `PUT /terminology`, slice 06's choice changes that path too.
