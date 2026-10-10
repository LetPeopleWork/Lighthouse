# ADR-224: An MCP summary rides beside unchanged facts — a field on an object answer, a second text block on a list or scalar answer

- **Status**: Proposed (2026-10-06, DESIGN wave for ADO Story #6218). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6218-readable-cli-output` — repo `lighthouse-clients` (package `mcp-core`)
- **Relies on**: ADR-223 (readers and wording in `client`).

## Context

The maintainer brought MCP summaries into story #6218 (2026-10-06). Every MCP tool the story converts returns a
`summary` with the same heading and sentence(s) the CLI prints, as `lighthouse_team_refinement_get` already does,
and its facts must otherwise stay as they are. Today a tool returns one text block, `label: <TOON of the
facts>` (JSON if TOON cannot encode the value). Refinement puts the summary inside the answer object:
`refinement: encode({ summary, ...facts })`.

That works for an object answer. Many tools return an **array** (`teams`, `portfolios`, `worktracking`,
`blackout_list`, `feature_get`, `feature_workitems`, `delivery_list`, several metric series) or a **scalar or
fixed line** (`version: 26.10.3.6`, `connectivity: success`, `team refreshed: 3`). An array cannot take a field
unless its shape changes.

## Decision

1. **Object answer**: `label: encode({ summary, ...facts })`, exactly as refinement does.
2. **Array or scalar answer**: the existing text block stays byte-identical, and a **second text block**
   `summary: <text>` follows it in the same `McpToolResult.content`.
3. **No recognised shape** (the reader returns `null`): no summary, and the result is exactly today's.
4. The reads for the summary (the instance's Terminology and one name) never fail the tool. A failed read falls
   back to the seeded words or to `{Term} [id: n]`.
5. The summary text is the `describe<Answer>Summary` function the CLI renderer uses, in the approved wording of
   `discuss/cli-sketches.md`. Tables are never put into a summary.
6. One helper, `withSummary(label, facts, summary)` in `mcp-core/src/toolResult.ts`, applies these rules. Each
   converted tool's description gains one sentence naming `summary`. Annotations are unchanged.

## Alternatives considered

- **Wrap arrays as `{ summary, items: [...] }`.** Rejected: it changes the shape of facts that agents and
  prompts already read (`teams[7]{…}` in TOON), against "facts unchanged apart from the summary".
- **A `summary:` line inside the facts block.** Rejected: a consumer that parses the text after `label: ` as
  JSON (TOON's fallback) would fail.
- **A second block for every tool, objects included.** It would give one convention for all tools. Rejected,
  because it departs from the refinement tool, which the maintainer named as the model, and that tool would
  then stand alone.

## Consequences

- Positive: every converted tool's facts are byte-identical (arrays) or key-identical with one added field
  (objects), and a test can pin that. The summary and the CLI share one function, so they cannot disagree.
- Negative: there are two conventions, depending on the answer's shape. The tool description names which one
  applies.
- Negative: an MCP client that shows only the first content block would not show the summary of a list. Probe:
  an `mcp-http` in-process e2e asserts that the second block reaches an SDK client. The maintainer was asked to
  confirm the convention (feature delta, "Open for the maintainer").
- Negative: summaries cost up to two extra reads per tool call (each with the client's connectivity check). On
  `mcp-http` that cost is per request. This was accepted, as for the refinement tool.
- `lighthouse-mcp-core` takes a minor bump for each slice that converts a tool.

## Amendment — 2026-10-10 (Bug #6254): the Delivery list is an object answer

The Context above lists `delivery_list` among the array answers. That stopped being true when the server began
answering a Portfolio's Deliveries as `{active, archived}` (first released in v26.8.31.7); the clients now read and
pass on that object. So `lighthouse_delivery_list` follows rule 1: its `summary` is a field of the answer object and
counts the running Deliveries under `active`, and its description says so. The decision itself is unchanged; only
which rule this one tool falls under moved.
