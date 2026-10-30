# Sources and how far each was verified

The rules in `SKILL.md` are summarised in our own words from three bodies of work. Research ran on
2026-10-30 from public pages only; the books themselves were not read cover to cover. Treat a rule
marked *practice* below as sound general guidance that the source is consistent with, not as a quote.

## Storytelling with Data — Cole Nussbaumer Knaflic (Wiley, 2015)

Best verified of the three: the author's own blog covers most rules directly.

- Verified on the author's site: pies and donuts (avoid; if forced, few ordered slices, direct labels),
  3D (never), dual y-axes (alternatives: label directly, stack two panels on a shared x-axis, common
  units), zero baseline for bars (dots/lines may start elsewhere), direct labels over legends,
  accessibility (contrast, text size, jargon), annotation, bullet graphs, stacked bars.
  - https://www.storytellingwithdata.com/blog/2020/5/14/what-is-a-pie-chart
  - https://www.storytellingwithdata.com/blog/2016/2/1/be-gone-dual-y-axis
  - https://www.storytellingwithdata.com/blog/2020/2/19/what-is-a-bar-chart
  - https://www.storytellingwithdata.com/blog/2018/6/26/accessible-data-viz-is-better-data-viz
  - https://www.storytellingwithdata.com/blog/2017/5/25/the-bullet-graph
- From summaries / background, not a primary page: Gestalt grouping, preattentive attributes, the
  "Big Idea" and 3-minute story, the declutter → grey → one accent → takeaway-title makeover recipe.
- Not examined: the *Let's Practice!* workbook, the podcast.

## The Big Book of Dashboards — Wexler, Shaffer, Cotgreave (Wiley, 2017)

Weakest web coverage (publisher and author pages refused automated reads).

- Verified: a dashboard is a visual display of data used to monitor conditions and/or facilitate
  understanding; the "right information, right audience, right medium, shortest time" test; start from one
  page; common mistakes (clutter, decoration, colour misuse); ask the audience for feedback often;
  dashboards must evolve; a chapter on red/green and colour-vision deficiency (~8% of men, ~1% of women).
  - https://coolinfographics.com/blog/2017/6/23/the-big-book-of-dashboards.html
  - https://interworks.com/blog/dmurray/2017/06/12/book-review-big-book-dashboards/
- *Practice* (consistent with the book, not confirmed against it): accuracy of position/length over
  angle/area, the scenario → visual table (BAN with context, bullet graph over gauges, small multiples on
  shared axes, actual vs forecast with a range), summary-top/detail-below layout, a separate mobile layout.
- Not verified at all: the authors' conditions under which a pie or dual axis is defensible.

## #MakeoverMonday — Andy Kriebel, Andy Cotgreave, Eva Murray

- Verified: the process — critique the chart not the author; say what works, what doesn't and why;
  understand the data before visualising (partial periods, outliers); one question per chart; keep it
  simple; time-box (~1 hour); iterate with feedback; credit the source. The pre-publish checklist (chart
  type, audience and mobile, title, consistency, colour with purpose, simplicity, proofreading, sources).
  The 2018 book's chapters on knowing the data, keeping it simple, iterating, and effective use of text.
  - https://www.vizwiz.com/p/makeover-monday-challenges.html
  - https://www.bitfern.co.nz/blog/hone-skills-makeover-monday/
  - https://www.storytellingwithdata.com/blog/2018/11/15/recommended-reading-makeovermonday
- *Practice*: the before → after patterns (pie → sorted bars, dual axis → panels, unlabelled axes →
  units in axis title, no context → reference line and annotation). Weekly viz-review content was not
  retrievable.

## Lighthouse decisions that override the sources

- **Every axis is labelled** even where direct labels would let the books drop it (maintainer,
  2026-10-30, Story 6452).
- **Titles name the thing, not the takeaway**, because product charts redraw on live data; the
  takeaway belongs in a computed caption or BAN.
- **Features over Time keeps its dual axis** (Work Items bars, Feature count line) — two distinct
  questions, both axes labelled.
