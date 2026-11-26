# Job analysis — story-6564-report-notes (DIVERGE)

Raw request: `request.md` in this folder (host-written; requester-verbatim blocks unchanged). DISCOVER job:
`job-flow-coach-keep-the-context-with-the-numbers` in `docs/product/jobs.yaml`.

## 1. Job extraction (5 Whys)

| Layer | Question | Answer |
|---|---|---|
| Tactical | What was asked? | "add notes to reports" (a proposed solution) |
| Operational | Why notes? | A number in a report has no place for the context the tracker does not hold (Team of 3 or 10; a training on a date). |
| Operational | Why does that matter? | A reader who was not in the review compares or misreads the numbers. |
| Strategic | What is the decision behind it? | Whether the reader trusts the number and acts on it, or discounts it. |
| Physical | Irreducible function | Bind explanatory facts to a number so they travel with it. |

Disruption check: a higher job (make numbers self-normalising, e.g. Throughput per person) would remove the
"how large the Team is" half of the job. No Team size, member count or assignee was found in `Models/` (grep, that
folder only; trackers not searched), so that route needs data Lighthouse may not hold.

## 2. Job statements

- **Functional**: When I put a report in front of people who were not there when its numbers were made, I want the facts
  that explain those numbers to sit with the report, so the reader does not misread them.
- **Emotional** (from DISCOVER, a proposal, not an observation): from uneasy about being compared to a bigger team to
  confident that the context travels.
- **Social** (same caveat): be the coach whose report explains itself.

## 3. ODI outcome statements

| # | Outcome | Evidence |
|---|---|---|
| O1 | Minimize the likelihood that a reader misreads a number because a fact about the Team is missing | Jessie (workshop) |
| O2 | Minimize the time it takes a reader to learn what changed in the period a report covers | Peter (workshop) |
| O3 | Minimize the number of caveats a coach must narrate from memory when presenting a report | DISCOVER hypothesis (voice or slide today) |
| O4 | Minimize the likelihood that a reader cannot tell who stated a piece of context | Maintainer: "if auth is on, we should identify who wrote it" |
| O5 | Minimize the effort required to keep a context fact correct across several reports of one Team | Inference from C1 and C2; not observed |

## 4. Under-served candidates

All desk estimates (DISCOVER: importance 3, satisfaction 1; two voices, one organisation). Most under-served in the
evidence: O1 and O2. O3 and O5 are inferences. Confidence stays low to medium.

## 5. The two open questions from discovery (host-summary in `request.md`, not the requester's words)

- C1: is "how large the Team is" a fact to **state** (free text) or a fact to **hold** (a value Lighthouse can reuse,
  for example per-person Throughput)? Not found in `Models/` (see section 1).
- C2: does context that is always true and context tied to a date need one container or two? What does the reader do
  differently with each?
