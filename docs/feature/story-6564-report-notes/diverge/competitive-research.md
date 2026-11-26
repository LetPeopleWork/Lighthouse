# Competitive scan — story-6564-report-notes (DIVERGE, rev. 1)

Brief scan, done directly with WebSearch and WebFetch (no researcher dispatch, as the requester asked for short).
Desk evidence from vendor documentation; no product was tried. Quotes below were read on the page, unless a row says
otherwise.

## Prior art in this repo

- Delivery notes (#5639): `Models/DeliveryNote.cs`, `API/DeliveryNotesController.cs`. Plain text, dated at creation,
  author id nullable when auth is off plus the author's name copied at write time, only the author edits, refused when
  the Delivery is archived.
- Reports: `Models/Reports/Report.cs` has an owner (a Team or a Portfolio), a `TemplateKey` and a
  `TemplatePayloadJson`. One report model serves every template, so anything keyed on the report is generic. The
  frontend has `pages/Common/Reports/ReportView.tsx` (live view) and `print/` (PDF).
- Team data: not found in `Models/Team.cs` (grep `-i size`, that file only). A search of `Models/` for `assignee`,
  `capacity`, `memberCount`, `TeamSize` and `contributors` found only forecast code that uses "contributors" for
  histograms. No member count, assignee or capacity is held in the models searched. `Models/` is the whole scope;
  trackers were not searched, so whether a connector can supply assignees is not verified.

## Products

| Product | What it does | Does well | Fails the job | Assumption about the user |
|---|---|---|---|---|
| [Grafana annotations](https://grafana.com/docs/grafana/latest/dashboards/annotations/) | Notes pinned to a time or region on a graph. "Press Ctrl/Cmd and click and drag on the panel to open the Add annotation dialog box." | Context lands on the exact date. "When you hover over an annotation, you can get event description and event tags." | Time-based only; a fact that is always true has no place on a time axis | The reader looks at the chart and hovers |
| [Datadog dashboard annotations](https://docs.datadoghq.com/dashboards/annotations/) | "Annotations let you manually place markers with descriptions on timeseries widgets"; "To annotate a span of time, set both a start and end time." | Spans cover a period | Bound to timeseries widgets | Events are discrete and datable |
| [Power BI data point annotations](https://learn.microsoft.com/power-bi/collaborate-share/service-power-bi-powerpoint-add-in-annotate) | Text callouts on chart data points, in the PowerPoint add-in. "The annotations won't show up when you view the same visual in the Power BI service or any other place" | Annotation text can reference the data point's value | Lives in one slide deck, not in the report; "Annotations aren't supported in custom visuals." | The coach presents from slides |
| [Easy Agile TeamRhythm Team Insights](https://help.easyagile.com/easy-agile-teamrhythm/understanding-team-insights) | A flow-metrics view. Throughput "is worth correlating with the size of the team or other impacting factors such as holidays" (page text, quoted in part) | Names team size as the context Throughput needs | The page offers no notes and no team size field (the only free text it describes is a feedback box) | The reader supplies the context in their head |
| Non-obvious: a Google Sheet as annotation source for Grafana ([doc](https://grafana.com/docs/plugins/grafana-googlesheets-datasource/latest/annotations/), known from a search summary only, page not read) | Events live in a sheet people already edit; Grafana draws them | Events are written once, where people already write, and shown on many dashboards | The report depends on an outside document; no author control | Someone keeps the sheet current |
| Non-obvious: a caption typed under a screenshot or in speaker notes (the DISCOVER habit; a hypothesis) | Free text next to the picture | No learning | Outside the product; lost when the picture is copied | The coach presents live |

ActionableAgile: a search returned only secondary sources and no vendor page with notes or a team size field. Not
verified either way.

## What this means for the options

1. Two mechanisms exist in the sources read: **a mark on the chart at a time** (Grafana, Datadog) and **a caption
   beside the chart** (slides, text on a page).
2. For team size, the one vendor page read (TeamRhythm) names it as context and offers no field for it. Not found in
   the pages read (Grafana, Datadog, Power BI, TeamRhythm); not verified absent elsewhere.
3. A chart mark needs a time axis. Not every report panel has one (a Then and Now comparison panel compares two periods).
4. Events kept once and shown in many places have a precedent (a sheet as the annotation source for Grafana).
