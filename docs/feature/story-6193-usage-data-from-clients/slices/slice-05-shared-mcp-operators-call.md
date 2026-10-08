# Slice 05 — A shared MCP server follows its operator's call

**Story** #6193 / US-06 · **Job** `job-operator-decide-usage-data-for-a-shared-mcp-server` · **Repo**
`lighthouse-clients` (`mcp-http`) + Lighthouse docs · **Estimate** ~3h

## Goal

`LIGHTHOUSE_USAGE_DATA=on` makes a shared `mcp-http` report the six mapped tool events with `source: Mcp`;
unset, it sends nothing; nobody is ever prompted.

## IN scope

- The variable, its start-up line and its one warning for an unknown value (D12).
- An in-memory grant requested on the first event, never under the veto; `DO_NOT_TRACK` overrides (D14).
- Slice 04's `mcp-core` hook, unchanged.
- `mcp-http/README.md` row; `docs/settings/usagedata.md`: a shared server counts as one, decided by its
  operator.

## OUT of scope

- A chart value (A13, follow-up). Per-user prompts (M8).

## Learning hypothesis

**Disproves that operators want this switch** — if no operator turns it on within the KPI-2 window, `Mcp`
counts come from stdio only and the variable is kept but not extended.

## Acceptance criteria

AC-06.1 … AC-06.5. Risk carrier: **AC-06.4**, a real `docker run` against the dev instance with the variable
on.

## Dependencies

Slice 04 (the `mcp-core` hook). **Release gate**: open product question 1 (the legal basis) answered.

## Reference class

`mcp-http`'s `LIGHTHOUSE_OAUTH_*` variables — env-only configuration with a fail-fast check.

## Pre-slice SPIKE

None.
