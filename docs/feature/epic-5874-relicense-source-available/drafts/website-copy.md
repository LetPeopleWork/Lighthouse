# Draft: website copy for slice 03

**Repo**: `LetPeopleWork/website` · **Status: draft, do not land before slice 01.**

Every change is below with its exact current text, so the slice is mechanical.
Fourteen sites. Thirteen are straight substitutions. **One is a decision, and it
was taken on 2026-09-11 — see "The `/compare` FAQ question" at the end.**

**Record the `/compare` organic-traffic baseline before merging.** Once this
ships the before-window is gone and the traffic KPI is unmeasurable.

---

## Straight substitutions

### `src/components/Hero.tsx:27`
```
- Open-source flow metrics and forecasting
+ Source-available flow metrics and forecasting
```

### `src/components/LighthouseSection.tsx:131`
```
- Community edition free forever • 100% open source • Self-hosted on Windows, macOS, Linux, or Docker
+ Community edition free forever • Source available on GitHub • Self-hosted on Windows, macOS, Linux, or Docker
```
"100% open source" has no honest equivalent — the "100%" was doing the work.
"Source available on GitHub" says the checkable thing instead.

### `src/components/SEO.tsx:26` — keyword list
```
- …, scrum metrics, kanban metrics, agile delivery, predictability, open source agile tool
+ …, scrum metrics, kanban metrics, agile delivery, predictability, self-hosted agile tool, source available
```
Dropping "open source agile tool" costs a keyword we no longer match.
"self-hosted agile tool" is the nearest term that is both true and searched.

### `src/components/SEO.tsx:60` and `src/pages/Index.tsx:24` — identical strings
```
- "Makers of Lighthouse, the open-source flow metrics and forecasting tool, and Sizing Poker, …"
+ "Makers of Lighthouse, the source-available flow metrics and forecasting tool, and Sizing Poker, …"
```

### `src/pages/Index.tsx:59` — JSON-LD product description
```
- "Open-source flow metrics and forecasting tool. Connects to Jira, …"
+ "Source-available flow metrics and forecasting tool. Connects to Jira, …"
```

### `src/pages/Index.tsx:68` — JSON-LD Community edition
```
- "Free open-source edition with core flow metrics and forecasting features. Capped to 3 teams and 1 portfolio."
+ "Free edition with core flow metrics and forecasting features. Capped to 3 teams and 1 portfolio."
```
"Free source-available edition" is a mouthful and the source-available fact is
already stated at product level on the same page. Just "Free edition" here.

### `src/pages/Index.tsx:118` — page description
```
- …you can defend. Open source, self-hosted, free to start, and a 30-day Self-Service trial…
+ …you can defend. Source available, self-hosted, free to start, and a 30-day Self-Service trial…
```

### `src/pages/Lighthouse.tsx:1307` — pricing comparison row
```
- { feature: "100% Open Source (MIT License)", community: true, self: true, enterprise: true },
+ { feature: "Source available — read and audit the code", community: true, self: true, enterprise: true },
```
Stays true for all three tiers, so the row keeps its shape. The MIT parenthetical
goes; naming a specific licence in a pricing table means editing the table every
time the licence changes.

### `public/manifest.json:4` and `README.md:82` — identical strings
```
- "Transform your organization with Lighthouse - the leading open-source flow metrics and forecasting tool."
+ "Transform your organization with Lighthouse - the leading source-available flow metrics and forecasting tool."
```

