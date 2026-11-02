# Competitive and prior-art research

Method note: web search and two full page fetches (PolicyViz, Desbarats). No `nw-researcher` sub-agent was dispatched;
findings below are from search results unless marked "fetched". Search results are summaries of vendor pages, not the
pages themselves, so treat product behaviour as indicative. **Absence of a feature in a result is not proof it does not
exist.**

## How established tools handle a long tail on a cycle time view

| Product | Behaviour found | Assumption about the reader | Where it fails the job |
|---|---|---|---|
| **ActionableAgile** (cycle time scatterplot) | Fixed axes (completion date x cycle time). The reader zooms into a subset with the Layout control and the stats box then changes to match the subset. Percentile lines are configurable, default 85%. | The reader decides what to look at; the numbers follow the view. | Numbers following the zoom is the thing Lighthouse must **not** do (O3): a clipped view silently changes the P85. No indicator of what was left out was found. |
| **Jira Cloud** cycle time report | Scatterplot of shipped issues against the weekly median; the report only includes issues with a cycle time under 12 months. | A cut-off by age is acceptable. | Drops items entirely by a fixed rule, and the reader is told in docs rather than on the chart. |
| **Azure DevOps** Cycle Time / Lead Time widgets | Scatter-plot control chart, moving average and standard-deviation band. No outlier exclusion found. | Show everything; the band tells you what is unusual. | An old item still squeezes the plot. |
| **Great Gadgets for Azure DevOps** (Stonikbyte; Control Chart and Histogram widgets) | A threshold line to flag unusually long cycle times, a standard-deviation setting to identify outliers; the Histogram widget can exclude breaches from the average. | Flagging and statistics are separate switches. | The exclusion changes a statistic, not the picture. |
| **Datadog** heatmap | Y-axis can be clipped to a range, or set to change bounds automatically from an absolute threshold, to remove the "outlier" series; min/max take a number or Auto. | The viewer wants the dense region; the tail is a configuration choice. | No marker for what was clipped was found. |
| **Grafana** heatmap | Y-axis min/max and softMin/softMax; a bucket above the colour max is painted in the max colour (clamping). A percentile-based auto-scale is an open request. | Clamp to the edge and keep going. | Clamping to a colour is a visual form of "off-scale", but nothing says how many or how far. |
| **Easy Agile TeamRhythm** | Legend checkbox per work item type hides/shows them on the chart. | The legend is the control surface. | Types only; no outlier notion. |

Closest to what the story asks for: Datadog (clip by threshold) and Grafana (clamp to the edge). **Nobody found shows a
count plus numbers on click**, which is where Lighthouse would differ.

## What the data visualisation literature says

- **Jon Schwabish, PolicyViz, "Break the frame" (fetched, 2022).** Four treatments of an outlier that stretches the axis:
  (1) two graphs, full and without the outlier ("makes more detailed comparisons across the other nine countries more
  difficult" otherwise); (2) label or colour the outlier; (3) **break the frame**: let the outlier mark run past the
  chart's border, which he recommends ("the frame becomes a useful aspect of the graph because it can be 'broken'");
  (4) **break the bar** with a diagonal slash: discouraged, "distorts the data and is an arbitrary decision as to where
  you cut the bars". Maps to our options 1 (frame), 3 (two views), 2 (label).
- **Nick Desbarats, Practical Charts (fetched, 2025).** Excluding the outlier "isn't ideal"; an axis break is "not a
  great solution either"; a log scale: "most audiences struggle to read values on logarithmic scales accurately";
  his preferred method "in virtually all situations" is an **inset chart** showing the zoomed part with the full range
  alongside. Caveat he states: in Power BI / Tableau style dynamic charts, the link between the two views is less
  obvious. Maps to option 3 (inset) and counts against option 5 (scale).
- **Axis-break tooling (search; ggbreak R package, Highcharts `xAxis.breaks`, matplotlib broken-axis example).** A break
  keeps the original scale and adds a visual cue that the axis is discontinuous; a research article calls it simpler
  than alternatives for outlier data. The literature that is not tooling documentation is cautious.
- **Tukey fences and skew (search; MetricGate, Hubert and Vandervieren 2008).** The 1.5 x IQR rule is a convention tied
  to normality; on right-skewed data it over-flags the upper tail. Low-authority sources; the 2008 paper itself was not
  read. Used only to reject rule C.
- **Lighthouse's own `visualization` skill** (Knaflic, Wexler et al., #MakeoverMonday, with Lighthouse overrides):
  "A cycle time scatterplot keeps one dot per item, because the outliers are the conversation"; bars start at zero; every
  axis labelled; "one 300-day outlier" is named as an awkward case a sketch must be checked against; reads at phone
  width, dark mode, enlarged and in the PDF. These constrain every option below.

## Non-obvious alternatives (different category, same job)

- **Boxplot / violin in statistics tools:** the outliers are drawn as separate points beyond a whisker, never dropped.
  Lesson: separate *marking* from *scaling*.
- **Map insets (Alaska/Hawaii on a US map):** the display convention of moving the awkward thing to a labelled inset so
  the main area keeps a useful scale. Lesson: option 3.
- **Prison Policy Initiative's charts (via PolicyViz):** the outlier bar leaves the frame with its value written on it.
  Lesson: option 1.
- **Clamped-colour heatmaps (Grafana):** values beyond the scale take the end colour. Lesson: option 1's off-scale mark.

## Gate G2

3+ real products named (7), non-obvious alternatives included (4), no generic market claims. Evidence-quality caveat
above stands.
