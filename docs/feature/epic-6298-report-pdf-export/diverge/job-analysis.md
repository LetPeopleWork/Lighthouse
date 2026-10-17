# Job Analysis — epic-6298-report-pdf-export (DIVERGE, Phase 1)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-17 · **Inputs**: `../feature-delta.md` (DISCOVER),
`../discover/wave-decisions.md`, `docs/product/journeys/epic-6298-report-pdf-export.yaml`, `docs/product/jobs.yaml`
(`job-flow-coach-show-whether-flow-changed`), spike #6052 (`docs/feature/story-6052-server-side-chart-rendering/spike/`),
Epic 5878 DIVERGE precedent, and one maintainer input of 2026-10-17 relayed during this wave: PDF export need not
work in every deployment model, and leaving out Standalone is acceptable if it makes the feature cheaper or simpler.

Evidence grade: DISCOVER failed G1-G3. All problem evidence comes from **one informant, the maintainer** (backlog
#1822 and #4753, both written by the maintainer, and the 2026-10-17 interview). The only customer-originated
signal is #1822's second-hand note that reference customers export screenshots by hand. The opportunity scores
below are **desk estimates by the diverger**, not survey data.

## 1. Raw request

> ADO Epic #6298 *Report PDF Export*: any report can leave Lighthouse as a PDF, so the numbers reach people who
> will not or cannot open Lighthouse. The PDF is a capability of a report, not a report type. The first payload
> is Then & Now.

The request names its solution (a PDF) and its first payload (Then & Now). DISCOVER challenged both (X1, X2).

## 2. Job extraction (5 Whys)

| Layer | Statement | Why go higher? |
|---|---|---|
| Tactical (the request) | "Export a report as a PDF." | Nobody wants a PDF for its own sake. |
| Operational | "Get the numbers I read in Lighthouse into a slide, an email or a hand-over pack." | Why does it have to leave Lighthouse? |
| Operational | "Because the person who needs them will not open Lighthouse (does not know it) or cannot (no access, Standalone install)." | Why do they need the numbers? |
| **Strategic** | "So someone outside the tool (sponsor, client, steering group) can form a view and decide on evidence, on their own and possibly weeks later, without the sender in the room." | Higher is "the engagement or the delivery succeeds", a life-goal answer. **Stop.** |
| **Physical (irreducible function)** | Carry a dated, self-explaining snapshot of a chosen set of numbers across an access boundary, with no re-work, and with nothing on it that the sender did not mean to send. | — |

**First-principles inversion**

1. Activity: screenshotting a chart, cropping it into a slide, or exporting CSV and rebuilding it.
2. Nobody wakes up wanting to make a slide. The sender wants the reader to *understand and act on* numbers the
   sender already trusts; the reader wants to decide without learning a tool.
3. Irreducible function: **a snapshot + its date and windows + enough explanation to read it cold + a carrier
   the reader already uses** (slide deck, email, document, chat). "PDF" is one carrier among several.

## 3. Job statements

**Functional (primary, `flow-coach`; also `delivery-lead-rte` and `product-owner`)**

> When someone who will not or cannot open Lighthouse needs to know where our flow or forecast stands,
> I want to hand them the numbers I already read in Lighthouse in a form that explains itself and says when it
> was taken,
> so they can form a view and decide on their own, without me rebuilding the numbers or narrating every caveat.

Three circumstances share this one job (V3): per engagement (coach hand-over or sponsor update), on a recurring
cadence (team lead or PM status update, steering slide) and ad hoc.

**Emotional**: from burdened ("screenshots again, then cropping them into the slide") to confident ("they read it
on their own, and it says when it was taken"). The sender must not fear that the reader quotes an old number as
today's.

**Social**: the sender looks prepared and professional to a sponsor or client. Lighthouse's numbers travel under
Lighthouse's name, which is also how the tool becomes known to people who have never opened it (the
land-and-expand path the Epic 5878 dissent protects).

**Reader job (candidate, `report-reader`, not created)**: "When I receive numbers from a tool I do not use, I want
to read them without learning the tool, so I can decide or pass them on." Recorded for DISCUSS. It is not a
separate jobs.yaml entry: its success measures (OS3, OS6 below) are already outcomes of the sender's job.

**Relationship to existing jobs**: when the report is a Then & Now, this job carries the result of
`job-flow-coach-show-whether-flow-changed` to its audience. That job's social dimension ("the sponsor can pass the
result upward") already assumes the result can leave Lighthouse; this job is the carrier, and it is independent
of the report type (C1).

## 4. Disruption check

Is there a higher-level job that would make this one unnecessary? **"The outside reader stays informed without
anyone sending anything."** Two things push in that direction:

- **A live read-only link or a scheduled delivery** (#5882). It removes the "send" step for readers who can reach
  the instance. It does not remove the snapshot: a steering pack is archived, forwarded and read weeks later,
  and a Standalone install has no reachable address.
- **The reader's (or sender's) own AI assistant over MCP** (DISCOVER D5). It already composes views. It serves
  the sender who wants a *custom* view and has an assistant; it does not give a reader without access anything
  they can open, and it re-states rather than freezes the numbers (no snapshot date unless the user asks for one).

Neither removes the job. Both are recorded as structurally different options in `options-raw.md`.

## 5. Outcome statements (ODI)

| # | Outcome statement | Source |
|---|---|---|
| OS1 | Minimize the time it takes to get a report's numbers into the place the outside reader already looks (slide, email, document) | O1; #1822; maintainer 2026-10-17 |
| OS2 | Minimize the likelihood that a reader without access to the instance sees no numbers at all | O2; V2 |
| OS3 | Minimize the likelihood that a shared number is read as current when it is not | O4; C6 |
| OS4 | Minimize the number of figures the sender rebuilds by hand outside Lighthouse | O3; CSV and AI-dashboard workarounds |
| OS5 | Minimize the likelihood that the shared output carries names or details the sender did not mean to send outside | O5; Q9; #4753 "anonymise" |
| OS6 | Minimize the likelihood that the outside reader misreads a figure because its meaning (terms, windows, direction of good) is explained only inside Lighthouse | journey step 4; C7 |
| OS7 | Minimize the effort of repeating the same share on a recurring cadence | V3 (recurring team lead / PM cadence); #5882 |

## 6. Opportunity candidates (desk estimates, 1-10)

Score = Importance + max(0, Importance − Satisfaction).

| Outcome | Importance | Satisfaction | Score | Status | Reasoning |
|---|---|---|---|---|---|
| OS3 read as current | 8 | 2 | **14** | **Under-served** | A screenshot or a CSV carries no date or window unless the sender types one in. |
| OS1 time into the slide | 8 | 3 | **13** | **Under-served** | The observed workaround works but costs crop-and-paste for every chart, every time. |
| OS6 misread without explanation | 7 | 2 | **12** | Under-served (borderline) | A cropped chart loses its header, windows and terminology. |
| OS4 rebuild by hand | 7 | 3 | 11 | Appropriately served | CSV and AI dashboards rebuild with effort, but they work for the senders who want to compose. |
| OS7 recurring effort | 6 | 2 | 10 | Appropriately served | Real for the team lead cadence, but #5882 owns unattended delivery. |
| OS2 reader without access | 6 | 5 | 7 | Over-served | Screenshots already reach a reader without access; the gap is effort and honesty, not reach. |
| OS5 unintended names | 6 | 4 | 8 | Over-served | A screenshot shows only what was cropped. It is a **constraint** on every option (Q9), not a differentiator. |

**Consequences for brainstorming.**

- The under-served outcomes are about **honesty and readiness of the snapshot** (OS3, OS6) and **speed into the
  reader's carrier** (OS1). The format matters less than the snapshot being dated, self-explaining and quick to
  drop where the reader looks.
- OS2 scoring low supports the maintainer's 2026-10-17 input: leaving Standalone out costs little, because a
  Standalone user still has screenshots, which already reach a reader without access.
- OS1 is about *what people capture today*, which is metrics charts and forecasts/Deliveries, not Then & Now (X2).
  An option whose first payload carries no chart serves OS3 and OS6 but not the observed OS1 workflow.

## DIVERGE gate G1

Not to be confused with DISCOVER's G1 (problem validation), which failed and stays failed; the maintainer
accepted that risk (DISCOVER D2).

- [x] Job at strategic level (an outside reader decides on evidence, on their own, later) and physical level
  (a dated, self-explaining snapshot across an access boundary).
- [x] No feature reference in the job statement ("PDF" appears only in the raw request and the tactical layer).
- [x] 7 ODI outcome statements; 3 under-served (OS3, OS1, OS6).

**DIVERGE-G1: PASS.**
