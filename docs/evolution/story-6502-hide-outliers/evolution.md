# Story 6502: Hide Outliers on the Cycle Time scatterplot

**Finished:** 2026-11-14 · ADO User Story #6502

## The problem

One very long closed Work Item stretched the y-axis of the Cycle Time scatterplot and squeezed every other dot into the bottom of the chart. A reader could not see where typical work lands, and could not tell what a stretched axis was hiding.

## Delivered

- **S1, with S2 folded into it** (`9c8eccf2c`): the chart does not draw outliers, a Work Item more than 5 times the chart's own median Cycle Time. The y-axis fits the rest. A `(!) N outliers` chip in the widget header (`(!) N` on a phone) opens a popover with the cut, the median, what is still counted, the lines above the axis and up to 10 rows, longest first. Percentile and SLE lines above the axis appear as labelled markers at the top edge. The chart draws its SLE line by default. Empty plots say "All visible Work Items are outliers." or "All types are hidden." S2 (the markers) shipped together with the cap because neither half is honest alone.
- **S3** (`c70d368f3`): "Open in a list" opens all outliers in the Work Items dialog, and the widget's help names the rule.
- **Refactors:** `53b37d34c` (the Cycle Time term is required wherever widget help is worded) and `c4acda4ca` (ask only for the lines above the axis).
- **Follow-ups after the mutation run:** `1577e2f3e` (the chip closes its popover during render instead of in an effect; a test kills the surviving "lines above the axis" mutant) and `88a3c8bfb` (the help states the rule in the seeded terms when no Team or Portfolio is known).
- **S4, after the maintainer's guided review** (oracle `d72dba673`, `53eb4d72f`; build `4bb4058ce`): the chip is amber outlined and opens the outliers straight in the Work Items dialog; the popover is gone. The lines above the axis are named in the chip's accessible name and tooltip instead. The usage event keeps 27 and is renamed `OutlierListOpened`; no release carried the old name. This replaces the popover described in S1 and S3 above.
- Percentiles, the SLE share, RAG, tiles, forecasts and View Data still include every item. No API, DTO or backend behaviour changed.

## Key decisions

- The rule is a pure function in `utils/charts/outliers.ts`, with one constant for the multiple, in the frontend and per chart (ADR-282).
- "Median" is Lighthouse's own 50th percentile, so the median behind the cut always equals the chart's "50%" line.
- Maintainer, 2026-11-14: the chip lives in the widget header; shorter popover and empty-plot copy; markers in the top margin; the scatterplot draws the SLE line by default.
- Maintainer guided review, 2026-11-14 (D13–D16): an amber chip, and the chip opens the list directly instead of a popover.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. `docs/metrics/flow-metrics.md` gained an "Outliers" section under Cycle Time Scatterplot and the SLE sentence now says the line is drawn by default. `docs/metrics/widgets.md` and `docs/metrics/portfolio-features.md` carry no outlier or SLE-default wording that became wrong, so they are unchanged. Terms are the seeded defaults. |
| Screenshots | Done, regenerated in the finalize commit: `docs/assets/features/metrics/cycleScatter.png` and `cycleScatterNamedCycleTime.png`, overwritten in place by `Screenshots.spec.ts`, never renamed. |
| Demo data | Done. Team Zenith carries one outlier, `TZ-301`, guarded by `DemoDataFactoryTest`. |
| Website assets | N/A, because the same two PNGs are overwritten in place and keep their names. A grep of `/storage/repos/website/src` for `cycleScatter` finds nothing, so no page names them. |
| Clients (CLI, MCP) | N/A, because no API or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done. ADR-282 moved to `docs/product/architecture/` (Accepted); ADR-270 glossary has the Outlier row under Flow metrics; `ARCHITECTURE.md` §12 has one line and the ADR index has the row; `brief.md` has a paragraph; `docs/product/jobs.yaml` has the new job. No context, edge or domain event is added. |
| Usage-data event | Done. `OutlierListOpened = 27` (named `OutlierPopoverOpened` before S4), listed in `docs/settings/usagedata.md`. |
| Mutation | Done. Frontend 88.47 % (gate 80 %), backend N/A because nothing there is mutable; see `docs/feature/story-6502-hide-outliers/mutation/README.md`. |
| Release notes | N/A; written at /release. |

## Known open

- EXAMINE rejected the feature on observation gaps only, not on a defect in behaviour.
- Three files stay under 80 % mutation: `outlierWording.ts` (76.74 %), `OutlierChip.tsx` (77.78 %) and `OutlierPopoverContent.tsx` (52.63 %). The overall score meets the gate. These scores are from before the follow-ups, which added tests and were not re-measured.