### `public/compare/index.html` — meta and body copy
```
:7    meta description   "…hosting model, open source, pricing, and integrations. Lighthouse is the open-source, self-hosted alternative."
                      →  "…hosting model, licensing, pricing, and integrations. Lighthouse is the source-available, self-hosted alternative."

:13   og:description     "…hosting, open source, pricing model, and integrations."
                      →  "…hosting, licensing, pricing model, and integrations."

:85   body               "Lighthouse is the open-source, self-hosted alternative: free to start, …"
                      →  "Lighthouse is the source-available, self-hosted alternative: free to start, …"

:97   table row          <th>Open source</th><td>Yes, 100% (GitHub)</td><td>No</td><td>No</td>
                      →  <th>Source available</th><td>Yes, public on GitHub</td><td>No</td><td>No</td>

:113  body               "Because it is open source, the tool survives budget cuts and vendor decisions: if you ever stop paying, your data and the tool stay with you."
                      →  "Because the source is public and you run it yourself, the tool survives budget cuts and vendor decisions: if you ever stop paying, your data and the tool stay with you."
```
The row at :97 is still a real differentiator — both competitors are closed SaaS,
so `Yes / No / No` reads exactly as it did. The sentence at :113 needs rewriting
rather than swapping, because "open source" was the *reason* in it; the reason is
now self-hosting plus a readable source, which is what actually delivers that
outcome anyway.

### `public/compare/index.html` — FAQ answers 2 and 3
```
:35   "…Lighthouse is open source and self-hosted, with a free Community edition…"
   →  "…Lighthouse is source available and self-hosted, with a free Community edition…"

:43   "…is open source, runs on your own infrastructure, and uses flat-rate pricing…"
   →  "…is source available, runs on your own infrastructure, and uses flat-rate pricing…"
```

---

## The `/compare` FAQ question — the one decision

This is a schema.org `FAQPage` entity, and the phrase sits in the **question**,
not the answer:

```json
"name": "Is there an open-source alternative to ActionableAgile and Nave?",
"acceptedAnswer": { "text": "Yes. Lighthouse … is an open-source, self-hosted flow metrics and Monte Carlo forecasting tool. …" }
```

A question cannot be word-swapped. It is phrased that way because it *is* the
search query — it is what makes the page eligible for a rich result on "open
source alternative to actionableagile". Keeping it and changing only the answer
would leave us answering "Yes" to a premise we no longer satisfy, in
machine-readable form, on our own site.

**Decision, 2026-09-11: change the question to one we do satisfy.** We forfeit
the open-source query rather than answer it evasively or decline it on our own
comparison page.

```json
- "name": "Is there an open-source alternative to ActionableAgile and Nave?",
- "text": "Yes. Lighthouse by LetPeopleWork is an open-source, self-hosted flow metrics and Monte Carlo forecasting tool. It connects to Jira, Azure DevOps, Linear, and ServiceNow, has a free Community edition, and flat-rate paid tiers instead of per-user pricing. Your delivery data stays on your own infrastructure."

+ "name": "Is there a self-hosted alternative to ActionableAgile and Nave?",
+ "text": "Yes. Lighthouse by LetPeopleWork is a self-hosted flow metrics and Monte Carlo forecasting tool that runs on your own infrastructure. Its source is public on GitHub, so you can read and audit exactly what it does with your delivery data. It connects to Jira, Azure DevOps, Linear, and ServiceNow, has a free Community edition, and flat-rate paid tiers instead of per-user pricing."
```

Why this phrasing:

- **"self-hosted alternative to \<competitor\>" is a query people actually run**,
  and unlike the old one we satisfy it completely and permanently.
- The answer still carries the source-is-public fact, but as the *reason you can
  trust the data claim* rather than as a licence badge. That was always the part
  doing the persuading.
- It leads with "Yes" again, which the old one did and which reads well in a rich
  result.

**What this costs, so nobody is surprised:** we lose eligibility on the
open-source query, which is the phrase this page currently ranks for. That is the
single largest measurable cost in the whole relicensing, which is why the
traffic baseline has to be captured before this merges.

---

## Do not touch

`src/pages/AI.tsx` and `src/components/AIIntegrationSection.tsx` say
"open-source" about the **LetPeopleWorkShop and LetPeopleGrow Claude Code
plugins**. Those are MIT and stay MIT. Changing them would introduce a false
statement. Verify at the end of the slice that they are untouched — a repo-wide
find-and-replace is exactly how they get caught in the blast.
