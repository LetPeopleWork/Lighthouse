# DISTILL — slice 07, Features (US-07)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/featuresView.test.ts` | 11 | 2 |
| `packages/mcp-core/src/featureSummaries.test.ts` | 3 | 0 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 07 | 3 | — |

## Scenarios

- Features as the Feature list shows them: `Feature · Name · Progress · Forecasted Start · Forecasted
  Completion (85%) · State`; OE-001 done, OE-002 in progress, OE-007 `Cannot forecast`.
- Forecasted Start follows `ForecastedStartCell.tsx`: an observed date outranks "Cannot forecast"; else
  the 85% start; `—` when unknown; `Cannot forecast` with no history (4 rows).
- Error/edge: no 85% date → `—`; terms renamed; a Feature without per-Team work → generic view.
- A Feature's Work Items: heading `OE-002 Deep-sea camera stream · 3 Work Items`, columns starting
  `ID Name Type State`; the Feature's name refused or not found → `Feature [id: 2] · 3 Work Items`.
- MCP: `3 Features`; the Work Items heading as a second block; the fallback heading.
- Guards: `--json` / `--toon` for get, and for workitems without any Feature-name read.

## DTO against sketch

OE-007's Forecasted Start: the sketch shows a date, the web's rule gives `Cannot forecast` without an
observed start. The scenarios pin the web's rule.

## Not pinned (open)

The "Owned by" column on a Feature's Work Items: `WorkItemDto` carries no owning Team, and the web shows
that column for Features only.
