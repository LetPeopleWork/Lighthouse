# Wave Decisions — epic-5878-baseline

ADO **#5878** *Baseline* (ValueFlow, Community). DISCOVER decisions D1-D7 live in
`discover/wave-decisions.md` and `feature-delta.md`.

---

## DIVERGE

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02 · **Interaction mode**: autonomous (subagent)
**Predecessor**: DISCOVER (desk research; G1-G3 fail, maintainer-accepted risk D2).
**Successor**: DISCUSS (`nw-product-owner`). The coordinator dispatches the peer review (`nw-diverger-reviewer`).

### Prior-wave reading

| Read | Status |
|---|---|
| `docs/product/jobs.yaml` (format, latest jobs, no changelog block) | ✓ |
| `docs/product/vision.md` | ⊘ does not exist |
| `feature-delta.md` (DISCOVER, all sections) | ✓ |
| `discover/wave-decisions.md` | ✓ |
| `docs/product/journeys/epic-5878-baseline.yaml` | ✓ |
| Brownfield: `XmRCalculator`, `SpecialCauseType`, `ProcessBehaviourChart`, `Team/PortfolioMetricsService` PBC methods, `AdditionalFieldsHelper`, `TeamDetail`/`PortfolioDetail` tabs | ✓ (targeted) |

### Fixed maintainer decisions (recorded, not re-opened)

- **[D8] Scope.** The feature applies to Teams and Portfolios and covers flow metrics mainly. A Monte Carlo forecast comparison is a Team-only maybe. Source: maintainer 2026-10-02.
- **[D9] Many per owner.** An owner may hold several baselines (reports). Source: maintainer 2026-10-02.
- **[D10] Frozen at creation.** Values are stored at creation, using the settings in force at that moment. Source: maintainer 2026-10-02.
- **[D11] Windows.** The current side is a rolling window, possibly selectable. At creation the user picks the baseline timeframe (e.g. last 30 or 90 days). Source: maintainer 2026-10-02.
- **[D12] Baseline may end in the past.** It is rebuilt once from history at creation and then frozen (e.g. 90 days ending at an engagement start 2 months ago). It must lie inside `DoneItemsCutoffDays` (C6). Source: maintainer 2026-10-02.
- **[D13] Surface.** A new **Reports** tab on Team and Portfolio. "Create Report" → pick a Template; the before/after comparison is the first Template; the Reports/Template concept is general. It is viewed live in-app first. PDF and email export come later: out of scope, but not to be blocked. Source: maintainer 2026-10-02.
- **[D14] 4-week assessment.** No separate feature. A selectable current window plus a baseline frozen at hand-over covers it. Source: maintainer 2026-10-02.
- **[D15] Access and clients.** CLI/MCP are out of scope. Users with edit rights on the Team/Portfolio create reports; readers view. Source: maintainer 2026-10-02.
- **[D16] Comparison items are selected from a list, and each has a rule.** Each item carries a defined trigger or rule that judges it good or not, for example "Cycle Time trending down = good", "WIP stable or down = good", "no signals from the PBC = good". The v1 set is limited. The model must make new items and rules easy to add: an extensible catalog, one registered definition per item (metric source, direction of good, rule). This replaces open item 7's "which mechanism" with "which v1 catalog, which rules, which seam". Source: maintainer 2026-10-02 (sent mid-wave; options were re-shaped around it).

### Artifacts produced

| Path (relative to the workspace) | What it holds |
|---|---|
| `diverge/job-analysis.md` | 5 Whys to strategic/physical level, job statements, candidate reader job, disruption check, 6 ODI outcomes with desk scores, G1 |
| `diverge/competitive-research.md` | ValueFlow (internal), Nave, ActionableAgile; 5 non-obvious alternatives; local codebase evidence; 5 insights; G2 |
| `diverge/options-raw.md` | HMW, 7 SCAMPER + 3 Crazy 8s, curation to 6 with diversity test, catalog item pool, Premium pool, naming pool, G3 |
| `diverge/taste-evaluation.md` | Locked weights, DVF, 6×5 matrix, 5 sensitivity tests, Premium and naming rubrics, post-scoring evidence, G4 |
| `recommendation.md` | Decision, top 3, dissent with trigger, answers to items 7-10, risks |
| `wave-decisions.md` | This section |

### SSOT updates

`docs/product/jobs.yaml` now has `epic-5878-baseline` in `feature_context` and a new job,
**`job-flow-coach-show-whether-flow-changed`**. The changelog convention is as in earlier waves: there is no
changelog block, so the job's `created:` and `note:` fields are the record, and `schema_version` stays 1. The
journey file was not edited. DISCUSS re-points its two proposed job ids.

### Decisions taken in this wave

- **DV-1. The job is to judge change against a fixed yardstick.** At the strategic level the job is to decide
  whether to keep, extend or stop a change to the way of working. At the physical level it is to compare two
  states of one process with a "before" fixed in advance and a rule agreed before looking, separating signal
  from routine variation. The under-served outcomes are OS2 (the before moves) and OS3 (noise presented as
  change).
- **DV-2. Recommended direction: Option 3, "Claims checklist" (4.35).** The selectable unit is a claim (metric,
  direction of good and rule fused). It leads under all 5 sensitivity tests. Runner-up: Option 2, Signal
  catalog (4.00). Dissent: Option 1, Threshold scorecard (3.90), with a pre-committed trigger.
- **DV-3. "Trending" means a detected shift against XmR limits frozen from the Then window, not a slope or a
  percentage.** There are four rule kinds (`ShiftInDirection`, `NoShiftAgainst`, `NoSignalsInWindow`,
  `ThresholdChange`) and four verdict states (Holds, Does not hold, No change yet, Not enough data). It reuses
  `XmRCalculator.Calculate(thenValues, nowValues)`.
- **DV-4. The v1 catalog has 6 claims** (C1-C6 in `recommendation.md`), C1-C4 preselected. The Team-only
  forecast lens and five more are nice-to-have registrations.
- **DV-5. The extension seam is one `ComparisonItemDefinition` per claim, and rule kinds are classes.**
  Templates are a preset selection. The catalog is independent of Reports.
- **DV-6. Freeze every applicable claim at creation**, not only the selected ones. Claims added later show "Not
  captured for this report". A settings fingerprint flags when Now runs under different settings.
- **DV-7. Community gets every claim and up to 2 reports per Team/Portfolio; Premium is unlimited.** Rules and
  claims are never gated. The export tier is decided when export is built (the dissent favours a Community PDF).
- **DV-8. The template is named "Then & Now"** (4.55; "Before & After" 4.50). There is no user-facing
  "Baseline", because it collides with the PBC setting.
- **DV-9. Usage data**: DEVOPS designs it. The candidate is a name-only event for report creation.

### Flagged, not resolved

- ValueFlow is not publicly verifiable (404 on 2026-10-02); the evidence is the internal demo only.
- The Nave Executive Report wording comes from a search snippet; only the home-page claim was verified.
- The opportunity scores are desk estimates; DISCOVER G2 stays FAIL.
- Downward detection is asymmetric on zero-clamped series (R-D).

### Resolved by the maintainer (2026-10-02)

1. **The template is named "Then & Now"**, which confirms DV-8.
2. **Community cap: 2 reports per Team/Portfolio, counted across all report templates**, not per template.
   Then & Now is only the first template and more reporting is planned (Epics 5882 and 5935), so the cap bites
   harder as templates are added. Premium stays unlimited. This confirms and sharpens DV-7.
3. **C6 (85th percentile Cycle Time down by 10% or more) stays in v1**, which confirms DV-4.
