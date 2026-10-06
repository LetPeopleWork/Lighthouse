# ADR-223: A pretty view reads the answer through a narrow reader and falls back to the generic view; the client's method types stay as they are

- **Status**: Proposed (2026-10-06, DESIGN wave for ADO Story #6218). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6218-readable-cli-output` — repo `lighthouse-clients` (packages `client`, `cli`, `mcp-core`)
- **Extends**: the `PrettyRenderer` seam of story #6147 (`lh refinement get`). Cross-ref: ADR-121 (client-side
  shaping of a server answer).

## Context

Every `lh` command except `lh refinement get` prints the generic view: a recursive `key: value` dump of the
answer. Story #6218 gives each command a renderer that states the answer as the web does. A renderer has to read
fields, but most `LighthouseClient` methods return `LighthouseApiResult<unknown>`: Teams, Portfolios, Features,
Deliveries, both forecasts, most metrics. The few that are typed (`CumulativeStateTimeResult`,
`RecurringBlackoutRule`, …) are casts as well: `requestJson` never checks a shape.

Constraints from DISCUSS:

- `--json` and `--toon` must print byte for byte what they print today and make the same calls.
- Lighthouse servers both older and newer than the CLI are in use. An answer whose shape the renderer does not
  recognise must print the generic view, exit 0, never `undefined` or `NaN`. A field that an older server simply
  does not send (`isOverdue`, `hasSufficientData`) is left unsaid.
- The same sentences are returned by the MCP tools as `summary` (maintainer, 2026-10-06), so whatever checks the
  shape has to be reachable from `mcp-core`, which depends only on `client`.

## Decision

1. **Per answer, a narrow view type and a reader** `read<Answer>(value: unknown): <View> | null` in `client`, beside
   that group's wording (`client/src/<group>Wording.ts`). The view holds only the fields its renderer and summary
   read. The reader checks and picks; it never reshapes, rounds or sorts.
2. **Required vs optional.** A missing or mistyped fact the answer cannot be stated without → `null`. A fact that
   may be absent on an older server → `undefined` in the view, and the renderer says nothing about it or prints
   `—`. In a list, every item needs `id` and `name`, or the whole list is unrecognised.
3. **`LighthouseClient` signatures do not change.** `--json` and `--toon` never pass through a reader.
4. **The fallback lives in one place.** `PrettyRenderer<T>` returns `string | null`; `formatPayload` prints the
   generic view when it gets `null`. In MCP, a `null` view means no `summary`; the facts are returned as today.
5. Where a typed view already exists, the reader returns that type after checking the fields it uses.

## Alternatives considered

- **Type the client methods** (`runManualForecast` → `LighthouseApiResult<ManualForecast>`, and so on). Rejected:
  the type would be a promise nothing keeps (`requestJson` casts), every consumer including `mcp-core` would
  inherit it, and it is false against an older server. D5's fallback would still need a runtime check, so the
  check would exist twice: once in the type and once in the code.
- **zod schemas in `client`.** Rejected for now: it would add a runtime dependency to the package every
  other package depends on, including the Bun-compiled `lh` binary, only to check about twenty narrow views of a
  few fields each. `mcp-core` uses zod for tool input schemas, which is a different job. Revisit if readers grow
  past a few fields each.
- **Guards private to `cli`.** Rejected: `mcp-core` could not reuse them for `summary`, and the wording functions
  in `client` would take an unchecked shape.
- **No fallback; fail on an unknown shape.** Rejected: the answer was read successfully, and the user would get
  an error for a formatting problem. The refinement precedent fails on an unreadable Team. That behaviour stays
  in refinement, and is not copied here.

## Consequences

- Positive: `--json`/`--toon` cannot change through this work, because they never reach a reader. MCP and CLI
  check a shape in the same way. An unexpected server shape gives the old view, not a wrong number.
- Negative: a reader that is wrong falls back silently, which can hide a regression. Mitigations: per-reader tests
  that remove each required and optional field, fixtures that follow the server DTOs' serialised shape, and a
  `--pretty` grep per converted group in CI's `smoke-integration` against the real demo-seeded container.
- Negative: there are two descriptions of each answer, the server DTO and the client view, and they can drift.
  The view lists only the fields it reads, which keeps that drift small, and the real-container smoke detects it.
- Neutral: `PrettyRenderer`'s return type widens. The refinement renderer is unaffected.
