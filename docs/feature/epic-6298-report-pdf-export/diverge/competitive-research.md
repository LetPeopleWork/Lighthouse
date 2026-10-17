# Competitive Research — epic-6298-report-pdf-export (DIVERGE, Phase 2)

**Agent**: Flux (`nw-diverger`), research delegated to `nw-researcher` · **Date**: 2026-10-17 · **Depth**:
lightweight (maintainer config): 4 products, 1 non-obvious alternative, about 14 searches and fetches.
Claims are marked **[V]** (verified on a primary source) or **[U]** (unverified).

The job (`job-analysis.md` §3): hand an outside reader the numbers the sender already trusts, in a form that
explains itself and says when it was taken.

## 1. Products

### 1.1 ActionableAgile Analytics (55 Degrees): per-chart image and CSV

- **[V]** A download control in the top navigation exports the current chart as an image, the raw data, or the
  filtered data (the data behind the chart as filtered). The Source Data view also exports CSV. All of it is per
  chart and runs in the browser.
  ([55 Degrees wiki, "Exporting your data"](https://55degrees.atlassian.net/wiki/spaces/AAS/pages/704938405))
- **[U]** Whether the image carries a date or the active filters: not documented.
- No PDF, report bundle, share link or scheduled delivery is documented.
- **Does well**: fastest path into a slide (OS1). The CSV lets an analyst rebuild the numbers (OS4).
- **Fails the job**: one chart per image with no surrounding story (OS6), and no documented date or window stamp (OS3).
- **Assumes**: a practitioner hand-builds the deck and writes the captions and dates.

### 1.2 Nave: the reader as a seat **[U]**

- **[U]** Marketing pages mention an "Executive Dashboard" across Teams
  ([kanban-dashboards](https://getnave.com/kanban-dashboards), [premium](https://getnave.com/premium); search
  snippets only, both fetches failed). No primary source for PDF export, scheduled email or public links was found.
- **Assumes (interpretation)**: stakeholders log in themselves ("unlimited for your entire team"). The outside
  reader becomes a seat, not a recipient. This fails the "cannot" reader (V2).
- Needs a manual check of Nave's help centre before anything relies on it. No insight in §4 and no score in
  `taste-evaluation.md` uses Nave; it is kept only so that the next look starts from what is already known.

### 1.3 Jira Cloud and Azure DevOps built-in dashboards: share the live view

- **[V]** A Jira dashboard can be shared as "Public". Anonymous viewers get limited access, do not see agile boards,
  and Atlassian treats public sharing as a data-exposure risk
  ([anonymous access](https://confluence.atlassian.com/adminjiracloud/allowing-anonymous-access-to-your-project-779172888.html),
  [KB](https://support.atlassian.com/jira/kb/anonymous-users-able-to-see-shared-filters-dashboards-or-project-issues-in-jira/)).
- **[V]** A paid Marketplace add-on (Appfire Dashboard Hub) sells password-optional public links
  ([Appfire](https://appfire.atlassian.net/wiki/spaces/RDD/pages/146310576)). That a separate product exists for
  this suggests demand the platform leaves unmet.
- **[V]** In Azure DevOps, "Copy dashboard" copies a layout to another Team. It is not an export
  ([MS Learn](https://learn.microsoft.com/en-us/azure/devops/report/dashboards/copy-dashboard?view=azure-devops)).
  **[U]** No native PDF or image export was found for either.
- **Does well**: zero effort for readers inside the tenant.
- **Fails the job**: the outside reader needs an account or an anonymous link. The view is live, with no "as of"
  (OS3) and nothing frozen. Screenshots stay the de-facto workaround.
- **Assumes**: the audience is inside the tool's tenant.

### 1.4 Power BI: every form, behind the top tiers

- **[V] Export to PDF or PowerPoint**: server-rendered, one report page per PDF page at a fixed 1280×720, with
  current or default filter values. Sensitivity labels carry over, an admin can switch it off, and it is capped at
  50 pages. Uncertified custom visuals do not render
  ([end-user-pdf](https://learn.microsoft.com/en-us/power-bi/collaborate-share/end-user-pdf)).
  **[U]** An export timestamp on the PDF is not documented.
- **[V] Email subscriptions**: scheduled, with an inline snapshot and a link. A PDF or PPTX attachment needs a
  Premium or PPU workspace (capped at 20 pages and 25 MB). **External recipients get it only on paid Premium
  capacity**; trial and PPU workspaces silently skip them
  ([end-user-subscribe](https://learn.microsoft.com/en-us/power-bi/collaborate-share/end-user-subscribe)).
- **[V] Publish to web**: creating a link needs Pro or PPU and tenant-admin enablement. Viewers need no
  account and no authentication, and anyone can reach the model data. Data is cached for an hour, viewers "may see
  a mix of current and previous values", and it is unavailable with row-level security
  ([service-publish-to-web](https://learn.microsoft.com/en-us/power-bi/collaborate-share/service-publish-to-web)).
- **Does well**: the fullest set: a portable server-rendered file, scheduled push and a public link.
- **Fails the job**: every path that reaches outsiders sits behind the top licence tiers or makes the data fully
  public. The public link is live and can mix data ages, which is the opposite of a dated snapshot (OS3).
- **Assumes**: an admin-governed enterprise with BI licences; readers are colleagues or the general public.

## 2. Non-obvious alternative (different category, same job): an AI assistant over MCP builds a shareable page

This is the workaround observed in DISCOVER (D5): a user connects an AI assistant to Lighthouse's MCP server and
has it build a dashboard.

- **[V]** Claude can build a self-contained HTML page (an artifact) from data pulled through MCP connectors and
  publish it: private by default, organisation-shared with sign-in on Team and Enterprise plans, or by a **public
  link with no sign-in** once an Owner allows it. Pages are versioned, and the author chooses which version
  viewers see. Requires a paid plan. Not available under CMEK, HIPAA or zero-data-retention settings, nor on
  Bedrock, Vertex or Foundry ([artifacts](https://code.claude.com/docs/en/artifacts)).
- **[V]** Pages may call connectors live (since July 2026), but each viewer's own account makes the call. A public or
  outside-the-organisation viewer "sees the page without its live sections". For an outside reader only the
  static snapshot works ([2026-w29](https://code.claude.com/docs/en/whats-new/2026-w29)).
- **Does well**: narrative plus numbers, composed on request; no BI licence; no build cost to Lighthouse.
- **Fails the job**: whether the numbers are right and carry an as-of date depends on the prompt (OS3). A
  mislabelled number reads as authoritative to an outsider (OS6). Organisations that block AI tools or need
  CMEK or zero data retention cannot use it. Every value passes through a third-party model (OS5).
- **Assumes**: the sender already works in an assistant and would rather say "make me a deck" than open an
  export menu.
- **Complement, out of scope**: the maintainer's adjacent idea, a Lighthouse skill that builds dashboards from
  Lighthouse's report types, belongs with the existing Lighthouse agent skills (`docs/feature/story-6217-lighthouse-skills`),
  not with this Epic. It would make this path more trustworthy rather than replace a portable file.

## 3. Lighthouse today (desk and codebase facts)

- No print stylesheet or `window.print` exists anywhere in `Lighthouse.Frontend/src` (searched 2026-10-17).
- A Delivery already exports CSV as "one settled table the calling surface builds" (ADR-172, which superseded
  ADR-162). The lesson recorded there: an export built by **reading the screen back** shipped five wrong
  columns. An export must be built from settled values (C10).
- Then & Now panels carry no charts. Per-claim charts were dropped on 2026-10-03 (Epic 5878 slice 11). A
  chart-bearing report does not exist until Custom Metric Reports (#6299).
- An unauthenticated embed identity (an API key standing in for the viewer) was designed and then withdrawn
  (ADR-129, superseded by ADR-137): the viewer now signs in as themselves. Lighthouse has no precedent for showing
  data without sign-in.
- Spike #6052: headless Chromium works on server and Docker with caveats (+95–110 MB pull, Helm memory limit
  must rise, 18 system libraries on minimal Linux). Tauri print-to-PDF costs ≈0 in Standalone. In-process Jint
  + HarfBuzz + Svg.Skia is near-identical for charts (+9 MB gz per RID, about 3 days of chart refactors). The
  Gotenberg sidecar reaches Docker and Helm only.

## 4. Insights for brainstorming

| # | Insight | Evidence |
|---|---|---|
| I1 | **None of the four products documents an as-of date and windows stamped on the shared output.** Power BI comes closest, through the email's send time. A dated, self-explaining snapshot (OS3, OS6) is open ground. | 1.1, 1.4, 2 |
| I2 | Two families: **live link** (Jira public, Power BI publish to web, Claude live pages) needs an account or makes data public, and mixes data ages without saying so. **Static file** (ActionableAgile PNG and CSV, Power BI PDF and PPTX) is portable but per chart or page-sized, and undated. | 1.1, 1.3, 1.4, 2 |
| I3 | **In 2 of the researched products, reaching outsiders is where the gate sits** (Power BI Premium for external recipients; Claude public links need an Owner to enable them). That is the pattern the Epic 5878 dissent warns against for land-and-expand. Two products are not a market norm. | 1.4, 2 |
| I4 | Self-hosted Lighthouse often has no public route, and Standalone has no address at all, so **a portable file fits better than a public link**. | 1.3, 3 |
| I5 | The AI path is real, free to Lighthouse, and composable. It loses on trust and dating, not on convenience. A Lighthouse output wins only if the tool, not the prompt, fixes the date, windows and terms. | 2 |
| I6 | **A per-chart image is the share form of the closest flow tool researched** (ActionableAgile, 1 of the products), and it matches the observed workaround (screenshots of metrics charts). | 1.1; DISCOVER X2 |

## DIVERGE gate G2

- [x] 3+ real products named with cited behaviours: ActionableAgile, Jira Cloud and Azure DevOps, Power BI (Nave
  recorded as unverified).
- [x] Non-obvious alternative: AI assistant over MCP publishing a shareable page (different category, same job),
  including the observed Lighthouse workaround.
- [x] No generic market claims; each claim is tagged [V] or [U] with a source.

**DIVERGE-G2: PASS.**
