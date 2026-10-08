# ADR-232: SLE Risk and Process Behaviour Charts are named-only metric selections in `lh`

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO User Story #6246). Interaction mode = **propose**.
- **Feature**: `story-6217-lighthouse-skills` — repo `lighthouse-clients` (`client`, `cli`, `mcp-core`)
- **Relies on**: ADR-223 (readers and wording in `client`), ADR-224 (summary beside unchanged facts).

## Context

The Daily Flow Review skill needs two reads the clients lack: SLE Risk per Work Item
(`GET /teams/{id}/metrics/sleRisk`) and Process Behaviour Charts with their signals
(`GET …/metrics/{throughput,arrivals,wipOverTime,totalWorkItemAge,cycleTime}/pbc`, plus `featureSize/pbc` for a
Portfolio). `lh metrics team|portfolio` fans out into one client call per selection and, without `--metrics`,
returns every selection. Adding the two new ones to that default would add six or seven requests to every plain
`lh metrics team` and lengthen a view that #6218 shaped to fit one screen. MCP has one tool per metric, so the
question only arises for the CLI.

Quality attributes: compatibility (scripts and people reading the default view), performance (requests per
command), and one place where each answer is worded.

## Decision

1. `sleRisk` (Team only) and `processBehaviorChart` (Team and Portfolio; aliases `pbc`,
   `processbehaviourchart`) are fetched only when named in `--metrics`. Without `--metrics`, `lh metrics` makes
   the same requests and prints the same output as before; a characterisation test pins this.
2. Both are listed in the help's `Allowed metrics:` line, so they are discoverable (and the drift test of
   ADR-230 requires the general skill to name them).
3. `--json` / `--toon` carry the server's facts unchanged under `sleRisk` and
   `processBehaviorChart.charts.<Type>`; `--pretty` and the MCP tools' `summary` use the same wording functions
   in `client/src/metricsWording.ts`.
4. The MCP tools are `lighthouse_team_metrics_sleRisk`, `lighthouse_team_metrics_processBehaviorChart` and
   `lighthouse_portfolio_metrics_processBehaviorChart` (US spelling, as the shipped `processBehaviorOverTime`
   tools), with `metricType` required on the chart tools.

## Alternatives considered

- **Add both to the default set**, as every other selection is. One rule for all keys. Rejected: every plain
  `lh metrics team` would make up to seven more requests and print five more sections, for answers most readers
  of the headline do not ask for.
- **New commands** (`lh risk team`, `lh pbc team`). Leaves `lh metrics` untouched. Rejected: two more groups
  for what are metrics of a Team, and the skill would teach two shapes for one kind of read.
- **A `--type` flag to narrow the chart selection.** Fewer requests when one chart is wanted. Not taken now:
  the daily needs two of five charts and the cost is parallel GETs; it can be added without breaking anything.

## Consequences

- Positive: nobody's default output or request count changes; the new reads are one word away.
- Negative: the first selections that the default leaves out; the help text has to say so, and the general
  skill does.
- Negative: `processBehaviorChart` and `processBehaviorOverTime` sit side by side; the help and the skill
  explain that one is the chart with its signals today, the other is how its limits moved.
