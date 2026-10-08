# ADR-230: A drift test in `pnpm test` holds the skills to the tools and commands that exist

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO User Story #6245). Interaction mode = **propose**.
- **Feature**: `story-6217-lighthouse-skills` — repo `lighthouse-clients` (`skills/skills.drift.test.ts`)
- **Relies on**: ADR-229 (the `skills/` layout).

## Context

The `lighthouse` skill was written on 2026-06-06 and, four months later, its tables named 13 of the 45 MCP tools
and about half of the `lh` usage lines. Nothing failed when a tool shipped without a mention, and nothing would
fail if a skill named a tool that does not exist (an assistant then calls it and gets an error, or invents an
answer). The two new skills name tools too. The maintainer wants a check that runs locally and in CI, with no
model and no network.

Quality attributes: maintainability (KPI-1: zero unnamed, zero non-existent), and a check that cannot pass by
reading nothing.

## Decision

1. A Vitest file, `skills/skills.drift.test.ts`, run by the existing `pnpm test` (the root config's `include`
   and `tsconfig.tests.json` gain `skills/`). It sits outside `packages/*/src`, so it needs no changeset.
2. The ground truth comes from the packages' public surface, not their source text: the MCP tool names from
   `createMcpCoreRuntime(...).listTools()`; the CLI's groups, `lh <group> <subcommand>` lines and `--metrics`
   keys from the help `runCliCommand` prints with stub dependencies.
3. Three rules, one `it` each: the general skill (its `SKILL.md` and `references/`) names every tool,
   subcommand and metric key; no skill names a `lighthouse_*` tool, `lh` command or `--metrics` key that does
   not exist (a known alias passes); every skill's frontmatter `name` equals its folder. An exemption list in
   the test carries a reason per entry and starts with `lh help` only.
4. It reports by name: each rule compares a sorted list of offenders with `[]`, so the failure lists every
   missing name, or every unknown name with its file and line.
5. It proves it read something: the parsed sets must contain known anchors (`lighthouse_team_list`, `metrics`,
   `refinement vote`, `wip`); otherwise it fails.

## Alternatives considered

- **Parse the TypeScript sources with regexes** (`name: "lighthouse_…"`, the help-text arrays). No stub
  dependencies needed. Rejected: a refactor that moves a definition to another file or builds a name from parts
  silently drops it from the check; the public API cannot drift from what users get.
- **A standalone script** (`scripts/check-skills.mjs`) wired into `pnpm run ci`. Rejected: it needs the built
  `dist/` or a TS runner, and it would be a second test runner beside Vitest, which already resolves the
  packages from source.
- **Check groups only**, as DISCUSS first wrote. Rejected: new capabilities ship as subcommands and metric keys
  (`blocked`, `cumulativeStateTime` were the ones missing), and a group-level check passes over them.
- **Inside `packages/mcp-core/src`.** Rejected: every skill edit would then require an (empty) changeset, and
  `mcp-core` does not own the skills.

## Consequences

- Positive: a tool or command added without a skill mention fails the pre-commit hook and CI, naming it; so
  does a skill naming one that does not exist.
- Positive: the specific skills may name only what they use; only the general skill must be complete.
- Negative: the general skill must mention every subcommand and metric key, including rarely used ones (the
  reference files carry those).
- Negative: a name written in plain prose, outside backticks and code blocks, is not checked by the reverse
  rule. The skills write every command and tool name in a code span by convention; the reviewer of a skill
  change holds that convention.
